# TickerScout

Live stock quote grid with a C# SignalR backend and a React + TypeScript + AG Grid frontend.

## Projects

- `/backend` - ASP.NET Core Minimal API + SignalR quote hub and random-walk simulator
- `/frontend` - React + TypeScript + AG Grid client

## Ports

- Backend: `http://localhost:5051`
- Frontend: `http://localhost:5173`
- SignalR hub: `http://localhost:5051/hubs/quotes`

## Run backend

```bash
cd backend
dotnet run
```

## Run frontend

```bash
cd frontend
npm install
npm run dev
```

The frontend connects to the backend using:

- `VITE_HUB_URL` for the SignalR hub (defaults to `/hubs/quotes` in the app, with Vite proxying to `http://localhost:5051` during local development)
- `VITE_AI_API_URL` for AI HTTP requests (defaults to same-origin, so Railway and other backend-served deployments can call `/api/ai/prompt` without extra configuration)

## Configurable symbols

Edit `backend/appsettings.json`:

- `Ai.Endpoint`: Azure AI project endpoint
- `Ai.ModelDeploymentName`: deployed model name used for the agent
- `Ai.AgentName`: Azure AI agent name
- `Ai.Username` / `Ai.AccessToken`: optional basic-auth credentials for deployments such as Railway (can also be supplied via `Ai__Username` / `Ai__AccessToken` environment variables)
- `Quote.Symbols`: symbol list used by the simulator
- `Quote.UpdateIntervalMs`: update interval in milliseconds
- `Cors.AllowedOrigins`: allowed frontend origins for local development
