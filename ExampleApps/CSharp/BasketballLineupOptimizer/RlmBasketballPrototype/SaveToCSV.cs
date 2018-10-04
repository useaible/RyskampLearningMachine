using RLM.Models.Optimizer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace RlmBasketballPrototype
{
    public class SaveToCSV
    {
        private string FileName { get; set; }
        private StreamWriter Sr { get; set; }
        public SaveToCSV(string fileName = "RLMData.csv", bool saveAfterEveryWrite = true)
        {
            int i = 0;
            do
            {
                i++;
            } while (File.Exists(fileName + "_" + i + ".csv"));
            FileName = fileName + "_" + i + ".csv";
            Sr = new StreamWriter(FileName);
            Sr.AutoFlush = saveAfterEveryWrite;
        }

        public void WriteLine(string line)
        {
            Sr.WriteLine(line);
        }

        public void WriteRLMSettings(RlmSettings settings)
        {
            var collection = settings.GetType().GetProperties();
            List<string> headers = new List<string>();
            List<string> data = new List<string>();

            foreach (PropertyInfo pi in collection)
            {
                headers.Add(pi.Name);
                data.Add(pi.GetValue(settings).ToString());
            }

            Sr.WriteLine(String.Join(",", headers));
            Sr.WriteLine(String.Join(",", data));
        }

        public void WriteSpace()
        {
            Sr.WriteLine();
        }

        public void WriteData(List<string> sessionData, string headers)
        {
            WriteSpace();
            WriteLine(headers);
            for (int i = 0; i < sessionData.Count(); i++)
            {
                Sr.WriteLine(i + 1 + "," + sessionData[i]);
            }
        }


    }
}
