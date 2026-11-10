/*
# Hệ thống Quản lý Văn bản nợ luân chuyển - Schema

## Tổng quan
Tạo cấu trúc cơ sở dữ liệu cho hệ thống quản lý và theo dõi văn bản nợ
luân chuyển giữa các phòng ban.

## 1. Bảng mới
- `departments` (Phòng ban): Phòng Khách hàng, Phòng Quản lý nợ, Phòng Kế toán
  - id, name, code, description, sort_order, created_at
- `app_users` (Người dùng hệ thống): Thuộc một phòng ban để phân quyền xử lý
  - id, name, email, role, department_id (FK), is_active, created_at
- `debt_documents` (Văn bản nợ): Thông tin văn bản nợ và trạng thái luân chuyển
  - id, document_number, customer_name, customer_phone, debt_amount, issue_date, due_date, current_department_id, status, priority, notes, created_at, updated_at
- `circulation_history` (Lịch sử luân chuyển): Theo dõi mỗi lần văn bản chuyển phòng ban
  - id, debt_document_id, from_department_id, to_department_id, action, performed_by_user_id, notes, created_at

## 2. Security
- RLS được bật trên tất cả bảng (single-tenant demo, không yêu cầu đăng nhập)
- Cấp quyền CRUD cho anon + authenticated
- Dữ liệu được chia sẻ công khai cho mục đích demo

## 3. Indexes
- Index trên document_number, status, current_department_id của debt_documents
- Index trên debt_document_id của circulation_history
*/

-- ==================== DEPARTMENTS ====================
CREATE TABLE IF NOT EXISTS departments (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  name text NOT NULL,
  code text NOT NULL UNIQUE,
  description text,
  sort_order int NOT NULL DEFAULT 0,
  created_at timestamptz DEFAULT now()
);

ALTER TABLE departments ENABLE ROW LEVEL SECURITY;
GRANT SELECT, INSERT, UPDATE, DELETE ON departments TO anon, authenticated;

DROP POLICY IF EXISTS "anon_select_departments" ON departments;
CREATE POLICY "anon_select_departments" ON departments FOR SELECT
  TO anon, authenticated USING (true);
DROP POLICY IF EXISTS "anon_insert_departments" ON departments;
CREATE POLICY "anon_insert_departments" ON departments FOR INSERT
  TO anon, authenticated WITH CHECK (true);
DROP POLICY IF EXISTS "anon_update_departments" ON departments;
CREATE POLICY "anon_update_departments" ON departments FOR UPDATE
  TO anon, authenticated USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS "anon_delete_departments" ON departments;
CREATE POLICY "anon_delete_departments" ON departments FOR DELETE
  TO anon, authenticated USING (true);

-- ==================== APP_USERS ====================
CREATE TABLE IF NOT EXISTS app_users (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  name text NOT NULL,
  email text NOT NULL UNIQUE,
  role text NOT NULL DEFAULT 'staff',
  department_id uuid REFERENCES departments(id) ON DELETE SET NULL,
  is_active boolean NOT NULL DEFAULT true,
  created_at timestamptz DEFAULT now()
);

ALTER TABLE app_users ENABLE ROW LEVEL SECURITY;
GRANT SELECT, INSERT, UPDATE, DELETE ON app_users TO anon, authenticated;

DROP POLICY IF EXISTS "anon_select_app_users" ON app_users;
CREATE POLICY "anon_select_app_users" ON app_users FOR SELECT
  TO anon, authenticated USING (true);
DROP POLICY IF EXISTS "anon_insert_app_users" ON app_users;
CREATE POLICY "anon_insert_app_users" ON app_users FOR INSERT
  TO anon, authenticated WITH CHECK (true);
DROP POLICY IF EXISTS "anon_update_app_users" ON app_users;
CREATE POLICY "anon_update_app_users" ON app_users FOR UPDATE
  TO anon, authenticated USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS "anon_delete_app_users" ON app_users;
CREATE POLICY "anon_delete_app_users" ON app_users FOR DELETE
  TO anon, authenticated USING (true);

-- ==================== DEBT_DOCUMENTS ====================
CREATE TABLE IF NOT EXISTS debt_documents (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  document_number text NOT NULL UNIQUE,
  customer_name text NOT NULL,
  customer_phone text,
  debt_amount numeric(14,2) NOT NULL DEFAULT 0,
  issue_date date NOT NULL,
  due_date date NOT NULL,
  current_department_id uuid REFERENCES departments(id) ON DELETE SET NULL,
  status text NOT NULL DEFAULT 'pending' CHECK (status IN ('pending', 'approved', 'overdue', 'rejected')),
  priority text NOT NULL DEFAULT 'normal' CHECK (priority IN ('low', 'normal', 'high', 'urgent')),
  notes text,
  created_at timestamptz DEFAULT now(),
  updated_at timestamptz DEFAULT now()
);

ALTER TABLE debt_documents ENABLE ROW LEVEL SECURITY;
GRANT SELECT, INSERT, UPDATE, DELETE ON debt_documents TO anon, authenticated;

DROP POLICY IF EXISTS "anon_select_debt_documents" ON debt_documents;
CREATE POLICY "anon_select_debt_documents" ON debt_documents FOR SELECT
  TO anon, authenticated USING (true);
DROP POLICY IF EXISTS "anon_insert_debt_documents" ON debt_documents;
CREATE POLICY "anon_insert_debt_documents" ON debt_documents FOR INSERT
  TO anon, authenticated WITH CHECK (true);
DROP POLICY IF EXISTS "anon_update_debt_documents" ON debt_documents;
CREATE POLICY "anon_update_debt_documents" ON debt_documents FOR UPDATE
  TO anon, authenticated USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS "anon_delete_debt_documents" ON debt_documents;
CREATE POLICY "anon_delete_debt_documents" ON debt_documents FOR DELETE
  TO anon, authenticated USING (true);

CREATE INDEX IF NOT EXISTS idx_debt_documents_document_number ON debt_documents(document_number);
CREATE INDEX IF NOT EXISTS idx_debt_documents_status ON debt_documents(status);
CREATE INDEX IF NOT EXISTS idx_debt_documents_department ON debt_documents(current_department_id);

-- ==================== CIRCULATION_HISTORY ====================
CREATE TABLE IF NOT EXISTS circulation_history (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  debt_document_id uuid NOT NULL REFERENCES debt_documents(id) ON DELETE CASCADE,
  from_department_id uuid REFERENCES departments(id) ON DELETE SET NULL,
  to_department_id uuid REFERENCES departments(id) ON DELETE SET NULL,
  action text NOT NULL CHECK (action IN ('created', 'forwarded', 'approved', 'rejected', 'returned', 'escalated')),
  performed_by_user_id uuid REFERENCES app_users(id) ON DELETE SET NULL,
  notes text,
  created_at timestamptz DEFAULT now()
);

ALTER TABLE circulation_history ENABLE ROW LEVEL SECURITY;
GRANT SELECT, INSERT, UPDATE, DELETE ON circulation_history TO anon, authenticated;

DROP POLICY IF EXISTS "anon_select_circulation_history" ON circulation_history;
CREATE POLICY "anon_select_circulation_history" ON circulation_history FOR SELECT
  TO anon, authenticated USING (true);
DROP POLICY IF EXISTS "anon_insert_circulation_history" ON circulation_history;
CREATE POLICY "anon_insert_circulation_history" ON circulation_history FOR INSERT
  TO anon, authenticated WITH CHECK (true);
DROP POLICY IF EXISTS "anon_update_circulation_history" ON circulation_history;
CREATE POLICY "anon_update_circulation_history" ON circulation_history FOR UPDATE
  TO anon, authenticated USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS "anon_delete_circulation_history" ON circulation_history;
CREATE POLICY "anon_delete_circulation_history" ON circulation_history FOR DELETE
  TO anon, authenticated USING (true);

CREATE INDEX IF NOT EXISTS idx_circulation_history_document ON circulation_history(debt_document_id);
CREATE INDEX IF NOT EXISTS idx_circulation_history_created ON circulation_history(created_at);

-- ==================== TRIGGER: updated_at ====================
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
  NEW.updated_at = now();
  RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trigger_debt_documents_updated_at ON debt_documents;
CREATE TRIGGER trigger_debt_documents_updated_at
  BEFORE UPDATE ON debt_documents
  FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
