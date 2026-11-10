/*
# Remove table-wide privileges that bypass row security

## Overview
Platform default privileges gave signed-in users TRUNCATE, REFERENCES and TRIGGER on every
application table. TRUNCATE ignores row level security, so a signed-in user could empty a
table entirely. This migration removes those privileges. Row-level rules and the
app's intended permissions are unchanged.

## 1. Modified tables
- `app_users`, `departments`, `debt_documents`, `circulation_history`, `document_edit_logs`:
  TRUNCATE, REFERENCES and TRIGGER are revoked from signed-in users and anonymous visitors.

## 2. Important notes
1. No data is changed. Re-running this migration is safe.
2. Read, write and delete access the app needs is kept as granted in the previous migrations.
*/

REVOKE TRUNCATE, REFERENCES, TRIGGER ON app_users, departments, debt_documents, circulation_history, document_edit_logs FROM authenticated, anon;
