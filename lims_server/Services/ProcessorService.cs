using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LimsServer.Entities;
using LimsServer.Helpers;
using Microsoft.EntityFrameworkCore;


namespace LimsServer.Services
{
    public interface IProcessorService
    {
        Task<IEnumerable<Processor>> GetAll();
        Task<Processor> GetById(string id);
        Task<Processor> Create(Processor processor);
        void Update(string id, Processor processor);
        //void Delete(string id);


        //IEnumerable<Processor> GetAll();
        //Processor GetById(int id);
        //Processor Create(Processor processor);
        ////void Update(User user, string password = null);
        //void Update(Processor processor, int processor_found);
        //void Delete(int id);
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
        /// <param name="id">processor ID</param>
        /// <returns>the processor with the specified ID</returns>
        public async Task<Processor> GetById(string id)
        {
            var processors = await _context.Processors.SingleAsync(w => w.id == id);
            return processors as Processor;
        }

        /// <summary>
        /// Updates the processor provided by id
        /// </summary>
        /// <param name="processor"></param>
        public async void Update(string id, Processor processor)
        {            
            var oldProc = await _context.Processors.SingleAsync(w => w.id == id);
            oldProc = processor;
            await _context.SaveChangesAsync();
        }


        //public void Delete(int id)
        //{
        //    var processor = _context.Processors.Find(id);
        //    if (processor != null)
        //    {
        //        _context.Processors.Remove(processor);
        //        _context.SaveChanges();
        //    }
        //}
        

        //public Processor GetById(int id)
        //{
        //    return _context.Processors.Find(id);
        //}



        //public void Update(Processor processorParam, int processor_found)
        //{
        //    Processor processor = _context.Processors.Find(processorParam.id);

        //    if (processor == null)
        //        throw new AppException($"Processor: {processor.name} with id: {processor.id} not found in database.");

        //    processor.name = processorParam.name;
        //    processor.file_type = processorParam.file_type;
        //    processor.description = processorParam.description;
        //    processor.process_found = processorParam.process_found;
        //}
    }
}
