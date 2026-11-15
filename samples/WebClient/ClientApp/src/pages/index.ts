import type { Pages } from '../spa';
import { HomePage } from './HomePage';
import { SessionPage } from './SessionPage';
import { CallApiPage } from './CallApiPage';

/** The app-id the backend renders, and the page it mounts. */
export const pages: Pages = {
  home: HomePage,
  session: SessionPage,
  'call-api': CallApiPage,
};
