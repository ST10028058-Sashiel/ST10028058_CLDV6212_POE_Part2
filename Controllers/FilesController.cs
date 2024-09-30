using ST10028058_CLDV6212_POE.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

public class FilesController : Controller
{
    private readonly AzureFileShareService _fileShareService;

    public FilesController(AzureFileShareService fileShareService)
    {
        _fileShareService = fileShareService;
    }

    public async Task<IActionResult> Index()
    {
        List<FileModel> files;
        try
        {
            files = await _fileShareService.ListFilesAsync("uploads");
        }
        catch (Exception ex)
        {
            ViewBag.Message = $"Failed to load files: {ex.Message}";
            files = new List<FileModel>();
        }

        return View(files);
    }

    [HttpPost]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            ModelState.AddModelError("file", "Please select a file to upload.");
            return await Index();
        }

        try
        {
            using (var client = new HttpClient())
            {
                var content = new MultipartFormDataContent();
                var fileContent = new StreamContent(file.OpenReadStream());
                content.Add(fileContent, "file", file.FileName);

                // Replace with your Azure Function URL
                var response = await client.PostAsync("http://localhost:7110/api/UploadFile", content);

                if (response.IsSuccessStatusCode)
                {
                    TempData["Message"] = $"File '{file.FileName}' uploaded successfully!";
                }
                else
                {
                    TempData["Message"] = $"File upload failed: {response.ReasonPhrase}";
                }
            }
        }
        catch (Exception ex)
        {
            TempData["Message"] = $"File upload failed: {ex.Message}";
        }

        return RedirectToAction("Index");
    }

    // Handle file download
    [HttpGet]
    public async Task<IActionResult> DownloadFile(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
        {
            return BadRequest("File name cannot be null or empty.");
        }

        try
        {
            var fileStream = await _fileShareService.DownloadFileAsync("uploads", fileName);

            if (fileStream == null)
            {
                return NotFound($"File '{fileName}' not found.");
            }

            return File(fileStream, "application/octet-stream", fileName);
        }
        catch (Exception ex)
        {
            return BadRequest($"Error downloading file: {ex.Message}");
        }
    }
}
