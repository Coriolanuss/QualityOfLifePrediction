using System;
using System.IO;

namespace DataParser
{
    class Program
    {
        static void Main(string[] args)
        {
            string pathToDB = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data\\QualityOfLife_Statistics.sqlite");
            string pathToExcel = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data\\Raw data.xls");

            var dataHelper = new DataHelper(pathToDB);
            //ParseData(dataHelper, pathToExcel);

        }
        
        static void ParseData(DataHelper dataHelper, string pathToExcel)
        {
            string readDataFromExcel_Log = dataHelper.ParseFromExcelToDB(pathToExcel);

            Console.Write(readDataFromExcel_Log);
            Console.WriteLine("Press any key to exit");
            Console.ReadKey(false);
        }    
    }
}