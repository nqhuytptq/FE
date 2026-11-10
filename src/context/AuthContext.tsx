import { createContext, useCallback, useContext, useEffect, useMemo, useState } from "react";
import { apiFetch, getStoredToken, setStoredToken } from "@/lib/api";
import type { AppUser } from "@/types";

export type AuthStatus = "loading" | "signed-out" | "no-access" | "error" | "ready";

interface AuthContextValue {
  status: AuthStatus;
  currentUser: AppUser | null;
  signIn: (email: string, password: string) => Promise<string | null>;
  signOut: () => Promise<void>;
}

const AuthContext = createContext<AuthContextValue | null>(null);

const SIGN_IN_ERROR = "Email hoặc mật khẩu không đúng";

interface LoginApiResponse {
  Token: string;
  User: {
    Id: string;
    Name: string;
    Email: string;
    Role: "staff" | "manager" | "admin";
    DepartmentId: string | null;
    DepartmentName: string | null;
    DepartmentCode: string | null;
    IsActive: boolean;
    CreatedAt: string;
  };
}

function mapApiUser(u: LoginApiResponse["User"]): AppUser {
  return {
    id: u.Id,
    name: u.Name,
    email: u.Email,
    role: u.Role,
    department_id: u.DepartmentId,
    is_active: u.IsActive,
    auth_user_id: u.Id,
    created_at: u.CreatedAt,
    department: u.DepartmentId
      ? {
          id: u.DepartmentId,
          name: u.DepartmentName ?? "",
          code: u.DepartmentCode ?? "",
          description: null,
          sort_order: 0,
          created_at: u.CreatedAt,
        }
      : null,
  };
}

const CURRENT_USER_KEY = "vcb_current_user";

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [status, setStatus] = useState<AuthStatus>("loading");
  const [currentUser, setCurrentUser] = useState<AppUser | null>(null);

  useEffect(() => {
    const token = getStoredToken();
    const cachedUser = localStorage.getItem(CURRENT_USER_KEY);
    if (token && cachedUser) {
      try {
        const parsed = JSON.parse(cachedUser) as AppUser;
        setCurrentUser(parsed);
        setStatus("ready");
      } catch {
        setStoredToken(null);
        localStorage.removeItem(CURRENT_USER_KEY);
        setStatus("signed-out");
      }
    } else {
      setStatus("signed-out");
    }
  }, []);

  const signIn = useCallback(async (email: string, password: string) => {
    try {
      const res = await apiFetch<LoginApiResponse>("/auth/login", {
        method: "POST",
        body: JSON.stringify({ Email: email, Password: password }),
      });
      setStoredToken(res.Token);
      const user = mapApiUser(res.User);
      localStorage.setItem(CURRENT_USER_KEY, JSON.stringify(user));
      setCurrentUser(user);
      setStatus("ready");
      return null;
    } catch (err) {
      const msg = err instanceof Error ? err.message : SIGN_IN_ERROR;
      return msg;
    }
  }, []);

  const signOut = useCallback(async () => {
    setStoredToken(null);
    localStorage.removeItem(CURRENT_USER_KEY);
    setCurrentUser(null);
    setStatus("signed-out");
  }, []);

  const value = useMemo(
    () => ({ status, currentUser, signIn, signOut }),
    [status, currentUser, signIn, signOut]
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuth must be used within AuthProvider");
  return ctx;
}





