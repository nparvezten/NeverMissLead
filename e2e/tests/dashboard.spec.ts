import { test, expect } from '@playwright/test';

test.describe('NeverMissLead Owner Dashboard E2E', () => {
  test('Owner Login & Dashboard Inspection Flow', async ({ page }) => {
    // 1. Navigate to login page
    await page.goto('http://localhost:4200/login');

    // 2. Verify login card is displayed
    const loginCard = page.locator('.login-card');
    await expect(loginCard).toBeVisible();

    // 3. Click demo credentials helper or fill inputs
    const demoBtn = page.locator('#demo-creds-tutoring');
    await expect(demoBtn).toBeVisible();
    await demoBtn.click();

    // 4. Submit login form
    await page.locator('#login-submit-btn').click();

    // 5. Verify redirection to /dashboard
    await expect(page).toHaveURL(/.*dashboard/);

    // 6. Verify navbar and business name badge
    const businessBadge = page.locator('.business-badge');
    await expect(businessBadge).toBeVisible({ timeout: 10000 });
    await expect(businessBadge).toContainText('Bright Minds');

    // 7. Verify stats metric cards exist
    await expect(page.locator('#stat-total-leads')).toBeVisible();
    await expect(page.locator('#stat-high-intent')).toBeVisible();
    await expect(page.locator('#stat-unanswered')).toBeVisible();
    await expect(page.locator('#stat-pending-followups')).toBeVisible();

    // 8. Tab 1: Leads View — Verify leads table is rendered
    await page.locator('#tab-leads').click();
    await expect(page.locator('.tab-content')).toBeVisible();

    // 9. Tab 2: Conversations & Citations Viewer
    await page.locator('#tab-conversations').click();
    const convSidebar = page.locator('.conv-sidebar');
    await expect(convSidebar).toBeVisible();

    // If there are conversations, clicking one loads the thread panel
    const firstConv = page.locator('.conv-card').first();
    if (await firstConv.isVisible()) {
      await firstConv.click();
      await expect(page.locator('.conv-thread-panel')).toBeVisible();
    }

    // 10. Tab 3: Unanswered Questions (FAQ Knowledge Gaps)
    await page.locator('#tab-unanswered').click();
    await expect(page.locator('.tab-content h2')).toContainText('Unanswered Questions');

    // 11. Tab 4: Follow-Up Automation
    await page.locator('#tab-followups').click();
    await expect(page.locator('.tab-content h2')).toContainText('Automated Follow-Up Tasks');

    // 12. Sign Out
    await page.locator('#logout-btn').click();
    await expect(page).toHaveURL(/.*login/);
  });
});
