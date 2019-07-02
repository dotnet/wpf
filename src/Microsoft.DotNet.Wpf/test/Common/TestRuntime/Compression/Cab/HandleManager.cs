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
using System.Collections;

using Handle = System.Int32;


internal class HandleManager
{
	private ArrayList handleArray;
	private Stack freeHandles;

	public HandleManager()
	{
		handleArray = new ArrayList();
		freeHandles = new Stack();
		reuseHandles = false;
	}

	public bool ReuseHandles
	{
		get { return reuseHandles; }
		set { reuseHandles = value; }
	}
	private bool reuseHandles;

	public Handle AllocHandle(object obj)
	{
		Handle handle;
		lock(this)
		{
			if(reuseHandles && freeHandles.Count > 0)
			{
				handle = (Handle) freeHandles.Pop();
				handleArray[(int) handle - 1] = obj;
			}
			else
			{
				handle = (Handle) (handleArray.Add(obj) + 1);
			}
		}
		return handle;
	}

	public object this[Handle handle]
	{
		get
		{
			if((int) handle > 0 && (int) handle <= handleArray.Count)
			{
				return handleArray[(int) handle - 1];
			}
			else
			{
				return null;
			}
		}
	}

	public void FreeHandle(Handle handle)
	{
		lock(this)
		{
			if((int) handle > 0 && (int) handle <= handleArray.Count)
			{
				handleArray[(int) handle - 1] = null;
				freeHandles.Push(handle);
			}
		}
	}
}

}
