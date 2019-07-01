// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Xml.Serialization;
using Microsoft.Test.Diagnostics;

// TODO: Currently only Connect and Disconnect actions are supported.
// UserSessionAction is not used for state management currently.

namespace Microsoft.Test.StateManagement
{
    /// <summary>
    /// Allows you to control a user session by disconnecting, connecting user sessions
    /// </summary>
    [Serializable()]
    public class UserSessionState : State<UserSession, Nullable<UserSessionAction>>
    {
        #region Private Member

        private int sessionId = -1;        
        
        #endregion

        #region Constructor

        /// <summary>
        /// Parameterless constructor used for serialization
        /// </summary>
        internal UserSessionState()
            : base()
        {
        }        

        /// <summary>
        /// Allows you to query and control a session based on a specified session Id
        /// </summary>
        /// <param name="sessionId"></param>
        public UserSessionState(int sessionId)
            : base()
        {
            this.sessionId = sessionId;
        }

        #endregion

        #region Public members

        /// <summary>
        /// The Id of the session to control
        /// </summary>
        public int SessionId
        {
            get 
            { 
                return sessionId; 
            }
            set 
            { 
                sessionId = value; 
            }
        }

        #endregion

        #region Override Members

        /// <summary>
        /// Gets the value of the state
        /// </summary>
        /// <returns>Value of the state</returns>
        public override UserSession GetValue()
        {
            return GetUserSessionState(sessionId);            
        }

        /// <summary>
        /// Allows you to transition the session state based on a given action
        /// </summary>
        /// <param name="value"></param>
        /// <param name="action"></param>
        public override bool SetValue(UserSession value, Nullable<UserSessionAction> action)
        {
            if (value == null)
                return false;

            switch (value.SessionType)
            {
                case UserSessionType.Active:
                    return UserSessionHelper.TryUserSessionConnect(sessionId, "Console");
                case UserSessionType.Disconnected:
                    return UserSessionHelper.TryUserSessionDisconnect(sessionId);
                case UserSessionType.Locked:
                    // TODO: Locked state is currently not supported 
                    return false;
            }            

            return false;
        }

        /// <summary/>
        public override bool Equals(object obj)
        {
            UserSessionState userSessionState = obj as UserSessionState;
            if (obj == null)
                return false;

            if (userSessionState.sessionId == sessionId)
                return true;            

            return false;
        }

        /// <summary/>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        #endregion

        #region Private Members

        // Returns the session which matches as closely as possible the requirements        
        private UserSession GetUserSessionState(int sessionId)
        {
            UserInfo userInfo = new UserInfo();
            List<WTS_SESSION_INFO> sessions = UserSessionHelper.GetUserSessions();            

            // Loop through the current user sessions to find the one with matching sessionId
            foreach (WTS_SESSION_INFO session in sessions)
            {
                if (session.SessionId == sessionId)
                {
                    UserSession sessionStateValue = null;
                    if (session.State == WTS_CONNECTSTATE_CLASS.WTSActive)
                    {                        
                        // A locked session also has its state as WTSActive.
                        // Check specifically if it is locked.
                        if (UserSessionHelper.IsLoginSession(sessionId))
                        {
                            sessionStateValue = new UserSession(UserSessionType.Locked);
                        }
                        else
                        {
                            sessionStateValue = new UserSession(UserSessionType.Active);
                        }
                    }
                    if (session.State == WTS_CONNECTSTATE_CLASS.WTSDisconnected)
                    {
                        sessionStateValue = new UserSession(UserSessionType.Disconnected);
                    }
                    
                    // Grab the user info (username, domain, password)
                    if (UserSessionHelper.GetUserSessionUserInfo(sessionId, out userInfo))
                    {
                        sessionStateValue.UserLoginInfo = userInfo;
                        return sessionStateValue;
                    }
                    else
                    {
                        // For machines without auto logon, password retrieval fails. We would like to
                        // throw an exception here to fail the test case.
                        throw new InvalidOperationException("Auto logon is required to be enabled for this task to succeed");
                    }
                }                                
            }                        

            return null;
        }
                
        #endregion

        // Old code keeping it around for reference for extending support to lock, logoff and logon
        #region Old code

        /*        
        /// <summary>
        /// Allows you to transition the session state based on a given action
        /// </summary>
        /// <param name="value"></param>
        /// <param name="action"></param>
        public override bool SetValue(UserSession value, Nullable<UserSessionAction> action)
        {
            if (sessionType == UserSessionType.Active)
                return SetActiveSession(value, action);
            else if (sessionType == UserSessionType.Disconnected)
                return SetDisconnectedSession(value, action);

            return false;
        }

        private bool SetActiveSession(UserSession value, Nullable<UserSessionAction> action)
        {
            if (value == null)
                return false;

            if (!action.HasValue)
            {
                UserSession currentValue = GetUserSessionState(UserSessionType.Active);

                if (Object.Equals(currentValue, value))
                    //This ensures we unlock if the session happened to be locked, but was not disconnected or changed in another way
                    return UserSessionHelper.TryUserSessionActivateConsole(value.SessionId);

                //Can't login without a valid username
                if (String.IsNullOrEmpty(value.UserLoginInfo.Username))
                    return false;

                return UserSessionHelper.TryUserSessionLogonToConsole(value.UserLoginInfo.Username, value.UserLoginInfo.Domain, value.UserLoginInfo.Password);
            }

            switch (action.Value)
            {
                case UserSessionAction.Connect:
                    throw new ArgumentException("Can't connect an active session", "action");
                case UserSessionAction.Disconnect:
                    return UserSessionHelper.TryUserSessionDisconnect(value.SessionId);
                case UserSessionAction.Logon:
                    if (String.IsNullOrEmpty(value.UserLoginInfo.Username))
                        return false;
                    return UserSessionHelper.TryUserSessionLogonToConsole(value.UserLoginInfo.Username, value.UserLoginInfo.Domain, value.UserLoginInfo.Password);
                case UserSessionAction.Logoff:
                    return UserSessionHelper.TryUserSessionLogoff(value.SessionId);
                default:
                    throw new ArgumentException("Action specified is unknown", "action");
            }
        }

        private bool SetDisconnectedSession(UserSession value,  Nullable<UserSessionAction> action)
        {
            if (value == null)
                return false;

            if (!UserSessionHelper.IsValidSession(value.SessionId))
                return false;

            if (!action.HasValue)
                return UserSessionHelper.TryUserSessionDisconnect(value.SessionId);

            switch (action.Value)
            {
                case UserSessionAction.Connect:
                    return UserSessionHelper.TryUserSessionConnect(value.SessionId, "Console");                    
                case UserSessionAction.Disconnect:
                    return UserSessionHelper.TryUserSessionDisconnect(value.SessionId);
                case UserSessionAction.Logon:
                    throw new ArgumentException("Can't logon to a disconnected session. Need an active session first", "action");
                case UserSessionAction.Logoff:
                    return UserSessionHelper.TryUserSessionLogoff(value.SessionId);
                default:
                    throw new ArgumentException("Action specified is unknown", "action");
            }
        }
        */

        #endregion
    }

    /// <summary>
    /// Contains the information 
    /// </summary>
    [Serializable()]
    public class UserSession
    {
        #region Private Data

        private UserSessionType sessionType;        
        private UserInfo userLoginInfo = new UserInfo();

        #endregion

        #region Constructors

        /// <summary/>
        public UserSession()
            : base()
        {
        }

        internal UserSession(UserSessionType sessionType)
            : base()
        {
            this.sessionType = sessionType;
        }

        #endregion

        #region Public Members       

        public UserSessionType SessionType
        {
            get { return sessionType; }
            set { sessionType = value; }
        }

        /// <summary>
        /// Login information for the current session
        /// </summary>
        public UserInfo UserLoginInfo
        {
            get { return userLoginInfo; }
            set { userLoginInfo = value; }
        }

        #endregion

        #region Override Members

        /// <summary/>
        public override bool Equals(object obj)
        {
            UserSession otherSession = obj as UserSession;
            if (otherSession == null)
                return false;

            if (sessionType != otherSession.sessionType)
                return false;

            return userLoginInfo.Equals(otherSession.userLoginInfo);
        }

        /// <summary/>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        #endregion
    }

    /// <summary>
    /// Termines what kind of session to use
    /// </summary>
    public enum UserSessionType
    {
        // Represents an active session
        Active = 1,
        // Represents a disconnected session
        Disconnected,
        // Represents a locked session
        Locked
    }

    /// <summary>
    /// Actions that can be performed on a session state.
    /// Different actions are valid depending on the session type
    /// </summary>
    public enum UserSessionAction : int
    {
        /// <summary>
        /// Connects the console to a session 
        /// </summary>
        Connect = 1,
        /// <summary>
        /// Disconnects the specified session
        /// </summary>
        Disconnect,
        /// <summary>
        /// Performs a logon procedure
        /// </summary>
        Logon,
        /// <summary>
        /// Logs off the specified session
        /// </summary>
        Logoff
    }
}
