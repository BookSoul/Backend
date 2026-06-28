using System;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260621093000_AddOrderReturnWorkflow")]
    public partial class AddOrderReturnWorkflow : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReturnReason",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReturnReasonDetail",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReturnReviewNote",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReturnRequestedAt",
                table: "Orders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReturnReviewedAt",
                table: "Orders",
                type: "datetime2",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "ReturnReason", table: "Orders");
            migrationBuilder.DropColumn(name: "ReturnReasonDetail", table: "Orders");
            migrationBuilder.DropColumn(name: "ReturnReviewNote", table: "Orders");
            migrationBuilder.DropColumn(name: "ReturnRequestedAt", table: "Orders");
            migrationBuilder.DropColumn(name: "ReturnReviewedAt", table: "Orders");
        }
    }
}
