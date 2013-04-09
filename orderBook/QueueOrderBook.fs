namespace BitPrice.OrderBook

open System.Collections.Generic

type Fifo<'a> when 'a : equality =
    new()={xs=[];rxs=[]}
    new(xs,rxs)={xs=xs;rxs=rxs}
       
    val xs: 'a list;
    val rxs: 'a list;
   
    static member Empty() = new Fifo<'a>()
    member q.IsEmpty = (q.xs = []) && (q.rxs = [])
    member q.Enqueue(x) = Fifo(q.xs,x::q.rxs)
    member q.Take() =
        if q.IsEmpty then failwith "fifo.Take: empty queue"
        else match q.xs with
                | [] -> (Fifo(List.rev q.rxs,[])).Take()
                | y::ys -> (Fifo(ys, q.rxs)),y

/// Internal representation of order book entries
type private OrderBookEntry =
    {OrderId : OrderId;
        Size    : OrderSize;
        Trader  : TraderId}

/// Internal representation of price points
type private OrderBookPricePoint =
    {Price   : OrderPrice;
        Entries : OrderBookEntry list}

/// Uses a standard list of lists to represent the bids and asks
type QueueOrderBook private (lastOrderId : OrderId, bids : OrderBookPricePoint list, asks : OrderBookPricePoint list, executionEvent : Event<OrderExecution * IOrderBook>) =
    let executeTrade (order : Order, entry : OrderBookEntry, price : OrderPrice) =
        let executedSize = min order.Size entry.Size
        let orderRemainder = {order with Size = order.Size - executedSize}
        let entryRemainder = {entry with Size = entry.Size - executedSize}
        let buyer, seller  = if order.Side = Buy then order.Trader, entry.Trader else entry.Trader, order.Trader
        let execution = {Product = order.Product; Buyer = buyer; Seller = seller; Price = price; Size = executedSize}
        (orderRemainder, entryRemainder, execution)

    new () = QueueOrderBook(OrderId.Zero, [], [], new Event<OrderExecution * IOrderBook>())
        
    member private b.Bids = bids
    member private b.Asks = asks

    member private b.IncrementLastOrderId () = let nextOrderId = OrderId.Increment lastOrderId in (nextOrderId, new QueueOrderBook(nextOrderId, b.Bids, b.Asks, executionEvent))

    member private b.AddOrder order =
        let (newOrderId, _) = b.IncrementLastOrderId()
        let (pricePoints, operator) = match order.Side with
                                      | Buy -> (b.Bids, (>))
                                      | Sell -> (b.Asks, (<))
        let obe = {OrderId = newOrderId; Size = order.Size; Trader = order.Trader}

        let rec insertInto pps = match pps with
                                 | [] -> {Price = order.Price; Entries = obe::[]}::[]
                                 | h::t when operator order.Price h.Price -> {Price = order.Price; Entries = obe::[]}::h::t
                                 | h::t when order.Price = h.Price -> {h with Entries = h.Entries @ [obe]}::t
                                 | h::t -> h::(insertInto t)
        
        let newPps = insertInto pricePoints
        let nob = match order.Side with
                  | Buy -> new QueueOrderBook(newOrderId, newPps, b.Asks, executionEvent)
                  | Sell -> new QueueOrderBook(newOrderId, b.Bids, newPps, executionEvent)

        (newOrderId, nob)

    member private b.CrossOrder order =
        let pricePoints = match order.Side with
                          | Buy -> b.Asks
                          | Sell -> b.Bids
        let (oRem, pps, x) = match pricePoints with
                             | [] -> failwith "Cannot cross order with empty book!"
                             | pp::pps -> match pp.Entries with
                                          | [] -> failwith "Empty price point!"
                                          | e::es -> executeTrade(order, e, pp.Price)
                                                  |> (fun (oRem, eRem, x) -> match eRem.Size > OrderSize.Zero with
                                                                             | true -> (oRem, {pp with Entries = eRem::es}::pps, x)
                                                                             | false -> match es with
                                                                                        | h::t -> (oRem, {pp with Entries = es}::pps, x)
                                                                                        | [] -> (oRem, pps, x))
        let nob = match order.Side with
                  | Buy -> new QueueOrderBook(lastOrderId, b.Bids, pps, executionEvent)
                  | Sell -> new QueueOrderBook(lastOrderId, pps, b.Asks, executionEvent)
        executionEvent.Trigger (x, nob :> IOrderBook)
        (oRem, nob)

    member private b.CancelOrder orderId =
        let rec removeFromEs entries =
            match entries with
            | [] -> ([], false)
            | e::es when e.OrderId = orderId -> (es, true)
            | e::es -> removeFromEs es |> (fun (l, r) -> (e::l, r))
        
        let rec removeFromPs pps = match pps with
                                   | [] -> ([], false)
                                   | h::t -> match removeFromEs h.Entries with
                                             | (l, true) -> match l with
                                                            | [] -> (t, true)
                                                            | e::es -> ({h with Entries = l}::t, true)
                                             | (l, false) -> removeFromPs t
                                                          |> (fun (m, r) -> (h::m, r))

        match removeFromPs bids with
        | (pps, true) -> (true, new QueueOrderBook(lastOrderId, pps, b.Asks, executionEvent))
        | (_, false) -> match removeFromPs asks with
                        | (pps, true) -> (true, new QueueOrderBook(lastOrderId, b.Bids, pps, executionEvent))
                        | (_, false) -> (false, b)
                
    member private b.ProcessOrder (order : Order) =
        match order with
        | _ when order.Size = OrderSize.Zero -> b.IncrementLastOrderId()
        | _ when order.Side = Buy ->
                match b.Asks with
                | h::t when order.Price >= h.Price -> b.CrossOrder order |> (fun (x, y) -> y.ProcessOrder x)
                | _ -> b.AddOrder order
        | _ -> //when order.Side = Sell
                match b.Bids with
                | h::t when order.Price <= h.Price -> b.CrossOrder order |> (fun (x, y) -> y.ProcessOrder x)
                | _ -> b.AddOrder order
        
    interface IOrderBook with
        member b.Limit order =
            b.ProcessOrder order |> (fun (a, b) -> (a, b :> IOrderBook))

        [<CLIEvent>]
        member b.Execution = executionEvent.Publish

        member b.BestBid () =
            match b.Bids with
            | h::_ -> Some h.Price
            | [] -> None

        member b.BestAsk () =
            match b.Asks with
            | h::_ -> Some(h.Price)
            | [] -> None

        member b.Cancel orderId =
            b.CancelOrder orderId |> (fun (a,b) -> (a, b :> IOrderBook))

    