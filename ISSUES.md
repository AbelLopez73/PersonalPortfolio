# PersonalPortfolio issue backlog

## Development Issues

### 1. Foundation setup
- Type: Feature
- Priority: High
- Phase: Foundation
- Status: Backlog
- Description: Initialize the Angular frontend and ASP.NET Core API structure and document the project architecture.
- Acceptance criteria:
  - Angular app is created.
  - API project is created.
  - Both run locally.
  - The README is written in English.

### 2. Public site landing page
- Type: Feature
- Priority: High
- Phase: Public Site
- Status: Backlog
- Description: Build the main landing page in English with a hero section, CTA, and navigation.
- Acceptance criteria:
  - Landing page is accessible.
  - Layout is responsive.
  - Clear calls to action are present.
  - Sections for About and Resume are included.

### 3. About / Resume page
- Type: Feature
- Priority: Medium
- Phase: About / Resume
- Status: Backlog
- Description: Add a public page with professional profile, experience, values, methodology, and relevant links.
- Acceptance criteria:
  - Page is published in English.
  - It is connected to the site navigation.

### 4. Portfolio showcase
- Type: Feature
- Priority: Medium
- Phase: Portfolio
- Status: Backlog
- Description: Present projects and case studies with technology stack, results, and links.
- Acceptance criteria:
  - Portfolio items render in a grid or cards.
  - Each project includes a summary and detail page.

### 5. Blog section
- Type: Feature
- Priority: Medium
- Phase: Blog
- Status: Backlog
- Description: Build a simple blog structure using repository-based content or Markdown files.
- Acceptance criteria:
  - Article listing works.
  - Article detail page works.
  - Content is versioned in the repository.

### 6. Collaborations section
- Type: Feature
- Priority: Medium
- Phase: Collaborations
- Status: Backlog
- Description: Add a section to present collaboration opportunities and contact channels.
- Acceptance criteria:
  - Page explains services and collaboration approach.
  - Includes a clear CTA to contact or the Telegram suggestions group.

### 7. Suggestions feature
- Type: Feature
- Priority: High
- Phase: Suggestions
- Status: Backlog
- Description: Accept public suggestions from visitors through a Telegram-based workflow.
- Acceptance criteria:
  - The form sends a message to the configured Telegram group.
  - Errors are handled gracefully.

### 8. Telegram integration
- Type: Feature
- Priority: High
- Phase: Telegram
- Status: Blocked
- Description: Configure the Telegram bot API and delivery flow from Angular and ASP.NET Core.
- Acceptance criteria:
  - Bot token and chat ID are managed securely.
  - Messages are delivered reliably.

### 9. Analytics integration
- Type: Feature
- Priority: Medium
- Phase: Analytics
- Status: Backlog
- Description: Add privacy-conscious analytics for visits, source tracking, and page metrics.
- Acceptance criteria:
  - Provider is selected.
  - Analytics are enabled.
  - Privacy scope is documented.

### 10. Admin panel
- Type: Feature
- Priority: High
- Phase: Admin Panel
- Status: Blocked
- Description: Create a protected administration panel for content updates and moderation.
- Acceptance criteria:
  - Login flow is defined.
  - Authentication provider is selected.
  - Access is restricted.

### 11. Accessibility and SEO
- Type: Feature
- Priority: Medium
- Phase: Accessibility
- Status: Backlog
- Description: Ensure the site is accessible, semantic, and optimized for search engines.
- Acceptance criteria:
  - Accessibility checks pass.
  - Metadata and route rendering are documented.

### 12. Testing and deployment
- Type: Chore
- Priority: High
- Phase: Testing
- Status: Backlog
- Description: Add automated tests and prepare deployment to a free hosting provider such as Render or Cloudflare Pages + Render.
- Acceptance criteria:
  - Unit and e2e coverage is added.
  - Deployment succeeds.
  - App health is checked.

## Pending Problems
- Free hosting sleep and quota limitations.
- Telegram delivery delay after API wake-up.
- Admin authentication provider decision.
- Analytics provider selection.
- No database persistence in version one.
- Spam and rate limiting.
- Collaboration request moderation.
- Telegram secret management.
- CORS configuration between Angular and ASP.NET Core.
- SEO behavior for prerendered routes.
- Error handling when Telegram is unavailable.

## GitHub issues and branch mapping

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

## Branch and promotion workflow

- `main` is the final production branch.
- `desarrollo` is the default branch and integration target for issue branches.
- `pruebas` receives changes promoted from `desarrollo` for validation.
- `produccion` receives validated changes promoted from `pruebas`.
- `main` receives approved releases promoted from `produccion`.
- Each issue is developed on its matching `feature/issue-...` branch and merged to `desarrollo` through a pull request that references the issue.
- Promote changes with pull requests in this order: `desarrollo` -> `pruebas` -> `produccion` -> `main`.

The branches and issues above have been created in GitHub. Branch protection and required pull-request checks are not configured yet.

Whenever a GitHub issue or any branch is created, renamed, or removed, update this table and the matching table in `README.md` in the same pull request. Do not merge that pull request until both tables match.
