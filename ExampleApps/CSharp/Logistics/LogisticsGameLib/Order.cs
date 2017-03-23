using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogisticsGameLib
{
    public class Order
    {
        public Order(string name, int amount)
        {
            // for Alvin's approval
            this.Name = name;
            this.Amount = amount;
        }
        public string Name { get; set; }
        public int Amount { get; set; }
        public int Backlog { get; set; }

        public int Timer { get; set; } = 2; // will be customisable later through the order processing time   

        public int ProcessOn { get; set; } = 0;

    }
}
