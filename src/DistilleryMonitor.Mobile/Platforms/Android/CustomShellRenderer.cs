#if ANDROID
using Android.Content;
using Microsoft.Maui.Controls.Handlers.Compatibility;
using Microsoft.Maui.Controls.Platform.Compatibility;
using AndroidX.CoordinatorLayout.Widget;
using Google.Android.Material.BottomNavigation;
using Android.Graphics.Drawables;
using AndroidX.AppCompat.App;
using Color = Android.Graphics.Color;

namespace DistilleryMonitor.Mobile.Platforms.Android
{
    public class CustomShellRenderer : ShellRenderer
    {
        private readonly Context _context;

        public CustomShellRenderer(Context context) : base(context)
        {
            _context = context;
        }

        protected override IShellBottomNavViewAppearanceTracker CreateBottomNavViewAppearanceTracker(ShellItem shellItem)
        {
            return new CustomBottomNavAppearanceTracker(this, shellItem);
        }

        // FIXA TOOLBAR/TITLE
        protected override void OnElementSet(Shell element)
        {
            base.OnElementSet(element);

            // Dölj ActionBar/Toolbar
            if (_context is AppCompatActivity activity)
            {
                activity.SupportActionBar?.Hide();
            }
        }
    }

    public class CustomBottomNavAppearanceTracker : ShellBottomNavViewAppearanceTracker
    {
        public CustomBottomNavAppearanceTracker(IShellContext shellContext, ShellItem shellItem)
            : base(shellContext, shellItem)
        {
        }

        public override void SetAppearance(BottomNavigationView bottomView, IShellAppearanceElement appearance)
        {
            base.SetAppearance(bottomView, appearance);

            var layoutParams = bottomView.LayoutParameters;
            layoutParams.Height = 130;
            bottomView.LayoutParameters = layoutParams;

            if (bottomView.LayoutParameters is CoordinatorLayout.LayoutParams coordParams)
            {
                coordParams.SetMargins(20, 0, 20, 20);
                bottomView.LayoutParameters = coordParams;
            }

            bottomView.SetPadding(0, 15, 0, 25);

            var shape = new GradientDrawable();
            shape.SetShape(ShapeType.Rectangle);
            shape.SetColor(Color.ParseColor("#0d1421"));
            bottomView.Background = shape;

            bottomView.ItemIconSize = 28;
        }
    }
}
#endif
