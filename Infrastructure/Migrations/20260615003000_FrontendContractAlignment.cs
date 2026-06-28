using System;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260615003000_FrontendContractAlignment")]
    public partial class FrontendContractAlignment : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_OrderItems",
                table: "OrderItems");

            migrationBuilder.AddColumn<string>(
                name: "ApprovalStatus",
                table: "Books",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "published");

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "Books",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByName",
                table: "Books",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByRole",
                table: "Books",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Featured",
                table: "Books",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Language",
                table: "Books",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OriginalPrice",
                table: "Books",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Pages",
                table: "Books",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Publisher",
                table: "Books",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionNote",
                table: "Books",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Seller",
                table: "Books",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SellerNote",
                table: "Books",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Year",
                table: "Books",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApprovalStatus",
                table: "Accessories",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "published");

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "Accessories",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByName",
                table: "Accessories",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByRole",
                table: "Accessories",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Accessories",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OriginalPrice",
                table: "Accessories",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionNote",
                table: "Accessories",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BlindBoxCategory",
                table: "BuybackRequests",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "BuybackRequests",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ConditionText",
                table: "BuybackRequests",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactEmail",
                table: "BuybackRequests",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactName",
                table: "BuybackRequests",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactPhone",
                table: "BuybackRequests",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "BuybackRequests",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OriginalPrice",
                table: "BuybackRequests",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PublishYear",
                table: "BuybackRequests",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "Id",
                table: "OrderItems",
                type: "uniqueidentifier",
                nullable: false,
                defaultValueSql: "NEWID()");

            migrationBuilder.AddColumn<string>(
                name: "Author",
                table: "OrderItems",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Brand",
                table: "OrderItems",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "OrderItems",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProductImage",
                table: "OrderItems",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProductName",
                table: "OrderItems",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProductTypeText",
                table: "OrderItems",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_OrderItems",
                table: "OrderItems",
                column: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_OrderItems",
                table: "OrderItems");

            migrationBuilder.DropColumn(name: "ApprovalStatus", table: "Books");
            migrationBuilder.DropColumn(name: "CreatedBy", table: "Books");
            migrationBuilder.DropColumn(name: "CreatedByName", table: "Books");
            migrationBuilder.DropColumn(name: "CreatedByRole", table: "Books");
            migrationBuilder.DropColumn(name: "Featured", table: "Books");
            migrationBuilder.DropColumn(name: "Language", table: "Books");
            migrationBuilder.DropColumn(name: "OriginalPrice", table: "Books");
            migrationBuilder.DropColumn(name: "Pages", table: "Books");
            migrationBuilder.DropColumn(name: "Publisher", table: "Books");
            migrationBuilder.DropColumn(name: "RejectionNote", table: "Books");
            migrationBuilder.DropColumn(name: "Seller", table: "Books");
            migrationBuilder.DropColumn(name: "SellerNote", table: "Books");
            migrationBuilder.DropColumn(name: "Year", table: "Books");

            migrationBuilder.DropColumn(name: "ApprovalStatus", table: "Accessories");
            migrationBuilder.DropColumn(name: "CreatedBy", table: "Accessories");
            migrationBuilder.DropColumn(name: "CreatedByName", table: "Accessories");
            migrationBuilder.DropColumn(name: "CreatedByRole", table: "Accessories");
            migrationBuilder.DropColumn(name: "Description", table: "Accessories");
            migrationBuilder.DropColumn(name: "OriginalPrice", table: "Accessories");
            migrationBuilder.DropColumn(name: "RejectionNote", table: "Accessories");

            migrationBuilder.DropColumn(name: "BlindBoxCategory", table: "BuybackRequests");
            migrationBuilder.DropColumn(name: "Category", table: "BuybackRequests");
            migrationBuilder.DropColumn(name: "ConditionText", table: "BuybackRequests");
            migrationBuilder.DropColumn(name: "ContactEmail", table: "BuybackRequests");
            migrationBuilder.DropColumn(name: "ContactName", table: "BuybackRequests");
            migrationBuilder.DropColumn(name: "ContactPhone", table: "BuybackRequests");
            migrationBuilder.DropColumn(name: "Description", table: "BuybackRequests");
            migrationBuilder.DropColumn(name: "OriginalPrice", table: "BuybackRequests");
            migrationBuilder.DropColumn(name: "PublishYear", table: "BuybackRequests");

            migrationBuilder.DropColumn(name: "Id", table: "OrderItems");
            migrationBuilder.DropColumn(name: "Author", table: "OrderItems");
            migrationBuilder.DropColumn(name: "Brand", table: "OrderItems");
            migrationBuilder.DropColumn(name: "Category", table: "OrderItems");
            migrationBuilder.DropColumn(name: "ProductImage", table: "OrderItems");
            migrationBuilder.DropColumn(name: "ProductName", table: "OrderItems");
            migrationBuilder.DropColumn(name: "ProductTypeText", table: "OrderItems");

            migrationBuilder.AddPrimaryKey(
                name: "PK_OrderItems",
                table: "OrderItems",
                columns: new[] { "OrderId", "BookId", "AccessoryId" });
        }
    }
}
