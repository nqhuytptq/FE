/*
# Add document edit log

## Overview
Records every change made to a debt document's information, who made it, and when.

## 1. New tables
- `document_edit_logs` (Nhật ký chỉnh sửa)
  - `id` (uuid, primary key)
  - `debt_document_id` (uuid, required): the edited document. Logs are removed with the document.
  - `edited_by_user_id` (uuid, nullable): the user who saved the change.
  - `changes` (jsonb): list of changed fields, each with `label`, `from` and `to` shown as readable text.
  - `created_at` (timestamptz): when the change was saved.

## 2. Security
- RLS enabled. The app has no sign-in, so access follows the same model as other tables.
- Grants: SELECT and INSERT only, to `anon` and `authenticated`. No UPDATE or DELETE, so saved log entries cannot be changed through the app.
- Policies: SELECT and INSERT only.

## 3. Important notes
1. The edited-by user is supplied by the app, not verified by a sign-in.
2. Index on `debt_document_id` for fast lookup of a document's history.
3. Safe to re-run.
*/

CREATE TABLE IF NOT EXISTS document_edit_logs (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  debt_document_id uuid NOT NULL REFERENCES debt_documents(id) ON DELETE CASCADE,
  edited_by_user_id uuid REFERENCES app_users(id) ON DELETE SET NULL,
  changes jsonb NOT NULL DEFAULT '[]'::jsonb,
  created_at timestamptz NOT NULL DEFAULT now()
);

ALTER TABLE document_edit_logs ENABLE ROW LEVEL SECURITY;

GRANT SELECT, INSERT ON document_edit_logs TO anon, authenticated;

DROP POLICY IF EXISTS "anon_select_document_edit_logs" ON document_edit_logs;
CREATE POLICY "anon_select_document_edit_logs" ON document_edit_logs FOR SELECT
  TO anon, authenticated USING (true);

DROP POLICY IF EXISTS "anon_insert_document_edit_logs" ON document_edit_logs;
CREATE POLICY "anon_insert_document_edit_logs" ON document_edit_logs FOR INSERT
  TO anon, authenticated WITH CHECK (true);

CREATE INDEX IF NOT EXISTS idx_document_edit_logs_document ON document_edit_logs(debt_document_id);
