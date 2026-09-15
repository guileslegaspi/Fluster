using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using OnlineMarketplace.Database;
using OnlineMarketplace.Hubs;
using OnlineMarketplace.Models;

namespace OnlineMarketplace.Controllers
{
	public class ChatController : Controller
	{
		private readonly ApplicationDbContext _context;
		private readonly IHubContext<ChatHub> _chatHubContext;

		public ChatController(ApplicationDbContext context, IHubContext<ChatHub> chatHubContext)
		{
			_context = context;
			_chatHubContext = chatHubContext;
		}

		[HttpPost]
		[IgnoreAntiforgeryToken]
		public async Task<IActionResult> SendMessage([FromForm] int receiverId, [FromForm] int? productId, [FromForm] string messageText)
		{
			var senderIdStr = HttpContext.Session.GetString("UserId");
			if (string.IsNullOrEmpty(senderIdStr))
			{
				return Unauthorized(new { success = false, message = "User not logged in" });
			}

			if (string.IsNullOrWhiteSpace(messageText))
			{
				return BadRequest(new { success = false, message = "Message content empty" });
			}

			int senderId = int.Parse(senderIdStr);

			var message = new MessageModel
			{
				SenderId = senderId,
				ReceiverId = receiverId,
				ProductId = productId,
				Content = messageText,
				SentAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc),
				IsRead = false
			};

			_context.Messages.Add(message);
			await _context.SaveChangesAsync();

			
			await _chatHubContext.Clients.All.SendAsync("ReceiveMessage", new
			{
				senderId = senderId,
				receiverId = receiverId,
				content = messageText,
				sentAt = message.SentAt
			});

			return Json(new { success = true });
		}

		[HttpGet]
		public async Task<IActionResult> GetConversations()
		{
			var userIdStr = HttpContext.Session.GetString("UserId");
			if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();

			int userId = int.Parse(userIdStr);

			var messages = await _context.Messages
				.Include(m => m.Sender)
				.Include(m => m.Receiver)
				.Where(m => m.SenderId == userId || m.ReceiverId == userId)
				.OrderByDescending(m => m.SentAt)
				.ToListAsync();

			var conversations = messages.Select(m => {
				var otherUser = m.SenderId == userId ? m.Receiver : m.Sender;

				string? avatarUrl = null;
				if (otherUser?.ProfilePicture != null && otherUser.ProfilePicture.Length > 0)
				{
					string contentType = !string.IsNullOrEmpty(otherUser.ProfilePictureContentType)
						? otherUser.ProfilePictureContentType
						: "image/png";
					avatarUrl = $"data:{contentType};base64,{Convert.ToBase64String(otherUser.ProfilePicture)}";
				}

				string displayName = !string.IsNullOrEmpty(otherUser?.UserName)
					? otherUser.UserName
					: $"{otherUser?.FirstName} {otherUser?.LastName}".Trim();

				if (string.IsNullOrWhiteSpace(displayName)) displayName = "Unknown User";

				return new
				{
					messageId = m.MessageId,
					content = m.Content,
					sentAt = m.SentAt,
					otherUserId = otherUser?.UserId ?? 0,
					otherUserName = displayName,
					otherUserProfilePicture = avatarUrl
				};
			}).ToList();

			return Json(conversations);
		}

		[HttpGet]
		public async Task<IActionResult> GetMessagesWithUser(int otherUserId)
		{
			var userIdStr = HttpContext.Session.GetString("UserId");
			if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();

			int userId = int.Parse(userIdStr);

			var messages = await _context.Messages
				.Where(m => (m.SenderId == userId && m.ReceiverId == otherUserId) ||
							(m.SenderId == otherUserId && m.ReceiverId == userId))
				.OrderBy(m => m.SentAt)
				.Select(m => new {
					m.MessageId,
					m.SenderId,
					m.ReceiverId,
					m.Content,
					m.SentAt
				})
				.ToListAsync();

			return Json(messages);
		}
	}
}