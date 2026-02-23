# ADR-008: Import/Export Design

## Status
Accepted

## Context
TaskHub must support data portability for onboarding/offboarding. Export allows backing up or migrating data; import allows bulk loading with validation and error reporting.

## Decision
Support JSON and CSV formats for both import and export. Validate each imported record individually and produce a detailed rejection report. Support idempotent imports via optional `clientProvidedId`.

## Options Considered

### Option A: JSON Only
- Simpler implementation
- Less accessible for non-technical users who prefer spreadsheets

### Option B: JSON and CSV (Chosen)
- JSON for programmatic use, CSV for spreadsheet users
- Both formats include all required fields
- Template download available for correct formatting

### Option C: Custom Binary Format
- Unnecessary complexity, no user benefit

## Consequences
- Export includes: title, description, status, priority, tags, dueDate
- Import validates each row: required fields, length limits, enum values, tag format, date validity
- Import report: acceptedCount, rejectedCount, error details (row number, clientProvidedId, message)
- Idempotency: clientProvidedId de-duplicates; overwrite option available
- No sensitive data leaked in rejection messages

## Follow-ups
- Add streaming export for large datasets
- Support XLSX format for better spreadsheet compatibility
- Add import preview/dry-run mode
