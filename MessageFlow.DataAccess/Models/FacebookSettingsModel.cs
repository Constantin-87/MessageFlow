using System.ComponentModel.DataAnnotations;

namespace MessageFlow.DataAccess.Models
{
    public class FacebookSettingsModel
    {
        public required string Id { get; set; }

        [Required(ErrorMessage = "Page ID is required.")]
        public string PageId { get; set; }

        [Required(ErrorMessage = "Access Token is required.")]
        public string AccessToken { get; set; }

        [Required(ErrorMessage = "CompanyId is required.")]
        public string CompanyId { get; set; }
    }
}