namespace BitPrice.OrderBook

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

/// Represents a new order
type Order = 
    {Product : ProductId;
        Trader  : TraderId;
        Side    : OrderSide;
        Price   : OrderPrice;
        Size    : OrderSize}

/// Represents both sides of an order execution
type OrderExecution =
    {Product : ProductId;
        Buyer   : TraderId;
        Seller  : TraderId;
        Price   : OrderPrice;
        Size    : OrderSize}

/// Interface to represent a generic limit order book
type IOrderBook =
    /// Submit a new limit order to the book
    abstract Limit : Order -> OrderId * IOrderBook
    /// Cancel a resting order
    abstract Cancel : OrderId -> bool * IOrderBook
    /// Execution notification event
    [<CLIEvent>]
    abstract Execution : IEvent<OrderExecution * IOrderBook>
    /// Inspect best resting bid
    abstract BestBid : unit -> OrderPrice option
    /// Inspect best resting ask
    abstract BestAsk : unit -> OrderPrice option

