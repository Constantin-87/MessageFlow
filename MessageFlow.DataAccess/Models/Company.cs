using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MessageFlow.DataAccess.Models
{
    public class Company
    {
        public string Id { get; set; }

        [Required(ErrorMessage = "Company Account Number is required.")]
        public string AccountNumber { get; set; }

        [Required(ErrorMessage = "Company Name is required.")]
        public string CompanyName { get; set; }

        [Required(ErrorMessage = "Company Description is required.")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Industry Type is required.")]
        public string IndustryType { get; set; }

        [Required(ErrorMessage = "Website is required.")]
        public string WebsiteUrl { get; set; }

        public ICollection<ApplicationUser>? Users { get; set; } = new List<ApplicationUser>();
        public ICollection<CompanyEmail>? CompanyEmails { get; set; } = new List<CompanyEmail>();
        public ICollection<CompanyPhoneNumber>? CompanyPhoneNumbers { get; set; } = new List<CompanyPhoneNumber>();
        public ICollection<Team>? Teams { get; set; } = new List<Team>();

        // Not stored property to hold the total users count
        [NotMapped]
        public int TotalUsers { get; set; }
    }
}