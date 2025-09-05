namespace LibraryWinForms.Models
{
    public class Author
    {
        public int AuthorId { get; set; }
        public string Name { get; set; } = "";
        public DateTime? BirthDate { get; set; }
        public string? Country { get; set; }
        public ICollection<BookAuthor> BookAuthors { get; set; } = new List<BookAuthor>();
    }
}
