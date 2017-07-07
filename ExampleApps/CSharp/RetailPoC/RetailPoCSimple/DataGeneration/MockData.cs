using RetailPoCSimple.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace RetailPoCSimple
{
    public class MockData : IDisposable
    {
        private PlanogramContext context;
        private List<Item> items;
        private List<Attributes> attributes;
        private List<string> flavors;
        private Random rnd;

        public delegate void dataPanelProgress(double value);
        public delegate void dataPanelGrid(bool refresh);
        public event dataPanelProgress Progress;
        public event dataPanelGrid Refreshdata;

        public List<Item> Items { get { return items; } set { items = value; } }
        public List<Attributes> Attributes { get { return attributes; } set { attributes = value; } }
        public List<string> Flavors { get { return flavors; } set { flavors = value; } }

        public MockData()
        {
            items = new List<Item>();
            attributes = new List<Attributes>();
            context = new PlanogramContext();
            flavors = new List<string> {
                "Mint Chocolate Chip","Vanilla Delight","Chocolate Fudge",
                "Coffee","Raspberry Sherbet","Mocha",
                "Banana Nut Fudge","Cookies and Cream","Birthday Cake",
                "Strawberry and Cheese","Rock and Pop Swirl","French Vanilla",
                "Daiquiri Ice","Peanut Butter and Chocolate","Pistachio Almond",
                "Peanut Butter","Rocky Road","Vanilla Graham",
                "Mango","Nutty Coconut","Berry Strawberry",
                "Orange sherbet","Pink Bubblegum","Chocoloate Almond",
                "Egg Nog Supreme","Ube Avocado","Cherry Strawyberry",
                "Chocolate Almond","Vanilla Coffee swirl","Lemon Custard",
                "Chocolate Chip Delight","Blackberry",
                // new set of flavors
                "Alumni Swirl", "Arboretum Breeze", "Birthday Bash", "Bittersweet Mint", "Black Raspberry", "Black Raspberry Frozen Yogurt", "Black Walnut Frozen Yogurt", "Butter Pecan", "Caramel Peanut Cluster", "Chocolate", "Chocolate Chip Cookie Dough", "Chocolate Frozen Yogurt", "Chocolate Marble", "Chocolate Marshmallow", "Chocolate No Sugar Added", "Coconut Chip", "Coffee Frozen Yogurt", "Cookies-n-Cream", "Death By Chocolate", "Happy Happy Joy Joy", "Keeney Beany Chocolate", "Marshmallow Cup", "Monkey Business", "Peachy Paterno", "Peanut Butter Swirl", "Pistachio", "Scholar's Chip", "Strawberry", "Strawberry Cheesecake", "Strawberry No Sugar Added", "Toffee Caramel Crunch", "Vanilla", "Vanilla Frozen Yogurt", "WPSU Coffee Break", "Apple Cobbler Crunch", "August Pie", "Bananas Foster", "Berkey Brickle", "Black Cow", "Blueberry Cheesecake", "Centennial Vanilla Bean", "Cherry Cheesecake", "Cherry Quist", "Chocolate Pretzel Crunch", "Coffee Mocha Fudge", "Crazy Charlie Sundae Swirl", "Egg Nog", "Espresso Fudge Pie", "Golden Chocolate Pecan", "Lion Tracks", "LionS'more", "Mint Nittany", "Monster Mash", "Orange Vanilla Sundae", "Palmer Mousseum With Almonds", "Peanut Butter Cup", "Peanut Butter Fudge Cluster", "Peanut Butter Marshmallow", "Peppermint Stick", "Pralines N Cream", "Pumpkin Pie", "Raspberry Fudge Torte", "Raspberry Parfait", "Russ Digs Roseberry", "Sea Salt Chocolate Caramel", "Strawberry Frozen Yogurt", "Teaberry", "Tin Roof Sundae", "Toasted Almond", "Turtle Creek", "Vanilla No Sugar Added", "White House", "Wicked Caramel Sundae",
            };

            rnd = new Random();
        }

        public MockData(PlanogramContext context)
        {

            items = new List<Item>();
            attributes = new List<Attributes>();
            
            this.context = context;

            rnd = new Random();
        }

        public double GetItemMinimumScore(SimulationSettings simSettings)
        {
            double retVal = double.MinValue;
            //retVal = context.Database.SqlQuery<double>(@"
            //        select 	
            //         min(m1 + m2 + m3 + m4 + m5) 
            //        from (
            //         select 
            //          a.ID,
            //          iif(@p0 = 0, 0, sum(c.Metric1) * (@p0 / 100)) m1,
            //          iif(@p1 = 0, 0, sum(c.Metric2) * (@p1 / 100)) m2,
            //          iif(@p2 = 0, 0, sum(c.Metric3) * (@p2 / 100)) m3,
            //          iif(@p3 = 0, 0, sum(c.Metric4) * (@p3 / 100)) m4,
            //          iif(@p4 = 0, 0, sum(c.Metric5) * (@p4 / 100)) m5	 
            //         from Items a
            //         inner join ItemAttributes b on a.ID = b.Item_ID
            //         inner join Attributes c on b.Attributes_ID = c.ID
            //         group by a.ID
            //        ) a
            //    ", simSettings.Metric1, simSettings.Metric2, simSettings.Metric3, simSettings.Metric4, simSettings.Metric5).FirstOrDefault();


            List<double> metricScores = new List<double>();
            foreach (var item in items)
            {
                double metric1 = simSettings.Metric1 == 0 ? 0 : item.Attributes.Select(a => a.Metric1).Sum() * (simSettings.Metric1 / 100);
                double metric2 = simSettings.Metric2 == 0 ? 0 : item.Attributes.Select(a => a.Metric2).Sum() * (simSettings.Metric2 / 100);
                double metric3 = simSettings.Metric3 == 0 ? 0 : item.Attributes.Select(a => a.Metric3).Sum() * (simSettings.Metric3 / 100);
                double metric4 = simSettings.Metric4 == 0 ? 0 : item.Attributes.Select(a => a.Metric4).Sum() * (simSettings.Metric4 / 100);
                double metric5 = simSettings.Metric5 == 0 ? 0 : item.Attributes.Select(a => a.Metric5).Sum() * (simSettings.Metric5 / 100);

                double metrics = metric1 + metric2 + metric3 + metric4 + metric5;

                metricScores.Add(metrics);
            }

            return metricScores.Min();
        }

        public double GetItemMaximumScore(SimulationSettings simSettings)
        {
            double retVal = double.MinValue;
            //retVal = context.Database.SqlQuery<double>(@"
            //        select 	
            //         max(m1 + m2 + m3 + m4 + m5) 
            //        from (
            //         select 
            //          a.ID,
            //          iif(@p0 = 0, 0, sum(c.Metric1) * (@p0 / 100)) m1,
            //          iif(@p1 = 0, 0, sum(c.Metric2) * (@p1 / 100)) m2,
            //          iif(@p2 = 0, 0, sum(c.Metric3) * (@p2 / 100)) m3,
            //          iif(@p3 = 0, 0, sum(c.Metric4) * (@p3 / 100)) m4,
            //          iif(@p4 = 0, 0, sum(c.Metric5) * (@p4 / 100)) m5      
            //         from Items a
            //         inner join ItemAttributes b on a.ID = b.Item_ID
            //         inner join Attributes c on b.Attributes_ID = c.ID
            //         group by a.ID
            //        ) a
            //    ", simSettings.Metric1, simSettings.Metric2, simSettings.Metric3, simSettings.Metric4, simSettings.Metric5).FirstOrDefault();

            List<double> metricScores = new List<double>();
            foreach (var item in items)
            {
                double metric1 = simSettings.Metric1 == 0 ? 0 : item.Attributes.Select(a => a.Metric1).Sum() * (simSettings.Metric1 / 100);
                double metric2 = simSettings.Metric2 == 0 ? 0 : item.Attributes.Select(a => a.Metric2).Sum() * (simSettings.Metric2 / 100);
                double metric3 = simSettings.Metric3 == 0 ? 0 : item.Attributes.Select(a => a.Metric3).Sum() * (simSettings.Metric3 / 100);
                double metric4 = simSettings.Metric4 == 0 ? 0 : item.Attributes.Select(a => a.Metric4).Sum() * (simSettings.Metric4 / 100);
                double metric5 = simSettings.Metric5 == 0 ? 0 : item.Attributes.Select(a => a.Metric5).Sum() * (simSettings.Metric5 / 100);

                double metrics = metric1 + metric2 + metric3 + metric4 + metric5;

                metricScores.Add(metrics);
            }

            return metricScores.Max();
        }

        public double GetItemMaxScoreForTop(SimulationSettings simSettings)
        {
            double retVal = 0;
            int numSlots = simSettings.NumShelves * simSettings.NumSlots;
            double top = Math.Ceiling(Convert.ToDouble(numSlots) / Convert.ToDouble(SimulationSettings.MAX_ITEMS));

            //var topItems = context.Database.SqlQuery<double>($@"
            //        select top {top}
	           //         (m1 + m2 + m3 + m4 + m5) as Score
            //        from (
	           //         select 
		          //          a.ID,
		          //          iif(@p0 = 0, 0, sum(c.Metric1) * (@p0 / 100)) m1,
		          //          iif(@p1 = 0, 0, sum(c.Metric2) * (@p1 / 100)) m2,
		          //          iif(@p2 = 0, 0, sum(c.Metric3) * (@p2 / 100)) m3,
		          //          iif(@p3 = 0, 0, sum(c.Metric4) * (@p3 / 100)) m4,
		          //          iif(@p4 = 0, 0, sum(c.Metric5) * (@p4 / 100)) m5
	           //         from Items a
	           //         inner join ItemAttributes b on a.ID = b.Item_ID
	           //         inner join Attributes c on b.Attributes_ID = c.ID
	           //         group by a.ID
            //        ) a
            //        order by Score desc
            //    "
            //    , simSettings.Metric1, simSettings.Metric2, simSettings.Metric3, simSettings.Metric4, simSettings.Metric5).ToList();


            List<double> metricScores = new List<double>();
            foreach (var item in Items)
            {
                double metric1 = simSettings.Metric1 == 0 ? 0 : item.Attributes.Select(a => a.Metric1).Sum() * (simSettings.Metric1 / 100);
                double metric2 = simSettings.Metric2 == 0 ? 0 : item.Attributes.Select(a => a.Metric2).Sum() * (simSettings.Metric2 / 100);
                double metric3 = simSettings.Metric3 == 0 ? 0 : item.Attributes.Select(a => a.Metric3).Sum() * (simSettings.Metric3 / 100);
                double metric4 = simSettings.Metric4 == 0 ? 0 : item.Attributes.Select(a => a.Metric4).Sum() * (simSettings.Metric4 / 100);
                double metric5 = simSettings.Metric5 == 0 ? 0 : item.Attributes.Select(a => a.Metric5).Sum() * (simSettings.Metric5 / 100);

                double metrics = metric1 + metric2 + metric3 + metric4 + metric5;

                metricScores.Add(metrics);
            }

            int atop = Convert.ToInt32(top);
            List<double> topItems = metricScores.OrderByDescending(a => a).Take(atop).ToList();

            int slotsAccountedFor = 0;
            for (int i = 0; i < topItems.Count; i++)
            {
                if (slotsAccountedFor + 1 >= numSlots)
                {
                    var remaining = numSlots - slotsAccountedFor;
                    topItems[i] = topItems[i] * remaining;
                }
                else
                {
                    slotsAccountedFor += 1;
                    topItems[i] = topItems[i] * 1;
                }
            }

            retVal = topItems.Sum();

            return retVal;
        }

        public Item[] GetItemsWithAttr()
        {
            return Items.OrderBy(a => a.ID).ToArray(); //context.Items.Include(a => a.Attributes).OrderBy(b=>b.ID).ToArray();
        }


        public void Generate(int numItems = 32)
        {
            // for now it is based on the hard coded flavors
            numItems = flavors.Count;
            
            //Generate Attributes
            for (int i = 1; i < 201; i++)
            {
                double min = (i < 100) ? 0.0 : 51.0;  // 0.0
                double max = (i >= 100) ? 51.0 : 101; // 101.0
                var attr = new Attributes
                {
                    ID = i,
                    Metric1 = RandomDoubleNumber(min, max),
                    Metric2 = RandomDoubleNumber(min, max),
                    Metric3 = RandomDoubleNumber(min, max),
                    Metric4 = RandomDoubleNumber(min, max),
                    Metric5 = RandomDoubleNumber(min, max),

                };

                attributes.Add(attr);
            }

            //Generate SKU's and assign attributes
            for (int i = 1; i < numItems + 1; i++)
            {
                HashSet<int> numbers = new HashSet<int>();
                //generate 10 unique random numbers from 1-200 which will then be use to
                //set the as the attributes for the item
                while (numbers.Count < 1)
                {
                    if (i < 90)
                    {
                        numbers.Add(rnd.Next(1, 101));
                    }
                    else
                    {
                        numbers.Add(rnd.Next(101, 201));
                    }

                    // numbers.Add(rnd.Next(101, 201));
                }

                var item = new Item
                {
                    ID = i,
                    Name = flavors[i - 1],
                    SKU = String.Format("{0:0000}", i),
                    Attributes = attributes.Where(x => numbers.Any(a => a == x.ID)).ToList(),
                    ImgUri = @"pack://application:,,,/RetailPoCSimple;component/Images/" + i + ".jpg"

                };

                items.Add(item);
            }
        }

        public int GetItemsCount()
        {
            var count = context.Items.Count();
            return count;
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
            if (context != null)
            {
                context.Dispose();
            }
        }

        public int NumItems { get; set; }
    }
}
