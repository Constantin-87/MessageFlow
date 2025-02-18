using System.ComponentModel.DataAnnotations;

namespace MessageFlow.Server.Models
{
    public class ProcessedPretrainData
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public int CompanyId { get; set; }  // Associated company
        public string FileDescription { get; set; }
        public FileType FileType { get; set; }
        public string FileUrl { get; set; }
        public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
    }

}
