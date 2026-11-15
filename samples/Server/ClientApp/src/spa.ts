import type { ComponentType } from 'react';

/**
 * The pages of the application, by app-id. Each one receives the model the backend rendered for it; `never` lets
 * pages with different models share the map.
 */
export type Pages = Record<string, ComponentType<{ model: never }>>;

export interface MountPoint {
  element: HTMLElement;
  appId: string;
  model: unknown;
}

/**
 * Reads <div id="app" app-id="login" app-model="eyJ...">: which page to show, and its model as base64 UTF-8 JSON.
 */
export function readMountPoint(): MountPoint {
  const element = document.getElementById('app');
  if (!element) throw new Error('The page has no <div id="app">.');

  const appId = element.getAttribute('app-id');
  if (!appId) throw new Error('<div id="app"> has no app-id.');

  const encoded = element.getAttribute('app-model');
  return { element, appId, model: encoded ? decodeModel(encoded) : {} };
}

function decodeModel(base64: string): unknown {
  const bytes = Uint8Array.from(atob(base64), c => c.charCodeAt(0));
  return JSON.parse(new TextDecoder().decode(bytes));
}
