import type { DebtDocument } from "@/types";
import { CUSTOMER_TYPE_LABELS, USER_ROLE_LABELS } from "@/types";
import type { AppUser } from "@/types";
import { formatDate, getPriorityLabel, getStatusLabel } from "@/lib/format";

const FORMULA_PREFIX = /^[=+\-@]/;

function escapeCsvCell(value: string | number): string {
  let text = String(value);
  if (typeof value === "string" && FORMULA_PREFIX.test(text)) text = `'${text}`;
  return /[",\r\n]/.test(text) ? `"${text.replace(/"/g, '""')}"` : text;
}

export function downloadDocumentsCsv(documents: DebtDocument[], filename: string): void {
  const header = [
    "Số văn bản",
    "Khách hàng",
    "Loại khách hàng",
    "Số chứng từ nợ",
    "Hạn thanh toán",
    "Trạng thái",
    "Ưu tiên",
    "Phòng đang xử lý",
  ];
  const rows = documents.map((doc) => [
    doc.document_number,
    doc.customer_name,
    doc.customer_type ? CUSTOMER_TYPE_LABELS[doc.customer_type] : "",
    doc.total_debt_items ?? 0,
    formatDate(doc.due_date),
    getStatusLabel(doc.status),
    getPriorityLabel(doc.priority),
    doc.current_department?.name ?? "",
  ]);
  const csv = [header, ...rows].map((row) => row.map(escapeCsvCell).join(",")).join("\r\n");

  const blob = new Blob(["\uFEFF" + csv], { type: "text/csv;charset=utf-8" });
  const url = URL.createObjectURL(blob);
  const link = window.document.createElement("a");
  link.href = url;
  link.download = filename;
  link.click();
  URL.revokeObjectURL(url);
}

export function downloadUsersCsv(
  users: AppUser[],
  filename: string,
): void {
  const header = [
    "Họ tên",
    "Email",
    "Phòng ban",
    "Vai trò",
    "Đăng nhập",
    "Trạng thái",
  ];
  const rows = users.map((u) => [
    u.name,
    u.email,
    u.department?.name ?? "",
    USER_ROLE_LABELS[u.role],
    u.auth_user_id ? "Đã cấp tài khoản" : "Chưa có mật khẩu",
    u.is_active ? "Hoạt động" : "Đã khóa",
  ]);
  const csv = [header, ...rows].map((row) => row.map(escapeCsvCell).join(",")).join("\r\n");

  const blob = new Blob(["\uFEFF" + csv], { type: "text/csv;charset=utf-8" });
  const url = URL.createObjectURL(blob);
  const link = window.document.createElement("a");
  link.href = url;
  link.download = filename;
  link.click();
  URL.revokeObjectURL(url);
}





