using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using BibliotecaAPI.Entities;

namespace BibliotecaAPI.Data
{
    public class ApplicationDbContext : IdentityDbContext<User>
    {
        public ApplicationDbContext(DbContextOptions options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Comment>().HasQueryFilter(b => !b.IsDeleted);

            modelBuilder.Entity<Rating>()
            .HasOne(r => r.Book)
            .WithMany(b => b.Ratings)
            .HasForeignKey(r => r.BookId);

            modelBuilder.Entity<Rating>()
                .HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId);
        }

        public DbSet<Author> Authors { get; set; }
        public DbSet<Book> Books { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<AuthorBook> AuthorsBooks { get; set; }
        public DbSet<Error> Errors { get; set; }
        public DbSet<Rating> Ratings { get; set; }
    }
}
