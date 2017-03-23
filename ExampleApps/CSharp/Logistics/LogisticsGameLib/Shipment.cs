using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogisticsGameLib
{
    public class Shipment
    {
        public int Amount { get; set; }

        public Order Order { get; set; }
    }
}
