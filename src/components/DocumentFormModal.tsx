import {
  X,
  FilePlus,
  Save,
  User,
  Phone,
  Calendar,
  FileText,
  Loader2,
} from "lucide-react";
import { useState } from "react";
import type {
  CustomerType,
  DebtDocument,
  DocumentFormValues,
  DocumentPriority,
} from "@/types";
import { CUSTOMER_TYPE_LABELS, PRIORITY_LABELS } from "@/types";
import { getTodayDateGmt7 } from "@/lib/format";

interface Props {
  document: DebtDocument | null;
  onClose: () => void;
  onSubmit: (values: DocumentFormValues) => Promise<boolean>;
}

const INPUT =
  "w-full rounded-lg border border-slate-200 px-3 py-2 text-sm text-slate-700 focus:border-blue-400 focus:outline-none focus:ring-2 focus:ring-blue-100";

export function DocumentFormModal({ document, onClose, onSubmit }: Props) {
  // Đưa biến edit vào TRONG component để nhận đúng props document
  const edit = !!document;

  const [name, setName] = useState(document?.customer_name ?? "");
  const [type, setType] = useState<CustomerType>(
    document?.customer_type ?? "company",
  );
  const [phone, setPhone] = useState(document?.customer_phone ?? "");
  const [issue, setIssue] = useState(
    document?.issue_date ?? getTodayDateGmt7(),
  );
  const [due, setDue] = useState(document?.due_date ?? "");
  const [priority, setPriority] = useState<DocumentPriority>(
    document?.priority ?? "normal",
  );
  const [notes, setNotes] = useState(document?.notes ?? "");
  const [hasDebt, setHasDebt] = useState(document?.has_debt_documents ?? false);

  // Khởi tạo items từ dữ liệu cũ nếu đang ở chế độ sửa (tuỳ thuộc vào cấu trúc type DebtDocument của bạn)
  const [items, setItems] = useState<
    Array<{ itemName: string; notes?: string }>
  >([]);

  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    if (!name.trim() || !due) {
      setError("Vui lòng nhập đầy đủ thông tin bắt buộc");
      return;
    }
    if (hasDebt && items.some((x) => !x.itemName.trim())) {
      setError("Tên chứng từ nợ không được để trống");
      return;
    }
    setBusy(true);
    const ok = await onSubmit({
      customerName: name.trim(),
      customerType: type,
      customerPhone: phone.trim(),
      issueDate: issue,
      dueDate: due,
      priority,
      notes: notes.trim(),
      hasDebtDocuments: hasDebt,
      debtItems: hasDebt ? items : [],
    });
    setBusy(false);
    if (ok) onClose();
    else setError("Không thể lưu hồ sơ");
  }

  return (
    <div className="fixed inset-0 z-50 flex items-start justify-center overflow-y-auto bg-slate-900/40 p-4 sm:p-8">
      <div className="w-full max-w-lg rounded-2xl bg-white shadow-2xl">
        <div className="flex items-center justify-between border-b px-6 py-5">
          <h2 className="flex items-center gap-2 text-lg font-bold">
            <FilePlus className="h-5 w-5 text-blue-600" />
            {edit
              ? `Sửa hồ sơ ${document?.document_number ?? ""}`
              : "Tạo hồ sơ mới"}
          </h2>
          <button onClick={onClose}>
            <X className="h-5 w-5 text-slate-400" />
          </button>
        </div>

        <form onSubmit={submit} className="space-y-4 px-6 py-6">
          {error && (
            <div className="rounded-lg bg-rose-50 px-3 py-2 text-sm text-rose-700">
              {error}
            </div>
          )}

          <Field label="Tên khách hàng" required>
            <input
              value={name}
              onChange={(e) => setName(e.target.value)}
              className={INPUT}
            />
          </Field>

          <Field label="Loại khách hàng" required>
            <div className="flex gap-2">
              {(
                Object.entries(CUSTOMER_TYPE_LABELS) as [CustomerType, string][]
              ).map(([k, v]) => (
                <button
                  type="button"
                  key={k}
                  onClick={() => setType(k)}
                  className={`rounded-lg px-3 py-1.5 text-sm ${type === k ? "bg-slate-900 text-white" : "bg-slate-100"}`}
                >
                  {v}
                </button>
              ))}
            </div>
          </Field>

          <div className="grid gap-4 sm:grid-cols-2">
            <Field label="Số điện thoại">
              <input
                value={phone}
                onChange={(e) => setPhone(e.target.value)}
                className={INPUT}
              />
            </Field>
          </div>

          <div className="grid gap-4 sm:grid-cols-2">
            <Field label="Ngày phát sinh">
              <input
                type="date"
                value={issue}
                onChange={(e) => setIssue(e.target.value)}
                className={INPUT}
              />
            </Field>
            <Field label="Hạn thanh toán" required>
              <input
                type="date"
                value={due}
                onChange={(e) => setDue(e.target.value)}
                className={INPUT}
              />
            </Field>
          </div>

          <Field label="Mức ưu tiên">
            <div className="flex flex-wrap gap-2">
              {(
                Object.entries(PRIORITY_LABELS) as [DocumentPriority, string][]
              ).map(([k, v]) => (
                <button
                  type="button"
                  key={k}
                  onClick={() => setPriority(k)}
                  className={`rounded-lg px-3 py-1.5 text-sm ${priority === k ? "bg-slate-900 text-white" : "bg-slate-100"}`}
                >
                  {v}
                </button>
              ))}
            </div>
          </Field>

          <Field label="Có chứng từ nợ không?" required>
            <div className="flex gap-2">
              <button
                type="button"
                onClick={() => setHasDebt(true)}
                className={`rounded-lg px-3 py-1.5 text-sm ${hasDebt ? "bg-blue-600 text-white" : "bg-slate-100"}`}
              >
                Có
              </button>
              <button
                type="button"
                onClick={() => {
                  setHasDebt(false);
                  setItems([]);
                }}
                className={`rounded-lg px-3 py-1.5 text-sm ${!hasDebt ? "bg-blue-600 text-white" : "bg-slate-100"}`}
              >
                Không
              </button>
            </div>
          </Field>

          {hasDebt && (
            <Field label="Danh sách chứng từ nợ">
              <div className="space-y-2">
                {items.map((x, i) => (
                  <div className="flex gap-2" key={i}>
                    <input
                      value={x.itemName}
                      onChange={(e) =>
                        setItems(
                          items.map((z, j) =>
                            j === i ? { ...z, itemName: e.target.value } : z,
                          ),
                        )
                      }
                      placeholder="Tên chứng từ"
                      className={INPUT}
                    />
                    <button
                      type="button"
                      onClick={() => setItems(items.filter((_, j) => j !== i))}
                      className="text-rose-600"
                    >
                      Xóa
                    </button>
                  </div>
                ))}
                <button
                  type="button"
                  onClick={() => setItems([...items, { itemName: "" }])}
                  className="text-sm font-medium text-blue-600"
                >
                  + Thêm chứng từ
                </button>
              </div>
            </Field>
          )}

          <Field label="Ghi chú">
            <textarea
              value={notes}
              onChange={(e) => setNotes(e.target.value)}
              rows={3}
              className={INPUT}
            />
          </Field>

          <div className="flex justify-end gap-3 border-t pt-4">
            <button
              type="button"
              onClick={onClose}
              className="rounded-lg px-4 py-2 text-sm"
            >
              Hủy
            </button>
            <button
              disabled={busy}
              className="flex items-center gap-2 rounded-lg bg-blue-600 px-4 py-2 text-sm font-semibold text-white"
            >
              {busy && <Loader2 className="h-4 w-4 animate-spin" />}
              {edit ? (
                <Save className="h-4 w-4" />
              ) : (
                <FileText className="h-4 w-4" />
              )}
              Lưu hồ sơ
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}

function Field({
  label,
  required,
  children,
}: {
  label: string;
  required?: boolean;
  children: React.ReactNode;
}) {
  return (
    <div>
      <label className="mb-1.5 block text-sm font-medium text-slate-700">
        {label}
        {required && <span className="text-rose-500"> *</span>}
      </label>
      {children}
    </div>
  );
}


