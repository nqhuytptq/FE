using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DebtManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Add_Return_And_Reject_Feature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "last_reject_reason",
                table: "debt_items",
                type: "varchar(1000)",
                maxLength: 1000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "rejected_at",
                table: "debt_items",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "rejected_by_department_code",
                table: "debt_items",
                type: "varchar(20)",
                maxLength: 20,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<Guid>(
                name: "rejected_by_user_id",
                table: "debt_items",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<string>(
                name: "rejected_by_user_name",
                table: "debt_items",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "return_reason",
                table: "debt_documents",
                type: "varchar(2000)",
                maxLength: 2000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "returned_at",
                table: "debt_documents",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "returned_by_user_id",
                table: "debt_documents",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<string>(
                name: "returned_by_user_name",
                table: "debt_documents",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "returned_from_department_code",
                table: "debt_documents",
                type: "varchar(20)",
                maxLength: 20,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "app_users",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000010"),
                column: "password_hash",
                value: "$2a$11$TfrXeERMeKnlCrMXlLX7huJ3eaMhzPUTFzqNi6kZjnpuoSM5LhTdy");

            migrationBuilder.UpdateData(
                table: "app_users",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000011"),
                column: "password_hash",
                value: "$2a$11$0oot4Kit9cXWfhPgEBQREuCN0/dBEZ4bIatZ6Bd06/li7gNUjBwR6");

            migrationBuilder.UpdateData(
                table: "app_users",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000012"),
                column: "password_hash",
                value: "$2a$11$hhNeG1e2wTVY229bp53MUucIuq8S3U3oFNNviqn/l/SGWNBmrbGQa");

            migrationBuilder.UpdateData(
                table: "app_users",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000021"),
                column: "password_hash",
                value: "$2a$11$0oot4Kit9cXWfhPgEBQREuCN0/dBEZ4bIatZ6Bd06/li7gNUjBwR6");

            migrationBuilder.UpdateData(
                table: "app_users",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000022"),
                column: "password_hash",
                value: "$2a$11$hhNeG1e2wTVY229bp53MUucIuq8S3U3oFNNviqn/l/SGWNBmrbGQa");

            migrationBuilder.UpdateData(
                table: "app_users",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000031"),
                column: "password_hash",
                value: "$2a$11$0oot4Kit9cXWfhPgEBQREuCN0/dBEZ4bIatZ6Bd06/li7gNUjBwR6");

            migrationBuilder.UpdateData(
                table: "app_users",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000032"),
                column: "password_hash",
                value: "$2a$11$hhNeG1e2wTVY229bp53MUucIuq8S3U3oFNNviqn/l/SGWNBmrbGQa");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "last_reject_reason",
                table: "debt_items");

            migrationBuilder.DropColumn(
                name: "rejected_at",
                table: "debt_items");

            migrationBuilder.DropColumn(
                name: "rejected_by_department_code",
                table: "debt_items");

            migrationBuilder.DropColumn(
                name: "rejected_by_user_id",
                table: "debt_items");

            migrationBuilder.DropColumn(
                name: "rejected_by_user_name",
                table: "debt_items");

            migrationBuilder.DropColumn(
                name: "return_reason",
                table: "debt_documents");

            migrationBuilder.DropColumn(
                name: "returned_at",
                table: "debt_documents");

            migrationBuilder.DropColumn(
                name: "returned_by_user_id",
                table: "debt_documents");

            migrationBuilder.DropColumn(
                name: "returned_by_user_name",
                table: "debt_documents");

            migrationBuilder.DropColumn(
                name: "returned_from_department_code",
                table: "debt_documents");

            migrationBuilder.UpdateData(
                table: "app_users",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000010"),
                column: "password_hash",
                value: "$2a$11$hzEIXhYXq/REvqHRbD3sXOxnY2Zta9mVyUUTlX7WXiXNH1jSjR3eG");

            migrationBuilder.UpdateData(
                table: "app_users",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000011"),
                column: "password_hash",
                value: "$2a$11$0uO5GSUPpQxlldM5MVhNh.PJK92drcRU5ftDZAl2MIcnScVbWvDtK");

            migrationBuilder.UpdateData(
                table: "app_users",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000012"),
                column: "password_hash",
                value: "$2a$11$g7V5745bze8SRwOKF7R.Z.bA/R53VdEiK0HpZoUzViCArSR0elh7q");

            migrationBuilder.UpdateData(
                table: "app_users",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000021"),
                column: "password_hash",
                value: "$2a$11$0uO5GSUPpQxlldM5MVhNh.PJK92drcRU5ftDZAl2MIcnScVbWvDtK");

            migrationBuilder.UpdateData(
                table: "app_users",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000022"),
                column: "password_hash",
                value: "$2a$11$g7V5745bze8SRwOKF7R.Z.bA/R53VdEiK0HpZoUzViCArSR0elh7q");

            migrationBuilder.UpdateData(
                table: "app_users",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000031"),
                column: "password_hash",
                value: "$2a$11$0uO5GSUPpQxlldM5MVhNh.PJK92drcRU5ftDZAl2MIcnScVbWvDtK");

            migrationBuilder.UpdateData(
                table: "app_users",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000032"),
                column: "password_hash",
                value: "$2a$11$g7V5745bze8SRwOKF7R.Z.bA/R53VdEiK0HpZoUzViCArSR0elh7q");
        }
    }
}
