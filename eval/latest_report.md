# NeverMissLead — RAG Pipeline Evaluation Report

**Generated at**: `2026-08-22 05:01:22 UTC`  
**Test Suite**: `18 questions` (12 answerable, 6 out-of-scope / near-miss)  
**Provider Configuration**: `Local / Free ($0 CPU Mode: all-MiniLM-L6-v2 + NullLlmClient)`

---

## 📊 Summary Metrics

| Metric | Target | Result | Status |
|---|:---:|:---:|:---:|
| **Overall Accuracy** | &ge; 85% | **100.0%** | ✅ PASS |
| **Answerable Accuracy** | &ge; 80% | **100.0%** (12/12) | ✅ PASS |
| **Citation Precision** | &ge; 90% | **100.0%** (12/12) | ✅ PASS |
| **Abstention Correctness** | &ge; 80% | **100.0%** (6/6) | ✅ PASS |

---

## 📋 Detailed Results Breakdown

| ID | Type | Topic | Status | Latency | Needs Human | Cited Chunks | Question |
|---|---|---|:---:|:---:|:---:|:---:|---|
| `q01` | `answerable` | pricing | ✅ `PASS` | 110.0ms | No | 1 | What is the fee for private 1-on-1 tutoring sessions? |
| `q02` | `answerable` | pricing_and_batch_size | ✅ `PASS` | 25.6ms | No | 1 | How much do group classes cost and what is the maximum class size? |
| `q03` | `answerable` | trial_policy | ✅ `PASS` | 37.8ms | No | 1 | Do you offer a free trial class before enrolling? |
| `q04` | `answerable` | subjects_and_grades | ✅ `PASS` | 27.6ms | No | 1 | What grades and subjects do you teach? |
| `q05` | `answerable` | location | ✅ `PASS` | 25.4ms | No | 1 | Where is your in-person coaching center located? |
| `q06` | `answerable` | batch_timings | ✅ `PASS` | 27.4ms | No | 1 | What are the timings for weekday batches? |
| `q07` | `answerable` | pricing | ✅ `PASS` | 28.2ms | No | 1 | How much is the 4-week exam crash course? |
| `q08` | `answerable` | tutor_experience | ✅ `PASS` | 27.9ms | No | 1 | What qualifications and experience do the instructors have? |
| `q09` | `answerable` | rescheduling_policy | ✅ `PASS` | 29.6ms | No | 1 | How much advance notice is required to reschedule a 1-on-1 class? |
| `q10` | `answerable` | fees | ✅ `PASS` | 28.3ms | No | 1 | Is there a registration or assessment fee when joining? |
| `q11` | `answerable` | delivery_mode | ✅ `PASS` | 26.2ms | No | 1 | How are online sessions conducted? |
| `q12` | `answerable` | batch_timings | ✅ `PASS` | 24.7ms | No | 1 | What are the weekend batch hours? |
| `q13` | `should_abstain` | out_of_scope_subject | ✅ `PASS` | 27.4ms | Yes | 0 | Do you offer Grade 11 Biology or Medical NEET coaching? |
| `q14` | `should_abstain` | out_of_scope_exam | ✅ `PASS` | 25.1ms | Yes | 0 | Can I prepare for the GRE or GMAT exam with your tutors? |
| `q15` | `should_abstain` | unsupported_location | ✅ `PASS` | 20.3ms | Yes | 0 | Do you have a coaching branch located in Whitefield or Koramangala? |
| `q16` | `should_abstain` | unstated_discount_policy | ✅ `PASS` | 19.6ms | Yes | 0 | Is there a family discount or sibling discount if two kids enroll together? |
| `q17` | `should_abstain` | out_of_scope_subject | ✅ `PASS` | 25.1ms | Yes | 0 | Do you provide French, Spanish, or foreign language classes for high schoolers? |
| `q18` | `should_abstain` | unsupported_refund_claim | ✅ `PASS` | 31.1ms | Yes | 0 | Do you offer full money-back guarantees after attending 2 months of classes? |

---

## 🔍 Sample Answers & Grounding Evidence

### `[q01]` What is the fee for private 1-on-1 tutoring sessions?
- **Type**: `answerable` | **Status**: `PASS` | **Latency**: `110.0ms`
- **Response**: Based on our FAQ: Bright Minds Coaching — ## 3. Pricing & Fee Structure
- **Group Classes (Max 6 students)**: $35 per hour (billed monthly at $280/month for 2 sessions per week).
- **Private 1-on-1 Tutoring**: $65 per hour (customized pacing and individualized homework review).
- **Exam Crash Courses (4-week intensive)**: $450 flat fee covering 16 hours of targeted problem solving.
- **Registration Fee**: A one-time registration and diagnostic assessment fee of $25 is applied upon initial enrollment.
- **Cited Chunks**: `['b9b22879-c5c8-421f-beae-ad50b2ecd163']`

### `[q02]` How much do group classes cost and what is the maximum class size?
- **Type**: `answerable` | **Status**: `PASS` | **Latency**: `25.6ms`
- **Response**: Based on our FAQ: Bright Minds Coaching — ## 3. Pricing & Fee Structure
- **Group Classes (Max 6 students)**: $35 per hour (billed monthly at $280/month for 2 sessions per week).
- **Private 1-on-1 Tutoring**: $65 per hour (customized pacing and individualized homework review).
- **Exam Crash Courses (4-week intensive)**: $450 flat fee covering 16 hours of targeted problem solving.
- **Registration Fee**: A one-time registration and diagnostic assessment fee of $25 is applied upon initial enrollment.
- **Cited Chunks**: `['b9b22879-c5c8-421f-beae-ad50b2ecd163']`

### `[q03]` Do you offer a free trial class before enrolling?
- **Type**: `answerable` | **Status**: `PASS` | **Latency**: `37.8ms`
- **Response**: Based on our FAQ: Bright Minds Coaching — ## 3. Pricing & Fee Structure
- **Group Classes (Max 6 students)**: $35 per hour (billed monthly at $280/month for 2 sessions per week).
- **Private 1-on-1 Tutoring**: $65 per hour (customized pacing and individualized homework review).
- **Exam Crash Courses (4-week intensive)**: $450 flat fee covering 16 hours of targeted problem solving.
- **Registration Fee**: A one-time registration and diagnostic assessment fee of $25 is applied upon initial enrollment.
- **Cited Chunks**: `['b9b22879-c5c8-421f-beae-ad50b2ecd163']`

### `[q04]` What grades and subjects do you teach?
- **Type**: `answerable` | **Status**: `PASS` | **Latency**: `27.6ms`
- **Response**: Based on our FAQ: Bright Minds Coaching — ## 2. Subjects Offered
- **Mathematics**: Algebra, Geometry, Trigonometry, Pre-Calculus, Calculus AB/BC, SAT/AP Math prep.
- **Physics**: Conceptual Physics, AP Physics 1 & C, Mechanics, Electromagnetism.
- **Chemistry**: General Chemistry, Honors Chemistry, AP Chemistry.
- **Computer Science**: Python fundamentals, Java / AP Computer Science A, Data Structures.
- **Cited Chunks**: `['0aab947b-17d9-482c-84a7-68b24afb0adb']`

### `[q05]` Where is your in-person coaching center located?
- **Type**: `answerable` | **Status**: `PASS` | **Latency**: `25.4ms`
- **Response**: Based on our FAQ: Bright Minds Coaching — ## 6. Location & Delivery Mode
- **Online**: Interactive live video sessions on Google Meet / Zoom with digital whiteboard and recorded replay access.
- **In-Person**: Available at our Indiranagar Center (100ft Road, Bangalore) for local students.
- **Cited Chunks**: `['88d13c9c-d6a0-4e13-a95f-14356e3f0b9b']`

