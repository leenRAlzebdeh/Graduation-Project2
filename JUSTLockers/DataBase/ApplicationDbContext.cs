using Microsoft.EntityFrameworkCore;
using JUSTLockers.Classes;

namespace JUSTLockers.DataBase
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions options) : base(options)
        {

        }
        public DbSet<Locker> Lockers { get; set; }

    }
}