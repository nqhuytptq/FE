import { X, User, Users, Phone, Calendar, Building2, FileText, ArrowRight, Plus, CheckCircle2, AlertTriangle, RotateCcw, UserCheck, CheckCircle, Pencil, ClipboardList, Layers } from "lucide-react";
import type { DebtDocumentWithHistory, CirculationAction, Department } from "@/types";
import { CUSTOMER_TYPE_LABELS } from "@/types";
import { formatDate, formatDateTime, getActionLabel } from "@/lib/format";
import { StatusBadge, PriorityBadge } from "@/components/Badges";
import { WorkflowActions } from "@/components/WorkflowActions";
import { DocumentEditLogList } from "@/components/DocumentEditLogList";
import { useRole } from "@/context/RoleContext";
import { supplementDebtItems, addDebtItems } from "@/hooks/useData";
import { useState } from "react";

interface DocumentDetailProps {
  document: DebtDocumentWithHistory | null;
  loading: boolean;
  departments: Department[];
  onClose: () => void;
  onActionPerformed: () => void;
  onReturn?: () => void;
  onEdit?: () => void;
}

const ACTION_ICONS: Record<CirculationAction, React.ElementType> = {
  Created: Plus, PkhSupApproved: CheckCircle2, PqlnReceived: UserCheck,
  PqlnSubmittedSup: ArrowRight, PqlnSupApproved: CheckCircle2, PdvReceived: UserCheck,
  PdvEntryPosted: AlertTriangle, PdvSupApprovedEntry: CheckCircle2, Supplemented: ClipboardList,
  Completed: CheckCircle, Rejected: AlertTriangle, Returned: RotateCcw,
};
const ACTION_COLORS: Record<CirculationAction, string> = {
  Created: "bg-sky-500", PkhSupApproved: "bg-emerald-500", PqlnReceived: "bg-indigo-500",
  PqlnSubmittedSup: "bg-amber-500", PqlnSupApproved: "bg-emerald-500", PdvReceived: "bg-indigo-500",
  PdvEntryPosted: "bg-orange-500", PdvSupApprovedEntry: "bg-amber-500", Supplemented: "bg-blue-500",
  Completed: "bg-emerald-600", Rejected: "bg-rose-500", Returned: "bg-amber-500",
};

export function DocumentDetail({ document, loading, departments, onClose, onActionPerformed, onReturn, onEdit }: DocumentDetailProps) {
  const { currentUser, currentDepartment } = useRole();
  const [selectedItems, setSelectedItems] = useState<string[]>([]);
  const [busy, setBusy] = useState(false);
  const [supplementError, setSupplementError] = useState<string | null>(null);
  const [newItemName, setNewItemName] = useState("");
  const [addingItem, setAddingItem] = useState(false);
  if (!document && !loading) return null;

  const maySupplement = !!document && !!currentUser && (
    (currentDepartment?.code === "PKH" && document.created_by_user_id === currentUser.id) ||
    (currentDepartment?.code === "PQLN" && document.pqln_receiver_id === currentUser.id)
  );

  async function handleSupplement() {
    if (!document || selectedItems.length === 0) return;
    setBusy(true); setSupplementError(null);
    const result = await supplementDebtItems(document.id, selectedItems);
    setBusy(false);
    if (result.success) { setSelectedItems([]); onActionPerformed(); }
    else setSupplementError(result.error);
  }

  const pkhItems = (document?.debt_items ?? []).filter(i => (i.source_department_code ?? "PKH") === "PKH");
  const pqlnItems = (document?.debt_items ?? []).filter(i => i.source_department_code === "PQLN");

  return (
    <div className="fixed inset-0 z-50 flex items-start justify-center overflow-y-auto bg-slate-900/40 p-4 backdrop-blur-sm sm:p-8">
      <div className="relative w-full max-w-3xl rounded-2xl bg-white shadow-2xl">
        {loading ? (
          <div className="flex h-64 items-center justify-center text-slate-400">Đang tải...</div>
        ) : document && (
          <>
            <div className="sticky top-0 z-10 flex items-start justify-between rounded-t-2xl border-b bg-white px-6 py-5">
              <div>
                <h2 className="flex items-center gap-2 text-lg font-bold">
                  <FileText className="h-5 w-5 text-slate-400" />
                  {document.document_number}
                </h2>
                <div className="mt-2 flex gap-2">
                  <StatusBadge status={document.status} />
                  <PriorityBadge priority={document.priority} />
                </div>
              </div>
              <div className="flex gap-2">
                {onEdit && (
                  <button onClick={onEdit} className="flex items-center gap-1 rounded-lg px-3 py-1.5 text-sm text-blue-600">
                    <Pencil className="h-4 w-4" />Sửa
                  </button>
                )}
                <button onClick={onClose}><X className="h-5 w-5 text-slate-400" /></button>
              </div>
            </div>

            <div className="space-y-6 px-6 py-6">
              <div className="grid gap-3 sm:grid-cols-2">
                <InfoRow icon={User} label="Khách hàng" value={document.customer_name} />
                <InfoRow icon={Users} label="Loại khách hàng" value={document.customer_type ? CUSTOMER_TYPE_LABELS[document.customer_type] : "Chưa phân loại"} />
                <InfoRow icon={Phone} label="Số điện thoại" value={document.customer_phone ?? "—"} />
                <InfoRow icon={ClipboardList} label="Số chứng từ nợ" value={String(document.total_debt_items ?? document.debt_items?.length ?? 0)} highlight />
                <InfoRow icon={Building2} label="Phòng hiện tại" value={document.current_department?.name ?? "—"} />
                <InfoRow icon={Calendar} label="Ngày tạo" value={formatDate(document.created_at)} />
                <InfoRow icon={UserCheck} label="Người khởi tạo" value={document.created_by_user_name ?? "—"} />
                <InfoRow icon={ClipboardList} label="Chứng từ nợ" value={document.has_debt_documents ? `${document.supplemented_debt_items ?? 0}/${document.total_debt_items ?? 0} đã bổ sung` : "Không có"} />
              </div>

              {/* Danh sách chứng từ nợ phân theo phòng ban */}
              {document.has_debt_documents && (
                <section className="space-y-4 rounded-xl border p-4">
                  <div className="flex items-center justify-between border-b pb-2">
                    <h3 className="flex items-center gap-2 font-bold text-slate-900">
                      <Layers className="h-4 w-4 text-blue-600" />
                      Chi tiết chứng từ nợ theo phòng ban
                    </h3>
                    <span className="text-xs text-slate-500">
                      Đã bổ sung: {document.supplemented_debt_items ?? 0}/{document.total_debt_items ?? 0}
                    </span>
                  </div>

                  {/* Nhóm PKH */}
                  <div className="space-y-2">
                    <p className="text-xs font-bold uppercase tracking-wide text-blue-700">
                      1. Chứng từ do Phòng Khách hàng (PKH) khai báo ({pkhItems.length})
                    </p>
                    {pkhItems.length === 0 ? (
                      <p className="text-xs italic text-slate-400">Không có chứng từ nợ khai báo từ PKH</p>
                    ) : (
                      pkhItems.map((item) => (
                        <label key={item.id} className="flex items-center gap-2 rounded-lg bg-blue-50/40 px-3 py-2 text-sm border border-blue-100">
                          <input
                            type="checkbox"
                            disabled={!maySupplement || item.is_supplemented}
                            checked={item.is_supplemented || selectedItems.includes(item.id)}
                            onChange={(e) => setSelectedItems((s) => e.target.checked ? [...s, item.id] : s.filter((id) => id !== item.id))}
                          />
                          <span className="flex-1 font-medium text-slate-800">{item.item_name}</span>
                          <span className={`text-xs ${item.is_supplemented ? "text-emerald-700 font-semibold" : "text-amber-700 font-medium"}`}>
                            {item.is_supplemented ? `Đã bổ sung ${item.supplemented_at ? formatDateTime(item.supplemented_at) : ""}` : "Chưa bổ sung"}
                          </span>
                        </label>
                      ))
                    )}
                  </div>

                  {/* Nhóm PQLN */}
                  <div className="space-y-2 pt-2 border-t border-dashed">
                    <p className="text-xs font-bold uppercase tracking-wide text-indigo-700">
                      2. Chứng từ do Phòng Quản lý nợ (PQLN) bổ sung ({pqlnItems.length})
                    </p>
                    {pqlnItems.length === 0 ? (
                      <p className="text-xs italic text-slate-400">Chưa có chứng từ nào do PQLN thêm</p>
                    ) : (
                      pqlnItems.map((item) => (
                        <label key={item.id} className="flex items-center gap-2 rounded-lg bg-indigo-50/40 px-3 py-2 text-sm border border-indigo-100">
                          <input
                            type="checkbox"
                            disabled={!maySupplement || item.is_supplemented}
                            checked={item.is_supplemented || selectedItems.includes(item.id)}
                            onChange={(e) => setSelectedItems((s) => e.target.checked ? [...s, item.id] : s.filter((id) => id !== item.id))}
                          />
                          <span className="flex-1 font-medium text-slate-800">{item.item_name}</span>
                          <span className={`text-xs ${item.is_supplemented ? "text-emerald-700 font-semibold" : "text-amber-700 font-medium"}`}>
                            {item.is_supplemented ? `Đã bổ sung ${item.supplemented_at ? formatDateTime(item.supplemented_at) : ""}` : "Chưa bổ sung"}
                          </span>
                        </label>
                      ))
                    )}
                  </div>

                  {maySupplement && (
                    <div className="pt-2">
                      <button
                        disabled={busy || !selectedItems.length}
                        onClick={handleSupplement}
                        className="rounded-lg bg-blue-600 px-3 py-2 text-sm font-semibold text-white disabled:opacity-50"
                      >
                        {busy ? "Đang cập nhật..." : `Bổ sung ${selectedItems.length} chứng từ đã chọn`}
                      </button>
                      {supplementError && <p className="mt-2 text-sm text-rose-600">{supplementError}</p>}
                    </div>
                  )}
                </section>
              )}

              {/* Chức năng NV PQLN thêm chứng từ nợ mới */}
              {currentDepartment?.code === "PQLN" && document.pqln_receiver_id === currentUser?.id && (
                <section className="rounded-xl border border-indigo-200 bg-indigo-50/50 p-4">
                  <h3 className="mb-2 text-sm font-bold text-indigo-900">PQLN: Thêm chứng từ nợ mới vào hồ sơ</h3>
                  <div className="flex gap-2">
                    <input
                      value={newItemName}
                      onChange={(e) => setNewItemName(e.target.value)}
                      placeholder="Nhập tên chứng từ nợ cần PQLN bổ sung..."
                      className="flex-1 rounded-lg border border-slate-200 bg-white px-3 py-1.5 text-sm"
                    />
                    <button
                      disabled={addingItem || !newItemName.trim()}
                      onClick={async () => {
                        if (!newItemName.trim()) return;
                        setAddingItem(true);
                        const res = await addDebtItems(document.id, [{ itemName: newItemName.trim() }]);
                        setAddingItem(false);
                        if (res.success) {
                          setNewItemName("");
                          onActionPerformed();
                        } else {
                          setSupplementError(res.error);
                        }
                      }}
                      className="rounded-lg bg-indigo-600 px-4 py-1.5 text-sm font-semibold text-white disabled:opacity-50 hover:bg-indigo-700"
                    >
                      {addingItem ? "Đang thêm..." : "+ Thêm chứng từ"}
                    </button>
                  </div>
                </section>
              )}

              {document.notes && (
                <div className="rounded-xl bg-slate-50 px-4 py-3">
                  <p className="text-xs font-semibold uppercase text-slate-500">Ghi chú</p>
                  <p className="mt-1 text-sm text-slate-700">{document.notes}</p>
                </div>
              )}

              <WorkflowActions
                document={document}
                departments={departments}
                onActionPerformed={onActionPerformed}
                onReturn={onReturn ?? (() => undefined)}
              />

              <section>
                <h3 className="mb-4 flex items-center gap-2 text-sm font-bold uppercase tracking-wide text-slate-800">
                  <ArrowRight className="h-4 w-4 text-slate-400" />
                  Luồng theo dõi hồ sơ
                </h3>
                <div className="space-y-3">
                  {document.circulation_history?.map((h) => {
                    const Icon = ACTION_ICONS[h.action] ?? FileText;
                    return (
                      <div key={h.id} className="flex gap-3 rounded-xl border border-slate-100 p-3 bg-white">
                        <span className={`flex h-8 w-8 shrink-0 items-center justify-center rounded-full text-white ${ACTION_COLORS[h.action] ?? "bg-slate-500"}`}>
                          <Icon className="h-4 w-4" />
                        </span>
                        <div className="min-w-0 flex-1">
                          <div className="flex flex-wrap items-center gap-2">
                            <b className="text-sm text-slate-900">{getActionLabel(h.action)}</b>
                            <span className="text-xs text-slate-400">{formatDateTime(h.created_at)}</span>
                          </div>
                          <p className="mt-1 text-xs text-slate-500">
                            {h.from_department?.name ?? ""}{h.to_department ? ` → ${h.to_department.name}` : ""}{h.performed_by_user ? ` · ${h.performed_by_user.name}` : ""}
                          </p>
                          {h.notes && <p className="mt-1 text-sm text-slate-600 bg-slate-50 rounded px-2 py-1">{h.notes}</p>}
                        </div>
                      </div>
                    );
                  })}
                </div>
              </section>

              <DocumentEditLogList logs={document.edit_logs ?? []} />
            </div>
          </>
        )}
      </div>
    </div>
  );
}

function InfoRow({ icon: Icon, label, value, highlight }: { icon: React.ElementType; label: string; value: string; highlight?: boolean }) {
  return (
    <div className="flex gap-3 rounded-xl border border-slate-100 px-4 py-3 bg-white">
      <Icon className="mt-0.5 h-4 w-4 text-slate-400" />
      <div>
        <p className="text-xs text-slate-500">{label}</p>
        <p className={`text-sm font-semibold ${highlight ? "text-blue-700" : "text-slate-800"}`}>{value}</p>
      </div>
    </div>
  );
}


