import os
import uuid
import asyncio
import psycopg2
from pathlib import Path

# Force local embedding
os.environ["EMBEDDING_PROVIDER"] = "local"

from config import settings
from embeddings import get_embedding_provider
from chunking import chunk_text
from retrieval import embed_and_store_chunks

# Fixed deterministic UUID for Bright Minds Coaching so frontend and tests can use it consistently
DEMO_BUSINESS_ID = uuid.UUID("a1b2c3d4-e5f6-7890-abcd-ef1234567890")

async def seed_demo():
    print(f"--- Seeding Demo Business: Bright Minds Coaching ({DEMO_BUSINESS_ID}) ---")
    
    faq_path = Path(__file__).parent / "tutor_faq_sample.md"
    with open(faq_path, "r", encoding="utf-8") as f:
        faq_text = f.read()

    doc_id = uuid.uuid4()

    with psycopg2.connect(settings.database_url) as conn:
        with conn.cursor() as cur:
            # 1. Clean existing records for this business if any
            cur.execute('DELETE FROM follow_up_tasks WHERE "BusinessId" = %s;', (str(DEMO_BUSINESS_ID),))
            cur.execute('DELETE FROM leads WHERE "BusinessId" = %s;', (str(DEMO_BUSINESS_ID),))
            cur.execute('DELETE FROM messages WHERE "ConversationId" IN (SELECT "Id" FROM conversations WHERE "BusinessId" = %s);', (str(DEMO_BUSINESS_ID),))
            cur.execute('DELETE FROM conversations WHERE "BusinessId" = %s;', (str(DEMO_BUSINESS_ID),))
            cur.execute('DELETE FROM kb_chunks WHERE "DocumentId" IN (SELECT "Id" FROM kb_documents WHERE "BusinessId" = %s);', (str(DEMO_BUSINESS_ID),))
            cur.execute('DELETE FROM kb_documents WHERE "BusinessId" = %s;', (str(DEMO_BUSINESS_ID),))
            cur.execute('DELETE FROM business_settings WHERE "BusinessId" = %s;', (str(DEMO_BUSINESS_ID),))
            cur.execute('DELETE FROM businesses WHERE "Id" = %s;', (str(DEMO_BUSINESS_ID),))
            
            # 2. Insert Business
            cur.execute(
                """
                INSERT INTO businesses ("Id", "Name", "Niche", "Timezone", "CreatedAt")
                VALUES (%s, %s, %s, %s, NOW());
                """,
                (str(DEMO_BUSINESS_ID), "Bright Minds Coaching", "private tutors", "Asia/Kolkata")
            )

            # 3. Insert Business Settings with PBKDF2 Hashed Password
            import base64, hashlib, os
            salt = os.urandom(16)
            subkey = hashlib.pbkdf2_hmac("sha512", b"BrightMinds2026!", salt, 100000, 32)
            demo_password_hash = f"100000.{base64.b64encode(salt).decode('utf-8')}.{base64.b64encode(subkey).decode('utf-8')}"

            cur.execute(
                """
                INSERT INTO business_settings ("BusinessId", "WidgetGreeting", "HandoffEmail", "BrandColor", "AllowedOrigins", "PasswordHash")
                VALUES (%s, %s, %s, %s, %s, %s);
                """,
                (
                    str(DEMO_BUSINESS_ID),
                    "Hi! I'm the Bright Minds AI assistant. Ask me anything about our subjects, batch timings, trial classes, or fees!",
                    "owner@brightminds.test",
                    "#4f46e5",
                    ["http://localhost:4200"],
                    demo_password_hash
                )
            )

            # 4. Insert Knowledge Document
            cur.execute(
                """
                INSERT INTO kb_documents ("Id", "BusinessId", "Title", "SourceType", "RawText", "UploadedAt")
                VALUES (%s, %s, %s, %s, %s, NOW());
                """,
                (
                    str(doc_id),
                    str(DEMO_BUSINESS_ID),
                    "Official FAQ & Pricing Guide 2026",
                    "FAQ",
                    faq_text
                )
            )
        conn.commit()

    print("Business and Document created in Postgres.")

    # 5. Chunk and Embed with LocalEmbeddingProvider by section
    import re
    raw_sections = [s.strip() for s in re.split(r"\n(?=##\s)", faq_text) if s.strip()]
    chunk_tuples = []
    for sec in raw_sections:
        if not sec.startswith("## "):
            continue
        content = f"Bright Minds Coaching — {sec}"
        chunk_tuples.append((content, len(content.split())))
    print(f"Document produced {len(chunk_tuples)} topic chunks.")

    provider = get_embedding_provider()
    chunk_ids = await embed_and_store_chunks(
        conn_str=settings.database_url,
        document_id=doc_id,
        business_id=DEMO_BUSINESS_ID,
        chunks=chunk_tuples,
        embedding_client=provider
    )

    print(f"Successfully embedded and inserted {len(chunk_ids)} chunks into kb_chunks!")
    print(f"Demo business seeded with ID: {DEMO_BUSINESS_ID}")

if __name__ == "__main__":
    asyncio.run(seed_demo())
