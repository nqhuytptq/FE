import { FileText, Users, Building2, Clock } from "lucide-react";

interface StatCardProps {
  icon: React.ElementType;
  label: string;
  value: string | number;
  sublabel?: string;
  accent: string;
}

function StatCard({ icon: Icon, label, value, sublabel, accent }: StatCardProps) {
  return (
    <div className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm transition hover:shadow-md">
      <div className="flex items-start justify-between">
        <div>
          <p className="text-sm font-medium text-slate-500">{label}</p>
          <p className="mt-1.5 text-3xl font-bold text-slate-900">{value}</p>
          {sublabel && (
            <p className="mt-1 text-xs text-slate-400">{sublabel}</p>
          )}
        </div>
        <div
          className={`flex h-12 w-12 items-center justify-center rounded-xl bg-${accent}-50 text-${accent}-600`}
        >
          <Icon className="h-6 w-6" strokeWidth={2} />
        </div>
      </div>
    </div>
  );
}

export interface DashboardStats {
  totalDocuments: number;
  pendingCount: number;
  approvedCount: number;
  overdueCount: number;
  totalDebtItems: number;
  totalUsers: number;
  totalDepartments: number;
}

export function StatCards({ stats }: { stats: DashboardStats }) {
  return (
    <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
      <StatCard
        icon={FileText}
        label="Tổng văn bản nợ"
        value={stats.totalDocuments}
        sublabel={`${stats.pendingCount} chờ duyệt`}
        accent="blue"
      />
      <StatCard
        icon={Clock}
        label="Quá hạn"
        value={stats.overdueCount}
        sublabel="Cần xử lý gấp"
        accent="rose"
      />
      <StatCard
        icon={Building2}
        label="Tổng chứng từ nợ"
        value={stats.totalDebtItems}
        sublabel="Đã khai báo trong hồ sơ"
        accent="emerald"
      />
      <StatCard
        icon={Users}
        label="Người dùng"
        value={stats.totalUsers}
        sublabel={`${stats.totalDepartments} phòng ban`}
        accent="indigo"
      />
    </div>
  );
}





