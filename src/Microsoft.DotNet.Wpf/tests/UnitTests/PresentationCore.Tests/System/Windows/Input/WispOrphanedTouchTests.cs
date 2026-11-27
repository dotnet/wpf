// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Windows.Input.StylusWisp;

namespace System.Windows.Input;

public sealed class WispOrphanedTouchTests
{
    [WpfTheory]
    [InlineData(true)]
    [InlineData(false)]
    public void OrphanedContact_ReenableBeforeUp_DoesNotRestoreAbandonedPress(bool left)
    {
        using MouseStateScope state = new();
        using TestStylusDevice contact = new(-1);
        state.Activate(contact);
        state.Press(contact, left);
        Assert.False(contact.InAir);
        Assert.True(contact.GestureWasFired);

        contact.TouchDevice.OnOrphaned();

        Assert.True(contact.TouchDevice.IsOrphaned);
        Assert.False(contact.SentMouseDown);
        Assert.Equal(MouseButtonState.Released, state.Logic.GetMouseLeftOrRightButtonState(left));
        state.Reenable(contact);
        Assert.False(contact.SentMouseDown);
        Assert.Equal(MouseButtonState.Released, state.Logic.GetMouseLeftOrRightButtonState(left));

        Assert.Equal(RawMouseActions.None, state.Up(contact));
        contact.TouchDevice.OnDeactivate();
        state.Activate(contact);
        state.Press(contact, left);
        Assert.False(contact.TouchDevice.IsOrphaned);
        state.Reenable(contact);
        Assert.Equal(MouseButtonState.Pressed, state.Logic.GetMouseLeftOrRightButtonState(left));
        Assert.Equal(Release(left), state.Up(contact));
    }

    [WpfTheory]
    [InlineData(true, true, false)]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    [InlineData(false, false, false)]
    [InlineData(true, true, true)]
    [InlineData(true, false, true)]
    [InlineData(false, true, true)]
    [InlineData(false, false, true)]
    public void OrphanedContact_ReenableAndLateUp_PreserveNewContactPress(
        bool oldLeft, bool newLeft, bool disabledUp)
    {
        using MouseStateScope state = new();
        using TestStylusDevice oldContact = new(-1);
        using TestStylusDevice newContact = new(-2);
        state.Activate(oldContact);
        state.Press(oldContact, oldLeft);
        oldContact.TouchDevice.OnOrphaned();
        state.Activate(newContact);
        state.Press(newContact, newLeft);
        Assert.Same(newContact, state.Logic.CurrentMousePromotionStylusDevice);

        // The old contact can become current again when its queued input is processed.
        state.Reenable(oldContact);

        Assert.Equal(MouseButtonState.Pressed, state.Logic.GetMouseLeftOrRightButtonState(newLeft));
        Assert.Equal(newLeft == oldLeft ? MouseButtonState.Pressed : MouseButtonState.Released,
            state.Logic.GetMouseLeftOrRightButtonState(oldLeft));
        if (disabledUp)
        {
            oldContact.AbandonPromotedMouseButton();
            oldContact.ResetStateForStylusUp();
        }
        else
        {
            Assert.Equal(RawMouseActions.None, state.Up(oldContact));
        }
        oldContact.TouchDevice.OnDeactivate();

        Assert.Same(newContact, state.Logic.CurrentMousePromotionStylusDevice);
        Assert.Equal(MouseButtonState.Pressed, state.Logic.GetMouseLeftOrRightButtonState(newLeft));
        Assert.Equal(Release(newLeft), state.Up(newContact));
        Assert.Equal(MouseButtonState.Released, state.Logic.GetMouseLeftOrRightButtonState(newLeft));
    }

    [WpfTheory]
    [InlineData(true, false)]
    [InlineData(false, false)]
    [InlineData(true, true)]
    [InlineData(false, true)]
    public void NonOrphanedStylus_Reenable_PreservesExistingStateRules(bool left, bool inAir)
    {
        using MouseStateScope state = new();
        using TestStylusDevice stylus = new(-1);
        state.Press(stylus, left);
        MouseStateScope.Set(stylus, "_fInAir", inAir);

        state.Reenable(stylus);

        Assert.Equal(inAir ? MouseButtonState.Released : MouseButtonState.Pressed,
            state.Logic.GetMouseLeftOrRightButtonState(left));
        Assert.Equal(MouseButtonState.Released, state.Logic.GetMouseLeftOrRightButtonState(!left));
    }

    [WpfTheory]
    [InlineData(true)]
    [InlineData(false)]
    public void NonOrphanedStylus_ReenableWithoutGesture_ReleasesState(bool left)
    {
        using MouseStateScope state = new();
        using TestStylusDevice stylus = new(-1);
        state.Press(stylus, left);
        MouseStateScope.Set(stylus, "_fGestureWasFired", false);

        state.Reenable(stylus);

        Assert.Equal(MouseButtonState.Released, state.Logic.GetMouseLeftOrRightButtonState(true));
        Assert.Equal(MouseButtonState.Released, state.Logic.GetMouseLeftOrRightButtonState(false));
    }

    private static RawMouseActions Press(bool left) =>
        left ? RawMouseActions.Button1Press : RawMouseActions.Button2Press;

    private static RawMouseActions Release(bool left) =>
        left ? RawMouseActions.Button1Release : RawMouseActions.Button2Release;

    // These contacts exercise bookkeeping only and have no tablet capable of native input.
    private sealed class TestStylusDevice(int id) : WispStylusDevice(null, "Orphaned contact", id, false, null)
    {
        internal override TabletDevice TabletDevice => null!;

        protected override void Dispose(bool disposing)
        {
            if (TouchDevice.IsActive)
            {
                TouchDevice.OnDeactivate();
            }
            base.Dispose(disposing);
        }
    }

    private sealed class MouseStateScope : IDisposable
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private static readonly FieldInfo s_currentStylus =
            typeof(WispLogic).GetField("_currentStylusDevice", PrivateInstance)!;
        private readonly object? _previousStylus;
        private readonly object? _previousPromotionOwner;
        private readonly MouseButtonState _leftBefore;
        private readonly MouseButtonState _rightBefore;

        internal MouseStateScope()
        {
            Logic = StylusLogic.GetCurrentStylusLogicAs<WispLogic>();
            _previousStylus = s_currentStylus.GetValue(Logic);
            _previousPromotionOwner = Logic.CurrentMousePromotionStylusDevice;
            _leftBefore = Logic.GetMouseLeftOrRightButtonState(true);
            _rightBefore = Logic.GetMouseLeftOrRightButtonState(false);
            Logic.UpdateMouseButtonState(RawMouseActions.Button1Release);
            Logic.UpdateMouseButtonState(RawMouseActions.Button2Release);
        }

        internal WispLogic Logic { get; }

        internal static void Set(TestStylusDevice stylus, string field, object value) =>
            typeof(WispStylusDevice).GetField(field, PrivateInstance)!.SetValue(stylus, value);

        internal void Activate(TestStylusDevice stylus) => stylus.TouchDevice.OnActivate();

        internal void Press(TestStylusDevice stylus, bool left)
        {
            Set(stylus, "_fInAir", false);
            Set(stylus, "_fGestureWasFired", true);
            Set(stylus, "_fLeftButtonDownTrigger", left);
            Assert.True(Logic.UpdateMouseButtonState(WispOrphanedTouchTests.Press(left)));
            Set(stylus, "_promotedMouseState", MouseButtonState.Pressed);
        }

        internal void Reenable(TestStylusDevice stylus)
        {
            s_currentStylus.SetValue(Logic, stylus);
            // A nonzero, unregistered HWND isolates re-enable from native tablet enumeration.
            Logic.OnWindowEnableChanged(new IntPtr(-1), disabled: false);
        }

        internal RawMouseActions Up(TestStylusDevice stylus) =>
            stylus.GetMouseActionsFromStylusEventAndPlaybackCachedDown(
                Stylus.StylusUpEvent, new StylusEventArgs(stylus.StylusDevice, 0));

        public void Dispose()
        {
            s_currentStylus.SetValue(Logic, _previousStylus);
            Logic.CurrentMousePromotionStylusDevice = _previousPromotionOwner;
            Logic.UpdateMouseButtonState(_leftBefore == MouseButtonState.Pressed
                ? RawMouseActions.Button1Press : RawMouseActions.Button1Release);
            Logic.UpdateMouseButtonState(_rightBefore == MouseButtonState.Pressed
                ? RawMouseActions.Button2Press : RawMouseActions.Button2Release);
        }
    }
}
