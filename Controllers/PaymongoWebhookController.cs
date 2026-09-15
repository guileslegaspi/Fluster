using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineMarketplace.Database;
using OnlineMarketplace.DTO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace OnlineMarketplace.Controllers
{
	[ApiController]
	[Route("webhooks/paymongo")]
	public class PaymongoWebhookController : Controller
	{
		private readonly IConfiguration _configuration;
		private readonly ApplicationDbContext _context;

		public PaymongoWebhookController(IConfiguration configuration, ApplicationDbContext context)
		{
			_configuration = configuration;
			_context = context;
		}

		[HttpPost]
		public async Task<IActionResult> HandleWebhook() 
		{
			var siginatureHeader = Request.Headers["Paymongo-Signature"].FirstOrDefault();

			if(string.IsNullOrEmpty(siginatureHeader))
			{
				return Unauthorized();
			}

			using var reader = new StreamReader(Request.Body);

			var rawBody = await reader.ReadToEndAsync();

			var webhookSecret = _configuration["Paymongo:WebhookSecret"];

			if (string.IsNullOrEmpty(webhookSecret)) 
			{
				return StatusCode(500);
			}

			var signatureParts = siginatureHeader.Split(',');

			string? timestamp = null;
			string? testSignature = null;

			foreach(var part in signatureParts) 
			{
				var pieces = part.Split('=', 2); ;

				if(pieces.Length != 2) 
				{
					continue;
				}

				if (pieces[0] == "t")
				{
					timestamp = pieces[1];
				}
				else if (pieces[0] == "te")
				{
					testSignature = pieces[1];
				}
			}

			if(string.IsNullOrEmpty(timestamp) || string.IsNullOrEmpty(testSignature)) 
			{
				return Unauthorized();
			}

			var signedpayload = $"{timestamp}.{rawBody}";

			using var hmac =
				new HMACSHA256(Encoding.UTF8.GetBytes(webhookSecret));

			var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(signedpayload));

			var computedSignature = Convert.ToHexString(computedHash).ToLowerInvariant();

			if (!CryptographicOperations.FixedTimeEquals(
					Encoding.UTF8.GetBytes(computedSignature),
					Encoding.UTF8.GetBytes(testSignature)))
			{
				return Unauthorized();
			}

			var webhook = JsonSerializer.Deserialize<PayMongoWebhook>(rawBody);

			if (webhook == null)
			{
				return BadRequest();
			}

	
			if (webhook.Data?.Attributes?.Type != "checkout_session.payment.paid")
			{
				return Ok(new
				{
					received = true
				});
			}

			var checkoutSessionId = webhook.Data?.Attributes?.Data?.Id;

			if (string.IsNullOrEmpty(checkoutSessionId))
			{
				return BadRequest();
			}

			var order = await _context.Order.FirstOrDefaultAsync(o => o.PayMongoCheckoutId == checkoutSessionId);

			if (order == null)
			{
				return NotFound();
			}

			order.Status = "Paid";

			await _context.SaveChangesAsync();

			return Ok(new
			{
				received = true,
				checkoutSessionId
			});
		}
		
	}
}
