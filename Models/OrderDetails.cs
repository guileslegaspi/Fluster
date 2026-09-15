using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OnlineMarketplace.Models
{
	public class OrderDetails
	{
		[Key]
		public int OrderDetailsId { get; set; }

		
		public int OrderId { get; set; }
		[ForeignKey("OrderId")]
		public OrderModel? Order { get; set; }

		
		public int ProductId { get; set; }
		[ForeignKey("ProductId")]
		public ProductModel? Product { get; set; }

		public int Quantity { get; set; }
		[Column(TypeName = "decimal(18, 2)")]
		public decimal UnitPrice { get; set; }

		[NotMapped]
		public decimal LineTotal => Quantity * UnitPrice;
	}
}
