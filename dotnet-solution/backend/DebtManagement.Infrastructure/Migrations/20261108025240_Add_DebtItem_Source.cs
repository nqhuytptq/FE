using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DebtManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Add_DebtItem_Source : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "debt_items",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_user_id",
                table: "debt_items",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<Guid>(
                name: "source_department_id",
                table: "debt_items",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.UpdateData(
                table: "app_users",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000010"),
                column: "password_hash",
                value: "$2a$11$fjHC2NwF04Cxk58h81Q5iur42YqSHI9oc00e7yYdvjgy1yb893qK.");

            migrationBuilder.UpdateData(
                table: "app_users",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000011"),
                column: "password_hash",
                value: "$2a$11$bSeWmLpCh60UyyaiGXaLAe94DqaWuEOXrVGr/lkKmIQTmNF93MlbW");

            migrationBuilder.UpdateData(
                table: "app_users",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000012"),
                column: "password_hash",
                value: "$2a$11$k4ikBYP7YiHzqdvfNAghDutEHAZwDv6yJnBZ8lh5G4lJIXxxMOxeO");

            migrationBuilder.UpdateData(
                table: "app_users",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000021"),
                column: "password_hash",
                value: "$2a$11$bSeWmLpCh60UyyaiGXaLAe94DqaWuEOXrVGr/lkKmIQTmNF93MlbW");

            migrationBuilder.UpdateData(
                table: "app_users",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000022"),
                column: "password_hash",
                value: "$2a$11$k4ikBYP7YiHzqdvfNAghDutEHAZwDv6yJnBZ8lh5G4lJIXxxMOxeO");

            migrationBuilder.UpdateData(
                table: "app_users",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000031"),
                column: "password_hash",
                value: "$2a$11$bSeWmLpCh60UyyaiGXaLAe94DqaWuEOXrVGr/lkKmIQTmNF93MlbW");

            migrationBuilder.UpdateData(
                table: "app_users",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000032"),
                column: "password_hash",
                value: "$2a$11$k4ikBYP7YiHzqdvfNAghDutEHAZwDv6yJnBZ8lh5G4lJIXxxMOxeO");

            migrationBuilder.CreateIndex(
                name: "IX_debt_items_created_by_user_id",
                table: "debt_items",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_debt_items_source_department_id",
                table: "debt_items",
                column: "source_department_id");

            migrationBuilder.AddForeignKey(
                name: "FK_debt_items_app_users_created_by_user_id",
                table: "debt_items",
                column: "created_by_user_id",
                principalTable: "app_users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_debt_items_departments_source_department_id",
                table: "debt_items",
                column: "source_department_id",
                principalTable: "departments",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_debt_items_app_users_created_by_user_id",
                table: "debt_items");

            migrationBuilder.DropForeignKey(
                name: "FK_debt_items_departments_source_department_id",
                table: "debt_items");

            migrationBuilder.DropIndex(
                name: "IX_debt_items_created_by_user_id",
                table: "debt_items");

            migrationBuilder.DropIndex(
                name: "IX_debt_items_source_department_id",
                table: "debt_items");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "debt_items");

            migrationBuilder.DropColumn(
                name: "created_by_user_id",
                table: "debt_items");

            migrationBuilder.DropColumn(
                name: "source_department_id",
                table: "debt_items");

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
    }
}
