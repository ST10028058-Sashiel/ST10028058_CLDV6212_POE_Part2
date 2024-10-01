using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;
using ST10028058_CLDV6212_POE.Models;

public class FilesController : Controller
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<FilesController> _logger;

    public FilesController(HttpClient httpClient, ILogger<FilesController> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    // Display the list of files
    public async Task<IActionResult> Index()
    {
        List<FileModel> files = new List<FileModel>();

        try
        {
            var response = await _httpClient.GetAsync("http://localhost:7110/api/FileShare");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                files = JsonConvert.DeserializeObject<List<FileModel>>(json);
                _logger.LogInformation("File list successfully retrieved.");
            }
            else
            {
                ViewBag.Message = "Failed to load files.";
                _logger.LogError($"Failed to retrieve file list. Status code: {response.StatusCode}");
            }
        }
        catch (HttpRequestException ex)
        {
            ViewBag.Message = $"Error retrieving files: {ex.Message}";
            _logger.LogError($"Error retrieving files: {ex.Message}");
        }

        return View(files);
    }

    // Handle file upload
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
            using (var content = new MultipartFormDataContent())
            {
                var fileContent = new StreamContent(file.OpenReadStream());
                fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
                content.Add(fileContent, "file", file.FileName);

                var response = await _httpClient.PostAsync("http://localhost:7110/api/FileShare", content);

                if (response.IsSuccessStatusCode)
                {
                    TempData["Message"] = $"File '{file.FileName}' uploaded successfully!";
                    _logger.LogInformation($"File '{file.FileName}' uploaded successfully.");
                }
                else
                {
                    TempData["Message"] = $"File upload failed: {response.ReasonPhrase}";
                    _logger.LogError($"File upload failed. Status code: {response.StatusCode}, Reason: {response.ReasonPhrase}");
                }
            }
        }
        catch (HttpRequestException ex)
        {
            TempData["Message"] = $"File upload failed: {ex.Message}";
            _logger.LogError($"Error uploading file: {ex.Message}");
        }

        return RedirectToAction("Index");
    }

    // Download file by fileName
    [HttpGet]
    public async Task<IActionResult> DownloadFile(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
        {
            return BadRequest("File name cannot be null or empty.");
        }

        try
        {
            var response = await _httpClient.GetAsync($"http://localhost:7110/api/FileShare/{fileName}");

            if (response.IsSuccessStatusCode)
            {
                var fileStream = await response.Content.ReadAsStreamAsync();
                _logger.LogInformation($"File '{fileName}' downloaded successfully.");
                return File(fileStream, "application/octet-stream", fileName);
            }
            else
            {
                _logger.LogError($"File '{fileName}' not found. Status code: {response.StatusCode}");
                return NotFound($"File '{fileName}' not found.");
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError($"Error downloading file: {ex.Message}");
            return BadRequest($"Error downloading file: {ex.Message}");
        }
    }
}

// FileModel class to match the Azure Function output

