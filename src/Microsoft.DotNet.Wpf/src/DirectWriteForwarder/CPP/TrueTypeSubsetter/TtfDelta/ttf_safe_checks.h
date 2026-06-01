// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef TTF_SAFE_CHECKS_H
#define TTF_SAFE_CHECKS_H

/*
 * TtfDelta safety switch — read once from managed code (AppContext),
 * then checked from native C code via the global.
 *
 * AppContext switch name:
 *   Switch.MS.Internal.TtfDelta.DisableDirectWriteForwarderBoundsCheckProtection
 *
 * Default (false / absent): Safe checks ON (new behavior).
 * Set to true:              Safe checks OFF (old behavior / opt-out).
 */

/* -1 = uninitialized, 0 = safe checks disabled (old), 1 = safe checks enabled (new) */
extern int g_fDWFBoundsCheckEnabled;

#define TTF_SAFE_CHECKS_ENABLED() (g_fDWFBoundsCheckEnabled != 0)

#endif /* TTF_SAFE_CHECKS_H */
