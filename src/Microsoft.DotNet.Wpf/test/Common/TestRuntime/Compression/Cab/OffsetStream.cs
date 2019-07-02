// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
#if !CABMINIMAL

#if CABEXTRACTONLY
namespace Microsoft.Test.Compression.Cab.Extract
#else
namespace Microsoft.Test.Compression.Cab
#endif
{

using System;
using System.IO;


/// <summary>
/// Wraps a source stream and offsets all read/write/seek calls by a given value.
/// </summary>
internal class OffsetStream : Stream
{
	public OffsetStream(Stream source, long offset)
	{
		if(source == null) throw new ArgumentNullException();

		this.source = source;
		this.offset = offset;

		this.source.Seek(offset, SeekOrigin.Current);
	}

	public Stream Source
	{
		get { return this.source; }
	}
	private Stream source;

	public long Offset
	{
		get { return this.offset; }
	}
	private long offset;

	public override bool CanRead { get { return this.source.CanRead; } }
	public override bool CanWrite { get { return this.source.CanWrite; } }
	public override bool CanSeek  { get { return this.source.CanSeek; } }
	public override int Read(byte[] buffer, int offset, int count) { return this.source.Read(buffer, offset, count); }
	public override void Write(byte[] buffer, int offset, int count) { this.source.Write(buffer, offset, count); }
	public override int ReadByte() { return this.source.ReadByte(); }
	public override void WriteByte(byte value) { this.source.WriteByte(value); }
	public override void Flush() { this.source.Flush(); }

	public override long Length
	{
		get { return this.source.Length - this.offset; } 
	}

	public override long Position
	{
		get { return this.source.Position - this.offset; }
		set { this.source.Position = value + this.offset; }
	}

	public override long Seek(long offset, SeekOrigin origin)
	{
		return this.source.Seek(offset + (origin == SeekOrigin.Begin ? this.offset : 0), origin) - this.offset;
	}

	public override void SetLength(long value)
	{
		this.source.SetLength(value + this.offset);
	}

	/// <summary>
	/// Closes the underlying stream.
	/// </summary>
	public override void Close()
	{
		this.source.Close();
	}
}

}
#endif // !CABMINIMAL
