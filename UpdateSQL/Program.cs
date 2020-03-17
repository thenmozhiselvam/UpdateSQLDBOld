
using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;

namespace UpdateSQL
{
    public class Program
    {
        private static log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        static string Filepath = string.Empty;
       
        static string SqlType = ConfigurationManager.AppSettings[Constants.SQL_TYPE];
        static void Main(string[] args)
        {
            try
            {
                ReadFile();
            }
            catch (Exception ex)
            {
                log.Error($"Exception occurred in Main method :{ex.Message} ,Details: {ex.InnerException}");
            }

        }

        public static void ReadFile()
        {
            try
            {
                log.Info("In ReadFile method");
                SQLDBModify sQLDBModify = new SQLDBModify();
                Console.WriteLine("Enter the File path (.csv) for fetching LibraryData :");
                Filepath = Console.ReadLine();
                if (!string.IsNullOrEmpty(Filepath))
                {
                    if (Path.GetExtension(Filepath).ToUpper() == ".CSV")
                    {
                        DataTable csvData = ConvertCSVtoDataTable(Filepath);
                        log.Info("Number of rows in CSV file : " + csvData.Rows.Count);
                        Console.WriteLine("File read successful.");
                        sQLDBModify.Update(csvData);
                    }
                    else
                    {
                        log.Info("Invalid file extension");
                        Console.WriteLine("Invalid file extension");
                        ReadFile();
                    }
                }
                else
                {
                    log.Info("Invalid File path to read");
                    Console.WriteLine("Invalid File path to read");
                    ReadFile();
                }
            }
            catch (Exception ex)
            {
                log.Info("Error : " + ex.Message);
                Console.WriteLine("Error :" + ex.Message);
            }
        }

        public static DataTable ConvertCSVtoDataTable(string strFilePath)
        {
            DataTable csvdatarows = new DataTable();
            try
            {

                using (StreamReader streamReader = new StreamReader(strFilePath))
                {
                    string[] headers = streamReader.ReadLine().Split(',');
                    foreach (string header in headers)
                    {
                        csvdatarows.Columns.Add(header);
                    }
                    while (!streamReader.EndOfStream)
                    {
                        string[] rows = streamReader.ReadLine().Split(',');
                        DataRow dr = csvdatarows.NewRow();
                        for (int i = 0; i < headers.Length; i++)
                        {
                            dr[i] = rows[i];
                        }
                        csvdatarows.Rows.Add(dr);
                    }

                }
            }
            catch (Exception ex)
            {
                log.Error($"Exception occurred in ConvertCSVtoDataTable method :{ex.Message} ,Details: {ex.InnerException}");
            }

            return csvdatarows;
        }
    }

}
