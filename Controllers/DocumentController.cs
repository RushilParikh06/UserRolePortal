using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

public class DocumentController : Controller
{
    [HttpPost]
    public async Task<IActionResult> UploadDocument(IFormFile file)
    {
        // 1. Check if a file was actually uploaded
        if (file == null || file.Length == 0)
        {
            return BadRequest("Please select a file to upload.");
        }

        // CONSTRAINT 1: Maximum Size 2MB
        long maxFileSize = 2 * 1024 * 1024; // 2 Megabytes in bytes
        if (file.Length > maxFileSize)
        {
            return BadRequest("File size exceeds the maximum limit of 2MB.");
        }

        // A valid PDF file always starts with the hex signature: 25 50 44 46 2D
        var pdfSignature = new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D };

        using (var stream = file.OpenReadStream())
        {
            // Read the first 5 bytes of the file
            var buffer = new byte[pdfSignature.Length];
            await stream.ReadExactlyAsync(buffer, 0, buffer.Length);

            // Compare the bytes to the PDF signature
            if (!buffer.SequenceEqual(pdfSignature))
            {
                return BadRequest("Invalid file type. Only genuine PDF files are allowed.");
            }

            // IMP Reset the stream position back to 0!
            // Since we read the first 5 bytes, we need to rewind the stream 
            // so that when you save it, the file isn't corrupted by missing its first 5 bytes.
            stream.Position = 0;

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (extension != ".pdf")
            {
                return BadRequest("The file must have a .pdf extension.");
            }

            // -------------------------------------------------------------
            // If all checks pass, proceed to save the file
            // -------------------------------------------------------------
            // var savePath = Path.Combine("your_upload_directory", file.FileName);
            // using (var fileStream = new FileStream(savePath, FileMode.Create))
            // {
            //     await stream.CopyToAsync(fileStream);
            // }
        }

        return Ok("Document uploaded successfully!");
    }
}