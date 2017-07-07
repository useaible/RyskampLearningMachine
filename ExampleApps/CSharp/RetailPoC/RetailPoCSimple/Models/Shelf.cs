using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RetailPoCSimple.Models
{
    public class Shelf
    {
        private static Random rand = new Random();
        private HashSet<ShelfItem> items = new HashSet<ShelfItem>();

        public int Number { get; set; }
        public IEnumerable<ShelfItem> Items
        {
            get
            {
                return items;
            }
        }

        public void Add(Item item, double metricScore)
        {            
            // todo get item instance from DB and set other needed properties
            //var dcolor = System.Drawing.Color.FromArgb(item.Color);
            items.Add(new ShelfItem() { ItemID = item.ID, Index = Convert.ToInt32(item.SKU), Name = item.Name, Score = metricScore, Order = items.Count + 1,
                ImageUri = item.ImgUri/*DrawingColor = dcolor, Color = System.Windows.Media.Color.FromArgb(dcolor.A, dcolor.R, dcolor.G, dcolor.B)*/ });
        }

        public void Add()
        {
            var color = System.Windows.Media.Color.FromRgb((byte)rand.Next(1, 255), (byte)rand.Next(1, 255), (byte)rand.Next(1, 255));
            items.Add(new ShelfItem() { ItemID = 1, Name = "item 1", Score = rand.Next(0, 101), Color = color, Order = 0 });
        }
    }
}
