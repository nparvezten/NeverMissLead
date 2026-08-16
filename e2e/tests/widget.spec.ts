import { test, expect } from '@playwright/test';

test.describe('NeverMissLead Chat Widget E2E', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('http://localhost:4200');
  });

  test('Happy Path: open widget, ask FAQ question, get cited answer with source chip', async ({ page }) => {
    // 1. Open the chat widget
    const launcherBtn = page.locator('.nml-launcher-btn');
    await expect(launcherBtn).toBeVisible();
    await launcherBtn.click();

    // 2. Verify chat window opens
    const chatWindow = page.locator('.nml-chat-window');
    await expect(chatWindow).toHaveClass(/visible/);

    // 3. Ask question from seeded FAQ
    const input = page.locator('.nml-chat-input');
    await input.fill('What is the fee for private 1-on-1 tutoring?');
    await page.locator('.nml-send-btn').click();

    // 4. Verify assistant response arrives
    const assistantBubble = page.locator('.nml-message-row.assistant .nml-message-content').last();
    await expect(assistantBubble).toContainText('65', { timeout: 15000 });

    // 5. Verify source chip is displayed
    const sourceChip = page.locator('.nml-source-chip').last();
    await expect(sourceChip).toBeVisible();
    await expect(sourceChip).toContainText('source');
  });

  test('Abstention Path: unanswerable question triggers handoff banner', async ({ page }) => {
    // 1. Open widget
    await page.locator('.nml-launcher-btn').click();

    // 2. Ask unanswerable question
    const input = page.locator('.nml-chat-input');
    await input.fill('Do you offer advanced scuba diving and swimming lessons?');
    await page.locator('.nml-send-btn').click();

    // 3. Verify handoff response and banner
    const assistantBubble = page.locator('.nml-message-row.assistant .nml-message-content').last();
    await expect(assistantBubble).toContainText("I'm not sure about that", { timeout: 15000 });

    const handoffBanner = page.locator('.nml-handoff-banner').last();
    await expect(handoffBanner).toBeVisible();
    await expect(handoffBanner).toContainText('flagged this for the owner');
  });

  test('Lead Capture Path: pricing question triggers contact capture form and submission', async ({ page }) => {
    // 1. Open widget
    await page.locator('.nml-launcher-btn').click();

    // 2. Ask pricing question that triggers lead intent
    const input = page.locator('.nml-chat-input');
    await input.fill('I would like to enroll and know the batch timing and price');
    await page.locator('.nml-send-btn').click();

    // 3. Verify inline contact card appears
    const contactCard = page.locator('.nml-contact-card');
    await expect(contactCard).toBeVisible({ timeout: 15000 });

    // 4. Fill in contact details
    await page.locator('input[name="contactName"]').fill('Sarah Jenkins');
    await page.locator('input[name="contactEmail"]').fill('sarah.jenkins@example.com');
    await page.locator('input[name="contactPhone"]').fill('+1-555-839-2019');

    // 5. Submit contact form
    await page.locator('.nml-submit-btn').click();

    // 6. Verify thank you confirmation message
    const confirmMessage = page.locator('.nml-message-row.assistant .nml-message-content').last();
    await expect(confirmMessage).toContainText('Thank you, Sarah Jenkins!', { timeout: 10000 });
  });
});
