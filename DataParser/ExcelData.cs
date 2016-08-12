using System;
using System.Collections.Generic;
using System.Linq;
using Excel;
using System.Data;
using System.IO;

namespace DataParser
{
    public class ExcelData
    {
        private string path;

        public ExcelData(string path)
        {
            this.path = path;
        }

        private IExcelDataReader GetExcelDataReader(bool isFirstRowAsColumnNames)
        {
            using (FileStream fileStream = File.Open(path, FileMode.Open, FileAccess.Read))
            {
                IExcelDataReader dataReader;

                if (path.EndsWith(".xls"))
                {
                    dataReader = ExcelReaderFactory.CreateBinaryReader(fileStream);
                }
                else if (path.EndsWith(".xlsx"))
                {
                    dataReader = ExcelReaderFactory.CreateOpenXmlReader(fileStream);
                }
                else
                {
                    //Throw exception for things you cannot correct
                    throw new Exception("The file to be processed is not an Excel file");
                }

                dataReader.IsFirstRowAsColumnNames = isFirstRowAsColumnNames;

                return dataReader;
            }
        }

        private System.Data.DataSet GetExcelDataAsDataSet(bool isFirstRowAsColumnNames)
        {
            return GetExcelDataReader(isFirstRowAsColumnNames).AsDataSet();
        }

        private DataTable GetExcelWorkSheet(string workSheetName, bool isFirstRowAsColumnNames)
        {
            System.Data.DataSet dataSet = GetExcelDataAsDataSet(isFirstRowAsColumnNames);
            DataTable workSheet = dataSet.Tables[workSheetName];

            if (workSheet == null)
            {
                throw new Exception(string.Format("The worksheet {0} does not exist, has an incorrect name, or does not have any data in the worksheet", workSheetName));
            }

            return workSheet;
        }

        public IEnumerable<DataRow> GetData(string workSheetName, bool isFirstRowAsColumnNames = true)
        {
            DataTable workSheet = GetExcelWorkSheet(workSheetName, isFirstRowAsColumnNames);

            IEnumerable<DataRow> rows = from DataRow row in workSheet.Rows
                                        select row;

            return rows;
        }
    }
}
