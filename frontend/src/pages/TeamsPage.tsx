import { FormEvent, useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { api } from '../api/client';

type Team = { id: string; name: string };

export function TeamsPage() {
  const { t } = useTranslation();
  const [teams, setTeams] = useState<Team[]>([]);
  const [name, setName] = useState('');

  const load = async () => setTeams(await api<Team[]>('/teams'));
  useEffect(() => {
    load().catch(console.error);
  }, []);

  const submit = async (e: FormEvent) => {
    e.preventDefault();
    await api('/teams', { method: 'POST', body: JSON.stringify({ name }) });
    setName('');
    await load();
  };

  return (
    <section>
      <h2>{t('teams.title')}</h2>
      <form onSubmit={submit} style={{ display: 'flex', gap: 8 }}>
        <input value={name} onChange={(e) => setName(e.target.value)} required />
        <button type="submit">+</button>
      </form>
      <ul>{teams.map((team) => <li key={team.id}>{team.name}</li>)}</ul>
    </section>
  );
}
