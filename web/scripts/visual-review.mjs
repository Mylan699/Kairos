// Captures every page at desktop and phone width, in both themes, and checks what can be measured.
// It needs the API and a served front. Run with: npm run visual:review
//
// The captures go to web/visual-review and the measurements to web/visual-review/report.json.

import { mkdirSync, writeFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { chromium } from '@playwright/test';

const BASE_URL = process.env.KAIROS_WEB_URL ?? 'http://localhost:4173';
const OUTPUT = fileURLToPath(new URL('../visual-review/', import.meta.url));

// KAIROS_PAGES=markets,risk limits a run to some pages, to check a correction quickly.
const ONLY = process.env.KAIROS_PAGES?.split(',');

const PAGES = [
  ['landing', '/'],
  ['dashboard', '/app'],
  ['blotter', '/app/blotter'],
  ['positions', '/app/positions'],
  ['markets', '/app/markets'],
  ['risk', '/app/risk'],
  ['replay', '/app/replay'],
].filter(([name]) => !ONLY || ONLY.includes(name));

const VIEWPORTS = [
  ['1440', { width: 1440, height: 900 }],
  ['390', { width: 390, height: 844 }],
];

const THEMES = ['dark', 'light'];

// The brand values, as the browser reports them.
const CANVAS = { dark: 'rgb(11, 14, 20)', light: 'rgb(245, 246, 249)' };

const SETTLE_MILLISECONDS = 2_500;
const FIXED_DATE = '2020-03-16';
const FIXED_MINUTE = 900;

const post = (path, body) =>
  fetch(`${BASE_URL}${path}`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body ?? {}),
  });

mkdirSync(OUTPUT, { recursive: true });

// Every capture shows the same moment of the same day, so that two reviews can be compared.
await post('/api/replay/load', { date: FIXED_DATE, startMinute: FIXED_MINUTE });
await post('/api/replay/pause');

const browser = await chromium.launch();
const report = [];

for (const [pageName, path] of PAGES) {
  for (const [widthName, viewport] of VIEWPORTS) {
    for (const theme of THEMES) {
      const context = await browser.newContext({ viewport, locale: 'fr-FR' });
      await context.addInitScript((value) => localStorage.setItem('kairos-theme', value), theme);

      const page = await context.newPage();
      const consoleErrors = [];
      page.on('console', (message) => message.type() === 'error' && consoleErrors.push(message.text()));
      page.on('pageerror', (error) => consoleErrors.push(error.message));

      await page.goto(`${BASE_URL}${path}`, { waitUntil: 'domcontentloaded', timeout: 60_000 });
      await page.waitForFunction(() => !document.querySelector('[aria-busy="true"]'), undefined, { timeout: 20_000 });
      await page.evaluate(() => document.fonts.ready);
      await page.waitForTimeout(SETTLE_MILLISECONDS);

      const measured = await page.evaluate(() => ({
        horizontalOverflow: document.documentElement.scrollWidth - window.innerWidth,
        background: getComputedStyle(document.documentElement).backgroundColor,
        fontFamily: getComputedStyle(document.body).fontFamily,
        hasMonoFigures: Boolean(document.querySelector('.num')) && getComputedStyle(document.querySelector('.num')).fontFamily.includes('Geist Mono'),
        geistLoaded: document.fonts.check('14px "Geist Variable"'),
        scrollingTables: [...document.querySelectorAll('.overflow-x-auto')].filter((zone) => zone.scrollWidth > zone.clientWidth).length,
      }));

      const file = `${pageName}-${widthName}-${theme}.png`;
      await page.screenshot({ path: `${OUTPUT}${file}`, fullPage: true });

      const problems = [
        ...(measured.horizontalOverflow > 0 ? [`débordement horizontal de ${measured.horizontalOverflow} px`] : []),
        ...(measured.scrollingTables > 0 ? [`${measured.scrollingTables} zone(s) à défilement horizontal`] : []),
        ...(measured.background === CANVAS[theme] ? [] : [`fond ${measured.background}`]),
        ...(measured.fontFamily.includes('Geist') && measured.geistLoaded ? [] : ['police Geist absente']),
        ...(measured.hasMonoFigures || pageName === 'landing' ? [] : ['chiffres sans Geist Mono']),
        ...consoleErrors.map((error) => `console : ${error.slice(0, 160)}`),
      ];

      report.push({ file, problems });
      console.log(`${problems.length === 0 ? 'ok ' : 'KO '} ${file}${problems.length === 0 ? '' : ` : ${problems.join(' | ')}`}`);

      await context.close();
    }
  }
}

await browser.close();
await post('/api/replay/play');

writeFileSync(`${OUTPUT}report.json`, `${JSON.stringify(report, null, 2)}\n`);

const failures = report.filter((entry) => entry.problems.length > 0).length;
console.log(`\n${report.length} captures, ${failures} avec des anomalies mesurées.`);
process.exitCode = failures === 0 ? 0 : 1;
