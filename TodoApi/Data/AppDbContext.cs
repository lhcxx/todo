using Microsoft.EntityFrameworkCore;
using TodoApi.Models;

namespace TodoApi.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Todo> Todos { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Team> Teams { get; set; }
        public DbSet<TeamMember> TeamMembers { get; set; }
        public DbSet<Activity> Activities { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // User - Todo relationship
            modelBuilder.Entity<User>()
                .HasMany(u => u.Todos)
                .WithOne(t => t.User)
                .HasForeignKey(t => t.UserId);

            // User - OwnedTeams relationship
            modelBuilder.Entity<User>()
                .HasMany(u => u.OwnedTeams)
                .WithOne(t => t.Owner)
                .HasForeignKey(t => t.OwnerId);

            // Team - TeamMember relationship
            modelBuilder.Entity<Team>()
                .HasMany(t => t.Members)
                .WithOne(tm => tm.Team)
                .HasForeignKey(tm => tm.TeamId);

            // User - TeamMember relationship
            modelBuilder.Entity<User>()
                .HasMany(u => u.TeamMemberships)
                .WithOne(tm => tm.User)
                .HasForeignKey(tm => tm.UserId);

            // Team - SharedTodos relationship
            modelBuilder.Entity<Team>()
                .HasMany(t => t.SharedTodos)
                .WithOne(todo => todo.Team)
                .HasForeignKey(todo => todo.TeamId);

            // Activity relationships
            modelBuilder.Entity<Activity>()
                .HasOne(a => a.User)
                .WithMany(u => u.Activities)
                .HasForeignKey(a => a.UserId);

            modelBuilder.Entity<Activity>()
                .HasOne(a => a.Team)
                .WithMany()
                .HasForeignKey(a => a.TeamId);

            modelBuilder.Entity<Activity>()
                .HasOne(a => a.Todo)
                .WithMany()
                .HasForeignKey(a => a.TodoId);
        }
    }
} 