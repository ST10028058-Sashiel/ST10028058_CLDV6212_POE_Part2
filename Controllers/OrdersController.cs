using Microsoft.AspNetCore.Mvc;
using ST10028058_CLDV6212_POE.Models;
using System.Threading.Tasks;
using System.Linq;
using System.Net.Http;
using Newtonsoft.Json;

namespace ST10028058_CLDV6212_Part1.Controllers
{
    public class OrderController : Controller
    {
        private readonly TableStorageService _tableStorageService;
        private readonly HttpClient _httpClient;

        public OrderController(TableStorageService tableStorageService, HttpClient httpClient)
        {
            _tableStorageService = tableStorageService;
            _httpClient = httpClient;
        }

        // Display all orders
        public async Task<IActionResult> Index()
        {
            var orders = await _tableStorageService.GetAllOrdersAsync();
            return View(orders);
        }

        // Display the create order form
        public async Task<IActionResult> Create()
        {
            var customers = await _tableStorageService.GetAllCustomersAsync();
            var products = await _tableStorageService.GetAllProductsAsync();

            ViewData["Customers"] = customers;
            ViewData["Products"] = products;

            return View();
        }

        // Handle the form submission for creating a new order
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Order order)
        {
            if (ModelState.IsValid)
            {
                var customers = await _tableStorageService.GetAllCustomersAsync();
                var products = await _tableStorageService.GetAllProductsAsync();

                // Find the selected customer and product
                var selectedCustomer = customers.FirstOrDefault(c => c.Customer_Name == order.CustomerName);
                var selectedProduct = products.FirstOrDefault(p => p.Product_Name == order.ProductName);

                if (selectedCustomer == null || selectedProduct == null)
                {
                    ModelState.AddModelError("", "Invalid customer or product selected.");
                    ViewData["Customers"] = customers;
                    ViewData["Products"] = products;
                    return View(order);
                }

                order.Customer_ID = selectedCustomer.Customer_Id;
                order.Product_ID = selectedProduct.Product_Id;
                order.PartitionKey = order.Customer_ID.ToString();
                order.RowKey = Guid.NewGuid().ToString();

                // Ensure Order_Date is in UTC
                order.Order_Date = DateTime.SpecifyKind(order.Order_Date, DateTimeKind.Utc);

                // Send the order details to the Azure Function
                var orderJson = JsonConvert.SerializeObject(order);
                var content = new StringContent(orderJson, System.Text.Encoding.UTF8, "application/json");

                HttpResponseMessage response = await _httpClient.PostAsync("http://localhost:7110/api/AddOrder", content);

                if (response.IsSuccessStatusCode)
                {
                    // Success message can be handled if needed
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    ModelState.AddModelError("", "There was an error adding the order to the table or queue.");
                    return View(order);
                }
            }

            var allCustomers = await _tableStorageService.GetAllCustomersAsync();
            var allProducts = await _tableStorageService.GetAllProductsAsync();
            ViewData["Customers"] = allCustomers;
            ViewData["Products"] = allProducts;

            return View(order);
        }

        // Display the details of a specific order
        public async Task<IActionResult> Details(string partitionKey, string rowKey)
        {
            var order = await _tableStorageService.GetOrderAsync(partitionKey, rowKey);
            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        // Display the delete confirmation page
        public async Task<IActionResult> Delete(string partitionKey, string rowKey)
        {
            var order = await _tableStorageService.GetOrderAsync(partitionKey, rowKey);
            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        // Handle the form submission for deleting an order
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string partitionKey, string rowKey)
        {
            var order = await _tableStorageService.GetOrderAsync(partitionKey, rowKey);
            if (order == null)
            {
                return NotFound();
            }

            await _tableStorageService.DeleteOrderAsync(partitionKey, rowKey);

            // Optionally notify via Azure Function or queue
            string message = $"Order with ID {order.Order_Id}, Customer {order.CustomerName}, Product {order.ProductName} has been deleted.";
            var content = new StringContent(JsonConvert.SerializeObject(message), System.Text.Encoding.UTF8, "application/json");

            HttpResponseMessage response = await _httpClient.PostAsync("http://localhost:7110/api/AddOrder", content);

            if (!response.IsSuccessStatusCode)
            {
                ModelState.AddModelError("", "There was an error notifying the queue.");
            }

            return View("DeleteConfirmed", order);
        }
    }
}
