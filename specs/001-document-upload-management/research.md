# Research: Document Upload and Management

## Summary

The document feature is a fit for the current Blazor Server architecture because the app already centralizes user, project, notification, and permission logic in services. The main design decisions focus on secure file handling, access enforcement, and local storage patterns that remain compatible with a future Azure migration.

## Decisions

### Decision: Local filesystem storage with a storage abstraction
- Decision: Store uploaded files outside `wwwroot` in a protected local directory such as `AppData/uploads`, and access them via a service abstraction.
- Rationale: The repository’s constitution requires offline-first operation and the stakeholder requirement explicitly calls for local storage with `IFileStorageService` migration support.
- Alternatives considered:
  - In-DB binary storage: Rejected because it is harder to manage, inefficient for large files, and inconsistent with the existing local-file-first design.
  - Storing files under `wwwroot`: Rejected because it exposes uploaded content directly and weakens security controls.

### Decision: Service-layer authorization instead of UI-only checks
- Decision: Validate project membership, uploader ownership, and role permissions in a document service before read/write operations.
- Rationale: This matches the repo’s current service-layer security pattern and prevents IDOR and unauthorized access even when a user manipulates URLs or requests directly.
- Alternatives considered:
  - UI-only checks: Rejected because the application already treats service enforcement as the trusted boundary.
  - Authorization only in Razor pages: Rejected because the page layer is not a reliable security boundary.

### Decision: Metadata-first document model with integer keys
- Decision: Use integer `DocumentId` keys and text category values consistent with the existing `User` and `Project` integer key pattern.
- Rationale: The stakeholder requirement explicitly prescribes integer keys and text-based categories for simplicity and consistency with the rest of the training data model.
- Alternatives considered:
  - GUID keys: Rejected because they would diverge from existing schema patterns and add unnecessary complexity for this training app.
  - Enum-backed categories: Rejected because the requirement states that category should be stored as text values for simple data handling.

### Decision: Role-based entity access model
- Decision: Permit document owners, team leads, project managers, and administrators to access documents according to explicit ownership and project membership rules.
- Rationale: This aligns with the existing role system (`Employee`, `TeamLead`, `ProjectManager`, `Administrator`) and keeps access management understandable for training.
- Alternatives considered:
  - Open project access: Rejected because it would violate the requirement to restrict access to authorized users.
  - All documents visible to everyone: Rejected because it breaks confidentiality and project isolation.

### Decision: Search and list views via EF Core filtering
- Decision: Query the document metadata table with filtered, indexed lookups by category, project, uploader, and text fields rather than in-memory scanning.
- Rationale: The app is small, but the product requirements expect page responsiveness for up to 500 records and search within 2 seconds.
- Alternatives considered:
  - Full in-memory search: Rejected because it would not scale and would bypass database indexing patterns.
  - Client-side filtering only: Rejected because it would not protect access control or support efficient querying.

### Decision: Notifications + audit events as existing cross-cutting services
- Decision: Use the existing notification infrastructure and add explicit audit event persistence for document actions.
- Rationale: The application already creates notifications for task/project activity, so document sharing and project events can reuse the same pattern.
- Alternatives considered:
  - Email-only notifications: Rejected because the app is offline-first and already emphasizes in-app notices.
  - Omitting audits: Rejected because the stakeholder requirements explicitly require reporting and compliance visibility.

## Open Questions Resolved

- The doc feature does not require a separate cloud service provider or external document store for the training scope.
- File validation should be performed before saving, using a whitelist of allowed extensions and a 25 MB maximum size per file.
- Document access rules will be enforced centrally in the service layer rather than relying on page logic.
- A future Azure migration path is satisfied by an abstraction layer and a consistent storage path strategy, without changing business logic.

## Implementation Guidance

The feature should be implemented in the existing service/data layer, then surfaced via Blazor pages and dashboard widgets. The project will not add a new framework or infrastructure layer unless a real gap appears during implementation.
