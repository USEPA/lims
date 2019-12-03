using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LimsServer.Entities;
using LimsServer.Helpers;

namespace LimsServer.Services
{
    public interface IWorkflowService
    {
        IEnumerable<Workflow> GetAll();
        Workflow GetById(int id);
        Workflow Create(Workflow task);
        //void Update(User user, string password = null);
        void Update(Workflow task);
        void Delete(int id);
    }
    public class WorkflowService : IWorkflowService
    {
        private DataContext _context;
        public WorkflowService(DataContext context)
        {
            _context = context;
        }
        public Workflow Create(Workflow task)
        {
            throw new NotImplementedException();
        }

        public void Delete(int id)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Workflow> GetAll()
        {
            throw new NotImplementedException();
        }

        public Workflow GetById(int id)
        {
            throw new NotImplementedException();
        }

        public void Update(Workflow task)
        {
            throw new NotImplementedException();
        }
    }
}
