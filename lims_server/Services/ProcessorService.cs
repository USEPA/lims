using System;
using System.Collections.Generic;
using System.Linq;
using LimsServer.Entities;
using LimsServer.Helpers;


namespace LimsServer.Services
{
    public interface IProcessorService
    {        
        IEnumerable<Processor> GetAll();
        Processor GetById(int id);
        Processor Create(Processor processor);
        //void Update(User user, string password = null);
        void Update(Processor processor, int processor_found);
        void Delete(int id);
    }

    public class ProcessorService : IProcessorService
    {
        private DataContext _context;

        public ProcessorService(DataContext context)
        {
            _context = context;
        }
        public Processor Create(Processor processor)
        {
            if (_context.Processors.Any(x => x.id == processor.id))
                throw new AppException($"Processor: {processor.name} with id: {processor.id} already exists.");

            _context.Processors.Add(processor);
            _context.SaveChanges();
            return processor;
        }

        public void Delete(int id)
        {
            var processor = _context.Processors.Find(id);
            if (processor != null)
            {
                _context.Processors.Remove(processor);
                _context.SaveChanges();
            }
        }

        public IEnumerable<Processor> GetAll()
        {
            return _context.Processors;
        }

        public Processor GetById(int id)
        {
            return _context.Processors.Find(id);
        }

        public void Update(Processor processorParam, int processor_found)
        {
            Processor processor = _context.Processors.Find(processorParam.id);

            if (processor == null)
                throw new AppException($"Processor: {processor.name} with id: {processor.id} not found in database.");

            processor.name = processorParam.name;
            processor.file_type = processorParam.file_type;
            processor.description = processorParam.description;
            processor.process_found = processorParam.process_found;
        }
    }
}
