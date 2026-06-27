using Microsoft.EntityFrameworkCore;

namespace UserRolePortal.Models
{
    // C# to PostGRE connection
    public class ApponDbContext : DbContext
    {
        // constructor 
        public ApponDbContext(DbContextOptions<ApponDbContext> options)
            : base(options)
        {
        }

        // DbSets - represent tables in database

        // Roles table
        public DbSet<Role> Roles { get; set; }

        // Users table
        public DbSet<User> Users { get; set; }

        // Documents table
        public DbSet<Document> Documents { get; set; }

        // UserStatusHistories table
        public DbSet<UserStatusHistory> UserStatusHistories { get; set; }

        // OnModelCreating - runs when database model is created
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure User-Role relationship
            // "One Role can have many Users"
            modelBuilder.Entity<User>()
                .HasOne(u => u.Role)
                .WithMany()
                .HasForeignKey(u => u.RoleId);

            // Seed initial roles
            modelBuilder.Entity<Role>().HasData(
                new Role { RoleId = 1, RoleName = "Admin" },
                new Role { RoleId = 2, RoleName = "User" }
            );
        }
    }
}