import { X, UserPlus, Save, Loader2, Mail, Lock, User, Building2, Power } from "lucide-react";
import { useState } from "react";
import type { AppUser, Department, UserFormValues, UserRole } from "@/types";
import { USER_ROLE_LABELS } from "@/types";

interface UserFormModalProps {
  user: AppUser | null;
  departments: Department[];
  isSelf: boolean;
  onClose: () => void;
  onSubmit: (values: UserFormValues) => Promise<string | null>;
}

const INPUT_CLASS =
  "w-full rounded-lg border border-slate-200 px-3 py-2 text-sm text-slate-700 focus:border-blue-400 focus:outline-none focus:ring-2 focus:ring-blue-100";
const MIN_PASSWORD_LENGTH = 8;
const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
const ROLES = Object.entries(USER_ROLE_LABELS) as [UserRole, string][];

export function UserFormModal({ user, departments, isSelf, onClose, onSubmit }: UserFormModalProps) {
  const isEdit = user !== null;
  const [name, setName] = useState(user?.name ?? "");
  const [email, setEmail] = useState(user?.email ?? "");
  const [password, setPassword] = useState("");
  const [departmentId, setDepartmentId] = useState(user?.department_id ?? "");
  const [role, setRole] = useState<UserRole>(user?.role ?? "staff");
  const [isActive, setIsActive] = useState(user?.is_active ?? true);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);

    if (!name.trim()) {
      setError("Vui lòng nhập họ tên");
      return;
    }
    if (!isEdit && !EMAIL_PATTERN.test(email.trim())) {
      setError("Vui lòng nhập email hợp lệ");
      return;
    }
    if (!isEdit && !password) {
      setError("Vui lòng nhập mật khẩu ban đầu");
      return;
    }
    if (password && password.length < MIN_PASSWORD_LENGTH) {
      setError(`Mật khẩu phải có ít nhất ${MIN_PASSWORD_LENGTH} ký tự`);
      return;
    }
    if (!departmentId) {
      setError("Vui lòng chọn phòng ban");
      return;
    }

    setSubmitting(true);
    const message = await onSubmit({
      name: name.trim(),
      email: email.trim().toLowerCase(),
      password,
      departmentId,
      role,
      isActive,
    });
    if (message) {
      setError(message);
      setSubmitting(false);
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-start justify-center overflow-y-auto bg-slate-900/40 p-4 backdrop-blur-sm sm:p-8">
      <div className="relative w-full max-w-lg rounded-2xl bg-white shadow-2xl">
        <div className="flex items-center justify-between rounded-t-2xl border-b border-slate-100 bg-white px-6 py-5">
          <div className="flex items-center gap-2">
            {isEdit ? (
              <Save className="h-5 w-5 text-blue-600" />
            ) : (
              <UserPlus className="h-5 w-5 text-blue-600" />
            )}
            <h2 className="text-lg font-bold text-slate-900">
              {isEdit ? "Sửa thông tin người dùng" : "Thêm người dùng"}
            </h2>
          </div>
          <button
            onClick={onClose}
            className="rounded-lg p-1.5 text-slate-400 transition hover:bg-slate-100 hover:text-slate-600"
          >
            <X className="h-5 w-5" />
          </button>
        </div>

        <form onSubmit={handleSubmit} className="space-y-5 px-6 py-6">
          {error && (
            <div className="rounded-lg border border-rose-200 bg-rose-50 px-3 py-2 text-sm text-rose-700">
              {error}
            </div>
          )}

          <Field label="Họ tên" icon={User} required>
            <input
              type="text"
              value={name}
              onChange={(e) => setName(e.target.value)}
              className={INPUT_CLASS}
            />
          </Field>

          <Field label="Email đăng nhập" icon={Mail} required={!isEdit}>
            {isEdit ? (
              <p className="rounded-lg bg-slate-50 px-3 py-2 text-sm text-slate-500">{email}</p>
            ) : (
              <input
                type="email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                placeholder="ten.ban@congty.vn"
                className={INPUT_CLASS}
              />
            )}
          </Field>

          <Field
            label={isEdit ? "Đặt mật khẩu mới" : "Mật khẩu ban đầu"}
            icon={Lock}
            required={!isEdit}
          >
            <input
              type="password"
              autoComplete="new-password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              placeholder={isEdit ? "Để trống nếu không đổi mật khẩu" : `Tối thiểu ${MIN_PASSWORD_LENGTH} ký tự`}
              className={INPUT_CLASS}
            />
          </Field>

          <Field label="Phòng ban" icon={Building2} required>
            <select
              value={departmentId}
              onChange={(e) => setDepartmentId(e.target.value)}
              className={INPUT_CLASS}
            >
              <option value="">Chọn phòng ban</option>
              {departments.map((dept) => (
                <option key={dept.id} value={dept.id}>
                  {dept.name}
                </option>
              ))}
            </select>
          </Field>

          <Field label="Vai trò">
            <div className="flex flex-wrap gap-2">
              {ROLES.map(([val, label]) => (
                <button
                  key={val}
                  type="button"
                  onClick={() => setRole(val)}
                  className={`rounded-lg px-3 py-1.5 text-sm font-medium transition ${
                    role === val
                      ? "bg-slate-900 text-white"
                      : "bg-slate-100 text-slate-600 hover:bg-slate-200"
                  }`}
                >
                  {label}
                </button>
              ))}
            </div>
            {role === "admin" && (
              <p className="mt-2 text-xs text-slate-500">
                Quản trị viên có thể thêm, sửa và khóa tài khoản người dùng.
              </p>
            )}
          </Field>

          {isEdit && (
            <label
              className={`flex items-center justify-between rounded-lg border border-slate-200 px-4 py-3 ${
                isSelf ? "opacity-60" : "cursor-pointer hover:bg-slate-50"
              }`}
            >
              <span className="flex items-center gap-2 text-sm font-medium text-slate-700">
                <Power className="h-4 w-4 text-slate-400" />
                Cho phép đăng nhập
              </span>
              <input
                type="checkbox"
                checked={isActive}
                disabled={isSelf}
                onChange={(e) => setIsActive(e.target.checked)}
                className="h-4 w-4 accent-blue-600"
              />
            </label>
          )}
          {isEdit && isSelf && (
            <p className="-mt-2 text-xs text-slate-500">Bạn không thể tự khóa tài khoản đang dùng.</p>
          )}

          <div className="flex justify-end gap-3 border-t border-slate-100 pt-4">
            <button
              type="button"
              onClick={onClose}
              className="rounded-lg px-4 py-2 text-sm font-medium text-slate-600 transition hover:bg-slate-100"
            >
              Hủy
            </button>
            <button
              type="submit"
              disabled={submitting}
              className="flex items-center gap-2 rounded-lg bg-blue-600 px-4 py-2 text-sm font-semibold text-white shadow-sm transition hover:bg-blue-700 disabled:opacity-60"
            >
              {submitting ? (
                <Loader2 className="h-4 w-4 animate-spin" />
              ) : isEdit ? (
                <Save className="h-4 w-4" />
              ) : (
                <UserPlus className="h-4 w-4" />
              )}
              {isEdit ? "Lưu thay đổi" : "Thêm người dùng"}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}

function Field({
  label,
  icon: Icon,
  required,
  children,
}: {
  label: string;
  icon?: React.ElementType;
  required?: boolean;
  children: React.ReactNode;
}) {
  return (
    <div>
      <label className="mb-1.5 flex items-center gap-1.5 text-sm font-medium text-slate-700">
        {Icon && <Icon className="h-3.5 w-3.5 text-slate-400" />}
        {label}
        {required && <span className="text-rose-500">*</span>}
      </label>
      {children}
    </div>
  );
}




