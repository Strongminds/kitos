// Requires Playwright and Chrome. Stops before credential entry; TLS validation stays enabled.
const { chromium } = require('playwright');

(async () => {
    const browser = await chromium.launch({ channel: 'chrome', headless: true });
    let stage = 'inspector';
    try {
        const context = await browser.newContext({ ignoreHTTPSErrors: false });
        const page = await context.newPage();
        page.on('requestfailed', request => {
            console.error(`Request failed: ${new URL(request.url()).origin} ${request.failure()?.errorText}`);
        });
        const inspector = await page.goto('https://localhost:7445', { waitUntil: 'domcontentloaded', timeout: 15000 });
        if (inspector.status() !== 200) throw new Error(`Inspector returned HTTP ${inspector.status()}`);
        await page.getByRole('heading', { name: 'KITOS local MCP inspector', exact: true }).waitFor();
        stage = 'broker redirect';
        await Promise.all([
            page.waitForURL(url => url.origin === 'https://localhost:7443' && url.pathname === '/connect/authorize', { waitUntil: 'domcontentloaded', timeout: 15000 }),
            page.getByRole('button', { name: 'Connect through KITOS broker', exact: true }).click()
        ]);
        await page.getByRole('heading', { name: 'Connect KITOS read access', exact: true }).waitFor();
        const jwt = page.locator('input[name="jwt"][type="password"]');
        await jwt.waitFor({ state: 'visible' });
        if (await jwt.inputValue() !== '') throw new Error('Token field must be empty');
        await page.getByRole('checkbox', { name: 'Allow read-only access', exact: true }).waitFor();
        console.log('PASS: inspector button reached the broker token-entry form at https://localhost:7443/connect/authorize. No credentials entered.');
        stage = 'broker reload';
        await page.reload({ waitUntil: 'domcontentloaded', timeout: 15000 });
        await jwt.waitFor({ state: 'visible' });
        if (await jwt.inputValue() !== '') throw new Error('Reloaded token field must be empty');
        console.log('PASS: token-entry page reloads before authentication.');
    } catch (error) {
        // Do not print OAuth URLs, cookies, hidden fields or page bodies.
        console.error(`FAIL at ${stage}: ${error.message.split('\n')[0].split(' at https://')[0]}`);
        process.exitCode = 1;
    } finally {
        await browser.close();
    }
})().catch(() => {
    console.error('FAIL: unable to launch Playwright/Chrome. Check installed prerequisites.');
    process.exitCode = 1;
});
