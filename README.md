# NeverMissLead — Grounded RAG Lead Capture & Follow-Up Assistant

[![CI Pipeline](https://github.com/nparvezten/NeverMissLead/actions/workflows/ci.yml/badge.svg)](https://github.com/nparvezten/NeverMissLead/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4.svg)](https://dotnet.microsoft.com/)
[![Angular 20+](https://img.shields.io/badge/Angular-20+-DD0031.svg)](https://angular.dev/)
[![Python 3.11+](https://img.shields.io/badge/Python-3.11+-3776AB.svg)](https://python.org/)
[![PostgreSQL + pgvector](https://img.shields.io/badge/Postgres-pgvector-336791.svg)](https://github.com/pgvector/pgvector)

NeverMissLead is a **multi-tenant, citation-grounded RAG (Retrieval-Augmented Generation) lead capture and follow-up platform** for independent service businesses.

Visitors ask questions in an embeddable website widget, receive accurate answers grounded directly in the business's own FAQ and pricing documents with verifiable source citations, and are automatically captured as qualified leads with scheduled follow-up sequences. When a visitor asks an out-of-scope or cross-tenant question, the assistant explicitly abstains and flags the conversation for owner handoff rather than hallucinating.

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

### Test Suite Verification Matrix

```text
├── xUnit (.NET 10 Backend)     :  45 / 45 Passing  (Clean Architecture, Native CQRS, PBKDF2, Dynamic CORS, Tenant Isolation)
├── Pytest (Python RAG Service) :  22 / 22 Passing  (Recursive chunking, cosine retrieval, Fake/Local providers)
├── Playwright (E2E Browser)    :   4 /  4 Passing  (Widget Q&A, Citation chips, Lead capture form, Owner dashboard)
├── Bandit (Python SAST)        :   0 Issues (0 High, 0 Medium, 0 Low)
└── Full Verification Pipeline  :   6 /  6 Stages Green (./run-pipeline.sh)
```

---

## 🏛️ System Architecture

NeverMissLead is architected as four loosely-coupled, containerized components with tenant-isolated vector retrieval:

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

### Method 1: Docker Compose (Recommended)

```bash
git clone https://github.com/nparvezten/NeverMissLead.git
cd NeverMissLead
docker compose up --build
```

- **Demo Landing Pages:** `http://localhost:4200/demo/tutoring`, `http://localhost:4200/demo/dental`, `http://localhost:4200/demo/realty`
- **Owner Dashboard:** `http://localhost:4200/dashboard`
- **.NET 10 API:** `http://localhost:5103` (`/swagger` or `/health`)
- **FastAPI RAG Service:** `http://localhost:8000` (`/docs`)

---

## 🛡️ Security & Multi-Tenant Isolation

1. **Cryptographic Password Hashing**: Owner passwords use ASP.NET Core built-in PBKDF2 (`HMAC-SHA512`, 100,000 iterations, 128-bit cryptographically secure salt, and constant-time equality check).
2. **AES-256 Field Encryption**: Lead PII (`name`, `phone`, `email`) is encrypted at rest using AES-256-CBC with HMAC-SHA256 authenticated integrity.
3. **Multi-Tenant Scoping**: All database reads, vector distance searches, lead queries, and conversation logs enforce `WHERE business_id = currentTenantId` at the database and vector layer.
4. **Dynamic CORS Enforcement**: Public widget requests validate the `Origin` header against the registered business's allowlist stored in PostgreSQL.
5. **Rate Limiting**: Public chat widget (`200 req/min dev`, `30 req/min prod`) and login endpoints (`5 req/min`) prevent brute-force attacks and abuse.
6. **HttpOnly Session Cookies**: JWT session tokens are persisted in `HttpOnly`, `SameSite=Strict`, `Secure` (over HTTPS) cookies.

---

## 📄 License

Distributed under the **MIT License**. See [`LICENSE`](LICENSE) for details.