import { FormEvent, useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { api } from '../api/client';

type Quiz = { id: string; name: string; dateUtc: string; location: string; status: string };

export function QuizzesPage() {
  const { t } = useTranslation();
  const [quizzes, setQuizzes] = useState<Quiz[]>([]);
  const [name, setName] = useState('');

  const load = async () => setQuizzes(await api<Quiz[]>('/quizzes'));
  useEffect(() => {
    load().catch(console.error);
  }, []);

  const submit = async (e: FormEvent) => {
    e.preventDefault();
    await api('/quizzes', {
      method: 'POST',
      body: JSON.stringify({
        name,
        dateUtc: new Date().toISOString(),
        location: 'Belgrade',
        leagueId: null,
        categoryIds: [],
        teamIds: []
      })
    });
    setName('');
    await load();
  };

  return (
    <section>
      <h2>{t('quizzes.title')}</h2>
      <form onSubmit={submit} style={{ display: 'flex', gap: 8 }}>
        <input value={name} onChange={(e) => setName(e.target.value)} required placeholder={t('quizzes.create')} />
        <button type="submit">{t('quizzes.create')}</button>
      </form>
      <ul>
        {quizzes.map((quiz) => (
          <li key={quiz.id}>{quiz.name} · {quiz.status}</li>
        ))}
      </ul>
    </section>
  );
}
