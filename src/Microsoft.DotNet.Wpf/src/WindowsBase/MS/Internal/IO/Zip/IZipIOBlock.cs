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

namespace MS.Internal.IO.Zip
{   
    internal enum PreSaveNotificationScanControlInstruction : int 
    {
        Continue = 0,   // instruction to continue Pre Save Notification on the following block 
        Stop = 1,          // instruction to stop Pre Save Notification loop
    }

    /// <summary>
    /// IZipIOBlock - this interface is used to enable polymorphism across all 
    /// supported Zip IO records. This enables ZipIOBlockManager to manipulate all blocks in a uniform way
    /// </summary>
    internal interface IZipIOBlock
    {
        /// <summary>
        /// This is the current offset of the block relative to the start of the archive stream.
        /// It might not necessarily correspond to the current location
        /// of the block on disk. It is rather the Offset that will be used by the Save function to write data out. 
        /// If Block Manager needs to insert a new block it should call Move which in Turn will mark those blocks 
        /// dirty and will change the offset. 
        /// </summary>
        long Offset{get;}

        /// <summary>
        /// This is the current size of the block,  it might not neccessarily correspond to the current size of the 
        /// block on disk. It is rather the Size that will be used up if Save function is called. Block Manager 
        /// doesn't have any direct control over this size. It is changeble only by Block Specific operations 
        /// (stream operations with ZipIOLocalFileBlock, add/remove files with the CentralDirectoryBlock and so on)
        /// </summary>
        long Size{get;}

        /// <summary>
        /// This is the current state of block, if block is marked dirty it means that it was either moved from the 
        /// original location of load, or it was changed by the external APIs. Block Manager 
        /// doesn't have any direct control over this flag. It can be affected by calls to Move,
        /// Save,  UpdateReferences and Block Specific operations (like stream operations with ZipIOLocalFileBlock, 
        /// add/remove files with the CentralDirectoryBlock and so on) 
        /// closingFlag parameter indicates whether we querying the Dirty state for purposes of flushing or closing.
        /// The only case where it makes a difference is the compressed stream. Which in the Write Through mode 
        /// will be Dirty for close but not Dirty for flush.
        /// </summary>
        bool GetDirtyFlag(bool closingFlag);

        /// <summary>
        /// This function is used by the ZipIOBlockManager to adjust positions of the block in the archive, it's mostly 
        /// used for adding/deleting new file item blocks. Call to this function with a parameter not equal to 0 must 
        /// mark the block as dirty. 
        /// </summary>
        void Move(long shiftSize);

        /// <summary>
        /// This function is used by the ZipIOBlockManager to Save given block into it's current Offset position.
        /// Call to this function will result in making DirtyFlag = false; 
        /// </summary>
        void Save(); 

        /// <summary>
        /// This function is used by the ZipIOBlockManager to prepare block for Saving. If called on a block this function 
        ///  is ultimately responsible for marking this block dirty for any reason including changes in other blocks. For example 
        /// during normal operation of the ZipIoBlock Manager EndOfCentralDirectoryBlock isn't updated with the location 
        /// and size of the Central Directory on every single operation that might affect it. Only just before saving 
        /// EndOfCentralDirectoryBlock is notified using update references call that it needs to check its' local record against 
        /// the position and size of the CentralDirectory. If there is a mismatch EndOfCentralDirectory will update it's local informtion
        /// and mark itself dirty. 
        /// Similar things happen when CentralDirectoryBlock is called to UpdateReferences; it walks through all RawDataBlocks 
        /// and FileItemBlocks, and updates Central Directory records accordingly. 
        /// closingFlag parameter indicates that we should be closing streams not just flushing them. It makes a huge 
        /// difference for deflate scenarios where Flush is NOP and Close is actually always writing out extra data.
        /// </summary>
        void UpdateReferences(bool closingFlag);

        /// <summary>
        /// This function is used by the ZipIOBlockManager to notify blocks that some area of the file is about to to be overwritten.
        /// Depending on the caching policy of each block type (or particular block instance), it might choose to ignore this notification. 
        /// For Example EndOfCentralDirectoryBlock and CentralDirectoryBlocks are always fully cached (have complete snapshot of 
        /// the latest data in memory). Which means, that they do not care whether the area of the file where this data has originated 
        /// is overwritten or not. 
        /// In contrast RawDataBlock and LocalFileBlock by default do not cache everything in memory, so they need to implement  
        /// PreSaveNotification call. These types of blocks need to at least load data that they might need from disk, and make sure that the
        /// area described by the parameters doesn't have any data that needs to be preserved. 
        ///         
        /// Block can also return a value indicating whether PreSaveNotification should be extended to the blocks that are positioned after 
        /// it in the Block List. For example, if block has completely handled PreSaveNotification in a way that it cached the whole area that 
        /// was in danger (of being overwritten) it means that no blocks need to worry about this anymore. After all, no 2 blocks should have 
        /// overlapping disk buffers. Another scenario is when a block can determine that the area in danger is positioned before the block's on-disk 
        /// buffers; this means that all blocks that are positioned later in the block list do not need to worry about this PreSaveNotification 
        /// as their buffers should be positioned even further along in the file. 
        /// </summary>
        PreSaveNotificationScanControlInstruction PreSaveNotification(long offset, long size);
    }
} 
