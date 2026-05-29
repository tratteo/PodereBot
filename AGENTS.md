# AGENTS.md — PodereBot

## Project

.NET 9 ASP.NET Core bot running on a Raspberry Pi (Linux ARM). Controls gates, heating, and sends crypto trading alerts via Telegram. Single-project, no tests, no CI.

## Build & run

```powershell
dotnet build
dotnet run                    # uses mock GPIO drivers in Development
dotnet build /p:OutputPath=build   # production-style build (matches install.sh)
```

`TELEGRAM_API_KEY` must be set (loaded from `.env` file at startup via DotNetEnv).

The Telegram.Bot SDK comes from a custom Azure DevOps feed (see `PodereBot.csproj` `RestoreSources`).

## Hardware drivers

| Environment | Pin driver | Temperature driver |
|-------------|-----------|-------------------|
| Development (ASPNETCORE_ENVIRONMENT=Development) | `MockPinDriver` | `MockTemperatureDriver` |
| Production (`SerialPort` set) | `SerialPinDriver` (COM4 @ 115200 baud) | `OneWireEmbeddedTemperatureDriver` |
| Production (no `SerialPort`) | `EmbeddedPinDriver` (System.Device.Gpio) | `OneWireEmbeddedTemperatureDriver` |

GPIO pins configured in `appsettings.json` `Pins` section.

## Bot commands

Commands auto-register via `[CommandMetadata(Key, Description, Admin)]` attribute and reflection. Commands with `Admin = true` require the sender's user ID to be in `appsettings.json` `Admins` list.

Interactive commands use `AttachEvents()`/`DetachEvents()` pattern for inline keyboard callbacks. Messages queued for deletion via `DeleteOnDetach()` are cleaned up on detach.

Bot language is **Italian**.

## REST API

All endpoints are restricted to local network (192.168.x.x, 127.0.0.1). Binds to `http://0.0.0.0:5050`.

| Method | Path | Purpose |
|--------|------|---------|
| GET | `/api/status` | Process uptime + memory (used by systemd watchdog) |
| POST | `/api/temperature` | Accept external sensor readings |

API endpoints auto-register via `IEndpoint` reflection (same pattern as commands).

## Heating

Heating program format: `hh:mm-hh:mm@temp/hh:mm-hh:mm@temp` (24h). `HeatingProgram.TryBuild()` validates intervals are non-overlapping and sorted. `HeatingProgramDaemon` polls every `PollIntervalSeconds` and applies temperature hysteresis with `ToleranceSeconds` delay.

## Skins

JSON files in `Skins/` directory define GIF assets shown for bot actions. Active skin persisted in `db.json`. `SetSkinCommand` is currently **disabled** (attribute commented out).

## Database

Single-file JSON database (`db.json`) written to `AppContext.BaseDirectory` on every `Edit()` call. Reads and clones on every `Data` access. No migrations.

## Crypto trading

`CryptoAlertDaemon` subscribes to Binance WebSocket kline stream for `SOLUSDC` on 1h timeframe. Preloads ~50 historical klines on connect. Strategy: `AtrStochRsiEmaStrategy`. Alerts sent to subscribed Telegram users.

## Formatting

CSharpier (`.csharpierrc.json`): printWidth 140, tabWidth 4, no tabs.

## Deployment

`install.sh` handles: build, systemd unit creation (`poderebot.service`), watchdog service (`poderebot-wd.service` + timer). Run with `sudo ./install.sh <username>`.

Systemd watchdog polls `GET /api/status` every 10 minutes; restarts the service if it fails.

## What is NOT here

- No tests (no test project, no test framework)
- No CI/CD (`.github/workflows/` is empty)
- No code generation, no migrations, no build artifacts to commit
- No monitoring, no structured logging export
