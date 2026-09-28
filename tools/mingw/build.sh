#!/bin/bash
# Cross-compile map-server.exe (Win32, pre-renewal per src/config) with MinGW i686
set -e
R=${R:-$(cd "$(dirname "$0")/../.." && pwd)}
O=${O:-$R/build-mingw}
SRV=${SRV:-map}
mkdir -p $O
CXX=i686-w64-mingw32-g++-posix
CC=i686-w64-mingw32-gcc-posix
INC="-I$R/tools/mingw/case_shim -I$R/src -I$R/3rdparty/rapidyaml/src -I$R/3rdparty/rapidyaml/ext/c4core/src -I$R/3rdparty/libconfig -I$R/3rdparty/mysql/include -I$R/3rdparty/pcre/include -I$R/3rdparty/zlib/include -I$R/3rdparty/json/include"
DEF="-DLIBCONFIG_STATIC -DYY_NO_UNISTD_H -DPCRE_SUPPORT -DWIN32 -D_WIN32_WINNT=0x0601 -DNDEBUG"
CXXFLAGS="-std=c++17 -O2 -g0 -pipe -fno-strict-aliasing -Wno-deprecated-declarations $DEF $INC"
CFLAGS="-O2 -pipe $DEF $INC"
build() { # src obj compiler flags
  local src=$1 obj=$O/$(echo "$1" | sed "s#$R/##; s#/#_#g; s#.*winapi_mingw#winapi_mingw#").o
  if [ ! -f "$obj" ] || [ "$src" -nt "$obj" ] || [ -n "$FORCE" ]; then
    echo "CC $src"; $2 $3 -c "$src" -o "$obj"
  fi
}
export -f build; export R O SRV CXX CC CXXFLAGS CFLAGS
{
  { ls $R/src/common/*.cpp | grep -v winapi.cpp; echo $R/tools/mingw/winapi_mingw.cpp; ls $R/src/$SRV/*.cpp; } | sed "s#\$# CXX#"
  ls $R/3rdparty/rapidyaml/src/c4/yml/*.cpp $R/3rdparty/rapidyaml/ext/c4core/src/c4/*.cpp | sed "s#\$# CXX#"
  ls $R/3rdparty/libconfig/*.c | sed "s#\$# CC#"
} | xargs -P4 -L1 bash -c 'if [ "$1" = CXX ]; then build "$0" "$CXX" "$CXXFLAGS"; else build "$0" "$CC" "$CFLAGS"; fi'
echo LINK
$CXX -static -static-libgcc -static-libstdc++ -o $O/$SRV-server.exe $(ls $O/*.o | grep -v "src_[a-z]*_" ; ls $O/src_common_*.o $O/src_${SRV}_*.o) \
  $R/3rdparty/mysql/lib/Win32/libmysql.dll $R/3rdparty/pcre/lib/Win32/pcre8.dll $R/3rdparty/zlib/lib/Win32/zlib.dll \
  -lws2_32 -lwinmm -liphlpapi
ls -la $O/$SRV-server.exe
