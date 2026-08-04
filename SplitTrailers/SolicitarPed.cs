using Android.App;
using Android.Content;
using Android.Content.Res;
using Android.Graphics;
using Android.OS;
using Android.Support.V4.App;
using Android.Text;
using Android.Text.Style;
using Android.Util;
using Android.Views;
using Android.Views.InputMethods;
using Android.Widget;
using AndroidX.AppCompat.App;
using AndroidX.RecyclerView.Widget;
using Google.Android.Material.AppBar;
using Google.Android.Material.Dialog;
using Java.Lang;
using Org.Json;
using SplitTrailers.Modal;
using SplitTrailers.Models;
using SQLite;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Threading.Tasks;
//using Plugin.LocalNotifications;
using System.Timers;
using TaskStackBuilder = Android.Support.V4.App.TaskStackBuilder;
using SplitTrailers.Helpers;

namespace SplitTrailers


{
    [Activity(Label = "Ingresar Pedido")]
    public partial class SolicitarPed : AppCompatActivity
    {
        public static string cvvehiculo, cvresponsable;
        public static string vehiculo, responsable;
        public static string currentVersionName, imei;
        public string Nombre = "", Mtipo = "", MProd = "", MTar = "", MFol = "", mUser = "", user = "";
        public string Mtipo2 = "", MProd2 = "", MTar2 = "", MFol2 = "", CveCam = "", mOp = "A", Version = "15.3";
        public static SQLiteConnection db;
        SqlConnection thisConnection = new SqlConnection(MainActivity.cadenaConexion);
        SqlDataAdapter da;
        DataSet ds = new DataSet();
        SqlCommand cmnd = new SqlCommand();
        SqlCommand cmnd1 = new SqlCommand();
        SqlDataReader reader1;
        public static DataTable det_pedidos = new DataTable("det_pedidos");
        public static DataTable det_pedidos2 = new DataTable("det_pedidos2");
        public static DataTable productos_leidos = new DataTable("productos_leidos");
        public static DataTable Pedidostotales = new DataTable("Pedidostotales");
        string query = "", prod_clave = "", folio = "", tipo = "", cadena = "", prod_nombre = "";
        int tarima = 0, caja = 0, tarimaf = 0;
        bool find = false;
        ArrayAdapter<string> comboAdapter;
        string[] strFrutas;
        public string tb_tabla = "tb_mstr_pedidos_nal";
        public string tipoembarque = "NAL";

        int diasmincarga = 0;



        //traer los datos e id de cada uno de los elementos de la vista
        EditText pedido;
        TextView detalleped;
        TextView PedidosSurtidos;
        Button capturar;

        static int PICK_CONTACT_REQUEST = 1;

        //Variables Timmer
        System.Timers.Timer timer;
        int min = 0, sec = 0, miliseconds = 1;
        private int countminute = 1;

        int NOTIFICATION_ID = 1000;
        static readonly string CHANNEL_ID = "location_notification";
        internal static readonly string COUNT_KEY = "count";
        internal static readonly string RESPON_SABLE = "Responable";
        int count = 0;

        public string Cancelado { get; private set; }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            string contenido = "";
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.SolicitarPedidos);
            LoadConnection();




            //Declaracion de los id de cada elemento
            pedido = FindViewById<EditText>(Resource.Id.agregarpedido);
            PedidosSurtidos = FindViewById<TextView>(Resource.Id.textTOTALESCA);
            capturar = FindViewById<Button>(Resource.Id.button1);
            capturar.Click += Btnlogin_Click;
            capturar.Enabled = false;

            //llenarpedidos();


            //Recuperar datos de la pantalla anterior
            cvvehiculo = Intent.GetStringExtra("cvcamioneta");
            cvresponsable = Intent.GetStringExtra("cvresponsable");
            vehiculo = Intent.GetStringExtra("camioneta");
            responsable = Intent.GetStringExtra("responsable");
            currentVersionName = Intent.GetStringExtra("currentVersionName");
            imei = Intent.GetStringExtra("imei");


            TextView usuario = FindViewById<TextView>(Resource.Id.usuario);
            usuario.Text = responsable.Trim() + " - Split Trailer";

            var quexi = db.Query<Pedidos>("SELECT DISTINCT Folio FROM Pedidos");
            foreach (var captu in quexi)
            {
                pedido.Text = captu.folio.ToString();
            }

            string pedido_alta = validapedidoalta(pedido.Text.Trim());
            if (pedido_alta.Trim() != pedido.Text.Trim())
            {
                pedido.Text = "";
                db.Query<Pedidos>("delete from  [Pedidos]");
                db.Query<ConPedidos>("delete from  [ConPedidos]");
                db.Query<xLote>("delete from  [xLote]");
                db.Query<xLoteFinal>("delete from  [xLoteFinal]");
                db.Query<xprod>("delete from  [xprod]");
            }




            //Buscar Pedidos en la Base de datos *************************************************************************
            var quex = db.Query<Pedidos>("SELECT DISTINCT Folio FROM Pedidos");
            foreach (var captu in quex)
            {
                pedido.Text = captu.folio.ToString();
                ConsPedSur(captu.folio.ToString());
                capturar.Enabled = true;
                string Tipoped = "NAL";
                tb_tabla = "tb_mstr_pedidos_nal";


                if (pedido.Text.Length > 0)
                {
                    if (Convert.ToInt32(pedido.Text) < 300000)
                    {
                        Tipoped = "EXP";
                        tb_tabla = "tb_mstr_pedidos_exp";

                    }
                }
                thisConnection.Open();
                string Cadena = "Select pdn_diasmin from " + tb_tabla + " Where pdn_folio = '" + pedido.Text.Trim() + "' AND pdn_estatus != 'C'";
                SqlCommand cmdxi = new SqlCommand(Cadena, thisConnection);
                try
                {
                    diasmincarga = Convert.ToInt32(cmdxi.ExecuteScalar());
                }
                catch
                {
                    diasmincarga = 12;
                }
                thisConnection.Close();
            }

            /*string pedido_alta = validapedidoalta(pedido.Text.Trim());

            if (pedido_alta.Trim() != pedido.Text.Trim()) {
                db.Query<Pedidos>("delete from  [Pedidos]");
                db.Query<ConPedidos>("delete from  [ConPedidos]");
                db.Query<xLote>("delete from  [xLote]");
                db.Query<xLoteFinal>("delete from  [xLoteFinal]");
                db.Query<xprod>("delete from  [xprod]");
            }*/

            //Termina busqueda de pedidos *********************************************************************************

            pedido.EditorAction += (sender, e) =>
            {

                if (e.ActionId == ImeAction.Done || e.ActionId == ImeAction.Next)
                {

                    string nombrecapturaactual = pedidoasignadoalta(pedido.Text.Trim());
                    if (responsable.Trim().Contains("SUPERVISOR") != true)
                    {
                        if (nombrecapturaactual.Trim().Length > 0)
                        {
                            if (nombrecapturaactual.Trim() != responsable.Trim())
                            {
                                #region MATERIAL DIALOG
                                /*// Construimos el título con color y negritas
                                var titleSpannable = new SpannableStringBuilder("Pedido en Captura Ocupado");
                                titleSpannable.SetSpan(new ForegroundColorSpan(Color.ParseColor("#DC3545")), 0, titleSpannable.Length(), SpanTypes.ExclusiveExclusive);
                                titleSpannable.SetSpan(new StyleSpan(TypefaceStyle.Bold), 0, titleSpannable.Length(), SpanTypes.ExclusiveExclusive);

                                // Construimos el mensaje con colores y negritas
                                var mensajeSpannable = new SpannableStringBuilder();
                                mensajeSpannable.Append("El pedido ");
                                int startPedido = mensajeSpannable.Length();
                                mensajeSpannable.Append(pedido.Text.Trim());
                                mensajeSpannable.SetSpan(new StyleSpan(TypefaceStyle.Bold), startPedido, mensajeSpannable.Length(), SpanTypes.ExclusiveExclusive);

                                mensajeSpannable.Append(" está activo con el armador ");
                                int startArmador = mensajeSpannable.Length();
                                mensajeSpannable.Append(nombrecapturaactual.Trim());
                                mensajeSpannable.SetSpan(new StyleSpan(TypefaceStyle.Bold), startArmador, mensajeSpannable.Length(), SpanTypes.ExclusiveExclusive);

                                mensajeSpannable.Append(".\n\nSolicite una transferencia si se trata de un cambio de turno.");
                                mensajeSpannable.SetSpan(new ForegroundColorSpan(Color.ParseColor("#5F6368")), 0, mensajeSpannable.Length(), SpanTypes.ExclusiveExclusive);

                                // Creamos el diálogo usando el ThemeOverlay oficial de Material3
                                var builder = new MaterialAlertDialogBuilder(this, Resource.Style.ThemeOverlay_Material3_MaterialAlertDialog);
                                builder.SetTitle(titleSpannable);
                                builder.SetIcon(Resource.Drawable.no);
                                builder.SetMessage(mensajeSpannable);

                                // Botón principal
                                builder.SetPositiveButton("Entendido", (s, e) => { });

                                // Crear y mostrar el diálogo
                                var dialog = builder.Create();
                                dialog.Show();

                                // Personalizamos el botón después de mostrarlo
                                dialog.Window.DecorView.Post(() =>
                                {
                                    var positiveButton = dialog.GetButton((int)DialogButtonType.Positive);
                                    positiveButton?.SetTextColor(Color.ParseColor("#00695C")); // Verde Material
                                    positiveButton?.SetAllCaps(false);
                                });*/
                                #endregion
                                // Diálogo: Pedido en Captura Ocupado
                                DialogHelper.ShowInfoDialog(this,
                                    title: "Pedido en Captura Ocupado",
                                    message: $"El pedido {pedido.Text.Trim()} está activo con el armador {nombrecapturaactual.Trim()}. Solicite una transferencia si se trata de un cambio de turno.",
                                    iconRes: Resource.Drawable.no);
                                pedido.SetSelection(0, pedido.Text.Length);
                                pedido.RequestFocus();
                                return;
                            }
                        }
                    }

                    int pedPendientes = Splitpendiente();
                    List<string> emb_folios = new List<string>();
                    emb_folios = emb_folioPendiente();
                    string emb_folioPEndiente = string.Join(", ", emb_folios);

                    if (pedPendientes > 0)
                    {
                        #region MATERIAL DIALOG
                        /*// Construimos el título con color rojo y negritas
                        var titleSpannable = new SpannableStringBuilder("Cuenta con Split Pendientes");
                        titleSpannable.SetSpan(new ForegroundColorSpan(Color.ParseColor("#DC3545")), 0, titleSpannable.Length(), SpanTypes.ExclusiveExclusive);
                        titleSpannable.SetSpan(new StyleSpan(TypefaceStyle.Bold), 0, titleSpannable.Length(), SpanTypes.ExclusiveExclusive);

                        // Construimos el mensaje con colores y negritas
                        var mensajeSpannable = new SpannableStringBuilder();
                        mensajeSpannable.Append("Usted tiene ");
                        int startPendientes = mensajeSpannable.Length();
                        mensajeSpannable.Append((char)pedPendientes);
                        mensajeSpannable.SetSpan(new StyleSpan(TypefaceStyle.Bold), startPendientes, mensajeSpannable.Length(), SpanTypes.ExclusiveExclusive);

                        mensajeSpannable.Append(" Split sin cargar del pedido: ");
                        int startFolio = mensajeSpannable.Length();
                        mensajeSpannable.Append(emb_folioPEndiente);
                        mensajeSpannable.SetSpan(new StyleSpan(TypefaceStyle.Bold), startFolio, mensajeSpannable.Length(), SpanTypes.ExclusiveExclusive);

                        mensajeSpannable.Append(".\nFavor de consultar la orden en el monitor de embarques y solicitar al supervisor la carga del Split o cancelarlo si no se ha cargado.");
                        mensajeSpannable.SetSpan(new ForegroundColorSpan(Color.ParseColor("#5F6368")), 0, mensajeSpannable.Length(), SpanTypes.ExclusiveExclusive);

                        // Creamos el diálogo con Material3
                        var builder = new MaterialAlertDialogBuilder(this, Resource.Style.ThemeOverlay_Material3_MaterialAlertDialog);
                        builder.SetTitle(titleSpannable);
                        builder.SetIcon(Resource.Drawable.no);
                        builder.SetMessage(mensajeSpannable);

                        // Botón principal
                        builder.SetPositiveButton("Entendido", (s, e) => { });

                        // Crear y mostrar el diálogo
                        var dialog = builder.Create();
                        dialog.Show();

                        // Personalizamos el botón después de mostrarlo
                        dialog.Window.DecorView.Post(() =>
                        {
                            var positiveButton = dialog.GetButton((int)DialogButtonType.Positive);
                            positiveButton?.SetTextColor(Color.ParseColor("#00695C")); // Verde Material
                            positiveButton?.SetAllCaps(false);
                        });*/
                        #endregion
                        DialogHelper.ShowInfoDialog(this,
                            title: "Cuenta con Split Pendientes",
                            message: $"Usted tiene {pedPendientes} Split sin cargar del pedido: {emb_folioPEndiente}. Favor de consultar la orden en el monitor de embarques y solicitar al supervisor la carga del Split o cancelarlo si no se ha cargado.",
                            iconRes: Resource.Drawable.no);
                        pedido.SetSelection(0, pedido.Text.Length);
                        pedido.RequestFocus();
                        return;
                    }

                    thisConnection.Open();
                    string Validapdn = "Select prov_clave from tb_mstr_pedidos_nal Where pdn_folio = '" + pedido.Text.Trim() + "'";
                    SqlCommand cmdvalida = new SqlCommand(Validapdn, thisConnection);
                    string provedor = Convert.ToString(cmdvalida.ExecuteScalar());
                    thisConnection.Close();

                    string hay = "N";
                    string Cadena = "";

                    #region comentarios
                    ////********************************************validacion pedidos pendientes********************************************************************************************
                    //if (provedor != "PC")
                    //{
                    //    if (responsable.Contains("PEDRO") || responsable.Contains("ALAN") || responsable.Contains("ABRAHAM"))
                    //    {
                    //        if (validar_ordenes() == "0")
                    //        {
                    //            pedido.SetSelection(0, pedido.Text.Length);
                    //            pedido.RequestFocus();
                    //            return;
                    //        }
                    //    }
                    //}
                    //********************************************validacion pedidos pendientes********************************************************************************************

                    //if (mOp == "C")
                    //{

                    //    List<FlimStarInfo> lstFlimStar = ConsPed(pedido.Text.Trim());
                    //    var gvObject = FindViewById<GridView>(Resource.Id.gvCtrl);



                    //    gvObject.Adapter = new myGVItemAdapter(this, lstFlimStar);
                    //    gvObject.ItemClick += new EventHandler<AdapterView.ItemClickEventArgs>(OnGridView_ItemClicked); //detalle_pedido
                    //    return;
                    //}
                    //else
                    //{
                    #endregion

                    var queryqe = db.Table<Pedidos>();
                    foreach (var captu in queryqe)
                    {
                        if (captu.folio == pedido.Text.Trim())
                        {
                            hay = "S";

                            #region MATERIAL DIALOG
                            /*// Construimos el título con color y negritas
                            var titleSpannable = new SpannableStringBuilder("Pedido ya agregado para captura");
                            titleSpannable.SetSpan(new ForegroundColorSpan(Color.ParseColor("#DC3545")), 0, titleSpannable.Length(), SpanTypes.ExclusiveExclusive);
                            titleSpannable.SetSpan(new StyleSpan(TypefaceStyle.Bold), 0, titleSpannable.Length(), SpanTypes.ExclusiveExclusive);

                            // Construimos el mensaje con colores y énfasis
                            var mensajeSpannable = new SpannableStringBuilder();
                            mensajeSpannable.Append("El pedido ");
                            int startPedido = mensajeSpannable.Length();
                            mensajeSpannable.Append(captu.folio);
                            mensajeSpannable.SetSpan(new StyleSpan(TypefaceStyle.Bold), startPedido, mensajeSpannable.Length(), SpanTypes.ExclusiveExclusive);

                            mensajeSpannable.Append(" ya se agregó para capturar.");
                            mensajeSpannable.SetSpan(new ForegroundColorSpan(Color.ParseColor("#5F6368")), 0, mensajeSpannable.Length(), SpanTypes.ExclusiveExclusive);

                            // Creamos el diálogo usando el ThemeOverlay oficial de Material3
                            var builder = new MaterialAlertDialogBuilder(this, Resource.Style.ThemeOverlay_Material3_MaterialAlertDialog);
                            builder.SetTitle(titleSpannable);
                            builder.SetIcon(Resource.Drawable.no);
                            builder.SetMessage(mensajeSpannable);
                            builder.SetCancelable(false);

                            // Botón principal
                            builder.SetPositiveButton("Entendido", (s, e) => { });

                            // Crear y mostrar el diálogo
                            var dialog = builder.Create();
                            dialog.Show();

                            // Personalizamos el botón después de mostrarlo
                            dialog.Window.DecorView.Post(() =>
                            {
                                var positiveButton = dialog.GetButton((int)DialogButtonType.Positive);
                                positiveButton?.SetTextColor(Color.ParseColor("#00695C")); // Verde Material
                                positiveButton?.SetAllCaps(false);
                            });*/
                            #endregion

                            DialogHelper.ShowInfoDialog(this,
                                title: "Pedido ya agregado para captura",
                                message: $"El pedido {captu.folio} ya se agregó para capturar.",
                                iconRes: Resource.Drawable.no);
                            pedido.SetSelection(0, pedido.Text.Length);
                            pedido.RequestFocus();
                            ConsPedSur(pedido.Text.ToString());
                            Toast.MakeText(this, "Actualizacion de Pedido Exitoso", ToastLength.Short).Show();
                            return;
                        }
                    }

                    string Tipoped = "NAL";
                    tb_tabla = "tb_mstr_pedidos_nal";


                    if (pedido.Text.Length > 0)
                    {
                        if (Convert.ToInt32(pedido.Text) < 300000)
                        {
                            Tipoped = "EXP";
                            tb_tabla = "tb_mstr_pedidos_exp";

                        }
                    }

                    //Borrar datos almacenados de la bd local

                    db.Query<Pedidos>("delete from  [Pedidos]");
                    db.Query<ConPedidos>("delete from  [ConPedidos]");
                    db.Query<xLote>("delete from  [xLote]");
                    db.Query<xLoteFinal>("delete from  [xLoteFinal]");
                    db.Query<xprod>("delete from  [xprod]");

                    //Borrar datos almacenados de la bd local


                    thisConnection.Open();
                    Cadena = "Select emb_folio from tb_det_split Where emb_folio = '" + pedido.Text.Trim() + "'";
                    SqlCommand cmd = new SqlCommand(Cadena, thisConnection);
                    string Emb = Convert.ToString(cmd.ExecuteScalar());
                    thisConnection.Close();
                    if (Emb.Trim().Length > 0)
                    {
                        #region MATERIAL DIALOG
                        /*// 🔹 Título con color rojo y negritas
                        var titleSpannable = new SpannableStringBuilder("Pedido ya capturado");
                        titleSpannable.SetSpan(new ForegroundColorSpan(Color.ParseColor("#DC3545")), 0, titleSpannable.Length(), SpanTypes.ExclusiveExclusive);
                        titleSpannable.SetSpan(new StyleSpan(TypefaceStyle.Bold), 0, titleSpannable.Length(), SpanTypes.ExclusiveExclusive);

                        // 🔹 Mensaje con color blanco y parte en negrita
                        var mensajeSpannable = new SpannableStringBuilder();
                        mensajeSpannable.Append("El pedido: ");
                        int startPedido = mensajeSpannable.Length();
                        mensajeSpannable.Append(folio);
                        mensajeSpannable.SetSpan(new StyleSpan(TypefaceStyle.Bold), startPedido, mensajeSpannable.Length(), SpanTypes.ExclusiveExclusive);
                        mensajeSpannable.Append(" ya ha sido capturado.");
                        mensajeSpannable.SetSpan(new ForegroundColorSpan(Color.ParseColor("#FFFFFF")), 0, mensajeSpannable.Length(), SpanTypes.ExclusiveExclusive);

                        // 🔹 Creamos el diálogo con el tema oficial de Material 3
                        var builder = new MaterialAlertDialogBuilder(this, Resource.Style.ThemeOverlay_Material3_MaterialAlertDialog);
                        builder.SetTitle(titleSpannable);
                        builder.SetIcon(Resource.Drawable.no);
                        builder.SetMessage(mensajeSpannable);
                        builder.SetCancelable(false);

                        // 🔹 Botón principal
                        builder.SetPositiveButton("Ok", (s, e) => { });

                        // 🔹 Crear y mostrar
                        var dialog = builder.Create();
                        dialog.Show();

                        // 🔹 Personalizar el botón (color, mayúsculas)
                        dialog.Window.DecorView.Post(() =>
                        {
                            var positiveButton = dialog.GetButton((int)DialogButtonType.Positive);
                            positiveButton?.SetTextColor(Color.ParseColor("#00695C")); // Verde Material
                            positiveButton?.SetAllCaps(false);
                        });*/
                        #endregion
                        DialogHelper.ShowInfoDialog(this,
                            title: "Pedido ya capturado",
                            message: $"El pedido: {pedido.Text.Trim()} ya ha sido capturado.",
                            iconRes: Resource.Drawable.no);
                        //pedido.Text = "";
                        //pedido.RequestFocus();

                        string StPed = EstatusPed(pedido.Text);
                        if (StPed != "--:--" || StPed.Trim().Length == 0)
                            capturar.Enabled = true;
                        mOp = "C";


                        //validacion de pedido vs surtido.... que no haya nada capturado de mas

                        //return;

                    }

                    DateTime FechaHoy = DateTime.Now;

                    thisConnection.Open();

                    Cadena = "Select pdn_fecha from " + tb_tabla + " Where pdn_folio = '" + pedido.Text.Trim() + "' AND pdn_estatus != 'C'";

                    SqlCommand cmdxi = new SqlCommand(Cadena, thisConnection);
                    DateTime FechaPedido = Convert.ToDateTime(cmdxi.ExecuteScalar());

                    Cadena = "Select pdn_diasmin from " + tb_tabla + " Where pdn_folio = '" + pedido.Text.Trim() + "' AND pdn_estatus != 'C'";
                    cmdxi = new SqlCommand(Cadena, thisConnection);
                    try
                    {
                        diasmincarga = Convert.ToInt32(cmdxi.ExecuteScalar());
                    }
                    catch
                    {
                        diasmincarga = 12;
                    }



                    TimeSpan tspan = FechaHoy - FechaPedido;

                    int dias = tspan.Days;

                    thisConnection.Close();
                    if (dias > 15)
                    {
                        #region MATERIAL DIALOG
                        /*// 🔹 Construimos el título con color y negritas
                        var titleSpannable = new SpannableStringBuilder("Pedido mayor a 15 Días");
                        titleSpannable.SetSpan(new ForegroundColorSpan(Color.ParseColor("#FA993E")), 0, titleSpannable.Length(), SpanTypes.ExclusiveExclusive);
                        titleSpannable.SetSpan(new StyleSpan(TypefaceStyle.Bold), 0, titleSpannable.Length(), SpanTypes.ExclusiveExclusive);

                        // 🔹 Construimos el mensaje con colores y negritas
                        var mensajeSpannable = new SpannableStringBuilder();
                        mensajeSpannable.Append("El pedido ");
                        int startPedido = mensajeSpannable.Length();
                        mensajeSpannable.Append(pedido.Text.Trim());
                        mensajeSpannable.SetSpan(new StyleSpan(TypefaceStyle.Bold), startPedido, mensajeSpannable.Length(), SpanTypes.ExclusiveExclusive);

                        mensajeSpannable.Append(" tiene una fecha de ");
                        int startFecha = mensajeSpannable.Length();
                        mensajeSpannable.Append(FechaPedido.ToString("dd/MM/yyyy"));
                        mensajeSpannable.SetSpan(new StyleSpan(TypefaceStyle.Bold), startFecha, mensajeSpannable.Length(), SpanTypes.ExclusiveExclusive);

                        mensajeSpannable.Append(" que supera los 15 días de validez. Favor de informar a ventas.");
                        mensajeSpannable.SetSpan(new ForegroundColorSpan(Color.ParseColor("#FAC73E")), 0, mensajeSpannable.Length(), SpanTypes.ExclusiveExclusive);

                        // 🔹 Creamos el diálogo usando el ThemeOverlay oficial de Material3
                        var builder = new MaterialAlertDialogBuilder(this, Resource.Style.ThemeOverlay_Material3_MaterialAlertDialog);
                        builder.SetTitle(titleSpannable);
                        builder.SetIcon(Resource.Drawable.warning);
                        builder.SetMessage(mensajeSpannable);
                        builder.SetCancelable(false);

                        // 🔹 Botón principal
                        builder.SetPositiveButton("Entendido", (s, e) => { });

                        // 🔹 Crear y mostrar el diálogo
                        var dialog = builder.Create();
                        dialog.Show();

                        // 🔹 Personalizamos el botón después de mostrarlo
                        dialog.Window.DecorView.Post(() =>
                        {
                            var positiveButton = dialog.GetButton((int)DialogButtonType.Positive);
                            positiveButton?.SetTextColor(Color.ParseColor("#FF8C00")); // Naranja Material
                            positiveButton?.SetAllCaps(false);
                        });*/
                        #endregion
                        DialogHelper.ShowWarningDialog(this,
                            message: $"El pedido {pedido.Text.Trim()} tiene una fecha de {FechaPedido.ToString("dd/MM/yyyy")} que supera los 15 días de validez. Favor de informar a ventas.",
                            positiveText: "Entendido");
                        pedido.Text = "";
                        pedido.RequestFocus();
                        capturar.Enabled = false;
                        return;
                    }

                    thisConnection.Open();
                    Cadena = "Select emb_folio from tb_mstr_embarque Where emb_folio = '" + pedido.Text.Trim() + "' AND sts = 'T' AND hora_fin != '--:--'";
                    SqlCommand embcerr = new SqlCommand(Cadena, thisConnection);
                    string embcer = Convert.ToString(embcerr.ExecuteScalar());
                    thisConnection.Close();
                    if (embcer.Trim().Length > 0)
                    {
                        #region MATERIAL DIALOG
                        /*// 🔹 Construimos el título con color y negritas
                        var titleSpannable = new SpannableStringBuilder("Embarque Cerrado");
                        titleSpannable.SetSpan(new ForegroundColorSpan(Color.ParseColor("#FA993E")), 0, titleSpannable.Length(), SpanTypes.ExclusiveExclusive);
                        titleSpannable.SetSpan(new StyleSpan(TypefaceStyle.Bold), 0, titleSpannable.Length(), SpanTypes.ExclusiveExclusive);

                        // 🔹 Construimos el mensaje con colores y negritas
                        var mensajeSpannable = new SpannableStringBuilder();
                        mensajeSpannable.Append("El embarque ");
                        int startPedido = mensajeSpannable.Length();
                        mensajeSpannable.Append(pedido.Text.Trim());
                        mensajeSpannable.SetSpan(new StyleSpan(TypefaceStyle.Bold), startPedido, mensajeSpannable.Length(), SpanTypes.ExclusiveExclusive);

                        mensajeSpannable.Append(" está cerrado y no se puede cargar.");
                        mensajeSpannable.SetSpan(new ForegroundColorSpan(Color.ParseColor("#FAC73E")), 0, mensajeSpannable.Length(), SpanTypes.ExclusiveExclusive);

                        // 🔹 Creamos el diálogo usando el ThemeOverlay oficial de Material3
                        var builder = new MaterialAlertDialogBuilder(this, Resource.Style.ThemeOverlay_Material3_MaterialAlertDialog);
                        builder.SetTitle(titleSpannable);
                        builder.SetIcon(Resource.Drawable.warning);
                        builder.SetMessage(mensajeSpannable);
                        builder.SetCancelable(false);

                        // 🔹 Botón principal
                        builder.SetPositiveButton("Entendido", (s, e) => { });

                        // 🔹 Crear y mostrar el diálogo
                        var dialog = builder.Create();
                        dialog.Show();

                        // 🔹 Personalizamos el botón después de mostrarlo
                        dialog.Window.DecorView.Post(() =>
                        {
                            var positiveButton = dialog.GetButton((int)DialogButtonType.Positive);
                            positiveButton?.SetTextColor(Color.ParseColor("#FF8C00")); // Naranja Material
                            positiveButton?.SetAllCaps(false);
                        });*/
                        #endregion
                        DialogHelper.ShowWarningDialog(this,
                            message: $"El embarque {pedido.Text.Trim()} está cerrado y no se puede cargar.",
                            positiveText: "Entendido");
                        pedido.Text = "";
                        capturar.Enabled = false;
                        pedido.RequestFocus();
                        return;
                    }

                    thisConnection.Open();
                    Cadena = "Select pdn_folio from " + tb_tabla + " Where pdn_folio = '" + pedido.Text.Trim() + "' AND pdn_estatus = 'C'";
                    SqlCommand cmdx = new SqlCommand(Cadena, thisConnection);
                    string Embx = Convert.ToString(cmdx.ExecuteScalar());
                    thisConnection.Close();
                    if (Embx.Trim().Length > 0)
                    {
                        #region MATERIAL DIALOG
                        /*// 🔹 Construimos el título con color y negritas
                        var titleSpannable = new SpannableStringBuilder("Pedido Cancelado");
                        titleSpannable.SetSpan(new ForegroundColorSpan(Color.ParseColor("#FA993E")), 0, titleSpannable.Length(), SpanTypes.ExclusiveExclusive);
                        titleSpannable.SetSpan(new StyleSpan(TypefaceStyle.Bold), 0, titleSpannable.Length(), SpanTypes.ExclusiveExclusive);

                        // 🔹 Construimos el mensaje con colores y negritas
                        var mensajeSpannable = new SpannableStringBuilder();
                        mensajeSpannable.Append("El pedido ");
                        int startPedido = mensajeSpannable.Length();
                        mensajeSpannable.Append(pedido.Text.Trim());
                        mensajeSpannable.SetSpan(new StyleSpan(TypefaceStyle.Bold), startPedido, mensajeSpannable.Length(), SpanTypes.ExclusiveExclusive);

                        mensajeSpannable.Append(" está cancelado y no se puede cargar.");
                        mensajeSpannable.SetSpan(new ForegroundColorSpan(Color.ParseColor("#FAC73E")), 0, mensajeSpannable.Length(), SpanTypes.ExclusiveExclusive);

                        // 🔹 Creamos el diálogo usando el ThemeOverlay oficial de Material3
                        var builder = new MaterialAlertDialogBuilder(this, Resource.Style.ThemeOverlay_Material3_MaterialAlertDialog);
                        builder.SetTitle(titleSpannable);
                        builder.SetIcon(Resource.Drawable.warning);
                        builder.SetMessage(mensajeSpannable);
                        builder.SetCancelable(false);

                        // 🔹 Botón principal
                        builder.SetPositiveButton("Entendido", (s, e) => { });

                        // 🔹 Crear y mostrar el diálogo
                        var dialog = builder.Create();
                        dialog.Show();

                        // 🔹 Personalizamos el botón después de mostrarlo
                        dialog.Window.DecorView.Post(() =>
                        {
                            var positiveButton = dialog.GetButton((int)DialogButtonType.Positive);
                            positiveButton?.SetTextColor(Color.ParseColor("#FF8C00")); // Naranja Material
                            positiveButton?.SetAllCaps(false);
                        });*/
                        #endregion
                        DialogHelper.ShowWarningDialog(this,
                            message: $"El pedido {pedido.Text.Trim()} está cancelado y no se puede cargar.",
                            positiveText: "Entendido");
                        pedido.Text = "";
                        pedido.Text = "";
                        capturar.Enabled = false;
                        pedido.RequestFocus();
                        return;
                    }

                    if (hay == "N")
                    {
                        thisConnection.Open();
                        Cadena = "Select a.pdn_folio,a.prod_clave,b.prod_nombre,a.pdn_num_unidades From tb_det_pedidos A, tb_Cat_producto B " +
                            "where a.pdn_folio = '" + pedido.Text.Trim() + "' and a.prod_clave = b.prod_clave and A.pdn_Tipo = '" + Tipoped + "'";
                        SqlDataAdapter da = new SqlDataAdapter(Cadena, thisConnection);
                        DataSet ds = new DataSet();
                        da.Fill(ds, "Ped");
                        DataTable Ped = ds.Tables["Ped"];
                        hay = "N";
                        thisConnection.Close();

                        if (Ped.Rows.Count == 0)
                        {
                            #region MATERIAL DIALOG
                            /*// 🔹 Construimos el título con color rojo y negritas
                            var titleSpannable = new SpannableStringBuilder("Pedido Inexistente");
                            titleSpannable.SetSpan(new ForegroundColorSpan(Color.ParseColor("#DC3545")), 0, titleSpannable.Length(), SpanTypes.ExclusiveExclusive);
                            titleSpannable.SetSpan(new StyleSpan(TypefaceStyle.Bold), 0, titleSpannable.Length(), SpanTypes.ExclusiveExclusive);

                            // 🔹 Construimos el mensaje con colores y negritas
                            var mensajeSpannable = new SpannableStringBuilder();
                            mensajeSpannable.Append("El pedido ");
                            int startPedido = mensajeSpannable.Length();
                            mensajeSpannable.Append(pedido.Text.Trim());
                            mensajeSpannable.SetSpan(new StyleSpan(TypefaceStyle.Bold), startPedido, mensajeSpannable.Length(), SpanTypes.ExclusiveExclusive);

                            mensajeSpannable.Append(" no existe o no se ha dado de alta.");
                            mensajeSpannable.SetSpan(new ForegroundColorSpan(Color.ParseColor("#FFFFFF")), 0, mensajeSpannable.Length(), SpanTypes.ExclusiveExclusive);

                            // 🔹 Creamos el diálogo usando el ThemeOverlay oficial de Material3
                            var builder = new MaterialAlertDialogBuilder(this, Resource.Style.ThemeOverlay_Material3_MaterialAlertDialog);
                            builder.SetTitle(titleSpannable);
                            builder.SetIcon(Resource.Drawable.no);
                            builder.SetMessage(mensajeSpannable);
                            builder.SetCancelable(false);

                            // 🔹 Botón principal
                            builder.SetPositiveButton("Entendido", (s, e) => { });

                            // 🔹 Crear y mostrar el diálogo
                            var dialog = builder.Create();
                            dialog.Show();

                            // 🔹 Personalizamos el botón después de mostrarlo
                            dialog.Window.DecorView.Post(() =>
                            {
                                var positiveButton = dialog.GetButton((int)DialogButtonType.Positive);
                                positiveButton?.SetTextColor(Color.ParseColor("#00695C")); // Verde Material
                                positiveButton?.SetAllCaps(false);
                            });*/
                            #endregion
                            DialogHelper.ShowErrorDialog(this,
                            message: $"El pedido {pedido.Text.Trim()} no existe o no se ha dado de alta.",
                            positiveText: "Entendido");
                            pedido.Text = "";
                            capturar.Enabled = false;
                            pedido.RequestFocus();
                            return;
                        }

                        if (Tipoped == "NAL")
                        {
                            llenarpedidos();
                            //string color = ValidarColores(pedido.Text.Trim());
                            string color = ValidarHoras(pedido.Text.Trim());

                            thisConnection.Open();
                            string Cadenasup = "Select top(1) isNull(supervisor, 0) from  tb_Respon_Split where nom_capsplit = '" + responsable.Trim() + "' AND status = 'A'";
                            SqlCommand cmdxsup = new SqlCommand(Cadenasup, thisConnection);
                            string validasupervisor = Convert.ToString(cmdxsup.ExecuteScalar());
                            thisConnection.Close();

                            if (color != "1" && validasupervisor != "1")
                            {
                                #region MATERIAL DIALOG
                                /*// 🔹 Construimos el título con color amarillo y negritas
                                var titleSpannable = new SpannableStringBuilder("NO SE PUEDE CONTINUAR CON EL ARMADO");
                                titleSpannable.SetSpan(new ForegroundColorSpan(Color.ParseColor("#E5FA7A")), 0, titleSpannable.Length(), SpanTypes.ExclusiveExclusive);
                                titleSpannable.SetSpan(new StyleSpan(TypefaceStyle.Bold), 0, titleSpannable.Length(), SpanTypes.ExclusiveExclusive);

                                // 🔹 Construimos el mensaje con color amarillo y negritas
                                var mensajeSpannable = new SpannableStringBuilder(color);
                                mensajeSpannable.SetSpan(new ForegroundColorSpan(Color.ParseColor("#F6F87A")), 0, mensajeSpannable.Length(), SpanTypes.ExclusiveExclusive);
                                mensajeSpannable.SetSpan(new StyleSpan(TypefaceStyle.Bold), 0, mensajeSpannable.Length(), SpanTypes.ExclusiveExclusive);

                                // 🔹 Creamos el diálogo usando el ThemeOverlay oficial de Material3
                                var builder = new MaterialAlertDialogBuilder(this, Resource.Style.ThemeOverlay_Material3_MaterialAlertDialog);
                                builder.SetTitle(titleSpannable);
                                builder.SetIcon(Resource.Drawable.no);
                                builder.SetMessage(mensajeSpannable);
                                builder.SetCancelable(false);

                                // 🔹 Botón principal
                                builder.SetPositiveButton("Entendido", (s, e) => { });

                                // 🔹 Crear y mostrar el diálogo
                                var dialog = builder.Create();
                                dialog.Show();

                                // 🔹 Personalizamos el botón después de mostrarlo
                                dialog.Window.DecorView.Post(() =>
                                {
                                    var positiveButton = dialog.GetButton((int)DialogButtonType.Positive);
                                    positiveButton?.SetTextColor(Color.ParseColor("#FFA000")); // Naranja Material
                                    positiveButton?.SetAllCaps(false);
                                });*/
                                #endregion
                                DialogHelper.ShowWarningDialog(this,
                                message: color,
                                positiveText: "Entendido");
                                pedido.Text = "";
                                capturar.Enabled = false;
                                pedido.RequestFocus();
                                return;
                                /*}
                                else if (color.Contains("AZUL"))
                                {
                                    Android.App.AlertDialog.Builder alertDialog = new Android.App.AlertDialog.Builder(this);
                                    alertDialog.SetTitle(Html.FromHtml("<font color='#7AE3F8' size = 10>Pedidos Azules Disponibles</font>"));
                                    alertDialog.SetIcon(Resource.Drawable.no);
                                    alertDialog.SetCancelable(false);
                                    alertDialog.SetMessage(Html.FromHtml("<font color='#7AC6F8' size = 10>" + color + "</font>"));
                                    alertDialog.SetNeutralButton("Ok", delegate
                                    {
                                        alertDialog.Dispose();
                                    });
                                    alertDialog.Show();
                                    pedido.Text = "";
                                    capturar.Enabled = false;
                                    pedido.RequestFocus();
                                    return;
                                }*/
                            }
                        }


                        if (nombrecapturaactual.Trim().Length == 0)
                        {

                            string StPed = EstatusPed(pedido.Text);
                            if (StPed != "--:--" || StPed.Trim().Length == 0)
                            {
                                thisConnection.Open();
                                string cadena = "INSERT INTO  tb_det_acceso_celulares ( fecha, imei, nom_usu, sistema, folio, version, estado) " +
                                           "VALUES(GETDATE(),'" + imei + "','" + responsable + "','SplitTrailer','" + pedido.Text.Trim() + "','" + currentVersionName + "','A')";
                                cmd = new SqlCommand(cadena, thisConnection);
                                cmd.ExecuteNonQuery();
                                thisConnection.Close();
                            }
                        }

                        foreach (DataRow row in Ped.Rows)
                        {

                            string mnom = row["prod_nombre"].ToString().Trim();
                            mnom = mnom.Replace("'", " ");

                            Pedidos Pedidoscapturados = new Pedidos { folio = pedido.Text.Trim(), prod_clave = row["prod_clave"].ToString().Trim(), nombre = mnom, pedido = Convert.ToInt32(row["pdn_num_unidades"]), surtido = 0 };
                            //Registra en la base de datos SQLite
                            db.Insert(Pedidoscapturados);


                            var encontrado = 0;
                            var query = db.Table<ConPedidos>();
                            foreach (var captu in query)
                            {
                                if (captu.prod_clave.ToString().Trim() == row["prod_clave"].ToString().Trim())
                                {
                                    encontrado = 1;
                                    var total = Convert.ToInt16(row["pdn_num_unidades"]) + Convert.ToInt16(captu.pedido);
                                    db.Query<ConPedidos>("UPDATE [ConPedidos] SET pedido = '" + total + "' WHERE prod_clave = '" + captu.prod_clave.ToString().Trim() + "'");
                                }
                            }

                            if (encontrado == 0)
                            {

                                ConPedidos consecutivo = new ConPedidos { prod_clave = row["prod_clave"].ToString().Trim(), nombre = mnom, pedido = Convert.ToInt32(row["pdn_num_unidades"]), surtido = 0 };
                                //Registra en la base de datos SQLite
                                db.Insert(consecutivo);

                            }

                            hay = "S";
                        }
                        if (mOp == "C")
                        {
                            thisConnection.Close();
                            ConsPedSur(pedido.Text.Trim());
                            return;
                        }
                        if (hay == "S")
                        {
                            ConsPedSur(pedido.Text.ToString());



                            Toast.MakeText(this, "Pedido agregado Correctamente", ToastLength.Short).Show();
                        }
                        thisConnection.Close();
                        if (mOp == "A")
                        {
                            capturar.Enabled = true;

                            //List<FlimStarInfo> lstFlimStar = detalle_pedido(pedido.Text.Trim(), "Acumulado");
                            //var gvObject = FindViewById<GridView>(Resource.Id.gvCtrl);
                            //gvObject.Adapter = new myGVItemAdapter(this, lstFlimStar);
                            //gvObject.ItemClick += new EventHandler<AdapterView.ItemClickEventArgs>(OnGridView_ItemClicked); //detalle_pedido
                        }
                        pedido.SetSelection(0, pedido.Text.Length);
                        pedido.RequestFocus();
                    }
                    else
                    {
                        e.Handled = false;
                    }

                    //}

                    LoadConnection();
                }
            };



            CreateNotificationChannel();
            //var button = FindViewById<Button>(Resource.Id.buttonnot);
            //button.Click += ButtonOnClick;

            // Referencia al MaterialToolbar
            MaterialToolbar toolbar = FindViewById<MaterialToolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);
            SupportActionBar.Title = "INGRESAR PEDIDO";
            SupportActionBar.SetDisplayHomeAsUpEnabled(false); // si quieres back button

        }

        string currentVersionCode;


        protected override void OnResume()
        {
            base.OnResume();
            timer = new System.Timers.Timer();
            timer.Interval = 1000; // 1 milliseconds  
            timer.Elapsed += Timer_Elapsed;
            timer.Start();
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (countminute < 60)
            {
                countminute++;
            }
            else if (countminute >= 60)
            {
                countminute = 1;
                Task.Run(async () =>
                {
                    await Validacambiosorden();
                });
            }
        }

        private void OnGridView_ItemClicked(object sender, AdapterView.ItemClickEventArgs e)
        {

        }

        List<FlimStarInfo> listItem = new List<FlimStarInfo>();

        List<FlimStarInfo> GetFlimStarInformation()
        {
            throw new NotImplementedException();
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.top_menus, menu);
            return base.OnCreateOptionsMenu(menu);
        }

        public string validapedidoalta(string pedidoenlectora)
        {
            string pedido = "";

            thisConnection.Open();
            string Cadena = "Select top(1) folio from tb_det_acceso_celulares where folio = '" + pedidoenlectora + "' AND nom_usu = '" + responsable.Trim() + "' AND estado = 'A'";
            SqlCommand cmdx = new SqlCommand(Cadena, thisConnection);
            pedido = Convert.ToString(cmdx.ExecuteScalar());
            thisConnection.Close();

            return pedido;
        }


        public string asignapedidoalta()
        {
            string pedido = "";

            thisConnection.Open();
            string Cadena = "Select folio from tb_det_acceso_celulares where folio != '' AND nom_usu = '" + responsable.Trim() + "' AND estado = 'A'";
            SqlCommand cmdx = new SqlCommand(Cadena, thisConnection);
            pedido = Convert.ToString(cmdx.ExecuteScalar());
            thisConnection.Close();

            return pedido;
        }

        public string pedidoasignadoalta(string folio)
        {
            string pedido = "";

            thisConnection.Open();
            string Cadena = "Select nom_usu from tb_det_acceso_celulares where folio = '" + folio.Trim() + "' AND estado = 'A'";
            SqlCommand cmdx = new SqlCommand(Cadena, thisConnection);
            pedido = Convert.ToString(cmdx.ExecuteScalar());
            thisConnection.Close();

            return pedido;
        }

        public int Splitpendiente()
        {
            string pedido = "";
            int pedidospendientes = 0;

            thisConnection.Open();
            //Buscar el numero del que parte si es campo o proceso              4915 6684 5489 8537
            cmnd = thisConnection.CreateCommand();
            query = "SELECT GETDATE()";
            SqlCommand cmd = new SqlCommand(query, thisConnection);
            DateTime tiempoactual = Convert.ToDateTime(cmd.ExecuteScalar());
            thisConnection.Close();

            query = "SELECT * FROM tb_det_split WHERE NOM_CAPSPLIT = '" + responsable.Trim() + "' AND estatus = 'A'";

            thisConnection.Open();
            cmd = new SqlCommand(query);
            cmd.Connection = thisConnection;
            SqlDataReader Info;
            Info = cmd.ExecuteReader();
            while (Info.Read())
            {

                string fe = Info["FECHA"].ToString().Replace("a.m.", "a. m.").Replace("p.m.", "p. m.");

                string[] Fechacaducidad = Info["FECHA"].ToString().Split('/');
                DateTime prueba = DateTime.Now;
                try
                {
                    fe = Fechacaducidad[1] + "/" + Fechacaducidad[0] + "/" + Fechacaducidad[2].Replace("a.m.", "a. m.").Replace("p.m.", "p. m.");
                    prueba = Convert.ToDateTime(fe);
                }
                catch
                {
                    fe = Fechacaducidad[0] + "/" + Fechacaducidad[1] + "/" + Fechacaducidad[2].Replace("a.m.", "a. m.").Replace("p.m.", "p. m.");
                    prueba = Convert.ToDateTime(fe);
                }

                TimeSpan span = tiempoactual.Subtract(prueba);

                int totalhoras = (span.Days * 24) + span.Hours;

                if (totalhoras > 36)
                {
                    pedidospendientes = pedidospendientes + 1;
                }
            }
            thisConnection.Close();

            return pedidospendientes;
        }

        public List<string> emb_folioPendiente()
        {
            List<string> listPedidosPendientes = new List<string>();
            string pedido = "";

            thisConnection.Open();
            //Buscar el numero del que parte si es campo o proceso              4915 6684 5489 8537
            cmnd = thisConnection.CreateCommand();
            SqlCommand cmd = new SqlCommand(query, thisConnection);
            cmd.CommandTimeout = 0;
            thisConnection.Close();

            query = "SELECT distinct(emb_folio) FROM tb_det_split WHERE NOM_CAPSPLIT = '" + responsable.Trim() + "' AND estatus = 'A'";

            thisConnection.Open();
            cmd = new SqlCommand(query);
            cmd.Connection = thisConnection;
            SqlDataReader Info;
            Info = cmd.ExecuteReader();
            int i = 0;
            while (Info.Read())
            {
                string emb_folio = Info["emb_folio"].ToString();
                listPedidosPendientes.Add(emb_folio);
            }
            thisConnection.Close();

            return listPedidosPendientes;
        }

        private void spinner_ItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {


            Spinner spinner = (Spinner)sender;
            var folio = spinner.GetItemAtPosition(e.Position).ToString();

            #region RECYCLERVIEW
            // Obtener los datos
            List<FlimStarInfo> lstFlimStar = detalle_pedido(folio.Trim(), "Individual");

            // Obtener referencia al RecyclerView
            var recyclerView = FindViewById<RecyclerView>(Resource.Id.gvCtrl);

            // Configurar el LayoutManager (solo la primera vez)
            if (recyclerView.GetLayoutManager() == null)
            {
                var layoutManager = new LinearLayoutManager(this);
                recyclerView.SetLayoutManager(layoutManager);
            }

            // Obtener el adaptador actual
            var adapter = recyclerView.GetAdapter() as MyRecyclerAdapter;

            if (adapter != null)
            {
                // Si ya existe un adaptador, actualizar los datos
                adapter.UpdateData(lstFlimStar);
            }
            else
            {
                // Si no existe, crear uno nuevo
                adapter = new MyRecyclerAdapter(this, lstFlimStar);
                recyclerView.SetAdapter(adapter);

                // Manejar clics en items
                adapter.ItemClick += (sender, position) =>
                {
                    var item = adapter.GetItem(position);
                    OnRecyclerViewItemClicked(position, item);
                };
            }
            #endregion
            #region GRIDVIEW
            /*List<FlimStarInfo> lstFlimStar = detalle_pedido(folio.Trim(), "Individual");
            var gvObject = FindViewById<GridView>(Resource.Id.gvCtrl);
            gvObject.Adapter = new myGVItemAdapter(this, null);
            gvObject.Adapter = null;
            gvObject.Adapter = new myGVItemAdapter(this, lstFlimStar);
            gvObject.ItemClick += new EventHandler<AdapterView.ItemClickEventArgs>(OnGridView_ItemClicked);*/
            #endregion
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            if (Convert.ToString(item.TitleFormatted) == "Nuevo")
            {
                //Pedidos.Enabled = true;
                pedido.Text = "";
                pedido.RequestFocus();
                capturar.Enabled = false;
                PedidosSurtidos.Text = "000|000";

                #region RECYCLERVIEW
                // Obtener los datos
                List<FlimStarInfo> lstFlimStar = detalle_pedido(folio.Trim(), "Individual");

                // Obtener referencia al RecyclerView
                var recyclerView = FindViewById<RecyclerView>(Resource.Id.gvCtrl);

                // Configurar el LayoutManager (solo la primera vez)
                if (recyclerView.GetLayoutManager() == null)
                {
                    var layoutManager = new LinearLayoutManager(this);
                    recyclerView.SetLayoutManager(layoutManager);
                }

                // Obtener el adaptador actual
                var adapter = recyclerView.GetAdapter() as MyRecyclerAdapter;

                if (adapter != null)
                {
                    // Si ya existe un adaptador, actualizar los datos
                    adapter.UpdateData(lstFlimStar);
                }
                else
                {
                    // Si no existe, crear uno nuevo
                    adapter = new MyRecyclerAdapter(this, lstFlimStar);
                    recyclerView.SetAdapter(adapter);

                    // Manejar clics en items
                    adapter.ItemClick += (sender, position) =>
                    {
                        var item = adapter.GetItem(position);
                        OnRecyclerViewItemClicked(position, item);
                    };
                }
                #endregion
                #region GRIDVIEW
                /*List<FlimStarInfo> lstFlimStar = detalle_pedido(folio.Trim(), "Individual");
                lstFlimStar.Clear();
                var gvObject = FindViewById<GridView>(Resource.Id.gvCtrl);
                gvObject.Adapter = new myGVItemAdapter(this, null);
                gvObject.Adapter = null;
                gvObject.Adapter = new myGVItemAdapter(this, lstFlimStar);
                gvObject.ItemClick += new EventHandler<AdapterView.ItemClickEventArgs>(OnGridView_ItemClicked);*/
                #endregion






                db.Query<Pedidos>("delete from  [Pedidos]");
                db.Query<ConPedidos>("delete from  [ConPedidos]");
                db.Query<xLote>("delete from  [xLote]");
                db.Query<xLoteFinal>("delete from  [xLoteFinal]");
                db.Query<xprod>("delete from  [xprod]");
                mOp = "A";
                Toast.MakeText(this, "Modo Captura Activado", ToastLength.Short).Show();




            }
            else if (Convert.ToString(item.TitleFormatted) == "Consultar")
            {
                capturar.Enabled = false;

                pedido.Text = "";
                mOp = "C";

                ConfigurarRecyclerView();
                // Agrega esto después de ConfigurarRecyclerView()
                new Handler().PostDelayed(() =>
                {
                    var recyclerView = FindViewById<RecyclerView>(Resource.Id.gvCtrl);
                    if (recyclerView != null)
                    {
                        // Verificar si los hijos son visibles
                        for (int i = 0; i < recyclerView.ChildCount; i++)
                        {
                            var child = recyclerView.GetChildAt(i);
                            System.Diagnostics.Debug.WriteLine($"Hijo {i}: Visible={child.Visibility}, Height={child.Height}, Width={child.Width}");

                            // Pintar cada hijo de un color diferente
                            if (i == 0) child.SetBackgroundColor(Android.Graphics.Color.Red);
                            else if (i == 1) child.SetBackgroundColor(Android.Graphics.Color.Blue);
                        }
                    }
                }, 1000);
                //#region RECYCLERVIEW
                //// Obtener los datos
                //List<FlimStarInfo> lstFlimStar = detalle_pedido(folio.Trim(), "Individual");

                //// Obtener referencia al RecyclerView
                //var recyclerView = FindViewById<RecyclerView>(Resource.Id.gvCtrl);

                //// Configurar el LayoutManager (solo la primera vez)
                //if (recyclerView.GetLayoutManager() == null)
                //{
                //    var layoutManager = new LinearLayoutManager(this);
                //    recyclerView.SetLayoutManager(layoutManager);
                //}

                //// Obtener el adaptador actual
                //var adapter = recyclerView.GetAdapter() as MyRecyclerAdapter;

                //if (adapter != null)
                //{
                //    // Si ya existe un adaptador, actualizar los datos
                //    adapter.UpdateData(lstFlimStar);
                //}
                //else
                //{
                //    // Si no existe, crear uno nuevo
                //    adapter = new MyRecyclerAdapter(this, lstFlimStar);
                //    recyclerView.SetAdapter(adapter);

                //    // Manejar clics en items
                //    adapter.ItemClick += (sender, position) =>
                //    {
                //        var item = adapter.GetItem(position);
                //        OnRecyclerViewItemClicked(position, item);
                //    };
                //}
                //#endregion

                #region GRIDVIEW
                /*List<FlimStarInfo> lstFlimStar = detalle_pedido(folio.Trim(), "Individual");
                lstFlimStar.Clear();
                var gvObject = FindViewById<GridView>(Resource.Id.gvCtrl);
                gvObject.Adapter = new myGVItemAdapter(this, null);
                gvObject.Adapter = null;
                gvObject.Adapter = new myGVItemAdapter(this, lstFlimStar);
                gvObject.ItemClick += new EventHandler<AdapterView.ItemClickEventArgs>(OnGridView_ItemClicked);*/
                #endregion


                db.Query<Pedidos>("delete from  [Pedidos]");
                db.Query<ConPedidos>("delete from  [ConPedidos]");
                db.Query<xLote>("delete from  [xLote]");
                db.Query<xLoteFinal>("delete from  [xLoteFinal]");
                db.Query<xprod>("delete from  [xprod]");
                Toast.MakeText(this, "Modo Consulta Activado", ToastLength.Short).Show();
            }
            else if (Convert.ToString(item.TitleFormatted) == "Imprimir Split")
            {
                fnShowCustomAlertDialogImprimir();
            }
            else if (Convert.ToString(item.TitleFormatted) == "Cancelar")
            {
                fnShowCustomAlertDialog();
            }
            else if (Convert.ToString(item.TitleFormatted) == "Reasignar Orden")
            {
                fnShowCustomAlertDialogReasignar();
            }
            else if (Convert.ToString(item.TitleFormatted) == "Solicitar Producto")
            {
                //ReasignarSupervisor();
                if (pedido.Text.ToString().Trim().Length > 0)
                {
                    Intent intent = new Intent(this, typeof(productosolicitar));
                    intent.PutExtra("cvresponsable", cvresponsable.ToString().Trim());
                    intent.PutExtra("responsable", responsable.ToString().Trim());
                    intent.PutExtra("ordenventa", pedido.Text.ToString().Trim());
                    intent.PutExtra("currentVersionName", currentVersionName.ToString().Trim());
                    intent.PutExtra("imei", imei.ToString().Trim());
                    StartActivity(intent);
                }
                else
                {
                    #region MATERIAL DIALOG
                    // 🔹 Construimos el título con color rojo y negritas
                    var titleSpannable = new SpannableStringBuilder("Pedido Inexistente");
                    titleSpannable.SetSpan(new ForegroundColorSpan(Color.ParseColor("#DC3545")), 0, titleSpannable.Length(), SpanTypes.ExclusiveExclusive);
                    titleSpannable.SetSpan(new StyleSpan(TypefaceStyle.Bold), 0, titleSpannable.Length(), SpanTypes.ExclusiveExclusive);

                    // 🔹 Construimos el mensaje con color blanco
                    var mensajeSpannable = new SpannableStringBuilder("Debe ingresar un pedido para realizar este movimiento");
                    mensajeSpannable.SetSpan(new ForegroundColorSpan(Color.ParseColor("#FFFFFF")), 0, mensajeSpannable.Length(), SpanTypes.ExclusiveExclusive);

                    // 🔹 Creamos el diálogo usando el ThemeOverlay oficial de Material3
                    var builder = new MaterialAlertDialogBuilder(this, Resource.Style.ThemeOverlay_Material3_MaterialAlertDialog);
                    builder.SetTitle(titleSpannable);
                    builder.SetIcon(Resource.Drawable.no);
                    builder.SetMessage(mensajeSpannable);
                    builder.SetCancelable(false);

                    // 🔹 Botón principal
                    builder.SetPositiveButton("Entendido", (s, e) => { });

                    // 🔹 Crear y mostrar el diálogo
                    var dialog = builder.Create();
                    dialog.Show();

                    // 🔹 Personalizamos el botón después de mostrarlo
                    dialog.Window.DecorView.Post(() =>
                    {
                        var positiveButton = dialog.GetButton((int)DialogButtonType.Positive);
                        positiveButton?.SetTextColor(Color.ParseColor("#00695C")); // Verde Material
                        positiveButton?.SetAllCaps(false);
                    });
                    #endregion

                    pedido.Text = "";
                    pedido.RequestFocus();
                }

            }
            else if (Convert.ToString(item.TitleFormatted) == "Cerrar Sesion")
            {
                #region MATERIAL DIALOG — Cerrar Sesión (con colores dinámicos)
                /*// Obtener colores del tema actual (Material 3 dinámico)
                var primaryColorAttr = new TypedValue();
                var onSurfaceColorAttr = new TypedValue();
                var errorColorAttr = new TypedValue();

                Theme.ResolveAttribute(Resource.Attribute.colorPrimary, primaryColorAttr, true);
                Theme.ResolveAttribute(Resource.Attribute.colorOnSurface, onSurfaceColorAttr, true);
                Theme.ResolveAttribute(Resource.Attribute.colorError, errorColorAttr, true);

                var colorPrimary = new Android.Graphics.Color(primaryColorAttr.Data);
                var colorOnSurface = new Android.Graphics.Color(onSurfaceColorAttr.Data);
                var colorError = new Android.Graphics.Color(errorColorAttr.Data);

                // Construcción del título
                var titleSpannable = new SpannableStringBuilder("Cerrar Sesión");
                titleSpannable.SetSpan(new ForegroundColorSpan(colorPrimary), 0, titleSpannable.Length(), SpanTypes.ExclusiveExclusive);
                titleSpannable.SetSpan(new StyleSpan(TypefaceStyle.Bold), 0, titleSpannable.Length(), SpanTypes.ExclusiveExclusive);

                // Mensaje
                var mensajeSpannable = new SpannableStringBuilder("¿Desea cerrar su sesión en este equipo?");
                mensajeSpannable.SetSpan(new ForegroundColorSpan(colorOnSurface), 0, mensajeSpannable.Length(), SpanTypes.ExclusiveExclusive);

                // Crear el diálogo Material
                var builder = new MaterialAlertDialogBuilder(this, Resource.Style.ThemeOverlay_Material3_MaterialAlertDialog);
                builder.SetTitle(titleSpannable);
                builder.SetIcon(Resource.Drawable.question);
                builder.SetMessage(mensajeSpannable);
                builder.SetCancelable(false);
                builder.SetPositiveButton("Sí", SaveAction);
                builder.SetNegativeButton("No", CancelaAction);

                var dialog = builder.Create();
                dialog.Show();

                // Personalizar botones con los colores dinámicos
                dialog.Window.DecorView.Post(() =>
                {
                    var positiveButton = dialog.GetButton((int)DialogButtonType.Positive);
                    var negativeButton = dialog.GetButton((int)DialogButtonType.Negative);

                    positiveButton?.SetTextColor(colorPrimary);
                    positiveButton?.SetAllCaps(false);

                    negativeButton?.SetTextColor(colorError);
                    negativeButton?.SetAllCaps(false);
                });*/
                #endregion

                // Diálogo de confirmación
                DialogHelper.ShowConfirmDialog(this,
                    title: "Cerrar Sesión",
                    message: "¿Desea cerrar su sesión en este equipo?",
                    positiveText: "Sí",
                    negativeText: "No",
                    positiveAction: SaveAction,
                    negativeAction: CancelaAction);

            }
            return base.OnOptionsItemSelected(item);
        }//4915 6684 5489 8537
         //Roberto Monoy

        private void fnShowCustomAlertDialogImprimir()
        {
            Intent intent = new Intent(this, typeof(ImprimirSplit));
            intent.PutExtra("respimprimir", responsable.ToString().Trim());
            intent.PutExtra("responsable", responsable.ToString().Trim());
            intent.PutExtra("currentVersionName", currentVersionName.ToString().Trim());
            intent.PutExtra("imei", imei.ToString().Trim());
            StartActivityForResult(intent, PICK_CONTACT_REQUEST);
        }

        private void OnRecyclerViewItemClicked(int position, FlimStarInfo item)
        {
            if (item != null)
            {
                // Tu lógica original para manejar clics en items
                // Por ejemplo:
                Toast.MakeText(this, $"Seleccionado: {item.Name}", ToastLength.Short).Show();

                // Aquí va tu código original de OnGridView_ItemClicked
                // adaptado para usar 'item' y 'position'
            }
        }

        //Cargar conexion de base de datos sqlite
        private void LoadConnection()
        {
            string folder = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);
            string dbPath = System.IO.Path.Combine(folder, "Split_Trailer.db3");

            bool exist = File.Exists(dbPath);
            db = new SQLiteConnection(dbPath);



            if (!exist)
            {
                //Crea la tabla en base al modelo si es la primera vez
                db.CreateTable<Pedidos>();
                db.CreateTable<ConPedidos>();
                db.CreateTable<xLote>();
                db.CreateTable<xLoteFinal>();
                db.CreateTable<xprod>();
                db.CreateTable<Mensajes>();
                db.CreateTable<XLoteSug>();
            }
            else
            {
                try
                {
                    var querydif = db.Query<xprod>("Select tipo_captura FROM xprod");
                    foreach (var captu in querydif)
                    {
                        string x = captu.tipo_captura;
                    }

                }
                catch
                {
                    db.Query<xprod>("ALTER TABLE xprod ADD tipo_captura string");
                    db.Query<xLote>("ALTER TABLE xLote ADD tipo_captura string");
                }
            }
        }

        private void SaveAction(object sender, DialogClickEventArgs e)
        {
            thisConnection.Open();
            string cadena = "UPDATE tb_det_acceso_celulares SET estado = 'T' WHERE " +
                        "nom_usu = '" + responsable + "' AND sistema = 'SplitTrailer' AND folio = '' AND estado = 'A'";
            SqlCommand cmd = new SqlCommand(cadena, thisConnection);
            cmd.ExecuteNonQuery();
            thisConnection.Close();
            Finish();
        }

        private void CancelaAction(object sender, DialogClickEventArgs e)
        {
            return;
        }



        List<FlimStarInfo> ConsPed(string mped)
        {
            thisConnection.Open();
            listItem.Clear();
            string contenido = "";
            //thisConnection.Open();
            string cadena = "Select DISTINCT A.prod_clave from tb_det_split AS A  JOIN tb_cat_producto AS B ON A.prod_clave = B.prod_clave Where A.emb_folio = '" + pedido.Text.Trim() + "' AND A.estatus != 'C' Order by A.prod_clave";
            SqlDataAdapter da = new SqlDataAdapter(cadena, thisConnection);
            DataSet ds = new DataSet();
            da.Fill(ds, "ConsPed");
            DataTable ConsPed = ds.Tables["ConsPed"];

            foreach (DataRow Row in ConsPed.Rows)
            {
                string texto = "";
                string producto = "";
                string cadena2 = "Select A.no_lote, A.prod_clave, A.tarima, A.cajas, B.prod_nombre from tb_det_split AS A  JOIN tb_cat_producto AS B ON A.prod_clave = B.prod_clave Where A.emb_folio = '" + pedido.Text.Trim() + "' AND A.prod_clave = '" + Row["prod_clave"].ToString().Trim() + "' AND A.estatus != 'C' Order by A.tarima, A.prod_clave, A.no_lote";


                SqlDataAdapter dai = new SqlDataAdapter(cadena2, thisConnection);
                DataSet dsi = new DataSet();

                dai.Fill(dsi, "ConsPedi");
                DataTable ConsPedi = dsi.Tables["ConsPedi"];
                foreach (DataRow Rowi in ConsPedi.Rows)
                {
                    producto = Rowi["prod_nombre"].ToString().Trim();
                    texto = texto + "Lote: " + Rowi["no_lote"].ToString().Trim() + " Tarima: " + Rowi["tarima"].ToString().Trim() + " Surtido: " + Rowi["cajas"].ToString().Trim() + System.Environment.NewLine;

                }

                listItem.Add(new FlimStarInfo()
                {
                    Name = producto,
                    Age = texto,
                    ImageID = Resource.Drawable.producto
                });
            }


            //LbxCons.Font = new Font(LbxCons.Font.Name, 7);   ;
            thisConnection.Close();

            return listItem;
        }


        List<FlimStarInfo> detalle_pedidoNEW(string mped, string mov)
        {
            var resultado = new List<FlimStarInfo>(); // Lista local

            thisConnection.Open();

            if (mov != "Acumulado")
            {
                var query = db.Table<Pedidos>();
                foreach (var captu in query)
                {
                    if (captu.folio == mped)
                    {
                        resultado.Add(new FlimStarInfo()
                        {
                            Name = captu.nombre,
                            Age = "Pedidos: " + captu.pedido + " Surtidos: " + captu.surtido + " Faltante por Armar: " + (Convert.ToInt32(captu.pedido) - Convert.ToInt32(captu.surtido)),
                            ImageID = Resource.Drawable.producto
                        });
                    }
                }
            }
            else
            {
                var query = db.Table<Pedidos>();
                foreach (var captu in query)
                {
                    resultado.Add(new FlimStarInfo()
                    {
                        Name = captu.nombre,
                        Age = "Pedidos: " + captu.pedido + " Surtido: " + captu.surtido + " Faltante Por Armar: " + (Convert.ToInt32(captu.pedido) - Convert.ToInt32(captu.surtido)),
                        ImageID = Resource.Drawable.producto
                    });
                }
            }

            System.Diagnostics.Debug.WriteLine($"DEBUG: Se encontraron {resultado.Count} items");
            thisConnection.Close();
            return resultado;
        }
        List<FlimStarInfo> detalle_pedido(string mped, string mov)
        {
            thisConnection.Open();
            listItem.Clear();

            if (mov != "Acumulado")
            {


                var query = db.Table<Pedidos>();
                foreach (var captu in query)
                {
                    if (captu.folio == mped)
                    {
                        listItem.Add(new FlimStarInfo()
                        {
                            Name = captu.nombre,
                            Age = "Pedidos: " + captu.pedido + " Surtidos: " + captu.surtido + " Faltante por Armar: " + (Convert.ToInt32(captu.pedido) - Convert.ToInt32(captu.surtido)),
                            ImageID = Resource.Drawable.producto
                        });
                    }
                }

            }
            else
            {

                var query = db.Table<Pedidos>();
                foreach (var captu in query)
                {

                    listItem.Add(new FlimStarInfo()
                    {
                        Name = captu.nombre,
                        Age = "Pedidos: " + captu.pedido + " Surtido: " + captu.surtido + " Faltante Por Armar: " + (Convert.ToInt32(captu.pedido) - Convert.ToInt32(captu.surtido)),
                        ImageID = Resource.Drawable.producto
                    });

                }

            }

            //LbxCons.Font = new Font(LbxCons.Font.Name, 7);   ;
            thisConnection.Close();

            return listItem;
        }

        void Btnlogin_Click(object sender, EventArgs e)
        {
            //ConsPedSur(pedido.Text.ToString());
            Intent intent = new Intent(this, typeof(capturar_split));
            intent.PutExtra("cvresponsable", cvresponsable.ToString());
            intent.PutExtra("responsable", responsable.ToString());
            intent.PutExtra("diascadmin", diasmincarga.ToString());
            intent.PutExtra("currentVersionName", currentVersionName.ToString().Trim());
            intent.PutExtra("imei", imei.ToString().Trim());
            StartActivity(intent);

        }

        private string EstatusPed(string mped)
        {
            string valor = "";
            thisConnection.Open();
            string Cadena = "Select hora_fin from tb_mstr_embarque where emb_folio = '" + mped + "'";
            SqlCommand cmd = new SqlCommand(Cadena, thisConnection);
            valor = Convert.ToString(cmd.ExecuteScalar());
            thisConnection.Close();
            return valor;
        }

        private void ConsPedSur(string mped)
        {
            db.Query<ConPedidos>("UPDATE [ConPedidos] SET surtido = 0");
            thisConnection.Open();


            string Cadena = "Select SUM(a.pdn_num_unidades) AS Pedidos From tb_det_pedidos A, tb_Cat_producto B " +
                                "where a.pdn_folio = '" + mped.Trim() + "' and a.prod_clave = b.prod_clave";
            SqlCommand cmd = new SqlCommand(Cadena, thisConnection);
            int cantped = Convert.ToInt32(cmd.ExecuteScalar());



            string cadena = "Select * From tb_det_pedidos A, tb_Cat_producto B where a.pdn_folio = '" + pedido.Text.Trim() + "' and a.prod_clave = b.prod_clave";
            SqlDataAdapter da = new SqlDataAdapter(cadena, thisConnection);
            DataSet ds = new DataSet();
            da.Fill(ds, "ConsPed");
            var ConsPed = ds.Tables["ConsPed"];

            if (mped.Length > 0)
            {
                if (Convert.ToInt32(mped) < 300000)
                {
                    mped = "0" + Convert.ToInt32(mped).ToString();
                }
            }


            cadena = "Select prod_clave, sum(cajas) as cajas from tb_det_split Where emb_folio = '" + mped.ToString() + "'" +
                     " AND estatus != 'C' Group By prod_clave Order by prod_clave";
            da = new SqlDataAdapter(cadena, thisConnection);
            ds = new DataSet();
            da.Fill(ds, "PedSur");
            var PedSur = ds.Tables["PedSur"];
            int Cp = 0, Cs = 0, sur = 0;
            thisConnection.Close();
            foreach (DataRow Row in ConsPed.Rows)
            {
                sur = 0;
                foreach (DataRow row in PedSur.Select("prod_clave = '" + Row["prod_clave"].ToString() + "'"))
                    sur = Convert.ToInt32(row["Cajas"]);
                db.Query<Pedidos>("UPDATE [Pedidos] SET surtido = '" + sur + "' WHERE prod_clave = '" + Row["prod_clave"].ToString().Trim() + "'");
                db.Query<ConPedidos>("UPDATE [ConPedidos] SET surtido = '" + sur + "' WHERE prod_clave = '" + Row["prod_clave"].ToString().Trim() + "'");

                Cp += Convert.ToInt32(Row["pdn_num_unidades"]);
                Cs += sur;
            }

            //MODIFICACION SURTIDO EN LECTORA ANTERIOR
            cadena = "Select prod_clave, sum(cajas) as cajas from tb_det_embarque Where emb_folio = '" + mped.Trim() + "'" +
                     " AND estatus != 'C' and OpCap = 'N' Group By prod_clave Order by prod_clave";
            da = new SqlDataAdapter(cadena, thisConnection);
            ds = new DataSet();
            da.Fill(ds, "PedSurdetemb");
            var PedSuremb = ds.Tables["PedSurdetemb"];
            int Cpemb = 0, Csemb = 0, suremb = 0;
            thisConnection.Close();
            foreach (DataRow Row in ConsPed.Rows)
            {
                sur = 0;
                foreach (DataRow row in PedSuremb.Select("prod_clave = '" + Row["prod_clave"].ToString() + "'"))
                    sur = Convert.ToInt32(row["Cajas"]);
                db.Query<Pedidos>("UPDATE [Pedidos] SET surtido = surtido + " + sur + " WHERE prod_clave = '" + Row["prod_clave"].ToString().Trim() + "'");
                db.Query<ConPedidos>("UPDATE [ConPedidos] SET surtido = surtido + " + sur + " WHERE prod_clave = '" + Row["prod_clave"].ToString().Trim() + "'");

                Cp += Convert.ToInt32(Row["pdn_num_unidades"]);
                Cs += sur;
            }




            PedidosSurtidos.Text = "Pedidos: " + cantped + " Surtidos: " + Cs + " Faltante Por Armar: " + (Convert.ToInt32(cantped) - Convert.ToInt32(Cs));

            #region RECYCLERVIEW
            // Obtener los datos
            List<FlimStarInfo> lstFlimStar = detalle_pedido(folio.Trim(), "Individual");

            // Obtener referencia al RecyclerView
            var recyclerView = FindViewById<RecyclerView>(Resource.Id.gvCtrl);

            // Configurar el LayoutManager (solo la primera vez)
            if (recyclerView.GetLayoutManager() == null)
            {
                var layoutManager = new LinearLayoutManager(this);
                recyclerView.SetLayoutManager(layoutManager);
            }

            // Obtener el adaptador actual
            var adapter = recyclerView.GetAdapter() as MyRecyclerAdapter;

            if (adapter != null)
            {
                // Si ya existe un adaptador, actualizar los datos
                adapter.UpdateData(lstFlimStar);
            }
            else
            {
                // Si no existe, crear uno nuevo
                adapter = new MyRecyclerAdapter(this, lstFlimStar);
                recyclerView.SetAdapter(adapter);

                // Manejar clics en items
                adapter.ItemClick += (sender, position) =>
                {
                    var item = adapter.GetItem(position);
                    OnRecyclerViewItemClicked(position, item);
                };
            }
            #endregion
            #region GRIDVIEW
            /*List<FlimStarInfo> lstFlimStar = detalle_pedido(pedido.Text.Trim(), "Acumulado");
            var gvObject = FindViewById<GridView>(Resource.Id.gvCtrl);
            gvObject.Adapter = new myGVItemAdapter(this, lstFlimStar);
            gvObject.ItemClick += new EventHandler<AdapterView.ItemClickEventArgs>(OnGridView_ItemClicked); //detalle_pedido*/
            #endregion
            //RECORRIDO SI HAY PRODUCTO CAPTURADO
            var quex = db.Table<xprod>();
            foreach (var captu in quex)
            {
                db.Query<Pedidos>("UPDATE [Pedidos] SET surtido = surtido + " + 1 + " WHERE prod_clave = '" + captu.Codigo.ToString().Trim() + "'");
                db.Query<ConPedidos>("UPDATE [ConPedidos] SET surtido = surtido + " + 1 + " WHERE prod_clave = '" + captu.Codigo.ToString().Trim() + "'");
            }

            //***********************************


            var querydif = db.Query<ConPedidos>("Select * FROM ConPedidos WHERE surtido > pedido");
            foreach (var captu in querydif)
            {
                DialogHelper.ShowWarningDialog(this,
                    message: $"El pedido: {folio} contiene un desfase en el producto {captu.nombre}, Surtido: {captu.surtido} / Pedido: {captu.pedido}; Favor de ingresar a Cancelación Parcial y ajustar la diferencia.",
                    positiveText: "Entendido");
                #region MATERIAL DIALOG
                /*// 🔹 Construimos el título con color naranja y negritas
                var titleSpannable = new SpannableStringBuilder("Se ha detectado un Cambio en la orden");
                titleSpannable.SetSpan(new ForegroundColorSpan(Color.ParseColor("#F96810")), 0, titleSpannable.Length(), SpanTypes.ExclusiveExclusive);
                titleSpannable.SetSpan(new StyleSpan(TypefaceStyle.Bold), 0, titleSpannable.Length(), SpanTypes.ExclusiveExclusive);

                // 🔹 Construimos el mensaje con colores y partes en negritas
                var mensajeSpannable = new SpannableStringBuilder();
                mensajeSpannable.Append("El pedido: ");
                int startFolio = mensajeSpannable.Length();
                mensajeSpannable.Append(folio);
                mensajeSpannable.SetSpan(new StyleSpan(TypefaceStyle.Bold), startFolio, mensajeSpannable.Length(), SpanTypes.ExclusiveExclusive);

                mensajeSpannable.Append(" contiene un desfase en el producto ");
                int startNombre = mensajeSpannable.Length();
                mensajeSpannable.Append(captu.nombre);
                mensajeSpannable.SetSpan(new StyleSpan(TypefaceStyle.Bold), startNombre, mensajeSpannable.Length(), SpanTypes.ExclusiveExclusive);

                mensajeSpannable.Append(", Surtido: ");
                int startSurtido = mensajeSpannable.Length();
                mensajeSpannable.Append((char)captu.surtido);
                mensajeSpannable.SetSpan(new StyleSpan(TypefaceStyle.Bold), startSurtido, mensajeSpannable.Length(), SpanTypes.ExclusiveExclusive);

                mensajeSpannable.Append(" / Pedido: ");
                int startPedido = mensajeSpannable.Length();
                mensajeSpannable.Append((char)captu.pedido);
                mensajeSpannable.SetSpan(new StyleSpan(TypefaceStyle.Bold), startPedido, mensajeSpannable.Length(), SpanTypes.ExclusiveExclusive);

                mensajeSpannable.Append("; Favor de ingresar a Cancelación Parcial y ajustar la diferencia.");
                mensajeSpannable.SetSpan(new ForegroundColorSpan(Color.ParseColor("#FFD14B")), 0, mensajeSpannable.Length(), SpanTypes.ExclusiveExclusive);

                // 🔹 Creamos el diálogo usando el ThemeOverlay oficial de Material3
                var builder = new MaterialAlertDialogBuilder(this, Resource.Style.ThemeOverlay_Material3_MaterialAlertDialog);
                builder.SetTitle(titleSpannable);
                builder.SetIcon(Resource.Drawable.no);
                builder.SetMessage(mensajeSpannable);
                builder.SetCancelable(false);

                // 🔹 Botón principal
                builder.SetPositiveButton("Entendido", (s, e) => { });

                // 🔹 Crear y mostrar el diálogo
                var dialog = builder.Create();
                dialog.Show();

                // 🔹 Personalizamos el botón después de mostrarlo
                dialog.Window.DecorView.Post(() =>
                {
                    var positiveButton = dialog.GetButton((int)DialogButtonType.Positive);
                    positiveButton?.SetTextColor(Color.ParseColor("#FF8C00")); // Naranja Material
                    positiveButton?.SetAllCaps(false);
                });*/
                #endregion
            }

        }

        // ========== MÉTODO fnShowCustomAlertDialog (diálogo de cancelación) ==========
        void fnShowCustomAlertDialog()
        {
            // Inflamos la vista personalizada
            View view = LayoutInflater.Inflate(Resource.Layout.frmsupervisor, null);
            EditText password = view.FindViewById<EditText>(Resource.Id.txtPassword);
            Button buttonaceptar = view.FindViewById<Button>(Resource.Id.btnLoginLL);
            Button button = view.FindViewById<Button>(Resource.Id.btnClearLL);

            // Creamos el diálogo con MaterialAlertDialogBuilder y vista personalizada
            var builder = new MaterialAlertDialogBuilder(this, Resource.Style.ThemeOverlay_Material3_MaterialAlertDialog);
            builder.SetView(view);
            builder.SetCancelable(false);

            // Como queremos colores dinámicos en el título, lo personalizamos
            var colorPrimary = ThemeHelper.GetColorFromTheme(this, Resource.Attribute.colorPrimary);
            var titleSpannable = new SpannableStringBuilder("Autorización de Cancelación");
            titleSpannable.SetSpan(new ForegroundColorSpan(colorPrimary), 0, titleSpannable.Length(), SpanTypes.ExclusiveExclusive);
            titleSpannable.SetSpan(new StyleSpan(TypefaceStyle.Bold), 0, titleSpannable.Length(), SpanTypes.ExclusiveExclusive);
            builder.SetTitle(titleSpannable);

            var dialog = builder.Create();

            // Eventos de los botones
            button.Click += delegate
            {
                dialog.Dismiss();
            };

            buttonaceptar.Click += delegate
            {
                thisConnection.Open();
                string cadena = "Select usuario From tb_Autoriza_OdeP Where clave = 'EM' and password = '" + password.Text.Trim() + "' AND Obs = 'C'";
                SqlCommand cmd = new SqlCommand(cadena, thisConnection);
                var mAutoriza = Convert.ToString(cmd.ExecuteScalar());

                if (string.IsNullOrWhiteSpace(mAutoriza))
                {
                    string cadenax = "Select CONCAT (nom_capsplit, '*', status_parcial) From tb_Respon_Split Where cve_cancel = '" + password.Text.Trim() + "' AND status = 'A'";
                    SqlCommand cmdx = new SqlCommand(cadenax, thisConnection);
                    var mAutorizax = Convert.ToString(cmdx.ExecuteScalar());

                    if (string.IsNullOrWhiteSpace(mAutorizax))
                    {
                        Toast.MakeText(this, "USUARIO Y PASSWORD INCORRECTO!!!", ToastLength.Short).Show();
                        thisConnection.Close();
                    }
                    else
                    {
                        string[] separadas = mAutorizax.Split('*');
                        thisConnection.Close();
                        Intent intent = new Intent(this, typeof(CancelarSplit));
                        intent.PutExtra("respcancel", separadas[0].Trim());
                        intent.PutExtra("cvresponsable", cvresponsable.Trim());
                        intent.PutExtra("responsable", responsable.Trim());
                        intent.PutExtra("Parcial", separadas[1].Trim());
                        intent.PutExtra("currentVersionName", currentVersionName.Trim());
                        intent.PutExtra("imei", imei.Trim());
                        StartActivityForResult(intent, PICK_CONTACT_REQUEST);
                        dialog.Dismiss();
                    }
                }
                else
                {
                    thisConnection.Close();
                    Intent intent = new Intent(this, typeof(CancelarSplit));
                    intent.PutExtra("respcancel", mAutoriza.Trim());
                    intent.PutExtra("cvresponsable", cvresponsable.Trim());
                    intent.PutExtra("responsable", responsable.Trim());
                    intent.PutExtra("Parcial", "B");
                    intent.PutExtra("currentVersionName", currentVersionName.Trim());
                    intent.PutExtra("imei", imei.Trim());
                    StartActivityForResult(intent, PICK_CONTACT_REQUEST);
                    dialog.Dismiss();
                }
            };

            dialog.Show();

            // Personalizar botones del diálogo principal (los que no están en el layout)
            dialog.Window.DecorView.Post(() =>
            {
                // No hay botones Positive/Negative en el diálogo porque usamos vista personalizada,
                // pero podemos personalizar los botones de la vista si es necesario.
            });
        }
        void fnShowCustomAlertDialogLEGACY()
        {
            #region MATERIAL DIALOG
            // Infla la vista personalizada
            View view = LayoutInflater.Inflate(Resource.Layout.frmsupervisor, null);

            // Crea el diálogo usando Material 3 y tu estilo personalizado
            var builder = new MaterialAlertDialogBuilder(this, Resource.Style.AppTheme_CustomAlertDialog);
            builder.SetView(view);
            builder.SetCancelable(false);

            // Crea el diálogo final
            var dialog = builder.Create();

            // Busca los elementos dentro de la vista
            EditText password = view.FindViewById<EditText>(Resource.Id.txtPassword);
            Button buttonaceptar = view.FindViewById<Button>(Resource.Id.btnLoginLL);
            Button button = view.FindViewById<Button>(Resource.Id.btnClearLL);

            // Botón de cancelar/cerrar
            button.Click += delegate
            {
                dialog.Dismiss();
            };

            // Botón de aceptar
            buttonaceptar.Click += delegate
            {
                thisConnection.Open();
                string cadena = "Select usuario From tb_Autoriza_OdeP Where clave = 'EM' and password = '" + password.Text.Trim() + "' AND Obs = 'C'";
                SqlCommand cmd = new SqlCommand(cadena, thisConnection);
                var mAutoriza = Convert.ToString(cmd.ExecuteScalar());

                if (string.IsNullOrWhiteSpace(mAutoriza))
                {
                    string cadenax = "Select CONCAT (nom_capsplit, '*', status_parcial) From tb_Respon_Split Where cve_cancel = '" + password.Text.Trim() + "' AND status = 'A'";
                    SqlCommand cmdx = new SqlCommand(cadenax, thisConnection);
                    var mAutorizax = Convert.ToString(cmdx.ExecuteScalar());

                    if (string.IsNullOrWhiteSpace(mAutorizax))
                    {
                        Toast.MakeText(this, "USUARIO Y PASSWORD INCORRECTO!!!", ToastLength.Short).Show();
                        thisConnection.Close();
                    }
                    else
                    {
                        string[] separadas = mAutorizax.Split('*');
                        thisConnection.Close();

                        Intent intent = new Intent(this, typeof(CancelarSplit));
                        intent.PutExtra("respcancel", separadas[0].Trim());
                        intent.PutExtra("cvresponsable", cvresponsable.Trim());
                        intent.PutExtra("responsable", responsable.Trim());
                        intent.PutExtra("Parcial", separadas[1].Trim());
                        intent.PutExtra("currentVersionName", currentVersionName.Trim());
                        intent.PutExtra("imei", imei.Trim());
                        StartActivityForResult(intent, PICK_CONTACT_REQUEST);
                        dialog.Dismiss();
                    }
                }
                else
                {
                    thisConnection.Close();
                    Intent intent = new Intent(this, typeof(CancelarSplit));
                    intent.PutExtra("respcancel", mAutoriza.Trim());
                    intent.PutExtra("cvresponsable", cvresponsable.Trim());
                    intent.PutExtra("responsable", responsable.Trim());
                    intent.PutExtra("Parcial", "B");
                    intent.PutExtra("currentVersionName", currentVersionName.Trim());
                    intent.PutExtra("imei", imei.Trim());
                    StartActivityForResult(intent, PICK_CONTACT_REQUEST);
                    dialog.Dismiss();
                }
            };

            // Muestra el diálogo
            dialog.Show();

            #endregion
            //Inflate layout
            #region ALERT DIALOG
            /*View view = LayoutInflater.Inflate(Resource.Layout.frmsupervisor, null);
            Android.App.AlertDialog builder = new Android.App.AlertDialog.Builder(this).Create();
            builder.SetView(view);
            builder.SetCanceledOnTouchOutside(false);
            EditText password = view.FindViewById<EditText>(Resource.Id.txtPassword);
            Button buttonaceptar = view.FindViewById<Button>(Resource.Id.btnLoginLL);
            Button button = view.FindViewById<Button>(Resource.Id.btnClearLL);
            button.Click += delegate
            {
                builder.Dismiss();

            };
            buttonaceptar.Click += delegate
            {
                thisConnection.Open();
                string cadena = "Select usuario From tb_Autoriza_OdeP Where clave = 'EM' and password = '" + password.Text.Trim() + "' AND Obs = 'C'";
                SqlCommand cmd = new SqlCommand(cadena, thisConnection);
                var mAutoriza = Convert.ToString(cmd.ExecuteScalar());
                if (mAutoriza.Trim().Length == 0)
                {

                    string cadenax = "Select CONCAT (nom_capsplit, '*', status_parcial)  From tb_Respon_Split Where cve_cancel = '" + password.Text.Trim() + "' AND status = 'A'";
                    SqlCommand cmdx = new SqlCommand(cadenax, thisConnection);
                    var mAutorizax = Convert.ToString(cmdx.ExecuteScalar());
                    if (mAutorizax.Trim().Length == 0)
                    {
                        Toast.MakeText(this, "USUARIO Y PASSWORD INCORRECTO!!!", ToastLength.Short).Show();
                        thisConnection.Close();
                    }
                    else
                    {
                        string[] separadas;
                        separadas = mAutorizax.Split('*');

                        thisConnection.Close();
                        Intent intent = new Intent(this, typeof(CancelarSplit));
                        intent.PutExtra("respcancel", separadas[0].ToString().Trim());
                        intent.PutExtra("cvresponsable", cvresponsable.ToString().Trim());
                        intent.PutExtra("responsable", responsable.ToString().Trim());
                        intent.PutExtra("Parcial", separadas[1].ToString().Trim());
                        intent.PutExtra("currentVersionName", currentVersionName.ToString().Trim());
                        intent.PutExtra("imei", imei.ToString().Trim());
                        StartActivityForResult(intent, PICK_CONTACT_REQUEST);
                        builder.Dismiss();
                    }

                }
                else
                {
                    thisConnection.Close();
                    Intent intent = new Intent(this, typeof(CancelarSplit));
                    intent.PutExtra("respcancel", mAutoriza.ToString().Trim());
                    intent.PutExtra("cvresponsable", cvresponsable.ToString().Trim());
                    intent.PutExtra("responsable", responsable.ToString().Trim());
                    intent.PutExtra("Parcial", "B");
                    intent.PutExtra("currentVersionName", currentVersionName.ToString().Trim());
                    intent.PutExtra("imei", imei.ToString().Trim());
                    StartActivityForResult(intent, PICK_CONTACT_REQUEST);
                    builder.Dismiss();
                }

            };
            builder.Show();*/
            #endregion
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            Cancelado = string.Empty;

            if (requestCode == PICK_CONTACT_REQUEST && resultCode == Result.Ok)
            {
                var uri = data.Data;

                if (pedido.Text.Length > 0)
                {
                    string resultado = data.GetStringExtra("pedido_cancelar");
                    if (pedido.Text.Trim() == resultado.Trim())
                    {
                        ConsPedSur(pedido.Text.ToString());
                        Toast.MakeText(this, "Actualizacion de Pedido Exitoso", ToastLength.Short).Show();
                    }
                }

            }

        }


        void ReasignarSupervisor()
        {
            //Inflate layout
            View view = LayoutInflater.Inflate(Resource.Layout.reasignar, null);
            Android.App.AlertDialog builder = new Android.App.AlertDialog.Builder(this).Create();
            builder.SetView(view);
            builder.SetCanceledOnTouchOutside(false);
            EditText password = view.FindViewById<EditText>(Resource.Id.txtPassword);
            Button buttonaceptar = view.FindViewById<Button>(Resource.Id.btnLoginLL);
            Button button = view.FindViewById<Button>(Resource.Id.btnClearLL);
            button.Click += delegate
            {
                builder.Dismiss();

            };
            buttonaceptar.Click += delegate
            {
                thisConnection.Open();
                string cadena = "Select usuario From tb_Autoriza_OdeP Where clave = 'EM' and password = '" + password.Text.Trim() + "' AND Obs = 'C'";
                SqlCommand cmd = new SqlCommand(cadena, thisConnection);
                var mAutoriza = Convert.ToString(cmd.ExecuteScalar());
                if (mAutoriza.Trim().Length == 0)
                {

                    string cadenax = "Select CONCAT (nom_capsplit, '*', status_parcial)  From tb_Respon_Split Where cve_cancel = '" + password.Text.Trim() + "' AND status = 'A'";
                    SqlCommand cmdx = new SqlCommand(cadenax, thisConnection);
                    var mAutorizax = Convert.ToString(cmdx.ExecuteScalar());
                    if (mAutorizax.Trim().Length == 0)
                    {
                        Toast.MakeText(this, "USUARIO Y PASSWORD INCORRECTO!!!", ToastLength.Short).Show();
                        thisConnection.Close();
                    }
                    else
                    {
                        string[] separadas;
                        separadas = mAutorizax.Split('*');

                        thisConnection.Close();
                        Intent intent = new Intent(this, typeof(CancelarSplit));
                        intent.PutExtra("respcancel", separadas[0].ToString().Trim());
                        intent.PutExtra("cvresponsable", cvresponsable.ToString().Trim());
                        intent.PutExtra("responsable", responsable.ToString().Trim());
                        intent.PutExtra("Parcial", separadas[1].ToString().Trim());
                        intent.PutExtra("currentVersionName", currentVersionName.ToString().Trim());
                        intent.PutExtra("imei", imei.ToString().Trim());
                        StartActivityForResult(intent, PICK_CONTACT_REQUEST);
                        builder.Dismiss();
                    }

                }
                else
                {
                    thisConnection.Close();
                    Intent intent = new Intent(this, typeof(CancelarSplit));
                    intent.PutExtra("respcancel", mAutoriza.ToString().Trim());
                    intent.PutExtra("cvresponsable", cvresponsable.ToString().Trim());
                    intent.PutExtra("responsable", responsable.ToString().Trim());
                    intent.PutExtra("Parcial", "B");
                    intent.PutExtra("currentVersionName", currentVersionName.ToString().Trim());
                    intent.PutExtra("imei", imei.ToString().Trim());
                    StartActivityForResult(intent, PICK_CONTACT_REQUEST);
                    builder.Dismiss();
                }

            };
            builder.Show();
        }

        void llenarpedidos()
        {
            //aGREGAR COLUMNAS AL DATATABLE
            thisConnection.Open();
            Pedidostotales.Clear();
            //Buscar el numero del que parte si es campo o proceso
            cmnd = thisConnection.CreateCommand();
            query = "SELECT GETDATE()";
            SqlCommand cmd = new SqlCommand(query, thisConnection);
            string FECHAEmb = Convert.ToDateTime(cmd.ExecuteScalar()).ToString("dd/MM/yyyy");

            //query = "SELECT A.PDN_FOLIO, (CASE WHEN MT.no_trailer is not null  OR MT.no_trailer != '' THEN 'AMARILLO' WHEN A.pdn_estatus = ' ' THEN 'ROJO'  ELSE 'AZUL' END) as ESTADOPDN, A.pdn_sumsplit, nom_usu FROM TB_MSTR_PEDIDOS_NAL A LEFT JOIN tb_mstr_trailer MT On  A.placacaja = MT.no_trailer AND fecha = '" + FECHAEmb + "'  LEFT JOIN tb_det_acceso_celulares ON folio = A.PDN_FOLIO AND estado = 'A' WHERE A.PDN_SUMSPLIT > '0.9' AND A.prov_clave NOT IN ('MRLUCKY', 'PC') and PDN_FECHA = '" + FECHAEmb + "' AND PDN_SURTIDO = ' ' AND PDN_ESTATUS <> 'C' AND CNTE_CLAVE <> 'AJUST' AND CNTE_CLAVE <> 'BASUR' AND CNTE_CLAVE <> 'PERDI' AND CNTE_CLAVE <> 'VMEN1' OR (PDN_TIPO = 'TRA' AND PDN_SITUACION <> 'MAQ')  AND A.pdn_surtido = '' ORDER BY estadopdn";

            query = "SELECT A.PDN_FOLIO, A.pdn_sumsplit, nom_usu, A.PDN_HORENT1, (CASE WHEN convert(datetime, concat(CONVERT(VARCHAR(10), GETDATE(), 103), ' ', PDN_HORENT1)) < convert(datetime, concat(CONVERT(VARCHAR(10), GETDATE(), 103), ' ', '07:00')) THEN convert(datetime, concat(CONVERT(VARCHAR(10), DATEADD(DAY, 1, GETDATE()), 103), ' ', PDN_HORENT1)) ELSE convert(datetime, concat(CONVERT(VARCHAR(10), GETDATE(), 103), ' ', PDN_HORENT1)) END) as Fecha1  FROM tb_mstr_pedidos_nal A LEFT JOIN tb_det_acceso_celulares ON folio = A.PDN_FOLIO AND estado = 'A' WHERE A.PDN_SUMSPLIT > '0.9' AND A.prov_clave NOT IN ('MRLUCKY', 'PC') and PDN_FECHA = '" + FECHAEmb + "' AND PDN_SURTIDO = ' ' AND PDN_ESTATUS <> 'C' AND CNTE_CLAVE <> 'AJUST' AND CNTE_CLAVE <> 'BASUR' AND CNTE_CLAVE <> 'PERDI' AND CNTE_CLAVE <> 'VMEN1' OR (PDN_TIPO = 'TRA' AND PDN_SITUACION <> 'MAQ')  AND A.pdn_surtido = '' ORDER BY fECHA1";

            SqlDataAdapter daq = new SqlDataAdapter(query, thisConnection);
            DataSet dsq = new DataSet();
            daq.Fill(dsq, "Pedidostotales");
            Pedidostotales = dsq.Tables["Pedidostotales"];
            thisConnection.Close();
        }


        string ValidarColores(string pedidoactual)
        {
            int AnteriorRojas = 0;
            int AnteriorAmarillas = 0;
            int AnteriorAzules = 0;
            int encontrado = 0;

            string colorpedido = "";
            foreach (DataRow row in Pedidostotales.Rows)
            {
                if (Convert.ToDecimal(row["pdn_sumsplit"].ToString()) > 0)
                {
                    if (pedidoactual != row["PDN_FOLIO"].ToString())
                    {
                        if (row["ESTADOPDN"].ToString().Trim() == "AMARILLO" && row["nom_usu"].ToString().Trim().Length == 0)
                        {
                            AnteriorAmarillas++;
                        }
                        else if (row["ESTADOPDN"].ToString().Trim() == "AZUL" && row["nom_usu"].ToString().Trim().Length == 0)
                        {
                            AnteriorAzules++;
                        }
                        else
                        {
                            AnteriorRojas++;
                        }

                    }
                    else
                    {
                        encontrado = 1;
                        colorpedido = row["ESTADOPDN"].ToString().Trim();
                        break;
                    }
                }
            }

            if (encontrado == 1)
            {
                if (AnteriorAmarillas > 0)
                {
                    if (colorpedido != "AMARILLO")
                    {
                        return "No se puede cargar el pedido porque existen " + AnteriorAmarillas + " AMARILLOS Pedidos Disponibles";
                    }
                    else
                    {
                        return "1";
                    }
                }
                else if (AnteriorAzules > 0)
                {
                    if (colorpedido == "ROJO")
                    {
                        return "No se puede cargar el pedido porque existen " + AnteriorAzules + " Pedidos AZULES Disponibles";
                    }
                    else
                    {
                        return "1";
                    }

                }
                else
                {
                    return "1";
                }
            }
            else
            {

                return "1";
            }
        }



        string ValidarHoras(string pedidoactual)
        {
            //Traer Ultimo Pedido Agregado
            int ultimopedido = 0;
            string Pedidoembarque = "0";
            string Tipopedido = "NAL";
            string pedidopendiente = "1";
            thisConnection.Open();
            string Cadena = "Select top(1) isNull(folio, 0) from tb_det_acceso_celulares where nom_usu = '" + responsable.Trim() + "' AND estado = 'A' AND Folio != '' ORDER bY fECHA desc";
            SqlCommand cmdx = new SqlCommand(Cadena, thisConnection);
            ultimopedido = Convert.ToInt32(cmdx.ExecuteScalar());
            Pedidoembarque = ultimopedido.ToString();
            thisConnection.Close();

            if (ultimopedido == 0 || Convert.ToInt32(ultimopedido) == Convert.ToInt32(pedido.Text.Trim()))
            {
                pedidopendiente = "1";
            }
            else
            {
                if (pedido.Text.Length > 0)
                {
                    if (Convert.ToInt32(ultimopedido) < 400000)
                    {
                        Tipopedido = "EXP";
                        Pedidoembarque = "0" + Convert.ToInt32(ultimopedido);
                    }
                }
                int faltanteporsurtir = 0;

                thisConnection.Open();
                string cadena = "Select A.prod_clave, A.pdn_num_unidades, ((SELECT isNull(SUM(cajas), 0) FROM tb_det_embarque WHERE emb_folio = '" + Pedidoembarque + "' AND prod_clave = A.prod_clave AND OpCap = 'N' AND Estatus = 'A' AND emb_Tipo = '" + Tipopedido + "') +  (SELECT isNull(SUM(cajas), 0) FROM tb_det_split WHERE emb_folio = '" + Pedidoembarque + "' AND prod_clave = A.prod_clave AND Estatus != 'C' AND emb_Tipo = '" + Tipopedido + "')) AS surtido, (SELECT COUNT(producto) from tb_det_sol_producto WHERE ord_vent = '" + Pedidoembarque + "' AND producto = A.prod_clave) AS solicitud FROM tb_det_pedidos A WHERE pdn_folio = '" + ultimopedido + "' AND pdn_Tipo = '" + Tipopedido + "'";
                SqlDataAdapter adapterInfo = new SqlDataAdapter(cadena, thisConnection);
                DataSet setinfo = new DataSet();
                adapterInfo.Fill(setinfo, "ConsPed");
                var informacion = setinfo.Tables["ConsPed"];
                thisConnection.Close();
                foreach (DataRow Row in informacion.Rows)
                {
                    faltanteporsurtir = (Convert.ToInt32(Row["pdn_num_unidades"].ToString().Trim().Replace(".000", "")) - (Convert.ToInt32(Row["surtido"].ToString().Trim())));
                    if (faltanteporsurtir < 30 && faltanteporsurtir > 0)
                    {
                        if (Convert.ToInt32(Row["solicitud"].ToString().Trim()) == 0)
                        {
                            pedidopendiente = "0";
                        }
                    }
                }
            }


            int econtrado2 = 0;
            DateTime horapedido2 = DateTime.Now;

            DataRow[] foundRows2;

            foundRows2 = Pedidostotales.Select("PDN_FOLIO = '" + ultimopedido + "'");

            // Print column 0 of each returned row.
            for (int i = 0; i < foundRows2.Length; i++)
            {
                econtrado2 = 1;
                horapedido2 = Convert.ToDateTime(foundRows2[i][4]);
            }



            int Anteriores = 0;
            string horaanterior = "";
            int encontrado = 0;
            int encontrado1 = 0;
            DateTime horapedido = DateTime.Now;
            DataRow[] foundRows;

            foundRows = Pedidostotales.Select("PDN_FOLIO = '" + pedidoactual + "'");

            // Print column 0 of each returned row.
            for (int i = 0; i < foundRows.Length; i++)
            {
                encontrado1 = 1;
                horapedido = Convert.ToDateTime(foundRows[i][4]);
            }

            List<string> listPedidosPendientes = new List<string>();

            if (encontrado1 > 0)
            {
                foreach (DataRow row in Pedidostotales.Rows)
                {
                    if (Convert.ToDecimal(row["pdn_sumsplit"].ToString()) > 0)
                    {
                        if (pedidoactual != row["PDN_FOLIO"].ToString())
                        {
                            if (horapedido > Convert.ToDateTime(row["fecha1"]) && row["nom_usu"].ToString().Trim().Length == 0)
                            {
                                Anteriores++;
                                horaanterior = Convert.ToDateTime(row["fecha1"]).ToString();
                                string emb_folio = row["pdn_folio"].ToString();
                                listPedidosPendientes.Add(emb_folio);
                            }
                        }
                        else
                        {
                            encontrado = 1;
                            break;
                        }
                    }
                }
            }
            string validarestos = "";
            thisConnection.Open();
            string cadenax = "Select validar_restos From tb_Respon_Split Where nom_capsplit = '" + responsable.Trim() + "' AND status = 'A'";
            SqlCommand cmdx1 = new SqlCommand(cadenax, thisConnection);
            validarestos = Convert.ToString(cmdx1.ExecuteScalar());
            thisConnection.Close();



            string emb_folioPEndiente = string.Join(", ", listPedidosPendientes);


            if (encontrado == 1)
            {
                if (Anteriores > 0)
                {
                    return "No se puede cargar el pedido porque existen los siguientes Pedidos " + emb_folioPEndiente + " pendientes de armado con hora maxima de carga de las " + horaanterior + ".\n";
                }
                else
                {
                    if (econtrado2 == 1 && validarestos.Length > 0)
                    {
                        if (horapedido > horapedido2 && pedidopendiente == "0")
                        {
                            return "No puede Cambiar de Orden Hasta concluir los split Faltantes de esta orden: " + Pedidoembarque + "";
                        }
                        else
                        {
                            return "1";
                        }
                    }
                    else
                    {
                        return "1";
                    }
                }
            }
            else
            {
                return "1";
            }
        }

        // ========== MÉTODO fnShowCustomAlertDialogReasignar ==========
        void fnShowCustomAlertDialogReasignar()
        {
            View view = LayoutInflater.Inflate(Resource.Layout.frmsupervisor, null);
            EditText password = view.FindViewById<EditText>(Resource.Id.txtPassword);
            Button buttonaceptar = view.FindViewById<Button>(Resource.Id.btnLoginLL);
            Button button = view.FindViewById<Button>(Resource.Id.btnClearLL);

            var builder = new MaterialAlertDialogBuilder(this, Resource.Style.ThemeOverlay_Material3_MaterialAlertDialog);
            builder.SetView(view);
            builder.SetCancelable(false);

            var colorPrimary = ThemeHelper.GetColorFromTheme(this, Resource.Attribute.colorPrimary);
            var titleSpannable = new SpannableStringBuilder("Autorización de Reasignación");
            titleSpannable.SetSpan(new ForegroundColorSpan(colorPrimary), 0, titleSpannable.Length(), SpanTypes.ExclusiveExclusive);
            titleSpannable.SetSpan(new StyleSpan(TypefaceStyle.Bold), 0, titleSpannable.Length(), SpanTypes.ExclusiveExclusive);
            builder.SetTitle(titleSpannable);

            var dialog = builder.Create();

            button.Click += delegate
            {
                dialog.Dismiss();
            };

            buttonaceptar.Click += delegate
            {
                thisConnection.Open();
                string cadenax = "Select nom_capsplit From tb_Respon_Split Where cve_cancel = '" + password.Text.Trim() + "' AND status = 'A'";
                SqlCommand cmdx = new SqlCommand(cadenax, thisConnection);
                var mAutorizax = Convert.ToString(cmdx.ExecuteScalar());

                if (mAutorizax.Trim().Length == 0)
                {
                    Toast.MakeText(this, "USUARIO Y PASSWORD INCORRECTO!!!", ToastLength.Short).Show();
                    thisConnection.Close();
                }
                else
                {
                    if (mAutorizax.Trim() == responsable.Trim())
                    {
                        thisConnection.Close();
                        Toast.MakeText(this, "No puede Asignarse Ordenes a usted mismo", ToastLength.Long).Show();
                    }
                    else
                    {
                        thisConnection.Close();
                        Intent intent = new Intent(this, typeof(reasignarterminar));
                        intent.PutExtra("respreasig", mAutorizax.ToString().Trim());
                        intent.PutExtra("cvresponsable", cvresponsable.ToString().Trim());
                        intent.PutExtra("responsable", responsable.ToString().Trim());
                        intent.PutExtra("currentVersionName", currentVersionName.ToString().Trim());
                        intent.PutExtra("imei", imei.ToString().Trim());
                        StartActivityForResult(intent, PICK_CONTACT_REQUEST);
                        dialog.Dismiss();
                    }
                }
            };

            dialog.Show();
        }
        void fnShowCustomAlertDialogReasignarLEGACY()
        {
            //Inflate layout
            View view = LayoutInflater.Inflate(Resource.Layout.frmsupervisor, null);
            Android.App.AlertDialog builder = new Android.App.AlertDialog.Builder(this).Create();
            builder.SetView(view);
            builder.SetCanceledOnTouchOutside(false);
            EditText password = view.FindViewById<EditText>(Resource.Id.txtPassword);
            Button buttonaceptar = view.FindViewById<Button>(Resource.Id.btnLoginLL);
            Button button = view.FindViewById<Button>(Resource.Id.btnClearLL);
            button.Click += delegate
            {
                builder.Dismiss();

            };
            buttonaceptar.Click += delegate
            {
                thisConnection.Open();
                string cadenax = "Select nom_capsplit From tb_Respon_Split Where cve_cancel = '" + password.Text.Trim() + "' AND status = 'A'";
                SqlCommand cmdx = new SqlCommand(cadenax, thisConnection);
                var mAutorizax = Convert.ToString(cmdx.ExecuteScalar());

                if (mAutorizax.Trim().Length == 0)
                {
                    Toast.MakeText(this, "USUARIO Y PASSWORD INCORRECTO!!!", ToastLength.Short).Show();
                    thisConnection.Close();
                }
                else
                {
                    if (mAutorizax.Trim() == responsable.Trim())
                    {
                        thisConnection.Close();
                        Toast.MakeText(this, "No puede Asignarse Ordenes a usted mismo", ToastLength.Long).Show();
                    }
                    else
                    {
                        thisConnection.Close();
                        Intent intent = new Intent(this, typeof(reasignarterminar));
                        intent.PutExtra("respreasig", mAutorizax.ToString().Trim());
                        intent.PutExtra("cvresponsable", cvresponsable.ToString().Trim());
                        intent.PutExtra("responsable", responsable.ToString().Trim());
                        intent.PutExtra("currentVersionName", currentVersionName.ToString().Trim());
                        intent.PutExtra("imei", imei.ToString().Trim());
                        StartActivityForResult(intent, PICK_CONTACT_REQUEST);
                        builder.Dismiss();
                    }


                    /*

                    thisConnection.Open();
                    string cadena = "UPDATE  tb_det_acceso_celulares SET estado = 'R' WHERE nom_usu = '" + responsable + "' AND folio = '" + ordenacerrarreasignar.Trim() + "' AND estado = 'A'";
                    SqlCommand cmd = new SqlCommand(cadena, thisConnection);
                    cmd.ExecuteNonQuery();

                    cadena = "INSERT INTO  tb_det_acceso_celulares ( fecha, imei, nom_usu, sistema, folio, version, estado) " +
                               "VALUES(GETDATE(),'" + MainActivity.imei + "','" + mAutorizax + "','SplitTrailer','" + ordenacerrarreasignar.Trim() + "','','A')";
                    cmd = new SqlCommand(cadena, thisConnection);
                    cmd.ExecuteNonQuery();
                    thisConnection.Close();
                    builder.Dismiss();

                    DataTable responsables = new DataTable("responsables");

                    thisConnection.Open();
                    //Buscar el numero del que parte si es campo o proceso
                    cmnd = thisConnection.CreateCommand();
                    query = "SELECT folio FROM tb_det_acceso_celulares WHERE estado = 'A' AND folio != '' AND nom_usu = '" + responsable + "'";
                    SqlDataAdapter dax = new SqlDataAdapter(query, thisConnection);
                    DataSet dsx = new DataSet();
                    dax.Fill(dsx, "responsables");
                    responsables = dsx.Tables["responsables"];
                    thisConnection.Close();

                    Spinner spinner2 = FindViewById<Spinner>(Resource.Id.pedidosactivos);
                    System.Collections.ArrayList listaFrutas2 = new System.Collections.ArrayList();



                    strFrutas = new System.String[responsables.Rows.Count + 1];

                    if (responsables.Rows.Count == 0)
                    {
                        strFrutas[0] = "Sin ordenes activas";

                    }
                    else
                    {
                        strFrutas[0] = "Seleccione una orden";

                    }
                    for (int i = 1; i <= responsables.Rows.Count; i++)
                    {
                        int x = i - 1;
                        strFrutas[i] = responsables.Rows[x]["folio"].ToString();
                    }


                    Collections.AddAll(listaFrutas2, strFrutas);
                    comboAdapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleSpinnerItem, strFrutas);
                    spinner2.Adapter = comboAdapter;
                    spinner2.ItemSelected += new EventHandler<AdapterView.ItemSelectedEventArgs>(spinner_ItemSelected2);

                    Toast.MakeText(this, "Orden de Venta Reasignada Correctamente para Continuar Armado", ToastLength.Long).Show();*/
                }
            };
            builder.Show();
        }

        public string validar_ordenes()
        {
            int ultimopedido = 0;
            string Pedidoembarque = "0";
            string Tipopedido = "NAL";
            string pedidopendiente = "1";
            thisConnection.Open();
            string Cadena = "Select top(1) isNull(folio, 0) from tb_det_acceso_celulares where nom_usu = '" + responsable.Trim() + "' AND estado = 'A' AND Folio != '' ORDER bY fECHA desc";
            SqlCommand cmdx = new SqlCommand(Cadena, thisConnection);
            ultimopedido = Convert.ToInt32(cmdx.ExecuteScalar());
            Pedidoembarque = ultimopedido.ToString();
            thisConnection.Close();

            if (ultimopedido == 0 || Convert.ToInt32(ultimopedido) == Convert.ToInt32(pedido.Text.Trim()))
            {
                return "1";
            }
            else
            {
                if (pedido.Text.Length > 0)
                {
                    if (Convert.ToInt32(ultimopedido) < 400000)
                    {
                        Tipopedido = "EXP";
                        Pedidoembarque = "0" + Convert.ToInt32(ultimopedido);
                    }
                }
                int faltanteporsurtir = 0;

                thisConnection.Open();
                string cadena = "Select A.prod_clave, A.pdn_num_unidades, ((SELECT isNull(SUM(cajas), 0) FROM tb_det_embarque WHERE emb_folio = '" + Pedidoembarque + "' AND prod_clave = A.prod_clave AND OpCap = 'N' AND Estatus = 'A' AND emb_Tipo = '" + Tipopedido + "') +  (SELECT isNull(SUM(cajas), 0) FROM tb_det_split WHERE emb_folio = '" + Pedidoembarque + "' AND prod_clave = A.prod_clave AND Estatus != 'C' AND emb_Tipo = '" + Tipopedido + "')) AS surtido, (SELECT COUNT(producto) from tb_det_sol_producto WHERE ord_vent = '" + Pedidoembarque + "' AND producto = A.prod_clave) AS solicitud FROM tb_det_pedidos A WHERE pdn_folio = '" + ultimopedido + "' AND pdn_Tipo = '" + Tipopedido + "'";
                SqlDataAdapter adapterInfo = new SqlDataAdapter(cadena, thisConnection);
                DataSet setinfo = new DataSet();
                adapterInfo.Fill(setinfo, "ConsPed");
                var informacion = setinfo.Tables["ConsPed"];
                thisConnection.Close();
                foreach (DataRow Row in informacion.Rows)
                {
                    faltanteporsurtir = (Convert.ToInt32(Row["pdn_num_unidades"].ToString().Trim().Replace(".000", "")) - (Convert.ToInt32(Row["surtido"].ToString().Trim())));
                    if (faltanteporsurtir < 30 && faltanteporsurtir > 0)
                    {
                        if (Convert.ToInt32(Row["solicitud"].ToString().Trim()) == 0)
                        {
                            pedidopendiente = "0";
                        }
                    }
                }
            }
            if (pedidopendiente == "0")
            {
                DialogHelper.ShowWarningDialog(this,
                    message: $"No puede cambiar de orden hasta concluir los split faltantes de esta orden: {Pedidoembarque}",
                    positiveText: "Entendido");
                #region MATERIAL DIALOG
                /*// 🔹 Construimos el título con color amarillo claro y negritas
                var titleSpannable = new SpannableStringBuilder("Faltan Split Por Armar");
                titleSpannable.SetSpan(new ForegroundColorSpan(Color.ParseColor("#E5FA7A")), 0, titleSpannable.Length(), SpanTypes.ExclusiveExclusive);
                titleSpannable.SetSpan(new StyleSpan(TypefaceStyle.Bold), 0, titleSpannable.Length(), SpanTypes.ExclusiveExclusive);

                // 🔹 Construimos el mensaje con color amarillo y parte dinámica en negrita
                var mensajeSpannable = new SpannableStringBuilder("No puede cambiar de orden hasta concluir los split faltantes de esta orden: ");
                int startPedido = mensajeSpannable.Length();
                mensajeSpannable.Append(Pedidoembarque);
                mensajeSpannable.SetSpan(new StyleSpan(TypefaceStyle.Bold), startPedido, mensajeSpannable.Length(), SpanTypes.ExclusiveExclusive);
                mensajeSpannable.SetSpan(new ForegroundColorSpan(Color.ParseColor("#F6F87A")), 0, mensajeSpannable.Length(), SpanTypes.ExclusiveExclusive);

                // 🔹 Creamos el diálogo usando el ThemeOverlay oficial de Material3
                var builder = new MaterialAlertDialogBuilder(this, Resource.Style.ThemeOverlay_Material3_MaterialAlertDialog);
                builder.SetTitle(titleSpannable);
                builder.SetIcon(Resource.Drawable.no);
                builder.SetMessage(mensajeSpannable);
                builder.SetCancelable(false);

                // 🔹 Botón principal
                builder.SetPositiveButton("Entendido", (s, e) => { });

                // 🔹 Crear y mostrar el diálogo
                var dialog = builder.Create();
                dialog.Show();

                // 🔹 Personalizamos el botón después de mostrarlo
                dialog.Window.DecorView.Post(() =>
                {
                    var positiveButton = dialog.GetButton((int)DialogButtonType.Positive);
                    positiveButton?.SetTextColor(Color.ParseColor("#FFA000")); // Naranja Material
                    positiveButton?.SetAllCaps(false);
                });*/
                #endregion
            }
            return pedidopendiente;//
        }

        async Task Validacambiosorden()
        {
            try
            {
                SqlConnection ConexionBase = new SqlConnection(MainActivity.cadenaConexion);
                ConexionBase.Open();
                string consultavalidar = "Select pdn_folio FROM tb_mstr_pedidos_nal JOIN tb_det_acceso_celulares ON pdn_folio = folio Where estado = 'A' AND nom_usu = '" + responsable.Trim() + "' AND pdn_situacion = 'M'";
                SqlDataAdapter adaptervalidar = new SqlDataAdapter(consultavalidar, ConexionBase);
                DataSet setinfovalidar = new DataSet();
                adaptervalidar.Fill(setinfovalidar, "ConsPed");
                var informacion = setinfovalidar.Tables["ConsPed"];
                ConexionBase.Close();
                foreach (DataRow Row in informacion.Rows)
                {
                    notificar(Row["pdn_folio"].ToString().Trim());
                    //CrossLocalNotifications.Current.Show("Modificacion a Orden de Venta", "Hola, Se ha modificado la orden de venta "+ Row["pdn_folio"].ToString().Trim() + ", Favor de Verificar los cambios", 0, DateTime.Now.AddSeconds(7));
                }
            }
            catch
            {
                Toast.MakeText(this, "Proceso de notificacion Fallo", ToastLength.Short).Show();
            }

        }

        void CreateNotificationChannel()
        {
            if (Build.VERSION.SdkInt < BuildVersionCodes.O)
            {
                // Notification channels are new in API 26 (and not a part of the
                // support library). There is no need to create a notification
                // channel on older versions of Android.
                return;
            }

            var name = "Local Notifications";
            var description = "The count from MainActivity.";
            var channel = new NotificationChannel(CHANNEL_ID, name, NotificationImportance.Default)
            {
                Description = description
            };

            var notificationManager = (NotificationManager)GetSystemService(NotificationService);
            notificationManager.CreateNotificationChannel(channel);
        }


        void notificar(string ordenmod)
        {
            // Pass the current button press count value to the next activity:
            var valuesForActivity = new Bundle();
            valuesForActivity.PutInt("ORDENVENTA", Convert.ToInt32(ordenmod));
            valuesForActivity.PutString("RESPONSABLE", responsable);
            valuesForActivity.PutString("cvcamioneta", cvvehiculo);
            valuesForActivity.PutString("cvresponsable", cvresponsable);
            valuesForActivity.PutString("camioneta", vehiculo);
            valuesForActivity.PutString("responsable", responsable);



            cvvehiculo = Intent.GetStringExtra("cvcamioneta");
            cvresponsable = Intent.GetStringExtra("cvresponsable");
            vehiculo = Intent.GetStringExtra("camioneta");
            responsable = Intent.GetStringExtra("responsable");
            // When the user clicks the notification, SecondActivity will start up.
            var resultIntent = new Intent(this, typeof(DetalleCaptura));

            // Pass some values to SecondActivity:
            resultIntent.PutExtras(valuesForActivity);

            // Construct a back stack for cross-task navigation:
            var stackBuilder = TaskStackBuilder.Create(this);
            //stackBuilder.AddParentStack(SolicitarPed);
            stackBuilder.AddParentStack(Java.Lang.Class.FromType(typeof(SolicitarPed)));
            stackBuilder.AddNextIntentWithParentStack(resultIntent);

            // Create the PendingIntent with the back stack:
            var resultPendingIntent = stackBuilder.GetPendingIntent(0, (int)PendingIntentFlags.UpdateCurrent);

            // Build the notification:
            var builder = new NotificationCompat.Builder(this)
                          .SetAutoCancel(true) // Dismiss the notification from the notification area when the user clicks on it
                          .SetContentIntent(resultPendingIntent) // Start up this activity when the user clicks the intent.
                          .SetContentTitle("Modificacion a Orden de Venta " + ordenmod) // Set the title
                          .SetNumber(Convert.ToInt32(ordenmod)) // Display the count in the Content Info
                          .SetSmallIcon(Resource.Drawable.logo_splittrailers) // This is the icon to display
                          .SetDefaults((int)NotificationDefaults.Sound)
                          .SetVibrate(new long[] { 0, 500, 1000, 500, 1000, 500, 2000 })
                          .SetPriority(NotificationCompat.PriorityMax)
                          .SetContentText($"Hola, Se ha modificado la orden de venta {ordenmod}, Favor de Verificar los cambios"); // the message to display.


            // Finally, publish the notification:
            var notificationManager = NotificationManagerCompat.From(this);
            notificationManager.Notify(Convert.ToInt32(ordenmod), builder.Build());

            // Increment the button press count:
            count++;
        }


        private void ConfigurarRecyclerView()
        {
            try
            {
                var recyclerView = FindViewById<RecyclerView>(Resource.Id.gvCtrl);

                if (recyclerView.GetLayoutManager() == null)
                {
                    recyclerView.SetLayoutManager(new LinearLayoutManager(this));
                }

                // Obtener datos REALES (sin datos de prueba)
                List<FlimStarInfo> lstFlimStar = detalle_pedido(folio.Trim(), "Individual");

                var adapter = new MyRecyclerAdapter(this, lstFlimStar);
                recyclerView.SetAdapter(adapter);

                adapter.ItemClick += (sender, position) =>
                {
                    var item = adapter.GetItem(position);
                    OnRecyclerViewItemClicked(position, item);
                };

                // Mostrar mensaje con datos reales
                if (lstFlimStar != null && lstFlimStar.Count > 0)
                {
                    Toast.MakeText(this, $"Mostrando {lstFlimStar.Count} productos", ToastLength.Short).Show();
                }
                else
                {
                    Toast.MakeText(this, "No hay productos para mostrar", ToastLength.Short).Show();
                }
            }
            catch (Java.Lang.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error configurando RecyclerView: {ex.Message}");
                Toast.MakeText(this, "Error cargando productos", ToastLength.Short).Show();
            }
        }

    }
}