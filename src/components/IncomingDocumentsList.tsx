import { useMemo, useState } from "react";
import { Inbox, ArrowRight, Clock, Search, Download } from "lucide-react";
import type { DebtDocument } from "@/types";
import { useRole } from "@/context/RoleContext";
import { getTodayDateGmt7, formatCurrency, formatDate, daysUntilDue } from "@/lib/format";
import { downloadDocumentsCsv } from "@/lib/exportCsv";
import { StatusBadge } from "@/components/Badges";
import { DateRangeFilter, type DateRange } from "@/components/DateRangeFilter";
import { CustomerTypeFilter, type CustomerTypeFilterValue } from "@/components/CustomerTypeFilter";

interface IncomingDocumentsListProps {
  documents: DebtDocument[];
  onSelect: (doc: DebtDocument) => void;
}

const RECEIVING_CODES = ["PQLN", "PDV"];

type QuickFilter = "all" | "new" | "processing" | "completed";

const QUICK_FILTERS: { key: QuickFilter; label: string }[] = [
  { key: "all", label: "Tất cả" },
  { key: "new", label: "Chờ tiếp nhận" },
  { key: "processing", label: "Đang xử lý" },
  { key: "completed", label: "Hoàn thành" },
];

export function IncomingDocumentsList({ documents, onSelect }: IncomingDocumentsListProps) {
  const { currentDepartment } = useRole();
  const deptId = currentDepartment?.id;
  const [quickFilter, setQuickFilter] = useState<QuickFilter>("all");
  const [search, setSearch] = useState("");
  const [dateRange, setDateRange] = useState<DateRange>({ from: "", to: "" });
  const [customerType, setCustomerType] = useState<CustomerTypeFilterValue>("all");

  const incoming = useMemo(
    () =>
      documents
        .filter(
          (d) =>
            d.current_department_id === deptId &&
            d.status !== "Completed" &&
            d.status !== "Rejected"
        )
        .sort((a, b) => a.due_date.localeCompare(b.due_date)),
    [documents, deptId]
  );

  const dated = useMemo(
    () =>
      incoming.filter(
        (d) =>
          (!dateRange.from || d.issue_date >= dateRange.from) &&
          (!dateRange.to || d.issue_date <= dateRange.to) &&
          (customerType === "all" || d.customer_type === customerType)
      ),
    [incoming, dateRange, customerType]
  );

  const searched = useMemo(() => {
    const q = search.trim().toLowerCase();
    if (!q) return dated;
    return dated.filter(
      (d) =>
        d.document_number.toLowerCase().includes(q) || d.customer_name.toLowerCase().includes(q)
    );
  }, [dated, search]);

  const visible = useMemo(
    () => { if (quickFilter === "all") return searched; if (quickFilter === "new") return searched.filter((d) => ["PqlnPendingReceive", "PdvPendingReceive"].includes(d.status)); if (quickFilter === "processing") return searched.filter((d) => ["PqlnReceived", "PqlnPendingSup", "PdvReceived", "PdvEntryPosted", "PdvEntryApproved"].includes(d.status)); return searched.filter((d) => d.status === "Completed"); },
    [searched, quickFilter]
  );

  if (!currentDepartment || !RECEIVING_CODES.includes(currentDepartment.code)) return null;

  return (
    <section className="rounded-2xl border border-slate-200 bg-white shadow-sm">
      <div className="flex items-center justify-between border-b border-slate-100 px-5 py-4">
        <div>
          <h2 className="text-lg font-bold text-slate-900">
            Văn bản đến {currentDepartment.name}
          </h2>
          <p className="mt-0.5 text-sm text-slate-500">
            {incoming.length} văn bản đang chờ xử lý, sắp xếp theo hạn thanh toán
          </p>
        </div>
        <div className="flex items-center gap-2">
          {incoming.length > 0 && (
            <span className="flex items-center gap-1.5 rounded-full bg-amber-100 px-3 py-1 text-xs font-semibold text-amber-700">
              <Inbox className="h-3.5 w-3.5" />
              {incoming.length} mới
            </span>
          )}
          <button
            onClick={() =>
              downloadDocumentsCsv(
                visible,
                `van-ban-den-${currentDepartment.code}-${getTodayDateGmt7()}.csv`
              )
            }
            disabled={visible.length === 0}
            className="flex items-center gap-1.5 rounded-lg border border-slate-200 px-3 py-1.5 text-xs font-semibold text-slate-700 transition hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-50"
          >
            <Download className="h-3.5 w-3.5" />
            Xuất Excel
          </button>
        </div>
      </div>

      <div className="border-b border-slate-100 px-5 pb-3 pt-4">
        <div className="relative mb-3">
          <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" />
          <input
            type="text"
            placeholder="Tìm theo số VB hoặc khách hàng..."
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            className="w-full rounded-lg border border-slate-200 py-2 pl-9 pr-3 text-sm text-slate-700 placeholder:text-slate-400 focus:border-blue-400 focus:outline-none focus:ring-2 focus:ring-blue-100"
          />
        </div>
        <div className="mb-3 flex flex-wrap items-end gap-4">
          <DateRangeFilter value={dateRange} onChange={setDateRange} />
          <CustomerTypeFilter value={customerType} onChange={setCustomerType} />
        </div>
        <div className="flex flex-wrap gap-2">
        {QUICK_FILTERS.map((f) => {
          const count = f.key === "all" ? searched.length : f.key === "new" ? searched.filter((d) => ["PqlnPendingReceive", "PdvPendingReceive"].includes(d.status)).length : f.key === "processing" ? searched.filter((d) => ["PqlnReceived", "PqlnPendingSup", "PdvReceived", "PdvEntryPosted", "PdvEntryApproved"].includes(d.status)).length : searched.filter((d) => d.status === "Completed").length;
          const active = quickFilter === f.key;
          return (
            <button
              key={f.key}
              onClick={() => setQuickFilter(f.key)}
              className={`flex items-center gap-1.5 rounded-lg px-3 py-1.5 text-sm font-medium transition ${
                active ? "bg-slate-900 text-white" : "text-slate-500 hover:bg-slate-100"
              }`}
            >
              {f.label}
              <span
                className={`rounded-full px-1.5 text-xs ${
                  active ? "bg-white/20 text-white" : "bg-slate-100 text-slate-600"
                }`}
              >
                {count}
              </span>
            </button>
          );
        })}
        </div>
      </div>

      {visible.length === 0 ? (
        <div className="flex flex-col items-center justify-center py-12 text-slate-400">
          <Inbox className="mb-3 h-10 w-10" strokeWidth={1.5} />
          <p className="text-sm font-medium">
            {incoming.length === 0
              ? "Không có văn bản đến cần xử lý"
              : "Không có văn bản nào trong bộ lọc này"}
          </p>
        </div>
      ) : (
        <ul className="divide-y divide-slate-100">
          {visible.map((doc) => {
            const days = daysUntilDue(doc.due_date);
            const dueText =
              days < 0
                ? `Quá ${Math.abs(days)} ngày`
                : days === 0
                  ? "Hết hạn hôm nay"
                  : `Còn ${days} ngày`;
            return (
              <li key={doc.id}>
                <button
                  onClick={() => onSelect(doc)}
                  className="group flex w-full items-center gap-4 px-5 py-4 text-left transition hover:bg-slate-50"
                >
                  <div className="min-w-0 flex-1">
                    <div className="flex flex-wrap items-center gap-2">
                      <span className="font-semibold text-slate-800">{doc.document_number}</span>
                      <StatusBadge status={doc.status} />
                    </div>
                    <p className="mt-0.5 truncate text-sm text-slate-600">{doc.customer_name}</p>
                  </div>
                  <div className="hidden text-right sm:block">
                    <p className="text-sm font-semibold text-slate-800">
                      {String(doc.total_debt_items ?? 0)}
                    </p>
                    <p
                      className={`mt-0.5 flex items-center justify-end gap-1 text-xs ${
                        days < 0 ? "text-rose-600" : days <= 7 ? "text-amber-600" : "text-slate-500"
                      }`}
                    >
                      <Clock className="h-3 w-3" />
                      {formatDate(doc.due_date)} · {dueText}
                    </p>
                  </div>
                  <ArrowRight className="h-4 w-4 shrink-0 text-slate-300 transition group-hover:translate-x-0.5 group-hover:text-blue-600" />
                </button>
              </li>
            );
          })}
        </ul>
      )}
    </section>
  );
}





