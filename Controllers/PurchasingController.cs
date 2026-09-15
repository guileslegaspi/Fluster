
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineMarketplace.Database;
using OnlineMarketplace.DTO;
using OnlineMarketplace.Models;
using System.Text;
using System.Text.Json;

namespace OnlineMarketplace.Controllers
{
	public class PurchasingController : Controller
	{
		private readonly ApplicationDbContext _context;
		private readonly IHttpClientFactory _httpClientFactory;

		public PurchasingController(
			ApplicationDbContext context,
			IHttpClientFactory httpClientFactory)
		{
			_context = context;
			_httpClientFactory = httpClientFactory;
		}

		private bool TryGetCurrentUserId(out int userId)
		{
			userId = 0;

			var userIdString = HttpContext.Session.GetString("UserId");

			if (string.IsNullOrEmpty(userIdString))
			{
				return false;
			}

			return int.TryParse(userIdString, out userId);
		}

		private IActionResult RedirectToLogin()
		{
			return RedirectToAction("Login", "Account");
		}

		// =========================================================
		// CART
		// =========================================================

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> AddToCart(
			int productId,
			int productQuantity = 1)
		{
			if (!TryGetCurrentUserId(out int currentUserId))
			{
				return RedirectToLogin();
			}

			if (productQuantity <= 0)
			{
				TempData["ErrorMessage"] = "Quantity must be greater than zero.";
				return RedirectToAction("Details", "Products", new { id = productId });
			}

			var product = await _context.Product.FindAsync(productId);

			if (product == null)
			{
				return NotFound();
			}

			
			if (product.SellerId == currentUserId)
			{
				TempData["ErrorMessage"] =
					"You cannot purchase your own listing.";

				return RedirectToAction(
					"Details",
					"Products",
					new { id = productId });
			}

			
			if (product.Stock <= 0)
			{
				TempData["ErrorMessage"] =
					"This product is currently out of stock.";

				return RedirectToAction(
					"Details",
					"Products",
					new { id = productId });
			}

			var existingCartItem = await _context.Cart
				.FirstOrDefaultAsync(c =>
					c.UserId == currentUserId &&
					c.ProductId == productId);

			if (existingCartItem != null)
			{
				int newQuantity =
					existingCartItem.Quantity + productQuantity;

				if (newQuantity > product.Stock)
				{
					TempData["ErrorMessage"] =
						$"Only {product.Stock} item(s) are available.";

					return RedirectToAction(
						"Details",
						"Products",
						new { id = productId });
				}

				existingCartItem.Quantity = newQuantity;
			}
			else
			{
				var cartItem = new CartModel
				{
					UserId = currentUserId,
					ProductId = productId,
					Quantity = productQuantity,
					CheckoutBox = true
				};

				_context.Cart.Add(cartItem);
			}

			await _context.SaveChangesAsync();

			return RedirectToAction(nameof(Cart));
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> RemoveFromCart(int? cartId)
		{
			if (!TryGetCurrentUserId(out int currentUserId))
			{
				return RedirectToLogin();
			}

			if (cartId == null)
			{
				return NotFound();
			}

			var cartItem = await _context.Cart
				.FirstOrDefaultAsync(c =>
					c.CartId == cartId &&
					c.UserId == currentUserId);

			if (cartItem == null)
			{
				return NotFound();
			}

			_context.Cart.Remove(cartItem);

			await _context.SaveChangesAsync();

			return RedirectToAction(nameof(Cart));
		}

		[HttpGet]
		public async Task<IActionResult> Cart()
		{
			if (!TryGetCurrentUserId(out int currentUserId))
			{
				return RedirectToLogin();
			}

			var cartItems = await _context.Cart
				.Include(c => c.Product)
				.Where(c => c.UserId == currentUserId)
				.ToListAsync();

			return View(cartItems);
		}

		// =========================================================
		// CHECKOUT - GET
		// =========================================================

		[HttpGet]
		public async Task<IActionResult> PayNow(int orderId)
		{
			if (!TryGetCurrentUserId(out int currentUserId))
			{
				return RedirectToLogin();
			}

			var order = await _context.Order
				.Include(o => o.Details)
					.ThenInclude(d => d.Product)
				.FirstOrDefaultAsync(o =>
					o.OrderId == orderId &&
					o.CustomerId == currentUserId);

			if (order == null)
			{
				return NotFound();
			}

			if (order.PaymentMethod != "PayMongo")
			{
				TempData["ErrorMessage"] =
					"This order does not use PayMongo.";

				return RedirectToAction(nameof(MyOrders));
			}

			if (order.Status != "Unpaid")
			{
				TempData["ErrorMessage"] =
					"This order is no longer awaiting payment.";

				return RedirectToAction(nameof(MyOrders));
			}

			return View(order);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> ContinuePayment(int orderId)
		{
			if (!TryGetCurrentUserId(out int currentUserId))
			{
				return RedirectToLogin();
			}

			var order = await _context.Order
				.FirstOrDefaultAsync(o =>
					o.OrderId == orderId &&
					o.CustomerId == currentUserId);

			if (order == null)
			{
				return NotFound();
			}

			if (order.PaymentMethod != "PayMongo")
			{
				TempData["ErrorMessage"] =
					"This order does not use PayMongo.";

				return RedirectToAction(nameof(MyOrders));
			}

			if (order.Status != "Unpaid")
			{
				TempData["ErrorMessage"] =
					"This order is no longer awaiting payment.";

				return RedirectToAction(nameof(MyOrders));
			}

			if (string.IsNullOrEmpty(order.PayMongoCheckoutId))
			{
				TempData["ErrorMessage"] =
					"No PayMongo checkout session was found for this order.";

				return RedirectToAction(nameof(MyOrders));
			}

			var checkoutSession =
				await GetPayMongoCheckoutSessionAsync(
					order.PayMongoCheckoutId);

			if (checkoutSession.CheckoutUrl == null)
			{
				TempData["ErrorMessage"] =
					"The PayMongo checkout session could not be retrieved.";

				return RedirectToAction(nameof(MyOrders));
			}

			if (checkoutSession.Status != "active")
			{
				TempData["ErrorMessage"] =
					"This PayMongo checkout session is no longer active.";

				return RedirectToAction(nameof(MyOrders));
			}

			return Redirect(checkoutSession.CheckoutUrl);
		}

		[HttpGet]
		public async Task<IActionResult> Checkout(
			int? productId,
			int quantity = 1)
		{
			if (!TryGetCurrentUserId(out int currentUserId))
			{
				return RedirectToLogin();
			}

			if (quantity <= 0)
			{
				TempData["ErrorMessage"] =
					"Quantity must be greater than zero.";

				return RedirectToAction(nameof(Cart));
			}

			var checkoutItems = new List<CheckoutItemDto>();

			if (productId.HasValue)
			{
				var product = await _context.Product
					.FirstOrDefaultAsync(p =>
						p.ProductId == productId.Value);

				if (product == null)
				{
					return NotFound();
				}

				if (product.SellerId == currentUserId)
				{
					TempData["ErrorMessage"] =
						"You cannot purchase your own listing.";

					return RedirectToAction(
						"Details",
						"Products",
						new { id = productId.Value });
				}

				if (product.Stock <= 0)
				{
					TempData["ErrorMessage"] =
						"This product is currently out of stock.";

					return RedirectToAction(
						"Details",
						"Products",
						new { id = productId.Value });
				}

				if (quantity > product.Stock)
				{
					TempData["ErrorMessage"] =
						$"Only {product.Stock} item(s) are available.";

					return RedirectToAction(
						"Details",
						"Products",
						new { id = productId.Value });
				}

				checkoutItems.Add(new CheckoutItemDto
				{
					ProductId = product.ProductId,
					ProductName = product.ProductName,

					
					UnitPrice = product.Price,

					Quantity = quantity,
					IsSelected = true
				});
			}

	
			else
			{
				var cartItems = await _context.Cart
					.Include(c => c.Product)
					.Where(c => c.UserId == currentUserId)
					.ToListAsync();

				foreach (var cartItem in cartItems)
				{
					if (cartItem.Product == null)
					{
						continue;
					}

					
					if (cartItem.Product.SellerId == currentUserId)
					{
						continue;
					}

					
					if (cartItem.Quantity <= 0 ||
						cartItem.Quantity > cartItem.Product.Stock)
					{
						continue;
					}

					checkoutItems.Add(new CheckoutItemDto
					{
						CartId = cartItem.CartId,
						ProductId = cartItem.Product.ProductId,
						ProductName = cartItem.Product.ProductName,
						
						UnitPrice = cartItem.Product.Price,

						Quantity = cartItem.Quantity
					});
				}
			}

			var user = await _context.User
			.FindAsync(currentUserId);

			var checkoutDto = new CheckoutDto
			{
				Items = checkoutItems,
				TotalPrice = checkoutItems.Sum(i =>
					i.UnitPrice * i.Quantity),
				ShippingAddress = user?.Address ?? string.Empty
			};

			return View(checkoutDto);
		}

		// =========================================================
		// CHECKOUT - POST
		// =========================================================

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Checkout(
			CheckoutDto checkoutDto)
		{
			if (!TryGetCurrentUserId(out int currentUserId))
			{
				return RedirectToLogin();
			}

			if (checkoutDto.Items == null ||
				!checkoutDto.Items.Any(i => i.IsSelected))
			{
				TempData["ErrorMessage"] =
					"No items selected for checkout.";

				return RedirectToAction(nameof(Cart));
			}

			var selectedItems = checkoutDto.Items
				.Where(i => i.IsSelected)
				.ToList();

			var rebuiltItems = new List<CheckoutItemDto>();

			foreach (var postedItem in selectedItems)
			{
				if (postedItem.Quantity <= 0)
				{
					TempData["ErrorMessage"] =
						"Invalid product quantity.";

					return RedirectToAction(nameof(Cart));
				}

				ProductModel? product = null;

				if (postedItem.CartId.HasValue)
				{
					var cartItem = await _context.Cart
						.Include(c => c.Product)
						.FirstOrDefaultAsync(c =>
							c.CartId == postedItem.CartId.Value &&
							c.UserId == currentUserId);

					if (cartItem == null ||
						cartItem.Product == null)
					{
						TempData["ErrorMessage"] =
							"One of the selected cart items is no longer available.";

						return RedirectToAction(nameof(Cart));
					}

					product = cartItem.Product;
					postedItem.Quantity = cartItem.Quantity;
				}

				else
				{
					product = await _context.Product
						.FirstOrDefaultAsync(p =>
							p.ProductId == postedItem.ProductId);
				}

				if (product == null)
				{
					TempData["ErrorMessage"] =
						"One of the selected products no longer exists.";

					return RedirectToAction(nameof(Cart));
				}

				if (product.SellerId == currentUserId)
				{
					TempData["ErrorMessage"] =
						"You cannot purchase your own listing.";

					return RedirectToAction(nameof(Cart));
				}


				if (postedItem.Quantity <= 0 ||
					postedItem.Quantity > product.Stock)
				{
					TempData["ErrorMessage"] =
						$"Not enough stock available for {product.ProductName}.";

					return RedirectToAction(nameof(Cart));
				}

			
				rebuiltItems.Add(new CheckoutItemDto
				{
					CartId = postedItem.CartId,
					ProductId = product.ProductId,

					ProductName = product.ProductName,

					UnitPrice = product.Price,

					Quantity = postedItem.Quantity,
					IsSelected = true
				});
			}

			checkoutDto.Items = rebuiltItems;

			checkoutDto.TotalPrice = rebuiltItems.Sum(i =>
				i.UnitPrice * i.Quantity);

			if (string.IsNullOrWhiteSpace(
				checkoutDto.ShippingAddress))
			{
				var user = await _context.User
					.FindAsync(currentUserId);

				if (user != null)
				{
					checkoutDto.ShippingAddress = user.Address;
				}
			}

			return View("Checkout", checkoutDto);
		}

		// =========================================================
		// REVIEW ORDER
		// =========================================================

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> ReviewOrder(
			CheckoutDto checkoutDto)
		{
			if (!TryGetCurrentUserId(out int currentUserId))
			{
				return RedirectToLogin();
			}

			if (checkoutDto.Items == null ||
				!checkoutDto.Items.Any())
			{
				TempData["ErrorMessage"] =
					"No items selected for review.";

				return RedirectToAction(nameof(Cart));
			}

			if (string.IsNullOrWhiteSpace(
				checkoutDto.ShippingAddress))
			{
				ModelState.AddModelError(
					"",
					"Please enter a valid shipping address.");
			}

			if (string.IsNullOrWhiteSpace(
				checkoutDto.SelectedPaymentMethod))
			{
				ModelState.AddModelError(
					"",
					"Please select a payment method.");
			}

			if (!ModelState.IsValid)
			{
				return View("Checkout", checkoutDto);
			}

			var rebuiltItems = new List<CheckoutItemDto>();

			foreach (var postedItem in checkoutDto.Items)
			{
				if (postedItem.Quantity <= 0)
				{
					ModelState.AddModelError(
						"",
						"Invalid product quantity.");

					continue;
				}

				ProductModel? product = null;

				if (postedItem.CartId.HasValue)
				{
					var cartItem = await _context.Cart
						.Include(c => c.Product)
						.FirstOrDefaultAsync(c =>
							c.CartId == postedItem.CartId.Value &&
							c.UserId == currentUserId);

					if (cartItem == null ||
						cartItem.Product == null)
					{
						ModelState.AddModelError(
							"",
							"A selected cart item is no longer available.");

						continue;
					}

					product = cartItem.Product;
					postedItem.Quantity = cartItem.Quantity;
				}
				else
				{
					product = await _context.Product
						.FirstOrDefaultAsync(p =>
							p.ProductId == postedItem.ProductId);
				}

				if (product == null)
				{
					ModelState.AddModelError(
						"",
						"A selected product no longer exists.");

					continue;
				}

				if (product.SellerId == currentUserId)
				{
					ModelState.AddModelError(
						"",
						$"You cannot purchase your own listing: {product.ProductName}.");

					continue;
				}

				if (postedItem.Quantity > product.Stock)
				{
					ModelState.AddModelError(
						"",
						$"Not enough stock available for {product.ProductName}.");

					continue;
				}

				rebuiltItems.Add(new CheckoutItemDto
				{
					CartId = postedItem.CartId,
					ProductId = product.ProductId,
					ProductName = product.ProductName,
					UnitPrice = product.Price,
					Quantity = postedItem.Quantity,
					IsSelected = true
				});
			}

			if (!ModelState.IsValid)
			{
				checkoutDto.Items = rebuiltItems;

				checkoutDto.TotalPrice = rebuiltItems.Sum(i =>
					i.UnitPrice * i.Quantity);

				return View("Checkout", checkoutDto);
			}

			checkoutDto.Items = rebuiltItems;

			checkoutDto.TotalPrice = rebuiltItems.Sum(i =>
				i.UnitPrice * i.Quantity);

			TempData["PendingCheckout"] =
				JsonSerializer.Serialize(checkoutDto);

			return View(checkoutDto);
		}

		// =========================================================
		// CONFIRM PAYMENT / CREATE ORDER
		// =========================================================

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> ConfirmPayment()
		{
			if (!TryGetCurrentUserId(out int currentUserId))
			{
				return RedirectToLogin();
			}

			var rawData = TempData.Peek("PendingCheckout") as string;

			if (string.IsNullOrEmpty(rawData))
			{
				return RedirectToAction(nameof(Cart));
			}

			var model = JsonSerializer.Deserialize<CheckoutDto>(
				rawData);

			if (model == null ||
				model.Items == null ||
				!model.Items.Any())
			{
				return RedirectToAction(nameof(Cart));
			}

			var trustedItems = new List<CheckoutItemDto>();

			foreach (var postedItem in model.Items)
			{
				if (postedItem.Quantity <= 0)
				{
					TempData["ErrorMessage"] =
						"Invalid product quantity.";

					return RedirectToAction(nameof(Cart));
				}

				ProductModel? product = null;

				if (postedItem.CartId.HasValue)
				{
					var cartItem = await _context.Cart
						.Include(c => c.Product)
						.FirstOrDefaultAsync(c =>
							c.CartId == postedItem.CartId.Value &&
							c.UserId == currentUserId);

					if (cartItem == null ||
						cartItem.Product == null)
					{
						TempData["ErrorMessage"] =
							"A selected cart item is no longer available.";

						return RedirectToAction(nameof(Cart));
					}

					product = cartItem.Product;

					postedItem.Quantity = cartItem.Quantity;
				}
				else
				{
					product = await _context.Product
						.FirstOrDefaultAsync(p =>
							p.ProductId == postedItem.ProductId);
				}

				if (product == null)
				{
					TempData["ErrorMessage"] =
						"A selected product no longer exists.";

					return RedirectToAction(nameof(Cart));
				}

				if (product.SellerId == currentUserId)
				{
					TempData["ErrorMessage"] =
						"You cannot purchase your own listing.";

					return RedirectToAction(nameof(Cart));
				}

				if (postedItem.Quantity > product.Stock)
				{
					TempData["ErrorMessage"] =
						$"Not enough stock available for {product.ProductName}.";

					return RedirectToAction(nameof(Cart));
				}

				trustedItems.Add(new CheckoutItemDto
				{
					CartId = postedItem.CartId,
					ProductId = product.ProductId,
					ProductName = product.ProductName,
					UnitPrice = product.Price,
					Quantity = postedItem.Quantity,
					IsSelected = true
				});
			}

			if (!trustedItems.Any())
			{
				TempData["ErrorMessage"] =
					"No valid items remain for checkout.";

				return RedirectToAction(nameof(Cart));
			}


			decimal totalPrice = trustedItems.Sum(i =>
				i.UnitPrice * i.Quantity);

			var order = new OrderModel
			{
				CustomerId = currentUserId,
				OrderDate = DateTime.UtcNow,
				ShippingAddress = model.ShippingAddress,
				PaymentMethod = model.SelectedPaymentMethod,
				Status = model.SelectedPaymentMethod == "COD"
					? "Processing"
					: "Unpaid",
				TotalPrice = totalPrice,

				Details = trustedItems.Select(i =>
					new OrderDetails
					{
						ProductId = i.ProductId,
						Quantity = i.Quantity,
						UnitPrice = i.UnitPrice
					}).ToList()
			};

			_context.Order.Add(order);

			foreach (var item in trustedItems)
			{
				var product = await _context.Product
					.FirstOrDefaultAsync(p =>
						p.ProductId == item.ProductId);

				if (product == null)
				{
					TempData["ErrorMessage"] =
						"A product could not be found.";

					return RedirectToAction(nameof(Cart));
				}

				if (item.Quantity > product.Stock)
				{
					TempData["ErrorMessage"] =
						$"Not enough stock available for {product.ProductName}.";

					return RedirectToAction(nameof(Cart));
				}

				product.Stock -= item.Quantity;
			}

			var cartIdsToRemove = trustedItems
				.Where(i => i.CartId.HasValue)
				.Select(i => i.CartId!.Value)
				.ToList();

			if (cartIdsToRemove.Any())
			{
				var cartItems = await _context.Cart
					.Where(c =>
						cartIdsToRemove.Contains(c.CartId) &&
						c.UserId == currentUserId)
					.ToListAsync();

				_context.Cart.RemoveRange(cartItems);
			}

			await _context.SaveChangesAsync();

			TempData.Remove("PendingCheckout");

			if (model.SelectedPaymentMethod == "PayMongo")
			{
				var lineItems = trustedItems
					.Select(i => new PayMongoLineItem
					{
						Name = i.ProductName,

						
						Amount = (int)Math.Round(
							i.UnitPrice * 100,
							MidpointRounding.AwayFromZero),

						Currency = "PHP",
						Quantity = i.Quantity
					})
					.ToList();

				var checkoutSession = await CreatePayMongoCheckoutSessionAsync( order.OrderId, lineItems);

				if (!string.IsNullOrEmpty(checkoutSession.CheckoutUrl))
				{
					order.PayMongoCheckoutId = checkoutSession.CheckoutId;

					await _context.SaveChangesAsync();

					return Redirect(checkoutSession.CheckoutUrl);
				}

				TempData["ErrorMessage"] =
					"Unable to initialize the PayMongo payment gateway.";

				return RedirectToAction(
					nameof(OrderConfirmation),
					new { id = order.OrderId });
			}

			return RedirectToAction(
				nameof(OrderConfirmation),
				new { id = order.OrderId });
		}

		// =========================================================
		// ORDER CONFIRMATION
		// =========================================================

		[HttpGet]
		public async Task<IActionResult> OrderConfirmation(int id)
		{
			if (!TryGetCurrentUserId(out int currentUserId))
			{
				return RedirectToLogin();
			}

			var order = await _context.Order
				.Include(o => o.Details)
					.ThenInclude(d => d.Product)
				.FirstOrDefaultAsync(o =>
					o.OrderId == id &&
					o.CustomerId == currentUserId);

			if (order == null)
			{
				return NotFound();
			}

			return View(order);
		}

		// =========================================================
		// CUSTOMER ORDERS
		// =========================================================

		[HttpGet]
		public async Task<IActionResult> MyOrders()
		{
			if (!TryGetCurrentUserId(out int currentUserId))
			{
				return RedirectToLogin();
			}

			var orders = await _context.Order
				.Include(o => o.Details)
					.ThenInclude(d => d.Product)
				.Where(o => o.CustomerId == currentUserId)
				.OrderByDescending(o => o.OrderDate)
				.ToListAsync();

			return View(orders);
		}

		// =========================================================
		// MARK ORDER AS RECEIVED
		// =========================================================

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> MarkAsReceived(
			int orderId)
		{
			if (!TryGetCurrentUserId(out int currentUserId))
			{
				return RedirectToLogin();
			}

			var order = await _context.Order
				.FirstOrDefaultAsync(o =>
					o.OrderId == orderId &&
					o.CustomerId == currentUserId);

			if (order == null)
			{
				return NotFound();
			}

			if (order.Status != "Shipped")
			{
				TempData["ErrorMessage"] =
					"Only shipped orders can be marked as received.";

				return RedirectToAction(nameof(MyOrders));
			}

			order.Status = "Received";

			await _context.SaveChangesAsync();

			TempData["SuccessMessage"] =
				"Order status updated to Received!";

			return RedirectToAction(nameof(MyOrders));
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> CancelOrder(
			int orderId)
		{
			if (!TryGetCurrentUserId(out int currentUserId))
			{
				return RedirectToLogin();
			}

			var order = await _context.Order
				.Include(o => o.Details)
					.ThenInclude(d => d.Product)
				.FirstOrDefaultAsync(o =>
					o.OrderId == orderId &&
					o.CustomerId == currentUserId);

			if (order == null)
			{
				return NotFound();
			}

			if (order.Status != "Unpaid" &&
				order.Status != "Processing")
			{
				TempData["ErrorMessage"] =
					$"Order #{orderId} cannot be cancelled because it is already {order.Status.ToLower()}.";

				return RedirectToAction(nameof(MyOrders));
			}

			foreach (var detail in order.Details)
			{
				if (detail.Product != null)
				{
					detail.Product.Stock += detail.Quantity;
				}
			}

			order.Status = "Cancelled";

			await _context.SaveChangesAsync();

			TempData["SuccessMessage"] =
				$"Order #{orderId} has been successfully cancelled.";

			return RedirectToAction(nameof(MyOrders));
		}

		// =========================================================
		// SELLER SALES
		// =========================================================

		[HttpGet]
		public async Task<IActionResult> MySales()
		{
			if (!TryGetCurrentUserId(out int currentUserId))
			{
				return RedirectToLogin();
			}

			var sales = await _context.Order
				.Include(o => o.Customer)
				.Include(o => o.Details)
					.ThenInclude(d => d.Product)
				.Where(o =>
					o.Details.Any(d =>
						d.Product != null &&
						d.Product.SellerId == currentUserId))
				.OrderByDescending(o => o.OrderDate)
				.ToListAsync();

			return View("MySales", sales);
		}

		// =========================================================
		// SELLER - UPDATE TRACKING
		// =========================================================

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> UpdateTrackingInfo(
			int orderId,
			string trackingNumber)
		{
			if (!TryGetCurrentUserId(out int currentUserId))
			{
				return RedirectToLogin();
			}

			if (string.IsNullOrWhiteSpace(trackingNumber))
			{
				TempData["ErrorMessage"] =
					"Tracking number cannot be empty.";

				return RedirectToAction(nameof(MySales));
			}

			var order = await _context.Order
				.Include(o => o.Details)
					.ThenInclude(d => d.Product)
				.FirstOrDefaultAsync(o =>
					o.OrderId == orderId);

			if (order == null)
			{
				TempData["ErrorMessage"] =
					"Order not found.";

				return RedirectToAction(nameof(MySales));
			}

			bool isSeller = order.Details.Any(d =>
				d.Product != null &&
				d.Product.SellerId == currentUserId);

			if (!isSeller)
			{
				return Forbid();
			}

			if (order.Status == "Cancelled" ||
				order.Status == "Received")
			{
				TempData["ErrorMessage"] =
					$"Order cannot be shipped because it is already {order.Status.ToLower()}.";

				return RedirectToAction(nameof(MySales));
			}

			order.TrackingNumber = trackingNumber.Trim();
			order.Status = "Shipped";

			await _context.SaveChangesAsync();

			TempData["SuccessMessage"] =
				$"Order #{orderId} updated to Shipped with tracking #{order.TrackingNumber}.";

			return RedirectToAction(nameof(MySales));
		}

		// =========================================================
		// SELLER - CANCEL ORDER
		// =========================================================

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> CancelSalesOrder(
			int orderId)
		{
			if (!TryGetCurrentUserId(out int currentUserId))
			{
				return RedirectToLogin();
			}

			var order = await _context.Order
				.Include(o => o.Details)
					.ThenInclude(d => d.Product)
				.FirstOrDefaultAsync(o =>
					o.OrderId == orderId);

			if (order == null)
			{
				TempData["ErrorMessage"] =
					"Order not found.";

				return RedirectToAction(nameof(MySales));
			}

			bool isSeller = order.Details.Any(d =>
				d.Product != null &&
				d.Product.SellerId == currentUserId);

			if (!isSeller)
			{
				return Forbid();
			}

			if (order.Status == "Cancelled" ||
				order.Status == "Completed" ||
				order.Status == "Received")
			{
				TempData["ErrorMessage"] =
					$"Order cannot be cancelled because it is already {order.Status.ToLower()}.";

				return RedirectToAction(nameof(MySales));
			}

			
			foreach (var detail in order.Details.Where(d =>
				d.Product != null &&
				d.Product.SellerId == currentUserId))
			{
				detail.Product!.Stock += detail.Quantity;
			}

			order.Status = "Cancelled";

			await _context.SaveChangesAsync();

			TempData["SuccessMessage"] =
				$"Order #{order.OrderId} has been successfully cancelled.";

			return RedirectToAction(nameof(MySales));
		}

		// =========================================================
		// PAYMONGO
		// =========================================================

		private async Task<(string? CheckoutId, string? CheckoutUrl)> CreatePayMongoCheckoutSessionAsync(int orderId, List<PayMongoLineItem> lineItems)
		{
			var client =
				_httpClientFactory.CreateClient("PayMongo");

			var requestPayload = new PayMongoCheckoutRequest
			{
				Data = new PayMongoCheckoutData
				{
					Attributes = new PayMongoCheckoutAttributes
					{
						LineItems = lineItems,

						PaymentMethodTypes = new List<string>
				{
					"gcash",
					"paymaya",
					"card",
					"dob"
				},

						SuccessUrl = Url.Action(
							"OrderConfirmation",
							"Purchasing",
							new { id = orderId },
							Request.Scheme),

						CancelUrl = Url.Action(
							"Cart",
							"Purchasing",
							null,
							Request.Scheme)
					}
				}
			};

			var json =
				JsonSerializer.Serialize(requestPayload);

			var jsonContent = new StringContent(
				json,
				Encoding.UTF8,
				"application/json");

			var response = await client.PostAsync(
				"checkout_sessions",
				jsonContent);

			if (!response.IsSuccessStatusCode)
			{
				return (null, null);
			}

			var responseStream =
				await response.Content.ReadAsStreamAsync();

			var result =
				await JsonSerializer.DeserializeAsync<PayMongoCheckoutResponse>(
					responseStream);

			return (
				result?.Data?.Id,
				result?.Data?.Attributes?.CheckoutUrl
			);
		}

		private async Task<(string? CheckoutId, string? CheckoutUrl, string? Status)> GetPayMongoCheckoutSessionAsync(string checkoutId)
		{
			var client =
				_httpClientFactory.CreateClient("PayMongo");

			var response = await client.GetAsync(
				$"checkout_sessions/{checkoutId}");

			if (!response.IsSuccessStatusCode)
			{
				return (null, null, null);
			}

			var responseStream =
				await response.Content.ReadAsStreamAsync();

			var result =
				await JsonSerializer.DeserializeAsync<PayMongoCheckoutResponse>(
					responseStream);

			return (
				result?.Data?.Id,
				result?.Data?.Attributes?.CheckoutUrl,
				result?.Data?.Attributes?.Status
			);
		}
	}
}

