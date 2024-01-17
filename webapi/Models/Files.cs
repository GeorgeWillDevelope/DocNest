using OfficeOpenXml.ConditionalFormatting.Contracts;
using System.ComponentModel.DataAnnotations.Schema;

namespace webapi.Models
{
    public class Files
    {
        public int Id { get; set; } 

        public string? FileName { get; set; }

        public DateTime DateOfUpload { get; set; }

        public string? OwnerId { get; set; }

        public string FileType { get; set; }  

        public string? ThumbnailFileName { get; set; }  

        public int NumberOfDownloads { get; set; }

        [NotMapped]
        public string ThumbnailUrl {  get; set; }

    }
}
