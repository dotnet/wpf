// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:
//      Mapping between a visual and its handles on different channels.
//

namespace System.Windows.Media.Composition
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Windows;
    using System.Windows.Media;


    /// <summary>
    /// Mapping between a visual and its handles on different channels.
    /// </summary>
    internal struct VisualProxy
    {
        // --------------------------------------------------------------------
        //
        //   Private Types
        //
        // --------------------------------------------------------------------

        #region Private Types

        /// <summary>
        /// Tuple binding a channel to a set of flags and a resource handle.
        /// </summary>
        /// <remarks>
        /// Note that DUCE.Channel is a reference type.
        /// </remarks>
        private struct Proxy
        {
            internal DUCE.Channel Channel;
            internal VisualProxyFlags Flags;
            internal DUCE.ResourceHandle Handle;
        }

        #endregion Private Types


        // --------------------------------------------------------------------
        //
        //   Internal Properties
        //
        // --------------------------------------------------------------------

        #region Internal Properties

        /// <summary>
        /// Returns the number of channels this resource is marshaled to.
        /// </summary>
        internal int Count
        {
            get
            {
                if (_tail == null) 
                {
                    return _head.Channel == null ? 0 : 1;
                }
                else
                {
                    //
                    // The logic here is simple: we keep at most one entry
                    // in the tail free. Heads has to be occupied and adds one.
                    //

                    int tailLength = _tail.Length;

                    bool lastTailIsEmpty = 
                        _tail[tailLength - 1].Channel == null;

                    return 1 + tailLength - (lastTailIsEmpty ? 1 : 0);
                }
            }
        }


        /// <summary>
        /// Returns true if the parent resource is marshaled to at least
        /// one channel.
        /// </summary>
        internal bool IsOnAnyChannel
        {
            get
            {
                return Count != 0;
            }
        }

        #endregion Internal Properties



        // --------------------------------------------------------------------
        //
        //   Internal Methods
        //
        // --------------------------------------------------------------------

        #region Internal Methods

        /// <summary>
        /// Returns true if the visual is marshaled to the
        /// specified channel.
        /// </summary>
        internal bool IsOnChannel(DUCE.Channel channel)
        {
            int index = Find(channel);

            if (index == PROXY_NOT_FOUND) 
            {
                return false;
            }
            else if (index == PROXY_STORED_INLINE) 
            {
                return !_head.Handle.IsNull;
            }
            else 
            {
                return !_tail[index].Handle.IsNull;
            }
        }

        /// <summary>
        /// If the visual is on channel, its reference count is increased.
        /// Otherwise, a new resource is created on that channel.
        /// </summary>
        internal bool CreateOrAddRefOnChannel(
            object instance,
            DUCE.Channel channel,
            DUCE.ResourceType resourceType)
        {
            int index = Find(channel);
            int count = Count;

            if (index == PROXY_NOT_FOUND) 
            {
                //
                // This is the case where we have to create a new resource.
                //

                if (_head.Channel == null) 
                {
                    //
                    // We're adding the first proxy.
                    // 
                    // Before:        [empty]
                    // After insert:  [ head]
                    //

                    Debug.Assert(count == 0);

                    _head.Channel = channel;
                    _head.Flags = VisualProxyFlags.None;

                    channel.CreateOrAddRefOnChannel(
                        instance,
                        ref _head.Handle, 
                        resourceType);
                }
                else
                {
                    if (_tail == null) 
                    {
                        //
                        // We're adding the second proxy.
                        // 
                        // Before:        [head]
                        // After resize:  [head] [ empty] [empty]
                        // After insert:  [head] [tail 0] [empty]
                        //                       ----------------

                        Debug.Assert(count == 1);

                        _tail = new Proxy[2];
                    }
                    else if (count > _tail.Length) 
                    {
                        //
                        // Increase the tail size by 2.
                        //                
                        // Before:        [head] [tail 0] ... [tail c-2]
                        // After resize:  [head] [tail 0] ... [tail c-2] [   empty] [empty]
                        // After insert:  [head] [tail 0] ... [tail c-3] [tail c-2] [empty]
                        //                       ------------------------------------------
                        //

                        ResizeTail(2);
                    }

                    //
                    // Now that we have a tail, fill in the first free element.
                    //

                    Proxy proxy;

                    proxy.Channel = channel;
                    proxy.Flags = VisualProxyFlags.None;
                    proxy.Handle = DUCE.ResourceHandle.Null;

                    channel.CreateOrAddRefOnChannel(
                        instance,
                        ref proxy.Handle, 
                        resourceType);

                    _tail[count - 1] = proxy;
                }

                return /* created */ true;
            }
            else if (index == PROXY_STORED_INLINE) 
            {
                //
                // We simply need to increase the reference count on head...
                //

                channel.CreateOrAddRefOnChannel(
                    instance,
                    ref _head.Handle, 
                    resourceType);
            }
            else
            {
                //
                // Increase the reference count on one of the tail proxies...
                //

                channel.CreateOrAddRefOnChannel(
                    instance,
                    ref _tail[index].Handle, 
                    resourceType);
            }

            return /* not created */ false;
        }


        /// <summary>
        /// If visual is on channel, its reference count is increased.
        /// Otherwise, a new resource is created on that channel.
        /// </summary>
        internal bool ReleaseOnChannel(DUCE.Channel channel)
        {
            int index = Find(channel);
            bool proxyRemoved = false;
            int count = Count;

            if (index == PROXY_STORED_INLINE) 
            {
                if (channel.ReleaseOnChannel(_head.Handle)) 
                {
                    //
                    // Need to move the last of the non-empty tail to head
                    // or clear the head. Erase the head if that was the last
                    // proxy.
                    //

                    if (count == 1) 
                    {
                        _head = new Proxy();
                    }
                    else
                    {
                        _head = _tail[count - 2];
                    }

                    proxyRemoved = true;
                }
            }
            else if (index >= 0) 
            {
                if (channel.ReleaseOnChannel(_tail[index].Handle)) 
                {
                    //
                    // Need to move the last of the non-empty tail to the
                    // removed index. Avoid in-place copying.
                    //

                    if (index != count - 2) 
                    {
                        _tail[index] = _tail[count - 2];
                    }

                    proxyRemoved = true;
                }
            }
            else
            {
                Debug.Assert(index != PROXY_NOT_FOUND);
                return false;
            }

            if (proxyRemoved) 
            {
                if (_tail != null) 
                {
                    //
                    // Keep the tail short. We allow for one extra free element
                    // in tail to avoid constant allocations / reallocations.
                    //

                    if (count == 2) 
                    {
                        //                        ------------------
                        // Before removal: [head] [tail c-1] [empty]
                        // Here and now:   [head] [ deleted] [empty]
                        // After removal:  [head]
                        //

                        _tail = null;
                    }
                    else if (count == _tail.Length) 
                    {
                        //                        ---------------------------------------------------
                        // Before removal: [head] [tail 0] [tail 1] ... [tail c-3] [tail c-2] [empty]
                        // Here and now:   [head] [tail 0] [tail 1] ... [tail c-3] [ deleted] [empty]
                        // After removal:  [head] [tail 0] [tail 1] ... [tail c-3]
                        //

                        ResizeTail(-2);
                    }
                    else
                    {
                        //                        ------------------------------------------------------
                        // Before removal: [head] [tail 0] [tail 1] ... [tail c-4] [tail c-3] [tail c-2]
                        // Here and now:   [head] [tail 0] [tail 1] ... [tail c-4] [tail c-3] [ deleted]
                        // After removal:  [head] [tail 0] [tail 1] ... [tail c-4] [tail c-3] [   empty]
                        //

                        _tail[count - 2] = new Proxy();
                    }
                }
            }

            return proxyRemoved;
        }


        /// <summary>
        /// Returns the channel that the n-th proxy connects to.
        /// </summary>
        internal DUCE.Channel GetChannel(int index)
        {
            Debug.Assert(index >= 0 && index < Count);

            if (index < Count) 
            {
                if (index == 0) 
                {
                    return _head.Channel;
                }
                else if (index > 0) 
                {
                    return _tail[index - 1].Channel;
                }
            }

            return null;                
        }

        #endregion Internal Methods



        // ----------------------------------------------------------------
        //
        //   Internal Methods: resource handle getters
        //
        // ----------------------------------------------------------------

        #region Internal Methods: resource handle getters

        /// <summary>
        /// Returns the handle the visual has on a specific channel.
        /// </summary>
        internal DUCE.ResourceHandle GetHandle(DUCE.Channel channel)
        {
            return GetHandle(Find(channel) + 1); // Find's results are -1 based, adjust by one.
        }


        /// <summary>
        /// Returns the handle the visual has on the n-th channel.
        /// </summary>
        internal DUCE.ResourceHandle GetHandle(int index)
        {
            Debug.Assert(index >= 0 && index < Count);

            if (index < Count) 
            {
                if (index == 0) 
                {
                    return _head.Handle;
                }
                else if (index > 0) 
                {
                    return _tail[index - 1].Handle;
                }
            }

            return DUCE.ResourceHandle.Null;                
        }

        #endregion Internal Methods: resource handle getters



        // ----------------------------------------------------------------
        //
        //   Internal Methods: visual proxy flags
        //
        // ----------------------------------------------------------------

        #region Internal Methods: visual proxy flags

        /// <summary>
        /// Returns the flags the visual has on a specific channel.
        /// </summary>
        internal VisualProxyFlags GetFlags(DUCE.Channel channel)
        {
            return GetFlags(Find(channel) + 1); // Find's results are -1 based, adjust by one.
        }


        /// <summary>
        /// Returns the handle the visual has on n-th channel.
        /// </summary>
        internal VisualProxyFlags GetFlags(int index)
        {
            if (index < Count) 
            {
                if (index == 0) 
                {
                    return _head.Flags;
                }
                else if (index > 0) 
                {
                    return _tail[index - 1].Flags;
                }
            }

            return VisualProxyFlags.None;                
        }


        /// <summary>
        /// Sets the flags the visual has on a specific channel.
        /// </summary>
        internal void SetFlags(
            DUCE.Channel channel,
            bool value,
            VisualProxyFlags flags)
        {
            SetFlags(Find(channel) + 1, value, flags); // Find's results are -1 based, adjust by one.
        }


        /// <summary>
        /// Sets the flags the visual has on the n-th channel.
        /// </summary>
        internal void SetFlags(
            int index,
            bool value,
            VisualProxyFlags flags)
        {
            Debug.Assert(index >= 0 && index < Count);

            if (index < Count) 
            {
                if (index == 0) 
                {
                    _head.Flags = 
                        value ? (_head.Flags | flags) : (_head.Flags & ~flags);
                }
                else if (index > 0) 
                {
                    _tail[index - 1].Flags = 
                        value ? (_tail[index - 1].Flags | flags) : (_tail[index - 1].Flags & ~flags);
                }
            }
        }


        /// <summary>
        /// Sets the flags on all channels the visual is marshaled to.
        /// </summary>
        internal void SetFlagsOnAllChannels(
            bool value,
            VisualProxyFlags flags)
        {
            if (_head.Channel != null) 
            {
                _head.Flags = 
                    value ? (_head.Flags | flags) : (_head.Flags & ~flags);

                for (int i = 0, limit = Count - 1; i < limit; i++) 
                {
                    _tail[i].Flags = 
                        value ? (_tail[i].Flags | flags) : (_tail[i].Flags & ~flags);
                }
            }
        }

        /// <summary>
        /// Returns true if the given flags are set for every proxy or if
        /// the visual is not being marshaled.
        /// </summary>        
        internal bool CheckFlagsOnAllChannels(
            VisualProxyFlags conjunctionFlags)
        {
            if (_head.Channel != null) 
            {
                if ((_head.Flags & conjunctionFlags) != conjunctionFlags)
                    return false;

                for (int i = 0, limit = Count - 1; i < limit; i++) 
                {
                    if ((_tail[i].Flags & conjunctionFlags) != conjunctionFlags)
                        return false;
                }
            }

            return true;
        }

        #endregion Internal Methods: visual proxy flags


        // --------------------------------------------------------------------
        //
        //   Private Methods
        //
        // --------------------------------------------------------------------

        #region Private Methods

        /// <summary>
        /// Looks for the given channel in the proxies list.
        /// </summary>
        private int Find(DUCE.Channel channel)
        {
            if (_head.Channel == channel) 
            {
                return PROXY_STORED_INLINE;
            }
            else if (_tail != null) 
            {
                for (int i = 0, limit = Count - 1; i < limit; i++) 
                {
                    if (_tail[i].Channel == channel) 
                    {
                        return i;
                    }
                }
            }

            return PROXY_NOT_FOUND;
        }

        /// <summary>
        /// Grows or shrinks the tail by a given size.
        /// </summary>
        private void ResizeTail(int delta)
        {
            int newLength = _tail.Length + delta;

            Debug.Assert(delta % 2 == 0 && newLength >= 2);

            Proxy[] reallocatedTail = new Proxy[newLength];

            Array.Copy(_tail, reallocatedTail, Math.Min(_tail.Length, newLength));

            _tail = reallocatedTail;
        }
        
        #endregion Private Methods



        // --------------------------------------------------------------------
        //
        //   Private Fields
        //
        // --------------------------------------------------------------------

        #region Private Fields

        /// <summary>
        /// Returned by the Find method to denote that a proxy for the given
        /// channel has not been found.
        /// </summary>
        private const int PROXY_NOT_FOUND = -2;

        /// <summary>
        /// Returned by the Find method to denote that a proxy for the given
        /// channel has been found in the inline storage.
        /// </summary>
        private const int PROXY_STORED_INLINE = -1;

        /// <summary>
        /// This data structure is optimized for single entry. _head is 
        /// the one entry that we inline into the struct for that purpose.
        /// </summary>
        private Proxy _head;

        /// <summary>
        /// All the other entries end up in this array.
        /// </summary>
        private Proxy[] _tail;

        #endregion Private Fields
    }
}


