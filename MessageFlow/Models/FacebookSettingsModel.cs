using System.ComponentModel.DataAnnotations;

namespace MessageFlow.Server.Models
{
    public class FacebookSettingsModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Page ID is required.")]
        public string PageId { get; set; }

        [Required(ErrorMessage = "Access Token is required.")]
        public string AccessToken { get; set; }

        public int CompanyId { get; set; } // Foreign key to link the settings to the company
    }
}
