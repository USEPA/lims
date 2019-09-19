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

namespace LimsServer.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ProcessorsController : ControllerBase
    {
        private readonly IHostingEnvironment _hostingEnvironment;

        public ProcessorsController(IHostingEnvironment hostingEnvironment)
        {
            _hostingEnvironment = hostingEnvironment;
        }

        // GET: api/Processors
        [HttpGet]
        public IActionResult Get()
        {
            string projectRootPath = _hostingEnvironment.ContentRootPath;
            DirectoryInfo di = new DirectoryInfo(Path.Combine(projectRootPath, "app_files", "processors"));
            FileInfo[] files= di.GetFiles();
            List<string> fileNames = new List<string>();
            foreach(var file in files)
            {
                fileNames.Add(Path.GetFileNameWithoutExtension(file.Name));
            }

            JObject jo = new JObject();
            jo["status"] = "Success";
            jo["processors"] = JToken.FromObject(fileNames);

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
        [HttpGet("{name}", Name = "Get")]
        public string Get(string name)
        {
            return name + " " + "hello";
        }

        //[HttpPost, DisableRequestSizeLimit]
        //public ActionResult UploadProcessor(IFormFile infile)
        [HttpPost]
        public async Task<IActionResult> UploadProcessor(IFormFile file)
        {
            try
            {
                string folderName = "temp";
                string webRootPath = _hostingEnvironment.WebRootPath;
                string projectRootPath = _hostingEnvironment.ContentRootPath;
                string newPath = Path.Combine(projectRootPath, "app_files", folderName, file.FileName);

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
                            var joProc = JObject.Parse(sProc);
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
                    JObject joMissingFields = new JObject();
                    joMissingFields["status"] = "Error. Upload failed. Missing template fields.";
                    joMissingFields["missing_fields"] = JToken.FromObject(lstMissingFields);
                    return Ok(joMissingFields);
                }
            }
            catch (Exception ex)
            {
                JObject joExcept = new JObject();
                joExcept["status"] = "Error. Upload failed. " + ex.Message;
                return Ok(joExcept);
            }
            JObject jo = new JObject();
            jo["status"] = "Successful upload";
            return Ok(jo);

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
    }
}
