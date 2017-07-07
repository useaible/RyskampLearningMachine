using CsvHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RetailPoCSimple.Models;
using System.IO;

namespace RetailPoCSimple
{
    public class SimulationCsvLogger
    {
        public List<SimulationData> RLMList { get; set; } = new List<SimulationData>();
        public List<SimulationData> TensorflowList { get; set; } = new List<SimulationData>();

        public void Add(SimulationData data, bool isTensorflow = false)
        {
            if (isTensorflow)
            {
                data.Engine = "Tensorflow";
                TensorflowList.Add(data);
            }
            else
            {
                data.Engine = "Ryskamp Learning Machine";
                RLMList.Add(data);
            }
        }

        public void Clear()
        {
            RLMList.Clear();
            TensorflowList.Clear();
        }

        public void ToCsv(string path, bool isTensorflow = false)
        {
            using (TextWriter wr = new StreamWriter(path))
            {
                var csv = new CsvWriter(wr);

                if (isTensorflow)
                {
                    csv.WriteRecords(TensorflowList);
                }
                else
                {
                    csv.WriteRecords(RLMList);
                }
            }
        }
    }
}
