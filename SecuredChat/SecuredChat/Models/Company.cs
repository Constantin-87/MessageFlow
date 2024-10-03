using System.ComponentModel.DataAnnotations;

namespace SecuredChat.Models
{
    public class Company
    {
        public int Id { get; set; }  // Primary key
        [Required(ErrorMessage = "Company Account Number is required.")]
        public string AccountNumber { get; set; }

        [Required(ErrorMessage = "Company Name is required.")]
        public string CompanyName { get; set; }

        public ICollection<Team> Teams { get; set; } = new List<Team>();
    }
}