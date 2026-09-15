using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OnlineMarketplace.Models
{
	public class OrderModel
	{
		[Key]
		public int OrderId { get; set; }

		public int CustomerId { get; set; }
		[ForeignKey("CustomerId")]
		public ApplicationUserModel? Customer { get; set; }

		public string ShippingAddress { get; set; } = string.Empty;

		public string PaymentMethod { get; set; } = string.Empty;

		public string? PayMongoCheckoutId { get; set; }

		public DateTime OrderDate { get; set; } = DateTime.UtcNow;
		public string Status { get; set; } = "Unpaid"; 

		public ICollection<OrderDetails> Details { get; set; } = new List<OrderDetails>();

		public decimal TotalPrice { get; set; }

		public string? TrackingNumber { get; set; }

	}
}
