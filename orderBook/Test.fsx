#load "OrderBook.fs"
open BitPrice.OrderBook

let ob = new BasicOrderBook() :> IOrderBook
let oid = OrderId.Zero
ob.Execution.Add(fun (x, _) -> printfn "%A bought %A %As for %A from %A" x.Buyer x.Size x.Product x.Price x.Seller)
let (oid, ob) = ob.Limit {Product=1us;Trader=1u;Side=OrderSide.Buy;Price=4.0m;Size=10.0m}
let (oid, ob) = ob.Limit {Product=1us;Trader=1u;Side=OrderSide.Buy;Price=5.0m;Size=10.0m}
let (oid, ob) = ob.Limit {Product=1us;Trader=1u;Side=OrderSide.Buy;Price=6.0m;Size=10.0m}
let (oid, ob) = ob.Limit {Product=1us;Trader=1u;Side=OrderSide.Sell;Price=7.0m;Size=10.0m}
let (oid, ob) = ob.Limit {Product=1us;Trader=1u;Side=OrderSide.Sell;Price=8.0m;Size=10.0m}
let (oid, ob) = ob.Limit {Product=1us;Trader=1u;Side=OrderSide.Sell;Price=9.0m;Size=10.0m}
ob.BestBid()
ob.BestAsk()
let (oid, ob) = ob.Limit {Product=1us;Trader=3u;Side=OrderSide.Buy;Price=100.5m;Size=1000.0m}
