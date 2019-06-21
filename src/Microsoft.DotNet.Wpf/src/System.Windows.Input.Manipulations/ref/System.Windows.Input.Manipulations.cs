namespace System.Windows.Input.Manipulations
{
    public sealed partial class InertiaExpansionBehavior2D : System.Windows.Input.Manipulations.InertiaParameters2D
    {
        public InertiaExpansionBehavior2D() { }
        public float DesiredDeceleration { get { throw null; } set { } }
        public float DesiredExpansionX { get { throw null; } set { } }
        public float DesiredExpansionY { get { throw null; } set { } }
        public float InitialRadius { get { throw null; } set { } }
        public float InitialVelocityX { get { throw null; } set { } }
        public float InitialVelocityY { get { throw null; } set { } }
    }
    public abstract partial class InertiaParameters2D
    {
        internal InertiaParameters2D() { }
    }
    public partial class InertiaProcessor2D
    {
        public InertiaProcessor2D() { }
        public System.Windows.Input.Manipulations.InertiaExpansionBehavior2D ExpansionBehavior { get { throw null; } set { } }
        public float InitialOriginX { get { throw null; } set { } }
        public float InitialOriginY { get { throw null; } set { } }
        public bool IsRunning { get { throw null; } }
        public System.Windows.Input.Manipulations.InertiaRotationBehavior2D RotationBehavior { get { throw null; } set { } }
        public System.Windows.Input.Manipulations.InertiaTranslationBehavior2D TranslationBehavior { get { throw null; } set { } }
        public event System.EventHandler<System.Windows.Input.Manipulations.Manipulation2DCompletedEventArgs> Completed { add { } remove { } }
        public event System.EventHandler<System.Windows.Input.Manipulations.Manipulation2DDeltaEventArgs> Delta { add { } remove { } }
        public void Complete(long timestamp) { }
        public bool Process(long timestamp) { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public void SetParameters(System.Windows.Input.Manipulations.InertiaParameters2D parameters) { }
    }
    public sealed partial class InertiaRotationBehavior2D : System.Windows.Input.Manipulations.InertiaParameters2D
    {
        public InertiaRotationBehavior2D() { }
        public float DesiredDeceleration { get { throw null; } set { } }
        public float DesiredRotation { get { throw null; } set { } }
        public float InitialVelocity { get { throw null; } set { } }
    }
    public sealed partial class InertiaTranslationBehavior2D : System.Windows.Input.Manipulations.InertiaParameters2D
    {
        public InertiaTranslationBehavior2D() { }
        public float DesiredDeceleration { get { throw null; } set { } }
        public float DesiredDisplacement { get { throw null; } set { } }
        public float InitialVelocityX { get { throw null; } set { } }
        public float InitialVelocityY { get { throw null; } set { } }
    }
    public partial class Manipulation2DCompletedEventArgs : System.EventArgs
    {
        internal Manipulation2DCompletedEventArgs() { }
        public float OriginX { get { throw null; } }
        public float OriginY { get { throw null; } }
        public System.Windows.Input.Manipulations.ManipulationDelta2D Total { get { throw null; } }
        public System.Windows.Input.Manipulations.ManipulationVelocities2D Velocities { get { throw null; } }
    }
    public partial class Manipulation2DDeltaEventArgs : System.EventArgs
    {
        internal Manipulation2DDeltaEventArgs() { }
        public System.Windows.Input.Manipulations.ManipulationDelta2D Cumulative { get { throw null; } }
        public System.Windows.Input.Manipulations.ManipulationDelta2D Delta { get { throw null; } }
        public float OriginX { get { throw null; } }
        public float OriginY { get { throw null; } }
        public System.Windows.Input.Manipulations.ManipulationVelocities2D Velocities { get { throw null; } }
    }
    public partial class Manipulation2DStartedEventArgs : System.EventArgs
    {
        internal Manipulation2DStartedEventArgs() { }
        public float OriginX { get { throw null; } }
        public float OriginY { get { throw null; } }
    }
    public partial class ManipulationDelta2D
    {
        internal ManipulationDelta2D() { }
        public float ExpansionX { get { throw null; } }
        public float ExpansionY { get { throw null; } }
        public float Rotation { get { throw null; } }
        public float ScaleX { get { throw null; } }
        public float ScaleY { get { throw null; } }
        public float TranslationX { get { throw null; } }
        public float TranslationY { get { throw null; } }
    }
    public abstract partial class ManipulationParameters2D
    {
        internal ManipulationParameters2D() { }
    }
    public sealed partial class ManipulationPivot2D : System.Windows.Input.Manipulations.ManipulationParameters2D
    {
        public ManipulationPivot2D() { }
        public float Radius { get { throw null; } set { } }
        public float X { get { throw null; } set { } }
        public float Y { get { throw null; } set { } }
    }
    public partial class ManipulationProcessor2D
    {
        public ManipulationProcessor2D(System.Windows.Input.Manipulations.Manipulations2D supportedManipulations) { }
        public ManipulationProcessor2D(System.Windows.Input.Manipulations.Manipulations2D supportedManipulations, System.Windows.Input.Manipulations.ManipulationPivot2D pivot) { }
        public float MinimumScaleRotateRadius { get { throw null; } set { } }
        public System.Windows.Input.Manipulations.ManipulationPivot2D Pivot { get { throw null; } set { } }
        public System.Windows.Input.Manipulations.Manipulations2D SupportedManipulations { get { throw null; } set { } }
        public event System.EventHandler<System.Windows.Input.Manipulations.Manipulation2DCompletedEventArgs> Completed { add { } remove { } }
        public event System.EventHandler<System.Windows.Input.Manipulations.Manipulation2DDeltaEventArgs> Delta { add { } remove { } }
        public event System.EventHandler<System.Windows.Input.Manipulations.Manipulation2DStartedEventArgs> Started { add { } remove { } }
        public void CompleteManipulation(long timestamp) { }
        public void ProcessManipulators(long timestamp, System.Collections.Generic.IEnumerable<System.Windows.Input.Manipulations.Manipulator2D> manipulators) { }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public void SetParameters(System.Windows.Input.Manipulations.ManipulationParameters2D parameters) { }
    }
    [System.FlagsAttribute]
    public enum Manipulations2D
    {
        None = 0,
        TranslateX = 1,
        TranslateY = 2,
        Translate = 3,
        Scale = 4,
        Rotate = 8,
        All = 15,
    }
    public partial class ManipulationVelocities2D
    {
        internal ManipulationVelocities2D() { }
        public static readonly System.Windows.Input.Manipulations.ManipulationVelocities2D Zero;
        public float AngularVelocity { get { throw null; } }
        public float ExpansionVelocityX { get { throw null; } }
        public float ExpansionVelocityY { get { throw null; } }
        public float LinearVelocityX { get { throw null; } }
        public float LinearVelocityY { get { throw null; } }
    }
    public partial struct Manipulator2D
    {
        public Manipulator2D(int id, float x, float y) { throw null; }
        public int Id { get { throw null; } set { } }
        public float X { get { throw null; } set { } }
        public float Y { get { throw null; } set { } }
        public override bool Equals(object obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Windows.Input.Manipulations.Manipulator2D manipulator1, System.Windows.Input.Manipulations.Manipulator2D manipulator2) { throw null; }
        public static bool operator !=(System.Windows.Input.Manipulations.Manipulator2D manipulator1, System.Windows.Input.Manipulations.Manipulator2D manipulator2) { throw null; }
    }
}
