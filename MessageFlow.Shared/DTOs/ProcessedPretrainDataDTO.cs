﻿using MessageFlow.Shared.Enums;

namespace MessageFlow.Shared.DTOs
{
    public class ProcessedPretrainDataDTO
    {
        public string Id { get; set; }
        public string FileDescription { get; set; }
        public string FileUrl { get; set; } // Updated after upload
        public int CompanyId { get; set; }
        public FileType FileType { get; set; }
        public DateTime ProcessedAt { get; set; }
    }
}
