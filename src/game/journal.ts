// The Rihla journal: shows a found page, and the story card at the start of the game.
import journal from '../../content/journal.json';
import texts from '../../content/religious_texts.json';
import { lang, t } from '../core/settings';

export interface Page { id: string; place: string; title: L; ancestor: L; jiddo: { en: string[]; ar: string[] }; verseIntro: L; verse: string; facts: L[] }
type L = { en: string; ar: string };
export const PAGES = journal.pages as Page[];

const el = (tag: string, cls: string | null, text?: string) => { const e = document.createElement(tag); if (cls) e.className = cls; if (text !== undefined) e.textContent = text; return e; };

function show(build: (body: HTMLElement) => void, closeLabel: string, onClose: () => void) {
  const root = document.getElementById('page')!, body = document.getElementById('page-body')!, btn = document.getElementById('page-close')!;
  body.replaceChildren(); build(body);
  btn.textContent = closeLabel;
  btn.onclick = () => { root.hidden = true; onClose(); };
  root.hidden = false; root.scrollTop = 0; (root.firstElementChild as HTMLElement).scrollTop = 0;
  btn.focus();
}
export const pageOpen = () => !document.getElementById('page')!.hidden;

export function showPage(page: Page, onClose: () => void) {
  const l = lang();
  show((b) => {
    b.append(el('h1', null, page.title[l]), el('h2', null, t('ancestorSays')), el('p', 'ancestor', page.ancestor[l]), el('h2', null, t('jiddoSays')));
    for (const para of page.jiddo[l]) b.append(el('p', null, para));
    const v = (texts.texts as Record<string, { reference: string; surah: L; ar: string; en: string }>)[page.verse];
    if (v) {
      b.append(el('p', null, page.verseIntro[l]));
      const box = el('div', 'verse');
      box.append(el('div', 'ar', v.ar), el('div', 'ref', `${v.surah[l]} · ${v.reference}`));
      if (l !== 'ar') box.append(el('p', null, `${t('meaning')}: ${v.en}`));
      b.append(box);
    }
    const facts = el('div', 'facts');
    for (const f of page.facts) facts.append(el('span', null, f[l]));
    b.append(facts);
  }, t('close'), onClose);
}

export function showIntro(onClose: () => void) {
  show((b) => { b.append(el('h1', null, t('intro.title')), el('p', null, t('intro.1')), el('p', null, t('intro.2')), el('p', 'ancestor', t('intro.3'))); }, t('begin'), onClose);
}
