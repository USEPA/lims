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
using LimsServer.Entities;
using LiteDB;

namespace LimsServer.Controllers
{
    [Authorize]
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

        /// <summary>
        /// Get a list of all the processors
        /// </summary>
        /// <returns></returns>
        // GET: api/Processors
        [HttpGet]
        public IActionResult Get()
        {
            string projectRootPath = _hostingEnvironment.ContentRootPath;
            DirectoryInfo di = new DirectoryInfo(Path.Combine(projectRootPath, "app_files", "processors"));
            FileInfo[] files = di.GetFiles();
            List<string> fileNames = new List<string>();
            foreach (var file in files)
            {
                fileNames.Add(Path.GetFileNameWithoutExtension(file.Name));
            }

            ReturnMessage rm = new ReturnMessage("success");
            JObject jo = rm.ToJObject();
            JObject joData = new JObject();
            joData["processors"] = JToken.FromObject(fileNames);
            jo["data"] = joData;
            return Ok(jo);

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
            Dictionary<string, string> data = new Dictionary<string, string>()
            {
                { "processor", file.FileName}
            };

            ReturnMessage rm = new ReturnMessage("failure", data);
            try
            {
                string folderName = "temp";
                string webRootPath = _hostingEnvironment.WebRootPath;
                string projectRootPath = _hostingEnvironment.ContentRootPath;
                string newPath = Path.Combine(projectRootPath, "app_files", folderName, file.FileName);

                JObject joProc = null;
                List<string> lstTemplateFields = new List<string>();
                if (file.Length > 0)
                {
                    //using (var stream = new FileStream(newPath, FileMode.Create))
                    using (var stream = new MemoryStream())
                    {
                        await file.CopyToAsync(stream);
                        stream.Position = 0;
                        string sProc;
                        using (var reader = new StreamReader(stream))
                        {
                            sProc = reader.ReadToEnd();
                            joProc = JObject.Parse(sProc);
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
                    rm.status = "Failure";
                    rm.message = "Missing template fileds: " + String.Join(",", lstMissingFields);
                    //JObject joMissingFields = new JObject();
                    //joMissingFields["status"] = "Error. Upload failed. Missing template fields.";
                    //joMissingFields["missing_fields"] = JToken.FromObject(lstMissingFields);
                    return Ok(rm.ToJObject());
                }
                else
                {
                    string filePath = Path.Combine(projectRootPath, "app_files", "processors", file.FileName);
                    System.IO.File.WriteAllText(filePath, joProc.ToString());
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
            List<string> lstMissingFields = new List<string>();
            string projPath = _hostingEnvironment.ContentRootPath;
            string templateFile = Path.Combine(projPath, "app_files", "template");
            string template = System.IO.File.ReadAllText(templateFile);
            string[] template_fields = template.Split(",".ToCharArray());

            List<string> lstTemplateFields = template_fields.ToList();
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
            using (var db = new LiteDatabase(@"lims_objects.db"))
            {
                // Get user collection
                var processors = db.GetCollection<Processor>("processors");
                Processor processor = processors.Find(x => x.name == name).FirstOrDefault();
                jo = (JObject)JToken.FromObject(processor);
            }
            return jo;

        }

        public class ReturnMessage
        {
            public string status;
            public string message;
            public Dictionary<string, string> data;

            public ReturnMessage(string _status, Dictionary<string, string> _data = null, string _message = "")
            {
                status = _status;
                data = _data;
                message = _message;
            }
            public JObject ToJObject()
            {
                JObject jo = JToken.FromObject(this) as JObject;
                return jo;
            }
        }
    }
}
