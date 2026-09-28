// MinGW has no MSVC structured exception handling; the original code only
// uses __try/__leave/__finally as a cleanup block, so map it to do/break/while.
#define __try do
#define __leave break
#define __finally while (0);
#include "../../src/common/winapi.cpp"
