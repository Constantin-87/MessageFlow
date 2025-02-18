using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MessageFlow.Server.Models
{
    public class Company
    {
        public int Id { get; set; }  // Primary key

        [Required(ErrorMessage = "Company Account Number is required.")]
        public string AccountNumber { get; set; }

        [Required(ErrorMessage = "Company Name is required.")]
        public string CompanyName { get; set; }

        [Required(ErrorMessage = "Company Description is required.")]
        public string Description { get; set; }

        public string IndustryType { get; set; }
        public string WebsiteUrl { get; set; }

        // Customer Support - Multiple Emails & Phone Numbers
        public ICollection<CompanyEmail> CompanyEmails { get; set; } = new List<CompanyEmail>();
        public ICollection<CompanyPhoneNumber> CompanyPhoneNumbers { get; set; } = new List<CompanyPhoneNumber>();

        // Stores file URLs and metadata for AI pretraining
        public ICollection<PretrainDataFile> PretrainDataFiles { get; set; } = new List<PretrainDataFile>();

        public ICollection<Team> Teams { get; set; } = new List<Team>();

        // Property to hold the total users count dynamically
        [NotMapped] // Property is not persisted in the database
        public int TotalUsers { get; set; }
    }
}