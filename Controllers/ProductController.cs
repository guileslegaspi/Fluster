using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OnlineMarketplace.Database;
using OnlineMarketplace.DTO;
using OnlineMarketplace.Models;

namespace OnlineMarketplace.Controllers
{
	public class ProductController : Controller
	{
		private readonly ApplicationDbContext _context;

		public ProductController(ApplicationDbContext context)
		{
			_context = context;
		}

		private SelectList GetCategorySelectList(string? selectedCategory = null)
		{
			var categories = CategoryDto.GetPredefinedCategories();
			return new SelectList(categories, "Name", "Name", selectedCategory);
		}

		[HttpGet]
		public async Task<IActionResult> Details(int? id)
		{
			if (id == null)
			{
				return NotFound();
			}

			var currentListing = await _context.Product
				.Include(p => p.Seller)
				.FirstOrDefaultAsync(x => x.ProductId == id);

			if (currentListing == null)
			{
				return NotFound();
			}

			return View(currentListing);
		}

		[HttpGet]
		public async Task<IActionResult> MyListings()
		{
			var userIdListings = HttpContext.Session.GetString("UserId");
			if (string.IsNullOrEmpty(userIdListings))
			{
				return View("Login", "Account");
			}

			int currentUserId = int.Parse(userIdListings);

			var userProducts = await _context.Product
				.Where(p => p.SellerId == currentUserId)
				.ToListAsync();

			return View(userProducts);
		}

		[HttpGet]
		public IActionResult CreateProduct()
		{
			if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserId")))
			{
				return RedirectToAction("Login", "Account");
			}

			ViewBag.CategorySelectList = GetCategorySelectList();
			return View();
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> CreateProduct(ProductModel model)
		{
			var userIdString = HttpContext.Session.GetString("UserId");

			if (string.IsNullOrEmpty(userIdString))
			{
				return RedirectToAction("Login", "Account");
			}

			var imageFile = Request.Form.Files.GetFile("ImageFile");

			if (imageFile != null && imageFile.Length > 0)
			{
				ModelState.Remove("Image");
			}

			if (imageFile == null && string.IsNullOrWhiteSpace(model.Image))
			{
				ModelState.AddModelError(
					"Image",
					"Please upload an image or provide an image URL.");
			}

			if (imageFile != null && imageFile.Length > 0)
			{
				var uploadsFolder = Path.Combine(
					Directory.GetCurrentDirectory(),
					"wwwroot",
					"uploads",
					"products");

				Directory.CreateDirectory(uploadsFolder);

				var fileExtension = Path.GetExtension(imageFile.FileName);
				var fileName = $"{Guid.NewGuid()}{fileExtension}";
				var filePath = Path.Combine(uploadsFolder, fileName);

				using (var stream = new FileStream(filePath, FileMode.Create))
				{
					await imageFile.CopyToAsync(stream);
				}

				model.Image = $"/uploads/products/{fileName}";
			}

			if (ModelState.IsValid)
			{
				model.SellerId = int.Parse(userIdString);

				_context.Product.Add(model);
				await _context.SaveChangesAsync();

				return RedirectToAction("Index", "Home");
			}

			ViewBag.CategorySelectList = GetCategorySelectList(model.Category);
			return View(model);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> DeleteProduct(int id)
		{
			var userIdstring = HttpContext.Session.GetString("UserId");

			if (string.IsNullOrEmpty(userIdstring))
			{
				return RedirectToAction("Login", "Account");
			}

			var product = await _context.Product.FindAsync(id);

			if (product == null)
			{
				return NotFound();
			}

			int currentUserId = int.Parse(userIdstring);

			if (product.SellerId != currentUserId)
			{
				return Unauthorized();
			}

			_context.Product.Remove(product);
			await _context.SaveChangesAsync();

			return RedirectToAction("MyListings");
		}

		[HttpGet]
		public async Task<IActionResult> EditProduct(int? id)
		{
			if (id == null) 
			{
				return NotFound();
			}
				
			var userIdString = HttpContext.Session.GetString("UserId");

			if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int currentUserId)) 
			{
				return RedirectToAction("Login", "Account");
			}
				
			var product = await _context.Product.FindAsync(id);

			if (product == null) 
			{
				return NotFound();
			}
				
			if (product.SellerId != currentUserId) 
			{
				return Unauthorized();
			}

			ViewBag.CategorySelectList = GetCategorySelectList(product.Category);

			return View(product);
		}


		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> EditProduct(int? id, ProductModel model)
		{
			if (id == null) 
			{
				return NotFound();
			}				

			var userIdString = HttpContext.Session.GetString("UserId");

			if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int currentUserId)) 
			{
				return RedirectToAction("Login", "Account");
			}
				
			var product = await _context.Product.FindAsync(id);

			if (product == null)
			{ 
				return NotFound();
			}
			
			if (product.SellerId != currentUserId) 
			{ 
				return Unauthorized();
			}

			if (ModelState.IsValid)
			{
				product.ProductName = model.ProductName;
				product.ProductDescription = model.ProductDescription;
				product.Price = model.Price;
				product.Stock = model.Stock;
				product.Category = model.Category;

				var imageFile = Request.Form.Files.GetFile("ImageFile");

				if (imageFile != null && imageFile.Length > 0)
				{
					var uploadsFolder = Path.Combine(
						Directory.GetCurrentDirectory(),
						"wwwroot",
						"uploads",
						"products");

					Directory.CreateDirectory(uploadsFolder);

					var fileExtension = Path.GetExtension(imageFile.FileName);
					var fileName = $"{Guid.NewGuid()}{fileExtension}";
					var filePath = Path.Combine(uploadsFolder, fileName);

					using (var stream = new FileStream(filePath, FileMode.Create))
					{
						await imageFile.CopyToAsync(stream);
					}

					product.Image = $"/uploads/products/{fileName}";
				}
				else if (!string.IsNullOrWhiteSpace(model.Image))
				{
					product.Image = model.Image;
				}

				await _context.SaveChangesAsync();

				return RedirectToAction("MyListings");
			}

			ViewBag.CategorySelectList = GetCategorySelectList(model.Category);

			return View(model);
		}
	}
}