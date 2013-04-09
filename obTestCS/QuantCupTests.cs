/* Besides crashing, the only way to sense 
   what is happening internally via the 
   required API functions is to submit 
   combinations of orders that trigger
   execution notifications. Normally the 
   tests would scale up more smoothly
   but because of the api's opacity, 
   the tests require implementation of 
   multiple bits of base functionality
   in limit, cancel, and execution to even 
   get to the most basic nontrivial tests. */



using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BitPrice.OrderBook;

/* Order Id */
using t_orderid = System.UInt64;

/* Price
    0-65536 interpreted as divided by 100
    eg the range is 000.00-655.36 
    eg the price 123.45 = 12345
    eg the price 23.45 = 2345 
    eg the price 23.4 = 2340 */
using t_price = System.UInt16;

/* Order Size */
using t_size = System.UInt64;

/* Side 
    Ask=1, Bid=0 */
using t_side = System.Int32;
using System.Collections;



    
class QuantCupTests<T> where T : IOrderBook
{
    const int MAX_EXECS = 100;

    /* Maximum price of a limit order 
      655.36 dollars 
      max size of unsigned short */
    const int MAX_PRICE = 65536;

    /* Minimum price of a limit order 
       0.01 dollars */
    const int MIN_PRICE = 00001;

    /* Maximum number of uncrossed orders that
       may be sitting on the book at a time.
       Gives the implementor a finite bound */
    const int MAX_LIVE_ORDERS = 65536;

    /* Maximum number of characters in both
       symbol and trader fields in order */
    const int STRINGLEN = 5;

    /* Limit Order */ 
    struct t_order {
        public string symbol;
        public string trader;
        public t_side side;
        public t_price price;
        public t_size size;

        public t_order(string s, string t, t_side sd, t_price p, t_size sz)
        {
            symbol = s;
            trader = t;
            side = sd;
            price = p;
            size = sz;
        }
    };

    /* Execution Report 
        send one per opposite-sided order 
        completely filled */
    struct t_execution {
        public string symbol;
        public string trader;
        public t_side side;
        public t_price price;
        public t_size size;

        public t_execution(string s, string t, t_side sd, t_price p, t_size sz)
        {
            symbol = s;
            trader = t;
            side = sd;
            price = p;
            size = sz;
        }
    };

    int is_ask(t_side side) { return side; }

    uint correct = 0;
    t_orderid orderid;
    uint totaltests = 0;
    t_execution[] execs_out = new t_execution[MAX_EXECS];
    int execs_out_iter;
    uint execs_out_len;
    bool exec_overflow;

    t_order oa101x100 = new t_order("JPM", "MAX", 1, 101, 100);
    t_order ob101x100 = new t_order("JPM", "MAX", 0, 101, 100);
    t_order oa101x50 =  new t_order("JPM", "MAX", 1, 101, 50);
    t_order ob101x50 =  new t_order("JPM", "MAX", 0, 101, 50);
    t_order oa101x25 =  new t_order("JPM", "MAX", 1, 101, 25);
    t_order ob101x25 =  new t_order("JPM", "MAX", 0, 101, 25);
    t_order ob101x25x = new t_order("JPM", "XAM", 0, 101, 25);

    t_execution xa101x100 = new t_execution("JPM", "MAX", 1, 101, 100);
    t_execution xb101x100 = new t_execution("JPM", "MAX", 0, 101, 100);
    t_execution xa101x50 =  new t_execution("JPM", "MAX", 1, 101, 50);
    t_execution xb101x50 =  new t_execution("JPM", "MAX", 0, 101, 50);
    t_execution xa101x25 =  new t_execution("JPM", "MAX", 1, 101, 25);
    t_execution xb101x25 =  new t_execution("JPM", "MAX", 0, 101, 25);
    t_execution xb101x25x = new t_execution("JPM", "XAM", 0, 101, 25);

    IOrderBook orderBook;
    void init()
    {
        prodStrId = new Dictionary<string, ushort>();
        prodIdStr = new Dictionary<ushort, string>();
        tradStrId = new Dictionary<string, uint>();
        tradIdStr = new Dictionary<uint, string>();
        nextProdId = 0;
        nextTradId = 0;
        orderBook = (IOrderBook)Activator.CreateInstance(typeof(T));
        orderBook.Execution += orderBook_Execution;
    }

    void orderBook_Execution(object sender, Tuple<OrderExecution, IOrderBook> args)
    {
        var fsExec = args.Item1;
        t_execution buyExec = new t_execution(prodStrFromId(fsExec.Product), tradStrFromId(fsExec.Buyer), 0, (ushort)fsExec.Price, (ulong)fsExec.Size);
        t_execution selExec = new t_execution(prodStrFromId(fsExec.Product), tradStrFromId(fsExec.Seller), 1, (ushort)fsExec.Price, (ulong)fsExec.Size);
        execution(buyExec);
        execution(selExec);
    }

    void destroy()
    {
        orderBook = null;
    }

    ushort nextProdId;
    uint nextTradId;
    Dictionary<string, ushort> prodStrId;
    Dictionary<ushort, string> prodIdStr;
    Dictionary<string, uint> tradStrId;
    Dictionary<uint, string> tradIdStr;

    ushort prodIdFromStr(string str)
    {
        if (!prodStrId.ContainsKey(str))
        {
            prodStrId[str] = nextProdId;
            prodIdStr[nextProdId] = str;
            nextProdId++;
        }

        return prodStrId[str];
    }

    string prodStrFromId(ushort id)
    {
        return prodIdStr[id];
    }

    uint tradIdFromStr(string str)
    {
        if (!tradStrId.ContainsKey(str))
        {
            tradStrId[str] = nextTradId;
            tradIdStr[nextTradId] = str;
            nextTradId++;
        }

        return tradStrId[str];
    }

    string tradStrFromId(uint id)
    {
        return tradIdStr[id];
    }

    t_orderid limit(t_order order)
    {
        var fsOrder = new Order(prodIdFromStr(order.symbol), tradIdFromStr(order.trader), order.side == 0? OrderSide.Buy : OrderSide.Sell, order.price, order.size);
        var result = orderBook.Limit(fsOrder);
        orderBook = result.Item2;
        return result.Item1.Item;
    }

    void cancel(t_orderid orderid)
    {
        var oid = OrderId.NewOID((uint)orderid);
        var result = orderBook.Cancel(oid);
        orderBook = result.Item2;
    }

    public void RunTests() {
      Console.Write("ECN Matching Engine Autotester Running\n" +
	     "--------------------------------------\n");  

      // ask
      t_order[] o1 = {oa101x100}; t_execution[] x1 = {}; correct += test( o1 , x1 )? 1u : 0u;; 
      // bid
      t_order[] o2 = { ob101x100 }; t_execution[] x2 = { }; correct += test(o2, x2) ? 1u : 0u; 

      // execution
      t_order[] o3 = { oa101x100, ob101x100 }; t_execution[] x3 = { xa101x100, xb101x100 }; correct += test(o3, x3) ? 1u : 0u; 

      // reordering
      t_order[] o4 = { oa101x100, ob101x100 }; t_execution[] x4 = { xb101x100, xa101x100 }; correct += test(o4, x4) ? 1u : 0u;
      t_order[] o5 = { ob101x100, oa101x100 }; t_execution[] x5 = { xa101x100, xb101x100 }; correct += test(o5, x5) ? 1u : 0u;
      t_order[] o6 = { ob101x100, oa101x100 }; t_execution[] x6 = { xb101x100, xa101x100 }; correct += test(o6, x6) ? 1u : 0u;

      // partial fill
      t_order[] o7 = { oa101x100, ob101x50 }; t_execution[] x7 = { xa101x50, xb101x50 }; correct += test(o7, x7) ? 1u : 0u;
      t_order[] o8 = { oa101x50, ob101x100 }; t_execution[] x8 = { xa101x50, xb101x50 }; correct += test(o8, x8) ? 1u : 0u;
  
      // incremental over fill 
      t_order[] o9 = { oa101x100, ob101x25, ob101x25, ob101x25, ob101x25, ob101x25 }; t_execution[] x9 = { xa101x25, xb101x25, xa101x25, xb101x25, xa101x25, xb101x25, xa101x25, xb101x25 }; correct += test(o9, x9) ? 1u : 0u;
      t_order[] o10 = { ob101x100, oa101x25, oa101x25, oa101x25, oa101x25, oa101x25 }; t_execution[] x10 = { xa101x25, xb101x25, xa101x25, xb101x25, xa101x25, xb101x25, xa101x25, xb101x25 }; correct += test(o10, x10) ? 1u : 0u;

      // queue position
      t_order[] o11 = { ob101x25x, ob101x25, oa101x25 }; t_execution[] x11 = { xa101x25, xb101x25x }; correct += test(o11, x11) ? 1u : 0u; 

      // cancel so no execution
      t_order[] o1st12 = { ob101x25 }; t_orderid[] c12 = { 1 }; t_order[] o2nd12 = { oa101x25 }; t_execution[] x12 = { }; correct += test_cancel(o1st12, c12, o2nd12, x12) ? 1u : 0u;

      // cancel from front of queue
      t_order[] o1st13 = { ob101x25x, ob101x25 }; t_orderid[] c13 = { 1 }; t_order[] o2nd13 = { oa101x25 }; t_execution[] x13 = { xa101x25, xb101x25 }; correct += test_cancel(o1st13, c13, o2nd13, x13) ? 1u : 0u;

      // cancel front, back, out of order then partial execution
      t_order[] o1st14 = { ob101x100, ob101x25x, ob101x25x, ob101x50 }; t_orderid[] c14 = { 1, 4, 3 }; t_order[] o2nd14 = { oa101x50 }; t_execution[] x14 = { xb101x25x, xa101x25 }; correct += test_cancel(o1st14, c14, o2nd14, x14) ? 1u : 0u;

      Console.Write("--------------------------------------\n");  
      Console.Write("You got {0}/{1} tests correct.\n", correct, totaltests);
    }

    void execution(t_execution exec)
    {
        execs_out_len++;
        if (exec_overflow || (execs_out_iter == MAX_EXECS))
        {
            exec_overflow = true;
        }
        execs_out[execs_out_iter] = exec;
        execs_out_iter++;
    }

    void set_globals() {
      orderid = 0;
      totaltests++;
      exec_overflow = false;
      execs_out_iter = 0;
      execs_out_len = 0;
    }

    bool feed_orders(t_order[] orders) {
      t_orderid id;
      uint i;
      for(i = 0; i < orders.Length; i++) {
        id = limit(orders[i]);
        orderid++;
        if (id != orderid) {
          Console.Write("orderid returned was {0}, should have been {1}.\n", 
	         id, i+1);
          return false;
        }
      }
      return true;
    }

    bool feed_cancels(t_orderid[] cancels) {
      uint i;
      for(i = 0; i < cancels.Length; i++) {
        cancel(cancels[i]); 
      }
      return true;
    }


    bool assert_exec_count(int num_execs_expected) {
      if (exec_overflow) {
        Console.Write("too many executions, test array overflow");
        return false;
      }
      bool correct = execs_out_len == num_execs_expected;
      if (!correct) {
        Console.Write("execution called {0} times, should have been {1}.\n",
	       execs_out_len, num_execs_expected);
      }
      return correct;
    }

    bool exec_eq(t_execution e1, t_execution e2) {
      return e1.symbol == e2.symbol && 
        e1.trader == e2.trader &&
        e1.side == e2.side && 
        e1.price == e2.price && 
        e1.size == e2.size;
    }

    bool assert_execs(t_execution[] execs) {
      uint i;
      for(i = 0; i < execs.Length; i+=2) {
        if(!((exec_eq(execs[i], execs_out[i]) && 
	      exec_eq(execs[i+1], execs_out[i+1])) || 
	     (exec_eq(execs[i], execs_out[i+1]) && 
	      exec_eq(execs[i+1], execs_out[i]))))  {
          Console.Write("executions #{0} and #{1},\n" +
	         "{{symbol={2}, trader={3}, side={4}, price={5}, size={6}}},\n" +
	         "{{symbol={7}, trader={8}, side={9}, price={10}, size={11}}}\n" +
	         "should have been\n" +
             "{{symbol={12}, trader={13}, side={14}, price={15}, size={16}}},\n" +
             "{{symbol={17}, trader={18}, side={19}, price={20}, size={21}}}.\n" +
	         "Stopped there.\n", 
	         i, i+1,
	         execs_out[i].symbol, execs_out[i].trader, execs_out[i].side, execs_out[i].price, execs_out[i].size,
	         execs_out[i+1].symbol, execs_out[i+1].trader, execs_out[i+1].side, execs_out[i+1].price, execs_out[i+1].size,
	         execs[i].symbol, execs[i].trader, execs[i].side, execs[i].price, execs[i].size,
	         execs[i+1].symbol, execs[i+1].trader, execs[i+1].side, execs[i+1].price, execs[i+1].size);
          return false;
        }
      }
      return true;
    }

    /* IN: orders: sequence of orders
       OUT: points received on test */
    bool test(t_order[] orders, t_execution[] execs) {
      bool ok = true;
      set_globals();
      init();
      ok = ok && feed_orders(orders);
      ok = ok && assert_exec_count(execs.Length);
      ok = ok && assert_execs(execs);
      destroy();
      if (!ok) Console.Write("test {0} failed.\n\n", totaltests);
      return ok;
    }

    /* IN: orders: sequence of orders
       OUT: points received on test */
    bool test_cancel(t_order[] orders1, t_orderid[] cancels, t_order[] orders2, t_execution[] execs) {
      bool ok = true;
      set_globals();
      init();
      ok = ok && feed_orders(orders1);
      feed_cancels(cancels);
      ok = ok && feed_orders(orders2);
      ok = ok && assert_exec_count(execs.Length);
      ok = ok && assert_execs(execs);
      destroy();
      if (!ok) Console.Write("test {0} failed.\n\n", totaltests);
      return ok;
    }


}