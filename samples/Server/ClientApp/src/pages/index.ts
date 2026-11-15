import type { Pages } from '../spa';
import { HomePage } from './HomePage';
import { LoginPage } from './LoginPage';
import { PasskeysPage } from './PasskeysPage';
import { LogoutPage } from './LogoutPage';
import { ErrorPage } from './ErrorPage';

/** The app-id the backend renders, and the page it mounts. */
export const pages: Pages = {
  home: HomePage,
  login: LoginPage,
  passkeys: PasskeysPage,
  logout: LogoutPage,
  error: ErrorPage,
};
