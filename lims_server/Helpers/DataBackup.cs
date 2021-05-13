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
    public class DataBackup
    {
        string backupDB = "lims_data.db";
        int daysStored = 7;

        public DataBackup() { }

        public bool DumpData(string id, string inputFile, string outputFile)
        {
            var conStrBuilder = new SqliteConnectionStringBuilder();
            conStrBuilder.DataSource = this.backupDB;

            try
            {
                using (var conn = new SqliteConnection(conStrBuilder.ConnectionString))
                {
                    conn.Open();
                    using (var trans = conn.BeginTransaction())
                    {
                        if (File.Exists(inputFile))
                        {
                            using(var ms = new MemoryStream())
                            {
                                var file = File.OpenRead(inputFile);
                                file.CopyTo(ms);
                                file.Close();
                                byte[] data = ms.ToArray();
                                var timestamp = DateTime.UtcNow.Ticks;
                                string query = String.Format("INSERT INTO InputData (id, create_date, data) VALUES(@id, @timestamp, @data)");
                                var com = conn.CreateCommand();
                                com.CommandText = query;
                                com.Parameters.Add("@id", SqliteType.Text).Value = id;
                                com.Parameters.Add("@timestamp", SqliteType.Integer).Value = timestamp;
                                com.Parameters.Add("@data", SqliteType.Blob).Value = data;
                                com.ExecuteNonQuery();
                            }                            
                        }

                        if (File.Exists(outputFile))
                        {
                            using (var ms = new MemoryStream())
                            {
                                var file = File.OpenRead(outputFile);
                                file.CopyTo(ms);
                                file.Close();
                                var data = ms.ToArray();
                                ms.Close();
                                var timestamp = DateTime.UtcNow.Ticks;
                                string query = String.Format("INSERT INTO OutputData (id, create_date, data) VALUES(@id, @timestamp, @data)");
                                var com = conn.CreateCommand();
                                com.CommandText = query;
                                com.Parameters.Add("@id", SqliteType.Text).Value = id;
                                com.Parameters.Add("@timestamp", SqliteType.Integer).Value = timestamp;
                                com.Parameters.Add("@data", SqliteType.Blob).Value = data;
                                com.ExecuteNonQuery();
                            }
                        }
                        trans.Commit();
                    }
                }
            }
            catch(Exception ex)
            {
                Log.Warning(ex, "Error dumping input/output files into database.");
                return false;
            }
            return true;
        }

        public string DataCheck(string id, DataContext context)
        {
            // Check lims.db for task ID entry
            List<Entities.Task> task = context.Tasks.Where(t => t.id == id).ToList();
            if (task.Count == 0)
            {
                return String.Format("No task ID found with that id: {0}.", id);
            }

            // Check lims_data.db for input and output data entries.
            var conStrBuilder = new SqliteConnectionStringBuilder();
            conStrBuilder.DataSource = this.backupDB;
            using (var conn = new SqliteConnection(conStrBuilder.ConnectionString))
            {
                conn.Open();
                string inQuery = "SELECT COUNT(*) FROM InputData WHERE id=@id";
                var inCmd = conn.CreateCommand();
                inCmd.CommandText = inQuery;
                inCmd.Parameters.Add("@id", SqliteType.Text).Value = id;
                int inCount = Convert.ToInt32(inCmd.ExecuteScalar());

                string outQuery = "SELECT COUNT(*) FROM OutputData WHERE id=@id";
                var outCmd = conn.CreateCommand();
                outCmd.CommandText = outQuery;
                outCmd.Parameters.Add("@id", SqliteType.Text).Value = id;
                int outCount = Convert.ToInt32(outCmd.ExecuteScalar());

                inCmd.Dispose();
                outCmd.Dispose();
                conn.Close();
                if(inCount == 0 || outCount == 0)
                {
                    return String.Format("Data for task ID: {0} is no longer backed up. Backup expired.", id);
                }
            }
            return "";
        }

        public Dictionary<string, byte[]> GetData(string id)
        {
            var conStrBuilder = new SqliteConnectionStringBuilder();
            conStrBuilder.DataSource = this.backupDB;
            Dictionary<string, byte[]> data = new Dictionary<string, byte[]>();
            try
            {
                using (var conn = new SqliteConnection(conStrBuilder.ConnectionString))
                {
                    conn.Open();
                    string query = "SELECT data FROM InputData WHERE id=@id";
                    var select = conn.CreateCommand();
                    select.CommandText = query;
                    select.Parameters.Add("@id", SqliteType.Text).Value = id;
                    using (var reader = select.ExecuteReader())
                    {
                        reader.Read();
                        MemoryStream inputBlob = reader.GetStream("data") as MemoryStream;
                        byte[] inputData = inputBlob.ToArray();
                        data.Add("input", inputData);
                        inputBlob.Close();
                    }
                    select = conn.CreateCommand();
                    query = "SELECT data FROM OutputData WHERE id=@id";
                    select.CommandText = query;
                    select.Parameters.Add("@id", SqliteType.Text).Value = id;

                    using (var reader = select.ExecuteReader())
                    {
                        reader.Read();
                        MemoryStream outputBlob = reader.GetStream("data") as MemoryStream;
                        byte[] outputData = outputBlob.ToArray();
                        data.Add("output", outputData);
                        outputBlob.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Error reading input/output files in database.");
                return null;
            }
            return data;
        }

        public byte[] GetTaskData(string id, DataContext context)
        {
            Entities.Task task = context.Tasks.Where(t => t.id == id).First();

            Dictionary<string, byte[]> data = this.GetData(task.id);

            Dictionary<string, byte[]> files = new Dictionary<string, byte[]>();
            if (data.ContainsKey("input"))
            {
                byte[] inputData = data["input"];
                string inputName = task.start.ToString("yyyyMMddHHmmss") + "_" + Path.GetFileName(task.inputFile);
                files.Add(inputName, inputData);
            }
            if (data.ContainsKey("output"))
            {
                byte[] outputData = data["output"];
                string outputName = task.start.ToString("yyyyMMddHHmmss") + "_" + Path.GetFileName(task.outputFile);
                files.Add(outputName, outputData);
            }
            byte[] compressedFile;
            using(var mStream = new MemoryStream())
            {
                using (var archive = new ZipArchive(mStream, ZipArchiveMode.Create, true))
                {
                    foreach (var file in files)
                    {
                        var f = archive.CreateEntry(file.Key, CompressionLevel.Optimal);
                        using(var zStream = f.Open())
                        {
                            zStream.Write(file.Value, 0, file.Value.Length);
                        }
                    }
                }
                compressedFile = mStream.ToArray();
            }
            return compressedFile;
        }

        public bool Cleanup()
        {
            Log.Information("Backup database cleanup started. Current data storage time: {0}", this.daysStored);
            // Current data store life set to 1 week
            long oneWeekTick = DateTime.UtcNow.AddDays(-1 * this.daysStored).Ticks;       // Tick difference for one week.

            // TESTING
            // long testTick = DateTime.UtcNow.AddMinutes(-5).Ticks;
            // oneWeekTick = testTick;

            var conStrBuilder = new SqliteConnectionStringBuilder();
            conStrBuilder.DataSource = this.backupDB;

            try
            {
                using (var conn = new SqliteConnection(conStrBuilder.ConnectionString))
                {
                    conn.Open();
                    using (var trans = conn.BeginTransaction())
                    {
                        var com1 = conn.CreateCommand();
                        string query = String.Format("DELETE FROM InputData WHERE create_date < @tick");
                        com1.CommandText = query;
                        com1.Parameters.Add("@tick", SqliteType.Integer).Value = oneWeekTick;
                        com1.ExecuteNonQuery();

                        var com2 = conn.CreateCommand();
                        query = String.Format("DELETE FROM OutputData WHERE create_date < @tick");
                        com2.CommandText = query;
                        com2.Parameters.Add("@tick", SqliteType.Integer).Value = oneWeekTick;
                        com2.ExecuteNonQuery();

                        trans.Commit();
                    }
                    var com3 = conn.CreateCommand();
                    string query3 = String.Format("VACUUM");
                    com3.CommandText = query3;
                    com3.ExecuteNonQuery();

                }
                Log.Information("Backup database cleanup completed.");
                return true;
            }
            catch (SqliteException ex)
            {
                Log.Warning("Error cleaning up database.", ex);
                return false;
            }
        }

        public void ScheduleCleanup()
        {
            string id = "db-cleanup-24hr";
            RecurringJob.RemoveIfExists(id);

            // Cleanup Scheduled for every 24 hours.
            RecurringJob.AddOrUpdate(id, () => this.Cleanup(), Cron.Daily);
            //TESTING
            //RecurringJob.AddOrUpdate(id, () => this.Cleanup(), Cron.Minutely);

        }
    }
}
