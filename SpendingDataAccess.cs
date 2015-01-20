using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Configuration;
using System.Transactions;
using System.Data;
using MyTraceLogger;
using System.Diagnostics;

namespace SpendingDataAccess
{
    public class SpendingDataAccess
    {
        string conectionString = "";
        private static MyTraceSourceLogger myLogger;

        public SpendingDataAccess()
        {
            //myLoger is null?
            myLogger = new MyTraceSourceLogger("SpendingDataTrace");

            if (myLogger != null)
            {
                try
                {
                    conectionString = ConfigurationManager.ConnectionStrings["Spendings"].ConnectionString;
                }
                catch (ArgumentNullException ex)
                {
                    myLogger.TraceEvent(TraceEventType.Error, 1, "ConectionString NullReferenceException: {0}", ex.Message);
                    throw;
                }
            }
            else
            {
                throw new NullReferenceException("Unable to initialize logger.");
            }
        }
        public List<displayCategorizedTransaction> GetDisplayCategorizedTransaction(DateTime beginTransaction, DateTime endTransaction)
        {
            List<displayCategorizedTransaction> wantedTransactionList = new List<displayCategorizedTransaction>();

            try
            {
                using (SqlConnection connection = new SqlConnection(conectionString))
                {
                    SqlCommand cmd = new SqlCommand();
                    cmd.CommandText = "select bn.BusinessFilterPhrase,tran.Date,tran.Descriptions, tran.CheckNumber, tran.Amt,cat.BusinessCategory,filterPhrase "
                        + "from tblTransaction tran "
                        + "join tblBusinessName bn on bn.BusinessID = tran.BusinessID "
                        + "";
                }
            }
            catch (SqlException ex)
            {
                StringBuilder errorMessages = new StringBuilder();

                for (int i = 0; i < ex.Errors.Count; i++)
                {
                    errorMessages.Append("Index #" + i + "\n" +
                        "Message: " + ex.Errors[i].Message + "\n" +
                        "LineNumber: " + ex.Errors[i].LineNumber + "\n" +
                        "Source: " + ex.Errors[i].Source + "\n" +
                        "Procedure: " + ex.Errors[i].Procedure + "\n");
                }
                myLogger.TraceEvent(TraceEventType.Error, 1, errorMessages.ToString());
            }
            catch (Exception ex)
            {
                myLogger.TraceEvent(TraceEventType.Error, 1, "Unknown error: {0}", ex.Message);
            }

            return wantedTransactionList;
        }
        /*Car Payment,Car Insurance,Car Maintenance,House Payment,House Insurance,House Maintenance,Electric & Gas
         *Cable, Internet,Phone,Gifts,Medical Bill,Grocery,Clothes,School Cost,Eat Out,Vacation,Pets,Entertainment,Other,Tax
         *Security*/
        public Dictionary<string, int> GetCategoriesList()
        {
            Dictionary<string, int> categoryList = new Dictionary<string, int>();
            try
            {
                using (SqlConnection connection = new SqlConnection(conectionString))
                {
                    connection.Open();
                    //get string filter and int category in tblBusinessName and put into dictionary list
                    SqlDataAdapter daFilter = new SqlDataAdapter("Select BusinessCategoryID,BusinessCategoryName from tblCategories", connection);
                    DataSet dsFilter = new DataSet();
                    daFilter.Fill(dsFilter);

                    foreach (DataTable table in dsFilter.Tables)
                    {
                        foreach (DataRow dr in table.Rows)
                        {
                            int tmpInt = 0;
                            int.TryParse(dr["BusinessCategoryID"].ToString(), out tmpInt);
                            try
                            {
                                categoryList.Add(dr["BusinessCategoryName"].ToString(), tmpInt);
                            }
                            catch (ArgumentException ex)
                            {
                                //There is duplication in table, need to log it.  Table suppose to not have duplication
                                myLogger.TraceEvent(TraceEventType.Warning, 2, "Duplicated BusinessFilterPhrase found: {0}", ex.Message);
                            }
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                StringBuilder errorMessages = new StringBuilder();

                for (int i = 0; i < ex.Errors.Count; i++)
                {
                    errorMessages.Append("Index #" + i + "\n" +
                        "Message: " + ex.Errors[i].Message + "\n" +
                        "LineNumber: " + ex.Errors[i].LineNumber + "\n" +
                        "Source: " + ex.Errors[i].Source + "\n" +
                        "Procedure: " + ex.Errors[i].Procedure + "\n");
                }
                myLogger.TraceEvent(TraceEventType.Error, 1, errorMessages.ToString());
                throw;
            }
            catch (Exception ex)
            {
                myLogger.TraceEvent(TraceEventType.Error, 1, "Unknown error: {0}", ex.Message);
                throw;
            }

            return categoryList;
        }
        public Dictionary<string, int> GetFilterCategoriesList()
        {
            Dictionary<string, int> filterCategoryList = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            try
            {
                using (SqlConnection connection = new SqlConnection(conectionString))
                {
                    connection.Open();
                    //get string filter and int category in tblBusinessName and put into dictionary list
                    SqlDataAdapter daFilter = new SqlDataAdapter("Select BusinessFilterPhrase,BusinessCategoryID from tblBusinessName", connection);
                    DataSet dsFilter = new DataSet();
                    daFilter.Fill(dsFilter);

                    foreach (DataTable table in dsFilter.Tables)
                    {
                        foreach (DataRow dr in table.Rows)
                        {
                            int tmpInt = 0;
                            int.TryParse(dr["BusinessCategoryID"].ToString(), out tmpInt);
                            try
                            {
                                filterCategoryList.Add(dr["BusinessFilterPhrase"].ToString(), tmpInt);
                            }
                            catch (ArgumentException ex)
                            {
                                //There is duplication in table, need to log it.  Table suppose to not have duplication
                                myLogger.TraceEvent(TraceEventType.Warning, 2, "Duplicated BusinessFilterPhrase found: {0}", ex.Message);
                            }
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                StringBuilder errorMessages = new StringBuilder();

                for (int i = 0; i < ex.Errors.Count; i++)
                {
                    errorMessages.Append("Index #" + i + "\n" +
                        "Message: " + ex.Errors[i].Message + "\n" +
                        "LineNumber: " + ex.Errors[i].LineNumber + "\n" +
                        "Source: " + ex.Errors[i].Source + "\n" +
                        "Procedure: " + ex.Errors[i].Procedure + "\n");
                }
                myLogger.TraceEvent(TraceEventType.Error, 1, errorMessages.ToString());
                throw;
            }
            catch (Exception ex)
            {
                myLogger.TraceEvent(TraceEventType.Error, 1, "Unknown error: {0}", ex.Message);
                throw;
            }

            return filterCategoryList;
        }

        public bool SaveDataToDB(List<CategorizedTransaction> categorizedTransactionList, Dictionary<string, int> filterCategoryList)
        {
            bool bReturn = true;

            try
            {

                using (TransactionScope scope = new TransactionScope())
                {

                    SaveFilterCategories(filterCategoryList);

                    SaveCategorizedTransaction(categorizedTransactionList);

                    scope.Complete();
                }

            }
            catch (SqlException ex)
            {
                StringBuilder errorMessages = new StringBuilder();

                for (int i = 0; i < ex.Errors.Count; i++)
                {
                    errorMessages.Append("Index #" + i + "\n" +
                        "Message: " + ex.Errors[i].Message + "\n" +
                        "LineNumber: " + ex.Errors[i].LineNumber + "\n" +
                        "Source: " + ex.Errors[i].Source + "\n" +
                        "Procedure: " + ex.Errors[i].Procedure + "\n");
                }
                myLogger.TraceEvent(TraceEventType.Error, 1, errorMessages.ToString());
            }
            catch (Exception ex)
            {
                myLogger.TraceEvent(TraceEventType.Error, 1, "Unknown error: {0}", ex.Message);
            }

            return bReturn;
        }

        public void SaveCategorizedTransaction(List<CategorizedTransaction> categorizedTransactionList)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(conectionString))
                {
                    SqlCommand cmd = new SqlCommand();
                    cmd.Connection = connection;

                    //get BusinessID vs BusinessFilterPhrase and put it in dictionary list
                    Dictionary<string, string> businessIDvsPhrase = new Dictionary<string, string>();
                    cmd.CommandText = "select BusinessID, BusinessFilterPhrase from tblBusinessName ";
                    SqlDataReader reader = cmd.ExecuteReader();

                    while (reader.Read())
                    {
                        try
                        {
                            businessIDvsPhrase.Add(reader["BusinessFilterPhrase"].ToString(), reader["BusinessID"].ToString());
                        }
                        catch (ArgumentException ex)
                        {
                            //duplication error, logs, this table BusinessFilterPhrase is unique
                            myLogger.TraceEvent(TraceEventType.Warning, 1, "Duplicated BusinessFilterPhrase detected: {0}", ex.Message);
                        }
                    }

                    reader.Close();

                    //List<CategorizedTransaction> categorizedTransactionList = null;
                    //inserting non-duplicate transaction
                    int rowAffected = 0;

                    if (categorizedTransactionList != null)
                    {
                        foreach (CategorizedTransaction transaction in categorizedTransactionList)
                        {
                            cmd.Parameters.Clear();
                            if (businessIDvsPhrase.ContainsKey(transaction.TranFilterPhrase))
                            {
                                cmd.CommandText = "insert into tblTransaction (Date,Descriptions,CheckNumber,Amount,BusinessID) " +
                                    "Values (@Date,@Descriptions,@CheckNumber,@Amount,@BusinessID)";

                                cmd.Parameters.AddWithValue("@Date", transaction.myDate);
                                cmd.Parameters.AddWithValue("@Descriptions", transaction.Description);
                                cmd.Parameters.AddWithValue("@CheckNumber", transaction.check);
                                cmd.Parameters.AddWithValue("@Amount", transaction.Amt);
                                cmd.Parameters.AddWithValue("@BusinessID", businessIDvsPhrase[transaction.TranFilterPhrase]);

                                rowAffected = cmd.ExecuteNonQuery();
                            }
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                StringBuilder errorMessages = new StringBuilder();

                for (int i = 0; i < ex.Errors.Count; i++)
                {
                    errorMessages.Append("Index #" + i + "\n" +
                        "Message: " + ex.Errors[i].Message + "\n" +
                        "LineNumber: " + ex.Errors[i].LineNumber + "\n" +
                        "Source: " + ex.Errors[i].Source + "\n" +
                        "Procedure: " + ex.Errors[i].Procedure + "\n");
                }
                myLogger.TraceEvent(TraceEventType.Error, 1, errorMessages.ToString());
            }
            catch (Exception ex)
            {
                myLogger.TraceEvent(TraceEventType.Error, 1, "Unknown error: {0}", ex.Message);
            }
        }

        public void SaveFilterCategories(Dictionary<string, int> filterCategoryList)
        {
            try
            {

                using (SqlConnection connection = new SqlConnection(conectionString))
                {
                    int rowAffected = 0;

                    SqlCommand cmd = new SqlCommand();
                    cmd.Connection = connection;
                    //cmd.CommandType = 

                    //inserting non-duplicate filter vs phrases
                    //Dictionary<string, int> filterCategoryList = null;
                    if (filterCategoryList != null)
                    {
                        foreach (KeyValuePair<string, int> filter in filterCategoryList)
                        {
                            cmd.Parameters.Clear();
                            cmd.CommandText = "insert into tblBusinessName (BusinessFilterPhrase,BusinessCategoryID) " +
                                            "select @BusinessFilterPhrase,@BusinessCategoryID " +
                                            "where NOT EXISTS " +
                                            "( select 1 from tblBusinessName as d1 where d1.BusinessFilterPhrase = @BusinessFilterPhrase) ";

                            cmd.Parameters.AddWithValue("@BusinessFilterPhrase", filter.Key);
                            cmd.Parameters.AddWithValue("@BusinessCategoryID", filter.Value);

                            rowAffected = cmd.ExecuteNonQuery();

                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                StringBuilder errorMessages = new StringBuilder();

                for (int i = 0; i < ex.Errors.Count; i++)
                {
                    errorMessages.Append("Index #" + i + "\n" +
                        "Message: " + ex.Errors[i].Message + "\n" +
                        "LineNumber: " + ex.Errors[i].LineNumber + "\n" +
                        "Source: " + ex.Errors[i].Source + "\n" +
                        "Procedure: " + ex.Errors[i].Procedure + "\n");
                }
                myLogger.TraceEvent(TraceEventType.Error, 1, errorMessages.ToString());
            }
            catch (Exception ex)
            {
                myLogger.TraceEvent(TraceEventType.Error, 1, "Unknown error: {0}", ex.Message);
            }
        }
    }
}
