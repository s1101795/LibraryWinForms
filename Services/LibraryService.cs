using LibraryWinForms.Data;
using LibraryWinForms.Models;
using Microsoft.EntityFrameworkCore;

namespace LibraryWinForms.Services
{
    public class LibraryService
    {
        public async Task<List<Book>> GetBooksAsync(string? keyword = null)
        {
            using var db = new LibraryContext();
            var q = db.Books
                .Include(b => b.BookAuthors)
                    .ThenInclude(ba => ba.Author)
                .AsQueryable();
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                q = q.Where(b => b.Title.Contains(keyword) || b.ISBN.Contains(keyword));
            }
            return await q.OrderBy(b => b.Title).Take(200).ToListAsync();
        }

        public async Task<int> AddBookAsync(string isbn, string title, string? publisher, int? year)
        {
            using var db = new LibraryContext();
            var book = new Book { ISBN = isbn, Title = title, Publisher = publisher, PublishYear = year, CreatedAt = DateTime.UtcNow };
            db.Books.Add(book);
            await db.SaveChangesAsync();
            return book.BookId;
        }

        public async Task<int> AddCopyAsync(int bookId, string barcode, string? location)
        {
            using var db = new LibraryContext();
            var copy = new Copy { BookId = bookId, Barcode = barcode, Location = location, Status = 0, CreatedAt = DateTime.UtcNow };
            db.Copies.Add(copy);
            await db.SaveChangesAsync();
            return copy.CopyId;
        }

        public async Task<int> AddMemberAsync(string name, string email, string? phone)
        {
            using var db = new LibraryContext();
            var m = new Member { Name = name, Email = email, Phone = phone, JoinDate = DateTime.UtcNow.Date };
            db.Members.Add(m);
            await db.SaveChangesAsync();
            return m.MemberId;
        }

        public async Task<List<Member>> GetMembersAsync(string? keyword = null)
        {
            using var db = new LibraryContext();
            var q = db.Members
                .Include(m => m.Loans)
                    .ThenInclude(l => l.Copy)
                .AsQueryable();
            if (!string.IsNullOrWhiteSpace(keyword))
                q = q.Where(m => m.Name.Contains(keyword) || m.Email.Contains(keyword));
            return await q.OrderBy(m => m.Name).Take(200).ToListAsync();
        }

        public async Task BorrowAsync(int copyId, int memberId, int loanDays = 14)
        {
            using var db = new LibraryContext();
            await db.Database.ExecuteSqlRawAsync("EXEC dbo.sp_BorrowCopy @p0, @p1, @p2", copyId, memberId, loanDays);
        }

        public async Task ReturnAsync(int loanId, decimal finePerDay = 10m)
        {
            using var db = new LibraryContext();
            await db.Database.ExecuteSqlRawAsync("EXEC dbo.sp_ReturnCopy @p0, @p1", loanId, finePerDay);
        }

        public async Task<List<Loan>> GetOpenLoansByMemberAsync(int memberId)
        {
            using var db = new LibraryContext();
            return await db.Loans
                .Include(l => l.Copy).ThenInclude(c => c.Book)
                .Where(l => l.MemberId == memberId && l.ReturnDate == null)
                .OrderBy(l => l.DueDate)
                .ToListAsync();
        }

        public async Task<List<Copy>> GetCopiesByBookAsync(int bookId)
        {
            using var db = new LibraryContext();
            return await db.Copies
                .Include(c => c.Loans)
                    .ThenInclude(l => l.Member)
                .Where(c => c.BookId == bookId)
                .OrderBy(c => c.Barcode)
                .ToListAsync();
        }

        public async Task BorrowByBookAsync(int bookId, int memberId, int loanDays = 14)
        {
            // 取得可借館藏
            var copies = await GetCopiesByBookAsync(bookId);
            var available = copies.FirstOrDefault(c => c.Status == 0);
            if (available == null)
                throw new Exception("本館已無此書館藏可借出。");
            await BorrowAsync(available.CopyId, memberId, loanDays);
        }

        public async Task ReturnByBookAsync(int bookId, int memberId, decimal finePerDay = 10m)
        {
            // 找出尚未歸還的 Loan
            var copies = await GetCopiesByBookAsync(bookId);
            var copyIds = copies.Select(c => c.CopyId).ToList();
            var openLoans = new List<Loan>();
            var loans = await GetOpenLoansByMemberAsync(memberId);
            openLoans.AddRange(loans.Where(l => copyIds.Contains(l.CopyId)));
            var loan = openLoans.FirstOrDefault();
            if (loan == null)
                throw new Exception("查無此會員尚未歸還的此書借閱紀錄。");
            await ReturnAsync(loan.LoanId, finePerDay);
        }
    }
}
