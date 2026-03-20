# AGENTS.md

## Purpose
- This repository contains a mixed ASP.NET Core backend and Angular frontend.
- Primary projects:
  - `oMediaCenter.Web` - ASP.NET Core host plus Angular client assets in `oMediaCenter.Web/wwwsource`
  - `oMediaCenter.Tests` - xUnit backend tests
  - Supporting class library / plugin projects under `oMediaCenter.*`
- When editing, preserve existing behavior unless the task explicitly asks for refactors.

## Repository Layout
- Solution file: `oMediaCenter.sln`
- Frontend package manifest: `oMediaCenter.Web/package.json`
- Angular config: `oMediaCenter.Web/angular.json`
- TypeScript config: `oMediaCenter.Web/tsconfig.json`
- Backend entrypoint: `oMediaCenter.Web/Program.cs`
- Legacy startup file still exists: `oMediaCenter.Web/Startup.cs`

## Build Commands
- Restore backend dependencies:
  - `dotnet restore oMediaCenter.sln`
- Build the full .NET solution:
  - `dotnet build oMediaCenter.sln`
- Build only the web host project:
  - `dotnet build oMediaCenter.Web/oMediaCenter.Web.csproj`
- Install frontend dependencies:
  - `npm install`
  - Run from `oMediaCenter.Web`
- Start Angular dev server:
  - `npm run start`
  - Run from `oMediaCenter.Web`
- Build Angular app:
  - `npm run build`
  - Run from `oMediaCenter.Web`
- Watch Angular build in development mode:
  - `npm run watch`
  - Run from `oMediaCenter.Web`

## Lint / Static Checks
- There is currently no dedicated `lint` script in `oMediaCenter.Web/package.json`.
- There is no repo-level ESLint, Prettier, or `dotnet format` command checked in.
- For frontend safety, use the Angular production build as the main static check:
  - `npm run build`
- TypeScript is configured in strict mode in `oMediaCenter.Web/tsconfig.json`.
- For backend safety, use:
  - `dotnet build oMediaCenter.sln`
- If you introduce a new lint tool, document the command here in the same change.

## Test Commands
- Run all backend tests:
  - `dotnet test oMediaCenter.Tests/oMediaCenter.Tests.csproj`
- Run a single backend test class:
  - `dotnet test oMediaCenter.Tests/oMediaCenter.Tests.csproj --filter "FullyQualifiedName~MetaDatabaseTests"`
- Run a single backend test method:
  - `dotnet test oMediaCenter.Tests/oMediaCenter.Tests.csproj --filter "FullyQualifiedName~MetaDatabaseTests.ShouldFindCorrectMovieNameWithParanSurroundedYearInFilename"`
- Run Angular unit tests in watch mode:
  - `npm run test`
  - Run from `oMediaCenter.Web`
- Run Angular tests once without watch:
  - `npm run test -- --watch=false --browsers=ChromeHeadless`
  - Run from `oMediaCenter.Web`
- Run a single Angular spec file:
  - `npm run test -- --include="wwwsource/app/settings/settings.component.spec.ts" --watch=false --browsers=ChromeHeadless`
  - Replace the spec path as needed; existing specs live under `oMediaCenter.Web/wwwsource/app/**/*.spec.ts`

## Test Caveats
- `angular.json` points test execution at `wwwsource/test.ts` and `wwwsource/polyfills.ts`.
- `tsconfig.spec.json` still references `src/test.ts` and `src/polyfills.ts`, which looks stale.
- `angular.json` also references `karma.conf.js`, but no `oMediaCenter.Web/karma.conf.js` file is currently checked in.
- If frontend tests fail before your code runs, inspect those config mismatches before changing application code.

## Cursor / Copilot Rules
- No `.cursorrules` file was found at the repository root.
- No files were found under `.cursor/rules/`.
- No `.github/copilot-instructions.md` file was found.
- Do not assume hidden Cursor or Copilot rules exist; rely on repository code and this file.

## Frontend Conventions
- Frontend source lives in `oMediaCenter.Web/wwwsource`, not the Angular default `src` folder.
- Angular selector prefix is `app`; component selectors follow `app-*` naming.
- Components are module-based today; `standalone: false` is common.
- New components, services, guards, and similar Angular artifacts should follow Angular CLI file naming:
  - `feature-name.component.ts`
  - `feature-name.service.ts`
  - `feature-name.component.html`
  - `feature-name.component.css`
- Keep related template, style, spec, and class files in the same folder.

## Formatting Rules
- Follow `oMediaCenter.Web/.editorconfig` for frontend files:
  - UTF-8 charset
  - spaces, size 2
  - final newline required
  - trim trailing whitespace
  - single quotes in `*.ts`
- For Markdown, trailing whitespace is allowed and max line length is disabled.
- Keep formatting consistent with the file you are editing, especially in older C# files.

## Import / Using Style
- Match existing local import style in Angular code.
- Use relative imports within `wwwsource/app`; there is no configured `@/` alias in current `tsconfig.json`.
- Keep imports grouped logically and avoid unused imports.
- In C#, keep `using` directives at the top of the file.
- Prefer removing dead `using` directives rather than leaving analyzer noise behind.

## TypeScript Rules
- `strict: true` is enabled; write code that satisfies strict null checks and strict template checking.
- `noImplicitReturns`, `noImplicitOverride`, and `noPropertyAccessFromIndexSignature` are enabled.
- Prefer explicit return types for public methods and exported helpers.
- Avoid `any`; prefer concrete interfaces or `unknown` plus narrowing.
- Preserve existing promise-based APIs unless the task specifically migrates them to Observables.
- When touching legacy code that uses `toPromise()`, avoid broad rewrites unless required for the task.

## Angular Patterns
- Prefer declarative templates and keep branching / transformation logic in the component or a helper.
- Use `ChangeDetectionStrategy.OnPush` for new presentational components when practical; some existing components already do this.
- Register injectable services consistently with the surrounding pattern; current code commonly provides services in `AppModule`.
- Keep route/page animation logic local to the component unless it is reused.
- Reuse existing environment files under `oMediaCenter.Web/wwwsource/environments` for environment-specific values.

## C# Rules
- Backend targets modern .NET and uses minimal hosting in `Program.cs`, but some legacy patterns remain.
- Prefer the active hosting style in `Program.cs` for new startup/service registration changes.
- Use explicit, descriptive names for controllers, services, models, and plugin classes.
- Preserve existing namespace structure under each project.
- Prefer `async Task` over `async void` for new asynchronous methods outside event handlers.
- Use dependency injection for loggers, database contexts, and services.

## Naming Conventions
- TypeScript classes, Angular components, and services: `PascalCase`
- TypeScript variables, functions, and methods: `camelCase`
- Angular selectors: `app-kebab-case`
- Angular folders and file stems: `kebab-case`
- C# classes, methods, properties: `PascalCase`
- C# locals / parameters / private fields: follow surrounding file style; newer code should prefer descriptive camelCase locals and `_camelCase` private fields
- Test files:
  - Angular: `*.spec.ts`
  - xUnit: `*Tests.cs` or behavior-driven names matching the existing project

## Error Handling
- Do not swallow exceptions silently.
- In backend code, log meaningful context before returning fallback responses.
- Existing controllers use `ILogger`; continue that pattern for new backend code.
- Return appropriate HTTP results from controllers instead of `null` when you are already modifying that area.
- In frontend code, prefer typed error handling and user-facing recovery paths over bare `console.error`.
- If you only touch nearby code, keep error handling changes narrowly scoped and low-risk.

## Testing Guidance
- Add or update tests when changing observable behavior.
- For Angular changes, prefer colocated spec files next to the component or service under `wwwsource/app`.
- For backend parsing / metadata logic, extend xUnit coverage in `oMediaCenter.Tests`.
- When fixing filename parsing or metadata extraction, add representative regression cases using `[Theory]` and `[InlineData]`.
- If you cannot run frontend tests because of the current Karma config mismatch, note that clearly in your final response.

## Practical Agent Advice
- Check whether a change belongs in the Angular client, the ASP.NET host, or a plugin project before editing.
- Be careful around legacy files that mix old and new framework conventions.
- Do not move frontend source from `wwwsource` to `src` unless the task is specifically a structural migration.
- Prefer small, targeted edits over style-only rewrites.
- Avoid introducing new dependencies unless necessary.
- Do not commit secrets, API keys, or environment-specific credentials.

## Pre-PR Verification
- Minimum for backend-only changes:
  - `dotnet build oMediaCenter.sln`
  - `dotnet test oMediaCenter.Tests/oMediaCenter.Tests.csproj`
- Minimum for frontend-only changes:
  - `npm run build`
  - relevant `npm run test -- --include=... --watch=false --browsers=ChromeHeadless` when test config is working
- For mixed changes, run both backend and frontend verification paths.

## When Updating This File
- Keep guidance specific to the checked-in repository state.
- Replace guesses with verified commands whenever you confirm new tooling.
- If Cursor or Copilot rule files are later added, summarize their important instructions here.
