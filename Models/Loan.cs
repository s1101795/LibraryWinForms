namespace LibraryWinForms.Models
{
    public class Loan
    {
        public int LoanId { get; set; }
        public int CopyId { get; set; }
        public int MemberId { get; set; }
        public DateTime LoanDate { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime? ReturnDate { get; set; }
        public decimal Fine { get; set; }
        public Copy Copy { get; set; } = null!;
        public Member Member { get; set; } = null!;
    }
}
