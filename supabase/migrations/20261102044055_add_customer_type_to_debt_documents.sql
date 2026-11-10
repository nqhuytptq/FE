/*
# Add customer type to debt documents

## Overview
Adds a classification for the customer on each debt document: company or individual.

## 1. Modified tables
- `debt_documents`
  - New column `customer_type` (text, nullable). Allowed values: 'company', 'individual'.
  - Existing documents are left NULL because their customer type was never recorded.

## 2. Security
- No new tables. Existing RLS policies and grants on `debt_documents` already cover the new column.

## 3. Important notes
1. The CHECK constraint only accepts 'company' or 'individual' (or NULL for legacy rows).
2. The column is added idempotently so the migration can be safely re-run.
*/

ALTER TABLE debt_documents ADD COLUMN IF NOT EXISTS customer_type text;

DO $$
BEGIN
  IF NOT EXISTS (
    SELECT 1 FROM pg_constraint
    WHERE conname = 'debt_documents_customer_type_check'
      AND conrelid = 'debt_documents'::regclass
  ) THEN
    ALTER TABLE debt_documents
      ADD CONSTRAINT debt_documents_customer_type_check
      CHECK (customer_type IS NULL OR customer_type IN ('company', 'individual'));
  END IF;
END $$;
