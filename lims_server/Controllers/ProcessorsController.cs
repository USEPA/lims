using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.Net.Http.Headers;
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Authorization;
using OfficeOpenXml;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Data.SQLite;
using LimsServer.Entities;
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

        public ProcessorsController(IWebHostEnvironment hostingEnvironment)
        {
            _hostingEnvironment = hostingEnvironment;
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
        // GET: api/Processors
        [HttpGet]
        public List<List<string>> Get()
        {
            ReturnMessage rm = new ReturnMessage("success");
            try
            {

                List<List<string>> data = SelectQuery("select id, name from processors");              
                             
                JObject jo = rm.ToJObject();
                JObject joData = new JObject();
                joData["processors"] = JToken.FromObject(data);
                jo["data"] = joData;
                return data;

            }
            catch (Exception ex)
            {
                rm.status = "failure";
                rm.message = ex.Message;
                return null;
            }            

            //MemoryStream ms = new MemoryStream();
            //using (FileStream file = new FileStream("Batch14_2019-03-14.xlsx", FileMode.Open, FileAccess.Read))
            //    file.CopyTo(ms);
            List<string> lst = new List<string>();
            FileInfo fi = new FileInfo("Batch14_2019-03-14.xlsx");
            using (var package = new ExcelPackage(fi))
            {
                var worksheet = package.Workbook.Worksheets[1]; // Tip: To access the first worksheet, try index 1, not 0
                string name = worksheet.Name;
                int numRows = worksheet.Dimension.End.Row;
                int numCols = worksheet.Dimension.End.Column;

                lst.Add(name);
                lst.Add("Hello World");
                //return lst.ToArray();
                //return Content(readExcelPackageToString(package, worksheet));
            }
            //return lst.ToArray();
        }

        
        // GET: api/Processors/Qubit2.0
        [HttpGet("{name}")]
        //public string Get(string name)
        public IActionResult Download(string name)
        {
            ReturnMessage rm = new ReturnMessage("success");
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
                rm.status = "failure";
                rm.message = "Could not find processor";
                return Ok(rm.ToJObject());
            }

            string processor = System.IO.File.ReadAllText(filePath);
            JObject joReturn = rm.ToJObject();
            joReturn["data"] = JObject.Parse(processor);
            return Ok(joReturn);

        }

        /// <summary>
        /// Upload a json based processor file
        /// It will be saved in the app_files/processors folder
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> UploadProcessor(IFormFile file)
        {
            string sProc = "";
            Dictionary<string, string> data = new Dictionary<string, string>()
            {
                { "processor", file.FileName}
            };

            string[] fields = Template.Fields.Clone() as string[];

            ReturnMessage rm = new ReturnMessage("failure", data);
            try
            {   
                JObject joProc = null;
                Processor proc = null;
                List<string> lstTemplateFields = new List<string>();
                if (file.Length > 0)
                {
                    //using (var stream = new FileStream(newPath, FileMode.Create))
                    using (var stream = new MemoryStream())
                    {
                        await file.CopyToAsync(stream);
                        stream.Position = 0;
                        
                        using (var reader = new StreamReader(stream))
                        {
                            sProc = reader.ReadToEnd();
                            
                            
                            joProc = JObject.Parse(sProc);
                            proc = JsonConvert.DeserializeObject<Processor>(joProc.ToString());

                            //string name = joProc["instrument"].ToString();
                            string name = proc.instrument;
                            JObject fldMappings = joProc["field_mappings"] as JObject;
                            foreach (var x in fldMappings)
                            {
                                string key = x.Key;
                                JToken val = x.Value;
                                string vProp = val.Value<string>();
                                lstTemplateFields.Add(key);
                            }
                        }
                    }
                }
                List<string> lstMissingFields = VerifyTemplateFields(lstTemplateFields);
                if (lstMissingFields != null)
                {
                    rm.status = "failure";
                    rm.message = "issing template fields: " + String.Join(",", lstMissingFields);                    
                    return Ok(rm.ToJObject());
                }
                else
                {
                    //string filePath = Path.Combine(projectRootPath, "app_files", "processors", file.FileName);

                    string sProcessor = JsonConvert.SerializeObject(proc);
                    InsertProcessorIntoDB(file.FileName, joProc.ToString(Formatting.None, null));
                    //System.IO.File.WriteAllText(filePath, joProc.ToString());
                }
            }
            catch (Exception ex)
            {
                rm.status = "Failure";
                rm.message = "Upload failed: " + ex.Message;
                return Ok(rm.ToJObject());
            }
            rm.status = "success";
            return Ok(rm.ToJObject());

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

        private List<string> VerifyTemplateFields(List<string> fields)
        {
            List<string> lstTemplateFields = Template.Fields.ToList();
            List<string> lstMissingFields = new List<string>();
            //string projPath = _hostingEnvironment.ContentRootPath;
            //string templateFile = Path.Combine(projPath, "app_files", "template");
            //string template = System.IO.File.ReadAllText(templateFile);
            //string[] template_fields = template.Split(",".ToCharArray());

            //List<string> lstTemplateFields = template_fields.ToList();
            foreach (string field in fields)
            {
                if (!lstTemplateFields.Contains(field, StringComparer.OrdinalIgnoreCase))
                    lstMissingFields.Add(field);
            }

            if (lstMissingFields.Count < 1)
                lstMissingFields = null;

            return lstMissingFields;
        }

        private JObject RetrieveProcessor(string name)
        {
            JObject jo = null;
            
            return jo;

        }

        
    }
}
