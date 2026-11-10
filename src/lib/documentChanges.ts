import type { CustomerType, DocumentFieldChange, DocumentFormValues, DocumentPriority } from "@/types";
import { CUSTOMER_TYPE_LABELS, PRIORITY_LABELS } from "@/types";
import { formatCurrency, formatDate } from "@/lib/format";

export interface StoredDocumentFields {
  customer_name: string;
  customer_type: CustomerType | null;
  customer_phone: string | null;
  
  issue_date: string;
  due_date: string;
  priority: DocumentPriority;
  notes: string | null;
}

const EMPTY_TEXT = "(trống)";

export function buildFieldChanges(
  before: StoredDocumentFields,
  after: DocumentFormValues
): DocumentFieldChange[] {
  const fields = [
    {
      label: "Tên khách hàng",
      from: before.customer_name,
      to: after.customerName,
    },
    {
      label: "Loại khách hàng",
      from: before.customer_type ? CUSTOMER_TYPE_LABELS[before.customer_type] : "",
      to: CUSTOMER_TYPE_LABELS[after.customerType],
    },
    {
      label: "Số điện thoại",
      from: before.customer_phone ?? "",
      to: after.customerPhone,
    },
    {
      label: "Ngày phát sinh",
      from: formatDate(before.issue_date),
      to: formatDate(after.issueDate),
    },
    {
      label: "Hạn thanh toán",
      from: formatDate(before.due_date),
      to: formatDate(after.dueDate),
    },
    {
      label: "Mức ưu tiên",
      from: PRIORITY_LABELS[before.priority],
      to: PRIORITY_LABELS[after.priority],
    },
    {
      label: "Ghi chú",
      from: before.notes ?? "",
      to: after.notes,
    },
  ];

  return fields
    .filter((f) => f.from !== f.to)
    .map((f) => ({ label: f.label, from: f.from || EMPTY_TEXT, to: f.to || EMPTY_TEXT }));
}





