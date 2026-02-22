using Microsoft.EntityFrameworkCore;
using HomeLabManager.Core.Entities;


namespace HomeLabManager.API.Infrastructure
{
    public class ApplicationDBContext : DbContext
    {
        public ApplicationDBContext(DbContextOptions<ApplicationDBContext> options) : base(options)
        {

        }

        public DbSet<Device> Devices
        {
            get
            {
                return Set<Device>();
            }
        }

        public DbSet<Product> Products
        {
            get
            {
                return Set<Product>();
            }
        }

        public DbSet<Vendor> Vendors
        {
            get
            {
                return Set<Vendor>();
            }
        }



    }
}
