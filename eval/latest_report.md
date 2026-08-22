# NeverMissLead — Multi-Tenant RAG Grounding Benchmark Report

**Generated at**: `2026-08-22 07:38:31 UTC`  
**Evaluated Tenants**: `3 distinct verticals` (48 total questions)  
**Provider Configuration**: `Local / Free ($0 CPU Mode: all-MiniLM-L6-v2 + NullLlmClient)`

---

## 📊 Combined Platform Metrics

| Metric | Target | Result | Status |
|---|:---:|:---:|:---:|
| **Overall Accuracy** | &ge; 85% | **100.0%** (48/48) | ✅ PASS |
| **Answerable Accuracy** | &ge; 80% | **100.0%** (32/32) | ✅ PASS |
| **Citation Precision** | &ge; 90% | **100.0%** (32/32) | ✅ PASS |
| **Abstention Correctness** | &ge; 80% | **100.0%** (16/16) | ✅ PASS |

---

## 🏢 Per-Business Benchmark Breakdown

| Business / Vertical | Questions | Answerable Acc | Citation Precision | Abstention Acc | Overall Score | Status |
|---|:---:|:---:|:---:|:---:|:---:|:---:|
| **Bright Minds Coaching** (STEM Tutoring) | 18 | 100.0% | 100.0% | 100.0% | **100.0%** | ✅ PASS |
| **Bright Smile Dental Clinic** (Dental Healthcare) | 15 | 100.0% | 100.0% | 100.0% | **100.0%** | ✅ PASS |
| **Skyline Realty Partners** (Real Estate Agency) | 15 | 100.0% | 100.0% | 100.0% | **100.0%** | ✅ PASS |

---

## 📋 Detailed Questions Breakdown by Business

### Bright Minds Coaching (`a1b2c3d4-e5f6-7890-abcd-ef1234567890`)

| ID | Type | Topic | Status | Latency | Needs Human | Cited Chunks | Question |
|---|---|---|:---:|:---:|:---:|:---:|---|
| `q01` | `answerable` | pricing | ✅ `PASS` | 82.2ms | No | 1 | What is the fee for private 1-on-1 tutoring sessions? |
| `q02` | `answerable` | pricing_and_batch_size | ✅ `PASS` | 20.8ms | No | 1 | How much do group classes cost and what is the maximum class size? |
| `q03` | `answerable` | trial_policy | ✅ `PASS` | 19.5ms | No | 1 | Do you offer a free trial class before enrolling? |
| `q04` | `answerable` | subjects_and_grades | ✅ `PASS` | 20.1ms | No | 1 | What grades and subjects do you teach? |
| `q05` | `answerable` | location | ✅ `PASS` | 23.5ms | No | 1 | Where is your in-person coaching center located? |
| `q06` | `answerable` | batch_timings | ✅ `PASS` | 24.0ms | No | 1 | What are the timings for weekday batches? |
| `q07` | `answerable` | pricing | ✅ `PASS` | 23.8ms | No | 1 | How much is the 4-week exam crash course? |
| `q08` | `answerable` | tutor_experience | ✅ `PASS` | 22.0ms | No | 1 | What qualifications and experience do the instructors have? |
| `q09` | `answerable` | rescheduling_policy | ✅ `PASS` | 21.8ms | No | 1 | How much advance notice is required to reschedule a 1-on-1 class? |
| `q10` | `answerable` | fees | ✅ `PASS` | 21.7ms | No | 1 | Is there a registration or assessment fee when joining? |
| `q11` | `answerable` | delivery_mode | ✅ `PASS` | 23.7ms | No | 1 | How are online sessions conducted? |
| `q12` | `answerable` | batch_timings | ✅ `PASS` | 21.7ms | No | 1 | What are the weekend batch hours? |
| `q13` | `should_abstain` | out_of_scope_subject | ✅ `PASS` | 20.6ms | Yes | 0 | Do you offer Grade 11 Biology or Medical NEET coaching? |
| `q14` | `should_abstain` | out_of_scope_exam | ✅ `PASS` | 22.3ms | Yes | 0 | Can I prepare for the GRE or GMAT exam with your tutors? |
| `q15` | `should_abstain` | unsupported_location | ✅ `PASS` | 23.7ms | Yes | 0 | Do you have a coaching branch located in Whitefield or Koramangala? |
| `q16` | `should_abstain` | unstated_discount_policy | ✅ `PASS` | 23.1ms | Yes | 0 | Is there a family discount or sibling discount if two kids enroll together? |
| `q17` | `should_abstain` | out_of_scope_subject | ✅ `PASS` | 23.3ms | Yes | 0 | Do you provide French, Spanish, or foreign language classes for high schoolers? |
| `q18` | `should_abstain` | unsupported_refund_claim | ✅ `PASS` | 37.0ms | Yes | 0 | Do you offer full money-back guarantees after attending 2 months of classes? |

### Bright Smile Dental Clinic (`b2c3d4e5-f6a7-8901-bcde-f12345678901`)

| ID | Type | Topic | Status | Latency | Needs Human | Cited Chunks | Question |
|---|---|---|:---:|:---:|:---:|:---:|---|
| `dental-01` | `answerable` | pricing | ✅ `PASS` | 17.7ms | No | 1 | How much is a routine dental cleaning and examination? |
| `dental-02` | `answerable` | services | ✅ `PASS` | 38.6ms | No | 1 | Do you offer Invisalign clear aligners? |
| `dental-03` | `answerable` | pricing | ✅ `PASS` | 21.9ms | No | 1 | What is the cost for in-office teeth whitening? |
| `dental-04` | `answerable` | insurance | ✅ `PASS` | 35.8ms | No | 1 | Do you accept Delta Dental and Cigna insurance? |
| `dental-05` | `answerable` | hours | ✅ `PASS` | 18.7ms | No | 1 | Are you open on Saturdays? |
| `dental-06` | `answerable` | emergencies | ✅ `PASS` | 19.6ms | No | 1 | What should I do if I have a severe tooth emergency after hours? |
| `dental-07` | `answerable` | policy | ✅ `PASS` | 19.8ms | No | 1 | What is your appointment cancellation policy? |
| `dental-08` | `answerable` | pricing | ✅ `PASS` | 19.6ms | No | 1 | How much do composite tooth fillings cost? |
| `dental-09` | `answerable` | financing | ✅ `PASS` | 38.5ms | No | 1 | Do you offer financing options for uninsured patients? |
| `dental-10` | `answerable` | services | ✅ `PASS` | 24.4ms | No | 1 | Do you treat pediatric and child patients? |
| `dental-11` | `should_abstain` | out_of_domain | ✅ `PASS` | 33.2ms | Yes | 0 | Do you offer automotive oil changes and tire replacement? |
| `dental-12` | `should_abstain` | out_of_domain | ✅ `PASS` | 19.0ms | Yes | 0 | Can I hire your team to cater my wedding reception? |
| `dental-13` | `should_abstain` | cross_tenant | ✅ `PASS` | 18.2ms | Yes | 0 | What is your hourly rate for AP Physics tutoring? |
| `dental-14` | `should_abstain` | cross_tenant | ✅ `PASS` | 20.0ms | Yes | 0 | What is your real estate commission on residential home sales? |
| `dental-15` | `should_abstain` | out_of_domain | ✅ `PASS` | 20.9ms | Yes | 0 | Do you manage maritime freight shipping containers? |

### Skyline Realty Partners (`c3d4e5f6-a7b8-9012-cdef-123456789012`)

| ID | Type | Topic | Status | Latency | Needs Human | Cited Chunks | Question |
|---|---|---|:---:|:---:|:---:|:---:|---|
| `realty-01` | `answerable` | commission | ✅ `PASS` | 22.6ms | No | 1 | What is your standard commission fee for selling a residential home? |
| `realty-02` | `answerable` | services | ✅ `PASS` | 24.8ms | No | 1 | Do buyer clients have to pay agent commission out of pocket? |
| `realty-03` | `answerable` | service_areas | ✅ `PASS` | 20.5ms | No | 1 | Which neighborhoods and areas do you cover? |
| `realty-04` | `answerable` | tours | ✅ `PASS` | 21.5ms | No | 1 | How much advance notice is required to schedule a private property viewing? |
| `realty-05` | `answerable` | timeline | ✅ `PASS` | 22.3ms | No | 1 | How long does it typically take from an accepted offer to closing? |
| `realty-06` | `answerable` | documents | ✅ `PASS` | 21.2ms | No | 1 | What documents are required when submitting a purchase offer? |
| `realty-07` | `answerable` | pricing | ✅ `PASS` | 23.1ms | No | 1 | How much does a home valuation or comparative market analysis cost? |
| `realty-08` | `answerable` | rentals | ✅ `PASS` | 21.7ms | No | 1 | What is your fee structure for rental tenant placement services? |
| `realty-09` | `answerable` | tours | ✅ `PASS` | 26.7ms | No | 1 | When are open houses held for your active listings? |
| `realty-10` | `answerable` | policy | ✅ `PASS` | 20.8ms | No | 1 | What is your listing agreement duration and cancellation policy? |
| `realty-11` | `should_abstain` | out_of_domain | ✅ `PASS` | 19.3ms | Yes | 0 | Do you provide veterinary surgery for pet animals? |
| `realty-12` | `should_abstain` | cross_tenant | ✅ `PASS` | 20.4ms | Yes | 0 | Do you accept Delta Dental insurance for teeth cleaning? |
| `realty-13` | `should_abstain` | cross_tenant | ✅ `PASS` | 20.6ms | Yes | 0 | What is your hourly rate for AP Physics tutoring? |
| `realty-14` | `should_abstain` | out_of_domain | ✅ `PASS` | 22.4ms | Yes | 0 | Can I hire your team to cater my wedding reception? |
| `realty-15` | `should_abstain` | out_of_domain | ✅ `PASS` | 21.4ms | Yes | 0 | Do you manage maritime freight shipping containers? |

