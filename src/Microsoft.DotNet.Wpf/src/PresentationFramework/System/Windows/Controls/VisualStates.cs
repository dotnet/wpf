// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

﻿
using System;
using System.Windows;
using System.Security;
using System.Runtime.InteropServices;

using MS.Internal;

namespace System.Windows.Controls
{
    /// <summary>
    /// Names and helpers for visual states in the controls.
    /// <remarks>THIS IS A SHARED FILE.  PresentationFramework.Design.dll must be rebuilt if changed.</remarks>
    /// </summary>
    internal static class VisualStates
    {
        #region CalendarDayButton

        /// <summary>
        /// Identifies the Today state.
        /// </summary>
        internal const string StateToday = "Today";

        /// <summary>
        /// Identifies the RegularDay state.
        /// </summary>
        internal const string StateRegularDay = "RegularDay";

        /// <summary>
        /// Name of the Day state group.
        /// </summary>
        internal const string GroupDay = "DayStates";

        /// <summary>
        /// Identifies the BlackoutDay state.
        /// </summary>
        internal const string StateBlackoutDay = "BlackoutDay";

        /// <summary>
        /// Identifies the NormalDay state.
        /// </summary>
        internal const string StateNormalDay = "NormalDay";

        /// <summary>
        /// Name of the BlackoutDay state group.
        /// </summary>
        internal const string GroupBlackout = "BlackoutDayStates";

        #endregion Constants

        #region GroupCalendarButtonFocus
        /// <summary>
        /// Unfocused state for Calendar Buttons
        /// </summary>
        public const string StateCalendarButtonUnfocused = "CalendarButtonUnfocused";

        /// <summary>
        /// Focused state for Calendar Buttons
        /// </summary>
        public const string StateCalendarButtonFocused = "CalendarButtonFocused";

        /// <summary>
        /// CalendarButtons Focus state group
        /// </summary>
        public const string GroupCalendarButtonFocus = "CalendarButtonFocusStates";

        #endregion GroupCalendarButtonFocus

        #region GroupCommon
        /// <summary>
        /// Normal state
        /// </summary>
        public const string StateNormal = "Normal";

        /// <summary>
        /// MouseOver state
        /// </summary>
        public const string StateMouseOver = "MouseOver";

        /// <summary>
        /// Pressed state
        /// </summary>
        public const string StatePressed = "Pressed";

        /// <summary>
        /// Disabled state
        /// </summary>
        public const string StateDisabled = "Disabled";

        /// <summary>
        /// Readonly state
        /// </summary>
        public const string StateReadOnly = "ReadOnly";

        /// <summary>
        /// Transition into the Normal state in the ProgressBar template.
        /// </summary>
        internal const string StateDeterminate = "Determinate";

        /// <summary>
        /// Common state group
        /// </summary>
        public const string GroupCommon = "CommonStates";
        #endregion GroupCommon

        #region GroupFocus
        /// <summary>
        /// Unfocused state
        /// </summary>
        public const string StateUnfocused = "Unfocused";

        /// <summary>
        /// Focused state
        /// </summary>
        public const string StateFocused = "Focused";

        /// <summary>
        /// Focused and Dropdown is showing state
        /// </summary>
        public const string StateFocusedDropDown = "FocusedDropDown";

        /// <summary>
        /// Focus state group
        /// </summary>
        public const string GroupFocus = "FocusStates";
        #endregion GroupFocus

         #region GroupExpansion

        /// <summary>
        /// Expanded state of the Expansion state group.
        /// </summary>
        public const string StateExpanded = "Expanded";

        /// <summary>
        /// Collapsed state of the Expansion state group.
        /// </summary>
        public const string StateCollapsed = "Collapsed";

        /// <summary>
        /// Expansion state group.
        /// </summary>
        public const string GroupExpansion = "ExpansionStates";
        #endregion GroupExpansion
        
        #region GroupOpen
        
        public const string StateOpen = "Open";
        public const string StateClosed = "Closed";
        
        public const string GroupOpen = "OpenStates";

        #endregion

        #region GroupHasItems
        
        /// <summary>
        /// HasItems state of the HasItems state group.
        /// </summary>
        public const string StateHasItems = "HasItems";

        /// <summary>
        /// NoItems state of the HasItems state group.
        /// </summary>
        public const string StateNoItems = "NoItems";

        /// <summary>
        /// HasItems state group.
        /// </summary>
        public const string GroupHasItems = "HasItemsStates";
        #endregion GroupHasItems

        #region GroupExpandDirection

        /// <summary>
        /// Down expand direction state of ExpandDirection state group.
        /// </summary>
        public const string StateExpandDown = "ExpandDown";

        /// <summary>
        /// Up expand direction state of ExpandDirection state group.
        /// </summary>
        public const string StateExpandUp = "ExpandUp";

        /// <summary>
        /// Left expand direction state of ExpandDirection state group.
        /// </summary>
        public const string StateExpandLeft = "ExpandLeft";

        /// <summary>
        /// Right expand direction state of ExpandDirection state group.
        /// </summary>
        public const string StateExpandRight = "ExpandRight";

        /// <summary>
        /// ExpandDirection state group.
        /// </summary>
        public const string GroupExpandDirection = "ExpandDirectionStates";
        #endregion
        

        #region GroupSelection
        /// <summary>
        /// Selected state
        /// </summary>
        public const string StateSelected = "Selected";

        /// <summary>
        /// Selected and unfocused state
        /// </summary>
        public const string StateSelectedUnfocused = "SelectedUnfocused";

        /// <summary>
        /// Selected and inactive state
        /// </summary>
        public const string StateSelectedInactive = "SelectedInactive";

        /// <summary>
        /// Unselected state
        /// </summary>
        public const string StateUnselected = "Unselected";

        /// <summary>
        /// Selection state group
        /// </summary>
        public const string GroupSelection = "SelectionStates";
        #endregion GroupSelection

        #region GroupEdit
        /// <summary>
        /// Editable state
        /// </summary>
        public const string StateEditable = "Editable";

        /// <summary>
        /// Uneditable state
        /// </summary>
        public const string StateUneditable = "Uneditable";

        /// <summary>
        /// Edit state group
        /// </summary>
        public const string GroupEdit = "EditStates";
        #endregion GroupEdit

        #region GroupActive
        /// <summary>
        /// Active state
        /// </summary>
        public const string StateActive = "Active";

        /// <summary>
        /// Inactive state
        /// </summary>
        public const string StateInactive = "Inactive";

        /// <summary>
        /// Active state group
        /// </summary>
        public const string GroupActive = "ActiveStates";
        #endregion GroupActive

        #region GroupValidation
        /// <summary>
        /// Valid state
        /// </summary>
        public const string StateValid = "Valid";

        /// <summary>
        /// InvalidFocused state
        /// </summary>
        public const string StateInvalidFocused = "InvalidFocused";

        /// <summary>
        /// InvalidUnfocused state
        /// </summary>
        public const string StateInvalidUnfocused = "InvalidUnfocused";

        /// <summary>
        /// Validation state group
        /// </summary>
        public const string GroupValidation = "ValidationStates";
        #endregion GroupValidation

        #region GroupWatermark
        /// <summary>
        /// Unwatermarked state
        /// </summary>
        public const string StateUnwatermarked = "Unwatermarked";

        /// <summary>
        /// Watermarked state
        /// </summary>
        public const string StateWatermarked = "Watermarked";

        /// <summary>
        /// Watermark state group
        /// </summary>
        public const string GroupWatermark = "WatermarkStates";
        #endregion GroupWatermark

        #region GroupChecked

        public const string StateChecked = "Checked";
        public const string StateUnchecked = "Unchecked";
        public const string StateIndeterminate = "Indeterminate";

        public const string GroupCheck = "CheckStates";

        #endregion

        #region GroupCurrent
        /// <summary>
        /// Regular state
        /// </summary>
        public const string StateRegular = "Regular";

        /// <summary>
        /// Current state
        /// </summary>
        public const string StateCurrent = "Current";

        /// <summary>
        /// Current state group
        /// </summary>
        public const string GroupCurrent = "CurrentStates";
        #endregion GroupCurrent

        #region GroupInteraction
        /// <summary>
        /// Display state
        /// </summary>
        public const string StateDisplay = "Display";

        /// <summary>
        /// Editing state
        /// </summary>
        public const string StateEditing = "Editing";

        /// <summary>
        /// Interaction state group
        /// </summary>
        public const string GroupInteraction = "InteractionStates";
        #endregion GroupInteraction


        #region GroupSort
        /// <summary>
        /// Unsorted state
        /// </summary>
        public const string StateUnsorted = "Unsorted";

        /// <summary>
        /// Sort Ascending state
        /// </summary>
        public const string StateSortAscending = "SortAscending";

        /// <summary>
        /// Sort Descending state
        /// </summary>
        public const string StateSortDescending = "SortDescending";

        /// <summary>
        /// Sort state group
        /// </summary>
        public const string GroupSort = "SortStates";
        #endregion GroupSort

        #region DataGridRow

        public const string DATAGRIDROW_stateAlternate = "Normal_AlternatingRow";
        public const string DATAGRIDROW_stateMouseOver = "MouseOver";
        public const string DATAGRIDROW_stateMouseOverEditing = "MouseOver_Unfocused_Editing";
        public const string DATAGRIDROW_stateMouseOverEditingFocused = "MouseOver_Editing";
        public const string DATAGRIDROW_stateMouseOverSelected = "MouseOver_Unfocused_Selected";
        public const string DATAGRIDROW_stateMouseOverSelectedFocused = "MouseOver_Selected";
        public const string DATAGRIDROW_stateNormal = "Normal";
        public const string DATAGRIDROW_stateNormalEditing = "Unfocused_Editing";
        public const string DATAGRIDROW_stateNormalEditingFocused = "Normal_Editing";
        public const string DATAGRIDROW_stateSelected = "Unfocused_Selected";
        public const string DATAGRIDROW_stateSelectedFocused = "Normal_Selected";

        #endregion DataGridRow

        #region DataGridRowHeader

        public const string DATAGRIDROWHEADER_stateMouseOver = "MouseOver";
        public const string DATAGRIDROWHEADER_stateMouseOverCurrentRow = "MouseOver_CurrentRow";
        public const string DATAGRIDROWHEADER_stateMouseOverEditingRow = "MouseOver_Unfocused_EditingRow";
        public const string DATAGRIDROWHEADER_stateMouseOverEditingRowFocused = "MouseOver_EditingRow";
        public const string DATAGRIDROWHEADER_stateMouseOverSelected = "MouseOver_Unfocused_Selected";
        public const string DATAGRIDROWHEADER_stateMouseOverSelectedCurrentRow = "MouseOver_Unfocused_CurrentRow_Selected";
        public const string DATAGRIDROWHEADER_stateMouseOverSelectedCurrentRowFocused = "MouseOver_CurrentRow_Selected";
        public const string DATAGRIDROWHEADER_stateMouseOverSelectedFocused = "MouseOver_Selected";
        public const string DATAGRIDROWHEADER_stateNormal = "Normal";
        public const string DATAGRIDROWHEADER_stateNormalCurrentRow = "Normal_CurrentRow";
        public const string DATAGRIDROWHEADER_stateNormalEditingRow = "Unfocused_EditingRow";
        public const string DATAGRIDROWHEADER_stateNormalEditingRowFocused = "Normal_EditingRow";
        public const string DATAGRIDROWHEADER_stateSelected = "Unfocused_Selected";
        public const string DATAGRIDROWHEADER_stateSelectedCurrentRow = "Unfocused_CurrentRow_Selected";
        public const string DATAGRIDROWHEADER_stateSelectedCurrentRowFocused = "Normal_CurrentRow_Selected";
        public const string DATAGRIDROWHEADER_stateSelectedFocused = "Normal_Selected";
        
        #endregion DataGridRowHeader


        /// <summary>
        /// Use VisualStateManager to change the visual state of the control.
        /// </summary>
        /// <param name="control">
        /// Control whose visual state is being changed.
        /// </param>
        /// <param name="useTransitions">
        /// true to use transitions when updating the visual state, false to
        /// snap directly to the new visual state.
        /// </param>
        /// <param name="stateNames">
        /// Ordered list of state names and fallback states to transition into.
        /// Only the first state to be found will be used.
        /// </param>
        public static void GoToState(Control control, bool useTransitions, params string[] stateNames)
        {
            if (stateNames == null)
            {
                return;
            }

            foreach (string name in stateNames)
            {
                if (VisualStateManager.GoToState(control, name, useTransitions))
                {
                    break;
                }
            }
        }
    }
}
