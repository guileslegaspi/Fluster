using System.ComponentModel.DataAnnotations;

namespace OnlineMarketplace.DTO
{
	public class ProfileViewDto
	{
		public int UserId { get; set; }
		public string? UserName { get; set; }
		public string EmailAddress { get; set; } = string.Empty;
		public string? FirstName { get; set; }
		public string? LastName { get; set; }
		public string? Address { get; set; }
		public string? PhoneNumber { get; set; }

		public string? ExistingProfilePictureBase64 { get; set; }
		public IFormFile? ProfileImageFile { get; set; }
		public bool DeleteProfilePicture { get; set; }
	}
}
