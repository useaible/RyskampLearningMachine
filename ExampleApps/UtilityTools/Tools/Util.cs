using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tools
{
    public static class Util
    {
        public static int GetInput(string label, int defVal = 0, bool isRange = false)
        {
            bool validInput = false;
            int retVal = 1;

            do
            {
                Console.Write(label);
                string rawInput = Console.ReadLine();
                validInput = int.TryParse(rawInput, out retVal);
                if (!validInput && string.IsNullOrWhiteSpace(rawInput))
                {
                    validInput = true;
                    retVal = defVal;
                }
                else if (validInput && retVal < 0)
                {
                    Console.WriteLine("Cannot be less than zero, try again...");
                    validInput = false;
                }
                else if (!validInput)
                {
                    Console.WriteLine("Invalid input try again...");
                }
            } while (!validInput);

            return retVal;
        }

        public static InputDoubleRanges GetInputDoubleRange(string label, double defVal = 0)
        {
            bool validInput = false;
            InputDoubleRanges retVal = new InputDoubleRanges() { IsRange = false, Values = new double[2] };

            do
            {
                Console.Write(label);
                string rawInput = Console.ReadLine();
                var split = rawInput.Split('-');
                if (split.Count() == 1)
                {
                    validInput = CheckInputDouble(split, retVal.Values);
                    if (string.IsNullOrWhiteSpace(split[0]) && !validInput)
                    {
                        validInput = true;
                        retVal.Values[0] = defVal;
                        retVal.IsRange = false;
                    }
                }
                else if (split.Count() == 2)
                {
                    validInput = CheckInputDouble(split, retVal.Values);
                    retVal.IsRange = true;
                }
                else
                {
                    Console.WriteLine("Invalid Input!");
                }
            } while (!validInput);

            return retVal;
        }

        public static bool CheckInputDouble(string[] rawInputs, double[] results)
        {
            bool validInput = true;
            for (int i = 0; i < rawInputs.Length; i++)
            {
                double val;
                validInput = double.TryParse(rawInputs[i], out val);
                if (validInput)
                {
                    results[i] = val;
                }
                else if (validInput && val < 0)
                {
                    Console.WriteLine("Cannot be less than zero, try again...");
                    validInput = false;
                    break;
                }
                else if (!validInput && rawInputs.Count() == 2)
                {
                    Console.WriteLine("Invalid input try again...");
                    break;
                }
            }
            return validInput;
        }

        public static double GetInputDouble(string label, double defVal = 0)
        {
            bool validInput = false;
            double retVal = 1;

            do
            {
                Console.Write(label);
                string rawInput = Console.ReadLine();
                validInput = double.TryParse(rawInput, out retVal);
                if (!validInput && string.IsNullOrWhiteSpace(rawInput))
                {
                    //Console.Write(defVal);
                    validInput = true;
                    retVal = defVal;
                }
                else if (validInput && retVal < 0)
                {
                    Console.WriteLine("Cannot be less than zero, try again...");
                    validInput = false;
                }
                else if (!validInput)
                {
                    Console.WriteLine("Invalid input try again...");
                }
            } while (!validInput);

            return retVal;
        }
    }
    
    public class InputDoubleRanges
    {
        public bool IsRange { get; set; }
        public double[] Values { get; set; }
    }
}
