import { createContext, useContext } from "react";
import type { Department, AppUser } from "@/types";

export interface RoleContextValue {
  currentDepartment: Department | null;
  currentUser: AppUser;
  departments: Department[];
  users: AppUser[];
}

const RoleContext = createContext<RoleContextValue | null>(null);

interface RoleProviderProps {
  currentUser: AppUser;
  departments: Department[];
  users: AppUser[];
  children: React.ReactNode;
}

export function RoleProvider({
  currentUser,
  departments,
  users,
  children,
}: RoleProviderProps) {
  const currentDepartment =
    departments.find((d) => d.id === currentUser.department_id) ?? null;

  return (
    <RoleContext.Provider
      value={{ currentDepartment, currentUser, departments, users }}
    >
      {children}
    </RoleContext.Provider>
  );
}

export function useRole(): RoleContextValue {
  const ctx = useContext(RoleContext);
  if (!ctx) throw new Error("useRole must be used within RoleProvider");
  return ctx;
}




