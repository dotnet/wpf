// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.




/* this ALWAYS GENERATED file contains the definitions for the interfaces */


 /* File created by MIDL compiler version 7.00.0498 */
/* Compiler settings for pentypes.idl:
    Oicf, W1, Zp8, env=Win32 (32b run)
    protocol : dce , ms_ext, c_ext, robust
    error checks: allocation ref bounds_check enum stub_data 
    VC __declspec() decoration level: 
         __declspec(uuid()), __declspec(selectany), __declspec(novtable)
         DECLSPEC_UUID(), MIDL_INTERFACE()
*/
//@@MIDL_FILE_HEADING(  )

#pragma warning( disable: 4049 )  /* more than 64k source lines */


/* verify that the <rpcndr.h> version is high enough to compile this file*/
#ifndef __REQUIRED_RPCNDR_H_VERSION__
#define __REQUIRED_RPCNDR_H_VERSION__ 500
#endif

/* verify that the <rpcsal.h> version is high enough to compile this file*/
#ifndef __REQUIRED_RPCSAL_H_VERSION__
#define __REQUIRED_RPCSAL_H_VERSION__ 100
#endif

#include "rpc.h"
#include "rpcndr.h"

#ifndef __RPCNDR_H_VERSION__
#error this stub requires an updated version of <rpcndr.h>
#endif // __RPCNDR_H_VERSION__


#ifndef __pentypes_h__
#define __pentypes_h__

#if defined(_MSC_VER) && (_MSC_VER >= 1020)
#pragma once
#endif

/* Forward Declarations */ 

/* header files for imported files */
#include "oaidl.h"
#include "ocidl.h"

#ifdef __cplusplus
extern "C"{
#endif 


/* interface __MIDL_itf_pentypes_0000_0000 */
/* [local] */ 

#include "tpcshrd.h"
#define TCXO_MARGIN          0x00000001
#define TCXO_PREHOOK         0x00000002
#define TCXO_CURSOR_STATE    0x00000004
#define TCXO_NO_CURSOR_DOWN  0x00000008
#define TCXO_NON_INTEGRATED  0x00000010
#define TCXO_POSTHOOK        0x00000020
#define TCXO_DONT_SHOW_CURSOR 0x00000080
#define TCXO_DONT_VALIDATE_TCS 0x00000100
#define TCXO_REPORT_RECT_MAPPING_CHANGE 0x00000200
#define TCXO_ALLOW_FLICKS 0x00000400
#define TCXO_ALLOW_FEEDBACK_TAPS 0x00000800
#define TCXO_ALLOW_FEEDBACK_BARREL 0x00001000
#define TCXO_ALLOW_ALL_TOUCH 0x00002000
#define TCXO_ALL (TCXO_MARGIN | TCXO_PREHOOK | TCXO_CURSOR_STATE | TCXO_NO_CURSOR_DOWN | TCXO_NON_INTEGRATED | TCXO_POSTHOOK | TCXO_DONT_SHOW_CURSOR | TCXO_DONT_VALIDATE_TCS | TCXO_REPORT_RECT_MAPPING_CHANGE | TCXO_ALLOW_FLICKS | TCXO_ALLOW_FEEDBACK_TAPS | TCXO_ALLOW_FEEDBACK_BARREL | TCXO_ALLOW_ALL_TOUCH)
#define TCXO_HOOK (TCXO_PREHOOK | TCXO_POSTHOOK)
#define TCXS_DISABLED        0x00000001
#define THWC_INTEGRATED      0x00000001
#define THWC_CSR_MUST_TOUCH  0x00000002
#define THWC_HARD_PROXIMITY  0x00000004
#define THWC_PHYSID_CSRS     0x00000008
#define IP_CURSOR_DOWN               0x00000001
#define IP_INVERTED                  0x00000002
#define IP_MARGIN                    0x00000004
#define IP_BARREL_DOWN		        0x00000008
#define IP_RECT_MAPPING_CHANGED 0x00000010
#define IP_ALL_STATUS_BITS (IP_CURSOR_DOWN | IP_INVERTED | IP_MARGIN | IP_BARREL_DOWN | IP_RECT_MAPPING_CHANGED)
#define TAB_SETTING_LINEARIZATION			            0x00000001
#define TAB_SETTING_PORTRAIT_USERTILT		            0x00000002
#define TAB_SETTING_LANDSCAPE_USERTILT		            0x00000004
#define TAB_SETTING_DISPLAY_ORIENTATION_DEFAULT_USERTILT 0x00000008
#define TAB_SETTING_DISPLAY_ORIENTATION_90_USERTILT	    0x00000010
#define TAB_SETTING_DISPLAY_ORIENTATION_180_USERTILT		0x00000100
#define TAB_SETTING_DISPLAY_ORIENTATION_270_USERTILT		0x00001000
#define SE_TAP                               0x00000010
#define SE_DBL_TAP				0x00000011
#define SE_RIGHT_TAP				0x00000012
#define SE_DRAG                              0x00000013
#define SE_RIGHT_DRAG			0x00000014
#define SE_HOLD_ENTER			0x00000015
#define SE_HOLD_LEAVE			0x00000016
#define SE_HOVER_ENTER			0x00000017
#define SE_HOVER_LEAVE			0x00000018
#define SE_MIDDLE_CLICK			0x00000019
#define SE_KEY                               0x0000001A
#define SE_MODIFIER_KEY			0x0000001B
#define SE_GESTURE_MODE			0x0000001C
#define SE_CURSOR				0x0000001D
#define SE_FLICK                             0x0000001F
#define SE_MODIFIER_CTRL			0x00000001
#define SE_MODIFIER_ALT			0x00000002
#define SE_MODIFIER_SHIFT		0x00000004
#define SE_NORMAL_CURSOR			0x00000001
#define SE_ERASER_CURSOR			0x00000002
#define SE_SYSTEMEVENT			0x00000001
#define SE_TYPE_MOUSE			0x00000000
#define SE_TYPE_KEYBOARD			0x00000001
#define SE_DELAY_PACKET			0x0000000F
#define SE_PRE_TAPDRAG			0x0000001E
#define WM_TABLET_DEFBASE                0x02C0
#define WM_TABLET_MAXOFFSET              0x20
#define WM_TABLET_CONTEXTCREATE              (WM_TABLET_DEFBASE + 0)
#define WM_TABLET_CONTEXTDESTROY             (WM_TABLET_DEFBASE + 1)
#define WM_TABLET_CURSORNEW                  (WM_TABLET_DEFBASE + 2)
#define WM_TABLET_CURSORINRANGE              (WM_TABLET_DEFBASE + 3)
#define WM_TABLET_CURSOROUTOFRANGE           (WM_TABLET_DEFBASE + 4)
#define WM_TABLET_CURSORDOWN                 (WM_TABLET_DEFBASE + 5)
#define WM_TABLET_CURSORUP                   (WM_TABLET_DEFBASE + 6)
#define WM_TABLET_PACKET                     (WM_TABLET_DEFBASE + 7)
#define WM_TABLET_ADDED                      (WM_TABLET_DEFBASE + 8)
#define WM_TABLET_DELETED                    (WM_TABLET_DEFBASE + 9)
#define WM_TABLET_SYSTEMEVENT                (WM_TABLET_DEFBASE + 10)
#define WM_TABLET_MAX                        (WM_TABLET_DEFBASE + WM_TABLET_MAXOFFSET)
#define TABLET_MESSAGE_EXTRA_INFO_MASK_PEN_OR_TOUCH   0xFF515700
#define TABLET_MESSAGE_EXTRA_INFO_MASK_TOUCH          0xFF515780
#define TABLET_MESSAGE_EXTRA_INFO_MASK_TIP            0xFF575100
#define MICROSOFT_TABLETPENSERVICE_PROPERTY _T("MicrosoftTabletPenServiceProperty")
#define TABLET_DISABLE_PRESSANDHOLD        0x00000001
#define TABLET_DISABLE_PENTAPFEEDBACK      0x00000008
#define TABLET_DISABLE_PENBARRELFEEDBACK   0x00000010
#define TABLET_DISABLE_TOUCHUIFORCEON      0x00000100
#define TABLET_DISABLE_TOUCHUIFORCEOFF     0x00000200
#define TABLET_DISABLE_TOUCHSWITCH         0x00008000
#define TABLET_DISABLE_FLICKS              0x00010000
#define TABLET_ENABLE_FLICKSONCONTEXT      0x00020000
#define TABLET_ENABLE_FLICKLEARNINGMODE    0x00040000
#define TABLET_DISABLE_SMOOTHSCROLLING     0x00080000
#define WISP_WINTAB_ERROR                    MAKE_HRESULT(1, FACILITY_ITF, 0x201)
#define WISP_PACKET_BUFFER_TOO_SMALL         MAKE_HRESULT(1, FACILITY_ITF, 0x211)
#define WISP_NO_DEFAULT_TABLET               MAKE_HRESULT(1, FACILITY_ITF, 0x212)
#define WISP_TABLET_CONTEXT_NOT_FOUND        MAKE_HRESULT(1, FACILITY_ITF, 0x213)
#define WISP_CURSOR_NOT_FOUND                MAKE_HRESULT(1, FACILITY_ITF, 0x214)
#define WISP_INVALID_TABLET_INDEX            MAKE_HRESULT(1, FACILITY_ITF, 0x215)
#define WISP_INVALID_TABLET_CONTEXT_INDEX    MAKE_HRESULT(1, FACILITY_ITF, 0x216)
#define WISP_INVALID_CURSOR_INDEX            MAKE_HRESULT(1, FACILITY_ITF, 0x217)
#define WISP_INVALID_BUTTON_INDEX            MAKE_HRESULT(1, FACILITY_ITF, 0x218)
#define WISP_INVALID_PACKET_SERIAL_NUM       MAKE_HRESULT(1, FACILITY_ITF, 0x219)
#define WISP_INVALID_WINDOW_HANDLE           MAKE_HRESULT(1, FACILITY_ITF, 0x21a)
#define WISP_INVALID_INPUT_RECT              MAKE_HRESULT(1, FACILITY_ITF, 0x21b)
#define WISP_INVALID_TABLET_CONTEXT_SETTINGS MAKE_HRESULT(1, FACILITY_ITF, 0x21c)
#define WISP_UNKNOWN_PROPERTY                MAKE_HRESULT(1, FACILITY_ITF, 0x21d)
#define WISP_UNITS_CONVERSION_UNDEFINED      MAKE_HRESULT(1, FACILITY_ITF, 0x21e)
#define SZ_REGKEY_PERSIST TEXT("Software\\Microsoft\\Wisp\\Pen\\Persist")
#define SZ_REGVAL_TYPE TEXT("type")
#define SZ_REGVAL_WINTABDEVICEID TEXT("WintabDeviceId")
#define SZ_REGVAL_HIDDEVICEPATH TEXT("HidDevicePath")
#define SZ_REGVAL_WINTABCURSORTYPE TEXT("WintabCursorType")
#define SZ_REGVAL_WINTABCURSORPHYSID TEXT("WintabCursorPhysid")
#define SZ_REGVAL_HIDCURSORID TEXT("HidCursorId")
#define SZ_REGVAL_HIDDEVICE TEXT("HidDevice")
#define SZ_REGVAL_NAME TEXT("Name")
typedef 
enum _CONTEXT_ENABLE_TYPE
    {	CONTEXT_ENABLE	= 1,
	CONTEXT_DISABLE	= 2
    } 	CONTEXT_ENABLE_TYPE;

typedef enum _CONTEXT_ENABLE_TYPE *PCONTEXT_ENABLE_TYPE;

typedef struct _TABLET_CONTEXT_SETTINGS
    {
    ULONG cPktProps;
    GUID *pguidPktProps;
    ULONG cPktBtns;
    GUID *pguidPktBtns;
    DWORD *pdwBtnDnMask;
    DWORD *pdwBtnUpMask;
    LONG lXMargin;
    LONG lYMargin;
    } 	TABLET_CONTEXT_SETTINGS;

typedef /* [unique] */  __RPC_unique_pointer TABLET_CONTEXT_SETTINGS *PTABLET_CONTEXT_SETTINGS;

#define SZ_EVENT_TABLETHARDWAREPRESENT TEXT("Global\\TabletHardwarePresent")


extern RPC_IF_HANDLE __MIDL_itf_pentypes_0000_0000_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_pentypes_0000_0000_v0_0_s_ifspec;

/* Additional Prototypes for ALL interfaces */

/* end of Additional Prototypes */

#ifdef __cplusplus
}
#endif

#endif



