using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RetailPoC.Models
{
    public class PlanogramOptResults
    {
        public IEnumerable<Shelf> Shelves { get; set; } = new List<Shelf>();
        public double Score { get; set; }
        public double AvgScore { get; set; }
        public double AvgLastTen { get; set; }
        public double MinScore { get; set; }
        public double MaxScore { get; set; }
        public int CurrentSession { get; set; }
        public TimeSpan TimeElapsed { get; set; }
        public int NumScoreHits { get; set; }
        
        public double MetricMin { get; set; }
        public double MetricMax { get; set; }

        public void CalculateItemColorIntensity()
        {
            foreach(var shelf in Shelves)
            {
                foreach(var item in shelf.Items)
                {
                    var perc = ConvertRange(MetricMin, MetricMax, 0D, 100D, item.Score);
                    System.Drawing.Color newColor = System.Drawing.Color.Blue;                    
                    item.PerfColor = System.Windows.Media.Color.FromArgb(Convert.ToByte(ConvertRange(0, 100, 0, 255, perc)), newColor.R, newColor.G, newColor.B);
                    item.ScoreDetailed = $"{item.Score.ToString("#,##0.##")} ({perc.ToString("#0.##")}%)";
                }
            }
        }
        
        private double ConvertRange(
            double originalStart, double originalEnd, // original range
            double newStart, double newEnd, // desired range
            double value) // value to convert
        {
            double scale = (double)(newEnd - newStart) / (originalEnd - originalStart);
            return (double)(newStart + ((value - originalStart) * scale));
        }
    }
}
