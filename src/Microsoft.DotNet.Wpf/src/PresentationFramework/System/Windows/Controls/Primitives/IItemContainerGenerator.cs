// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: IItemContainerGenerator interface
//
// Specs:       Data Styling.mht
//

using System;
using System.Windows.Markup;
using MS.Internal.Data;

namespace System.Windows.Controls.Primitives
{
    /// <summary>
    ///     Interface through which a layout element (such as a panel) marked
    ///     as an ItemsHost communicates with the ItemContainerGenerator of its
    ///     items owner.
    /// </summary>
    public interface IItemContainerGenerator
    {
        /// <summary>
        /// Return the ItemContainerGenerator appropriate for use by the given panel
        /// </summary>
        ItemContainerGenerator GetItemContainerGeneratorForPanel(Panel panel);

        /// <summary> Prepare the generator to generate, starting at the given position and direction </summary>
        /// <remarks>
        /// This method must be called before calling GenerateNext.  It returns an
        /// IDisposable object that tracks the lifetime of the generation loop.
        /// This method sets the generator's status to GeneratingContent;  when
        /// the IDisposable is disposed, the status changes to ContentReady or
        /// Error, as appropriate.
        /// </remarks>
        IDisposable StartAt(GeneratorPosition position, GeneratorDirection direction);

        /// <summary> Prepare the generator to generate, starting at the given position and direction </summary>
        /// <remarks>
        /// This method must be called before calling GenerateNext.  It returns an
        /// IDisposable object that tracks the lifetime of the generation loop.
        /// This method sets the generator's status to GeneratingContent;  when
        /// the IDisposable is disposed, the status changes to ContentReady or
        /// Error, as appropriate.
        /// </remarks>
        IDisposable StartAt(GeneratorPosition position, GeneratorDirection direction, bool allowStartAtRealizedItem);

        /// <summary> Return the container element used to display the next item. </summary>
        /// <remarks>
        /// This method must be called in the scope of the IDisposable returned by
        /// a previous call to StartAt.
        /// </remarks>
        DependencyObject GenerateNext();

        /// <summary> Return the container element used to display the next item.
        /// When the next item has not been realized, this method returns a container
        /// and sets isNewlyRealized to true.  When the next item has been realized,
        /// this method returns the exisiting container and sets isNewlyRealized to
        /// false.
        /// </summary>
        /// <remarks>
        /// This method must be called in the scope of the IDisposable returned by
        /// a previous call to StartAt.
        /// </remarks>
        DependencyObject GenerateNext(out bool isNewlyRealized);

        /// <summary>
        /// Prepare the given element to act as the ItemUI for the
        /// corresponding item.  This includes applying the ItemUI style,
        /// forwarding information from the host control (ItemTemplate, etc.),
        /// and other small adjustments.
        /// </summary>
        /// <remarks>
        /// This method must be called after the element has been added to the
        /// visual tree, so that resource references and inherited properties
        /// work correctly.
        /// </remarks>
        /// <param name="container"> The container to prepare.
        /// Normally this is the result of the previous call to GenerateNext.
        /// </param>
        void PrepareItemContainer(DependencyObject container);

        /// <summary>
        /// Remove all generated elements.
        /// </summary>
        void RemoveAll();

        /// <summary>
        /// Remove generated elements.
        /// </summary>
        /// <remarks>
        /// The position must refer to a previously generated item, i.e. its
        /// Offset must be 0.
        /// </remarks>
        void Remove(GeneratorPosition position, int count);

        /// <summary>
        /// Map an index into the items collection to a GeneratorPosition.
        /// </summary>
        GeneratorPosition GeneratorPositionFromIndex(int itemIndex);

        /// <summary>
        /// Map a GeneratorPosition to an index into the items collection.
        /// </summary>
        int IndexFromGeneratorPosition(GeneratorPosition position);
    }

    /// <summary>
    /// A user of the ItemContainerGenerator describes positions using this struct.
    /// Some examples:
    /// To start generating forward from the beginning of the item list,
    /// specify position (-1, 0) and direction Forward.
    /// To start generating backward from the end of the list,
    /// specify position (-1, 0) and direction Backward.
    /// To generate the items after the element with index k, specify
    /// position (k, 0) and direction Forward.
    /// </summary>
    public struct GeneratorPosition
    {
        /// <summary>
        /// Index, with respect to realized elements.  The special value -1
        /// refers to a fictitious element at the beginning or end of the
        /// the list.
        /// </summary>
        public int Index  { get { return _index; }  set { _index = value; } }

        /// <summary>
        /// Offset, with respect to unrealized items near the indexed element.
        /// An offset of 0 refers to the indexed element itself, an offset
        /// of 1 refers to the next (unrealized) item, and an offset of -1
        /// refers to the previous item.
        /// </summary>
        public int Offset { get { return _offset; } set { _offset = value; } }

        /// <summary> Constructor </summary>
        public GeneratorPosition(int index, int offset)
        {
            _index = index;
            _offset = offset;
        }

        /// <summary> Return a hash code </summary>
        // This is required by FxCop.
        public override int GetHashCode()
        {
            return _index.GetHashCode() + _offset.GetHashCode();
        }

        /// <summary>Returns a string representation of the GeneratorPosition</summary>
        public override string ToString()
        {
            return string.Concat("GeneratorPosition (", _index.ToString(TypeConverterHelper.InvariantEnglishUS), ",", _offset.ToString(TypeConverterHelper.InvariantEnglishUS), ")");
        }

        
        // The remaining methods are present only because they are required by FxCop.

        /// <summary> Equality test </summary>
        // This is required by FxCop.
        public override bool Equals(object o)
        {
            if (o is GeneratorPosition)
            {
                GeneratorPosition that = (GeneratorPosition)o;
                return  this._index == that._index &&
                        this._offset == that._offset;
            }
            return false;
        }

        /// <summary> Equality test </summary>
        // This is required by FxCop.
        public static bool operator==(GeneratorPosition gp1, GeneratorPosition gp2)
        {
            return  gp1._index == gp2._index &&
                    gp1._offset == gp2._offset;
        }

        /// <summary> Inequality test </summary>
        // This is required by FxCop.
        public static bool operator!=(GeneratorPosition gp1, GeneratorPosition gp2)
        {
            return  !(gp1 == gp2);
        }

        private int _index;
        private int _offset;
    }

    /// <summary>
    /// This enum is used by the ItemContainerGenerator and its client to specify
    /// the direction in which the generator produces UI.
    /// </summary>
    public enum GeneratorDirection
    {
        /// <summary> generate forward through the item collection </summary>
        Forward,

        /// <summary> generate backward through the item collection </summary>
        Backward
    }

    /// <summary>
    /// This enum is used by the ItemContainerGenerator to indicate its status.
    /// </summary>
    public enum GeneratorStatus
    {
        ///<summary>The generator has not tried to generate content</summary>
        NotStarted,
        ///<summary>The generator is generating containers</summary>
        GeneratingContainers,
        ///<summary>The generator has finished generating containers</summary>
        ContainersGenerated,
        ///<summary>The generator has finished generating containers, but encountered one or more errors</summary>
        Error
    }
}

