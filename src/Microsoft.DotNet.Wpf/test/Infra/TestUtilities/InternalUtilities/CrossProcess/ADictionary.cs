// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.Test.CrossProcess
{
    /// <summary>
    /// Abstract Dictionary - A base type defining the behavior covered by Client and Server Dictionaries.
    /// A lot of advanced dictionary capabilities are presently marked as not implemented for simplicity as we don't use/test those API's.
    /// </summary>
    public abstract class ADictionary : IDictionary<string, string>, IDisposable
    {

        #region IDisposable Members

        public abstract void Dispose();

        #endregion

        #region IDictionary<string,string> Members

        public abstract void Add(string key, string value);

        public abstract bool ContainsKey(string key);

        public virtual ICollection<string> Keys
        {
            get { throw new NotImplementedException(); }
        }

        public abstract bool Remove(string key);

        public virtual bool TryGetValue(string key, out string value)
        {
            throw new NotImplementedException();
        }

        public virtual ICollection<string> Values
        {
            get { throw new NotImplementedException(); }
        }

        public abstract string this[string key] { get; set; }

        #endregion

        #region ICollection<KeyValuePair<string,string>> Members

        public abstract void Clear();

        public virtual void Add(KeyValuePair<string, string> item)
        {
            throw new NotImplementedException();
        }

        public virtual bool Contains(KeyValuePair<string, string> item)
        {
            throw new NotImplementedException();
        }

        public virtual void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public virtual int Count
        {
            get { throw new NotImplementedException(); }
        }

        public virtual bool IsReadOnly
        {
            get { throw new NotImplementedException(); }
        }

        public virtual bool Remove(KeyValuePair<string, string> item)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region IEnumerable<KeyValuePair<string,string>> Members

        public virtual IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        #endregion

        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}