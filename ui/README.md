# MultiAgentLab UI

Standalone Angular 21 front-end for the **MultiAgentLab POC — Revisión de Historias de Usuario**.

Communicates exclusively with the backend via REST + polling (`GET /executions/{id}/log`).
See [`documentation/07_Angular_UI_Design.md`](../documentation/07_Angular_UI_Design.md) for the full design.

---

## Prerequisites

- Node.js >= 18
- Backend running at `http://localhost:5000` (see `/src/Api`)

---

## Development

```bash
cd ui
npm install
npm start          # → http://localhost:4200
```

To point at a different backend:
```bash
# Edit src/environments/environment.ts
apiBaseUrl: 'http://your-backend-host:port'
```

## Build

```bash
npm run build               # development build
npm run build -- --configuration=production  # production build → dist/ui/browser/
```

## Test

```bash
npm test
```

---

## Architecture

| Layer | Technology |
|---|---|
| Framework | Angular 21 (standalone, Signals) |
| UI | Angular Material 21 (dark theme, Material 3) |
| State | `ExecutionStateService` (Signals) |
| Real-time | `ExecutionStreamService` (polling `GET /executions/{id}/log`) |
| HTTP | Angular `HttpClient` via `ReviewApiService` |

### 4-panel layout

```
┌─ Toolbar ─────────────────────────────────────────────────┐
│ Story Input │ Agent Decisions │ Event Timeline             │
│             │                 │                            │
└─────────────────────────────────────────────────────────── ┘
│ Final Result                                               │
└────────────────────────────────────────────────────────────┘
```

### Key source paths

| Path | Purpose |
|---|---|
| `src/app/core/models/api.models.ts` | All TypeScript interfaces |
| `src/app/core/services/review-api.service.ts` | HTTP wrappers |
| `src/app/core/services/execution-stream.service.ts` | Polling abstraction |
| `src/app/core/services/execution-state.service.ts` | Centralized Signals state |
| `src/app/features/history-input/` | Story input + mock case selector |
| `src/app/features/decision-panel/` | Live agent status cards |
| `src/app/features/event-timeline/` | Streaming event log |
| `src/app/features/final-result/` | Final analysis result |
| `src/environments/environment.ts` | Dev config (`apiBaseUrl`, `pollIntervalMs`) |

## Development server

To start a local development server, run:

```bash
ng serve
```

Once the server is running, open your browser and navigate to `http://localhost:4200/`. The application will automatically reload whenever you modify any of the source files.

## Code scaffolding

Angular CLI includes powerful code scaffolding tools. To generate a new component, run:

```bash
ng generate component component-name
```

For a complete list of available schematics (such as `components`, `directives`, or `pipes`), run:

```bash
ng generate --help
```

## Building

To build the project run:

```bash
ng build
```

This will compile your project and store the build artifacts in the `dist/` directory. By default, the production build optimizes your application for performance and speed.

## Running unit tests

To execute unit tests with the [Vitest](https://vitest.dev/) test runner, use the following command:

```bash
ng test
```

## Running end-to-end tests

For end-to-end (e2e) testing, run:

```bash
ng e2e
```

Angular CLI does not come with an end-to-end testing framework by default. You can choose one that suits your needs.

## Additional Resources

For more information on using the Angular CLI, including detailed command references, visit the [Angular CLI Overview and Command Reference](https://angular.dev/tools/cli) page.
