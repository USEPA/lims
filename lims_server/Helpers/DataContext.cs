using Microsoft.EntityFrameworkCore;
using LimsServer.Entities;

namespace LimsServer.Helpers
{
    public class DataContext : DbContext
    {
        public DataContext(DbContextOptions<DataContext> options) : base(options) { }
        public DbSet<User> Users { get; set; }
        public DbSet<Task> Tasks { get; set; }
        public DbSet<Processor> Processors { get; set; }
        public DbSet<Workflow> Workflows { get; set; }
    }
}