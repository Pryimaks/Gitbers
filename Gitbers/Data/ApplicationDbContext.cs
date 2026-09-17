using Gitbers.Models;
using Microsoft.EntityFrameworkCore;

namespace Gitbers.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users => Set<User>();
        public DbSet<Role> Roles => Set<Role>();
        public DbSet<Team> Teams => Set<Team>();
        public DbSet<TeamMember> TeamMembers => Set<TeamMember>();
        public DbSet<GitHubActivity> GitHubActivities => Set<GitHubActivity>();
        public DbSet<MetricSnapshot> MetricSnapshots => Set<MetricSnapshot>();
        public DbSet<Notification> Notifications => Set<Notification>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>()
                .HasOne(u => u.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TeamMember>()
                .HasOne(tm => tm.Team)
                .WithMany(t => t.Members)
                .HasForeignKey(tm => tm.TeamId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TeamMember>()
                .HasOne(tm => tm.User)
                .WithMany(u => u.TeamMembers)
                .HasForeignKey(tm => tm.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<GitHubActivity>()
                .HasOne(ga => ga.TeamMember)
                .WithMany()
                .HasForeignKey(ga => ga.TeamMemberId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MetricSnapshot>()
                .HasOne(ms => ms.Team)
                .WithMany(t => t.MetricSnapshots)
                .HasForeignKey(ms => ms.TeamId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Role>().HasData(
                new Role
                {
                    RoleId = 1,
                    Name = "Admin",
                    Description = "Адміністратор системи"
                },
                new Role
                {
                    RoleId = 2,
                    Name = "Manager",
                    Description = "Керівник команди"
                },
                new Role
                {
                    RoleId = 3,
                    Name = "Analyst",
                    Description = "Аналітик"
                },
                new Role
                {
                    RoleId = 4,
                    Name = "Developer",
                    Description = "Розробник"
                },
                new Role
                {
                    RoleId = 5,
                    Name = "Guest",
                    Description = "Гість системи"
                }
            );
        }
    }
}