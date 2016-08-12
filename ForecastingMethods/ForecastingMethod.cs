using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ForecastingMethods
{
    class IncomingData
    {
        public IncomingData(Dictionary<double, double> rawData)
        {
            IncomingPoints = new double[2, rawData.Count];

            for (int i = 0; i < rawData.Count; i++)
            {
                IncomingPoints[0, i] = rawData[i];

            }
        }
        public double[,] IncomingPoints { get; set; }
    }
    class ForecastData
    {
        public ForecastData(int n) // n - number of points
        {
            this.ForecastPoints = new double[2, n];
        }

        public double[,] ForecastPoints { get; set; }
    }

    public class Point
    {
        public Point(double date, double value)
        {
            this.Date = date;
            this.Value = value;
        }
        public double Date { get; set; }
        public double Value { get; set; }
    }
    /*class MovingAverage : ForecastingMethod
    {
        public override ForecastData Solve(IncomingData input)
        {
            ForecastData output = new ForecastData(input.IncomingPoints.Length);

            input.IncomingPoints[0, 1] = 10;

            return output;
        }
    }*/

    public class ForecasingMethods
    {
        public static double[,] ShounsMethod(double[,] input, int startIndex, int n)
        {
            double[,] output = new double[2, input.Length];





            return output;
        }
        public static double[,] BraunsMethod(double[,] input, int startIndex, int n)
        {
            double[,] output = new double[2, input.Length];





            return output;
        }
    }
}
