import { useCallback } from 'react';
import { BrowserRouter, Navigate, Route, Routes, useLocation, useNavigate } from 'react-router';
import { ConsoleLayout, NavItem } from '../components/ui';
import type { ConsoleModel } from '../spa';
import { useApi, useLoad, type ProfileModel } from './api';
import { AccountContext } from './context';
import { OverviewPage } from './OverviewPage';
import { ProfilePage } from './ProfilePage';
import { SecurityPage } from './SecurityPage';
import { ApplicationsPage } from './ApplicationsPage';
import { LinkedPage } from './LinkedPage';
import { ActivityPage } from './ActivityPage';

/** The account console: the signed-in user's self-service, under /account. */
export function AccountApp({ model }: { model: ConsoleModel }) {
  return (
    <BrowserRouter basename="/account">
      <Shell model={model} />
    </BrowserRouter>
  );
}

const pages = [
  { path: '/', label: 'Overview' },
  { path: '/profile', label: 'Profile' },
  { path: '/security', label: 'Security' },
  { path: '/applications', label: 'Applications' },
  { path: '/linked', label: 'Linked accounts' },
  { path: '/activity', label: 'Activity' },
];

function Shell({ model }: { model: ConsoleModel }) {
  const api = useApi(model);
  const profile = useLoad(useCallback(() => api.get<ProfileModel>('/api/account/me'), [api]));
  const navigate = useNavigate();
  const { pathname } = useLocation();

  const nav = (
    <>
      {pages.map(page => (
        <NavItem key={page.path} href={`/account${page.path === '/' ? '' : page.path}`} active={page.path === '/' ? pathname === '/' : pathname.startsWith(page.path)} onClick={() => navigate(page.path)}>
          {page.label}
        </NavItem>
      ))}
      {model.user.isAdmin && <NavItem href="/admin" active={false}>Administration</NavItem>}
    </>
  );

  return (
    <AccountContext.Provider value={{ api, model, profile }}>
      <ConsoleLayout product={model.product} title="My account" nav={nav} userName={model.user.name ?? model.user.userName ?? ''}>
        <Routes>
          <Route path="/" element={<OverviewPage />} />
          <Route path="/profile" element={<ProfilePage />} />
          <Route path="/security" element={<SecurityPage />} />
          <Route path="/applications" element={<ApplicationsPage />} />
          <Route path="/linked" element={<LinkedPage />} />
          <Route path="/activity" element={<ActivityPage />} />
          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
      </ConsoleLayout>
    </AccountContext.Provider>
  );
}
