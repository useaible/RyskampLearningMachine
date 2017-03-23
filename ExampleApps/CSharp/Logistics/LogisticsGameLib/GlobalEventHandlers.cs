using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace LogisticsGameLib
{
    public class GlobalEventHandlers
    {
        public void DeliveryEventHandler(object obj, ElapsedEventArgs e, Player destination, Order order, string SourceName = null)
        {
            //Order currOrder;

            //destination.Orders.TryDequeue(out currOrder);

            destination.Inventory = destination.Inventory + order.Amount;
            destination.Expected = destination.Expected - order.Amount;
            destination.CurReceive = order.Amount;
            ((Timer)obj).Stop();
        }

        public void ManufacturingEventHandler(object sender, ElapsedEventArgs e, Player factory, int amountPerDay)
        {
            factory.CurReceive = amountPerDay;
            factory.Inventory = factory.Inventory + amountPerDay;

            if (factory.Expected >= amountPerDay)
            {
                factory.Expected = factory.Expected - amountPerDay;
            }
            else
            {
                factory.Expected = 0;
            }

            ((Timer)sender).Stop();
        }
        public void Servicing_OrderEventHandler(object obj, ElapsedEventArgs e, Player player, Order order)
        {
            player.Receive(order);
            ((Timer)obj).Stop();
        }
    }
}
