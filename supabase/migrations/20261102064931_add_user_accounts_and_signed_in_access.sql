/*
# Add user accounts and restrict data access to signed-in users

## Overview
The app now requires sign-in. Each staff member signs in with email and password, and
their login is linked to their record in the user list. Data is only available to
signed-in users. Account management is done by administrators through a server function.

## 1. Modified tables
- `app_users`
  - New column `auth_user_id` (uuid, unique, nullable): links the staff record to its login account.
  - Role must be one of: 'staff', 'manager', 'admin'. 'admin' manages users.
  - Client users can only READ this table. Changes to name, department, role, and active status
    are only made by the administrator server function, so no one can grant themselves admin rights.

## 2. Access rules
- `departments`: signed-in users can read. No client writes.
- `app_users`: signed-in users can read. No client writes.
- `debt_documents`, `circulation_history`: signed-in users can read, create, update and delete.
  All staff share the same document workspace.
- `document_edit_logs`: signed-in users can read and add entries. No updates or deletes.
- The public (anonymous) role no longer has any access to these tables.

## 3. Trustworthy "who did it"
- `document_edit_logs.edited_by_user_id` and `circulation_history.performed_by_user_id`
  are filled in by the database from the signed-in session on insert. Client values are overwritten.

## 4. Important notes
1. Anonymous access is removed. The app cannot load data until a user signs in.
2. The migration is safe to re-run.
*/

ALTER TABLE app_users ADD COLUMN IF NOT EXISTS auth_user_id uuid UNIQUE REFERENCES auth.users(id) ON DELETE SET NULL;

DO $$
BEGIN
  IF NOT EXISTS (
    SELECT 1 FROM pg_constraint
    WHERE conname = 'app_users_role_check' AND conrelid = 'app_users'::regclass
  ) THEN
    ALTER TABLE app_users
      ADD CONSTRAINT app_users_role_check CHECK (role IN ('staff', 'manager', 'admin'));
  END IF;
END $$;

REVOKE ALL ON departments, app_users, debt_documents, circulation_history, document_edit_logs FROM anon;

DROP POLICY IF EXISTS "anon_select_departments" ON departments;
DROP POLICY IF EXISTS "anon_insert_departments" ON departments;
DROP POLICY IF EXISTS "anon_update_departments" ON departments;
DROP POLICY IF EXISTS "anon_delete_departments" ON departments;
DROP POLICY IF EXISTS "anon_select_app_users" ON app_users;
DROP POLICY IF EXISTS "anon_insert_app_users" ON app_users;
DROP POLICY IF EXISTS "anon_update_app_users" ON app_users;
DROP POLICY IF EXISTS "anon_delete_app_users" ON app_users;
DROP POLICY IF EXISTS "anon_select_debt_documents" ON debt_documents;
DROP POLICY IF EXISTS "anon_insert_debt_documents" ON debt_documents;
DROP POLICY IF EXISTS "anon_update_debt_documents" ON debt_documents;
DROP POLICY IF EXISTS "anon_delete_debt_documents" ON debt_documents;
DROP POLICY IF EXISTS "anon_select_circulation_history" ON circulation_history;
DROP POLICY IF EXISTS "anon_insert_circulation_history" ON circulation_history;
DROP POLICY IF EXISTS "anon_update_circulation_history" ON circulation_history;
DROP POLICY IF EXISTS "anon_delete_circulation_history" ON circulation_history;
DROP POLICY IF EXISTS "anon_select_document_edit_logs" ON document_edit_logs;
DROP POLICY IF EXISTS "anon_insert_document_edit_logs" ON document_edit_logs;

REVOKE INSERT, UPDATE, DELETE ON departments FROM authenticated;
GRANT SELECT ON departments TO authenticated;
DROP POLICY IF EXISTS "signed_in_select_departments" ON departments;
CREATE POLICY "signed_in_select_departments" ON departments FOR SELECT
  TO authenticated USING (true);

REVOKE INSERT, UPDATE, DELETE ON app_users FROM authenticated;
GRANT SELECT ON app_users TO authenticated;
GRANT SELECT, INSERT, UPDATE ON app_users TO service_role;
DROP POLICY IF EXISTS "signed_in_select_app_users" ON app_users;
CREATE POLICY "signed_in_select_app_users" ON app_users FOR SELECT
  TO authenticated USING (true);

GRANT SELECT, INSERT, UPDATE, DELETE ON debt_documents TO authenticated;
DROP POLICY IF EXISTS "signed_in_select_debt_documents" ON debt_documents;
CREATE POLICY "signed_in_select_debt_documents" ON debt_documents FOR SELECT
  TO authenticated USING (true);
DROP POLICY IF EXISTS "signed_in_insert_debt_documents" ON debt_documents;
CREATE POLICY "signed_in_insert_debt_documents" ON debt_documents FOR INSERT
  TO authenticated WITH CHECK (true);
DROP POLICY IF EXISTS "signed_in_update_debt_documents" ON debt_documents;
CREATE POLICY "signed_in_update_debt_documents" ON debt_documents FOR UPDATE
  TO authenticated USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS "signed_in_delete_debt_documents" ON debt_documents;
CREATE POLICY "signed_in_delete_debt_documents" ON debt_documents FOR DELETE
  TO authenticated USING (true);

GRANT SELECT, INSERT, UPDATE, DELETE ON circulation_history TO authenticated;
DROP POLICY IF EXISTS "signed_in_select_circulation_history" ON circulation_history;
CREATE POLICY "signed_in_select_circulation_history" ON circulation_history FOR SELECT
  TO authenticated USING (true);
DROP POLICY IF EXISTS "signed_in_insert_circulation_history" ON circulation_history;
CREATE POLICY "signed_in_insert_circulation_history" ON circulation_history FOR INSERT
  TO authenticated WITH CHECK (true);
DROP POLICY IF EXISTS "signed_in_update_circulation_history" ON circulation_history;
CREATE POLICY "signed_in_update_circulation_history" ON circulation_history FOR UPDATE
  TO authenticated USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS "signed_in_delete_circulation_history" ON circulation_history;
CREATE POLICY "signed_in_delete_circulation_history" ON circulation_history FOR DELETE
  TO authenticated USING (true);

GRANT SELECT, INSERT ON document_edit_logs TO authenticated;
DROP POLICY IF EXISTS "signed_in_select_document_edit_logs" ON document_edit_logs;
CREATE POLICY "signed_in_select_document_edit_logs" ON document_edit_logs FOR SELECT
  TO authenticated USING (true);
DROP POLICY IF EXISTS "signed_in_insert_document_edit_logs" ON document_edit_logs;
CREATE POLICY "signed_in_insert_document_edit_logs" ON document_edit_logs FOR INSERT
  TO authenticated WITH CHECK (true);

CREATE OR REPLACE FUNCTION set_edit_log_actor()
RETURNS trigger
LANGUAGE plpgsql
SET search_path = public
AS $$
BEGIN
  NEW.edited_by_user_id := (SELECT id FROM app_users WHERE auth_user_id = auth.uid());
  RETURN NEW;
END;
$$;

DROP TRIGGER IF EXISTS trg_set_edit_log_actor ON document_edit_logs;
CREATE TRIGGER trg_set_edit_log_actor
  BEFORE INSERT ON document_edit_logs
  FOR EACH ROW EXECUTE FUNCTION set_edit_log_actor();

CREATE OR REPLACE FUNCTION set_circulation_actor()
RETURNS trigger
LANGUAGE plpgsql
SET search_path = public
AS $$
BEGIN
  NEW.performed_by_user_id := (SELECT id FROM app_users WHERE auth_user_id = auth.uid());
  RETURN NEW;
END;
$$;

DROP TRIGGER IF EXISTS trg_set_circulation_actor ON circulation_history;
CREATE TRIGGER trg_set_circulation_actor
  BEFORE INSERT ON circulation_history
  FOR EACH ROW EXECUTE FUNCTION set_circulation_actor();
