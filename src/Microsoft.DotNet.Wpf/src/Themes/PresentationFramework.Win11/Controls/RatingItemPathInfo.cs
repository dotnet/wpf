using System.Windows;
using System.Windows.Media;

namespace  PresentationFramework.Win11.Controls
{
    public class RatingItemPathInfo : RatingItemInfo
    {
        public RatingItemPathInfo()
        {
        }

        #region DisabledData

        public static readonly DependencyProperty DisabledDataProperty =
            DependencyProperty.Register(
                nameof(DisabledData),
                typeof(Geometry),
                typeof(RatingItemPathInfo),
                null);

        public Geometry DisabledData
        {
            get => (Geometry)GetValue(DisabledDataProperty);
            set => SetValue(DisabledDataProperty, value);
        }

        #endregion

        #region Data

        public static readonly DependencyProperty DataProperty =
            DependencyProperty.Register(
                nameof(Data),
                typeof(Geometry),
                typeof(RatingItemPathInfo),
                null);

        public Geometry Data
        {
            get => (Geometry)GetValue(DataProperty);
            set => SetValue(DataProperty, value);
        }

        #endregion

        #region PlaceholderData

        public static readonly DependencyProperty PlaceholderDataProperty =
            DependencyProperty.Register(
                nameof(PlaceholderData),
                typeof(Geometry),
                typeof(RatingItemPathInfo),
                null);

        public Geometry PlaceholderData
        {
            get => (Geometry)GetValue(PlaceholderDataProperty);
            set => SetValue(PlaceholderDataProperty, value);
        }

        #endregion

        #region PointerOverData

        public static readonly DependencyProperty PointerOverDataProperty =
            DependencyProperty.Register(
                nameof(PointerOverData),
                typeof(Geometry),
                typeof(RatingItemPathInfo),
                null);

        public Geometry PointerOverData
        {
            get => (Geometry)GetValue(PointerOverDataProperty);
            set => SetValue(PointerOverDataProperty, value);
        }

        #endregion

        #region PointerOverPlaceholderData

        public static readonly DependencyProperty PointerOverPlaceholderDataProperty =
            DependencyProperty.Register(
                nameof(PointerOverPlaceholderData),
                typeof(Geometry),
                typeof(RatingItemPathInfo),
                null);

        public Geometry PointerOverPlaceholderData
        {
            get => (Geometry)GetValue(PointerOverPlaceholderDataProperty);
            set => SetValue(PointerOverPlaceholderDataProperty, value);
        }

        #endregion

        #region UnsetData

        public static readonly DependencyProperty UnsetDataProperty =
            DependencyProperty.Register(
                nameof(UnsetData),
                typeof(Geometry),
                typeof(RatingItemPathInfo),
                null);

        public Geometry UnsetData
        {
            get => (Geometry)GetValue(UnsetDataProperty);
            set => SetValue(UnsetDataProperty, value);
        }

        #endregion

        protected override Freezable CreateInstanceCore()
        {
            return new RatingItemPathInfo();
        }
    }
}
