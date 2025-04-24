using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MessageFlow.Client.Models.DTOs
{
    public class PretrainDataFileDTO
    {
        public string Id { get; set; }

        [Required]
        public string FileName { get; set; }

        [Required]
        public string FileUrl { get; set; }

        public string FileDescription { get; set; }

        [NotMapped]
        public Stream FileContent { get; set; }

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        public string CompanyId { get; set; }
        public CompanyDTO Company { get; set; }
    }
}