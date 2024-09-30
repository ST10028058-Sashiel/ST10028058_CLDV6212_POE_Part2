using ST10028058_CLDV6212_POE.Models;
using ST10028058_CLDV6212_POE.Services;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Net.Http.Headers;

public class ProductsController : Controller
{
    private readonly HttpClient _httpClient;
    private readonly TableStorageService _tableStorageService;
    private readonly BlobService _blobService;

    public ProductsController(HttpClient httpClient, TableStorageService tableStorageService, BlobService blobService)
    {
        _httpClient = httpClient;
        _tableStorageService = tableStorageService;
        _blobService = blobService;
    }

    public async Task<IActionResult> Index()
    {
        var products = await _tableStorageService.GetAllProductsAsync();
        return View(products);
    }

    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Create(Product product, IFormFile file)
    {
        if (ModelState.IsValid)
        {
            // Upload image to Blob Storage if provided
            if (file != null)
            {
                using var stream = file.OpenReadStream();
                var imageUrl = await _blobService.UploadAsync(stream, file.FileName);
                product.ImageUrl = imageUrl;
            }

            // Prepare the product and file data to send to Azure Function
            var formData = new MultipartFormDataContent();
            var productJson = JsonConvert.SerializeObject(product);
            formData.Add(new StringContent(productJson), "product");

            if (file != null)
            {
                var fileContent = new StreamContent(file.OpenReadStream());
                fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
                formData.Add(fileContent, "file", file.FileName);
            }

            var response = await _httpClient.PostAsync("http://localhost:7110/api/AddProduct", formData);

            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction("Index");
            }

            ModelState.AddModelError("", "There was an error adding the product.");
        }

        return View(product);
    }

    public async Task<IActionResult> Details(string partitionKey, string rowKey)
    {
        var product = await _tableStorageService.GetProductAsync(partitionKey, rowKey);
        if (product == null)
        {
            return NotFound();
        }
        return View(product);
    }

    public async Task<IActionResult> Delete(string partitionKey, string rowKey)
    {
        var product = await _tableStorageService.GetProductAsync(partitionKey, rowKey);
        if (product != null && !string.IsNullOrEmpty(product.ImageUrl))
        {
            await _blobService.DeleteBlobAsync(product.ImageUrl);
        }

        await _tableStorageService.DeleteProductAsync(partitionKey, rowKey);
        return RedirectToAction("Index");
    }
}
