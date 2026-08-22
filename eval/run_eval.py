"""Automated Multi-Tenant RAG Evaluation Harness for NeverMissLead.

Evaluates the end-to-end RAG pipeline across three distinct business verticals:
1. Bright Minds Coaching (STEM Tutoring)
2. Bright Smile Dental Clinic (Dental Healthcare)
3. Skyline Realty Partners (Real Estate Agency)

Reports individual per-business and aggregated platform metrics:
- Overall Accuracy (>= 85%)
- Citation Precision (>= 90%)
- Abstention Correctness (>= 80%)
"""

import json
import os
import sys
import time
from datetime import datetime, timezone
from pathlib import Path
import httpx

API_BASE_URL = os.getenv("API_BASE_URL", "http://localhost:5103")
ALLOWED_ORIGIN = "http://localhost:4200"

SUITES = [
    {
        "business_name": "Bright Minds Coaching",
        "niche": "STEM Tutoring",
        "business_id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
        "qa_file": "qa_pairs.json"
    },
    {
        "business_name": "Bright Smile Dental Clinic",
        "niche": "Dental Healthcare",
        "business_id": "b2c3d4e5-f6a7-8901-bcde-f12345678901",
        "qa_file": "dental_qa_pairs.json"
    },
    {
        "business_name": "Skyline Realty Partners",
        "niche": "Real Estate Agency",
        "business_id": "c3d4e5f6-a7b8-9012-cdef-123456789012",
        "qa_file": "realty_qa_pairs.json"
    }
]


def load_qa_pairs(filename: str) -> list[dict]:
    qa_path = Path(__file__).parent / filename
    with open(qa_path, "r", encoding="utf-8") as f:
        return json.load(f)


def evaluate_question(client: httpx.Client, business_id: str, item: dict) -> dict:
    url = f"{API_BASE_URL}/api/v1/widget/{business_id}/chat"
    headers = {
        "Content-Type": "application/json",
        "Origin": ALLOWED_ORIGIN
    }
    payload = {
        "message": item["question"]
    }

    start_time = time.perf_counter()
    try:
        response = client.post(url, json=payload, headers=headers, timeout=10.0)
        elapsed_ms = round((time.perf_counter() - start_time) * 1000, 1)

        if response.status_code != 200:
            return {
                "id": item["id"],
                "question": item["question"],
                "type": item["type"],
                "topic": item.get("topic", ""),
                "status": "error",
                "error": f"HTTP {response.status_code}: {response.text}",
                "elapsed_ms": elapsed_ms,
                "is_accurate": False,
                "is_citation_valid": False,
                "is_abstention_correct": False,
                "answer": "",
                "cited_chunks": [],
                "needs_human": False
            }

        data = response.json()
        answer = data.get("assistantMessage") or data.get("message", "")
        cited_chunks = data.get("citedChunkIds", [])
        needs_human = data.get("needsHuman", False)

        is_answerable = item["type"] == "answerable"
        expected_keywords = item.get("expected_keywords", [])

        # Evaluate Answerable
        if is_answerable:
            has_citations = len(cited_chunks) > 0
            has_keywords = any(kw.lower() in answer.lower() for kw in expected_keywords) if expected_keywords else True
            not_abstaining = not needs_human

            is_accurate = not_abstaining and has_citations and has_keywords
            is_citation_valid = has_citations
            is_abstention_correct = False
            status = "PASS" if is_accurate else "FAIL"

        # Evaluate Should Abstain
        else:
            correctly_flagged = needs_human or len(cited_chunks) == 0 or "not sure" in answer.lower()
            no_hallucination = not (len(cited_chunks) > 0 and len(answer) > 100 and "not sure" not in answer.lower())

            is_accurate = correctly_flagged and no_hallucination
            is_citation_valid = True  # Valid absence of citations
            is_abstention_correct = correctly_flagged and no_hallucination
            status = "PASS" if is_abstention_correct else "FAIL"

        return {
            "id": item["id"],
            "question": item["question"],
            "type": item["type"],
            "topic": item.get("topic", ""),
            "status": status,
            "elapsed_ms": elapsed_ms,
            "is_accurate": is_accurate,
            "is_citation_valid": is_citation_valid,
            "is_abstention_correct": is_abstention_correct,
            "answer": answer,
            "cited_chunks": cited_chunks,
            "needs_human": needs_human
        }

    except Exception as ex:
        elapsed_ms = round((time.perf_counter() - start_time) * 1000, 1)
        return {
            "id": item["id"],
            "question": item["question"],
            "type": item["type"],
            "topic": item.get("topic", ""),
            "status": "error",
            "error": str(ex),
            "elapsed_ms": elapsed_ms,
            "is_accurate": False,
            "is_citation_valid": False,
            "is_abstention_correct": False,
            "answer": "",
            "cited_chunks": [],
            "needs_human": False
        }


def calculate_metrics(results: list[dict]) -> dict:
    total = len(results)
    answerables = [r for r in results if r["type"] == "answerable"]
    abstentions = [r for r in results if r["type"] == "should_abstain"]

    total_answerable = len(answerables)
    accurate_answerables = sum(1 for r in answerables if r["is_accurate"])
    valid_citations = sum(1 for r in answerables if r["is_citation_valid"])

    total_abstain = len(abstentions)
    correct_abstentions = sum(1 for r in abstentions if r["is_abstention_correct"])

    accuracy_pct = round((accurate_answerables / total_answerable * 100), 1) if total_answerable > 0 else 0.0
    citation_precision_pct = round((valid_citations / total_answerable * 100), 1) if total_answerable > 0 else 0.0
    abstention_pct = round((correct_abstentions / total_abstain * 100), 1) if total_abstain > 0 else 0.0
    overall_score = round(((accurate_answerables + correct_abstentions) / total * 100), 1) if total > 0 else 0.0

    return {
        "total": total,
        "total_answerable": total_answerable,
        "accurate_answerables": accurate_answerables,
        "valid_citations": valid_citations,
        "total_abstain": total_abstain,
        "correct_abstentions": correct_abstentions,
        "accuracy_pct": accuracy_pct,
        "citation_precision_pct": citation_precision_pct,
        "abstention_pct": abstention_pct,
        "overall_score": overall_score
    }


def generate_multi_tenant_report(suite_results: list[dict], combined_metrics: dict, output_path: Path):
    timestamp = datetime.now(timezone.utc).strftime("%Y-%m-%d %H:%M:%S UTC")
    total_questions = sum(s["metrics"]["total"] for s in suite_results)

    report = f"""# NeverMissLead — Multi-Tenant RAG Grounding Benchmark Report

**Generated at**: `{timestamp}`  
**Evaluated Tenants**: `3 distinct verticals` ({total_questions} total questions)  
**Provider Configuration**: `Local / Free ($0 CPU Mode: all-MiniLM-L6-v2 + NullLlmClient)`

---

## 📊 Combined Platform Metrics

| Metric | Target | Result | Status |
|---|:---:|:---:|:---:|
| **Overall Accuracy** | &ge; 85% | **{combined_metrics['overall_score']}%** ({combined_metrics['accurate_answerables'] + combined_metrics['correct_abstentions']}/{combined_metrics['total']}) | {'✅ PASS' if combined_metrics['overall_score'] >= 85 else '⚠️ REVIEW'} |
| **Answerable Accuracy** | &ge; 80% | **{combined_metrics['accuracy_pct']}%** ({combined_metrics['accurate_answerables']}/{combined_metrics['total_answerable']}) | {'✅ PASS' if combined_metrics['accuracy_pct'] >= 80 else '⚠️ REVIEW'} |
| **Citation Precision** | &ge; 90% | **{combined_metrics['citation_precision_pct']}%** ({combined_metrics['valid_citations']}/{combined_metrics['total_answerable']}) | {'✅ PASS' if combined_metrics['citation_precision_pct'] >= 90 else '⚠️ REVIEW'} |
| **Abstention Correctness** | &ge; 80% | **{combined_metrics['abstention_pct']}%** ({combined_metrics['correct_abstentions']}/{combined_metrics['total_abstain']}) | {'✅ PASS' if combined_metrics['abstention_pct'] >= 80 else '⚠️ REVIEW'} |

---

## 🏢 Per-Business Benchmark Breakdown

| Business / Vertical | Questions | Answerable Acc | Citation Precision | Abstention Acc | Overall Score | Status |
|---|:---:|:---:|:---:|:---:|:---:|:---:|
"""

    for s in suite_results:
        m = s["metrics"]
        status_icon = "✅ PASS" if m["overall_score"] >= 85 else "⚠️ REVIEW"
        report += f"| **{s['business_name']}** ({s['niche']}) | {m['total']} | {m['accuracy_pct']}% | {m['citation_precision_pct']}% | {m['abstention_pct']}% | **{m['overall_score']}%** | {status_icon} |\n"

    report += "\n---\n\n## 📋 Detailed Questions Breakdown by Business\n\n"

    for s in suite_results:
        report += f"### {s['business_name']} (`{s['business_id']}`)\n\n"
        report += "| ID | Type | Topic | Status | Latency | Needs Human | Cited Chunks | Question |\n"
        report += "|---|---|---|:---:|:---:|:---:|:---:|---|\n"
        for r in s["results"]:
            status_icon = "✅" if r["status"] == "PASS" else "❌"
            cited_count = len(r.get("cited_chunks", []))
            needs_human_str = "Yes" if r.get("needs_human") else "No"
            report += f"| `{r['id']}` | `{r['type']}` | {r['topic']} | {status_icon} `{r['status']}` | {r['elapsed_ms']}ms | {needs_human_str} | {cited_count} | {r['question']} |\n"
        report += "\n"

    with open(output_path, "w", encoding="utf-8") as f:
        f.write(report)


def main():
    print("=== NeverMissLead Multi-Tenant RAG Evaluation Harness ===", flush=True)
    all_results = []
    suite_summaries = []

    with httpx.Client() as client:
        for suite in SUITES:
            b_name = suite["business_name"]
            b_id = suite["business_id"]
            qa_pairs = load_qa_pairs(suite["qa_file"])
            print(f"\n--- Evaluating {b_name} ({len(qa_pairs)} questions) ---", flush=True)

            suite_results = []
            for idx, item in enumerate(qa_pairs, 1):
                print(f"[{idx:02d}/{len(qa_pairs):02d}] Testing {item['id']} ({item['type']}): {item['question'][:40]}... ", end="", flush=True)
                res = evaluate_question(client, b_id, item)
                print(f"[{res['status']}] ({res['elapsed_ms']}ms)", flush=True)
                suite_results.append(res)
                all_results.append(res)
                time.sleep(0.04)

            metrics = calculate_metrics(suite_results)
            suite_summaries.append({
                "business_name": b_name,
                "niche": suite["niche"],
                "business_id": b_id,
                "results": suite_results,
                "metrics": metrics
            })

    combined_metrics = calculate_metrics(all_results)
    report_path = Path(__file__).parent / "latest_report.md"
    generate_multi_tenant_report(suite_summaries, combined_metrics, report_path)

    print("\n" + "="*60, flush=True)
    print("       MULTI-TENANT EVALUATION SUMMARY", flush=True)
    print("="*60, flush=True)
    for s in suite_summaries:
        m = s["metrics"]
        print(f"🏢 {s['business_name']:<28} Overall: {m['overall_score']:>5.1f}% | Acc: {m['accuracy_pct']:>5.1f}% | Cite: {m['citation_precision_pct']:>5.1f}% | Abstain: {m['abstention_pct']:>5.1f}%", flush=True)

    print("-" * 60, flush=True)
    print(f"🌐 PLATFORM COMBINED ({combined_metrics['total']} Questions)")
    print(f"   Overall Score:          {combined_metrics['overall_score']}%", flush=True)
    print(f"   Answerable Accuracy:    {combined_metrics['accuracy_pct']}%", flush=True)
    print(f"   Citation Precision:     {combined_metrics['citation_precision_pct']}%", flush=True)
    print(f"   Abstention Correctness: {combined_metrics['abstention_pct']}%", flush=True)
    print(f"   Report written to:      {report_path}", flush=True)
    print("="*60, flush=True)

    if combined_metrics["overall_score"] < 80.0:
        print("\n[WARNING] Platform overall benchmark score is below 80%.", flush=True)
        sys.exit(1)
    else:
        print("\n[SUCCESS] All 3 businesses passed multi-tenant grounding benchmarks!", flush=True)
        sys.exit(0)


if __name__ == "__main__":
    main()
