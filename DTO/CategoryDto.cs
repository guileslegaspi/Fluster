namespace OnlineMarketplace.DTO
{
	public class CategoryDto
	{
		public int Id { get; set; }
		public string Name { get; set; } = string.Empty;

		public static List<CategoryDto> GetPredefinedCategories()
		{
			return new List<CategoryDto>
			{
				new CategoryDto { Id = 1, Name = "Men Clothing" },
				new CategoryDto { Id = 2, Name = "Female Apparel" },
				new CategoryDto { Id = 3, Name = "Gadgets" },
				new CategoryDto { Id = 4, Name = "Food" },
				new CategoryDto { Id = 5, Name = "Accessories" },
				new CategoryDto { Id = 6, Name = "Toys" },
				new CategoryDto { Id = 7, Name = "Kids" },
				new CategoryDto { Id = 8, Name = "Pets" },
				new CategoryDto { Id = 9, Name = "Other" }
			};
		}
	}
}
