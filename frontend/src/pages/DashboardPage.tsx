import { useTranslation } from 'react-i18next';

export function DashboardPage() {
  const { t } = useTranslation();

  return (
    <section>
      <h2>{t('dashboard.welcome')} Demo Organization</h2>
      <p>{t('dashboard.metrics')}</p>
      <ul>
        <li>MTD quizzes: 3 / 10</li>
        <li>Teams: 12</li>
        <li>Leagues: 1</li>
      </ul>
    </section>
  );
}
