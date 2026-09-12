# NeverMissLead — Grounded RAG Lead Capture & Follow-Up Assistant

[![CI Pipeline](https://github.com/nparvezten/NeverMissLead/actions/workflows/ci.yml/badge.svg)](https://github.com/nparvezten/NeverMissLead/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4.svg)](https://dotnet.microsoft.com/)
[![Angular 20+](https://img.shields.io/badge/Angular-20+-DD0031.svg)](https://angular.dev/)
[![Python 3.11+](https://img.shields.io/badge/Python-3.11+-3776AB.svg)](https://python.org/)
[![PostgreSQL + pgvector](https://img.shields.io/badge/Postgres-pgvector-336791.svg)](https://github.com/pgvector/pgvector)
[![Security: Bandit Clean](https://img.shields.io/badge/Security-Bandit_SAST_0_Issues-brightgreen.svg)](eval/latest_report.md)

NeverMissLead is a **multi-tenant, citation-grounded RAG (Retrieval-Augmented Generation) lead capture and follow-up platform** for independent service businesses (e.g., private coaching, dental clinics, real estate brokerages).

Visitors interact with an embeddable website widget, receive accurate answers grounded directly in the business's own FAQ and pricing documents with verifiable source citations, and are captured as qualified leads with automated follow-up scheduling. When an out-of-scope or cross-tenant query occurs, the system deterministically abstains and initiates owner handoff rather than hallucinating.

---

## 🎯 Positioning

RAG-grounded chat widgets for lead capture aren't a new category — commercial platforms like Botsonic, Heeya, and Landbot already serve this market well.
**This repository isn't claiming to be a novel idea; it's a fully open, end-to-end reference implementation of that pattern**, built to show real engineering depth where most competitors are closed-source SaaS: a working multi-tenant architecture (not a marketing claim — see the live cross-tenant isolation tests), a published, re-runnable accuracy benchmark instead of a blog-post claim, a documented security audit trail, and a genuinely free-by-default AI layer that's config-swappable to a paid provider without a rebuild.

If you're evaluating this as a learning resource, a portfolio reference, or a self-hosted starting point for your own vertical — that's exactly what it's for.

## 🏛️ Executive Summary & Key Architectural Highlights

This repository serves as a **production-ready reference architecture** demonstrating modern system design, polyglot microservice orchestration, zero-cost developer experience, and enterprise security hygiene:

1. **Polyglot Clean Architecture**:
   * **Core Platform (.NET 10 Web API)**: Clean Architecture with Domain-Driven Design (DDD), Native MIT CQRS (`IMediator`, `IRequest<T>`, `IRequestHandler<TReq, TResp>` with assembly scanning — zero commercial or paid runtime packages), FluentValidation, and RFC 7807 ProblemDetails.
   * **RAG Microservice (Python 3.11+ FastAPI)**: Specialized AI service owning document tokenization, sliding-window chunking, vector embedding, and similarity search.
   * **Frontend (Angular 20+ LTS)**: High-performance standalone components, Reactive Signals state management, and modern TailwindCSS styling across both visitor widget and owner dashboard.
2. **Deterministic Grounding & Zero-Hallucination Guardrail**:
   * Every AI response is strictly tied to verifiable `cited_chunk_ids`.
   * Out-of-scope or ungrounded queries trigger immediate abstention and owner escalation (`needs_human = true`), backed by a 48-case benchmark suite achieving **100% accuracy and citation precision**.
3. **Multi-Provider AI Strategy ($0 Cost Default)**:
   * **Zero External Cost in Local/CI Mode**: Uses CPU-based `sentence-transformers` (`all-MiniLM-L6-v2`) and local stub generation out of the box — no credit cards, API keys, or cloud accounts needed to clone and run.
   * **Hot-Swappable Hosted AI**: Config-driven provider factory (`LLM_PROVIDER`, `EMBEDDING_PROVIDER`) allows seamless switching to OpenAI, Anthropic Claude, or Google Gemini via environment variables with zero code rebuilds.
4. **Boring, Defensible Infrastructure**:
   * Utilizes **PostgreSQL 16 with native `pgvector`** for relational data and cosine distance vector search (`<=>`), avoiding costly standalone vector databases.
5. **Data Protection & Multi-Tenant Isolation**:
   * Cryptographic password hashing (ASP.NET Core PBKDF2 with HMAC-SHA512, 100,000 iterations, 128-bit salt).
   * AES-256-CBC field encryption with HMAC-SHA256 for lead PII (`name`, `phone`, `email`).
   * Dynamic Origin CORS validation and per-endpoint rate limiting for public widget access.

---

## 🏢 Multi-Tenant Vertical Demonstrations

NeverMissLead serves multiple distinct business verticals using a single, unified backend and RAG engine with strict tenant isolation at the database and vector levels:

| Demo Route | Business Vertical | Sample FAQ Content | Demo Credentials |
|---|---|---|---|
| **[`/demo/tutoring`](http://localhost:4200/demo/tutoring)** | **Bright Minds Coaching**<br>*(STEM Tutoring & Test Prep)* | Algebra, Physics, SAT/AP prep, hourly rates ($35–$65/hr), trial policy | `owner@brightminds.test`<br>`BrightMinds2026!` |
| **[`/demo/dental`](http://localhost:4200/demo/dental)** | **Bright Smile Dental Clinic**<br>*(Dental Care & Surgery)* | Routine cleanings ($120), teeth whitening ($350), Invisalign ($3,800), insurance | `owner@brightsmile.test`<br>`BrightSmile2026!` |
| **[`/demo/realty`](http://localhost:4200/demo/realty)** | **Skyline Realty Partners**<br>*(Residential & Commercial Realty)* | 5% commission structure, CMA valuations, rental placement, offer documents | `owner@skylinerealty.test`<br>`Skyline2026!` |

---

## 📊 Grounding Benchmark & Multi-Tenant Results

Retrieval accuracy, citation precision, and hallucination resistance are verified continuously by an automated evaluation harness ([`eval/run_eval.py`](eval/run_eval.py)) across **48 benchmark questions** spanning all three verticals (32 answerable queries, 16 adversarial/out-of-scope near-misses).

See [`eval/latest_report.md`](eval/latest_report.md) for full question-by-question telemetry.

### 🌐 Combined Platform Metrics

| Evaluation Metric | Target Threshold | Benchmark Result | Status |
|---|:---:|:---:|:---:|
| **Overall Accuracy** | &ge; 85% | **100.0%** (48/48) | ✅ PASS |
| **Answerable Accuracy** | &ge; 80% | **100.0%** (32/32) | ✅ PASS |
| **Citation Precision** | &ge; 90% | **100.0%** (32/32) | ✅ PASS |
| **Abstention Correctness** | &ge; 80% | **100.0%** (16/16) | ✅ PASS |

### 🏢 Per-Vertical Benchmark Breakdown

| Business / Vertical | Questions | Answerable Acc | Citation Precision | Abstention Acc | Overall Score | Status |
|---|:---:|:---:|:---:|:---:|:---:|
| **Bright Minds Coaching** *(Tutoring)* | 18 | 100.0% | 100.0% | 100.0% | **100.0%** | ✅ PASS |
| **Bright Smile Dental Clinic** *(Dental)* | 15 | 100.0% | 100.0% | 100.0% | **100.0%** | ✅ PASS |
| **Skyline Realty Partners** *(Realty)* | 15 | 100.0% | 100.0% | 100.0% | **100.0%** | ✅ PASS |

---

## 🧪 Comprehensive Verification Matrix

```text
├── xUnit (.NET 10 Backend)     :  45 / 45 Passing  (Clean Architecture, Native CQRS, PBKDF2, Dynamic CORS, Tenant Isolation)
├── Pytest (Python RAG Service) :  22 / 22 Passing  (Recursive chunking, cosine retrieval, Fake/Local providers)
├── Playwright (E2E Browser)    :  10 / 10 Passing  (Widget Q&A, Citation chips, Lead capture form, 3 vertical demos)
├── Vitest (Angular Unit)       :   1 /  1 Passing  (Standalone router & core components)
├── Bandit (Python SAST)        :   0 Issues        (Zero High, Medium, or Low security vulnerabilities)
└── Full Verification Pipeline  :   6 /  6 Stages   (Automated run-pipeline.sh & GitHub Actions CI)
```

---

## 📐 System Architecture & Workflow Diagrams

Explore the comprehensive vector diagrams in [`docs/diagram/`](docs/diagram/):

* [**System Architecture**](docs/diagram/system_architecture.svg): Polyglot .NET + FastAPI + PostgreSQL + Angular topology.
* [**Database Entity-Relationship (ERD)**](docs/diagram/database_erd.svg): Relational schema and pgvector storage.
* [**Knowledge Ingestion Workflow**](docs/diagram/ingestion_flow.svg): Document chunking and embedding pipeline.
* [**Chat & Grounded RAG Flow**](docs/diagram/chat_rag_flow.svg): Public widget Q&A, citation resolution, and abstention routing.
* [**Lead Capture & Follow-Up Flow**](docs/diagram/lead_followup_flow.svg): Intent classification, AES-256 PII encryption, and `IHostedService` background polling.

```mermaid
flowchart TB
    subgraph Clients["Clients & Presentation"]
        Visitor["🌐 Website Visitors<br/>(Tutoring, Dental, Realty Widgets)"]
        Owner["👤 Business Owners<br/>(Multi-Tenant Dashboard)"]
    end

    subgraph Frontend["Frontend App (Angular 20+ Standalone)"]
        WidgetApp["Embeddable Chat Widget<br/>(TailwindCSS, Lightweight)"]
        DashboardApp["Owner Dashboard<br/>(Leads, Follow-ups, Audit)"]
    end

    subgraph Backend["Backend API (ASP.NET Core 10 Web API)"]
        API["REST Controllers (/api/v1/)<br/>Dynamic CORS & Rate Limiters"]
        CQRS["Native MIT CQRS Handlers<br/>(No MediatR Runtime Package)"]
        Crypto["AES-256 PII Cryptography<br/>PBKDF2 Password Hashing"]
        Poller["FollowUpPollerService<br/>(Background HostedService)"]
    end

    subgraph RAG["RAG Microservice (Python FastAPI)"]
        FastAPI["FastAPI App (/embed, /query)"]
        Chunker["Markdown Header Chunking"]
        Embedder["EmbeddingProvider Factory<br/>(Default: sentence-transformers)"]
    end

    subgraph Database["Primary Data Store (PostgreSQL 16)"]
        Relational["Relational Tables<br/>(businesses, leads, conversations)"]
        VectorStore["pgvector Extension<br/>(kb_chunks vector(384) Cosine)"]
    end

    subgraph ExternalAI["External AI Services (Optional / Config-Swappable)"]
        HostedLLM["Hosted LLM APIs<br/>(OpenAI / Claude / Gemini)"]
        HostedEmb["Hosted Embedding APIs<br/>(text-embedding-3-small, etc.)"]
    end

    Visitor -->|Unauthenticated Chat/Lead POST| WidgetApp
    Owner -->|JWT Cookie Authenticated| DashboardApp

    WidgetApp -->|HTTP REST| API
    DashboardApp -->|HTTP REST| API

    API --> CQRS
    CQRS --> Crypto
    CQRS -->|Tenant Scoped Query| Relational
    CQRS -->|HTTP /query| FastAPI
    Poller -->|Poll Pending Tasks| Relational

    FastAPI --> Chunker
    FastAPI --> Embedder
    Embedder -->|pgvector Cosine Search| VectorStore

    CQRS -.->|LLM_PROVIDER != none| HostedLLM
    Embedder -.->|EMBEDDING_PROVIDER != local| HostedEmb
```

---

## ⚡ Quickstart Guide

### Method 1: Docker Compose (All Services Containerized)

```bash
git clone https://github.com/nparvezten/NeverMissLead.git
cd NeverMissLead
docker compose up --build
```

* **Demo Landing Pages:**
  * STEM Tutoring: [`http://localhost:4200/demo/tutoring`](http://localhost:4200/demo/tutoring)
  * Dental Clinic: [`http://localhost:4200/demo/dental`](http://localhost:4200/demo/dental)
  * Real Estate: [`http://localhost:4200/demo/realty`](http://localhost:4200/demo/realty)
* **Owner Dashboard:** [`http://localhost:4200/dashboard`](http://localhost:4200/dashboard) (or [`/login`](http://localhost:4200/login))
* **.NET 10 API:** [`http://localhost:5103/health`](http://localhost:5103/health) (Swagger: [`http://localhost:5103/swagger`](http://localhost:5103/swagger))
* **FastAPI RAG Service:** [`http://localhost:8000/docs`](http://localhost:8000/docs)

### Method 2: Local Developer Mode

```bash
# 1. Start PostgreSQL with pgvector
docker compose up -d postgres

# 2. Start .NET API
dotnet run --project backend/API/NeverMissLead.API.csproj --urls "http://localhost:5103"

# 3. Start Python RAG Service
cd rag-service
source .venv/bin/activate
uvicorn main:app --host 0.0.0.0 --port 8000 --app-dir .

# 4. Start Angular Frontend
cd ../frontend
npm start -- --port 4200
```

### Method 3: Automated Verification Pipeline

```bash
./run-pipeline.sh
```

---

## 🛡️ Security & Privacy Guardrails

1. **Cryptographic Password Hashing**: Owner credentials use ASP.NET Core PBKDF2 (`HMAC-SHA512`, 100,000 iterations, 128-bit cryptographically secure salt, and constant-time equality check).
2. **AES-256 Field Encryption**: Lead PII (`name`, `phone`, `email`) is encrypted at rest using AES-256-CBC with HMAC-SHA256 authenticated integrity. Raw PII is never logged.
3. **Multi-Tenant Scoping**: All database reads, vector distance searches, lead queries, and conversation logs enforce `WHERE business_id = currentTenantId` at the database and vector layer.
4. **Dynamic CORS Enforcement**: Public widget requests validate the `Origin` header against the registered business's allowlist stored in PostgreSQL.
5. **Rate Limiting**: Public chat widget (`200 req/min dev`, `30 req/min prod`) and login endpoints (`5 req/min`) prevent brute-force attacks and abuse.
6. **HttpOnly Session Cookies**: JWT session tokens are persisted in `HttpOnly`, `SameSite=Strict`, `Secure` (over HTTPS) cookies.

---

## 📄 License

Distributed under the **MIT License**. See [`LICENSE`](LICENSE) for details.
