#!/usr/bin/env bash
set -euo pipefail

echo "================================================================"
echo "      NeverMissLead — Full Automated Verification Pipeline      "
echo "================================================================"

# 1. Backend C# Build & Warnings-as-Errors
echo ""
echo ">>> [1/5] Building .NET 10 Backend (Clean Architecture + Native CQRS)..."
dotnet build backend/NeverMissLead.slnx --warnaserror

# 2. Backend xUnit Test Suite
echo ""
echo ">>> [2/5] Running Backend xUnit Test Suite..."
dotnet test backend/NeverMissLead.slnx --logger "console;verbosity=normal"

# 3. Python RAG Microservice Pytest Suite & Bandit SAST Scan
echo ""
echo ">>> [3/6] Running Python RAG Service Pytest Suite..."
rag-service/.venv/bin/pytest rag-service/tests

echo ""
echo ">>> [4/6] Running Bandit Python SAST Security Audit..."
rag-service/.venv/bin/bandit -r rag-service/ eval/ -x "rag-service/.venv" -s B101

# 4. Angular Frontend Build
echo ""
echo ">>> [5/6] Building Angular 20+ Standalone Frontend..."
npm run build --prefix frontend

# 5. RAG Evaluation Harness Benchmark
echo ""
echo ">>> [6/6] Executing RAG Benchmark Evaluation Harness..."
PYTHONUNBUFFERED=1 rag-service/.venv/bin/python eval/run_eval.py

echo ""
echo "================================================================"
echo "  ✅ ALL 5 PIPELINE STAGES PASSED CLEANLY (Zero Regressions)    "
echo "================================================================"
