using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OnlineMarketplace.Migrations
{
    /// <inheritdoc />
    public partial class updatedfieldsforproduct : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Product_User_UserId",
                table: "Product");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "Product",
                newName: "SellerId");

            migrationBuilder.RenameIndex(
                name: "IX_Product_UserId",
                table: "Product",
                newName: "IX_Product_SellerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Product_User_SellerId",
                table: "Product",
                column: "SellerId",
                principalTable: "User",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Product_User_SellerId",
                table: "Product");

            migrationBuilder.RenameColumn(
                name: "SellerId",
                table: "Product",
                newName: "UserId");

            migrationBuilder.RenameIndex(
                name: "IX_Product_SellerId",
                table: "Product",
                newName: "IX_Product_UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Product_User_UserId",
                table: "Product",
                column: "UserId",
                principalTable: "User",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
