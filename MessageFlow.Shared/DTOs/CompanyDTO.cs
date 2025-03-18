using System.ComponentModel.DataAnnotations;

namespace MessageFlow.Shared.DTOs
{
    public class CompanyDTO
    {
        public string Id { get; set; }  // Primary key

        [Required(ErrorMessage = "Company Account Number is required.")]
        public string AccountNumber { get; set; }

        [Required(ErrorMessage = "Company Name is required.")]
        public string CompanyName { get; set; }

        [Required(ErrorMessage = "Company Description is required.")]
        public string Description { get; set; }

        public string IndustryType { get; set; }
        public string WebsiteUrl { get; set; }

        // Customer Support - Multiple Emails & Phone Numbers
        public ICollection<CompanyEmailDTO> CompanyEmails { get; set; } = new List<CompanyEmailDTO>();
        public ICollection<CompanyPhoneNumberDTO> CompanyPhoneNumbers { get; set; } = new List<CompanyPhoneNumberDTO>();

        // Stores file URLs and metadata for AI pretraining
        public ICollection<PretrainDataFileDTO> PretrainDataFilesDTO { get; set; } = new List<PretrainDataFileDTO>();

        public ICollection<TeamDTO> Teams { get; set; } = new List<TeamDTO>();

        // Property to hold the total users count dynamically
        public int TotalUsers { get; set; }
    }
}