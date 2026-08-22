import os
import re
import uuid
import asyncio
import base64
import hashlib
import psycopg2
from pathlib import Path

# Force local embedding
os.environ["EMBEDDING_PROVIDER"] = "local"

from config import settings
from embeddings import get_embedding_provider
from retrieval import embed_and_store_chunks

# Deterministic tenant IDs
BUSINESSES = [
    {
        "id": uuid.UUID("a1b2c3d4-e5f6-7890-abcd-ef1234567890"),
        "name": "Bright Minds Coaching",
        "niche": "private tutors",
        "timezone": "Asia/Kolkata",
        "faq_file": "tutor_faq_sample.md",
        "title": "Official FAQ & Pricing Guide 2026",
        "email": "owner@brightminds.test",
        "password": b"BrightMinds2026!",  # nosec B105 - demo seed credentials
        "greeting": "Hi! I'm the Bright Minds AI assistant. Ask me anything about our subjects, batch timings, trial classes, or fees!",
        "brand_color": "#4f46e5",
        "prefix": "Bright Minds Coaching"
    },
    {
        "id": uuid.UUID("b2c3d4e5-f6a7-8901-bcde-f12345678901"),
        "name": "Bright Smile Dental Clinic",
        "niche": "dental clinic",
        "timezone": "America/New_York",
        "faq_file": "dental_faq_sample.md",
        "title": "Official FAQ & Services Guide 2026",
        "email": "owner@brightsmile.test",
        "password": b"BrightSmile2026!",  # nosec B105 - demo seed credentials
        "greeting": "Hello! Welcome to Bright Smile Dental Clinic. Ask me about our treatments, cleaning costs, accepted insurance, or appointments!",
        "brand_color": "#0d9488",
        "prefix": "Bright Smile Dental Clinic"
    },
    {
        "id": uuid.UUID("c3d4e5f6-a7b8-9012-cdef-123456789012"),
        "name": "Skyline Realty Partners",
        "niche": "real estate agency",
        "timezone": "America/Los_Angeles",
        "faq_file": "realty_faq_sample.md",
        "title": "Official FAQ & Client Advisory Guide 2026",
        "email": "owner@skylinerealty.test",
        "password": b"Skyline2026!",  # nosec B105 - demo seed credentials
        "greeting": "Welcome to Skyline Realty Partners! How can we assist with your home purchase, property sale, or luxury rental?",
        "brand_color": "#0284c7",
        "prefix": "Skyline Realty Partners"
    }
]

async def seed_all_tenants():
    print("=== Seeding Multi-Tenant Data for 3 Businesses ===")
    provider = get_embedding_provider()

    with psycopg2.connect(settings.database_url) as conn:
        with conn.cursor() as cur:
            for b in BUSINESSES:
                b_id = str(b["id"])
                print(f"\n--- Seeding {b['name']} ({b_id}) ---")

                # Read FAQ file
                faq_path = Path(__file__).parent / b["faq_file"]
                with open(faq_path, "r", encoding="utf-8") as f:
                    faq_text = f.read()

                # 1. Clean existing records for this business
                cur.execute('DELETE FROM follow_up_tasks WHERE "BusinessId" = %s;', (b_id,))
                cur.execute('DELETE FROM leads WHERE "BusinessId" = %s;', (b_id,))
                cur.execute('DELETE FROM messages WHERE "ConversationId" IN (SELECT "Id" FROM conversations WHERE "BusinessId" = %s);', (b_id,))
                cur.execute('DELETE FROM conversations WHERE "BusinessId" = %s;', (b_id,))
                cur.execute('DELETE FROM kb_chunks WHERE "DocumentId" IN (SELECT "Id" FROM kb_documents WHERE "BusinessId" = %s);', (b_id,))
                cur.execute('DELETE FROM kb_documents WHERE "BusinessId" = %s;', (b_id,))
                cur.execute('DELETE FROM business_settings WHERE "BusinessId" = %s;', (b_id,))
                cur.execute('DELETE FROM businesses WHERE "Id" = %s;', (b_id,))

                # 2. Insert Business
                cur.execute(
                    """
                    INSERT INTO businesses ("Id", "Name", "Niche", "Timezone", "CreatedAt")
                    VALUES (%s, %s, %s, %s, NOW());
                    """,
                    (b_id, b["name"], b["niche"], b["timezone"])
                )

                # 3. Insert Business Settings with PBKDF2 Hashed Password
                salt = os.urandom(16)
                subkey = hashlib.pbkdf2_hmac("sha512", b["password"], salt, 100000, 32)
                password_hash = f"100000.{base64.b64encode(salt).decode('utf-8')}.{base64.b64encode(subkey).decode('utf-8')}"

                cur.execute(
                    """
                    INSERT INTO business_settings ("BusinessId", "WidgetGreeting", "HandoffEmail", "BrandColor", "AllowedOrigins", "PasswordHash")
                    VALUES (%s, %s, %s, %s, %s, %s);
                    """,
                    (
                        b_id,
                        b["greeting"],
                        b["email"],
                        b["brand_color"],
                        ["http://localhost:4200", "http://127.0.0.1:4200", "http://localhost:5103"],
                        password_hash
                    )
                )

                # 4. Insert Knowledge Document
                doc_id = uuid.uuid4()
                cur.execute(
                    """
                    INSERT INTO kb_documents ("Id", "BusinessId", "Title", "SourceType", "RawText", "UploadedAt")
                    VALUES (%s, %s, %s, %s, %s, NOW());
                    """,
                    (
                        str(doc_id),
                        b_id,
                        b["title"],
                        "FAQ",
                        faq_text
                    )
                )
                conn.commit()

                # 5. Chunk and Embed
                raw_sections = [s.strip() for s in re.split(r"\n(?=##\s)", faq_text) if s.strip()]
                chunk_tuples = []
                for sec in raw_sections:
                    if not sec.startswith("## "):
                        continue
                    content = f"{b['prefix']} — {sec}"
                    chunk_tuples.append((content, len(content.split())))

                print(f"Embedding {len(chunk_tuples)} topic chunks for {b['name']}...")
                chunk_ids = await embed_and_store_chunks(
                    conn_str=settings.database_url,
                    document_id=doc_id,
                    business_id=b["id"],
                    chunks=chunk_tuples,
                    embedding_client=provider
                )
                print(f"Stored {len(chunk_ids)} chunks for {b['name']}.")

    print("\n✅ All 3 businesses successfully seeded and embedded!")

if __name__ == "__main__":
    asyncio.run(seed_all_tenants())
