/*
# Allow 'completed' in circulation history and store status snapshot

## 1. Modified tables
- `circulation_history`
  - CHECK constraint on `action` now also accepts 'completed' (Phòng Kế toán xác nhận đã thu nợ).
  - New nullable column `status_after` (text): the document status right after the step happened.
    Used by the timeline to show the "Trạng thái" of each step.

## 2. Security
- No changes to RLS policies or grants. Existing anon/authenticated policies still apply.

## 3. Notes
- No existing rows are modified or removed. Older history rows keep `status_after = NULL`.
- Idempotent: constraint is dropped with IF EXISTS before being recreated.
*/

ALTER TABLE circulation_history DROP CONSTRAINT IF EXISTS circulation_history_action_check;
ALTER TABLE circulation_history ADD CONSTRAINT circulation_history_action_check
  CHECK (action IN ('created', 'forwarded', 'approved', 'rejected', 'returned', 'escalated', 'completed'));

ALTER TABLE circulation_history ADD COLUMN IF NOT EXISTS status_after text;

ALTER TABLE circulation_history DROP CONSTRAINT IF EXISTS circulation_history_status_after_check;
ALTER TABLE circulation_history ADD CONSTRAINT circulation_history_status_after_check
  CHECK (status_after IS NULL OR status_after IN ('pending', 'approved', 'overdue', 'rejected', 'completed'));
