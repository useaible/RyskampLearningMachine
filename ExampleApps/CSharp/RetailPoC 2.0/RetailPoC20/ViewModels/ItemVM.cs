using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RetailPoC.ViewModels
{
    public class ItemVM : INotifyPropertyChanged
    {
        private int id;
        public int ID
        {
            get { return id; }
            set { id = value; onPropertyChanged(this, "ID"); }
        }

        public string sku;
        public string SKU
        {
            get { return sku; }
            set { sku = value; onPropertyChanged(this, "SKU"); }
        }

        public string name;
        public string Name
        {
            get { return name; }
            set { name = value; onPropertyChanged(this, "Name"); }
        }

        private int color;
        public int Color
        {
            get { return color; }
            set { color = value; onPropertyChanged(this, "Color"); }
        }

        //private List<AttributesVM> attributes = new List<AttributesVM>();
        //public List<AttributesVM> Attributes
        //{
        //    get { return attributes; }
        //    set { attributes = value; }
        //}

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
