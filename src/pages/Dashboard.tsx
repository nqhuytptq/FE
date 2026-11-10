import { useMemo, useState, useRef } from "react";
import {
  ScrollText,
  Layers,
  LayoutDashboard,
  FileText,
  Building2,
  Bell,
  UserCog,
} from "lucide-react";
import {
  useDepartments,
  useAppUsers,
  useDebtDocuments,
  useDocumentWithHistory,
  createDebtDocument,
  updateDebtDocument,
  returnDocument,
} from "@/hooks/useData";
import { RoleProvider, useRole } from "@/context/RoleContext";
import { StatCards } from "@/components/StatCards";
import { FlowDiagram } from "@/components/FlowDiagram";
import { DocumentTable } from "@/components/DocumentTable";
import { IncomingDocumentsList } from "@/components/IncomingDocumentsList";
import { DocumentDetail } from "@/components/DocumentDetail";
import { UserMenu } from "@/components/UserMenu";
import { UserManagementPage } from "@/components/users/UserManagementPage";
import { DocumentFormModal } from "@/components/DocumentFormModal";

import type {
  AppUser,
  DebtDocument,
  Department,
  DocumentFormValues,
} from "@/types";

export function Dashboard({ currentUser }: { currentUser: AppUser }) {
  const { departments, loading: deptLoading } = useDepartments();
  const { users, loading: usersLoading } = useAppUsers();
  const {
    documents,
    loading: docsLoading,
    error: docsError,
    refetch: refetchDocs,
  } = useDebtDocuments();

  if (deptLoading || usersLoading) {
    return (
      <div className="flex h-screen items-center justify-center text-slate-400">
        <p className="text-sm">Đang tải dữ liệu...</p>
      </div>
    );
  }

  return (
    <RoleProvider
      currentUser={currentUser}
      departments={departments}
      users={users}
    >
      <DashboardContent
        documents={documents}
        docsLoading={docsLoading}
        docsError={docsError}
        refetchDocs={refetchDocs}
        departments={departments}
        users={users}
      />
    </RoleProvider>
  );
}

interface DashboardContentProps {
  documents: DebtDocument[];
  docsLoading: boolean;
  docsError: string | null;
  refetchDocs: () => void;
  departments: Department[];
  users: AppUser[];
}

function DashboardContent({
  documents,
  docsLoading,
  docsError,
  refetchDocs,
  departments,
  users,
}: DashboardContentProps) {
  const { currentDepartment, currentUser } = useRole();
  const isAdmin = currentUser.role === "admin";
  const [view, setView] = useState<"overview" | "users">("overview");
  const [selectedDocId, setSelectedDocId] = useState<string | null>(null);
  const [formMode, setFormMode] = useState<"create" | "edit" | null>(null);

  // Hook lấy chi tiết tài liệu (chỉ khai báo 1 lần)
  const {
    document: selectedDoc,
    loading: docLoading,
    refetch: refetchDoc,
  } = useDocumentWithHistory(selectedDocId);

  // Ref và hàm xử lý cuộn trang xuống section Văn bản nợ
  const debtDocsRef = useRef<HTMLDivElement>(null);

  const scrollToDebtDocs = () => {
    if (view !== "overview") {
      setView("overview");
    }
    setTimeout(() => {
      debtDocsRef.current?.scrollIntoView({
        behavior: "smooth",
        block: "start",
      });
    }, 50);
  };

  const stats = useMemo(() => {
    const pendingCount = documents.filter(
      (d) =>
        d.status === "PkhPendingSup" ||
        d.status === "PqlnPendingReceive" ||
        d.status === "PqlnPendingSup" ||
        d.status === "PdvPendingReceive",
    ).length;
    const approvedCount = documents.filter(
      (d) => d.status === "PdvEntryPosted" || d.status === "PdvEntryApproved",
    ).length;
    const overdueCount = documents.filter(
      (d) => d.status === "PdvEntryPosted" || d.status === "PdvEntryApproved",
    ).length;
    const completedCount = documents.filter(
      (d) => d.status === "Completed",
    ).length;
    const totalDebtItems = documents.reduce(
      (sum, d) => sum + (d.total_debt_items ?? 0),
      0,
    );
    return {
      totalDocuments: documents.length,
      pendingCount,
      approvedCount,
      overdueCount,
      completedCount,
      totalDebtItems,
      totalUsers: users.length,
      totalDepartments: departments.length,
    };
  }, [documents, departments, users]);

  const incomingCount = useMemo(() => {
    if (!currentDepartment) return 0;
    return documents.filter(
      (d) =>
        d.current_department_id === currentDepartment.id &&
        d.status !== "Completed" &&
        d.status !== "Rejected",
    ).length;
  }, [documents, currentDepartment]);

  const handleSelect = (doc: DebtDocument) => {
    setSelectedDocId(doc.id);
  };

  const canEditSelected =
    !!selectedDoc &&
    !!currentUser &&
    currentDepartment?.code === "PKH" &&
    selectedDoc.current_department_id === currentDepartment.id &&
    selectedDoc.created_by_user_id === currentUser.id &&
    selectedDoc.status !== "Completed" &&
    selectedDoc.status !== "Rejected";

  const handleCreate = async (params: DocumentFormValues): Promise<boolean> => {
    if (!currentDepartment || !currentUser) return false;
    const result = await createDebtDocument({
      ...params,
      departmentId: currentDepartment.id,
      userId: currentUser.id,
    });
    if (result.success) {
      refetchDocs();
      return true;
    }
    return false;
  };

  const handleSave = async (values: DocumentFormValues): Promise<boolean> => {
    if (formMode === "create") return handleCreate(values);
    if (!selectedDoc || !currentUser) return false;
    const result = await updateDebtDocument({
      ...values,
      documentId: selectedDoc.id,
    });
    if (result.success) {
      refetchDocs();
      refetchDoc();
      return true;
    }
    return false;
  };

  const handleActionPerformed = () => {
    refetchDocs();
    refetchDoc();
  };

  return (
    <div className="min-h-screen bg-slate-50">
      {/* Header */}
      <header className="sticky top-0 z-40 border-b border-slate-200 bg-white/80 backdrop-blur-md">
        <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
          <div className="flex h-16 items-center justify-between">
            <div className="flex items-center gap-3">
              <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-blue-600 text-white shadow-sm">
                <ScrollText className="h-5 w-5" strokeWidth={2} />
              </div>
              <div>
                <h1 className="text-base font-bold text-slate-900">
                  Quản lý Văn bản nợ luân chuyển
                </h1>
                <p className="text-xs text-slate-500">
                  Hệ thống theo dõi & điều phối văn bản nợ
                </p>
              </div>
            </div>
            <div className="flex items-center gap-4">
              {/* Incoming notification badge */}
              {incomingCount > 0 && (
                <div className="hidden items-center gap-1.5 rounded-full bg-amber-50 px-3 py-1.5 text-sm font-medium text-amber-700 ring-1 ring-amber-200 sm:flex">
                  <Bell className="h-4 w-4 animate-pulse" />
                  {incomingCount} văn bản chờ xử lý
                </div>
              )}
              <UserMenu />
            </div>
          </div>
        </div>
      </header>

      {/* Nav tabs */}
      <nav className="border-b border-slate-200 bg-white">
        <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
          <div className="flex gap-1">
            <TabButton
              icon={LayoutDashboard}
              label="Tổng quan"
              active={view === "overview"}
              onClick={() => setView("overview")}
            />
            <TabButton
              icon={FileText}
              label="Văn bản nợ"
              onClick={scrollToDebtDocs}
            />
            {isAdmin && (
              <TabButton
                icon={UserCog}
                label="Người dùng"
                active={view === "users"}
                onClick={() => setView("users")}
              />
            )}
            <div className="ml-auto hidden items-center gap-2 py-3 text-sm text-slate-400 sm:flex">
              <Layers className="h-4 w-4" />
              <span>.NET Solution · Blazor + ASP.NET Core</span>
            </div>
          </div>
        </div>
      </nav>

      {/* Main content */}
      <main className="mx-auto max-w-7xl space-y-6 px-4 py-6 sm:px-6 lg:px-8">
        {view === "users" && isAdmin ? (
          <UserManagementPage />
        ) : (
          <>
            {docsError && (
              <div className="rounded-xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700">
                Lỗi tải dữ liệu: {docsError}
              </div>
            )}

            {docsLoading ? (
              <div className="flex h-64 items-center justify-center text-slate-400">
                <p className="text-sm">Đang tải dữ liệu...</p>
              </div>
            ) : (
              <>
                <StatCards stats={stats} />
                <FlowDiagram departments={departments} documents={documents} />
                <IncomingDocumentsList
                  documents={documents}
                  onSelect={handleSelect}
                />
                <div ref={debtDocsRef} className="scroll-mt-20">
                  <DocumentTable
                    documents={documents}
                    onSelect={handleSelect}
                    onCreateNew={() => setFormMode("create")}
                  />
                </div>
              </>
            )}
          </>
        )}
      </main>

      {/* Modals */}
      <DocumentDetail
        document={selectedDoc}
        loading={docLoading}
        departments={departments}
        onClose={() => setSelectedDocId(null)}
        onActionPerformed={handleActionPerformed}
        onEdit={canEditSelected ? () => setFormMode("edit") : undefined}
      />

      {formMode && (
        <DocumentFormModal
          document={formMode === "edit" ? selectedDoc : null}
          onClose={() => setFormMode(null)}
          onSubmit={handleSave}
        />
      )}

      <footer className="border-t border-slate-200 py-6 text-center text-xs text-slate-400">
        Hệ thống Quản lý Văn bản nợ luân chuyển · Mô phỏng kiến trúc ASP.NET
        Core + Blazor WebAssembly
      </footer>
    </div>
  );

  function TabButton({
    icon: Icon,
    label,
    active,
    onClick,
  }: {
    icon: React.ElementType;
    label: string;
    active?: boolean;
    onClick?: () => void;
  }) {
    return (
      <button
        onClick={onClick}
        className={`flex items-center gap-2 border-b-2 px-3 py-3 text-sm font-medium transition ${
          active
            ? "border-blue-600 text-blue-600"
            : "border-transparent text-slate-500 hover:text-slate-700"
        }`}
      >
        <Icon className="h-4 w-4" />
        {label}
      </button>
    );
  }
}
