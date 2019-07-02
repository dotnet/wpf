// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

#if CABMINIMAL
#if CABEXTRACTONLY
namespace Microsoft.Test.Compression.Cab.MiniExtract
#else
namespace Microsoft.Test.Compression.Cab.Mini
#endif
#else
#if CABEXTRACTONLY
namespace Microsoft.Test.Compression.Cab.Extract
#else
namespace Microsoft.Test.Compression.Cab
#endif
#endif
{

using System;
using System.IO;


/// <summary>
/// Duplicates a source stream by maintaining a separate position.
/// </summary>
/// <remarks>
/// WARNING: duplicate streams are not thread-safe with respect to each other or the original stream.
/// If multiple threads use duplicate copies of the same stream, they must synchronize for any operations.
/// </remarks>
internal class DuplicateStream : Stream
{
	public DuplicateStream(Stream source)
	{
		if(source == null) throw new ArgumentNullException();

		// Don't allow pointless chaining.
		if(source is DuplicateStream) source = ((DuplicateStream) source).Source;

		this.source = source;
		this.position = 0;
	}

	public Stream Source
	{
		get { return this.source; }
	}
	private Stream source;

	public override bool CanRead  { get { return this.source.CanRead; } }
	public override bool CanWrite { get { return this.source.CanWrite; } }
	public override bool CanSeek  { get { return this.source.CanSeek; } }
	public override long Length   { get { return this.source.Length; } }

	public override void Flush() { this.source.Flush(); }
	public override void SetLength(long value) { this.source.SetLength(value); }

	/// <summary>
	/// Closes the underlying stream, effectively closing ALL duplicates.
	/// </summary>
	public override void Close()
	{
		this.source.Close();
	}

	public override int Read(byte[] buffer, int offset, int count)
	{
		long saveSourcePosition = this.source.Position;
		this.source.Position = this.position;
		int read = this.source.Read(buffer, offset, count);
		this.position = this.source.Position;
		this.source.Position = saveSourcePosition;
		return read;
	}

	public override void Write(byte[] buffer, int offset, int count)
	{
		long saveSourcePosition = this.source.Position;
		this.source.Position = this.position;
		this.source.Write(buffer, offset, count);
		this.position = this.source.Position;
		this.source.Position = saveSourcePosition;
	}

	public override long Seek(long offset, SeekOrigin origin)
	{
		long originPosition = 0;
		if(origin == SeekOrigin.Current) originPosition = this.position;
		else if(origin == SeekOrigin.End) originPosition = this.Length;
		this.position = originPosition + offset;
		return this.position;
	}

	public override long Position
	{
		get { return this.position; }
		set { this.position = value; }
	}
	private long position;
}

}
