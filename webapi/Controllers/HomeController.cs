using Microsoft.AspNetCore.Authorization;
using DocumentFormat.OpenXml.Packaging;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Drawing;
using System;
using System.IO;
using System.Configuration;
using System.Runtime.InteropServices;
using Syncfusion.XlsIO;
using Aspose.Pdf;
using Aspose.Pdf.Devices;

namespace webapi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HomeController : ControllerBase
    {
        [AllowAnonymous]
        [HttpPost("UploadFile")]
        public async Task<IActionResult> UploadFile(IFormFile files)
        {
            if (files == null || files.Length == 0)
                return BadRequest("File not selected");

            // Process the file here
            // For example, save it to the server or perform any required actions

            // You can replace this logic with your actual file processing logic
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "uploads", files.FileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await files.CopyToAsync(stream);
            }

            return Ok("File uploaded successfully");
        }





        private string GenerateThumbnail(string fileName, Stream fileStream)
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

        private string GenerateExcelThumbnail(Stream excelFileStream)
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
                    return SaveThumbnail(bitmap);
                }
            }
        }

        private string GenerateWordThumbnail(Stream wordFileStream)
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
                    // Convert to Base64 string representation of the image
                    return Convert.ToBase64String(ms2.ToArray());
                }
            }
        }
        private string GeneratePdfThumbnail(Stream pdfFileStream)
        {
            // Assuming the first page of the PDF
            var pageNumber = 1;

            // Convert the PDF page to a bitmap
            var pdfBitmap = RenderPdfToBitmap(pdfFileStream, pageNumber);

            // Assuming you want to resize the image for the thumbnail
            var resizedBitmap = new Bitmap(pdfBitmap, new Size(100, 100));

            // Save the resized bitmap as a thumbnail image
            return SaveThumbnail(resizedBitmap);
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

        private string GenerateTextThumbnail(Stream textFileStream)
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
                    graphics.DrawString(text, new Font("Arial", 10), Brushes.Black, new PointF(10, 10));
                }

                return SaveThumbnail(bitmap);
            }
        }

        private string SaveThumbnail(Bitmap bitmap)
        {
            var thumbnailPath = Path.Combine(Directory.GetCurrentDirectory(), "thumbnails", $"{Guid.NewGuid()}_thumbnail.png");
            bitmap.Save(thumbnailPath);

            return thumbnailPath;
        }
    }
}

