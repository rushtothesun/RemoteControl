# RemoteControl

A DreamPoeBot plugin that lets you control multiple bot instances over your LAN without using in-game chat commands.

## What it does

RemoteControl runs a lightweight TCP/HTTP listener on each bot instance. A commander (either the built-in WPF panel or the standalone RemoteCommander app) sends JSON commands over the network, which get translated into native DreamPoeBot messages and forwarded to FollowBot.

This replaces the old approach of typing commands into party chat, which was slow and added detection risk.

## How it works

- Each bot runs a `TcpCommandServer` bound to a private LAN IP (192.168.x.x, 10.x.x.x, etc.) or loopback
- The commander sends HTTP POST requests with `{"command":"StartFollow"}` style payloads
- `CommandDispatcher` maps command strings to FollowBot's internal `RC_` message IDs
- Bot lifecycle commands (`BotStart`, `BotStop`) are handled directly without going through FollowBot
- Parameterized commands like `SetUltTimer:15` pass values through the DreamPoeBot message system

## Available Commands

| Command | What it does |
|---------|-------------|
| StartFollow / StopFollow | Toggle following |
| StartAttack / StopAttack | Toggle combat |
| StartLoot / StopLoot | Toggle looting |
| StartPortal / StopPortal | Toggle auto-TP |
| Teleport | Teleport to leader |
| OpenPortal | Open a town portal |
| EnterPortal | Enter a nearby portal |
| Stash | Go stash inventory |
| NewInstance | Create new map instance |
| FollowTownOn/Off | Toggle follow in town |
| FollowHideoutOn/Off | Toggle follow in hideout |
| FollowHeistOn/Off | Toggle follow in heist |
| AutoDepositOn/Off | Toggle auto-deposit |
| UseGuildStash / UseRegularStash | Switch stash type |
| UltPortalOn/Off | Toggle portal after Ultimatum |
| SetUltTimer:N | Set Ultimatum portal search time (seconds) |
| Unloader | Trigger Ultimatum unloader sweep |
| SetUnloaderDelay:N | Set unloader start delay (ms) |
| BotStart / BotStop | Start/stop the bot (handled directly, not through FollowBot) |

## Setup

1. Drop the RemoteControl folder into `3rdParty/`
2. Enable the plugin in DreamPoeBot
3. Configure the listen port in settings (default: 5200)
4. Point your commander at `<bot-ip>:5200`

## Companion App

The standalone [RemoteCommander](https://github.com/rushtothesun/RemoteCommander) app provides a WPF control panel for sending commands to all your bots from one place. It doesn't require DreamPoeBot to run — just point it at your bot IPs.

## Requirements

- DreamPoeBot with FollowBot installed
- .NET Framework 4.8
- Bots must be on the same LAN (or use loopback for local testing)
