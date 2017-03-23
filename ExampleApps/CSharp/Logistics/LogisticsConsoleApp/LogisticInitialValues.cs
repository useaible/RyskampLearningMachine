using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogisticsConsoleApp
{
    public class LogisticInitialValues
    {
        public static double StorageCost = 0.5;
        public static double BacklogCost = 1;
        public static int InitialInventory = 50;
        public static double[] PlayerMinRange = new double[2] { 0, 50 };
        public static double[] PlayerMaxRange = new double[2] { 51, 120 };
        public static double[] FactoryRange = new double[2] { 1, 100 };
        public static IEnumerable<int> CustomerOrders = new List<int>() { 11, 1, 16, 6, 10, 26, 1, 25, 28, 25, 2, 23, 3, 5, 24, 20, 3, 27, 22, 24, 19, 29, 27, 28, 24, 2, 9, 26, 7, 4, 18, 21, 18, 26, 8, 27, 5, 29, 27, 6, 6, 17, 4, 6, 3, 22, 17, 1, 21, 21, 1, 21, 27, 19, 17, 11, 4, 18, 11, 8, 18, 5, 18, 25, 7, 10, 6, 7, 27, 27, 15, 3, 29, 17, 19, 2, 25, 21, 25, 3, 15, 9, 5, 15, 4, 27, 23, 29, 9, 26, 24, 16, 19, 19, 17, 1, 28, 9, 29, 23 };
    }
}
