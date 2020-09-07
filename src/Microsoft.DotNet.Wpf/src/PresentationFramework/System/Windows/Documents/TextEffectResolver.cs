// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Text Effect Setter
// 

using System.Collections.Generic;

using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

using MS.Internal.Text;

namespace System.Windows.Documents
{   
    /// <summary>
    /// Helper class to help set text effects into the Text container
    /// </summary>
    public static class TextEffectResolver
    {   
        //---------------------------
        // public static methods
        //---------------------------
        
        /// <summary>
        /// resolves text effect on a text range to a list of text effect targets.
        /// The method will walk the text and perform the following task:
        /// 1) For each continous block of text, create a text effect targeting the scoping element
        /// 2) For the text effect created, calculate the starting cp index and cp count for the effect
        /// 
        /// The method will create freezable copy of the TextEffect passed in and fill in 
        /// CharacterIndex and Count for the range.
        /// </summary>
        /// <param name="startPosition">starting text pointer</param>
        /// <param name="endPosition">end text pointer</param>
        /// <param name="effect">effect that is apply on the text</param>
        public static TextEffectTarget[] Resolve(
            TextPointer             startPosition, 
            TextPointer             endPosition,
            TextEffect              effect
            )
        {
            if (effect == null)
                throw new ArgumentNullException("effect");

            ValidationHelper.VerifyPositionPair(startPosition, endPosition);            
            TextPointer   effectStart   = new TextPointer(startPosition);            

            // move to the first character symbol at or after Start position
            MoveToFirstCharacterSymbol(effectStart);

            TextEffect effectCopy;
            List<TextEffectTarget> list = new List<TextEffectTarget>();

            // we will now traverse the TOM and resolve text effects to the immediate parent 
            // of the characters. We are effectively applying the text effect onto 
            // block of continous text.
            while (effectStart.CompareTo(endPosition) < 0)
            {   
                // create a copy of the text effect 
                // so that we can set the CharacterIndex and Count
                effectCopy                 = effect.Clone();

                // create a position
                TextPointer continuousTextEnd = new TextPointer(effectStart);
                
                // move the position to the end of the continuous text block
                MoveToFirstNonCharacterSymbol(continuousTextEnd, endPosition);

                // make sure we are not out of the range
                continuousTextEnd = (TextPointer)TextPointerBase.Min(continuousTextEnd, endPosition);

                // set the character index to be the distance from the Start 
                // of this text block to the Start of the text container
                effectCopy.PositionStart = effectStart.TextContainer.Start.GetOffsetToPosition(effectStart);

                // count is the distance from the text block start to end
                effectCopy.PositionCount = effectStart.GetOffsetToPosition(continuousTextEnd);

                list.Add(
                    new TextEffectTarget(
                        effectStart.Parent, 
                        effectCopy
                        )
                 );
                
                // move the effectStart to the beginning of the next text block.
                effectStart = continuousTextEnd;
                MoveToFirstCharacterSymbol(effectStart);
            }

            return list.ToArray();
        }


        //---------------------------
        // Private static methods
        //---------------------------
        
        // move to the first character symbol
        private static void MoveToFirstCharacterSymbol(TextPointer navigator)
        {
            while (navigator.GetPointerContext(LogicalDirection.Forward) != TextPointerContext.Text
                && navigator.MoveToNextContextPosition(LogicalDirection.Forward)) ;
        }

        // move to the first non-character symbol, but not pass beyond the limit
        private static void MoveToFirstNonCharacterSymbol(
            TextPointer navigator,   // navigator to move
            TextPointer  stopHint     // don't move further if we already pass beyond this point
            )         
        {
            while (navigator.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.Text 
                && navigator.CompareTo(stopHint) < 0
                && navigator.MoveToNextContextPosition(LogicalDirection.Forward)) ;
        }
    }

    /// <summary>
    /// result from TextEffectSetter which contains the TextEffect created and the DependencyObject 
    /// to which the TextEffect should be set.
    /// </summary>
    public class TextEffectTarget
    {
        private DependencyObject   _element;
        private TextEffect         _effect;

        internal TextEffectTarget(
            DependencyObject element,          
            TextEffect       effect
            )
        {
            if (element == null)
                throw new ArgumentNullException("element");

            if (effect == null)
                throw new ArgumentNullException("effect");
            
            _element = element;
            _effect  = effect;
        }

        /// <summary>
        /// The DependencyObject that the TextEffect is targetting.
        /// </summary>
        public DependencyObject Element
        {
            get { return _element; }
        }
            
        /// <summary>
        /// The TextEffect
        /// </summary>
        public TextEffect TextEffect
        {
            get { return _effect; }
        }

        /// <summary>
        /// Enable the TextEffect on the target. If the texteffect is 
        /// already enabled, this will be a no-op.
        /// </summary>
        public void Enable()
        {
            TextEffectCollection textEffects = DynamicPropertyReader.GetTextEffects(_element);
            if (textEffects == null)
            {
                textEffects = new TextEffectCollection();
                        
                // use it as reference to avoid creating a copy (Freezable pattern)
                _element.SetValue(TextElement.TextEffectsProperty, textEffects);
            }

 
            // check whether this instance is already enabled
            for (int i = 0; i < textEffects.Count; i++)
            {
                if (textEffects[i] == _effect)
                    return; // no-op
            }

            // use this as reference. 
            textEffects.Add(_effect);                    
        }

        /// <summary>
        /// Disable TextEffect on the target. If the texteffect is 
        /// already disabled, this will be a no-op.
        /// </summary>         
        public void Disable()
        {
            TextEffectCollection textEffects = DynamicPropertyReader.GetTextEffects(_element);

            if (textEffects != null)
            {
                for (int i = 0; i < textEffects.Count; i++)
                {
                    if (textEffects[i] == _effect)
                    {
                        // remove the exact instance of the effect from the collection
                        textEffects.RemoveAt(i);
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Return whether the text effect is enabled on the target element
        /// </summary>        
        public bool IsEnabled 
        {
            get 
            {
                TextEffectCollection textEffects = DynamicPropertyReader.GetTextEffects(_element);
                if (textEffects != null)
                {
                    for (int i = 0; i < textEffects.Count; i++)
                    {
                        if (textEffects[i] == _effect)
                        {
                           return true;
                        }
                    }
                }

                return false;
            }
        }
    }
}

