import i18n from 'i18next'
import { initReactI18next } from 'react-i18next'
import en from './locales/en.json'
import hi from './locales/hi.json'
import kn from './locales/kn.json'
import ta from './locales/ta.json'
import te from './locales/te.json'

export const SUPPORTED_LANGUAGES = ['kn', 'en', 'te', 'ta', 'hi'] as const
export type Language = (typeof SUPPORTED_LANGUAGES)[number]

/** Teacher UI is Kannada first (spec §3.1). */
export const DEFAULT_LANGUAGE: Language = 'kn'

export const LANGUAGE_STORAGE_KEY = 'slate.language'

export function isLanguage(value: unknown): value is Language {
  return typeof value === 'string' && (SUPPORTED_LANGUAGES as readonly string[]).includes(value)
}

function storedLanguage(): Language {
  try {
    const value = localStorage.getItem(LANGUAGE_STORAGE_KEY)
    return isLanguage(value) ? value : DEFAULT_LANGUAGE
  } catch {
    return DEFAULT_LANGUAGE
  }
}

function onLanguageChanged(language: string) {
  document.documentElement.lang = language
  try {
    localStorage.setItem(LANGUAGE_STORAGE_KEY, language)
  } catch {
    // Storage can be unavailable (private mode); the choice then lasts for this session only.
  }
}

export function initI18n() {
  if (!i18n.isInitialized) {
    i18n.on('languageChanged', onLanguageChanged)
    void i18n.use(initReactI18next).init({
      resources: {
        kn: { translation: kn },
        en: { translation: en },
        te: { translation: te },
        ta: { translation: ta },
        hi: { translation: hi },
      },
      lng: storedLanguage(),
      fallbackLng: 'en',
      supportedLngs: SUPPORTED_LANGUAGES,
      interpolation: { escapeValue: false },
      initAsync: false,
    })
  }
  return i18n
}
