using RLM.Models;
using RLV.Core.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace RLV.Core.Interfaces
{
    public interface IRLVSelectedDetailVM : INotifyPropertyChanged, INotifyCollectionChanged
    {
        long? PreviousSessionId { get; set; }
        long? CurrentSessionId { get; set; }
        long? PreviousSession { get; set; }
        double? PreviousTime { get; set; }
        long? PreviousCase { get; set; }
        double? PreviousScore { get; set; }
        long? CurrentSession { get; set; }
        double? CurrentTime { get; set; }
        long? CurrentCase { get; set; }
        double? CurrentScore { get; set; }
        ObservableCollection<RLVIODetailsVM> InputDetails { get; set; }
        ObservableCollection<RLVIODetailsVM> OutputDetails { get; set; }
        ObservableCollection<RLVItemDisplayVM> Labels { get; set; }
        ObservableCollection<RLVItemDisplayVM> Values { get; set; }
        //ObservableCollection<IValueConverter> DefaultFormatters { get; set; }
    }
}
