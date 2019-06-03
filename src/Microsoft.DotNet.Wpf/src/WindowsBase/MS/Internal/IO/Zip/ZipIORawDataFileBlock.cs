// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


//
//
// Description:
//  This is an internal class that enables interactions with Zip archives
//  for OPC scenarios 
//
//
//
//

using System;
using System.IO;
using System.Diagnostics;
using System.Collections;
using System.Runtime.Serialization;
using System.Windows;  
using MS.Internal.IO.Packaging;         // for PackagingUtilities

namespace MS.Internal.IO.Zip
{
    internal class ZipIORawDataFileBlock :  IZipIOBlock
    {
        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------
        // standard IZipIOBlock functionality
        public long Offset
        {
            get
            {
                return _offset;
            }
        }

        public long Size
        {
            get
            {
                return  _size;
            }
        }

        public bool GetDirtyFlag(bool closingFlag)
        {
            return _dirtyFlag;
        }

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------
        public void Move(long shiftSize)
        {
            if (shiftSize != 0)
            {
                checked{_offset +=shiftSize;}
                _dirtyFlag = true;
                Debug.Assert(_offset >=0);                
            }
        }

        public void Save()
        {
            if(GetDirtyFlag(true)) // in case we do not know whether we are closing or not we should think we are closing as a more conservative approach 
            {                
                // we need to move the whole block to the new position 
                long moveBlockSourceOffset = _persistedOffset;
                long moveBlockSize = _size;
                long moveBlockTargetOffset = _offset;

                if (_cachePrefixStream != null)
                {   
                    // if we have something in cache we only should move whatever isn't cached
                    checked{moveBlockSourceOffset += _cachePrefixStream.Length;}
                    checked{moveBlockTargetOffset += _cachePrefixStream.Length;}
                    checked{moveBlockSize -= _cachePrefixStream.Length;}
                    Debug.Assert(moveBlockSize >=0);                    
                }

                _blockManager.MoveData(moveBlockSourceOffset, moveBlockTargetOffset, moveBlockSize);

                // only after data on disk was moved it is safe to flush the cached prefix buffer 
                if (_cachePrefixStream != null)
                {
                    if (_blockManager.Stream.Position != _offset)
                    {
                        // we need to seek 
                        _blockManager.Stream.Seek(_offset, SeekOrigin.Begin);
                    }
                    
                    Debug.Assert(_cachePrefixStream.Length > 0);     // should be taken care of by the constructor 
                                                                                        // and PreSaveNotification                

                    // we do not need to flush here because Block Manager is responsible for flushing
                    // this in it's Save method
                    _cachePrefixStream.WriteToStream(_blockManager.Stream);

                    // we can free the memory
                    _cachePrefixStream.Close();
                    _cachePrefixStream = null;
                }

                // we are not shifted between on disk image and in memory image any more 
                _persistedOffset = _offset;

                _dirtyFlag = false;         
            }
        }

        public void UpdateReferences(bool closingFlag)
        {
            // this block doesn't have external references so there is nothing we need to do here 
        }
        
        public PreSaveNotificationScanControlInstruction PreSaveNotification(long offset, long size)
        {
            return ZipIOBlockManager.CommonPreSaveNotificationHandler(
                                                            _blockManager.Stream,
                                                        offset, size,
                                                        _persistedOffset, _size,
                                                        ref _cachePrefixStream);
        }

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------
        internal long DiskImageShift 
        {
            get
            {   
                return _offset - _persistedOffset;
            }
        }

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------
        internal static ZipIORawDataFileBlock Assign(ZipIOBlockManager blockManager, long offset, long size)          
        {
            if (size <= 0)
            {
                throw new ArgumentOutOfRangeException ("size");
            }

            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException ("offset");
            }
                
            ZipIORawDataFileBlock block = new ZipIORawDataFileBlock(blockManager);
            block._persistedOffset = offset;
            block._offset = offset;
            block._size = size;

            return block;
        }

        internal bool DiskImageContains(IZipIOBlock block)
        {
            checked
            {
                Debug.Assert(block != null);
                Debug.Assert(block.Offset >=0);
                Debug.Assert(block.Size > 0);

                return (_persistedOffset <= block.Offset) && 
                            (_persistedOffset + _size >= block.Offset +block.Size);
            }
        }
        
        internal bool DiskImageContains(long offset)
        {
            checked
            {        
                Debug.Assert(offset >=0);

                return (_persistedOffset <= offset) && (_persistedOffset + _size > offset); 
            }
        }
    
        internal void SplitIntoPrefixSuffix(IZipIOBlock block, 
                                        out ZipIORawDataFileBlock prefixBlock, out ZipIORawDataFileBlock suffixBlock)
        {
            // assert that current's block cache isn't loaded, if it is  
            // we probably missed an opportunity to used this cache in order to parse the new block 
            // and it might be based on the overriden data on disk 
            // This block can only be in cached state as a part of single BlockManager.Save execution.
            // It can NOT be in cached state prior to BlockManager.Save function entry or after 
            // BlockManager.Save execution completed
            Debug.Assert(_cachePrefixStream == null);
    
            // Assert that block is containe inside the current raw block 
            Debug.Assert(DiskImageContains(block));

            checked
            {
                prefixBlock = null;
                suffixBlock = null;
                if (block.Offset > _persistedOffset)
                {
                    // we have a new non empty prefix;
                    long newBlockOffset = _persistedOffset; 
                    long newBlockSize = block.Offset - _persistedOffset;

                    prefixBlock = Assign(_blockManager, newBlockOffset , newBlockSize);
                }
                
                if (block.Offset + block.Size < _persistedOffset + _size)
                {
                    // we have a new non empty suffix;
                    long newBlockOffset = block.Offset + block.Size; 
                    long newBlockSize = _persistedOffset + _size - newBlockOffset;
                    
                    suffixBlock = Assign(_blockManager, newBlockOffset, newBlockSize);
                }
            }
        }

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------
        private ZipIORawDataFileBlock(ZipIOBlockManager blockManager) 
        {
            _blockManager = blockManager;
        }

        //------------------------------------------------------
        //
        //  Private Members
        //
        //------------------------------------------------------
        private SparseMemoryStream _cachePrefixStream = null;

        private ZipIOBlockManager _blockManager;

        private long _persistedOffset;

        private long _offset;
        private long _size;
        private bool  _dirtyFlag;        
    }
}
