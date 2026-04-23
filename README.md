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

The frontend connects to the SignalR hub using `VITE_HUB_URL` (defaults to `http://localhost:5051/hubs/quotes`).

## Configurable symbols

Edit `backend/appsettings.json`:

- `Quote.Symbols`: symbol list used by the simulator
- `Quote.UpdateIntervalMs`: update interval in milliseconds
- `Cors.AllowedOrigins`: allowed frontend origins for local development
