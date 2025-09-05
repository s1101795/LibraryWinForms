namespace LibraryWinForms.Models
{
    public class Copy
    {
        public int CopyId { get; set; }
        public int BookId { get; set; }
        public string Barcode { get; set; } = "";
        public byte Status { get; set; } // 0可借,1外借中,2遺失,3毀損
        public string? Location { get; set; }
        public DateTime CreatedAt { get; set; }
        public Book Book { get; set; } = null!;
        public ICollection<Loan> Loans { get; set; } = new List<Loan>();
    }
}
