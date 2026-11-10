#!/usr/bin/env node
// Turns the story notes (US-knowledge/*.md) into readable HTML pages, one .html next to each .md.
// Usage, from the repository root:   node US-knowledge/build-html.js
// No packages needed (Node only). It understands the small Markdown subset the notes use:
// headings, paragraphs, bold, `code`, links, bullet and numbered lists, tables, fenced code blocks.
// The .md file stays the source. Never edit the .html by hand: run this script again.

'use strict';
const fs = require('fs');
const path = require('path');

const dir = __dirname;
const SKIP = new Set(['_template.md']);

const esc = (s) => s.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
const slug = (s) => s.toLowerCase().replace(/<[^>]+>/g, '').replace(/[^a-z0-9]+/g, '-').replace(/^-+|-+$/g, '');

function inline(text) {
  // Code spans are lifted out first so nothing inside them is formatted, and bold can wrap them.
  const codes = [];
  let t = text.replace(/`([^`]+)`/g, (m, c) => {
    codes.push(`<code>${esc(c)}</code>`);
    return `\u0000${codes.length - 1}\u0000`;
  });
  t = esc(t);
  t = t.replace(/\*\*([^*]+)\*\*/g, '<strong>$1</strong>');
  t = t.replace(/\[([^\]]+)\]\(([^)\s]+)\)/g, (m, label, href) => {
    const external = /^https?:/.test(href);
    const target = href.replace(/\.md$/, '.html');
    return `<a href="${target}"${external ? ' target="_blank" rel="noopener"' : ''}>${label}</a>`;
  });
  return t.replace(/\u0000(\d+)\u0000/g, (m, n) => codes[Number(n)]);
}

function statusClass(cellText) {
  const t = cellText.replace(/<[^>]+>/g, '').trim();
  if (/^(Yes|Matches|Done)\b/.test(t)) return 'ok';
  if (/^(Differs|Partly|Not in design|Written)\b/.test(t)) return 'warn';
  if (/^(Not found|Not built|No\b)/.test(t)) return 'bad';
  return '';
}

function splitRow(line) {
  return line.trim().replace(/^\|/, '').replace(/\|$/, '').split('|').map((c) => c.trim());
}

function render(md) {
  const lines = md.replace(/\r\n/g, '\n').split('\n');
  const out = [];
  const headings = [];
  let title = '';
  let meta = [];
  let i = 0;

  while (i < lines.length) {
    const line = lines[i];

    if (/^```/.test(line)) {
      const buf = [];
      i++;
      while (i < lines.length && !/^```/.test(lines[i])) buf.push(lines[i++]);
      i++;
      out.push(`<div class="code"><pre><code>${esc(buf.join('\n'))}</code></pre></div>`);
      continue;
    }

    const h = /^(#{1,3})\s+(.*)$/.exec(line);
    if (h) {
      const level = h[1].length;
      const text = h[2].trim();
      if (level === 1 && !title) {
        title = text;
        i++;
        // The bold "Label: value" lines right after the title become the header facts.
        while (i < lines.length && lines[i].trim() === '') i++;
        while (i < lines.length && /^\*\*[^*]+:\*\*/.test(lines[i])) {
          const m = /^\*\*([^*]+):\*\*\s*(.*)$/.exec(lines[i]);
          meta.push([m[1], m[2]]);
          i++;
        }
        continue;
      }
      const id = slug(text);
      if (level === 2) headings.push([id, text]);
      out.push(`<h${level} id="${id}">${inline(text)}</h${level}>`);
      i++;
      continue;
    }

    if (/^\|/.test(line) && i + 1 < lines.length && /^\|[\s:|-]+\|?\s*$/.test(lines[i + 1])) {
      const head = splitRow(line);
      i += 2;
      const rows = [];
      while (i < lines.length && /^\|/.test(lines[i])) rows.push(splitRow(lines[i++]));
      const th = head.map((c) => `<th>${inline(c)}</th>`).join('');
      const body = rows
        .map((r) => {
          const tds = r
            .map((c) => {
              const cls = statusClass(c);
              return `<td${cls ? ` class="st ${cls}"` : ''}>${inline(c)}</td>`;
            })
            .join('');
          return `<tr>${tds}</tr>`;
        })
        .join('');
      out.push(`<div class="table"><table><thead><tr>${th}</tr></thead><tbody>${body}</tbody></table></div>`);
      continue;
    }

    if (/^\s*\d+\.\s+/.test(line) || /^\s*-\s+/.test(line)) {
      const ordered = /^\s*\d+\.\s+/.test(line);
      const items = [];
      const marker = ordered ? /^\s*\d+\.\s+(.*)$/ : /^\s*-\s+(.*)$/;
      while (i < lines.length && marker.test(lines[i])) {
        let text = marker.exec(lines[i])[1];
        i++;
        while (i < lines.length && /^\s{2,}\S/.test(lines[i]) && !marker.test(lines[i])) {
          text += ' ' + lines[i].trim();
          i++;
        }
        items.push(`<li>${inline(text)}</li>`);
      }
      out.push(`<${ordered ? 'ol' : 'ul'}>${items.join('')}</${ordered ? 'ol' : 'ul'}>`);
      continue;
    }

    if (line.trim() === '') {
      i++;
      continue;
    }

    // Paragraph: every source line stays on its own line.
    const buf = [];
    while (
      i < lines.length &&
      lines[i].trim() !== '' &&
      !/^(#{1,3}\s|```|\|)/.test(lines[i]) &&
      !/^\s*\d+\.\s+/.test(lines[i]) &&
      !/^\s*-\s+/.test(lines[i])
    ) {
      buf.push(inline(lines[i].trim()));
      i++;
    }
    out.push(`<p>${buf.join('<br>')}</p>`);
  }

  return { title, meta, headings, body: out.join('\n') };
}

const CSS = `
:root {
  --font-body: "Inter", system-ui, -apple-system, "Segoe UI", Roboto, sans-serif;
  --font-head: "Space Grotesk", "Inter", system-ui, -apple-system, "Segoe UI", sans-serif;
  --font-mono: "JetBrains Mono", ui-monospace, "Cascadia Mono", Consolas, "DejaVu Sans Mono", "Liberation Mono", monospace;
  --bg: #f6f8fa; --surface: #ffffff; --fg: #14202b; --muted: #55677a; --line: #dbe2ea;
  --accent: #0a6c74; --accent-soft: #e0f1f2;
  --ok: #17703f; --ok-bg: #e2f4ea; --warn: #85560a; --warn-bg: #fdf0d3; --bad: #a3261d; --bad-bg: #fce6e3;
  --code-bg: #0f1b26; --code-fg: #d6e2ee; --inline-bg: #e9eef3;
}
@media (prefers-color-scheme: dark) {
  :root:not([data-theme="light"]) {
    --bg: #0d141b; --surface: #141d26; --fg: #e5edf4; --muted: #93a5b6; --line: #263443;
    --accent: #55c7c9; --accent-soft: #11302f;
    --ok: #62d394; --ok-bg: #11301f; --warn: #f0b94d; --warn-bg: #382b0c; --bad: #ff8c82; --bad-bg: #3a1613;
    --code-bg: #091016; --code-fg: #d6e2ee; --inline-bg: #1d2935; color-scheme: dark;
  }
}
:root[data-theme="dark"] {
  --bg: #0d141b; --surface: #141d26; --fg: #e5edf4; --muted: #93a5b6; --line: #263443;
  --accent: #55c7c9; --accent-soft: #11302f;
  --ok: #62d394; --ok-bg: #11301f; --warn: #f0b94d; --warn-bg: #382b0c; --bad: #ff8c82; --bad-bg: #3a1613;
  --code-bg: #091016; --code-fg: #d6e2ee; --inline-bg: #1d2935; color-scheme: dark;
}
* { box-sizing: border-box; }
body {
  background: var(--bg); color: var(--fg); font-family: var(--font-body); font-size: 16px; line-height: 1.6;
  padding-left: max(16px, env(safe-area-inset-left, 0px)); padding-right: max(16px, env(safe-area-inset-right, 0px));
  padding-block: 28px 64px;
}
.page { max-width: 920px; margin: 0 auto; display: flex; flex-direction: column; gap: 20px; min-width: 0; }
header { display: flex; flex-direction: column; gap: 14px; }
h1 { font-family: var(--font-head); font-size: clamp(1.6rem, 4.5vw, 2.3rem); line-height: 1.15; margin: 0; text-wrap: balance; letter-spacing: -0.01em; }
.facts { display: grid; grid-template-columns: max-content 1fr; gap: 6px 16px; margin: 0; padding: 14px 16px; background: var(--surface); border: 1px solid var(--line); border-radius: 8px; }
.facts dt { font-size: 0.74rem; text-transform: uppercase; letter-spacing: 0.08em; color: var(--muted); padding-top: 3px; }
.facts dd { margin: 0; min-width: 0; overflow-wrap: anywhere; }
nav.toc { display: flex; flex-wrap: wrap; gap: 8px; }
nav.toc a { font-size: 0.82rem; color: var(--accent); background: var(--accent-soft); padding: 4px 10px; border-radius: 999px; text-decoration: none; }
nav.toc a:hover, nav.toc a:focus-visible { text-decoration: underline; outline: none; }
main { display: flex; flex-direction: column; gap: 14px; min-width: 0; }
h2 { font-family: var(--font-head); font-size: 1.35rem; margin: 22px 0 0; padding-top: 18px; border-top: 1px solid var(--line); text-wrap: balance; scroll-margin-top: 12px; }
h3 { font-family: var(--font-head); font-size: 1.02rem; margin: 10px 0 0; color: var(--accent); }
p { margin: 0; max-width: 72ch; }
ul, ol { margin: 0; padding-left: 1.4rem; max-width: 76ch; display: flex; flex-direction: column; gap: 6px; }
a { color: var(--accent); }
code { font-family: var(--font-mono); font-size: 0.86em; background: var(--inline-bg); padding: 1px 6px; border-radius: 4px; overflow-wrap: anywhere; }
.code { overflow-x: auto; background: var(--code-bg); border-radius: 8px; border: 1px solid var(--line); }
.code pre { margin: 0; padding: 14px 16px; }
.code code { background: none; padding: 0; color: var(--code-fg); font-size: 0.8rem; line-height: 1.5; white-space: pre; overflow-wrap: normal; }
.table { overflow-x: auto; border: 1px solid var(--line); border-radius: 8px; background: var(--surface); }
table { border-collapse: collapse; width: 100%; font-size: 0.9rem; font-variant-numeric: tabular-nums; }
th, td { text-align: left; vertical-align: top; padding: 9px 12px; border-bottom: 1px solid var(--line); }
th { font-size: 0.74rem; text-transform: uppercase; letter-spacing: 0.07em; color: var(--muted); background: var(--bg); white-space: nowrap; }
tbody tr:last-child td { border-bottom: none; }
td:first-child { min-width: 9rem; }
td.st { font-weight: 600; }
td.ok { color: var(--ok); background: var(--ok-bg); }
td.warn { color: var(--warn); background: var(--warn-bg); }
td.bad { color: var(--bad); background: var(--bad-bg); }
footer { color: var(--muted); font-size: 0.8rem; border-top: 1px solid var(--line); padding-top: 14px; }
@media (max-width: 560px) { .facts { grid-template-columns: 1fr; gap: 2px; } .facts dt { padding-top: 8px; } }
@media (prefers-reduced-motion: reduce) { * { scroll-behavior: auto !important; } }
`;

function page(note, sourceName) {
  const facts = note.meta.length
    ? `<dl class="facts">${note.meta.map(([k, v]) => `<dt>${esc(k)}</dt><dd>${inline(v)}</dd>`).join('')}</dl>`
    : '';
  const toc = note.headings.length
    ? `<nav class="toc" aria-label="Sections">${note.headings
        .map(([id, text]) => `<a href="#${id}">${esc(text)}</a>`)
        .join('')}</nav>`
    : '';
  return `<!doctype html>
<html lang="en">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1, viewport-fit=cover">
<meta name="claude:viewport-fit-sides" content="cover">
<title>${esc(note.title)}</title>
<link rel="preconnect" href="https://fonts.googleapis.com">
<link rel="stylesheet" href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600&family=Space+Grotesk:wght@500;600&family=JetBrains+Mono:wght@400;500&display=swap">
<style>${CSS}</style>
</head>
<body>
<div class="page">
<header>
<h1>${inline(note.title)}</h1>
${facts}
${toc}
</header>
<main>
${note.body}
</main>
<footer>Generated from ${esc(sourceName)} by US-knowledge/build-html.js. Edit the .md file, then run the script again.</footer>
</div>
</body>
</html>
`;
}

const files = fs
  .readdirSync(dir)
  .filter((f) => f.endsWith('.md') && !SKIP.has(f))
  .sort();

let count = 0;
for (const file of files) {
  const md = fs.readFileSync(path.join(dir, file), 'utf8');
  const note = render(md);
  if (!note.title) note.title = file.replace(/\.md$/, '');
  const target = file === 'README.md' ? 'index.html' : file.replace(/\.md$/, '.html');
  fs.writeFileSync(path.join(dir, target), page(note, file), 'utf8');
  console.log(`${file} -> ${target}`);
  count++;
}
console.log(`Done: ${count} page(s). Open US-knowledge/index.html in a browser.`);
