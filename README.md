# Reasonable Live Player (RLP)

Reasonable Live Player automates live set playback in [Reason](https://www.reasonstudios.com/). It opens Reason song files in sequence and advances to the next song when a MIDI trigger note is received — hands-free transitions for live performance.

## Features

- Build and save playlists of `.reason` / `.rns` song files
- Drag and drop files from Windows Explorer into the playlist
- Reorder songs by dragging within the list
- Advance to the next song automatically via a MIDI trigger note
- Skip, pause, and resume the playlist at any time
- Select the next song to play while paused
- Configurable MIDI device, channel, and trigger note
- Configurable transition delay between songs
- Always-on-top mode for live use
- Remembers your last playlist on startup

## System Requirements

| Requirement | Details |
|---|---|
| **Operating System** | Windows 10 or later (64-bit) |
| **Reason** | Reason 13 (or later) installed — RLP opens `.reason` / `.rns` files via shell association |
| **.NET Runtime** | [.NET 8.0 Desktop Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) (x64) — download the **Desktop Runtime**, not just the base runtime |
| **Virtual MIDI Driver** | [LoopBe1](https://www.nerds.de/en/loopbe1.html) (free, recommended) or any virtual MIDI loopback driver |

### Why a virtual MIDI driver?

RLP listens for a MIDI note to know when to advance to the next song. Reason sends that note out through an External MIDI Instrument device in the rack. A virtual MIDI driver like LoopBe1 creates an internal MIDI cable that connects Reason's output to RLP's input — no physical hardware needed.

## Installation

1. Install the [.NET 8.0 Desktop Runtime (x64)](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) if you don't already have it.
2. Install [LoopBe1](https://www.nerds.de/en/loopbe1.html) (or another virtual MIDI loopback driver).
3. Download `ReasonableLivePlayer.exe`:
   - **Pre-built**: grab the latest from the [Releases](https://github.com/t8bloom1/ReasonableLivePlayer/releases) page (when available).
   - **Build it yourself**: see [Building from Source](#building-from-source) below.
4. Place it anywhere you like and run it — no installer needed.

## Quick Start

1. Open RLP and click **＋** (or drag `.reason` files from Explorer) to build your playlist.
2. Click **⚙** (Settings) and select your virtual MIDI device (e.g. "LoopBe Internal MIDI"), channel, and trigger note number.
3. The MIDI indicator dot turns **green** when connected.
4. Press **▶** to start — RLP opens the first song in Reason.
5. When Reason plays the trigger note, RLP closes the current song and opens the next one.

## Setting Up Reason Songs

Each song in your set needs a small bit of setup so it can tell RLP when to advance. This is done using Reason's **External MIDI Instrument** device.

### Step-by-step

1. **Create an External MIDI Instrument device**
   - In Reason's rack, right-click and choose *Create > Players > External MIDI Instrument*.

2. **Configure its MIDI output**
   - On the External MIDI Instrument panel, set the **MIDI Output** to your virtual MIDI device (e.g. "LoopBe Internal MIDI").
   - Set the **MIDI Channel** to match the channel configured in RLP Settings (default: channel 1).

3. **Add a trigger note in the sequencer**
   - Create a sequencer lane for the External MIDI Instrument.
   - At the point in the song where you want RLP to advance (typically the very end), draw a single note.
   - The note's **pitch** must match the trigger note number configured in RLP Settings (default: C0 / note 0).
   - The note can be very short — RLP only needs the note-on event.

4. **Test it**
   - Start RLP with at least two songs in the playlist and press ▶.
   - Play the Reason song. When it reaches the trigger note, RLP should close it and open the next song.

### Tips

- Keep a blank or "standby" Reason file open in the background to keep Reason loaded in memory. This makes song transitions faster.
- If the next song doesn't open reliably, increase the **Transition Delay** in RLP Settings (default: 5 seconds). This gives Reason time to finish closing the previous song.
- You can use any note number as the trigger — just make sure RLP Settings and the External MIDI Instrument note match.
- LoopBe1 is free for personal use and installs a single virtual MIDI port, which is all RLP needs.

## Toolbar Reference

| Button | Action |
|---|---|
| ▶ / ⏸ | Play or pause the playlist |
| ⏭ | Skip to the next song |
| ＋ | Add `.reason` / `.rns` files |
| 💾 | Save playlist (`.rlp`) |
| 📂 | Open a saved playlist |
| ⚙ | Settings (MIDI device, channel, note, transition delay) |
| 📌 | Toggle always-on-top |
| ? | Help |

## Building from Source

```bash
# Clone the repository
git clone https://github.com/t8bloom1/ReasonableLivePlayer.git
cd ReasonableLivePlayer

# Build
dotnet build ReasonLivePlayer/ReasonLivePlayer.csproj

# Run tests
dotnet test ReasonLivePlayer.Tests/ReasonLivePlayer.Tests.csproj

# Publish single-file executable
dotnet publish ReasonLivePlayer/ReasonLivePlayer.csproj -c Release -r win-x64 -o dist
```

The published executable will be at `dist/ReasonableLivePlayer.exe`.

## License

This project is licensed under the [MIT License](LICENSE).
