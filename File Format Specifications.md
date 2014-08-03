[TOC]

# Assumptions
All values are in little-endian byte order unless otherwise specified.

|
| byte   | Unsigned 1-byte integer |
| char   | 1-byte fixed-width character |
| ushort | Unsigned 2-byte integer |
| uint   | Unsigned 4-byte integer |
| float  | Single-precision floating-point number |

# BAR
```cpp
<char*4> Signature "BAR\x01" [0x42, 0x41, 0x52, 0x01]
<uint> # of files
<byte*8> Padding
for each file:
    <uint> Type
    <char*4> ID
    <uint> Offset
    <uint> Size
<byte*?> Raw data
```

## String Table (Bar Type 0x02)
```cpp
<uint> Version? Always 1
<uint> # of strings
for each string:
    <uint> ID
    <uint> Offset
<byte*?> Raw data
```
* Strings are read until a `NULL` byte is read. Note that there are text commands that accept `NULL` as an argument; these will _not_ signal the end.

## eventviewer (Bar Type 0x02)
```cpp
<uint> Version? Always 1
<uint> # of entries
for each entry:
    <ushort> IsSubLevel? (0 = top-level, 1 = sub-level)
    <ushort> String ID
    <ushort> World ID of event?
    <ushort> English event ID
    <ushort> Japanese event ID
    <ushort> Restriction ID (0 = none)
```

# IDX
```cpp
<uint> # of files
for each file:
    <uint> Main hash
    <ushort> Secondary Hash
    <ushort> Compression Flags
    <uint> LBA offset in IMG
    <uint> Uncompressed size
```
* LBA is in units of 2048 bytes. `LBA offset` of 1 is equal to a byte offset of 2048.
* `Compression Flags` is partially bit-field, partially integer.
    * `this & 0x8000` specifies whether the `Secondary Hash` is used. If it's set both hashes are checked, otherwise  only `Main hash` is checked.
    * `this & 0x4000` specifies whether the file is compressed or not.
    * `((this & 0x3FFF) + 1) * 2048` is the compressed size in bytes. If the file is not compressed, use `Uncompressed size` instead.
        * This field can store a maximum of 32MB, even though larger non-compressed files are allowed.
        * There is a weird occurrence in existing IDX files, where the `0x1000` bit is _never_ set. This mainly affects non-compressed files, but at least one known compressed file is affected: `10303F6F`. Compressed files affected by this cannot be properly read without special handling.

## IMG
```cpp
<byte*?> Raw Data
```

# advice.bin
```cpp
<uint> ?
<uint> # of items
for each item:
    <uint> Image number (Treated in some places as a signed byte; "advice_%03d.imz")
    <uint> Slot A?
    <uint> Slot B?
    <uint> Slot C?
    <uint> ?
    <uint> Page number (Treated in some places as a signed byte)
    <byte*40> ?
```
