import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import './index.css';
import { readMountPoint } from './spa';
import { pages } from './pages';

const { element, appId, model } = readMountPoint();
const Page = pages[appId];
if (!Page) throw new Error(`Unknown app-id "${appId}".`);

createRoot(element).render(
  <StrictMode>
    <Page model={model as never} />
  </StrictMode>,
);
