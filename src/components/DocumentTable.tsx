type FilterStatus = "all" | "pending" | "approved" | "overdue" | "Completed";
﻿import {
  Search,
  FileText,
  Inbox,
  Plus,
  Bell,
  Eye,
  ArrowRight,
  Download,
} from "lucide-react";
import { useState, useMemo } from "react";
import type { DebtDocument, DocumentStatus } from "@/types";
import { CUSTOMER_TYPE_LABELS } from "@/types";
import {
  getTodayDateGmt7,
  formatCurrency,
  formatDate,
  daysUntilDue,
} from "@/lib/format";
import { StatusBadge, PriorityBadge } from "@/components/Badges";
import { useRole } from "@/context/RoleContext";
import { downloadDocumentsCsv } from "@/lib/exportCsv";
import { DateRangeFilter, type DateRange } from "@/components/DateRangeFilter";
import {
  CustomerTypeFilter,
  type CustomerTypeFilterValue,
} from "@/components/CustomerTypeFilter";

interface DocumentTableProps {
  documents: DebtDocument[];
  onSelect: (doc: DebtDocument) => void;
  onCreateNew?: () => void;
}


export function DocumentTable({
  documents,
  onSelect,
  onCreateNew,
}: DocumentTableProps) {
  const { currentDepartment, currentUser } = useRole();
  const [search, setSearch] = useState("");
  const [filter, setFilter] = useState<FilterStatus>("all");
  const [viewMode, setViewMode] = useState<"inbox" | "mine">("inbox");
  const [dateRange, setDateRange] = useState<DateRange>({ from: "", to: "" });
  const [customerType, setCustomerType] =
    useState<CustomerTypeFilterValue>("all");

  const deptId = currentDepartment?.id;
  const deptCode = currentDepartment?.code;

  const visibleDocs = useMemo(() => {
    if (viewMode === "inbox" && deptId) {
      return documents.filter((d) => d.current_department_id === deptId);
    }
    if (viewMode === "mine" && currentUser) {
      return documents.filter(
        (d) =>
          d.created_by_user_id === currentUser.id ||
          d.pqln_receiver_id === currentUser.id ||
          d.pdv_receiver_id === currentUser.id,
      );
    }
    return documents;
  }, [documents, viewMode, deptId, currentUser]);
  const filtered = useMemo(() => {
    return visibleDocs.filter((d) => {
      if (filter === "pending" && !["PkhPendingSup", "PqlnPendingReceive", "PqlnPendingSup", "PdvPendingReceive"].includes(d.status)) return false;
      if (filter === "approved" && !["PqlnReceived", "PdvReceived", "PdvEntryPosted", "PdvEntryApproved"].includes(d.status)) return false;
      if (filter === "overdue" && (d.status === "Completed" || d.status === "Rejected" || daysUntilDue(d.due_date) >= 0)) return false;
      if (filter === "Completed" && d.status !== "Completed") return false;
      if (dateRange.from && d.issue_date < dateRange.from) return false;
      if (dateRange.to && d.issue_date > dateRange.to) return false;
      if (customerType !== "all" && d.customer_type !== customerType)
        return false;
      if (search) {
        const q = search.toLowerCase();
        return (
          d.document_number.toLowerCase().includes(q) ||
          d.customer_name.toLowerCase().includes(q)
        );
      }
      return true;
    });
  }, [visibleDocs, search, filter, dateRange, customerType]);

  const incomingCount = useMemo(() => {
    if (!deptId) return 0;
    return documents.filter(
      (d) =>
        d.current_department_id === deptId &&
        d.status !== "Completed" &&
        d.status !== "Rejected",
    ).length;
  }, [documents, deptId]);

  const filterTabs: { key: FilterStatus; label: string }[] = [
    { key: "all", label: "Tất cả" },
    { key: "pending", label: "Chờ duyệt" },
    { key: "approved", label: "Đã duyệt" },
    { key: "overdue", label: "Quá hạn" },
    { key: "Completed", label: "Hoàn thành" },
  ];
  return (
    <div className="rounded-2xl border border-slate-200 bg-white shadow-sm">
      {/* Header */}
      <div className="flex flex-col gap-4 border-b border-slate-100 p-5 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <div className="flex items-center gap-2">
            <h2 className="text-lg font-bold text-slate-900">Văn bản nợ</h2>
            {viewMode === "inbox" && incomingCount > 0 && (
              <span className="flex items-center gap-1 rounded-full bg-amber-100 px-2.5 py-0.5 text-xs font-semibold text-amber-700">
                <Bell className="h-3 w-3" />
                {incomingCount} văn bản chờ xử lý
              </span>
            )}
          </div>
          <p className="mt-0.5 text-sm text-slate-500">
            {filtered.length} / {visibleDocs.length} văn bản
            {viewMode === "inbox" && ` tại ${currentDepartment?.name}`}
          </p>
        </div>
        <div className="flex flex-col gap-2 sm:flex-row sm:items-center">
          {/* View mode toggle */}
          <div className="flex rounded-lg bg-slate-100 p-0.5">
            <button
              onClick={() => setViewMode("inbox")}
              className={`flex items-center gap-1.5 rounded-md px-3 py-1.5 text-sm font-medium transition ${
                viewMode === "inbox"
                  ? "bg-white text-slate-900 shadow-sm"
                  : "text-slate-500"
              }`}
            >
              <Inbox className="h-3.5 w-3.5" />
              Hòm thư
            </button>
            <button
              onClick={() => setViewMode("mine")}
              className={`flex items-center gap-1.5 rounded-md px-3 py-1.5 text-sm font-medium transition ${
                viewMode === "mine"
                  ? "bg-white text-slate-900 shadow-sm"
                  : "text-slate-500"
              }`}
            >
              <Eye className="h-3.5 w-3.5" />
              Tất cả
            </button>
          </div>
          <button
            onClick={() => {
              const scope =
                viewMode === "inbox" && deptCode ? deptCode : "tat-ca";
              downloadDocumentsCsv(
                filtered,
                `van-ban-no-${scope}-${getTodayDateGmt7()}.csv`,
              );
            }}
            disabled={filtered.length === 0}
            className="flex items-center justify-center gap-1.5 rounded-lg border border-slate-200 px-3 py-2 text-sm font-semibold text-slate-700 transition hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-50"
          >
            <Download className="h-4 w-4" />
            Xuất Excel
          </button>
          {deptCode === "PKH" && onCreateNew && (
            <button
              onClick={onCreateNew}
              className="flex items-center gap-1.5 rounded-lg bg-blue-600 px-3 py-2 text-sm font-semibold text-white shadow-sm transition hover:bg-blue-700"
            >
              <Plus className="h-4 w-4" />
              Tạo mới văn bản nợ
            </button>
          )}
        </div>
      </div>

      {/* Filter tabs */}
      <div className="flex gap-2 px-5 pt-4">
        {filterTabs.map((tab) => (
          <button
            key={tab.key}
            onClick={() => setFilter(tab.key)}
            className={`rounded-lg px-3 py-1.5 text-sm font-medium transition ${
              filter === tab.key
                ? "bg-slate-900 text-white"
                : "text-slate-500 hover:bg-slate-100"
            }`}
          >
            {tab.label}
          </button>
        ))}
      </div>

      {/* Search */}
      <div className="px-5 pt-3">
        <div className="relative">
          <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" />
          <input
            type="text"
            placeholder="Tìm theo số VB hoặc khách hàng..."
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            className="w-full rounded-lg border border-slate-200 py-2 pl-9 pr-3 text-sm text-slate-700 placeholder:text-slate-400 focus:border-blue-400 focus:outline-none focus:ring-2 focus:ring-blue-100 sm:w-72"
          />
        </div>
      </div>

      <div className="px-5 pt-3">
        <div className="flex flex-wrap items-end gap-4 px-5 pt-3">
          <DateRangeFilter value={dateRange} onChange={setDateRange} />
          <CustomerTypeFilter value={customerType} onChange={setCustomerType} />
        </div>
      </div>

      {/* Table */}
      {filtered.length === 0 ? (
        <div className="flex flex-col items-center justify-center py-16 text-slate-400">
          <Inbox className="mb-3 h-12 w-12" strokeWidth={1.5} />
          <p className="text-sm font-medium">
            {viewMode === "inbox"
              ? "Không có văn bản nào trong hòm thư"
              : "Không có văn bản nào"}
          </p>
        </div>
      ) : (
        <div className="overflow-x-auto p-5">
          <table className="w-full text-left text-sm">
            <thead>
              <tr className="border-b border-slate-100 text-xs font-semibold uppercase tracking-wide text-slate-500">
                <th className="pb-3 pr-4">Số văn bản</th>
                <th className="pb-3 pr-4">Khách hàng</th>
                <th className="pb-3 pr-4">Số chứng từ nợ</th>
                <th className="pb-3 pr-4">Phòng ban hiện tại</th>
                <th className="pb-3 pr-4">Hạn thanh toán</th>
                <th className="pb-3 pr-4">Trạng thái</th>
                <th className="pb-3 pr-4">Ưu tiên</th>
                <th className="pb-3 pr-4 text-right">Xử lý</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-50">
              {filtered.map((doc) => {
                const days = daysUntilDue(doc.due_date);
                const isOverdue = days < 0;
                const isInMyDept = doc.current_department_id === deptId;
                const canAct =
                  isInMyDept &&
                  doc.status !== "Completed" &&
                  doc.status !== "Rejected";
                return (
                  <tr
                    key={doc.id}
                    onClick={() => onSelect(doc)}
                    className="cursor-pointer transition hover:bg-slate-50"
                  >
                    <td className="py-3 pr-4">
                      <div className="flex items-center gap-2">
                        <FileText className="h-4 w-4 text-slate-400" />
                        <span className="font-semibold text-slate-700">
                          {doc.document_number}
                        </span>
                      </div>
                    </td>
                    <td className="py-3 pr-4 text-slate-600">
                      <div>{doc.customer_name}</div>
                      <div className="text-xs text-slate-400">
                        {doc.customer_type
                          ? CUSTOMER_TYPE_LABELS[doc.customer_type]
                          : "Chưa phân loại"}
                      </div>
                    </td>
                    <td className="py-3 pr-4 font-semibold text-slate-800">
                      {String(doc.total_debt_items ?? 0)}
                    </td>
                    <td className="py-3 pr-4">
                      <span
                        className={`rounded px-2 py-0.5 text-xs font-medium ${
                          isInMyDept
                            ? "bg-blue-100 text-blue-700"
                            : "bg-slate-100 text-slate-600"
                        }`}
                      >
                        {doc.current_department?.name ?? "—"}
                      </span>
                    </td>
                    <td className="py-3 pr-4">
                      <span
                        className={
                          isOverdue
                            ? "font-medium text-rose-600"
                            : days <= 7
                              ? "font-medium text-amber-600"
                              : "text-slate-600"
                        }
                      >
                        {formatDate(doc.due_date)}
                      </span>
                      {isOverdue && (
                        <span className="ml-1 text-xs text-rose-500">
                          ({Math.abs(days)} ngày)
                        </span>
                      )}
                    </td>
                    <td className="py-3 pr-4">
                      <StatusBadge status={doc.status} />
                    </td>
                    <td className="py-3 pr-4">
                      <PriorityBadge priority={doc.priority} />
                    </td>
                    <td className="py-3 pr-4 text-right">
                      {canAct ? (
                        <span className="inline-flex items-center gap-1 rounded-lg bg-amber-50 px-2.5 py-1 text-xs font-semibold text-amber-700">
                          <ArrowRight className="h-3 w-3" />
                          Chờ xử lý
                        </span>
                      ) : (
                        <span className="inline-flex items-center gap-1 text-xs text-slate-400">
                          <Eye className="h-3 w-3" />
                          Xem
                        </span>
                      )}
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}

