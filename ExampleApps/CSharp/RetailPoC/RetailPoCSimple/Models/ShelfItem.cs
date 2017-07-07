using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RetailPoCSimple.Models
{
    public class ShelfItem
    {
        public int ItemID { get; set; }
        public int Index { get; set; }
        public string Name { get; set; }
        public System.Drawing.Color DrawingColor { get; set; }
        public System.Windows.Media.Color Color { get; set; }
        public System.Windows.Media.Color PerfColor { get; set; }
        public string ScoreDetailed { get; set; }
        public double Score { get; set; }
        public int Order { get; set; }
        public string ImageUri { get; set; }
    }
}
