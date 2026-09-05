# Análisis del producto y plan de mejora

*Fork de IdentityServer4 4.1.x · net10.0 / net11.0 · 14 de noviembre de 2026*

Este documento recoge un análisis exhaustivo del estado actual del fork y propone mejoras ordenadas por impacto. Se ha elaborado leyendo el código fuente completo de `src/` y `tests/`, contrastándolo con los avisos de seguridad publicados para IdentityServer4 y Duende IdentityServer, y verificando a mano cada hallazgo de severidad alta. Las referencias `archivo:línea` apuntan al estado del código en la fecha indicada.

---

## 1. Resumen ejecutivo

**Estado general.** La base es sólida: el código hereda las decisiones correctas de IdentityServer4 4.x (PKCE obligatorio por defecto, `at+jwt`, rotación de refresh tokens, handles y códigos almacenados hasheados, CSPRNG en todas partes, HTML codificado en las páginas renderizadas) y el fork ha ido más allá al eliminar los grants `implicit` y `password` y al portar ya el parche de **CVE-2024-39694** (open redirect en `IsLocalUrl`), que Duende nunca publicó para IS4. Compila sin errores ni warnings y los 996 tests pasan.

**Lo que más urge.** Hay un hallazgo de severidad alta en código propio del fork (el flujo de login por nonce) y tres huecos heredados que Duende corrigió sin asignar CVE. Ninguno requiere un rediseño; son cambios acotados.

> **Estado a 16/11/2026: fases 0 a 6 aplicadas, más la ronda posterior de §5.6** (sin `ClientConfiguration`, sólo endpoint routing, `prompt` extensible, flujos deprecados tras flags, `IdentityServer.IdentityModel` propio y sin FluentAssertions). Fase 0: S1 (salvo dos puntos de producto), S3, S4, S12 y los tres parches heredados de Duende (§3.4). Fase 1: build, empaquetado y CI. Fase 2: S2, S5–S11 y S13–S24, con cuatro desviaciones justificadas del plan (§3.5). Fase 3: limpieza estructural (§5.4). Fase 4: endpoint routing (§4.5). Fase 5: PAR, DPoP, resource indicators, `client_id` en end session y rotación automática de claves (§3.7). Fase 6: nullable, `LoggerMessage`, source generation, analizadores y validación de opciones (§5.5). Cada corrección de seguridad tiene tests que fallan si se deshace.

**Diez acciones con mayor retorno**, en orden:

| # | Acción | Área | Esfuerzo |
|---|---|---|---|
| 1 | Corregir el flujo de login por nonce: comprobar caducidad y cliente, no forzar sesión persistente de 1 día, pasar por las políticas de login, filtrar `login_hint_token` en logs | Seguridad | 1–2 días |
| 2 | Aplicar los tres parches heredados de Duende: no devolver `redirect_uri` en errores no seguros, exigir secreto para `client_credentials`, usar `PostLogoutRedirectUris` en el validador AppAuth | Seguridad | 0,5 día |
| 3 | Emitir el logout token con `typ: logout+jwt` y propagar `cnf` en la introspección de reference tokens | Seguridad | 0,5 día |
| 4 | Base del repositorio: primer commit, CI, versionado (MinVer), metadatos de paquete, LICENSE/NOTICE, SourceLink, `TreatWarningsAsErrors`, `dotnet.config` | Build | 1 día |
| 5 | Resolver la licencia de FluentAssertions 8 (de pago para uso comercial): fijar 7.2.0 o migrar a AwesomeAssertions | Legal/Build | 1 hora |
| 6 | Registrar `TimeProvider` explícitamente y sustituir el singleton mutable de `IdentityServerOptions` por `IValidateOptions` + `ValidateOnStart` | Arquitectura | 0,5 día |
| 7 | Sacar los modelos específicos de negocio (`ClientPropertyNames`, `LegalTerm*`, `MarketingNotification`, `SupportedCountry`, `LoginProviders`) de `IdentityServer.Models` | Diseño | 0,5 día |
| 8 | Añadir `iss` a la respuesta de autorización (RFC 9207) y endurecer `request_uri` (sin redirecciones, DNS resuelto, pre-registro) | Seguridad/Spec | 1 día |
| 9 | Migración aditiva a endpoint routing (`MapIdentityServer()`), manteniendo `IEndpointHandler` como adaptador | Arquitectura | 2–3 semanas |
| 10 | Nullable reference types por proyecto (Storage → Models → AspNetIdentity → core) y completar la migración a `LoggerMessage` | Modernización | continuo |

---

## 2. Contexto y alcance

### 2.1 Qué es este fork

- Fork de IdentityServer4 4.1.x con los namespaces renombrados de `IdentityServer4.*` a `IdentityServer.*` y cuatro proyectos de librería: `IdentityServer` (27.245 líneas), `IdentityServer.Models` (1.992), `IdentityServer.Storage` (512) y `IdentityServer.AspNetIdentity` (448), más `IdentityServer.Test` (store de usuarios de prueba).
- Multi-target `net10.0;net11.0`, `LangVersion latest`, usings implícitos, gestión central de paquetes, System.Text.Json en lugar de Newtonsoft para grants persistidos, `TimeProvider` en lugar de `ISystemClock`.
- **Diferencias funcionales respecto a IS4 4.1.x:** se han eliminado el flujo implícito (`id_token`, `token`) y el grant `password` (ROPC); se ha añadido un **patrón de nonce** propio (`/connect/nonce`, grant `urn:auth:nonce`, parámetro `login_hint_token`) que permite a un cliente convertir un access token en un inicio de sesión interactivo; se ha añadido `StrictRedirectUriValidatorAppAuth` para apps nativas; y `IdentityServer.Models` incluye modelos de negocio (términos legales, países, proveedores de login, propiedades de cliente) que no usa ningún proyecto de `src/`.
- No existe el paquete EntityFramework ni la UI de quickstart. No hay historial git, CI, versionado ni metadatos de paquete.

### 2.2 Método

1. Lectura completa de `src/` y `tests/` en tres pasadas paralelas: arquitectura y hosting, seguridad del protocolo, y calidad/modernización.
2. Investigación de los avisos publicados (GitHub Security Advisories, CVE.org, blog y release notes de Duende, repositorio archivado de IS4 en el tag 4.1.2) para distinguir qué vulnerabilidades son heredadas y cuáles pertenecen sólo a funcionalidades de Duende.
3. Verificación manual en este código de cada hallazgo alto y de cada aviso heredado.
4. `dotnet list package --vulnerable --include-transitive`: sin paquetes vulnerables.

---

## 3. Seguridad

### 3.1 Avisos publicados y su estado en el fork

| Aviso | Qué era | ¿Heredado de IS4? | Estado aquí |
|---|---|---|---|
| **CVE-2024-39694** (GHSA-ff4q-64jc-gx98 / GHSA-55p7-v223-x366) | Open redirect: `IsLocalUrl` aceptaba URLs con caracteres de control tras la barra inicial (`/\t/evil.com`), que algunos navegadores normalizan a `//evil.com`. IS4 ≤ 4.1.2 afectado y sin parche oficial | Sí | **Corregido.** `HasControlCharacter` presente en `src/IdentityServer/Extensions/StringsExtensions.cs:143-181`, con tests en `tests/IdentityServer.UnitTests/Extensions/IsLocalUrlTests.cs` |
| IS4 4.1.2, commit `997a6cd` (sin CVE) | XSS en el callback de end session: las URLs de front-channel logout se concatenaban sin codificar en `<iframe src=…>` | Sí | **Corregido.** `HtmlEncoder.Default.Encode(url)` en `Endpoints/Results/EndSessionCallbackResult.cs:66` |
| Duende PR #1933 (7.3.0, sin CVE) | `AuthorizeResult` pasaba `RedirectUri`/`ResponseMode` a la página de error incluso para errores "no seguros" (scope, nonce, response_mode inválidos, parámetros demasiado largos), permitiendo que la página de error devolviera al usuario al cliente | Sí | **Corregido (fase 0).** Eliminado el bloque de `Endpoints/Results/AuthorizeResult.cs` |
| Duende PR #1796 (7.3.0, sin CVE) | `DefaultClientConfigurationValidator` aceptaba `client_credentials` con `RequireClientSecret=false`, es decir, client credentials sin autenticación | Sí | **Corregido (fase 0).** `ValidateSecretsAsync` rechaza la combinación. Cambio de comportamiento: un cliente así deja de cargarse y el token endpoint responde `invalid_client` |
| Duende PR #619 (6.0.0, sin CVE) | `StrictRedirectUriValidatorAppAuth` validaba el post-logout redirect contra `RedirectUris` en vez de `PostLogoutRedirectUris` | Sí | **Corregido (fase 0).** Ahora consulta `PostLogoutRedirectUris` |
| **CVE-2026-4349** (VulDB, GHSA-46qm-5mvh-mmxr sin revisar) | "Autenticación incorrecta vía `id_token_hint` en `/connect/authorize`", IS4 4.1.0–4.1.2 | Dudoso | **No confirmable.** Sin PoC ni detalle técnico público. El endpoint authorize de IS4 no consume `id_token_hint`; a lo sumo es un hueco de conformidad (OIDC Core 3.1.2.1 exige error si el `sub` del hint no coincide con la sesión). Vigilar, no tratar como confirmado |
| CVE-2024-49755 (DPoP en Local APIs), GHSA-mxv6-xwqj-ww2p (PAR, CVSS 8.2) | Fallos en funcionalidades de Duende 7.x | No | No aplica: el fork no tiene DPoP ni PAR. Relevante si se implementan: ver §3.6 |
| CVE-2024-21319 (`System.IdentityModel.Tokens.Jwt` < 5.7.0 / < 6.34 / < 7.1.2), CVE-2024-21907 (Newtonsoft < 13.0.1) | DoS en dependencias que fijaba IS4 4.1.2 | Sí en IS4 | **No afecta.** El fork usa Microsoft.IdentityModel 8.23.0 y no usa Newtonsoft |

**Conclusión:** todos los avisos públicos aplicables están corregidos tras la fase 0. El riesgo restante está en los hallazgos propios de §3.2.

### 3.2 Hallazgos de la revisión de código, por severidad

Leyenda: **[Bug]** defecto confirmado · **[Hardening]** funciona pero por debajo de la práctica recomendada · **[Spec]** desviación de RFC/OIDC.

#### Alta

**S1. Flujo de login por nonce (código propio del fork) — [Bug] · fase 0: (a), (d), (e), (g) corregidos; (c) y (f) pendientes de decisión**
`ResponseHandling/Default/AuthorizeInteractionResponseGenerator.cs:116-205`. Un cliente llama a `/connect/nonce` con un access token válido, recibe un nonce (handle de 256 bits) y lo pasa como `login_hint_token` con `acr_values=urn:auth:nonce` a `/connect/authorize`; el servidor inicia sesión en el navegador como el sujeto de ese token sin interacción. Verificado en código:

- **(a) No se comprueba la caducidad.** `AuthorizationNonce.Lifetime` (120 s por defecto, `Client.cs:168`) y `CreationTime` nunca se leen; `DefaultGrantStore.GetItemAsync` (`Stores/Default/DefaultGrantStore.cs:75-97`) tampoco comprueba `Expiration`. Un nonce es válido hasta que el store lo purgue, que con el store en memoria por defecto es nunca.
- **(b) Uso entre clientes: intencionado, no es un bug** *(corrección de la primera versión de este informe)*. `docs/reference/nonce_pattern.md` define el patrón como SSO de una aplicación origen a una aplicación destino, y el test de integración emite el nonce con `client1` y lo consume con `client2`. Sí es relevante que **cualquier** cliente puede consumir un nonce, aunque no tenga el grant `urn:auth:nonce`; restringirlo (p. ej. lista de clientes destino permitidos) es una decisión de producto.
- **(c) Sesión persistente forzada de 1 día** (`IsPersistent = true`, `ExpiresUtc = ahora + 1 día`), ignorando `Authentication.CookieLifetime` (10 h). *Fase 0:* ahora usa `TimeProvider`; el valor de 1 día y la persistencia se mantienen hasta que se decida. *Corrección:* `amr`, `idp` y `auth_time` se conservan del access token original (no son claims de protocolo filtradas), así que no se fuerza `amr=password`.
- **(d) Salta las políticas de login** (`EnableLocalLogin`, `IdentityProviderRestrictions`, `UserSsoLifetime`, `max_age`) porque `result ??= ProcessLoginAsync(...)` (línea 95) sólo se ejecuta si el nonce no devolvió respuesta.
- **(e) `login_hint_token` no está en `AuthorizeRequestSensitiveValuesFilter`** (`Options/LoggingOptions.cs:23-27`): una credencial de inicio de sesión acaba en los logs de Debug/Error.
- **(f) Cualquier access token del cliente sirve**, sin scope dedicado: un access token de API filtrado escala a sesión interactiva en el IdP.
- **(g) `request.SessionId` se calculó antes del sign-in**, así que el code/id_token emitidos llevan el `sid` antiguo (vacío).

Remedio aplicado en fase 0: se comprueba `CreationTime + Lifetime` con `TimeProvider` (el nonce caducado se consume y se rechaza); tras el sign-in se ejecuta `ProcessLoginAsync`, de modo que `max_age`, `IdentityProviderRestrictions`, `EnableLocalLogin` y `UserSsoLifetime` del cliente destino se aplican; `login_hint_token` está en `AuthorizeRequestSensitiveValuesFilter`; `request.SessionId` toma el `sid` de la sesión recién creada. Pendiente de decisión: (c) duración y persistencia de la cookie, (f) scope dedicado para `/connect/nonce` y restricción de clientes destino. Nota adicional: la documentación del patrón pide `prompt=login`, pero el código rechaza cualquier `prompt` distinto de `none`.

**S2. Confianza en certificados mTLS depende por completo del host — [Hardening, dependiente de configuración] · corregido en fase 2**
`X509NameSecretValidator` compara sólo `cert.Subject` (`Validation/Default/X509NameSecretValidator.cs:35-61`) y `MutualTlsSecretParser` acepta lo que devuelva `Connection.GetClientCertificateAsync()` (`MutualTlsSecretParser.cs:56-69`) sin verificar cadena ni comprobar que el esquema de certificado realmente autenticó. El parser está activo también en `/connect/token` normal, donde `MutualTlsEndpointMiddleware` no se ejecuta. Si el host acepta certificados autofirmados (como hacían los ejemplos originales de IS4), un secreto por nombre es suplantable por cualquiera que emita un certificado con el mismo Subject DN. Remedio: validar cadena en el validador o exigir un marcador de autenticación puesto por el middleware; preferir secretos por thumbprint SHA-256 (hoy `X509ThumbprintSecretValidator` usa el `Thumbprint` SHA-1, `X509ThumbprintSecretValidator.cs:35,51`).

#### Media

**S3. Logout token emitido con `typ: at+jwt` — [Bug/Spec] · corregido en fase 0** (nueva opción `LogoutTokenJwtType = "logout+jwt"` y tipo `TokenTypes.LogoutToken`; un logout token ya no valida como access token)
`DefaultBackChannelLogoutService.CreateTokenAsync` llama a `IdentityServerTools.IssueJwtAsync` (`IdentityServerTools.cs:32-48`), que crea un `Token` cuyo `Type` por defecto es `access_token` (`IdentityServer.Models/Token.cs:74`); `DefaultTokenCreationService.CreateHeaderAsync` estampa por tanto `typ: at+jwt`. OIDC Back-Channel Logout 1.0 §2.4 exige `logout+jwt`. Peor: el token pasa `TokenValidator.ValidateAccessTokenAsync` (el `typ` coincide, `aud` no se valida y sin claim `client_id` se salta la comprobación de cliente), por lo que serviría como access token en `LocalApiAuthenticationHandler` sin `ExpectedScope`.

**S4. Introspección pierde `cnf` en reference tokens — [Bug] · corregido en fase 0**
`TokenValidator.ReferenceTokenToClaims` (`Validation/Default/TokenValidator.cs:364-380`) nunca mapea `Token.Confirmation`. Una API que confíe en introspección no puede exigir el binding de certificado (mTLS + reference tokens).

**S5. Sin detección de reutilización de refresh tokens — [Spec] · corregido en fase 2**
`DefaultRefreshTokenService` rota por defecto (`OneTimeOnly`) y rechaza handles consumidos (`Services/Default/DefaultRefreshTokenService.cs:86-93`), pero `AcceptConsumedTokenAsync` sólo devuelve `false` (líneas 121-124): no se revoca la familia (RFC 9700 §4.14.2). La rotación Get→Update→Create no es atómica (190-201). En el refresh no se revalidan los `AllowedScopes` actuales del cliente y, con `UpdateAccessTokenClaimsOnRefresh=false` (por defecto), se refirma el mismo conjunto de claims incluido el mismo `jti` (`TokenResponseGenerator.cs:187-193`). El down-scoping por `scope` en refresh se ignora (`TokenRequestValidator.cs:310-342`).

**S6. Replay de authorization code sin revocación — [Hardening] · corregido en fase 2 (detección y revocación; la carrera concurrente persiste, ver §3.5)**
El código se consume con `Get` y luego `Remove` (`TokenRequestValidator.cs:187-200`), no atómico; dos intercambios concurrentes pueden tener éxito y, en reuso, no se revocan los tokens ya emitidos (RFC 6749 §4.1.2 SHOULD; RFC 9700 §4.5.3).

**S7. Access tokens todavía en el front channel — [Spec] · corregido en fase 2**
`code token` y `code id_token token` siguen en `Constants.SupportedResponseTypes` (`Constants.cs:13-19`) y en `AuthorizeResponseGenerator.CreateHybridFlowResponseAsync` (90-159). OAuth 2.1 / RFC 9700 §2.1.2 lo prohíben. Eliminarlos o ponerlos tras una opción explícita; conservar `code id_token` si hace falta hybrid.

**S8. Sin `iss` en la respuesta de autorización (RFC 9207) — [Spec] · corregido en fase 2**
`Extensions/AuthorizeResponseExtensions.cs:5-56` no añade `iss`, y discovery no publica `authorization_response_iss_parameter_supported`. Es la defensa estándar contra mix-up attacks; Duende lo emite desde 5.0.

**S9. `request_uri` (JAR) con riesgo de SSRF si se habilita — [Hardening] · corregido en fase 2 (sin cambiar `StrictJarValidation`, ver §3.5)**
`DefaultJwtRequestUriHttpClient` (`Services/Default/DefaultJwtRequestUriHttpClient.cs:28-117`) exige HTTPS, bloquea literales IP privados, limita a 10 s y 51.200 bytes. Pero no resuelve DNS (rebinding), **sigue redirecciones** (`AllowAutoRedirect` por defecto), no bloquea `fc00::/7`, `192.0.0.0/24`, `198.18.0.0/15` ni multicast, y cualquier llamada no autenticada que conozca un `client_id` con `EnableJwtRequestUri=true` dispara la petición. `request_uri` no exige pre-registro (RFC 9101 §10.4) y `StrictJarValidation` está desactivado por defecto (`IdentityServerOptions.cs:44`). Está desactivado por defecto (`EndpointOptions.cs:19`), lo que acota el riesgo.

**S10. Device flow: sin protección contra fuerza bruta del `user_code` — [Hardening] · corregido en fase 2**
`DefaultDeviceFlowInteractionService.FindByUserCodeAsync` no limita intentos (espacio de 9 dígitos, 300 s). La clave del throttle guarda el device code **en claro** (`DistributedDeviceFlowThrottlingService.cs:42`). `HandleRequestAsync` almacena `ScopesValuesConsented` sin comprobar que sea subconjunto de `RequestedScopes` (línea 66).

**S11. Issuer derivado de la cabecera `Host` — [Hardening] · mitigado en fase 2 con un aviso de arranque, ver §3.5**
Si `IssuerUri` no está configurado, el issuer, el `aud` de `private_key_jwt` y el documento de discovery dependen del `Host` de la petición (`Extensions/HttpContextExtensions.cs:117-134`). Exigir `IssuerUri` fuera de Development o documentar el filtrado de host en el proxy.

**S12. Logging de material sensible — [Hardening] · parcialmente corregido en fase 0** (`code` ofuscado, `code_verifier` eliminado del mensaje, `code`, `code_verifier` y `login_hint_token` añadidos a los filtros; el logging de tokens completos a nivel Trace sigue igual)
`TokenRequestValidator` escribe el `code` en claro (líneas 190, 204) y el `code_verifier` (259) a nivel Error, aunque `TokenRequestValidationLog` los ofusca. Tokens completos a nivel Trace en `TokenEndpoint.cs:98-115`, `AuthorizeEndpointBase.cs:134-150` y `LocalApiAuthenticationHandler.cs:60`. Faltan en los filtros: `login_hint_token`, `code`, `code_verifier`, `request`.

#### Baja

- **S13.** *Fase 2: acepta el issuer como `aud` y fija `ValidAlgorithms`; sin tope de vida ni `typ`.* `private_key_jwt` sólo acepta `aud = <issuer>/connect/token` (`PrivateKeyJwtSecretValidator.cs:66-70`); RFC 9700 §4.15 recomienda aceptar también el issuer. Sin tope de vida de la aserción, sin `ValidAlgorithms`, `typ` sin restringir. La caché de replay de `jti` usa `IDistributedCache`, que por defecto es `AddDistributedMemoryCache()` (`Core.cs:160`): protección por nodo.
- **S14.** *Corregido en fase 2.* Validador AppAuth: `IsLoopback` acepta cualquier path/query/fragment en `http://127.0.0.1:<puerto>` (RFC 8252 §7.3 sólo permite variar el puerto), acepta puerto 0 y no soporta `[::1]` (`StrictRedirectUriValidatorAppAuth.cs:63-109`). El test del fork afirma explícitamente que paths y fragments se permiten.
- **S15.** *Corregido en fase 2 (`InputLengthRestrictions.State`, 4000).* `state` sin límite de longitud (`InputLengthRestrictions` no tiene `State`); se refleja en returnUrl, código y respuesta.
- **S16.** *Corregido en fase 2.* `StrictRedirectUriValidator` compara con `OrdinalIgnoreCase` (RFC 6749 §3.1.2.3 exige comparación simple) y no rechaza fragment en `redirect_uri`.
- **S17.** *Fase 2: se rechazan dos métodos a la vez; el hash sin sal sigue igual.* No se rechaza el uso simultáneo de dos métodos de autenticación de cliente (`SecretParser.cs:27-54`; RFC 6749 §2.3 MUST NOT). Secretos compartidos hasheados con SHA-256/512 sin sal (`HashExtensions.cs:13-24`): adecuado para secretos generados, crackeable offline si un administrador elige contraseñas.
- **S18.** *Corregido en fase 2.* `aud` omitido en el access token cuando sólo hay `ApiScope`s y `EmitStaticAudienceClaim=false` (`DefaultTokenService.cs:195-203`); RFC 9068 §2.2 lo exige.
- **S19.** *Fase 2: sólo `S256`, y se publican los algoritmos y `authorization_response_iss_parameter_supported`.* Discovery anuncia `code_challenge_methods_supported: ["plain","S256"]` aunque `plain` se rechaza por defecto; `CustomEntries` no puede sobrescribir claves estándar; faltan `request_object_signing_alg_values_supported` y `token_endpoint_auth_signing_alg_values_supported`.
- **S20.** *Corregido en fase 2.* `DistributedCacheAuthorizationParametersMessageStore.DeleteAsync` borra la clave `id` en vez de `"{prefix}-{id}"`: las entradas nunca se borran (bug heredado de IS4).
- **S21.** *Fase 2: avisos de arranque fuera de Development.* `AddDeveloperSigningCredential` persiste la clave privada en claro en `tempkey.jwk` sin aviso en producción. `AddIdentityServer()` registra stores de grants en memoria por defecto con sólo una línea de log.
- **S22.** *Fase 2, parcial: `client_id` debe coincidir con el `id_token_hint`, ver §3.5.* End session: no se soporta el parámetro `client_id` de RP-Initiated Logout 1.0.
- **S23.** *Corregido en fase 2.* `prompt` desconocido se ignora en silencio (`AuthorizeRequestValidator.cs:561-564`); Duende 7.0 devuelve 400.
- **S24.** *Corregido en fase 2.* Las redirecciones tras POST usan 302; FAPI 2.0 y Duende 8.0 usan 303.

#### Verificado como correcto

`IsLocalUrl` con control de caracteres y backslashes · codificación HTML en form_post, iframes de logout y check_session · CSP nivel 2 con hashes en las páginas renderizadas · bearer token no aceptado en query string · `typ: at+jwt` impide usar un id_token como access token · claves de grants, device codes y user codes almacenados hasheados · `RandomNumberGenerator`/`CryptoRandom` en todas partes, ningún `System.Random` · System.Text.Json sin polimorfismo en la serialización de grants (sin riesgo de gadgets) · `Cache-Control: no-store` en respuestas de token · CORS por lista de orígenes, sin credenciales ni comodines · introspección y revocación limitadas por cliente/API · `id_token_hint` validado sin comprobar caducidad (correcto) · ROPC e implicit eliminados · dependencias sin avisos.

### 3.3 Cobertura de RFCs y buenas prácticas (OAuth 2.1, RFC 9700)

| Especificación | Estado | Evidencia |
|---|---|---|
| RFC 7636 PKCE por defecto, S256 efectivo | Presente | `Client.cs:87,92`; `AuthorizeRequestValidator.cs:371-429` |
| Sin implicit, sin ROPC | Presente | `Constants.cs:13-19`; `TokenRequestValidator.cs:115-127` |
| Sin access tokens en el front channel | Parcial (S7) | `AuthorizeResponseGenerator.cs:90-159` |
| `redirect_uri` exacto | Parcial (S14, S16) | `StrictRedirectUriValidator.cs:19` |
| RFC 9207 `iss` en respuesta | **Ausente** (S8) | `AuthorizeResponseExtensions.cs` |
| RFC 9126 PAR | **Presente (fase 5)** | `PushedAuthorizationEndpoint.cs`, `AuthorizeRequestValidator.cs` |
| RFC 9449 DPoP | **Presente (fase 5)**, sin nonces de servidor | `DefaultDPoPProofValidator.cs`, `DPoPRequest.cs`, `TokenEndpoint.cs` |
| RFC 8705 mTLS | Presente con huecos (S2, S4) | `Additional.cs:304-311`, `MutualTlsEndpointMiddleware.cs` |
| RFC 8707 resource indicators | **Presente (fase 5)** salvo en device authorization | `ResourceIndicators.cs`, `DefaultResourceValidator.cs` |
| RFC 8693 token exchange | Ausente (hay hook de impersonación para extension grants) | `ValidatedRequest.cs:96` |
| RFC 9068 JWT access tokens | Parcial (S18) | `DefaultTokenService.cs:195-203` |
| RFC 8252 apps nativas | Parcial (S14) | `ValidationOptions.cs:11-25` |
| CIBA | Ausente | — |
| OIDC Back-Channel Logout 1.0 | Parcial (S3) | `DefaultBackChannelLogoutService.cs` |
| OIDC RP-Initiated Logout 1.0 | **Presente** (S22 en fase 2, `client_id` sin `id_token_hint` en fase 5) | `EndSessionRequestValidator.cs` |
| OIDC Front-Channel Logout / Session Management | Presente | `EndSessionCallbackResult.cs`, `CheckSessionResult.cs` |
| RFC 7591 registro dinámico de clientes | Ausente | — |
| Rotación automática de claves de firma | **Presente (fase 5)**, opcional | `AutomaticKeyManager.cs`, `DefaultKeyMaterialService.cs` |
| Rotación de refresh tokens + detección de reuso | Parcial (S5) | `DefaultRefreshTokenService.cs` |
| JAR estricto (RFC 9101) | Parcial, desactivado por defecto (S9) | `IdentityServerOptions.cs:44` |
| RFC 8628 device flow | Presente, sin anti fuerza bruta (S10) | — |

### 3.4 Cambios aplicados en la fase 0

| Cambio | Archivos | Tests |
|---|---|---|
| Nonce: caducidad, políticas del cliente destino, `sid` de la nueva sesión, `TimeProvider` | `AuthorizeInteractionResponseGenerator.cs` | `AuthorizeInteractionResponseGeneratorTests_Nonce.cs` (9), `NonceTests.cs` (2 nuevos) |
| Página de error sin `redirect_uri` en errores no seguros | `AuthorizeResult.cs` | 12 tests de `AuthorizeTests.cs` actualizados |
| `client_credentials` exige secreto | `DefaultClientConfigurationValidator.cs` | `ClientConfigurationValidation.cs` (2 nuevos), `ClientCredentialsClient.cs` (1 invertido) |
| AppAuth post-logout contra `PostLogoutRedirectUris` | `StrictRedirectUriValidatorAppAuth.cs` | `StrictRedirectUriValidatorAppAuthValidation.cs` (2 nuevos) |
| Logout token `typ: logout+jwt` | `IdentityServerOptions.cs`, `IdentityServerConstants.cs`, `DefaultTokenCreationService.cs`, `IdentityServerTools.cs` (nueva sobrecarga con tipo), `DefaultBackChannelLogoutService.cs` | `AccessTokenValidation.cs` (1 nuevo), `EndSessionTests.cs` (aserción de cabecera) |
| `cnf` en reference tokens | `TokenValidator.cs` | `AccessTokenValidation.cs` (1 nuevo) |
| Filtros de logging | `LoggingOptions.cs`, `TokenRequestValidator.cs` | `SensitiveValuesFilterTests.cs` (2 nuevos) |

Cada test nuevo se comprobó deshaciendo temporalmente su corrección: todos fallan sin ella y pasan con ella. Suite completa: 0 errores, 0 warnings, 1.015 tests por framework en verde.

**Cambios de comportamiento a comunicar:** clientes `client_credentials` con `RequireClientSecret=false` dejan de funcionar; la UI de error ya no recibe `RedirectUri`/`ResponseMode` para errores no seguros; los receptores de back-channel logout que validen `typ` deben aceptar `logout+jwt`; tras un login por nonce, el cliente destino puede pedir login si sus políticas (`max_age`, restricciones de IdP, `EnableLocalLogin`, `UserSsoLifetime`) no se cumplen.

### 3.5 Cambios aplicados en la fase 2

| Hallazgo | Cambio | Archivos principales |
|---|---|---|
| S2 mTLS | `MutualTlsSecretParser` sólo acepta el certificado si el esquema de certificados lo autenticó en un endpoint mTLS (marca puesta por `MutualTlsEndpointMiddleware`). `X509ThumbprintSecretValidator` acepta también thumbprints SHA-256 | `MutualTlsSecretParser.cs`, `MutualTlsEndpointMiddleware.cs`, `X509ThumbprintSecretValidator.cs` |
| S5 refresh tokens | Reuso de un refresh token rotado revoca los refresh tokens del mismo sujeto y cliente (hook virtual `OnConsumedTokenReusedAsync`). En el refresh se revalidan los scopes contra el cliente actual, se admite `scope` más estrecho (sin reducir el grant guardado) y cada access token recibe un `jti` nuevo | `DefaultRefreshTokenService.cs`, `TokenRequestValidator.cs`, `TokenResponseGenerator.cs` |
| S6 authorization code | El código canjeado se guarda como consumido hasta que caduca; un segundo canje se rechaza y revoca refresh y reference tokens del sujeto y cliente. Nuevo `AuthorizationCode.ConsumedTime` y método `IAuthorizationCodeStore.ConsumeAuthorizationCodeAsync` con implementación por defecto | `TokenRequestValidator.cs`, `DefaultAuthorizationCodeStore.cs`, `IAuthorizationCodeStore.cs` |
| S7 front channel | Retirados `code token` y `code id_token token`; el flujo hybrid ya no emite access tokens | `Constants.cs`, `AuthorizeResponseGenerator.cs` |
| S8 RFC 9207 | `iss` en respuestas de autorización (éxito y errores al cliente) y en discovery. Opción `EmitIssuerIdentificationResponseParameter`, activada por defecto | `AuthorizeResult.cs`, `AuthorizeResponseExtensions.cs`, `DiscoveryResponseGenerator.cs` |
| S9 `request_uri` | Debe estar registrada en el nuevo `Client.RequestUris` (el fragmento se ignora). El cliente HTTP no sigue redirecciones y sólo conecta a IPs públicas, resolviendo DNS en el momento de conectar; rangos bloqueados ampliados; los fallos de red devuelven error en vez de excepción | `AuthorizeRequestValidator.cs`, `JwtRequestUriHttpMessageHandler.cs` (nuevo), `DefaultJwtRequestUriHttpClient.cs`, `Additional.cs` |
| S10 device flow | Límite de búsquedas fallidas de `user_code` por usuario o IP (`DeviceFlowOptions.UserCodeMaxFailedAttempts`, 5 en 5 minutos). Los scopes consentidos deben estar entre los pedidos. La clave del throttle guarda el device code hasheado | `DefaultDeviceFlowInteractionService.cs`, `DistributedDeviceFlowThrottlingService.cs` |
| S11, S21 | Avisos al arrancar fuera de Development: sin `IssuerUri`, store de grants en memoria, `AddDeveloperSigningCredential` | `IdentityServerApplicationBuilderExtensions.cs`, `LoggerExtensions.cs` |
| S13–S24 | Ver la nota de cada punto en §3.2 | — |

Verificación: 0 errores, 0 warnings, 1.119 tests por framework en verde. Las 26 correcciones se comprobaron con mutaciones: cada una hace fallar al menos un test cuando se deshace.

**Desviaciones del plan, con motivo:**

- **`StrictJarValidation` sigue desactivado por defecto.** Activarlo rompería a cualquier cliente que envíe request objects con `typ: JWT`, y la ganancia es pequeña porque el `aud` ya distingue request objects de client assertions.
- **`IssuerUri` no es obligatorio en producción.** El issuer dinámico es legítimo detrás de proxies o con varios hosts; se emite un aviso en lugar de impedir el arranque.
- **`client_id` sin `id_token_hint` en end session no se implementó en esta fase.** Fijar el cliente sólo con `client_id` desactivaba la pantalla de confirmación (`ShowSignoutPrompt` dependía de que no hubiera cliente) y abría logout CSRF. Sólo se aplicó la regla obligatoria: si llegan ambos, deben coincidir. *Resuelto en la fase 5 (§3.7): la confirmación pasa a depender de que haya un `id_token_hint` válido.*
- **El consumo del authorization code no es atómico.** Hacerlo atómico exige cambiar el contrato de `IPersistedGrantStore`. Se detecta y revoca el replay secuencial, que es el caso habitual; dos canjes simultáneos exactos siguen siendo posibles.

**Cambios de comportamiento a comunicar:**

- Clientes hybrid con `code token` o `code id_token token` reciben `unsupported_response_type`.
- Un `prompt` desconocido (por ejemplo `create`) devuelve `invalid_request`.
- Las redirect URIs comparan path y query exactos; sólo esquema y host ignoran mayúsculas. Un `redirect_uri` con fragmento se rechaza.
- El validador AppAuth rechaza el puerto 0 y los fragmentos; una URI loopback registrada con path exige ese path.
- `request_uri` exige registrar la URI en `Client.RequestUris`.
- Tras POST, las redirecciones usan 303 en lugar de 302.
- Las respuestas de autorización incluyen `iss`.
- Reusar un refresh token rotado revoca los refresh tokens de ese usuario en ese cliente; un cliente que reintente con el token antiguo tras un fallo de red obliga al usuario a volver a iniciar sesión. Se puede ajustar sobrescribiendo `AcceptConsumedTokenAsync` u `OnConsumedTokenReusedAsync`.
- Un refresh falla con `invalid_scope` si el cliente perdió acceso a algún scope del grant.
- Los access tokens sin API resources llevan el `aud` estático `{issuer}/resources`.
- Un cliente mTLS que llamara a `/connect/token` directamente, sin pasar por el endpoint o dominio mTLS, deja de autenticarse por certificado.
- Enviar a la vez credenciales Basic y `client_secret` en el cuerpo devuelve `invalid_client`.

### 3.6 Qué aprender de los avisos de Duende si se implementan PAR o DPoP

- **PAR (GHSA-mxv6-xwqj-ww2p, CVSS 8.2):** el `client_id` empujado debe estar ligado al cliente autenticado (RFC 9126 §2.2); los clientes que no se autentican no pueden usar redirect URIs no registradas; las URIs no registradas deben ser `https` y pasar `InvalidRedirectUriPrefixes`. El estado de PAR debe consumirse también en caso de error.
- **DPoP (CVE-2024-49755):** comparar siempre `cnf.jkt` del access token con el thumbprint de la clave de la prueba, también en Local APIs.
- Añadir **listas de algoritmos permitidos** para JAR, client assertions y DPoP (Duende 7.3.0).

### 3.7 Cambios aplicados en la fase 5

| Pieza | Cambio | Archivos principales |
|---|---|---|
| `client_id` en end session | Sin `id_token_hint`, `client_id` identifica al cliente para validar `post_logout_redirect_uri` contra las URIs registradas. No autentica la petición: `LogoutRequest.ShowSignoutPrompt` sólo se salta con un `id_token_hint` válido (nuevo `ClientAuthenticated`). Cliente desconocido, deshabilitado o no OIDC devuelve error | `EndSessionRequestValidator.cs`, `LogoutRequest.cs`, `ValidatedEndSessionRequest.cs` |
| Resource indicators (RFC 8707) | `resource` en authorize (varios valores) y en token (uno). Cada valor debe ser una URI absoluta sin fragmento y nombrar un API resource de los scopes pedidos; si no, `invalid_target`. El `aud` del access token se limita a esos APIs. El código y el refresh token guardan los recursos autorizados, y un refresh puede pedir token para cualquiera de ellos. Nuevo `ApiResource.RequireResourceIndicator`: ese API sólo entra en el `aud` si se pide expresamente | `ResourceIndicators.cs` (nuevo), `DefaultResourceValidator.cs`, validadores de grant, `TokenResponseGenerator.cs`, `IReadableStringCollectionExtensions.cs` |
| PAR (RFC 9126) | Endpoint `/connect/par` con autenticación de cliente. Devuelve `urn:ietf:params:oauth:request_uri:…` de un solo uso (600 s por defecto). Lecciones de GHSA-mxv6-xwqj-ww2p: el `client_id` del cuerpo debe coincidir con el cliente autenticado, no se admiten redirect URIs sin registrar, `request_uri` en el cuerpo se rechaza y la petición se consume al responder al cliente, también con error. Durante login y consentimiento la URL de retorno sólo lleva `client_id` y `request_uri`; el estado procesado (por ejemplo `prompt`) se guarda en el store. `Client.RequirePushedAuthorization` y `PushedAuthorizationOptions.Required`, anunciado en discovery | `PushedAuthorizationEndpoint.cs`, `PushedAuthorizationReturnUrl.cs`, `DefaultPushedAuthorizationRequestStore.cs`, `AuthorizeRequestValidator.cs`, `AuthorizeEndpointBase.cs` |
| DPoP (RFC 9449) | Validación completa de la prueba: `typ`, algoritmo asimétrico permitido, `jwk` público, firma, `jti` contra replay, `htm`, `htu`, ventana de `iat` y `ath`. El access token lleva `cnf.jkt` y `token_type: DPoP`. `dpop_jkt` en authorize y la prueba en PAR ligan el código a la clave. Los refresh tokens de clientes públicos quedan ligados a la clave. `Client.RequireDPoP`. Lección de CVE-2024-49755: userinfo y Local API exigen el esquema `DPoP` y una prueba de la clave ligada, y rechazan un token ligado presentado como Bearer | `DefaultDPoPProofValidator.cs`, `DPoPRequest.cs`, `TokenEndpoint.cs`, `UserInfoEndpoint.cs`, `LocalApiAuthenticationHandler.cs` |
| Rotación automática de claves | Opcional (`KeyManagementOptions.Enabled`). Crea claves por algoritmo, las publica en el JWKS 14 días antes de firmar con ellas, las retira a los 90 días y las borra tras 14 días más. Clave privada cifrada con data protection. Store por defecto en disco (`FileSystemSigningKeyStore`); `ISigningKeyStore` permite otro compartido | `AutomaticKeyManager.cs`, `SigningKeyStores.cs`, `DefaultKeyMaterialService.cs` |

Verificación: 0 errores, 0 warnings y 1.212 tests por framework en verde; la integración (339 tests) pasa también con endpoint routing. Doce mutaciones sobre las protecciones nuevas, una por protección, hacen fallar al menos un test cada una: ligadura DPoP en recursos, `ath`, `dpop_jkt`, refresh de cliente público, replay, `client_id` en PAR, consumo de PAR, PAR de otro cliente, guardado del estado de PAR, filtrado de audiencia, subconjunto de recursos y confirmación de logout.

**No hecho, con motivo:**

- **Token exchange (RFC 8693).** El roadmap lo condicionaba a un caso de uso y no lo hay. Sin él habría que inventar la política de delegación e impersonación.
- **OpenID Conformance Suite en CI.** No hay CI en el repositorio: la solución referencia `deploy/pipelines/main.yml`, que no existe. Tampoco hay un host de ejemplo con login, que la suite necesita.
- **Nonces de servidor en DPoP (`DPoP-Nonce`).** Son opcionales; el replay se limita con `jti` y una ventana de `iat` de un minuto más un minuto de margen.
- **`resource` en device authorization y como array dentro de un request object.** Los tokens del device flow excluyen los APIs con `RequireResourceIndicator`.
- **Redirect URIs sin registrar en PAR (RFC 9126 §2.4).** Se dejan fuera a propósito: es la superficie del aviso de Duende.

**Limitaciones conocidas:**

- Con el router por middleware, la reescritura de `/connect/mtls/token` cambia el path que se compara con `htu`. Un cliente que combine DPoP con el path mTLS debe usar el dominio mTLS o `MapIdentityServer()`.
- Con varias instancias, el primer arranque puede crear una clave por instancia. Cada una ve las claves de las demás en `KeyCacheDuration` (1 hora). Es mejor arrancar primero una sola instancia o crear la clave antes del despliegue.

**Cambios de comportamiento a comunicar:**

- `LogoutRequest.ShowSignoutPrompt` es `true` salvo que la petición traiga un `id_token_hint` válido. Antes bastaba con que hubiera cliente.
- Discovery anuncia `pushed_authorization_request_endpoint` y `dpop_signing_alg_values_supported`. El endpoint PAR está activo por defecto (`EnablePushedAuthorizationEndpoint`).
- Un cliente que ya enviara un header `DPoP` recibe ahora tokens `DPoP` en lugar de Bearer.
- `IReplayCache` se registra siempre, no sólo con `AddJwtBearerClientAuthentication`.
- Contratos públicos: `EndSessionRequestValidator` recibe `IClientStore`, `IRefreshTokenService.CreateRefreshTokenAsync` acepta los recursos autorizados, y `IAuthorizeRequestValidator` e `ITokenRequestValidator` ganan sobrecargas con implementación por defecto. Hay un tipo nuevo de persisted grant, `pushed_authorization_request`, sin `SubjectId`.

---

## 4. Arquitectura y hosting

### 4.1 Cómo fluye una petición hoy

`UseIdentityServer()` (`Configuration/IdentityServerApplicationBuilderExtensions.cs:14-29`) monta: `Validate()` de arranque → `BaseUrlMiddleware` (copia `PathBase` a `HttpContext.Items`) → CORS → `UseAuthentication()` (configurable) → `MutualTlsEndpointMiddleware` → `IdentityServerMiddleware`.

`IdentityServerMiddleware` (`Hosting/IdentityServerMiddleware.cs:31-77`) resuelve por método-inyección `IEndpointRouter`, `IUserSession`, `IEventService` e `IBackChannelLogoutService` en **cada petición** que pasa por él, sea o no un endpoint del servidor; ejecuta `EnsureSessionIdCookieAsync()` (autentica la cookie en cada petición, incluidas las de máquina a `/connect/token`); registra un callback `OnStarting` que, si hubo sign-out, **envía las notificaciones de back-channel logout antes de escribir el primer byte de la respuesta** (clientes lentos retrasan la redirección del usuario); y por último `router.Find(context)` → `handler.ProcessAsync` → `result.ExecuteAsync`.

`EndpointRouter` (`Hosting/EndpointRouter.cs:16-55`) hace un recorrido lineal con comparación exacta de path, sin restricción de método HTTP; gana el primero registrado. Cada handler comprueba a mano método y content-type (13 sitios). Siete resultados resuelven sus dependencias con service locator en `Init(HttpContext)` y tienen constructores `internal` para tests. El JSON se escribe con System.Text.Json por reflexión sobre `Dictionary<string, object>` y tipos anónimos.

Detalles relevantes: `IEndpointResult.ExecuteAsync(HttpContext)` tiene exactamente la firma de `Microsoft.AspNetCore.Http.IResult`; el modo path de mTLS **reescribe `Request.Path`** (`MutualTlsEndpointMiddleware.cs:57-70`); `GetIdentityServerOrigin` recorta el subdominio con `StartsWith(DomainName)` sin comprobar el punto siguiente (`HttpContextExtensions.cs:36-46`), así que `mtlsfoo.example` se corrompe; `EnableNonceEndpoint` sólo lo respeta discovery, `IsEndpointEnabled` no tiene caso para Nonce (`EndpointOptionsExtensions.cs:7-19`) y el endpoint sigue sirviendo "deshabilitado"; el endpoint de nonce tampoco está en `CorsPaths`.

### 4.2 ¿Migrar a endpoint routing / Minimal APIs? Sí, de forma aditiva

**Veredicto:** merece la pena, pero como migración **aditiva**, no como reescritura. La capa de hosting es pequeña (~830 líneas en `Hosting/`, ~1.100 en `Endpoints/`, ~1.500 en `Results/` de las cuales ~300 son JavaScript embebido). El coste es casi todo compatibilidad de API pública, y dos decisiones lo neutralizan: `IEndpointResult : IResult` (misma firma, cero cambios) y un adaptador que mapea los descriptores `Endpoint` existentes, de modo que `AddEndpoint<T>` sigue funcionando.

**Qué se gana, concreto para este código:**

1. Eliminar las 13 comprobaciones manuales de método/content-type: `MapGet`/`MapPost` + `Accepts<T>` dan 405/415 gratis.
2. Políticas por endpoint que hoy son imposibles porque todo es un middleware opaco: `RequireRateLimiting` en `/connect/token` y `/connect/deviceauthorization` (resuelve S10 parcialmente), `CacheOutput` en discovery/jwks, `RequireCors` en lugar de la lista `CorsPaths`, `RequireAuthorization(política de certificado)` en lugar de reescribir el path para mTLS, `WithName`/`Produces`/OpenAPI.
3. `MapGroup("/connect/mtls")` con los mismos handlers elimina el modo path de `MutualTlsEndpointMiddleware`; `LinkGenerator.GetUriByName` sustituye la concatenación de strings en `DiscoveryResponseGenerator.cs:130-170`.
4. Resultados como datos puros (`TypedResults.Json(dto, JsonContext.Default.X)`, `TypedResults.Redirect`, `TypedResults.Content(html)`): desaparecen los siete resultados con service locator y sus constructores internos; serialización source-generated, segura para trimming/AOT.
5. Tests que afirman sobre `Results<Ok<TokenDto>, BadRequest<TokenErrorDto>>` en vez de montar `DefaultHttpContext` con un `IServiceProvider` falso.
6. Filtros de endpoint para el boilerplate repetido (log → validar → evento), para el interruptor de habilitado/deshabilitado (`IEndpointFilter` leyendo `IOptionsMonitor<IdentityServerOptions>`, que mantiene el comportamiento dinámico actual) y para `no-store`/CSP, hoy dispersos por los resultados.

**Qué rompe o hay que conservar:**

- API pública: `IEndpointHandler`, `IEndpointResult`, `IEndpointRouter`, `Hosting.Endpoint`, `AddEndpoint<T>` y los resultados públicos. No hay consumidores externos en el repo, pero es el punto de extensión documentado de IS4.
- `UseIdentityServer()` como pipeline autocontenido frente a `MapIdentityServer()` sobre `IEndpointRouteBuilder`, que requiere que el host coloque `UseRouting`/`UseAuthentication`/`UseAuthorization`. `IdentityServerMiddlewareOptions.AuthenticationMiddleware` pierde sentido. Los fixtures de integración usan `Configure` + `UseIdentityServer()` sin `UseRouting` (`IdentityServerPipeline.cs:58-60`).
- Lo transversal de `IdentityServerMiddleware` **no es por endpoint** y debe seguir siendo middleware: la sincronización de la cookie de sesión y el hook `OnStarting` de sign-out son lo que hace que un `SignOutAsync` desde un controlador de UI dispare el back-channel logout. Sólo la parte de dispatch es sustituible.
- mTLS en modo dominio (basado en host) no tiene equivalente limpio en routing: `RequireHost` admite `*.example.com`, no "prefijo `mtls.` del host que sea". Mantener el middleware para ese modo o escribir un `MatcherPolicy`.
- Paths duplicados: routing lanza `AmbiguousMatchException`; hoy gana el primero.
- La clase pública `Hosting.Endpoint` colisiona con `Microsoft.AspNetCore.Http.Endpoint` (de ahí el alias global en `_Imports.cs:60`); con routing la colisión se agudiza. Renombrar a `EndpointDescriptor`.

**Diseño recomendado:**

```csharp
public interface IEndpointResult : IResult { }   // sin cambios de código en los resultados

public static RouteGroupBuilder MapIdentityServer(this IEndpointRouteBuilder endpoints)
{
    var group = endpoints.MapGroup("");
    foreach (var ep in endpoints.ServiceProvider.GetServices<Endpoint>())   // AddEndpoint<T> sigue funcionando
    {
        group.Map(ep.Path, async ctx =>
        {
            var handler = (IEndpointHandler)ctx.RequestServices.GetRequiredService(ep.Handler);
            var result = await handler.ProcessAsync(ctx);
            if (result is not null) await result.ExecuteAsync(ctx);
        })
        .WithDisplayName(ep.Name)
        .WithMetadata(new IdentityServerEndpointMetadata(ep.Name))
        .AddEndpointFilter<EndpointEnabledFilter>();
    }
    return group;
}
```

Los endpoints integrados se mapean nativamente con restricciones de método y metadatos; CORS y mTLS pasan a metadatos (`RequireCors`, `MapGroup("/connect/mtls").RequireAuthorization(...)`); queda un `IdentityServerMiddleware` reducido (cookie de sesión + sign-out), idealmente enviando las notificaciones de back-channel a un `Channel<T>` consumido por un `BackgroundService` para no bloquear el inicio de la respuesta.

**Plan por fases:**

1. **Preparación, no rompe nada:** `IEndpointResult : IResult`; resultados usando `TypedResults` internamente; `JsonSerializerContext` para los DTOs; `EndpointRouter` singleton; resolver `IBackChannelLogoutService` de forma perezosa dentro del callback; añadir el caso `Nonce` a `IsEndpointEnabled`; eliminar código muerto.
2. **`MapIdentityServer()` aditivo;** `UseIdentityServer()` detecta endpoint routing (`app.Properties["__EndpointRouteBuilder"]`) y delega el dispatch, o mantiene el router legado. `[Obsolete] IEndpointRouter`. Fixture de integración basado en `UseRouting` junto al actual.
3. **Metadatos:** CORS y mTLS en modo path pasan a metadatos/grupos; `MutualTlsEndpointMiddleware` queda sólo para modo dominio; discovery vía `LinkGenerator`.
4. **Eliminación (siguiente major):** quitar `IEndpointRouter`, el matching por path y `BaseUrlMiddleware`; `UseIdentityServer` = middleware de sesión + `MapIdentityServer`.

### 4.2.1 Estado tras la fase 4

Aplicadas las fases 1 a 3 del plan anterior; la 4 (eliminación) queda para la siguiente versión mayor. Detalle en §4.5.

### 4.3 Inyección de dependencias y opciones

1. **`TimeProvider` nunca lo registra la librería.** `AddTimeProvider()` es un no-op en net10/net11 (`TimeProvider.cs:36-43`; el resto del archivo es `#if NET6_0`, código muerto, y declara un tipo público en `namespace System`). Se resuelve hoy sólo porque `AddAuthentication()` registra `TimeProvider.System` como efecto secundario. Cualquier composición que no llame a `AddCookieAuthentication` falla en tiempo de ejecución al resolver `DefaultUserSession` o `DefaultEventService`. Arreglo: `TryAddSingleton(TimeProvider.System)` en `AddRequiredPlatformServices` y borrar el archivo.
2. **Singleton mutable de `IdentityServerOptions` inicializado por efecto secundario.** Se registra como snapshot (`Core.cs:18-19`, con `Add`, no `TryAdd`) y `PostConfigureInternalCookieOptions` muta `UserInteraction.LoginUrl/LogoutUrl` la primera vez que se materializan las opciones de cookie (`ConfigureInternalCookieOptions.cs:80-84`). El `ValidateOptions` de arranque no puede validar `LoginUrl` y los valores dependen del orden de las peticiones. No existe ningún `IValidateOptions` en el proyecto. Sustituir por `IValidateOptions<IdentityServerOptions>` + `ValidateOnStart()` y resolver los valores por defecto en lectura.
3. **`IHttpContextAccessor` como dependencia universal** en 21 archivos, cuando la mayoría sólo quiere el issuer (`DefaultTokenService.cs:134,182`, `TokenValidator.cs:227`, `JwtRequestValidator.cs:31`, `PrivateKeyJwtSecretValidator.cs:68`, `LogoutNotificationService.cs:43`, `IdentityServerTools.cs:36`). `CorsPolicyProvider.cs:44` lo usa ignorando su propio parámetro `context`. Un `IIssuerNameService`/`IServerUrls` con scope elimina la mayoría y permite ejecutar back-channel logout fuera de una petición.
4. **Decoradores por reflexión** (`Core.cs:236-279` + `Decorator.cs`), duplicados literalmente en `IdentityServer.AspNetIdentity`. Usan `MakeGenericType` (IL3050 en AOT), leen `ImplementationInstance/Factory/Type` sin comprobar `IsKeyedService` (lanza con descriptores keyed en .NET 8+), y fallan si `AddIdentityServer()` se llama dos veces. Los keyed services (`AddKeyedScoped<IAuthenticationService>("inner")`) sustituyen todo el mecanismo.
5. **`Add` no idempotente** donde se espera `TryAdd`: `AddEndpoint<T>` añade un singleton y un transient por llamada (`Core.cs:81-82`); `AddJwtRequestUriHttpClient`/`AddBackChannelLogoutHttpClient` se llaman incondicionalmente y acumulan acciones de configuración; `AddTransient<IClientSecretValidator>`/`IApiSecretValidator` (`Core.cs:155-156`) usan `Add` mientras todo alrededor usa `TryAdd`.
6. **`AddDistributedMemoryCache()` forzado** (`Core.cs:159`): en despliegues multi-nodo el throttle de device flow, la caché de replay de `jti` y `DistributedCacheStateDataFormatter` quedan silenciosamente por nodo.
7. **Heurísticas sobre descriptores:** `AddInMemoryClients` inspecciona el último descriptor de `ICorsPolicyService` (`InMemory.cs:117-123`), dependiente del orden y roto con keyed services; `AddSingleton(clients)` registra `IEnumerable<Client>` y mezclar sobrecargas produce registros múltiples.
8. **Validación de arranque** resuelve stores desde un scope, compara tipos por `FullName` y bloquea con `GetAwaiter().GetResult()` (`IdentityServerApplicationBuilderExtensions.cs:41-57`). Un `IHostedService`/`IStartupFilter` o `ValidateOnStart` encajan mejor.
9. **Trimming/AOT:** STJ por reflexión con `Dictionary<string,object>` y tipos anónimos; `section.Bind(...)` y `Configure<IdentityServerOptions>(configuration)` necesitan `EnableConfigurationBindingGenerator`; `UseMiddleware<T>` con `Invoke` por método-inyección (convertir a `IMiddleware`). No declarar `IsTrimmable` hasta resolverlo.

### 4.4 Otros puntos arquitectónicos

- **`HttpContext.Items` como transporte de estado** (`idsvr:IdentityServerBasePath`, `idsvr:IdentityServerSignOutCalled`). `Request.PathBase` o una feature tipada son lo idiomático.
- **Estado estático:** `CheckSessionResult.FormattedHtml` cacheado a nivel de proceso para un único nombre de cookie (`CheckSessionResult.cs:15-17,38-52`); colecciones públicas mutables no `readonly` en `Constants.cs:70,104,244` y, lo más grave, `IdentityServerConstants.SupportedSigningAlgorithms` (`IdentityServerConstants.cs:90`) es un campo público mutable que cualquiera puede reemplazar. `DiscoveryOptions.CustomEntries` es un `Dictionary` compartido.
- **Coste por petición en peticiones ajenas:** cuatro resoluciones de servicio, una autenticación de cookie y un closure `OnStarting` para cada petición tras `UseIdentityServer`, incluidos archivos estáticos y la UI. Además `AuthenticationMiddleware` por defecto hace `UseAuthentication()`, de modo que el patrón habitual (el host también lo llama) autentica dos veces.
- **Sync-over-async:** `IdentityServerApplicationBuilderExtensions.cs:57`; `DistributedCacheStateDataFormatter.cs:61,91` (contrato sincrónico de `ISecureDataFormat` sobre Redis).
- **Federated sign-out:** el wrapper escribe HTML en una respuesta ya gestionada por el handler interno siempre que `StatusCode == 200` (`AuthenticationRequestHandlerWrapper.cs:29-34,70-81`), acoplamiento frágil a internos del handler.
- **`ProtectedResourceErrorResult`** (`:32,:39`): `string.Format($"...")` sobre un string ya interpolado (una `{` en `ErrorDescription` lanza `FormatException`), comillas sin escapar en una cabecera, campos públicos mutados durante `ExecuteAsync`.
- Sustituciones modernas que aplican: `HybridCache` para `DefaultCache<T>` y el throttle de device flow (no para `DistributedCacheStateDataFormatter`, que es sincrónico); `LinkGenerator`; `Channel<T>` + `BackgroundService` para back-channel logout; `TypedResults.Json` + `JsonSerializerContext`; `Results.Problem` sólo para 405/415/404 (los errores OAuth deben seguir siendo `error`/`error_description`); `crypto.subtle.digest` en lugar del SHA-256 en JavaScript de 2014 embebido en `CheckSessionResult.cs:66-223`.

---

### 4.5 Cambios aplicados en la fase 4

| Cambio | Detalle |
|---|---|
| `IEndpointResult : IResult` | Todo resultado de un endpoint handler es también un `IResult` de ASP.NET Core, sin cambios en las implementaciones existentes |
| `MapIdentityServer()` | Nuevo, aditivo. Mapea cada endpoint registrado (incluidos los de `AddEndpoint<T>`) y su alias `/connect/mtls/...`, con metadatos `IdentityServerEndpointMetadata`. Devuelve un `IdentityServerEndpointConventionBuilder`: las convenciones se aplican a todos los endpoints o, con `ForEndpoint(nombre)`, a uno y su alias mTLS. Paths duplicados: gana el primero registrado, como en el router. Endpoint deshabilitado: 404 |
| Nombres públicos | `IdentityServerConstants.EndpointNames` es público (antes sólo existía una copia interna) |
| `IdentityServerMiddleware` | Si la petición ya está enrutada a un endpoint mapeado, deja que lo ejecute el middleware de endpoints; si no, usa el router de siempre. Ya no resuelve router, eventos ni servicio de back-channel en cada petición: sólo cuando hacen falta. La ejecución de handlers se comparte entre ambos modos |
| mTLS | En modo routing, el alias `/connect/mtls/...` se autentica con el esquema de certificado sin reescribir `Request.Path`. Corregido de paso el recorte del subdominio mTLS en `GetIdentityServerOrigin`, que no comprobaba el punto (`mtlsfoo.example`) |
| Back-channel logout | Se envía desde un `BackgroundService` alimentado por un `Channel<T>` acotado, en lugar de bloquear el inicio de la respuesta de sign-out. Se capturan esquema, host y base path para resolver el issuer igual que en la petición original. Si la cola está llena se envía en línea. Opción `Authentication.SendBackChannelLogoutNotificationsInBackground`, activada por defecto |
| Router | `EndpointRouter` registrado como singleton (no tiene estado) |
| Renombrado | El paquete de modelos de aplicación pasa a llamarse `IdentityServer.ClientConfiguration` |

Verificación: 0 errores, 0 warnings, 1.150 tests por framework en verde (dos pasadas seguidas sin fallos). Toda la suite de integración (293 tests) pasa también con `IDENTITYSERVER_TEST_ENDPOINT_ROUTING=true`, que sirve los endpoints con `MapIdentityServer()`. Nuevos tests: endpoints mapeados, flujo de código, endpoint deshabilitado, convenciones por endpoint, alias mTLS, back-channel en segundo plano (issuer correcto y respuesta que no espera al cliente), y el primer test de integración de mTLS, en ambos modos.

**Cambios de comportamiento:** las notificaciones de back-channel logout llegan unos milisegundos después de la respuesta de sign-out en lugar de antes; un `IBackChannelLogoutService` personalizado que dependa del `HttpContext` real debe desactivar la opción. En modo routing, un endpoint deshabilitado responde 404 en vez de pasar la petición al siguiente middleware, y una barra final en el path ya no impide el match.

**No hecho, con motivo:**

- **Los resultados no usan `TypedResults` internamente.** Ya son `IResult`; reescribirlos no cambia el comportamiento y queda para la fase de `JsonSerializerContext`.
- **`IEndpointRouter` no se marca `[Obsolete]`.** El modo middleware sigue siendo el predeterminado y se mantiene hasta la siguiente versión mayor.
- **CORS sigue basado en `CorsPaths`.** Funciona igual en ambos modos; pasarlo a metadatos (`RequireCors`) es opcional.
- **Discovery no usa `LinkGenerator`.** Las URLs siguen construyéndose como antes, lo que es correcto en ambos modos.
- **La cola de back-channel no se vacía al apagar el host.** Las notificaciones pendientes en ese momento se pierden.

## 5. Calidad del código y modernización

### 5.1 Build, empaquetado y CI (nada es distribuible hoy)

| Falta | Detalle | Propuesta |
|---|---|---|
| Historial y CI | Sin commits; `IdentityServer.slnx:7-8` referencia `deploy/pipelines/main.yml`, que no existe | Primer commit; pipeline con `dotnet build -warnaserror`, `dotnet test`, `dotnet pack`, publicación a feed interno |
| Versionado | Ningún `<Version>`, MinVer ni NBGV; `README.md:37` aún acredita MinVer; el arranque loguea `AssemblyInformationalVersion` = "1.0.0" para siempre | MinVer por tags como `GlobalPackageReference` |
| Metadatos de paquete | Sin `PackageId`, `Authors`, `Description`, `PackageLicenseExpression`, `RepositoryUrl`, `PackageReadmeFile`. `dotnet pack` generaría paquetes genéricos `IdentityServer.*` **y también `IdentityServer.Test`** (sin `IsPackable=false`) | Prefijo propio (p. ej. `IdentityServer.*`), `IsPackable=false` en `IdentityServer.Test` |
| Licencia | No hay `LICENSE` ni `NOTICE`; Apache-2.0 obliga a conservar la atribución a Brock Allen y Dominick Baier | Añadir ambos antes de distribuir |
| SourceLink / reproducible | Sin `Microsoft.SourceLink.*`, `PublishRepositoryUrl`, `EmbedUntrackedSources`, `ContinuousIntegrationBuild`, `IncludeSymbols`/`snupkg` | Todo en `Directory.Build.props` |
| XML docs | `CS1591` suprimido pero `GenerateDocumentationFile` nunca se activa: supresión inerte y sin IntelliSense para consumidores. ~90 % de los miembros públicos ya tienen `///` | Activarlo en `src/*` |
| Strong naming | IS4 4.x estaba firmado; quitarlo rompe `InternalsVisibleTo`/binding redirects de consumidores | Documentarlo en la guía de migración |
| Analizadores | `.editorconfig:237` usa `[test/**]` pero la carpeta es `tests/` (nunca aplica); IDE0005/0059/0060 a `error` sin `EnforceCodeStyleInBuild` (sólo en IDE); reglas VSTHRD/ASYNC configuradas sin referenciar esos paquetes; `CA1848` suprimido en NoWarn y a `warning` en editorconfig (gana NoWarn); `end_of_line = crlf` con 82 archivos LF | `TreatWarningsAsErrors`, `EnforceCodeStyleInBuild`, `AnalysisMode=Recommended`, arreglar el glob, `dotnet format whitespace` en un único commit |
| API pública | 397 tipos públicos, sin seguimiento de cambios | `Microsoft.CodeAnalysis.PublicApiAnalyzers` con `PublicAPI.Shipped.txt` sembrado desde IS4 4.1.x: el delta es la guía de migración |
| Runner de tests | `UseMicrosoftTestingPlatformRunner` sin `dotnet.config`: `dotnet test` falla con el SDK 10 | `dotnet.config` con `[dotnet.test.runner] name = "Microsoft.Testing.Platform"` |
| Multi-target | `net10.0;net11.0` para todo, tests incluidos (se ejecutan dos veces); sin `global.json` | Valorar sólo `net10.0` (LTS); si se mantiene net11, fijar SDK en `global.json` |

**Dependencias.**
- `IdentityModel 7.0.0` es la última versión bajo ese id; el sucesor es `Duende.IdentityModel` (sigue siendo Apache-2.0, misma API, namespace `Duende.IdentityModel`). Es dependencia de la API pública de `IdentityServer.Models` (13 usos), así que migrar es un cambio de código fuente para consumidores: hacerlo en la primera versión numerada, o internalizar las pocas constantes y eliminar la dependencia de Models. Está declarado dos veces en los grupos condicionales por TFM (`Directory.Packages.props:6,11`).
- **FluentAssertions 8.11.0:** desde 8.0 (enero 2025) está bajo la Xceed Community License, gratuita para OSS y **de pago por desarrollador para uso comercial**. Para un fork corporativo hay que comprar licencia o cambiar: fijar `7.2.0` (Apache-2.0, API idéntica para lo que se usa) o `AwesomeAssertions` (fork Apache-2.0, mismo namespace). Con los global usings de `_Imports.cs` el cambio es de dos líneas por proyecto.
- `Microsoft.IdentityModel.* 8.23.0`: correcto. El código sigue usando `JwtSecurityTokenHandler` en 4 sitios; Microsoft recomienda `JsonWebTokenHandler`. No urgente.
- Sin paquetes con avisos de vulnerabilidad (verificado con `dotnet list package --vulnerable --include-transitive`).

### 5.2 Modernización del lenguaje y runtime (sólo donde compensa)

**Nullable reference types (mayor retorno en corrección).** 0 anotaciones hoy. Orden sugerido: `IdentityServer.Storage` (512 líneas, medio día; aquí se decide `Task<Client?>` en `IClientStore.FindClientByIdAsync`, el contrato del que dependen todos los stores) → `IdentityServer.Models` (1 día) → `IdentityServer.AspNetIdentity` (horas) → core (2–4 semanas archivo a archivo con `#nullable enable`, empezando por `Extensions/` y `Validation/Models/`). Bugs que destaparía: `PrincipalExtensions.cs:38,66,140,178,193,217` hacen `identity as ClaimsIdentity` y desreferencian (NRE con identidades que no son de claims); `RefreshToken.cs:76-108` desreferencia `AccessToken` que admite null.

**TimeProvider: inyectado pero con 7 rodeos.** `Message.cs:12` (constructor con `DateTime.UtcNow`, documentado "sólo para tests" pero usado en producción desde `LoginPageResult.cs:52`, `ConsentPageResult.cs:52`, `CustomRedirectResult.cs:63` y dos message stores); `AuthorizeInteractionResponseGenerator.cs:197`; `GrantValidationResult.cs:77`; `LegalTerm.cs:28,40`; `LegalTermCountry.cs:22,28`. En tests, `MockSystemClock` declara `UtcNow` pero nunca sobrescribe `GetUtcNow()`: es inerte, y `DefaultTokenServiceTests` corre contra la hora real. Usar `Microsoft.Extensions.TimeProvider.Testing.FakeTimeProvider`.

**Logging source-generated: migración a medias.** 112 métodos `[LoggerMessage]` frente a 338 llamadas directas en ~60 archivos (`AuthorizeInteractionResponseGenerator` 27, `DefaultRefreshTokenService` 20, `TokenRequestValidator` 16). `InMemoryCorsPolicyService.cs:44,48` usa `{0}` posicional (rompe el logging estructurado). `TokenRequestValidator.LogWithRequestDetails` (596-623) y `AuthorizeRequestValidator` (703-714) construyen y serializan el objeto de log en cada llamada sin comprobar `IsEnabled`. Terminar la migración y quitar `CA1848` de NoWarn.

**System.Text.Json source generation.** Tres singletons de opciones (`ObjectSerializer.cs:5`, `LogSerializer.cs:8`, `PersistentGrantSerializer.cs:9`) y 12 sitios por reflexión. Un `JsonSerializerContext` para las formas de grants persistidos es la única vía a trimming. `DiscoveryDocument.cs:5-24` y `JsonWebKey.cs:5-16` usan nombres de propiedad en minúscula/snake_case para dirigir el JSON: usar `[JsonPropertyName]` o `JsonNamingPolicy.SnakeCaseLower`.

**Colecciones y crypto.** 33 `static readonly List<>/Dictionary<>` en `Constants.cs` consultados en cada petición → `FrozenSet`/`FrozenDictionary`. 8× `SHA256.Create()` → `SHA256.HashData`. 19 usos de `IdentityModel.Base64Url` → `System.Buffers.Text.Base64Url` (in-box desde .NET 9). `TimeConstantComparer.IsEqual` → `CryptographicOperations.FixedTimeEquals`. `CryptoHelper.cs:74` parsea los últimos 3 caracteres con `Substring` + `int.Parse` → `switch`. `GrantTypes.cs:5-24` asigna un array nuevo en cada acceso.

**Records, `init`, `sealed`.** 0 `sealed`, 0 `record`. Candidatos a record con igualdad por valor ya escrita a mano: `ClientClaim.cs:55-80`, `Secret.cs:80-108`, `ClaimLite`, `ClaimsPrincipalLite`, `PersistedGrantFilter`, `SecurityKeyInfo`, `Hosting/Endpoint`. Los 21 eventos exponen `{ get; set; }` pero se fijan en el constructor: `init`. **No** convertir `Client`/`Resource`/`ApiScope` en records: se enlazan desde `IConfigurationSection`. Sellar los 87 tipos internos (CA1852) y los públicos no extensibles; dejar los `Default*` sin sellar porque están diseñados para `virtual`. `Client.ValidateGrantTypes` (`Client.cs:310-333`) enumera la entrada hasta 5 veces y `GrantTypeValidatingHashSet.Add` clona el set en cada inserción: O(n²).

### 5.3 Smells de diseño

**Tipos duplicados entre ensamblados (la limpieza más valiosa).** `IdentityServerUser` existe en `IdentityServer.Models` (internal, 89 líneas) y en `IdentityServer` (public), copia literal, sólo porque `RefreshToken.cs:46-60` construye el principal. `Constants` e `IdentityServerConstants` están definidos dos veces en el mismo namespace (`SecretTypes.SharedSecret` y `ProtocolTypes.OpenIdConnect` pueden divergir). `StringExtensions` existe dos veces en `IdentityServer.Extensions` (y el archivo se llama `StringsExtensions.cs` en ambos). `Decorator<T>` y `AddDecorator` están copiados en `IdentityServer.AspNetIdentity`.

**Código de negocio dentro del framework.** `IdentityServer.Models` incluye `ClientPropertyNames.cs` (22 claves como `forgotPasswordMethods`, `subscribeNewslettersEndpoint`), `ClientPropertyValues.cs`, `LegalTerm.cs`, `LegalTermCountry.cs`, `LegalVersion.cs`, `MarketingNotification.cs`, `SupportedCountry.cs`, `LoginProviders.cs`. **Cero referencias desde `src/`**; sólo las toca un test. Pertenecen a la aplicación host o a un paquete `*.Models`. Además `LegalTerm.cs:20` compara códigos de país con `InvariantCultureIgnoreCase` (debería ser `OrdinalIgnoreCase`).

**Código muerto y restos de grants eliminados.** `ProfileIsActiveCallers.ResourceOwnerValidation`, `ProtocolTypes.WsFederation/Saml2p`, `InputLengthRestrictions.UserName/Password`, `ValidatedTokenRequest.UserName`, `TokenRequestValidationLog.UserName` (nunca asignados); 5 de 6 métodos `[LoggerMessage]` de AspNetIdentity sin llamadas (restos del `ResourceOwnerPasswordValidator<TUser>` borrado); `SubjectTypes.Ppid` y `Client.PairWiseSubjectSalt` nunca se leen pero discovery anuncia `"pairwise"` (`Constants.cs:70`): se publicita una funcionalidad no implementada; miembros `[Obsolete]` conservados; `#if NET6_0` en tres archivos; `AuthorizeEndpointBase.cs:61-64` tiene un `if (request.IsNoncePatternFlow) { }` vacío; `NonceEndpoint.cs:32` loguea `InvalidHttpRequestForTokenEndpoint` (copy-paste) mientras `InvalidHttpRequestForNonceEndpoint` no se usa; `IdentityTokenIssued` definido dos veces en dos `partial class LoggerExtensions` de namespaces distintos. Comentarios obsoletos en `Client.cs:72`, `AuthorizationNonce.cs:57`, `DeviceAuthorizationSuccessEvent.cs:4`, `InvalidClientConfiguration.cs:4,10`. 22 apariciones de `https://idsvr4` en tests de integración.

**God classes.** `AuthorizeRequestValidator.cs` 714 líneas (`ValidateOptionalParametersAsync` 170 líneas). `TokenRequestValidator.cs` 630 líneas y **con estado**: `_validatedRequest` (línea 48) se muta desde todos los métodos privados, de modo que la corrección depende del registro transient; comprueba la caducidad del code dos veces (199 y 209). El `switch` de 116-128 ya despacha por grant: dividir en validadores por grant (`IGrantTypeValidator`). `DiscoveryResponseGenerator` 439, `TokenValidator` 432, `Additional.cs` 424, `TokenResponseGenerator` 412, `Client.cs` 410 (~45 propiedades + 9 colecciones).

**Excepciones y flujo de control.** 10× `throw new Exception(...)` (`IResourceStoreExtensions.cs:37-63`, `TokenExtensions.cs:78-115`, `ProfileService.cs:61-115`) → `InvalidOperationException` o una `InvalidConfigurationException`. `ClaimsExtensions.cs:74` `catch { }` traga errores JSON. `StringsExtensions.cs:241-248` try/catch alrededor de `new Uri` → `Uri.TryCreate`.

**Colecciones públicas mutables.** Todas las colecciones de `Client`, `Resource`, `ApiResource`, `Token`, `Resources`, `IdentityServerUser` son `ICollection<T> { get; set; } = new HashSet<T>()`. `Token.Scopes` (`Token.cs:138`) es una consulta LINQ reevaluada en cada acceso; `Token.SubjectId`/`SessionId` usan `SingleOrDefault` y lanzan con claims `sub`/`sid` duplicadas (alcanzable desde un profile service). Los DTOs de configuración necesitan setters para `Bind`; los modelos de grant en runtime deberían ser `init` + `IReadOnlyCollection`.

**Nombres y ubicación.** Clases estáticas con nombre de interfaz (`IEnumerableExtensions`, `IResourceStoreExtensions`, `ICacheExtensions`, …). Extensiones en el namespace equivocado para su carpeta (`ResourceExtensions`, `HashExtensions`, `ClientExtensions` en `IdentityServer.Models`; `AuthenticationManagerExtensions` en `Microsoft.AspNetCore.Http` dentro de `HttpContextAuthenticationExtensions.cs`). Métodos `Task` sin sufijo `Async` (`IUserSessionExtensions.GetLogoutNotificationContext`, `IResourceStoreExtensions.CreateResourceValidationResult`). Archivos con varios tipos (`LogoutRequest.cs`, `ConsentResponse.cs`, `Enums.cs`, `InvalidClientConfiguration.cs`). Magic strings: `"UI"`, `"scope"`, nombres de cabecera literales en `HttpResponseExtensions.cs:27-107` (usar `HeaderNames`), `"Certificate"` en `MtlsOptions.cs:16`.

**AspNetIdentity.** `ProfileService<TUser>` tiene dos constructores y el `Logger?.` de la línea 170 es defensa muerta. `AddAspNetIdentity` fuerza `SameSite=None` en las cookies de aplicación y externa (`IdentityServerBuilderExtensions.cs:37,43`): decisión relevante para seguridad que debería ser opt-in y documentada. `UserClaimsFactory.cs:27` sustituye la claim `name` de Identity por `preferred_username` sin documentar ni probar. **El proyecto entero no tiene tests.**

---

### 5.4 Cambios aplicados en la fase 3

| Bloque | Cambio |
|---|---|
| Código muerto | Eliminados el shim `TimeProvider.cs` y los bloques `#if NET6_0`, `SubjectTypes`, `Client.PairWiseSubjectSalt`, `Constants.SupportedSubjectTypes`, `InputLengthRestrictions.UserName/Password`, `ValidatedTokenRequest.UserName`, `ProfileIsActiveCallers.ResourceOwnerValidation`, los dos `PrincipalExtensions.GetName` obsoletos, la constante `IdentityServerOrigin`, cinco logs sin uso de AspNetIdentity, un log duplicado y otro sin uso, un `if` vacío, código comentado, una comprobación nula imposible y comentarios copiados que no correspondían |
| Bugs de paso | `EnableNonceEndpoint = false` ahora deshabilita el endpoint (antes sólo lo ocultaba en discovery). `NonceEndpoint` registra su propio mensaje de error en vez del del token endpoint |
| Relojes | `TimeProvider.System` se registra explícitamente; ya no depende de que `AddAuthentication()` lo registre de rebote. Los mensajes de login, consent, redirect personalizado y los message stores usan el reloj inyectado |
| Modelos de negocio | `ClientPropertyNames`, `ClientPropertyValues`, `LegalTerm`, `LegalTermCountry`, `LegalVersion`, `MarketingNotification`, `SupportedCountry` y `LoginProviders` pasan al nuevo proyecto `IdentityServer.ClientConfiguration`, con el mismo namespace para no obligar a cambiar código. `LegalTerm` compara códigos de país con `OrdinalIgnoreCase` |
| Tipos duplicados | `IdentityServerUser` existe una sola vez, público, en `IdentityServer.Models` (mismo namespace `IdentityServer`). Las constantes privadas de Models pasan a una única clase interna `ModelConstants`, con tests que comprueban que coinciden con las públicas. Los helpers de cadenas y colecciones de Models son internos y con nombre propio; el núcleo tiene sus propios `IsMissing`/`IsPresent` |
| Decoradores | Keyed services: el registro original se mueve bajo una clave y el decorador lo recibe con `[FromKeyedServices]`. Desaparecen `Decorator<T>`, `MakeGenericType` y la copia duplicada en AspNetIdentity, que ahora reutiliza el helper del núcleo vía `InternalsVisibleTo` |
| Issuer | Nuevo `IIssuerNameService` con `DefaultIssuerNameService`: `IssuerUri` si está configurado, si no el origen de la petición. Lo usan `IdentityServerTools`, `DefaultTokenService`, `TokenValidator`, `JwtRequestValidator`, `PrivateKeyJwtSecretValidator`, `LogoutNotificationService` y el back-channel logout. Con `IssuerUri` configurado se pueden emitir tokens fuera de una petición HTTP. Discovery y la respuesta de authorize siguen leyendo el `HttpContext`, donde siempre existe |
| `TokenRequestValidator` | Dividido en un validador por grant (`AuthorizationCodeGrantValidator`, `ClientCredentialsGrantValidator`, `RefreshTokenGrantValidator`, `DeviceCodeGrantValidator`, `ExtensionGrantTokenRequestValidator`), un `TokenRequestScopeValidator` compartido y un `TokenRequestValidationContext` por petición. Ya no guarda estado en campos. El detalle de log sólo se serializa si el nivel está activo. Constructor y registro en DI sin cambios |
| Tests | El validador estático compartido entre clases de test paralelas se registra ahora por host. `MockSystemClock`, que nunca sobrescribía `GetUtcNow`, se elimina. El pipeline de integración y las clases de test que crean hosts liberan host, `TestServer` y clientes HTTP (28 archivos). Nuevos tests para el helper de decoradores, `DefaultIssuerNameService`, el registro de `TimeProvider`, `EnableNonceEndpoint` y la coherencia de constantes |

Verificación: 0 errores, 0 warnings, 1.134 tests por framework en verde; `dotnet pack` genera los cinco paquetes de `src/`.

**Cambios incompatibles de API pública** (no hay ninguna versión publicada todavía, pero afectan a código que extienda la librería):

- Constructores que reciben `IIssuerNameService` en lugar de `IHttpContextAccessor`: `IdentityServerTools` (que además recibe `IdentityServerOptions`), `JwtRequestValidator`, `PrivateKeyJwtSecretValidator` y `LogoutNotificationService`. `DefaultTokenService` recibe `IIssuerNameService` como parámetro adicional.
- `LocalApiAuthenticationHandler` ya no recibe `TimeProvider`.
- Eliminados: `Client.PairWiseSubjectSalt`, el enum `SubjectTypes`, `InputLengthRestrictions.UserName` y `.Password`, `ValidatedTokenRequest.UserName`, `PrincipalExtensions.GetName`, `ProfileIsActiveCallers.ResourceOwnerValidation` y `TimeProviderExtensions.AddTimeProvider`.
- `IdentityServer.Models` ya no expone `IsMissing`/`IsPresent` públicos.
- Los modelos de negocio requieren referenciar el paquete `IdentityServer.ClientConfiguration`.
- `IdentityServerUser` pasa del ensamblado `IdentityServer` a `IdentityServer.Models`, con el mismo namespace.

**No hecho, con motivo:**

- **`FakeTimeProvider` no se adoptó.** `StubClock` ya cubre el caso sin añadir una dependencia, y los `DateTime.UtcNow` de los fixtures de persisted grants son coherentes entre sí.
- **`IValidateOptions` y el singleton mutable de `IdentityServerOptions` (§4.3) siguen pendientes.** Pertenecían a la fase 1 y no los cubrió la sesión que la ejecutó. *`IValidateOptions` se resolvió en la fase 6 (§5.5).*
- **Discovery y la respuesta de authorize siguen calculando el issuer desde la petición.** Siempre corren dentro de una, y la migración a endpoint routing de la fase 4 es el momento natural para inyectar el servicio.

### 5.5 Cambios aplicados en la fase 6

| Bloque | Cambio |
|---|---|
| Logging | Las 362 llamadas directas a `ILogger` de `src/` pasan por 339 métodos `[LoggerMessage]` generados, en cuatro clases de `Logging/` (`RequestValidationLog`, `SecretLog`, `ServicesLog`, `ResponseAndHostingLog`) con rangos de EventId propios (0x6000–0x67FF). Los placeholders posicionales `{0}` pasan a nombrados y los objetos de detalle sólo se construyen si el nivel está activo. CA1848, CA1873 y CA2253 son errores en `src/` |
| Nullable | Activado en los cinco proyectos de `src/`, sin ningún `#nullable disable`. Los stores devuelven `Task<T?>` en búsquedas que pueden fallar, los valores opcionales de protocolo y configuración son `string?`, y `= default!` se usa sólo en propiedades que siempre se rellenan antes de leerse. `IsMissing`, `IsPresent`, `IsNullOrEmpty`, `IsAuthenticated` y `ValidationResult.IsError` llevan `[NotNullWhen]`/`[MemberNotNullWhen]`. Donde antes había una `NullReferenceException` posible, ahora hay una `InvalidOperationException` con mensaje (URLs de UI sin configurar, generador de user codes inexistente, mensaje de error que no se pudo guardar) |
| Serialización | `PersistentGrantSerializerContext` genera en compilación los contratos JSON de los grants persistidos. Un test compara la salida carácter a carácter con el serializador por reflexión anterior y comprueba que lee grants antiguos; los tipos de grant propios siguen pasando por reflexión |
| Colecciones y crypto | Las tablas de `Constants` que sólo se consultan son `FrozenSet`/`FrozenDictionary`; las que fijan el orden de discovery siguen siendo arrays. `SHA256.HashData` en lugar de `SHA256.Create()`, `System.Buffers.Text.Base64Url` en lugar del de IdentityModel, `CryptographicOperations.FixedTimeEquals` en lugar de `TimeConstantComparer`, y `CryptoHelper` deja de parsear el algoritmo con `Substring` + `int.Parse`. `GrantTypeValidatingHashSet` y los tipos internos señalados quedan `sealed` |
| Analizadores | De las 36 reglas rebajadas temporalmente en `.editorconfig`, 33 vuelven a ser errores: rendimiento, globalización, uso y documentación. Quedan tres como sugerencia porque arreglarlas cambia la API pública: CA1051 (campos `protected`), CA1716 (nombres que coinciden con palabras clave) y CA1822 (miembros públicos que podrían ser `static`). `tests/.editorconfig` relaja las que no aportan en tests |
| Opciones | `IdentityServerOptionsValidator` con `IValidateOptions` y `ValidateOnStart`: un issuer que no es URL absoluta, duraciones o límites no positivos, algoritmos DPoP simétricos y una gestión de claves incoherente impiden arrancar el host |
| Tests | Snapshots del documento de discovery y de la forma del access token (`Snapshots/*.verified.json`). Stryker.NET como herramienta local con `stryker-config.json` |
| Formato | Todos los `.cs` de `src/` y `tests/` con CRLF, como declara `.editorconfig`: había 292 archivos en LF |

Verificación: 0 errores, 0 warnings y 1.238 tests por framework en verde en dos pasadas; la integración (349 tests) pasa también con endpoint routing y `dotnet pack` genera los cinco paquetes. Una ejecución de Stryker acotada a `DefaultDPoPProofValidator` dio un 52 %; los mutantes sin cobertura señalaron siete casos sin test (prueba malformada, algoritmo simétrico, sin `jwk`, `jti`, `iat` o `htm`, `htu` con otro esquema o puerto, `ath` de otro token). Con esos tests sube al 55 %, y lo que sobrevive son textos de error, llamadas de log y mutantes equivalentes.

**Cambios de API pública** (anotaciones nullable aparte, que no cambian el binario):

- `IAuthorizationParametersMessageStore.DeleteAsync`, `IMessageStore<T>.ReadAsync` e `IEndSessionRequestValidator.ValidateAsync` aceptan `null`; `IConsentMessageStore.ReadAsync` e `IMessageStore<T>.WriteAsync` pueden devolver `null`.
- `ProfileDataRequestContext.RequestedResources` y `.ValidatedRequest` son anulables: un `IProfileService` propio debe comprobarlos.
- `IClaimsService.GetAccessTokenClaimsAsync` recibe un sujeto anulable (client credentials).
- Seis parámetros renombrados para coincidir con su interfaz (CA1725), por ejemplo `TokenResponseGenerator.ProcessAsync(validationResult)`; afecta sólo a llamadas con argumentos nombrados.
- Algunos mensajes de log de interacción cambian de texto ("Error: …" pasa a "Interaction error: …", porque el generador rechaza el prefijo en mensajes de nivel Information) y tienen EventIds nuevos.

**No hecho, con motivo:**

- **Verify.** La versión actual exige declarar en el build una licencia de patrocinio o una exención (código abierto, ingresos pequeños). Decidirlo es una cuestión legal del propietario, como la licencia de FluentAssertions de §6. Los snapshots usan un helper propio de 60 líneas con el mismo flujo (`.verified.json` / `.received.json`); cambiar a Verify es directo si se declara la licencia.
- **Records e `init`.** `Secret` y `ClientClaim` tienen una igualdad deliberadamente parcial (sólo valor y tipo) que un record cambiaría; `Client`, `Resource` y los eventos se enlazan o se enriquecen después de construirse, y pasar sus setters a `init` rompería a quien los modifica.
- **`IdentityServerOptions` sigue siendo un singleton mutable.** Muchos tests y hosts cambian opciones después de arrancar; se valida al inicio, pero no se congela.
- **Ejecución completa de Stryker.** Con 1.200 tests por mutante tarda horas; la configuración se probó con una ejecución acotada (ver README).

### 5.6 Cambios aplicados después de la fase 6

| Petición | Cambio |
|---|---|
| Borrar `IdentityServer.ClientConfiguration` | Proyecto, test y referencias eliminados. Los modelos de negocio pertenecen a la aplicación host |
| Sólo endpoint routing | Eliminados `IEndpointRouter`, `EndpointRouter`, el despacho en `IdentityServerMiddleware` y la reescritura de `/connect/mtls/...`. `UseIdentityServer()` es sólo middleware; `MapIdentityServer()` sirve los endpoints y `AddIdentityServer()` registra el routing. Sin `MapIdentityServer()`, la primera petición falla con un `InvalidOperationException` explicativo. La limitación de DPoP con el path mTLS de §3.7 desaparece |
| `prompt` extensible | `UserInteraction.PromptValuesSupported` (publicado como `prompt_values_supported`), `IAuthorizePromptHandler`, `AddPromptHandler<T>(prompt)` (keyed services) y `AddPromptRedirect(prompt, url)`. Los manejadores corren antes de login y consentimiento; si piden interacción, el prompt se quita de la URL de retorno |
| Flujos deprecados tras flags | Implicit (`AllowImplicitFlow`), access tokens desde authorize, también en hybrid (`AllowAccessTokensViaBrowser`), y password (`AllowResourceOwnerPasswordFlow` + `IResourceOwnerPasswordValidator`), todos desactivados por defecto. Un cliente con el grant y sin el flag no supera la validación de configuración, y los endpoints lo vuelven a comprobar. No se anuncian en discovery. `password` se filtra de los logs. Validadores para usuarios de test y para ASP.NET Identity |
| Paquete propio de IdentityModel | `IdentityServer.IdentityModel` (namespace propio para no chocar con `IdentityModel`/`Duende.IdentityModel` en los hosts) con constantes y helpers derivados de IdentityModel 7.0.0. Los tests de integración llevan un cliente de protocolo mínimo portado en `Common/ProtocolClient`. Atribución en `NOTICE` |
| Quitar FluentAssertions | 2.603 aserciones convertidas a `Assert` de xUnit con un script, más las conversiones manuales. Resuelve la licencia de §5.1 |
| CHANGELOG | `CHANGELOG.md` en inglés con los cambios respecto a IdentityServer4 4.1.2 |

**Cambios de comportamiento:** la reescritura mTLS ya no existe (los alias se mapean como endpoints); un `prompt` registrado (por ejemplo `create`) deja de dar `invalid_request`; `code token` y `code id_token token` vuelven a existir, pero sólo con `AllowAccessTokensViaBrowser` (antes `unsupported_response_type`, ahora `unauthorized_client` sin el flag).

## 6. Suite de tests

**Forma.** 801 tests (580 unitarios, 221 de integración), xunit.v3 + Microsoft.Testing.Platform, mocks escritos a mano (33 en `UnitTests/Common/`, sin Moq/NSubstitute: bien). `IdentityServerPipeline.cs` (440 líneas) levanta un `TestServer` en proceso con páginas falsas de login/consent/logout; `BrowserClient` mantiene cookies y sigue redirecciones.

**Sin cobertura.** `TokenResponseGenerator` (el componente que emite cada respuesta de token), `DiscoveryResponseGenerator`, `IntrospectionResponseGenerator`, `TokenRevocationResponseGenerator` (sólo indirectamente), `ApiSecretValidator`, `DefaultEventService`/`DefaultEventSink`, `DefaultProfileService`, `DefaultBackChannelLogoutService`, `DefaultDeviceFlowInteractionService`, `DefaultCache`, los tres `Caching*Store` (sólo se prueba el registro), `OidcReturnUrlParser`, `IdentityServerAuthenticationService`, `IdentityServerTools`, `DistributedCacheStateDataFormatter`, `MutualTlsEndpointMiddleware`, **todo `Hosting/LocalApiAuthentication/`**, **todo `IdentityServer.AspNetIdentity`**, y los hallazgos S1 (caducidad y cliente del nonce). Cobertura fina en `StrictRedirectUriValidator`, validadores X509, `DefaultKeyMaterialService`, `UserInfoEndpoint`, `EndSessionEndpoint`.

**Fragilidad.**
- `tests/IdentityServer.IntegrationTests/Clients/Setup/Startup.cs:5`: `static public ICustomTokenRequestValidator CustomTokenRequestValidator` es **estado estático mutable** escrito por `CustomTokenRequestValidatorClient` y leído por todas las clases que arrancan `Startup`; xunit v3 paraleliza clases por defecto, así que otros tests pueden ver un validador ajeno. Necesita `Startup` por instancia o `[Collection]`.
- `MockSystemClock` inerte y `StubClock` con hora real; 56 `DateTime.UtcNow` en `DefaultPersistedGrantServiceTests`, 29 en `DefaultRefreshTokenServiceTests`, 17 en `DefaultPersistedGrantStoreTests`: fixtures creados "ahora" y afirmados contra el reloj, frágiles en los límites de lifetime.
- `IdentityServerPipeline.Initialize` y los clientes construyen `IHost`/`TestServer` por test y nunca los liberan; con 221 tests × 2 TFMs se filtran hosts y HttpClients. Hacer el pipeline `IAsyncDisposable` con `IAsyncLifetime`.
- `Validation/Setup/Factory.cs` cablea a mano 10+ argumentos de constructor para `TokenRequestValidator`/`AuthorizeRequestValidator`; cada cambio de constructor rompe ~30 archivos. Mejor un `ServiceCollection` con `AddIdentityServer()` y sobrescribir lo que se prueba.
- Aserciones de recuento exacto de claims (`payload.Count().Should().Be(8)`), frágiles ante cambios aditivos.

**Sugerencias.** Snapshots con Verify para discovery, JWKS, payloads de token por grant y respuestas de error (detectarían de paso el `pairwise` falso). Ejecutar la OpenID Conformance Suite oficial contra el `TestServer` en CI: README y docs afirman "certificado" pero el fork no lo ha pasado. Tests de arquitectura (NetArchTest): "Models no referencia IdentityModel", "ningún `DateTime.UtcNow` fuera de TimeProvider", "cada `Default*` registrado en `Core.cs` tiene test". Stryker.NET sobre `Validation/Default/*`: los validadores son muy ramificados y los tests afirman sobre todo booleanos `IsError`. Añadir `Microsoft.Extensions.TimeProvider.Testing`.

---

## 7. Documentación

`docs/` es el contenido de readthedocs de IdentityServer4 pasado a Markdown (99 archivos). Sólo hay dos añadidos del fork: `docs/endpoints/nonce.md` y `docs/reference/nonce_pattern.md`.

**Obsoleto:** 68 menciones a `IdentityServer4`; 12 archivos enlazan a `identityserver4.readthedocs.io`; `docs/intro/packaging.md` lista los paquetes antiguos, MyGet, `IdentityServer4.Templates`, `AccessTokenValidation`, Quickstart.UI; los quickstarts referencian `dotnet new -i IdentityServer4.Templates`, ASP.NET Core 3.1, `IdentityServer4.EntityFramework` (**no existe en este repo**), `oidc-client` (deprecado); `docs/topics/resource_owner.md` documenta ROPC, que ya no existe; `docs/reference/client.md:43` documenta `AllowAccessTokensViaBrowser`, eliminado con implicit; `docs/reference/aspnet_identity.md` menciona el validador ROPC.

**Sin documentar:** `Client.AuthorizationNonceLifetime`, `GrantTypes.CodeAndNonce`, `EndpointsOptions.EnableNonceEndpoint`, `IAuthorizationNonceStore`, los eventos de nonce, `TokenValidationOptions.ValidIssuers`, los `SameSite=None` de `AddAspNetIdentity`, la inyección de `TimeProvider` (los docs hablan de `ISystemClock`), el validador AppAuth.

**Qué debería añadir un fork mantenido:** (1) guía de migración desde IdentityServer4 4.1.x (renombrado de namespaces y paquetes, grants eliminados, `ISystemClock` → `TimeProvider`, Newtonsoft → System.Text.Json y **compatibilidad del campo `Data` de los `PersistedGrant` existentes**, strong naming, ausencia de EF); (2) CHANGELOG generado desde los diffs del PublicApiAnalyzer; (3) `SECURITY.md` (canal de reporte, versiones soportadas, política ante avisos en la cadena IdentityModel/Microsoft.IdentityModel); (4) `LICENSE` + `NOTICE`; (5) página de modelo de amenazas del patrón de nonce (un solo uso, caducidad, binding a cliente, replay), que es la principal funcionalidad de seguridad propia del fork.

---

## 8. Roadmap propuesto

| Fase | Contenido | Dependencias |
|---|---|---|
| **0. Inmediato (días)** ✅ | S1 flujo de nonce · parches heredados (PR #1933, #1796, #619) · S3 `logout+jwt` · S4 `cnf` en introspección · S12 filtros de logging · tests para todo ello. **Hecho**; quedan S1 (c) y (f) por decidir | ninguna |
| **1. Base del repositorio (1 semana)** | Primer commit · CI · MinVer · metadatos de paquete · LICENSE/NOTICE · SourceLink · `TreatWarningsAsErrors` + `EnforceCodeStyleInBuild` · `dotnet.config` · `global.json` · licencia de FluentAssertions · `dotnet format` en un commit · `TimeProvider` registrado · `IValidateOptions` | ninguna; en paralelo con la fase 0 |
| **2. Hardening (2–3 semanas)** ✅ | **Hecho** salvo las desviaciones de §3.5. S5 detección de reuso de refresh tokens y revocación de familia · S6 consumo atómico de códigos · S7 retirar `code token` · S8 `iss` RFC 9207 · S9 `request_uri` (sin redirecciones, DNS, pre-registro, `StrictJarValidation` por defecto) · S10 límite de intentos en user_code · S11 exigir `IssuerUri` en producción · S2 confianza mTLS · S13–S24 | fase 0 |
| **3. Limpieza estructural (2 semanas)** ✅ | **Hecho** salvo lo indicado en §5.4. Sacar modelos de negocio de Models · deduplicar tipos entre ensamblados · borrar código muerto y `[Obsolete]` · keyed services en lugar de decoradores · `IIssuerNameService` en lugar de `IHttpContextAccessor` · dividir `TokenRequestValidator` por grant · arreglar los tests frágiles (estado estático, relojes, disposal) | fase 1 |
| **4. Endpoint routing (2–3 semanas)** ✅ | **Hecho** salvo lo indicado en §4.5. Fases 1–3 del plan de §4.2: `IEndpointResult : IResult`, `MapIdentityServer()` aditivo, metadatos para CORS/mTLS, back-channel logout por `Channel<T>` | fase 3 (keyed services, TimeProvider) |
| **5. Funcionalidad de protocolo (por prioridad de negocio)** ✅ | **Hecho** salvo lo indicado en §3.7 (token exchange sin caso de uso, conformance suite sin CI). Rotación automática de claves · PAR (RFC 9126) con las lecciones de GHSA-mxv6-xwqj-ww2p · DPoP (RFC 9449) · resource indicators (RFC 8707) · `client_id` en end_session · token exchange (RFC 8693) si hay caso de uso · OpenID Conformance Suite en CI | fase 4 (rate limiting por endpoint, metadatos) |
| **6. Modernización continua** ✅ | **Hecho** salvo lo indicado en §5.5 (records/`init`, Verify por licencia). Nullable (Storage → Models → AspNetIdentity → core) · `LoggerMessage` al 100 % · `JsonSerializerContext` · `Frozen*` · records/`init`/`sealed` · `Base64Url` in-box · snapshots con Verify · Stryker | transversal |

Las fases 0 y 1 son independientes entre sí y pueden arrancar hoy. La fase 4 (routing) conviene hacerla después de la 3 porque los keyed services, el registro explícito de `TimeProvider` y la eliminación de `IHttpContextAccessor` simplifican mucho la migración.

---

## Apéndice A. Fuentes de los avisos de seguridad

- CVE-2024-39694 · GHSA-ff4q-64jc-gx98 (Duende) · GHSA-55p7-v223-x366 (IS4, sin parche) · commit de corrección `269ca2171fe1e901c87f2f0797bbc7c230db87c6` en DuendeSoftware/products · blog `duendesoftware.com/blog/20240731-security-patch`
- IS4 4.1.2 · issue #5184, PR #5188, commit `997a6cdd643e46cd5762b710c4ddc43574cbec2e` (DuendeArchive/IdentityServer4)
- Duende PR #1933 (7.3.0) "Don't return redirect URI or response mode on unsafe errors" · PR #1796 (7.3.0) "Ensure Client Secret is Required for Clients with ClientCredentials Grant" · PR #619 (6.0.0) "Fix PostLogout check in AppAuth redirect validator"
- CVE-2024-49755 · GHSA-v9xq-2mvm-x8xc · commit `f28cac921c1f545afe65af71a9327224755b6dac`
- GHSA-mxv6-xwqj-ww2p (PAR, 2026-09-28) · release `is-7.0.10`
- CVE-2026-4349 · GHSA-46qm-5mvh-mmxr (VulDB, sin revisar, sin detalle técnico público)
- CVE-2024-21319 · GHSA-59j7-ghrg-fj52 (Microsoft.IdentityModel) · CVE-2024-21907 · GHSA-5crp-9r3c-p9vr (Newtonsoft.Json)
- Guías de actualización de Duende (v6.3→7.0, v7.4→8.0) y notas de versión en `github.com/DuendeSoftware/products/releases`
- Lista de proveedores OpenID certificados: `openid.net/certification/certified-openid-providers-profiles/` (IdentityServer4 certificado en 2016; este fork no ha sido certificado)
