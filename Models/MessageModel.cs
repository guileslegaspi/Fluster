using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OnlineMarketplace.Models
{
	public class MessageModel
	{
		[Key]
		public int MessageId { get; set; }

		[Required]
		public int SenderId { get; set; }

		[Required]
		public int ReceiverId { get; set; }

		public int? ProductId { get; set; }

		[Required]
		public string Content { get; set; } = string.Empty;

		public DateTime SentAt { get; set; } = DateTime.UtcNow;

		public bool IsRead { get; set; } = false;

		[ForeignKey("SenderId")]
		public virtual ApplicationUserModel? Sender { get; set; }

		[ForeignKey("ReceiverId")]
		public virtual ApplicationUserModel? Receiver { get; set; }

		[ForeignKey("ProductId")]
		public virtual ProductModel? Product { get; set; }
	}
}
