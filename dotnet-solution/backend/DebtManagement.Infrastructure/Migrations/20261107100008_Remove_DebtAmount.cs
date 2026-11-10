using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DebtManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Remove_DebtAmount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "debt_amount",
                table: "debt_documents",
                newName: "DebtAmount");

            migrationBuilder.AlterColumn<decimal>(
                name: "DebtAmount",
                table: "debt_documents",
                type: "decimal(65,30)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.UpdateData(
                table: "app_users",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000010"),
                column: "password_hash",
                value: "$2a$11$/di.xJu2/gDtcGCM4gjlu./wl6RYuCAtMyNtN0l30C2Au3Ht9M0qa");

            migrationBuilder.UpdateData(
                table: "app_users",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000011"),
                column: "password_hash",
                value: "$2a$11$GDXXroXEo7TabQNcQzeAxuli8b2p6UhS1hGc.Ck.J7zFluVB12BW2");

            migrationBuilder.UpdateData(
                table: "app_users",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000012"),
                column: "password_hash",
                value: "$2a$11$Mdiq00Pm3Onfc9touaZLHONNS9dljIBszXD.FRYrnUb6UnfnWy04.");

            migrationBuilder.UpdateData(
                table: "app_users",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000021"),
                column: "password_hash",
                value: "$2a$11$GDXXroXEo7TabQNcQzeAxuli8b2p6UhS1hGc.Ck.J7zFluVB12BW2");

            migrationBuilder.UpdateData(
                table: "app_users",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000022"),
                column: "password_hash",
                value: "$2a$11$Mdiq00Pm3Onfc9touaZLHONNS9dljIBszXD.FRYrnUb6UnfnWy04.");

            migrationBuilder.UpdateData(
                table: "app_users",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000031"),
                column: "password_hash",
                value: "$2a$11$GDXXroXEo7TabQNcQzeAxuli8b2p6UhS1hGc.Ck.J7zFluVB12BW2");

            migrationBuilder.UpdateData(
                table: "app_users",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000032"),
                column: "password_hash",
                value: "$2a$11$Mdiq00Pm3Onfc9touaZLHONNS9dljIBszXD.FRYrnUb6UnfnWy04.");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DebtAmount",
                table: "debt_documents",
                newName: "debt_amount");

            migrationBuilder.AlterColumn<decimal>(
                name: "debt_amount",
                table: "debt_documents",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(65,30)");

            migrationBuilder.UpdateData(
                table: "app_users",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000010"),
                column: "password_hash",
                value: "$2a$11$G.BsiAc2EgeHSjCEIyXhLOz1NDHbNmW7akYH4rQgmoIyV.h20T/Tm");

            migrationBuilder.UpdateData(
                table: "app_users",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000011"),
                column: "password_hash",
                value: "$2a$11$w8pYYNhhnOPAA6QEDoOVJeZM7wyElelCfd16LmOHUXxrYBgdYBwtS");

            migrationBuilder.UpdateData(
                table: "app_users",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000012"),
                column: "password_hash",
                value: "$2a$11$ObV8clNnXxBAJYJ6sKr4.OGgkrIkIxGFP7qECnYeVqTUKcenIHgT2");

            migrationBuilder.UpdateData(
                table: "app_users",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000021"),
                column: "password_hash",
                value: "$2a$11$w8pYYNhhnOPAA6QEDoOVJeZM7wyElelCfd16LmOHUXxrYBgdYBwtS");

            migrationBuilder.UpdateData(
                table: "app_users",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000022"),
                column: "password_hash",
                value: "$2a$11$ObV8clNnXxBAJYJ6sKr4.OGgkrIkIxGFP7qECnYeVqTUKcenIHgT2");

            migrationBuilder.UpdateData(
                table: "app_users",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000031"),
                column: "password_hash",
                value: "$2a$11$w8pYYNhhnOPAA6QEDoOVJeZM7wyElelCfd16LmOHUXxrYBgdYBwtS");

            migrationBuilder.UpdateData(
                table: "app_users",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000032"),
                column: "password_hash",
                value: "$2a$11$ObV8clNnXxBAJYJ6sKr4.OGgkrIkIxGFP7qECnYeVqTUKcenIHgT2");
        }
    }
}
