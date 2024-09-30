using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ST10028058_CLDV6212_POE.Models;
using ST10028058_CLDV6212_POE.Services;

public class CustomersController : Controller
{
    private readonly TableStorageService _tableStorageService;
    private readonly HttpClient _httpClient;
    private readonly string _azureFunctionUrl = "http://localhost:7110/api/AddCustomer";  

    public CustomersController(TableStorageService tableStorageService, HttpClient httpClient)
    {
        _tableStorageService = tableStorageService;
        _httpClient = httpClient;
    }

    // Leave the GetAllCustomers using TableStorageService
    public async Task<IActionResult> Index()
    {
        var customers = await _tableStorageService.GetAllCustomersAsync();
        return View(customers);
    }

    // Leave the Details method using TableStorageService
    public async Task<IActionResult> Details(string partitionKey, string rowKey)
    {
        var customer = await _tableStorageService.GetCustomerAsync(partitionKey, rowKey);

        if (customer == null)
        {
            return NotFound();
        }

        return View(customer);
    }

    // Update Create to use Azure Function
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Create(Customer customer)
    {
        customer.PartitionKey = "CustomersPartition";
        customer.RowKey = Guid.NewGuid().ToString();

        // Call the Azure Function to add a new customer
        var customerJson = JsonSerializer.Serialize(customer);
        var content = new StringContent(customerJson, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(_azureFunctionUrl, content);

        if (response.IsSuccessStatusCode)
        {
            return RedirectToAction("Index");
        }

        return View(customer); // Return the view if creation fails
    }

    // Leave the Delete method using TableStorageService
    public async Task<IActionResult> Delete(string partitionKey, string rowKey)
    {
        var customer = await _tableStorageService.GetCustomerAsync(partitionKey, rowKey);
        if (customer == null)
        {
            return NotFound();
        }

        return View(customer);
    }

    [HttpPost, ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(string partitionKey, string rowKey)
    {
        await _tableStorageService.DeleteCustomerAsync(partitionKey, rowKey);
        return RedirectToAction(nameof(Index));
    }
}
