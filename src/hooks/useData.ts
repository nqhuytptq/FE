﻿﻿﻿import { useEffect, useState, useCallback } from "react";
import { apiFetch } from "@/lib/api";
import { getEffectiveStatus } from "@/lib/format";
import type {
  Department,
  DebtDocument,
  CirculationHistory,
  DebtDocumentWithHistory,
  AppUser,
  CirculationAction,
  DocumentFormValues,
  DebtItem,
  DocumentStatus,
} from "@/types";

interface ApiDepartment {
  id: string;
  name: string;
  code: string;
  description: string | null;
  sort_order: number;
}

interface ApiUser {
  Id: string;
  Name: string;
  Email: string;
  Role: "staff" | "manager" | "admin";
  DepartmentId: string | null;
  DepartmentName: string | null;
  DepartmentCode: string | null;
  IsActive: boolean;
  CreatedAt: string;
}

interface ApiDebtItem {
  Id: string;
  DebtDocumentId: string;
  ItemName: string;
  IsSupplemented: boolean;
  SupplementedAt: string | null;
  SupplementedByUserId: string | null;
  SupplementedByUserName: string | null;
  Notes: string | null;
  SourceDepartmentId?: string | null;
  SourceDepartmentName?: string | null;
  SourceDepartmentCode?: string | null;
  CreatedByUserId?: string | null;
  CreatedByUserName?: string | null;
  CreatedAt?: string;
}

interface ApiDebtDocument {
  Id: string;
  DocumentNumber: string;
  CustomerName: string;
  CustomerPhone: string | null;
  CustomerType: "company" | "individual" | null;
  
  IssueDate: string;
  DueDate: string;
  CurrentDepartmentId: string | null;
  CurrentDepartmentName: string | null;
  CurrentDepartmentCode: string | null;
  Status: DocumentStatus;
  Priority: "low" | "normal" | "high" | "urgent";
  Notes: string | null;
  HasDebtDocuments: boolean;
  CreatedByUserId: string | null;
  CreatedByUserName: string | null;
  PqlnReceiverId: string | null;
  PqlnReceiverName: string | null;
  PqlnReceivedAt: string | null;
  PdvReceiverId: string | null;
  PdvReceiverName: string | null;
  PdvReceivedAt: string | null;
  EntryPostedAt: string | null;
  CompletedAt: string | null;
  TotalDebtItems: number;
  SupplementedDebtItems: number;
  CreatedAt: string;
  UpdatedAt: string;
}

interface ApiDebtDocumentDetail extends ApiDebtDocument {
  SupPkhApprovedAt: string | null;
  SupPqlnApprovedAt: string | null;
  SupPdvApprovedEntryAt: string | null;
  DebtItems: ApiDebtItem[];
  CirculationHistory: Array<{
    Id: string;
    FromDepartmentId: string | null;
    FromDepartmentName: string | null;
    ToDepartmentId: string | null;
    ToDepartmentName: string | null;
    Action: CirculationAction;
    StatusAfter: DocumentStatus | null;
    PerformedByUserId: string | null;
    PerformedByUserName: string | null;
    Notes: string | null;
    CreatedAt: string;
  }>;
  EditLogs: Array<{
    Id: string;
    EditedByUserId: string | null;
    EditedByUserName: string | null;
    ChangesJson: string;
    CreatedAt: string;
  }>;
}

function mapDept(d: ApiDepartment): Department {
  return {
    id: d.id,
    name: d.name,
    code: d.code,
    description: d.description,
    sort_order: d.sort_order,
    created_at: new Date().toISOString(),
  };
}

function mapUser(u: ApiUser): AppUser {
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

function mapDoc(d: ApiDebtDocument): DebtDocument {
  return {
    id: d.Id,
    document_number: d.DocumentNumber,
    customer_name: d.CustomerName,
    customer_phone: d.CustomerPhone,
    customer_type: d.CustomerType,
    issue_date: d.IssueDate,
    due_date: d.DueDate,
    current_department_id: d.CurrentDepartmentId,
    status: d.Status,
    priority: d.Priority,
    notes: d.Notes,
    has_debt_documents: d.HasDebtDocuments,
    created_by_user_id: d.CreatedByUserId,
    created_by_user_name: d.CreatedByUserName,
    pqln_receiver_id: d.PqlnReceiverId,
    pqln_receiver_name: d.PqlnReceiverName,
    pqln_received_at: d.PqlnReceivedAt,
    pdv_receiver_id: d.PdvReceiverId,
    pdv_receiver_name: d.PdvReceiverName,
    pdv_received_at: d.PdvReceivedAt,
    entry_posted_at: d.EntryPostedAt,
    completed_at: d.CompletedAt,
    total_debt_items: d.TotalDebtItems,
    supplemented_debt_items: d.SupplementedDebtItems,
    created_at: d.CreatedAt,
    updated_at: d.UpdatedAt,
    current_department: d.CurrentDepartmentId
      ? {
          id: d.CurrentDepartmentId,
          name: d.CurrentDepartmentName ?? "",
          code: d.CurrentDepartmentCode ?? "",
          description: null,
          sort_order: 0,
          created_at: d.CreatedAt,
        }
      : null,
  };
}

export function useDepartments() {
  const [departments, setDepartments] = useState<Department[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    apiFetch<ApiDepartment[]>("/departments")
      .then((data) => setDepartments((data ?? []).map(mapDept)))
      .catch((err) => setError(err instanceof Error ? err.message : "Lỗi"))
      .finally(() => setLoading(false));
  }, []);

  return { departments, loading, error };
}

export function useAppUsers() {
  const [users, setUsers] = useState<AppUser[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    apiFetch<ApiUser[]>("/users?activeOnly=true")
      .then((data) => setUsers((data ?? []).map(mapUser)))
      .catch((err) => setError(err instanceof Error ? err.message : "Lỗi"))
      .finally(() => setLoading(false));
  }, []);

  return { users, loading, error };
}

export function useAllAppUsers() {
  const [users, setUsers] = useState<AppUser[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fetchUsers = useCallback(async () => {
    setLoading(true);
    try {
      const data = await apiFetch<ApiUser[]>("/users");
      setUsers((data ?? []).map(mapUser));
      setError(null);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Lỗi");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchUsers();
  }, [fetchUsers]);

  return { users, loading, error, refetch: fetchUsers };
}

function withEffectiveStatus<T extends DebtDocument>(doc: T): T {
  return { ...doc, status: getEffectiveStatus(doc) };
}

export function useDebtDocuments() {
  const [documents, setDocuments] = useState<DebtDocument[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fetch = useCallback(async () => {
    setLoading(true);
    try {
      const data = await apiFetch<ApiDebtDocument[]>("/documents");
      setDocuments((data ?? []).map(mapDoc).map(withEffectiveStatus));
      setError(null);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Lỗi");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    fetch();
  }, [fetch]);

  return { documents, loading, error, refetch: fetch };
}

export function useDocumentWithHistory(documentId: string | null) {
  const [document, setDocument] = useState<DebtDocumentWithHistory | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const fetch = useCallback(async () => {
    if (!documentId) {
      setDocument(null);
      return;
    }
    setLoading(true);
    try {
      const data = await apiFetch<ApiDebtDocumentDetail>(`/documents/${documentId}`);
      if (!data) {
        setDocument(null);
        return;
      }
      const baseDoc = mapDoc(data);

      const debtItems: DebtItem[] = (data.DebtItems ?? []).map((i) => ({
        id: i.Id,
        debt_document_id: data.Id,
        item_name: i.ItemName,
        is_supplemented: i.IsSupplemented,
        supplemented_at: i.SupplementedAt,
        supplemented_by_user_id: i.SupplementedByUserId,
        supplemented_by_user_name: i.SupplementedByUserName,
        notes: i.Notes,
        source_department_id: i.SourceDepartmentId ?? null,
        source_department_name: i.SourceDepartmentName ?? null,
        source_department_code: i.SourceDepartmentCode ?? null,
        created_by_user_id: i.CreatedByUserId ?? null,
        created_by_user_name: i.CreatedByUserName ?? null,
        created_at: i.CreatedAt ?? "",
      }));

      const history: CirculationHistory[] = (data.CirculationHistory ?? []).map((h) => ({
        id: h.Id,
        debt_document_id: data.Id,
        from_department_id: h.FromDepartmentId,
        to_department_id: h.ToDepartmentId,
        action: h.Action,
        status_after: h.StatusAfter,
        performed_by_user_id: h.PerformedByUserId,
        notes: h.Notes,
        created_at: h.CreatedAt,
        from_department: h.FromDepartmentId
          ? { id: h.FromDepartmentId, name: h.FromDepartmentName ?? "", code: "", description: null, sort_order: 0, created_at: h.CreatedAt }
          : null,
        to_department: h.ToDepartmentId
          ? { id: h.ToDepartmentId, name: h.ToDepartmentName ?? "", code: "", description: null, sort_order: 0, created_at: h.CreatedAt }
          : null,
        performed_by_user: h.PerformedByUserId
          ? { id: h.PerformedByUserId, name: h.PerformedByUserName ?? "", email: "", role: "staff", department_id: null, is_active: true, auth_user_id: null, created_at: h.CreatedAt }
          : null,
      }));

      const editLogs = (data.EditLogs ?? []).map((l) => ({
        id: l.Id,
        debt_document_id: data.Id,
        edited_by_user_id: l.EditedByUserId,
        changes: JSON.parse(l.ChangesJson || "[]"),
        created_at: l.CreatedAt,
        edited_by_user: l.EditedByUserId
          ? { id: l.EditedByUserId, name: l.EditedByUserName ?? "", email: "", role: "staff" as const, department_id: null, is_active: true, auth_user_id: null, created_at: l.CreatedAt }
          : null,
      }));

      setDocument(withEffectiveStatus({
        ...baseDoc,
        debt_items: debtItems,
        circulation_history: history,
        edit_logs: editLogs,
        sup_pkh_approved_at: data.SupPkhApprovedAt,
        sup_pqln_approved_at: data.SupPqlnApprovedAt,
        sup_pdv_approved_entry_at: data.SupPdvApprovedEntryAt,
      }));
      setError(null);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Lỗi");
      setDocument(null);
    } finally {
      setLoading(false);
    }
  }, [documentId]);

  useEffect(() => {
    fetch();
  }, [fetch]);

  return { document, loading, error, refetch: fetch };
}

export type WorkflowActionResult = { success: boolean; error: string | null };

// 1. Tạo mới hồ sơ
export async function createDebtDocument(values: any, _userId?: string): Promise<WorkflowActionResult> {
  try {
    await apiFetch("/documents", {
      method: "POST",
      body: JSON.stringify({
        CustomerName: values.customerName,
        CustomerPhone: values.customerPhone || null,
        CustomerType: values.customerType,
        
        IssueDate: values.issueDate,
        DueDate: values.dueDate,
        Priority: values.priority,
        Notes: values.notes || null,
        HasDebtDocuments: values.hasDebtDocuments ?? false,
        DebtItems: (values.debtItems || []).map((item: any) => ({
          ItemName: item.itemName,
          Notes: item.notes || null,
        })),
      }),
    });
    return { success: true, error: null };
  } catch (err) {
    return { success: false, error: err instanceof Error ? err.message : "Lỗi tạo hồ sơ" };
  }
}

// 2. Chỉnh sửa hồ sơ
export async function updateDebtDocument(params: any): Promise<WorkflowActionResult> {
  try {
    const vals = params.values ?? params;
    await apiFetch(`/documents/${params.documentId}`, {
      method: "PUT",
      body: JSON.stringify({
        CustomerName: vals.customerName,
        CustomerPhone: vals.customerPhone || null,
        CustomerType: vals.customerType,
        
        IssueDate: vals.issueDate,
        DueDate: vals.dueDate,
        Priority: vals.priority,
        Notes: vals.notes || null,
        HasDebtDocuments: vals.hasDebtDocuments ?? false,
        DebtItems: (vals.debtItems || []).map((item: any) => ({
          ItemName: item.itemName,
          Notes: item.notes || null,
        })),
      }),
    });
    return { success: true, error: null };
  } catch (err) {
    return { success: false, error: err instanceof Error ? err.message : "Lỗi cập nhật hồ sơ" };
  }
}

// 3. SUP PKH duyệt
export async function pkhSupApproveDocument(documentId: string, notes?: string): Promise<WorkflowActionResult> {
  try {
    await apiFetch(`/documents/${documentId}/pkh-sup-approve`, {
      method: "POST",
      body: JSON.stringify({ Notes: notes || null }),
    });
    return { success: true, error: null };
  } catch (err) {
    return { success: false, error: err instanceof Error ? err.message : "Lỗi duyệt SUP PKH" };
  }
}

// 4. NV PQLN tiếp nhận
export async function pqlnReceiveDocument(documentId: string): Promise<WorkflowActionResult> {
  try {
    await apiFetch(`/documents/${documentId}/pqln-receive`, { method: "POST" });
    return { success: true, error: null };
  } catch (err) {
    return { success: false, error: err instanceof Error ? err.message : "Lỗi tiếp nhận PQLN" };
  }
}

// 5. NV PQLN đẩy SUP
export async function pqlnSubmitSupDocument(documentId: string, notes?: string): Promise<WorkflowActionResult> {
  try {
    await apiFetch(`/documents/${documentId}/pqln-submit-sup`, {
      method: "POST",
      body: JSON.stringify({ Notes: notes || null }),
    });
    return { success: true, error: null };
  } catch (err) {
    return { success: false, error: err instanceof Error ? err.message : "Lỗi đẩy SUP PQLN" };
  }
}

// 6. SUP PQLN duyệt
export async function pqlnSupApproveDocument(documentId: string, notes?: string): Promise<WorkflowActionResult> {
  try {
    await apiFetch(`/documents/${documentId}/pqln-sup-approve`, {
      method: "POST",
      body: JSON.stringify({ Notes: notes || null }),
    });
    return { success: true, error: null };
  } catch (err) {
    return { success: false, error: err instanceof Error ? err.message : "Lỗi duyệt SUP PQLN" };
  }
}

// 7. NV PDV tiếp nhận
export async function pdvReceiveDocument(documentId: string): Promise<WorkflowActionResult> {
  try {
    await apiFetch(`/documents/${documentId}/pdv-receive`, { method: "POST" });
    return { success: true, error: null };
  } catch (err) {
    return { success: false, error: err instanceof Error ? err.message : "Lỗi tiếp nhận PDV" };
  }
}

// 8. NV PDV đi bút toán
export async function pdvPostEntryDocument(documentId: string, notes?: string): Promise<WorkflowActionResult> {
  try {
    await apiFetch(`/documents/${documentId}/pdv-post-entry`, {
      method: "POST",
      body: JSON.stringify({ Notes: notes || null }),
    });
    return { success: true, error: null };
  } catch (err) {
    return { success: false, error: err instanceof Error ? err.message : "Lỗi đi bút toán" };
  }
}

// 9. SUP PDV duyệt hoàn tất (Lần 1 hoặc Lần 2)
export async function pdvSupApproveDocument(documentId: string, notes?: string): Promise<WorkflowActionResult> {
  try {
    await apiFetch(`/documents/${documentId}/pdv-sup-approve`, {
      method: "POST",
      body: JSON.stringify({ Notes: notes || null }),
    });
    return { success: true, error: null };
  } catch (err) {
    return { success: false, error: err instanceof Error ? err.message : "Lỗi duyệt SUP PDV" };
  }
}

// 10. Bổ sung chứng từ nợ
export async function supplementDebtItems(
  documentId: string,
  supplementedItemIds: string[],
  notes?: string
): Promise<WorkflowActionResult> {
  try {
    await apiFetch(`/documents/${documentId}/supplement`, {
      method: "POST",
      body: JSON.stringify({
        SupplementedItemIds: supplementedItemIds,
        Notes: notes || null,
      }),
    });
    return { success: true, error: null };
  } catch (err) {
    return { success: false, error: err instanceof Error ? err.message : "Lỗi bổ sung chứng từ" };
  }
}

// 11. Trả về
export async function returnDocument(params: {
  documentId: string;
  toDepartmentId: string;
  reason: string;
}): Promise<WorkflowActionResult> {
  try {
    await apiFetch(`/documents/${params.documentId}/return`, {
      method: "POST",
      body: JSON.stringify({ ToDepartmentId: params.toDepartmentId, Notes: params.reason || null }),
    });
    return { success: true, error: null };
  } catch (err) {
    return { success: false, error: err instanceof Error ? err.message : "Lỗi trả về" };
  }
}

// 12. Từ chối
export async function rejectDocument(params: {
  documentId: string;
  reason: string;
}): Promise<WorkflowActionResult> {
  try {
    await apiFetch(`/documents/${params.documentId}/reject`, {
      method: "POST",
      body: JSON.stringify({ Notes: params.reason || null }),
    });
    return { success: true, error: null };
  } catch (err) {
    return { success: false, error: err instanceof Error ? err.message : "Lỗi từ chối" };
  }
}

// Cũ tương thích
export async function completeDocument(params: any): Promise<WorkflowActionResult> {
  return pdvSupApproveDocument(params.documentId, params.notes);
}
export async function forwardDocument(params: any): Promise<WorkflowActionResult> {
  return { success: true, error: null };
}

export type { CirculationAction, CirculationHistory };




export async function addDebtItems(
  documentId: string,
  items: Array<{ itemName: string; notes?: string }>,
  notes?: string
): Promise<WorkflowActionResult> {
  try {
    await apiFetch(`/documents/${documentId}/debt-items`, {
      method: "POST",
      body: JSON.stringify({ Items: items, Notes: notes || null }),
    });
    return { success: true, error: null };
  } catch (err) {
    return { success: false, error: err instanceof Error ? err.message : "Lỗi thêm chứng từ nợ" };
  }
}




