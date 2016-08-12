using QualityOfLifePrediction.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using OxyPlot;
using OxyPlot.Series;
using System.Data;

namespace QualityOfLifePrediction
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        QualityOfLife_StatisticsEntities context = new QualityOfLife_StatisticsEntities();
        public static List<DataPoint> selectedData = new List<DataPoint>();
        public static long selectedDataSet_ID;
        public static string selectedIndicator;
        public static string selectedUnit;

        public MainWindow()
        {
            InitializeComponent();

            string pathToDB = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data\\QualityOfLife_Statistics.sqlite");
            context.Database.Connection.ConnectionString = "data source=\"" + pathToDB + "\"";
            context.Database.Connection.Open();

            var countryList = (from c in context.Countries
                               orderby c.Name ascending
                               select c.Name).ToList();

            countryDataComboBox.ItemsSource = countryList;
        }

        private void countryDataComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            indicatorComboBox.SelectedIndex = -1;

            var indicatorList = (from i in context.Indicators
                                 orderby i.Name ascending
                                 select (i.Name + " [" + i.Unit.Name + "]")).ToList();
            indicatorComboBox.ItemsSource = indicatorList;
            indicatorComboBox.IsEnabled = true;
        }

        private void indicatorComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (indicatorComboBox.SelectedIndex != -1)
            {
                sourceComboBox.SelectedIndex = -1;

                long selectedIndicator_ID = GetSelectedIndicator();
                var sourceList = (from ds in context.DataSets
                                  where ds.Indicator_ID == selectedIndicator_ID
                                  select ds.DataSource.Note).ToList();
                sourceComboBox.ItemsSource = sourceList;
                sourceComboBox.IsEnabled = true;
            }
        }

        private void sourceComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sourceComboBox.SelectedIndex != -1)
            {
                long selectedCountry_ID = GetSelectedCountry();
                long selectedIndicator_ID = GetSelectedIndicator();
                long selectedSource_ID = GetSelectedSource();
                selectedDataSet_ID = (from ds in context.DataSets
                                      where ds.Country_ID == selectedCountry_ID && ds.Indicator_ID == selectedIndicator_ID && ds.DataSource_ID == selectedSource_ID
                                      select ds.DataSet_ID).First();


                selectedIndicator = (from i in context.Indicators
                                     where i.Indicator_ID == selectedIndicator_ID
                                     select (i.Name)).First();
                selectedUnit = (from i in context.Indicators
                                where i.Indicator_ID == selectedIndicator_ID
                                select (i.Unit.Name)).First();

                var Data = ((from d in context.Data
                             where d.DataSet_ID == selectedDataSet_ID
                             orderby d.Date ascending
                             select new { d.Date, d.Value, Unit = d.DataSet.Indicator.Unit.Name }).ToList());
                dataGrid.ItemsSource = Data;
                //AddEntryButton.IsEnabled = true;


                selectedData = new List<DataPoint>();
                foreach (var x in Data)
                    selectedData.Add(new DataPoint(x.Date, x.Value));
            }
        }

        private long GetSelectedCountry()
        {
            return (from c in context.Countries
                    where c.Name == (string)countryDataComboBox.SelectedItem
                    select c.Country_ID).First();
        }
        private long GetSelectedIndicator()
        {
            return (from i in context.Indicators
                    where (i.Name + " [" + i.Unit.Name + "]") == (string)indicatorComboBox.SelectedItem
                    select i.Indicator_ID).First();
        }
        private long GetSelectedSource()
        {
            return (from s in context.DataSources
                    where s.Note == (string)sourceComboBox.SelectedItem
                    select s.DataSource_ID).First();
        }

        private void dataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //editEntryButton.IsEnabled = true;
            //deleteEntryButton.IsEnabled = true;
        }

        private void AddEntryButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void editEntryButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void deleteEntryButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void tabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ForecastingTab.IsSelected && selectedData.Any())
            {
                ForecastPlotView.Model.Series.Clear();

                var inputSeries = new LineSeries
                {
                    Title = selectedIndicator,
                    MarkerFill = OxyColor.FromArgb(255, 0, 0, 0),
                    MarkerType = MarkerType.Square,
                    MarkerSize = 3,
                    CanTrackerInterpolatePoints = false,
                    Color = OxyColor.FromArgb(255, 0, 0, 0),
                    StrokeThickness = 2
                };
                inputSeries.Points.AddRange(selectedData);
                ForecastPlotView.Model.Series.Add(inputSeries);

                var forecastSeries1 = new LineSeries
                {
                    Title = "Прогноз методом Брауна",
                    MarkerFill = OxyColor.FromArgb(255, 0, 0, 0),
                    MarkerType = MarkerType.Circle,
                    MarkerSize = 3,
                    CanTrackerInterpolatePoints = false,
                    Color = OxyColor.FromArgb(200, 0, 0, 0),
                    StrokeThickness = 2,
                    LineStyle = LineStyle.Dash
                };
                List<DataPoint> BrownForecast = FromForecastTableToListOfDataPoints(exponentialSmoothing(GetInput(), /*(int)comboBox.SelectedValue*/ 3, (Decimal)0.8));
                forecastSeries1.Points.AddRange(BrownForecast);
                ForecastPlotView.Model.Series.Add(forecastSeries1);

                var forecastSeries2 = new LineSeries
                {
                    Title = "Прогноз методом Шоуна",
                    MarkerFill = OxyColor.FromArgb(255, 0, 0, 0),
                    MarkerType = MarkerType.Triangle,
                    MarkerSize = 3,
                    CanTrackerInterpolatePoints = false,
                    Color = OxyColor.FromArgb(200, 0, 0, 0),
                    StrokeThickness = 2,
                    LineStyle = LineStyle.Dot
                };
                List<DataPoint> ShownForecast = LinearForecasting(/*(int)comboBox.SelectedValue*/ 3, 0.3);
                forecastSeries2.Points.AddRange(ShownForecast);
                ForecastPlotView.Model.Series.Add(forecastSeries2);

                ForecastPlotView.Model.Axes.Clear();
                ForecastPlotView.Model.Axes.Add(new OxyPlot.Axes.LinearAxis() {
                    Position = OxyPlot.Axes.AxisPosition.Bottom,
                    Title = "Years"
                });
                ForecastPlotView.Model.Axes.Add(new OxyPlot.Axes.LinearAxis()
                {
                    Position = OxyPlot.Axes.AxisPosition.Left,
                    Title = selectedUnit,
                });

                ForecastPlotView.InvalidatePlot();

                // Calculating errors of forecasting
                BrownMADtextBox.Text = Math.Round(GetMAD(selectedData, BrownForecast), 2).ToString();
                BrownMSEtextBox.Text = Math.Round(GetMSE(selectedData, BrownForecast), 2).ToString();
                BrownMAPEtextBox.Text = (Math.Round(GetMAPE(selectedData, BrownForecast), 2)*100).ToString() + "%";

                ShownMADtextBox.Text = Math.Round(GetMAD(selectedData, ShownForecast), 2).ToString();
                ShownMSEtextBox.Text = Math.Round(GetMSE(selectedData, ShownForecast), 2).ToString();
                ShownMAPEtextBox.Text = (Math.Round(GetMAPE(selectedData, ShownForecast), 2)*100).ToString() + "%";
            }
        }
        /*public static List<DataPoint> GetFakeForecast1()
        {

            selectedData.d


            return new List<DataPoint>() {
                    new DataPoint(1990, 10490.3717),
                    new DataPoint(1991, 10490.3717),
                    new DataPoint(1992, 10039.19686),
                    new DataPoint(1993, 9570.534739),
                    new DataPoint(1994, 8540.721889),
                    new DataPoint(1995, 7255.367881),
                    new DataPoint(1996, 6068.495797),
                    new DataPoint(1997, 5136.861078),
                    new DataPoint(1998, 4729.121743),
                    new DataPoint(1999, 4525.851644),
                    new DataPoint(2000, 4489.192906),
                    new DataPoint(2001, 4589.441972),
                    new DataPoint(2002, 4870.192915),
                    new DataPoint(2003, 5250.385746),
                    new DataPoint(2004, 5719.461077),
                    new DataPoint(2005, 6291.784507),
                    new DataPoint(2006, 6834.45452),
                    new DataPoint(2007, 7378.16272),
                    new DataPoint(2008, 7876.995912),
                    new DataPoint(2009, 8376.030688),
                    new DataPoint(2010, 8259.490939),
                    new DataPoint(2011, 8034.73806),
                    new DataPoint(2012, 7874.778558),
                    new DataPoint(2013, 8148.358905),
                    new DataPoint(2014, 8312.918176),
                    new DataPoint(2015, 8307.984248),
                    new DataPoint(2016, 8347.169506),
                    new DataPoint(2017, 8350.250732),
                    new DataPoint(2018, 8377.979196),
                    new DataPoint(2019, 8358.466478),
                };
        }*/
        public static ForecastTable exponentialSmoothing(decimal[] values, int Extension, decimal Alpha)
        {
            ForecastTable dt = new ForecastTable();

            for (Int32 i = 0; i < (values.Length + Extension); i++)
            {
                //Insert a row for each value in set
                DataRow row = dt.NewRow();
                dt.Rows.Add(row);

                row.BeginEdit();
                //assign its sequence number
                row["Instance"] = i;
                if (i < values.Length)
                {//test set
                    row["Value"] = values[i];
                    if (i == 0)
                    {//initialize first value
                        row["Forecast"] = values[i];
                    }
                    else
                    {//main area of forecasting
                        DataRow priorRow = dt.Select("Instance=" + (i - 1).ToString())[0];
                        decimal PriorForecast = (Decimal)priorRow["Forecast"];
                        decimal PriorValue = (Decimal)priorRow["Value"];

                        row["Forecast"] = PriorForecast + (Alpha * (PriorValue - PriorForecast));
                    }
                }
                else
                {//extension has to use prior forecast instead of prior value
                    DataRow priorRow = dt.Select("Instance=" + (i - 1).ToString())[0];
                    decimal PriorForecast = (Decimal)priorRow["Forecast"];
                    decimal PriorValue = (Decimal)priorRow["Forecast"];

                    row["Forecast"] = PriorForecast + (Alpha * (PriorValue - PriorForecast));
                }
                row.EndEdit();
            }

            dt.AcceptChanges();

            return dt;
        }
        /*public static List<DataPoint> GetFakeForecast2()
        {
            return new List<DataPoint>() {
                    new DataPoint(1990, 10490.3717),
                    new DataPoint(1991, 10490.3717),
                    new DataPoint(1992, 10039.19686),
                    new DataPoint(1993, 9570.534739),
                    new DataPoint(1994, 9028.13434),
                    new DataPoint(1995, 7838.531416),
                    new DataPoint(1996, 6709.674473),
                    new DataPoint(1997, 5702.879095),
                    new DataPoint(1998, 4979.831307),
                    new DataPoint(1999, 4662.537295),
                    new DataPoint(2000, 4518.401926),
                    new DataPoint(2001, 4569.266977),
                    new DataPoint(2002, 4768.340674),
                    new DataPoint(2003, 5061.802503),
                    new DataPoint(2004, 5491.968105),
                    new DataPoint(2005, 6291.784507),
                    new DataPoint(2006, 6534.998706),
                    new DataPoint(2007, 7087.800836),
                    new DataPoint(2008, 7663.248702),
                    new DataPoint(2009, 8098.183343),
                    new DataPoint(2010, 8156.57815),
                    new DataPoint(2011, 8155.680207),
                    new DataPoint(2012, 8096.520327),
                    new DataPoint(2013, 7985.824312),
                    new DataPoint(2014, 8195.750635),
                    new DataPoint(2015, 8301.454967),
                    new DataPoint(2016, 8340.117523),
                    new DataPoint(2017, 8345.40651),
                    new DataPoint(2018, 8347.276681),
                    new DataPoint(2019, 8367.329516),
                };
        }*/
        private static List<DataPoint> FromForecastTableToListOfDataPoints(ForecastTable input)
        {
            List<DataPoint> result = new List<DataPoint>();

            for (int i = 0; i < input.Rows.Count; i++)
            {
                double X = ((selectedData.Count > i) ? selectedData.ElementAt(i).X : (selectedData.ElementAt(0).X + i));
                DataRow temp = input.Rows[i];         
                Decimal Y = (Decimal)temp["Forecast"];
            result.Add(new DataPoint(X,(double)Y));
            }
            return result;
        }
        private static decimal[] GetInput()
        {
            Decimal[] result = new Decimal[selectedData.Count];

            for (int i = 0; i < selectedData.Count; i++)
                result[i] = (Decimal)selectedData.ElementAt(i).Y;

            return result;
        }
        private static List<DataPoint> LinearForecasting(int Extension, double Alpha)// Alpha [from 0 to 1] - flatness ratio
        {
            List<DataPoint> result = new List<DataPoint>();

            for (int i = 0; i < selectedData.Count + Extension; i++)
            {
                if (i < Extension)
                { result.Add(new DataPoint(
                    selectedData.ElementAt(i).X,
                    selectedData.ElementAt(i).Y));
                }
                else if (i < selectedData.Count)
                {
                    double newVal = selectedData.ElementAt(i).Y + (selectedData.ElementAt(i).Y - selectedData.ElementAt(i- Extension).Y) * Alpha;

                    result.Add(new DataPoint(
                        selectedData.ElementAt(i).X,
                        newVal));
                }
                else
                {
                    double newVal = result.ElementAt(i - 1).Y+ (result.ElementAt(i-1).Y - selectedData.ElementAt(i - Extension).Y) * Alpha;
                    double newYear = selectedData.ElementAt(selectedData.Count - 1).X - (selectedData.Count-1) + i;

                    result.Add(new DataPoint(
                        newYear,
                        newVal));
                }
            }


            return result;
        }
        private static double GetMAD(List<DataPoint> data, List<DataPoint> forecast)
        {
            double result = 0;

            double tempSum = 0;
            for (int i = 0; i < data.Count; i++)
            {
                tempSum += Math.Abs(data.ElementAt(i).Y - forecast.ElementAt(i).Y);
            }
            tempSum = tempSum / data.Count;
            result = tempSum;

            return result;
        }
        private static double GetMSE(List<DataPoint> data, List<DataPoint> forecast)
        {
            double result = 0;

            double tempSum = 0;
            for (int i = 0; i < data.Count; i++)
            {
                tempSum += Math.Pow((data.ElementAt(i).Y - forecast.ElementAt(i).Y), 2);
            }
            tempSum = tempSum / data.Count;
            result = tempSum;

            return result;
        }
        private static double GetMAPE(List<DataPoint> data, List<DataPoint> forecast)
        {
            double result = 0;

            double tempSum = 0;
            for (int i = 0; i < data.Count; i++)
            {
                tempSum += (data.ElementAt(i).Y != 0) ? Math.Abs(data.ElementAt(i).Y - forecast.ElementAt(i).Y)/ data.ElementAt(i).Y : 0;
            }
            tempSum = tempSum / data.Count;
            result = tempSum;

            return result;
        }
    }

    public class ForecastTable : DataTable
    {
        // An instance of a DataTable with some default columns.  Expressions help quickly calculate E
        public ForecastTable()
        {
            this.Columns.Add("Instance", typeof(Int32));    //The position in which this value occurred in the time-series
            this.Columns.Add("Value", typeof(Decimal));     //The value which actually occurred
            this.Columns.Add("Forecast", typeof(Decimal));  //The forecasted value for this instance
            this.Columns.Add("Holdout", typeof(Boolean));   //Identifies a holdout actual value row, for testing after err is calculated

            //E(t) = D(t) - F(t)
            this.Columns.Add("Error", typeof(Decimal), "Forecast-Value");
            //Absolute Error = |E(t)|
            this.Columns.Add("AbsoluteError", typeof(Decimal), "IIF(Error>=0, Error, Error * -1)");
            //Percent Error = E(t) / D(t)
            this.Columns.Add("PercentError", typeof(Decimal), "IIF(Value<>0, Error / Value, Null)");
            //Absolute Percent Error = |E(t)| / D(t)
            this.Columns.Add("AbsolutePercentError", typeof(Decimal), "IIF(Value <> 0, AbsoluteError / Value, Null)");
        }
    }

    public class MainViewModel
    {
        public MainViewModel()
        {
            PlotModel pModel = new PlotModel();

            this.Model = pModel;
        }
        public PlotModel Model { get; set; }
    }
}


