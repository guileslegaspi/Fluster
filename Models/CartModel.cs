using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OnlineMarketplace.Models
{
	public class CartModel
	{
		[Key]
		public int CartId { get; set; }

		public int UserId {  get; set; }

		public int ProductId { get; set; }
		[ForeignKey("ProductId")]
		public ProductModel? Product { get; set; }

		public int Quantity { get; set; } 

		public bool CheckoutBox { get; set; }
	}
}
