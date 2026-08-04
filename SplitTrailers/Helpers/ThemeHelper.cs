using Android.Content;
using Android.Content.Res;
using Android.Graphics;
using Android.Util;

namespace SplitTrailers.Helpers
{
    public static class ThemeHelper
    {
        public static Color GetColorFromTheme(Context context, int attribute)
        {
            var typedValue = new TypedValue();
            context.Theme.ResolveAttribute(attribute, typedValue, true);
            return new Color(typedValue.Data);
        }

        public static int GetColorIntFromTheme(Context context, int attribute)
        {
            var typedValue = new TypedValue();
            context.Theme.ResolveAttribute(attribute, typedValue, true);
            return typedValue.Data;
        }
    }
}