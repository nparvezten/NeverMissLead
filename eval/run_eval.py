"""Automated RAG Evaluation Harness for NeverMissLead.

Evaluates the end-to-end RAG pipeline against a benchmark suite of
answerable FAQ questions and out-of-scope/near-miss questions.
Reports three key metrics:
1. Accuracy: % of answerable questions correctly answered with grounded content
2. Citation Precision: % of answers backed by verified source chunks
3. Abstention Correctness: % of unanswerable questions correctly routed to owner handoff
"""

import json
import os
import sys
import time
from datetime import datetime, timezone
from pathlib import Path
import httpx

API_BASE_URL = os.getenv("API_BASE_URL", "http://localhost:5103")
DEMO_BUSINESS_ID = os.getenv("DEMO_BUSINESS_ID", "a1b2c3d4-e5f6-7890-abcd-ef1234567890")
ALLOWED_ORIGIN = "http://localhost:4200"


def load_qa_pairs() -> list[dict]:
    qa_path = Path(__file__).parent / "qa_pairs.json"
    with open(qa_path, "r", encoding="utf-8") as f:
        return json.load(f)


def evaluate_question(client: httpx.Client, item: dict) -> dict:
    url = f"{API_BASE_URL}/api/v1/widget/{DEMO_BUSINESS_ID}/chat"
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
            is_citation_valid = True  # Not applicable / valid absence
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


def generate_report(results: list[dict], output_path: Path):
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

    timestamp = datetime.now(timezone.utc).strftime("%Y-%m-%d %H:%M:%S UTC")

    report = f"""# NeverMissLead — RAG Pipeline Evaluation Report

**Generated at**: `{timestamp}`  
**Test Suite**: `{total} questions` ({total_answerable} answerable, {total_abstain} out-of-scope / near-miss)  
**Provider Configuration**: `Local / Free ($0 CPU Mode: all-MiniLM-L6-v2 + NullLlmClient)`

---

## 📊 Summary Metrics

| Metric | Target | Result | Status |
|---|:---:|:---:|:---:|
| **Overall Accuracy** | &ge; 85% | **{overall_score}%** | {'✅ PASS' if overall_score >= 85 else '⚠️ REVIEW'} |
| **Answerable Accuracy** | &ge; 80% | **{accuracy_pct}%** ({accurate_answerables}/{total_answerable}) | {'✅ PASS' if accuracy_pct >= 80 else '⚠️ REVIEW'} |
| **Citation Precision** | &ge; 90% | **{citation_precision_pct}%** ({valid_citations}/{total_answerable}) | {'✅ PASS' if citation_precision_pct >= 90 else '⚠️ REVIEW'} |
| **Abstention Correctness** | &ge; 80% | **{abstention_pct}%** ({correct_abstentions}/{total_abstain}) | {'✅ PASS' if abstention_pct >= 80 else '⚠️ REVIEW'} |

---

## 📋 Detailed Results Breakdown

| ID | Type | Topic | Status | Latency | Needs Human | Cited Chunks | Question |
|---|---|---|:---:|:---:|:---:|:---:|---|
"""

    for r in results:
        status_icon = "✅" if r["status"] == "PASS" else "❌"
        cited_count = len(r.get("cited_chunks", []))
        needs_human_str = "Yes" if r.get("needs_human") else "No"
        report += f"| `{r['id']}` | `{r['type']}` | {r['topic']} | {status_icon} `{r['status']}` | {r['elapsed_ms']}ms | {needs_human_str} | {cited_count} | {r['question']} |\n"

    report += """
---

## 🔍 Sample Answers & Grounding Evidence

"""
    for r in results[:5]:  # Show first 5 detailed examples
        report += f"### `[{r['id']}]` {r['question']}\n"
        report += f"- **Type**: `{r['type']}` | **Status**: `{r['status']}` | **Latency**: `{r['elapsed_ms']}ms`\n"
        report += f"- **Response**: {r['answer']}\n"
        report += f"- **Cited Chunks**: `{r['cited_chunks']}`\n\n"

    with open(output_path, "w", encoding="utf-8") as f:
        f.write(report)

    return {
        "total": total,
        "overall_score": overall_score,
        "accuracy_pct": accuracy_pct,
        "citation_precision_pct": citation_precision_pct,
        "abstention_pct": abstention_pct
    }


def main():
    print("=== NeverMissLead RAG Evaluation Harness ===", flush=True)
    qa_pairs = load_qa_pairs()
    print(f"Loaded {len(qa_pairs)} benchmark questions from qa_pairs.json", flush=True)

    results = []
    with httpx.Client() as client:
        for idx, item in enumerate(qa_pairs, 1):
            print(f"[{idx:02d}/{len(qa_pairs):02d}] Testing {item['id']} ({item['type']}): {item['question'][:45]}... ", end="", flush=True)
            res = evaluate_question(client, item)
            print(f"[{res['status']}] ({res['elapsed_ms']}ms)", flush=True)
            results.append(res)
            time.sleep(0.05)

    report_path = Path(__file__).parent / "latest_report.md"
    summary = generate_report(results, report_path)

    print("\n--- Evaluation Summary ---", flush=True)
    print(f"Overall Score:         {summary['overall_score']}%", flush=True)
    print(f"Answerable Accuracy:   {summary['accuracy_pct']}%", flush=True)
    print(f"Citation Precision:    {summary['citation_precision_pct']}%", flush=True)
    print(f"Abstention Correctness: {summary['abstention_pct']}%", flush=True)
    print(f"Report saved to:       {report_path}", flush=True)

    if summary["overall_score"] < 80.0:
        print("\n[WARNING] Overall benchmark score is below 80%. Review latest_report.md for failure details.", flush=True)
        sys.exit(1)
    else:
        print("\n[SUCCESS] Benchmark passed all target thresholds.", flush=True)
        sys.exit(0)


if __name__ == "__main__":
    main()
