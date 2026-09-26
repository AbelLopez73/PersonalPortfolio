# PersonalPortfolio

A multi-profile personal portfolio built with Angular and ASP.NET Core. It is designed to present distinct professional identities, including Software Engineer, Warehouse Associate, and Professional Driver, with room for additional technical specializations and resume files.

## Project Structure

```text
src/
	PersonalPortfolio.Api/   ASP.NET Core Web API
	PersonalPortfolio.sln    .NET solution
	frontend/                Angular application
```

## Technologies in Use

### Frontend

- Angular 22 with standalone application configuration and TypeScript 6.
- Angular signals (`signal` and `computed`) for the selected profile and derived view state.
- TypeScript types for profile variants, highlights, experience, and project data.
- Angular built-in template control flow (`@for`) for rendering profile content and collections.
- SCSS for component and global styling.
- Angular Router is configured; the current portfolio is rendered by the root component.
- Angular `HttpClient` and the SignalR JavaScript client support the admin session and live presence connection.
- Vitest is included in the Angular test toolchain.

### Backend

- ASP.NET Core Web API on .NET 8 with nullable reference types and implicit global usings enabled.
- Minimal API endpoint (`GET /api/portfolio`) for the initial portfolio sample data.
- Controller support enabled for future controller-based endpoints.
- Swashbuckle provides Swagger/OpenAPI documentation in development.
- Telegram OIDC Authorization Code with PKCE is configured as the admin identity provider; the handler is only enabled when the BotFather Client ID/Secret and configured owner Telegram user ID are present.
- ASP.NET Core cookie authentication restricts admin endpoints, and an authorized SignalR hub tracks owner presence across browser devices.
- CORS origins are configuration-driven and allow credentials for the Angular client.
- HTTPS redirection is enabled. Production admin cookies require HTTPS.

## Implementation Approach

- Portfolio content is represented as typed profile data and selected through an in-page profile switcher.
- Angular signals hold the selected profile; computed signals expose the corresponding summary, focus areas, highlights, experience, projects, and resume link to the template.
- The Angular profile data is currently local to the root component. The frontend does not yet fetch data from the API.
- The API endpoint currently returns sample data; persistent content storage has not been added.
- The admin login and SignalR presence are implemented but remain disabled until Telegram Login and owner configuration are supplied through server-side settings.
- The public suggestion form, Telegram message delivery service, and ntfy offline preview are still planned; no notification service is active yet.
- Frontend and backend are separate applications and are built independently.
- Work is organized around GitHub issues. Every issue has a matching working branch, listed below and in [ISSUES.md](ISSUES.md).

## Issue and Branch Mapping

| Issue or branch role | GitHub issue | Branch |
|---|---|---|
| 1. Foundation setup | [#1](https://github.com/AbelLopez73/PersonalPortfolio/issues/1) | `feature/issue-1-foundation-setup` |
| 2. Public site landing page | [#2](https://github.com/AbelLopez73/PersonalPortfolio/issues/2) | `feature/issue-2-public-site-landing-page` |
| 3. About / Resume page | [#3](https://github.com/AbelLopez73/PersonalPortfolio/issues/3) | `feature/issue-3-about-resume-page` |
| 4. Portfolio showcase | [#4](https://github.com/AbelLopez73/PersonalPortfolio/issues/4) | `feature/issue-4-portfolio-showcase` |
| 5. Blog section | [#5](https://github.com/AbelLopez73/PersonalPortfolio/issues/5) | `feature/issue-5-blog-section` |
| 6. Collaborations section | [#6](https://github.com/AbelLopez73/PersonalPortfolio/issues/6) | `feature/issue-6-collaborations-section` |
| 7. Suggestions feature | [#7](https://github.com/AbelLopez73/PersonalPortfolio/issues/7) | `feature/issue-7-suggestions-feature` |
| 8. Telegram webhook integration | [#8](https://github.com/AbelLopez73/PersonalPortfolio/issues/8) | `feature/issue-8-telegram-webhook-integration` |
| 9. Analytics integration | [#9](https://github.com/AbelLopez73/PersonalPortfolio/issues/9) | `feature/issue-9-analytics-integration` |
| 10. Admin panel | [#10](https://github.com/AbelLopez73/PersonalPortfolio/issues/10) | `feature/issue-10-admin-panel` |
| 11. Accessibility and SEO | [#11](https://github.com/AbelLopez73/PersonalPortfolio/issues/11) | `feature/issue-11-accessibility-seo` |
| 12. Testing and deployment | [#12](https://github.com/AbelLopez73/PersonalPortfolio/issues/12) | `feature/issue-12-testing-deployment` |
| 13. Multiple profile variants | [#13](https://github.com/AbelLopez73/PersonalPortfolio/issues/13) | `feature/issue-13-profile-variants` |
| 14. Profile photo and resume assets | [#14](https://github.com/AbelLopez73/PersonalPortfolio/issues/14) | `feature/issue-14-profile-photo-resume-assets` |
| Workflow: integration | N/A | `desarrollo` |
| Workflow: validation | N/A | `pruebas` |
| Workflow: release candidate | N/A | `produccion` |
| Workflow: released versions | N/A | `main` |

Whenever a GitHub issue or any branch is created, renamed, or removed, update the mapping tables in both `README.md` and `ISSUES.md` in the same pull request. Do not merge that pull request until both tables match.

## Pull Request and Release Flow

1. Implement each issue on its matching `feature/issue-...` branch.
2. Open a pull request from the issue branch to `desarrollo`, link the issue, and merge after review and a successful build.
3. When the integrated changes work and have been tested in `desarrollo`, open a pull request from `desarrollo` to `pruebas`.
4. After validation succeeds in `pruebas`, open a pull request from `pruebas` to `produccion`.
5. Once the release candidate is validated in `produccion`, assign its version and create a version tag on that validated commit, for example `v1.0.0`.
6. Open a pull request from `produccion` to `main` and merge the tagged release. Use a merge commit so the validated, tagged production commit remains in `main` history.

`desarrollo` is the integration branch, `pruebas` is the validation branch, `produccion` contains release candidates, and `main` contains released versions.

## Local Development

Prerequisites: .NET 8 SDK, Node.js, and npm.

Run the API from the repository root:

```bash
dotnet run --project src/PersonalPortfolio.Api/PersonalPortfolio.Api.csproj
```

Run the Angular app from `src/frontend`:

```bash
npm install
npm start
```

The Angular development server runs at `http://localhost:4200`. In Development, the API exposes Swagger UI at its configured local address and serves the sample portfolio at `/api/portfolio`.

## Build and Test

From the repository root, build the API solution:

```bash
dotnet build src/PersonalPortfolio.sln
```

From `src/frontend`, build the Angular application and run its test command:

```bash
npm run build
npm test
```

## Planned, Not Yet Implemented

- Connect the portfolio profile data to the API.
- Add real profile photo and separate downloadable resumes for each professional identity.
- Add the public suggestions form and secure Telegram Bot API delivery; use ntfy only for a sanitized preview when the owner is absent.
- Add persistence for submissions, analytics, blog content, and deployment automation.
- Expand the Software Engineer profile for VB.NET APIs, Python, IBM i/RPG/ILE, SQL Server, PHP, and other stacks. These are profile targets, not additional runtimes currently implemented by this application.

## Admin Notification Configuration

Telegram Login is configured in BotFather with the deployed site's allowed origin and callback URL (`/signin-telegram`). Supply values only on the API host, using User Secrets locally or deployment environment variables:

- `TelegramLogin__ClientId`
- `TelegramLogin__ClientSecret`
- `TelegramLogin__OwnerUserId` (the numeric Telegram user ID from the OIDC `id` claim)
- `Frontend__AllowedOrigins` (allowed Angular origin(s))

The BotFather Client Secret and Telegram Bot API token must never be placed in Angular configuration or committed to the repository. ntfy is intended as a push notification in its mobile app, not an SMS. Its public topic is readable by anyone who knows its name, so use a high-entropy topic and send only a redacted preview, never the full suggestion.

## Documentation and Backlog

See [ISSUES.md](ISSUES.md) for the project backlog and its matching issue-and-branch table. Keep both tables synchronized whenever issues or branches change.
