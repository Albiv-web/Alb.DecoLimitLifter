# Deco Limit Lifter (From The Depths)

> Lift blueprint decoration limits (~100k decos) by extending the blueprint header format.  
> Backward-compatible loader; saver writes legacy when possible, sentinel when needed.

## What it does
- Replaces the blueprint header length encoding:
  - **Legacy path**: 2-byte header length + varint data → used when it fits, keeps vanilla compatibility.
  - **Sentinel path**: `0xFFFF` + 4-byte headerLen + 4-byte dataLen → used for large blueprints.
- Loader reads **both** formats. Saver prefers legacy when it fits, or sentinel if not.
