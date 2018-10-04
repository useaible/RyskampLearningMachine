using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RLM;
using RLM.Enums;
using RLM.Models.Interfaces;

namespace RLM.Models.Optimizer
{
    public class Resource : IRlmFormula
    {
        public Resource()
        {
            DataObjDictionary = new Dictionary<int, DataObj>();
        }

        public string Name { get; set; }
        public Category Type { get; set; }
        public RLMObject RLMObject { get; set; }
        public object Value { get; set; }
        public double Min { get; set; }
        public double Max { get; set; }
        public string FilePath { get; set; }
        public IEnumerable<string> Formula { get; set; }
        public IDictionary<int, DataObj> DataObjDictionary { get; set; }
        public string DataType { get; set; }
        public RlmInputDataType RlmInputDataType { get; set; }
        public Dictionary<string, ResourceAttribute> ResourceAttributes { get; set; }
        public RlmInputType? RlmInputType { get; set; }

        public void UploadDataResource(string filePath)
        {
            string line;
            string[] dataHeaders;
            var resourceAttributes = new List<ResourceAttribute>();
            var dataList = new List<string[]>();
            var sr = new StreamReader(filePath);

            int i = 0;
            while ((line = sr.ReadLine()) != null)
            {
                i++;
                if (i == 1)
                {
                    dataHeaders = line.Split(',');
                    for (int k = 0; k < dataHeaders.Length; k++)
                    {
                        if (k > 0)
                        {
                            var attribute = new ResourceAttribute();
                            attribute.Name = dataHeaders[k];
                            resourceAttributes.Add(attribute);
                        }
                        else
                        {
                            // Ignore
                        }
                    }
                }
                else
                {
                    dataList.Add(line.Split(','));
                }
            }

            foreach (string[] item in dataList)
            {
                var dataObj = new DataObj();
                // Make attributes and store to attribute list
                for (int j = 0; j < item.Length; j++)
                {
                    if (j == 0)
                    {
                        //TO DO, implement robust TryParse Code
                        dataObj.Id = int.Parse(item[j]);
                    }
                    else
                    {
                        if (dataObj.AttributeDictionary == null)
                        {
                            dataObj.AttributeDictionary = new Dictionary<string, object>();
                        }

                        double value;
                        var valueCheck = double.TryParse(item[j], out value);
                        if (valueCheck)
                        {
                            resourceAttributes[j - 1].AttributeDataType = value.GetType();
                            dataObj.AttributeDictionary.Add(resourceAttributes[j - 1].Name, value);
                        }
                        else
                        {
                            bool exists = resourceAttributes[j - 1].ResourceAttributeValues.Contains(item[j]);
                            if (!exists)
                            {
                                resourceAttributes[j - 1].ResourceAttributeValues.Add(item[j]);
                            }
                            else
                            {
                                // ignore
                            }

                            resourceAttributes[j - 1].AttributeDataType = item[j].GetType();
                            dataObj.AttributeDictionary.Add(resourceAttributes[j - 1].Name, item[j]);
                        }
                    }
                }

                DataObjDictionary.Add(dataObj.Id, dataObj);

            }

            ResourceAttributes = new Dictionary<string, ResourceAttribute>();

            foreach (var item in resourceAttributes)
            {
                ResourceAttributes.Add(item.Name, item);
            }

            sr.Close();
        }

    }

    public enum Category
    {
        Constant,
        Range,
        Data,
        Computed,
        Variable
    }

    public enum RLMObject
    {
        None,
        Output,
        Input
    }
}
