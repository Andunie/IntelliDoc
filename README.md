IntelliDoc is an end-to-end Intelligent Document Processing (IDP) solution that leverages the power of AI to process, validate, and integrate unstructured documents—such as invoices, receipts, contracts, and CVs—into enterprise systems (ERP/CRM).
Designed with Vertical Slice Architecture principles, it delivers high scalability and performance thanks to its Event-Driven architecture.
Key Features

1. AI-Powered Extraction
Multi-Model AI: Utilizes Google Gemini 1.5 Flash to extract data from documents with 99% accuracy.
Auto-Classification: Automatically detects document types (Invoices, Receipts, CVs, Contracts).
Detailed Analysis: Parses line items, tax numbers, and total amounts into structured tabular data.

2. Human-in-the-Loop Workbench
Split-View Interface: Side-by-side display of the original PDF and the AI-extracted data for efficient verification.
Field-Level Audit: Tracks exactly who changed which field, when, and why (e.g., "100.00 -> 150.00") with second-level precision logs.

3. Advanced Analytics & Search
Elasticsearch Integration: Millisecond-latency Full-Text and Fuzzy search across millions of documents.
Financial Dashboard: Live charts showing spending trends, top vendors, and category distributions powered by Recharts.

4. Enterprise Integrations
Email Ingestion Bot: Automatically fetches and processes attachments (.pdf, .jpg) sent to a designated system email address.
webhook Outgoing Webhooks: Instantly posts approved documents as JSON payloads to 3rd party systems like SAP, Logo, Slack, or Zapier.
Excel Export: Exports documents and line items into structured Excel reports (Single or Batch mode).
Architecture & Tech Stack
The project is built using modern software principles on a Modular Monolith structure. Modules are loosely coupled and communicate asynchronously via RabbitMQ.
Backend (.NET 9 Web API)
Architecture: Modular Monolith (Vertical Slice).
Modules: Identity, Intake, Extraction, Audit, Search, Integration, EmailIngestion.
Messaging: MassTransit + RabbitMQ.
Database: PostgreSQL (EF Core) + Elasticsearch.
Object Storage: MinIO (S3 Compatible).
Background Jobs: .NET Worker Services (IMAP Listener).
Infrastructure (Docker)
The entire system spins up with a single docker-compose command.

- Installation
Prerequisites
Docker Desktop
.NET 9 SDK
Node.js 18+
1. Start Infrastructure
code
Bash
docker-compose up -d
(Starts Postgres, RabbitMQ, MinIO, and Elasticsearch containers.)
2. Backend Setup
Run the migration commands to create the database schemas:
code
Bash
cd src/IntelliDoc.Api

dotnet ef database update --context IdentityDbContext
dotnet ef database update --context IntakeDbContext
dotnet ef database update --context ExtractionDbContext
dotnet ef database update --context AuditDbContext
dotnet ef database update --context IntegrationDbContext

dotnet run
API URL: https://localhost:7203
3. Frontend Setup
code
Bash
cd intellidoc-ui
npm install
npm run dev
UI URL: http://localhost:3000
System Workflow
Ingestion: Document is uploaded (via Web UI or Email Bot).
Event: IDocumentUploaded event is published.

Processing:
Document saved to MinIO.
Python AI service extracts data (OCR/LLM).
Data is indexed in Elasticsearch.
Validation: User reviews and validates data on the Workbench.
Audit: Every field change is logged for audit trails.
Integration: Upon approval, the document data is sent to the target ERP system via Webhook.

Author
İlhan Yiğitoğlu
GitHub: github.com/ilhanyigitoglu
License
This project is licensed under the MIT License.
