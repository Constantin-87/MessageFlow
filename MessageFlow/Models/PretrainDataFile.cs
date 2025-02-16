using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MessageFlow.Models
{
    public class PretrainDataFile
    {
        public int Id { get; set; }

        [Required]
        public string FileName { get; set; } // Original file name

        [Required]
        public string FileUrl { get; set; } // URL to blob storage

        public string FileDescription { get; set; }

        //[Required]
        //public FileType FileType { get; set; }

        [NotMapped]
        public Stream FileContent { get; set; } // Holds the file content temporarily before upload


        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        public int CompanyId { get; set; }
        public Company Company { get; set; }
    }
}
