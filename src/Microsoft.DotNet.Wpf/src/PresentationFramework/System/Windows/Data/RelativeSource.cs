// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Defines RelativeSource MarkupExtension.
//

using System.ComponentModel;    // ISupportInitialize
using System.Diagnostics;
using System.Windows.Markup;    // MarkupExtension

namespace System.Windows.Data
{
    /// <summary> This enum describes the type of RelativeSource
    /// </summary>
    public enum RelativeSourceMode
    {
        /// <summary>use the DataContext from the previous scope
        /// </summary>
        PreviousData,

        /// <summary>use the target element's styled parent
        /// </summary>
        TemplatedParent,

        /// <summary>use the target element itself
        /// </summary>
        Self,

        /// <summary>use the target element's ancestor of a specified Type
        /// </summary>
        FindAncestor
    }

    /// <summary>
    ///
    /// RelativeSource Modes are
    ///     PreviousData   - use the DataContext from the previous scope
    ///     TemplatedParent- use the target element's styled parent
    ///     Self           - use the target element itself
    ///     FindAncestor   - use the hosting Type
    /// Only FindAncestor mode allows AncestorType and AncestorLevel.
    /// </summary>
    [MarkupExtensionReturnType(typeof(RelativeSource))]
    public class RelativeSource : MarkupExtension, ISupportInitialize
    {
#region constructors

        /// <summary>Constructor
        /// </summary>
        public RelativeSource()
        {
            // default mode to FindAncestor so that setting Type and Level would be OK
            _mode = RelativeSourceMode.FindAncestor;
        }

        /// <summary>Constructor
        /// </summary>
        public RelativeSource(RelativeSourceMode mode)
        {
            InitializeMode(mode);
        }

        /// <summary>Constructor for FindAncestor mode
        /// </summary>
        public RelativeSource(RelativeSourceMode mode, Type ancestorType, int ancestorLevel)
        {
            InitializeMode(mode);
            AncestorType = ancestorType;
            AncestorLevel = ancestorLevel;
        }

#endregion constructors

#region ISupportInitialize

        /// <summary>Begin Initialization</summary>
        void ISupportInitialize.BeginInit()
        {
        }

        /// <summary>End Initialization, verify that internal state is consistent</summary>
        void ISupportInitialize.EndInit()
        {
            if (IsUninitialized)
                throw new InvalidOperationException(SR.Get(SRID.RelativeSourceNeedsMode));
            if (_mode == RelativeSourceMode.FindAncestor && (AncestorType == null))
                throw new InvalidOperationException(SR.Get(SRID.RelativeSourceNeedsAncestorType));
        }

#endregion ISupportInitialize

#region public properties

        /// <summary>static instance of RelativeSource for PreviousData mode.
        /// </summary>
        public static RelativeSource PreviousData
        {
            get
            {
                if (s_previousData == null)
                {
                    s_previousData = new RelativeSource(RelativeSourceMode.PreviousData);
                }
                return s_previousData;
            }
        }

        /// <summary>static instance of RelativeSource for TemplatedParent mode.
        /// </summary>
        public static RelativeSource TemplatedParent
        {
            get
            {
                if (s_templatedParent == null)
                {
                    s_templatedParent = new RelativeSource(RelativeSourceMode.TemplatedParent);
                }
                return s_templatedParent;
            }
        }

        /// <summary>static instance of RelativeSource for Self mode.
        /// </summary>
        public static RelativeSource Self
        {
            get
            {
                if (s_self == null)
                {
                    s_self = new RelativeSource(RelativeSourceMode.Self);
                }
                return s_self;
            }
        }

        /// <summary>mode of RelativeSource
        /// </summary>
        /// <remarks> Mode is read-only after initialization.
        /// If Mode is not set explicitly, setting AncestorType or AncestorLevel will implicitly lock the Mode to FindAncestor. </remarks>
        /// <exception cref="InvalidOperationException"> RelativeSource Mode is immutable after initialization;
        /// instead of changing the Mode on this instance, create a new RelativeSource or use a different static instance. </exception>
        [ConstructorArgument("mode")]
        public RelativeSourceMode Mode
        {
            get { return _mode; }
            set
            {
                if (IsUninitialized)
                {
                    InitializeMode(value);
                }
                else if (value != _mode)    // mode changes are not allowed
                {
                    throw new InvalidOperationException(SR.Get(SRID.RelativeSourceModeIsImmutable));
                }
            }
        }

        /// <summary> The Type of ancestor to look for, in FindAncestor mode.
        /// </summary>
        /// <remarks> if Mode has not been set explicitly, setting AncestorType will implicitly lock Mode to FindAncestor. </remarks>
        /// <exception cref="InvalidOperationException"> RelativeSource is not in FindAncestor mode </exception>
        public Type AncestorType
        {
            get { return _ancestorType; }
            set
            {
                if (IsUninitialized)
                {
                    Debug.Assert(_mode == RelativeSourceMode.FindAncestor);
                    AncestorLevel = 1;  // lock the mode and set default level
                }

                if (_mode != RelativeSourceMode.FindAncestor)
                {
                    // in all other modes, AncestorType should not get set to a non-null value
                    if (value != null)
                        throw new InvalidOperationException(SR.Get(SRID.RelativeSourceNotInFindAncestorMode));
                }
                else
                {
                    _ancestorType = value;
                }
            }
        }

        /// <summary>
        /// This method is used by TypeDescriptor to determine if this property should
        /// be serialized.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool ShouldSerializeAncestorType()
        {
            return (_mode == RelativeSourceMode.FindAncestor);
        }

        /// <summary> The level of ancestor to look for, in FindAncestor mode.  Use 1 to indicate the one nearest to the target element.
        /// </summary>
        /// <remarks> if Mode has not been set explicitly, getting AncestorLevel will return -1 and
        /// setting AncestorLevel will implicitly lock Mode to FindAncestor. </remarks>
        /// <exception cref="InvalidOperationException"> RelativeSource is not in FindAncestor mode </exception>
        /// <exception cref="ArgumentOutOfRangeException"> AncestorLevel cannot be set to less than 1 </exception>
        public int AncestorLevel
        {
            get { return _ancestorLevel; }
            set
            {
                Debug.Assert((!IsUninitialized) || (_mode == RelativeSourceMode.FindAncestor));

                if (_mode != RelativeSourceMode.FindAncestor)
                {
                    // in all other modes, AncestorLevel should not get set to a non-zero value
                    if (value != 0)
                        throw new InvalidOperationException(SR.Get(SRID.RelativeSourceNotInFindAncestorMode));
                }
                else if (value < 1)
                {
                    throw new ArgumentOutOfRangeException(SR.Get(SRID.RelativeSourceInvalidAncestorLevel));
                }
                else
                {
                    _ancestorLevel = value;
                }
            }
        }

        /// <summary>
        /// This method is used by TypeDescriptor to determine if this property should
        /// be serialized.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool ShouldSerializeAncestorLevel()
        {
            return (_mode == RelativeSourceMode.FindAncestor);
        }

#endregion public properties

#region public methods

        /// <summary>
        ///  Return an object that should be set on the targetObject's targetProperty
        ///  for this markup extension.  
        /// </summary>
        /// <param name="serviceProvider">ServiceProvider that can be queried for services.</param>
        /// <returns>
        ///  The object to set on this property.
        /// </returns>
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            if (_mode == RelativeSourceMode.PreviousData)
                return PreviousData;
            if (_mode == RelativeSourceMode.Self)
                return Self;
            if (_mode == RelativeSourceMode.TemplatedParent)
                return TemplatedParent;
            return this;
        }

#endregion public methods

#region private properties
        private bool IsUninitialized
        {
            get { return (_ancestorLevel == -1); }
        }
#endregion private properties

#region private methods
        void InitializeMode(RelativeSourceMode mode)
        {
            Debug.Assert(IsUninitialized);

            if (mode == RelativeSourceMode.FindAncestor)
            {
                // default level
                _ancestorLevel = 1;
                _mode = mode;
            }
            else if (mode == RelativeSourceMode.PreviousData
                || mode == RelativeSourceMode.Self
                || mode == RelativeSourceMode.TemplatedParent)
            {
                _ancestorLevel = 0;
                _mode = mode;
            }
            else
            {
                throw new ArgumentException(SR.Get(SRID.RelativeSourceModeInvalid), "mode");
            }
        }
#endregion private methods

#region private fields

        private RelativeSourceMode _mode;
        private Type _ancestorType;
        private int _ancestorLevel = -1;    // while -1, indicates _mode has not been set

        private static RelativeSource s_previousData;
        private static RelativeSource s_templatedParent;
        private static RelativeSource s_self;
#endregion private fields
    }
}
