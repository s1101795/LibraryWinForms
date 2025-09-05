using Microsoft.EntityFrameworkCore;
using LibraryWinForms.Models;

namespace LibraryWinForms.Data
{
    public class LibraryContext : DbContext
    {
        public DbSet<Book> Books => Set<Book>();
        public DbSet<Author> Authors => Set<Author>();
        public DbSet<BookAuthor> BookAuthors => Set<BookAuthor>();
        public DbSet<Copy> Copies => Set<Copy>();
        public DbSet<Member> Members => Set<Member>();
        public DbSet<Loan> Loans => Set<Loan>();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // ⚠ 請依照你的環境調整連線字串
            optionsBuilder.UseSqlServer("Server=.\\SQLEXPRESS;Database=LibraryDb;Trusted_Connection=True;TrustServerCertificate=True");
        }

        protected override void OnModelCreating(ModelBuilder mb)
        {
            // 若你的資料表都在 dbo，先指定預設 schema
            mb.HasDefaultSchema("dbo");

            // ✅ 把實體對應到資料庫中的「單數」表名
            mb.Entity<Book>().ToTable("Book");
            mb.Entity<Author>().ToTable("Author");
            mb.Entity<BookAuthor>().ToTable("BookAuthor");
            mb.Entity<Copy>().ToTable("Copy");
            mb.Entity<Member>().ToTable("Member");
            mb.Entity<Loan>().ToTable("Loan");

            // 你的原有設定
            mb.Entity<Book>().HasIndex(b => b.ISBN).IsUnique();
            mb.Entity<Member>().HasIndex(m => m.Email).IsUnique();

            mb.Entity<BookAuthor>().HasKey(x => new { x.BookId, x.AuthorId });
            mb.Entity<BookAuthor>()
                .HasOne(x => x.Book).WithMany(x => x.BookAuthors).HasForeignKey(x => x.BookId);
            mb.Entity<BookAuthor>()
                .HasOne(x => x.Author).WithMany(x => x.BookAuthors).HasForeignKey(x => x.AuthorId);

            mb.Entity<Copy>().Property(c => c.Status).HasDefaultValue((byte)0);

            //（可省略，靠慣例也會生效）補上明確外鍵關聯：
            mb.Entity<Copy>()
                .HasOne(c => c.Book).WithMany(b => b.Copies).HasForeignKey(c => c.BookId);
            mb.Entity<Loan>()
                .HasOne(l => l.Copy).WithMany(c => c.Loans).HasForeignKey(l => l.CopyId);
            mb.Entity<Loan>()
                .HasOne(l => l.Member).WithMany(m => m.Loans).HasForeignKey(l => l.MemberId);
        }

    }
}
