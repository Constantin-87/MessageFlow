﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MessageFlow.Shared.DTOs
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
        public Stream FileContent { get; set; } // Holds the file content temporarily before upload


        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        public string CompanyId { get; set; }
        public CompanyDTO Company { get; set; }
    }
}