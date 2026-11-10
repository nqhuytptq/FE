import {
  UserPlus,
  FileSearch,
  CheckCircle2,
  ArrowRight,
  AlertTriangle,
} from "lucide-react";
import type { Department, DebtDocument } from "@/types";
import { formatCurrency } from "@/lib/format";

interface FlowDiagramProps {
  departments: Department[];
  documents: DebtDocument[];
}

interface DeptStat {
  pending: number;
  approved: number;
  overdue: number;
  totalAmount: number;
}

function getDeptStats(deptId: string, docs: DebtDocument[]): DeptStat {
  const deptDocs = docs.filter((d) => d.current_department_id === deptId);
  return {
    pending: deptDocs.filter((d) => d.status === "PkhPendingSup").length,
    approved: deptDocs.filter(
      (d) => d.status === "PqlnPendingSup" || d.status === "PdvEntryPosted",
    ).length,
    overdue: 0,
    totalAmount: deptDocs.reduce(
      (sum, d) => sum + (d.total_debt_items ?? 0),
      0,
    ),
  };
}

const DEPT_META: Record<
  string,
  {
    icon: React.ElementType;
    color: string;
    subtitle: string;
  }
> = {
  PKH: {
    icon: UserPlus,
    color: "blue",
    subtitle: "Khởi tạo",
  },
  PQLN: {
    icon: FileSearch,
    color: "amber",
    subtitle: "Thẩm định & kiểm soát",
  },
  PDV: {
    icon: CheckCircle2,
    color: "emerald",
    subtitle: "Tiếp nhận & đi bút toán",
  },
};

export function FlowDiagram({ departments, documents }: FlowDiagramProps) {
  const ordered = [...departments].sort((a, b) => a.sort_order - b.sort_order);

  return (
    <div className="rounded-2xl border border-slate-200 bg-white p-6 shadow-sm">
      <div className="mb-6">
        <h2 className="text-lg font-bold text-slate-900">
          Luồng luân chuyển văn bản nợ
        </h2>
        <p className="mt-1 text-sm text-slate-500">
          Văn bản nợ di chuyển qua 3 phòng ban theo quy trình xử lý
        </p>
      </div>

      <div className="flex flex-col items-stretch gap-3 lg:flex-row lg:items-center lg:justify-between">
        {ordered.map((dept, idx) => {
          const meta = DEPT_META[dept.code] ?? {
            icon: Building2Fallback,
            color: "slate",
            subtitle: "",
          };
          const Icon = meta.icon;
          const stats = getDeptStats(dept.id, documents);

          return (
            <div
              key={dept.id}
              className="flex flex-1 flex-col items-stretch gap-3 lg:flex-row lg:items-center"
            >
              <div
                className={`group relative flex-1 rounded-2xl border-2 border-${meta.color}-200 bg-${meta.color}-50/50 p-5 transition hover:border-${meta.color}-400 hover:shadow-lg`}
              >
                <div className="flex items-start gap-3">
                  <div
                    className={`flex h-11 w-11 shrink-0 items-center justify-center rounded-xl bg-${meta.color}-500 text-white shadow-sm`}
                  >
                    <Icon className="h-5 w-5" strokeWidth={2} />
                  </div>
                  <div className="min-w-0 flex-1">
                    <h3 className="text-sm font-bold text-slate-900">
                      {dept.name}
                    </h3>
                    <p className="text-xs text-slate-500">{meta.subtitle}</p>
                  </div>
                </div>

                <div className="mt-4 grid grid-cols-3 gap-2">
                  <div className="rounded-lg bg-white/70 px-2 py-1.5 text-center">
                    <p className="text-lg font-bold text-amber-600">
                      {stats.pending}
                    </p>
                    <p className="text-[10px] font-medium text-slate-500">
                      Chờ duyệt
                    </p>
                  </div>
                  <div className="rounded-lg bg-white/70 px-2 py-1.5 text-center">
                    <p className="text-lg font-bold text-emerald-600">
                      {stats.approved}
                    </p>
                    <p className="text-[10px] font-medium text-slate-500">
                      Đã duyệt
                    </p>
                  </div>
                  <div className="rounded-lg bg-white/70 px-2 py-1.5 text-center">
                    <p className="text-lg font-bold text-rose-600">
                      {stats.overdue}
                    </p>
                    <p className="text-[10px] font-medium text-slate-500">
                      Quá hạn
                    </p>
                  </div>
                </div>

                <div className="mt-3 rounded-lg bg-slate-900/5 px-3 py-2">
                  <p className="text-[10px] font-medium uppercase tracking-wide text-slate-500">
                    Tổng nợ đang xử lý
                  </p>
                  <p className="text-sm font-bold text-slate-800">
                    {formatCurrency(stats.totalAmount)}
                  </p>
                </div>

                {stats.overdue > 0 && (
                  <div className="absolute -top-2 -right-2 flex h-6 w-6 items-center justify-center rounded-full bg-rose-500 text-white shadow-md">
                    <AlertTriangle className="h-3.5 w-3.5" />
                  </div>
                )}
              </div>

              {idx < ordered.length - 1 && (
                <div className="flex items-center justify-center lg:flex-shrink-0">
                  <div className="flex items-center gap-1">
                    <div className="h-0.5 w-8 bg-slate-300 lg:w-12" />
                    <ArrowRight className="h-5 w-5 text-slate-400" />
                  </div>
                </div>
              )}
            </div>
          );
        })}
      </div>

      <div className="mt-6 flex flex-wrap items-center gap-4 rounded-xl bg-slate-50 px-4 py-3 text-xs">
        <span className="font-semibold text-slate-600">Chú thích:</span>
        <span className="flex items-center gap-1.5">
          <span className="h-2.5 w-2.5 rounded-full bg-amber-500" />
          Chờ duyệt
        </span>
        <span className="flex items-center gap-1.5">
          <span className="h-2.5 w-2.5 rounded-full bg-emerald-500" />
          Đã duyệt
        </span>
        <span className="flex items-center gap-1.5">
          <span className="h-2.5 w-2.5 rounded-full bg-rose-500" />
          Quá hạn
        </span>
        <span className="flex items-center gap-1.5 text-slate-500">
          <ArrowRight className="h-3.5 w-3.5" />
          Hướng luân chuyển
        </span>
      </div>
    </div>
  );
}

function Building2Fallback() {
  return null;
}

