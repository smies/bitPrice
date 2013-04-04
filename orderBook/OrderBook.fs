module OrderBook

type OrderSide =
    | Buy
    | Sell
    member s.Opposite =
        match s with
        | Buy -> Sell
        | Sell -> Buy
    
type OrderSize = decimal

type OrderPrice = decimal

type OrderId = OID of uint32 with
    static member Increment (OID o) = OID(o + 1u)
    static member Zero = OID(0u)

type TraderId = uint32

type ProductId = uint16

type Order = 
    {Product : ProductId;
     Trader  : TraderId;
     Side    : OrderSide;
     Price   : OrderPrice;
     Size    : OrderSize}

type OrderExecution =
    {Product : ProductId;
     Buyer   : TraderId;
     Seller  : TraderId;
     Price   : OrderPrice;
     Size    : OrderSize}

type OrderBookEntry =
    {Size    : OrderSize;
     Trader  : TraderId}

type OrderBookPricePoint =
    {Price   : OrderPrice;
     Entries : seq<OrderBookEntry>}

type IOrderBook =
    abstract Limit : Order -> OrderId * IOrderBook
    abstract Cancel : OrderId -> IOrderBook
    abstract Execution : IEvent<OrderExecution * IOrderBook>
    abstract BestBid : unit -> OrderPrice
    abstract BestAsk : unit -> OrderPrice

type BasicOrderBook(?lastOrderId : OrderId, ?bids : List<OrderBookPricePoint>, ?asks : List<OrderBookPricePoint>) =
    let lastOrderId = defaultArg lastOrderId OrderId.Zero
    let nextOrderId () = OrderId.Increment lastOrderId

    let bids = defaultArg bids []
    let asks = defaultArg asks []

    let executionEvent = new Event<OrderExecution * IOrderBook>()

    let executeTrade (order : Order, entry : OrderBookEntry, price : OrderPrice) =
        let executedSize = min order.Size entry.Size
        let orderRemainder = {order with Size = order.Size - executedSize}
        let entryRemainder = {entry with Size = entry.Size - executedSize}
        let buyer, seller  = if order.Side = Buy then order.Trader, entry.Trader else entry.Trader, order.Trader
        let execution = {Product = order.Product; Buyer = buyer; Seller = seller; Price = price; Size = executedSize}
        (orderRemainder, entryRemainder, execution)

    let rec crossOrder (order : Order, entries : List<OrderBookEntry>, price : OrderPrice) =
        match entries with
        | h::t when order.Size >= h.Size -> executeTrade(order, h, price) |> (fun (x, _, z) -> let nob = new BasicOrderBook(lastOrderId,  crossOrder(x, t, price))
        | h::t -> executeTrade(order, h, price) |> (fun (x, y) -> (x, y::t))
        | [] -> (order, [])

    let insertOrder operator order (pp::pricePoints) =
        | operator order.Price pp.Price ->
    
    interface IOrderBook with
        member b.Limit order =
            let orderId = nextOrderId()
            match order.Side with
                | OrderSide.Buy  -> orderId
                | OrderSide.Sell -> orderId

        member b.Execution = executionEvent.Publish