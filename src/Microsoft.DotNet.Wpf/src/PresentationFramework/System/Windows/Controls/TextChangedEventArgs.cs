// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: TextChanged event argument.
//

using System.ComponentModel;
using System.Windows.Documents;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace System.Windows.Controls
{
    #region TextChangedEvent

    /// <summary>
    /// How the undo stack caused or is affected by a text change.
    /// </summary>
    public enum UndoAction
    {
        /// <summary>
        /// This change will not affect the undo stack at all
        /// </summary>
        None = 0,

        /// <summary>
        /// This change will merge into the previous undo unit
        /// </summary>
        Merge = 1,

        /// <summary>
        /// This change is the result of a call to Undo()
        /// </summary>
        Undo = 2,

        /// <summary>
        /// This change is the result of a call to Redo()
        /// </summary>
        Redo = 3,

        /// <summary>
        /// This change will clear the undo stack
        /// </summary>
        Clear = 4,

        /// <summary>
        /// This change will create a new undo unit
        /// </summary>
        Create = 5
    }

    /// <summary>
    /// The delegate to use for handlers that receive TextChangedEventArgs
    /// </summary>
    public delegate void TextChangedEventHandler(object sender, TextChangedEventArgs e);

    /// <summary>
    /// The TextChangedEventArgs class represents a type of RoutedEventArgs that
    /// are relevant to events raised by changing editable text.
    /// </summary>
    public class TextChangedEventArgs : RoutedEventArgs
    {
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="id">event id</param>
        /// <param name="action">UndoAction</param>
        /// <param name="changes">ReadOnlyCollection</param>
        public TextChangedEventArgs(RoutedEvent id, UndoAction action, ICollection<TextChange> changes) : base()
        {
            if (id == null)
            {
                throw new ArgumentNullException("id");
            }

            if (action < UndoAction.None || action > UndoAction.Create)
            {
                throw new InvalidEnumArgumentException("action", (int)action, typeof(UndoAction));
            }

            RoutedEvent=id;
            _undoAction = action;
            _changes = changes;
        }

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="id">event id</param>
        /// <param name="action">UndoAction</param>
        public TextChangedEventArgs(RoutedEvent id, UndoAction action)
            : this(id, action, new ReadOnlyCollection<TextChange>(new List<TextChange>()))
        {
        }

        /// <summary>
        /// How the undo stack caused or is affected by this text change
        /// </summary>
        /// <value>UndoAction enum</value>
        public UndoAction UndoAction
        {
            get
            {
                return _undoAction;
            }
        }

        /// <summary>
        /// A readonly list of TextChanges, sorted by offset in order from the beginning to the end of
        /// the document, describing the dirty areas of the document and in what way(s) those
        /// areas differ from the pre-change document.  For example, if content at offset 0 was:
        ///
        /// The quick brown fox jumps over the lazy dog.
        ///
        /// and changed to:
        /// 
        /// The brown fox jumps over a dog.
        /// 
        /// Changes would contain two TextChanges with the following information:
        /// 
        /// Change 1
        /// --------
        /// Offset: 5
        /// RemovedCount: 6
        /// AddedCount: 0
        /// PropertyCount: 0
        /// 
        /// Change 2
        /// --------
        /// Offset: 26
        /// RemovedCount: 8
        /// AddedCount: 1
        /// PropertyCount: 0
        ///
        /// If no changes were made, the collection will be empty.
        /// </summary>
        public ICollection<TextChange> Changes
        {
            get
            {
                return _changes;
            }
        }

        /// <summary>
        /// The mechanism used to call the type-specific handler on the
        /// target.
        /// </summary>
        /// <param name="genericHandler">
        /// The generic handler to call in a type-specific way.
        /// </param>
        /// <param name="genericTarget">
        /// The target to call the handler on.
        /// </param>
        protected override void InvokeEventHandler(Delegate genericHandler, object genericTarget)
        {
            TextChangedEventHandler handler;

            handler = (TextChangedEventHandler)genericHandler;
            handler(genericTarget, this);
        }

        private UndoAction _undoAction;
        private readonly ICollection<TextChange> _changes;
    }

    #endregion TextChangedEvent
}
