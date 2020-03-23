using LimsServer.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using System;
using System.Threading.Tasks;

namespace LimsServer.Controllers
{
    [Authorize]
    [Route("api/data")]
    [ApiController]
    public class DataController : Controller
    {

        private readonly DataContext _context;

        public DataController(DataContext context)
        {
            _context = context;
        }

        /// <summary>
        /// GET the input and output data for a specified task ID, if available
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpGet("task/{id}")]
        public async Task<IActionResult> GetTaskData(string id)
        {
            string fileName = id + "_backup.zip";
            DataBackup db = new DataBackup();
            try
            {
                byte[] dataBytes = db.GetTaskData(id, this._context);
                if (dataBytes == null)
                {
                    return View();
                }
                else if (dataBytes.Length == 0)
                {
                    return View();
                }
                var mimeType = "application/....";
                return new FileContentResult(dataBytes, mimeType)
                {
                    FileDownloadName = fileName
                };
            }
            catch(Exception ex)
            {
                Log.Warning(ex, "Error downloading task data.");
                return View();
            }
        }

    }
}