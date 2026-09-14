# Data Model: Document Upload and Management

## Overview

The document feature extends the existing ContosoDashboard data model with structured metadata for stored files, access grants, and activity tracking. The design stays consistent with the repository’s current pattern: integer primary keys, user/project references, EF Core navigation properties, and service-level authorization enforcement.

## Entities

### Document

Represents a stored document with metadata and a file reference.

**Fields**
- `DocumentId` (int, PK): primary key for the document record
- `Title` (string, required): user-facing name of the document
- `Description` (string, optional): longer explanation or context
- `Category` (string, required): values such as `Project Documents`, `Team Resources`, `Personal Files`, `Reports`, `Presentations`, `Other`
- `FileName` (string, required): logical filename or original name, not trusted for storage path creation
- `StoredFilePath` (string, required): relative path to the securely stored file
- `MimeType` (string, optional): MIME type such as `application/pdf` with a length suitable for 255 characters
- `FileSizeBytes` (long): record size for display and validation
- `UploadedByUserId` (int, FK): uploader reference
- `ProjectId` (int?, FK): optional project association
- `TaskId` (int?, FK): optional task attachment association
- `UploadedDateUtc` (DateTime): timestamp for upload event
- `UpdatedDateUtc` (DateTime): last metadata change timestamp
- `IsDeleted` (bool): soft-deletion flag for audit / recovery workflows

**Relationships**
- Many-to-one with `User` via `UploadedByUserId`
- Many-to-one with `Project` via `ProjectId`
- Many-to-one with `TaskItem` via `TaskId`
- One-to-many with `DocumentShare`
- One-to-many with `AuditEvent`

**Validation Rules**
- `Title` must be present and not blank
- `Category` must be one of the allowed values
- `StoredFilePath` must be generated server-side and must not be user-controlled
- `MimeType` must be a valid type string and within the configured max length
- `ProjectId` and `TaskId` may be null for personal uploads

### DocumentShare

Represents permission to share a document with a user or group context.

**Fields**
- `DocumentShareId` (int, PK)
- `DocumentId` (int, FK)
- `UserId` (int, FK): recipient user
- `GrantedByUserId` (int, FK): user who granted access
- `SharedDateUtc` (DateTime)
- `IsActive` (bool)

**Relationships**
- Many-to-one with `Document`
- Many-to-one with `User` (recipient)
- Many-to-one with `User` (granting user)

**Validation Rules**
- Recipient and granting user must exist
- A share record must map to a valid document
- Shares must be active only when access is still valid

### AuditEvent

Represents an audit record for document actions.

**Fields**
- `AuditEventId` (int, PK)
- `DocumentId` (int?, FK): related document if applicable
- `UserId` (int, FK): actor responsible for the action
- `ActionType` (string, required): upload, download, share, delete, metadata-edit
- `ActionTimestampUtc` (DateTime)
- `Details` (string, optional): serialized or descriptive context

**Relationships**
- Many-to-one with `Document`
- Many-to-one with `User`

**Validation Rules**
- `ActionType` must be recognized and non-empty
- `UserId` must reference an existing user
- `Details` may be optional but should be present for high-value events

## Existing Model Extensions

The following existing models will be related to document activity:
- `User` gains a collection of uploaded documents and shared access records
- `Project` gains a collection of project documents
- `TaskItem` gains an optional relation to attached documents
- `Notification` can be extended or reused for share and assignment notifications

## State Transitions

Document life cycle will be simple and explicit:
- `Draft/Uploaded` → `Active`
- `Active` → `Shared`
- `Active` → `Deleted`
- `Active` → `Updated`

The actual feature may implement most of this logically without a formal complex state machine, but the underlying operations should be auditable and easy to reason about.

## Design Notes

- Do not trust user-provided filenames for file storage paths
- Use GUID-based storage names to avoid collisions and path traversal
- Ensure document access checks happen in the service layer before returning data or allowing writes
- Keep category values as text rather than enum-backed integer values to match the requirement and simplify training examples
