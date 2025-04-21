using System.ComponentModel.DataAnnotations;

namespace MessageFlow.Shared.DTOs
{
    public class FacebookSettingsDTO
    {
        public string Id { get; set; }

        [Required(ErrorMessage = "Page ID is required.")]
        public string PageId { get; set; }

        [Required(ErrorMessage = "Access Token is required.")]
        public string AccessToken { get; set; }

        public string CompanyId { get; set; }
    }
}
