import i18n from 'i18next';
import { initReactI18next } from 'react-i18next';
import sr from '../i18n/sr.json';
import en from '../i18n/en.json';

const preferred = localStorage.getItem('quizory-language') ?? 'sr';

i18n.use(initReactI18next).init({
  resources: { sr: { translation: sr }, en: { translation: en } },
  lng: preferred,
  fallbackLng: 'sr',
  interpolation: { escapeValue: false }
});

export default i18n;
