export type DocumentStatus =
  | "Draft"
  | "PkhPendingSup"
  | "PqlnPendingReceive"
  | "PqlnReceived"
  | "PqlnPendingSup"
  | "PdvPendingReceive"
  | "PdvReceived"
  | "PdvEntryPosted"
  | "PdvEntryApproved"
  | "Completed"
  | "Rejected";

export type DocumentPriority = "low" | "normal" | "high" | "urgent";
export type CustomerType = "company" | "individual";

export type CirculationAction =
  | "Created"
  | "PkhSupApproved"
  | "PqlnReceived"
  | "PqlnSubmittedSup"
  | "PqlnSupApproved"
  | "PdvReceived"
  | "PdvEntryPosted"
  | "PdvSupApprovedEntry"
  | "Supplemented"
  | "Completed"
  | "Rejected"
  | "Returned";

export interface DebtItem {
  id: string;
  debt_document_id: string;
  item_name: string;
  is_supplemented: boolean;
  supplemented_at: string | null;
  supplemented_by_user_id: string | null;
  supplemented_by_user_name?: string | null;
  notes: string | null;
  source_department_id: string | null;
  source_department_name?: string | null;
  source_department_code?: string | null;
  created_by_user_id: string | null;
  created_by_user_name?: string | null;
  created_at: string;
}

export interface DocumentFormValues {
  customerName: string;
  customerType: CustomerType;
  customerPhone: string;
  
  issueDate: string;
  dueDate: string;
  priority: DocumentPriority;
  notes: string;
  hasDebtDocuments: boolean;
  debtItems?: Array<{ itemName: string; notes?: string }>;
}

export interface Department {
  id: string;
  name: string;
  code: string; // PKH, PQLN, PDV
  description: string | null;
  sort_order: number;
  created_at: string;
}

export type UserRole = "staff" | "manager" | "admin";

export interface UserFormValues {
  name: string;
  email: string;
  password: string;
  departmentId: string;
  role: UserRole;
  isActive: boolean;
}

export interface AppUser {
  id: string;
  name: string;
  email: string;
  role: UserRole;
  department_id: string | null;
  is_active: boolean;
  auth_user_id: string | null;
  created_at: string;
  department?: Department | null;
}

export interface DebtDocument {
  id: string;
  document_number: string;
  customer_name: string;
  customer_phone: string | null;
  customer_type: CustomerType | null;
  
  issue_date: string;
  due_date: string;
  current_department_id: string | null;
  status: DocumentStatus;
  priority: DocumentPriority;
  notes: string | null;
  has_debt_documents: boolean;

  created_by_user_id: string | null;
  created_by_user_name?: string | null;

  pqln_receiver_id: string | null;
  pqln_receiver_name?: string | null;
  pqln_received_at: string | null;

  pdv_receiver_id: string | null;
  pdv_receiver_name?: string | null;
  pdv_received_at: string | null;

  entry_posted_at: string | null;
  completed_at: string | null;

  total_debt_items?: number;
  supplemented_debt_items?: number;

  created_at: string;
  updated_at: string;
  current_department?: Department | null;
}

export interface CirculationHistory {
  id: string;
  debt_document_id: string;
  from_department_id: string | null;
  to_department_id: string | null;
  action: CirculationAction;
  status_after: DocumentStatus | null;
  performed_by_user_id: string | null;
  notes: string | null;
  created_at: string;
  from_department?: Department | null;
  to_department?: Department | null;
  performed_by_user?: AppUser | null;
}

export interface DebtDocumentWithHistory extends DebtDocument {
  debt_items?: DebtItem[];
  circulation_history?: CirculationHistory[];
  edit_logs?: DocumentEditLog[];

  sup_pkh_approved_at?: string | null;
  sup_pqln_approved_at?: string | null;
  sup_pdv_approved_entry_at?: string | null;
}

export interface DocumentFieldChange {
  label: string;
  from: string;
  to: string;
}

export interface DocumentEditLog {
  id: string;
  debt_document_id: string;
  edited_by_user_id: string | null;
  changes: DocumentFieldChange[];
  created_at: string;
  edited_by_user?: AppUser | null;
}

export const STATUS_LABELS: Record<DocumentStatus, string> = {
  Draft: "Khởi tạo",
  PkhPendingSup: "Chờ SUP PKH duyệt",
  PqlnPendingReceive: "Chờ PQLN tiếp nhận",
  PqlnReceived: "PQLN đã tiếp nhận",
  PqlnPendingSup: "Chờ SUP PQLN duyệt",
  PdvPendingReceive: "Chờ PDV tiếp nhận",
  PdvReceived: "PDV đã tiếp nhận",
  PdvEntryPosted: "Đã đi bút toán",
  PdvEntryApproved: "Đã duyệt bút toán (Chờ nợ CT)",
  Completed: "ĐÃ HOÀN TẤT",
  Rejected: "Từ chối",
};

export const CUSTOMER_TYPE_LABELS: Record<CustomerType, string> = {
  company: "Công ty",
  individual: "Cá nhân",
};

export const USER_ROLE_LABELS: Record<UserRole, string> = {
  staff: "Nhân viên",
  manager: "Lãnh đạo phòng (SUP)",
  admin: "Quản trị viên",
};

export const PRIORITY_LABELS: Record<DocumentPriority, string> = {
  low: "Thấp",
  normal: "Bình thường",
  high: "Cao",
  urgent: "Khẩn cấp",
};

export const ACTION_LABELS: Record<CirculationAction, string> = {
  Created: "NV PKH khởi tạo",
  PkhSupApproved: "SUP PKH duyệt",
  PqlnReceived: "NV PQLN tiếp nhận",
  PqlnSubmittedSup: "NV PQLN đẩy SUP duyệt",
  PqlnSupApproved: "SUP PQLN duyệt",
  PdvReceived: "NV PDV tiếp nhận",
  PdvEntryPosted: "NV PDV đi bút toán",
  PdvSupApprovedEntry: "SUP PDV duyệt bút toán (Lần 1)",
  Supplemented: "Bổ sung chứng từ nợ",
  Completed: "SUP PDV duyệt hoàn tất",
  Rejected: "Từ chối hồ sơ",
  Returned: "Trả lại hồ sơ",
};

export const DEPARTMENT_FLOW = ["PKH", "PQLN", "PDV"] as const;

export type DepartmentCode = (typeof DEPARTMENT_FLOW)[number];

export const DEPARTMENT_LABELS: Record<DepartmentCode, string> = {
  PKH: "Phòng Khách hàng",
  PQLN: "Phòng Quản lý nợ",
  PDV: "Phòng Dịch vụ khách hàng",
};





