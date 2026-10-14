// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Reflection;
using System.Windows.Input.StylusWisp;
using System.Windows.Media;

namespace System.Windows.Input;

public sealed class WispStylusTouchDeviceTests
{
    [Fact]
    public void StoredItems_ClearNonemptyQueue_InvalidatesSnapshot()
    {
        WispLogic.StagingAreaInputItemList queue = new();
        StagingAreaInputItem item = new(false);
        queue.AddItem(item);
        StagingAreaInputItem[] snapshot = queue.ToArray();
        long version = queue.Version;

        queue.Clear();

        Assert.Empty(queue);
        Assert.Same(item, Assert.Single(snapshot));
        Assert.NotEqual(version, queue.Version);
    }

    [Fact]
    public void StoredItems_ClearDetachedQueue_PreservesReplayRevision()
    {
        WispLogic.StagingAreaInputItemList queue = new();
        queue.AddItem(new StagingAreaInputItem(false));
        queue.Clear();
        long version = queue.Version;

        queue.Clear();

        Assert.Equal(version, queue.Version);
    }

    [WpfFact]
    public void ManipulationCancelledDuringTouchUp_DefersAndRetainsItems()
    {
        using TestStylusDevice stylus = new();
        WispStylusTouchDevice touch = stylus.TouchDevice;
        StagingAreaInputItem item = new(false);
        touch.BeginContact();
        touch.StoredStagingAreaItems.AddItem(item);
        touch.BeginTouchUp();

        ((IManipulator)touch).ManipulationEnded(cancel: true);

        Assert.True(touch.DeferMousePromotion);
        Assert.Same(item, Assert.Single(touch.StoredStagingAreaItems));
        touch.EndTouchUp(touch.ContactGeneration);
        Assert.False(touch.DeferMousePromotion);
    }

    [WpfFact]
    public void BeginContact_AdvancesGenerationWithoutActivationOrQueuedInput()
    {
        using TestStylusDevice stylus = new();
        WispStylusTouchDevice touch = stylus.TouchDevice;
        touch.BeginContact();
        long oldGeneration = touch.ContactGeneration;
        touch.BeginTouchUp();
        ((IManipulator)touch).ManipulationEnded(cancel: true);

        touch.BeginContact();
        long newGeneration = touch.ContactGeneration;

        Assert.NotEqual(oldGeneration, newGeneration);
        Assert.False(touch.DeferMousePromotion);
        touch.BeginTouchUp();
        ((IManipulator)touch).ManipulationEnded(cancel: true);
        touch.EndTouchUp(oldGeneration);
        Assert.True(touch.DeferMousePromotion);
        touch.EndTouchUp(newGeneration);
        Assert.False(touch.DeferMousePromotion);
    }

    [WpfFact]
    public void ManipulationCancelledAtDeactivation_DefersUntilTouchUpScopeEnds()
    {
        using TestStylusDevice stylus = new();
        WispStylusTouchDevice touch = stylus.TouchDevice;
        touch.BeginContact();
        touch.OnActivate();
        touch.StoredStagingAreaItems.AddItem(new StagingAreaInputItem(false));
        touch.BeginTouchUp();
        EventHandler cancel = (_, _) => ((IManipulator)touch).ManipulationEnded(cancel: true);
        touch.Deactivated += cancel;
        try
        {
            StagingAreaInputItem[] snapshot = touch.StoredStagingAreaItems.ToArray();
            touch.StoredStagingAreaItems.Clear();
            long detachedVersion = touch.StoredStagingAreaItems.Version;

            touch.OnDeactivate();

            Assert.True(touch.DeferMousePromotion);
            Assert.Single(snapshot);
            Assert.Empty(touch.StoredStagingAreaItems);
            Assert.Equal(detachedVersion, touch.StoredStagingAreaItems.Version);
            touch.EndTouchUp(touch.ContactGeneration);
            Assert.False(touch.DeferMousePromotion);
        }
        finally
        {
            touch.Deactivated -= cancel;
            if (touch.IsActive)
            {
                touch.OnDeactivate();
            }
            touch.EndTouchUp(touch.ContactGeneration);
        }
    }

    [WpfFact]
    public void HandledPreviewStylusUp_Deactivation_DefersCancellation()
    {
        using ReplayStylusDevice stylus = new();
        WispStylusTouchDevice touch = stylus.TouchDevice;
        WispLogic logic = StylusLogic.GetCurrentStylusLogicAs<WispLogic>();
        touch.BeginContact();
        touch.OnActivate();
        bool deferredDuringDeactivation = false;
        EventHandler cancel = (_, _) =>
        {
            ((IManipulator)touch).ManipulationEnded(cancel: true);
            deferredDuringDeactivation = touch.DeferMousePromotion;
        };
        touch.Deactivated += cancel;
        StylusEventArgs input = new(stylus.StylusDevice, 0)
        {
            RoutedEvent = Stylus.PreviewStylusUpEvent,
            Handled = true
        };
        StagingAreaInputItem item = new(false);
        item.Reset(input, null);
        ProcessInputEventArgs args = new();
        args.Reset(item, InputManager.Current);
        try
        {
            typeof(WispLogic).GetMethod("PromotePreviewToMain",
                BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(logic, new object[] { args });

            Assert.True(deferredDuringDeactivation);
            Assert.False(touch.IsActive);
            Assert.False(touch.DeferMousePromotion);
            Assert.Equal(0, StylusTouchDeviceBase.ActiveDeviceCount);
            Assert.Null(logic.CurrentMousePromotionStylusDevice);
            Assert.True(input.Handled);
        }
        finally
        {
            touch.Deactivated -= cancel;
            if (touch.IsActive)
            {
                touch.OnDeactivate();
            }
        }
    }

    [WpfFact]
    public void SuccessfulManipulation_DiscardsQueueWithoutDeferredReplay()
    {
        using TestStylusDevice stylus = new();
        WispStylusTouchDevice touch = stylus.TouchDevice;
        touch.BeginContact();
        touch.StoredStagingAreaItems.AddItem(new StagingAreaInputItem(false));
        touch.BeginTouchUp();

        ((IManipulator)touch).ManipulationEnded(cancel: false);

        Assert.False(touch.DeferMousePromotion);
        Assert.False(touch.PromotingToOther);
        Assert.Empty(touch.StoredStagingAreaItems);
        touch.EndTouchUp(touch.ContactGeneration);
    }

    [WpfFact]
    public void PrimaryDeactivates_SecondaryContactDoesNotInheritMousePromotion()
    {
        using TestStylusDevice first = new(-1);
        using TestStylusDevice second = new(-2);
        WispStylusTouchDevice primary = first.TouchDevice;
        WispStylusTouchDevice secondary = second.TouchDevice;
        WispLogic logic = StylusLogic.GetCurrentStylusLogicAs<WispLogic>();
        primary.BeginContact();
        secondary.BeginContact();
        try
        {
            primary.OnActivate();
            secondary.OnActivate();
            Assert.Same(first, logic.CurrentMousePromotionStylusDevice);
            Assert.True(primary.IsPrimary);
            Assert.False(secondary.IsPrimary);

            primary.OnDeactivate();

            Assert.Equal(1, StylusTouchDeviceBase.ActiveDeviceCount);
            Assert.NotNull(logic.CurrentMousePromotionStylusDevice);
            Assert.NotSame(first, logic.CurrentMousePromotionStylusDevice);
            Assert.NotSame(second, logic.CurrentMousePromotionStylusDevice);
            Assert.False(secondary.IsPrimary);
        }
        finally
        {
            if (primary.IsActive)
            {
                primary.OnDeactivate();
            }
            if (secondary.IsActive)
            {
                secondary.OnDeactivate();
            }
        }
        Assert.Null(logic.CurrentMousePromotionStylusDevice);
        Assert.Equal(0, StylusTouchDeviceBase.ActiveDeviceCount);
    }

    [WpfTheory]
    [InlineData(true)]
    [InlineData(false)]
    public void CompletedReplay_Cancellation_ReleasesOnlyItsOwnedButton(bool left)
    {
        using ReplayStylusDevice stylus = new();
        using TestStylusDevice other = new(-2);
        using CompletedReplayScope replay = new(stylus);
        replay.Press(left);
        Assert.True(replay.Logic.UpdateMouseButtonState(Press(!left), other));

        replay.Logic.CancelCompletedTouchPromotion(stylus);

        Assert.Equal(MouseButtonState.Released, replay.Logic.GetMouseLeftOrRightButtonState(left));
        Assert.Equal(MouseButtonState.Pressed, replay.Logic.GetMouseLeftOrRightButtonState(!left));
        Assert.False(replay.Logic.CanPromoteCompletedTouch(stylus));
    }

    [WpfTheory]
    [InlineData(true)]
    [InlineData(false)]
    public void CompletedReplay_LaterDuplicatePress_DoesNotReleaseNewerButtonState(bool left)
    {
        using ReplayStylusDevice stylus = new();
        using TestStylusDevice other = new(-2);
        using CompletedReplayScope replay = new(stylus);
        replay.Press(left);

        Assert.False(replay.Logic.UpdateMouseButtonState(Press(left), other));
        Assert.False(replay.Logic.CanPromoteCompletedTouch(stylus));
        replay.Logic.CancelCompletedTouchPromotion(stylus);

        Assert.Equal(MouseButtonState.Pressed, replay.Logic.GetMouseLeftOrRightButtonState(left));
    }

    [WpfTheory]
    [InlineData(true, false, false)]
    [InlineData(false, false, false)]
    [InlineData(true, true, false)]
    [InlineData(false, true, false)]
    [InlineData(true, false, true)]
    [InlineData(false, false, true)]
    public void CompletedReplay_RawMouseAttribution_InvalidatesOnlyUnmarkedMouseInput(
        bool left, bool promotedSignature, bool stylusDevice)
    {
        using ReplayStylusDevice stylus = new();
        using CompletedReplayScope replay = new(stylus);
        replay.Press(left);

        replay.ProcessMouseReport(Press(left), promotedSignature, stylusDevice);

        Assert.Equal(promotedSignature || stylusDevice, replay.Logic.CanPromoteCompletedTouch(stylus));
        replay.Logic.CancelCompletedTouchPromotion(stylus);
        Assert.Equal(promotedSignature || stylusDevice ? MouseButtonState.Released : MouseButtonState.Pressed,
            replay.Logic.GetMouseLeftOrRightButtonState(left));
    }

    [WpfTheory]
    [InlineData(true)]
    [InlineData(false)]
    public void CompletedReplay_RawMouseOnOtherButton_PreservesReceipt(bool left)
    {
        using ReplayStylusDevice stylus = new();
        using CompletedReplayScope replay = new(stylus);
        replay.Press(left);

        replay.ProcessMouseReport(Press(!left));

        Assert.True(replay.Logic.CanPromoteCompletedTouch(stylus));
        replay.Logic.CancelCompletedTouchPromotion(stylus);
        Assert.Equal(MouseButtonState.Released, replay.Logic.GetMouseLeftOrRightButtonState(left));
    }

    [WpfTheory]
    [InlineData(true, false)]
    [InlineData(false, false)]
    [InlineData(true, true)]
    [InlineData(false, true)]
    public void CompletedReplay_RawMouseRelease_AllowsNextTouch(bool left, bool cancelBeforeRelease)
    {
        using ReplayStylusDevice stylus = new();
        using ReplayStylusDevice next = new(-2);
        using CompletedReplayScope replay = new(stylus);
        replay.Press(left);

        replay.ProcessMouseReport(Press(left));
        if (cancelBeforeRelease)
        {
            replay.EndReplay();
        }
        replay.ProcessMouseReport(left ? RawMouseActions.Button1Release : RawMouseActions.Button2Release);
        replay.Logic.CancelCompletedTouchPromotion(stylus);

        Assert.Equal(MouseButtonState.Released, replay.Logic.GetMouseLeftOrRightButtonState(left));
        Assert.False(stylus.SentMouseDown);
        if (!cancelBeforeRelease)
        {
            Assert.False(replay.Logic.CanPromoteCompletedTouch(stylus));
        }
        Assert.True(replay.Logic.UpdateMouseButtonState(Press(left), next));
        Assert.Equal(RawMouseActions.None, stylus.GetMouseActionsFromStylusEventAndPlaybackCachedDown(
            Stylus.StylusUpEvent, new StylusEventArgs(stylus.StylusDevice, 0)));
        Assert.Equal(MouseButtonState.Pressed, replay.Logic.GetMouseLeftOrRightButtonState(left));
        Assert.True(replay.Logic.UpdateMouseButtonState(
            left ? RawMouseActions.Button1Release : RawMouseActions.Button2Release, next));
    }

    [WpfTheory]
    [InlineData(true)]
    [InlineData(false)]
    public void CompletedReplay_RawMouseHeldAcrossCleanup_PreservesPhysicalPress(bool left)
    {
        using ReplayStylusDevice stylus = new();
        using TestStylusDevice other = new(-2);
        using CompletedReplayScope replay = new(stylus);
        replay.Press(left);

        replay.ProcessMouseReport(Press(left));
        Assert.False(replay.Logic.UpdateMouseButtonState(Press(left), other));
        replay.Logic.CancelCompletedTouchPromotion(stylus);

        Assert.Equal(MouseButtonState.Pressed, replay.Logic.GetMouseLeftOrRightButtonState(left));
        Assert.False(stylus.SentMouseDown);
        Assert.False(replay.Logic.CanPromoteCompletedTouch(stylus));
        replay.ProcessMouseReport(left ? RawMouseActions.Button1Release : RawMouseActions.Button2Release);
        Assert.Equal(MouseButtonState.Released, replay.Logic.GetMouseLeftOrRightButtonState(left));
    }

    [WpfFact]
    public void CompletedReplay_RawMouseTakesOverNestedButtons_ReleasesAfterBothReplaysEnd()
    {
        using ReplayStylusDevice outerDevice = new(-1);
        using ReplayStylusDevice innerDevice = new(-2);
        using CompletedReplayScope outer = new(outerDevice);
        outer.Press(left: true);
        using CompletedReplayScope inner = new(innerDevice);
        inner.Press(left: false);

        inner.ProcessMouseReport(RawMouseActions.Button1Press | RawMouseActions.Button2Press);
        inner.EndReplay();
        outer.EndReplay();

        Assert.False(outerDevice.SentMouseDown);
        Assert.False(innerDevice.SentMouseDown);
        Assert.Equal(MouseButtonState.Pressed, outer.Logic.GetMouseLeftOrRightButtonState(true));
        Assert.Equal(MouseButtonState.Pressed, outer.Logic.GetMouseLeftOrRightButtonState(false));
        outer.ProcessMouseReport(RawMouseActions.Button1Release | RawMouseActions.Button2Release);
        Assert.Equal(MouseButtonState.Released, outer.Logic.GetMouseLeftOrRightButtonState(true));
        Assert.Equal(MouseButtonState.Released, outer.Logic.GetMouseLeftOrRightButtonState(false));
    }

    [WpfTheory]
    [InlineData(true)]
    [InlineData(false)]
    public void CompletedReplay_RawMouseReleaseWithoutPress_RetiresStaleDevice(bool left)
    {
        using ReplayStylusDevice stylus = new();
        using CompletedReplayScope replay = new(stylus);
        replay.Press(left);

        replay.ProcessMouseReport(left ? RawMouseActions.Button1Release : RawMouseActions.Button2Release);

        Assert.False(stylus.SentMouseDown);
        Assert.Equal(MouseButtonState.Released, replay.Logic.GetMouseLeftOrRightButtonState(left));
        Assert.False(replay.Logic.CanPromoteCompletedTouch(stylus));
    }

    [WpfTheory]
    [InlineData(true)]
    [InlineData(false)]
    public void RawMouseWithoutCompletedPress_DoesNotChangeStylusButtonState(bool left)
    {
        using ReplayStylusDevice stylus = new();
        using CompletedReplayScope replay = new(stylus);
        Assert.True(replay.Logic.UpdateMouseButtonState(Press(left), stylus));

        replay.ProcessMouseReport(left ? RawMouseActions.Button1Release : RawMouseActions.Button2Release);

        Assert.Equal(MouseButtonState.Pressed, replay.Logic.GetMouseLeftOrRightButtonState(left));
    }

    [WpfTheory]
    [InlineData(true)]
    [InlineData(false)]
    public void CompletedReplay_NewContact_PreservesNewQueueAndButtonState(bool left)
    {
        using ReplayStylusDevice stylus = new();
        using CompletedReplayScope replay = new(stylus);
        replay.Press(left);
        WispStylusTouchDevice touch = stylus.TouchDevice;

        touch.BeginContact();
        Assert.Equal(MouseButtonState.Released, replay.Logic.GetMouseLeftOrRightButtonState(left));
        StagingAreaInputItem newItem = new(false);
        touch.StoredStagingAreaItems.AddItem(newItem);
        Assert.True(replay.Logic.UpdateMouseButtonState(Press(left), stylus));
        replay.Logic.CancelCompletedTouchPromotion(stylus);

        Assert.False(replay.Logic.IsReplayingCompletedTouch(stylus));
        Assert.Same(newItem, Assert.Single(touch.StoredStagingAreaItems));
        Assert.Equal(MouseButtonState.Pressed, replay.Logic.GetMouseLeftOrRightButtonState(left));
    }

    [WpfFact]
    public void CompletedReplay_QueueChanges_StopsReplayWithoutDiscardingNewItems()
    {
        using ReplayStylusDevice stylus = new();
        using CompletedReplayScope replay = new(stylus);
        replay.Press(left: true);
        StagingAreaInputItem newItem = new(false);

        stylus.TouchDevice.StoredStagingAreaItems.AddItem(newItem);

        Assert.False(replay.Logic.CanPromoteCompletedTouch(stylus));
        replay.Logic.CancelCompletedTouchPromotion(stylus);
        Assert.Same(newItem, Assert.Single(stylus.TouchDevice.StoredStagingAreaItems));
        Assert.Equal(MouseButtonState.Released, replay.Logic.GetMouseLeftOrRightButtonState(true));
    }

    [WpfTheory]
    [InlineData(true)]
    [InlineData(false)]
    public void CompletedReplay_DeviceDisposed_RetiresPressBeforeInvalidation(bool left)
    {
        using ReplayStylusDevice stylus = new();
        using CompletedReplayScope replay = new(stylus);
        replay.Press(left);

        stylus.Dispose();

        Assert.False(stylus.IsValid);
        Assert.False(replay.Logic.CanPromoteCompletedTouch(stylus));
        Assert.Equal(MouseButtonState.Released, replay.Logic.GetMouseLeftOrRightButtonState(left));
    }

    [WpfTheory]
    [InlineData(true)]
    [InlineData(false)]
    public void CompletedReplay_TerminalUp_AcknowledgesOwnedLeftOrRightRelease(bool left)
    {
        using ReplayStylusDevice stylus = new();
        using CompletedReplayScope replay = new(stylus);
        StylusSystemGestureEventArgs gesture = new(stylus.StylusDevice, 0,
            left ? SystemGesture.Tap : SystemGesture.RightTap);
        stylus.GetMouseActionsFromStylusEventAndPlaybackCachedDown(Stylus.StylusSystemGestureEvent, gesture);
        replay.Press(left);

        RawMouseActions actions = stylus.GetMouseActionsFromStylusEventAndPlaybackCachedDown(
            Stylus.StylusUpEvent, new StylusEventArgs(stylus.StylusDevice, 0));

        Assert.Equal(left ? RawMouseActions.Button1Release : RawMouseActions.Button2Release, actions);
        Assert.Equal(MouseButtonState.Released, replay.Logic.GetMouseLeftOrRightButtonState(left));
        Assert.False(stylus.SentMouseDown);
        Assert.True(replay.Logic.CanPromoteCompletedTouch(stylus));
    }

    [WpfTheory]
    [InlineData(true)]
    [InlineData(false)]
    public void CompletedReplay_SourceCancellation_UsesActualSourceIdentity(bool left)
    {
        using ReplayStylusDevice stylus = new();
        using CompletedReplayScope replay = new(stylus);
        replay.Press(left);

        replay.CancelSource(new TestPresentationSource());

        Assert.True(replay.Logic.CanPromoteCompletedTouch(stylus));
        Assert.Equal(MouseButtonState.Pressed, replay.Logic.GetMouseLeftOrRightButtonState(left));
        replay.CancelSource(replay.Source);
        Assert.False(replay.Logic.CanPromoteCompletedTouch(stylus));
        Assert.Equal(MouseButtonState.Released, replay.Logic.GetMouseLeftOrRightButtonState(left));
        Assert.False(stylus.SentMouseDown);
    }

    [WpfFact]
    public void CompletedReplay_SourceCancelledBeforePress_RejectsLaterReceipt()
    {
        using ReplayStylusDevice stylus = new();
        using CompletedReplayScope replay = new(stylus);
        replay.TrackSource();

        replay.CancelSource(replay.Source);

        Assert.False(replay.Logic.CanPromoteCompletedTouch(stylus));
        Assert.False(replay.Logic.RecordCompletedTouchMousePress(stylus, Press(true), replay.Source));
        Assert.Equal(MouseButtonState.Released, replay.Logic.GetMouseLeftOrRightButtonState(true));
    }

    [WpfFact]
    public void CompletedReplay_NestedOperation_CancelsOnlyMatchingDevice()
    {
        using ReplayStylusDevice outerDevice = new(-1);
        using ReplayStylusDevice innerDevice = new(-2);
        using CompletedReplayScope outer = new(outerDevice);
        outer.Press(left: true);
        using (CompletedReplayScope inner = new(innerDevice))
        {
            inner.Press(left: false);

            inner.Logic.CancelCompletedTouchPromotion(outerDevice);

            Assert.Equal(MouseButtonState.Released, inner.Logic.GetMouseLeftOrRightButtonState(true));
            Assert.Equal(MouseButtonState.Pressed, inner.Logic.GetMouseLeftOrRightButtonState(false));
            Assert.False(inner.Logic.CanPromoteCompletedTouch(outerDevice));
            Assert.True(inner.Logic.CanPromoteCompletedTouch(innerDevice));
        }
        Assert.False(outer.Logic.CanPromoteCompletedTouch(outerDevice));
        Assert.Equal(MouseButtonState.Released, outer.Logic.GetMouseLeftOrRightButtonState(false));
    }

    [WpfTheory]
    [InlineData(true)]
    [InlineData(false)]
    public void CompletedReplay_NestedRelease_AcknowledgesMatchingOuterReceipt(bool left)
    {
        using ReplayStylusDevice outerDevice = new(-1);
        using ReplayStylusDevice innerDevice = new(-2);
        using CompletedReplayScope outer = new(outerDevice);
        outer.Press(left);
        using (CompletedReplayScope inner = new(innerDevice))
        {
            inner.Press(!left);

            Assert.True(inner.Logic.UpdateMouseButtonState(
                left ? RawMouseActions.Button1Release : RawMouseActions.Button2Release, outerDevice));

            Assert.True(inner.Logic.CanPromoteCompletedTouch(innerDevice));
            Assert.Equal(MouseButtonState.Released, inner.Logic.GetMouseLeftOrRightButtonState(left));
            Assert.Equal(MouseButtonState.Pressed, inner.Logic.GetMouseLeftOrRightButtonState(!left));
        }
        Assert.True(outer.Logic.CanPromoteCompletedTouch(outerDevice));
    }

    [WpfTheory]
    [InlineData(true)]
    [InlineData(false)]
    public void CompletedReplay_NestedPress_RecordsMatchingOuterSource(bool left)
    {
        using ReplayStylusDevice outerDevice = new(-1);
        using ReplayStylusDevice innerDevice = new(-2);
        using CompletedReplayScope outer = new(outerDevice);
        using CompletedReplayScope inner = new(innerDevice);
        inner.Press(!left);

        outer.Press(left);
        outer.CancelSource(outer.Source);

        Assert.Equal(MouseButtonState.Released, inner.Logic.GetMouseLeftOrRightButtonState(left));
        Assert.Equal(MouseButtonState.Pressed, inner.Logic.GetMouseLeftOrRightButtonState(!left));
        Assert.False(inner.Logic.CanPromoteCompletedTouch(outerDevice));
        Assert.True(inner.Logic.CanPromoteCompletedTouch(innerDevice));
    }

    private static RawMouseActions Press(bool left) =>
        left ? RawMouseActions.Button1Press : RawMouseActions.Button2Press;

    // Bookkeeping contacts intentionally have no valid tablet and cannot emit native input.
    private sealed class TestStylusDevice(int id = -1) : WispStylusDevice(null, "Test contact", id, false, null)
    {
        internal override TabletDevice TabletDevice => null!;
    }

    // Model a valid device's lifetime only for receipt checks; no input is promoted from this double.
    private sealed class ReplayStylusDevice(int id = -1) : WispStylusDevice(null, "Replay receipt", id, false, null)
    {
        private bool _lifetimeEnded;
        internal override TabletDevice TabletDevice => null!;
        internal override bool IsValid => !_lifetimeEnded;

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            _lifetimeEnded = true;
        }
    }

    private sealed class TestPresentationSource : PresentationSource
    {
        public override Visual RootVisual { get; set; } = null!;
        public override bool IsDisposed => false;
        protected override CompositionTarget GetCompositionTargetCore() => null!;
    }

    // Install the private receipt through its real constructor, without exposing test-only product APIs.
    private sealed class CompletedReplayScope : IDisposable
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private static readonly Type s_promotionType =
            typeof(WispLogic).GetNestedType("CompletedTouchMousePromotion", BindingFlags.NonPublic)!;
        private static readonly FieldInfo s_activePromotion =
            typeof(WispLogic).GetField("_completedTouchMousePromotion", PrivateInstance)!;
        private static readonly FieldInfo s_inputEnabled =
            typeof(WispLogic).GetField("_inputEnabled", PrivateInstance)!;
        private static readonly MethodInfo s_preProcessInput =
            typeof(WispLogic).GetMethod("PreProcessInput", PrivateInstance)!;
        private static readonly MethodInfo s_cancelSource =
            typeof(WispLogic).GetMethod("CancelCompletedTouchPromotionsForSource", PrivateInstance)!;
        private static readonly FieldInfo s_promotedMouseState =
            typeof(WispStylusDevice).GetField("_promotedMouseState", PrivateInstance)!;

        private readonly ReplayStylusDevice _stylus;
        private readonly object _promotion;
        private readonly object? _previous;
        private readonly MouseButtonState _leftBefore;
        private readonly MouseButtonState _rightBefore;
        private readonly TestPresentationSource _source = new();

        internal CompletedReplayScope(ReplayStylusDevice stylus)
        {
            _stylus = stylus;
            Logic = StylusLogic.GetCurrentStylusLogicAs<WispLogic>();
            stylus.TouchDevice.BeginContact();
            _previous = s_activePromotion.GetValue(Logic);
            _leftBefore = Logic.GetMouseLeftOrRightButtonState(true);
            _rightBefore = Logic.GetMouseLeftOrRightButtonState(false);
            _promotion = s_promotionType.GetConstructors(PrivateInstance).Single().Invoke(
                new object[] { stylus.TouchDevice, stylus.TouchDevice.StoredStagingAreaItems, false, false });
            s_promotionType.GetProperty("Previous", PrivateInstance)!.SetValue(_promotion, _previous);
            s_activePromotion.SetValue(Logic, _promotion);
            Assert.True(Logic.CanPromoteCompletedTouch(stylus));
        }

        internal WispLogic Logic { get; }
        internal PresentationSource Source => _source;

        internal void TrackSource() =>
            s_promotionType.GetProperty("MouseInputSource", PrivateInstance)!.SetValue(_promotion, _source);

        internal void CancelSource(PresentationSource source) =>
            s_cancelSource.Invoke(Logic, new object[] { source });

        internal void Press(bool left)
        {
            Assert.True(Logic.UpdateMouseButtonState(WispStylusTouchDeviceTests.Press(left), _stylus));
            s_promotedMouseState.SetValue(_stylus, MouseButtonState.Pressed);
            Assert.True(Logic.RecordCompletedTouchMousePress(_stylus, WispStylusTouchDeviceTests.Press(left), _source));
        }

        internal void ProcessMouseReport(RawMouseActions actions,
            bool promotedSignature = false, bool stylusDevice = false)
        {
            RawMouseInputReport report = new(InputMode.Foreground, 0, _source, actions, 0, 0, 0,
                promotedSignature ? new IntPtr(unchecked((int)0xFF515700)) : IntPtr.Zero);
            InputReportEventArgs input = new(stylusDevice ? _stylus.StylusDevice : null, report)
            {
                RoutedEvent = InputManager.PreviewInputReportEvent,
                Handled = true
            };
            StagingAreaInputItem item = new(false);
            item.Reset(input, null);
            PreProcessInputEventArgs args = new();
            args.Reset(item, InputManager.Current);
            object? enabled = s_inputEnabled.GetValue(Logic);
            try
            {
                s_inputEnabled.SetValue(Logic, true);
                // Handled reports isolate raw ownership invalidation from native tablet processing.
                s_preProcessInput.Invoke(Logic, new object[] { InputManager.Current, args });
            }
            finally
            {
                s_inputEnabled.SetValue(Logic, enabled);
            }
        }

        internal void EndReplay()
        {
            Logic.CancelCompletedTouchPromotion(_stylus);
            s_activePromotion.SetValue(Logic, _previous);
        }

        public void Dispose()
        {
            EndReplay();
            if (_previous is null)
            {
                Logic.UpdateMouseButtonState(_leftBefore == MouseButtonState.Pressed
                    ? RawMouseActions.Button1Press : RawMouseActions.Button1Release, _stylus);
                Logic.UpdateMouseButtonState(_rightBefore == MouseButtonState.Pressed
                    ? RawMouseActions.Button2Press : RawMouseActions.Button2Release, _stylus);
            }
        }
    }
}
