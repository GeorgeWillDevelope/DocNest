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

namespace webapi.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class HomeController : ControllerBase
    {
        private readonly DocNestDbContext _context;
        private readonly ICloudStorageService _cloudStorageService;

        public HomeController(DocNestDbContext context, ICloudStorageService cloudStorageService)
        {
            _context = context;
            _cloudStorageService = cloudStorageService;
        }

        [AllowAnonymous]
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
                    DateOfUpload = DateTime.Now,
                    FileType = file.ContentType
                };

                await _cloudStorageService.UploadFileAsync(file, fileInfo.FileName);

                // Generate thumbnail and save
                var bitmap = GenerateThumbnail(fileInfo.FileName, file.OpenReadStream());
                SaveThumbnail(bitmap, fileInfo.FileName);

                _context.Add(fileInfo);
            }

            await _context.SaveChangesAsync();

            return Ok("Files uploaded successfully");
        }

        private async void SaveThumbnail(Bitmap bitmap, string fileName)
        {
            var thumbnailFileName = GenerateFileNameToSave(fileName, true);

            using (var stream = new MemoryStream())
            {
                bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);

                // Reset the stream position to the beginning
                stream.Position = 0;

                // Create a new FormFile
                var formFile = new FormFile(stream, 0, stream.Length, fileName, thumbnailFileName)
                {
                    Headers = new HeaderDictionary(),
                    ContentType = "image/png" // Set the content type based on the image format
                };

                await _cloudStorageService.UploadFileAsync(formFile, thumbnailFileName);
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
                    img.GetThumbnailImage((int)(img.Width * 0.5), (int)(img.Height * 0.5), null, IntPtr.Zero).Save(ms2, System.Drawing.Imaging.ImageFormat.Png);
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
                Resolution resolution = new Resolution(300);

                // Create an instance of JpegDevice and set height, width, resolution, and quality of the image
                JpegDevice jpegDevice = new JpegDevice(45, 59, resolution, 100);

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
                var bitmap = new Bitmap(200, 200);
                using (var graphics = Graphics.FromImage(bitmap))
                {
                    graphics.DrawString(text, new System.Drawing.Font("Arial", 10), Brushes.Black, new PointF(10, 10));
                }

                return bitmap;
            }
        }
    }
}

