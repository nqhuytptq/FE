using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DebtManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Add_DebtItem_Verification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_verified_by_pdv",
                table: "debt_items",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_verified_by_pqln",
                table: "debt_items",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "pdv_verified_at",
                table: "debt_items",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "pdv_verified_by_user_id",
                table: "debt_items",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<DateTime>(
                name: "pqln_verified_at",
                table: "debt_items",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "pqln_verified_by_user_id",
                table: "debt_items",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.CreateIndex(
                name: "IX_debt_items_pdv_verified_by_user_id",
                table: "debt_items",
                column: "pdv_verified_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_debt_items_pqln_verified_by_user_id",
                table: "debt_items",
                column: "pqln_verified_by_user_id");

            migrationBuilder.AddForeignKey(
                name: "FK_debt_items_app_users_pdv_verified_by_user_id",
                table: "debt_items",
                column: "pdv_verified_by_user_id",
                principalTable: "app_users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_debt_items_app_users_pqln_verified_by_user_id",
                table: "debt_items",
                column: "pqln_verified_by_user_id",
                principalTable: "app_users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_debt_items_app_users_pdv_verified_by_user_id",
                table: "debt_items");

            migrationBuilder.DropForeignKey(
                name: "FK_debt_items_app_users_pqln_verified_by_user_id",
                table: "debt_items");

            migrationBuilder.DropIndex(
                name: "IX_debt_items_pdv_verified_by_user_id",
                table: "debt_items");

            migrationBuilder.DropIndex(
                name: "IX_debt_items_pqln_verified_by_user_id",
                table: "debt_items");

            migrationBuilder.DropColumn(
                name: "is_verified_by_pdv",
                table: "debt_items");

            migrationBuilder.DropColumn(
                name: "is_verified_by_pqln",
                table: "debt_items");

            migrationBuilder.DropColumn(
                name: "pdv_verified_at",
                table: "debt_items");

            migrationBuilder.DropColumn(
                name: "pdv_verified_by_user_id",
                table: "debt_items");

            migrationBuilder.DropColumn(
                name: "pqln_verified_at",
                table: "debt_items");

            migrationBuilder.DropColumn(
                name: "pqln_verified_by_user_id",
                table: "debt_items");
        }
    }
}