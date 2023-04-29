using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hangfire;
using LimsServer.Services;
using Microsoft.Data.Sqlite;
using Serilog;

namespace LimsServer.Helpers
{
    public class DBPurge
    {
        public int secret { get; set; }
        public int saveDays { get; set; } = 7;
        public bool allCancelled { get; set; } = true;
        public bool allCompleted { get; set; } = true;
        public bool deleteWorkflows { get; set; } = true;
        //public bool deleteAllTasks { get; set; } = false;
        public bool deleteAllLogs { get; set; } = false;
    }

    public class DBManagement
    {
        DBPurge details;
        string dbName = "lims.db";
        string secret = null;

        public DBManagement(DBPurge purge)
        {
            details = purge;
            secret = "A1D0C6E83F027327D8461063F4AC58A6";

        }

        public string generateSecret(string value)
        {
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] inputBytes = Encoding.ASCII.GetBytes(value);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                return Convert.ToHexString(hashBytes);
            }
        }

        public Dictionary<string, string> dbCleanup()
        {
            Dictionary<string, string> results = new Dictionary<string, string>();

            // Simple safeguard against accidently purging the db.
            if (generateSecret(details.secret.ToString()) != secret)
            {
                results.Add("error", "The power to delete is denied to you who knows not the secret.");
                return results;
            }

            string purgeDate = DateTime.Now.AddDays(-1 * details.saveDays).ToString("yyyy-MM-dd HH:mm:ss");
            // Date purge
            string dateQuery = string.Format("DELETE FROM Logs WHERE strftime('%s', time) < strftime('%s', @purgeDate)");
            if (!dbDeleteQuery(dateQuery, "@purgeDate", purgeDate))
            {
                results.Add("DateLogPurge", "Error attempting to purge logs by date with query: " + dateQuery);
            }
            string taskDateQuery = string.Format("DELETE FROM Tasks WHERE strftime('%s', start) < strftime('%s', @purgeDate)");
            if (!dbDeleteQuery(taskDateQuery, "@purgeDate", purgeDate))
            {
                results.Add("DateTaskPurge", "Error attempting to purge tasks by date with query: " + taskDateQuery);
            }

            if (details.allCancelled)          // Delete all cancelled tasks
            {
                string cancelledQuery = "DELETE FROM Tasks WHERE status='CANCELLED'";
                if (!dbDeleteQuery(cancelledQuery))
                {
                    results.Add("Cancelled", "Error attempting to delete all cancelled tasks with query: " + cancelledQuery);
                }
            }
            if (details.allCompleted)          // Delete all completed tasks
            {
                string completedQuery = "DELETE FROM Tasks WHERE status='COMPLETED'";
                if (!dbDeleteQuery(completedQuery))
                {
                    results.Add("Completed", "Error attempting to delete all completed tasks with query: " + completedQuery);
                }
            }
            //if (this.details.deleteAllTasks)        // Delete all tasks
            //{
            //    string allTasksQuery = "DELETE FROM Tasks";
            //    if (!this.dbDeleteQuery(allTasksQuery))
            //    {
            //        results.Add("AllTasks", "Error attempting to delete all tasks with query: " + allTasksQuery);
            //    }
            //    else
            //    {
            //        string setWorkflowsInactive = "UPDATE Workflows SET active=0";
            //        this.dbDeleteQuery(setWorkflowsInactive);
            //    }
            //}
            if (details.deleteWorkflows)       // Delete all inactive workflows
            {
                string deleteWorkflows = "DELETE FROM Workflows WHERE active=0";
                if (!dbDeleteQuery(deleteWorkflows))
                {
                    results.Add("DeleteWorkflows", "Error attempting to delete all inactive workflows with query: " + deleteWorkflows);
                }
            }

            // Dangling task purge
            string danglingQuery = "DELETE FROM Tasks WHERE Tasks.workflowId NOT IN (SELECT id FROM Workflows)";
            if (!dbDeleteQuery(danglingQuery))
            {
                results.Add("DanglingPurge", "Error attempting to purge dangling tasks with query: " + danglingQuery);
            }

            if (details.deleteAllLogs)         // Delete all logs
            {
                string allLogsQuery = "DELETE FROM Logs";
                if (!dbDeleteQuery(allLogsQuery))
                {
                    results.Add("DeleteAllLogs", "Error attempting to delete all logs with query: " + allLogsQuery);
                }
            }
            else // Delete all logs which don't have a task or workflow in the database
            {
                string logsQuery = "DELETE FROM Logs WHERE taskID NOT IN (SELECT id FROM Tasks)";
                if (!dbDeleteQuery(logsQuery))
                {
                    results.Add("DeleteLogs", "Error attempting to delete logs with query: " + logsQuery);
                }
            }

            return results;
        }

        private bool dbDeleteQuery(string query, string key = null, string value = null)
        {
            var conStrBuilder = new SqliteConnectionStringBuilder();
            conStrBuilder.DataSource = dbName;
            try
            {
                using (SqliteConnection con = new SqliteConnection(conStrBuilder.ConnectionString))
                {
                    con.Open();
                    SqliteCommand com = con.CreateCommand();
                    com.CommandText = query;
                    if (!string.IsNullOrEmpty(value) && !string.IsNullOrEmpty(key))
                    {
                        com.Parameters.Add(key, SqliteType.Text).Value = value;
                    }
                    com.ExecuteNonQuery();
                }
                return true;
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Error attempting to execute query the database. Query: " + query);
                return false;
            }

        }
    }
}
