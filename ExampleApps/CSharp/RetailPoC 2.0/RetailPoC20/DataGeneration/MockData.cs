using RetailPoC20.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.IO;
using CsvHelper;
using Microsoft.Win32;

namespace RetailPoC20
{
    public class MockData : IDisposable
    {
        private PlanogramContext _context;
        private List<Item> _items;
        private List<Attributes> _attributes;
        Random rnd;

        public delegate void dataPanelProgress(double value);
        public delegate void dataPanelGrid(bool refresh);
        public event GeneratingData GeneratingDataEvent;
        public event dataPanelGrid Refreshdata;

        public MockData()
        {
            _items = new List<Item>();
            _attributes = new List<Attributes>();
            _context = new PlanogramContext();
            rnd = new Random();
        }

        public MockData(PlanogramContext context)
        {

            _items = new List<Item>();
            _attributes = new List<Attributes>();
            
            _context = context;

            rnd = new Random();
        }
        
        public double GetItemMinimumScore(RPOCSimulationSettings simSettings)
        {
            double retVal = double.MinValue;
            retVal = _context.Database.SqlQuery<double>(@"
                    select 	
	                    min(m1 + m2 + m3 + m4 + m5 + m6 + m7 + m8 + m9 + m10) 
                    from (
	                    select 
		                    a.ID,
		                    iif(@p0 = 0, 0, sum(c.Metric1) * (@p0 / 100)) m1,
		                    iif(@p1 = 0, 0, sum(c.Metric2) * (@p1 / 100)) m2,
		                    iif(@p2 = 0, 0, sum(c.Metric3) * (@p2 / 100)) m3,
		                    iif(@p3 = 0, 0, sum(c.Metric4) * (@p3 / 100)) m4,
		                    iif(@p4 = 0, 0, sum(c.Metric5) * (@p4 / 100)) m5,
		                    iif(@p5 = 0, 0, sum(c.Metric6) * (@p5 / 100)) m6,
		                    iif(@p6 = 0, 0, sum(c.Metric7) * (@p6 / 100)) m7,
		                    iif(@p7 = 0, 0, sum(c.Metric8) * (@p7 / 100)) m8,
		                    iif(@p8 = 0, 0, sum(c.Metric9) * (@p8 / 100)) m9,
		                    iif(@p9 = 0, 0, sum(c.Metric10) * (@p9 / 100)) m10
	                    from Items a
	                    inner join ItemAttributes b on a.ID = b.Item_ID
	                    inner join Attributes c on b.Attributes_ID = c.ID
	                    group by a.ID
                    ) a
                ", simSettings.Metric1, simSettings.Metric2, simSettings.Metric3, simSettings.Metric4, simSettings.Metric5, simSettings.Metric6, simSettings.Metric7, simSettings.Metric8, simSettings.Metric9, simSettings.Metric10).FirstOrDefault();

            return retVal;
        }

        public double GetItemMaximumScore(RPOCSimulationSettings simSettings)
        {
            double retVal = double.MinValue;
            retVal = _context.Database.SqlQuery<double>(@"
                    select 	
	                    max(m1 + m2 + m3 + m4 + m5 + m6 + m7 + m8 + m9 + m10) 
                    from (
	                    select 
		                    a.ID,
		                    iif(@p0 = 0, 0, sum(c.Metric1) * (@p0 / 100)) m1,
		                    iif(@p1 = 0, 0, sum(c.Metric2) * (@p1 / 100)) m2,
		                    iif(@p2 = 0, 0, sum(c.Metric3) * (@p2 / 100)) m3,
		                    iif(@p3 = 0, 0, sum(c.Metric4) * (@p3 / 100)) m4,
		                    iif(@p4 = 0, 0, sum(c.Metric5) * (@p4 / 100)) m5,
		                    iif(@p5 = 0, 0, sum(c.Metric6) * (@p5 / 100)) m6,
		                    iif(@p6 = 0, 0, sum(c.Metric7) * (@p6 / 100)) m7,
		                    iif(@p7 = 0, 0, sum(c.Metric8) * (@p7 / 100)) m8,
		                    iif(@p8 = 0, 0, sum(c.Metric9) * (@p8 / 100)) m9,
		                    iif(@p9 = 0, 0, sum(c.Metric10) * (@p9 / 100)) m10
	                    from Items a
	                    inner join ItemAttributes b on a.ID = b.Item_ID
	                    inner join Attributes c on b.Attributes_ID = c.ID
	                    group by a.ID
                    ) a
                ", simSettings.Metric1, simSettings.Metric2, simSettings.Metric3, simSettings.Metric4, simSettings.Metric5, simSettings.Metric6, simSettings.Metric7, simSettings.Metric8, simSettings.Metric9, simSettings.Metric10).FirstOrDefault();

            return retVal;
        }

        public double GetItemMaxScoreForTop(RPOCSimulationSettings simSettings)
        {
            double retVal = 0;
            int numSlots = simSettings.NumShelves * simSettings.NumSlots;
            double top = Math.Ceiling(Convert.ToDouble(numSlots) / Convert.ToDouble(RPOCSimulationSettings.MAX_ITEMS));

            var topItems = _context.Database.SqlQuery<double>($@"
                    select top {top}
	                    (m1 + m2 + m3 + m4 + m5 + m6 + m7 + m8 + m9 + m10) as Score
                    from (
	                    select 
		                    a.ID,
		                    iif(@p0 = 0, 0, sum(c.Metric1) * (@p0 / 100)) m1,
		                    iif(@p1 = 0, 0, sum(c.Metric2) * (@p1 / 100)) m2,
		                    iif(@p2 = 0, 0, sum(c.Metric3) * (@p2 / 100)) m3,
		                    iif(@p3 = 0, 0, sum(c.Metric4) * (@p3 / 100)) m4,
		                    iif(@p4 = 0, 0, sum(c.Metric5) * (@p4 / 100)) m5,
		                    iif(@p5 = 0, 0, sum(c.Metric6) * (@p5 / 100)) m6,
		                    iif(@p6 = 0, 0, sum(c.Metric7) * (@p6 / 100)) m7,
		                    iif(@p7 = 0, 0, sum(c.Metric8) * (@p7 / 100)) m8,
		                    iif(@p8 = 0, 0, sum(c.Metric9) * (@p8 / 100)) m9,
		                    iif(@p9 = 0, 0, sum(c.Metric10) * (@p9 / 100)) m10
	                    from Items a
	                    inner join ItemAttributes b on a.ID = b.Item_ID
	                    inner join Attributes c on b.Attributes_ID = c.ID
	                    group by a.ID
                    ) a
                    order by Score desc
                "
                , simSettings.Metric1, simSettings.Metric2, simSettings.Metric3, simSettings.Metric4, simSettings.Metric5, simSettings.Metric6, simSettings.Metric7, simSettings.Metric8, simSettings.Metric9, simSettings.Metric10).ToList();

            int slotsAccountedFor = 0;
            for (int i = 0; i < topItems.Count; i++)
            {
                if (slotsAccountedFor + 10 >= numSlots)
                {
                    var remaining = numSlots - slotsAccountedFor;
                    topItems[i] = topItems[i] * remaining;
                }
                else
                {
                    slotsAccountedFor += 10;
                    topItems[i] = topItems[i] * 10D;
                }                
            }

            retVal = topItems.Sum();

            return retVal;
        }

        public Item[] GetItemsWithAttr()
        {
            return _context.Items.Include(a => a.Attributes).OrderBy(b=>b.ID).ToArray();
        }

        
        public async Task Generate(int numItems=5000)
        {

            numItems = this.NumItems;

            //Generate Attributes
            for (int i = 1; i < 201; i++)
            {
                var attr = new Attributes
                {
                    ID = i,
                    Metric1 = RandomDoubleNumber(0.0, 100.0),
                    Metric2 = RandomDoubleNumber(0.0, 100.0),
                    Metric3 = RandomDoubleNumber(0.0, 100.0),
                    Metric4 = RandomDoubleNumber(0.0, 100.0),
                    Metric5 = RandomDoubleNumber(0.0, 100.0),
                    Metric6 = RandomDoubleNumber(0.0, 100.0),
                    Metric7 = RandomDoubleNumber(0.0, 100.0),
                    Metric8 = RandomDoubleNumber(0.0, 100.0),
                    Metric9 = RandomDoubleNumber(0.0, 100.0),
                    Metric10 = RandomDoubleNumber(0.0, 100.0)
                };

                _attributes.Add(attr);

                //System.Diagnostics.Debug.WriteLine($"Attribute {i}");
            }

            double currentRows = 0;
    
            while (currentRows < _attributes.Count())
            {
                currentRows = _context.ItemAttributes.Count();

                _context.ItemAttributes.AddRange(_attributes.Skip(Convert.ToInt32(currentRows)).Take(10));

                //update progress
                var currentProgress = ((currentRows / _attributes.Count()) * 100);
                GeneratingDataEvent?.Invoke(currentProgress, "Generating historical metric data...", false);

                await _context.SaveChangesAsync();
            }

            GeneratingDataEvent?.Invoke(100, "Generating historical metric data...", false);

            //_context.ItemAttributes.AddRange(_attributes);
            //_context.SaveChangesAsync();

            //Generate SKU's and assign attributes
            for (int i = 1; i< numItems+1; i++)
            {
                HashSet<int> numbers = new HashSet<int>();
                //generate 10 unique random numbers from 1-200 which will then be use to
                //set the as the attributes for the item
                while (numbers.Count < 10)
                {
                    numbers.Add(rnd.Next(1, 200));
                }

                //generating a random color
                byte red = (byte)rnd.Next(0, 255);
                byte green = (byte)rnd.Next(0, 255);
                byte blue = (byte)rnd.Next(0, 255);
                //create the color
                Color color = Color.FromArgb(red, green, blue);
   
                var item = new Item
                {
                    ID = i,
                    Name = $"Item{i}",
                    SKU = String.Format("{0:0000}", i),
                    Attributes = _attributes.Where(x => numbers.Any(a => a == x.ID)).ToList(),
                    Color = color.ToArgb() //converts the color to integer to be save in the database
                };

                _items.Add(item);

                //System.Diagnostics.Debug.WriteLine($"Item {i}");
            }

            currentRows = 0;
            GeneratingDataEvent?.Invoke(0, "Generating random products...", false);
            while (currentRows < _items.Count())
            {
                currentRows = _context.Items.Count();

                _context.Items.AddRange(_items.Skip(Convert.ToInt32(currentRows)).Take(100));

                //update progress   
                var currentProgress = ((currentRows / _items.Count()) * 100);
                GeneratingDataEvent?.Invoke(currentProgress, "Generating random products...", false);

                await _context.SaveChangesAsync();

            }

            GeneratingDataEvent?.Invoke(100, "Generating random products...", true);

            //_context.Items.AddRange(_items);
            //_context.SaveChangesAsync();
            //Refreshdata(true);
        }

        public int GetItemsCount()
        {
            var count = _context.Items.Count();
            return count;
        }

        public void DownloadRetailData()
        {
            string sql = @"SELECT
	                        Items.ID AS ItemID,
	                        Items.SKU,
	                        Items.Name,
	                        Attributes.Metric1,
	                        Attributes.Metric2,
	                        Attributes.Metric3,
	                        Attributes.Metric4,
	                        Attributes.Metric5,
	                        Attributes.Metric6,
	                        Attributes.Metric7,
	                        Attributes.Metric8,
	                        Attributes.Metric9,
	                        Attributes.Metric10
                        FROM Items
                        JOIN ItemAttributes ON ItemAttributes.Item_ID = Items.ID
                        JOIN Attributes ON Attributes.ID = ItemAttributes.Attributes_ID";

            using (PlanogramContext ctx = new PlanogramContext())
            {
                var result = ctx.Database.SqlQuery<RetailData>(sql).ToList();
                SaveFileDialog saver = new SaveFileDialog();
                saver.Filter = "Csv file (*.csv)|*.csv";
                saver.FileName = "retail_data";
                if (saver.ShowDialog() == true)
                {
                    var pathToFile = saver.FileName;
                    using (TextWriter wr = new StreamWriter(pathToFile))
                    {
                        var csv = new CsvWriter(wr);
                        csv.WriteRecords(result);
                    }
                }
            }
        }

        public void DropDB(string dbName)
        {
            using (PlanogramContext ctx = new PlanogramContext())
            {
                if (ctx.DBExists(dbName))
                {
                    DropDB(ctx, dbName);
                }
            }
        }

        private static void DropDB(PlanogramContext ctx, string dbName)
        {
            if (ctx != null && !string.IsNullOrEmpty(dbName))
            {
                ctx.DropDB(dbName);
                System.Diagnostics.Debug.WriteLine("Db dropped...");
            }
        }

        private double RandomDoubleNumber(double minimum, double maximum)
        {
            int randomNumber = rnd.Next(1, 11);
            if (randomNumber > 8)
            {
                double highRange = Convert.ToDouble(rnd.Next(80, 101)) / 100D;
                var value = highRange * (maximum * 2) + minimum;
                return value;
            }
            else
            {
                return rnd.NextDouble() * (maximum  * 0.5) + minimum;
            }
        }

        public void Dispose()
        {
            if (_context != null)
            {
                _context.Dispose();
            }
        }

        public int NumItems { get; set; }
    }
}
