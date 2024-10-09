using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MessageFlow.Models
{
    public class Company
    {
        public int Id { get; set; }  // Primary key
        [Required(ErrorMessage = "Company Account Number is required.")]
        public string AccountNumber { get; set; }

        [Required(ErrorMessage = "Company Name is required.")]
        public string CompanyName { get; set; }

        public ICollection<Team> Teams { get; set; } = new List<Team>();

        // Add a property to hold the total users count dynamically
        [NotMapped] // This ensures the property is not persisted in the database
        public int TotalUsers { get; set; }
    }
}