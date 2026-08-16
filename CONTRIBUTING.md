# Contributing to NeverMissLead

Thank you for your interest in contributing to **NeverMissLead**! This is an
open-source, RAG-powered lead capture and follow-up assistant built for
independent service businesses (v1 niche: private tutors / coaching
classes), with tenant-scoped data isolation and citation-required answers
as first-class requirements.

---

## 🚀 Quickstart: Local Development Environment

### 1. Clone the Repository
```bash
git clone https://github.com/nparvezten/NeverMissLead.git
cd NeverMissLead
```

### 2. One-Command Bootstrap
```bash
./setup.sh
```
Restores C# packages, installs Angular dependencies, and sets up the
Python virtual environment for `rag-service/`.

---

## 🛠️ Architecture & Extension Guidelines

```text
NeverMissLead/
├── backend/                  # C# ASP.NET Core 10 Web API
│   ├── API/                  # Controllers, Middlewares, Program.cs
│   ├── Application/          # Native MIT CQRS Queries, Commands, Validators
│   ├── Domain/                # Entities, Value Objects, Domain Events
│   └── Infrastructure/       # EF Core, AES-256 Encryption, pgvector repo
├── frontend/                  # Angular 20+ LTS Standalone Components
│   └── src/app/
│       ├── widget/            # Embeddable chat widget (small bundle)
│       └── dashboard/         # Owner dashboard (leads, conversations, KB)
├── rag-service/                # Python 3.11+ FastAPI microservice
│   ├── chunking.py
│   ├── embeddings.py
│   └── retrieval.py
├── eval/                       # RAG eval harness — question/answer pairs + scoring
├── docs/                        # Architecture diagrams, ADRs
└── docker-compose.yml           # Full multi-container orchestration
```

---

## ⚡ How to Extend Functionality

### 1. Adding a New Backend API Feature (Native CQRS)
NeverMissLead uses the same **100% MIT-licensed Native C# Mediator pattern**
as our other projects (no MediatR v13+, no paid packages).

1. **Define Request & Response DTO**: record implementing
   `IRequest<TResponse>` in `backend/Application/<Feature>/`.
2. **Implement Handler**: class implementing `IRequestHandler<TQuery,TResponse>`
   — auto-registered via assembly scanning.
3. **Expose Controller Endpoint**: action in `backend/API/Controllers/v1/`
   invoking `_mediator.Send(query)`.
4. If the feature touches the public widget endpoint, add/update the
   rate-limit policy and confirm `business_id` scoping in the repository
   query — this is reviewed on every PR touching that path.

### 2. Adding a New Frontend UI Feature (Angular Signals)
1. Standalone components under `frontend/src/app/widget/` (visitor-facing,
   keep the bundle small) or `frontend/src/app/dashboard/` (owner-facing).
2. Use `signal()`/`computed()` state; avoid new RxJS subscriptions where a
   signal will do.

### 3. Extending the RAG Service (Python FastAPI)
1. Chunking/embedding changes go in `rag-service/chunking.py` /
   `embeddings.py`.
2. **Any change to chunking, embeddings, or the generation prompt must be
   re-validated against `eval/run_eval.py` before merge.** Attach the
   before/after eval report to the PR — this is non-negotiable, it's the
   whole point of the eval harness.

### 4. Adding a New AI Provider (Embedding or LLM)
1. Embeddings: implement the `EmbeddingProvider` base class in
   `rag-service/embeddings.py`, add it to the provider factory, and add it
   as an allowed value for `EMBEDDING_PROVIDER`.
2. LLM: implement `ILlmClient` in `backend/Infrastructure/`, register it in
   the DI factory keyed off `LLM_PROVIDER`, and never call the vendor SDK
   directly from anywhere outside that one implementation class.
3. New providers must ship with a fake/mock usable in tests — PRs adding a
   provider without a test double will be rejected, since it risks unit
   tests silently requiring a real API key to pass.
4. Never commit an API key, sample `.env` with a real key, or log a request/
   response payload that could contain one.

---

## 🧪 Verification & Testing Rules

Before submitting a Pull Request:

```bash
./run-pipeline.sh
```

Verifies:
- ✅ C# build & static formatting (`dotnet build`, `dotnet format`).
- ✅ xUnit backend unit tests (`dotnet test`).
- ✅ Python pytest & Bandit SAST scan (`pytest`, `bandit`).
- ✅ Angular standalone production build.
- ✅ RAG eval harness (`eval/run_eval.py`) — no regression in
  answerable-question accuracy or abstain-correctness vs. `eval/latest_report.md`.

---

## 🔒 Security & Licensing Policies
- **Licensing**: only permissive open-source packages (MIT, Apache 2.0,
  BSD). No commercial or dual-licensed packages.
- **Tenant Isolation**: every query touching `kb_chunks`, `conversations`,
  `leads`, or `messages` must filter by `business_id`. PRs that introduce
  an unscoped query on these tables will be rejected.
- **PII Handling**: lead name/phone/email are AES-256 encrypted at rest.
  Never log raw message content or contact details — log conversation and
  chunk IDs instead.