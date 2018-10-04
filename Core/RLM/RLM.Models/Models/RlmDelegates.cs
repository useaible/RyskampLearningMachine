using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RLM.Models
{
    public delegate void DataPersistenceCompleteDelegate();
    public delegate void DataPersistenceProgressDelegate(long processed, long total);
    public delegate void LoadNetworkProgressDelegate(long processed, long total);
}
