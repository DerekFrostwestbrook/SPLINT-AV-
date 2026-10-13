# SPLINT community build SDK

This SDK builds the open source FREE and PRO editions. The commercial MAX and MAX-NATIVE source code and release build tools are not included in this repository.

## Requirements

Use Windows x64 with the .NET SDK, .NET Framework reference assemblies, Python 3, and a valid `hash.txt` file containing one SHA 256 digest per line. The hash file is data supplied by the person producing the build and is not included in these source folders.

## Build

Run `build_splint.cmd` from this directory. The executables are written to `dist\FREE` and `dist\PRO`. Set `SPLINT_OUTPUT_DIR` to use another output folder and `SPLINT_HASH_SOURCE` to point at a different hash list.

FREE does not require an activation key. To produce an activatable PRO build, set `SPLINT_PRO_KEY_HASH` to the 64 character SHA 256 hash of a key issued for that build. Without it, the SDK uses a development value and PRO will explain that activation has not been configured. The build does not contact a license server and does not issue or reveal keys.

The SDK does not build or publish MAX or MAX-NATIVE.

## Source licenses

FREE is licensed under the 0BSD License in `free\LICENSE`.

PRO is licensed under the MIT License in `pro\LICENSE`. Keep its copyright and license notice in redistributed copies and substantial portions.

The malware hash list and third party libraries or data are not relicensed by these source licenses. Verify the terms and provenance of any data supplied to a build.
