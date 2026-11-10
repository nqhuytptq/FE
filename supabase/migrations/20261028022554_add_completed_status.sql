/*
# Add 'completed' status to debt documents

## 1. Changes
- Alter CHECK constraint on debt_documents.status to include 'completed'
- 'completed' = Phòng Kế toán đã xác nhận thu nợ xong, quy trình kết thúc
- Add 'completed' to ACTION_LABELS / CirculationAction in frontend

## 2. Notes
- No data loss: existing rows keep their current status values
- Constraint dropped and recreated to include new value
*/

ALTER TABLE debt_documents DROP CONSTRAINT IF EXISTS debt_documents_status_check;
ALTER TABLE debt_documents ADD CONSTRAINT debt_documents_status_check
  CHECK (status IN ('pending', 'approved', 'overdue', 'rejected', 'completed'));
