import { LogOut, RefreshCw, ShieldAlert } from "lucide-react";
import { useAuth } from "@/context/AuthContext";

interface AccessBlockedScreenProps {
  status: "no-access" | "error";
}

export function AccessBlockedScreen({ status }: AccessBlockedScreenProps) {
  const { signOut } = useAuth();
  const isError = status === "error";

  return (
    <div className="flex min-h-screen items-center justify-center bg-slate-50 px-4">
      <div className="w-full max-w-md rounded-2xl border border-slate-200 bg-white p-8 text-center shadow-sm">
        <div className="mx-auto flex h-14 w-14 items-center justify-center rounded-2xl bg-amber-50 text-amber-600 ring-1 ring-amber-200">
          <ShieldAlert className="h-7 w-7" />
        </div>
        <h1 className="mt-5 text-xl font-bold text-slate-900">
          {isError ? "Không thể kiểm tra quyền truy cập" : "Tài khoản chưa được cấp quyền"}
        </h1>
        <p className="mt-2 text-sm leading-relaxed text-slate-500">
          {isError
            ? "Đã xảy ra lỗi khi tải thông tin tài khoản của bạn. Vui lòng thử lại."
            : "Tài khoản của bạn chưa được liên kết với hồ sơ nhân viên hoặc đang bị khóa. Vui lòng liên hệ quản trị viên."}
        </p>

        <div className="mt-8 flex flex-col gap-3 sm:flex-row sm:justify-center">
          {isError && (
            <button
              onClick={() => window.location.reload()}
              className="flex items-center justify-center gap-2 rounded-lg bg-blue-600 px-4 py-2.5 text-sm font-semibold text-white shadow-sm transition hover:bg-blue-700"
            >
              <RefreshCw className="h-4 w-4" />
              Thử lại
            </button>
          )}
          <button
            onClick={() => signOut()}
            className="flex items-center justify-center gap-2 rounded-lg border border-slate-200 bg-white px-4 py-2.5 text-sm font-semibold text-slate-700 transition hover:bg-slate-50"
          >
            <LogOut className="h-4 w-4" />
            Đăng xuất
          </button>
        </div>
      </div>
    </div>
  );
}




