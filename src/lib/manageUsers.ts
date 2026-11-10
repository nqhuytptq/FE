import { apiFetch } from "@/lib/api";
import type { UserRole } from "@/types";

export type ManageUserInput =
  | {
      action: "create";
      name: string;
      email: string;
      password: string;
      departmentId: string;
      role: UserRole;
    }
  | {
      action: "update";
      userId: string;
      name: string;
      departmentId: string;
      role: UserRole;
      isActive: boolean;
      password?: string;
    };

export type ManageUserResult = { success: true } | { success: false; error: string };

const GENERIC_ERROR = "Không thể lưu thông tin người dùng. Vui lòng thử lại.";

export async function manageUser(input: ManageUserInput): Promise<ManageUserResult> {
  try {
    if (input.action === "create") {
      await apiFetch("/users", {
        method: "POST",
        body: JSON.stringify({
          Name: input.name,
          Email: input.email,
          Password: input.password,
          DepartmentId: input.departmentId,
          Role: input.role,
        }),
      });
      return { success: true };
    } else {
      await apiFetch(`/users/${input.userId}`, {
        method: "PUT",
        body: JSON.stringify({
          Name: input.name,
          DepartmentId: input.departmentId,
          Role: input.role,
          IsActive: input.isActive,
          Password: input.password || null,
        }),
      });
      return { success: true };
    }
  } catch (err) {
    return {
      success: false,
      error: err instanceof Error ? err.message : GENERIC_ERROR,
    };
  }
}





