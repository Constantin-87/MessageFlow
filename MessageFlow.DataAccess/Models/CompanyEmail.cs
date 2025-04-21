using System.ComponentModel.DataAnnotations;

namespace MessageFlow.DataAccess.Models
{
    public class CompanyEmail
    {
        public string Id { get; set; }

        [Required, EmailAddress]
        public string EmailAddress { get; set; }

        public string Description { get; set; }

        public string CompanyId { get; set; }
    }
}