using QualityOfLifePrediction.Data;
using System;
using System.Linq;

namespace DataParser
{
    class DataHelper
    {
        public QualityOfLife_StatisticsEntities _context;
        public DataHelper(string pathToDB)
        {
            this._context = new QualityOfLife_StatisticsEntities();
            this._context.Database.Connection.ConnectionString = "data source=\"" + pathToDB + "\"";
            this._context.Database.Connection.Open();
        }

        public string ParseFromExcelToDB(string pathToExcel)
        {
            string returnLog = "";
            
            var excelData = new ExcelData(pathToExcel);
            var MetaDataIndicators_Worksheet = excelData.GetData("Metadata - Indicators", true);
            var MetaDataCountries_Worksheet = excelData.GetData("Metadata - Countries", true);
            var Data_Worksheet = excelData.GetData("Data", false);


            // filling tables: Regions, IncomeGroups, Countries
            foreach (var x in MetaDataCountries_Worksheet)
            {
                // Adding Region
                var Region_Name = ConvertFromDBVal<string>(x[2]);
                returnLog += Add_Region(Region_Name);

                // Adding IncomeGroup
                var IncomeGroup_Name = ConvertFromDBVal<string>(x[3]);
                returnLog += "\n" + Add_IncomeGroup(IncomeGroup_Name);

                // Adding Country
                var Country_Name = ConvertFromDBVal<string>(x[0]);
                var Country_Code = ConvertFromDBVal<string>(x[1]);
                var Country_Note = ConvertFromDBVal<string>(x[4]);
                var Region_ID = (int)(from r in _context.Regions
                                      where r.Name == Region_Name
                                      select r.Region_ID).First();
                var IncomeGroup_ID = (int)(from i in _context.IncomeGroups
                                           where i.Name == IncomeGroup_Name
                                           select i.IncomeGroup_ID).First();
                returnLog += "\n" + Add_Country(Country_Name, Country_Code, Country_Note, Region_ID, IncomeGroup_ID);
            }
            returnLog += "\n" + "----- Regions, IncomeGroups, Countries tables have been filled -----";


            // filling tables: Organizations, DataSources, Units, Indicators, Datasets
            foreach (var x in MetaDataIndicators_Worksheet)
            {
                // Adding Organization
                var Organization_Name = ConvertFromDBVal<string>(x[3]);
                returnLog += "\n" + Add_Organization(Organization_Name);

                // Adding DataSource
                var DataSource_Note = ConvertFromDBVal<string>(x[2]);
                var Organization_ID = (from o in _context.Organizations
                                       where o.Name == Organization_Name
                                       select o.Organization_ID).First();
                returnLog += "\n" + Add_DataSource(DataSource_Note, Organization_ID);

                // Adding Unit
                string[] strings = (ConvertFromDBVal<string>(x[1])).Split(new char[] { '(' }, StringSplitOptions.RemoveEmptyEntries);
                var Unit_Name = (strings.Length < 2) ? "undefined" : strings[strings.Length - 1].Replace(")", @"");
                returnLog += "\n" + Add_Unit(Unit_Name);

                // Adding Indicator
                var Indicator_Name = (strings.Length < 1) ? "" : strings[0];
                var Indicator_Code = ConvertFromDBVal<string>(x[0]);
                var Unit_ID = (from u in _context.Units
                               where u.Name == Unit_Name
                               select u.Unit_ID).First();
                returnLog += "\n" + Add_Indicator(Indicator_Name, Indicator_Code, Unit_ID);

                //Adding DataSet
                string Country_Code = "UKR";
                var IndicatorForDataSet_ID = (from i in _context.Indicators
                                              where i.Code == Indicator_Code
                                              select i.Indicator_ID).First();
                var DataSourceForDataSet_ID = (from ds in _context.DataSources
                                               where ds.Organization_ID == Organization_ID && ds.Note == DataSource_Note
                                               select ds.DataSource_ID).First();
                var CountryForDataSet_ID = (from c in _context.Countries
                                            where c.Code == Country_Code
                                            select c.Country_ID).First();
                returnLog += "\n" + Add_DataSet(IndicatorForDataSet_ID, DataSourceForDataSet_ID, CountryForDataSet_ID);
            }
            returnLog += "\n" + "----- Organizations, DataSources, Units, Indicators, Datasets, tables have been filled -----";


            // getting headers of worksheet
            string[] headers = new string[60];
            for (int i = 0; i < 60; i++)
                headers[i] = ConvertFromDBVal<string>((Data_Worksheet.First())[i]);

            // filling table: Data
            foreach (var x in Data_Worksheet)
            {
                if ((string)x[0] != "Country Name") // omitting first row of headers
                {
                    var IndCode = ConvertFromDBVal<string>(x[3]);
                    var CountryCode = ConvertFromDBVal<string>(x[1]);

                    var IndicatorForData_ID = (from i in _context.Indicators
                                               where i.Code == IndCode
                                               select i.Indicator_ID).First();
                    var DataSourceForData_ID = (from dset in _context.DataSets
                                                where dset.Indicator_ID == IndicatorForData_ID
                                                select dset.DataSource_ID).First(); // bad code
                    var CountryForData_ID = (from c in _context.Countries
                                             where c.Code == CountryCode
                                             select c.Country_ID).First();

                    // Adding Datum
                    for (int i = 4; i < 60; i++)
                    {
                        if (x[i] != null && x[i] != DBNull.Value)
                        {
                            var Data_ID = i - 3;
                            var DataSet_ID = (from c in _context.DataSets
                                              where c.Indicator_ID == IndicatorForData_ID && c.DataSource_ID == DataSourceForData_ID && c.Country_ID == CountryForData_ID
                                              select c.DataSet_ID).First();
                            var Data_Date = Int32.Parse(ConvertFromDBVal<string>(headers[i]));
                            var Data_Value = ConvertFromDBVal<double>(x[i]);

                            returnLog += "\n" + Add_Data(Data_ID, DataSet_ID, Data_Date, Data_Value);
                        }
                    }
                }
            }

            returnLog += "\n" + "----- Data table have been filled -----";
            return returnLog;
        }

        public static T ConvertFromDBVal<T>(object obj)
        {
            if (obj == null || obj == DBNull.Value)
            {
                return default(T); // returns the default value for the type
            }
            else
            {
                return (T)obj;
            }
        }

        public string Add_Region(string _name)
        {
            if (_name == "")
                return "Error: Region name is empty. No entry was added.";

            var newRegion = new Region { Name = _name };

            if (!ContainsRegion(newRegion))
            {
                try
                {
                    _context.Regions.Add(newRegion);
                    _context.SaveChanges();
                }
                catch (Exception e) { return " X  No Region was added. Details:\n" + e.Message; }
            }
            else { return "X  No Region was added. Details:\n" + "UNIQUE Constraint Failed"; }

            return " V  Region " + _name + " added.";
        }
        public string Add_IncomeGroup(string _name)
        {
            if (_name == "")
                return "Error: IncomeGroup name is empty. No entry was added.";

            var newIncomeGroup = new IncomeGroup { Name = _name };

            if (!ContainsIncomeGroup(newIncomeGroup))
            {
                try
                {
                    _context.IncomeGroups.Add(newIncomeGroup);
                    _context.SaveChanges();
                }
                catch (Exception e) { return " X  No IncomeGroup was added. Details:\n" + e.Message; }
            }
            else { return " X  No IncomeGroup was added. Details:\n" + "UNIQUE Constraint Failed"; }

            return " V  IncomeGroup " + _name + " added.";
        }
        public string Add_Country(string _name, string _code, string _note, long _region_ID, long _incomeGroup_ID)
        {
            if (_name == "")
                return "Error: Country name is empty. No entry was added.";

            var newCountry = new Country
            {
                Name = _name,
                Code = _code,
                Note = _note,
                Region_ID = _region_ID,
                IncomeGroup_ID = _incomeGroup_ID
            };

            if (!ContainsCountry(newCountry))
            {
                try
                {
                    _context.Countries.Add(newCountry);
                    _context.SaveChanges();
                }
                catch (Exception e) { return " X  No Country was added. Details:\n" + e.Message; }
            }
            else { return " X  No Country was added. Details:\n" + "UNIQUE Constraint Failed"; }

            return " V  Country " + _name + " added.";
        }
        public string Add_Organization(string _name)
        {
            if (_name == "")
                return "Error: Organization name is empty. No entry was added.";

            var newOrganization = new Organization { Name = _name };

            if (!ContainsOrganization(newOrganization))
            {
                try
                {
                    _context.Organizations.Add(newOrganization);
                    _context.SaveChanges();
                }
                catch (Exception e) { return " X  No Organization was added. Details:\n" + e.Message; }
            }
            else { return " X  No Organization was added. Details:\n" + "UNIQUE Constraint Failed"; }

            return " V  Organization " + _name + " added.";
        }
        public string Add_DataSource(string _Note, long _organization_ID)
        {
            var newDataSource = new DataSource { Note = _Note, Organization_ID = _organization_ID };

            if (!ContainsDataSource(newDataSource))
            {
                try
                {
                    _context.DataSources.Add(newDataSource);
                    _context.SaveChanges();
                }
                catch (Exception e) { return " X  No DataSource was added. Details:\n" + e.Message; }
            }
            else { return " X  No DataSource was added. Details:\n" + "UNIQUE Constraint Failed"; }

            return " V  DataSource enrty added.";
        }
        public string Add_Unit(string _name)
        {
            var newUnit = new Unit { Name = _name };

            if (!ContainsUnit(newUnit))
            {
                try
                {
                    _context.Units.Add(newUnit);
                    _context.SaveChanges();
                }
                catch (Exception e) { return " X  No Unit was added. Details:\n" + e.Message; }
            }
            else { return " X  No Unit was added. Details:\n" + "UNIQUE Constraint Failed"; }

            return " V  Unit " + _name + " added.";
        }
        public string Add_Indicator(string _name, string _code, long _unit_ID)
        {
            if (_name == "")
                return "Error: Indicator name is empty. No entry was added.";

            var newIndicator = new Indicator { Name = _name, Code = _code, Unit_ID = _unit_ID };

            if (!ContainsIndicator(newIndicator))
            {
                try
                {
                    _context.Indicators.Add(newIndicator);
                    _context.SaveChanges();
                }
                catch (Exception e) { return " X  No Indicator was added. Details:\n" + e.Message; }
            }
            else { return " X  No Indicator was added. Details:\n" + "UNIQUE Constraint Failed"; }

            return " V  Indicator " + _name + " added.";
        }
        public string Add_DataSet(long IndicatorForDataSet_ID, long DataSourceForDataSet_ID, long CountryForDataSet_ID)
        {
            var newDataSet = new QualityOfLifePrediction.Data.DataSet { Indicator_ID = IndicatorForDataSet_ID, DataSource_ID = DataSourceForDataSet_ID, Country_ID = CountryForDataSet_ID };

            if (!ContainsDataSet(newDataSet))
            {
                try
                {
                    _context.DataSets.Add(newDataSet);
                    _context.SaveChanges();
                }
                catch (Exception e) { return " X  No DataSet was added. Details:\n" + e.Message; }
            }
            else { return " X  No DataSet was added. Details:\n" + "UNIQUE Constraint Failed"; }

            return " V  DataSet enrty added.";
        }
        public string Add_Data(long Data_ID, long DataSet_ID, long Data_Date, double Data_Value)
        {
            Datum newData = new Datum { Data_ID = Data_ID, DataSet_ID = DataSet_ID, Date = Data_Date, Value = Data_Value };

            if (!ContainsData(newData))
            {
                try
                {
                    _context.Data.Add(newData);
                    _context.SaveChanges();
                }
                catch (Exception e) { return " X  No Data entry was added. Details:\n" + e.Message; }
            }
            else { return " X  No Data entry was added. Details:\n" + "UNIQUE Constraint Failed"; }

            return " V  Data enrty in DataSet #" + DataSet_ID + " was added.";
        }

        private bool ContainsRegion(Region _region)
        {
            return (from r in _context.Regions
                    where r.Name == _region.Name
                    select r).Any();
        }
        private bool ContainsIncomeGroup(IncomeGroup _incomeGroup)
        {
            return (from i in _context.IncomeGroups
                    where i.Name == _incomeGroup.Name
                    select i).Any();
        }
        private bool ContainsCountry(Country _country)
        {
            return (from c in _context.Countries
                    where c.Name == _country.Name
                    select c).Any()
                    || (from c in _context.Countries
                        where c.Code == _country.Code
                        select c).Any();
        }
        private bool ContainsOrganization(Organization _organization)
        {
            return (from o in _context.Organizations
                    where o.Name == _organization.Name
                    select o).Any();
        }
        private bool ContainsDataSource(DataSource _dataSource)
        {
            return (from d in _context.DataSources
                    where d.Note == _dataSource.Note && d.Organization_ID == _dataSource.Organization_ID
                    select d).Any();
        }
        private bool ContainsUnit(Unit _unit)
        {
            return (from u in _context.Units
                    where u.Name == _unit.Name
                    select u).Any();
        }
        private bool ContainsIndicator(Indicator _indicator)
        {
            return (from i in _context.Indicators
                    where i.Name == _indicator.Name
                    select i).Any()
                    && (from i in _context.Indicators
                        where i.Code == _indicator.Code
                        select i).Any();
        }
        private bool ContainsDataSet(DataSet _dataSet)
        {
            return (from ds in _context.DataSets
                    where ds.Indicator_ID == _dataSet.Indicator_ID && ds.DataSource_ID == _dataSet.DataSource_ID && ds.Country_ID == _dataSet.Country_ID
                    select ds).Any();
        }
        private bool ContainsData(Datum _data)
        {
            return (from d in _context.Data
                    where d.DataSet_ID == _data.DataSet_ID && d.Date == _data.Date
                    select d).Any();
        }     
    }
}
