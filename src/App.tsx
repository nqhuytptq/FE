import { AuthProvider, useAuth } from "@/context/AuthContext";
import { Dashboard } from "@/pages/Dashboard";
import { LoginPage } from "@/components/LoginPage";
import { AccessBlockedScreen } from "@/components/AccessBlockedScreen";

function AppGate() {
  const { status, currentUser } = useAuth();

  if (status === "loading") {
    return (
      <div className="flex h-screen items-center justify-center text-slate-400">
        <p className="text-sm">Đang tải...</p>
      </div>
    );
  }
  if (status === "signed-out") return <LoginPage />;
  if (status === "ready" && currentUser) return <Dashboard currentUser={currentUser} />;
  return <AccessBlockedScreen status={status === "error" ? "error" : "no-access"} />;
}

function App() {
  return (
    <AuthProvider>
      <AppGate />
    </AuthProvider>
  );
}

export default App;




