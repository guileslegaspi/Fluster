using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OnlineMarketplace.Models
{
	public class ProductModel
	{
		[Key]
		public int ProductId { get; set; }

		[Required]
		[StringLength(100)]
		public string ProductName { get; set; } = string.Empty;

		[Required]
		public string ProductDescription { get; set; } = string.Empty;

		[Range(0, int.MaxValue, ErrorMessage = "Stock must not be negative!")]
		public int Stock { get; set; }

		[Required]
		[Range(0.01, 1000000.00)]
		[Column(TypeName = "decimal(18,2)")]
		public decimal Price { get; set; }

		public string Image { get; set; } = string.Empty;

		[NotMapped]
		public IFormFile? ImageFile { get; set; }

		[Required(ErrorMessage = "Please select a category.")]
		public string Category { get; set; } = string.Empty;

		public int SellerId { get; set; }
		[ForeignKey("SellerId")]
		public ApplicationUserModel? Seller { get; set; }

	}
}
