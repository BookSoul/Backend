using System;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260625154500_AddDonateRequestWorkflow")]
public partial class AddDonateRequestWorkflow : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(
            name: "ReviewedAt",
            table: "DonateRequests",
            type: "datetime2",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "StaffNote",
            table: "DonateRequests",
            type: "nvarchar(max)",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "Status",
            table: "DonateRequests",
            type: "int",
            nullable: false,
            defaultValue: 0);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "ReviewedAt",
            table: "DonateRequests");

        migrationBuilder.DropColumn(
            name: "StaffNote",
            table: "DonateRequests");

        migrationBuilder.DropColumn(
            name: "Status",
            table: "DonateRequests");
    }
}
