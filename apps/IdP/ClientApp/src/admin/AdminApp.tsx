import { BrowserRouter, Navigate, Route, Routes, useLocation, useNavigate } from 'react-router';
import { ConsoleLayout, NavItem } from '../components/ui';
import type { ConsoleModel } from '../spa';
import { AdminContext, useApi } from './hooks';
import { DashboardPage } from './DashboardPage';
import { ClientsPage } from './ClientsPage';
import { ClientEditPage } from './ClientEditPage';
import { ScopesPage } from './ScopesPage';
import { UsersPage } from './UsersPage';
import { UserDetailPage } from './UserDetailPage';
import { RolesPage } from './RolesPage';
import { GroupsPage } from './GroupsPage';
import { GroupMembersPage } from './GroupMembersPage';
import { IdentityProvidersPage, IdentityProviderEditPage } from './IdentityProvidersPage';
import { SessionsPage } from './SessionsPage';
import { KeysPage } from './KeysPage';
import { EventsPage } from './EventsPage';

const navItems: { path: string; label: string }[] = [
  { path: '/', label: 'Dashboard' },
  { path: '/clients', label: 'Clients' },
  { path: '/scopes', label: 'Scopes & resources' },
  { path: '/users', label: 'Users' },
  { path: '/roles', label: 'Roles' },
  { path: '/groups', label: 'Groups' },
  { path: '/identity-providers', label: 'Identity providers' },
  { path: '/sessions', label: 'Sessions & tokens' },
  { path: '/keys', label: 'Signing keys' },
  { path: '/events', label: 'Events' },
];

/** The administration console, at /admin/**. */
export function AdminApp({ model }: { model: ConsoleModel }) {
  const api = useApi(model);
  return (
    <AdminContext.Provider value={{ api, model }}>
      <BrowserRouter basename="/admin">
        <Shell model={model} />
      </BrowserRouter>
    </AdminContext.Provider>
  );
}

function Shell({ model }: { model: ConsoleModel }) {
  const navigate = useNavigate();
  const { pathname } = useLocation();
  const isActive = (path: string) => (path === '/' ? pathname === '/' : pathname === path || pathname.startsWith(`${path}/`));

  const nav = (
    <>
      {navItems.map(item => (
        <NavItem key={item.path} href={`/admin${item.path === '/' ? '' : item.path}`} active={isActive(item.path)} onClick={() => navigate(item.path)}>
          {item.label}
        </NavItem>
      ))}
      <div className="my-2 border-t border-slate-200" />
      <NavItem href="/account" active={false}>My account</NavItem>
    </>
  );

  return (
    <ConsoleLayout product={model.product} title="Administration" nav={nav} userName={model.user.name ?? model.user.userName ?? 'Administrator'}>
      <Routes>
        <Route path="/" element={<DashboardPage />} />
        <Route path="/clients" element={<ClientsPage />} />
        <Route path="/clients/new" element={<ClientEditPage />} />
        <Route path="/clients/:clientId" element={<ClientEditPage />} />
        <Route path="/scopes" element={<ScopesPage />} />
        <Route path="/users" element={<UsersPage />} />
        <Route path="/users/:id" element={<UserDetailPage />} />
        <Route path="/roles" element={<RolesPage />} />
        <Route path="/groups" element={<GroupsPage />} />
        <Route path="/groups/:id" element={<GroupMembersPage />} />
        <Route path="/identity-providers" element={<IdentityProvidersPage />} />
        <Route path="/identity-providers/new" element={<IdentityProviderEditPage />} />
        <Route path="/identity-providers/:alias" element={<IdentityProviderEditPage />} />
        <Route path="/sessions" element={<SessionsPage />} />
        <Route path="/keys" element={<KeysPage />} />
        <Route path="/events" element={<EventsPage />} />
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </ConsoleLayout>
  );
}
