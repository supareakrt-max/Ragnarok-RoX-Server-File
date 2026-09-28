# Cross-compile the Windows (Win32) servers with MinGW

The Ro-X server runs 32-bit Windows binaries linked against the DLLs in
`3rdparty/*/lib/Win32` (`libmysql.dll`, `pcre8.dll`, `zlib.dll`). This script
builds them on Linux, so no Visual Studio is needed.

```bash
sudo apt-get install g++-mingw-w64-i686
# src/custom is not in git: put the Ro-X custom files there first
mkdir -p src/custom && cp ai_system/src/custom/* src/custom/
# the other src/custom files must exist (comment-only stubs are fine)
SRV=map   tools/mingw/build.sh     # -> build-mingw/map-server.exe
SRV=char  tools/mingw/build.sh
SRV=login tools/mingw/build.sh
```

- `case_shim/`: Windows headers are included with mixed case (`<Windows.h>`),
  and MinGW on Linux uses lowercase file names.
- `winapi_mingw.cpp`: compiles `src/common/winapi.cpp`, which uses MSVC-only
  `__try/__finally`, as a plain cleanup block.
- The exe changes its working directory to its own folder at startup, so it
  must sit in the server root next to `conf/`, `db/` and `npc/`.
