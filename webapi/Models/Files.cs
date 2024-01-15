namespace webapi.Models
{
    public class Files
    {
        public int Id { get; set; } 

        public string? FileName { get; set; }

        public DateTime DateOfUpload { get; set; }

        public string? OwnerId { get; set; }

        public FileType FileType { get; set; }  

        public string? ThumbnailFileName { get; set; }  

        public int NumberOfDownloads { get; set; }

    }
}
