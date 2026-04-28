# Jellystream

Jellystream is a proposed Jellyfin plugin focused on making AceStream playback reliable from the official Jellyfin Fire TV app.

## Primary Requirement

The main target client is Jellyfin for Fire TV. Every design choice should optimize for playback through Jellyfin's standard Android TV/Fire TV pipeline, without requiring custom client code, sideloaded players, or manual URL handling on the Fire TV device.

## Core Strategy

Expose AceStream sources to Jellyfin as normal Live TV channels via a generated M3U endpoint. Jellyfin Fire TV should only see regular TV channels and regular stream URLs.

The plugin should not send raw `acestream://` URLs to clients. It should resolve each AceStream content id server-side and expose a Jellyfin-compatible HTTP stream.

Preferred stream shape for Fire TV:

1. HLS (`.m3u8`) when the AceStream service or companion bridge can provide it.
2. MPEG-TS over HTTP when HLS is not available.
3. Jellyfin transcoded stream as fallback when direct playback fails.

## Fire TV Compatibility Requirements

- No custom Fire TV app changes.
- No external player dependency on Fire TV.
- Playback must start from Jellyfin's Live TV UI.
- Channels must appear as normal Jellyfin Live TV channels.
- Stream URLs must be reachable by the Jellyfin server and by the playback path Jellyfin chooses.
- Startup delay must be hidden as much as possible with prebuffering.
- Failures must surface as clear Jellyfin/plugin errors, not silent player hangs.

## Architecture

- Jellyfin plugin in C#/.NET, based on the official Jellyfin plugin template.
- Admin configuration page for AceStream engine, playlist sources, and Fire TV compatibility options.
- Internal AceStream client that talks to an external AceStream engine/server.
- Generated M3U endpoint for Jellyfin Live TV integration.
- Optional generated XMLTV endpoint for guide data.
- Stream session manager for prebuffering, keepalive, concurrency limits, and cleanup.

## Key Components

### AceStream Client

- Healthcheck AceStream engine availability.
- Resolve `acestream://CONTENT_ID` and raw content ids.
- Start stream sessions before Jellyfin attempts playback.
- Track current playback URL and status.
- Apply strict timeout and cancellation behavior.

### M3U Provider

- Imports local and remote M3U/M3U8 playlists.
- Normalizes channel names, groups, logos, and `tvg-id` metadata.
- Deduplicates content ids.
- Generates a stable Jellyfin-facing M3U endpoint.
- Uses plugin-controlled stream URLs instead of raw AceStream URLs.

### Stream Endpoint

- Receives Jellyfin playback requests per channel.
- Starts or reuses an AceStream session.
- Waits for prebuffer readiness up to a configured timeout.
- Redirects to the best stream URL or proxies only when required.
- Returns explicit HTTP errors for offline, timeout, invalid content id, or concurrency limit.

### Fire TV Mode

Fire TV mode should be enabled by default and should prefer conservative playback behavior:

- Prefer HLS URLs if available.
- Prefer server-side prebuffering before returning playback.
- Avoid exotic codecs or container assumptions.
- Allow Jellyfin transcoding when direct playback is unreliable.
- Keep stream URLs stable during a playback session.
- Keep sessions alive briefly after stop to absorb app reconnects.

## Configuration

Minimum configuration:

- AceStream API base URL.
- AceStream playback base URL if different from API URL.
- Stream output preference: `HLS`, `MPEG-TS`, `Auto`.
- Fire TV compatibility mode: enabled by default.
- Prebuffer timeout seconds.
- Keepalive minutes after last access.
- Maximum concurrent streams.
- Playlist sources.
- Allowed remote playlist hosts.

Optional configuration:

- XMLTV sources.
- Manual channel-to-guide mappings.
- Hidden channels.
- Favorite groups.
- Custom user-agent for playlist fetches.

## Security Rules

- Do not bundle AceStream binaries or content lists.
- Do not ship preconfigured piracy sources.
- Validate content ids before use.
- Sanitize playlist names, logos, groups, and guide ids.
- Block local-network SSRF risks for remote playlist imports unless explicitly allowed.
- Use allowlists for playlist hosts.
- Never expose unauthenticated control endpoints beyond what Jellyfin requires.
- Log enough for diagnostics without leaking credentials or private playlist URLs.

## Implementation Phases

### Phase 1: Fire TV MVP

- Create installable Jellyfin plugin skeleton.
- Add plugin configuration page.
- Add AceStream healthcheck.
- Add M3U parser and generated M3U endpoint.
- Add per-channel stream endpoint.
- Resolve AceStream server-side and expose HTTP playback URLs.
- Test playback through Jellyfin Live TV with Fire TV compatibility mode enabled.

### Phase 2: Reliability

- Add prebuffering and readiness checks.
- Add session keepalive.
- Add concurrency limits.
- Add cleanup scheduled task.
- Add clear admin diagnostics.
- Add fallback from HLS to MPEG-TS when configured as `Auto`.

### Phase 3: Guide And UX

- Add XMLTV import endpoint.
- Add channel mapping UI.
- Add channel health indicators.
- Add favorite groups and hidden channels.
- Add playlist refresh scheduling.

### Phase 4: Hardening And Packaging

- Unit-test parser, validation, URL generation, and session state.
- Integration-test against a local AceStream-compatible endpoint.
- Document Docker Compose examples.
- Build release artifact and Jellyfin plugin manifest.
- Add upgrade notes per Jellyfin server version.

## Acceptance Criteria

- A configured AceStream channel appears in Jellyfin Live TV.
- The same channel starts playback from the official Jellyfin Fire TV app.
- No `acestream://` URL is ever sent to the Fire TV app.
- If AceStream is offline, the admin healthcheck reports it clearly.
- If a stream cannot start, Jellyfin receives a deterministic HTTP error instead of hanging indefinitely.
- Reopening the same channel shortly after stopping reuses or quickly restarts the session.

## Current MVP

The repository now contains the first server-side MVP scaffold:

- `Jellyfin.Plugin.Jellystream.sln`
- `Jellyfin.Plugin.Jellystream/Jellyfin.Plugin.Jellystream.csproj`
- Jellyfin plugin entry point and configuration model.
- M3U parser for `acestream://CONTENT_ID`, raw 40-character content ids, and HTTP URLs containing `id`, `content_id`, or `contentId` query parameters.
- Generated Jellyfin-facing M3U endpoint at `/Jellystream/Playlist.m3u`.
- Healthcheck endpoint at `/Jellystream/Health`.
- Channel listing endpoint at `/Jellystream/Channels`.
- Stream endpoint at `/Jellystream/Stream/{channelId}`.
- Diagnostic raw stream endpoint at `/Jellystream/StreamByContentId/{contentId}`.

The default playback mode is proxied through Jellyfin. This is intentional for Fire TV because the Fire TV app should only communicate with Jellyfin, not directly with AceStream or Docker-internal addresses.

## Build

This project targets Jellyfin `10.11.3` and `net9.0`, matching the current upstream plugin template guidance.

Install the .NET SDK, then run:

```bash
dotnet restore Jellyfin.Plugin.Jellystream.sln
dotnet build Jellyfin.Plugin.Jellystream.sln -c Release
```

For a manual Jellyfin install, publish the plugin and copy the output to a Jellyfin plugin folder:

```bash
dotnet publish Jellyfin.Plugin.Jellystream.sln -c Release
mkdir -p "$HOME/.local/share/jellyfin/plugins/Jellystream"
cp Jellyfin.Plugin.Jellystream/bin/Release/net9.0/publish/* "$HOME/.local/share/jellyfin/plugins/Jellystream/"
```

Restart Jellyfin after copying the plugin.

## Install From Plugin Repository

Add this repository URL in Jellyfin:

```text
https://raw.githubusercontent.com/tsulhc/jellystream/main/manifest.json
```

Jellyfin path:

```text
Dashboard -> Plugins -> Repositories -> Add
```

After adding the repository, open the plugin catalog, install `Jellystream`, and restart Jellyfin.

Release packages are published as GitHub release assets. The manifest points to the latest compatible zip and includes Jellyfin's required MD5 checksum.

## Package A Release

Create a local release zip and checksum with:

```bash
bash scripts/package-plugin.sh 0.1.0.0
```

This writes:

```text
dist/jellystream_0.1.0.0.zip
dist/jellystream_0.1.0.0.zip.md5
```

## First Manual Test

Set `InlineM3U` in the plugin configuration XML to a small test playlist:

```m3u
#EXTM3U
#EXTINF:-1 tvg-id="test" group-title="AceStream",Test Channel
acestream://0123456789abcdef0123456789abcdef01234567
```

Then add the generated playlist URL to Jellyfin Live TV as an M3U tuner:

```text
http://YOUR_JELLYFIN_HOST:8096/Jellystream/Playlist.m3u
```

The Fire TV app should see the channel through Jellyfin Live TV. The channel stream URL in the generated M3U points back to Jellyfin, not to AceStream.

## AceStream URL Template

The default playback template is:

```text
/ace/getstream?id={contentId}
```

Different AceStream bridges expose different paths. Keep `AceStreamPlaybackBaseUrl` as the base server URL and adjust `PlaybackUrlTemplate` to match the bridge being used.

## Existing Code Policy

No external code should be copied into the plugin until it has been reviewed for license compatibility and security. Existing AceStream HTTP API clients may be used as references, but the first implementation should be a small native C# client tailored to the exact API calls needed by the plugin.
