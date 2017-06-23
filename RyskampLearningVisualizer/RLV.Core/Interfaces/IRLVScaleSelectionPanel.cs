using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RLV.Core.Interfaces
{
    public interface IRLVScaleSelectionPanel : IRLVPanel
    {
        //events
        event ScaleChangedDelegate ScaleChangedEvent;
        void SetCurrentCaseId(int caseId);
        void SetDefaultScale(double scale);
        void SetViewModel(IRLVScaleSelectionVM vm);
        void SetScaleValueManual(double scale);
    }
}
