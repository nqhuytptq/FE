import { CheckCircle2, UserCheck, Send, Calculator, ClipboardCheck } from "lucide-react";
import { useState } from "react";
import type { DebtDocument, Department } from "@/types";
import { useRole } from "@/context/RoleContext";
import {
  pkhSupApproveDocument,
  pqlnReceiveDocument,
  pqlnSubmitSupDocument,
  pqlnSupApproveDocument,
  pdvReceiveDocument,
  pdvPostEntryDocument,
  pdvSupApproveDocument,
} from "@/hooks/useData";

interface Props {
  document: DebtDocument;
  departments: Department[];
  onActionPerformed: () => void;
  onReturn?: () => void;
}

export function WorkflowActions({ document, onActionPerformed, onReturn }: Props) {
  const { currentDepartment, currentUser } = useRole();
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  if (!currentDepartment || !currentUser) return null;
  const code = currentDepartment.code;
  const role = currentUser.role;

  async function act(fn: () => Promise<{ success: boolean; error: string | null }>) {
    setBusy(true);
    setError(null);
    const r = await fn();
    setBusy(false);
    if (r.success) onActionPerformed();
    else setError(r.error);
  }

  let content: React.ReactNode = null;
  if (code === "PKH" && role === "manager" && document.status === "PkhPendingSup") {
    content = <Button icon={CheckCircle2} label="Duyệt & chuyển PQLN" onClick={() => act(() => pkhSupApproveDocument(document.id))} />;
  }
  if (code === "PQLN" && role === "staff" && document.status === "PqlnPendingReceive") {
    content = <Button icon={UserCheck} label="Tiếp nhận hồ sơ" onClick={() => act(() => pqlnReceiveDocument(document.id))} />;
  }
  if (code === "PQLN" && role === "staff" && document.status === "PqlnReceived" && document.pqln_receiver_id === currentUser.id) {
    content = <Button icon={Send} label="Đẩy SUP PQLN duyệt" onClick={() => act(() => pqlnSubmitSupDocument(document.id))} />;
  }
  if (code === "PQLN" && role === "manager" && document.status === "PqlnPendingSup") {
    content = <Button icon={CheckCircle2} label="Duyệt & chuyển PDV" onClick={() => act(() => pqlnSupApproveDocument(document.id))} />;
  }
  if (code === "PDV" && role === "staff" && document.status === "PdvPendingReceive") {
    content = <Button icon={UserCheck} label="Tiếp nhận hồ sơ" onClick={() => act(() => pdvReceiveDocument(document.id))} />;
  }
  if (code === "PDV" && role === "staff" && document.status === "PdvReceived" && document.pdv_receiver_id === currentUser.id) {
    content = <Button icon={Calculator} label="Đẩy & đi bút toán" onClick={() => act(() => pdvPostEntryDocument(document.id))} />;
  }
  if (code === "PDV" && role === "manager" && (document.status === "PdvEntryPosted" || document.status === "PdvEntryApproved")) {
    content = (
      <Button
        icon={ClipboardCheck}
        label={document.status === "PdvEntryPosted" && document.has_debt_documents ? "Duyệt bút toán (Lần 1)" : "Duyệt hoàn tất hồ sơ"}
        onClick={() => act(() => pdvSupApproveDocument(document.id))}
      />
    );
  }

  const mayReturn = (code === "PQLN" || code === "PDV") && role === "manager" && document.status !== "Completed";

  if (!content && !mayReturn && error == null) return null;

  return (
    <div className="rounded-2xl border border-slate-200 bg-slate-50 p-4">
      <p className="mb-3 text-xs font-semibold uppercase tracking-wide text-slate-500">Thao tác xử lý</p>
      <div className="flex flex-wrap items-center gap-2">
        {content}
        {mayReturn && (
          <button
            onClick={() => onReturn?.()}
            disabled={busy}
            className="rounded-lg border border-amber-300 bg-white px-3 py-2 text-sm font-semibold text-amber-700 hover:bg-amber-50"
          >
            Trả lại hồ sơ
          </button>
        )}
      </div>
      {error && <p className="mt-2 rounded-lg bg-rose-50 px-3 py-1.5 text-xs text-rose-700">{error}</p>}
    </div>
  );
}

function Button({ icon: Icon, label, onClick }: { icon: React.ElementType; label: string; onClick: () => void }) {
  return (
    <button
      onClick={onClick}
      className="flex items-center gap-2 rounded-lg bg-blue-600 px-4 py-2 text-sm font-semibold text-white shadow-sm hover:bg-blue-700"
    >
      <Icon className="h-4 w-4" />
      {label}
    </button>
  );
}


