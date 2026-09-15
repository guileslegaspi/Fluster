using Microsoft.EntityFrameworkCore;
using OnlineMarketplace.Models;

namespace OnlineMarketplace.Database
{
	public class ApplicationDbContext : DbContext
	{
		public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) 
		{

		}

		
		public DbSet<ApplicationUserModel> User {  get; set; }
		public DbSet<CartModel> Cart { get; set; }
		public DbSet<OrderModel> Order { get; set; }
		public DbSet<MessageModel> Messages { get; set; }
		public DbSet<OrderDetails> OrderDetails { get; set; }
		public DbSet<ProductModel> Product { get; set; }
		
	}
}
