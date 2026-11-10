import { useState, useRef, useEffect } from "react";
import { ChevronDown, LogOut, Building2 } from "lucide-react";
import { useRole } from "@/context/RoleContext";
import { useAuth } from "@/context/AuthContext";
import { DEPARTMENT_LABELS, USER_ROLE_LABELS } from "@/types";
import type { DepartmentCode } from "@/types";

function getInitials(name: string) {
  const words = name.trim().split(/\s+/).filter(Boolean);
  if (words.length === 0) return "?";
  const first = words[0][0];
  const last = words.length > 1 ? words[words.length - 1][0] : "";
  return (first + last).toUpperCase();
}

export function UserMenu() {
  const { currentDepartment, currentUser } = useRole();
  const { signOut } = useAuth();
  const [open, setOpen] = useState(false);
  const ref = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const handler = (e: MouseEvent) => {
      if (ref.current && !ref.current.contains(e.target as Node)) {
        setOpen(false);
      }
    };
    document.addEventListener("mousedown", handler);
    return () => document.removeEventListener("mousedown", handler);
  }, []);

  const departmentLabel = currentDepartment
    ? DEPARTMENT_LABELS[currentDepartment.code as DepartmentCode] ?? currentDepartment.name
    : "Chưa phân phòng ban";

  return (
    <div className="relative" ref={ref}>
      <button
        onClick={() => setOpen(!open)}
        className="flex items-center gap-2.5 rounded-xl border border-slate-200 bg-white px-2.5 py-1.5 transition hover:bg-slate-50"
      >
        <div className="flex h-8 w-8 items-center justify-center rounded-lg bg-blue-600 text-xs font-bold text-white shadow-sm">
          {getInitials(currentUser.name)}
        </div>
        <div className="hidden text-left sm:block">
          <p className="text-sm font-semibold text-slate-800">{currentUser.name}</p>
          <p className="text-xs text-slate-500">{USER_ROLE_LABELS[currentUser.role]}</p>
        </div>
        <ChevronDown
          className={`h-4 w-4 text-slate-400 transition ${open ? "rotate-180" : ""}`}
        />
      </button>

      {open && (
        <div className="absolute right-0 top-full z-50 mt-2 w-64 overflow-hidden rounded-xl border border-slate-200 bg-white shadow-xl">
          <div className="border-b border-slate-100 px-4 py-3">
            <p className="text-sm font-semibold text-slate-800">{currentUser.name}</p>
            <p className="truncate text-xs text-slate-500">{currentUser.email}</p>
            <div className="mt-2 flex items-center gap-1.5 text-xs text-slate-600">
              <Building2 className="h-3.5 w-3.5 text-slate-400" />
              {departmentLabel}
            </div>
          </div>
          <button
            onClick={() => signOut()}
            className="flex w-full items-center gap-2.5 px-4 py-3 text-left text-sm font-medium text-rose-600 transition hover:bg-rose-50"
          >
            <LogOut className="h-4 w-4" />
            Đăng xuất
          </button>
        </div>
      )}
    </div>
  );
}




