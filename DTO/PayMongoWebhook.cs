using System.Text.Json.Serialization;

namespace OnlineMarketplace.DTO
{
	public class PayMongoWebhook
	{
		[JsonPropertyName("data")]
		public PayMongoWebhookData? Data { get; set; }
	}

	public class PayMongoWebhookData
	{
		[JsonPropertyName("attributes")]
		public PayMongoWebhookAttributes? Attributes { get; set; }
	}

	public class PayMongoWebhookAttributes
	{
		[JsonPropertyName("type")]
		public string? Type { get; set; }

		[JsonPropertyName("data")]
		public PayMongoWebhookResource? Data { get; set; }
	}

	public class PayMongoWebhookResource
	{
		[JsonPropertyName("id")]
		public string? Id { get; set; }
	}
}