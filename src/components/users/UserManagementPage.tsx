import { useMemo, useState } from "react";
import { Pencil, Plus, Loader2, UserCheck, UserX, KeyRound, Search, Download } from "lucide-react";
import { useAllAppUsers } from "@/hooks/useData";
import { useRole } from "@/context/RoleContext";
import { useAuth } from "@/context/AuthContext";
import { manageUser, type ManageUserInput } from "@/lib/manageUsers";
import { downloadUsersCsv } from "@/lib/exportCsv";
import { getTodayDateGmt7 } from "@/lib/format";
import { UserFormModal } from "@/components/users/UserFormModal";
import { PasswordChangedNotice } from "@/components/users/PasswordChangedNotice";
import { USER_ROLE_LABELS, type AppUser, type UserFormValues } from "@/types";

type Editor = { mode: "create" } | { mode: "edit"; user: AppUser } | null;

export function UserManagementPage() {
  const { departments, currentUser } = useRole();
  const { users, loading, error, refetch } = useAllAppUsers();
  const { signOut } = useAuth();
  const [editor, setEditor] = useState<Editor>(null);
  const [passwordChanged, setPasswordChanged] = useState(false);
  const [search, setSearch] = useState("");
  const [deptFilter, setDeptFilter] = useState<string>("all");
  const [statusFilter, setStatusFilter] = useState<string>("all");

  const handleExport = () => {
    const date = getTodayDateGmt7();
    downloadUsersCsv(filteredUsers, `danh-sach-nguoi-dung-${date}.csv`);
  };

  const filteredUsers = useMemo(() => {
    const q = search.trim().toLowerCase();
    return users.filter((u) => {
      if (deptFilter !== "all" && u.department_id !== deptFilter) return false;
      if (statusFilter === "active" && !u.is_active) return false;
      if (statusFilter === "locked" && u.is_active) return false;
      if (q && !u.name.toLowerCase().includes(q) && !u.email.toLowerCase().includes(q)) return false;
      return true;
    });
  }, [users, search, deptFilter, statusFilter]);

  const handleSubmit = async (values: UserFormValues): Promise<string | null> => {
    const input: ManageUserInput =
      editor?.mode === "edit"
        ? {
            action: "update",
            userId: editor.user.id,
            name: values.name,
            departmentId: values.departmentId,
            role: values.role,
            isActive: values.isActive,
            ...(values.password ? { password: values.password } : {}),
          }
        : {
            action: "create",
            name: values.name,
            email: values.email,
            password: values.password,
            departmentId: values.departmentId,
            role: values.role,
          };

    const result = await manageUser(input);
    if (!result.success) return result.error;
    setEditor(null);
    if (editor?.mode === "edit" && editor.user.id === currentUser.id && values.password) {
      setPasswordChanged(true);
      return null;
    }
    refetch();
    return null;
  };

  if (passwordChanged) {
    return <PasswordChangedNotice onSignIn={() => signOut()} />;
  }

  return (
    <section className="space-y-6">
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h2 className="text-xl font-bold text-slate-900">Quản lý người dùng</h2>
          <p className="mt-1 text-sm text-slate-500">
            Cấp tài khoản đăng nhập, phân phòng ban, vai trò và khóa truy cập.
          </p>
        </div>
        <div className="flex flex-wrap items-center gap-2">
          <button
            onClick={handleExport}
            disabled={loading || filteredUsers.length === 0}
            className="flex items-center justify-center gap-2 rounded-lg border border-slate-200 bg-white px-4 py-2.5 text-sm font-semibold text-slate-700 shadow-sm transition hover:bg-slate-50 disabled:opacity-50"
          >
            <Download className="h-4 w-4" />
            Xuất CSV
          </button>
          <button
            onClick={() => setEditor({ mode: "create" })}
            className="flex items-center justify-center gap-2 rounded-lg bg-blue-600 px-4 py-2.5 text-sm font-semibold text-white shadow-sm transition hover:bg-blue-700"
          >
            <Plus className="h-4 w-4" />
            Thêm người dùng
          </button>
        </div>
      </div>

      {error && (
        <div className="rounded-xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700">
          Lỗi tải danh sách người dùng. Vui lòng tải lại trang.
        </div>
      )}

      <div className="flex flex-col gap-3 rounded-2xl border border-slate-200 bg-white p-4 shadow-sm sm:flex-row sm:items-center">
        <div className="relative flex-1">
          <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" />
          <input
            type="text"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            placeholder="Tìm theo tên hoặc email..."
            className="w-full rounded-lg border border-slate-200 py-2 pl-9 pr-3 text-sm text-slate-700 placeholder:text-slate-400 focus:border-blue-400 focus:outline-none focus:ring-2 focus:ring-blue-100"
          />
        </div>
        <select
          value={deptFilter}
          onChange={(e) => setDeptFilter(e.target.value)}
          className="rounded-lg border border-slate-200 px-3 py-2 text-sm text-slate-700 focus:border-blue-400 focus:outline-none focus:ring-2 focus:ring-blue-100"
        >
          <option value="all">Tất cả phòng ban</option>
          {departments.map((d) => (
            <option key={d.id} value={d.id}>
              {d.name}
            </option>
          ))}
        </select>
        <div className="flex rounded-lg border border-slate-200 p-0.5">
          {[
            { v: "all", label: "Tất cả" },
            { v: "active", label: "Hoạt động" },
            { v: "locked", label: "Đã khóa" },
          ].map((opt) => (
            <button
              key={opt.v}
              onClick={() => setStatusFilter(opt.v)}
              className={`rounded-md px-3 py-1.5 text-sm font-medium transition ${
                statusFilter === opt.v
                  ? "bg-blue-600 text-white"
                  : "text-slate-600 hover:bg-slate-100"
              }`}
            >
              {opt.label}
            </button>
          ))}
        </div>
      </div>

      <div className="overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm">
        {loading ? (
          <div className="flex h-48 items-center justify-center text-slate-400">
            <Loader2 className="h-5 w-5 animate-spin" />
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full min-w-[720px] text-left text-sm">
              <thead className="border-b border-slate-200 bg-slate-50 text-xs font-semibold uppercase tracking-wide text-slate-500">
                <tr>
                  <th className="px-5 py-3">Người dùng</th>
                  <th className="px-5 py-3">Phòng ban</th>
                  <th className="px-5 py-3">Vai trò</th>
                  <th className="px-5 py-3">Đăng nhập</th>
                  <th className="px-5 py-3">Trạng thái</th>
                  <th className="px-5 py-3 text-right">Thao tác</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100">
                {filteredUsers.length === 0 ? (
                  <tr>
                    <td colSpan={6} className="px-5 py-12 text-center text-sm text-slate-400">
                      Không tìm thấy người dùng phù hợp.
                    </td>
                  </tr>
                ) : (
                  filteredUsers.map((user) => (
                  <tr key={user.id} className="transition hover:bg-slate-50/70">
                    <td className="px-5 py-3.5">
                      <p className="font-semibold text-slate-800">
                        {user.name}
                        {user.id === currentUser.id && (
                          <span className="ml-2 rounded-md bg-blue-50 px-1.5 py-0.5 text-xs font-medium text-blue-600">
                            Bạn
                          </span>
                        )}
                      </p>
                      <p className="text-xs text-slate-500">{user.email}</p>
                    </td>
                    <td className="px-5 py-3.5 text-slate-600">
                      {user.department?.name ?? "Chưa phân phòng ban"}
                    </td>
                    <td className="px-5 py-3.5 text-slate-600">
                      {USER_ROLE_LABELS[user.role]}
                    </td>
                    <td className="px-5 py-3.5">
                      {user.auth_user_id ? (
                        <span className="inline-flex items-center gap-1.5 rounded-full bg-emerald-50 px-2.5 py-1 text-xs font-medium text-emerald-700 ring-1 ring-emerald-200">
                          <KeyRound className="h-3 w-3" />
                          Đã cấp tài khoản
                        </span>
                      ) : (
                        <span className="inline-flex items-center gap-1.5 rounded-full bg-amber-50 px-2.5 py-1 text-xs font-medium text-amber-700 ring-1 ring-amber-200">
                          <KeyRound className="h-3 w-3" />
                          Chưa có mật khẩu
                        </span>
                      )}
                    </td>
                    <td className="px-5 py-3.5">
                      {user.is_active ? (
                        <span className="inline-flex items-center gap-1.5 text-xs font-medium text-emerald-700">
                          <UserCheck className="h-3.5 w-3.5" />
                          Hoạt động
                        </span>
                      ) : (
                        <span className="inline-flex items-center gap-1.5 text-xs font-medium text-rose-600">
                          <UserX className="h-3.5 w-3.5" />
                          Đã khóa
                        </span>
                      )}
                    </td>
                    <td className="px-5 py-3.5 text-right">
                      <button
                        onClick={() => setEditor({ mode: "edit", user })}
                        className="inline-flex items-center gap-1.5 rounded-lg px-3 py-1.5 text-sm font-medium text-blue-600 transition hover:bg-blue-50"
                      >
                        <Pencil className="h-3.5 w-3.5" />
                        Sửa
                      </button>
                    </td>
                  </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {editor && (
        <UserFormModal
          user={editor.mode === "edit" ? editor.user : null}
          departments={departments}
          isSelf={editor.mode === "edit" && editor.user.id === currentUser.id}
          onClose={() => setEditor(null)}
          onSubmit={handleSubmit}
        />
      )}
    </section>
  );
}




