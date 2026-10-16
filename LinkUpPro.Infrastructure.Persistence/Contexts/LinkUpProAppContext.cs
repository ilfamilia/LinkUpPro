using LinkUpPro.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace LinkUpPro.Infrastructure.Persistence.Contexts
{
    public class LinkUpProAppContext : DbContext
    {
        public LinkUpProAppContext(DbContextOptions<LinkUpProAppContext> options) : base(options) { }
    
        public DbSet<Comment> Comments { get; set; }
        public DbSet<Friendship> Friendships { get; set; }
        public DbSet<FriendshipRequest> FriendshipRequests { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Post> Posts { get; set; }
        public DbSet<Reaction> Reactions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        }
    }
}
