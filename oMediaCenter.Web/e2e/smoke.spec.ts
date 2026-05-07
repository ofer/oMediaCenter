import { test, expect } from '@playwright/test';

const MEDIA_HASH = 'FCB7748FC27E976C83A4A1D270DC5DAF';

test.describe('smoke tests', () => {
  test('server is reachable', async ({ page }) => {
    const response = await page.goto('/');
    expect(response?.status()).toBe(200);
  });

  test('navigation links work', async ({ page }) => {
    await page.goto('/');
    await expect(page.locator('.top-bar')).toBeVisible();
    await expect(page.locator('a[routerlink="/medialist"]')).toBeVisible();
    await expect(page.locator('a[routerlink="/recentlyplayed"]')).toBeVisible();
  });

  test('media player page loads with video and info panel', async ({ page }) => {
    await page.goto(`/#/media/${MEDIA_HASH}`);
    await page.waitForSelector('vg-player', { timeout: 10000 });

    // Video element is present
    await expect(page.locator('video#singleVideo')).toBeVisible();

    // Info panel is present with movie title
    await expect(page.locator('.media-player-info-panel')).toBeVisible();
    await expect(page.locator('.media-player-info-panel')).toContainText('Tangled');
  });
});
