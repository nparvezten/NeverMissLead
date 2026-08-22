using Microsoft.EntityFrameworkCore;
using NeverMissLead.Application.Interfaces;
using NeverMissLead.Domain.Entities;
using NeverMissLead.Domain.Enums;

namespace NeverMissLead.Infrastructure.Persistence;

/// <summary>
/// Seeds reference data on startup across multiple distinct business verticals.
/// </summary>
public static class DatabaseSeeder
{
    public static readonly Guid DemoBusinessId = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890");
    public static readonly Guid DentalBusinessId = Guid.Parse("b2c3d4e5-f6a7-8901-bcde-f12345678901");
    public static readonly Guid RealtyBusinessId = Guid.Parse("c3d4e5f6-a7b8-9012-cdef-123456789012");

    public static async Task SeedAsync(
        ApplicationDbContext db,
        IPasswordHashService passwordHashService,
        IEncryptionService encryptionService,
        CancellationToken cancellationToken = default)
    {
        // ── 1. Tenant: Bright Minds Coaching (Tutoring) ───────────────────────────
        if (!await db.Businesses.AnyAsync(b => b.Id == DemoBusinessId, cancellationToken))
        {
            var business = Business.Create("Bright Minds Coaching", "Coaching & Tutoring", "Asia/Kolkata", DemoBusinessId);
            db.Businesses.Add(business);

            var passwordHash = passwordHashService.HashPassword("BrightMinds2026!");
            var settings = BusinessSettings.Create(
                DemoBusinessId,
                "owner@brightminds.test",
                "Hi! I'm the Bright Minds AI assistant. Ask me anything about our subjects, batch timings, trial classes, or fees!",
                "#4f46e5",
                ["http://localhost:4200", "http://127.0.0.1:4200", "http://localhost:5103"],
                passwordHash);
            db.BusinessSettings.Add(settings);

            var sampleFaq = """
                # Bright Minds Coaching — Official FAQ & Pricing Guide 2026

                ## 1. About Us
                Bright Minds Coaching provides premium STEM tutoring for middle and high school students (Grades 6–12). Our tutors are graduates from top engineering and science universities with 5+ years of teaching experience.

                ## 2. Subjects Offered
                - **Mathematics**: Algebra, Geometry, Trigonometry, Pre-Calculus, Calculus AB/BC, SAT/AP Math prep.
                - **Physics**: Conceptual Physics, AP Physics 1 & C, Mechanics, Electromagnetism.
                - **Chemistry**: General Chemistry, Honors Chemistry, AP Chemistry.
                - **Computer Science**: Python fundamentals, Java / AP Computer Science A, Data Structures.

                ## 3. Pricing & Fee Structure
                - **Group Classes (Max 6 students)**: $35 per hour (billed monthly at $280/month for 2 sessions per week).
                - **Private 1-on-1 Tutoring**: $65 per hour (customized pacing and individualized homework review).
                - **Exam Crash Courses (4-week intensive)**: $450 flat fee covering 16 hours of targeted problem solving.
                - **Registration Fee**: A one-time registration and diagnostic assessment fee of $25 is applied upon initial enrollment.

                ## 4. Batch Timings & Schedule
                - **Weekday Batches**: Monday through Thursday from 4:30 PM to 6:00 PM and 6:30 PM to 8:00 PM IST / EST.
                - **Weekend Batches**: Saturday and Sunday mornings from 9:30 AM to 11:30 AM and 2:00 PM to 4:00 PM.
                - **Flexible 1-on-1 Slots**: Scheduled directly with the student's assigned tutor based on mutual availability.

                ## 5. Free Trial & Demo Policy
                - We offer a **100% free, no-obligation 45-minute trial session** for any subject.
                - The trial includes a quick diagnostic assessment and discussion with the lead instructor.

                ## 6. Location & Delivery Mode
                - **Online**: Interactive live video sessions on Google Meet / Zoom with digital whiteboard and recorded replay access.
                - **In-Person**: Available at our Indiranagar Center (100ft Road, Bangalore) for local students.

                ## 7. Attendance, Rescheduling & Refund Policy
                - 24-hour advance notice is required to reschedule a 1-on-1 session without penalty.
                - Monthly fee subscriptions can be cancelled with 7 days' notice prior to the next billing cycle.
                - Unused hours for 1-on-1 packages remain valid for 90 days.
                """;

            var doc = KbDocument.Create(DemoBusinessId, "Bright Minds Coaching Official FAQ & Pricing Guide", sampleFaq, SourceType.PlainText);
            db.KbDocuments.Add(doc);

            var conversation = Conversation.Start(DemoBusinessId, "vis_demo_tutoring");
            db.Conversations.Add(conversation);

            var lead = Lead.Capture(
                conversation.Id,
                DemoBusinessId,
                encryptionService.Encrypt("Sarah Jenkins"),
                encryptionService.Encrypt("+1 (555) 234-5678"),
                encryptionService.Encrypt("sarah.jenkins@example.com"),
                "Interested in Grade 11 AP Calculus AB private tutoring and weekend batches.",
                100);
            db.Leads.Add(lead);

            var followUp = FollowUpTask.ScheduleForLead(
                lead.Id,
                DemoBusinessId,
                DateTime.UtcNow.AddHours(1),
                "Hi Sarah, thanks for reaching out to Bright Minds Coaching! Would you like to schedule your free 45-minute AP Calculus trial class this weekend?",
                FollowUpChannel.Email,
                conversation.Id);
            db.FollowUpTasks.Add(followUp);
        }

        // ── 2. Tenant: Bright Smile Dental Clinic ──────────────────────────────────
        if (!await db.Businesses.AnyAsync(b => b.Id == DentalBusinessId, cancellationToken))
        {
            var business = Business.Create("Bright Smile Dental Clinic", "Dental Care", "America/New_York", DentalBusinessId);
            db.Businesses.Add(business);

            var passwordHash = passwordHashService.HashPassword("BrightSmile2026!");
            var settings = BusinessSettings.Create(
                DentalBusinessId,
                "owner@brightsmile.test",
                "Hello! Welcome to Bright Smile Dental Clinic. Ask me about our treatments, cleaning costs, accepted insurance, or appointments!",
                "#0d9488",
                ["http://localhost:4200", "http://127.0.0.1:4200", "http://localhost:5103"],
                passwordHash);
            db.BusinessSettings.Add(settings);

            var dentalFaq = """
                # Bright Smile Dental Clinic — Official FAQ & Services Guide 2026

                ## 1. About Our Clinic
                Bright Smile Dental Clinic provides comprehensive general, cosmetic, and pediatric dental care with over 15 years of combined clinical excellence.

                ## 2. Dental Services Offered
                - **Preventive Care**: Routine dental cleaning, oral examinations, digital X-rays, fluoride treatment.
                - **Restorative Dentistry**: Composite tooth-colored fillings, porcelain crowns, root canal therapy.
                - **Orthodontics**: Invisalign clear aligners, ceramic braces, traditional metal braces.
                - **Cosmetic Procedures**: In-office laser teeth whitening, take-home custom whitening trays, veneers.
                - **Pediatric Dentistry**: Child exams, gentle cleanings, habit counseling.
                - **Oral Surgery**: Simple and surgical tooth extractions, wisdom teeth removal.

                ## 3. Pricing & Fee Schedule
                - **Routine Dental Cleaning & Examination**: $120.
                - **Digital X-Ray Series**: $65 (complimentary with first comprehensive exam).
                - **Composite Tooth-Colored Filling**: $180 to $250 per tooth.
                - **Single-Visit Porcelain Crown**: $950 per unit.
                - **In-Office Laser Teeth Whitening**: $350.
                - **Invisalign Clear Aligners**: Complete treatment packages start from $3,800 following free 3D scan.
                - **Emergency Examination**: $95 flat fee.

                ## 4. Insurance & Payment Options
                - In-network with **Delta Dental, Cigna, MetLife, Guardian, Aetna, United Concordia**.
                - Uninsured savings plan: $199/year (2 free cleanings + 20% off procedures).
                - Financing via **CareCredit** and **LendingClub** (up to 24 months 0% interest).

                ## 5. Clinic Hours & Location
                - Monday–Thursday 8:00 AM–6:00 PM, Friday 8:00 AM–4:00 PM, Saturday 9:00 AM–2:00 PM.
                - Location: 450 Medical Arts Pavilion, Suite 200, Metro Health District.

                ## 6. Appointment Booking & Cancellation Policy
                - Online 24/7 or by phone. 24 hours' notice required to avoid $50 late fee.

                ## 7. Emergency Dental Care Policy
                - Daily reserved emergency slots. Call and press 1 for on-call dentist.
                """;

            var doc = KbDocument.Create(DentalBusinessId, "Bright Smile Dental Clinic FAQ & Services Guide", dentalFaq, SourceType.PlainText);
            db.KbDocuments.Add(doc);

            var conversation = Conversation.Start(DentalBusinessId, "vis_demo_dental");
            db.Conversations.Add(conversation);

            var lead = Lead.Capture(
                conversation.Id,
                DentalBusinessId,
                encryptionService.Encrypt("David Miller"),
                encryptionService.Encrypt("+1 (555) 345-6789"),
                encryptionService.Encrypt("david.miller@example.com"),
                "Inquiring about Invisalign consultation and teeth whitening package.",
                90);
            db.Leads.Add(lead);

            var followUp = FollowUpTask.ScheduleForLead(
                lead.Id,
                DentalBusinessId,
                DateTime.UtcNow.AddHours(1),
                "Hi David, Bright Smile Dental Clinic here! Would you like to schedule your free 3D digital scan for Invisalign?",
                FollowUpChannel.Email,
                conversation.Id);
            db.FollowUpTasks.Add(followUp);
        }

        // ── 3. Tenant: Skyline Realty Partners ────────────────────────────────────
        if (!await db.Businesses.AnyAsync(b => b.Id == RealtyBusinessId, cancellationToken))
        {
            var business = Business.Create("Skyline Realty Partners", "Real Estate Brokerage", "America/Los_Angeles", RealtyBusinessId);
            db.Businesses.Add(business);

            var passwordHash = passwordHashService.HashPassword("Skyline2026!");
            var settings = BusinessSettings.Create(
                RealtyBusinessId,
                "owner@skylinerealty.test",
                "Welcome to Skyline Realty Partners! How can we assist with your home purchase, property sale, or luxury rental?",
                "#0284c7",
                ["http://localhost:4200", "http://127.0.0.1:4200", "http://localhost:5103"],
                passwordHash);
            db.BusinessSettings.Add(settings);

            var realtyFaq = """
                # Skyline Realty Partners — Official FAQ & Client Advisory Guide 2026

                ## 1. About Our Agency
                Skyline Realty Partners is a full-service residential and commercial real estate brokerage with over $250M in closed transactions.

                ## 2. Real Estate Services Offered
                - **Buyer Representation**: Property search, private tours, valuation, offer drafting, escrow negotiation.
                - **Seller Listings**: Market analysis (CMA), HDR photography, 3D Matterport virtual tours, MLS syndication.
                - **Luxury & Residential Rentals**: Tenant placement, screening, lease agreements.
                - **Commercial & Office Leasing**: Tenant and landlord representation.
                - **Investment Advisory**: Multi-family property underwriting, cap rate analysis, 1031 tax exchanges.

                ## 3. Commission Rates & Fee Structure
                - **Residential Property Sales**: Standard **5% total sales commission** (2.5% listing / 2.5% buyer agent).
                - **Buyer Representation Fee**: Paid by the seller out of closing proceeds; zero direct fee to buyer.
                - **Rental Tenant Placement**: Flat fee equivalent to **one month's rent**.
                - **Home Valuation Consultation**: 100% complimentary CMA report.

                ## 4. Service Areas & Neighborhoods
                - Downtown Core, Financial District, Waterfront Promenade, Uptown Arts District, North Bay Suburbs, Eastridge Hills.

                ## 5. Property Viewing & Tour Scheduling
                - Private tours require 24 hours' notice. Weekend open houses Saturday/Sunday 1:00 PM–4:00 PM.

                ## 6. Transaction Timeline & Closing Process
                - Accepted offer to closing average: **30 to 45 calendar days** (Cash: 10–14 days).
                - Required documents: Mortgage pre-approval letter or proof of funds, photo ID.

                ## 7. Agency Representation & Cancellation Policy
                - 90-day standard listing agreement with unconditional 7-day cancellation guarantee.
                """;

            var doc = KbDocument.Create(RealtyBusinessId, "Skyline Realty Partners FAQ & Advisory Guide", realtyFaq, SourceType.PlainText);
            db.KbDocuments.Add(doc);

            var conversation = Conversation.Start(RealtyBusinessId, "vis_demo_realty");
            db.Conversations.Add(conversation);

            var lead = Lead.Capture(
                conversation.Id,
                RealtyBusinessId,
                encryptionService.Encrypt("Elena Rostova"),
                encryptionService.Encrypt("+1 (555) 456-7890"),
                encryptionService.Encrypt("elena.rostova@example.com"),
                "Looking to sell 3-bedroom home in Waterfront District and explore Downtown condos.",
                95);
            db.Leads.Add(lead);

            var followUp = FollowUpTask.ScheduleForLead(
                lead.Id,
                RealtyBusinessId,
                DateTime.UtcNow.AddHours(1),
                "Hi Elena, Skyline Realty Partners here! We have compiled your complimentary CMA for your Waterfront property.",
                FollowUpChannel.Email,
                conversation.Id);
            db.FollowUpTasks.Add(followUp);
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
