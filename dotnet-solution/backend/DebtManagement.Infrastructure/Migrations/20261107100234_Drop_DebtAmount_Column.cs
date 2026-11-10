using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DebtManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Drop_DebtAmount_Column : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DebtAmount",
                table: "debt_documents");

            migrationBuilder.UpdateData(
                table: "app_users",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000010"),
                column: "password_hash",
                value: "$2a$11$4Fk1x3LrZh5y.2PVm7lG7eiaSxEsMhCC5CBb1KNSE8zviCXnZmtd.");

            migrationBuilder.UpdateData(
                table: "app_users",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000011"),
                column: "password_hash",
                value: "$2a$11$H0aRnJHVGymATq6h.r12EeuOdbEIGLrrppu5gMYR/MVKxboRn4ukO");

            migrationBuilder.UpdateData(
                table: "app_users",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000012"),
                column: "password_hash",
                value: "$2a$11$uoUamFh4xovSoSSQb5tXgeyJ.jAuTe2gOXGb4ijDGP8rKa8HVn6Ve");

            migrationBuilder.UpdateData(
                table: "app_users",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000021"),
                column: "password_hash",
                value: "$2a$11$H0aRnJHVGymATq6h.r12EeuOdbEIGLrrppu5gMYR/MVKxboRn4ukO");

            migrationBuilder.UpdateData(
                table: "app_users",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000022"),
                column: "password_hash",
                value: "$2a$11$uoUamFh4xovSoSSQb5tXgeyJ.jAuTe2gOXGb4ijDGP8rKa8HVn6Ve");

            migrationBuilder.UpdateData(
                table: "app_users",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000031"),
                column: "password_hash",
                value: "$2a$11$H0aRnJHVGymATq6h.r12EeuOdbEIGLrrppu5gMYR/MVKxboRn4ukO");

            migrationBuilder.UpdateData(
                table: "app_users",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000032"),
                column: "password_hash",
                value: "$2a$11$uoUamFh4xovSoSSQb5tXgeyJ.jAuTe2gOXGb4ijDGP8rKa8HVn6Ve");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DebtAmount",
                table: "debt_documents",
                type: "decimal(65,30)",
                nullable: false,
                defaultValue: 0m);

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
    }
}
