using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LimsServer.Entities;
using LimsServer.Helpers;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace LimsServer.Services
{
    public interface IProcessorService
    {
        System.Threading.Tasks.Task<IEnumerable<Processor>> GetAll();
        Task<Processor> GetById(string id);
        Task<Processor> GetByName(string name);
        Task<Processor> Create(Processor processor);
        System.Threading.Tasks.Task Update(string id, Processor processor);
        System.Threading.Tasks.Task Update(Processor[] processor);
    }

    public class ProcessorService : IProcessorService
    {
        private DataContext _context;

        public ProcessorService(DataContext context)
        {
            _context = context;
        }
        /// <summary>
        /// Create a new Processor and add to db context.
        /// </summary>
        /// <param name="processor">New processor to add</param>
        /// <returns>The added processor, as seen from the db context, or an empty processor with an error message.</returns>
        public async Task<Processor> Create(Processor processor)
        {

            //processor.id = workflowID;
            try
            {
                var result = await _context.Processors.AddAsync(processor);
                await _context.SaveChangesAsync();

                return result.Entity;
            }
            catch (Exception ex)
            {
                var result = new Processor();
                //result.message = ex.Message;
                return result;
            }
        }

        /// <summary>
        /// Query all processors from current db context
        /// </summary>
        /// <returns>List of processors</returns>
        public async System.Threading.Tasks.Task<IEnumerable<Processor>> GetAll()
        {
            var processors = await _context.Processors.ToListAsync();
            return processors as List<Processor>;
        }

        /// <summary>
        /// Query processors for specified id.
        /// </summary>
        /// <param name="name">processor name</param>
        /// <returns>the processor with the specified name</returns>
        public async Task<Processor> GetByName(string name)
        {
            try
            {
                var processor = await _context.Processors.SingleAsync(p => p.name == name);
                return processor as Processor;
            }
            catch (InvalidOperationException)
            {
                Serilog.Log.Information("No processor found with name: {0}", name);
                return null;
            }
        }

        /// <summary>
        /// Query processors for specified id.
        /// </summary>
        /// <param name="id">processor ID</param>
        /// <returns>the processor with the specified ID</returns>
        public async Task<Processor> GetById(string id)
        {
            try
            {
                var processor = await _context.Processors.SingleAsync(p => p.id == id);
                return processor as Processor;
            }
            catch (InvalidOperationException)
            {
                Serilog.Log.Information("No processor found with ID: {0}", id);
                return null;
            }
        }

        /// <summary>
        /// Updates the processor provided by id
        /// </summary>
        /// <param name="processor"></param>
        public async System.Threading.Tasks.Task Update(string id, Processor processor)
        {
            try
            {
                var p = await this._context.Processors.SingleAsync(p0 => p0.id == id);
                p.name = processor.name;
                p.version = processor.version;
                p.file_type = processor.file_type;
                p.description = processor.description;
                p.enabled = processor.enabled;
                p.process_found = processor.process_found;
                await _context.SaveChangesAsync();
            }
            catch (InvalidOperationException)
            {
                Serilog.Log.Information("Processor not updated, no processor found with ID: {0}", id);
            }
        }

        /// <summary>
        /// Updates the processor provided by id
        /// </summary>
        /// <param name="processor"></param>
        public async System.Threading.Tasks.Task Update(Processor[] processors)
        {
            _context.Processors.UpdateRange(processors);
            await _context.SaveChangesAsync();
        }
    }
}
