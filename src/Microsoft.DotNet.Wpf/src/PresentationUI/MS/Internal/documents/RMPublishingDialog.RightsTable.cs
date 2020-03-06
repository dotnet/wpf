// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: 
//    RightsTable is the grid control used to represent permisions 
//    in the RMPublishingDialog

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Security;
using System.Windows.Forms;
using System.Windows.TrustUI;

namespace MS.Internal.Documents
{
    /// <summary>
    /// The publishing dialog.
    /// </summary>
    internal sealed partial class RMPublishingDialog : DialogBaseForm
    {

        //------------------------------------------------------
        //
        //  RightsTable
        //
        //------------------------------------------------------
        /// <summary>
        /// A control for displaying and interacting with the current rm permissions.
        /// </summary>
        private class RightsTable : DataGridView
        {
            //------------------------------------------------------
            //  Constructor
            //------------------------------------------------------
            public RightsTable()
                : base()
            {
                // Set properties and handlers to handle checkbox changes without waiting for
                // EditMode to finish.
                EditMode = DataGridViewEditMode.EditOnEnter;
                this.CellBeginEdit += new DataGridViewCellCancelEventHandler(RightsTable_CellBeginEdit);
                this.CurrentCellDirtyStateChanged +=
                    new EventHandler(RightsTable_CurrentCellDirtyStateChanged);
            }

            //------------------------------------------------------
            //  Public Methods
            //------------------------------------------------------
            #region Public Methods
            /// <summary>
            /// Test if the given user name represents the Everyone user.
            /// </summary>
            /// <param name="userName">The user name to test</param>
            /// <returns>True if the user name is the Everyone user</returns>
            public static bool IsEveryone(string userName)
            {
                return userName.Equals(SR.Get(SRID.RMPublishingAnyoneUserDisplay), StringComparison.OrdinalIgnoreCase);
            }

            /// <summary>
            /// Test if the user name in the given row represents the Everyone user.
            /// </summary>
            /// <param name="row">The row to test</param>
            /// <returns>True if the row contains the Everyone user</returns>
            public static bool IsEveryone(DataGridViewRow row)
            {
                return IsEveryone(GetUsernameFromRow(row));
            }

            /// <summary>
            /// Gets the RightsManagementPermissions represented by the checkboxes in a row
            /// in the table of grants.
            /// </summary>
            /// <param name="row">The DataGridViewRow of the table</param>
            /// <returns>The permissions represented in the row</returns>
            public static RightsManagementPermissions GetPermissionsFromRow(DataGridViewRow row)
            {
                RightsManagementPermissions permissions =
                    RightsManagementPermissions.AllowNothing;

                if (row != null)
                {
                    // Enumerate through all the columns containing rights
                    // Starting at column index 1, since 0 refers to the user name.
                    for (int col = 1; col < _rightsTableColumnCount; col++)
                    {
                        // Since row is valid, and column index has been checked
                        // then cast as bool
                        bool allowed = (bool)row.Cells[col].Value;

                        // If the current cell's checkbox is checked, then add in
                        // the relevant permission
                        if (allowed)
                        {
                            permissions |= GetPermissionFromColumn(IndexToRightsTableColumn(col));
                        }
                    }
                }

                return permissions;
            }

            /// <summary>
            /// Add user to the table and scroll to the user's row if the user is not already 
            /// present.  If the user is already into the table then go to that user's row.
            /// </summary>
            /// <param name="user">RightsTableUser to add</param>
            public void AddUser(RightsTableUser user)
            {
                this.AddUser(user, false);
            }

            /// <summary>
            /// Add user to the table and scroll to the user's row if the user is not already 
            /// present.  If the user is already into the table then go to that user's row.
            /// </summary>
            /// <param name="user">RightsTableUser to add</param>
            /// <param name="isViewerOwner">Set to true if the current viewing user is an owner of
            /// the document.</param>
            public void AddUser(RightsTableUser user, bool isViewerOwner)
            {
                // Check if this user is already in the table
                if (CheckDuplicate(user.Name))
                {
                    return;
                }

                int rowIndex = 0;
                // If user is the owner, then add them to the start of the list.
                if (isViewerOwner)
                {
                    // Insert to the start of the list.
                    Rows.Insert(0, CreateDataGridViewRow(user, isViewerOwner));
                    FirstDisplayedScrollingRowIndex = 0;
                }
                else
                {
                    // The current user is not the owner, add to end of list.
                    rowIndex = Rows.Add(CreateDataGridViewRow(user, isViewerOwner));
                    FirstDisplayedScrollingRowIndex = rowIndex;
                }

                if ((user.allowOwner) || (isViewerOwner))
                {
                    // If the current user has AllowOwner permission, or is the
                    // current owner then disable the checkbox functionality.
                    UpdateAllowOwner(Rows[rowIndex]);
                }
                else if (_everyoneUserPresent)
                {
                    // If the current user is not an owner and there is an 
                    // Everyone user, copy the permissions from the Everyone
                    // row to this one
                    UpdateRowFromEveryone(Rows[_everyoneRowIndex], Rows[rowIndex]);
                }

                // Check if user is Everyone
                if (IsEveryone(user.Name))
                {
                    _everyoneUserPresent = true;
                    _everyoneRowIndex = rowIndex;
                }
            }

            /// <summary>
            /// Delete the currently selected row.
            /// </summary>
            public void DeleteUser()
            {
                // Ensure that a row (and only 1 row) is currently selected.
                if (this.SelectedRows.Count == 1)
                {
                    DataGridViewRow row = this.SelectedRows[0];

                    // Do not delete the current user (index 0)
                    if (row.Index > 0)
                    {
                        // Check if user is Everyone
                        if (IsEveryone(row))
                        {
                            UpdateAllRowsOnEveryoneRemoval(row);
                            _everyoneUserPresent = false;
                        }
                        this.Rows.Remove(row);
                    }
                }
            }

            /// <summary>
            /// Initializes the table to display information about rights to grant.
            /// </summary>
            /// <param name="grantDictionary">The grants to list in the table</param>
            public void InitializeRightsTable(
                string ownerName, 
                IDictionary<RightsManagementUser, RightsManagementLicense> grantDictionary)
            {
                InitializeRightsTableUIComponents();
                InitializeRightsTableContent(ownerName, grantDictionary);
            }

            /// <summary>
            /// Initializes the table content
            /// </summary>
            /// <param name="grantDictionary">The grants to list in the table</param>
            private void InitializeRightsTableContent(string ownerName, IDictionary<RightsManagementUser, RightsManagementLicense> grantDictionary)
            {
                // Check if there are already grants issued
                if (grantDictionary != null)
                {
                    // For each given key, add an entry in the table.
                    foreach (RightsManagementUser user in grantDictionary.Keys)
                    {
                        // Check if the user represents the current owner (or
                        // user viewing the document)
                        if (user.Name.ToUpperInvariant().Equals(
                                ownerName.ToUpperInvariant(),
                                StringComparison.Ordinal))
                        {
                            // Add user with full permissions, as the current owner.
                            AddUser(new RightsTableUser(
                                ownerName,
                                true, // AllowView
                                true, // AllowCopy
                                true, // AllowPrint
                                true, // AllowSign
                                true),// AllowOwner
                                true);// IsOwner
                        }
                        else
                        {
                            RightsManagementLicense license = grantDictionary[user];
                            if (license != null)
                            {
                                // Add user with permissions given in the license
                                AddUser(new RightsTableUser(
                                    user.Name,
                                    license.HasPermission(RightsManagementPermissions.AllowView),
                                    license.HasPermission(RightsManagementPermissions.AllowCopy),
                                    license.HasPermission(RightsManagementPermissions.AllowPrint),
                                    license.HasPermission(RightsManagementPermissions.AllowSign),
                                    license.HasPermission(RightsManagementPermissions.AllowOwner)));
                            }
                        }
                    }
                }
                else // Document is not signed, as no licenses exist in the dictionary.
                {
                    // Add user as owner.
                    AddUser(new RightsTableUser(
                        ownerName,
                        true, // AllowView
                        true, // AllowCopy
                        true, // AllowPrint
                        true, // AllowSign
                        true));// AllowOwner
                }
            }

            /// <summary>
            /// Return the current username from the given row.
            /// </summary>
            /// <param name="row">Row to use</param>
            /// <returns>The username in the row, otherwise String.Empty</returns>
            public static string GetUsernameFromRow(DataGridViewRow row)
            {
                if (row != null)
                {
                    DataGridViewCell cell = row.Cells[RightsTableColumnToIndex(RightsTableColumn.User)];
                    if (cell != null)
                    {
                        return (string)cell.Value;
                    }
                }
                return string.Empty;
            }
            #endregion Public Methods

            //------------------------------------------------------
            //  Private Methods
            //------------------------------------------------------
            #region Private Methods


            /// <summary>
            /// Checks if the specified user is already in the table, and scrolls to
            /// that user if present.
            /// </summary>
            private bool CheckDuplicate(string name)
            {
                foreach (DataGridViewRow row in Rows)
                {
                    if (GetUsernameFromRow(row).Equals(name, StringComparison.OrdinalIgnoreCase))
                    {
                        FirstDisplayedScrollingRowIndex = row.Index;
                        return true;
                    }
                }
                return false;
            }

            /// <summary>
            /// Create a DataGridViewColumn
            /// </summary>
            /// <param name="name">Name of the column</param>
            /// <param name="text">Header text to display</param>
            /// <param name="valueType">Type of column content (string or bool)</param>
            /// <param name="minWidth">Minimum width of column.</param>
            /// <returns>A DataGridViewColumn using the given parameters.</returns>
            private DataGridViewColumn CreateColumnHeader(
                string name, string text, Type valueType, int minWidth)
            {
                DataGridViewColumn header = new System.Windows.Forms.DataGridViewColumn();
                // Set Name value if one was given.
                if (!string.IsNullOrEmpty(name))
                {
                    header.Name = name;
                }
                // Set HeaderText if text value was given.
                if (!string.IsNullOrEmpty(text))
                {
                    header.HeaderText = text;
                }

                // Set the MinimumWidth, or use 20 if not defined.  Columns are autosized, but
                // this just ensures they aren't too small.
                header.MinimumWidth = (minWidth > 0) ? minWidth : _minimumBoolColumnWidth;

                // Set the value type of the column.
                header.ValueType = valueType;
                // If column type is bool, then set the CheckBox column properties.
                if (valueType == typeof(bool))
                {
                    header.ReadOnly = false;
                    header.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                    header.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                }
                else // Since column type is not bool, assume string, and set properties.
                {
                    header.ReadOnly = true;
                    header.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                    header.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleLeft;
                }
                header.Resizable = DataGridViewTriState.False;
                header.SortMode = DataGridViewColumnSortMode.NotSortable;

                return header;
            }

            /// <summary>
            /// Create a DataGridViewDisableCheckBoxCell with default properties.
            /// </summary>
            /// <param name="enabled">Represents the Enabled state of the cell.</param>
            /// <param name="value">Represents the current Checked value of the cell</param>
            /// <returns>The new DataGridViewDisableCheckBoxCell</returns>
            private DataGridViewDisableCheckBoxCell CreateCheckBoxCell(bool enabled, bool value)
            {
                DataGridViewDisableCheckBoxCell boolCell = new DataGridViewDisableCheckBoxCell();

                boolCell.Enabled = enabled;
                boolCell.Value = value;
                boolCell.Style = _boolCellStyle;

                return boolCell;
            }

            /// <summary>
            /// Create a DataGridViewRow with the given user data.
            /// </summary>
            /// <param name="newUser">RightsTableUser to represent.</param>
            /// <param name="isViewerOwner">True if the viewing user is anthe primary owner of the document.
            /// If true, the user will be given owner rights, and all the checkbox cells will be
            /// grayed out since the viewing owner's rights cannot be removed.</param>
            /// <returns>The new DataGridViewRow</returns>
            private DataGridViewRow CreateDataGridViewRow(RightsTableUser newUser, bool isViewerOwner)
            {
                DataGridViewRow row = new DataGridViewRow();

                // Name
                DataGridViewCell nameCell = new DataGridViewTextBoxCell();
                nameCell.Value = newUser.Name;
                nameCell.Style = _textCellStyle;
                row.Cells.Add(nameCell);

                // AllowView
                row.Cells.Add(CreateCheckBoxCell(false, newUser.allowView || isViewerOwner));

                // AllowCopy
                row.Cells.Add(CreateCheckBoxCell(
                    !((isViewerOwner) || newUser.allowOwner), newUser.allowCopy || isViewerOwner));

                // AllowPrint
                row.Cells.Add(CreateCheckBoxCell(
                    !((isViewerOwner) || newUser.allowOwner), newUser.allowPrint || isViewerOwner));

                // AllowSign
                row.Cells.Add(CreateCheckBoxCell(
                    !((isViewerOwner) || newUser.allowOwner), newUser.allowSign || isViewerOwner));

                // AllowOwner
                row.Cells.Add(CreateCheckBoxCell(
                    !(isViewerOwner), newUser.allowOwner || isViewerOwner));

                return row;
            }

            /// <summary>
            /// Get the current RightsManagementPermissions enum value that the
            /// given column represents.
            /// </summary>
            /// <param name="column">Column to represent.</param>
            /// <returns>RightsManagementPermission of the column, otherwise
            /// RightsManagementPermissions.AllowNothing.</returns>
            private static RightsManagementPermissions GetPermissionFromColumn(RightsTableColumn column)
            {
                RightsManagementPermissions permission = RightsManagementPermissions.AllowNothing;

                switch (column)
                {
                    case RightsTableColumn.AllowView:
                        permission = RightsManagementPermissions.AllowView;
                        break;
                    case RightsTableColumn.AllowCopy:
                        permission = RightsManagementPermissions.AllowCopy;
                        break;
                    case RightsTableColumn.AllowPrint:
                        permission = RightsManagementPermissions.AllowPrint;
                        break;
                    case RightsTableColumn.AllowSign:
                        permission = RightsManagementPermissions.AllowSign;
                        break;
                    case RightsTableColumn.AllowOwner:
                        permission = RightsManagementPermissions.AllowOwner;
                        break;
                }
                return permission;
            }

            /// <summary>
            /// Initialize the UI
            /// </summary>
            private void InitializeRightsTableUIComponents()
            {
                // Add the column headers.
                Columns.Add(
                    CreateColumnHeader("Name", SR.Get(SRID.RMPublishingUserHeader), typeof(string), 150));
                Columns.Add(
                    CreateColumnHeader("AllowView", SR.Get(SRID.RMPublishingReadHeader), typeof(bool), -1));
                Columns.Add(
                    CreateColumnHeader("AllowCopy", SR.Get(SRID.RMPublishingCopyHeader), typeof(bool), -1));
                Columns.Add(
                    CreateColumnHeader("AllowPrint", SR.Get(SRID.RMPublishingPrintHeader), typeof(bool), -1));
                Columns.Add(
                    CreateColumnHeader("AllowSign", SR.Get(SRID.RMPublishingSignHeader), typeof(bool), -1));
                Columns.Add(
                    CreateColumnHeader("AllowOwner", SR.Get(SRID.RMPublishingOwnerHeader), typeof(bool), -1));

                // Set the TextBox cell style
                _textCellStyle = new DataGridViewCellStyle();
                _textCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
                _textCellStyle.NullValue = string.Empty;
                _textCellStyle.WrapMode = DataGridViewTriState.False;

                // Set the CheckBox cell style
                _boolCellStyle = new DataGridViewCellStyle();
                _boolCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                _boolCellStyle.NullValue = false;

                // Set the cell border style
                AdvancedCellBorderStyle.All = DataGridViewAdvancedCellBorderStyle.None;
                AdvancedCellBorderStyle.Bottom = DataGridViewAdvancedCellBorderStyle.Single;

                AdvancedColumnHeadersBorderStyle.All = DataGridViewAdvancedCellBorderStyle.None;

                BackgroundColor = DefaultCellStyle.BackColor;

                // Set the Height of the RightsTable to display 4 rows of users.
                if (RowTemplate != null)
                {
                    // Extra 2 is to account for column underline.
                    Height = this.RowTemplate.Height * 4 + this.ColumnHeadersHeight + 2;
                }
            }

            /// <summary>
            /// Handler for the CellBeginEdit.  This is used to reject edits for
            /// disabled CheckBoxes.
            /// </summary>
            /// <param name="sender"></param>
            /// <param name="e"></param>
            private void RightsTable_CellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
            {
                // Ensure the cell is valid (one of the CheckBox cells).
                if ((e != null) && (e.RowIndex > 0) && (e.RowIndex < Rows.Count) &&
                    (e.ColumnIndex > 0) && (e.ColumnIndex < Columns.Count))
                {
                    DataGridViewDisableCheckBoxCell cell = Rows[e.RowIndex].Cells[e.ColumnIndex] as DataGridViewDisableCheckBoxCell;

                    // If the cell is not enabled then cancel the edit.
                    if ((cell == null) || (!cell.Enabled))
                    {
                        e.Cancel = true;
                    }
                }
                else
                {
                    // Since a non-CheckBox cell was selected cancel the edit.
                    e.Cancel = true;
                }
            }

            /// <summary>
            /// Used to update the cells on CheckBox toggles.
            /// </summary>
            /// <param name="sender"></param>
            /// <param name="e"></param>
            private void RightsTable_CurrentCellDirtyStateChanged(object sender, EventArgs e)
            {
                // End EditMode
                EndEdit();

                // If there is no currently marked cell, then return.
                if (CurrentCell == null)
                {
                    return;
                }

                bool isCurrentRowEveryone = IsEveryone(CurrentRow);

                // If the current row contains the Everyone user, update all the rows in the table
                // as appropriate to reflect that the permissions for Everyone have been changed
                if (isCurrentRowEveryone)
                {
                    UpdateAllRowsFromEveryone(CurrentRow);
                }

                if (CurrentCell.ColumnIndex == RightsTableColumnToIndex(RightsTableColumn.AllowOwner))
                {
                    // If the AllowOwner column was selected update the row, and redraw as necessary.
                    DataGridViewDisableCheckBoxCell cell = CurrentCell as DataGridViewDisableCheckBoxCell;
                    if ((cell != null) && (cell.Enabled))
                    {
                        UpdateAllowOwner(CurrentRow);

                        if (isCurrentRowEveryone)
                        {
                            // If the Everyone user's Owner permission has been changed, the whole
                            // table needs to be invalidated
                            Invalidate();
                        }
                        else
                        {
                            // Otherwise just the current row needs to be redrawn
                            InvalidateRow(cell.RowIndex);
                        }
                    }
                }
                else if (isCurrentRowEveryone)
                {
                    // If a non-Owner permission has changed for the Everyone user, redraw the
                    // whole column containing the permission
                    InvalidateColumn(CurrentCell.ColumnIndex);
                }
                else
                {
                    // Since just the current cell was toggled, redraw it.
                    InvalidateCell(CurrentCell);
                }
            }

            /// <summary>
            /// Convert a RightsTableColumn to the column index value in the table.
            /// </summary>
            /// <param name="column">The column to find</param>
            /// <returns>An integer referring to the index of the column, 
            /// -1 otherwise</returns>
            private static int RightsTableColumnToIndex(RightsTableColumn column)
            {
                return (int)column;
            }

            /// <summary>
            /// Convert a column index into the appropriate RightsTableColumn
            /// </summary>
            /// <param name="index">Column index for which to find the enum.</param>
            /// <returns>The appropriate RightsTableColumn value, otherwise
            /// RightsTableColumn.Unknown</returns>
            private static RightsTableColumn IndexToRightsTableColumn(int index)
            {
                // Ensure the index refers to a valid enum entry
                if ((index < 0) || (index >= _rightsTableColumnCount))
                {
                    return RightsTableColumn.Unknown;
                }
                else
                {
                    // Convert the int to an enum
                    return (RightsTableColumn)Enum.ToObject(typeof(RightsTableColumn), index);
                }
            }

            /// <summary>
            /// Updates all the non-owner rows in the table to match a change to the row containing
            /// the Everyone user.
            /// </summary>
            /// <remarks>
            /// This method copies all permissions granted to Everyone to all of the users in the
            /// table. For example, if the Everyone row has the Copy permission set, all other
            /// non-owner rows will also have it checked, but the checkboxes will be disabled so
            /// that the users' permissions are consistent with the ones granted to Everyone.
            /// </remarks>
            /// <param name="everyoneRow">The row containing the Everyone user</param>
            private void UpdateAllRowsFromEveryone(DataGridViewRow everyoneRow)
            {
                if (everyoneRow == null)
                {
                    throw new ArgumentNullException("everyoneRow");
                }

                // Update all the non-owner rows from the Everyone row
                for (int rowIndex = _firstNonOwnerRow; rowIndex < Rows.Count; rowIndex++)
                {
                    DataGridViewRow currentRow = Rows[rowIndex];

                    if (currentRow != everyoneRow)
                    {
                        UpdateRowFromEveryone(everyoneRow, currentRow);
                    }
                }
            }

            /// <summary>
            /// Updates the given row with the rights granted to the Everyone row.
            /// </summary>
            /// <param name="everyoneRow">The row containing the Everyone user</param>
            /// <param name="targetRow">The target row to update</param>
            private void UpdateRowFromEveryone(DataGridViewRow everyoneRow, DataGridViewRow targetRow)
            {
                if (everyoneRow == null)
                {
                    throw new ArgumentNullException("everyoneRow");
                }

                if (targetRow == null)
                {
                    throw new ArgumentNullException("targetRow");
                }

                int ownerColumnIndex = RightsTableColumnToIndex(RightsTableColumn.AllowOwner);

                // Check if the Everyone and target users are granted the Owner permission
                bool isEveryoneOwner = (bool)everyoneRow.Cells[ownerColumnIndex].Value;
                bool isTargetOwner = (bool)targetRow.Cells[ownerColumnIndex].Value;

                if (isEveryoneOwner || isTargetOwner)
                {
                    // If either the Everyone row or the target row is an owner, then:
                    //  - disable the target row's owner permission checkbox if the permission is
                    //    granted to Everyone (so the Owner permission cannot be removed from the
                    //    target row)
                    //  - if the target row was not already an owner, check the checkbox and call
                    //    UpdateAllowOwner to grant all of the other permissions to the target row

                    DataGridViewDisableCheckBoxCell targetOwnerCell =
                        targetRow.Cells[ownerColumnIndex] as DataGridViewDisableCheckBoxCell;
                    if (targetOwnerCell != null)
                    {
                        targetOwnerCell.Enabled = !isEveryoneOwner;

                        if (!isTargetOwner)
                        {
                            targetOwnerCell.Value = true;
                            UpdateAllowOwner(targetRow);
                        }
                    }
                }
                else
                {
                    // If neither Everyone nor the target user is an owner, just copy the non-owner
                    // rights from the Everyone row to the target row

                    for (int column = RightsTableColumnToIndex(_leftModifiablePermissionColumn);
                         column < ownerColumnIndex;
                         column++)
                    {
                        // Test if Everyone has the current permission
                        bool everyoneHasPermission = false;

                        DataGridViewDisableCheckBoxCell everyoneCell =
                            everyoneRow.Cells[column] as DataGridViewDisableCheckBoxCell;
                        if (everyoneCell != null)
                        {
                            everyoneHasPermission = (bool)everyoneCell.Value;
                        }

                        DataGridViewDisableCheckBoxCell targetCell =
                            targetRow.Cells[column] as DataGridViewDisableCheckBoxCell;
                        if (targetCell != null)
                        {
                            if (everyoneHasPermission)
                            {
                                // If Everyone has the permission, check the box for the permission
                                // for the target user and disable the checkbox
                                targetCell.Value = true;
                                targetCell.Enabled = false;
                            }
                            else
                            {
                                // If Everyone doesn't have the permission, leave the box as is and
                                // enable it
                                targetCell.Enabled = true;
                            }
                        }
                    }
                }
            }

            /// <summary>
            /// Updates all the other rows in the table when the row containing the Everyone user 
            /// is removed.
            /// <remarks>
            /// For each currently checked column in the Everyone row, this enables the (previously
            /// disabled) checkboxes in this column in every other row while leaving them checked.
            /// For example, if the Everyone row has the Copy permission set, all other non-owner
            /// rows will also have it checked, but the checkboxes will be disabled so that the
            /// users' permissions are consistent with the ones granted to Everyone.  When the
            /// Everyone row is removed, all the other users will still have the Copy permission
            /// checkbox checked, but the checkboxes will now be enabled so they can be unchecked.
            /// </remarks>
            /// <param name="everyoneRow">The row containing the Everyone user</param>
            private void UpdateAllRowsOnEveryoneRemoval(DataGridViewRow everyoneRow)
            {
                if (everyoneRow == null)
                {
                    throw new ArgumentNullException("everyoneRow");
                }

                // Go through all the columns in the Everyone row
                for (int column = RightsTableColumnToIndex(_leftModifiablePermissionColumn); column < _rightsTableColumnCount; column++)
                {
                    DataGridViewDisableCheckBoxCell cell =
                        everyoneRow.Cells[column] as DataGridViewDisableCheckBoxCell;
                    if (cell != null)
                    {
                        // If the checkbox in the current column is checked (i.e. the permission it
                        // represents was granted to the Everyone user), uncheck it. This will
                        // enable the checkboxes in all the other rows.
                        if ((bool)cell.Value)
                        {
                            cell.Value = false;
                        }
                    }
                }

                // Update all the other rows from this one to enable the checkboxes as necessary
                UpdateAllRowsFromEveryone(everyoneRow);
            }

            /// <summary>
            /// Updates the CheckBoxes on a row.  Used when AllowOwner has been toggled,
            /// or a row needs to be reset when AllowOwner permission may have been set
            /// on the row.
            /// </summary>
            /// <param name="row">The row to update.</param>
            private void UpdateAllowOwner(DataGridViewRow row)
            {
                if (row == null)
                {
                    throw new ArgumentNullException("row");
                }

                DataGridViewDisableCheckBoxCell cell =
                    row.Cells[RightsTableColumnToIndex(RightsTableColumn.AllowOwner)]
                    as DataGridViewDisableCheckBoxCell;
                if (cell == null)
                {
                    return;
                }

                // Get the AllowOwner value.
                bool allowOwner = (bool)cell.Value;

                int rowIndex = row.Index;

                // Leave the AllowOwner checkbox disabled if it is already disabled for some other
                // reason (i.e. AllowOwner is checked in the Everyone row). Otherwise only disable
                // AllowOwner CheckBox on the first row.
                cell.Enabled = (cell.Enabled) && (rowIndex != 0);

                // See if there is an Everyone row from which we should determine if checkboxes
                // should be enabled. If a permission is set in the Everyone row, we need to ensure
                // that we don't enable the corresponding checkbox in this row.
                DataGridViewRow everyoneRow = null;
                if (_everyoneUserPresent && (rowIndex != _everyoneRowIndex))
                {
                    everyoneRow = Rows[_everyoneRowIndex];
                }

                // Loop through remaining columns (from the first modifiable column to end).
                for (int i = RightsTableColumnToIndex(_leftModifiablePermissionColumn); i < Columns.Count - 1; i++)
                {
                    // Disable the current cell if AllowOwner is set
                    bool enableCell = !allowOwner;

                    // Determine whether this column is checked in the Everyone row. If so, the
                    // cell should be disabled.
                    if (enableCell && (everyoneRow != null))
                    {
                        DataGridViewDisableCheckBoxCell everyoneCell =
                            everyoneRow.Cells[i] as DataGridViewDisableCheckBoxCell;
                        if (everyoneCell != null)
                        {
                            enableCell = !((bool)everyoneCell.Value);
                        }
                    }

                    // Set the cell's checkbox to checked and gray it out if necessary
                    cell = row.Cells[i] as DataGridViewDisableCheckBoxCell;
                    if (cell != null)
                    {
                        cell.Value = true;
                        cell.Enabled = enableCell;
                    }
                }
            }

            #endregion Private Methods

            //------------------------------------------------------
            //  Private Fields
            //------------------------------------------------------
            #region Private Fields

            /// <summary>
            /// A value referring to the number of valid entries in the RightsTableColumn enum.
            /// </summary>
            private const int _rightsTableColumnCount = 6;

            /// <summary>
            /// Stores the default cell style for TextBox entries (username)
            /// </summary>
            private DataGridViewCellStyle _textCellStyle;

            /// <summary>
            /// Stores the default cell style for Boolean entries (permissions)
            /// </summary>
            private DataGridViewCellStyle _boolCellStyle;

            /// <summary>
            /// The minimum column width for the permission columns.
            /// </summary>
            private const int _minimumBoolColumnWidth = 20;

            /// <summary>
            /// A reference to the leftmost modifiable RightsTableColumn
            /// </summary>
            private const RightsTableColumn _leftModifiablePermissionColumn = RightsTableColumn.AllowCopy;

            /// <summary>
            /// The index of the first row in the table that is not always an owner.
            /// </summary>
            private const int _firstNonOwnerRow = 1;

            /// <summary>
            /// Indicates whether the Everyone user is contained in the table.
            /// </summary>
            private bool _everyoneUserPresent = false;

            /// <summary>
            /// The row index of the Everyone user.
            /// </summary>
            private int _everyoneRowIndex = -1;

            #endregion

            //------------------------------------------------------
            //  Public Properties
            //------------------------------------------------------
            #region Public Properties

            /// <summary>
            /// Indicates whether the anyone user is contained in the table.
            /// </summary>
            /// 
            public bool AnyoneUserPresent 
            {
                get
                {
                    return _everyoneUserPresent;
                }
            }

            #endregion

            //------------------------------------------------------
            //  RightsTableColumn (enum)
            //------------------------------------------------------
            /// <summary>
            /// Current order of RightsTable columns
            /// </summary>
            private enum RightsTableColumn : int
            {
                /// <summary>
                /// Unknown value
                /// </summary>
                Unknown = -1,
                /// <summary>
                /// Username
                /// </summary>
                User = 0,
                /// <summary>
                /// AllowView
                /// </summary>
                AllowView = 1,
                /// <summary>
                /// AllowCopy
                /// </summary>
                AllowCopy = 2,
                /// <summary>
                /// AllowPrint
                /// </summary>
                AllowPrint = 3,
                /// <summary>
                /// AllowSign
                /// </summary>
                AllowSign = 4,
                /// <summary>
                /// AllowOwner
                /// </summary>
                AllowOwner = 5,
            }

            //------------------------------------------------------
            //  DataGridViewDisableCheckBoxCell (nested class)
            //------------------------------------------------------
            /// <summary>
            /// This class is used to represent a DataGridViewCheckBoxCell that also stores
            /// an Enabled state.  This code is based on an MSDN2 example for
            /// DataGridViewDisableButtonCell, but modified for CheckBoxes.
            /// </summary>
            public class DataGridViewDisableCheckBoxCell : DataGridViewCheckBoxCell
            {

                //------------------------------------------------------
                //  Constructors
                //------------------------------------------------------
                /// <summary>
                /// Default constructor initialized with unchecked and enabled CheckBox
                /// </summary>
                public DataGridViewDisableCheckBoxCell()
                    : base(false)
                {
                    Enabled = true;
                }

                //------------------------------------------------------
                //  Public Properties
                //------------------------------------------------------
                /// <summary>
                /// Enabled Property, controls the enabled state of the CheckBox.
                /// </summary>
                public bool Enabled
                {
                    get
                    {
                        return _enabledValue;
                    }
                    set
                    {
                        _enabledValue = value;
                    }
                }

                //------------------------------------------------------
                //  Public Methods
                //------------------------------------------------------
                /// <summary>
                /// Override the Clone method so that the Enabled property is copied.
                /// </summary>
                /// <returns></returns>
                public override object Clone()
                {
                    DataGridViewDisableCheckBoxCell cell =
                        (DataGridViewDisableCheckBoxCell)base.Clone();
                    cell.Enabled = this.Enabled;
                    return cell;
                }

                //------------------------------------------------------
                //  Protected Methods
                //------------------------------------------------------
                
                /// <summary>
                /// Override Paint to control appearance of CheckBox with regard to the Enable
                /// state.  If Enable=false this will draw an inactive CheckBox, otherwise
                /// the default base.Paint operation is performed.
                /// </summary>
                /// <param name="graphics"></param>
                /// <param name="clipBounds"></param>
                /// <param name="cellBounds"></param>
                /// <param name="rowIndex"></param>
                /// <param name="elementState"></param>
                /// <param name="value"></param>
                /// <param name="formattedValue"></param>
                /// <param name="errorText"></param>
                /// <param name="cellStyle"></param>
                /// <param name="advancedBorderStyle"></param>
                /// <param name="paintParts"></param>
                protected override void Paint(Graphics graphics,
                    Rectangle clipBounds, Rectangle cellBounds, int rowIndex,
                    DataGridViewElementStates elementState, object value,
                    object formattedValue, string errorText,
                    DataGridViewCellStyle cellStyle,
                    DataGridViewAdvancedBorderStyle advancedBorderStyle,
                    DataGridViewPaintParts paintParts)
                {
                    // The button cell is disabled, so paint the border,  
                    // background, and disabled button for the cell.
                    if (!this._enabledValue)
                    {
                        // Draw the cell background, if specified.
                        if ((paintParts & DataGridViewPaintParts.Background) ==
                            DataGridViewPaintParts.Background)
                        {
                            SolidBrush cellBackground = new SolidBrush(
                                (this.Selected) ? cellStyle.SelectionBackColor : cellStyle.BackColor);
                            graphics.FillRectangle(cellBackground, cellBounds);
                            cellBackground.Dispose();
                        }

                        // Draw the cell borders, if specified.
                        if ((paintParts & DataGridViewPaintParts.Border) ==
                            DataGridViewPaintParts.Border)
                        {
                            PaintBorder(graphics, clipBounds, cellBounds, cellStyle,
                                advancedBorderStyle);
                        }

                        // Determine button state.
                        System.Windows.Forms.VisualStyles.CheckBoxState state = System.Windows.Forms.VisualStyles.CheckBoxState.CheckedDisabled;
                        if (!((bool)this.Value))
                        {
                            state = System.Windows.Forms.VisualStyles.CheckBoxState.UncheckedDisabled;
                        }

                        // Calculate the area in which to draw the button.
                        System.Drawing.Size boxSize = CheckBoxRenderer.GetGlyphSize(graphics, state);

                        // This will draw the button centered within the cell.
                        System.Drawing.Point p = new System.Drawing.Point(
                            cellBounds.X + (cellBounds.Width - boxSize.Width) / 2,
                            cellBounds.Y + (cellBounds.Height - boxSize.Height) / 2);

                        // Draw `the disabled button. 
                        CheckBoxRenderer.DrawCheckBox(graphics, p, state);
                    }
                    else
                    {
                        // The button cell is enabled, so let the base class 
                        // handle the painting.
                        base.Paint(graphics, clipBounds, cellBounds, rowIndex,
                            elementState, value, formattedValue, errorText,
                            cellStyle, advancedBorderStyle, paintParts);
                    }
                }

                //------------------------------------------------------
                //  Private Fields
                //------------------------------------------------------
                /// <summary>
                /// Instance value of the Enabled property.
                /// </summary>
                private bool _enabledValue;
                
            }
        }

        //------------------------------------------------------
        //
        //  RightsTableUser
        //
        //------------------------------------------------------
        /// <summary>
        /// A data structure to represent a user with rm permissions in the RightsTable
        /// </summary>
        private struct RightsTableUser
        {
            /// <summary>
            /// Constructor for a generic user, only given AllowView permission.
            /// </summary>
            /// <param name="name"></param>
            public RightsTableUser(string name)
                : this(name, true, false, false, false, false) { }

            /// <summary>
            /// Constructor to initialize RightsTableUser
            /// </summary>
            /// <param name="name">A string representing the user's name</param>
            /// <param name="allowView">AllowView permission</param>
            /// <param name="allowCopy">AllowCopy permission</param>
            /// <param name="allowPrint">AllowPrint permission</param>
            /// <param name="allowSign">AllowSign permission</param>
            /// <param name="allowOwner">AllowOwner permission</param>
            public RightsTableUser(
                string name,
                bool allowView,
                bool allowCopy,
                bool allowPrint,
                bool allowSign,
                bool allowOwner)
            {
                _name = name;
                _allowView = allowView;
                _allowCopy = allowCopy;
                _allowPrint = allowPrint;
                _allowSign = allowSign;
                _allowOwner = allowOwner;
            }

            /// <summary>
            /// A string representing the user's name
            /// </summary>
            public string Name
            {
                get
                {
                    return _name;
                }
            }

            /// <summary>
            /// AllowView permission
            /// </summary>
            public bool allowView
            {
                get
                {
                    return _allowView;
                }
            }

            /// <summary>
            /// AllowCopy permission
            /// </summary>
            public bool allowCopy
            {
                get
                {
                    return _allowCopy;
                }
            }

            /// <summary>
            /// AllowPrint permission
            /// </summary>
            public bool allowPrint
            {
                get
                {
                    return _allowPrint;
                }
            }

            /// <summary>
            /// AllowSign permission
            /// </summary>
            public bool allowSign
            {
                get
                {
                    return _allowSign;
                }
            }

            /// <summary>
            /// AllowOwner permission
            /// </summary>
            public bool allowOwner
            {
                get
                {
                    return _allowOwner;
                }
            }

            private string _name;
            private bool _allowView;
            private bool _allowCopy;
            private bool _allowPrint;
            private bool _allowSign;
            private bool _allowOwner;
        }
    }
}
