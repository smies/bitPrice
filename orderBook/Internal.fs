
// NOTE: If warnings appear, you may need to retarget this project to .NET 4.0. Show the Solution
// Pad, right-click on the project node, choose 'Options --> Build --> General' and change the target
// framework to .NET 4.0 or .NET 4.5.

module orderBook.Internal

type OrderDirection =
    | Buy
    | Sell
    
type OrderStr =
    struct
        val direction: OrderDirection
        val price: decimal
        val size: decimal
        val placed: System.DateTimeOffset
    end

type Order =
    {
        direction: OrderDirection
        price: decimal
        size: decimal
        placed: System.DateTimeOffset
    }

type OrderBook =
    abstract bestBid : unit -> Order
    abstract bestAsk : unit -> Order
    abstract willTrade : Order -> bool
    
let buildOrderBook (orders : Order seq) =
    let orderTable = orders
    {new OrderBook with
        member t.bestBid () = orderTable |> Seq.maxBy (fun x -> x.price)
        member t.bestAsk () = orderTable |> Seq.minBy (fun x -> x.price)
        member t.willTrade o =
            match o.direction with
                | OrderDirection.Buy -> o.price >= (t.bestAsk ()).price
                | OrderDirection.Sell -> o.price <= (t.bestBid ()).price}
                
    
