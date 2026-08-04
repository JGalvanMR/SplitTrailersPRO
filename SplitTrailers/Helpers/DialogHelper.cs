using System;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Text;
using Android.Text.Style;
using Google.Android.Material.Dialog;
using Android.Widget;

namespace SplitTrailers.Helpers
{
    public static class DialogHelper
    {
        /// <summary>
        /// Muestra un diálogo de error (título en color error, icono de "no").
        /// </summary>
        public static void ShowErrorDialog(Context context, string message, string positiveText = "Entendido", EventHandler<DialogClickEventArgs> positiveAction = null)
        {
            var colorError = ThemeHelper.GetColorFromTheme(context, Resource.Attribute.colorError);
            var titleSpannable = new SpannableStringBuilder("Error");
            titleSpannable.SetSpan(new ForegroundColorSpan(colorError), 0, titleSpannable.Length(), SpanTypes.ExclusiveExclusive);
            titleSpannable.SetSpan(new StyleSpan(TypefaceStyle.Bold), 0, titleSpannable.Length(), SpanTypes.ExclusiveExclusive);

            var builder = new MaterialAlertDialogBuilder(context, Resource.Style.ThemeOverlay_Material3_MaterialAlertDialog);
            builder.SetTitle(titleSpannable);
            builder.SetIcon(Resource.Drawable.no);
            builder.SetMessage(message);
            builder.SetPositiveButton(positiveText, positiveAction ?? delegate { });
            var dialog = builder.Create();
            dialog.Show();

            dialog.Window.DecorView.Post(() =>
            {
                var btn = dialog.GetButton((int)DialogButtonType.Positive);
                btn?.SetTextColor(colorError);
                btn?.SetAllCaps(false);
            });
        }

        /// <summary>
        /// Muestra un diálogo de éxito (título en color primario, icono de "exito").
        /// </summary>
        public static void ShowSuccessDialog(Context context, string message, string positiveText = "Aceptar", EventHandler<DialogClickEventArgs> positiveAction = null)
        {
            var colorPrimary = ThemeHelper.GetColorFromTheme(context, Resource.Attribute.colorPrimary);
            var titleSpannable = new SpannableStringBuilder("Éxito");
            titleSpannable.SetSpan(new ForegroundColorSpan(colorPrimary), 0, titleSpannable.Length(), SpanTypes.ExclusiveExclusive);
            titleSpannable.SetSpan(new StyleSpan(TypefaceStyle.Bold), 0, titleSpannable.Length(), SpanTypes.ExclusiveExclusive);

            var builder = new MaterialAlertDialogBuilder(context, Resource.Style.ThemeOverlay_Material3_MaterialAlertDialog);
            builder.SetTitle(titleSpannable);
            builder.SetIcon(Resource.Drawable.exito);
            builder.SetMessage(message);
            builder.SetPositiveButton(positiveText, positiveAction ?? delegate { });
            var dialog = builder.Create();
            dialog.Show();

            dialog.Window.DecorView.Post(() =>
            {
                var btn = dialog.GetButton((int)DialogButtonType.Positive);
                btn?.SetTextColor(colorPrimary);
                btn?.SetAllCaps(false);
            });
        }

        /// <summary>
        /// Muestra un diálogo de advertencia (título en color amarillo, icono de "warning").
        /// </summary>
        public static void ShowWarningDialog(Context context, string message, string positiveText = "Entendido", EventHandler<DialogClickEventArgs> positiveAction = null)
        {
            var colorWarning = Color.ParseColor("#F57C00"); // Naranja Material para advertencias
            var titleSpannable = new SpannableStringBuilder("Advertencia");
            titleSpannable.SetSpan(new ForegroundColorSpan(colorWarning), 0, titleSpannable.Length(), SpanTypes.ExclusiveExclusive);
            titleSpannable.SetSpan(new StyleSpan(TypefaceStyle.Bold), 0, titleSpannable.Length(), SpanTypes.ExclusiveExclusive);

            var builder = new MaterialAlertDialogBuilder(context, Resource.Style.ThemeOverlay_Material3_MaterialAlertDialog);
            builder.SetTitle(titleSpannable);
            builder.SetIcon(Resource.Drawable.warning);
            builder.SetMessage(message);
            builder.SetPositiveButton(positiveText, positiveAction ?? delegate { });
            var dialog = builder.Create();
            dialog.Show();

            dialog.Window.DecorView.Post(() =>
            {
                var btn = dialog.GetButton((int)DialogButtonType.Positive);
                btn?.SetTextColor(colorWarning);
                btn?.SetAllCaps(false);
            });
        }

        /// <summary>
        /// Muestra un diálogo de confirmación (Sí/No) con título y mensaje personalizados.
        /// </summary>
        public static void ShowConfirmDialog(Context context, string title, string message, string positiveText = "Sí", string negativeText = "No",
            EventHandler<DialogClickEventArgs> positiveAction = null, EventHandler<DialogClickEventArgs> negativeAction = null)
        {
            var colorPrimary = ThemeHelper.GetColorFromTheme(context, Resource.Attribute.colorPrimary);
            var colorOnSurface = ThemeHelper.GetColorFromTheme(context, Resource.Attribute.colorOnSurface);

            var titleSpannable = new SpannableStringBuilder(title);
            titleSpannable.SetSpan(new ForegroundColorSpan(colorPrimary), 0, titleSpannable.Length(), SpanTypes.ExclusiveExclusive);
            titleSpannable.SetSpan(new StyleSpan(TypefaceStyle.Bold), 0, titleSpannable.Length(), SpanTypes.ExclusiveExclusive);

            var builder = new MaterialAlertDialogBuilder(context, Resource.Style.ThemeOverlay_Material3_MaterialAlertDialog);
            builder.SetTitle(titleSpannable);
            builder.SetIcon(Resource.Drawable.question);
            builder.SetMessage(message);
            builder.SetPositiveButton(positiveText, positiveAction ?? delegate { });
            builder.SetNegativeButton(negativeText, negativeAction ?? delegate { });
            var dialog = builder.Create();
            dialog.Show();

            dialog.Window.DecorView.Post(() =>
            {
                var positiveBtn = dialog.GetButton((int)DialogButtonType.Positive);
                positiveBtn?.SetTextColor(colorPrimary);
                positiveBtn?.SetAllCaps(false);

                var negativeBtn = dialog.GetButton((int)DialogButtonType.Negative);
                negativeBtn?.SetTextColor(colorOnSurface);
                negativeBtn?.SetAllCaps(false);
            });
        }

        /// <summary>
        /// Muestra un diálogo informativo con título personalizado.
        /// </summary>
        public static void ShowInfoDialog(Context context, string title, string message, string positiveText = "Aceptar", int iconRes = Resource.Drawable.Info,
            EventHandler<DialogClickEventArgs> positiveAction = null)
        {
            var colorPrimary = ThemeHelper.GetColorFromTheme(context, Resource.Attribute.colorPrimary);
            var titleSpannable = new SpannableStringBuilder(title);
            titleSpannable.SetSpan(new ForegroundColorSpan(colorPrimary), 0, titleSpannable.Length(), SpanTypes.ExclusiveExclusive);
            titleSpannable.SetSpan(new StyleSpan(TypefaceStyle.Bold), 0, titleSpannable.Length(), SpanTypes.ExclusiveExclusive);

            var builder = new MaterialAlertDialogBuilder(context, Resource.Style.ThemeOverlay_Material3_MaterialAlertDialog);
            builder.SetTitle(titleSpannable);
            builder.SetIcon(iconRes);
            builder.SetMessage(message);
            builder.SetPositiveButton(positiveText, positiveAction ?? delegate { });
            var dialog = builder.Create();
            dialog.Show();

            dialog.Window.DecorView.Post(() =>
            {
                var btn = dialog.GetButton((int)DialogButtonType.Positive);
                btn?.SetTextColor(colorPrimary);
                btn?.SetAllCaps(false);
            });
        }

        /// <summary>
        /// Muestra un diálogo con lista de selección única (RadioButton).
        /// </summary>
        public static void ShowSingleChoiceDialog(Context context, string title, string[] items, int checkedItem,
            EventHandler<DialogClickEventArgs> itemSelected, string positiveText = "OK",
            EventHandler<DialogClickEventArgs> positiveAction = null)
        {
            var colorPrimary = ThemeHelper.GetColorFromTheme(context, Resource.Attribute.colorPrimary);
            var titleSpannable = new SpannableStringBuilder(title);
            titleSpannable.SetSpan(new ForegroundColorSpan(colorPrimary), 0, titleSpannable.Length(), SpanTypes.ExclusiveExclusive);
            titleSpannable.SetSpan(new StyleSpan(TypefaceStyle.Bold), 0, titleSpannable.Length(), SpanTypes.ExclusiveExclusive);

            var builder = new MaterialAlertDialogBuilder(context, Resource.Style.ThemeOverlay_Material3_MaterialAlertDialog);
            builder.SetTitle(titleSpannable);
            builder.SetCancelable(false);
            builder.SetSingleChoiceItems(items, checkedItem, itemSelected);
            builder.SetPositiveButton(positiveText, positiveAction ?? delegate { });
            var dialog = builder.Create();
            dialog.Show();

            dialog.Window.DecorView.Post(() =>
            {
                var btn = dialog.GetButton((int)DialogButtonType.Positive);
                btn?.SetTextColor(colorPrimary);
                btn?.SetAllCaps(false);
            });
        }
    }
}