# Deco Limit Lifter (From The Depths)

First release. Might be bugs I haven't encountered: Please DM me if you encounter any, Discord username: albeeettt

Confirm patch on startup (looks like an error, isn't).

> Lift blueprint decoration limits (tested ~100k) by extending the save format.
> Backward-compatible on load; the saver writes **legacy** when it fits and **sentinel** only when needed.
> Not tested in multiplayer. I don't advise it either. 

## Usage

* Play as normal.
* Small crafts save exactly like vanilla (<5k decos).
* Big crafts (>5k decos) save using the extended format automatically.

> Sharing note: **Legacy** saves load fine in vanilla (anything under 5k decos). **Sentinel** saves require this mod.

## Limitations

* Vanilla FtD can’t load **sentinel** blueprints. Keep within legacy limits if you need to share without this mod.
* Extremely huge blueprints are bounded by your configured ceilings (`Max*Bytes`).

## What it does 

* Replaces the blueprint header/data length encoding, and ups limits to support >5k decos. 

  * **Legacy path** – 2-byte header length + chunked (16-bit) data length.
    Used automatically when everything fits to keep vanilla compatibility.
  * **Sentinel path** – `0xFFFF` + 4-byte `headerLen` + 4-byte `dataLen`.
    Kicks in only for large blueprints (more decos / more data).
* The loader reads **both** formats. The saver prefers **legacy** and switches to **sentinel** only if required.

## Highlights

* On-demand buffer growth (no giant pools by default).
* Run-once pool init; no per-save reallocation thrash.
* Global save buffer (`ByteStore.MegaBytes`) sized sensibly at boot, with safe auto-grow.
* Capacity guards before each write prevent overflows and index OOR.
* Keeps small/vanilla blueprints snappy; unlocks huge decos when you need them.

## How it works (short)

* Harmony patches route `SuperLoader.Deserialise` and `SuperSaver.Serialise` to extended implementations.
* If header/data exceed the legacy limits, the saver writes the **sentinel** format; otherwise it writes **vanilla**.
* A tiny startup patch resizes `ByteStore.MegaBytes` (the big blueprint buffer) and never shrinks it.
* Capacity guards top up `Header` and `DataSorted` just-in-time (next power-of-two, bounded by caps).

## Configuration (advanced)

* Central knobs live in `DecoLimits.cs`:

  * `MaxDecorations` – runtime soft cap we set on the game’s decoration manager.
  * `SaveBufferBytes` – floor for `ByteStore.MegaBytes` (default 20 MB).
    Increase if you build truly massive blueprints.
  * `MaxSaveBufferBytes`, `MaxDataSortedBytes`, `MaxHeaderBytes` – safety ceilings for growth.

## Performance 

* Small blueprints: zero extra allocations on save/delete.
* Large blueprints: a handful of predictable growth steps (power-of-two) with hard caps.
* No UI spam; debug logging is off by default. Except confirming successful patch on startup (looks like an error, isn't). 

## Troubleshooting

* **Error:** “Save buffer too small. Need X bytes, have Y.”
  **Fix:** raise `SaveBufferBytes` (20 MB → 32 MB or 64 MB). The mod prevented corruption; you just need a bigger buffer.
* **Harmony failed to patch (explicit interface):**
  You’re likely on a different build. Ensure the mod files are up to date; the loader uses an interface map to resolve `ByIdHelpWrite`.

## What changes in the save file?

* **Legacy** blueprints are unchanged (still vanilla-readable).
* **Sentinel** blueprints add: `0xFFFF` + `uint32 headerLen` + `uint32 dataLen`, followed by the usual header/data bytes.

## Credits

* Built with Harmony, thanks to DeltaEpsilon and wolficik for helping me get started. 

