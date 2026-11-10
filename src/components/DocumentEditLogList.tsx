import { History, Pencil } from "lucide-react";
import type { DocumentEditLog } from "@/types";
import { formatDateTime } from "@/lib/format";

interface DocumentEditLogProps {
  logs: DocumentEditLog[];
}

export function DocumentEditLogList({ logs }: DocumentEditLogProps) {
  return (
    <div>
      <h3 className="mb-4 flex items-center gap-2 text-sm font-bold uppercase tracking-wide text-slate-700">
        <History className="h-4 w-4 text-slate-400" />
        Nhật ký chỉnh sửa
      </h3>
      {logs.length === 0 ? (
        <p className="text-sm text-slate-400">Chưa có lần chỉnh sửa nào</p>
      ) : (
        <ul className="space-y-3">
          {logs.map((log) => (
            <li key={log.id} className="rounded-xl border border-slate-100 px-4 py-3">
              <div className="flex flex-wrap items-center gap-2 text-sm">
                <Pencil className="h-3.5 w-3.5 text-blue-600" />
                <span className="font-semibold text-slate-800">
                  {log.edited_by_user?.name ?? "Không xác định"}
                </span>
                <span className="text-xs text-slate-400">{formatDateTime(log.created_at)}</span>
              </div>
              <ul className="mt-2 space-y-1">
                {log.changes.map((change) => (
                  <li key={change.label} className="text-sm text-slate-600">
                    <span className="font-medium text-slate-700">{change.label}:</span>{" "}
                    <span className="text-slate-400 line-through">{change.from}</span>
                    {" \u2192 "}
                    <span className="font-semibold text-slate-800">{change.to}</span>
                  </li>
                ))}
              </ul>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}




