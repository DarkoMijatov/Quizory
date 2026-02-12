import { FormEvent, useState } from 'react';
import { useTranslation } from 'react-i18next';

export function SettingsPage() {
  const { t } = useTranslation();
  const [color, setColor] = useState('#5E35B1');

  const submit = (e: FormEvent) => {
    e.preventDefault();
    alert(`Saved: ${color}`);
  };

  return (
    <section>
      <h2>{t('settings.title')}</h2>
      <form onSubmit={submit}>
        <label>
          {t('settings.color')}
          <input type="color" value={color} onChange={(e) => setColor(e.target.value)} />
        </label>
        <button type="submit">{t('common.save')}</button>
      </form>
    </section>
  );
}
