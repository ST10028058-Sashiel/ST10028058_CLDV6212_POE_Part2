using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Text;
using System.Text.Json;  // Ensure System.Text.Json is included for serialization
using System.Threading.Tasks;
using ST10028058_CLDV6212_POE.Models;
using ST10028058_CLDV6212_POE.Services;

public class CustomersController : Controller
{
    private readonly TableStorageService _tableStorageService;
    private readonly HttpClient _httpClient;
    private readonly string _azureFunctionUrl = "http://localhost:7110/api/AddCustomer";  // Replace with actual Function URL if deployed

    public CustomersController(TableStorageService tableStorageService, HttpClient httpClient)
    {
        _tableStorageService = tableStorageService;
        _httpClient = httpClient;
    }

    // Index action - Displays all customers
    public async Task<IActionResult> Index()
    {
        var customers = await _tableStorageService.GetAllCustomersAsync();
        return View(customers);
    }

    // Details action - Displays customer details
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
            return RedirectToAction("Login");
        }

        return View(customer); // Return the view if creation fails
    }


    // Create/Register action - Displays registration form
    public IActionResult Register()
    {
        return View();
    }

    // Create/Register POST action - Submits registration data
    [HttpPost]
    public async Task<IActionResult> Register(Customer customer)
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

    // Login action - Displays login form
    public IActionResult Login()
    {
        return View();
    }

    // Login POST action - Handles login logic
    [HttpPost]
    public async Task<IActionResult> Login(Customer customer)
    {
        var customers = await _tableStorageService.GetAllCustomersAsync();
        var existingCustomer = customers.FirstOrDefault(c => c.email == customer.email && c.password == customer.password);

        if (existingCustomer != null)
        {
            // Set session for the logged-in user
            HttpContext.Session.SetString("UserEmail", customer.email);
            return RedirectToAction("Index", "Home");
        }

        ModelState.AddModelError("", "Invalid email or password");
        return View(customer);
    }

    // Logout action - Logs out the user
    public IActionResult Logout()
    {
        HttpContext.Session.Remove("UserEmail");
        return RedirectToAction("Login");
    }

    // Delete action - Displays confirmation for deleting a customer
    public async Task<IActionResult> Delete(string partitionKey, string rowKey)
    {
        var customer = await _tableStorageService.GetCustomerAsync(partitionKey, rowKey);
        if (customer == null)
        {
            return NotFound();
        }

        return View(customer);
    }

    // Delete POST action - Handles customer deletion
    [HttpPost, ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(string partitionKey, string rowKey)
    {
        await _tableStorageService.DeleteCustomerAsync(partitionKey, rowKey);
        return RedirectToAction(nameof(Index));
    }
}
