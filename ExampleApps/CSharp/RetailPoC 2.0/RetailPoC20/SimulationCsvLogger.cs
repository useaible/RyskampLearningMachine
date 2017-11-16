using CsvHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RetailPoC20.Models;
using System.IO;

namespace RetailPoC20
{
    public class SimulationCsvLogger
    {
        public List<SimulationData> RLMList { get; set; } = new List<SimulationData>();
        public List<SimulationData> TensorflowList { get; set; } = new List<SimulationData>();
        public List<SimulationData> EncogList { get; set; } = new List<SimulationData>();

        public void Add(SimulationData data, bool isTensorflow)
        {
            if (isTensorflow)
            {
                data.Engine = "Tensorflow";
                TensorflowList.Add(data);
            }
            else
            {
                data.Engine = "Encog";
                EncogList.Add(data);
            }
        }
        public void Add(SimulationData data)
        {
            data.Engine = "Ryskamp Learning Machine";
            RLMList.Add(data);
        }

        public void Clear()
        {
            RLMList.Clear();
            TensorflowList.Clear();
            EncogList.Clear();
        }

        public void ToCsv(string path, bool isTensorflow)
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
                    csv.WriteRecords(EncogList);
                }
            }
        }

        public void ToCsv(string path)
        {
            using (TextWriter wr = new StreamWriter(path))
            {
                var csv = new CsvWriter(wr);
                csv.WriteRecords(RLMList);
            }
        }
    }
}
