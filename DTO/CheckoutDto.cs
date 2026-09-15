namespace OnlineMarketplace.DTO
{
	public class CheckoutDto
	{
		public List<CheckoutItemDto> Items { get; set; } = new();

		public decimal TotalPrice { get; set; }

		public string ShippingAddress { get; set; } = string.Empty;
		public string SelectedPaymentMethod { get; set; } = string.Empty;
	}
	
}
