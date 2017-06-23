using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RLV.Core.Interfaces
{
    public interface IRLVScaleSelectionVM : INotifyPropertyChanged
    {
        long CurrentCaseId { get; set; }
        string SliderLabelText { get; set; }
        double DefaultScale { get; set; }
    }
}
