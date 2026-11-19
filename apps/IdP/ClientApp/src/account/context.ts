import { createContext, useContext } from 'react';
import type { Api } from '../api';
import type { ConsoleModel } from '../spa';
import type { Loaded, ProfileModel } from './api';

/** What every page of the console shares: the API client, the page model and the user's profile. */
export interface AccountContextValue {
  api: Api;
  model: ConsoleModel;
  profile: Loaded<ProfileModel>;
}

export const AccountContext = createContext<AccountContextValue | null>(null);

export function useAccount(): AccountContextValue {
  const value = useContext(AccountContext);
  if (!value) throw new Error('useAccount needs an AccountContext.');
  return value;
}
