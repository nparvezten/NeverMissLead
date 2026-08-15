# AGENTS.md - System Context & Architectural Directives for NeverMissLead

## Product Framing (read this before writing any code)
NeverMissLead is a RAG-powered lead capture and follow-up assistant for independent
service businesses (v1 niche: **private tutors / coaching classes**). A visitor
lands on the business's website, asks a question in a chat widget, gets an
accurate answer grounded in the business's own pricing/FAQ/services docs, and —
if they show buying intent — is captured as a qualified lead with an automated
follow-up sequence. If the AI is unsure, it hands off to the business owner
instead of guessing.

**Do not build WhatsApp integration in v1.** Meta Business API approval is a
multi-week external dependency unrelated to whether the core product works.
The chat widget is channel-agnostic by design so WhatsApp can be added in v2
as a new "channel adapter" without touching the RAG/lead engine.

**North star metric for v1**: given a real tutor's FAQ/pricing doc, can a
stranger ask 15 realistic questions and get correct, cited answers on 12+ of
them, with the AI correctly abstaining (handoff) on the other 3? That eval
result is the actual portfolio artifact — more valuable on GitHub than the UI.

## Behavioral Persona
- Act as a Principal Engineer building a real, shippable v1 — not a demo.
- Prefer boring, provable infrastructure over cleverness (Postgres+pgvector
  over a new vector DB; a polling HostedService over a new job-queue system).
- Self-heal build/test/dependency failures autonomously; explain non-trivial
  fixes in commit messages.
- Every RAG answer must carry a citation back to the source chunk. No citation
  = the endpoint must return "I'm not sure, let me get the owner" instead of
  a hallucinated answer.

## Context & Repository
- Project Name: NeverMissLead
- Target niche for v1 content/testing: private tutors / coaching classes
  (small, well-defined FAQ/pricing surface — good for RAG eval quality)
- Legal Guardrail: Lead PII (name, phone, email) is business-customer data,
  not the tutor's internal data — encrypt at rest same as any PII, but there
  is no "SIR/electoral roll"-style sensitivity here. Standard data-protection
  hygiene (DPDP Act 2023 basics) is sufficient: encrypt PII fields, don't log
  raw PII, let a business delete their data on request.

## Technology Stack
- Backend API: C# .NET 10 Web API, Clean Architecture, Native MIT CQRS
  (`IRequestHandler<TRequest,TResponse>` + assembly-scanning, same pattern as
  your existing projects — no MediatR v13+, no paid packages), FluentValidation,
  Serilog, PostgreSQL + `pgvector` extension.
- Python Microservice: Python 3.11+, FastAPI — owns the RAG pipeline only
  (chunking, embeddings, retrieval, lead-intent classification). It does NOT
  own persistence; it reads/writes through the .NET API or directly to
  Postgres via a scoped connection, your call — recommend direct Postgres
  access for the vector table only, everything else through the .NET API.
- Frontend: Angular 20+ LTS Standalone Components, Signals, TailwindCSS.
  Two apps in one frontend project: (1) embeddable chat widget (small bundle,
  iframe or web component), (2) owner dashboard (leads, conversations, KB
  management).
- LLM: any OpenAI-compatible or Anthropic API — keep provider behind an
  interface (`ILlmClient`) so it's swappable and testable with a fake.
- Testing: xUnit (backend), pytest (Python), Playwright E2E (widget → lead
  capture happy path), plus a **RAG eval harness** (see below) — this is the
  most important test suite for credibility.

## Architecture Constraints
- CQRS Pattern: same Native MIT mediator pattern as your existing projects.
- Domain Layer: `Business`, `KnowledgeDocument`, `Lead`, `Conversation`,
  `Message`, `FollowUpTask` as entities; `LeadStatus`, `HandoffReason` as
  value objects/enums; domain events for `LeadCapturedEvent`,
  `HandoffRequestedEvent`, `FollowUpDueEvent`.
- Infrastructure Layer: EF Core for relational tables; raw `pgvector` column
  + cosine-distance query for the chunk-embeddings table (EF Core doesn't
  need to understand vectors — a small Dapper/raw-SQL repository is fine
  here, don't force an ORM abstraction onto it).
- Presentation Layer: Controllers under `/api/v1/`, RFC7807 ProblemDetails on
  all faults, global exception middleware, rate limiting on the **public**
  widget endpoint specifically (it's unauthenticated and internet-facing).

## Data Model (v1, minimum viable)
```
businesses            (id, name, niche, timezone, created_at)
business_settings     (business_id, widget_greeting, handoff_email, brand_color)
kb_documents          (id, business_id, title, source_type, raw_text, uploaded_at)
kb_chunks             (id, document_id, chunk_text, embedding vector(1536), token_count)
conversations         (id, business_id, visitor_ref, started_at, status,
                        needs_human bool, handoff_reason)
messages              (id, conversation_id, role[user|assistant|system],
                        content, cited_chunk_ids[], created_at)
leads                 (id, conversation_id, business_id, name_enc, phone_enc,
                        email_enc, intent_summary, qualification_score,
                        status[new|contacted|converted|lost], created_at)
follow_up_tasks       (id, lead_id, scheduled_for, channel, message_draft,
                        status[pending|sent|skipped], sent_at)
```
PII fields (`name_enc`, `phone_enc`, `email_enc`) use the same AES-256
field-encryption approach as your electoral-roll project. Blind indexing is
optional here — you rarely need exact-match lookup on lead phone numbers at
this stage; skip it in v1, add it if dedup-by-phone becomes a real need.

## RAG Pipeline (Python microservice)
1. **Ingest**: owner uploads FAQ/pricing text or PDF → chunk (~300-500 tokens,
   overlap ~50) → embed each chunk → store in `kb_chunks`.
2. **Retrieve**: visitor question → embed → cosine-similarity top-k (k=4)
   from that business's chunks only (always filter by `business_id` — never
   cross-tenant retrieval, this is a correctness *and* security requirement).
3. **Generate**: LLM call with retrieved chunks + strict system prompt:
   "Answer only from the provided context. If the context doesn't contain
   the answer, say you're not sure and offer to connect them with the owner."
4. **Cite**: return which `kb_chunks` were used; store `cited_chunk_ids` on
   the message row. An answer with zero cited chunks is a signal to flag
   `needs_human`.
5. **Classify** (lightweight, can be a second cheap LLM call or a rules
   pass): does this message show lead intent (asking about price, asking to
   enroll, asking for a demo/trial)? If yes, prompt the widget to collect
   name + phone/email inline, create a `Lead` row.

## Follow-Up Automation (v1, deliberately simple)
- No external job-queue package needed. A single `IHostedService` polls
  `follow_up_tasks` where `scheduled_for <= now() AND status = 'pending'`
  every N minutes, sends via email (SMTP — free tier, e.g. a transactional
  email API) or logs a "send via WhatsApp" stub for v2.
- Follow-up trigger rules for v1: lead created + no reply from visitor within
  1 hour → schedule a follow-up task; conversation ended with `needs_human`
  and no owner response within 4 hours → escalate (email the owner).

## Package & Licensing Policy
- Same as your other projects: MIT / Apache 2.0 / BSD only. No paid or
  dual-licensed packages. `pgvector` (PostgreSQL extension, PostgreSQL
  license) is fine. Avoid adding a dedicated vector DB (Pinecone/Weaviate)
  for v1 — it's an unnecessary moving part at this scale and a portfolio
  reviewer will read "used Postgres well" as more senior than "used the
  trendy vector DB."

## Security & Privacy Guardrails
- Public widget endpoint (`/api/v1/widget/{businessId}/chat`) is
  unauthenticated by design (visitors aren't logged in) — this means it
  needs its own rate limit, input length caps, and strict `business_id`
  scoping on every DB query touching it. Treat it as the highest-risk
  surface in the system.
- Owner dashboard endpoints require auth (JWT via HttpOnly/Secure/SameSite
  cookies, same as your existing pattern — never localStorage).
- CORS: widget endpoint allows only origins registered to that business
  (stored in `business_settings` or a separate allow-list table); dashboard
  API allows only your own frontend origin.
- Never log raw PII or full LLM prompts containing PII; log conversation IDs
  and chunk IDs instead.

## Eval Harness (this is the actual credibility artifact)
Create `eval/` with:
- `eval/tutor_faq_sample.md` — a realistic sample tutor FAQ/pricing doc.
- `eval/qa_pairs.json` — 15-20 question/expected-answer pairs, each tagged
  `answerable` or `should_abstain`.
- `eval/run_eval.py` — runs the RAG pipeline against every pair, scores
  correctness (can start as manual/LLM-judged, doesn't need to be fancy),
  and outputs a markdown report checked into the repo.
- Re-run this eval after any prompt or chunking change, commit the updated
  report. This is what turns the repo from "a chatbot demo" into "proof of
  product thinking" — put a link to the latest eval report at the top of
  the README.

## Zero Technical Debt Rules
Apply SOLID, DRY, KISS. No magic numbers, hardcoded strings, or
commented-out blocks. Public APIs require XML docs (C#) / docstrings
(Python). `ILlmClient` and `IEmbeddingClient` must be interfaces with a
fake/mock implementation for tests — never hit a real LLM API in unit tests.

---

## 6-Week Build Plan (10-15 hrs/week)

**Week 1 — Skeleton**
.NET Clean Architecture scaffold (Domain/Application/Infrastructure/API),
Postgres + `pgvector` extension enabled, EF Core migrations for the core
tables, `/health` endpoint, business CRUD (create a business, set greeting).

**Week 2 — RAG core**
Python FastAPI service: `/embed` (chunk+embed a document → write to
`kb_chunks`), `/query` (embed question → top-k retrieval). Wire the .NET API
to call it. No LLM generation yet — just prove retrieval quality manually.

**Week 3 — Generation + widget**
Add LLM generation with citation-required prompting. Build the Angular
embeddable widget (standalone component, minimal bundle) that calls
`/api/v1/widget/{businessId}/chat`. Manually test against your sample tutor
FAQ doc.

**Week 4 — Leads + handoff**
Lead-intent classification, inline contact capture in the widget, `Lead`
creation, `needs_human` flagging logic, owner email notification on
handoff.

**Week 5 — Follow-up + dashboard**
`IHostedService` follow-up poller, Angular owner dashboard (leads table,
conversation viewer with cited sources shown, "unanswered questions" list —
this last one is a great business-value feature: it tells the owner exactly
what content their FAQ is missing).

**Week 6 — Eval, polish, ship**
Build the eval harness, run it, fix the worst failures, write the README
with an architecture diagram (even a simple Mermaid/draw.io one), record a
2-3 min demo video (upload a tutor's FAQ → ask questions → show a lead
land in the dashboard → show a follow-up get scheduled). Push public repo.

v2 candidates (not v1): WhatsApp channel adapter, blind-index lead dedup,
multi-language support (relevant for Indian SMB market), Stripe billing.