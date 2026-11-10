using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DebtManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class V2_Workflow_DebtItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "departments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    code = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    sort_order = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_departments", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "app_users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    email = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    password_hash = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    role = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    department_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    is_active = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_app_users", x => x.id);
                    table.ForeignKey(
                        name: "FK_app_users_departments_department_id",
                        column: x => x.department_id,
                        principalTable: "departments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "debt_documents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    document_number = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    customer_name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    customer_phone = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    customer_type = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    debt_amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    issue_date = table.Column<DateOnly>(type: "date", nullable: false),
                    due_date = table.Column<DateOnly>(type: "date", nullable: false),
                    current_department_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    priority = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    notes = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    has_debt_documents = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    sup_pkh_approved_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    sup_pkh_approver_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    pqln_receiver_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    pqln_received_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    sup_pqln_approved_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    sup_pqln_approver_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    pdv_receiver_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    pdv_received_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    entry_posted_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    entry_posted_by_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    sup_pdv_approved_entry_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    sup_pdv_entry_approver_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    completed_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    completed_by_user_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_debt_documents", x => x.id);
                    table.ForeignKey(
                        name: "FK_debt_documents_app_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "app_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_debt_documents_app_users_pdv_receiver_id",
                        column: x => x.pdv_receiver_id,
                        principalTable: "app_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_debt_documents_app_users_pqln_receiver_id",
                        column: x => x.pqln_receiver_id,
                        principalTable: "app_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_debt_documents_departments_current_department_id",
                        column: x => x.current_department_id,
                        principalTable: "departments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "circulation_history",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    debt_document_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    from_department_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    to_department_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    action = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    status_after = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    performed_by_user_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    notes = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_circulation_history", x => x.id);
                    table.ForeignKey(
                        name: "FK_circulation_history_app_users_performed_by_user_id",
                        column: x => x.performed_by_user_id,
                        principalTable: "app_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_circulation_history_debt_documents_debt_document_id",
                        column: x => x.debt_document_id,
                        principalTable: "debt_documents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_circulation_history_departments_from_department_id",
                        column: x => x.from_department_id,
                        principalTable: "departments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_circulation_history_departments_to_department_id",
                        column: x => x.to_department_id,
                        principalTable: "departments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "debt_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    debt_document_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    item_name = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    is_supplemented = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    supplemented_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    supplemented_by_user_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    notes = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_debt_items", x => x.id);
                    table.ForeignKey(
                        name: "FK_debt_items_app_users_supplemented_by_user_id",
                        column: x => x.supplemented_by_user_id,
                        principalTable: "app_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_debt_items_debt_documents_debt_document_id",
                        column: x => x.debt_document_id,
                        principalTable: "debt_documents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "document_edit_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    debt_document_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    edited_by_user_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    changes = table.Column<string>(type: "json", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_document_edit_logs", x => x.id);
                    table.ForeignKey(
                        name: "FK_document_edit_logs_app_users_edited_by_user_id",
                        column: x => x.edited_by_user_id,
                        principalTable: "app_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_document_edit_logs_debt_documents_debt_document_id",
                        column: x => x.debt_document_id,
                        principalTable: "debt_documents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.InsertData(
                table: "departments",
                columns: new[] { "id", "code", "created_at", "description", "name", "sort_order" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0000-000000000001"), "PKH", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Tiếp nhận và khởi tạo hồ sơ văn bản nợ", "Phòng Khách hàng", 1 },
                    { new Guid("00000000-0000-0000-0000-000000000002"), "PQLN", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Thẩm định, tiếp nhận và kiểm soát chứng từ nợ", "Phòng Quản lý nợ", 2 },
                    { new Guid("00000000-0000-0000-0000-000000000003"), "PDV", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Tiếp nhận, đi bút toán và hoàn tất hồ sơ", "Phòng Dịch vụ khách hàng", 3 }
                });

            migrationBuilder.InsertData(
                table: "app_users",
                columns: new[] { "id", "created_at", "department_id", "email", "is_active", "name", "password_hash", "role" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0000-000000000010"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("00000000-0000-0000-0000-000000000001"), "admin@vcb.com", true, "Quản trị viên", "$2a$11$G.BsiAc2EgeHSjCEIyXhLOz1NDHbNmW7akYH4rQgmoIyV.h20T/Tm", "admin" },
                    { new Guid("00000000-0000-0000-0000-000000000011"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("00000000-0000-0000-0000-000000000001"), "nv.pkh@vcb.com", true, "Nguyễn Văn Khách Hàng (NV)", "$2a$11$w8pYYNhhnOPAA6QEDoOVJeZM7wyElelCfd16LmOHUXxrYBgdYBwtS", "staff" },
                    { new Guid("00000000-0000-0000-0000-000000000012"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("00000000-0000-0000-0000-000000000001"), "sup.pkh@vcb.com", true, "Trần Trưởng Phòng KH (SUP)", "$2a$11$ObV8clNnXxBAJYJ6sKr4.OGgkrIkIxGFP7qECnYeVqTUKcenIHgT2", "manager" },
                    { new Guid("00000000-0000-0000-0000-000000000021"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("00000000-0000-0000-0000-000000000002"), "nv.pqln@vcb.com", true, "Lê Quản Lý Nợ (NV)", "$2a$11$w8pYYNhhnOPAA6QEDoOVJeZM7wyElelCfd16LmOHUXxrYBgdYBwtS", "staff" },
                    { new Guid("00000000-0000-0000-0000-000000000022"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("00000000-0000-0000-0000-000000000002"), "sup.pqln@vcb.com", true, "Phạm Trưởng Phòng QLN (SUP)", "$2a$11$ObV8clNnXxBAJYJ6sKr4.OGgkrIkIxGFP7qECnYeVqTUKcenIHgT2", "manager" },
                    { new Guid("00000000-0000-0000-0000-000000000031"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("00000000-0000-0000-0000-000000000003"), "nv.pdv@vcb.com", true, "Hoàng Dịch Vụ KH (NV)", "$2a$11$w8pYYNhhnOPAA6QEDoOVJeZM7wyElelCfd16LmOHUXxrYBgdYBwtS", "staff" },
                    { new Guid("00000000-0000-0000-0000-000000000032"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("00000000-0000-0000-0000-000000000003"), "sup.pdv@vcb.com", true, "Vũ Trưởng Phòng DVKH (SUP)", "$2a$11$ObV8clNnXxBAJYJ6sKr4.OGgkrIkIxGFP7qECnYeVqTUKcenIHgT2", "manager" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_app_users_department_id",
                table: "app_users",
                column: "department_id");

            migrationBuilder.CreateIndex(
                name: "IX_app_users_email",
                table: "app_users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_circulation_history_debt_document_id",
                table: "circulation_history",
                column: "debt_document_id");

            migrationBuilder.CreateIndex(
                name: "IX_circulation_history_from_department_id",
                table: "circulation_history",
                column: "from_department_id");

            migrationBuilder.CreateIndex(
                name: "IX_circulation_history_performed_by_user_id",
                table: "circulation_history",
                column: "performed_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_circulation_history_to_department_id",
                table: "circulation_history",
                column: "to_department_id");

            migrationBuilder.CreateIndex(
                name: "IX_debt_documents_created_by_user_id",
                table: "debt_documents",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_debt_documents_current_department_id",
                table: "debt_documents",
                column: "current_department_id");

            migrationBuilder.CreateIndex(
                name: "IX_debt_documents_document_number",
                table: "debt_documents",
                column: "document_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_debt_documents_pdv_receiver_id",
                table: "debt_documents",
                column: "pdv_receiver_id");

            migrationBuilder.CreateIndex(
                name: "IX_debt_documents_pqln_receiver_id",
                table: "debt_documents",
                column: "pqln_receiver_id");

            migrationBuilder.CreateIndex(
                name: "IX_debt_documents_status",
                table: "debt_documents",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_debt_items_debt_document_id",
                table: "debt_items",
                column: "debt_document_id");

            migrationBuilder.CreateIndex(
                name: "IX_debt_items_supplemented_by_user_id",
                table: "debt_items",
                column: "supplemented_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_departments_code",
                table: "departments",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_document_edit_logs_debt_document_id",
                table: "document_edit_logs",
                column: "debt_document_id");

            migrationBuilder.CreateIndex(
                name: "IX_document_edit_logs_edited_by_user_id",
                table: "document_edit_logs",
                column: "edited_by_user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "circulation_history");

            migrationBuilder.DropTable(
                name: "debt_items");

            migrationBuilder.DropTable(
                name: "document_edit_logs");

            migrationBuilder.DropTable(
                name: "debt_documents");

            migrationBuilder.DropTable(
                name: "app_users");

            migrationBuilder.DropTable(
                name: "departments");
        }
    }
}

