// Browser regression for CSP redirect handling. The token POST is intercepted;
// no credentials or authorization request are sent to KITOS.
const { chromium } = require('playwright');

(async () => {
    const browser = await chromium.launch({ channel: 'chrome', headless: true });
    let stage = 'inspector';
    try {
        const context = await browser.newContext({ ignoreHTTPSErrors: false });
        let intercepted = false;
        await context.route('https://localhost:7443/connect/authorize**', async route => {
            if (route.request().method() !== 'POST') return route.continue();
            intercepted = true;
            await route.fulfill({ status: 302, headers: { location: 'https://localhost:7445/callback?error=synthetic-browser-check' } });
        });
        const page = await context.newPage();
        page.on('console', message => {
            if (message.text().includes('form-action')) console.error('Browser reported a form-action CSP violation.');
        });
        await page.goto('https://localhost:7445', { waitUntil: 'domcontentloaded' });
        stage = 'broker token-entry';
        await Promise.all([
            page.waitForURL('https://localhost:7443/connect/authorize**', { waitUntil: 'domcontentloaded', timeout: 15000 }),
            page.getByRole('button', { name: 'Connect through KITOS broker', exact: true }).click()
        ]);
        await page.locator('input[name="jwt"]').fill('synthetic-intercepted-token');
        await page.getByRole('checkbox', { name: 'Allow read-only access', exact: true }).check();
        stage = 'synthetic callback redirect';
        await Promise.all([
            page.waitForURL(url => url.origin === 'https://localhost:7445' && url.pathname === '/callback', { waitUntil: 'domcontentloaded', timeout: 5000 }),
            page.getByRole('button', { name: 'Connect', exact: true }).click()
        ]);
        if (!intercepted) throw new Error('Expected intercepted form POST');
        // The inspector rejects the synthetic error callback; reaching it proves CSP allowed the redirect.
        console.log('PASS: real broker page CSP permits form redirect to the registered callback. POST response simulated; synthetic error callback rejected by inspector; no real credential or token exchange.');
    } catch {
        console.error(`FAIL at ${stage}: broker form could not reach the intercepted registered callback. No real credentials used.`);
        process.exitCode = 1;
    } finally {
        await browser.close();
    }
})().catch(() => { console.error('FAIL: browser prerequisites unavailable.'); process.exitCode = 1; });
