using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineMarketplace.Database;
using OnlineMarketplace.DTO;
using OnlineMarketplace.Models;
using System.Diagnostics;

namespace OnlineMarketplace.Controllers
{
	public class HomeController : Controller
	{
		private readonly ILogger<HomeController> _logger;
		private readonly ApplicationDbContext _context;

		public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
		{
			_logger = logger;
			_context = context;
		}

		[HttpGet]
		public async Task<IActionResult> Index(List<string>? categories, string? searchQuery)
		{
			var selectedCategories = categories?
				.Where(c => !string.IsNullOrWhiteSpace(c))
				.ToList() ?? new List<string>();

			ViewBag.Categories = CategoryDto.GetPredefinedCategories();
			ViewBag.SelectedCategories = selectedCategories;
			ViewBag.SearchQuery = searchQuery;

			var userIdString = HttpContext.Session.GetString("UserId");

			var productsQuery = _context.Product
				.Include(p => p.Seller)
				.AsQueryable();

			if (!string.IsNullOrEmpty(userIdString) &&
				int.TryParse(userIdString, out int currentUserId))
			{
				productsQuery = productsQuery
					.Where(p => p.SellerId != currentUserId);
			}

			if (selectedCategories.Any())
			{
				productsQuery = productsQuery
					.Where(p => selectedCategories.Contains(p.Category));
			}

			if (!string.IsNullOrEmpty(searchQuery))
			{
				productsQuery = productsQuery
					.Where(p =>
						p.ProductName.ToLower().Contains(searchQuery.ToLower()) ||
						(p.ProductDescription != null &&
						 p.ProductDescription.ToLower().Contains(searchQuery.ToLower())));
			}

			var products = await productsQuery
				.OrderByDescending(p => p.ProductId)
				.Take(20)
				.ToListAsync();

			return View(products);
		}

		public IActionResult Privacy()
		{
			return View();
		}

		[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
		public IActionResult Error()
		{
			return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
		}
	}
}