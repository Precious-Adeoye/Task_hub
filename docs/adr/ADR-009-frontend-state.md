# ADR-009: Frontend State and Data Fetching Approach

## Status
Accepted

## Context
The React frontend needs to manage authentication state, organisation context, todo data, and UI state (filters, pagination, editing). We need a pattern that is simple, maintainable, and appropriate for the application's complexity.

## Decision
Use React built-in state management: `useState` for local component state, `useContext` for auth state, and `useEffect` for data fetching. Use Axios as the HTTP client with interceptors for cross-cutting concerns.

## Options Considered

### Option A: Redux / Zustand
- Global state management library
- Adds boilerplate and complexity
- Overkill for current application size

### Option B: React Query / TanStack Query
- Excellent caching and refetching
- Additional dependency and learning curve
- Best for apps with heavy data fetching patterns

### Option C: Built-in React State (Chosen)
- useState + useContext + useEffect
- No additional dependencies
- Sufficient for current scope
- Easy to understand and maintain

## Consequences
- Auth state shared via React Context (`AuthProvider`)
- Todo data fetched on mount and when filters/org change
- Optimistic UI updates for toggle with rollback on failure
- Axios interceptors handle X-Organisation-Id and X-Correlation-Id headers
- Loading and error states managed per-component

## Follow-ups
- Migrate to React Query if data fetching complexity grows
- Extract todo state management into a custom hook
- Break monolithic App.tsx into smaller components
