using RetailPoC20.Models;
using RetailPoC20.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RetailPoC20
{
    public class DataFactory
    {
        public DataFactory()
        {
            Metrics.CollectionChanged += Metrics_CollectionChanged;
        }

        private void Metrics_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            ObservableCollection<MetricVM> metrics = sender as ObservableCollection<MetricVM>;

            int total = 0;
            foreach(var m in metrics)
            {
                total += m.Value;
            }

            Total = total;
        }

        public List<ItemVM> Items
        {
            get
            {
                List<ItemVM> items = new List<ItemVM>();

                using (PlanogramContext ctx = new PlanogramContext())
                {
                    var ctxItems = ctx.Items;//.Include("Attributes");
                    
                    foreach (var a in ctxItems)
                    {
                        List<AttributesVM> attrs = new List<AttributesVM>();

                        foreach (var b in a.Attributes)
                        {
                            AttributesVM attr = new AttributesVM()
                            {
                                ID = b.ID,
                                Metric1 = b.Metric1,
                                Metric2 = b.Metric2,
                                Metric3 = b.Metric3,
                                Metric4 = b.Metric4,
                                Metric5 = b.Metric5,
                                Metric6 = b.Metric6,
                                Metric7 = b.Metric7,
                                Metric8 = b.Metric8,
                                Metric9 = b.Metric9,
                                Metric10 = b.Metric10,
                            };

                            attrs.Add(attr);
                        }

                        ItemVM item = new ItemVM()
                        {
                            ID = a.ID,
                            SKU = a.SKU,
                            Name = a.Name,
                            Color = a.Color
                        };

                        //item.Attributes = attrs;

                        items.Add(item);
                    }
                }

                return items;
            }
        }

        public ObservableCollection<MetricVM> Metrics
        {
            get
            {
                ObservableCollection<MetricVM> metrics = new ObservableCollection<MetricVM>();
                for (int i = 0; i < 10; i++)
                {
                    MetricVM metric = new MetricVM() { ID = i + 1, Name = $"Metric{i + 1}", Value = 10 };
                    metrics.Add(metric);
                }

                return metrics;
            }
        }

        private int total;
        public int Total { get { return total; } set { total = value; onPropertyChanged(this, "Total"); } }

        // Declare the PropertyChanged event
        public event PropertyChangedEventHandler PropertyChanged;

        // OnPropertyChanged will raise the PropertyChanged event passing the
        // source property that is being updated.
        private void onPropertyChanged(object sender, string propertyName)
        {
            if (this.PropertyChanged != null)
            {
                PropertyChanged(sender, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
