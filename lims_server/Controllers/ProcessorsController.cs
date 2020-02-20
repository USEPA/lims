using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.Net.Http.Headers;
using System.IO;
using System.IO.Compression;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Data.SQLite;
using LimsServer.Services;
using LimsServer.Dtos;
using LimsServer.Entities;
using PluginBase;
//using LiteDB;

namespace LimsServer.Controllers
{
    //[Authorize]
    [Route("[controller]")]
    [ApiController]
    public class ProcessorsController : ControllerBase
    {
        //private readonly IHostingEnvironment _hostingEnvironment;
        private IProcessorService _processorService;
        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly ILogger<ProcessorsController> _logger;

        public ProcessorsController(
            IWebHostEnvironment hostingEnvironment, 
            ILogger<ProcessorsController> logger,
            IProcessorService proccessorService)
        {
            _hostingEnvironment = hostingEnvironment;
            _logger = logger;
            _processorService = proccessorService;
        }


        /// <summary>
        /// Loop over the folders in the processors folder.
        /// Each folder will contain a dll that has a class that 
        /// implmenets the IProcessor interface
        /// The dll will have the same name as the folder.
        /// </summary>
        /// <returns></returns>
        private List<Processor> GetListOfProcessors()
        {
            List<Processor> lst = null;
            return lst;

            //return _processorService.GetAll().ToList();

            List<ProcessorDTO> lstProcessors = new List<ProcessorDTO>();
            ProcessorManager procMgr = new ProcessorManager();
           
            //try
            //{                
                string projectRootPath = _hostingEnvironment.ContentRootPath;
                string processorPath = Path.Combine(projectRootPath, "app_files", "processors");
                lstProcessors = procMgr.GetProcessors(processorPath);                
            //}
            //catch (Exception ex)
            //{
                //_logger.LogError(ex.Message, "Problem reading processors directory list.");                
            //}
            //return lstProcessors;

        }
        private void InsertProcessorIntoDB(Processor proc)
        {
            
            string qry = @"insert or replace into processors(unique_id, name, description, instrument_file_type, processor_found) 
                        values('{0}', '{1}', '{2}', '{3}',  1)";

            //'{1}', '{2}', '{3}', '{4}', 1)";

            qry = string.Format(qry, proc.id, proc.name, proc.description, proc.file_type, "1");

            //try
            //{
                string projectRootPath = _hostingEnvironment.ContentRootPath;
                string file_name = Path.Combine(projectRootPath, "app_files", "database", "lims.db");
                using (SQLiteConnection conn = new SQLiteConnection("Data Source=" + file_name))
                {
                    conn.Open();
                    SQLiteCommand command = conn.CreateCommand();
                    command.CommandText = qry;
                    //command.Parameters.AddWithValue("$text1", jsonProcessor);
                    command.ExecuteNonQuery();
                }
            //}
            //catch(Exception ex)
            //{
            //    string sval = ex.Message;
            //}
        }

        private void SetProcessorsToNotFound()
        {

            string qry = @"update processors set processor_found = 0 where processor_found = 1";                                                

            //try
            //{
                string projectRootPath = _hostingEnvironment.ContentRootPath;
                string file_name = Path.Combine(projectRootPath, "app_files", "database", "lims.db");
                using (SQLiteConnection conn = new SQLiteConnection("Data Source=" + file_name))
                {
                    conn.Open();
                    SQLiteCommand command = conn.CreateCommand();
                    command.CommandText = qry;                    
                    command.ExecuteNonQuery();
                }
            //}
            //catch (Exception ex)
            //{
            //    string sval = ex.Message;
            //}
        }

        //private List<List<string>> SelectQuery(string query)
        //{
        //    List<List<string>> retData = new List<List<string>>();
        //    string projectRootPath = _hostingEnvironment.ContentRootPath;
        //    string file_name = Path.Combine(projectRootPath, "app_files", "database", "lims.db");
        //    using (SQLiteConnection conn = new SQLiteConnection("Data Source=" + file_name))
        //    {
        //        conn.Open();
        //        conn.EnableExtensions(true);
        //        conn.LoadExtension("SQLite.Interop.dll", "sqlite3_json_init");
        //        SQLiteCommand command = conn.CreateCommand();
        //        command.CommandText = query;                
        //        var reader = command.ExecuteReader();
                
        //        while (reader.Read())
        //        {
        //            List<string> row = new List<string>();
        //            int id = reader.GetInt32(0);
        //            string name = reader.GetString(1);
        //            row.Add(id.ToString());
        //            row.Add(name);
        //            //string processor = reader.GetString(2);
        //            retData.Add(row);
        //        }
        //    }
        //    return retData;
        //}

        /// <summary>
        /// Get a list of all the processors
        /// </summary>
        /// <returns></returns>
        // GET: Processors
        [HttpGet]
        public DataResponseMessage Get()
        {
            DataResponseMessage rm = new DataResponseMessage();
            try
            {
                List<Processor> lstProcessors = GetListOfProcessors();
                SetProcessorsToNotFound();
                foreach (Processor proc in lstProcessors)
                {
                    InsertProcessorIntoDB(proc);
                }
                //rm.AddData("processors", JsonConvert.SerializeObject(lstProcessors));
                string jsonProcessors= JsonConvert.SerializeObject(lstProcessors);                
                var jaProcs = JArray.Parse(jsonProcessors);                
                rm.AddData("processors", jaProcs);
                rm.Message = "success";
                return rm;                
            }
            catch (Exception ex)
            {                                
                _logger.LogError("Error getting processors", ex.Message);
                rm.ErrorMessages.Add("Error retrieving list of processors");           
            }

            return rm;                        
        }

        
        

        /// <summary>
        /// Upload a zip file containing the binary dll that implements an IProcessor interface.
        /// It will be saved in a folder with the same name as the file
        /// The zipped dll should have the same name as the zip file
        /// It will be saved in the app_files/processors/{filename} folder
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<ResponseMessage> UploadProcessor(IFormFile file)
        {
            DataResponseMessage rm = new DataResponseMessage();
            if (file == null || file.FileName == "" || file.Length < 1)
            {
                rm.AddErrorMessage("Uploaded processor file is null or empty");
                return rm;
            }

            string tempPath = Path.GetTempFileName();
            string fileName = file.FileName;
            string projectRootPath = _hostingEnvironment.ContentRootPath;
            string fileNameNoExt = Path.GetFileNameWithoutExtension(fileName);
            string filePath = Path.Combine(projectRootPath, "app_files", "processors", fileNameNoExt);
            Directory.CreateDirectory(filePath);
                        
            try
            {

                //using (var stream = new FileStream(newPath, FileMode.Create))
                using (var stream = System.IO.File.Create(tempPath))
                {
                    await file.CopyToAsync(stream);
                }

                System.IO.Compression.ZipFile.ExtractToDirectory(tempPath, filePath);
                rm.Message = "Successfully uploaded file";
                //rm.Data.Add("file", fileName);

                
            }
            catch (Exception ex)
            {
                rm.AddErrorAndLogMessage(string.Format("Error uploading file: {0}", fileName));
            }
            //rm.status = "success";
            return rm;

        }

        // POST: api/Processors
        //[HttpPost]
        //public void Post([FromBody] string value)
        //{
        //}

        // PUT: api/Processors/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE: api/ApiWithActions/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
        

        private JObject RetrieveProcessor(string name)
        {
            JObject jo = null;
            
            return jo;

        }

        
    }
}
