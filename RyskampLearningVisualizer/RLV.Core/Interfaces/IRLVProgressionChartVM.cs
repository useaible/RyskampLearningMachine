using RLV.Core.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RLV.Core.Interfaces
{
    public interface IRLVProgressionChartVM : INotifyPropertyChanged, INotifyCollectionChanged
    {
        double? CurrentTime { get; set; }
        double? CurrentScore { get; set; }
        string XAxisTitle { get; set; }
        string YAxisTitle { get; set; }
        object XLabelFormatter { get; set; }
        object YLabelFormatter { get; set; }
        object SeriesCollection { get; set; }
        ObservableCollection<RLVItemDisplayVM> Labels { get; set; }
        ObservableCollection<RLVItemDisplayVM> Values { get; set; }
    }
}
