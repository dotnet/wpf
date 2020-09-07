// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


inline
CompositionNotifier&
MediaInstance::
GetCompositionNotifier(
    void
    )
{
    return m_compositionNotifier;
}

inline
CMediaEventProxy&
MediaInstance::
GetMediaEventProxy(
    void
    )
{
    return m_mediaEventProxy;
}

