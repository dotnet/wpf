// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Base class for Automation Idenfitiers (Property, Event, etc.)


using System;
using System.Collections;
using System.Diagnostics;
using MS.Internal.Automation;
using MS.Internal.UIAutomationTypes.Interop;


namespace System.Windows.Automation
{
    /// <summary>
    /// Base class for object identity based identifiers.
    /// Implement ISerializable to ensure that it remotes propertly
    /// This class is effectively abstract, only derived classes are
    /// instantiated.
    /// </summary>
#if (INTERNAL_COMPILE)
    internal class AutomationIdentifier : IComparable
#else
    public class AutomationIdentifier : IComparable
#endif
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        // All Guids will be empty now since we are not calling UiaLookupId, but we need to keep the Guid
        // constructor and field because VS reflects into it
        internal AutomationIdentifier(UiaCoreTypesApi.AutomationIdType type, int id, string programmaticName) 
            : this(type, id, Guid.Empty, programmaticName)
        {
        }

        // Internal so only our own derived classes can actually
        // use this class. (3rd party classes can try deriving,
        // but the internal ctor will prevent instantiation.)
        internal AutomationIdentifier(UiaCoreTypesApi.AutomationIdType type, int id, Guid guid, string programmaticName)
        {
            Debug.Assert(id != 0);
            _id = id;
            _type = type;
            _guid = guid;
            _programmaticName = programmaticName;
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        #region Public Properties

        /// <summary>
        /// Returns underlying identifier as used by provider interfaces.
        /// </summary>
        /// <remarks>
        /// Use LookupById method to convert back from Id to an
        /// AutomationIdentifier
        /// </remarks>
        public int Id
        {
            get
            {
                return _id;
            }
        }

        /// <summary>
        /// Returns the programmatic name passed in on registration.
        /// </summary>
        /// <remarks>
        /// Appends the type to the programmatic name.
        /// </remarks>
        public string ProgrammaticName
        {
            get
            {
                return _programmaticName;
            }
        }

        #endregion Public Properties


        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods

        /// <summary>
        /// Tests whether two AutomationIdentifier objects are equivalent
        /// </summary>
        public override bool Equals( object obj )
        {
            return obj == (object)this;
        }

        /// <summary>
        /// Overrides Object.GetHashCode()
        /// </summary>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        /// <summary>
        /// For IComparable()
        /// </summary>
        public int CompareTo(object obj)
        {
            Debug.Assert(obj != null, "Null obj!");
            if (obj == null)
                throw new ArgumentNullException("obj");

            // Ordering allows arrays of references to these to be sorted - though the sort order is undefined.
            Debug.Assert(obj is AutomationIdentifier, "CompareTo called with unexpected type");
            return GetHashCode() - obj.GetHashCode();
        }

        #endregion Public Methods


        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        internal static AutomationIdentifier Register(UiaCoreTypesApi.AutomationIdType type, int id, string programmaticName)
        {
            // Keep past behavior, if the id is not supported by the current OS, return null 
            if (!IsIdSupported(type, id))
            {
                return null;
            }
            lock (_idTable)
            {
                // See if instance already exists...
                AutomationIdentifier autoid = (AutomationIdentifier)_idTable[id];
                if (autoid != null)
                {
                    return autoid;
                }

                // If not, create one...
                switch (type)
                {
                    case UiaCoreTypesApi.AutomationIdType.Property:      autoid = new AutomationProperty(id, programmaticName);      break;
                    case UiaCoreTypesApi.AutomationIdType.Event:         autoid = new AutomationEvent(id, programmaticName);         break;
                    case UiaCoreTypesApi.AutomationIdType.TextAttribute: autoid = new AutomationTextAttribute(id, programmaticName); break;
                    case UiaCoreTypesApi.AutomationIdType.Pattern:       autoid = new AutomationPattern(id, programmaticName);       break;
                    case UiaCoreTypesApi.AutomationIdType.ControlType:   autoid = new ControlType(id, programmaticName);             break;

                    default: Debug.Assert(false, "Invalid type specified for AutomationIdentifier");
                        throw new InvalidOperationException("Invalid type specified for AutomationIdentifier");
                }

                _idTable[id] = autoid;
                return autoid;
            }
        }

        internal static AutomationIdentifier LookupById(UiaCoreTypesApi.AutomationIdType type, int id)
        {
            AutomationIdentifier autoid;
            lock (_idTable)
            {
                autoid = (AutomationIdentifier) _idTable[id];
            }

            if(autoid == null)
            {
                return null;
            }

            if(autoid._type != type)
            {
                return null;
            }

            return autoid;
        }
        #endregion Internal Methods

        #region Private Methods

        private static bool IsIdSupported(UiaCoreTypesApi.AutomationIdType type, int id)
        {
            switch (type)
            {
                case UiaCoreTypesApi.AutomationIdType.Property: return IsPropertySupported(id);
                case UiaCoreTypesApi.AutomationIdType.Event: return IsEventSupported(id);
                case UiaCoreTypesApi.AutomationIdType.TextAttribute: return IsTextAttributeSupported(id);
                case UiaCoreTypesApi.AutomationIdType.Pattern: return IsPatternSupported(id);
                case UiaCoreTypesApi.AutomationIdType.ControlType: return IsControlTypeSupported(id);

                default: return false;
            }
        }

        private static bool IsPropertySupported(int id)
        {
            return ((id >= (int)AutomationIdentifierConstants.FirstProperty)
                    && (id <= (int)AutomationIdentifierConstants.LastSupportedProperty));
        }

        private static bool IsEventSupported(int id)
        {
            return ((id >= (int)AutomationIdentifierConstants.FirstEvent)
                    && (id <= (int)AutomationIdentifierConstants.LastSupportedEvent));
        }

        private static bool IsPatternSupported(int id)
        {
            return ((id >= (int)AutomationIdentifierConstants.FirstPattern)
                    && (id <= (int)AutomationIdentifierConstants.LastSupportedPattern));
        }

        private static bool IsTextAttributeSupported(int id)
        {
            return ((id >= (int)AutomationIdentifierConstants.FirstTextAttribute)
                    && (id <= (int)AutomationIdentifierConstants.LastSupportedTextAttribute));
        }

        private static bool IsControlTypeSupported(int id)
        {
            return ((id >= (int)AutomationIdentifierConstants.FirstControlType)
                    && (id <= (int)AutomationIdentifierConstants.LastSupportedControlType));
        }

        #endregion Private Methods



        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        private Guid _guid;
        private UiaCoreTypesApi.AutomationIdType _type;
        private int    _id; // value used in core
        private string _programmaticName;

        // As of 8/18/03 there were 187 entries added in a normal loading
        private static Hashtable _idTable = new Hashtable(200,1.0f);

        #endregion Private Fields
    }
}
