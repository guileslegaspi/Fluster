using System.Text.Json.Serialization;

namespace OnlineMarketplace.Models
{
	public class PayMongoCheckoutRequest
	{
		[JsonPropertyName("data")]
		public PayMongoCheckoutData Data { get; set; } = new();
	}

	public class PayMongoCheckoutData
	{
		[JsonPropertyName("attributes")]
		public PayMongoCheckoutAttributes Attributes { get; set; } = new();
	}

	public class PayMongoCheckoutAttributes
	{
		[JsonPropertyName("line_items")]
		public List<PayMongoLineItem> LineItems { get; set; } = new();

		[JsonPropertyName("payment_method_types")]
		public List<string> PaymentMethodTypes { get; set; } = new();

		[JsonPropertyName("success_url")]
		public string? SuccessUrl { get; set; }

		[JsonPropertyName("cancel_url")]
		public string? CancelUrl { get; set; }
	}

	public class PayMongoLineItem
	{
		[JsonPropertyName("amount")]
		public int Amount { get; set; }

		[JsonPropertyName("currency")]
		public string Currency { get; set; } = "PHP";

		[JsonPropertyName("name")]
		public string Name { get; set; } = string.Empty;

		[JsonPropertyName("quantity")]
		public int Quantity { get; set; }
	}

	public class PayMongoCheckoutResponse
	{
		[JsonPropertyName("data")]
		public PayMongoResponseData? Data { get; set; }
	}

	public class PayMongoResponseData
	{
		[JsonPropertyName("id")]
		public string? Id { get; set; }

		[JsonPropertyName("attributes")]
		public PayMongoResponseAttributes? Attributes { get; set; }
	}

	
	public class PayMongoResponseAttributes
	{
		[JsonPropertyName("checkout_url")]
		public string? CheckoutUrl { get; set; }

		[JsonPropertyName("status")]
		public string? Status { get; set; }
	}
}