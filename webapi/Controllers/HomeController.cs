using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Drawing;
using Syncfusion.XlsIO;
using Aspose.Pdf;
using Aspose.Pdf.Devices;
using webapi.Service;
using webapi.Data;
using DocumentFormat.OpenXml.Presentation;
using webapi.Models;
using System.IO;
using System.Net.Http;

namespace webapi.Controllers
{
    [AllowAnonymous]
    [Route("api/[controller]")]
    [ApiController]
    public class HomeController : ControllerBase
    {
        private readonly DocNestDbContext _context;
        private readonly ICloudStorageService _cloudStorageService;
        private readonly HttpClient _httpClient;

        public HomeController(DocNestDbContext context, ICloudStorageService cloudStorageService, HttpClient httpClient)
        {
            _context = context;
            _cloudStorageService = cloudStorageService;
            _httpClient = httpClient;
        }
        
        [HttpPost("UploadFiles")]
        public async Task<IActionResult> UploadFiles(List<IFormFile> files)
        {
            if (files == null || files.Count == 0)
                return BadRequest("No files selected");

            foreach (var file in files)
            {
                Files fileInfo = new Files
                {
                    FileName = GenerateFileNameToSave(file.FileName, false),
                    DateOfUpload = DateTime.UtcNow,
                    FileType = file.ContentType,
                    ThumbnailFileName = GenerateFileNameToSave(file.FileName, true)
                };

                await _cloudStorageService.UploadFileAsync(file, fileInfo.FileName);

                // Generate thumbnail and save
                var bitmap = GenerateThumbnail(fileInfo.FileName, file.OpenReadStream());

                SaveThumbnail(bitmap, fileInfo.ThumbnailFileName);

                _context.Add(fileInfo);
            }

            await _context.SaveChangesAsync();

            return Ok("Files uploaded successfully");
        }

        [HttpGet("ListAll")]
        public async Task<IActionResult> ListAll()
        {
            var files = _context.Files.ToList();  

            var thumbnailList = files.Select(async file =>
            {
                // Populate the ThumbnailUrl property with corresponding IFormFile
                file.ThumbnailUrl = await _cloudStorageService.GetSignedUrlAsync(file.ThumbnailFileName);

                return new
                {
                    file.Id,
                    file.FileName,
                    file.DateOfUpload,
                    file.FileType,
                    file.NumberOfDownloads,
                    file.ThumbnailUrl
                };
            });

            return new JsonResult(thumbnailList);
        }

        [HttpGet("DownloadFile/{id}")]
        public async Task<IActionResult> DownloadFile(int id)
        {
            try
            {
                using (var transaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        var file = await _context.Files.FindAsync(id);

                        if (file == null)
                        {
                            transaction.Rollback();
                            return NotFound();
                        }

                        // Get the signed URL for the file from the cloud storage service
                        var fileUrl = await _cloudStorageService.GetSignedUrlAsync(file.FileName);

                        // File Download incrementation
                        file.NumberOfDownloads += 1;
                        _context.Update(file);
                        await _context.SaveChangesAsync();

                        // Download the file content
                        var fileContent = await _httpClient.GetByteArrayAsync(fileUrl);

                        transaction.Commit();

                        return File(fileContent, file.FileType);
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        return StatusCode(500, "Internal Server Error");
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal Server Error");
            }
        }

        [HttpPost("ShareUrl")]
        public async Task<IActionResult> ShareUrl([FromBody] UrlRequestModel model)
        {
            if (model.Minutes <= 0)
            {
                return BadRequest("Invalid duration. Duration must be greater than 0.");
            }

            var file = await _context.Files.FindAsync(model.Id);

            if (file == null)
            {
                return NotFound();
            }

            var fileUrl = await _cloudStorageService.GetSignedUrlAsync(file.FileName, model.Minutes);


            return Ok(fileUrl);
        }

        public class UrlRequestModel
        {
            public int Id { get; set; }
            public int Minutes { get; set; }
        }

        private async void SaveThumbnail(Bitmap bitmap, string fileName)
        {
            using (var stream = new MemoryStream())
            {
                bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);

                // Reset the stream position to the beginning
                stream.Position = 0;

                // Create a new FormFile
                var formFile = new FormFile(stream, 0, stream.Length, fileName, fileName)
                {
                    Headers = new HeaderDictionary(),
                    ContentType = "image/png" // Set the content type based on the image format
                };

                await _cloudStorageService.UploadFileAsync(formFile, fileName);
            }

        }

        private string? GenerateFileNameToSave(string incomingFileName, bool isThumbnail)
        {
            var fileName = Path.GetFileNameWithoutExtension(incomingFileName);
            var extension = isThumbnail ? ".png" : Path.GetExtension(incomingFileName);
            return isThumbnail ? $"{fileName}-thumbnail-{DateTime.Now.ToUniversalTime().ToString("yyyyMMddHHmmss")}{extension}"
                : $"{fileName}-{DateTime.Now.ToUniversalTime().ToString("yyyyMMddHHmmss")}{extension}";
        }

        private Bitmap GenerateThumbnail(string fileName, Stream fileStream)
        {
            var extension = Path.GetExtension(fileName)?.ToLower();

            switch (extension)
            {
                case ".xlsx":
                    return GenerateExcelThumbnail(fileStream);
                case ".docx":
                    return GenerateWordThumbnail(fileStream);
                case ".pdf":
                    return GeneratePdfThumbnail(fileStream);
                case ".txt":
                    return GenerateTextThumbnail(fileStream);
                case ".png":
                    return GenerateImageThumbnail(fileStream);
                case ".jpeg":
                    return GenerateImageThumbnail(fileStream);
                case ".jpg":
                    return GenerateImageThumbnail(fileStream);
                default:
                    throw new NotSupportedException($"File type '{extension}' is not supported for thumbnail generation.");
            }
        }

        private Bitmap GenerateExcelThumbnail(Stream excelFileStream)
        {
            // Initialize XlsIO
            using (var excelEngine = new ExcelEngine())
            {
                var application = excelEngine.Excel;

                // Open the Excel workbook
                var workbook = application.Workbooks.Open(excelFileStream);

                // Get the first worksheet
                var worksheet = workbook.Worksheets[0];

                // Convert the worksheet to an image and obtain the stream
                using (var stream = new MemoryStream())
                {
                    worksheet.ConvertToImage(1, 1, worksheet.UsedRange.LastRow, worksheet.UsedRange.LastColumn, stream);

                    // Reset the stream position before creating the Bitmap
                    stream.Position = 0;

                    // Create a bitmap from the stream
                    var bitmap = new Bitmap(stream);

                    // Save the bitmap as a thumbnail image
                    return bitmap;
                }
            }
        }

        private Bitmap GenerateWordThumbnail(Stream wordFileStream)
        {

            using (var reader = new MemoryStream())
            {
                wordFileStream.CopyTo(reader);
                reader.Seek(0, SeekOrigin.Begin);

                // Creates a Spire.Doc object to work with
                Spire.Doc.Document doc = new Spire.Doc.Document(reader, Spire.Doc.FileFormat.Auto);
                // SaveToImages creates an array of System.Drawing.Image, we take only the 1st element
                System.Drawing.Image img = doc.SaveToImages(0, 1, Spire.Doc.Documents.ImageType.Bitmap)[0];

                using (var ms2 = new MemoryStream())
                {
                    // We create a thumbnail (0.5 width and height = 50%)
                    img.GetThumbnailImage(300, 300, null, IntPtr.Zero).Save(ms2, System.Drawing.Imaging.ImageFormat.Png);
                    // Save the bitmap as a thumbnail image
                    return new Bitmap(ms2);
                }
            }
        }

        private Bitmap GeneratePdfThumbnail(Stream pdfFileStream)
        {
            // Assuming the first page of the PDF
            var pageNumber = 1;

            // Convert the PDF page to a bitmap
            var pdfBitmap = RenderPdfToBitmap(pdfFileStream, pageNumber);

            // Assuming you want to resize the image for the thumbnail
            var resizedBitmap = new Bitmap(pdfBitmap, new Size(100, 100));

            // Save the resized bitmap as a thumbnail image
            return resizedBitmap;
        }

        private Bitmap RenderPdfToBitmap(Stream pdfFileStream, int pageNumber)
        {
            Document pdfDocument = new Document(pdfFileStream);

            // Get page of desired index from collection
            var page = pdfDocument.Pages[pageNumber];

            // Create stream for image file
            using (MemoryStream imageStream = new MemoryStream())
            {
                // Create Resolution object
                Resolution resolution = new Resolution(800);

                // Create an instance of JpegDevice and set height, width, resolution, and quality of the image
                JpegDevice jpegDevice = new JpegDevice(300, 300, resolution, 100);

                // Convert a particular page and save the image to stream
                jpegDevice.Process(page, imageStream);

                // Reset the position of the MemoryStream to the beginning
                imageStream.Position = 0;

                // Create a Bitmap from the MemoryStream
                return new Bitmap(imageStream);
            }
        }

        private Bitmap GenerateTextThumbnail(Stream textFileStream)
        {
            // Read the text from the text file (assuming it's short)
            using (var reader = new StreamReader(textFileStream))
            {
                // Read the first 30 lines from the text file
                var lines = new List<string>();
                for (int i = 0; i < 30 && !reader.EndOfStream; i++)
                {
                    lines.Add(reader.ReadLine());
                }

                // Join the lines to create a single string
                var text = string.Join(Environment.NewLine, lines);

                // Create a simple thumbnail with the text
                var bitmap = new Bitmap(300, 300);
                using (var graphics = Graphics.FromImage(bitmap))
                {
                    graphics.DrawString(text, new System.Drawing.Font("Arial", 10), Brushes.Black, new PointF(10, 10));
                }

                return bitmap;
            }
        }

        public Bitmap GenerateImageThumbnail(Stream imageStream)
        {
            // Load the image from the stream using System.Drawing.Bitmap
            using (var originalBitmap = new Bitmap(imageStream))
            {
                // Resize the image to create a thumbnail
                var thumbnailBitmap = new Bitmap(300, 300);
                using (var graphics = Graphics.FromImage(thumbnailBitmap))
                {
                    graphics.DrawImage(originalBitmap, 0, 0, 300, 300);
                }

                return thumbnailBitmap;
            }
        }
    }
}

