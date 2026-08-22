using Microsoft.EntityFrameworkCore;
using NeverMissLead.Application.Interfaces;
using NeverMissLead.Domain.Entities;
using NeverMissLead.Domain.Enums;

namespace NeverMissLead.Infrastructure.Persistence;

/// <summary>
/// Seeds reference data on startup if the database is unpopulated.
/// </summary>
public static class DatabaseSeeder
{
    public static readonly Guid DemoBusinessId = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890");

    public static async Task SeedAsync(
        ApplicationDbContext db,
        IPasswordHashService passwordHashService,
        IEncryptionService encryptionService,
        CancellationToken cancellationToken = default)
    {
        if (await db.Businesses.AnyAsync(b => b.Id == DemoBusinessId, cancellationToken))
        {
            return;
        }

        // 1. Create Demo Business
        var business = Business.Create(
            "Bright Minds Coaching",
            "Coaching & Tutoring",
            "Asia/Kolkata",
            DemoBusinessId);
        db.Businesses.Add(business);

        // 2. Create Settings with PBKDF2 Password Hash
        var passwordHash = passwordHashService.HashPassword("BrightMinds2026!");
        var settings = BusinessSettings.Create(
            DemoBusinessId,
            "owner@brightminds.test",
            "Hi there! Welcome to Bright Minds Coaching. How can we help you today?",
            "#2563EB",
            ["http://localhost:4200", "http://127.0.0.1:4200", "http://localhost:5103"],
            passwordHash);
        db.BusinessSettings.Add(settings);

        // 3. Create Sample Knowledge Document
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

        var doc = KbDocument.Create(
            DemoBusinessId,
            "Bright Minds Coaching Official FAQ & Pricing Guide",
            sampleFaq,
            SourceType.PlainText);
        db.KbDocuments.Add(doc);

        // 4. Create Initial Conversation & Sample Lead
        var conversation = Conversation.Start(DemoBusinessId, "vis_demo_initial");
        db.Conversations.Add(conversation);

        var nameEnc = encryptionService.Encrypt("Sarah Jenkins");
        var phoneEnc = encryptionService.Encrypt("+1 (555) 234-5678");
        var emailEnc = encryptionService.Encrypt("sarah.jenkins@example.com");

        var lead = Lead.Capture(
            conversation.Id,
            DemoBusinessId,
            nameEnc,
            phoneEnc,
            emailEnc,
            "Interested in Grade 11 AP Calculus AB private tutoring and weekend batches.",
            100);
        db.Leads.Add(lead);

        // 5. Follow-Up Task
        var followUp = FollowUpTask.ScheduleForLead(
            lead.Id,
            DemoBusinessId,
            DateTime.UtcNow.AddHours(1),
            "Hi Sarah, thanks for reaching out to Bright Minds Coaching! Would you like to schedule your free 45-minute AP Calculus trial class this weekend?",
            FollowUpChannel.Email,
            conversation.Id);
        db.FollowUpTasks.Add(followUp);

        await db.SaveChangesAsync(cancellationToken);
    }
}
