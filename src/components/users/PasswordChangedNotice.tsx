import { CheckCircle2, LogIn } from "lucide-react";

interface PasswordChangedNoticeProps {
  onSignIn: () => void;
}

export function PasswordChangedNotice({ onSignIn }: PasswordChangedNoticeProps) {
  return (
    <div className="flex min-h-[60vh] items-center justify-center">
      <div className="w-full max-w-md rounded-2xl border border-slate-200 bg-white p-8 text-center shadow-sm">
        <div className="mx-auto flex h-14 w-14 items-center justify-center rounded-2xl bg-emerald-50 text-emerald-600 ring-1 ring-emerald-200">
          <CheckCircle2 className="h-7 w-7" />
        </div>
        <h2 className="mt-5 text-xl font-bold text-slate-900">Đổi mật khẩu thành công</h2>
        <p className="mt-2 text-sm leading-relaxed text-slate-500">
          Vui lòng đăng nhập lại bằng mật khẩu mới để tiếp tục.
        </p>
        <button
          onClick={onSignIn}
          className="mt-8 inline-flex items-center justify-center gap-2 rounded-lg bg-blue-600 px-4 py-2.5 text-sm font-semibold text-white shadow-sm transition hover:bg-blue-700"
        >
          <LogIn className="h-4 w-4" />
          Đăng nhập lại
        </button>
      </div>
    </div>
  );
}




