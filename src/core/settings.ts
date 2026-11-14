// Player settings, quality presets and the two languages.
import ar from '../../content/i18n/ar.json';
import en from '../../content/i18n/en.json';
import type { DriveMode } from '../physics/vehicle';

export type Lang = 'en' | 'ar';
export type Quality = 'low' | 'medium' | 'high' | 'ultra';
export interface Preset { pixelRatio: number; shadows: number; lod: number; anisotropy: number; environment: boolean; /** Anti-aliasing samples for the finishing effects; 0 turns them off. */ postfx: number; /** Milliseconds per frame spent refreshing hill shadows. */ shadeBudget: number; /** Photographic ground detail close to the camera. */ detail: boolean }

export const PRESETS: Record<Quality, Preset> = {
  low: { pixelRatio: 0.65, shadows: 0, lod: 0.42, anisotropy: 2, environment: false, postfx: 0, shadeBudget: 0.8, detail: false },
  medium: { pixelRatio: 1, shadows: 1024, lod: 0.75, anisotropy: 4, environment: true, postfx: 0, shadeBudget: 1.5, detail: true },
  high: { pixelRatio: 1.25, shadows: 2048, lod: 1, anisotropy: 8, environment: true, postfx: 4, shadeBudget: 2.5, detail: true },
  ultra: { pixelRatio: 2, shadows: 4096, lod: 1.6, anisotropy: 16, environment: true, postfx: 4, shadeBudget: 4, detail: true },
};
export const QUALITIES: Quality[] = ['low', 'medium', 'high', 'ultra'];

export interface Settings { lang: Lang; quality: Quality | 'auto'; mode: DriveMode; labels: boolean; volume: number }
const KEY = 'rihla.settings.v1';

export function loadSettings(): Settings {
  const def: Settings = { lang: (navigator.language || '').startsWith('ar') ? 'ar' : 'en', quality: 'auto', mode: 'family', labels: true, volume: 0.7 };
  try { return { ...def, ...JSON.parse(localStorage.getItem(KEY) || '{}') }; } catch { return def; }
}
export function saveSettings(s: Settings) { try { localStorage.setItem(KEY, JSON.stringify(s)); } catch { /* private window: settings last for this visit only */ } }

/** A first guess at a quality level; the game steps down by itself if the frame rate is poor. */
export function guessQuality(gpu = ''): Quality {
  const nav = navigator as Navigator & { deviceMemory?: number };
  // Older Intel graphics built into laptops ("HD Graphics", "UHD Graphics", "Iris") start on Low.
  if (/intel/i.test(gpu) && !/arc/i.test(gpu)) return 'low';
  const mobile = matchMedia('(pointer: coarse)').matches;
  if (mobile) return (nav.deviceMemory ?? 4) >= 6 ? 'medium' : 'low';
  return (nav.hardwareConcurrency ?? 4) >= 8 ? 'high' : 'medium';
}

/** What the player has done so far. Kept in the browser; proper save slots come in Phase 2. */
export interface Progress { introSeen: boolean; pages: string[] }
const SAVE_KEY = 'rihla.progress.v1';
export function loadProgress(): Progress { try { return { introSeen: false, pages: [], ...JSON.parse(localStorage.getItem(SAVE_KEY) || '{}') }; } catch { return { introSeen: false, pages: [] }; } }
export function saveProgress(p: Progress) { try { localStorage.setItem(SAVE_KEY, JSON.stringify(p)); } catch { /* not saved in a private window */ } }

const STRINGS: Record<Lang, Record<string, string>> = { en, ar };
let current: Lang = 'en';
export function setLang(l: Lang) { current = l; document.documentElement.lang = l; document.documentElement.dir = l === 'ar' ? 'rtl' : 'ltr'; }
export const lang = () => current;
export const t = (key: string) => STRINGS[current][key] ?? STRINGS.en[key] ?? key;
/** Western digits in English, Eastern Arabic digits in Arabic. */
export const num = (v: number, digits = 0) => v.toLocaleString(current === 'ar' ? 'ar-EG' : 'en-US', { maximumFractionDigits: digits, minimumFractionDigits: digits, useGrouping: false });
