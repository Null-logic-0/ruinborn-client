# Ruinborn Client

Ruinborn Client is a Unity prototype for the Ruinborn real-time multiplayer experiment.

This is not intended to be presented as a polished production game. I am not a game developer; the main purpose of this client was to test an Elixir/Phoenix backend and see what it can handle for real-time multiplayer-style traffic: joins, movement updates, attacks, health changes, match countdowns, deaths, and match-end events.

The game client exists mostly as a playable test surface for the separate Ruinborn Elixir project.

## About The Ruinborn Elixir Project

The backend lives in the `ruinborn` Phoenix application. It uses Phoenix Channels, PubSub, a registry, and dynamically supervised match processes to run simple two-player matches.

The Elixir side handles:

- WebSocket connections through Phoenix Channels
- Match rooms using topics like `match:<match_id>`
- Two-player room capacity
- Player join and leave events
- Countdown and match start flow
- Player position updates
- Server-side attack resolution
- HP updates, deaths, and match-ended events

This Unity project connects to that backend and acts as a rough game client so the backend can be tested with real movement and combat messages instead of only unit tests or manual WebSocket calls.

## What This Unity Client Does

- Connects to the Phoenix server with a `player_id`
- Joins a configured match room
- Sends local player position updates
- Spawns and moves remote players from server events
- Sends attack events with the current weapon and position
- Reacts to HP, death, countdown, match start, and match end events
- Provides a simple menu and third-person combat scene

## Project Details

- Engine: Unity `6000.4.11f1`
- Render pipeline: Universal Render Pipeline
- Networking: NativeWebSocket
- JSON: Newtonsoft JSON
- Main scenes:
  - `Assets/Scenes/MainMenu.unity`
  - `Assets/Scenes/SampleScene.unity`

Important scripts:

- `Assets/Scripts/RuinbornNetwork.cs` handles Phoenix WebSocket connection, joins, heartbeats, inbound events, and outbound pushes.
- `Assets/Scripts/NetworkSender.cs` sends player movement.
- `Assets/Scripts/GameManager.cs` manages remote player spawning, movement, HP, and death events.
- `Assets/Scripts/CombatController.cs` handles local weapon switching, attack animation, and attack messages.
- `Assets/Scripts/MainMenuManager.cs` loads the playable scene from the menu.

## Configuration

The client expects a JSON config file at:

```text
Assets/StreamingAssets/config.json
```

Example:

```json
{
  "server_url": "ws://localhost:4000/socket/websocket",
  "match_id": "room_1"
}
```

The client appends the `player_id` and Phoenix protocol version automatically:

```text
?player_id=<player_id>&vsn=2.0.0
```

## Running Locally

1. Start the Ruinborn Phoenix backend.

```sh
cd /path/to/ruinborn
mix setup
mix phx.server
```

2. Open this Unity project in Unity `6000.4.11f1` or a compatible Unity 6 version.
3. Confirm `Assets/StreamingAssets/config.json` points to the Phoenix server.
4. Open `Assets/Scenes/MainMenu.unity`.
5. Press Play.

To test multiplayer behavior, run two clients with different player IDs, or clear/change the saved `ruinborn_player_id` value between runs.

## Phoenix Channel Events

Inbound events sent by the Unity client:

- `move` sends the local player position.
- `attack` sends the selected weapon and attacker position.
- `heartbeat` keeps the Phoenix socket alive.

Outbound events handled by the Unity client:

- `player_joined`
- `player_left`
- `player_moved`
- `hp_update`
- `player_died`
- `countdown`
- `match_start`
- `match_ended`

## Notes

This repository includes third-party Unity assets and packages. Their own license terms may differ from this project's MIT license. Check the relevant asset folders and package sources before redistributing builds or assets.

The code is experimental and learning-oriented. The interesting part of the project is the loop between Unity and the Elixir/Phoenix backend: using a simple client to pressure-test real-time server behavior.

## License

Ruinborn Client code is released under the MIT License. See [LICENSE](LICENSE) for details.
