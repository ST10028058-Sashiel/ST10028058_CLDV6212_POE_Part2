using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ST10028058_CLDV6212_POE.Models;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System;

public class FilesController : Controller
{
    private readonly HttpClient _httpClient;
    private readonly AzureFileShareService _fileShareService;

    public FilesController(HttpClient httpClient, AzureFileShareService fileShareService)
    {
        _httpClient = httpClient;
        _fileShareService = fileShareService;
    }

    // List files in the "uploads" directory
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

    // Upload file using the Azure Function
    [HttpPost]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            ModelState.AddModelError("file", "Please select a file to upload.");
            return RedirectToAction("Index");
        }

        try
        {
            // Upload the file via Azure Function
            using (var stream = file.OpenReadStream())
            {
                var content = new MultipartFormDataContent();
                var fileContent = new StreamContent(stream);
                fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
                content.Add(fileContent, "file", file.FileName);

                // Hard-coded Azure Function URL for file uploads
                var functionUrl = "https://st10028058-fileshare.azurewebsites.net/";

                var response = await _httpClient.PostAsync(functionUrl, content);

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

    // Handle file download using AzureFileShareService
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

