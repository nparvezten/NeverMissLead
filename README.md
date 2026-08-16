# NeverMissLead — Never Lose a Lead

NeverMissLead is a RAG-powered lead capture and follow-up assistant for
independent service businesses. It answers visitor questions from your
own FAQ/pricing docs (with citations, not guesses), captures qualified
leads automatically, schedules follow-ups, and hands off to you when it's
unsure. v1 targets one niche — private tutors / coaching classes — as a
proof of concept before broadening.

---

## 🏗️ System Architecture

Four decoupled pieces, same pattern as our other projects:

* `frontend/`: **Angular 20+ LTS Standalone** app — the embeddable chat widget
  (small bundle, drops into any business's website) plus the owner
  dashboard (leads, conversations, knowledge-base management), using
  Signals + TailwindCSS.
* `backend/`: **C# .NET 10 Web API**, Clean Architecture, Native MIT CQRS,
  FluentValidation, Serilog, PostgreSQL (+ `pgvector` extension).
* `rag-service/`: **Python 3.11+ FastAPI** microservice — chunking,
  embeddings, retrieval, and lightweight lead-intent classification.
* `docker-compose.yml`: one-command orchestration for all services.

---

## ⚡ Quickstart Guide

### Method 1: Docker (Recommended)

```bash
git clone https://github.com/nparvezten/NeverMissLead.git
cd NeverMissLead
docker compose up --build
```

* **Widget demo / Dashboard:** `http://localhost:4200`
* **.NET API:** `http://localhost:5103`
* **RAG Service:** `http://localhost:8000`

### Method 2: Manual Local Development

#### Prerequisites
- **.NET 10 SDK**
- **Node.js 20+ & npm**
- **Python 3.11+**
- **PostgreSQL 15+** with the `pgvector` extension enabled

#### Step 1: Backend API
```bash
dotnet run --project backend/API/NeverMissLead.API.csproj --launch-profile http
```
*(Runs EF Core migrations and seeds a demo tutor business on startup.)*

#### Step 2: RAG Microservice
```bash
cd rag-service
python3 -m venv .venv
source .venv/bin/activate  # Windows: .venv\Scripts\activate
pip install -r requirements.txt
uvicorn main:app --host 0.0.0.0 --port 8000
```

#### Step 3: Frontend
```bash
cd frontend
npm install
npx ng serve --port 4200 --proxy-config proxy.conf.json
```

---

## 💸 Cost Model: Free by Default, Client-Ready by Config

NeverMissLead runs **entirely free out of the box** — `docker compose up`
uses a local embedding model (`sentence-transformers`, CPU-only, no API key)
and a stub/local LLM provider, so you can clone, run, and demo the full
pipeline at zero cost.

When a real deployment needs a hosted model (OpenAI, Claude, or Gemini),
switch providers via configuration only — no code changes:

```bash
# .env or environment variables
EMBEDDING_PROVIDER=openai      # local | openai | gemini
LLM_PROVIDER=anthropic         # none | ollama | openai | anthropic | gemini
OPENAI_API_KEY=...
ANTHROPIC_API_KEY=...
```

Both providers sit behind interfaces (`EmbeddingProvider` in `rag-service`,
`ILlmClient` in the backend) — see AGENTS.md's "Multi-Provider AI Strategy"
for the full design. API keys are never committed to the repo.

---

## 🔑 Demo Sandbox Data

Pre-seeded on startup for immediate testing:

### Owner Dashboard Login
* **Email:** `owner@demotutors.nevermisslead.dev`
* **Password:** `DemoPassword123!`

### Sample Knowledge Base
A demo tutoring business ("Bright Minds Coaching") is seeded with a sample
FAQ/pricing document — used both for manual testing and as the source
document for the eval harness in `eval/tutor_faq_sample.md`.

### Sample Widget Embed
```html
<script src="http://localhost:4200/widget.js" data-business-id="demo-tutors"></script>
```

---

## 📊 RAG Quality — Eval Report

This is the artifact that matters more than the UI: `eval/run_eval.py`
scores the RAG pipeline against a fixed set of question/answer pairs
(answerable + should-abstain cases) so retrieval/prompt quality is
measurable, not vibes-based. Latest report: `eval/latest_report.md`.

```bash
cd eval
python run_eval.py
```

---

## 🧪 Automated Verification & Test Suite

```bash
./run-pipeline.sh
```

Runs:
1. Backend C# build, `dotnet format`, xUnit test suite.
2. Python pytest + Bandit SAST scan.
3. Angular standalone production build.
4. RAG eval harness (`eval/run_eval.py`), report diffed against baseline.

---

## 🛡️ Security & Privacy

1. **AES-256 field encryption** on lead PII (name, phone, email).
2. **Tenant isolation**: every retrieval query is scoped by `business_id` —
   no cross-business data leakage, enforced at the repository layer.
3. **Rate limiting** on the public, unauthenticated widget endpoint.
4. **No PII in logs**: conversation and chunk IDs are logged, not raw
   message content or contact details.

See `LEGAL.md` for the non-affiliation statement (this is an independent
tool, not affiliated with any messaging platform or CRM vendor) and data
handling policy.

---

## 📝 License

Distributed under the **MIT License**. See `LICENSE` for more information.