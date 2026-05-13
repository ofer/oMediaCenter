import { test, expect } from '@playwright/test';

const JUNK_PATTERNS = [
  /\bxvid\b/i, /\bx264\b/i, /\bdvdscr\b/i, /\bdvdrip\b/i,
  /\bbluray\b/i, /\b720p\b/i, /\b1080p\b/i, /\bYIFY\b/i,
  /\bETRG\b/i, /\bRARBG\b/i, /\baXXo\b/i,
  /\.avi$/i, /\.mkv$/i, /\.mp4$/i,
  /\[\s*www/i, /\[Eng\]/i, /\[1080p\]/i,
];

test.describe('media list - names display correctly', () => {

  test('page loads with media items', async ({ page }) => {
    const response = await page.goto('/#/medialist');
    expect(response?.status()).toBe(200);
    await page.waitForSelector('.omc-list-item', { timeout: 15000 });
    const items = page.locator('.omc-list-item');
    expect(await items.count()).toBeGreaterThan(0);

    // No JS console errors
    const errors: string[] = [];
    page.on('console', msg => {
      if (msg.type() === 'error') errors.push(msg.text());
    });
    // Give a moment for any deferred errors
    await page.waitForTimeout(1000);
    expect(errors).toEqual([]);
  });

  test('no junk tokens in displayed movie names', async ({ page }) => {
    await page.goto('/#/medialist');
    await page.waitForSelector('.omc-list-item', { timeout: 15000 });

    const buttons = page.locator('.omc-list-item button');
    const count = await buttons.count();

    for (let i = 0; i < count; i++) {
      const text = await buttons.nth(i).textContent() ?? '';
      for (const pattern of JUNK_PATTERNS) {
        expect(text, `"${text}" contains junk pattern ${pattern}`).not.toMatch(pattern);
      }
    }
  });

  test('"Happy Gilmore" appears correctly', async ({ page }) => {
    await page.goto('/#/medialist');
    await page.waitForSelector('.omc-list-item', { timeout: 15000 });
    const content = await page.locator('.omc-list-item').allTextContents();
    const joined = content.join(' ');
    expect(joined).toContain('Happy Gilmore');
    expect(joined).not.toContain('[ www UsaBit com ]');
  });

  test('"12 Angry Men" or "12angrymen" appears', async ({ page }) => {
    await page.goto('/#/medialist');
    await page.waitForSelector('.omc-list-item', { timeout: 15000 });
    const content = await page.locator('.omc-list-item').allTextContents();
    const joined = content.join(' ');
    const has12Angry = joined.includes('12 Angry Men') || joined.toLowerCase().includes('12angrymen');
    expect(has12Angry, 'Expected "12 Angry Men" or "12angrymen"').toBe(true);
    expect(joined).not.toContain('sprinter-12angrymen-cd1');
  });

  test('"The Middle" appears with season/episode info', async ({ page }) => {
    await page.goto('/#/medialist');
    await page.waitForSelector('.omc-list-item', { timeout: 15000 });
    const content = await page.locator('.omc-list-item').allTextContents();
    const joined = content.join(' ');
    expect(joined).toContain('The Middle');
    expect(joined).not.toContain('The Middle S03E01+E02');
  });

  test('"argo" appears correctly', async ({ page }) => {
    await page.goto('/#/medialist');
    await page.waitForSelector('.omc-list-item', { timeout: 15000 });
    const content = await page.locator('.omc-list-item').allTextContents();
    const joined = content.join(' ').toLowerCase();
    expect(joined).toContain('argo');
    expect(joined).not.toContain('wtf-argo');
  });

  test('no entry named exactly "#"', async ({ page }) => {
    await page.goto('/#/medialist');
    await page.waitForSelector('.omc-list-item', { timeout: 15000 });
    const buttons = page.locator('.omc-list-item button');
    const count = await buttons.count();
    for (let i = 0; i < count; i++) {
      const text = (await buttons.nth(i).textContent() ?? '').trim();
      expect(text, 'Found an entry named exactly "#"').not.toBe('#');
    }
  });
});

// OpenAI-resolved name tests -- only run when OPENAI_API_KEY is set
test.describe('OpenAI-resolved names', () => {
  test.skip(!process.env.OPENAI_API_KEY, 'Skipped: OPENAI_API_KEY not set');

  test('"Sherlock Holmes" appears (not "Sherlock Holms")', async ({ page }) => {
    await page.goto('/#/medialist');
    await page.waitForSelector('.omc-list-item', { timeout: 15000 });
    const content = await page.locator('.omc-list-item').allTextContents();
    const joined = content.join(' ');
    expect(joined).toContain('Sherlock Holmes');
    expect(joined).not.toContain('Sherlock Holms');
  });

  test('"A Few Good Men" appears (not "A Few Good Me")', async ({ page }) => {
    await page.goto('/#/medialist');
    await page.waitForSelector('.omc-list-item', { timeout: 15000 });
    const content = await page.locator('.omc-list-item').allTextContents();
    const joined = content.join(' ');
    expect(joined).toContain('A Few Good Men');
    expect(joined).not.toContain('A Few Good Me');
  });

  test('"Zootopia 2" appears (not "Z00topia 2")', async ({ page }) => {
    await page.goto('/#/medialist');
    await page.waitForSelector('.omc-list-item', { timeout: 15000 });
    const content = await page.locator('.omc-list-item').allTextContents();
    const joined = content.join(' ');
    expect(joined).toContain('Zootopia 2');
    expect(joined).not.toContain('Z00topia');
  });
});
