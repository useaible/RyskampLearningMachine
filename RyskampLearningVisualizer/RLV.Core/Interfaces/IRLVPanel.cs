using RLV.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RLV.Core.Interfaces
{
    public interface IRLVPanel
    {
        object ViewModel { get; }
        void UpdateBindings(RLVItemDisplayVM userVal);
        void SaveConfiguration();
    }
}
