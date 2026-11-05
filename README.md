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
- Angular `HttpClient` connects the public portfolio assistant to the ASP.NET Core API; the SignalR JavaScript client supports admin presence.
- The public chat interface keeps an in-memory conversation and displays pending and unavailable states.
- Vitest is included in the Angular test toolchain.

### Backend

- ASP.NET Core Web API on .NET 8 with nullable reference types and implicit global usings enabled.
- Minimal API endpoints provide the initial portfolio sample (`GET /api/portfolio`) and Ollama-powered assistant (`POST /api/assistant/chat`).
- A typed `HttpClient` service sends chat requests to Ollama's `/api/chat` endpoint. The Ollama URL and model are configuration-driven; defaults are `http://localhost:11434` and `deepseek-r1:8b`.
- The assistant sends a bounded conversation history and a portfolio-specific system prompt. Ollama runs separately from the API and is not bundled with the application containers.
- Controller support enabled for future controller-based endpoints.
- Swashbuckle provides Swagger/OpenAPI documentation in development.
- Telegram OIDC Authorization Code with PKCE is configured as the admin identity provider; the handler is only enabled when the BotFather Client ID/Secret and configured owner Telegram user ID are present.
- ASP.NET Core cookie authentication restricts admin endpoints, and an authorized SignalR hub tracks owner presence across browser devices.
- CORS origins are configuration-driven and allow credentials for the Angular client.
- HTTPS redirection is enabled. Production admin cookies require HTTPS.

## Implementation Approach

- Portfolio content is represented as typed profile data and selected through an in-page profile switcher.
- Angular signals hold the selected profile; computed signals expose the corresponding summary, focus areas, highlights, experience, projects, and resume link to the template.
- The Angular profile data is currently local to the root component. The frontend does not yet fetch the portfolio data from the API; the assistant uses a concise profile context maintained by the backend.
- The public assistant runs locally through Ollama and answers questions about the portfolio profiles, skills, and projects. It is not an admin agent and does not provide arbitrary access to server tools.
- The API endpoint currently returns sample data; persistent content storage has not been added.
- Telegram Login and private owner-presence code are legacy implementation that will be replaced by the owner moderation workspace in issue #10.
- The community roadmap uses a public join-request form, a server-side Telegram bot, and an explicitly moderated public feed; see issues #7, #8, #10, and #17.
- Frontend and backend are separate applications and are built independently.
- Docker is the standard local packaging and validation path for the application.
- Work is organized around GitHub issues. Every issue has a matching working branch, listed below and in [ISSUES.md](ISSUES.md).

## Deployment Direction

- Docker Compose is used locally to validate the same frontend and API container boundaries that will be deployed.
- IONOS is the selected final hosting provider. The application will be deployed as containers so the ASP.NET Core API can keep Telegram integration and other server-side behavior private.
- Hosting is not configured during feature development or validation. The IONOS setup is addressed only in the pull request from `pruebas` to `produccion`, under issue [#12](https://github.com/AbelLopez73/PersonalPortfolio/issues/12).
- That release-candidate pull request must document the IONOS service selection, domain, HTTPS configuration, container deployment procedure, environment variables, and Telegram callback URL. Secrets are configured on IONOS and never committed to this repository.

## Issue and Branch Mapping

| Issue or branch role | GitHub issue | Branch |
|---|---|---|
| 1. Foundation setup | [#1](https://github.com/AbelLopez73/PersonalPortfolio/issues/1) | `feature/issue-1-foundation-setup` |
| 2. Public site landing page | [#2](https://github.com/AbelLopez73/PersonalPortfolio/issues/2) | `feature/issue-2-public-site-landing-page` |
| 3. About / Resume page | [#3](https://github.com/AbelLopez73/PersonalPortfolio/issues/3) | `feature/issue-3-about-resume-page` |
| 4. Portfolio showcase | [#4](https://github.com/AbelLopez73/PersonalPortfolio/issues/4) | `feature/issue-4-portfolio-showcase` |
| 5. Blog section | [#5](https://github.com/AbelLopez73/PersonalPortfolio/issues/5) | `feature/issue-5-blog-section` |
| 6. Collaborations section | [#6](https://github.com/AbelLopez73/PersonalPortfolio/issues/6) | `feature/issue-6-collaborations-section` |
| 7. Telegram community join request form | [#7](https://github.com/AbelLopez73/PersonalPortfolio/issues/7) | `feature/issue-7-suggestions-feature` |
| 8. Telegram community bot and webhook integration | [#8](https://github.com/AbelLopez73/PersonalPortfolio/issues/8) | `feature/issue-8-telegram-webhook-integration` |
| 9. Analytics integration | [#9](https://github.com/AbelLopez73/PersonalPortfolio/issues/9) | `feature/issue-9-analytics-integration` |
| 10. Owner moderation workspace | [#10](https://github.com/AbelLopez73/PersonalPortfolio/issues/10) | `feature/issue-10-admin-panel` |
| 11. Accessibility and SEO | [#11](https://github.com/AbelLopez73/PersonalPortfolio/issues/11) | `feature/issue-11-accessibility-seo` |
| 12. Testing and deployment | [#12](https://github.com/AbelLopez73/PersonalPortfolio/issues/12) | `feature/issue-12-testing-deployment` |
| 13. Multiple profile variants | [#13](https://github.com/AbelLopez73/PersonalPortfolio/issues/13) | `feature/issue-13-profile-variants` |
| 14. Profile photo and resume assets | [#14](https://github.com/AbelLopez73/PersonalPortfolio/issues/14) | `feature/issue-14-profile-photo-resume-assets` |
| 17. Public Telegram community feed | [#17](https://github.com/AbelLopez73/PersonalPortfolio/issues/17) | `feature/issue-17-public-telegram-community-feed` |
| 18. Public portfolio assistant powered by Ollama | [#18](https://github.com/AbelLopez73/PersonalPortfolio/issues/18) | `feature/issue-18-public-portfolio-assistant` |
| Workflow: integration | N/A | `desarrollo` |
| Workflow: validation | N/A | `pruebas` |
| Workflow: release candidate | N/A | `produccion` |
| Workflow: released versions | N/A | `main` |

Whenever a GitHub issue or any branch is created, renamed, or removed, update the mapping tables in both `README.md` and `ISSUES.md` in the same pull request. Do not merge that pull request until both tables match.

## Pull Request and Release Flow

1. Implement each issue on its matching `feature/issue-...` branch.
2. Open a pull request from the issue branch to `desarrollo`, link the issue, and merge after review and a successful build.
3. When the integrated changes work and have been tested in `desarrollo`, open a pull request from `desarrollo` to `pruebas`.
4. After validation succeeds in `pruebas`, open a pull request from `pruebas` to `produccion`. This is the point to define and document the IONOS deployment configuration required by issue #12.
5. Once the release candidate is validated in `produccion`, assign its version and create a version tag on that validated commit, for example `v1.0.0`.
6. Open a pull request from `produccion` to `main` and merge the tagged release. Use a merge commit so the validated, tagged production commit remains in `main` history.

`desarrollo` is the integration branch, `pruebas` is the validation branch, `produccion` contains release candidates, and `main` contains released versions.

## Local Development

Prerequisites: .NET 8 SDK, Node.js, npm, and Ollama with the `deepseek-r1:8b` model installed.

Install or start Ollama, then pull the configured model:

```bash
ollama pull deepseek-r1:8b
```

Keep Ollama running while using the assistant. When the API runs directly on the host, it connects to `http://localhost:11434`. Override `Ollama:BaseUrl` or `Ollama:Model` in .NET configuration to use another Ollama server or model.

Run the API from the repository root:

```bash
dotnet run --project src/PersonalPortfolio.Api/PersonalPortfolio.Api.csproj
```

Run the Angular app from `src/frontend`:

```bash
npm install
npm start
```

The Angular development server runs at `http://localhost:4200`. In Development, the API exposes Swagger UI at its configured local address, serves the sample portfolio at `/api/portfolio`, and accepts assistant messages at `/api/assistant/chat`.

## Local Docker Validation

Docker Compose is the standard local validation path before changes are promoted to the `pruebas` branch. It runs the Angular production build behind Nginx and the ASP.NET Core API as separate containers. Nginx proxies `/api` and `/hubs` to the API, including SignalR WebSocket upgrades. The same container boundary will be used for the final IONOS deployment, whose infrastructure details are decided only in the `pruebas` to `produccion` pull request.

Prerequisite: Docker Desktop must be running.

```bash
docker compose up --build
```

Open `http://localhost:4200` for the complete site. The API is also available directly at `http://localhost:5016`, and Swagger is available at `http://localhost:5016/swagger` while the local Docker environment uses the Development setting.

The API container connects to Ollama on the Windows host through `host.docker.internal:11434`. Docker Desktop and Ollama must both be running, and the configured model must already be pulled on the host. Compose sets `Ollama__BaseUrl` and `Ollama__Model` for the API; change these values if using another host or model.

The Telegram login and notification services remain disabled until server-side credentials are configured. Do not add bot tokens, client secrets, or owner IDs to `docker-compose.yml`.

Stop the local environment with:

```bash
docker compose down
```

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

- Connect the portfolio profile data to the API; the assistant currently uses a concise backend-maintained profile context.
- Add real profile photo and separate downloadable resumes for each professional identity.
- Add the public community join-request form, secure Telegram bot integration, owner moderation workspace, and approved public community feed.
- Add persistence for submissions, analytics, blog content, and deployment automation.
- Expand the Software Engineer profile for VB.NET APIs, Python, IBM i/RPG/ILE, SQL Server, PHP, and other stacks. These are profile targets, not additional runtimes currently implemented by this application.

## Telegram Community Configuration

The Telegram community bot will be configured only on the API host, using User Secrets locally or deployment environment variables. The exact settings and webhook setup are implemented in issue #8.

- Bot token
- Community chat ID
- Webhook secret
- Allowed frontend origin(s)

The bot token, webhook secret, and moderation credentials must never be placed in Angular configuration or committed to the repository. The public group disclosure must explain that a moderator may select messages for publication on the site.

## Documentation and Backlog

See [ISSUES.md](ISSUES.md) for the project backlog and its matching issue-and-branch table. Keep both tables synchronized whenever issues or branches change.
