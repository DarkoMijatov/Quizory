import { useTranslation } from 'react-i18next';

export function LanguageSwitcher() {
  const { i18n, t } = useTranslation();

  const changeLanguage = (language: 'sr' | 'en') => {
    i18n.changeLanguage(language);
    localStorage.setItem('quizory-language', language);
  };

  return (
    <label>
      {t('language.switch')}
      <select value={i18n.language} onChange={(e) => changeLanguage(e.target.value as 'sr' | 'en')}>
        <option value="sr">Srpski</option>
        <option value="en">English</option>
      </select>
    </label>
  );
}
