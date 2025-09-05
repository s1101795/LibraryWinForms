namespace LibraryWinForms.Models
{
    public class Book
    {
        public int BookId { get; set; }
        public string ISBN { get; set; } = "";
        public string Title { get; set; } = "";
        public string? Category { get; set; }
        public string? Publisher { get; set; }
        public int? PublishYear { get; set; }
        public DateTime CreatedAt { get; set; }
        public ICollection<Copy> Copies { get; set; } = new List<Copy>();
        public ICollection<BookAuthor> BookAuthors { get; set; } = new List<BookAuthor>();
    }
}
