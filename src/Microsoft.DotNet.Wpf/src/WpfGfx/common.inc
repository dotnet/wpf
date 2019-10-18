// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


##------------------------------------------------------------------------------
## Licensed to the .NET Foundation under one or more agreements.
## The .NET Foundation licenses this file to you under the MIT license.
## See the LICENSE file in the project root for more information.
##
##  File:       common.inc
##------------------------------------------------------------------------------

!INCLUDE $(BASEDIR)\tools\devdiv\sources.inc

SOURCES_USED=$(WGX_PATH)\common.inc

## Includes --------------------------------------------------------------------

#
# SPLIT-DEPRECATED: Seems as if CONDITIONAL_INCLUDES is not used anywhere. 
# Commeting them out to see if anything breaks.
#
# CONDITIONAL_INCLUDES= \
#    winheap.h \
#    winwlm.h \
#    rpcerr.h \
#    rpcmac.h \
#    macname1.h \
#    macpub.h \
#    macapi.h \
#    macname2.h \
#    macocidl.h \
#    stdole2.tlb \

WGXINCLUDES=\
    $(WGX_PATH)\Shared\Inc; \
    $(WGX_PATH)\Include; \
    $(WGX_PATH)\Include\Generated; \
    $(WGX_PATH)\External\inc; \


## Lib includes ----------------------------------------------------------------
# You should refer to these in your sources files instead of hardcoding
# the path to any particular lib.

WGX_CORETEST_LIB=$(WGX_OBJ_PATH)\core\testdll\$(O)\milcoretest.lib
WGX_EXEUTIL_LIB=$(WGX_OBJ_PATH)\Shared\Util\ExeUtil\$(O)\ExeUtil.lib
WGX_DLLUTIL_LIB=$(WGX_OBJ_PATH)\Shared\Util\DllUtil\$(O)\DllUtil.lib
WGX_CONUTIL_LIB=$(WGX_OBJ_PATH)\Shared\Util\ConUtil\$(O)\ConUtil.lib
WGX_UTIL_LIB=$(WGX_OBJ_PATH)\Shared\Util\UtilLib\$(O)\Util.lib
WGX_DEBUG_LIB=$(WGX_OBJ_PATH)\Shared\Debug\DebugLib\$(O)\DebugLib.lib

#
# GSchneid: WCP* variables deprecated for WCP Graphics. Instead use the WGX 
# constants. This helps cut down on the number of root variables have to be 
# adjusted when moving stuff around.
#

# WCP_EXEUTIL_LIB=$(WGX_EXEUTIL_LIB)
# WCP_DLLUTIL_LIB=$(WGX_DLLUTIL_LIB)
# WCP_CONUTIL_LIB=$(WGX_CONUTIL_LIB)
# WCP_UTIL_LIB=$(WGX_UTIL_LIB)
# WCP_DEBUG_LIB=$(WGX_DEBUG_LIB)


## Other stuff -----------------------------------------------------------------

# Lots of warnings is good
WARNING_LEVEL=W4

# Wp64 is used to try to catch as many 64-bit port issues as can be detected
# on a 32-bit build.
MSC_WARNING_LEVEL=-$(WARNING_LEVEL) -WX -Wp64

# Some are not so good, however.
USER_CS_FLAGS=$(USER_CS_FLAGS) /nowarn:0679

# standard defines
C_DEFINES = $(C_DEFINES) \
    /D_WCTYPE_INLINE_DEFINED \
    /DSTRICT \
    /DUNICODE \
    /D_UNICODE \

#
# There is a compiler bug that stopped supporting the override keyword.
# Because MIL\... doesn't have common headers, but does share this file, 
# MIL\common.inc, 'override' is unconditionally defined to '__override' 
# (the SAL keyword).  MIL\Core does have a common header MIL\Core\std.h and
# if the current version of the compiler is different it undefines override.
#
C_DEFINES = $(C_DEFINES) \
    /Doverride=__override
    
 
#
# IntSafe does not provide the signed version of its functions unless
# this variable is defined.
#
C_DEFINES = $(C_DEFINES) \
    /DENABLE_INTSAFE_SIGNED_FUNCTIONS=1


# In debug build turn on RTTI (for DYNCAST macro) and turn off all optimization
!if !$(FREEBUILD)
USE_RTTI=1
MSC_OPTIMIZATION=/Od /wd4748 /wd4756
LINKER_NOICF=1      # ICF is s-l-o-w
LINKER_NOREF=1
USE_DEBUGLIB=1
USE_PERFMETER=1
!else
MSC_OPTIMIZATION=/Oxs
!endif

# set up inline optimizations
!if !$(FREEBUILD) && defined(DEBUG_NO_INLINES)
MSC_OPTIMIZATION=$(MSC_OPTIMIZATION) /Ob0
!else
MSC_OPTIMIZATION=$(MSC_OPTIMIZATION) /Ob1
!endif

# user intrinsic functions
MSC_OPTIMIZATION=$(MSC_OPTIMIZATION) /Oi

# Always use defaults opts for IA64
IA64_OPTIMIZATION=$(DEFAULT_MSC_OPT)

NOT_LEAN_AND_MEAN=1

# let us specify the libs we want
USE_NOLIBS=1

# use MSVCRT DLL
USE_MSVCRT=1

# user mode windows by default
UMTYPE=windows

# Don't use this for now...
## Downlevel support (reconsider) ----------------------------------------------
# Use NT 4.0/Win95 functionality only
#WIN32_IE_VERSION=0x0400
#WIN32_WINNT_VERSION=0x0400
#WIN32_WIN95_VERSION=0x0400
#SUBSYSTEM_VERSION=4.00

!if $(386)
NO_NTDLL=1
!endif

## Browser info ----------------------------------------------------------------
# By default, don't generate a .bsc file
NO_BROWSER_FILE=1

!ifdef WCP_BROWSER_INFO
USER_ENV_BROWSER_INFO=1
BROWSER_INFO=1
!endif


## ICECAP support --------------------------------------------------------------
!if defined(USE_ICECAP) || defined(USING_ICECAP4_ICEPICK)
C_DEFINES = $(C_DEFINES) /DPRODUCT_PROF
!if defined(USING_ICECAP4_ICEPICK)
C_DEFINES = $(C_DEFINES) /DICECAP4
PERFLIBS=$(WGX_PATH)\external\lib\$(TARGET_DIRECTORY)\icecap.lib
!endif
!endif

!ifdef USE_PERFTAGS
C_DEFINES = $(C_DEFINES) /DPERFTAGS
!message PERFTAGS was defined
USE_DEBUGLIB=1
!endif

!ifdef USE_PERFMETER
C_DEFINES = $(C_DEFINES) /DPERFMETER=$(USE_PERFMETER)
USE_DEBUGLIB=1
! if $(FREEBUILD)
!  message PERFMETER was defined to $(USE_PERFMETER)
! endif
!endif

!ifdef USE_NULLDEVICE
C_DEFINES = $(C_DEFINES) /DNULLDEVICE
!message USE_NULLDEVICE was defined
!endif

!ifdef DBG_STEP_RENDERING
C_DEFINES = $(C_DEFINES) /DDBG_STEP_RENDERING=$(DBG_STEP_RENDERING)
!message DBG_STEP_RENDERING was defined to $(DBG_STEP_RENDERING)
!endif


## Startup functions -----------------------------------------------------------
!if defined(USE_DEBUGLIB)
WGXDLLMAIN=_DllMainStartupDebug
WGXEXEWINMAIN=_WinMainStartupDebug
WGXCONMAIN=_ConMainStartupDebug
!else
WGXDLLMAIN=_DllMainStartup
WGXEXEWINMAIN=_WinMainStartup
WGXCONMAIN=_ConMainStartup
!endif

