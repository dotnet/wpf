// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
#pragma once

#ifndef __WIN32INC_HPP__
#define __WIN32INC_HPP__

#include <wpfsdl.h>
#include <stddef.h>
#include <codeanalysis\sourceannotations.h>  //TEMPORARY INCLUDE
#include <windows.h>
#include <winspool.h>
#include "winddiui.h"
#include <assert.h>
#include <xpsobjectmodel_1.h>
#include <xpsprint.h>
#include <DocumentTarget.h>

#define DOWNLEVEL_BAD_ALLOC

#using SYSTEMXAML_DLL as_friend
#using PRESENTATIONFRAMEWORK_DLL as_friend
#using REACHFRAMEWORK_DLL as_friend
#using PRESENTATIONCORE_DLL as_friend
#using WINDOWSBASE_DLL as_friend

#endif
