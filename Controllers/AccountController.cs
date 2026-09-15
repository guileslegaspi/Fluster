using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineMarketplace.Database;
using OnlineMarketplace.DTO;
using OnlineMarketplace.Models;
using System.ComponentModel.DataAnnotations;

namespace OnlineMarketplace.Controllers
{
	public class AccountController : Controller
	{
		private readonly ApplicationDbContext _context;
		private readonly IPasswordHasher<ApplicationUserModel> _passwordHasher;

		public AccountController(ApplicationDbContext context, IPasswordHasher<ApplicationUserModel> passwordHasher)
		{
			_context = context;
			_passwordHasher = passwordHasher;
		}
		[HttpGet]
		public IActionResult Register()
		{
			return View();
		}

		[HttpPost]
		public async Task<IActionResult> Register(ApplicationUserModel applicationUserModel)
		{
			if (ModelState.IsValid)
			{
				var existingUser = await _context.User.FirstOrDefaultAsync(x => x.EmailAddress == applicationUserModel.EmailAddress);
				if (existingUser != null)
				{
					ModelState.AddModelError("", "Email already registered");
					return View(applicationUserModel);
				}

				applicationUserModel.PasswordHash = _passwordHasher.HashPassword(applicationUserModel, applicationUserModel.PasswordHash);

				_context.User.Add(applicationUserModel);
				await _context.SaveChangesAsync();

				return RedirectToAction("Login");


			}
			return View(applicationUserModel);
		}

		[HttpGet]
		public IActionResult Login()
		{
			return View();
		}

		[HttpPost]
		public IActionResult Login(string email, string password)
		{
			var user = _context.User.FirstOrDefault(x => x.EmailAddress == email);

			if (user != null)
			{
				var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);

				if (result == PasswordVerificationResult.Success)
				{
					HttpContext.Session.SetString("UserId", user.UserId.ToString());
					HttpContext.Session.SetString("UserName", user.UserName ?? "");

					if (user.ProfilePicture != null && user.ProfilePictureContentType != null)
					{
						string base64 = $"data:{user.ProfilePictureContentType};base64,{Convert.ToBase64String(user.ProfilePicture)}";
						HttpContext.Session.SetString("UserProfilePic", base64);
					}
					else
					{
						HttpContext.Session.Remove("UserProfilePic");
					}

					return RedirectToAction("Index", "Home");
				}
			}

			ModelState.AddModelError("", "Invalid login attempt");
			return View();
		}

		[HttpGet]
		public IActionResult Logout()
		{
			HttpContext.Session.Clear();
			return RedirectToAction("Index", "Home");
		}

		[HttpGet]
		public async Task<IActionResult> Profile()
		{
			var currentUserId = HttpContext.Session.GetString("UserId");

			if (string.IsNullOrEmpty(currentUserId) || !int.TryParse(currentUserId, out int userId))
			{
				return RedirectToAction(nameof(Login));
			}

			var user = await _context.User.FindAsync(userId);
			if (user == null)
			{
				return NotFound();
			}

			string? base64Image = null;
			if (user.ProfilePicture != null && user.ProfilePictureContentType != null)
			{
				base64Image = $"data:{user.ProfilePictureContentType};base64,{Convert.ToBase64String(user.ProfilePicture)}";
			}

			var viewModel = new ProfileViewDto
			{
				UserId = user.UserId,
				UserName = user.UserName,
				FirstName = user.FirstName,
				LastName = user.LastName,
				EmailAddress = user.EmailAddress,
				PhoneNumber = user.PhoneNumber,
				Address = user.Address, 
				ExistingProfilePictureBase64 = base64Image
			};

			return View(viewModel);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Profile(ProfileViewDto profileViewDto)
		{
			var currentUserId = HttpContext.Session.GetString("UserId");

			if (string.IsNullOrEmpty(currentUserId) || !int.TryParse(currentUserId, out int userId))
			{
				return RedirectToAction("Login", "Account");
			}

			var user = await _context.User.FindAsync(userId);
			if (user == null)
			{
				return NotFound();
			}

			if (ModelState.IsValid)
			{
				user.FirstName = profileViewDto.FirstName;
				user.LastName = profileViewDto.LastName;
				user.Address = profileViewDto.Address;
				user.PhoneNumber = profileViewDto.PhoneNumber;

				
				if (profileViewDto.DeleteProfilePicture)
				{
					user.ProfilePicture = null;
					user.ProfilePictureContentType = null;
				}
				
				else if (profileViewDto.ProfileImageFile != null && profileViewDto.ProfileImageFile.Length > 0)
				{
					using (var memoryStream = new MemoryStream())
					{
						await profileViewDto.ProfileImageFile.CopyToAsync(memoryStream);
						user.ProfilePicture = memoryStream.ToArray();
						user.ProfilePictureContentType = profileViewDto.ProfileImageFile.ContentType;
					}
				}

				_context.User.Update(user);
				await _context.SaveChangesAsync();

				
				if (user.ProfilePicture != null && user.ProfilePictureContentType != null)
				{
					string base64 = $"data:{user.ProfilePictureContentType};base64,{Convert.ToBase64String(user.ProfilePicture)}";
					HttpContext.Session.SetString("UserProfilePic", base64);
				}
				else
				{
					HttpContext.Session.Remove("UserProfilePic");
				}

				return RedirectToAction("Profile");
			}

			return View(profileViewDto);
		}


	}
}
