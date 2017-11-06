using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RetailPoC.Models
{
    public class PlanogramSize
    {
        public string Name { get; set; }
        public int Shelves { get; set; }
        public int SlotsPerShelf { get; set; }
        public int ItemsCount { get; set; }
        public int Metrics { get; set; }
        public double BaseScoringPercentage { get; set; }

        public override string ToString()
        {
            return $"{Name} ({Shelves} x {SlotsPerShelf} with {ItemsCount} random data), {BaseScoringPercentage}% Scoring Percentage";
        }
    }
}
