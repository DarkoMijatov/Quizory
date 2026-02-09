import { Link, Outlet } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { LanguageSwitcher } from './LanguageSwitcher';

export function Layout() {
  const { t } = useTranslation();

  return (
    <div style={{ fontFamily: 'Inter, sans-serif', margin: '0 auto', maxWidth: 1000, padding: 16 }}>
      <header style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <h1>{t('appName')}</h1>
        <LanguageSwitcher />
      </header>
      <nav style={{ display: 'flex', gap: 12 }}>
        <Link to="/">{t('nav.dashboard')}</Link>
        <Link to="/quizzes">{t('nav.quizzes')}</Link>
        <Link to="/teams">{t('nav.teams')}</Link>
        <Link to="/leagues">{t('nav.leagues')}</Link>
        <Link to="/settings">{t('nav.settings')}</Link>
      </nav>
      <main style={{ marginTop: 20 }}>
        <Outlet />
      </main>
    </div>
  );
}
