import React from 'react';
import ReactDOM from 'react-dom/client';
import { BrowserRouter, Route, Routes } from 'react-router-dom';
import './lib/i18n';
import { Layout } from './components/Layout';
import { DashboardPage } from './pages/DashboardPage';
import { QuizzesPage } from './pages/QuizzesPage';
import { TeamsPage } from './pages/TeamsPage';
import { LeaguesPage } from './pages/LeaguesPage';
import { SettingsPage } from './pages/SettingsPage';

ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<Layout />}>
          <Route index element={<DashboardPage />} />
          <Route path="quizzes" element={<QuizzesPage />} />
          <Route path="teams" element={<TeamsPage />} />
          <Route path="leagues" element={<LeaguesPage />} />
          <Route path="settings" element={<SettingsPage />} />
        </Route>
      </Routes>
    </BrowserRouter>
  </React.StrictMode>
);
