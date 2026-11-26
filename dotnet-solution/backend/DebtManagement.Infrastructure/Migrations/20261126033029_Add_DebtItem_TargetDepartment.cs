using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DebtManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Add_DebtItem_TargetDepartment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "target_department_id",
                table: "debt_items",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.UpdateData(
                table: "app_users",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000010"),
                column: "password_hash",
                value: "$2a$11$V2jlU9uO0OcqSMB//F3kyOLSsIIv85f0EkAybGVmpQwcaLZ0zalvi");

            migrationBuilder.UpdateData(
                table: "app_users",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000011"),
                column: "password_hash",
                value: "$2a$11$uYt.RmdUutU5MGe23nB7Jua5uj4geU//.XwBpTBduDtjJBhQF6Puu");

            migrationBuilder.UpdateData(
                table: "app_users",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000012"),
                column: "password_hash",
                value: "$2a$11$lI6MWdT9iHnC7w6glKofy.NEFc3lRvgno8jMp8jYpWLaGuEp.oqAm");

            migrationBuilder.UpdateData(
                table: "app_users",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000021"),
                column: "password_hash",
                value: "$2a$11$uYt.RmdUutU5MGe23nB7Jua5uj4geU//.XwBpTBduDtjJBhQF6Puu");

            migrationBuilder.UpdateData(
                table: "app_users",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000022"),
                column: "password_hash",
                value: "$2a$11$lI6MWdT9iHnC7w6glKofy.NEFc3lRvgno8jMp8jYpWLaGuEp.oqAm");

            migrationBuilder.UpdateData(
                table: "app_users",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000031"),
                column: "password_hash",
                value: "$2a$11$uYt.RmdUutU5MGe23nB7Jua5uj4geU//.XwBpTBduDtjJBhQF6Puu");

            migrationBuilder.UpdateData(
                table: "app_users",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000032"),
                column: "password_hash",
                value: "$2a$11$lI6MWdT9iHnC7w6glKofy.NEFc3lRvgno8jMp8jYpWLaGuEp.oqAm");

            migrationBuilder.CreateIndex(
                name: "IX_debt_items_target_department_id",
                table: "debt_items",
                column: "target_department_id");

            migrationBuilder.AddForeignKey(
                name: "FK_debt_items_departments_target_department_id",
                table: "debt_items",
                column: "target_department_id",
                principalTable: "departments",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_debt_items_departments_target_department_id",
                table: "debt_items");

            migrationBuilder.DropIndex(
                name: "IX_debt_items_target_department_id",
                table: "debt_items");

            migrationBuilder.DropColumn(
                name: "target_department_id",
                table: "debt_items");

            migrationBuilder.UpdateData(
                table: "app_users",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000010"),
                column: "password_hash",
                value: "$2a$11$uMYcfWagO9pYps9VizNks.PKp7nOo6PAjrtxGgQME4Dwaz2hQMGiS");

            migrationBuilder.UpdateData(
                table: "app_users",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000011"),
                column: "password_hash",
                value: "$2a$11$oAvdA68ImpohZPjsUktc2e9DPZ70Q3ekivQi60v4Ta4iG06/EF9/e");

            migrationBuilder.UpdateData(
                table: "app_users",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000012"),
                column: "password_hash",
                value: "$2a$11$W6IDQLB6rRXwsRJGnqjgiOLygrqqAtdgC6BXIxXIknXyZY50E1uKm");

            migrationBuilder.UpdateData(
                table: "app_users",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000021"),
                column: "password_hash",
                value: "$2a$11$oAvdA68ImpohZPjsUktc2e9DPZ70Q3ekivQi60v4Ta4iG06/EF9/e");

            migrationBuilder.UpdateData(
                table: "app_users",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000022"),
                column: "password_hash",
                value: "$2a$11$W6IDQLB6rRXwsRJGnqjgiOLygrqqAtdgC6BXIxXIknXyZY50E1uKm");

            migrationBuilder.UpdateData(
                table: "app_users",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000031"),
                column: "password_hash",
                value: "$2a$11$oAvdA68ImpohZPjsUktc2e9DPZ70Q3ekivQi60v4Ta4iG06/EF9/e");

            migrationBuilder.UpdateData(
                table: "app_users",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000032"),
                column: "password_hash",
                value: "$2a$11$W6IDQLB6rRXwsRJGnqjgiOLygrqqAtdgC6BXIxXIknXyZY50E1uKm");
        }
    }
}
