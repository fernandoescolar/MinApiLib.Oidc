// The JSON shapes of /api/admin/** (camelCase; enums as camelCase strings).

export interface EventModel {
  id: string;
  category: string;
  name: string;
  eventType: 'Success' | 'Failure' | 'Information' | 'Error';
  eventId: number;
  message: string | null;
  timeStamp: string;
  activityId: string | null;
  remoteIpAddress: string | null;
  subjectId: string | null;
  clientId: string | null;
  username: string | null;
  details: unknown;
}

export interface Dashboard {
  server: {
    name: string;
    version: string;
    issuer: string;
    discovery: string;
    registrationEnabled: boolean;
    requireEmailVerification: boolean;
    emailConfigured: boolean;
    eventsRetentionDays: number;
  };
  counts: {
    users: number;
    disabledUsers: number;
    newUsersThisWeek: number;
    clients: number;
    disabledClients: number;
    identityProviders: number;
    activeGrants: number;
    refreshTokens: number;
    eventsLast24h: { successes: number; failures: number };
  };
  signInsByDay: { date: string; signIns: number; failures: number }[];
  recentEvents: EventModel[];
}

export type GrantType = 'authorization_code' | 'client_credentials' | 'hybrid' | 'implicit' | 'password' | 'urn:ietf:params:oauth:grant-type:device_code';

export const grantTypes: { value: GrantType; label: string; hint: string }[] = [
  { value: 'authorization_code', label: 'Authorization code', hint: 'Interactive sign-in with PKCE. Web, mobile and desktop apps.' },
  { value: 'client_credentials', label: 'Client credentials', hint: 'Machine-to-machine, no user.' },
  { value: 'hybrid', label: 'Hybrid', hint: 'Code and tokens from the authorization endpoint. Legacy.' },
  { value: 'implicit', label: 'Implicit', hint: 'Deprecated by OAuth 2.1; needs "Allow implicit flow".' },
  { value: 'password', label: 'Resource owner password', hint: 'Deprecated by OAuth 2.1; needs "Allow resource owner password flow".' },
  { value: 'urn:ietf:params:oauth:grant-type:device_code', label: 'Device code', hint: 'Input-constrained devices (TVs, CLIs).' },
];

export interface ClientClaim {
  type: string;
  value: string;
  valueType: string;
}

export interface SecretModel {
  id: string;
  description: string | null;
  expiration: string | null;
  type: string;
}

export interface ClientSummary {
  clientId: string;
  clientName: string | null;
  description: string | null;
  enabled: boolean;
  allowedGrantTypes: string[];
  requireConsent: boolean;
  requireDPoP: boolean;
  secrets: number;
  logoUri: string | null;
}

/** The server's Client model. PUT takes it whole, so every field travels back. */
export interface ClientModel {
  enabled: boolean;
  clientId: string;
  protocolType: string;
  requireClientSecret: boolean;
  clientName: string | null;
  description: string | null;
  clientUri: string | null;
  logoUri: string | null;
  requireConsent: boolean;
  allowRememberConsent: boolean;
  allowedGrantTypes: string[];
  requirePkce: boolean;
  allowPlainTextPkce: boolean;
  allowImplicitFlow: boolean;
  allowResourceOwnerPasswordFlow: boolean;
  allowAccessTokensViaBrowser: boolean;
  requireRequestObject: boolean;
  requirePushedAuthorization: boolean;
  requireDPoP: boolean;
  redirectUris: string[];
  requestUris: string[];
  postLogoutRedirectUris: string[];
  frontChannelLogoutUri: string | null;
  frontChannelLogoutSessionRequired: boolean;
  backChannelLogoutUri: string | null;
  backChannelLogoutSessionRequired: boolean;
  allowOfflineAccess: boolean;
  allowedScopes: string[];
  alwaysIncludeUserClaimsInIdToken: boolean;
  identityTokenLifetime: number;
  allowedIdentityTokenSigningAlgorithms: string[];
  accessTokenLifetime: number;
  authorizationCodeLifetime: number;
  authorizationNonceLifetime: number;
  absoluteRefreshTokenLifetime: number;
  slidingRefreshTokenLifetime: number;
  consentLifetime: number | null;
  refreshTokenUsage: 'reUse' | 'oneTimeOnly';
  updateAccessTokenClaimsOnRefresh: boolean;
  refreshTokenExpiration: 'sliding' | 'absolute';
  accessTokenType: 'jwt' | 'reference';
  enableLocalLogin: boolean;
  identityProviderRestrictions: string[];
  includeJwtId: boolean;
  claims: ClientClaim[];
  alwaysSendClientClaims: boolean;
  clientClaimsPrefix: string;
  userSsoLifetime: number | null;
  userCodeType: string | null;
  deviceCodeLifetime: number;
  allowedCorsOrigins: string[];
  properties: Record<string, string>;
}

export type ClientDetail = ClientModel & { secrets: SecretModel[] };
export type CreatedClient = ClientDetail & { clientSecret: string | null };

/** The server defaults, for a new client. */
export function newClient(): ClientModel {
  return {
    enabled: true,
    clientId: '',
    protocolType: 'oidc',
    requireClientSecret: true,
    clientName: null,
    description: null,
    clientUri: null,
    logoUri: null,
    requireConsent: false,
    allowRememberConsent: true,
    allowedGrantTypes: ['authorization_code'],
    requirePkce: true,
    allowPlainTextPkce: false,
    allowImplicitFlow: false,
    allowResourceOwnerPasswordFlow: false,
    allowAccessTokensViaBrowser: false,
    requireRequestObject: false,
    requirePushedAuthorization: false,
    requireDPoP: false,
    redirectUris: [],
    requestUris: [],
    postLogoutRedirectUris: [],
    frontChannelLogoutUri: null,
    frontChannelLogoutSessionRequired: true,
    backChannelLogoutUri: null,
    backChannelLogoutSessionRequired: true,
    allowOfflineAccess: false,
    allowedScopes: ['openid', 'profile'],
    alwaysIncludeUserClaimsInIdToken: false,
    identityTokenLifetime: 300,
    allowedIdentityTokenSigningAlgorithms: [],
    accessTokenLifetime: 3600,
    authorizationCodeLifetime: 300,
    authorizationNonceLifetime: 120,
    absoluteRefreshTokenLifetime: 2592000,
    slidingRefreshTokenLifetime: 1296000,
    consentLifetime: null,
    refreshTokenUsage: 'oneTimeOnly',
    updateAccessTokenClaimsOnRefresh: false,
    refreshTokenExpiration: 'absolute',
    accessTokenType: 'jwt',
    enableLocalLogin: true,
    identityProviderRestrictions: [],
    includeJwtId: true,
    claims: [],
    alwaysSendClientClaims: false,
    clientClaimsPrefix: 'client_',
    userSsoLifetime: null,
    userCodeType: null,
    deviceCodeLifetime: 300,
    allowedCorsOrigins: [],
    properties: {},
  };
}

export interface ResourceModel {
  name: string;
  displayName: string | null;
  description: string | null;
  enabled: boolean;
  required: boolean;
  emphasize: boolean;
  showInDiscoveryDocument: boolean;
  userClaims: string[];
  properties: Record<string, string>;
}

export interface ApiResourceModel extends ResourceModel {
  scopes: string[];
  allowedAccessTokenSigningAlgorithms: string[];
  requireResourceIndicator: boolean;
  secrets: SecretModel[];
}

export interface NewSecretResult {
  secret: SecretModel;
  value: string;
}

export interface UserSummary {
  id: string;
  userName: string;
  email: string | null;
  emailConfirmed: boolean;
  displayName: string;
  enabled: boolean;
  lockedOut: boolean;
  twoFactorEnabled: boolean;
  roles: string[];
  createdAt: string;
  lastSignInAt: string | null;
}

export interface Claim {
  type: string;
  value: string;
}

export interface UserDetail {
  id: string;
  userName: string;
  email: string | null;
  emailConfirmed: boolean;
  firstName: string | null;
  lastName: string | null;
  phoneNumber: string | null;
  phoneNumberConfirmed: boolean;
  enabled: boolean;
  lockoutEnd: string | null;
  accessFailedCount: number;
  twoFactorEnabled: boolean;
  recoveryCodesLeft: number;
  hasPassword: boolean;
  roles: string[];
  effectiveRoles: string[];
  groups: { id: string; name: string }[];
  claims: Claim[];
  attributes: Record<string, string>;
  passkeys: { id: string; name: string | null; createdAt: string; synced: boolean; userVerified: boolean }[];
  logins: { provider: string; displayName: string | null; providerKey: string; linkedAt: string }[];
  createdAt: string;
  updatedAt: string;
  lastSignInAt: string | null;
}

/** PUT /users/{id}: a null or omitted field keeps its value. */
export interface UserInput {
  userName?: string;
  email?: string;
  emailConfirmed?: boolean;
  firstName?: string;
  lastName?: string;
  phoneNumber?: string;
  enabled?: boolean;
  roles?: string[];
  groups?: string[];
  claims?: Claim[];
  attributes?: Record<string, string>;
  password?: string;
  sendInvitation?: boolean;
}

export type UserAction = 'send-password-reset' | 'send-verification' | 'confirm-email' | 'unlock' | 'reset-authenticator' | 'sign-out';

export interface RoleModel {
  id: string;
  name: string;
  description: string | null;
  builtIn: boolean;
  users: number;
}

export interface GroupModel {
  id: string;
  name: string;
  description: string | null;
  roles: string[];
  attributes: Record<string, string>;
  members: number;
  createdAt: string;
}

export interface GroupInput {
  name: string;
  description: string | null;
  roles: string[];
  attributes: Record<string, string>;
}

export interface GrantModel {
  key: string;
  type: string;
  subjectId: string | null;
  subjectName: string | null;
  sessionId: string | null;
  clientId: string;
  clientName: string | null;
  description: string | null;
  creationTime: string;
  expiration: string | null;
  consumedTime: string | null;
}

export const grantTypeLabels: Record<string, string> = {
  refresh_token: 'Refresh token',
  reference_token: 'Reference token',
  authorization_code: 'Authorization code',
  user_consent: 'Consent',
  device_code: 'Device code',
  par: 'Pushed authorization request',
};

export interface KeysModel {
  jwksUri: string;
  keys: { id: string; created: string; algorithm: string; dataProtected: boolean; version: number }[];
}

export interface EventFilters {
  categories: string[];
  names: string[];
  types: string[];
}

export interface IdentityProviderModel {
  alias: string;
  displayName: string;
  enabled: boolean;
  authority: string;
  clientId: string;
  hasClientSecret: boolean;
  scopes: string[];
  getClaimsFromUserInfoEndpoint: boolean;
  usePkce: boolean;
  autoProvision: boolean;
  trustEmail: boolean;
  showOnLoginPage: boolean;
  order: number;
  callbackPath: string;
  createdAt: string;
  updatedAt: string;
  linkedUsers: number;
}

export interface IdentityProviderInput {
  alias: string;
  displayName: string;
  enabled: boolean;
  authority: string;
  clientId: string;
  clientSecret: string;
  scopes: string[];
  getClaimsFromUserInfoEndpoint: boolean;
  usePkce: boolean;
  autoProvision: boolean;
  trustEmail: boolean;
  showOnLoginPage: boolean;
  order: number;
}
