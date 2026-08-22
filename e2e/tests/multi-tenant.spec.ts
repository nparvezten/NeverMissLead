import { test, expect } from '@playwright/test';

test.describe('NeverMissLead Multi-Tenant Verticals & Routing E2E', () => {
  test('Vertical 1: Tutoring demo renders Bright Minds Coaching branding', async ({ page }) => {
    await page.goto('http://localhost:4200/demo/tutoring');

    await expect(page.locator('.brand-name')).toContainText('Bright Minds Coaching');
    await expect(page.locator('.hero-headline')).toContainText('Master Math, Physics & CS');
    await expect(page.locator('.hero-tag')).toContainText('Grades 8–12 STEM Excellence');
  });

  test('Vertical 2: Dental demo renders Bright Smile Dental Clinic branding', async ({ page }) => {
    await page.goto('http://localhost:4200/demo/dental');

    await expect(page.locator('.brand-name')).toContainText('Bright Smile Dental Clinic');
    await expect(page.locator('.hero-headline')).toContainText('Advanced Dental Care');
    await expect(page.locator('.hero-tag')).toContainText('Dentistry');
  });

  test('Vertical 3: Realty demo renders Skyline Realty Partners branding', async ({ page }) => {
    await page.goto('http://localhost:4200/demo/realty');

    await expect(page.locator('.brand-name')).toContainText('Skyline Realty Partners');
    await expect(page.locator('.hero-headline')).toContainText('Find Your Dream Home');
    await expect(page.locator('.hero-tag')).toContainText('Brokerage');
  });

  test('Vertical Switcher: Navbar links seamlessly toggle between verticals', async ({ page }) => {
    await page.goto('http://localhost:4200');

    // Click Dental pill
    await page.locator('a.vertical-pill', { hasText: 'Dental Clinic' }).click();
    await expect(page).toHaveURL(/.*demo\/dental/);
    await expect(page.locator('.brand-name')).toContainText('Bright Smile Dental Clinic');

    // Click Realty pill
    await page.locator('a.vertical-pill', { hasText: 'Realty Partners' }).click();
    await expect(page).toHaveURL(/.*demo\/realty/);
    await expect(page.locator('.brand-name')).toContainText('Skyline Realty Partners');

    // Click Tutoring pill
    await page.locator('a.vertical-pill', { hasText: 'Tutoring' }).click();
    await expect(page).toHaveURL(/.*demo\/tutoring/);
    await expect(page.locator('.brand-name')).toContainText('Bright Minds Coaching');
  });

  test('Multi-Tenant Login: Logging in as Dental Owner shows Dental Clinic in dashboard', async ({ page }) => {
    await page.goto('http://localhost:4200/login');

    // Click Dental quick fill
    await page.locator('#demo-creds-dental').click();
    await page.locator('#login-submit-btn').click();

    await expect(page).toHaveURL(/.*dashboard/);
    await expect(page.locator('.business-badge')).toContainText('Bright Smile Dental Clinic', { timeout: 10000 });
  });

  test('Multi-Tenant Login: Logging in as Realty Owner shows Skyline Realty in dashboard', async ({ page }) => {
    await page.goto('http://localhost:4200/login');

    // Click Realty quick fill
    await page.locator('#demo-creds-realty').click();
    await page.locator('#login-submit-btn').click();

    await expect(page).toHaveURL(/.*dashboard/);
    await expect(page.locator('.business-badge')).toContainText('Skyline Realty Partners', { timeout: 10000 });
  });
});
