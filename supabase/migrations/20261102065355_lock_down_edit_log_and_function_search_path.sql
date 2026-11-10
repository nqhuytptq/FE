/*
# Make the document edit log append-only and pin a function search path

## Overview
The edit log must only ever grow. Signed-in users could still update or delete log rows
because table-wide privileges were inherited from the platform defaults. This migration
removes those privileges so the log can only be read and appended to.

## 1. Modified tables
- `document_edit_logs`: signed-in users keep SELECT and INSERT only. UPDATE, DELETE and
  TRUNCATE are revoked.

## 2. Modified functions
- `update_updated_at_column`: fixed search path so it cannot be redirected by a caller.

## 3. Important notes
1. Re-running this migration is safe.
2. No data is changed.
*/

REVOKE UPDATE, DELETE, TRUNCATE ON document_edit_logs FROM authenticated, anon;
GRANT SELECT, INSERT ON document_edit_logs TO authenticated;

ALTER FUNCTION public.update_updated_at_column() SET search_path = public;
