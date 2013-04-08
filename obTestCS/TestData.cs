using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BitPrice;
using Microsoft.VisualBasic.FileIO;

namespace obTestCS
{
    public static class TestData
    {
        private static Dictionary<string, ushort> productMap = new Dictionary<string, ushort>() {
			{"SYM", 1},
		};

        private static Dictionary<string, uint> traderMap = new Dictionary<string, uint>() {
			{"ID0", 0},
			{"ID1", 1},
			{"ID2", 2},
			{"ID3", 3},
			{"ID4", 4},
			{"ID5", 5},
			{"ID6", 6},
			{"ID7", 7},
			{"ID8", 8},
			{"ID9", 9},
		};

        public static IEnumerable<OrderBook.Order> Orders()
        {
            TextFieldParser parser = new TextFieldParser(@"order_data.csv");
            parser.TextFieldType = FieldType.Delimited;
            parser.SetDelimiters(",");
            
            while (!parser.EndOfData)
            {
                //Processing row
                string[] fields = parser.ReadFields();
                yield return new OrderBook.Order(productMap[fields[0]],
                                                 traderMap[fields[1]],
                                                 fields[2] == "0" ? OrderBook.OrderSide.Buy : OrderBook.OrderSide.Sell,
                                                 decimal.Parse(fields[3]),
                                                 decimal.Parse(fields[4]));
            }
            parser.Close();
        }
    }
}