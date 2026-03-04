using Microsoft.EntityFrameworkCore;
using HomeLabManager.Core.Entities;


namespace HomeLabManager.API.Infrastructure
{
    public class ApplicationDBContext : DbContext
    {
        public ApplicationDBContext(DbContextOptions<ApplicationDBContext> options) : base(options){ }

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
        public DbSet<Component> Components
        {
            get
            {
                return Set<Component>();
            }
        }
        public DbSet<DeviceComponent> DeviceComponents
        {
            get
            {
                return Set<DeviceComponent>();
            }
        }

        //documentation for ModelBuilder how the application should be modeled in the database
        //property is used to configure the model and its relationships 
        // https://learn.microsoft.com/en-us/ef/core/modeling/
        //https://learn.microsoft.com/en-us/ef/core/modeling/entity-properties?tabs=fluent-api%2Cwith-nrt
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            //this section is for device entity configuration

            //configure the maximum length of the SerialNumber property to 100 characters to avoid potential issues with long serial numbers in the database
            modelBuilder.Entity<Device>().Property(device => device.SerialNumber).HasMaxLength(100);

            //configure nickname property to limit length
            modelBuilder.Entity<Device>().Property(device => device.NickName).HasMaxLength(100);

            //configure location property to limit length
            modelBuilder.Entity<Device>().Property(device => device.Location).HasMaxLength(100);

            //configure unique index on SerialNumber
            modelBuilder.Entity<Device>().HasIndex(device => device.SerialNumber).IsUnique();

            //configure the relationship between device and product
            modelBuilder.Entity<Device>()
            .HasOne(device => device.Product)//one to one relationship between device and product
            .WithMany() // one to many relationship with product to devices
            .HasForeignKey(device => device.ProductId)//this foreignKey is in device table and references the product table
            .OnDelete(DeleteBehavior.Restrict);//prevent deletion of a product if connected to a device
        
            //this section is for product entity configuration
            //configure the max length for model number
            modelBuilder.Entity<Product>().Property(product => product.ModelNumber).HasMaxLength(100);
            
            //configure the max length for product name
            modelBuilder.Entity<Product>().Property(product => product.ProductName).HasMaxLength(100);
            
            //configure the max length for CPU name
            modelBuilder.Entity<Product>().Property(product => product.CPUName).HasMaxLength(100);
            
            //configure the max length for storage for device
            modelBuilder.Entity<Product>().Property(product => product.StorageForDevice).HasMaxLength(100);

            //configure the relationship between product and vendor
            modelBuilder.Entity<Product>()
            .HasOne(product => product.Vendor)//one to one relationship between product and vendor
            .WithMany() // one to many relationship with vendor to products
            .HasForeignKey(product => product.VendorId)//this foreignKey is in product table and references the vendor table
            .OnDelete(DeleteBehavior.Restrict);//prevent deletion of a vendor if connected to a product
        
            //this section is for vendor entity configuration
            
            //configure the max length for vendor name
            modelBuilder.Entity<Vendor>().Property(vendor => vendor.VendorName).HasMaxLength(100);

            //max length for url to prevent issues with long urls in the database
            modelBuilder.Entity<Vendor>().Property(vendor => vendor.VendorBaseUrl).HasMaxLength(300);

            //this section is for component entity configuration
            //configure the max length for component name
            modelBuilder.Entity<Component>().Property(component => component.Name).HasMaxLength(100);

            //configure the max length for component type
            modelBuilder.Entity<Component>().Property(component => component.ComponentType).HasMaxLength(100);

            //configure the max length for manufacturer
            modelBuilder.Entity<Component>().Property(component => component.Manufacturer).HasMaxLength(100);

            //configure the max length for model number
            modelBuilder.Entity<Component>().Property(component => component.ModelNumber).HasMaxLength(100);

            //configure the max length for specifications
            modelBuilder.Entity<Component>().Property(component => component.Specifications).HasMaxLength(500);

            //configure the relationship between component and vendor
            modelBuilder.Entity<Component>()
                .HasOne<Vendor>()       //one to one relationship between component and vendor
                .WithMany()             //vendor can have many components, but component only has one vendor
                .HasForeignKey(component => component.VendorId) //this foreignKey is in component table and references the vendor table
                .OnDelete(DeleteBehavior.SetNull); //allow deletion of a vendor if connected to a component, but set the foreign key to null to avoid orphaned records

            //this section is for device component entity configuration

            //configure the max length for serial number
            modelBuilder.Entity<DeviceComponent>().Property(dc => dc.SerialNumber).HasMaxLength(100);

            //configure the max length for notes
            modelBuilder.Entity<DeviceComponent>().Property(dc => dc.Notes).HasMaxLength(500);

            //configure the relationship between device-component and device
            modelBuilder.Entity<DeviceComponent>()
                .HasOne<Device>()       // DeviceComponent references one Device
                .WithMany()             // Device can have many DeviceComponents
                .HasForeignKey(dc => dc.DeviceId)
                .OnDelete(DeleteBehavior.Cascade);  // If device deleted, delete all its component records

            //create composite index for fast lookups of device components by device and component
            modelBuilder.Entity<DeviceComponent>()
                .HasIndex(dc => new { dc.DeviceId, dc.ComponentId });

        }


    }
}
