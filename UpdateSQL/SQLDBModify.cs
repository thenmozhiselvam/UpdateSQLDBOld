using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UpdateSQL
{
    public class SQLDBModify
    {
        private static log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public string Table = string.Empty;
        public string OnPremSqlDB = string.Empty;
        public string OnPremSqlServer = string.Empty;
        public string AzureSqlDB = string.Empty;
        public string AzureSqlDBServer = string.Empty;
        public string SqlType = string.Empty;
        static string userName = string.Empty;
        static string Password = string.Empty;
        SqlConnection sqlConnection = new SqlConnection();
        public SQLDBModify()
        {
            Table = ConfigurationManager.AppSettings[Constants.TABLE_NAME];
            OnPremSqlDB = ConfigurationManager.AppSettings[Constants.ONPREM_SQLDB];
            OnPremSqlServer = ConfigurationManager.AppSettings[Constants.ONPREM_SQLSERVER];
            AzureSqlDB = ConfigurationManager.AppSettings[Constants.AZURE_SQLDB];
            AzureSqlDBServer = ConfigurationManager.AppSettings[Constants.AZURE_SQL_DBSERVER];
            SqlType= ConfigurationManager.AppSettings[Constants.SQL_TYPE];
        }

        public void Update(DataTable csvData)
        {
           
         
            string documentum_i_chronicle_id = string.Empty, documentum_r_object_id = string.Empty, i_chronicle_id = string.Empty, a_webc_url = string.Empty, title = string.Empty, display_order = string.Empty,
            r_object_id = string.Empty, content_id = string.Empty, r_folder_path = string.Empty, i_full_format = string.Empty, r_object_type = string.Empty;
            try
            {
                if (SqlType == "OnPremisesSQL")
                {

                    //SQL Integrated Connection string building
                    sqlConnection.ConnectionString =
                    "Data Source=" + OnPremSqlServer + ";" +
                    "Initial Catalog=" + OnPremSqlDB + ";" +
                    "Integrated Security=True;" + "MultipleActiveResultSets=True;" +
                    "App=EntityFramework providerName=System.Data.EntityClient";
                }

                else if (SqlType == "AzureSQL")
                {
                    Console.WriteLine("Enter the username to connect Azure SQL db :");
                    userName = Console.ReadLine();
                    Console.WriteLine("Enter the password");
                    Password = Console.ReadLine();

                    //SQL Integrated Connection string building
                    sqlConnection.ConnectionString ="Server =" + AzureSqlDBServer +";" + "Initial Catalog=" + AzureSqlDB+ ";" + "Persist Security Info = False;" + "User ID=" + userName + ";" + "Password=" + Password + ";" + "MultipleActiveResultSets = False; Encrypt = True; TrustServerCertificate = False; Connection Timeout = 30";
                
                }
                   // SqlConnection sqlConnection = new SqlConnection(ConfigurationManager.AppSettings[Constants.CONNECTION_STRING]);

                foreach (DataRow dataRow in csvData.Rows)
                {
                    sqlConnection.Open();
                    documentum_i_chronicle_id = Convert.ToString(dataRow[Constants.DOCUMENTUM_I_CHRONICLE_ID]);
                    documentum_r_object_id = Convert.ToString(dataRow[Constants.DOCUMENTUM_R_OBJECT_ID]);
                    log4net.GlobalContext.Properties[Constants.R_OBJECT_ID] = documentum_r_object_id;
                    i_chronicle_id = Convert.ToString(dataRow[Constants.I_CHRONICLE_ID]);
                    r_object_id = Convert.ToString(dataRow[Constants.R_OBJECT_ID]);

                    content_id = Convert.ToString(dataRow[Constants.CONTENT_ID]);
                    r_folder_path = Convert.ToString(dataRow[Constants.R_FOLDER_PATH]);
                    i_full_format = Convert.ToString(dataRow[Constants.I_FULL_FORMAT]);
                    r_object_type = Convert.ToString(dataRow[Constants.R_OBJECT_TYPE]);

                    int rowCount = GetDataFromSQL(sqlConnection, documentum_i_chronicle_id, documentum_r_object_id, i_full_format, r_object_type);

                    if (rowCount > 0)
                    {
                        if (rowCount == 1)
                        {
                            if (r_object_type.ToUpper() != Constants.IR_ARTICLE.ToUpper())
                                UpdateDB(sqlConnection, i_chronicle_id, r_object_id, content_id, r_folder_path, r_object_type, i_full_format, documentum_r_object_id, documentum_i_chronicle_id);
                            else
                            {
                                a_webc_url = Convert.ToString(dataRow["a_webc_url"]);
                                UpdateDB_Articles(sqlConnection, i_chronicle_id, r_object_id, content_id, r_folder_path, r_object_type, i_full_format, a_webc_url, documentum_r_object_id, documentum_i_chronicle_id);
                            }
                        }
                        else
                            log.Debug($"SQL data Row count more than 1 - RowCount : {rowCount} for documentum_r_object_id  : {documentum_r_object_id},r_object_id : {r_object_id}, i_chronicle_id : {i_chronicle_id}, content_id : {content_id}, r_folder_path : {r_folder_path}");
                    }
                    else
                    {
                        if (r_object_type.ToUpper() == Constants.IR_ARTICLE_IMAGE.ToUpper())
                        {
                            a_webc_url = Convert.ToString(dataRow["a_webc_url"]);
                            title = dataRow["title"].ToString();
                            display_order = !string.IsNullOrEmpty(dataRow["display_order"].ToString()) ? dataRow["display_order"].ToString() : "0";
                            InsertDB(sqlConnection, r_object_id, content_id, r_object_type, r_folder_path, i_full_format, a_webc_url, title, documentum_r_object_id, i_chronicle_id, display_order);
                        }
                        else
                            log.Info($"For the given for documentum_r_object_id : {documentum_r_object_id}, content not available in sql r_object_id : {r_object_id}, i_chronicle_id : {i_chronicle_id}, content_id : {content_id}, r_folder_path : {r_folder_path}");
                    }
                    sqlConnection.Close();
                }
            }
            catch (Exception ex)
            {
                log.Error($"Exception occurred in UpdateDB method :{ex.Message} ,Details: {ex.InnerException}");
            }
        }

        public int GetDataFromSQL(SqlConnection sqlConnection, string documentum_i_chronicle_id, string documentum_r_object_id, string i_full_format, string r_object_type)
        {
            int count = 0;
            try
            {
                using (SqlCommand command = new SqlCommand("Select COUNT (*) from " + Table + " Where r_object_id = @documentum_r_object_id and i_chronicle_id = @documentum_i_chronicle_id and i_full_format= @i_full_format and r_object_type =@r_object_type", sqlConnection))
                {
                    command.Parameters.AddWithValue(Constants.SP_R_OBJECT_TYPE, r_object_type);
                    command.Parameters.AddWithValue(Constants.SP_I_FULL_FORMAT, i_full_format);
                    command.Parameters.AddWithValue(Constants.SP_DOCUMENTUM_R_OBJECT_ID, documentum_r_object_id);
                    command.Parameters.AddWithValue(Constants.SP_DOCUMENTUM_I_CHRONICLE_ID, documentum_i_chronicle_id);
                    count = (int)command.ExecuteScalar();

                }
            }
            catch (Exception ex)
            {
                log.Error($"Exception occurred in GetDataFromSQL method :{ex.Message} ,Details: {ex.InnerException}");
            }
            return count;
        }

        public void UpdateDB(SqlConnection sqlConnection, string i_chronicle_id, string r_object_id, string content_id, string r_folder_path, string r_object_type, string i_full_format, string documentum_r_object_id, string documentum_i_chronicle_id)
        {
            try
            {
                log.Info($"In UpdateDB Method for documentum_r_object_id  : {documentum_r_object_id}");
                using (SqlCommand command = sqlConnection.CreateCommand())
                {
                    command.CommandText = "UPDATE " + Table + " SET r_object_id = @r_object_id, i_chronicle_id = @i_chronicle_id,content_id = @content_id,r_folder_path = @r_folder_path Where r_object_id = @documentum_r_object_id and i_chronicle_id = @documentum_i_chronicle_id and i_full_format= @i_full_format and r_object_type =@r_object_type";
                    //update column in sql
                    command.Parameters.AddWithValue(Constants.SP_I_CHRONICLE_ID, i_chronicle_id);
                    command.Parameters.AddWithValue(Constants.SP_R_OBJECT_ID, r_object_id);
                    command.Parameters.AddWithValue(Constants.SP_CONTENT_ID, content_id);
                    command.Parameters.AddWithValue(Constants.SP_R_FOLDER_PATH, r_folder_path);
                    //used in where condition
                    command.Parameters.AddWithValue(Constants.SP_R_OBJECT_TYPE, r_object_type);
                    command.Parameters.AddWithValue(Constants.SP_I_FULL_FORMAT, i_full_format);
                    command.Parameters.AddWithValue(Constants.SP_DOCUMENTUM_R_OBJECT_ID, documentum_r_object_id);
                    command.Parameters.AddWithValue(Constants.SP_DOCUMENTUM_I_CHRONICLE_ID, documentum_i_chronicle_id);
                    command.ExecuteNonQuery();
                    log.Debug($"Updated SQL data successfully for documentum_r_object_id  : {documentum_r_object_id}, r_object_id : {r_object_id}, i_chronicle_id : {i_chronicle_id}, content_id : {content_id}, r_folder_path : {r_folder_path}");

                }
            }
            catch (Exception ex)
            {
                log.Debug($"Exception in UpdateDB Method for documentum_r_object_id  : {documentum_r_object_id}, r_object_id : {r_object_id}, i_chronicle_id : {i_chronicle_id}, content_id : {content_id}, r_folder_path : {r_folder_path}, Error : {ex.Message}");
            }
        }

        public void UpdateDB_Articles(SqlConnection sqlConnection, string i_chronicle_id, string r_object_id, string content_id, string r_folder_path, string r_object_type, string i_full_format, string a_webc_url, string documentum_r_object_id, string documentum_i_chronicle_id)
        {
            try
            {
                log.Info($"In UpdateDB Method for documentum_r_object_id  : {documentum_r_object_id}");
                using (SqlCommand command = sqlConnection.CreateCommand())
                {
                    command.CommandText = "UPDATE " + Table + " SET r_object_id = @r_object_id, i_chronicle_id = @i_chronicle_id,content_id = @content_id,r_folder_path = @r_folder_path,a_webc_url = @a_webc_url Where r_object_id = @documentum_r_object_id and i_chronicle_id = @documentum_i_chronicle_id and i_full_format= @i_full_format and r_object_type =@r_object_type";
                    //update column in sql
                    command.Parameters.AddWithValue(Constants.SP_I_CHRONICLE_ID, i_chronicle_id);
                    command.Parameters.AddWithValue(Constants.SP_R_OBJECT_ID, r_object_id);
                    command.Parameters.AddWithValue(Constants.SP_CONTENT_ID, content_id);
                    command.Parameters.AddWithValue(Constants.SP_R_FOLDER_PATH, r_folder_path);
                    command.Parameters.AddWithValue(Constants.SP_A_WEBC_URL, a_webc_url);
                    //used in where condition
                    command.Parameters.AddWithValue(Constants.SP_R_OBJECT_TYPE, r_object_type);
                    command.Parameters.AddWithValue(Constants.SP_I_FULL_FORMAT, i_full_format);
                    command.Parameters.AddWithValue(Constants.SP_DOCUMENTUM_R_OBJECT_ID, documentum_r_object_id);
                    command.Parameters.AddWithValue(Constants.SP_DOCUMENTUM_I_CHRONICLE_ID, documentum_i_chronicle_id);
                    command.ExecuteNonQuery();
                    log.Debug($"Updated SQL data successfully for documentum_r_object_id  : {documentum_r_object_id}, r_object_id : {r_object_id}, i_chronicle_id : {i_chronicle_id}, content_id : {content_id}, r_folder_path : {r_folder_path}, a_webc_url : {a_webc_url}");

                }
            }
            catch (Exception ex)
            {
                log.Debug($"Exception in UpdateDB Method for documentum_r_object_id  : {documentum_r_object_id}, r_object_id : {r_object_id}, i_chronicle_id : {i_chronicle_id}, content_id : {content_id}, r_folder_path : {r_folder_path}, Error : {ex.Message}");
            }
        }

        public void InsertDB(SqlConnection sqlConnection, string r_object_id, string content_id, string r_object_type, string r_folder_path, string i_full_format, string a_webc_url, string title, string documentum_r_object_id, string i_chronicle_id, string display_order)
        {
            string query = string.Empty;
            try
            {
                query = "INSERT INTO " + Table + " (r_object_id, i_chronicle_id, content_id, r_object_type, r_folder_path,  i_full_format,a_webc_url,title,object_name,display_order,r_modify_date)";
                query += " VALUES (@r_object_id, @i_chronicle_id, @content_id, @r_object_type, @r_folder_path, @i_full_format,@a_webc_url,@title,@object_name,@display_order,GETDATE())";

                using (SqlCommand command = sqlConnection.CreateCommand())
                {
                    command.CommandText = query;
                    command.Parameters.AddWithValue(Constants.SP_R_OBJECT_ID, r_object_id);
                    command.Parameters.AddWithValue(Constants.SP_I_CHRONICLE_ID, r_object_id);
                    command.Parameters.AddWithValue(Constants.SP_CONTENT_ID, content_id);
                    command.Parameters.AddWithValue(Constants.SP_R_OBJECT_TYPE, r_object_type);
                    command.Parameters.AddWithValue(Constants.SP_R_FOLDER_PATH, r_folder_path);
                    command.Parameters.AddWithValue(Constants.SP_I_FULL_FORMAT, i_full_format);
                    command.Parameters.AddWithValue(Constants.SP_A_WEBC_URL, a_webc_url);
                    command.Parameters.AddWithValue(Constants.SP_TITLE, title);
                    command.Parameters.AddWithValue(Constants.SP_OBJECT_NAME, title);
                    command.Parameters.AddWithValue(Constants.SP_DISPLAY_ORDER, display_order);
                    command.ExecuteNonQuery();
                    log.Debug($"Inserted SQL data successfully for documentum_r_object_id  : {documentum_r_object_id}, r_object_id : {r_object_id}, i_chronicle_id : {i_chronicle_id}, content_id : {content_id}, r_folder_path : {r_folder_path}");
                }
            }
            catch (Exception ex)
            {
                log.Debug($"Exception in InsertDB Method for documentum_r_object_id  : {documentum_r_object_id}, r_object_id : {r_object_id}, i_chronicle_id : {i_chronicle_id}, content_id : {content_id}, r_folder_path : {r_folder_path}, Error : {ex.Message}");
            }
        }
    }
}
