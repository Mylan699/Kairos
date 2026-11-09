// Generates the PNG icons and the Open Graph image from the files of the brand folder.
// Run with: npm run generate:brand

import { readFileSync, writeFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { chromium } from '@playwright/test';

const fromHere = (path) => fileURLToPath(new URL(path, import.meta.url));

const favicon = readFileSync(fromHere('../../brand/favicon.svg'), 'utf8');
const mark = readFileSync(fromHere('../../brand/kairos-mark-dark.svg'), 'utf8');

const font = (path) => readFileSync(fromHere(`../node_modules/${path}`)).toString('base64');
const geist = font('@fontsource-variable/geist/files/geist-latin-wght-normal.woff2');
const geistMono = font('@fontsource-variable/geist-mono/files/geist-mono-latin-wght-normal.woff2');

const ICONS = [
  { file: 'favicon-32.png', size: 32, opaque: false },
  { file: 'icon-192.png', size: 192, opaque: false },
  { file: 'icon-512.png', size: 512, opaque: false },
  // iOS draws transparent corners in black, so its icon needs a solid background.
  { file: 'apple-touch-icon.png', size: 180, opaque: true },
];

const openGraphPage = `<!doctype html>
<style>
  @font-face { font-family: Geist; src: url(data:font/woff2;base64,${geist}); font-weight: 100 900; }
  @font-face { font-family: 'Geist Mono'; src: url(data:font/woff2;base64,${geistMono}); font-weight: 100 900; }
  * { margin: 0; box-sizing: border-box; }
  body { width: 1200px; height: 630px; background: #0b0e14; color: #e8ecf4; font-family: Geist; overflow: hidden; position: relative; }
  .curve { position: absolute; right: -40px; top: 40px; opacity: .9; overflow: visible; }
  .content { position: absolute; left: 88px; top: 0; bottom: 0; display: flex; flex-direction: column; justify-content: center; }
  /* The symbol has padding around its stem: this lines the stem up with the text below. */
  .logo { display: flex; align-items: center; gap: 28px; margin-left: -25px; }
  .logo svg { width: 132px; height: 132px; }
  .wordmark { font-size: 124px; font-weight: 600; letter-spacing: -0.04em; line-height: 1; }
  .signature { margin-top: 36px; font-family: 'Geist Mono'; font-size: 24px; letter-spacing: 0.32em; color: #8a93a6; }
  .line { margin-top: 56px; font-size: 38px; line-height: 1.25; color: #e8ecf4; }
</style>
<svg class="curve" width="620" height="550" viewBox="15 10 26 22" fill="none" aria-hidden="true">
  <path d="M15 30 L21.5 23.5 L26.5 26.5 L36 13" stroke="#151a24" stroke-width="5" stroke-linecap="round" stroke-linejoin="round" />
</svg>
<div class="content">
  <div class="logo">${mark}<span class="wordmark">Kairos</span></div>
  <p class="signature">CROSS-ASSET TRADING DESK</p>
  <p class="line">Actions, change et taux dans un seul P&amp;L en euros.</p>
</div>`;

const browser = await chromium.launch();

for (const { file, size, opaque } of ICONS) {
  const page = await browser.newPage({ viewport: { width: size, height: size } });
  await page.setContent(
    `<!doctype html><body style="margin:0;background:${opaque ? '#151a24' : 'transparent'}">${favicon.replace('<svg ', `<svg width="${size}" height="${size}" style="display:block" `)}</body>`,
  );
  await page.screenshot({ path: fromHere(`../public/${file}`), omitBackground: !opaque });
  await page.close();
}

const page = await browser.newPage({ viewport: { width: 1200, height: 630 } });
await page.setContent(openGraphPage);
await page.evaluate(() => document.fonts.ready);
await page.screenshot({ path: fromHere('../public/og-image.png') });

await browser.close();

writeFileSync(
  fromHere('../public/site.webmanifest'),
  `${JSON.stringify(
    {
      name: 'Kairos, poste de trading cross-asset',
      short_name: 'Kairos',
      description: 'Actions, change et taux dans un seul P&L en euros, avec rejeu de journées de marché historiques.',
      start_url: '/',
      display: 'standalone',
      background_color: '#0b0e14',
      theme_color: '#0b0e14',
      icons: [
        { src: '/icon-192.png', sizes: '192x192', type: 'image/png' },
        { src: '/icon-512.png', sizes: '512x512', type: 'image/png' },
      ],
    },
    null,
    2,
  )}\n`,
);
