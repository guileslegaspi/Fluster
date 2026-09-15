namespace OnlineMarketplace.DTO
{
	public class CheckoutItemDto
	{
		public int? CartId { get; set; }
		public int ProductId { get; set; }
		public string ProductName { get; set; } = string.Empty;
		
		public decimal UnitPrice { get; set; }
		public int Quantity { get; set; }

		public bool IsSelected { get; set; }
		public decimal LineTotal => Quantity * UnitPrice;
	}
}
