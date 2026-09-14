# Tasks: Document Upload and Management

**Input**: Design documents from `/specs/001-document-upload-management/`
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Establish the document feature structure and configuration needed before implementation begins.

- [X] T001 Create the document feature scaffolding and file layout under `ContosoDashboard/Models/`, `ContosoDashboard/Services/`, `ContosoDashboard/Pages/`, and `ContosoDashboard/Data/`
- [X] T002 [P] Add document upload configuration to `ContosoDashboard/Program.cs` and `ContosoDashboard/appsettings.json` for storage root, size limit, and allowed extensions
- [X] T003 [P] Create the protected upload directory convention for local storage in `AppData/uploads` and document path naming guidance for future migration

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Build the shared document schema, storage abstraction, and authorization framework that all document flows depend on.

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel

- [X] T004 Create the core document metadata model in `ContosoDashboard/Models/Document.cs` with title, category, file metadata, uploader, project linkage, and validation rules
- [X] T005 Create the sharing and audit models in `ContosoDashboard/Models/DocumentShare.cs` and `ContosoDashboard/Models/AuditEvent.cs`
- [X] T006 [P] Update `ContosoDashboard/Data/ApplicationDbContext.cs` with document DbSets, indexes, navigation properties, and relationship configuration
- [X] T007 [P] Implement the storage abstraction contract in `ContosoDashboard/Services/IFileStorageService.cs` and the local implementation in `ContosoDashboard/Services/LocalFileStorageService.cs`
- [X] T008 Implement the authorization and validation foundation in `ContosoDashboard/Services/DocumentService.cs` for project membership, ownership checks, and upload restrictions
- [X] T009 [P] Wire document notification and access support into `ContosoDashboard/Services/NotificationService.cs` and `ContosoDashboard/Program.cs`
- [X] T010 Add database initialization and seed support for document-related data in `ContosoDashboard/Data/ApplicationDbContext.cs`

---

## Phase 3: User Story 1 - Upload and organize work documents (Priority: P1) 🎯 MVP

**Goal**: Allow authenticated users to upload valid documents with required metadata and see them in their personal/project document views.

**Independent Test**: A user can select supported files, provide required metadata, and complete a successful upload without leaving the dashboard.

### Implementation for User Story 1

- [X] T011 [US1] Implement upload validation, path generation, and file persistence flow in `ContosoDashboard/Services/DocumentService.cs` and `ContosoDashboard/Services/LocalFileStorageService.cs`
- [X] T012 [US1] Implement secure upload metadata persistence and category/tag handling in `ContosoDashboard/Models/Document.cs` and `ContosoDashboard/Data/ApplicationDbContext.cs`
- [X] T013 [US1] Add the document upload form and progress feedback in `ContosoDashboard/Pages/Documents.razor`
- [X] T014 [US1] Add upload success, error, and rejection messaging in `ContosoDashboard/Pages/Documents.razor` and `ContosoDashboard/Services/DocumentService.cs`
- [X] T015 [US1] Add personal/project document listing and sorting in `ContosoDashboard/Pages/Documents.razor` and `ContosoDashboard/Pages/ProjectDetails.razor`

**Checkpoint**: At this point, User Story 1 should be fully functional and independently testable

---

## Phase 4: User Story 2 - View and search project and personal documents (Priority: P1)

**Goal**: Make uploaded files easy to find through filtering, sorting, and project-aware search.

**Independent Test**: A user can filter, sort, and search a document list and only sees documents they are allowed to access.

### Implementation for User Story 2

- [ ] T016 [US2] Implement document retrieval, filters, and permission-aware queries in `ContosoDashboard/Services/DocumentService.cs`
- [ ] T017 [US2] Add category, project, and date-range filtering UI in `ContosoDashboard/Pages/Documents.razor`
- [ ] T018 [US2] Implement search by title, description, tags, uploader name, and project name in `ContosoDashboard/Services/DocumentService.cs`
- [ ] T019 [US2] Add document search controls and sorting behavior in `ContosoDashboard/Pages/Documents.razor`
- [ ] T020 [US2] Add project document visibility checks and access enforcement to `ContosoDashboard/Pages/ProjectDetails.razor` and `ContosoDashboard/Services/DocumentService.cs`

**Checkpoint**: At this point, User Stories 1 and 2 should both work independently

---

## Phase 5: User Story 3 - Share and manage document access safely (Priority: P1)

**Goal**: Ensure documents can be shared, replaced, deleted, and downloaded only by authorized users.

**Independent Test**: A document owner can share, replace metadata, and delete a document while unauthorized access is denied.

### Implementation for User Story 3

- [ ] T021 [US3] Implement authorized download and preview logic in `ContosoDashboard/Services/DocumentService.cs` and the document access route or page flow
- [ ] T022 [US3] Implement metadata editing and file replacement flows in `ContosoDashboard/Services/DocumentService.cs` and `ContosoDashboard/Pages/Documents.razor`
- [ ] T023 [US3] Implement delete confirmation and secure file cleanup in `ContosoDashboard/Services/DocumentService.cs` and `ContosoDashboard/Services/LocalFileStorageService.cs`
- [ ] T024 [US3] Add document share permissions and notification creation in `ContosoDashboard/Services/DocumentService.cs` and `ContosoDashboard/Services/NotificationService.cs`
- [ ] T025 [US3] Add the shared-with-me view and recipient-facing notifications in `ContosoDashboard/Pages/Documents.razor` and `ContosoDashboard/Pages/Index.razor`

**Checkpoint**: The document lifecycle should be safe, reviewable, and independently usable

---

## Phase 6: User Story 4 - Use documents from task and dashboard workflows (Priority: P2)

**Goal**: Integrate document management into the existing task and dashboard experiences without disrupting current workflows.

**Independent Test**: A user can attach or view related documents while working in task and dashboard contexts.

### Implementation for User Story 4

- [ ] T026 [US4] Add document attachment support to `ContosoDashboard/Pages/Tasks.razor` and `ContosoDashboard/Services/TaskService.cs`
- [ ] T027 [US4] Preserve task/project association when documents are attached in `ContosoDashboard/Models/TaskItem.cs` and `ContosoDashboard/Data/ApplicationDbContext.cs`
- [ ] T028 [US4] Add recent documents and document summary counts to `ContosoDashboard/Pages/Index.razor` and `ContosoDashboard/Services/DashboardService.cs`

**Checkpoint**: Document usage should be visible from primary project and daily workflow screens

---

## Phase 7: User Story 5 - Audit and maintain document integrity (Priority: P2)

**Goal**: Give administrators activity visibility and reporting for document operations and access patterns.

**Independent Test**: An administrator can review document activity and identify upload, share, download, and delete events.

### Implementation for User Story 5

- [ ] T029 [US5] Add audit logging for uploads, downloads, sharing, and deletions in `ContosoDashboard/Services/DocumentService.cs` and `ContosoDashboard/Models/AuditEvent.cs`
- [ ] T030 [US5] Add admin-facing report queries for most active uploaders, categories, and access patterns in `ContosoDashboard/Services/DocumentService.cs`
- [ ] T031 [US5] Create an administrator reporting view or dashboard panel in `ContosoDashboard/Pages/Index.razor` or a new `ContosoDashboard/Pages/Reports.razor`

**Checkpoint**: Document activity should be observable and auditable for the training governance model

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Finalize documentation, validation, and cross-story quality improvements.

- [ ] T032 [P] Update `README.md` with document upload usage, security notes, and local-storage guidance for the training app
- [ ] T033 [P] Validate the feature against the scenarios in `specs/001-document-upload-management/quickstart.md` and confirm security and performance behavior
- [ ] T034 [P] Review and refactor shared document service calls for consistency across `ContosoDashboard/Services/`, `ContosoDashboard/Pages/`, and `ContosoDashboard/Data/`
- [ ] T035 Run the project build and resolve any remaining compile or integration issues affecting the document feature

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion and blocks all user stories
- **User Stories (Phase 3-7)**: Depend on Foundational completion; can proceed in parallel if staffed, or in priority order P1 -> P2
- **Polish (Phase 8)**: Depends on all desired user stories being complete

### User Story Dependencies

- **User Story 1 (US1)**: No dependencies; starts after Foundational work
- **User Story 2 (US2)**: Starts after Foundational work; depends on the document model and listing/query infrastructure
- **User Story 3 (US3)**: Starts after Foundational work; depends on upload and access authorization basics
- **User Story 4 (US4)**: Depends on the document model, project linkage, and dashboard integration points
- **User Story 5 (US5)**: Depends on document actions being logged and access semantics being enforced

### Parallel Opportunities

- Setup tasks T002 and T003 can run in parallel
- Foundational tasks T006, T007, and T009 can run in parallel once setup is complete
- User stories can be split across team members after Phase 2
- Different views and service tasks within a story can be developed in parallel when they touch distinct files

---

## Parallel Example: User Story 1

```bash
# Example parallel work for User Story 1
Task: "Implement upload validation, path generation, and file persistence flow in ContosoDashboard/Services/DocumentService.cs and ContosoDashboard/Services/LocalFileStorageService.cs"
Task: "Add the document upload form and progress feedback in ContosoDashboard/Pages/Documents.razor"
Task: "Add upload status messages and validation handling in ContosoDashboard/Pages/Documents.razor and ContosoDashboard/Services/DocumentService.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Setup and Foundational phases
2. Implement User Story 1: upload and organization
3. Validate the document upload flow independently
4. Stop and confirm the primary business value works before moving to search, sharing, and audit features

### Incremental Delivery

1. Complete the document schema and storage abstraction
2. Deliver upload and listing capability
3. Add document search and project visibility
4. Add secure sharing and lifecycle management
5. Add task/dashboard integration and audit/reporting
6. Finish with quality and documentation polish

### Parallel Team Strategy

With multiple developers:

1. One developer builds the shared service and storage foundation
2. One developer builds the upload and listing experience
3. One developer builds the sharing and audit workflow
4. One developer integrates dashboard/task screens and final polish
