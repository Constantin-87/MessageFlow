using System.ComponentModel.DataAnnotations;

namespace MessageFlow.Server.Models
{
    public class CompanyEmail
    {
        public int Id { get; set; }

        [Required, EmailAddress]
        public string EmailAddress { get; set; }

        public string Description { get; set; }

        public int CompanyId { get; set; }
        public Company Company { get; set; }
    }
}
