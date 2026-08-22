# Changelog

All notable changes to the NeverMissLead project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2026-08-22

### Summary
Initial release of **NeverMissLead** — a multi-tenant, RAG-powered lead capture and follow-up assistant for independent service businesses (v1 reference niche: private tutors and coaching classes). Built with clean architecture, zero paid SDK dependencies in default mode, high grounding accuracy, and strong PII privacy guarantees.

### Features Included

#### 💬 Chat Widget & Citation-Grounded RAG
- **Embeddable Chat Widget**: Angular 20+ standalone widget embeddable via a single `<script>` tag.
- **Local Embedding & Vector Search**: FastAPI microservice utilizing CPU `sentence-transformers` (`all-MiniLM-L6-v2`, 384 dimensions) with PostgreSQL `pgvector` cosine similarity.
- **Strict Grounding & Citations**: Every RAG answer returns `cited_chunk_ids` linked back to the exact knowledge document source chunks.

#### 🤝 Graceful Abstention & Handoff
- **Hallucination Prevention**: Questions outside the knowledge base trigger polite abstention (`"I'm not sure about that — let me connect you with the owner."`) with `needsHuman = true` and `HandoffReason = NoCitedChunks`.
- **FAQ Gap Detection**: Abstentions are automatically recorded and surfaced to the business owner to highlight missing documentation.

#### 🎯 Lead Intent Detection & AES-256 PII Encryption
- **Intent Classification**: Classifies visitor messages for commercial buying intent (pricing inquiries, trial class requests, enrollment questions).
- **Inline Contact Capture**: Seamless inline widget form capturing visitor name, phone, and email without leaving the conversation.
- **AES-256 Field Encryption**: Lead PII (`name_enc`, `phone_enc`, `email_enc`) is encrypted at rest using AES-256-CBC with HMAC-SHA256 authenticated integrity.

#### 📊 Owner Dashboard
- **Leads Management**: Real-time leads viewer with on-the-fly decryption and status progression (`New` → `Contacted` → `Converted` → `Lost`).
- **Conversation Thread Viewer**: Full audit trail of visitor interactions and cited knowledge chunks.
- **Unanswered Questions / Gap Analysis**: Dedicated view for inquiries that triggered AI abstentions.
- **Follow-Up Timeline**: Overview of pending and dispatched follow-up communications.

#### ⏱️ Follow-Up Automation
- **Background Dispatcher**: `IHostedService` (`FollowUpPollerService`) periodically polling and scheduling email escalations for handoffs and lead outreach tasks.

#### 🛡️ Complete Security Hardening
- **Cryptographic Password Hashing**: ASP.NET Core built-in PBKDF2 (`HMAC-SHA512`, 100,000 iterations, cryptographically random 128-bit salt).
- **JWT Cookie Authentication**: Session tokens stored in `HttpOnly`, `SameSite=Strict`, `Secure` (over HTTPS) cookies.
- **Rate Limiting**: Public widget chat endpoint (`200 req/min dev`, `30 req/min prod`) and login endpoint (`5 req/min`) protected against abuse.
- **Strict Tenant Isolation**: All queries enforce `WHERE business_id = currentTenantId` at the database and vector layer.
- **Dynamic CORS Enforcement**: Widget endpoints validate request origins against the tenant's allowlist.
- **Bandit Python SAST**: Verified clean across `rag-service/` and `eval/` with 0 security vulnerabilities.

#### 📈 RAG Grounding Benchmark Harness
- **Evaluation Suite**: 18-case ground-truth dataset (`eval/qa_pairs.json`) asserting $\ge 80\%$ benchmark thresholds.
- **Benchmark Results**: 100% Accuracy (18/18), 100% Citation Precision (18/18), and 100% Abstention Correctness (18/18).
- **Checked-in Report**: Auto-generated markdown report in `eval/latest_report.md`.

#### 🚀 CI Pipeline ($0 Cost & 100% Verified)
- **GitHub Actions Workflow** (`.github/workflows/ci.yml`):
  - `backend`: .NET 10 restore, `--warnaserror` build, 40 xUnit tests passing.
  - `rag-service`: Python 3.11, 22 pytest tests, Bandit SAST security audit.
  - `frontend`: Node 22, Angular 20 production bundle build.
  - `eval-benchmark`: Service container `pgvector:pg16`, migration health probe, and 18-query evaluation benchmark run.
- **Multi-Provider AI Architecture**: Fully functional at $0 in local/demo mode, with zero-code-change config switching for OpenAI, Claude, or Gemini in production.
