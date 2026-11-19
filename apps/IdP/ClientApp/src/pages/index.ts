import type { Pages } from '../spa';
import { LoginPage } from '../flow/LoginPage';
import { MfaPage } from '../flow/MfaPage';
import { ConsentPage } from '../flow/ConsentPage';
import { DevicePage, ErrorPage, ForgotPasswordPage, LogoutPage, MessagePage, RegisterPage, ResetPasswordPage } from '../flow/SimplePages';
import { AccountApp } from '../account/AccountApp';
import { AdminApp } from '../admin/AdminApp';

/** The app-id the backend renders, and the page it mounts. */
export const pages: Pages = {
  login: LoginPage,
  mfa: MfaPage,
  consent: ConsentPage,
  logout: LogoutPage,
  error: ErrorPage,
  message: MessagePage,
  register: RegisterPage,
  'forgot-password': ForgotPasswordPage,
  'reset-password': ResetPasswordPage,
  device: DevicePage,
  account: AccountApp,
  admin: AdminApp,
};
