using MessageFlow.Shared.Enums;
using System.ComponentModel.DataAnnotations;

namespace MessageFlow.DataAccess.Models
{
    public class ProcessedPretrainData
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string CompanyId { get; set; }
        public string FileDescription { get; set; }
        public FileType FileType { get; set; }
        public string FileUrl { get; set; }
        public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
    }
}
