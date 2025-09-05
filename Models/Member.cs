namespace LibraryWinForms.Models
{
    public class Member
    {
        public int MemberId { get; set; }
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
        public string? Phone { get; set; }
        public DateTime JoinDate { get; set; }
        public ICollection<Loan> Loans { get; set; } = new List<Loan>();
    }
}
