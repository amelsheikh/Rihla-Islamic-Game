// Player settings, quality presets and the two languages.
import ar from '../../content/i18n/ar.json';
import en from '../../content/i18n/en.json';
import type { DriveMode } from '../physics/vehicle';

export type Lang = 'en' | 'ar';
export type Quality = 'low' | 'medium' | 'high' | 'ultra';
export interface Preset { pixelRatio: number; shadows: number; lod: number; anisotropy: number; environment: boolean; antialias: boolean }

export const PRESETS: Record<Quality, Preset> = {
  low: { pixelRatio: 0.7, shadows: 0, lod: 0.55, anisotropy: 2, environment: false, antialias: false },
  medium: { pixelRatio: 1, shadows: 1024, lod: 0.8, anisotropy: 4, environment: true, antialias: true },
  high: { pixelRatio: 1.5, shadows: 2048, lod: 1.15, anisotropy: 8, environment: true, antialias: true },
  ultra: { pixelRatio: 2, shadows: 4096, lod: 1.8, anisotropy: 16, environment: true, antialias: true },
};
export const QUALITIES: Quality[] = ['low', 'medium', 'high', 'ultra'];

export interface Settings { lang: Lang; quality: Quality | 'auto'; mode: DriveMode; labels: boolean }
const KEY = 'rihla.settings.v1';

export function loadSettings(): Settings {
  const def: Settings = { lang: (navigator.language || '').startsWith('ar') ? 'ar' : 'en', quality: 'auto', mode: 'family', labels: true };
  try { return { ...def, ...JSON.parse(localStorage.getItem(KEY) || '{}') }; } catch { return def; }
}
export function saveSettings(s: Settings) { try { localStorage.setItem(KEY, JSON.stringify(s)); } catch { /* private window: settings last for this visit only */ } }

/** A first guess at a quality level; the game steps down by itself if the frame rate is poor. */
export function guessQuality(): Quality {
  const nav = navigator as Navigator & { deviceMemory?: number };
  const mobile = matchMedia('(pointer: coarse)').matches;
  if (mobile) return (nav.deviceMemory ?? 4) >= 6 ? 'medium' : 'low';
  return (nav.hardwareConcurrency ?? 4) >= 8 ? 'high' : 'medium';
}

const STRINGS: Record<Lang, Record<string, string>> = { en, ar };
let current: Lang = 'en';
export function setLang(l: Lang) { current = l; document.documentElement.lang = l; document.documentElement.dir = l === 'ar' ? 'rtl' : 'ltr'; }
export const lang = () => current;
export const t = (key: string) => STRINGS[current][key] ?? STRINGS.en[key] ?? key;
/** Western digits in English, Eastern Arabic digits in Arabic. */
export const num = (v: number, digits = 0) => v.toLocaleString(current === 'ar' ? 'ar-EG' : 'en-US', { maximumFractionDigits: digits, minimumFractionDigits: digits, useGrouping: false });
