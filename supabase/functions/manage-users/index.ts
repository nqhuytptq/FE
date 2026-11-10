import { createClient } from "npm:@supabase/supabase-js@2";

const corsHeaders = {
  "Access-Control-Allow-Origin": "*",
  "Access-Control-Allow-Methods": "GET, POST, PUT, DELETE, OPTIONS",
  "Access-Control-Allow-Headers": "Content-Type, Authorization, X-Client-Info, Apikey",
};

const ROLES = ["staff", "manager", "admin"];
const UUID_PATTERN = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;
const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
const MIN_PASSWORD_LENGTH = 8;
const BANNED_DURATION = "876000h";

const admin = createClient(
  Deno.env.get("SUPABASE_URL")!,
  Deno.env.get("SUPABASE_SERVICE_ROLE_KEY")!,
  { auth: { persistSession: false, autoRefreshToken: false } },
);

class ValidationError extends Error {}

function json(body: unknown, status = 200) {
  return new Response(JSON.stringify(body), {
    status,
    headers: { ...corsHeaders, "Content-Type": "application/json" },
  });
}

function readName(body: Record<string, unknown>) {
  const name = typeof body.name === "string" ? body.name.trim() : "";
  if (!name || name.length > 100) throw new ValidationError("Tên người dùng không hợp lệ");
  return name;
}

function readDepartmentId(body: Record<string, unknown>) {
  const departmentId = typeof body.departmentId === "string" ? body.departmentId : "";
  if (!UUID_PATTERN.test(departmentId)) throw new ValidationError("Vui lòng chọn phòng ban");
  return departmentId;
}

function readRole(body: Record<string, unknown>) {
  const role = typeof body.role === "string" ? body.role : "";
  if (!ROLES.includes(role)) throw new ValidationError("Vai trò không hợp lệ");
  return role;
}

function readPassword(body: Record<string, unknown>, required: boolean) {
  const password = typeof body.password === "string" ? body.password : "";
  if (!password) {
    if (required) throw new ValidationError("Vui lòng nhập mật khẩu");
    return null;
  }
  if (password.length < MIN_PASSWORD_LENGTH) {
    throw new ValidationError(`Mật khẩu phải có ít nhất ${MIN_PASSWORD_LENGTH} ký tự`);
  }
  return password;
}

async function assertDepartmentExists(departmentId: string) {
  const { data, error } = await admin
    .from("departments")
    .select("id")
    .eq("id", departmentId)
    .maybeSingle();
  if (error) throw error;
  if (!data) throw new ValidationError("Phòng ban không tồn tại");
}

async function createUser(body: Record<string, unknown>) {
  const name = readName(body);
  const departmentId = readDepartmentId(body);
  const role = readRole(body);
  const password = readPassword(body, true)!;
  const email = typeof body.email === "string" ? body.email.trim().toLowerCase() : "";
  if (!EMAIL_PATTERN.test(email)) throw new ValidationError("Email không hợp lệ");

  const { data: existing, error: existingError } = await admin
    .from("app_users")
    .select("id")
    .eq("email", email)
    .maybeSingle();
  if (existingError) throw existingError;
  if (existing) {
    throw new ValidationError("Email này đã có trong danh sách. Hãy sửa người dùng đó để cấp mật khẩu.");
  }

  await assertDepartmentExists(departmentId);

  const { data: created, error: createError } = await admin.auth.admin.createUser({
    email,
    password,
    email_confirm: true,
  });
  if (createError || !created.user) {
    console.error("createUser auth failed", createError);
    throw new ValidationError("Không thể tạo tài khoản đăng nhập với email này");
  }

  const { data: row, error: insertError } = await admin
    .from("app_users")
    .insert({
      name,
      email,
      role,
      department_id: departmentId,
      is_active: true,
      auth_user_id: created.user.id,
    })
    .select("*")
    .single();

  if (insertError) {
    await admin.auth.admin.deleteUser(created.user.id);
    throw insertError;
  }

  return json({ user: row }, 201);
}

async function updateUser(body: Record<string, unknown>, callerId: string) {
  const userId = typeof body.userId === "string" ? body.userId : "";
  if (!UUID_PATTERN.test(userId)) throw new ValidationError("Người dùng không hợp lệ");

  const { data: target, error: targetError } = await admin
    .from("app_users")
    .select("id, email, auth_user_id, is_active")
    .eq("id", userId)
    .maybeSingle();
  if (targetError) throw targetError;
  if (!target) return json({ error: "Không tìm thấy người dùng" }, 404);

  const name = readName(body);
  const departmentId = readDepartmentId(body);
  const role = readRole(body);
  const password = readPassword(body, false);
  const isActive = typeof body.isActive === "boolean" ? body.isActive : target.is_active;

  if (target.id === callerId) {
    if (!isActive) throw new ValidationError("Không thể khóa tài khoản đang đăng nhập");
    if (role !== "admin") throw new ValidationError("Không thể bỏ quyền quản trị của chính mình");
  }

  await assertDepartmentExists(departmentId);

  const { data: row, error: updateError } = await admin
    .from("app_users")
    .update({ name, role, department_id: departmentId, is_active: isActive })
    .eq("id", userId)
    .select("*")
    .single();
  if (updateError) throw updateError;

  let authUserId = target.auth_user_id;
  if (password) {
    if (authUserId) {
      const { error } = await admin.auth.admin.updateUserById(authUserId, { password });
      if (error) throw error;
    } else {
      const { data: created, error } = await admin.auth.admin.createUser({
        email: target.email,
        password,
        email_confirm: true,
      });
      if (error || !created.user) throw error ?? new Error("Auth user not created");
      authUserId = created.user.id;
      const { error: linkError } = await admin
        .from("app_users")
        .update({ auth_user_id: authUserId })
        .eq("id", userId);
      if (linkError) throw linkError;
    }
  }

  if (authUserId) {
    const { error } = await admin.auth.admin.updateUserById(authUserId, {
      ban_duration: isActive ? "none" : BANNED_DURATION,
    });
    if (error) throw error;
  }

  return json({ user: { ...row, auth_user_id: authUserId } });
}

Deno.serve(async (req) => {
  if (req.method === "OPTIONS") return new Response(null, { status: 200, headers: corsHeaders });

  try {
    const token = req.headers.get("Authorization")?.replace(/^Bearer\s+/i, "");
    if (!token) return json({ error: "Vui lòng đăng nhập" }, 401);

    const { data: authData, error: authError } = await admin.auth.getUser(token);
    if (authError || !authData.user) return json({ error: "Phiên đăng nhập không hợp lệ" }, 401);

    const { data: caller, error: callerError } = await admin
      .from("app_users")
      .select("id, role, is_active")
      .eq("auth_user_id", authData.user.id)
      .maybeSingle();
    if (callerError) throw callerError;
    if (!caller || !caller.is_active || caller.role !== "admin") {
      return json({ error: "Bạn không có quyền quản lý người dùng" }, 403);
    }

    const body = (await req.json()) as Record<string, unknown>;
    if (body.action === "create") return await createUser(body);
    if (body.action === "update") return await updateUser(body, caller.id);
    return json({ error: "Thao tác không hợp lệ" }, 400);
  } catch (err) {
    if (err instanceof ValidationError) return json({ error: err.message }, 400);
    console.error("manage-users failed", err);
    return json({ error: "Đã xảy ra lỗi. Vui lòng thử lại." }, 500);
  }
});
