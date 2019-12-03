using System;
using System.Collections.Generic;
using System.Linq;
using LimsServer.Entities;
using LimsServer.Helpers;

namespace LimsServer.Services
{
    public interface ITaskService
    {
        IEnumerable<Task> GetAll();
        Task GetById(int id);
        Task Create(Task task);
        //void Update(User user, string password = null);
        void Update(Task task);
        void Delete(int id);
    }
    public class TaskService : ITaskService
    {
        private DataContext _context;
        public TaskService(DataContext context)
        {
            _context = context;
        }
        public Task Create(Task task)
        {
            throw new NotImplementedException();
        }

        public void Delete(int id)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Task> GetAll()
        {
            throw new NotImplementedException();
        }

        public Task GetById(int id)
        {
            throw new NotImplementedException();
        }

        public void Update(Task task)
        {
            throw new NotImplementedException();
        }
    }
}
