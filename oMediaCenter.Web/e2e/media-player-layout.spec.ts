import { test, expect } from '@playwright/test';

const MEDIA_HASH = 'FCB7748FC27E976C83A4A1D270DC5DAF';

test.use({ viewport: { width: 1280, height: 720 } });

test.describe('media player layout', () => {
  test('video does not overlap the top bar and is vertically centered below it', async ({ page }) => {
    await page.goto(`/#/media/${MEDIA_HASH}`);

    // Wait for Angular to render the media player
    await page.waitForSelector('vg-player', { timeout: 10000 });

    const topBar = page.locator('.top-bar');
    const video = page.locator('vg-player');

    await expect(topBar).toBeVisible();
    await expect(video).toBeVisible();

    const topBarBox = await topBar.boundingBox();
    const videoBox = await video.boundingBox();

    expect(topBarBox).not.toBeNull();
    expect(videoBox).not.toBeNull();

    // The video element's top edge must be at or below the top bar's bottom edge
    const topBarBottom = topBarBox!.y + topBarBox!.height;
    expect(videoBox!.y).toBeGreaterThanOrEqual(topBarBottom);

    // Verify the player content block is vertically centered in available space.
    // The flex container centers the entire content (video + info panel), so we
    // check that the content block's center is near the available space center.
    const contentBlock = page.locator('app-media-player > div');
    const contentBox = await contentBlock.boundingBox();
    expect(contentBox).not.toBeNull();

    const viewportSize = page.viewportSize()!;
    const availableHeight = viewportSize.height - topBarBottom;
    const contentCenterY = contentBox!.y + contentBox!.height / 2;
    const availableCenterY = topBarBottom + availableHeight / 2;

    // Allow 25% tolerance for centering (accounts for content height variability)
    const tolerance = availableHeight * 0.25;
    expect(Math.abs(contentCenterY - availableCenterY)).toBeLessThan(tolerance);
  });

  test('top bar has a z-index that keeps it above the video', async ({ page }) => {
    await page.goto(`/#/media/${MEDIA_HASH}`);
    await page.waitForSelector('vg-player', { timeout: 10000 });

    const topBarZIndex = await page.locator('.top-bar').evaluate(
      (el) => window.getComputedStyle(el).zIndex
    );

    // The top bar must have a numeric z-index to stay above absolutely positioned content
    expect(topBarZIndex).not.toBe('auto');
    expect(Number(topBarZIndex)).toBeGreaterThan(0);
  });
});
