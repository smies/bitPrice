using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BitPrice;


namespace obTestCS
{
    class Program
    {
        static void Main(string[] args)
        {
            OrderBook.IOrderBook ob = new OrderBook.BasicOrderBook();
            ob.Execution += ob_Execution;

            foreach (OrderBook.Order order in TestData.Orders())
            {
                var ret = ob.Limit(order);
                ob = ret.Item2;
                Console.WriteLine("Added order #" + ret.Item1.Item);
            }

            Console.ReadLine();
        }

        static void ob_Execution(object sender, Tuple<OrderBook.OrderExecution, OrderBook.IOrderBook> x)
        {
            Console.WriteLine("{0} bought {1} {2}s for {3} from {4}", x.Item1.Buyer, x.Item1.Size, x.Item1.Product, x.Item1.Price, x.Item1.Seller);
        }
    }
}
