// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;

namespace System.Xaml.Permissions
{
    [Serializable]
    public sealed class XamlLoadPermission : CodeAccessPermission, IUnrestrictedPermission
    {
        private static IList<XamlAccessLevel> s_emptyAccessLevel;
        private bool _isUnrestricted;

        private const string IPermissionTagName = "IPermission";
        private const string ClassAttributeName = "class";
        private const string VersionAttributeName = "version";
        private const string VersionNumber = "1";
        private const string UnrestrictedAttributeName = "Unrestricted";

        public XamlLoadPermission(PermissionState state)
        {
            Init(state == PermissionState.Unrestricted, null);
        }

        public XamlLoadPermission(XamlAccessLevel allowedAccess)
        {
            if (allowedAccess == null)
            {
                throw new ArgumentNullException(nameof(allowedAccess));
            }

            Init(false, new XamlAccessLevel[] { allowedAccess });
        }

        public XamlLoadPermission(IEnumerable<XamlAccessLevel> allowedAccess)
        {
            if (allowedAccess == null)
            {
                throw new ArgumentNullException(nameof(allowedAccess));
            }

            var accessList = new List<XamlAccessLevel>(allowedAccess);
            foreach (XamlAccessLevel accessLevel in allowedAccess)
            {
                if (accessLevel == null)
                {
                    throw new ArgumentException(SR.Get(SRID.CollectionCannotContainNulls, nameof(allowedAccess)), nameof(allowedAccess));
                }

                accessList.Add(accessLevel);
            }
            Init(false, accessList);
        }

#if NETCOREAPP3_0
        [ComVisible(false)]
        public override bool Equals(object obj)
        {
            IPermission perm = obj as IPermission;
            if (obj != null && perm == null)
            {
                return false;
            }

            try
            {
                if (!IsSubsetOf(perm))
                {
                    return false;
                }

                if (perm != null && !perm.IsSubsetOf(this))
                {
                    return false;
                }
            }
            catch (ArgumentException)
            {
                // Any argument exception implies inequality
                // Note that we require a try/catch here because we have to deal with
                // custom permissions that may throw exceptions indiscriminately
                return false;
            }

            return true;
        }

        [ComVisible(false)]
        public override int GetHashCode() => base.GetHashCode();
#endif 

        // copy ctor. We can reuse the list of the existing instance, because it is a
        // ReadOnlyCollection over a privately created array, hence is never mutated,
        // even if the other instance is mutated via FromXml().
        private XamlLoadPermission(XamlLoadPermission other)
        {
            _isUnrestricted = other._isUnrestricted;
            AllowedAccess = other.AllowedAccess;
        }

        private void Init(bool isUnrestricted, IList<XamlAccessLevel> allowedAccess)
        {
            _isUnrestricted = isUnrestricted;
            if (allowedAccess == null)
            {
                if (s_emptyAccessLevel == null)
                {
                    s_emptyAccessLevel = new ReadOnlyCollection<XamlAccessLevel>(new XamlAccessLevel[0]);
                }
                AllowedAccess = s_emptyAccessLevel;
            }
            else
            {
                Debug.Assert(!isUnrestricted);
                AllowedAccess = new ReadOnlyCollection<XamlAccessLevel>(allowedAccess);
            }
        }

        public IList<XamlAccessLevel> AllowedAccess { get; private set; } // ReadOnlyCollection

        public override IPermission Copy() => new XamlLoadPermission(this);

        public override void FromXml(SecurityElement elem)
        {
            if (elem == null)
            {
                throw new ArgumentNullException(nameof(elem));
            }
            if (elem.Tag != IPermissionTagName)
            {
                throw new ArgumentException(SR.Get(SRID.SecurityXmlUnexpectedTag, elem.Tag, IPermissionTagName), nameof(elem));
            }

            string className = elem.Attribute(ClassAttributeName);
            if (className == null || !className.StartsWith(GetType().FullName, false, TypeConverterHelper.InvariantEnglishUS))
            {
                throw new ArgumentException(SR.Get(SRID.SecurityXmlUnexpectedValue, className, ClassAttributeName, GetType().FullName), nameof(elem));
            }

            string version = elem.Attribute(VersionAttributeName);
            if (version != null && version != VersionNumber)
            {
                throw new ArgumentException(SR.Get(SRID.SecurityXmlUnexpectedValue, className, VersionAttributeName, VersionNumber), nameof(elem));
            }

            string unrestricted = elem.Attribute(UnrestrictedAttributeName);
            if (unrestricted != null && bool.Parse(unrestricted))
            {
                Init(true, null);
            }
            else
            {
                List<XamlAccessLevel> allowedAccess = null;
                if (elem.Children != null)
                {
                    allowedAccess = new List<XamlAccessLevel>(elem.Children.Count);
                    foreach (SecurityElement child in elem.Children)
                    {
                        allowedAccess.Add(XamlAccessLevel.FromXml(child));
                    }
                }
                Init(false, allowedAccess);
            }
        }

        public bool Includes(XamlAccessLevel requestedAccess)
        {
            if (requestedAccess == null)
            {
                throw new ArgumentNullException(nameof(requestedAccess));
            }

            if (_isUnrestricted)
            {
                return true;
            }
            foreach (XamlAccessLevel allowedAccess in AllowedAccess)
            {
                if (allowedAccess.Includes(requestedAccess))
                {
                    return true;
                }
            }
            return false;
        }

        public override IPermission Intersect(IPermission target)
        {
            if (target == null)
            {
                return null;
            }

            XamlLoadPermission other = CastPermission(target, nameof(target));
            if (other.IsUnrestricted())
            {
                return Copy();
            }
            if (IsUnrestricted())
            {
                return other.Copy();
            }

            var result = new List<XamlAccessLevel>();
            // We could optimize this with a hash, but we don't expect people to be creating
            // large unions of access levels.
            foreach (XamlAccessLevel accessLevel in AllowedAccess)
            {
                // First try the full access level
                if (other.Includes(accessLevel))
                {
                    result.Add(accessLevel);
                }
                // Then try the assembly subset
                else if (accessLevel.PrivateAccessToTypeName != null)
                {
                    XamlAccessLevel assemblyAccess = accessLevel.AssemblyOnly();
                    if (other.Includes(assemblyAccess))
                    {
                        result.Add(assemblyAccess);
                    }
                }
            }
            return new XamlLoadPermission(result);
        }

        public override bool IsSubsetOf(IPermission target)
        {
            if (target == null)
            {
                return !IsUnrestricted() && AllowedAccess.Count == 0;
            }

            XamlLoadPermission other = CastPermission(target, nameof(target));
            if (other.IsUnrestricted())
            {
                return true;
            }
            if (IsUnrestricted())
            {
                return false;
            }

            foreach (XamlAccessLevel accessLevel in AllowedAccess)
            {
                if (!other.Includes(accessLevel))
                {
                    return false;
                }
            }
            return true;
        }

        public override SecurityElement ToXml()
        {
            var element = new SecurityElement(IPermissionTagName);
            element.AddAttribute(ClassAttributeName, GetType().AssemblyQualifiedName);
            element.AddAttribute(VersionAttributeName, VersionNumber);

            if (IsUnrestricted())
            {
                element.AddAttribute(UnrestrictedAttributeName, Boolean.TrueString);
            }
            else
            {
                foreach (XamlAccessLevel accessLevel in AllowedAccess)
                {
                    element.AddChild(accessLevel.ToXml());
                }
            }

            return element;
        }

        public override IPermission Union(IPermission other)
        {
            if (other == null)
            {
                return Copy();
            }

            XamlLoadPermission xamlOther = CastPermission(other, nameof(other));
            if (IsUnrestricted() || xamlOther.IsUnrestricted())
            {
                return new XamlLoadPermission(PermissionState.Unrestricted);
            }

            var mergedAccess = new List<XamlAccessLevel>(AllowedAccess);
            foreach (XamlAccessLevel accessLevel in xamlOther.AllowedAccess)
            {
                if (!Includes(accessLevel))
                {
                    mergedAccess.Add(accessLevel);
                    if (accessLevel.PrivateAccessToTypeName != null)
                    {
                        // If we have an entry for access to just the assembly of this type, it is now redundant
                        for (int i = 0; i < mergedAccess.Count; i++)
                        {
                            if (mergedAccess[i].PrivateAccessToTypeName == null &&
                                mergedAccess[i].AssemblyNameString == accessLevel.AssemblyNameString)
                            {
                                mergedAccess.RemoveAt(i);
                                break;
                            }
                        }
                    }
                }
            }
            return new XamlLoadPermission(mergedAccess);
        }
        
        public bool IsUnrestricted() => _isUnrestricted;

        private static XamlLoadPermission CastPermission(IPermission other, string argName)
        {
            if (!(other is XamlLoadPermission result))
            {
                throw new ArgumentException(SR.Get(SRID.ExpectedLoadPermission), argName);
            }

            return result;
        }
    }
}
