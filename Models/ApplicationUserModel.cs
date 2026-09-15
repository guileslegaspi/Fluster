
using System.ComponentModel.DataAnnotations;

namespace OnlineMarketplace.Models
{
	public class ApplicationUserModel
	{
		[Key]
		public int UserId { get; set; }

		[Required]
		[StringLength(255)]
		public string? UserName { get; set; }

		[Required]
		[EmailAddress]
		public string EmailAddress {  get; set; } 

		[Required]
		[StringLength(500)]
		[DataType(DataType.Password)]
		public string PasswordHash { get; set; }

		[StringLength(100)]
		public string? FirstName { get; set; }

		[StringLength(100)]
		public string? LastName { get; set; }

		[StringLength(100)]
		public string? Address {  get; set; }

		[Phone]
		public string? PhoneNumber { get; set; } = string.Empty;

		public byte[]? ProfilePicture { get; set; }

		[StringLength (100)]
		public string? ProfilePictureContentType { get; set; }
		
	}
}
