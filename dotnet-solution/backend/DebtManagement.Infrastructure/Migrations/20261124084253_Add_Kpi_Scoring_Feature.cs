using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DebtManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Add_Kpi_Scoring_Feature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "kpi_configs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    effective_from = table.Column<DateOnly>(type: "date", nullable: false),
                    monthly_max_count_penalty = table.Column<decimal>(type: "decimal(10,4)", precision: 10, scale: 4, nullable: false),
                    delay_penalty_rate = table.Column<decimal>(type: "decimal(10,4)", precision: 10, scale: 4, nullable: false),
                    delay_cycle_days = table.Column<int>(type: "int", nullable: false),
                    delay_rounding_mode = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    completion_milestone_type = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    sla_pkh_days = table.Column<int>(type: "int", nullable: false),
                    sla_pqln_days = table.Column<int>(type: "int", nullable: false),
                    sla_pdv_days = table.Column<int>(type: "int", nullable: false),
                    direct_pdv_total_sla_days = table.Column<int>(type: "int", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_kpi_configs", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "kpi_periods",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    year = table.Column<int>(type: "int", nullable: false),
                    month = table.Column<int>(type: "int", nullable: false),
                    working_days_in_month = table.Column<int>(type: "int", nullable: false),
                    cutoff_date = table.Column<DateOnly>(type: "date", nullable: false),
                    status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    calculated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    finalized_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    finalized_by_user_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    notes = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_kpi_periods", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "work_calendar_holidays",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    holiday_date = table.Column<DateOnly>(type: "date", nullable: false),
                    is_working_day = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    description = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_work_calendar_holidays", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "kpi_violation_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    kpi_period_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    debt_document_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    debt_item_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    document_number = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    customer_name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    item_name = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    workflow_type = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    issue_date = table.Column<DateOnly>(type: "date", nullable: false),
                    sla_due_date = table.Column<DateOnly>(type: "date", nullable: false),
                    actual_completed_date = table.Column<DateOnly>(type: "date", nullable: true),
                    pkh_days = table.Column<int>(type: "int", nullable: false),
                    pqln_days = table.Column<int>(type: "int", nullable: false),
                    pdv_days = table.Column<int>(type: "int", nullable: false),
                    delay_working_days = table.Column<int>(type: "int", nullable: false),
                    is_carried_over = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    auto_suggested_department_code = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    override_department_code = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    final_department_code = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    is_excluded = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    admin_note = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_kpi_violation_items", x => x.id);
                    table.ForeignKey(
                        name: "FK_kpi_violation_items_kpi_periods_kpi_period_id",
                        column: x => x.kpi_period_id,
                        principalTable: "kpi_periods",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

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

            migrationBuilder.InsertData(
                table: "kpi_configs",
                columns: new[] { "id", "completion_milestone_type", "delay_cycle_days", "delay_penalty_rate", "delay_rounding_mode", "direct_pdv_total_sla_days", "effective_from", "monthly_max_count_penalty", "sla_pdv_days", "sla_pkh_days", "sla_pqln_days", "updated_at" },
                values: new object[] { new Guid("00000000-0000-0000-0000-000000000100"), "CompletedAt", 5, 0.01m, "Linear", 9, new DateOnly(2026, 9, 1), 0.25m, 3, 6, 3, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.CreateIndex(
                name: "IX_kpi_periods_year_month",
                table: "kpi_periods",
                columns: new[] { "year", "month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_kpi_violation_items_kpi_period_id",
                table: "kpi_violation_items",
                column: "kpi_period_id");

            migrationBuilder.CreateIndex(
                name: "IX_work_calendar_holidays_holiday_date",
                table: "work_calendar_holidays",
                column: "holiday_date",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "kpi_configs");

            migrationBuilder.DropTable(
                name: "kpi_violation_items");

            migrationBuilder.DropTable(
                name: "work_calendar_holidays");

            migrationBuilder.DropTable(
                name: "kpi_periods");

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
    }
}
