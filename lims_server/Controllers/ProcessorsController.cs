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
using OfficeOpenXml;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Data.SQLite;
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
        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly ILogger<ProcessorsController> _logger;

        public ProcessorsController(IWebHostEnvironment hostingEnvironment, ILogger<ProcessorsController> logger)
        {
            _hostingEnvironment = hostingEnvironment;
            _logger = logger;
        }


        /// <summary>
        /// Loop over the folders in the processors folder.
        /// Each folder will contain a dll that has a class that 
        /// implmenets the IProcessor interface
        /// The dll will have the same name as the folder.
        /// </summary>
        /// <returns></returns>
        private List<ProcessorDTO> GetListOfProcessors()
        {
            List<ProcessorDTO> lstProcessors = new List<ProcessorDTO>();
            ProcessorManager procMgr = new ProcessorManager();
           
            try
            {                
                string projectRootPath = _hostingEnvironment.ContentRootPath;
                string processorPath = Path.Combine(projectRootPath, "app_files", "processors");
                lstProcessors = procMgr.GetProcessors(processorPath);
                
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, "Problem reading processors directory list.");
            }
            return lstProcessors;

        }
        private void InsertProcessorIntoDB(string name, string jsonProcessor)
        {
            string projectRootPath = _hostingEnvironment.ContentRootPath;
            string file_name = Path.Combine(projectRootPath, "app_files", "database", "lims.db");
            using (SQLiteConnection conn = new SQLiteConnection("Data Source=" + file_name))
            {
                conn.Open();
                conn.EnableExtensions(true);
                conn.LoadExtension("SQLite.Interop.dll", "sqlite3_json_init");
                SQLiteCommand command = conn.CreateCommand();
                command.CommandText = string.Format("insert into processors (name, processor) values ('{0}', json($text1))", name);
                command.Parameters.AddWithValue("$text1", jsonProcessor);
                command.ExecuteNonQuery();
            }
        }


        private List<List<string>> SelectQuery(string query)
        {
            List<List<string>> retData = new List<List<string>>();
            string projectRootPath = _hostingEnvironment.ContentRootPath;
            string file_name = Path.Combine(projectRootPath, "app_files", "database", "lims.db");
            using (SQLiteConnection conn = new SQLiteConnection("Data Source=" + file_name))
            {
                conn.Open();
                conn.EnableExtensions(true);
                conn.LoadExtension("SQLite.Interop.dll", "sqlite3_json_init");
                SQLiteCommand command = conn.CreateCommand();
                command.CommandText = query;                
                var reader = command.ExecuteReader();
                
                while (reader.Read())
                {
                    List<string> row = new List<string>();
                    int id = reader.GetInt32(0);
                    string name = reader.GetString(1);
                    row.Add(id.ToString());
                    row.Add(name);
                    //string processor = reader.GetString(2);
                    retData.Add(row);
                }
            }
            return retData;
        }

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
                List<ProcessorDTO> lstProcessors = GetListOfProcessors();
                rm.AddData("processors", JsonConvert.SerializeObject(lstProcessors));
                rm.Message = "success";
                return rm;                
            }
            catch (Exception ex)
            {                                
                _logger.LogError(ex.Message, "Error getting processors");
                rm.ErrorMessages.Add("Error retrieving list of processors");           
            }

            return rm;                        
        }

        
        // GET: api/Processors/Qubit2.0
        [HttpGet("{name}")]
        //public string Get(string name)
        public IActionResult Download(string name)
        {
            ResponseValue rm = new ResponseValue("success");
            JObject jo = RetrieveProcessor(name);

            var data = new Dictionary<string, string>()
            {
                {"processor", name }
            };
            rm.data = data;
            string projectRootPath = _hostingEnvironment.ContentRootPath;
            string filePath = Path.Combine(projectRootPath, "app_files", "processors", name);
            filePath += ".json";
            FileInfo fi = new FileInfo(filePath);
            if (!fi.Exists)
            {
                //rm.status = "failure";
                //rm.message = "Could not find processor";
                return Ok(rm.ToJObject());
            }

            string processor = System.IO.File.ReadAllText(filePath);
            JObject joReturn = rm.ToJObject();
            joReturn["data"] = JObject.Parse(processor);
            return Ok(joReturn);

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
                rm.AddErrorMessage("Upload processor file is null or empty");
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
                rm.Data.Add("file", fileName);

                
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
