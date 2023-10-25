using H5_Serverside_Crypting.Models;
using Microsoft.EntityFrameworkCore;

namespace H5_Serverside_Crypting.Services
{
    public class toDoContext : DbContext
    {
        public toDoContext(DbContextOptions<toDoContext> options) : base(options)
        {

        }
        public DbSet<toDoModel> toDo { get; set; }
    
    }
}
