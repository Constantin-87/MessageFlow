using MessageFlow.Client.Models.Enums;

namespace MessageFlow.Client.Models.DTOs
{
    public class ProcessedPretrainDataDTO
    {
        public string Id { get; set; }
        public string FileDescription { get; set; }
        public string FileUrl { get; set; }
        public string CompanyId { get; set; }
        public FileType FileType { get; set; }
        public DateTime ProcessedAt { get; set; }
    }
}