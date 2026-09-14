# Quickstart: Document Upload and Management Validation

## Prerequisites

- .NET 8.0 SDK installed
- SQL Server LocalDB available for the training project
- The ContosoDashboard app configured and running locally
- Access to the seeded mock users in the application

## Local Setup

1. Open a terminal in the repository root.
2. Restore dependencies:

```powershell
dotnet restore
```

3. Run the app:

```powershell
cd ContosoDashboard
dotnet run
```

4. Open the browser to the local login page and sign in as one of the seeded sample users.

## Validation Scenarios

### 1. Upload a valid document

- Sign in as a project member or an employee.
- Open the document management page or project page.
- Choose a supported file such as a PDF or Word document under 25 MB.
- Enter a document title, category, optional description, and optional project association.
- Submit the upload.

Expected outcome:
- The upload succeeds.
- The document appears in the user’s list with the expected metadata.
- The file is stored securely outside `wwwroot` and not exposed as a direct static asset.

### 2. Reject invalid input

- Attempt to upload a file with an unsupported extension, such as `.exe`.
- Attempt a file beyond 25 MB.
- Submit without a required title or category.

Expected outcome:
- The system blocks the upload.
- Clear user feedback appears.
- No database record or file content is persisted for rejected uploads.

### 3. Browse and search documents

- Upload at least two documents with different categories and tags.
- Open the personal documents view and filter by category or project.
- Search by title, tag, or uploader name.

Expected outcome:
- Documents update based on the selected filter and search terms.
- Only authorized documents appear in the results.

### 4. Share and access control

- Upload a document in a project context.
- Share it with another project user or a valid recipient.
- Attempt direct access as an unauthorized user.

Expected outcome:
- The new recipient can access the shared document under the allowed permission model.
- An unauthorized user cannot view or download the document.

### 5. Delete and audit flows

- Delete a document after confirmation.
- Review the document activity or notification log.

Expected outcome:
- The document is removed from the active list and the related activity is logged.
- Administrators can inspect the event history and identify who performed the action.

## Post-Validation Checks

- Run the project build:

```powershell
dotnet build
```

- Confirm that the app remains offline-capable and does not require external cloud storage for local development.
- Validate service-layer authorization for project documents and personal documents before moving to deeper feature work.
