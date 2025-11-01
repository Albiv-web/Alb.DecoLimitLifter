# Deco Limit Lifter (From The Depths)

Lift the ~5k decoration cap in **From The Depths** by extending the blueprint save format. Small crafts keep vanilla compatibility; huge crafts gain headroom without corrupting saves.

> **TL;DR**
>
> * Saves and loads **vanilla** format when it fits (under the usual limits).
> * Automatically switches to an extended **sentinel** format only when required (big crafts).
> * Vanilla FtD can load **legacy** saves; **sentinel** saves require this mod.

---

## Why this exists

FtD blueprints store a header and a data block. With lots of decorations, those lengths exceed vanilla’s 16-bit limits. This mod writes a backwards-compatible “legacy” layout when it fits and a “sentinel” layout when it doesn’t, so you can build big without breaking small.

---

## Features

* Backwards-compatible load path for both **legacy** and **sentinel** saves.
* Auto-switching saver: prefers **legacy**, escalates to **sentinel** only when needed.
* Sensible default buffers for small crafts; safe **auto-grow** for large crafts.
* Runtime soft cap for decorations (default 100k).
* Capacity guards to prevent index out-of-range and save corruption.
* Minimal overhead for normal-sized builds; predictable growth for huge ones.

---

## Requirements

* From The Depths (PC)
* Harmony patching (the mod uses Harmony attributes under the hood)
* Windows-compatible .NET runtime included with the game (no separate install for end-users)

---

## Installation

1. Download the release (or build from source).
2. Copy the entire **DecoLimitless** folder into your FtD **Mods** folder.

   * Typical paths:

     * Windows (Steam):
       `...\Steam\steamapps\common\From The Depths\Mods\`
     * Linux (Proton) path varies; place alongside other working mods.
3. Start the game. Enable the mod in the Mod Manager if it isn’t already.
4. On first load you may see a brief “patch confirmation” style message — this is expected and indicates Harmony applied the patches successfully. Will be removed later. 

---

## Usage

Just play as normal.

* Small/normal crafts (<5k decos): saves stay **vanilla**; fully shareable with un-modded players.
* Big crafts (>5k decos): saves switch to **sentinel** automatically; requires this mod to load.

---

## Configuration (advanced)

Tuning knobs live in `DecoLimits.cs`. Keep default values unless you consistently hit the guardrails.

| Key                  |     Default | What it controls                     |
| -------------------- | ----------: | ------------------------------------ |
| `MaxDecorations`     |     100 000 | Runtime soft cap on decorations      |
| `SaveBufferBytes`    |  20 000 000 | Global save buffer floor (ByteStore) |
| `MaxSaveBufferBytes` | 268 435 456 | Hard ceiling for global buffer       |
| `MaxDataSortedBytes` |  67 108 864 | Hard ceiling for data block          |
| `MaxHeaderBytes`     |   4 194 304 | Hard ceiling for header block        |

**Rule of thumb for giant builds**
Recommended `SaveBufferBytes` ≈ `20 MB` + `220 bytes × decoration_count` (rounded up to next power of two).

---

## Compatibility & limits

* **Vanilla FtD** can load **legacy** saves. It **cannot** load **sentinel** saves.
* **Sentinel** saves are intended for single-player. Multiplayer has not been tested; don’t rely on it.
* Extremely large blueprints are ultimately bounded by your configured ceilings.

---

## Troubleshooting

* **“Save buffer too small. Need X bytes, have Y.”**
  Raise `SaveBufferBytes` (e.g., 20 MB → 32 MB or 64 MB). The guard prevented corruption; you just need a larger outer buffer.

* **“Harmony failed to patch (explicit interface)”**
  Usually a version mismatch. Update the mod to match your game build so method signatures resolve correctly.

* **Performance feels worse on small saves**
  Ensure you didn’t crank the buffer ceilings unnecessarily. Defaults keep small saves snappy while allowing on-demand growth.

---

## How it works (short)

### Saver formats

* **Legacy (vanilla-compatible)**

  ```
  [UInt16 headerLen]
  [UInt16 pad=0]
  [dataLen split into ≤100 chunks of UInt16]
  [header bytes]
  [data bytes]
  ```

* **Sentinel (extended)**

  ```
  [0xFFFF]
  [UInt32 headerLen]
  [UInt32 dataLen]
  [header bytes]
  [data bytes]
  ```

The loader reads **both** layouts. The saver writes **legacy** whenever possible, and **sentinel** only if any length exceeds vanilla limits.

### Buffering strategy

* Global outer buffer (`ByteStore.MegaBytes`) is ensured large enough at boot and never shrinks during the session.
* Header/Data arrays grow on demand to the next power-of-two, clamped by hard ceilings.
* Small saves reuse vanilla-sized pools; large saves take a few predictable growth steps.

---

## Building from source

> Target framework: **.NET Framework 4.7.2**
> IDE: Visual Studio 2017+ (or `msbuild`)

1. Open `DecoLimitLifter.sln`.
2. Add references to the game’s assemblies (e.g., `BrilliantSkies.*`) from your FtD installation.
3. Build `Release`.
4. Copy the produced DLL and mod files into `From The Depths\Mods\DecoLimitless\`.

**Notes**

* No external NuGet packages are required for the core patches.
* If you change any constants in `DecoLimits.cs`, keep the ceilings sane to avoid OOM.

---

## Performance expectations

* Small blueprints: no extra allocations on save/delete.
* Large blueprints: bounded growth; guardrails prevent overruns and corruption.
* Debug logging is quiet by default; only a brief startup confirmation is shown.

---

## Sharing blueprints

* **Legacy**: freely share with anyone (vanilla loads fine).
* **Sentinel**: share only with players using this mod.

---

## Contributing

Issues, bugs and PRs are welcome. Please include:

* Game build number and steps to reproduce.
* A minimal blueprint (or counts) that triggers the problem.
* Logs or exact exception messages.

You can also DM me on Discord: **albeeettt**.

---

## Roadmap / future ideas

* Safer multiplayer behavior (investigation).
* Config UI for buffer sizing hints and ceilings.
* Additional diagnostics toggles, sentinel / legacy pre-viewer. 

---

## Credits

* Built with **Harmony**; thanks to community members who helped with early testing and interfaces: DeltaEpsilon & wolficik

---

## License

MIT. See `DecoLimitless/LICENSE`.
