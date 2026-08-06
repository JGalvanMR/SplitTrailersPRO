using Android.App;
using Android.Bluetooth;
using Android.Content;
using Android.Graphics;
using Android.Nfc;
using Android.OS;
using Android.Text;
using Android.Text.Style;
using Android.Views;
using Android.Views.InputMethods;
using Android.Widget;
using AndroidX.AppCompat.App;
//Librerias de la impresion Bluetooth
using Com.Woosim.Printer;
using Google.Android.Material.AppBar;
using Google.Android.Material.Dialog;
using Java.IO;
using Java.Lang;
using Java.Net;
using Java.Util;
using Org.Json;
using Plugin.DeviceInfo;
using SplitTrailers.Modal;
using SplitTrailers.Models;
using SQLite;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Threading;
using static Android.Content.ClipData;
using SplitTrailers.Helpers;

namespace SplitTrailers
{
    [Activity(Label = "Capturar Split")]

    public partial class capturar_split : AppCompatActivity, Android.Text.ITextWatcher
    {
        public static int valido = 0, veces = 0;
        public static string cvvehiculo, cvresponsable, currentVersionName;
        public static string vehiculo, responsable;
        public string Nombre = "", Mtipo = "", MProd = "", MTar = "", MFol = "", mUser = "", mAutoriza = "", user = "", motfolade = "";
        public string cvecam = "", muser = "", mconcen = "1";
        public static string AutoPed = "N";
        public int proceso = 0;
        public static string EtiquetaExiste = "S", EtiquetaCapturada = "S", FechaCaducada = "S", OrdenExiste = "S";
        public static string HayExistencias = "S";
        public static string Surtidomayor = "S";
        public static string ValiFechacad = "S";
        public static string ValiMinFechaPTC = "S";
        public static string EstructuraEtiqueta = "S";
        public static string dondegenera = "";
        public static SQLiteConnection db;
        SqlConnection thisConnection = new SqlConnection(MainActivity.cadenaConexion);
        int FolioCampo = 0;
        SqlDataAdapter da;
        DataSet ds = new DataSet();
        SqlCommand cmnd = new SqlCommand();
        SqlCommand cmnd1 = new SqlCommand();
        SqlDataReader reader1;
        public static DataTable det_pedidos = new DataTable("det_pedidos");
        public static DataTable det_pedidos2 = new DataTable("det_pedidos2");
        public static DataTable productos_leidos = new DataTable("productos_leidos");
        string query = "", prod_clave = "", folio = "", tipo = "", cadena = "", prod_nombre = "";
        int tarima = 0, caja = 0, tarimaf = 0;
        bool find = false;
        ArrayAdapter<System.String> comboAdapter;
        System.String[] strFrutas;


        public static string imei = "";


        DataTable CatProd = new DataTable();

        //Declarar los datos de los items en el layout CapturarSplit
        EditText foliocaptura;
        TextView total;
        TextView pedidoencaptura;
        Button Guardar;

        TextView diasmincarga;

        TextView nosplit;


        Int32 TotCaj;

        int diasmincad = 0;


        string valorfinal = "";

        EditText password;

        //Datos supervisor
        EditText supervisor;
        EditText passwordsupervisor;

        //CheckBox Eliminar Caja
        CheckBox Eliminar_caja;


        //Radio button
        RadioButton etiblanca;
        RadioButton etiverde;

        DateTime fechaactual;

        EditText et;
        Spinner prue;

        //Variables de solicitud al servidor si realiza o no guardado de datos de la bd interna a la bd del servidor antes de borrar

        Context context;
        Runnable listener;
        private static string INFO_FILE = "http://192.168.123.4:81/EmbarquesApk/estado_respaldo.txt";
        private int respaldo_activo = 1;


        private NfcAdapter _nfcAdapter;


        //Variables de la Impresion//
        string deviceName = "WOOSIM";
        //string deviceName = "WO0SIM5";
        private BluetoothAdapter mBluetoothAdapter = null;
        private BluetoothDevice mmDevice = null;
        private BluetoothSocket mmSocket = null;
        private Stream mmOutputStream;
        private Stream mmInputStream;


        protected override void OnCreate(Bundle savedInstanceState)
        {
            //Android.Telephony.TelephonyManager mTelephonyMgr;
            //mTelephonyMgr = (Android.Telephony.TelephonyManager)GetSystemService(TelephonyService);
            //IMEI number  
            imei = GetDeviceID();

            cvvehiculo = Intent.GetStringExtra("cvcamioneta");
            cvresponsable = Intent.GetStringExtra("cvresponsable");
            vehiculo = Intent.GetStringExtra("camioneta");
            responsable = Intent.GetStringExtra("responsable");
            diasmincad = Convert.ToInt32(Intent.GetStringExtra("diascadmin"));
            imei = Intent.GetStringExtra("imei");
            currentVersionName = Intent.GetStringExtra("currentVersionName");

            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.CapturarSplit);
            LoadConnection();
            TotCaj = 0;
            muser = SolicitarPed.responsable;
            cvecam = SolicitarPed.cvvehiculo;

            context = this;

            thisConnection.Open();
            cmnd = thisConnection.CreateCommand();
            cmnd.CommandText = "select inicio_campo from Tb_folio_campo";
            FolioCampo = Convert.ToInt32(cmnd.ExecuteScalar());
            thisConnection.Close();


            thisConnection.Open();

            string Cadena = "SELECT cast( dateadd(day, datediff(day, 0, current_timestamp), 0) as smalldatetime) AS Date";
            SqlCommand cmdx = new SqlCommand(Cadena, thisConnection);
            fechaactual = Convert.ToDateTime(cmdx.ExecuteScalar());

            thisConnection.Close();



            Eliminar_caja = FindViewById<CheckBox>(Resource.Id.Eliminar);

            foliocaptura = FindViewById<EditText>(Resource.Id.Folio);
            total = FindViewById<TextView>(Resource.Id.totalcapturado);
            diasmincarga = FindViewById<TextView>(Resource.Id.splitdiasmin);
            pedidoencaptura = FindViewById<TextView>(Resource.Id.pedidoencaptura);
            nosplit = FindViewById<TextView>(Resource.Id.splitcantidad);
            etiblanca = FindViewById<RadioButton>(Resource.Id.radio_blanco);
            etiverde = FindViewById<RadioButton>(Resource.Id.radio_verde);
            Guardar = FindViewById<Button>(Resource.Id.GuardarCapturado);
            Guardar.Click += BtnGuardar_Click;
            Guardar.Enabled = false;

            foliocaptura.LongClickable = false;

            thisConnection.Open();
            string cadena = "Select prod_clave,prod_nombre from tb_cat_producto where estatus = 'A' AND (prod_tipo = 'PTP' OR prod_tipo = 'PTC')  order by LEN(prod_clave) DESC";
            SqlDataAdapter da = new SqlDataAdapter(cadena, thisConnection);
            DataSet ds = new DataSet();
            da.Fill(ds, "CatProd");
            CatProd = ds.Tables["CatProd"];

            //Traer numero de split
            var quex = db.Table<Pedidos>();
            foreach (var captu in quex)
            {
                nosplit.Text = "Split Numero: " + NoSplit(captu.folio.ToString());
                pedidoencaptura.Text = "Pedido Actual: " + captu.folio.ToString();
            }

            //cadena = "SELECT CASE WHEN ((SELECT CONVERT(datetime, (SELECT Convert(varchar(8),GetDate(), 108) hora), 108)) > (SELECT Convert(datetime,'00:00:00', 108) HoraServidor)) AND ((SELECT CONVERT(datetime, (SELECT Convert(varchar(8),GetDate(), 108) hora), 108)) < (SELECT Convert(datetime,'05:00:00', 108) HoraServidor)) THEN '1' WHEN ((SELECT CONVERT(datetime, (SELECT Convert(varchar(8),GetDate(), 108) hora), 108)) > (SELECT Convert(datetime,'17:54:00', 108) HoraServidor)) AND ((SELECT CONVERT(datetime, (SELECT Convert(varchar(8),GetDate(), 108) hora), 108)) < (SELECT Convert(datetime,'18:36:00', 108) HoraServidor)) THEN '1' ELSE '2' END";
            cadena = "SELECT CASE WHEN ((SELECT CONVERT(datetime, (SELECT Convert(varchar(8),GetDate(), 108) hora), 108)) > (SELECT Convert(datetime,'04:59:00', 108) HoraServidor)) AND ((SELECT CONVERT(datetime, (SELECT Convert(varchar(8),GetDate(), 108) hora), 108)) < (SELECT Convert(datetime,'05:00:00', 108) HoraServidor)) THEN '1' WHEN ((SELECT CONVERT(datetime, (SELECT Convert(varchar(8),GetDate(), 108) hora), 108)) > (SELECT Convert(datetime,'17:54:00', 108) HoraServidor)) AND ((SELECT CONVERT(datetime, (SELECT Convert(varchar(8),GetDate(), 108) hora), 108)) < (SELECT Convert(datetime,'18:36:00', 108) HoraServidor)) THEN '1' ELSE '2' END";
            SqlCommand cmddias = new SqlCommand(cadena, thisConnection);
            var valordias = Convert.ToString(cmddias.ExecuteScalar());

            if (valordias == "1")
            {
                diasmincad = diasmincad - 1;
            }

            diasmincarga.Text = "DIAS MINIMOS DE CARGA: " + diasmincad;
            //tERMINA TRAER NUMERO DE SPLIT

            thisConnection.Close();

            //Llamar al Documento en el servidor para saber si la opcion para hacer el respaldo esta activa



            //****************************************Inicio Lectura de QR**************************************************************************************

            foliocaptura.AddTextChangedListener(this);


            List<FlimStarInfo> lstFlimStar = productocapturado();
            var gvObject = FindViewById<GridView>(Resource.Id.gvCtr2);
            gvObject.Adapter = new myGVItemAdapter(this, lstFlimStar);
            gvObject.ItemClick += new EventHandler<AdapterView.ItemClickEventArgs>(OnGridView_ItemClicked);

            total.Text = TotCaj.ToString("##0");

            _nfcAdapter = NfcAdapter.GetDefaultAdapter(this);
            //foliocaptura.KeyPress += onEditTextKeyPress;
            foliocaptura.KeyPress += Foliocaptura_KeyPress;


            // Referencia al MaterialToolbar
            MaterialToolbar toolbar = FindViewById<MaterialToolbar>(Resource.Id.toolbar);
            // Asigna como ActionBar usando SupportActionBar
            SetSupportActionBar(toolbar);

            // Opcional: título centrado
            SupportActionBar.Title = "CAPTURAR SPLIT";

            // Ahora SupportActionBar no es null
            SupportActionBar.Title = "CAPTURAR SPLIT";
            SupportActionBar.SetDisplayHomeAsUpEnabled(true); // si quieres back button
        }

        public string GetDeviceID()
        {
            const string PrefKey = "native_device_unique_id";

            try
            {
                var context = Android.App.Application.Context;

                // 1. Intentar recuperar el ID guardado usando SharedPreferences nativo
                var prefs = Android.Preferences.PreferenceManager.GetDefaultSharedPreferences(context);
                string deviceId = prefs.GetString(PrefKey, string.Empty);

                if (!string.IsNullOrEmpty(deviceId))
                {
                    return deviceId;
                }

                // 2. Obtener el Android ID nativo del dispositivo como base inicial
                deviceId = Android.Provider.Settings.Secure.GetString(context.ContentResolver, Android.Provider.Settings.Secure.AndroidId);

                // 3. Si por alguna razón el AndroidId es nulo o inválido, generamos un GUID único
                if (string.IsNullOrEmpty(deviceId) || deviceId == "9774d56d682e549c")
                {
                    deviceId = Guid.NewGuid().ToString();
                }

                // 4. Guardarlo de forma persistente para que nunca cambie mientras la app esté instalada
                using (var editor = prefs.Edit())
                {
                    editor.PutString(PrefKey, deviceId);
                    editor.Apply();
                }

                return deviceId;
            }
            catch (Java.Lang.Exception)
            {
                return Guid.NewGuid().ToString();
            }
            catch (System.Exception)
            {
                return Guid.NewGuid().ToString();
            }
        }

        public bool OnTouch(View v, MotionEvent e)
        {
            // Pass the event to the edit text to have the blinking cursor.
            v.OnTouchEvent(e);
            // Hide the input.
            var imm = ((InputMethodManager)v.Context.GetSystemService(Context.InputMethodService));
            imm?.HideSoftInputFromWindow(v.WindowToken, HideSoftInputFlags.None);
            return true;
        }



        //Incia Metodo PARA TRAER DATOS DESDE EL ARCHIVO EN EL SERVIDOR
        private void getData()
        {
            try
            {
                //context = this;

                // Datos remotos
                string data = downloadHttp(new URL(INFO_FILE));
                JSONObject json = new JSONObject(data.ToString());
                respaldo_activo = json.GetInt("respaldoactivo");
                System.Console.WriteLine("AutoUpdate", "Datos obtenidos con éxito");
            }
            catch (JSONException e)
            {
                System.Console.WriteLine("AutoUpdate", "Ha habido un error con el JSON", e);
            }
            catch (Android.Content.PM.PackageManager.NameNotFoundException e)
            {
                System.Console.WriteLine("AutoUpdate", "Ha habido un error con el packete :S", e);
            }
            catch (System.IO.IOException e)
            {
                System.Console.WriteLine("AutoUpdate", "Ha habido un error con la descarga", e);
            }
        }

        private static string downloadHttp(URL url)
        {
            // Codigo de coneccion, Irrelevante al tema.

            StrictMode.ThreadPolicy policy = new StrictMode.ThreadPolicy.Builder().PermitAll().Build();
            StrictMode.SetThreadPolicy(policy);
            HttpURLConnection c = (HttpURLConnection)url.OpenConnection();

            c.RequestMethod = "GET";
            c.ReadTimeout = (15 * 1000);
            c.UseCaches = false;
            c.Connect();
            Java.IO.BufferedReader reader = new Java.IO.BufferedReader(new Java.IO.InputStreamReader(c.InputStream));
            Java.Lang.StringBuilder stringBuilder = new Java.Lang.StringBuilder();
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                stringBuilder.Append(line + "\n");
            }
            return stringBuilder.ToString();
        }

        private void BtnGuardar_Click(object sender, EventArgs e)
        {
            try
            {
                FindPrinter();
                sendData();
                sendData();
            }
            catch (System.Exception ex)
            {
                //Toast.MakeText(this, "Error al Imprimir - " + ex.ToString() + "", ToastLength.Short).Show();
                SendMail("jgalvan@mrlucky.com.mx", "Error generado en la impresion de Split de sistema split trailer detalle: " + ex, "Error al Imprimir SPLIT");
            }

            //validarGuardar();

            Guardar.Enabled = false;
            //Evitar apagar la pantalla*********************************************************************************************
            var pm = PowerManager.FromContext(context);
            var wakeLock = pm.NewWakeLock(WakeLockFlags.Full, "Guardar");
            wakeLock.Acquire();
            //Adquirir el wakelock**************************************************************************************************

            var progressDialog = ProgressDialog.Show(this, "Espere Por Favor...", "Guardando Split", true);
            new System.Threading.Thread(new ThreadStart(delegate
            {//LOAD METHOD TO GET ACCOUNT INFO

                db.Query<xLoteFinal>("delete from  [xLoteFinal]");
                db.Query<Pedidos>("UPDATE [Pedidos] SET surtido = '0'");
                thisConnection.Open();
                string mped = pedidoencaptura.Text.ToString().Trim();
                mped = mped.Replace("Pedido Actual: ", "");
                //Actualizacion de pedido leido por cada producto

                string mpedido = mped;
                //Actualizacion de pedido leido por cada producto

                db.Query<xLote>("UPDATE [xLote] SET Pedido = '" + mpedido + "'");


                var productoscapturados = db.Table<xLote>();
                foreach (var captu in productoscapturados)
                {
                    string mtip = "", mfol = "", mcod = "", mtar = "", mcaj = "", mdia = "", mmes = "", mfeccap = "", mTipoCaptura = "";

                    mtip = captu.Tipo.ToString().Trim();
                    mfol = captu.Folio.ToString().Trim();
                    mcod = captu.Codigo.ToString().Trim();
                    mtar = captu.Tarima.ToString().Trim();
                    mcaj = captu.Cajas.ToString().Trim();
                    mdia = captu.diacad.ToString().Trim();
                    mmes = captu.mescad.ToString().Trim();
                    mfeccap = captu.fecha_captura.ToString().Trim();
                    mTipoCaptura = captu.tipo_captura.ToString().Trim();
                    string lectura = mtip + mfol + mcod + mtar + mcaj;
                    string nom = traenom(mcod);
                    var pedidos = db.Query<Pedidos>("SELECT * FROM [Pedidos] Where prod_clave = '" + mcod.ToString().Trim() + "'");
                    foreach (var pedisur in pedidos)
                    {
                        //if (mcod == pedisur.prod_clave) {
                        //mped = pedisur.surtido.ToString();
                        if (Convert.ToInt32(pedisur.surtido) < Convert.ToInt32(pedisur.pedido))
                        {
                            //mped = pedisur.folio.ToString();
                            db.Query<Pedidos>("UPDATE [Pedidos] SET surtido = '" + (Convert.ToInt32(pedisur.surtido) + 1) + "' WHERE prod_clave = '" + mcod.ToString() + "' AND Folio = '" + mped.ToString() + "'");
                            //db.Query<xLote>("UPDATE [xLote] SET Pedido = '" + mped + "' WHERE Tipo = '" + mtip.ToString() + "' AND Folio = '" + mfol.ToString() + "' AND Codigo = '" + mcod.ToString() + "' AND Tarima = '" + mtar.ToString() + "' AND Cajas = '" + mcaj.ToString() + "'");
                            break;
                        }
                        //}
                    }
                    string cadena = "insert into tb_det_Etiqueta(fecha,emb_folio, fecha_cap, Eti_Lectura, Eti_Recibo, Eti_Producto, Eti_Caja, Eti_TarIni, Eti_TarFin, Cve_Camioneta, FecCap, Version, Imei, Split, Estatus, Tipo_Captura) " +
                                    "Values('" + System.DateTime.Now.ToString("dd/MM/yyyy") + "','" + mped + "','" + mfeccap + "','" + lectura + "','" + mfol + "','" + mcod + "','" + mcaj + "','" + mtar + "','" + mtar + "','" +
                                    cvecam + "','" + System.DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + "','" + currentVersionName + "','" + imei + "', '" + nosplit.Text.Replace("Split Numero: ", "") + "', 'A', '" + mTipoCaptura + "')";
                    SqlCommand cmd = new SqlCommand(cadena, thisConnection);
                    cmd.ExecuteNonQuery();
                    string hay = "N";
                    var lotesproducto = db.Query<xLoteFinal>("SELECT * FROM [xLoteFinal] Where tipo = '" + mtip + "' and Pedido = '" + mped + "' and Folio = '" + mfol + "' and Codigo = '" + mcod + "' and Tarima = '" + mtar + "'");
                    foreach (var lotesencontrado in lotesproducto)
                    {
                        db.Query<xLoteFinal>("UPDATE [xLoteFinal] SET Cajas = '" + (Convert.ToInt32(lotesencontrado.Cajas) + 1) + "' WHERE Tipo = '" + mtip + "' and Pedido = '" + mped + "' and Folio = '" + mfol + "' and Codigo = '" + mcod + "' and Tarima = '" + mtar + "'");
                        hay = "S";
                    }
                    if (hay == "N")
                    {
                        xLoteFinal LoteFinal = new xLoteFinal { Tipo = mtip, Pedido = mped, Folio = mfol, Codigo = mcod, Tarima = mtar, Cajas = "1", nombre = nom, diacad = mdia, mescad = mmes };
                        //Registra en la base de datos SQLite
                        db.Insert(LoteFinal);

                    }
                }
                var pedidoslote = db.Table<xLoteFinal>();
                foreach (var lotes in pedidoslote)
                {
                    var ampm = System.DateTime.Now.ToString("tt");
                    ampm = ampm.Replace(" ", "");
                    // AGREGO LOS REGISTROS EN LA TABLA DE LOS SPLIT PARA QUE ´PUEDAN SER CARGADOS EN LA CAMIONETA
                    string mnom = lotes.nombre.ToString().Trim();
                    mnom = mnom.Replace("'", " ");

                    string ordven = lotes.Pedido.ToString();
                    string tipord = "NAL";

                    if (Convert.ToInt32(lotes.Pedido.ToString().Trim()) < 300000)
                    {
                        ordven = "0" + Convert.ToInt32(lotes.Pedido.ToString().Trim()).ToString();
                        tipord = "EXP";
                    }

                    string cadena = "Insert into tb_det_split(emb_folio, prod_clave, emb_tipo, no_lote, cajas, tarima, nom_prod, tipo_rec, estatus, LUGAR, TARINI, TARFIN, DIACAD, MESCAD, FECHA, HORA, NOM_CAPSPLIT, XCAJA) " +
                                    "Values('" + ordven.ToString() + "','" + lotes.Codigo.ToString() + "','" + tipord.Trim() + "','" + lotes.Folio.ToString() + "','" + lotes.Cajas.ToString() + "','" + nosplit.Text.Replace("Split Numero: ", "") + "',' " +
                                    mnom + "','" + lotes.Tipo.ToString() + "','A','Nacional','" + lotes.Tarima.ToString() + "','" +
                                    lotes.Tarima.ToString().ToString() + "','" + lotes.diacad.ToString() + "','" + lotes.mescad.ToString() + "','" + System.DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss") + " " + ampm + "','" +
                                    System.DateTime.Now.ToString("hh:mm") + " " + ampm + "','" + muser.Trim() + "','S')";
                    //MessageBox.Show(cadena);
                    SqlCommand cmd = new SqlCommand(cadena, thisConnection);
                    cmd.ExecuteNonQuery();
                    // ACTUALIZO LAS CAJAS SURTIDAS DE ACUERDO AL FOLIO CODIGO Y TARIMA 
                    if (lotes.Pedido.ToString().Trim().Length > 0)
                    {
                        if (lotes.Tipo.ToString() == "PTC")
                            cadena = "UPDATE TB_DET_TRAZABILIDAD SET SURTIDO = SURTIDO + " + lotes.Cajas.ToString() + " WHERE PROD_CLAVE = '" + lotes.Codigo.ToString() + "' AND RECIBO = '" + lotes.Folio.ToString() + "' " +
                                "AND TIPO = 'PTC' AND TARIMA = '" + Convert.ToInt32(lotes.Tarima.ToString()).ToString() + "' ";

                        else
                            cadena = "UPDATE TB_DET_ETI_FINAL SET CAJAS_SUR = CAJAS_SUR + " + lotes.Cajas.ToString() + " WHERE CVE_PROD = '" + lotes.Codigo.ToString().Trim() + "' AND FOLIO = '" + lotes.Folio.ToString() + "' " +
                                "AND TARIMA = '" + Convert.ToInt32(lotes.Tarima).ToString() + "' ";
                        cmd = new SqlCommand(cadena, thisConnection);
                        cmd.ExecuteNonQuery();
                    }
                }
                if (AutoPed == "S")
                {
                    var pedidosprod = db.Table<Pedidos>();
                    foreach (var prod in pedidosprod)
                    {
                        if (Convert.ToInt16(prod.pedido) != Convert.ToInt16(prod.surtido))
                            AgregaRegistroPedidoAuto(prod.folio.ToString(), "Prod: (" + prod.prod_clave.ToString() + ") " + prod.nombre.ToString().Trim() + " Ped:" + prod.pedido.ToString().Trim() + " Sur:" + prod.surtido.ToString().Trim());
                    }
                }
                AgregaProdXPedido();

                AgregaTempSplit();

                if (ValiFechacad == "N")
                {
                    AgregaDetaEtiAdelantado();
                }

                thisConnection.Close();


                db.Query<Pedidos>("delete from  [Pedidos]");
                db.Query<ConPedidos>("delete from  [ConPedidos]");
                db.Query<xLote>("delete from  [xLote]");
                db.Query<xLoteFinal>("delete from  [xLoteFinal]");
                db.Query<xprod>("delete from  [xprod]");


                #region MATERIAL DIALOG
                RunOnUiThread(() =>
                {
                    DialogHelper.ShowSuccessDialog(this,
                        message: "Información Grabada Correctamente!!!",
                        positiveText: "OK",
                        positiveAction: (s, e) =>
                        {
                            Intent intent = new Intent(this, typeof(SolicitarPed));
                            intent.AddFlags(ActivityFlags.ClearTop | ActivityFlags.SingleTop);
                            intent.PutExtra("cvresponsable", cvresponsable.ToString());
                            intent.PutExtra("responsable", responsable.ToString());
                            intent.PutExtra("currentVersionName", currentVersionName.ToString().Trim());
                            intent.PutExtra("imei", imei.ToString().Trim());
                            StartActivity(intent);
                            Finish();
                        });
                });
                #endregion
                #region MATERIAL DIALOG LEGACY
                /*RunOnUiThread(() =>
                {
                    var builder = new MaterialAlertDialogBuilder(this, Resource.Style.ThemeOverlay_Material3_MaterialAlertDialog);

                    // Título en rojo
                    builder.SetTitle(Html.FromHtml(
                        "<font color='#DC3545'><b>Información Almacenada</b></font>",
                        FromHtmlOptions.ModeLegacy
                    ));

                    builder.SetIcon(Resource.Drawable.exito);

                    // Mensaje en blanco
                    builder.SetMessage(Html.FromHtml(
                        "<font color='#FFFFFF'>Información Grabada Correctamente!!!</font>",
                        FromHtmlOptions.ModeLegacy
                    ));

                    builder.SetCancelable(false);

                    // Botón OK
                    builder.SetPositiveButton(Html.FromHtml(
                        "<font color='#DC3545'><b>OK</b></font>",
                        FromHtmlOptions.ModeLegacy
                    ), delegate
                    {
                        builder.Dispose();

                        // Navegación al Activity de SolicitarPed
                        Intent intent = new Intent(this, typeof(SolicitarPed));
                        intent.AddFlags(ActivityFlags.ClearTop | ActivityFlags.SingleTop);
                        intent.PutExtra("cvresponsable", cvresponsable.ToString());
                        intent.PutExtra("responsable", responsable.ToString());
                        intent.PutExtra("currentVersionName", currentVersionName.ToString().Trim());
                        intent.PutExtra("imei", imei.ToString().Trim());
                        StartActivity(intent);
                        Finish();
                    });

                    var dialog = builder.Create();
                    dialog.Show();

                    // Personalización del botón
                    var btn = dialog.GetButton((int)DialogButtonType.Positive);
                    btn?.SetTextColor(Android.Graphics.Color.ParseColor("#DC3545"));
                    btn?.SetAllCaps(false);
                });*/
                #endregion

                #region ALERT DIALOG
                /*Android.App.AlertDialog.Builder alertDialog = new Android.App.AlertDialog.Builder(this);
                alertDialog.SetTitle(Html.FromHtml("<font color='#dc3545' size = 10>Informacion Almacenada</font>"));
                alertDialog.SetIcon(Resource.Drawable.exito);
                alertDialog.SetMessage(Html.FromHtml("<font color='#FFFFFF' size = 10>Información Grabada Correctamente!!! </font>"));
                alertDialog.SetCancelable(false);
                alertDialog.SetNeutralButton("Ok", delegate
                {
                    alertDialog.Dispose();

                    Intent intent = new Intent(this, typeof(SolicitarPed));
                    intent.AddFlags(ActivityFlags.ClearTop);
                    Intent.AddFlags(ActivityFlags.SingleTop);
                    //intent.PutExtra("cvcamioneta", cvvehiculo.ToString());
                    intent.PutExtra("cvresponsable", cvresponsable.ToString());
                    //intent.PutExtra("camioneta", vehiculo.ToString());
                    intent.PutExtra("responsable", responsable.ToString());
                    intent.PutExtra("currentVersionName", currentVersionName.ToString().Trim());
                    intent.PutExtra("imei", imei.ToString().Trim());
                    StartActivity(intent);
                    Finish();
                });
                RunOnUiThread(() => alertDialog.Show());*/
                #endregion

                RunOnUiThread(() => Toast.MakeText(this, "Split Almacenado Correctamente.", ToastLength.Long).Show()); //HIDE PROGRESS DIALOG 
                RunOnUiThread(() => progressDialog.Hide());

            })).Start();

        }

        public void validarGuardar()
        {

            if (_nfcAdapter == null)
            {
                RunOnUiThread(() =>
                {
                    DialogHelper.ShowErrorDialog(this,
                        message: "NFC no soportado en el dispositivo.",
                        positiveText: "OK");
                });
                #region MATERIAL DIALOG
                /*RunOnUiThread(() =>
                {
                    var builder = new MaterialAlertDialogBuilder(this, Resource.Style.ThemeOverlay_Material3_MaterialAlertDialog);

                    // Título en rojo
                    builder.SetTitle(Html.FromHtml(
                        "<font color='#DC3545'><b>NFC No Disponible</b></font>",
                        FromHtmlOptions.ModeLegacy
                    ));

                    // Mensaje en blanco
                    builder.SetMessage(Html.FromHtml(
                        "<font color='#FFFFFF'>NFC no soportado en el dispositivo.</font>",
                        FromHtmlOptions.ModeLegacy
                    ));

                    builder.SetCancelable(true);

                    // Botón OK
                    builder.SetPositiveButton(Html.FromHtml(
                        "<font color='#DC3545'><b>OK</b></font>",
                        FromHtmlOptions.ModeLegacy
                    ), delegate
                    {
                        builder.Dispose();
                    });

                    var dialog = builder.Create();
                    dialog.Show();

                    // Personalización del botón
                    var btn = dialog.GetButton((int)DialogButtonType.Positive);
                    btn?.SetTextColor(Android.Graphics.Color.ParseColor("#DC3545"));
                    btn?.SetAllCaps(false);
                });*/
                #endregion

                #region ALERT DIALOG
                /*var alert = new Android.App.AlertDialog.Builder(this).Create();
                alert.SetMessage("NFC No soportado en el dispositivo.");
                alert.SetTitle("NFC No Disponible");
                alert.Show();*/
                #endregion
            }
            else
            {

                var tagDetected = new IntentFilter(NfcAdapter.ActionTagDiscovered);
                var ndefDetected = new IntentFilter(NfcAdapter.ActionNdefDiscovered);
                var techDetected = new IntentFilter(NfcAdapter.ActionTechDiscovered);
                var filters = new[] { ndefDetected, tagDetected, techDetected };

                var intent = new Intent(this, GetType()).AddFlags(ActivityFlags.SingleTop);

                var pendingIntent = PendingIntent.GetActivity(this, 0, intent, 0);

                // Gives your current foreground activity priority in receiving NFC events over all other activities.
                _nfcAdapter.EnableForegroundDispatch(this, pendingIntent, filters, null);
            }

            actualizar_detalle();
            //Evitar apagar la pantalla*********************************************************************************************
            var pm = PowerManager.FromContext(context);
            var wakeLock = pm.NewWakeLock(WakeLockFlags.Full, "Validar");
            wakeLock.Acquire();
            //Adquirir el wakelock**************************************************************************************************

            var progressDialog = ProgressDialog.Show(this, "Espere Por Favor...", "Validando Informacion Capturada...", true);


            new System.Threading.Thread(new ThreadStart(delegate
            {//LOAD METHOD TO GET ACCOUNT INFO

                //try
                //{
                dondegenera = "Iniciovalidar";
                string existenproductos = "NO";
                var existeCapturado = db.Table<xprod>();
                foreach (var captu in existeCapturado)
                {
                    existenproductos = "SI";
                    break;
                }

                if (existenproductos == "SI")
                {
                    insertarinfo();
                    if (validaestructuraetiqueta() == "SI")
                    {
                        db.Query<Mensajes>("delete from  [Mensajes]");
                        AutoPed = "N";
                        RunOnUiThread(() => Guardar.Enabled = false);

                        var validando = valida();
                        var producto = validaprod();
                        //var validandofec = validafecad();
                        //var validandofec = validafecadMod();
                        var validandofec = validafecadMAXIMOS();

                        if (Surtidomayor == "NR")
                        {
                            ImprimirDialogs(0);
                            RunOnUiThread(() => Guardar.Enabled = false);
                        }
                        else if (HayExistencias != "S")
                        {
                            ImprimirDialogs(0);
                        }
                        else if (ValiMinFechaPTC != "S")
                        {
                            ImprimirDialogs(0);
                            RunOnUiThread(() => Guardar.Enabled = false);
                        }
                        else
                        {
                            if (Surtidomayor == "NR" || EtiquetaCapturada == "N")
                            {
                                ImprimirDialogs(0);
                            }
                            else if (OrdenExiste == "N")
                            {
                                ImprimirDialogs(0);
                            }
                            else
                            {
                                if (EtiquetaExiste == "S")
                                {
                                    if (producto == "S" && (validando == "S"))
                                    {
                                        if ((ValiFechacad == "N"))
                                        {
                                            RunOnUiThread(() => Guardar.Enabled = false);

                                            et = new EditText(this);
                                            et.InputType = Android.Text.InputTypes.TextVariationPassword | Android.Text.InputTypes.ClassText;
                                            et.LongClickable = false;
                                            et.Hint = "Password";

                                            #region MATERIAL DIALOG
                                            RunOnUiThread(() =>
                                            {
                                                var builder = new MaterialAlertDialogBuilder(this, Resource.Style.ThemeOverlay_Material3_MaterialAlertDialog);

                                                // Título en rojo
                                                builder.SetTitle(Html.FromHtml(
                                                    "<font color='#DC3545'><b>Autorización Folios Adelantados</b></font>",
                                                    FromHtmlOptions.ModeLegacy
                                                ));

                                                // Establecer custom view
                                                builder.SetView(et);

                                                builder.SetCancelable(false);

                                                // Botón Guardar
                                                builder.SetPositiveButton(Html.FromHtml(
                                                    "<font face='Comic Sans MS, arial' color='#DC3545'><b>Guardar</b></font>",
                                                    FromHtmlOptions.ModeLegacy
                                                ), SaveName);

                                                // Botón Cancelar
                                                builder.SetNegativeButton(Html.FromHtml(
                                                    "<font face='Comic Sans MS, arial' color='#DC3545'><b>Cancelar</b></font>",
                                                    FromHtmlOptions.ModeLegacy
                                                ), CancelAction);

                                                var dialog = builder.Create();
                                                dialog.Show();

                                                // Personalizar botones
                                                var positiveBtn = dialog.GetButton((int)DialogButtonType.Positive);
                                                positiveBtn?.SetAllCaps(false);
                                                positiveBtn?.SetTextColor(Android.Graphics.Color.ParseColor("#DC3545"));

                                                var negativeBtn = dialog.GetButton((int)DialogButtonType.Negative);
                                                negativeBtn?.SetAllCaps(false);
                                                negativeBtn?.SetTextColor(Android.Graphics.Color.ParseColor("#DC3545"));
                                            });
                                            #endregion


                                            #region ALERT DIALOG
                                            /*AndroidX.AppCompat.App.AlertDialog.Builder ad = new AndroidX.AppCompat.App.AlertDialog.Builder(this);
                                            ad.SetTitle("Autorizacion Folios Adelantados");
                                            ad.SetCancelable(false);
                                            ad.SetView(et);
                                            ad.SetPositiveButton(Html.FromHtml("<font face = 'Comic Sans MS, arial' color='#dc3545' size = '10'>Guardar</font>"), SaveName);
                                            ad.SetNegativeButton(Html.FromHtml("<font face = 'Comic Sans MS, arial' color='#dc3545' size = '10'>Cancelar</font>"), CancelAction);
                                            RunOnUiThread(() => ad.Show());*/
                                            #endregion
                                            //RunOnUiThread(() => fnShowCustomAlertDialogCancel());
                                        }
                                        else
                                        {
                                            RunOnUiThread(() => Guardar.Enabled = true);
                                        }
                                    }
                                    else
                                    {
                                        RunOnUiThread(() => Guardar.Enabled = false);
                                    }


                                    /*if (validando != "S" || producto != "S")
                                    {
                                        if (producto == "N" || HayExistencias == "S")
                                        {
                                            RunOnUiThread(() => Guardar.Enabled = true);
                                        }
                                    }*/

                                    ImprimirDialogs(0);
                                }
                                else
                                {
                                    ImprimirDialogs(0);
                                }


                            }

                        }

                    }
                    insertarinfoMensaje();
                    List<FlimStarInfo> lstFlimStar = detalle_lote();
                    var gvObject = FindViewById<GridView>(Resource.Id.gvCtr2);
                    RunOnUiThread(() => gvObject.Adapter = new myGVItemAdapter(this, lstFlimStar));
                    gvObject.ItemClick += new EventHandler<AdapterView.ItemClickEventArgs>(OnGridView_ItemClicked);

                }
                else
                {
                    RunOnUiThread(() =>
                    {
                        DialogHelper.ShowErrorDialog(this,
                            message: "No existen productos capturados para validar",
                            positiveText: "OK");
                    });
                    #region MATERIAL DIALOG
                    /*RunOnUiThread(() =>
                    {
                        var builder = new MaterialAlertDialogBuilder(this, Resource.Style.ThemeOverlay_Material3_MaterialAlertDialog);

                        // Título en rojo
                        builder.SetTitle(Html.FromHtml(
                            "<font color='#DC3545'><b>Sin Productos Capturados</b></font>",
                            FromHtmlOptions.ModeLegacy
                        ));

                        builder.SetIcon(Resource.Drawable.no);

                        // Mensaje en blanco
                        builder.SetMessage(Html.FromHtml(
                            "<font color='#FFFFFF'>No existen productos capturados para validar</font>",
                            FromHtmlOptions.ModeLegacy
                        ));

                        builder.SetCancelable(false);

                        // Botón OK
                        builder.SetPositiveButton(Html.FromHtml(
                            "<font color='#DC3545'><b>OK</b></font>",
                            FromHtmlOptions.ModeLegacy
                        ), delegate
                        {
                            builder.Dispose();
                        });

                        var dialog = builder.Create();
                        dialog.Show();

                        // Personalizar botón
                        var btn = dialog.GetButton((int)DialogButtonType.Positive);
                        btn?.SetTextColor(Android.Graphics.Color.ParseColor("#DC3545"));
                        btn?.SetAllCaps(false);
                    });*/
                    #endregion

                    #region ALERT DIALOG
                    /*Android.App.AlertDialog.Builder alertDialog = new Android.App.AlertDialog.Builder(this);
                    alertDialog.SetTitle(Html.FromHtml("<font color='#dc3545' size = 10>Sin Productos Capturados</font>"));
                    alertDialog.SetIcon(Resource.Drawable.no);
                    alertDialog.SetMessage(Html.FromHtml("<font color='#FFFFFF' size = 10>No existen productos capturados para validar</font>"));
                    alertDialog.SetCancelable(false);
                    alertDialog.SetNeutralButton("Ok", delegate
                    {
                        alertDialog.Dispose();
                    });
                    RunOnUiThread(() => alertDialog.Show());*/
                    #endregion
                }


                mconcen = "1";
                RunOnUiThread(() => Toast.MakeText(this, "Proceso Validado correctamente.", ToastLength.Long).Show()); //HIDE PROGRESS DIALOG 
                RunOnUiThread(() => progressDialog.Hide());
                wakeLock.Release();

            })).Start();




        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.top_menu_captura, menu);
            return base.OnCreateOptionsMenu(menu);
        }

        private void AgregaProdXPedido()
        {

            var pedidosproducto = db.Table<Pedidos>();
            foreach (var producto in pedidosproducto)
            {

                //Ver si hay registros del producto
                string Cadena = "Select CANTSURTIDO from TB_DET_SPLIT_PRODXPED where PDN_FOLIO = '" + producto.folio.ToString() + "' AND PROD_CLAVE = '" + producto.prod_clave.ToString() + "'";
                SqlCommand cmdx = new SqlCommand(Cadena, thisConnection);
                var valor = Convert.ToString(cmdx.ExecuteScalar());

                // termina ver si hay registros

                int total = traetotal(producto.prod_clave.ToString().Trim());

                string mnom = producto.nombre.ToString().Trim();
                mnom = mnom.Replace("'", " ");

                string cadena = "";

                if (valor == "")
                {
                    cadena = "INSERT INTO TB_DET_SPLIT_PRODXPED(FECHA,CVE_CAMIONETA,NOM_CAPSPLIT,PDN_FOLIO,PROD_CLAVE,PROD_NOMBRE,CANTPEDIDO,CANTSURTIDO) " +
                                "VALUES('" + System.DateTime.Now.ToString("dd/MM/yyyy") + "','" + cvecam + "','" + muser.Substring(0, 20) +
                                "','" + producto.folio.ToString() + "','" + producto.prod_clave.ToString() + "','" + mnom +
                                "','" + producto.pedido.ToString() + "','" + total + "')";
                }
                else
                {
                    cadena = "UPDATE TB_DET_SPLIT_PRODXPED  SET cantsurtido = '" + total + "' WHERE PDN_FOLIO = '" + producto.folio.ToString() + "' AND PROD_CLAVE = '" + producto.prod_clave.ToString() + "'";
                }


                SqlCommand cmd = new SqlCommand(cadena, thisConnection);
                cmd.ExecuteNonQuery();
            }
        }

        private void AgregaTempSplit()
        {

            var pedidosproducto = db.Query<Pedidos>("SELECT DISTINCT folio FROM [Pedidos]");
            foreach (var producto in pedidosproducto)
            {
                var ampmx = System.DateTime.Now.ToString("tt");
                ampmx = ampmx.Replace(" ", "");

                string cadena = "";

                if (Convert.ToInt32(nosplit.Text.Replace("Split Numero: ", "")) == 1)
                {
                    cadena = "INSERT INTO TB_TMP_PED(EMB_FOLIO, EMB_TIPO, STATUS, LUGAR, NOM_CAPSPLIT, FECHA) " +
                                "VALUES('" + producto.folio.ToString() + "', 'NAL', 'A', 'NAL', '" + muser.Trim() + "',  '" + System.DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss") + " " + ampmx + "')";

                    SqlCommand cmd = new SqlCommand(cadena, thisConnection);
                    cmd.ExecuteNonQuery();
                }


            }
        }

        private void AgregaRegistroPedidoAuto(string Mped, string mDet)
        {
            string cadena = "INSERT INTO TB_REGISTRO_MOVIMIENTOS(FECHA,NOM_COMPU,NOM_USU,TIPO_MOV,OP_CLAVE,FOLIO,DETALLE,SISTEMA,MOV_FOLIO) " +
                            "VALUES('" + System.DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + "','CEL " + imei + "','" + mAutoriza.Trim() + "','A','7.10','" +
                            Mped + "','" + mDet + "','SPLIT','" + Mped + "')";
            //MessageBox.Show(cadena);
            SqlCommand cmd = new SqlCommand(cadena, thisConnection);
            cmd.ExecuteNonQuery();
        }

        private void ConsPedSurdos(string mped)
        {
            db.Query<ConPedidos>("UPDATE [ConPedidos] SET surtido = 0");
            thisConnection.Open();


            string Cadena = "Select SUM(a.pdn_num_unidades) AS Pedidos From tb_det_pedidos A, tb_Cat_producto B " +
                                "where a.pdn_folio = '" + mped.Trim() + "' and a.prod_clave = b.prod_clave";
            SqlCommand cmd = new SqlCommand(Cadena, thisConnection);
            int cantped = Convert.ToInt32(cmd.ExecuteScalar());




            string cadena = "Select * From tb_det_pedidos A, tb_Cat_producto B where a.pdn_folio = '" + mped.Trim() + "' and a.prod_clave = b.prod_clave";
            SqlDataAdapter da = new SqlDataAdapter(cadena, thisConnection);
            DataSet ds = new DataSet();
            da.Fill(ds, "ConsPed");
            var ConsPed = ds.Tables["ConsPed"];

            if (mped.Length > 0)
            {
                if (Convert.ToInt32(mped) < 300000)
                {
                    mped = "0" + mped;
                }
            }


            cadena = "Select prod_clave, sum(cajas) as cajas from tb_det_split Where emb_folio = '" + mped.Trim() + "'" +
                     " AND estatus != 'C' Group By prod_clave Order by prod_clave ASC";
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


            //RECORRIDO SI HAY PRODUCTO CAPTURADO
            var quex = db.Table<xprod>();
            foreach (var captu in quex)
            {
                db.Query<Pedidos>("UPDATE [Pedidos] SET surtido = surtido + " + 1 + " WHERE prod_clave = '" + captu.Codigo.ToString().Trim() + "'");
                db.Query<ConPedidos>("UPDATE [ConPedidos] SET surtido = surtido + " + 1 + " WHERE prod_clave = '" + captu.Codigo.ToString().Trim() + "'");
            }

            //***********************************

        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            if (item.TitleFormatted.ToString() == "Limpiar")
            {
                total.Text = "000";
                TotCaj = 0;
                foliocaptura.Text = "";
                Guardar.Enabled = false;
                foliocaptura.RequestFocus();
                db.Query<xLote>("delete from  [xLote]");
                db.Query<xprod>("delete from  [xprod]");
                db.Query<Pedidos>("UPDATE [Pedidos] SET surtido = '0'");
                db.Query<ConPedidos>("UPDATE [ConPedidos] SET surtido = '0'");
                db.Query<ConPedidos>("Delete FROM [ConPedidos] WHERE pedido = '0'");

                ConsPedSurdos(pedidoencaptura.Text.Replace("Pedido Actual: ", ""));

                List<FlimStarInfo> lstFlimStar = detalle_pedido();
                lstFlimStar.Clear();
                var gvObject = FindViewById<GridView>(Resource.Id.gvCtr2);
                gvObject.Adapter = new myGVItemAdapter(this, null);
                gvObject.Adapter = null;
                gvObject.Adapter = new myGVItemAdapter(this, lstFlimStar);
                gvObject.ItemClick += new EventHandler<AdapterView.ItemClickEventArgs>(OnGridView_ItemClicked);
                mconcen = "1";

                var quex = db.Table<Pedidos>();
                foreach (var captu in quex)
                {
                    ConsPedSur(captu.folio.ToString());
                }

                Toast.MakeText(this, "La informacion ha sido limpiada", ToastLength.Short).Show();

            }
            else if (item.TitleFormatted.ToString() == "Validar")
            {
                if (_nfcAdapter == null)
                {
                    RunOnUiThread(() =>
                    {
                        DialogHelper.ShowErrorDialog(this,
                            message: "NFC no soportado en el dispositivo.",
                            positiveText: "OK");
                    });
                    #region MATERIAL DIALOG
                    /*RunOnUiThread(() =>
                    {
                        var builder = new MaterialAlertDialogBuilder(this, Resource.Style.ThemeOverlay_Material3_MaterialAlertDialog);

                        // Título en rojo
                        builder.SetTitle(Html.FromHtml(
                            "<font color='#DC3545'><b>NFC No Disponible</b></font>",
                            FromHtmlOptions.ModeLegacy
                        ));

                        // Mensaje en blanco
                        builder.SetMessage(Html.FromHtml(
                            "<font color='#FFFFFF'>NFC no soportado en el dispositivo.</font>",
                            FromHtmlOptions.ModeLegacy
                        ));

                        builder.SetCancelable(true);

                        // Botón OK
                        builder.SetPositiveButton(Html.FromHtml(
                            "<font color='#DC3545'><b>OK</b></font>",
                            FromHtmlOptions.ModeLegacy
                        ), delegate
                        {
                            builder.Dispose();
                        });

                        var dialog = builder.Create();
                        dialog.Show();

                        // Personalización del botón
                        var btn = dialog.GetButton((int)DialogButtonType.Positive);
                        btn?.SetTextColor(Android.Graphics.Color.ParseColor("#DC3545"));
                        btn?.SetAllCaps(false);
                    });*/
                    #endregion

                    #region ALERT DIALOG
                    /*var alert = new Android.App.AlertDialog.Builder(this).Create();
                    alert.SetMessage("NFC No soportado en el dispositivo.");
                    alert.SetTitle("NFC No Disponible");
                    alert.Show();*/
                    #endregion
                }
                else
                {

                    var tagDetected = new IntentFilter(NfcAdapter.ActionTagDiscovered);
                    var ndefDetected = new IntentFilter(NfcAdapter.ActionNdefDiscovered);
                    var techDetected = new IntentFilter(NfcAdapter.ActionTechDiscovered);
                    var filters = new[] { ndefDetected, tagDetected, techDetected };

                    var intent = new Intent(this, GetType()).AddFlags(ActivityFlags.SingleTop);

                    var pendingIntent = PendingIntent.GetActivity(this, 0, intent, 0);

                    // Gives your current foreground activity priority in receiving NFC events over all other activities.
                    _nfcAdapter.EnableForegroundDispatch(this, pendingIntent, filters, null);
                }

                actualizar_detalle();
                //Evitar apagar la pantalla*********************************************************************************************
                var pm = PowerManager.FromContext(context);
                var wakeLock = pm.NewWakeLock(WakeLockFlags.Full, "Validar");
                wakeLock.Acquire();
                //Adquirir el wakelock**************************************************************************************************

                var progressDialog = ProgressDialog.Show(this, "Espere Por Favor...", "Validando Informacion Capturada...", true);


                new System.Threading.Thread(new ThreadStart(delegate
                {//LOAD METHOD TO GET ACCOUNT INFO

                    //try
                    //{
                    dondegenera = "Iniciovalidar";
                    string existenproductos = "NO";
                    var existeCapturado = db.Table<xprod>();
                    foreach (var captu in existeCapturado)
                    {
                        existenproductos = "SI";
                        break;
                    }

                    if (existenproductos == "SI")
                    {
                        insertarinfo();
                        if (validaestructuraetiqueta() == "SI")
                        {
                            db.Query<Mensajes>("delete from  [Mensajes]");
                            AutoPed = "N";
                            RunOnUiThread(() => Guardar.Enabled = false);

                            var validando = valida();
                            var producto = validaprod();
                            //var validandofec = validafecad();
                            //var validandofec = validafecadMod();
                            var validandofec = validafecadMAXIMOS();

                            if (Surtidomayor == "NR")
                            {
                                ImprimirDialogs(0);
                                RunOnUiThread(() => Guardar.Enabled = false);
                            }
                            else if (HayExistencias != "S")
                            {
                                ImprimirDialogs(0);
                            }
                            else if (ValiMinFechaPTC != "S")
                            {
                                ImprimirDialogs(0);
                                RunOnUiThread(() => Guardar.Enabled = false);
                            }
                            else
                            {
                                if (Surtidomayor == "NR" || EtiquetaCapturada == "N")
                                {
                                    ImprimirDialogs(0);
                                }
                                else if (OrdenExiste == "N")
                                {
                                    ImprimirDialogs(0);
                                }
                                else
                                {
                                    if (EtiquetaExiste == "S")
                                    {
                                        if (producto == "S" && (validando == "S"))
                                        {
                                            if ((ValiFechacad == "N"))
                                            {
                                                RunOnUiThread(() => Guardar.Enabled = false);

                                                et = new EditText(this);
                                                et.InputType = Android.Text.InputTypes.TextVariationPassword | Android.Text.InputTypes.ClassText;
                                                et.LongClickable = false;
                                                et.Hint = "Password";

                                                #region MATERIAL DIALOG
                                                RunOnUiThread(() =>
                                                {
                                                    var builder = new MaterialAlertDialogBuilder(this, Resource.Style.ThemeOverlay_Material3_MaterialAlertDialog);

                                                    // Título en rojo y en negritas
                                                    builder.SetTitle(Html.FromHtml(
                                                        "<font color='#DC3545'><b>Autorización Folios Adelantados</b></font>",
                                                        FromHtmlOptions.ModeLegacy
                                                    ));

                                                    // Vista personalizada
                                                    builder.SetView(et);

                                                    builder.SetCancelable(false);

                                                    // Botón Guardar
                                                    builder.SetPositiveButton(Html.FromHtml(
                                                        "<font face='Comic Sans MS, arial' color='#DC3545'><b>Guardar</b></font>",
                                                        FromHtmlOptions.ModeLegacy
                                                    ), SaveName);

                                                    // Botón Cancelar
                                                    builder.SetNegativeButton(Html.FromHtml(
                                                        "<font face='Comic Sans MS, arial' color='#DC3545'><b>Cancelar</b></font>",
                                                        FromHtmlOptions.ModeLegacy
                                                    ), CancelAction);

                                                    var dialog = builder.Create();
                                                    dialog.Show();

                                                    // Personalizar botones
                                                    var positiveBtn = dialog.GetButton((int)DialogButtonType.Positive);
                                                    positiveBtn?.SetTextColor(Android.Graphics.Color.ParseColor("#DC3545"));
                                                    positiveBtn?.SetAllCaps(false);

                                                    var negativeBtn = dialog.GetButton((int)DialogButtonType.Negative);
                                                    negativeBtn?.SetTextColor(Android.Graphics.Color.ParseColor("#DC3545"));
                                                    negativeBtn?.SetAllCaps(false);
                                                });
                                                #endregion

                                                #region ALERT DIALOG
                                                /*AndroidX.AppCompat.App.AlertDialog.Builder ad = new AndroidX.AppCompat.App.AlertDialog.Builder(this);
                                                ad.SetTitle("Autorizacion Folios Adelantados");
                                                ad.SetCancelable(false);
                                                ad.SetView(et);
                                                ad.SetPositiveButton(Html.FromHtml("<font face = 'Comic Sans MS, arial' color='#dc3545' size = '10'>Guardar</font>"), SaveName);
                                                ad.SetNegativeButton(Html.FromHtml("<font face = 'Comic Sans MS, arial' color='#dc3545' size = '10'>Cancelar</font>"), CancelAction);
                                                RunOnUiThread(() => ad.Show());*/
                                                #endregion
                                                //RunOnUiThread(() => fnShowCustomAlertDialogCancel());
                                            }
                                            else
                                            {
                                                RunOnUiThread(() => Guardar.Enabled = true);
                                            }
                                        }
                                        else
                                        {
                                            RunOnUiThread(() => Guardar.Enabled = false);
                                        }


                                        /*if (validando != "S" || producto != "S")
                                        {
                                            if (producto == "N" || HayExistencias == "S")
                                            {
                                                RunOnUiThread(() => Guardar.Enabled = true);
                                            }
                                        }*/

                                        ImprimirDialogs(0);
                                    }
                                    else
                                    {
                                        ImprimirDialogs(0);
                                    }


                                }

                            }

                        }
                        insertarinfoMensaje();
                        List<FlimStarInfo> lstFlimStar = detalle_lote();
                        var gvObject = FindViewById<GridView>(Resource.Id.gvCtr2);
                        RunOnUiThread(() => gvObject.Adapter = new myGVItemAdapter(this, lstFlimStar));
                        gvObject.ItemClick += new EventHandler<AdapterView.ItemClickEventArgs>(OnGridView_ItemClicked);

                    }
                    else
                    {
                        RunOnUiThread(() =>
                        {
                            DialogHelper.ShowErrorDialog(this,
                                message: "No existen productos capturados para validar",
                                positiveText: "OK");
                        });
                        #region MATERIAL DIALOG
                        /*RunOnUiThread(() =>
                        {
                            var builder = new MaterialAlertDialogBuilder(this, Resource.Style.ThemeOverlay_Material3_MaterialAlertDialog);

                            // Título en rojo y negritas
                            builder.SetTitle(Html.FromHtml(
                                "<font color='#DC3545'><b>Sin Productos Capturados</b></font>",
                                FromHtmlOptions.ModeLegacy
                            ));

                            builder.SetIcon(Resource.Drawable.no);

                            // Mensaje en blanco
                            builder.SetMessage(Html.FromHtml(
                                "<font color='#FFFFFF'>No existen productos capturados para validar</font>",
                                FromHtmlOptions.ModeLegacy
                            ));

                            builder.SetCancelable(false);

                            // Botón OK
                            builder.SetPositiveButton(Html.FromHtml(
                                "<font color='#DC3545'><b>OK</b></font>",
                                FromHtmlOptions.ModeLegacy
                            ), delegate
                            {
                                builder.Dispose();
                            });

                            var dialog = builder.Create();
                            dialog.Show();

                            // Personalizar botón
                            var btn = dialog.GetButton((int)DialogButtonType.Positive);
                            btn?.SetTextColor(Android.Graphics.Color.ParseColor("#DC3545"));
                            btn?.SetAllCaps(false);
                        });*/
                        #endregion

                        #region ALERT DIALOG
                        /*Android.App.AlertDialog.Builder alertDialog = new Android.App.AlertDialog.Builder(this);
                        alertDialog.SetTitle(Html.FromHtml("<font color='#dc3545' size = 10>Sin Productos Capturados</font>"));
                        alertDialog.SetIcon(Resource.Drawable.no);
                        alertDialog.SetMessage(Html.FromHtml("<font color='#FFFFFF' size = 10>No existen productos capturados para validar</font>"));
                        alertDialog.SetCancelable(false);
                        alertDialog.SetNeutralButton("Ok", delegate
                        {
                            alertDialog.Dispose();
                        });
                        RunOnUiThread(() => alertDialog.Show());*/
                        #endregion
                    }


                    mconcen = "1";
                    RunOnUiThread(() => Toast.MakeText(this, "Proceso Validado correctamente.", ToastLength.Long).Show()); //HIDE PROGRESS DIALOG 
                    RunOnUiThread(() => progressDialog.Hide());
                    wakeLock.Release();

                })).Start();



            }
            else if (item.TitleFormatted.ToString() == "Reetiquetar")
            {
                Intent intent = new Intent(this, typeof(solicitarreimpresion));
                intent.PutExtra("cvresponsable", cvresponsable.ToString());
                intent.PutExtra("responsable", responsable.ToString());
                intent.PutExtra("embarque", pedidoencaptura.Text.Replace("Pedido Actual: ", "").ToString());
                intent.PutExtra("currentVersionName", currentVersionName.ToString().Trim());
                intent.PutExtra("imei", imei.ToString().Trim());
                StartActivity(intent);


            }
            else
            {
                mconcen = "2";
                List<FlimStarInfo> lstFlimStar = detalle_pedido();
                var gvObject = FindViewById<GridView>(Resource.Id.gvCtr2);
                gvObject.Adapter = new myGVItemAdapter(this, lstFlimStar);
                gvObject.ItemClick += new EventHandler<AdapterView.ItemClickEventArgs>(OnGridView_ItemClicked); //detalle_pedido
                Toast.MakeText(this, "Modo Concentrado Activado", ToastLength.Short).Show();

            }


            return base.OnOptionsItemSelected(item);
        }

        private void CancelAction(object sender, DialogClickEventArgs e)
        {
            return;
        }

        private void SaveName(object sender, DialogClickEventArgs e)
        {
            //nombre_recibido = et.Text.Trim().ToUpper();
            Guardar.Enabled = false;
            thisConnection.Open();
            string cadena = "Select usuario,password From tb_Autoriza_OdeP Where password = '" + et.Text.Trim().ToUpper() + "' AND clave = 'EM' AND obs = 'Autoriza Caducidad'";
            SqlCommand cmd = new SqlCommand(cadena, thisConnection);
            mAutoriza = Convert.ToString(cmd.ExecuteScalar());
            if (mAutoriza.Trim().Length == 0)
            {
                Toast.MakeText(this, "PASSWORD INCORRECTO!!!", ToastLength.Short).Show();
                thisConnection.Close();
            }
            else
            {
                if (mAutoriza.Trim() == "USER X")
                {

                    cadena = "SELECT DATENAME(dw, getdate())";
                    cmd = new SqlCommand(cadena, thisConnection);
                    string diadelasemana = Convert.ToString(cmd.ExecuteScalar());

                    if (diadelasemana == "Domingo")
                    {
                        thisConnection.Close();
                        AutoPed = "S";
                        Guardar.Enabled = true;
                        return;
                    }
                    else
                    {
                        cadena = "SELECT Convert(varchar(8),GetDate(), 108) HoraServidor";
                        cmd = new SqlCommand(cadena, thisConnection);
                        string horasemana = Convert.ToString(cmd.ExecuteScalar());

                        if ((Convert.ToDateTime(horasemana) > Convert.ToDateTime("22:45:00")) || (Convert.ToDateTime(horasemana) < Convert.ToDateTime("07:15:00")))
                        {
                            thisConnection.Close();
                            AutoPed = "S";
                            Guardar.Enabled = true;

                            #region MATERIAL DIALOG - Selección única
                            RunOnUiThread(() =>
                            {
                                string[] items = new string[] { "Requerido Por Cliente", "Caja Inexistente", "Caja No Encontrada", "No Apto Para Carga" };

                                var builder = new MaterialAlertDialogBuilder(this, Resource.Style.ThemeOverlay_Material3_MaterialAlertDialog);

                                // Título en rojo y negritas
                                builder.SetTitle(Html.FromHtml(
                                    "<font color='#DC3545'><b>Motivo de Folio Adelantado</b></font>",
                                    FromHtmlOptions.ModeLegacy
                                ));

                                builder.SetCancelable(false);

                                int selectedIndex = 0; // opción por defecto

                                builder.SetSingleChoiceItems(items, selectedIndex, new EventHandler<DialogClickEventArgs>((senderx, erre) =>
                                {
                                    var d = senderx as Android.App.AlertDialog;

                                    // Guardar selección y mostrar toast
                                    motfolade = items[erre.Which].Trim();
                                    Toast.MakeText(this, $"Seleccionado: {motfolade}", ToastLength.Short).Show();

                                    // Cerrar diálogo
                                    d.Dismiss();
                                }));

                                builder.SetPositiveButton("OK", delegate { });

                                var dialog = builder.Create();
                                dialog.Show();

                                // Personalización opcional de botón OK
                                var btn = dialog.GetButton((int)DialogButtonType.Positive);
                                btn?.SetTextColor(Android.Graphics.Color.ParseColor("#DC3545"));
                                btn?.SetAllCaps(false);
                            });
                            #endregion

                            #region ALERT DIALOG
                            /*AndroidX.AppCompat.App.AlertDialog.Builder ad = new AndroidX.AppCompat.App.AlertDialog.Builder(this);
                            string[] items = new string[] { "Requerido Por Cliente", "Caja Inexistente", "Caja No Encontrada", "No Apto Para Carga" };
                            ad.SetTitle("Motivo de Folio Adelantado");
                            ad.SetCancelable(false);
                            ad.SetSingleChoiceItems(items, 0, new EventHandler<DialogClickEventArgs>(delegate (object senderx, DialogClickEventArgs erre)
                            {
                                // Get reference to AlertDialog
                                var d = (senderx as Android.App.AlertDialog);

                                // Do something with selected index
                                Toast.MakeText(this, $"Seleccionado: {items[erre.Which]}", ToastLength.Short).Show();
                                motfolade = items[erre.Which].Trim();

                                //Dismiss Dialog
                                d.Dismiss();
                                //return;
                            }));
                            ad.SetPositiveButton("OK", delegate { });
                            ad.Show();*/
                            #endregion

                            return;
                        }
                        else
                        {

                            if ((Convert.ToDateTime(horasemana) > Convert.ToDateTime("10:25:00")) && (Convert.ToDateTime(horasemana) < Convert.ToDateTime("11:05:00")))
                            {
                                thisConnection.Close();
                                AutoPed = "S";
                                Guardar.Enabled = true;

                                #region MATERIAL DIALOG - Selección única
                                RunOnUiThread(() =>
                                {
                                    string[] items = new string[] { "Requerido Por Cliente", "Caja Inexistente", "Caja No Encontrada", "No Apto Para Carga" };

                                    var builder = new MaterialAlertDialogBuilder(this, Resource.Style.ThemeOverlay_Material3_MaterialAlertDialog);

                                    // Título en rojo y negritas
                                    builder.SetTitle(Html.FromHtml(
                                        "<font color='#DC3545'><b>Motivo de Folio Adelantado</b></font>",
                                        FromHtmlOptions.ModeLegacy
                                    ));

                                    builder.SetCancelable(false);

                                    int selectedIndex = 0; // opción por defecto

                                    // Lista de selección única
                                    builder.SetSingleChoiceItems(items, selectedIndex, new EventHandler<DialogClickEventArgs>((senderx, erre) =>
                                    {
                                        var dialog = senderx as Android.App.AlertDialog;

                                        // Guardar selección y mostrar toast
                                        motfolade = items[erre.Which].Trim();
                                        Toast.MakeText(this, $"Seleccionado: {motfolade}", ToastLength.Short).Show();

                                        // Cerrar diálogo tras selección
                                        dialog.Dismiss();
                                    }));

                                    // Botón OK
                                    builder.SetPositiveButton("OK", delegate { });

                                    var dialogCreated = builder.Create();
                                    dialogCreated.Show();

                                    // Personalizar botón OK
                                    var btn = dialogCreated.GetButton((int)DialogButtonType.Positive);
                                    btn?.SetTextColor(Android.Graphics.Color.ParseColor("#DC3545"));
                                    btn?.SetAllCaps(false);
                                });
                                #endregion


                                #region ALERT DIALOG
                                /*AndroidX.AppCompat.App.AlertDialog.Builder ad = new AndroidX.AppCompat.App.AlertDialog.Builder(this);
                                string[] items = new string[] { "Requerido Por Cliente", "Caja Inexistente", "Caja No Encontrada", "No Apto Para Carga" };
                                ad.SetTitle("Motivo de Folio Adelantado");
                                ad.SetCancelable(false);
                                ad.SetSingleChoiceItems(items, 0, new EventHandler<DialogClickEventArgs>(delegate (object senderx, DialogClickEventArgs erre)
                                {
                                    // Get reference to AlertDialog
                                    var d = (senderx as Android.App.AlertDialog);

                                    // Do something with selected index
                                    Toast.MakeText(this, $"Seleccionado: {items[erre.Which]}", ToastLength.Short).Show();
                                    motfolade = items[erre.Which].Trim();

                                    //Dismiss Dialog
                                    d.Dismiss();
                                    //return;
                                }));
                                ad.SetPositiveButton("OK", delegate { });
                                ad.Show();*/
                                #endregion

                                return;
                            }
                            else
                            {

                                if ((Convert.ToDateTime(horasemana) > Convert.ToDateTime("17:55:00")) && (Convert.ToDateTime(horasemana) < Convert.ToDateTime("18:35:00")))
                                {
                                    thisConnection.Close();
                                    AutoPed = "S";
                                    Guardar.Enabled = true;

                                    #region MATERIAL DIALOG - Selección Única
                                    RunOnUiThread(() =>
                                    {
                                        string[] items = new string[] { "Requerido Por Cliente", "Caja Inexistente", "Caja No Encontrada", "No Apto Para Carga" };

                                        // Construimos el diálogo con Material3
                                        var builder = new MaterialAlertDialogBuilder(this, Resource.Style.ThemeOverlay_Material3_MaterialAlertDialog);

                                        // Título en rojo y negritas
                                        builder.SetTitle(Html.FromHtml(
                                            "<font color='#DC3545'><b>Motivo de Folio Adelantado</b></font>",
                                            FromHtmlOptions.ModeLegacy
                                        ));

                                        builder.SetCancelable(false);

                                        int selectedIndex = 0; // opción por defecto

                                        // Lista de selección única
                                        builder.SetSingleChoiceItems(items, selectedIndex, new EventHandler<DialogClickEventArgs>((sender, e) =>
                                        {
                                            var dialog = sender as Android.App.AlertDialog;

                                            // Guardar selección y mostrar toast
                                            motfolade = items[e.Which].Trim();
                                            Toast.MakeText(this, $"Seleccionado: {motfolade}", ToastLength.Short).Show();

                                            // Cerrar diálogo
                                            dialog.Dismiss();
                                        }));

                                        // Botón OK
                                        builder.SetPositiveButton("OK", delegate { });

                                        // Crear y mostrar diálogo
                                        var dialogCreated = builder.Create();
                                        dialogCreated.Show();

                                        // Personalizar botón OK
                                        var btn = dialogCreated.GetButton((int)DialogButtonType.Positive);
                                        btn?.SetTextColor(Android.Graphics.Color.ParseColor("#DC3545"));
                                        btn?.SetAllCaps(false);
                                    });
                                    #endregion

                                    #region ALERT DIALOG
                                    /*AndroidX.AppCompat.App.AlertDialog.Builder ad = new AndroidX.AppCompat.App.AlertDialog.Builder(this);
                                    string[] items = new string[] { "Requerido Por Cliente", "Caja Inexistente", "Caja No Encontrada", "No Apto Para Carga" };
                                    ad.SetTitle("Motivo de Folio Adelantado");
                                    ad.SetCancelable(false);
                                    ad.SetSingleChoiceItems(items, 0, new EventHandler<DialogClickEventArgs>(delegate (object senderx, DialogClickEventArgs erre)
                                    {
                                        // Get reference to AlertDialog
                                        var d = (senderx as Android.App.AlertDialog);

                                        // Do something with selected index
                                        Toast.MakeText(this, $"Seleccionado: {items[erre.Which]}", ToastLength.Short).Show();
                                        motfolade = items[erre.Which].Trim();

                                        //Dismiss Dialog
                                        d.Dismiss();
                                        //return;
                                    }));
                                    ad.SetPositiveButton("OK", delegate { });
                                    ad.Show();*/
                                    #endregion

                                    return;
                                }
                                else
                                {
                                    RunOnUiThread(() =>
                                    {
                                        DialogHelper.ShowErrorDialog(this,
                                            message: "El Usuario X estará disponible de 11:00 pm a 7:00 am, de 10:30 am a 11:00 am, para realizar autorizaciones. Por favor acuda con los encargados en turno.",
                                            positiveText: "Ok");
                                    });
                                    #region MATERIAL DIALOG - Usuario No Disponible
                                    /*RunOnUiThread(() =>
                                    {
                                        // Construimos el título con color rojo y negritas
                                        var titleSpannable = new SpannableStringBuilder("Usuario No Disponible para Autorizar Folio Adelantado");
                                        titleSpannable.SetSpan(new ForegroundColorSpan(Color.ParseColor("#DC3545")), 0, titleSpannable.Length(), SpanTypes.ExclusiveExclusive);
                                        titleSpannable.SetSpan(new StyleSpan(TypefaceStyle.Bold), 0, titleSpannable.Length(), SpanTypes.ExclusiveExclusive);

                                        // Construimos el mensaje con color blanco
                                        var mensajeSpannable = new SpannableStringBuilder("El Usuario X estará disponible de 11:00 pm a 7:00 am, de 10:30 am a 11:00 am, para realizar autorizaciones. Por favor acuda con los encargados en turno.");
                                        mensajeSpannable.SetSpan(new ForegroundColorSpan(Color.ParseColor("#FFFFFF")), 0, mensajeSpannable.Length(), SpanTypes.ExclusiveExclusive);

                                        // Creamos el diálogo usando Material3
                                        var builder = new MaterialAlertDialogBuilder(this, Resource.Style.ThemeOverlay_Material3_MaterialAlertDialog);
                                        builder.SetTitle(titleSpannable);
                                        builder.SetIcon(Resource.Drawable.no);
                                        builder.SetMessage(mensajeSpannable);
                                        builder.SetCancelable(false);

                                        // Botón OK
                                        builder.SetPositiveButton("Ok", (s, e) => { });

                                        // Crear y mostrar el diálogo
                                        var dialog = builder.Create();
                                        dialog.Show();

                                        // Personalizamos el botón OK
                                        dialog.Window.DecorView.Post(() =>
                                        {
                                            var positiveButton = dialog.GetButton((int)DialogButtonType.Positive);
                                            positiveButton?.SetTextColor(Color.ParseColor("#DC3545")); // Rojo
                                            positiveButton?.SetAllCaps(false);
                                        });
                                    });*/
                                    #endregion

                                    #region ALERT DIALOG
                                    /*Android.App.AlertDialog.Builder alertDialog = new Android.App.AlertDialog.Builder(this);
                                    alertDialog.SetTitle(Html.FromHtml("<font color='#dc3545' size = 10>Usuario No Disponible para Autorizar Folio Adelantado</font>"));
                                    alertDialog.SetIcon(Resource.Drawable.no);
                                    alertDialog.SetMessage(Html.FromHtml("<font color='#FFFFFF' size = 10>El Usuario X estara disponible de 11:00 pm a 7:00 am, de 10:30 am a 11:00 am, de  para realizar Autorizaciones, Por favor Acuda con los encargados en turno</font>"));
                                    alertDialog.SetCancelable(false);
                                    alertDialog.SetNeutralButton("Ok", delegate
                                    {
                                        alertDialog.Dispose();
                                    });
                                    alertDialog.Show();*/
                                    #endregion
                                }

                            }
                        }
                    }
                }
                else
                {
                    thisConnection.Close();
                    AutoPed = "S";
                    Guardar.Enabled = true;
                    RunOnUiThread(() =>
                    {
                        string[] items = new string[] { "Requerido Por Cliente", "Caja Inexistente", "Caja No Encontrada", "No Apto Para Carga" };
                        DialogHelper.ShowSingleChoiceDialog(this,
                            title: "Motivo de Folio Adelantado",
                            items: items,
                            checkedItem: 0,
                            itemSelected: (senderx, erre) =>
                            {
                                var d = senderx as Android.App.AlertDialog;
                                motfolade = items[erre.Which].Trim();
                                Toast.MakeText(this, $"Seleccionado: {motfolade}", ToastLength.Short).Show();
                                d?.Dismiss();
                            },
                            positiveText: "OK");
                    });
                    #region MATERIAL DIALOG - Motivo de Folio Adelantado
                    /*RunOnUiThread(() =>
                    {
                        string[] items = new string[] { "Requerido Por Cliente", "Caja Inexistente", "Caja No Encontrada", "No Apto Para Carga" };

                        var builder = new MaterialAlertDialogBuilder(this, Resource.Style.ThemeOverlay_Material3_MaterialAlertDialog);
                        builder.SetTitle("Motivo de Folio Adelantado");
                        builder.SetCancelable(false);

                        // Configurar selección única
                        builder.SetSingleChoiceItems(items, 0, (senderx, erre) =>
                        {
                            // Obtener referencia al diálogo
                            var dialog = senderx as AndroidX.AppCompat.App.AlertDialog;

                            // Guardar selección
                            motfolade = items[erre.Which].Trim();

                            // Mostrar Toast
                            Toast.MakeText(this, $"Seleccionado: {motfolade}", ToastLength.Short).Show();

                            // Cerrar diálogo
                            dialog?.Dismiss();
                        });

                        // Botón OK adicional (opcional)
                        builder.SetPositiveButton("OK", (s, e) => { });

                        // Crear y mostrar
                        var dialogFinal = builder.Create();
                        dialogFinal.Show();

                        // Personalizar botón OK
                        dialogFinal.Window.DecorView.Post(() =>
                        {
                            var positiveButton = dialogFinal.GetButton((int)DialogButtonType.Positive);
                            positiveButton?.SetTextColor(Color.ParseColor("#00695C")); // Verde Material
                            positiveButton?.SetAllCaps(false);
                        });
                    });*/
                    #endregion

                    #region ALERTD DIALOG
                    /*AndroidX.AppCompat.App.AlertDialog.Builder ad = new AndroidX.AppCompat.App.AlertDialog.Builder(this);
                    string[] items = new string[] { "Requerido Por Cliente", "Caja Inexistente", "Caja No Encontrada", "No Apto Para Carga" };
                    ad.SetTitle("Motivo de Folio Adelantado");
                    ad.SetCancelable(false);
                    ad.SetSingleChoiceItems(items, 0, new EventHandler<DialogClickEventArgs>(delegate (object senderx, DialogClickEventArgs erre)
                    {
                        // Get reference to AlertDialog
                        var d = (senderx as Android.App.AlertDialog);

                        // Do something with selected index
                        Toast.MakeText(this, $"Seleccionado: {items[erre.Which]}", ToastLength.Short).Show();
                        motfolade = items[erre.Which].Trim();

                        //Dismiss Dialog
                        d.Dismiss();
                        //return;
                    }));
                    ad.SetPositiveButton("OK", delegate { });
                    ad.Show();*/
                    #endregion

                    return;
                }
            }

        }

        private EventHandler<DialogClickEventArgs> Guardarvalor()
        {
            throw new NotImplementedException();
        }

        private void ImprimirDialogs(int mensaje)
        {
            int mensajeactual = 0;
            var query = db.Table<Mensajes>();
            foreach (var captu in query)
            {
                if (mensajeactual == mensaje)
                {
                    if (captu.titulo.Trim() == "Existe un folio anterior disponible")
                    {
                        #region MATERIAL DIALOG LEGACY - Mensaje de Advertencia
                        RunOnUiThread(() =>
                        {
                            // Construimos el título con color
                            var titleSpannable = new SpannableStringBuilder(captu.titulo.ToString());
                            titleSpannable.SetSpan(new ForegroundColorSpan(Color.ParseColor("#FCEC70")), 0, titleSpannable.Length(), SpanTypes.ExclusiveExclusive);
                            titleSpannable.SetSpan(new StyleSpan(TypefaceStyle.Bold), 0, titleSpannable.Length(), SpanTypes.ExclusiveExclusive);

                            // Construimos el mensaje
                            var mensajeSpannable = new SpannableStringBuilder(captu.mensaje.ToString());
                            mensajeSpannable.SetSpan(new ForegroundColorSpan(Color.ParseColor("#E0F1FA")), 0, mensajeSpannable.Length(), SpanTypes.ExclusiveExclusive);

                            // Creamos el diálogo
                            var builder = new MaterialAlertDialogBuilder(this, Resource.Style.ThemeOverlay_Material3_MaterialAlertDialog);
                            builder.SetTitle(titleSpannable);
                            builder.SetIcon(Resource.Drawable.warning);
                            builder.SetMessage(mensajeSpannable);
                            builder.SetCancelable(false);

                            // Botón principal
                            builder.SetPositiveButton("Ok", (s, e) =>
                            {
                                ImprimirDialogs(mensaje + 1);
                            });

                            // Crear y mostrar el diálogo
                            var dialog = builder.Create();
                            dialog.Show();

                            // Personalizamos el botón después de mostrarlo
                            dialog.Window.DecorView.Post(() =>
                            {
                                var positiveButton = dialog.GetButton((int)DialogButtonType.Positive);
                                positiveButton?.SetTextColor(Color.ParseColor("#00695C")); // Verde Material
                                positiveButton?.SetAllCaps(false);
                            });
                        });
                        #endregion

                        #region ALERT DIALOG
                        /*Android.App.AlertDialog.Builder alertDialog = new Android.App.AlertDialog.Builder(this);
                        alertDialog.SetTitle(Html.FromHtml("<font color='#FCEC70' size = 10>" + captu.titulo.ToString() + "</font>"));
                        alertDialog.SetIcon(Resource.Drawable.warning);
                        alertDialog.SetMessage(Html.FromHtml("<font color='#E0F1FA' size = 10>" + captu.mensaje.ToString() + "</font>"));
                        alertDialog.SetCancelable(false);
                        alertDialog.SetNeutralButton("Ok", delegate
                        {
                            alertDialog.Dispose();
                            ImprimirDialogs(mensaje + 1);

                        });
                        RunOnUiThread(() => alertDialog.Show());*/
                        #endregion

                    }
                    else if (captu.titulo.Trim() == "Etiqueta ya capturada" || captu.titulo.Trim() == "Etiqueta ya capturada En PreSplit")
                    {
                        #region MATERIAL DIALOG - Mensaje Informativo
                        RunOnUiThread(() =>
                        {
                            // Construimos el título con color
                            var titleSpannable = new SpannableStringBuilder(captu.titulo.ToString());
                            titleSpannable.SetSpan(new ForegroundColorSpan(Color.ParseColor("#3dc2ff")), 0, titleSpannable.Length(), SpanTypes.ExclusiveExclusive);
                            titleSpannable.SetSpan(new StyleSpan(TypefaceStyle.Bold), 0, titleSpannable.Length(), SpanTypes.ExclusiveExclusive);

                            // Construimos el mensaje con color
                            var mensajeSpannable = new SpannableStringBuilder(captu.mensaje.ToString());
                            mensajeSpannable.SetSpan(new ForegroundColorSpan(Color.ParseColor("#50c8ff")), 0, mensajeSpannable.Length(), SpanTypes.ExclusiveExclusive);

                            // Creamos el diálogo usando Material3
                            var builder = new MaterialAlertDialogBuilder(this, Resource.Style.ThemeOverlay_Material3_MaterialAlertDialog);
                            builder.SetTitle(titleSpannable);
                            builder.SetIcon(Resource.Drawable.Info);
                            builder.SetMessage(mensajeSpannable);
                            builder.SetCancelable(false);

                            // Botón principal
                            builder.SetPositiveButton("Ok", (s, e) =>
                            {
                                ImprimirDialogs(mensaje + 1);
                            });

                            // Crear y mostrar el diálogo
                            var dialog = builder.Create();
                            dialog.Show();

                            // Personalizamos el botón después de mostrarlo
                            dialog.Window.DecorView.Post(() =>
                            {
                                var positiveButton = dialog.GetButton((int)DialogButtonType.Positive);
                                positiveButton?.SetTextColor(Color.ParseColor("#00695C")); // Verde Material
                                positiveButton?.SetAllCaps(false);
                            });
                        });
                        #endregion

                        #region ALERT DIALOG
                        /*Android.App.AlertDialog.Builder alertDialog = new Android.App.AlertDialog.Builder(this);
                        alertDialog.SetTitle(Html.FromHtml("<font color='#3dc2ff' size = 10>" + captu.titulo.ToString() + "</font>"));
                        alertDialog.SetIcon(Resource.Drawable.Info);
                        alertDialog.SetMessage(Html.FromHtml("<font color='#50c8ff' size = 10>" + captu.mensaje.ToString() + "</font>"));
                        alertDialog.SetCancelable(false);
                        alertDialog.SetNeutralButton("Ok", delegate
                        {
                            alertDialog.Dispose();
                            ImprimirDialogs(mensaje + 1);

                        });
                        RunOnUiThread(() => alertDialog.Show());*/
                        #endregion
                    }
                    else if (captu.titulo.Trim() == "Tarima Surtida Completamente")
                    {
                        #region MATERIAL DIALOG - Mensaje Advertencia
                        RunOnUiThread(() =>
                        {
                            // Construimos el título con color y negrita
                            var titleSpannable = new SpannableStringBuilder(captu.titulo.ToString());
                            titleSpannable.SetSpan(new ForegroundColorSpan(Color.ParseColor("#FABF57")), 0, titleSpannable.Length(), SpanTypes.ExclusiveExclusive);
                            titleSpannable.SetSpan(new StyleSpan(TypefaceStyle.Bold), 0, titleSpannable.Length(), SpanTypes.ExclusiveExclusive);

                            // Construimos el mensaje con color
                            var mensajeSpannable = new SpannableStringBuilder(captu.mensaje.ToString());
                            mensajeSpannable.SetSpan(new ForegroundColorSpan(Color.ParseColor("#FECB82")), 0, mensajeSpannable.Length(), SpanTypes.ExclusiveExclusive);

                            // Creamos el diálogo usando Material3
                            var builder = new MaterialAlertDialogBuilder(this, Resource.Style.ThemeOverlay_Material3_MaterialAlertDialog);
                            builder.SetTitle(titleSpannable);
                            builder.SetIcon(Resource.Drawable.no);
                            builder.SetMessage(mensajeSpannable);
                            builder.SetCancelable(false);

                            // Botón principal
                            builder.SetPositiveButton("Ok", (s, e) =>
                            {
                                ImprimirDialogs(mensaje + 1);
                            });

                            // Crear y mostrar el diálogo
                            var dialog = builder.Create();
                            dialog.Show();

                            // Personalizamos el botón después de mostrarlo
                            dialog.Window.DecorView.Post(() =>
                            {
                                var positiveButton = dialog.GetButton((int)DialogButtonType.Positive);
                                positiveButton?.SetTextColor(Color.ParseColor("#00695C")); // Verde Material
                                positiveButton?.SetAllCaps(false);
                            });
                        });
                        #endregion

                        #region ALERT DIALOG
                        /*Android.App.AlertDialog.Builder alertDialog = new Android.App.AlertDialog.Builder(this);
                        alertDialog.SetTitle(Html.FromHtml("<font color='#FABF57' size = 10>" + captu.titulo.ToString() + "</font>"));
                        alertDialog.SetIcon(Resource.Drawable.no);
                        alertDialog.SetMessage(Html.FromHtml("<font color='#FECB82' size = 10>" + captu.mensaje.ToString() + "</font>"));
                        alertDialog.SetCancelable(false);
                        alertDialog.SetNeutralButton("Ok", delegate
                        {
                            alertDialog.Dispose();
                            ImprimirDialogs(mensaje + 1);

                        });
                        RunOnUiThread(() => alertDialog.Show());*/
                        #endregion
                    }
                    else
                    {
                        #region MATERIAL DIALOG - Mensaje Crítico
                        RunOnUiThread(() =>
                        {
                            // Construimos el título con color y negrita
                            var titleSpannable = new SpannableStringBuilder(captu.titulo.ToString());
                            titleSpannable.SetSpan(new ForegroundColorSpan(Color.ParseColor("#FF0000")), 0, titleSpannable.Length(), SpanTypes.ExclusiveExclusive);
                            titleSpannable.SetSpan(new StyleSpan(TypefaceStyle.Bold), 0, titleSpannable.Length(), SpanTypes.ExclusiveExclusive);

                            // Construimos el mensaje con color
                            var mensajeSpannable = new SpannableStringBuilder(captu.mensaje.ToString());
                            mensajeSpannable.SetSpan(new ForegroundColorSpan(Color.ParseColor("#FFFFFF")), 0, mensajeSpannable.Length(), SpanTypes.ExclusiveExclusive);

                            // Creamos el diálogo usando Material3
                            var builder = new MaterialAlertDialogBuilder(this, Resource.Style.ThemeOverlay_Material3_MaterialAlertDialog);
                            builder.SetTitle(titleSpannable);
                            builder.SetIcon(Resource.Drawable.no);
                            builder.SetMessage(mensajeSpannable);
                            builder.SetCancelable(false);

                            // Botón principal
                            builder.SetPositiveButton("Ok", (s, e) =>
                            {
                                ImprimirDialogs(mensaje + 1);
                            });

                            // Crear y mostrar el diálogo
                            var dialog = builder.Create();
                            dialog.Show();

                            // Personalizamos el botón después de mostrarlo
                            dialog.Window.DecorView.Post(() =>
                            {
                                var positiveButton = dialog.GetButton((int)DialogButtonType.Positive);
                                positiveButton?.SetTextColor(Color.ParseColor("#FF0000")); // Rojo para énfasis
                                positiveButton?.SetAllCaps(false);
                            });
                        });
                        #endregion

                        #region ALERT DIALOG
                        /*Android.App.AlertDialog.Builder alertDialog = new Android.App.AlertDialog.Builder(this);
                        alertDialog.SetTitle(Html.FromHtml("<font color='#FF0000' size = 10>" + captu.titulo.ToString() + "</font>"));
                        alertDialog.SetIcon(Resource.Drawable.no);
                        alertDialog.SetMessage(Html.FromHtml("<font color='#FFFFFF' size = 10>" + captu.mensaje.ToString() + "</font>"));
                        alertDialog.SetCancelable(false);
                        alertDialog.SetNeutralButton("Ok", delegate
                        {
                            alertDialog.Dispose();
                            ImprimirDialogs(mensaje + 1);

                        });
                        RunOnUiThread(() => alertDialog.Show());*/
                        #endregion
                    }

                }
                mensajeactual++;


            }
        }

        private void LoadConnection()
        {
            string folder = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);
            string dbPath = System.IO.Path.Combine(folder, "Split_Trailer.db3");

            bool exist = System.IO.File.Exists(dbPath);
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

        }

        List<FlimStarInfo> listItem = new List<FlimStarInfo>();

        List<FlimStarInfo> detalle_pedido()
        {
            thisConnection.Open();
            listItem.Clear();

            var query = db.Table<ConPedidos>();
            foreach (var captu in query)
            {

                listItem.Add(new FlimStarInfo()
                {
                    Name = captu.nombre,
                    Age = "Pedidos: " + captu.pedido + " Surtido: " + captu.surtido,
                    ImageID = Resource.Drawable.producto
                });

            }

            mconcen = "2";

            //LbxCons.Font = new Font(LbxCons.Font.Name, 7);   ;
            thisConnection.Close();

            return listItem;
        }

        List<FlimStarInfo> detalle_lote()
        {
            thisConnection.Open();
            listItem.Clear();

            var query = db.Table<xLote>();
            foreach (var captu in query)
            {

                listItem.Add(new FlimStarInfo()
                {
                    Name = captu.nombre,
                    Age = "Folio: " + captu.Folio + " Tarima: " + captu.Tarima + " Caja: " + captu.Cajas + "Dia/Mes Caducidad:" + captu.diacad + "/" + captu.mescad,
                    ImageID = Resource.Drawable.producto
                });

            }

            thisConnection.Close();

            return listItem;
        }

        List<FlimStarInfo> detalle_Surtido()
        {

            listItem.Clear();

            var query = db.Table<ConPedidos>();
            foreach (var captu in query)
            {

                listItem.Add(new FlimStarInfo()
                {
                    Name = captu.nombre,
                    Age = "Pedidos: " + captu.pedido + " Surtido: " + captu.surtido,
                    ImageID = Resource.Drawable.producto
                });

            }


            return listItem;
        }

        List<FlimStarInfo> productocapturado()
        {

            listItem.Clear();

            var query = db.Table<xprod>();
            foreach (var captu in query)
            {

                listItem.Add(new FlimStarInfo()
                {
                    Name = traenom(captu.Codigo.ToString().Trim()),
                    Age = "Recibo: " + captu.Folio + "Tarima: " + captu.Tarima + " Caja: " + captu.Cajas,
                    ImageID = Resource.Drawable.producto
                });

                TotCaj++;
            }


            return listItem;
        }

        private void OnGridView_ItemClicked(object sender, AdapterView.ItemClickEventArgs e)
        {

        }

        private string validaestructuraetiqueta()
        {
            dondegenera = "validaestructuraetiqueta";
            string ok = "SI";
            var productoscapturados = db.Table<xprod>();
            foreach (var captu in productoscapturados)
            {
                var mtip = captu.Tipo.ToString();
                var mfol = captu.Folio.ToString();
                var mcod = captu.Codigo.ToString();
                var mtar = captu.Tarima.ToString();
                var mcaj = captu.Cajas.ToString();
                var NOmprod = traenom(captu.Codigo.ToString());


                try
                {
                    int vfol = Convert.ToInt32(mfol);
                    int vtar = Convert.ToInt32(mtar);
                    int vcaj = Convert.ToInt32(mcaj);
                }
                catch (System.Exception ex)
                {
                    #region MATERIAL DIALOG - Error en Etiqueta
                    RunOnUiThread(() =>
                    {
                        // Construimos el título con color y negrita
                        var titleSpannable = new SpannableStringBuilder("Error en la estructura de Etiqueta");
                        titleSpannable.SetSpan(new ForegroundColorSpan(Color.ParseColor("#3dc2ff")), 0, titleSpannable.Length(), SpanTypes.ExclusiveExclusive);
                        titleSpannable.SetSpan(new StyleSpan(TypefaceStyle.Bold), 0, titleSpannable.Length(), SpanTypes.ExclusiveExclusive);

                        // Construimos el mensaje con color
                        string mensajeTexto = $"La etiqueta del producto [ {mcod} | {NOmprod} | Recibo: {mfol} | Tarima: {mtar} | Caja: {mcaj} ] contiene un error en la tarima, recibo o folio, favor de informar al supervisor, validar la información, retirar y reetiquetar la caja y leer la nueva etiqueta";
                        var mensajeSpannable = new SpannableStringBuilder(mensajeTexto);
                        mensajeSpannable.SetSpan(new ForegroundColorSpan(Color.ParseColor("#50c8ff")), 0, mensajeSpannable.Length(), SpanTypes.ExclusiveExclusive);

                        // Creamos el diálogo usando Material3
                        var builder = new MaterialAlertDialogBuilder(this, Resource.Style.ThemeOverlay_Material3_MaterialAlertDialog);
                        builder.SetTitle(titleSpannable);
                        builder.SetIcon(Resource.Drawable.Info);
                        builder.SetMessage(mensajeSpannable);
                        builder.SetCancelable(false);

                        // Botón principal
                        builder.SetPositiveButton("Ok", (s, e) =>
                        {
                            //Borrado de Etiquetas capturadas
                            db.Query<xprod>("delete from [xprod] Where Tipo = @0 AND Folio = @1 AND Codigo = @2 AND Tarima = @3 AND Cajas = @4", mtip, mfol, mcod, mtar, mcaj);
                            db.Query<ConPedidos>("UPDATE [ConPedidos] SET surtido = surtido - 1 WHERE prod_clave = @0", mcod.Trim());
                        });

                        // Crear y mostrar el diálogo
                        var dialog = builder.Create();
                        dialog.Show();

                        // Personalizamos el botón después de mostrarlo
                        dialog.Window.DecorView.Post(() =>
                        {
                            var positiveButton = dialog.GetButton((int)DialogButtonType.Positive);
                            positiveButton?.SetTextColor(Color.ParseColor("#3dc2ff"));
                            positiveButton?.SetAllCaps(false);
                        });
                    });
                    #endregion

                    #region ALERT DIALOG
                    /*Android.App.AlertDialog.Builder alertDialog = new Android.App.AlertDialog.Builder(this);
                    alertDialog.SetTitle(Html.FromHtml("<font color='#3dc2ff' size = 10>Error en la estructura de Etiqueta</font>"));
                    alertDialog.SetIcon(Resource.Drawable.Info);
                    alertDialog.SetMessage(Html.FromHtml("<font color='#50c8ff' size = 10>La etiqueta del producto [ " + mcod + " | " + NOmprod + " | " + " Recibo: " + mfol + " | Tarima: " + mtar + " | Caja: " + mcaj + " ] contiene un error en la tarima, recibo o folio, favor de informar al supervisor, validar la informacion, retirar y reetiquetar la caja y leer  la nueva etiqueta</font>"));
                    alertDialog.SetCancelable(false);
                    alertDialog.SetNeutralButton("Ok", delegate
                    {
                        alertDialog.Dispose();
                        //Borrado de Etiquetas capturadas
                        db.Query<xprod>("delete from[xprod] Where Tipo = '" + mtip + "' AND Folio = '" + mfol + "' AND Codigo = '" + mcod + "' AND Tarima = '" + mtar + "' AND Cajas = '" + mcaj + "'");
                        db.Query<ConPedidos>("UPDATE [ConPedidos] SET surtido = surtido - " + 1 + " WHERE prod_clave = '" + mcod.ToString().Trim() + "'");
                        //******************************

                    });
                    RunOnUiThread(() => alertDialog.Show());*/
                    #endregion

                    ok = "NO";
                }


            }

            return ok;
        }

        private string valida()
        {
            dondegenera = "valida";
            HayExistencias = "S";
            EtiquetaExiste = "S";
            OrdenExiste = "S";
            EtiquetaCapturada = "S";
            FechaCaducada = "S";
            db.Query<xLote>("delete from  [xLote]");
            string ok = "S";
            int tot = 0, totok = 0;
            thisConnection.Open();
            string mtip = "", mfol = "", mcod = "", mtar = "", mcaj = "", mfeccap = "", mtipocaptura = "";
            string amtip = "", amfol = "", amcod = "", amtar = "", amcaj = "", amfeccap = "";
            var conta = 0;
            var productoscapturados = db.Table<xprod>();
            foreach (var captu in productoscapturados)
            {
                string er = "";
                mtip = captu.Tipo.ToString();
                mfol = captu.Folio.ToString();
                mcod = captu.Codigo.ToString();
                mtar = captu.Tarima.ToString();
                mcaj = captu.Cajas.ToString();
                mfeccap = captu.fecha_captura.ToString();
                mtipocaptura = captu.tipo_captura.ToString();


                string nom = traenom(captu.Codigo.ToString().Trim());
                string lectura = mtip + mfol + mcod + mtar + mcaj;
                /*string fechacap = ValidaCaja(lectura).Trim();
                string fechacappre = ValidaCajaPreesplit(lectura).Trim();*/
                string fechacapone = ValidaCaja(lectura).Trim();
                string[] responsablelec = fechacapone.Split('/');
                string[] fechas = responsablelec[1].Split('*');

                string fechacap = Convert.ToString(fechas[0]);
                string Embcap = Convert.ToString(fechas[1]);
                string fechacappre = "";
                try
                {
                    fechacappre = Convert.ToString(fechas[2]);
                }
                catch
                {
                    fechacappre = "";
                }

                try
                {
                    int vfol = Convert.ToInt32(mfol);
                    int vtar = Convert.ToInt32(mtar);
                    int vcaj = Convert.ToInt32(mcaj);
                }
                catch (System.Exception ex)
                {
                    EstructuraEtiqueta = "N";
                    Mensajes mensa = new Mensajes { titulo = "Error en la estructura de Etiqueta", mensaje = "La etiqueta del producto [ " + mcod + " | " + nom + " | Recibo: " + mfol + " | Tarima " + mtar + " | Caja: " + mcaj + " ] contiene un error en la tarima, recibo o folio, favor de informar al supervisor, validar la informacion, retirar y reetiquetar la caja y leer  la nueva etiqueta" };
                    db.Insert(mensa);

                    //Borrado de Etiquetas capturadas
                    db.Query<xprod>("delete from[xprod] Where Tipo = '" + mtip + "' AND Folio = '" + mfol + "' AND Codigo = '" + mcod + "' AND Tarima = '" + mtar + "' AND Cajas = '" + mcaj + "'");
                    db.Query<ConPedidos>("UPDATE [ConPedidos] SET surtido = surtido - " + 1 + " WHERE prod_clave = '" + mcod.ToString().Trim() + "'");
                    //******************************

                    ok = "NO";
                }



                if (fechacap.Length > 0)
                {
                    /*string Embcap = ValidaEmb(lectura).Trim();
                    string EmbRespo = ValidaCajaRespon(lectura).Trim();

                    Mensajes mensa = new Mensajes { titulo = "Etiqueta ya capturada", mensaje = "Error Etiqueta YA FUE CAPTURADA!! " + "\n\r" + mtip + " | " + mfol + " | " + mcod + " | " + mtar + " | " + mcaj + "\n\r" + ", " +
                        "Día " + fechacap + "\n\r" + nom + "\n\r" + EmbRespo + "Favor de Ir a Liberar su Reimpresion, Colocar la Nueva Etiqueta y Volver a Leer" };
                    db.Insert(mensa);

                    string mped = pedidoencaptura.Text.ToString().Trim();
                    mped = mped.Replace("Pedido Actual: ", "");

                    string reetiquetado = "insert into Tb_Det_Sol_Reetiquetado (Fecha, emb_folio, fecha_cap, Lectura, Recibo, Producto, Caja, TarIni, TarFin, Cve_Camioneta, Estatus, Obs, armador, autorizo, origen) values" +
                        " ('" + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + "', '" + mped + "',  GETDATE(), '" + lectura + "', '" + mfol + "', '" + mcod + "', '" + mcaj + "', '" + mtar + "', '" + mtar + "', '', 'A', 'SOLICITUD DE REIMPRESION POR ETIQUETA YA LEIDA', '" + responsable + "', '', 'EMB')";
                    SqlCommand cmd = new SqlCommand(reetiquetado, thisConnection);
                    cmd.ExecuteNonQuery();


                    /*nsajes mensa = new Mensajes { titulo = "Etiqueta ya capturada", mensaje = "Error Etiqueta YA FUE CAPTURADA!! " + "\n\r" + mtip + " | " + mfol + " | " + mcod + " | " + mtar + " | " + mcaj + "\n\r" + "Día " + fechacap + "\n\r" + "Embarque " + Embcap + "\n\r" + nom };
                    db.Insert(mensa);*/
                    //Borrado de Etiquetas capturadas
                    /*db.Query<xprod>("delete from[xprod] Where Tipo = '" + mtip + "' AND Folio = '" + mfol + "' AND Codigo = '" + mcod + "' AND Tarima = '" + mtar + "' AND Cajas = '" + mcaj + "'");
                    db.Query<ConPedidos>("UPDATE [ConPedidos] SET surtido = surtido - " + 1 + " WHERE prod_clave = '" + mcod.ToString().Trim() + "'");
                    EtiquetaCapturada = "N";
                    TotCaj--;
                    ok = "N";
                    er = "S";*/

                    string mped = pedidoencaptura.Text.ToString().Trim();
                    mped = mped.Replace("Pedido Actual: ", "");
                    string EmbRespo = responsablelec[0].Trim();

                    //Mensajes mensa = new Mensajes { titulo = "Etiqueta ya capturada", mensaje = "Error Etiqueta YA FUE CAPTURADA!! " + "\n\r" + mtip + " | " + mfol + " | " + mcod + " | " + mtar + " | " + mcaj + "\n\r" + "Día " + fechacap + "\n\r" + "Embarque " + Embcap + "\n\r" + nom + "  Informe al Supervisor de Camionetas para Liberacion de Cajas" };
                    Mensajes mensa = new Mensajes
                    {
                        titulo = "Etiqueta ya capturada",
                        mensaje = "Error Etiqueta YA FUE CAPTURADA!! " + "\n\r" + mtip + " | " + mfol + " | " + mcod + " | " + mtar + " | " + mcaj + "\n\r" + ", " +
                        "Día " + fechacap + "\n\r" + nom + "\n\r" + EmbRespo + "La liberacion automatica, quedo deshabilitada, favor de informar a Personal de Camaras Frias"
                    };
                    db.Insert(mensa);
                    //Borrado de Etiquetas capturadas
                    db.Query<xprod>("delete from[xprod] Where Tipo = '" + mtip + "' AND Folio = '" + mfol + "' AND Codigo = '" + mcod + "' AND Tarima = '" + mtar + "' AND Cajas = '" + mcaj + "'");
                    db.Query<ConPedidos>("UPDATE [ConPedidos] SET surtido = surtido - " + 1 + " WHERE prod_clave = '" + mcod.ToString().Trim() + "'");
                    ok = "N";
                    er = "S";


                    /*string reetiquetado = "insert into Tb_Det_Sol_Reetiquetado (Fecha, emb_folio, fecha_cap, Lectura, Recibo, Producto, Caja, TarIni, TarFin, Cve_Camioneta, Estatus, Obs, armador, autorizo, origen) values" +
                        " ('" + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + "', '" + mped + "',  GETDATE(), '" + lectura + "', '" + mfol + "', '" + mcod + "', '" + mcaj + "', '" + mtar + "', '" + mtar + "', '', 'A', 'SOLICITUD DE REIMPRESION POR ETIQUETA YA LEIDA', '" + responsable + "', '', 'EMB')";
                    SqlCommand cmd = new SqlCommand(reetiquetado, thisConnection);
                    cmd.ExecuteNonQuery();*/
                }
                else
                {
                    if (fechacappre.Length > 0)
                    {
                        //string Embcap = ValidaEmb(lectura).Trim();
                        Mensajes mensa = new Mensajes { titulo = "Etiqueta ya capturada En PreSplit", mensaje = "Etiqueta YA FUE CAPTURADA EN PRE-SPLIT!! " + "\n\r" + mtip + " | " + mfol + " | " + mcod + " | " + mtar + " | " + mcaj + "\n\r" + "Día " + fechacap + "\n\r" + "\n\r" + nom + ", No se Puede Obtener Esta Caja" };
                        db.Insert(mensa);
                        //Borrado de Etiquetas capturadas
                        db.Query<xprod>("delete from[xprod] Where Tipo = '" + mtip + "' AND Folio = '" + mfol + "' AND Codigo = '" + mcod + "' AND Tarima = '" + mtar + "' AND Cajas = '" + mcaj + "'");
                        db.Query<ConPedidos>("UPDATE [ConPedidos] SET surtido = surtido - " + 1 + " WHERE prod_clave = '" + mcod.ToString().Trim() + "'");
                        EtiquetaCapturada = "N";
                        TotCaj--;
                        ok = "N";
                        er = "S";
                    }
                }




                xLote consecutivo = new xLote { Tipo = captu.Tipo.ToString(), Pedido = "", Folio = captu.Folio, Codigo = captu.Codigo, Tarima = captu.Tarima, Cajas = captu.Cajas, nombre = nom, diacad = "", mescad = "", fecha_captura = mfeccap, tipo_captura = captu.tipo_captura };
                //Registra en la base de datos SQLite
                db.Insert(consecutivo);


                totok++;

                conta++;

                if (er == "S")
                {
                    tot++;
                }

            }

            var existencias = db.Query<xprod>("Select Folio, Codigo, Tarima, Tipo, COUNT(Tipo) AS Cajas FROM xprod GROUP BY Folio, Codigo, Tarima, Tipo");

            foreach (var captu in existencias)
            {
                string er = "";
                mtip = captu.Tipo.ToString();
                mfol = captu.Folio.ToString();
                mcod = captu.Codigo.ToString();
                mtar = captu.Tarima.ToString();
                int mcajas = Convert.ToInt32(captu.Cajas.ToString());
                string nom = traenom(captu.Codigo.ToString().Trim());

                string cadena = "";
                //traer nombre de producto para validar cuantos dias debo aumentar.

                int diascad = 14;
                if (nom.Contains("BETABEL"))
                {
                    diascad = 60;
                }
                else if (nom.Contains("AJO"))
                {
                    diascad = 180;
                }
                else if (nom.Contains("ADEREZO") || nom.Contains("VINAGRETA") || nom.Contains("QUESO"))
                {
                    diascad = 90;
                }

                if (mtip == "PTC")
                    cadena = "SELECT ETIQUETA AS PROD,SURTIDO,FECHA_CAD AS FECCAD, (CASE fecha_cad WHEN '' THEN  FORMAT( DATEADD(day, " + diascad + ", pti_fecha), 'dd/MM/yyyy', 'en-US' ) WHEN fecha_cad THEN fecha_cad END) AS fecha_cad, B.rpt_estatus AS estado FROM TB_DET_TRAZABILIDAD JOIN tb_mstr_recepcion_pt B ON Recibo = B.rpt_recibo WHERE PROD_CLAVE = '" + mcod + "' AND RECIBO = '" + mfol + "' " +
                             "AND TIPO = '" + mtip + "' AND TARIMA = '" + Convert.ToInt32(mtar).ToString() + "' ";

                else
                    cadena = "SELECT NUM_CAJAS AS PROD, CAJAS_SUR AS SURTIDO,NUM_LOTE AS FECCAD, ISNULL(fechacad, FORMAT( DATEADD(day, " + diascad + ", fecha), 'yyyyMMdd', 'en-US' )) AS fecha_cad, B.ordp_estatus AS estado FROM TB_DET_ETI_FINAL JOIN tb_mstr_ordenes_prod B ON Folio = B.ordp_folio WHERE CVE_PROD = '" + mcod + "' AND FOLIO = '" + mfol + "' " +
                        "AND TARIMA = '" + Convert.ToInt32(mtar).ToString() + "' ";

                SqlDataAdapter da = new SqlDataAdapter(cadena, thisConnection);
                DataSet ds = new DataSet();

                //MessageBox.Show(cadena); 
                da.Fill(ds, "Info");
                DataTable Info = ds.Tables["Info"];
                //MessageBox.Show(Info.Rows.Count.ToString()); 
                if (Info.Rows.Count == 0)
                {
                    ok = "N";
                    EtiquetaExiste = "N";

                    Mensajes mensa = new Mensajes { titulo = "Tarima No Existe", mensaje = "Error Tarima No Existe!! " + "\n\r" + mtip + " | " + mfol + " | " + mcod + " | " + mtar + "\n\r" + "\n\r" + nom + " Informe al supervisor, Valide en Monitor de Caducidades, Retire Todas las cajas - Total capturado: " + mcajas + ", Favor de Informar a Sistemas" };
                    db.Insert(mensa);
                    try
                    {
                        SendMail("ricardo.cortes@mrlucky.com.mx;jgalvan@mrlucky.com.mx;ahernandez@mrlucky.com.mx", "Error Tarima No Existe!! " + "\n\r" + mtip + " | " + mfol + " | " + mcod + " | " + mtar + "\n\r" + "\n\r" + nom + " Valide en Monitor de Caducidades", "Tarima Con problema de Existencia");
                    }
                    catch
                    {

                    }

                    db.Query<xprod>("delete from[xprod] Where Tipo = '" + mtip + "' AND Folio = '" + mfol + "' AND Codigo = '" + mcod + "' AND Tarima = '" + mtar + "'");
                    db.Query<ConPedidos>("UPDATE [ConPedidos] SET surtido = surtido - " + mcajas + " WHERE prod_clave = '" + mcod.ToString().Trim() + "'");

                    tot++;
                    continue;
                }

                System.String diacaducidad = "";
                System.String mescaducidad = "";

                foreach (DataRow row in Info.Rows)
                {
                    if (mtip == "PTC" && row["estado"].ToString().Trim() == "F")
                    {
                        ok = "N";
                        OrdenExiste = "N";

                        Mensajes mensa = new Mensajes { titulo = "Recepcion de Producto Terminado Cancelada", mensaje = "ERROR! La orden" + mfol + " Fue Cancelada, Favor de Retirar las cajas, Informar a Personal de Descargue y Materia Prima Para proceder al Reetiquetado, Leer nuevamente" };
                        db.Insert(mensa);


                        db.Query<xprod>("delete from[xprod] Where Tipo = '" + mtip + "' AND Folio = '" + mfol + "' AND Codigo = '" + mcod + "' AND Tarima = '" + mtar + "'");
                        db.Query<ConPedidos>("UPDATE [ConPedidos] SET surtido = surtido - " + mcajas + " WHERE prod_clave = '" + mcod.ToString().Trim() + "'");

                        tot++;
                        continue;
                    }
                    else if (mtip == "PTP" && row["estado"].ToString().Trim() == "C")
                    {
                        ok = "N";
                        OrdenExiste = "N";

                        Mensajes mensa = new Mensajes { titulo = "Orden de Produccion Cancelada", mensaje = "ERROR! La orden" + mfol + " Fue Cancelada, Favor de Retirar las cajas, Informar a Personal de Calidad de Ensaladas o Fresco Para proceder al Reetiquetado, Leer nuevamente" };
                        db.Insert(mensa);


                        db.Query<xprod>("delete from[xprod] Where Tipo = '" + mtip + "' AND Folio = '" + mfol + "' AND Codigo = '" + mcod + "' AND Tarima = '" + mtar + "'");
                        db.Query<ConPedidos>("UPDATE [ConPedidos] SET surtido = surtido - " + mcajas + " WHERE prod_clave = '" + mcod.ToString().Trim() + "'");

                        tot++;
                        continue;
                    }
                    else
                    {
                        int mP = Convert.ToInt32(row["PROD"]);
                        int mS = Convert.ToInt32(row["SURTIDO"]);
                        int cant = 0;

                        //TRAER LA CANTIDAD DE CAJAS EXISTENTES EN LA LECTURA
                        var query1 = db.Query<xprod>("SELECT * FROM [xprod] Where tipo = '" + mtip + "' and Folio = '" + mfol + "' and Codigo = '" + mcod + "' and Tarima = '" + mtar + "'");
                        foreach (var captu1 in query1)
                        {
                            cant = cant + 1;

                        }

                        if ((mS + cant) > mP)
                        {
                            ok = "N";
                            //LbxCap.SelectedIndex = i;
                            HayExistencias = "NE";
                            er = "S";

                            if (amtip != mtip || amfol != mfol || amcod != mcod || amtar != mtar)
                            {
                                Mensajes mensa = new Mensajes { titulo = "Etiqueta Sin Existencias", mensaje = "Error en la Etiqueta Ya No Hay Existecia!!" + "\n\r" + mtip + " | " + mfol + " | " + mcod + " | " + mtar + " \n\r" + nom + "\n\r" + cant.ToString() };
                                db.Insert(mensa);
                            }

                            //AgregaSolHabilitarFolios(mtip, mfol, mcod, nom, mtar, ((mS + cant) - mP).ToString());
                            AgregaFolioSinExistencia(mtip, mfol, mcod, nom, mtar, cant.ToString());
                        }
                        string feccad = "";

                        diacaducidad = traediafecad(row["feccad"].ToString(), mtip);
                        mescaducidad = traemesfecad(row["feccad"].ToString(), mtip);




                        //Validacion de fecha de caduciadad que debe venir*****************************************************************************************************

                        if (diacaducidad == "|")
                        {
                            diacaducidad = traediafecadrec(row["fecha_cad"].ToString(), mtip);
                        }

                        if (mescaducidad == "|")
                        {
                            mescaducidad = traemesfecadrec(row["fecha_cad"].ToString(), mtip);
                        }

                        xLote consecutivo = new xLote { Tipo = captu.Tipo.ToString(), Pedido = "", Folio = captu.Folio, Codigo = captu.Codigo, Tarima = captu.Tarima, Cajas = captu.Cajas, nombre = nom, diacad = diacaducidad.Trim(), mescad = mescaducidad.Trim(), fecha_captura = mfeccap };


                        db.Query<xLote>("UPDATE [xLote] SET mescad = '" + mescaducidad.Trim() + "', diacad = '" + diacaducidad.Trim() + "'  WHERE Codigo = '" + captu.Codigo + "' AND Folio = '" + captu.Folio + "' AND Tarima = '" + captu.Tarima + "' AND Tipo = '" + captu.Tipo + "'");
                        //Registra en la base de datos SQLite
                        //db.Insert(consecutivo);
                        //totok++;

                        if (er == "S")
                        {
                            tot++;
                        }


                        amtip = mtip;
                        amfol = mfol;
                        amcod = mcod;
                        amtar = mtar;
                    }



                }
            }

            if (tot > 0)
            {

                Mensajes mensa = new Mensajes { titulo = "Se detectaron etiquetas con ERROR", mensaje = "Se detectaron " + tot + " Etiquetas con error" };
                db.Insert(mensa);

            }

            thisConnection.Close();
            //List<FlimStarInfo> lstFlimStar = detalle_Surtido();
            //var gvObject = FindViewById<GridView>(Resource.Id.gvCtr2);
            //RunOnUiThread(() => gvObject.Adapter = new myGVItemAdapter(this, lstFlimStar));
            //gvObject.ItemClick += new EventHandler<AdapterView.ItemClickEventArgs>(OnGridView_ItemClicked); //detalle_pedido


            RunOnUiThread(() => total.Text = totok.ToString("##0"));
            return ok;
        }

        /*private string ValidaCaja(string cadena)
        {
            string Cadena = "Select fecha_cap From tb_Det_Etiqueta " +
                           "Where Eti_Lectura = '" + cadena + "' AND Estatus != 'C'";
            SqlCommand cmd = new SqlCommand(Cadena, thisConnection);
            string Valor = Convert.ToString(cmd.ExecuteScalar());
            return Valor;
        }*/

        private string ValidaCaja(string cadena)
        {
            /*string Cadena = "Select fecha_cap From tb_Det_Etiqueta " +
                           "Where Eti_Lectura = '" + cadena + "' AND Estatus != 'C'";
            string Cadena = "Select CONCAT(A.fecha_cap, '*', B.NOM_CAPSPLIT) AS datoscaptura From tb_Det_Etiqueta A LEFT JOIN tb_det_split B ON A.emb_folio = B.emb_folio AND A.Eti_TarIni = B.TARINI AND A.Eti_Producto = B.prod_clave AND A.Split = B.tarima Where A.Eti_Lectura = '" + cadena + "' AND A.Estatus != 'C'";
            SqlCommand cmd = new SqlCommand(Cadena, thisConnection);
            string Valor = Convert.ToString(cmd.ExecuteScalar());
            string[] valores = Valor.Split('*');
            Valor = "";
            if ((valores[0].ToString().Trim().Length > 0) && (valores[1].ToString().Trim().Length == 0))
            {
                cadena = "UPDATE tb_det_Etiqueta SET Estatus = 'C', Obs = 'Cancelacion de Etiqueta Por Error En Sistema' Where Eti_Lectura = '" + cadena + "' AND Estatus = 'A'";
                cmd = new SqlCommand(cadena, thisConnection);
                cmd.ExecuteNonQuery();
            }
            else if ((valores[0].ToString().Trim().Length > 0) && (valores[1].ToString().Trim().Length > 0))
            {
                Valor = valores[0].ToString().Trim();
            }
            return Valor;*/
            string Cadena = "SELECT TOP 1 CONCAT(isnull((Select CONCAT(B.NOM_CAPSPLIT, '/', A.fecha_cap, '*', A.emb_folio) From tb_Det_Etiqueta A LEFT JOIN tb_det_split B ON A.emb_folio = B.emb_folio AND A.Eti_Recibo = B.no_lote AND A.Eti_TarIni = B.TARINI AND A.Eti_Producto = B.prod_clave AND A.Split = B.tarima Where A.Eti_Lectura = '" + cadena + "' AND A.Estatus != 'C'), '/*'), '*', (Select fecha_cap From Tb_Det_Etiqueta_Presplit Where Eti_Lectura = '" + cadena + "' AND Estatus = 'A'  AND Fecha = CONVERT(varchar,getdate(),112)))";
            SqlCommand cmd = new SqlCommand(Cadena, thisConnection);
            string Valor = Convert.ToString(cmd.ExecuteScalar());
            string[] valores = Valor.Split('/');
            string[] fechas = valores[1].Split('*');
            if ((fechas[0].ToString().Trim().Length > 0) && (valores[0].ToString().Trim().Length == 0))
            {
                cadena = "UPDATE tb_det_Etiqueta SET Estatus = 'C', Obs = 'Cancelacion de Etiqueta Por Error En Sistema' Where Eti_Lectura = '" + cadena + "' AND Estatus = 'A'";
                cmd = new SqlCommand(cadena, thisConnection);
                cmd.ExecuteNonQuery();
                try
                {
                    Valor = "/**" + fechas[2].ToString().Trim();
                }
                catch
                {
                    Valor = "/**";
                }
            }
            return Valor;
        }

        private string ValidaEmb(string cadena)
        {
            string Cadena = "Select emb_folio From tb_Det_Etiqueta " +
                           "Where Eti_Lectura = '" + cadena + "'";
            SqlCommand cmd = new SqlCommand(Cadena, thisConnection);
            string Valor = Convert.ToString(cmd.ExecuteScalar());
            return Valor;
        }

        private string ValidaCajaRespon(string cadena)
        {
            string Cadena = "Select CONCAT(' CAPTURADO POR: ', RTRIM(B.NOM_CAPSPLIT), ' / EMBARQUE: ', A.emb_folio) AS datoscaptura From tb_Det_Etiqueta A LEFT JOIN tb_det_split B ON A.emb_folio = B.emb_folio AND A.Eti_TarIni = B.TARINI AND A.Eti_Producto = B.prod_clave AND A.Split = B.tarima Where A.Eti_Lectura = '" + cadena + "' AND A.Estatus != 'C'";
            SqlCommand cmd = new SqlCommand(Cadena, thisConnection);
            string Valor = Convert.ToString(cmd.ExecuteScalar());
            return Valor;
        }

        private string traenom(string cve)
        {
            string nom = "";
            foreach (DataRow row in CatProd.Select("prod_clave = '" + cve + "'"))
                nom = row["prod_nombre"].ToString().Trim();

            nom = nom.Replace("'", " ");
            return nom;
        }

        private void AgregaFolioSinExistencia(string mTi, string mFo, string mPr, string mNo, string mTa, string mCa)
        {
            string cadena = "INSERT INTO TB_DET_SPLIT_FOLIOSINEXIS(FECHA,FECHACAP,CVE_CAMIONETA,NOM_CAPSPLIT,TIPO,FOLIO,PROD_CLAVE,PROD_NOMBRE,TARIMA,CAJA) " +
                            "VALUES('" + DateTime.Now.ToString("dd/MM/yyyy") + "','" + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + "','" +
                            cvecam + "','" + muser.Substring(0, 20) + "','" + mTi + "','" + mFo + "','" + mPr + "','" + mNo + "','" + mTa + "','" + mCa + "')";
            //MessageBox.Show(cadena);
            SqlCommand cmd = new SqlCommand(cadena, thisConnection);
            cmd.ExecuteNonQuery();
        }

        private void AgregaSolHabilitarFolios(string mTi, string mFo, string mPr, string mNo, string mTa, string mCa)
        {
            string mped = pedidoencaptura.Text.ToString().Trim();
            mped = mped.Replace("Pedido Actual: ", "");

            string reetiquetado = "SELECT COUNT (fecha_cap) FROM Tb_Det_Sol_Reetiquetado WHERE emb_folio = '" + mped + "' AND Recibo = '" + mFo + "' AND Producto = '" + mPr + "' AND TarIni = '" + mTa + "' AND Estatus = 'A' AND armador = '" + responsable + "'";
            SqlCommand cmdX = new SqlCommand(reetiquetado, thisConnection);
            int cantidad = Convert.ToInt32(cmdX.ExecuteScalar());

            if (cantidad == 0)
            {
                string cadena = "IF NOT EXISTS(SELECT emb_folio FROM tb_Det_Sol_Mod_inventario WHERE emb_folio = '" + mped + "' AND orden = '" + mFo + "' AND  id_codigo = '" + mPr + "' AND  tarima = '" + mTa + "' AND tipo = '" + mTi + "' AND estatus = 'A') INSERT INTO  tb_Det_Sol_Mod_inventario(emb_folio, orden, tipo, id_codigo, descrip, cajas_mod, fecha_cap, capturo, motivo, tarima, estatus) " +
                            "VALUES('" + mped + "','" + mFo + "','" + mTi + "','" + mPr + "','" + mNo + "','" + mCa + "',GETDATE(),'" + responsable + "','Folio Modificado Por intervension de Split Trailer','" + mTa + "','A')";
                //MessageBox.Show(cadena);
                SqlCommand cmd = new SqlCommand(cadena, thisConnection);
                cmd.ExecuteNonQuery();

            }
        }

        private string traediafecad(string fecha, string tipo)
        {
            string Cad = "|";
            int pos = 0;
            if (fecha.Trim().Length > 0)
            {
                if (tipo == "PTP")
                {
                    pos = fecha.Trim().IndexOf("FC");
                    Cad = fecha.Substring(pos + 5, 2);
                    //Cad = fecha.Substring(fecha.Length - 3, 2);
                }
                else
                {
                    Cad = fecha.Substring(0, 2);
                }

            }
            return Cad;
        }

        private string traemesfecad(string fecha, string tipo)
        {
            int pos = 0;
            string Cad = "|";
            if (fecha.Trim().Length > 0)
            {

                if (tipo == "PTP")
                {
                    pos = fecha.Trim().IndexOf("FC");
                    Cad = fecha.Substring(pos + 2, 3);
                    //Cad = fecha.Substring(fecha.Length - 6, 3);
                }
                else
                {
                    Cad = traemes(Convert.ToInt32(fecha.Substring(3, 2)));
                }
            }
            return Cad;
        }

        private string traediafecadrec(string fecha, string tipo)
        {
            fecha = fecha.Trim();
            string Cad = " | ";
            if (fecha.Trim().Length > 0)
            {
                if (tipo == "PTP")
                    Cad = fecha.Substring(fecha.Length - 2, 2);
                else
                    Cad = fecha.Substring(0, 2);
            }
            return Cad;
        }

        private string traemesfecadrec(string fecha, string tipo)
        {
            fecha = fecha.Trim();
            string Cad = " | ";
            if (fecha.Trim().Length > 0)
            {
                if (tipo == "PTP")
                    Cad = traemes(Convert.ToInt32(fecha.Substring(fecha.Length - 4, 2)));
                else
                    Cad = traemes(Convert.ToInt32(fecha.Substring(3, 2)));
            }
            return Cad;
        }

        private string validafecad()
        {
            string Valor = "";
            ValiFechacad = "S";
            //Obtener los productos con su tipo de lo que se ha leido******************************************************************
            var productoscapturados = db.Query<xLote>("Select Tipo, Codigo, nombre FROM xLote GROUP BY Tipo, Codigo, nombre");
            db.Query<XLoteSug>("delete from[XLoteSug]");

            var allItems = db.Table<xLote>().ToList();
            int count = allItems.Count;
            int[] validados = new int[count + 1];
            int capturas = 0;
            foreach (var captu in productoscapturados)
            {
                int totalpro = 0;
                int totaldisponibles = 0;
                int totalusadas;
                int simulador = 0;
                int totaldis = 0;
                string fechaant = "";

                //traer el total de recibos vencidos para que no entren en la condicion
                var prodcapx = db.Query<xLote>("Select COUNT(ID) AS Cajas FROM xLote Where Codigo = '" + captu.Codigo.Trim() + "'");

                foreach (var capturadox in prodcapx)
                {
                    totalpro = Convert.ToInt32(capturadox.Cajas.ToString().Trim());

                }

                int resttotal = traerecibosvencidos(captu.Codigo.Trim(), captu.Tipo.Trim());

                totalpro = totalpro - resttotal;

                //Obtener los diferentes folios disponibles dependiendo el codigo y el tipo
                string todobien = "OK";
                int prod_cap = 0;
                int usadas = 0;
                int existefecant = 0;
                string cadena = "";
                string tipo = captu.Tipo.Trim();
                string prod = captu.Codigo.Trim();
                string diacadant = "";
                string mescadant = "";
                if (tipo == "PTC")
                {
                    cadena = "SELECT  (etiqueta - surtido) AS disponible, (CASE fecha_cad WHEN '' THEN  FORMAT( DATEADD(day, 15, pti_fecha), 'dd/MM/yyyy', 'en-US' ) WHEN fecha_cad THEN fecha_cad END) AS fecha_cad, (CASE fecha_cad WHEN '' THEN  FORMAT( DATEADD(day, 15, pti_fecha), 'yyyyMMdd', 'en-US' ) WHEN fecha_cad THEN FORMAT(convert(datetime,fecha_cad), 'yyyyMMdd', 'en-US' ) END) AS fecha_cadu, recibo, tarima FROM TB_DET_TRAZABILIDAD Inner JOIN tb_mstr_recepcion_pt ON rpt_recibo = recibo WHERE PROD_CLAVE = '" + prod + "' AND pti_estatus_sur = '' AND tipo = 'PTC' AND (rpt_tipo != 'TR' OR (rpt_tipo != 'TR' AND rpt_inventario = 'S')) AND rpt_estatus = '' AND  (etiqueta - surtido) > 0 Order By fecha_cadu";
                }
                else
                {
                    cadena = "SELECT (num_cajas - cajas_sur) AS disponible, ISNULL(NULLIF(fechacad,' '), FORMAT( DATEADD(day, 15, fecha), 'yyyyMMdd', 'en-US' )) AS fecha_cad, folio AS recibo, tarima FROM tb_det_eti_final Inner JOIN tb_mstr_ordenes_prod ON folio = ordp_folio WHERE cve_prod = '" + prod + "' AND estatus_sur != 'S' AND ordp_estatus != 'C' AND (num_cajas - cajas_sur) > 0 Order By fechacad";
                }

                SqlDataAdapter da = new SqlDataAdapter(cadena, thisConnection);
                DataSet ds = new DataSet();
                da.Fill(ds, "xlotes");
                DataTable xlote = ds.Tables["xlotes"];


                //Recorrido de cada uno de los folios y la validacion correspondiente hacia lo que tengo capturado************************

                foreach (DataRow row in xlote.Rows)
                {

                    string Cadena = "Select Count(fecha) AS Total From Tb_Det_Etiqueta_Presplit " +
                                    "Where Eti_Recibo = '" + row["recibo"].ToString().Trim() + "' AND Eti_Producto = '" + captu.Codigo.Trim() + "' AND Eti_TarIni = '" + Convert.ToInt32(row["tarima"].ToString().Trim()) + "' AND Estatus = 'A'";

                    thisConnection.Open();
                    SqlCommand cmd = new SqlCommand(Cadena, thisConnection);
                    int TotalLeido = Convert.ToInt32(cmd.ExecuteScalar());
                    thisConnection.Close();

                    row["disponible"] = Convert.ToInt32(row["disponible"].ToString().Trim()) - TotalLeido;

                    if (Convert.ToInt32(row["disponible"]) > 0)
                    {
                        if (totalpro > 0)
                        {

                            string diacad = traediafecadrec(row["fecha_cad"].ToString().Trim(), tipo);
                            string mescad = traemesfecadrec(row["fecha_cad"].ToString().Trim(), tipo);
                            if ((diacadant == diacad && mescadant == mescad) || (diacadant == "" && mescadant == ""))
                            {
                                todobien = "OK";
                            }
                            else
                            {
                                if (totaldisponibles == 0)
                                {
                                    todobien = "OK";
                                }
                                else
                                {
                                    todobien = "NO";
                                }
                            }

                            if (todobien == "OK")
                            {
                                var prodcap = db.Query<xLote>("Select COUNT(ID) AS Cajas FROM xLote Where Codigo = '" + captu.Codigo.Trim() + "' AND Folio = '" + row["recibo"].ToString().Trim() + "'  AND CAST(Tarima as int) = '" + Convert.ToInt32(row["tarima"].ToString().Trim()) + "'");

                                foreach (var capturado in prodcap)
                                {
                                    usadas = Convert.ToInt32(capturado.Cajas.ToString().Trim());
                                    totaldis = Convert.ToInt32(row["disponible"].ToString().Trim()) - Convert.ToInt32(capturado.Cajas.ToString().Trim());
                                    simulador = simulador + totaldis;
                                    totalpro = totalpro - usadas;
                                    totaldisponibles = totaldisponibles + totaldis;
                                }

                                if (totaldis > 0)
                                {
                                    XLoteSug sugeridos = new XLoteSug { recibosug = row["recibo"].ToString().Trim(), fecrecsug = diacad + "/" + mescad, cveprod = prod, Tarima = row["tarima"].ToString().Trim(), Cajasdis = totaldis, Cajasusadas = usadas, foliomens = "" };
                                    db.Insert(sugeridos);
                                }
                                else
                                {
                                    XLoteSug sugeridos = new XLoteSug { recibosug = row["recibo"].ToString().Trim(), fecrecsug = diacad + "/" + mescad, cveprod = prod, Tarima = row["tarima"].ToString().Trim(), Cajasdis = 0, Cajasusadas = usadas, foliomens = "" };
                                    db.Insert(sugeridos);
                                }

                                diacadant = diacad;
                                mescadant = mescad;

                            }
                            else
                            {
                                var loteSug = db.Query<XLoteSug>("Select  *  FROM XLoteSug Where cveprod = '" + captu.Codigo.Trim() + "' AND cajasdis != 0 LIMIT 1");

                                foreach (var capturado in loteSug)
                                {
                                    string recibosug = capturado.recibosug;
                                    string fecrecsug = capturado.fecrecsug;
                                    string cveprod = capturado.cveprod;
                                    string tarima = capturado.Tarima;
                                    int cajasdis = capturado.Cajasdis;
                                    int cajasusadas = capturado.Cajasusadas;
                                    Mensajes mensa = new Mensajes { titulo = "Existe un folio anterior disponible", mensaje = "El recibo " + "\n\r" + capturado.recibosug.ToString().Trim() + " De la tarima  " + capturado.Tarima.Trim() + " Tiene  " + capturado.Cajasdis + " cajas disponibles del producto: " + captu.nombre.Trim() + " Con Fecha de     del" + capturado.fecrecsug };
                                    db.Insert(mensa);
                                    ValiFechacad = "N";
                                    db.Query<XLoteSug>("DELETE  FROM XLoteSug Where cveprod = '" + captu.Codigo.Trim() + "' AND cajasdis != 0 AND Cajasusadas <= 0");
                                    XLoteSug sugeridosact = new XLoteSug { recibosug = recibosug.ToString().Trim(), fecrecsug = fecrecsug, cveprod = cveprod, Tarima = tarima.ToString().Trim(), Cajasdis = cajasdis, Cajasusadas = cajasusadas, foliomens = "S" };
                                    db.Insert(sugeridosact);
                                    totalpro = 0;
                                }

                            }
                        }
                    }

                }


            }


            return Valor;


        }

        private string traemes(int mes)
        {
            string nom = "";
            switch (mes)
            {
                case 1: { nom = "ENE"; break; }
                case 2: { nom = "FEB"; break; }
                case 3: { nom = "MAR"; break; }
                case 4: { nom = "ABR"; break; }
                case 5: { nom = "MAY"; break; }
                case 6: { nom = "JUN"; break; }
                case 7: { nom = "JUL"; break; }
                case 8: { nom = "AGO"; break; }
                case 9: { nom = "SEP"; break; }
                case 10: { nom = "OCT"; break; }
                case 11: { nom = "NOV"; break; }
                case 12: { nom = "DIC"; break; }
            }
            return nom;
        }

        private string validaprod()
        {
            dondegenera = "validaprod";
            /*string ok = "S", nom = "";
            Surtidomayor = "S";
            var productoscapturadosx = db.Table<xprod>();
            var productoscapturados = db.Table<ConPedidos>();
            foreach (var captu in productoscapturados)
            {
                if (Convert.ToInt32(captu.pedido) > Convert.ToInt32(captu.surtido))
                {

                    //Mensajes mensa = new Mensajes { titulo = "Error En el Producto", mensaje = "Producto " + captu.nombre.ToString() + "  Surtido es menor al Pedido" + "\n\r" + nom + "\n\r" + " Pedidos: " + captu.pedido.ToString() + "  Surtidos: " + captu.surtido.ToString() };
                    //db.Insert(mensa);

                    //ok = "N";
                }
                else if (Convert.ToInt32(captu.pedido) < Convert.ToInt32(captu.surtido))
                {
                    Mensajes mensa2 = new Mensajes { titulo = "Error En el Producto", mensaje = "Producto " + captu.nombre.ToString() + " Surtido es Mayor al Pedido " + "\n\r" + nom + "\n\r" + " Pedidos: " + captu.pedido.ToString() + "  Surtidos: " + captu.surtido.ToString() + " Favor de Iniciar Captura Inversa (Borrado de Cajas) o Cancelar parcial segun sea el caso" };
                    db.Insert(mensa2);

                    Surtidomayor = "NR";
                }
            }
            return ok;*/
            string ok = "S", nom = "";
            Surtidomayor = "S";

            var productoscapturadosx = db.Table<xprod>();

            var productoscapturados = db.Query<ConPedidos>("Select * FROM ConPedidos Where CAST(surtido  AS INTEGER) > CAST(pedido AS INTEGER)");
            foreach (var captu in productoscapturados)
            {
                Mensajes mensa2 = new Mensajes { titulo = "Error En el Producto", mensaje = "Producto " + captu.nombre.ToString() + " Surtido es Mayor al Pedido " + "\n\r" + nom + "\n\r" + " Pedidos: " + captu.pedido.ToString() + "  Surtidos: " + captu.surtido.ToString() + " Debe Iniciar la captura nuevamente" };
                db.Insert(mensa2);
                Surtidomayor = "NR";
            }
            return ok;
        }

        void fnShowCustomAlertDialog()
        {
            //Inflate layout
            View view = LayoutInflater.Inflate(Resource.Layout.frmsupervisor, null);
            var builder = new MaterialAlertDialogBuilder(this, Resource.Style.ThemeOverlay_Material3_MaterialAlertDialog);
            //AndroidX.AppCompat.App.AlertDialog builder = new AndroidX.AppCompat.App.AlertDialog.Builder(this).Create();
            builder.SetView(view);
            builder.SetCancelable(false);
            //builder.SetCanceledOnTouchOutside(false);
            password = view.FindViewById<EditText>(Resource.Id.txtPassword);
            password.LongClickable = false;
            password.Enabled = false;
            Button buttonaceptar = view.FindViewById<Button>(Resource.Id.btnLoginLL);
            Button button = view.FindViewById<Button>(Resource.Id.btnClearLL);
            button.Click += delegate
            {
                builder.Dispose();

            };
            buttonaceptar.Click += delegate
            {
                thisConnection.Open();
                string cadena = "Select usuario,password From tb_Autoriza_OdeP Where password = '" + password.Text.Trim() + "' AND clave = 'EM' AND obs = 'Autoriza Caducidad'";
                SqlCommand cmd = new SqlCommand(cadena, thisConnection);
                mAutoriza = Convert.ToString(cmd.ExecuteScalar());
                if (mAutoriza.Trim().Length == 0)
                {
                    Toast.MakeText(this, "PASSWORD INCORRECTO!!!", ToastLength.Short).Show();
                    thisConnection.Close();
                }
                else
                {
                    thisConnection.Close();

                    AutoPed = "S";
                    Guardar.Enabled = true;
                    builder.Dispose();
                }

            };
            builder.Show();
        }

        void fnShowCustomAlertDialogCancel()
        {
            //Inflate layout
            View view = LayoutInflater.Inflate(Resource.Layout.frmsupervisor, null);
            AndroidX.AppCompat.App.AlertDialog builder = new AndroidX.AppCompat.App.AlertDialog.Builder(this).Create();
            builder.SetView(view);
            builder.SetCanceledOnTouchOutside(false);
            TextView titulo = view.FindViewById<TextView>(Resource.Id.titleLogin);
            password = view.FindViewById<EditText>(Resource.Id.txtPassword);
            Button buttonaceptar = view.FindViewById<Button>(Resource.Id.btnLoginLL);
            Button button = view.FindViewById<Button>(Resource.Id.btnClearLL);
            button.Click += delegate
            {
                builder.Dismiss();

            };

            password.LongClickable = false;
            titulo.Text = "Autorizacion Folios Adelantados";
            buttonaceptar.Click += delegate
            {
                thisConnection.Open();
                string cadena = "Select usuario,password From tb_Autoriza_OdeP Where password = '" + password.Text.Trim() + "' AND clave = 'EM'";
                SqlCommand cmd = new SqlCommand(cadena, thisConnection);
                mAutoriza = Convert.ToString(cmd.ExecuteScalar());
                if (mAutoriza.Trim().Length == 0)
                {
                    Toast.MakeText(this, "PASSWORD INCORRECTO!!!", ToastLength.Short).Show();
                    thisConnection.Close();
                }
                else
                {
                    thisConnection.Close();
                    Guardar.Enabled = true;
                    builder.Dismiss();
                }

            };
            builder.Show();
        }

        private string repetido(string mtip, string mfol, string mcod, string mtar, string mcaj)
        {
            string Ok = "N";
            var productoscapturados = db.Query<xprod>("Select * FROM xprod Where Codigo = '" + mcod + "' AND Folio = '" + mfol + "'  AND Tarima = '" + mtar + "' AND Cajas = '" + mcaj + "' AND Tipo = '" + mtip + "'");

            foreach (var captu in productoscapturados)
            {
                Ok = "S";
                break;
            }
            return Ok;
        }

        private int traetotal(string mcod)
        {
            int total = 0;
            var productoscapturados = db.Query<ConPedidos>("Select * FROM ConPedidos Where prod_clave = '" + mcod + "'");
            foreach (var captu in productoscapturados)
            {
                total = Convert.ToInt32(captu.surtido);
                break;
            }
            return total;
        }

        private int traetotalpedido(string mcod)
        {
            int total = 0;
            var productoscapturados = db.Query<ConPedidos>("Select * FROM ConPedidos Where prod_clave = '" + mcod + "'");
            foreach (var captu in productoscapturados)
            {
                total = Convert.ToInt32(captu.pedido);
                break;
            }
            return total;
        }

        private int traerecibosvencidos(string codigo, string tipo)
        {
            int total = 0;
            var productoscapturados = db.Query<xLote>("select Folio, Codigo, Tarima, Count(Cajas) AS Cajas FROM xLote Where Codigo = '" + codigo + "' AND Tipo = '" + tipo + "' Group by Folio, Codigo, Tarima");
            foreach (var captu in productoscapturados)
            {
                int total_recibo_cap = Convert.ToInt32(captu.Cajas);
                if (tipo == "PTC")
                    cadena = "SELECT ETIQUETA AS PROD,SURTIDO,FECHA_CAD AS FECCAD, pti_estatus_sur AS estatus_sur FROM TB_DET_TRAZABILIDAD WHERE PROD_CLAVE = '" + captu.Codigo.Trim() + "' AND RECIBO = '" + captu.Folio.Trim() + "' " +
                             "AND TIPO = '" + tipo + "' AND TARIMA = '" + Convert.ToInt32(captu.Tarima).ToString() + "' ";

                else
                    cadena = "SELECT NUM_CAJAS AS PROD, CAJAS_SUR AS SURTIDO,NUM_LOTE AS FECCAD, estatus_sur FROM TB_DET_ETI_FINAL WHERE CVE_PROD = '" + captu.Codigo.Trim() + "' AND FOLIO = '" + captu.Folio.Trim() + "' " +
                        "AND TARIMA = '" + Convert.ToInt32(captu.Tarima).ToString() + "' ";

                SqlDataAdapter da = new SqlDataAdapter(cadena, thisConnection);
                DataSet ds = new DataSet();

                //MessageBox.Show(cadena); 
                da.Fill(ds, "Info");
                DataTable Info = ds.Tables["Info"];
                //MessageBox.Show(Info.Rows.Count.ToString()); 

                foreach (DataRow row in Info.Rows)
                {
                    int mP = Convert.ToInt32(row["PROD"]);
                    int mS = Convert.ToInt32(row["SURTIDO"]);

                    if (((total_recibo_cap + mS) > mP) || (row["estatus_sur"].ToString().Trim() == "S"))
                    {
                        total = total + total_recibo_cap;
                        XLoteSug sugeridos = new XLoteSug { recibosug = captu.Folio.Trim(), fecrecsug = "/", cveprod = captu.Codigo.Trim(), Tarima = Convert.ToInt32(captu.Tarima).ToString(), Cajasdis = 0, Cajasusadas = total_recibo_cap };
                        db.Insert(sugeridos);
                    }
                }
            }
            return total;
        }

        void insertarinfo()
        {
            dondegenera = "inserinfo";
            if (respaldo_activo == 1)
            {
                string horaactual = DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss");
                string pedido_actual = pedidoencaptura.Text.Trim().Replace("Pedido Actual: ", "");
                string numerosplit = nosplit.Text.Trim().Replace("Split Numero: ", "");
                string cadenainfomensaje = "";

                thisConnection.Open();
                try
                {

                    var productoscapturados = db.Table<xLote>();
                    foreach (var captu in productoscapturados)
                    {
                        string mtip = "", mfol = "", mcod = "", mtar = "", mcaj = "", mdia = "", mmes = "", mfeccap = "";

                        mtip = captu.Tipo.ToString().Trim();
                        mfol = captu.Folio.ToString().Trim();
                        mcod = captu.Codigo.ToString().Trim();
                        mtar = captu.Tarima.ToString().Trim();
                        mcaj = captu.Cajas.ToString().Trim();
                        mdia = captu.diacad.ToString().Trim();
                        mmes = captu.mescad.ToString().Trim();
                        mfeccap = captu.fecha_captura.ToString().Trim();
                        string lectura = mtip + mfol + mcod + mtar + mcaj;
                        string nom = traenom(mcod);

                        cadenainfomensaje = "insert into Tb_Etiqueta_Capturada_Validar(Fecha, emb_folio, fecha_cap, Eti_Lectura, Eti_Recibo, Eti_Producto, Eti_Caja, Eti_TarIni, Eti_TarFin, Cve_Camioneta, FecCap, Version, Imei, Split, veces) " +
                                       "Values ('" + horaactual + "', '" + pedido_actual + "','" + mfeccap + "','" + lectura + "', '" + mfol + "', '" + mcod + "', '" + mcaj + "', '" + mtar + "', '" + mtar + "', '', '" + mfeccap + "', '" + currentVersionName + "', '" + imei + "', '" + numerosplit + "', '" + veces + "')";
                        SqlCommand cmd = new SqlCommand(cadenainfomensaje, thisConnection);
                        cmd.ExecuteNonQuery();

                    }

                    var concentradocapturados = db.Table<ConPedidos>();
                    foreach (var captu in concentradocapturados)
                    {
                        string mnom = captu.nombre.ToString().Trim();
                        mnom = mnom.Replace("'", " ");

                        cadenainfomensaje = "insert into  Tb_Etiqueta_Split_Validar(Fecha, emb_folio, prod_clave, nombre, pedido, surtido, Split, veces) " +
                                       "Values ('" + horaactual + "', '" + pedido_actual + "','" + captu.prod_clave + "','" + mnom + "','" + captu.pedido + "','" + captu.surtido + "','" + numerosplit + "', '" + veces + "')";
                        SqlCommand cmd = new SqlCommand(cadenainfomensaje, thisConnection);
                        cmd.ExecuteNonQuery();
                    }

                }
                catch (System.Exception ex)
                {
                    SendMail("jgalvan@mrlucky.com.mx", "Error generado en el registro de Split de sistema split trailer detalle: " + ex + " Consulta " + cadenainfomensaje, "Error al guardar Mensajes de error embarque " + pedido_actual);
                }

                thisConnection.Close();

            }

            veces++;
        }

        void insertarinfoMensaje()
        {
            dondegenera = "inserarinfoMensaje";

            if (respaldo_activo == 1)
            {

                string horaactual = DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss");
                string pedido_actual = pedidoencaptura.Text.Trim().Replace("Pedido Actual: ", "");
                string numerosplit = nosplit.Text.Trim().Replace("Split Numero: ", "");
                string cadenainfomensaje = "";
                thisConnection.Open();
                try
                {

                    var query = db.Table<Mensajes>();
                    foreach (var captu in query)
                    {
                        string mnom = captu.mensaje.ToString().Trim();
                        mnom = mnom.Replace("'", " ");
                        cadenainfomensaje = "insert into Tb_Etiqueta_Mensajes_Validar(Fecha, emb_folio, titulo, mensaje, split, veces) " +
                                       "Values('" + horaactual + "', '" + pedido_actual + "','" + captu.titulo + "','" + mnom + "','" + numerosplit + "', '" + veces + "')";
                        SqlCommand cmd = new SqlCommand(cadenainfomensaje, thisConnection);
                        cmd.ExecuteNonQuery();

                    }


                }
                catch (System.Exception ex)
                {
                    SendMail("jgalvan@mrlucky.com.mx", "Error generado en el registro de mensajes de error de sistema split trailer detalle: " + ex + " Consulta " + cadenainfomensaje, "Error al guardar Mensajes de error embarque " + pedido_actual);
                }
                thisConnection.Close();
            }


        }

        public void SendMail(string Dest, string mBody, string mAsunto)
        {
            MailMessage msg = new MailMessage();
            MailMessage email = new MailMessage();

            string[] destinatarios = Dest.Split(';');
            foreach (string destinos in destinatarios)
            {
                email.To.Add(new MailAddress(destinos));
            }
            //email.To.Add(new MailAddress("gcamacho@mrlucky.com.mx"));

            email.From = new MailAddress("jgalvan@mrlucky.com.mx"); //
            email.Subject = mAsunto; //"Mensaje de Prueba";
            email.Body = mBody;  //"Información de la factura";
            email.IsBodyHtml = true;
            email.Priority = MailPriority.Normal;



            SmtpClient smtp = new SmtpClient();
            smtp.Host = "mail1.mrlucky.com.mx";
            smtp.Port = 587;
            smtp.EnableSsl = true;
            smtp.UseDefaultCredentials = false;
            smtp.Credentials = new NetworkCredential("jgalvan", "mnK3a2aN@1|Q21VV");

            try
            {
                smtp.Send(email);
                email.Dispose();
                RunOnUiThread(() => Toast.MakeText(this, "correo enviado exitosamente\r\n", ToastLength.Short).Show());
            }
            catch (System.Exception ex)
            {

                RunOnUiThread(() => Toast.MakeText(this, "correo no enviado\r\n" + ex.ToString(), ToastLength.Short).Show());
            }
        }

        private string NoSplit(string mped)
        {
            string Cadena = "Select MAX(tarima) from tb_det_split where emb_folio = '" + mped + "'";
            SqlCommand cmd = new SqlCommand(Cadena, thisConnection);
            string cad = Convert.ToString(cmd.ExecuteScalar());
            cad = (cad.Trim().Length == 0) ? "1" : (Convert.ToInt32(cad) + 1).ToString();
            return cad;
        }


        private void ConsPedSur(string mped)
        {
            thisConnection.Open();
            string cadena = "Select prod_clave as Codigo, nom_prod as Nombre, cant_ped as Pedido, 0 as Surtido from tb_ped_embarque Where emb_folio = '" + mped.Trim() + "' Order by nom_prod";
            SqlDataAdapter da = new SqlDataAdapter(cadena, thisConnection);
            DataSet ds = new DataSet();
            da.Fill(ds, "ConsPed");
            var ConsPed = ds.Tables["ConsPed"];
            cadena = "Select prod_clave, sum(cajas) as cajas from tb_det_split Where emb_folio = '" + mped.Trim() + "'" +
                     "And estatus != 'C' Group By prod_clave Order by prod_clave ASC";
            da = new SqlDataAdapter(cadena, thisConnection);
            ds = new DataSet();
            da.Fill(ds, "PedSur");
            var PedSur = ds.Tables["PedSur"];
            int Cp = 0, Cs = 0, sur = 0;
            thisConnection.Close();
            foreach (DataRow Row in ConsPed.Rows)
            {
                sur = 0;
                foreach (DataRow row in PedSur.Select("prod_clave = '" + Row["Codigo"].ToString() + "'"))
                    sur = Convert.ToInt32(row["Cajas"]);

                db.Query<ConPedidos>("UPDATE [ConPedidos] SET surtido = '" + sur + "' WHERE prod_clave = '" + Row["Codigo"].ToString().Trim() + "'");

                Cp += Convert.ToInt32(Row["pedido"]);
                Cs += sur;
            }

        }

        private void AgregaDetaEtiAdelantado()
        {
            var recibosatrasusa = db.Query<XLoteSug>("Select * FROM [XLoteSug] Where Cajasusadas != 0 Order By recibosug, cveprod");
            foreach (var recibos in recibosatrasusa)
            {
                if (recibos.Cajasusadas > 0)
                {
                    db.Query<xLote>("UPDATE [xLoteFinal] SET cajas = CAST(Cajas as int) - " + recibos.Cajasusadas + " WHERE Folio = '" + recibos.recibosug.ToString().Trim() + "' AND Codigo = '" + recibos.cveprod.ToString().Trim() + "' AND CAST(Tarima as int) = " + Convert.ToInt32(recibos.Tarima.ToString()));
                }
            }

            var productoscap = db.Query<xLoteFinal>("Select DISTINCT(Codigo) FROM [xLoteFinal] Order By Codigo ASC");
            foreach (var productos in productoscap)
            {
                var recibosatras = db.Query<XLoteSug>("Select  *  FROM XLoteSug Where cveprod = '" + productos.Codigo.Trim() + "' AND cajasdis != 0 AND foliomens = 'S' ORDER BY recibosug, Tarima LIMIT 1");
                foreach (var recibos in recibosatras)
                {

                    var folio = recibos.recibosug.Trim();
                    var producto = recibos.cveprod.Trim();
                    var tarima = recibos.Tarima.Trim();
                    var feccad = recibos.fecrecsug.Trim();

                    var recibosCapturados = db.Query<xLote>("Select * FROM [xLoteFinal] Where Codigo = '" + recibos.cveprod.ToString().Trim() + "' Order By Pedido, codigo");
                    foreach (var reccapturado in recibosCapturados)
                    {
                        string fechacaducidadcapturado = reccapturado.diacad.Trim() + "/" + reccapturado.mescad.Trim();
                        if (reccapturado.Folio.Trim() != folio && reccapturado.Tarima.Trim() != tarima)
                        {
                            if (fechacaducidadcapturado != feccad)
                            {
                                string cadena = "insert into tb_det_folio_adelantado (responsable, fecha, emb_folio, recibo_cap, fecreccap, recibo_sug, fecrecsug, prod_clave, producto, cantidad, autorizo, tarimacap, tarimasug, imei, motivo) " +
                               "Values('" + responsable + "','" + DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss tt") + "','" + reccapturado.Pedido + "', '" + reccapturado.Folio + "', '" + reccapturado.diacad + "/" + reccapturado.mescad + "','" + recibos.recibosug + "', '" + recibos.fecrecsug + "', '" + recibos.cveprod + "', '" + reccapturado.nombre + "', '" + reccapturado.Cajas + "', '" + mAutoriza.Trim() + "', '" + reccapturado.Tarima.Trim() + "', '" + recibos.Tarima.Trim() + "', '" + imei + "', '" + motfolade.Trim() + "')";
                                SqlCommand cmd = new SqlCommand(cadena, thisConnection);
                                cmd.ExecuteNonQuery();
                                break;
                            }
                        }
                    }
                }
            }
        }
        private string ValidaCajaPreesplit(string cadena)
        {
            string Cadena = "Select fecha_cap From Tb_Det_Etiqueta_Presplit " +
                          "Where Eti_Lectura = '" + cadena + "' AND Estatus = 'A'";
            SqlCommand cmd = new SqlCommand(Cadena, thisConnection);
            string Valor = Convert.ToString(cmd.ExecuteScalar());
            return Valor;
        }


        public void OnTextChanged(Java.Lang.ICharSequence s, int start, int before, int count)
        {

        }


        public void etiquetablanca()
        {
            string captura = foliocaptura.Text.Trim();
            int pos = captura.Trim().IndexOf("=");
            //MessageBox.Show(pos.ToString()); 
            if (pos == -1)
            {
                foliocaptura.Text = "";
                foliocaptura.RequestFocus();
                valorfinal = captura;
                return;
            }
            captura = captura.Substring(pos + 1, captura.Length - (pos + 1)).Trim();
            captura = captura.Replace("=", "");
            //Se Retrabaja en la validacion original de la etiqueta blanca, debido a errores con la etiqueta de aguilares *** 21/12/2022

            string mcaj = "", mtar = "", mcod = "", mfol = "", mtip = "", Ent = "N";

            for (int i = 0; i < CatProd.Rows.Count; i++)
            {
                string producto_clave = CatProd.Rows[i]["Prod_Clave"].ToString().Trim();
                bool esta = captura.Contains(producto_clave);

                if (esta)
                {
                    mcod = producto_clave;
                    break;
                }
            }

            int posprod = captura.Trim().IndexOf(mcod);

            /////ERROR AQUI MODIFICAR MAÑANA
            mfol = captura.Substring(0, posprod).Trim();
            mtip = "PTP";
            string restocaptura = captura.Replace(mfol, "").Replace(mcod, "");
            if (restocaptura.Length == 6)
            {
                if (mfol.Length == 5)
                {
                    mtip = "PTC";
                }
                mcaj = restocaptura.Substring(3, 3);
                mtar = restocaptura.Substring(0, 3);
            }
            else if (restocaptura.Length == 9)
            {
                mtip = "PTC";
                mcaj = restocaptura.Substring(6, 3);
                mtar = restocaptura.Substring(0, 3);
            }
            else
            {
                mtip = "PTC";
                mcaj = restocaptura.Substring(4, 3);
                mtar = restocaptura.Substring(0, 2);
            }


            /*

            int tam = captura.Length;
            
            if (tam > 20) //Etiqueta de Campo que no es Aguilares y Proceso Planta
            {
                Int32 ValorFolio = Convert.ToInt32(captura.Substring(0, 6));
                if (ValorFolio > FolioCampo) // Etiqueta de Campo
                { // Etiqueta de Campo
                    Ent = "S";
                }
                else
                {
                    mcaj = foliocaptura.Text.Substring(tam - 3, 3);
                    mtar = foliocaptura.Text.Substring(tam - 7, 2);
                    mfol = foliocaptura.Text.Substring(0, 6);
                    mcod = foliocaptura.Text.Substring(6, tam - 13);
                    mtip = "PTC";
                    if (traenom(mcod) != "")
                    {
                        Ent = "S";
                    }
                }
            }
            if (Ent == "N") // Valido si el PTP Planta o PTC de Aguilares
            {
                mcaj = captura.Substring(tam - 3, 3);
                mtar = captura.Substring(tam - 6, 3);
                int tam2 = tam - 6;
                mtip = "PTP";
                if (tam2 == 15) // Etiqueta de Aguilares	
                {
                    mfol = captura.Substring(0, 5);
                    mcod = captura.Substring(5, tam - 11);
                    mtip = "PTC";
                }
                else if (tam2 <= 14) // Etiqueta de Aguilares	
                {
                    mfol = captura.Substring(0, 4);
                    mcod = captura.Substring(4, tam - 10);
                    mtip = "PTC";
                }
                else
                {
                    mfol = captura.Substring(0, 6);
                    mcod = captura.Substring(6, tam - 12);
                }
                var nombreproducto = traenom(mcod); //Valido si existe el producto, si no quiere decir que es recibo de 6 digitos pero de produccion


                if (nombreproducto == "")
                {
                    mfol = captura.Substring(0, 6);
                    mcod = captura.Substring(6, tam - 12);
                    mtip = "PTP";
                }

                nombreproducto = traenom(mcod); //Valido si existe el producto, si no quiere decir que es recibo de 6 digitos

                if (nombreproducto == "")
                {
                    mcaj = captura.Substring(tam - 2, 2);
                    mtar = captura.Substring(tam - 4, 2);
                    mfol = captura.Substring(0, 6);
                    mcod = captura.Substring(6, tam - 10);
                    mtip = "PTC";
                }

                nombreproducto = traenom(mcod);

                if (nombreproducto == "")
                {
                    mcaj = captura.Substring(tam - 3, 3);
                    mtar = captura.Substring(tam - 6, 3);
                    mfol = captura.Substring(0, 5);
                    mcod = captura.Substring(5, tam - 11);
                    mtip = "PTC";
                }
            }
            else //Etiqueta de Campo que no es Aguilares
            {
                mcaj = captura.Substring(tam - 3, 3);
                mtar = captura.Substring(tam - 7, 2);
                mfol = captura.Substring(0, 6);
                mcod = captura.Substring(6, tam - 13);
                mtip = "PTC";
            }*/
            mtip = mtip.Trim();
            mfol = mfol.Trim();
            mcod = mcod.Trim();
            mtar = mtar.Trim();
            mcaj = mcaj.Trim();


            string cad = mtip + " | " + mfol + " | " + mcod + " | " + mtar + " | " + mcaj;
            if (repetido(mtip, mfol, mcod, mtar, mcaj) != "S")
            {
                string lectura = mtip + mfol + mcod + mtar + mcaj;
                lectura = lectura.Trim();

                try
                {
                    xprod Pedidoscapturados = new xprod { Tipo = mtip, Folio = mfol, Codigo = mcod, Tarima = mtar, Cajas = mcaj, fecha_captura = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"), tipo_captura = "B", Lecturabd = lectura };
                    db.Insert(Pedidoscapturados);

                    int totalx = traetotal(mcod);

                    totalx = totalx + 1;

                    string existeprod = "NO";
                    var pedidos = db.Query<ConPedidos>("Select * FROM ConPedidos Where prod_clave = '" + mcod.ToString().Trim() + "'");

                    foreach (var pedisur in pedidos)
                    {
                        db.Query<ConPedidos>("UPDATE [ConPedidos] SET surtido = '" + totalx + "' WHERE prod_clave = '" + mcod.ToString() + "'");
                        existeprod = "SI";
                    }


                    if (existeprod == "NO")
                    {
                        ConPedidos ConsecutivosPedidos = new ConPedidos { prod_clave = mcod.ToString(), nombre = traenom(mcod.ToString().Trim()), pedido = 0, surtido = Convert.ToInt16(totalx) };
                        db.Insert(ConsecutivosPedidos);
                    }



                    TotCaj++;
                    total.Text = TotCaj.ToString("##0");

                    listItem.Add(new FlimStarInfo()
                    {
                        Name = traenom(mcod.ToString().Trim()),
                        Age = "Recibo: " + mfol + "Tarima: " + mtar + " Caja: " + mcaj,
                        ImageID = Resource.Drawable.producto
                    });
                }
                catch
                {
                    Toast.MakeText(this, "Duplicidad Evitada", ToastLength.Short).Show();
                }

            }
            foliocaptura.Text = "";
            foliocaptura.RequestFocus();
            valorfinal = captura;



            List<FlimStarInfo> lstFlimStar = listItem;
            var gvObject = FindViewById<GridView>(Resource.Id.gvCtr2);
            gvObject.Adapter = new myGVItemAdapter(this, lstFlimStar);
        }


        public void eliminaretiquetablanca()
        {
            int pos = foliocaptura.Text.Trim().IndexOf("=");
            //MessageBox.Show(pos.ToString()); 
            if (pos == -1)
            {
                foliocaptura.SetSelection(0, foliocaptura.Text.Length);
                foliocaptura.RequestFocus();
                valorfinal = foliocaptura.Text;
                return;
            }
            foliocaptura.Text = foliocaptura.Text.Substring(pos + 1, foliocaptura.Text.Length - (pos + 1)).Trim();
            foliocaptura.Text = foliocaptura.Text.Replace("=", "");
            int tam = foliocaptura.Text.Length;
            string mcaj = "", mtar = "", mcod = "", mfol = "", mtip = "", Ent = "N";

            for (int i = 0; i < CatProd.Rows.Count; i++)
            {
                string producto_clave = CatProd.Rows[i]["Prod_Clave"].ToString().Trim();
                bool esta = foliocaptura.Text.Contains(producto_clave);

                if (esta)
                {
                    mcod = producto_clave;
                    break;
                }
            }

            int posprod = foliocaptura.Text.Trim().IndexOf(mcod);
            mfol = foliocaptura.Text.Substring(0, posprod).Trim();
            mtip = "PTP";
            string restocaptura = foliocaptura.Text.Replace(mfol, "").Replace(mcod, "");
            if (restocaptura.Length == 6)
            {
                if (mfol.Length == 5)
                {
                    mtip = "PTC";
                }
                mcaj = restocaptura.Substring(3, 3);
                mtar = restocaptura.Substring(0, 3);
            }
            else if (restocaptura.Length == 9)
            {
                mtip = "PTC";
                mcaj = restocaptura.Substring(6, 3);
                mtar = restocaptura.Substring(0, 3);
            }
            else
            {
                mtip = "PTC";
                mcaj = restocaptura.Substring(4, 3);
                mtar = restocaptura.Substring(0, 2);
            }

            /*
            if (tam > 20) //Etiqueta de Campo que no es Aguilares y Proceso Planta
            {
                Int32 ValorFolio = Convert.ToInt32(foliocaptura.Text.Substring(0, 6));
                if (ValorFolio > FolioCampo) // Etiqueta de Campo
                    Ent = "S";
            }
            if (Ent == "N") // Valido si el PTP Planta o PTC de Aguilares
            {
                mcaj = foliocaptura.Text.Substring(tam - 3, 3);
                mtar = foliocaptura.Text.Substring(tam - 6, 3);
                int tam2 = tam - 6;
                mtip = "PTP";
                if (tam2 == 15) // Etiqueta de Aguilares	
                {
                    mfol = foliocaptura.Text.Substring(0, 5);
                    mcod = foliocaptura.Text.Substring(5, tam - 11);
                    mtip = "PTC";
                }
                else if (tam2 <= 14) // Etiqueta de Aguilares	
                {
                    mfol = foliocaptura.Text.Substring(0, 4);
                    mcod = foliocaptura.Text.Substring(4, tam - 10);
                    mtip = "PTC";
                }
                else
                {
                    mfol = foliocaptura.Text.Substring(0, 6);
                    mcod = foliocaptura.Text.Substring(6, tam - 12);
                }
                var nombreproducto = traenom(mcod); //Valido si existe el producto, si no quiere decir que es recibo de 6 digitos pero de produccion


                if (nombreproducto == "")
                {
                    mfol = foliocaptura.Text.Substring(0, 6);
                    mcod = foliocaptura.Text.Substring(6, tam - 12);
                    mtip = "PTP";
                }

                nombreproducto = traenom(mcod); //Valido si existe el producto, si no quiere decir que es recibo de 6 digitos

                if (nombreproducto == "")
                {
                    mcaj = foliocaptura.Text.Substring(tam - 2, 2);
                    mtar = foliocaptura.Text.Substring(tam - 4, 2);
                    mfol = foliocaptura.Text.Substring(0, 6);
                    mcod = foliocaptura.Text.Substring(6, tam - 10);
                    mtip = "PTC";
                }

                nombreproducto = traenom(mcod);

                if (nombreproducto == "")
                {
                    mcaj = foliocaptura.Text.Substring(tam - 3, 3);
                    mtar = foliocaptura.Text.Substring(tam - 6, 3);
                    mfol = foliocaptura.Text.Substring(0, 5);
                    mcod = foliocaptura.Text.Substring(5, tam - 11);
                    mtip = "PTC";
                }
            }
            else //Etiqueta de Campo que no es Aguilares
            {
                mcaj = foliocaptura.Text.Substring(tam - 3, 3);
                mtar = foliocaptura.Text.Substring(tam - 7, 2);
                mfol = foliocaptura.Text.Substring(0, 6);
                mcod = foliocaptura.Text.Substring(6, tam - 13);
                mtip = "PTC";
            }*/
            string cad = mtip + " | " + mfol + " | " + mcod + " | " + mtar + " | " + mcaj;
            if (repetido(mtip, mfol, mcod, mtar, mcaj) == "S")
            {
                //xprod Pedidoscapturados = new xprod { Tipo = mtip, Folio = mfol, Codigo = mcod, Tarima = mtar, Cajas = mcaj, fecha_captura = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") };
                //db.Insert(Pedidoscapturados);

                db.Query<xprod>("DELETE FROM xprod Where Codigo = '" + mcod + "' AND Folio = '" + mfol + "'  AND Tarima = '" + mtar + "' AND Cajas = '" + mcaj + "' AND Tipo = '" + mtip + "'");

                int totalx = traetotal(mcod);



                totalx = totalx - 1;

                string existeprod = "NO";
                var pedidos = db.Query<ConPedidos>("Select * FROM ConPedidos Where prod_clave = '" + mcod.ToString().Trim() + "'");

                foreach (var pedisur in pedidos)
                {
                    db.Query<ConPedidos>("UPDATE [ConPedidos] SET surtido = '" + totalx + "' WHERE prod_clave = '" + mcod.ToString() + "'");
                    existeprod = "SI";
                }

                int totalP = traetotalpedido(mcod);

                if ((totalx < 1) && totalP == 0)
                {
                    db.Query<ConPedidos>("Delete FROM ConPedidos Where prod_clave = '" + mcod.ToString().Trim() + "'");
                }


                TotCaj--;
                total.Text = TotCaj.ToString("##0");

                string nombreprod = traenom(mcod.ToString().Trim());

                foreach (var item in listItem.ToArray())
                {
                    string descrip = "Recibo: " + mfol + "Tarima: " + mtar + " Caja: " + mcaj;
                    if (item.Name == nombreprod.ToString().Trim() && item.Age == descrip)
                    {
                        listItem.Remove(item);
                    }
                }


                /*listItem.Remove(new FlimStarInfo()
                {
                    Name = traenom(mcod.ToString().Trim()),
                    Age = "Recibo: " + mfol + "Tarima: " + mtar + " Caja: " + mcaj,
                    ImageID = Resource.Drawable.producto
                });*/
            }
            foliocaptura.SetSelection(0, foliocaptura.Text.Length);
            foliocaptura.RequestFocus();
            valorfinal = foliocaptura.Text;



            List<FlimStarInfo> lstFlimStar = listItem;
            var gvObject = FindViewById<GridView>(Resource.Id.gvCtr2);
            gvObject.Adapter = new myGVItemAdapter(this, lstFlimStar);
        }


        //public void etiquetaverde()
        //{
        //    int tam = foliocaptura.Text.Length;
        //    string mcaj = "", mtar = "", mcod = "", mfol = "", mtip = "", Ent = "N";
        //    if (foliocaptura.Text.Trim().Contains(" ") == true)
        //    {
        //        if (tam < 18)
        //        {
        //            mtar = foliocaptura.Text.Substring(tam - 3, 3);
        //            mfol = foliocaptura.Text.Substring(0, 5);
        //            mcod = foliocaptura.Text.Replace(mfol, "");
        //            mcod = mcod.Replace(mtar, "");
        //            mtar = mtar.Replace(" ", "0");
        //            mtip = "PTC";
        //        }
        //        else
        //        {
        //            mtar = foliocaptura.Text.Substring(tam - 3, 3);
        //            mfol = foliocaptura.Text.Substring(0, 6);
        //            mcod = foliocaptura.Text.Replace(mfol, "");
        //            mcod = mcod.Replace(mtar, "");
        //            mtar = mtar.Replace(" ", "0");
        //            mtip = "PTP";
        //            if (mfol.Substring(0, 1) == "0")
        //            {
        //                mtip = "PTC";
        //                mfol = Convert.ToInt32(mfol).ToString();
        //            }
        //        }
        //    }
        //    else
        //    {
        //        string mtari = foliocaptura.Text.Substring(tam - 4, 4);
        //        mtar = foliocaptura.Text.Substring(tam - 4, 2);
        //        mfol = foliocaptura.Text.Substring(0, 6);
        //        mcod = foliocaptura.Text.Replace(mfol, "");
        //        mcod = mcod.Replace(mtari, "");
        //        mtip = "PTC";

        //    }




        //    DataTable Foliosleidos = new DataTable();
        //    string CadenaFolios = "Select Eti_Lectura, fecha_cap From tb_Det_Etiqueta " +
        //                   "WHERE (Eti_Producto = '" + mcod + "') AND (Eti_Recibo = '" + mfol + "') AND (Eti_TarIni = '" + mtar + "')";
        //    thisConnection.Open();
        //    SqlDataAdapter da = new SqlDataAdapter(CadenaFolios, thisConnection);
        //    DataSet ds = new DataSet();
        //    da.Fill(ds, "Foliosleidos");
        //    Foliosleidos = ds.Tables["Foliosleidos"];
        //    thisConnection.Close();

        //    DataTable FoliosleidosPresplit = new DataTable();
        //    string CadenaFoliospreesplit = "Select Eti_Lectura, fecha_cap From tb_Det_Etiqueta " +
        //                   "WHERE (Eti_Producto = '" + mcod + "') AND (Eti_Recibo = '" + mfol + "') AND (Eti_TarIni = '" + mtar + "')";
        //    thisConnection.Open();
        //    SqlDataAdapter dapre = new SqlDataAdapter(CadenaFoliospreesplit, thisConnection);
        //    DataSet dspre = new DataSet();
        //    dapre.Fill(dspre, "FoliosleidosPresplit");
        //    FoliosleidosPresplit = dspre.Tables["FoliosleidosPresplit"];
        //    thisConnection.Close();

        //    string cadenatarimacompleta = "";

        //    if (mtip == "PTP")
        //    {
        //        cadenatarimacompleta = "SELECT (num_cajas - CAJAS_SUR) AS DISPONIBLE FROM TB_DET_ETI_FINAL WHERE CVE_PROD = '" + mcod.Trim() + "' AND FOLIO = '" + mfol.Trim() + "' " +
        //    "AND TARIMA = '" + Convert.ToInt32(mtar.Trim()).ToString() + "' ";

        //    }
        //    else
        //    {
        //        cadenatarimacompleta = "SELECT (etiqueta - surtido) AS DISPONIBLE FROM TB_DET_TRAZABILIDAD WHERE PROD_CLAVE = '" + mcod.Trim() + "' AND RECIBO = '" + mfol.Trim() + "' " +
        //         "AND TIPO = '" + mtip + "' AND TARIMA = '" + Convert.ToInt32(mtar.Trim()).ToString() + "' ";
        //    }

        //    thisConnection.Open();
        //    SqlCommand cmd = new SqlCommand(cadenatarimacompleta, thisConnection);
        //    int disponible = Convert.ToInt32(cmd.ExecuteScalar());

        //    thisConnection.Close();



        //    if (disponible > 0)
        //    {
        //        int total_caja_verde = 0;
        //        disponible++;
        //        int n = 1;
        //        int cajaactual = 1;
        //        while (n < disponible)
        //        {
        //            if (cajaactual.ToString().Length == 1)
        //            {
        //                mcaj = "00" + cajaactual.ToString();
        //            } else if (cajaactual.ToString().Length == 2) {
        //                mcaj = "0" + cajaactual.ToString();
        //            }
        //            else
        //            {
        //                mcaj = cajaactual.ToString();
        //            }

        //            mtip = mtip.Trim();
        //            mfol = mfol.Trim();
        //            mcod = mcod.Trim();
        //            mtar = mtar.Trim();
        //            mcaj = mcaj.Trim();

        //            string lectura = mtip + mfol + mcod + mtar + mcaj;
        //            thisConnection.Open();
        //            string fechacap = ValidaCajaEtiVerde(lectura, Foliosleidos).Trim();
        //            string fechacappre = ValidaCajaPreesplitVerde(lectura, FoliosleidosPresplit).Trim();
        //            thisConnection.Close();
        //            if (fechacap.Length > 0)
        //            {
        //                cajaactual++;
        //            }
        //            else if (fechacappre.Length > 0)
        //            {
        //                cajaactual++;
        //            }
        //            else
        //            {
        //                string cad = mtip + " | " + mfol + " | " + mcod + " | " + mtar + " | " + mcaj;
        //                if (repetido(mtip, mfol, mcod, mtar, mcaj) != "S")
        //                {
        //                    xprod Pedidoscapturados = new xprod { Tipo = mtip, Folio = mfol, Codigo = mcod, Tarima = mtar, Cajas = mcaj, fecha_captura = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"), tipo_captura = "V" };
        //                    db.Insert(Pedidoscapturados);

        //                    int totalx = traetotal(mcod);

        //                    totalx = totalx + 1;


        //                    var pedidos = db.Table<ConPedidos>();

        //                    string existeprod = "NO";
        //                    foreach (var pedisur in pedidos)
        //                    {
        //                        if (pedisur.prod_clave.ToString().Trim() == mcod.ToString().Trim())
        //                        {
        //                            existeprod = "SI";
        //                        }
        //                    }


        //                    if (existeprod == "SI")
        //                    {
        //                        db.Query<ConPedidos>("UPDATE [ConPedidos] SET surtido = '" + totalx + "' WHERE prod_clave = '" + mcod.ToString() + "'");
        //                    }
        //                    else
        //                    {
        //                        ConPedidos ConsecutivosPedidos = new ConPedidos { prod_clave = mcod.ToString(), nombre = traenom(mcod.ToString().Trim()), pedido = 0, surtido = Convert.ToInt16(totalx) };
        //                        db.Insert(ConsecutivosPedidos);
        //                    }

        //                    cajaactual++;
        //                    total_caja_verde++;
        //                    TotCaj++;
        //                    total.Text = TotCaj.ToString("##0");
        //                    listItem.Add(new FlimStarInfo()
        //                    {
        //                        Name = traenom(mcod.ToString().Trim()),
        //                        Age = "Recibo: " + mfol + "Tarima: " + mtar + " Caja: " + mcaj,
        //                        ImageID = Resource.Drawable.producto
        //                    });

        //                }

        //                n++;
        //            }

        //        }
        //        foliocaptura.SetSelection(0, foliocaptura.Text.Length);
        //        foliocaptura.RequestFocus();
        //        valorfinal = foliocaptura.Text;
        //        //iMPRESION DE MENSAJE QUE INDICARA CUANTO DE CADA TARIMA SE LOGRO CARGAR Y SIMULAR
        //        Android.App.AlertDialog.Builder alertDialog = new Android.App.AlertDialog.Builder(this);
        //        alertDialog.SetTitle(Html.FromHtml("<font color='#55F721' size = 10>LECTURA POR TARIMA</font>"));
        //        alertDialog.SetIcon(Resource.Drawable.nota);
        //        alertDialog.SetMessage(Html.FromHtml("<font color='#9FFA7A' size = 10>Se han Capturado " + total_caja_verde + " Cajas,  Del Folio " + mfol + " De la tarima " + mtar + " Del Producto " + traenom(mcod.ToString().Trim()) + "</font>"));
        //        alertDialog.SetNeutralButton("Ok", delegate
        //        {
        //            alertDialog.Dispose();
        //        });
        //        alertDialog.Show();

        //    }
        //    else
        //    {
        //        Android.App.AlertDialog.Builder alertDialog = new Android.App.AlertDialog.Builder(this);
        //        alertDialog.SetTitle(Html.FromHtml("<font color='#55F721' size = 10>EXISTENCIA NO DISPONIBLE</font>"));
        //        alertDialog.SetIcon(Resource.Drawable.nota);
        //        alertDialog.SetMessage(Html.FromHtml("<font color='#9FFA7A' size = 10>La Tarima Actual No Cuenta con Existencia Disponible, Favor de Depurar los folios correspondientes y volver a leer</font>"));
        //        alertDialog.SetNeutralButton("Ok", delegate
        //        {
        //            alertDialog.Dispose();
        //        });
        //        alertDialog.Show();
        //        foliocaptura.SetSelection(0, foliocaptura.Text.Length);
        //        foliocaptura.RequestFocus();
        //        valorfinal = foliocaptura.Text;
        //    }

        //    List<FlimStarInfo> lstFlimStar = listItem;
        //    var gvObject = FindViewById<GridView>(Resource.Id.gvCtr2);
        //    gvObject.Adapter = new myGVItemAdapter(this, lstFlimStar);
        //}

        public void etiquetaverde()
        {
            int tam = foliocaptura.Text.Length;
            string mcaj = "", mtar = "", mcod = "", mfol = "", mtip = "", Ent = "N";
            string Bcaj = "", Btar = "", Bcod = "", Bfol = "", Btip = "", BEnt = "N";
            if (foliocaptura.Text.Trim().Contains(" ") == true)
            {
                if (tam < 18)
                {
                    mtar = foliocaptura.Text.Substring(tam - 3, 3);
                    mfol = foliocaptura.Text.Substring(0, 5);
                    mcod = foliocaptura.Text.Replace(mfol, "");
                    mcod = mcod.Replace(mtar, "");
                    mtar = mtar.Replace(" ", "0");
                    mtip = "PTC";
                }
                else
                {
                    mtar = foliocaptura.Text.Substring(tam - 3, 3);
                    mfol = foliocaptura.Text.Substring(0, 6);
                    mcod = foliocaptura.Text.Replace(mfol, "");
                    mcod = mcod.Replace(mtar, "");
                    mtar = mtar.Replace(" ", "0");
                    mtip = "PTP";
                    if (mfol.Substring(0, 1) == "0")
                    {
                        mtip = "PTC";
                        mfol = Convert.ToInt32(mfol).ToString();
                    }
                }
            }
            else
            {

                for (int i = 0; i < CatProd.Rows.Count; i++)
                {
                    string producto_clave = CatProd.Rows[i]["Prod_Clave"].ToString().Trim();
                    bool esta = foliocaptura.Text.Contains(producto_clave);

                    if (esta)
                    {
                        mcod = producto_clave;
                        break;
                    }
                }

                int posprod = foliocaptura.Text.Trim().IndexOf(mcod);
                mfol = foliocaptura.Text.Substring(0, posprod).Trim();
                string restocaptura = foliocaptura.Text.Replace(mfol, "").Replace(mcod, "");
                if (restocaptura.Length == 6)
                {
                    mtip = "PTC";
                    mtar = restocaptura.Substring(0, 3);
                }
                else
                {
                    mtip = "PTC";
                    mtar = restocaptura.Substring(0, 2);
                }

                /*string mtari = foliocaptura.Text.Substring(tam - 4, 4);
                mtar = foliocaptura.Text.Substring(tam - 4, 2);
                mfol = foliocaptura.Text.Substring(0, 6);
                mcod = foliocaptura.Text.Replace(mfol, "");
                mcod = mcod.Replace(mtari, "");
                mtip = "PTC";*/

            }
            mtip = mtip.Trim();
            mfol = mfol.Trim();
            mcod = mcod.Trim();
            mtar = mtar.Trim();

            //Inicio de Confirmacion de Etiqueta verde VS Etiqueta Blanca
            EditText etiblan = new EditText(this);
            etiblan.InputType = Android.Text.InputTypes.TextVariationNormal | Android.Text.InputTypes.ClassText;
            etiblan.LongClickable = false;
            etiblan.Hint = "Lectura Etiqueta Blanca";

            #region MATERIAL DIALOG - Confirmacion Etiqueta Blanca
            var ad = new MaterialAlertDialogBuilder(this, Resource.Style.ThemeOverlay_Material3_MaterialAlertDialog);
            ad.SetTitle("Confirmación Etiqueta Blanca");
            ad.SetCancelable(false);
            ad.SetView(etiblan);
            ad.SetPositiveButton(Html.FromHtml("<font face='Comic Sans MS, arial' color='#dc3545' size='10'>VALIDAR</font>"), (senderAlert, args) =>
            {
                string captura = etiblan.Text.Trim();
                int pos = captura.IndexOf("=");

                if (pos == -1)
                {
                    etiblan.Text = "";
                    foliocaptura.Text = "";
                    foliocaptura.RequestFocus();
                    valorfinal = captura;
                    return;
                }
                captura = captura.Substring(pos + 1, captura.Length - (pos + 1)).Trim();
                captura = captura.Replace("=", "");
                int Btam = captura.Length;
                if (Btam > 20)
                {
                    int ValorFolio = Convert.ToInt32(captura.Substring(0, 6));
                    if (ValorFolio > FolioCampo)
                        BEnt = "S";
                }
                if (BEnt == "N")
                {
                    Bcaj = captura.Substring(Btam - 3, 3);
                    Btar = captura.Substring(Btam - 6, 3);
                    int Btam2 = Btam - 6;
                    Btip = "PTP";
                    if (Btam2 == 15)
                    {
                        Bfol = captura.Substring(0, 5);
                        Bcod = captura.Substring(5, Btam - 11);
                        Btip = "PTC";
                    }
                    else if (Btam2 <= 14)
                    {
                        Bfol = captura.Substring(0, 4);
                        Bcod = captura.Substring(4, Btam - 10);
                        Btip = "PTC";
                    }
                    else
                    {
                        Bfol = captura.Substring(0, 6);
                        Bcod = captura.Substring(6, Btam - 12);
                    }

                    var Bnombreproducto = traenom(Bcod);
                    if (Bnombreproducto == "")
                    {
                        Bfol = captura.Substring(0, 6);
                        Bcod = captura.Substring(6, Btam - 12);
                        Btip = "PTP";
                    }

                    Bnombreproducto = traenom(mcod);

                    if (Bnombreproducto == "")
                    {
                        Bcaj = captura.Substring(Btam - 2, 2);
                        Btar = captura.Substring(Btam - 4, 2);
                        Bfol = captura.Substring(0, 6);
                        Bcod = captura.Substring(6, Btam - 10);
                        Btip = "PTC";
                    }
                }
                else
                {
                    Bcaj = captura.Substring(Btam - 3, 3);
                    Btar = captura.Substring(Btam - 7, 2);
                    Bfol = captura.Substring(0, 6);
                    Bcod = captura.Substring(6, Btam - 13);
                    Btip = "PTC";
                }
                Btip = Btip.Trim();
                Bfol = Bfol.Trim();
                Bcod = Bcod.Trim();
                Btar = Btar.Trim();
                Bcaj = Bcaj.Trim();
                if (mfol == Bfol && Btip == mtip && Bcod == mcod && mtar == Btar)
                {
                    ad.Dispose();
                    DataTable Foliosleidos = new DataTable();
                    string CadenaFolios = $"Select Eti_Lectura, fecha_cap From tb_Det_Etiqueta WHERE Eti_Producto = '{mcod}' AND Eti_Recibo = '{mfol}' AND Eti_TarIni = '{mtar}' AND Estatus = 'A'";

                    thisConnection.Open();
                    SqlDataAdapter da = new SqlDataAdapter(CadenaFolios, thisConnection);
                    DataSet ds = new DataSet();
                    da.Fill(ds, "Foliosleidos");


                    Foliosleidos = ds.Tables["Foliosleidos"];
                    thisConnection.Close();

                    DataTable FoliosleidosPresplit = new DataTable();
                    string CadenaFoliospreesplit = "Select Eti_Lectura, fecha_cap From tb_Det_Etiqueta " +
                               "WHERE (Eti_Producto = '" + mcod + "') AND (Eti_Recibo = '" + mfol + "') AND (Eti_TarIni = '" + mtar + "') AND Estatus IN ('A', 'S')";
                    thisConnection.Open();
                    SqlDataAdapter dapre = new SqlDataAdapter(CadenaFoliospreesplit, thisConnection);
                    DataSet dspre = new DataSet();
                    dapre.Fill(dspre, "FoliosleidosPresplit");
                    FoliosleidosPresplit = dspre.Tables["FoliosleidosPresplit"];
                    thisConnection.Close();

                    string cadenatarimacompleta = "";

                    if (mtip == "PTP")
                    {
                        cadenatarimacompleta = "SELECT (num_cajas - CAJAS_SUR) AS DISPONIBLE FROM TB_DET_ETI_FINAL WHERE CVE_PROD = '" + mcod.Trim() + "' AND FOLIO = '" + mfol.Trim() + "' " +
                    "AND TARIMA = '" + Convert.ToInt32(mtar.Trim()).ToString() + "' ";

                    }
                    else
                    {
                        cadenatarimacompleta = "SELECT (etiqueta - surtido) AS DISPONIBLE FROM TB_DET_TRAZABILIDAD WHERE PROD_CLAVE = '" + mcod.Trim() + "' AND RECIBO = '" + mfol.Trim() + "' " +
                         "AND TIPO = '" + mtip + "' AND TARIMA = '" + Convert.ToInt32(mtar.Trim()).ToString() + "' ";
                    }

                    thisConnection.Open();
                    SqlCommand cmd = new SqlCommand(cadenatarimacompleta, thisConnection);
                    int disponible = Convert.ToInt32(cmd.ExecuteScalar());

                    thisConnection.Close();



                    if (disponible > 0)
                    {
                        int total_caja_verde = 0;
                        disponible++;
                        int n = 1;
                        int cajaactual = 1;
                        while (n < disponible)
                        {
                            if (cajaactual.ToString().Length == 1)
                            {
                                mcaj = "00" + cajaactual.ToString();
                            }
                            else if (cajaactual.ToString().Length == 2)
                            {
                                mcaj = "0" + cajaactual.ToString();
                            }
                            else
                            {
                                mcaj = cajaactual.ToString();
                            }

                            mtip = mtip.Trim();
                            mfol = mfol.Trim();
                            mcod = mcod.Trim();
                            mtar = mtar.Trim();
                            mcaj = mcaj.Trim();

                            string lectura = mtip + mfol + mcod + mtar + mcaj;
                            thisConnection.Open();
                            string fechacap = ValidaCajaEtiVerde(lectura, Foliosleidos).Trim();
                            string fechacappre = ValidaCajaPreesplitVerde(lectura, FoliosleidosPresplit).Trim();
                            thisConnection.Close();
                            if (fechacap.Length > 0)
                            {
                                cajaactual++;
                            }
                            else if (fechacappre.Length > 0)
                            {
                                cajaactual++;
                            }
                            else
                            {
                                string cad = mtip + " | " + mfol + " | " + mcod + " | " + mtar + " | " + mcaj;
                                if (repetido(mtip, mfol, mcod, mtar, mcaj) != "S")
                                {
                                    string lectura2 = mtip + mfol + mcod + mtar + mcaj;
                                    lectura2 = lectura2.Trim();

                                    try
                                    {
                                        xprod Pedidoscapturados = new xprod { Tipo = mtip, Folio = mfol, Codigo = mcod, Tarima = mtar, Cajas = mcaj, fecha_captura = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"), tipo_captura = "V", Lecturabd = lectura2 };
                                        db.Insert(Pedidoscapturados);

                                        int totalx = traetotal(mcod);

                                        totalx = totalx + 1;


                                        var pedidos = db.Table<ConPedidos>();

                                        string existeprod = "NO";
                                        foreach (var pedisur in pedidos)
                                        {
                                            if (pedisur.prod_clave.ToString().Trim() == mcod.ToString().Trim())
                                            {
                                                existeprod = "SI";
                                            }
                                        }


                                        if (existeprod == "SI")
                                        {
                                            db.Query<ConPedidos>("UPDATE [ConPedidos] SET surtido = '" + totalx + "' WHERE prod_clave = '" + mcod.ToString() + "'");
                                        }
                                        else
                                        {
                                            ConPedidos ConsecutivosPedidos = new ConPedidos { prod_clave = mcod.ToString(), nombre = traenom(mcod.ToString().Trim()), pedido = 0, surtido = Convert.ToInt16(totalx) };
                                            db.Insert(ConsecutivosPedidos);
                                        }

                                        cajaactual++;
                                        total_caja_verde++;
                                        TotCaj++;
                                        total.Text = TotCaj.ToString("##0");
                                        listItem.Add(new FlimStarInfo()
                                        {
                                            Name = traenom(mcod.ToString().Trim()),
                                            Age = "Recibo: " + mfol + "Tarima: " + mtar + " Caja: " + mcaj,
                                            ImageID = Resource.Drawable.producto
                                        });
                                    }
                                    catch
                                    {
                                        Toast.MakeText(this, "Duplicidad Evitada", ToastLength.Short).Show();
                                    }
                                }

                                n++;
                            }

                        }
                        foliocaptura.SetSelection(0, foliocaptura.Text.Length);
                        foliocaptura.RequestFocus();
                        valorfinal = foliocaptura.Text;
                        DialogHelper.ShowInfoDialog(this,
                            title: "LECTURA POR TARIMA",
                            message: $"Se han capturado {total_caja_verde} cajas del folio {mfol}, de la tarima {mtar}, del producto {traenom(mcod.ToString().Trim())}.",
                            positiveText: "Ok",
                            iconRes: Resource.Drawable.nota);
                        //iMPRESION DE MENSAJE QUE INDICARA CUANTO DE CADA TARIMA SE LOGRO CARGAR Y SIMULAR
                        /*var alertDialog = new Android.App.AlertDialog.Builder(this);
                        alertDialog.SetTitle(Html.FromHtml("<font color='#55F721' size=10>LECTURA POR TARIMA</font>"));
                        alertDialog.SetIcon(Resource.Drawable.nota);
                        alertDialog.SetMessage(Html.FromHtml("<font color='#9FFA7A' size=10>Se han capturado " + total_caja_verde +
                            " cajas del folio " + mfol + ", de la tarima " + mtar +
                            ", del producto " + traenom(mcod.ToString().Trim()) + ".</font>"));
                        alertDialog.SetCancelable(false);
                        alertDialog.SetNeutralButton("Ok", (s, e) =>
                        {
                            alertDialog.Dispose();
                        });
                        alertDialog.Show();*/
                    }
                    else
                    {
                        /*var alertDialog = new Android.App.AlertDialog.Builder(this);
                        alertDialog.SetTitle(Html.FromHtml("<font color='#55F721' size=10>EXISTENCIA NO DISPONIBLE</font>"));
                        alertDialog.SetIcon(Resource.Drawable.nota);
                        alertDialog.SetMessage(Html.FromHtml("<font color='#9FFA7A' size=10>La tarima actual no cuenta con existencia disponible. Favor de depurar los folios correspondientes y volver a leer.</font>"));
                        alertDialog.SetCancelable(false);
                        alertDialog.SetNeutralButton("Ok", (s, e) =>
                        {
                            alertDialog.Dispose();
                        });
                        alertDialog.Show();*/
                        DialogHelper.ShowWarningDialog(this,
                            message: "La tarima actual no cuenta con existencia disponible. Favor de depurar los folios correspondientes y volver a leer.",
                            positiveText: "Ok");
                        foliocaptura.SetSelection(0, foliocaptura.Text.Length);
                        foliocaptura.RequestFocus();
                        valorfinal = foliocaptura.Text;
                    }

                    List<FlimStarInfo> lstFlimStar = listItem;
                    var gvObject = FindViewById<GridView>(Resource.Id.gvCtr2);
                    gvObject.Adapter = new myGVItemAdapter(this, lstFlimStar);
                    return;
                }
                else
                {
                    ad.Dispose();
                    var alertDialog = new Android.App.AlertDialog.Builder(this);
                    alertDialog.SetTitle(Html.FromHtml("<font color='#dc3545' size=10>ETIQUETA VERDE NO CORRESPONDE A TARIMA</font>"));
                    alertDialog.SetIcon(Resource.Drawable.no);
                    alertDialog.SetMessage(Html.FromHtml("<font color='#FFFFFF' size=10>Los datos de la etiqueta verde no corresponden a los datos de la etiqueta blanca. Verifique la información e informe a un supervisor.</font>"));
                    alertDialog.SetCancelable(false);
                    alertDialog.SetNeutralButton("Ok", (s, e) =>
                    {
                        alertDialog.Dispose();
                        foliocaptura.SetSelection(0, foliocaptura.Text.Length);
                        foliocaptura.RequestFocus();
                        valorfinal = foliocaptura.Text;
                    });
                    alertDialog.Show();
                }
            });
            ad.SetNegativeButton(Html.FromHtml("<font face='Comic Sans MS, arial' color='#dc3545' size='10'>Cancelar</font>"), (senderAlert, args) =>
            {
                etiblan.Text = "";
                foliocaptura.Text = "";
                foliocaptura.RequestFocus();
                ad.Dispose();
                return;
            });
            ad.Show();
            #endregion

            #region ALERT DIALOG
            /*AndroidX.AppCompat.App.AlertDialog.Builder ad = new AndroidX.AppCompat.App.AlertDialog.Builder(this);
            ad.SetTitle("Confirmacion Etiqueta Blanca");
            ad.SetCancelable(false);
            ad.SetView(etiblan);
            ad.SetPositiveButton(Html.FromHtml("<font face = 'Comic Sans MS, arial' color='#dc3545' size = '10'>VALIDAR</font>"), (senderAlert, args) =>
            {
                string captura = etiblan.Text.Trim();
                int pos = captura.Trim().IndexOf("=");
                //MessageBox.Show(pos.ToString()); 
                if (pos == -1)
                {
                    etiblan.Text = "";
                    foliocaptura.Text = "";
                    foliocaptura.RequestFocus();
                    valorfinal = captura;
                    return;
                }
                captura = captura.Substring(pos + 1, captura.Length - (pos + 1)).Trim();
                captura = captura.Replace("=", "");
                int Btam = captura.Length;
                if (Btam > 20) //Etiqueta de Campo que no es Aguilares y Proceso Planta
                {
                    Int32 ValorFolio = Convert.ToInt32(captura.Substring(0, 6));
                    if (ValorFolio > FolioCampo) // Etiqueta de Campo
                        BEnt = "S";
                }
                if (BEnt == "N") // Valido si el PTP Planta o PTC de Aguilares
                {
                    Bcaj = captura.Substring(Btam - 3, 3);
                    Btar = captura.Substring(Btam - 6, 3);
                    int Btam2 = Btam - 6;
                    Btip = "PTP";
                    if (Btam2 == 15) // Etiqueta de Aguilares	
                    {
                        Bfol = captura.Substring(0, 5);
                        Bcod = captura.Substring(5, Btam - 11);
                        Btip = "PTC";
                    }
                    else if (Btam2 <= 14) // Etiqueta de Aguilares	
                    {
                        Bfol = captura.Substring(0, 4);
                        Bcod = captura.Substring(4, Btam - 10);
                        Btip = "PTC";
                    }
                    else
                    {
                        Bfol = captura.Substring(0, 6);
                        Bcod = captura.Substring(6, Btam - 12);
                    }
                    var Bnombreproducto = traenom(Bcod); //Valido si existe el producto, si no quiere decir que es recibo de 6 digitos pero de produccion


                    if (Bnombreproducto == "")
                    {
                        Bfol = captura.Substring(0, 6);
                        Bcod = captura.Substring(6, Btam - 12);
                        Btip = "PTP";
                    }

                    Bnombreproducto = traenom(mcod); //Valido si existe el producto, si no quiere decir que es recibo de 6 digitos

                    if (Bnombreproducto == "")
                    {
                        Bcaj = captura.Substring(Btam - 2, 2);
                        Btar = captura.Substring(Btam - 4, 2);
                        Bfol = captura.Substring(0, 6);
                        Bcod = captura.Substring(6, Btam - 10);
                        Btip = "PTC";
                    }
                }
                else //Etiqueta de Campo que no es Aguilares
                {
                    Bcaj = captura.Substring(Btam - 3, 3);
                    Btar = captura.Substring(Btam - 7, 2);
                    Bfol = captura.Substring(0, 6);
                    Bcod = captura.Substring(6, Btam - 13);
                    Btip = "PTC";
                }
                Btip = Btip.Trim();
                Bfol = Bfol.Trim();
                Bcod = Bcod.Trim();
                Btar = Btar.Trim();
                Bcaj = Bcaj.Trim();
                if (mfol == Bfol && Btip == mtip && Bcod == mcod && mtar == Btar)
                {
                    ad.Dispose();
                    DataTable Foliosleidos = new DataTable();
                    string CadenaFolios = "Select Eti_Lectura, fecha_cap From tb_Det_Etiqueta " +
                                   "WHERE (Eti_Producto = '" + mcod + "') AND (Eti_Recibo = '" + mfol + "') AND (Eti_TarIni = '" + mtar + "') AND Estatus = 'A'";
                    thisConnection.Open();
                    SqlDataAdapter da = new SqlDataAdapter(CadenaFolios, thisConnection);
                    DataSet ds = new DataSet();
                    da.Fill(ds, "Foliosleidos");


                    Foliosleidos = ds.Tables["Foliosleidos"];
                    thisConnection.Close();

                    DataTable FoliosleidosPresplit = new DataTable();
                    string CadenaFoliospreesplit = "Select Eti_Lectura, fecha_cap From tb_Det_Etiqueta " +
                                   "WHERE (Eti_Producto = '" + mcod + "') AND (Eti_Recibo = '" + mfol + "') AND (Eti_TarIni = '" + mtar + "') AND Estatus IN ('A', 'S')";
                    thisConnection.Open();
                    SqlDataAdapter dapre = new SqlDataAdapter(CadenaFoliospreesplit, thisConnection);
                    DataSet dspre = new DataSet();
                    dapre.Fill(dspre, "FoliosleidosPresplit");
                    FoliosleidosPresplit = dspre.Tables["FoliosleidosPresplit"];
                    thisConnection.Close();

                    string cadenatarimacompleta = "";

                    if (mtip == "PTP")
                    {
                        cadenatarimacompleta = "SELECT (num_cajas - CAJAS_SUR) AS DISPONIBLE FROM TB_DET_ETI_FINAL WHERE CVE_PROD = '" + mcod.Trim() + "' AND FOLIO = '" + mfol.Trim() + "' " +
                    "AND TARIMA = '" + Convert.ToInt32(mtar.Trim()).ToString() + "' ";

                    }
                    else
                    {
                        cadenatarimacompleta = "SELECT (etiqueta - surtido) AS DISPONIBLE FROM TB_DET_TRAZABILIDAD WHERE PROD_CLAVE = '" + mcod.Trim() + "' AND RECIBO = '" + mfol.Trim() + "' " +
                         "AND TIPO = '" + mtip + "' AND TARIMA = '" + Convert.ToInt32(mtar.Trim()).ToString() + "' ";
                    }

                    thisConnection.Open();
                    SqlCommand cmd = new SqlCommand(cadenatarimacompleta, thisConnection);
                    int disponible = Convert.ToInt32(cmd.ExecuteScalar());

                    thisConnection.Close();



                    if (disponible > 0)
                    {
                        int total_caja_verde = 0;
                        disponible++;
                        int n = 1;
                        int cajaactual = 1;
                        while (n < disponible)
                        {
                            if (cajaactual.ToString().Length == 1)
                            {
                                mcaj = "00" + cajaactual.ToString();
                            }
                            else if (cajaactual.ToString().Length == 2)
                            {
                                mcaj = "0" + cajaactual.ToString();
                            }
                            else
                            {
                                mcaj = cajaactual.ToString();
                            }

                            mtip = mtip.Trim();
                            mfol = mfol.Trim();
                            mcod = mcod.Trim();
                            mtar = mtar.Trim();
                            mcaj = mcaj.Trim();

                            string lectura = mtip + mfol + mcod + mtar + mcaj;
                            thisConnection.Open();
                            string fechacap = ValidaCajaEtiVerde(lectura, Foliosleidos).Trim();
                            string fechacappre = ValidaCajaPreesplitVerde(lectura, FoliosleidosPresplit).Trim();
                            thisConnection.Close();
                            if (fechacap.Length > 0)
                            {
                                cajaactual++;
                            }
                            else if (fechacappre.Length > 0)
                            {
                                cajaactual++;
                            }
                            else
                            {
                                string cad = mtip + " | " + mfol + " | " + mcod + " | " + mtar + " | " + mcaj;
                                if (repetido(mtip, mfol, mcod, mtar, mcaj) != "S")
                                {
                                    string lectura2 = mtip + mfol + mcod + mtar + mcaj;
                                    lectura2 = lectura2.Trim();

                                    try
                                    {
                                        xprod Pedidoscapturados = new xprod { Tipo = mtip, Folio = mfol, Codigo = mcod, Tarima = mtar, Cajas = mcaj, fecha_captura = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"), tipo_captura = "V", Lecturabd = lectura2 };
                                        db.Insert(Pedidoscapturados);

                                        int totalx = traetotal(mcod);

                                        totalx = totalx + 1;


                                        var pedidos = db.Table<ConPedidos>();

                                        string existeprod = "NO";
                                        foreach (var pedisur in pedidos)
                                        {
                                            if (pedisur.prod_clave.ToString().Trim() == mcod.ToString().Trim())
                                            {
                                                existeprod = "SI";
                                            }
                                        }


                                        if (existeprod == "SI")
                                        {
                                            db.Query<ConPedidos>("UPDATE [ConPedidos] SET surtido = '" + totalx + "' WHERE prod_clave = '" + mcod.ToString() + "'");
                                        }
                                        else
                                        {
                                            ConPedidos ConsecutivosPedidos = new ConPedidos { prod_clave = mcod.ToString(), nombre = traenom(mcod.ToString().Trim()), pedido = 0, surtido = Convert.ToInt16(totalx) };
                                            db.Insert(ConsecutivosPedidos);
                                        }

                                        cajaactual++;
                                        total_caja_verde++;
                                        TotCaj++;
                                        total.Text = TotCaj.ToString("##0");
                                        listItem.Add(new FlimStarInfo()
                                        {
                                            Name = traenom(mcod.ToString().Trim()),
                                            Age = "Recibo: " + mfol + "Tarima: " + mtar + " Caja: " + mcaj,
                                            ImageID = Resource.Drawable.producto
                                        });
                                    }
                                    catch
                                    {
                                        Toast.MakeText(this, "Duplicidad Evitada", ToastLength.Short).Show();
                                    }
                                }

                                n++;
                            }

                        }
                        foliocaptura.SetSelection(0, foliocaptura.Text.Length);
                        foliocaptura.RequestFocus();
                        valorfinal = foliocaptura.Text;
                        //iMPRESION DE MENSAJE QUE INDICARA CUANTO DE CADA TARIMA SE LOGRO CARGAR Y SIMULAR
                        Android.App.AlertDialog.Builder alertDialog = new Android.App.AlertDialog.Builder(this);
                        alertDialog.SetTitle(Html.FromHtml("<font color='#55F721' size = 10>LECTURA POR TARIMA</font>"));
                        alertDialog.SetIcon(Resource.Drawable.nota);
                        alertDialog.SetMessage(Html.FromHtml("<font color='#9FFA7A' size = 10>Se han Capturado " + total_caja_verde + " Cajas,  Del Folio " + mfol + " De la tarima " + mtar + " Del Producto " + traenom(mcod.ToString().Trim()) + "</font>"));
                        alertDialog.SetNeutralButton("Ok", delegate
                        {
                            alertDialog.Dispose();
                        });
                        alertDialog.Show();

                    }
                    else
                    {
                        Android.App.AlertDialog.Builder alertDialog = new Android.App.AlertDialog.Builder(this);
                        alertDialog.SetTitle(Html.FromHtml("<font color='#55F721' size = 10>EXISTENCIA NO DISPONIBLE</font>"));
                        alertDialog.SetIcon(Resource.Drawable.nota);
                        alertDialog.SetMessage(Html.FromHtml("<font color='#9FFA7A' size = 10>La Tarima Actual No Cuenta con Existencia Disponible, Favor de Depurar los folios correspondientes y volver a leer</font>"));
                        alertDialog.SetNeutralButton("Ok", delegate
                        {
                            alertDialog.Dispose();
                        });
                        alertDialog.Show();
                        foliocaptura.SetSelection(0, foliocaptura.Text.Length);
                        foliocaptura.RequestFocus();
                        valorfinal = foliocaptura.Text;
                    }

                    List<FlimStarInfo> lstFlimStar = listItem;
                    var gvObject = FindViewById<GridView>(Resource.Id.gvCtr2);
                    gvObject.Adapter = new myGVItemAdapter(this, lstFlimStar);
                    return;
                }
                else
                {
                    ad.Dispose();
                    Android.App.AlertDialog.Builder alertDialog = new Android.App.AlertDialog.Builder(this);
                    alertDialog.SetTitle(Html.FromHtml("<font color='#dc3545' size = 10>ETIQUETA VERDE NO CORRESPONDE A TARIMA</font>"));
                    alertDialog.SetIcon(Resource.Drawable.no);
                    alertDialog.SetMessage(Html.FromHtml("<font color='#FFFFFF' size = 10>Los Datos de La etiqueta Verde no Corresponden a los Datos de la Etiqueta Blanca, Valide la Informacion e informe a un Supervisor</font>"));
                    alertDialog.SetCancelable(false);
                    alertDialog.SetNeutralButton("Ok", delegate
                    {
                        alertDialog.Dispose();
                        foliocaptura.SetSelection(0, foliocaptura.Text.Length);
                        foliocaptura.RequestFocus();
                        valorfinal = foliocaptura.Text;
                    });
                    alertDialog.Show();
                }
            });
            ad.SetNegativeButton(Html.FromHtml("<font face = 'Comic Sans MS, arial' color='#dc3545' size = '10'>Cancelar</font>"), (senderAlert, args) =>
            {
                etiblan.Text = "";
                foliocaptura.Text = "";
                foliocaptura.RequestFocus();
                ad.Dispose();
                return;
            });
            ad.Show();*/
            #endregion
            //TERMINO de Confirmacion de Etiqueta verde VS Etiqueta Blanca
        }

        public void AfterTextChanged(IEditable s)
        {
            //if (mconcen == "2")
            //{
            //    Android.App.AlertDialog.Builder alertDialog = new Android.App.AlertDialog.Builder(this);
            //    alertDialog.SetTitle(Html.FromHtml("<font color='#dc3545' size = 10>Modo concentrado Activado</font>"));
            //    alertDialog.SetIcon(Resource.Drawable.no);
            //    alertDialog.SetMessage(Html.FromHtml("<font color='#FFFFFF' size = 10>Esta consultando el concentrado no se puede capturar codigo. </font>"));
            //    alertDialog.SetCancelable(false);
            //    alertDialog.SetNeutralButton("Ok", delegate
            //    {
            //        alertDialog.Dispose();
            //        foliocaptura.SetSelection(0, foliocaptura.Text.Length);
            //        foliocaptura.RequestFocus();
            //        valorfinal = foliocaptura.Text;
            //    });
            //    alertDialog.Show();
            //    return;


            //} // esta consultando el concentrado no se puede capturar codigo

            //string folio = foliocaptura.Text;
            //Guardar.Enabled = false;

            //if (folio != valorfinal && folio != "")
            //{
            //    //var TxtCod = (EditText)sender;
            //    if (Eliminar_caja.Checked == true)
            //    {
            //        eliminaretiquetablanca();

            //    }
            //    else
            //    {

            //        if (etiblanca.Checked == true)
            //        {
            //            etiquetablanca();
            //        }
            //        else
            //        {
            //            etiquetaverde();

            //        }

            //    }

            //}
            //else
            //{
            //    foliocaptura.SetSelection(0, foliocaptura.Text.Length);
            //    foliocaptura.RequestFocus();
            //    valorfinal = foliocaptura.Text;
            //}
        }

        #region ACTUALIZACION COMPATIBILIDAD CON TABLETS
        public void Foliocaptura_KeyPress(object sender, View.KeyEventArgs e)
        {
            // Detectar cuando se presiona la tecla Enter
            if (e.Event.Action == KeyEventActions.Down && e.KeyCode == Keycode.Enter)
            {
                if (mconcen == "2")
                {
                    RunOnUiThread(() =>
                    {
                        DialogHelper.ShowWarningDialog(this,
                            message: "Está consultando el modo concentrado. No es posible capturar códigos en este modo.",
                            positiveText: "Entendido",
                            positiveAction: (s, e) =>
                            {
                                foliocaptura.SetSelection(0, foliocaptura.Text.Length);
                                foliocaptura.RequestFocus();
                                valorfinal = foliocaptura.Text;
                            });
                    });
                    #region MATERIAL DIALOG - MODO CONCENTRADO ACTIVADO
                    /*// --- Construcción del título con color y negritas ---
                    var titleSpannable = new SpannableStringBuilder("Modo Concentrado Activado");
                    titleSpannable.SetSpan(new ForegroundColorSpan(Color.ParseColor("#DC3545")), 0, titleSpannable.Length(), SpanTypes.ExclusiveExclusive);
                    titleSpannable.SetSpan(new StyleSpan(TypefaceStyle.Bold), 0, titleSpannable.Length(), SpanTypes.ExclusiveExclusive);

                    // --- Construcción del mensaje ---
                    var mensajeSpannable = new SpannableStringBuilder();
                    mensajeSpannable.Append("Está consultando el ");
                    int startModo = mensajeSpannable.Length();
                    mensajeSpannable.Append("modo concentrado");
                    mensajeSpannable.SetSpan(new StyleSpan(TypefaceStyle.Bold), startModo, mensajeSpannable.Length(), SpanTypes.ExclusiveExclusive);
                    mensajeSpannable.Append(".\n\n");
                    mensajeSpannable.Append("No es posible capturar códigos en este modo.");
                    mensajeSpannable.SetSpan(new ForegroundColorSpan(Color.ParseColor("#5F6368")), 0, mensajeSpannable.Length(), SpanTypes.ExclusiveExclusive);

                    // --- Creación del diálogo Material ---
                    var builder = new MaterialAlertDialogBuilder(this, Resource.Style.ThemeOverlay_Material3_MaterialAlertDialog);
                    builder.SetTitle(titleSpannable);
                    builder.SetIcon(Resource.Drawable.no);
                    builder.SetMessage(mensajeSpannable);
                    builder.SetCancelable(false);

                    // --- Botón principal ---
                    builder.SetPositiveButton("Entendido", (s, e) =>
                    {
                        foliocaptura.SetSelection(0, foliocaptura.Text.Length);
                        foliocaptura.RequestFocus();
                        valorfinal = foliocaptura.Text;
                    });

                    // --- Mostrar el diálogo ---
                    var dialog = builder.Create();
                    dialog.Show();

                    // --- Personalización del botón tras mostrar el diálogo ---
                    dialog.Window.DecorView.Post(() =>
                    {
                        var positiveButton = dialog.GetButton((int)DialogButtonType.Positive);
                        positiveButton?.SetTextColor(Color.ParseColor("#00695C")); // Verde Material 600
                        positiveButton?.SetAllCaps(false);
                    });*/
                    #endregion

                }

                string folio = foliocaptura.Text;
                Guardar.Enabled = false;

                if (folio != valorfinal && folio != "")
                {
                    if (Eliminar_caja.Checked)
                    {
                        eliminaretiquetablanca();
                    }
                    else
                    {
                        if (etiblanca.Checked)
                            etiquetablanca();
                        else
                            etiquetaverde();
                    }
                }
                else
                {
                    foliocaptura.SetSelection(0, foliocaptura.Text.Length);
                    foliocaptura.RequestFocus();
                    valorfinal = foliocaptura.Text;
                }

                // Devolver true para indicar que el evento se manejó

            }
            else
            {
                e.Handled = false;
            }

            // Si no fue Enter, no hacemos nada especial

        }

        #endregion

        public void BeforeTextChanged(ICharSequence s, int start, int count, int after)
        {

        }

        private string ValidaCajaEtiVerde(string cadena, DataTable foliosleidos)
        {
            /*string Cadena = "Select fecha_cap From tb_Det_Etiqueta " +
                           "Where Eti_Lectura = '" + cadena + "' AND Estatus != 'C'";
            SqlCommand cmd = new SqlCommand(Cadena, thisConnection);*/
            string Valor = "";

            DataRow[] datos = foliosleidos.Select("Eti_Lectura = '" + cadena + "'");

            if (datos.Length > 0)
            {
                Valor = datos[0].ItemArray[1].ToString();
            }

            return Valor;

        }

        private string ValidaCajaPreesplitVerde(string cadena, DataTable foliosleidos)
        {
            /*string Cadena = "Select fecha_cap From tb_Det_Etiqueta " +
                           "Where Eti_Lectura = '" + cadena + "' AND Estatus != 'C'";
            SqlCommand cmd = new SqlCommand(Cadena, thisConnection);*/
            string Valor = "";

            DataRow[] datos = foliosleidos.Select("Eti_Lectura = '" + cadena + "'");

            if (datos.Length > 0)
            {
                Valor = datos[0].ItemArray[1].ToString();
            }

            return Valor;

        }


        //Validacion Modificada de Fecha de Caduciad y Folio Sugerido
        private string validafecadMod()
        {
            dondegenera = "validafecMod";
            string Valor = "";
            ValiFechacad = "S";
            //Obtener los productos con su tipo de lo que se ha leido******************************************************************
            var productoscapturados = db.Query<xLote>("Select Tipo, Codigo, nombre FROM xLote GROUP BY Tipo, Codigo, nombre");
            db.Query<XLoteSug>("delete from[XLoteSug]");

            var allItems = db.Table<xLote>().ToList();
            int count = allItems.Count;
            int[] validados = new int[count + 1];
            int capturas = 0;
            foreach (var captu in productoscapturados)
            {
                int totalpro = 0;
                int totaldisponibles = 0;
                int totalusadas;
                int simulador = 0;
                int totaldis = 0;
                string fechaant = "";
                int totaldisreal = 0;
                int totalprodsimulado = 0;

                //traer el total de recibos vencidos para que no entren en la condicion
                var prodcapx = db.Query<xLote>("Select COUNT(ID) AS Cajas FROM xLote Where Codigo = '" + captu.Codigo.Trim() + "'");

                foreach (var capturadox in prodcapx)
                {
                    totalpro = Convert.ToInt32(capturadox.Cajas.ToString().Trim());

                }

                int resttotal = traerecibosvencidos(captu.Codigo.Trim(), captu.Tipo.Trim());

                totalpro = totalpro - resttotal;
                totalprodsimulado = totalpro;

                //Obtener los diferentes folios disponibles dependiendo el codigo y el tipo
                string todobien = "OK";
                int prod_cap = 0;
                int usadas = 0;
                int existefecant = 0;
                string cadena = "";
                string tipo = captu.Tipo.Trim();
                string prod = captu.Codigo.Trim();

                string diacadant = "";
                string mescadant = "";
                //traer nombre de producto para validar cuantos dias debo aumentar.
                string prodnom = captu.nombre.Trim();
                int diascad = 14;
                if (prodnom.Contains("BETABEL"))
                {
                    diascad = 60;
                }
                else if (prodnom.Contains("AJO"))
                {
                    diascad = 180;
                }
                else if (prodnom.Contains("ADEREZO") || prodnom.Contains("VINAGRETA") || prodnom.Contains("QUESO"))
                {
                    diascad = 90;
                }


                if (tipo == "PTC")
                {
                    cadena = "SELECT  (etiqueta - surtido) AS disponible, (CASE fecha_cad WHEN '' THEN  FORMAT( DATEADD(day, " + diascad + ", pti_fecha), 'dd/MM/yyyy', 'en-US' ) WHEN fecha_cad THEN fecha_cad END) AS fecha_cad, (CASE fecha_cad WHEN '' THEN  FORMAT( DATEADD(day, " + diascad + ", pti_fecha), 'yyyyMMdd', 'en-US' ) WHEN fecha_cad THEN FORMAT(convert(datetime,fecha_cad), 'yyyyMMdd', 'en-US' ) END) AS fecha_cadu, recibo, tarima FROM TB_DET_TRAZABILIDAD Inner JOIN tb_mstr_recepcion_pt ON rpt_recibo = recibo WHERE PROD_CLAVE = '" + prod + "' AND pti_estatus_sur = '' AND tipo = 'PTC' AND (rpt_tipo != 'TR' OR (rpt_tipo != 'TR' AND rpt_inventario = 'S')) AND rpt_estatus = '' AND  (etiqueta - surtido) > 0 Order By fecha_cadu";
                }
                else
                {
                    cadena = "SELECT (num_cajas - cajas_sur) AS disponible, ISNULL(NULLIF(fechacad,' '), FORMAT( DATEADD(day, " + diascad + ", fecha), 'yyyyMMdd', 'en-US' )) AS fecha_cad, folio AS recibo, tarima FROM tb_det_eti_final Inner JOIN tb_mstr_ordenes_prod ON folio = ordp_folio WHERE cve_prod = '" + prod + "' AND estatus_sur != 'S' AND ordp_estatus != 'C' AND etiqueta = 'S' AND (num_cajas - cajas_sur) > 0 Order By fecha_cad";
                }

                SqlDataAdapter da = new SqlDataAdapter(cadena, thisConnection);
                DataSet ds = new DataSet();
                da.Fill(ds, "xlotes");
                DataTable xlote = ds.Tables["xlotes"];
                //Recorrido de cada uno de los folios y la validacion correspondiente hacia lo que tengo capturado************************

                string foliosAnt = "";

                foreach (DataRow row in xlote.Rows)
                {
                    int total_prod_simula = totalprodsimulado;
                    string Cadena = "Select Count(fecha) AS Total From Tb_Det_Etiqueta_Presplit " +
                                    "Where Eti_Recibo = '" + row["recibo"].ToString().Trim() + "' AND Eti_Producto = '" + captu.Codigo.Trim() + "' AND Eti_TarIni = '" + Convert.ToInt32(row["tarima"].ToString().Trim()) + "' AND Estatus = 'A'";

                    thisConnection.Open();
                    SqlCommand cmd = new SqlCommand(Cadena, thisConnection);
                    int TotalLeido = Convert.ToInt32(cmd.ExecuteScalar());
                    int usadasant = 0;
                    thisConnection.Close();

                    row["disponible"] = Convert.ToInt32(row["disponible"].ToString().Trim()) - TotalLeido;

                    if (Convert.ToInt32(row["disponible"]) > 0)
                    {
                        if (totalpro > 0)
                        {

                            string diacad = traediafecadrec(row["fecha_cad"].ToString().Trim(), tipo);
                            string mescad = traemesfecadrec(row["fecha_cad"].ToString().Trim(), tipo);



                            var prodcap = db.Query<xLote>("Select COUNT(ID) AS Cajas FROM xLote Where Codigo = '" + captu.Codigo.Trim() + "' AND Folio = '" + row["recibo"].ToString().Trim() + "'  AND CAST(Tarima as int) = '" + Convert.ToInt32(row["tarima"].ToString().Trim()) + "'");

                            foreach (var capturado in prodcap)
                            {

                                usadas = Convert.ToInt32(capturado.Cajas.ToString().Trim());
                                usadasant = usadas;
                                totaldis = Convert.ToInt32(row["disponible"].ToString().Trim()) - Convert.ToInt32(capturado.Cajas.ToString().Trim());
                                simulador = simulador + totaldis;
                                totalpro = totalpro - usadas;
                                totaldisponibles = totaldisponibles + totaldis;
                                totaldisreal = totaldis;
                            }

                            if (totaldis > 0 && totalpro > 0)
                            {
                                var prodcapfecad = db.Query<xLote>("Select COUNT(ID) AS Cajas FROM xLote Where Codigo = '" + captu.Codigo.Trim() + "' AND diacad = '" + diacad + "'AND mescad = '" + mescad + "'");

                                foreach (var capturado in prodcapfecad)
                                {
                                    usadas = Convert.ToInt32(capturado.Cajas.ToString().Trim()) - usadasant;
                                    totaldis = Convert.ToInt32(totaldis) - Convert.ToInt32(capturado.Cajas.ToString().Trim());
                                    simulador = simulador + totaldis;
                                    totalpro = totalpro - usadas;
                                    totaldisponibles = totaldisponibles + totaldis;
                                }
                            }

                            if (totaldis > 0)
                            {
                                if (totalpro > 0)
                                {
                                    XLoteSug sugeridos = new XLoteSug { recibosug = row["recibo"].ToString().Trim(), fecrecsug = diacad + "/" + mescad, cveprod = prod, Tarima = row["tarima"].ToString().Trim(), Cajasdis = totaldisreal, Cajasusadas = usadas, foliomens = "" };
                                    db.Insert(sugeridos);

                                    break;

                                }

                            }

                            //diacadant = diacad;
                            //mescadant = mescad;



                        }
                        else
                        {
                            break;
                        }
                    }

                }

                var loteSug = db.Query<XLoteSug>("Select  *  FROM XLoteSug Where cveprod = '" + captu.Codigo.Trim() + "' AND cajasdis != 0 LIMIT 1");
                foreach (var capturado in loteSug)
                {
                    string recibosug = capturado.recibosug;
                    string fecrecsug = capturado.fecrecsug;
                    string cveprod = capturado.cveprod;
                    string tarima = capturado.Tarima;
                    int cajasdis = capturado.Cajasdis;
                    int cajasusadas = capturado.Cajasusadas;
                    Mensajes mensa = new Mensajes { titulo = "Existe un folio anterior disponible", mensaje = "El recibo " + "\n\r" + capturado.recibosug.ToString().Trim() + " De la tarima  " + capturado.Tarima.Trim() + " Tiene  " + capturado.Cajasdis + " cajas disponibles del producto: " + captu.nombre.Trim() + " Con Fecha de Caducidad del" + capturado.fecrecsug + ", Favor de Buscar a personal de Camaras Frias para la autorizacion" };
                    db.Insert(mensa);
                    ValiFechacad = "N";

                    XLoteSug sugeridosact = new XLoteSug { recibosug = recibosug.ToString().Trim(), fecrecsug = fecrecsug, cveprod = cveprod, Tarima = tarima.ToString().Trim(), Cajasdis = cajasdis, Cajasusadas = cajasusadas, foliomens = "S" };
                    db.Insert(sugeridosact);
                    totalpro = 0;
                }


            }


            return Valor;


        }

        private string validafecadMAXIMOSdescontinuado() //version antes del 01/09/2022
        {
            //***************************************INSTRUCCION QUE VALIDA QUE LAS FECHAS DE CADUCIDAD ACTUALES NO SE PASEN DE LOS DIAS DE CARGA DE LA ORDEN**************************************************
            dondegenera = "validafecMod";
            string Valor = "";
            ValiFechacad = "S";
            ValiMinFechaPTC = "S";
            //Obtener los productos con su tipo de lo que se ha leido******************************************************************
            var productoscapturados = db.Query<xLote>("Select Tipo, Codigo, nombre FROM xLote GROUP BY Tipo, Codigo, nombre");
            db.Query<XLoteSug>("delete from[XLoteSug]");

            var allItems = db.Table<xLote>().ToList();
            int count = allItems.Count;
            int[] validados = new int[count + 1];
            int capturas = 0;
            foreach (var captu in productoscapturados)
            {
                int totalpro = 0;
                int totaldisponibles = 0;
                int totalusadas;
                int simulador = 0;
                int totaldis = 0;
                string fechaant = "";
                int totaldisreal = 0;
                int totalprodsimulado = 0;

                //traer el total de recibos vencidos para que no entren en la condicion
                var prodcapx = db.Query<xLote>("Select COUNT(ID) AS Cajas FROM xLote Where Codigo = '" + captu.Codigo.Trim() + "'");

                foreach (var capturadox in prodcapx)
                {
                    totalpro = Convert.ToInt32(capturadox.Cajas.ToString().Trim());

                }

                int resttotal = traerecibosvencidos(captu.Codigo.Trim(), captu.Tipo.Trim());

                totalpro = totalpro - resttotal;
                totalprodsimulado = totalpro;

                //Obtener los diferentes folios disponibles dependiendo el codigo y el tipo
                string todobien = "OK";
                int prod_cap = 0;
                int usadas = 0;
                int existefecant = 0;
                string cadena = "";
                string tipo = captu.Tipo.Trim();
                string prod = captu.Codigo.Trim();

                string diacadant = "";
                string mescadant = "";
                //traer nombre de producto para validar cuantos dias debo aumentar.
                string prodnom = captu.nombre.Trim();
                int diascad = 14;
                if (prodnom.Contains("BETABEL"))
                {
                    diascad = 60;
                }
                else if (prodnom.Contains("AJO"))
                {
                    diascad = 180;
                }
                else if (prodnom.Contains("ADEREZO") || prodnom.Contains("VINAGRETA") || prodnom.Contains("QUESO"))
                {
                    diascad = 90;
                }


                if (tipo == "PTC")
                {
                    cadena = "SELECT  (etiqueta - surtido) AS disponible, (CASE fecha_cad WHEN '' THEN  FORMAT( DATEADD(day, " + diascad + ", pti_fecha), 'dd/MM/yyyy', 'en-US' ) WHEN fecha_cad THEN fecha_cad END) AS fecha_cad, (CASE fecha_cad WHEN '' THEN  FORMAT( DATEADD(day, " + diascad + ", pti_fecha), 'yyyyMMdd', 'en-US' ) WHEN fecha_cad THEN FORMAT(convert(datetime,fecha_cad), 'yyyyMMdd', 'en-US' ) END) AS fecha_cadu, recibo, tarima, DATEDIFF(day, GETDATE(), (CASE fecha_cad WHEN '' THEN  FORMAT( DATEADD(day, " + diascad + ", pti_fecha), 'yyyyMMdd', 'en-US' ) WHEN fecha_cad THEN FORMAT(convert(datetime,fecha_cad), 'yyyyMMdd', 'en-US' ) END)) AS diasdisp, preautorizado  FROM TB_DET_TRAZABILIDAD Inner JOIN tb_mstr_recepcion_pt ON rpt_recibo = recibo WHERE PROD_CLAVE = '" + prod + "' AND pti_estatus_sur = '' AND tipo = 'PTC' AND (rpt_tipo != 'TR' OR (rpt_tipo != 'TR' AND rpt_inventario = 'S')) AND rpt_estatus = '' AND  (etiqueta - surtido) > 0 Order By fecha_cadu";
                }
                else
                {
                    cadena = "SELECT (num_cajas - cajas_sur) AS disponible, ISNULL(NULLIF(fechacad,' '), FORMAT( DATEADD(day, " + diascad + ", fecha), 'yyyyMMdd', 'en-US' )) AS fecha_cad, folio AS recibo, tarima, DATEDIFF(day, GETDATE(), fechacad) AS diasdisp, preautorizado FROM tb_det_eti_final Inner JOIN tb_mstr_ordenes_prod ON folio = ordp_folio WHERE cve_prod = '" + prod + "' AND estatus_sur != 'S' AND ordp_estatus != 'C' AND etiqueta = 'S' AND (num_cajas - cajas_sur) > 0 Order By fecha_cad";
                }

                SqlDataAdapter da = new SqlDataAdapter(cadena, thisConnection);
                DataSet ds = new DataSet();
                da.Fill(ds, "xlotes");
                DataTable xlote = ds.Tables["xlotes"];
                //Recorrido de cada uno de los folios y la validacion correspondiente hacia lo que tengo capturado************************

                string foliosAnt = "";

                foreach (DataRow row in xlote.Rows)
                {
                    usadas = 0;
                    int total_prod_simula = totalprodsimulado;
                    int TotalLeido = 0;
                    string Cadena = "Select Count(fecha) AS Total From Tb_Det_Etiqueta_Presplit " +
                                    "Where Eti_Recibo = '" + row["recibo"].ToString().Trim() + "' AND Eti_Producto = '" + captu.Codigo.Trim() + "' AND Eti_TarIni = '" + Convert.ToInt32(row["tarima"].ToString().Trim()) + "' AND Estatus = 'A'";

                    thisConnection.Open();
                    SqlCommand cmd = new SqlCommand(Cadena, thisConnection);
                    TotalLeido = Convert.ToInt32(cmd.ExecuteScalar());
                    int usadasant = 0;
                    thisConnection.Close();

                    if (Convert.ToInt32(diasmincad) <= Convert.ToInt32(row["diasdisp"].ToString().Trim()))
                    {
                        row["disponible"] = Convert.ToInt32(row["disponible"].ToString().Trim()) - TotalLeido;
                        if (Convert.ToInt32(row["disponible"]) > 0)
                        {
                            if (totalpro > 0)
                            {
                                string diacad = traediafecadrec(row["fecha_cad"].ToString().Trim(), tipo);
                                string mescad = traemesfecadrec(row["fecha_cad"].ToString().Trim(), tipo);
                                var prodcap = db.Query<xLote>("Select COUNT(ID) AS Cajas FROM xLote Where Codigo = '" + captu.Codigo.Trim() + "' AND Folio = '" + row["recibo"].ToString().Trim() + "'  AND CAST(Tarima as int) = '" + Convert.ToInt32(row["tarima"].ToString().Trim()) + "'");

                                foreach (var capturado in prodcap)
                                {
                                    usadas = Convert.ToInt32(capturado.Cajas.ToString().Trim());
                                    usadasant = usadas;
                                    totaldis = Convert.ToInt32(row["disponible"].ToString().Trim()) - Convert.ToInt32(capturado.Cajas.ToString().Trim());
                                    simulador = simulador + totaldis;
                                    totalpro = totalpro - usadas;
                                    totaldisponibles = totaldisponibles + totaldis;
                                    totaldisreal = totaldis;
                                }

                                if (totaldis > 0 && totalpro > 0)
                                {
                                    var prodcapfecad = db.Query<xLote>("Select COUNT(ID) AS Cajas FROM xLote Where Codigo = '" + captu.Codigo.Trim() + "' AND diacad = '" + diacad + "'AND mescad = '" + mescad + "'");

                                    foreach (var capturado in prodcapfecad)
                                    {
                                        usadas = Convert.ToInt32(capturado.Cajas.ToString().Trim()) - usadasant;
                                        totaldis = Convert.ToInt32(totaldis) - Convert.ToInt32(capturado.Cajas.ToString().Trim());
                                        simulador = simulador + totaldis;
                                        totalpro = totalpro - usadas;
                                        totaldisponibles = totaldisponibles + totaldis;
                                    }
                                }

                                if (totaldis > 0)
                                {
                                    if (totalpro > 0)
                                    {
                                        XLoteSug sugeridos = new XLoteSug { recibosug = row["recibo"].ToString().Trim(), fecrecsug = diacad + "/" + mescad, cveprod = prod, Tarima = row["tarima"].ToString().Trim(), Cajasdis = totaldisreal, Cajasusadas = usadas, foliomens = row["diasdisp"].ToString().Trim() };
                                        db.Insert(sugeridos);
                                        break;
                                    }

                                }

                                //diacadant = diacad;
                                //mescadant = mescad;

                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                    else
                    {
                        if (totalpro > 0)
                        {
                            string diacad = traediafecadrec(row["fecha_cad"].ToString().Trim(), tipo);
                            string mescad = traemesfecadrec(row["fecha_cad"].ToString().Trim(), tipo);
                            var prodcap = db.Query<xLote>("Select COUNT(ID) AS Cajas FROM xLote Where Codigo = '" + captu.Codigo.Trim() + "' AND Folio = '" + row["recibo"].ToString().Trim() + "'  AND CAST(Tarima as int) = '" + Convert.ToInt32(row["tarima"].ToString().Trim()) + "'");

                            foreach (var capturado in prodcap)
                            {
                                usadas = Convert.ToInt32(capturado.Cajas.ToString().Trim());
                                usadasant = usadas;
                                totaldis = Convert.ToInt32(row["disponible"].ToString().Trim()) - Convert.ToInt32(capturado.Cajas.ToString().Trim());
                                simulador = simulador + totaldis;
                                totalpro = totalpro - usadas;
                                totaldisponibles = totaldisponibles + totaldis;
                                totaldisreal = totaldis;
                            }

                            if (row["preautorizado"].ToString().Trim() != "A" && usadas > 0)
                            {
                                string productodefalult = "";
                                int diasdefault = 0;
                                thisConnection.Open();
                                query = "SELECT * FROM Tb_Cat_Prod_especial WHERE cve_prod = '" + prod.Trim() + "' AND estado = 'A'";

                                cmd = new SqlCommand(query);
                                cmd.Connection = thisConnection;
                                SqlDataReader Info;
                                Info = cmd.ExecuteReader();
                                while (Info.Read())
                                {
                                    productodefalult = "1";
                                    diasdefault = Convert.ToInt32(Info["dias_min"].ToString());
                                }
                                thisConnection.Close();
                                if (productodefalult != "1" || (Convert.ToInt32(diasdefault) > Convert.ToInt32(row["diasdisp"].ToString().Trim())))
                                {
                                    if (tipo == "PTC")
                                    {
                                        Mensajes mensa = new Mensajes { titulo = "FOLIO CAMPO NO APTO PARA CARGA Y SIN AUTORIZACION PREVIA", mensaje = "El recibo " + "\n\r" + row["recibo"].ToString().Trim() + " De la tarima  " + Convert.ToInt32(row["tarima"].ToString().Trim()) + " del producto: " + prodnom + " Tiene " + row["diasdisp"].ToString().Trim() + " Dias de caducidad, La carga Solicita con dias " + diasmincad + ", No se puede cargar sin autorizacion previa" };
                                        db.Insert(mensa);
                                        ValiFechacad = "N";
                                    }
                                    else
                                    {
                                        Mensajes mensa = new Mensajes { titulo = "FOLIO NO APTO PARA CARGA", mensaje = "El recibo " + "\n\r" + row["recibo"].ToString().Trim() + " De la tarima  " + Convert.ToInt32(row["tarima"].ToString().Trim()) + " del producto: " + prodnom + " Tiene " + row["diasdisp"].ToString().Trim() + " Dias de caducidad, La carga Solicita con dias " + diasmincad + ", Favor de Buscar a personal de Camaras Frias para la autorizacion" };
                                        db.Insert(mensa);
                                        ValiFechacad = "N";
                                    }
                                }

                            }



                            if (totaldis > 0 && totalpro > 0)
                            {
                                var prodcapfecad = db.Query<xLote>("Select COUNT(ID) AS Cajas FROM xLote Where Codigo = '" + captu.Codigo.Trim() + "' AND diacad = '" + diacad + "'AND mescad = '" + mescad + "'");

                                foreach (var capturado in prodcapfecad)
                                {
                                    usadas = Convert.ToInt32(capturado.Cajas.ToString().Trim()) - usadasant;
                                    totaldis = Convert.ToInt32(totaldis) - Convert.ToInt32(capturado.Cajas.ToString().Trim());
                                    simulador = simulador + totaldis;
                                    totalpro = totalpro - usadas;
                                    totaldisponibles = totaldisponibles + totaldis;
                                }
                            }
                        }
                        else
                        {
                            break;
                        }
                    }
                }

                var loteSug = db.Query<XLoteSug>("Select  *  FROM XLoteSug Where cveprod = '" + captu.Codigo.Trim() + "' AND cajasdis != 0 LIMIT 1");
                foreach (var capturado in loteSug)
                {
                    string recibosug = capturado.recibosug;
                    string fecrecsug = capturado.fecrecsug;
                    string cveprod = capturado.cveprod;
                    string tarima = capturado.Tarima;
                    int cajasdis = capturado.Cajasdis;
                    int cajasusadas = capturado.Cajasusadas;
                    string diasdif = capturado.foliomens;
                    Mensajes mensa = new Mensajes { titulo = "Existe un folio anterior disponible", mensaje = "El recibo " + "\n\r" + capturado.recibosug.ToString().Trim() + " De la tarima  " + capturado.Tarima.Trim() + " Tiene  " + capturado.Cajasdis + " cajas disponibles del producto: " + captu.nombre.Trim() + " Con Fecha de Caducidad del " + capturado.fecrecsug + ", Dias disponibles " + diasdif + ", Favor de Buscar a personal de Camaras Frias para la autorizacion" };
                    db.Insert(mensa);
                    ValiFechacad = "N";

                    XLoteSug sugeridosact = new XLoteSug { recibosug = recibosug.ToString().Trim(), fecrecsug = fecrecsug, cveprod = cveprod, Tarima = tarima.ToString().Trim(), Cajasdis = cajasdis, Cajasusadas = cajasusadas, foliomens = "S" };
                    db.Insert(sugeridosact);
                    totalpro = 0;
                }


            }


            return Valor;


        }


        public void actualizar_detalle()
        {
            string ordenactual = pedidoencaptura.Text.Replace("Pedido Actual: ", "").Trim();

            thisConnection.Open();
            string cadena = "SELECT prod_clave, pdn_num_unidades FROM tb_det_pedidos WHERE pdn_folio = '" + ordenactual + "'";
            SqlDataAdapter da = new SqlDataAdapter(cadena, thisConnection);
            DataSet ds = new DataSet();
            da.Fill(ds, "det_pedidos");
            det_pedidos = new DataTable();
            det_pedidos = ds.Tables["det_pedidos"];
            thisConnection.Close();


            var query = db.Table<ConPedidos>();
            foreach (var captu in query)
            {
                DataRow[] foundRows;
                foundRows = det_pedidos.Select("prod_clave = '" + captu.prod_clave.ToString().Trim() + "'");
                int pedidoactual = Convert.ToInt32(captu.pedido.ToString().Trim());
                int x = foundRows.Length;
                int pedidoactualsistema = 0;
                if (x > 0)
                {
                    pedidoactualsistema = Convert.ToInt32(foundRows[0][1]);
                }
                if (pedidoactual != pedidoactualsistema)
                {
                    db.Query<ConPedidos>("UPDATE [ConPedidos] SET pedido = '" + Convert.ToInt32(pedidoactualsistema) + "' WHERE prod_clave = '" + captu.prod_clave.ToString().Trim() + "'");
                    db.Query<Pedidos>("UPDATE [Pedidos] SET pedido = '" + Convert.ToInt32(pedidoactualsistema) + "' WHERE prod_clave = '" + captu.prod_clave.ToString().Trim() + "'");
                }
            }

        }

        protected override void OnNewIntent(Intent intent)
        {
            /*var alertMessage = new Android.App.AlertDialog.Builder(this).Create();
            var rawMessages = intent.GetParcelableArrayExtra(NfcAdapter.ExtraNdefMessages);
            if (rawMessages == null) return;
            var msg = (NdefMessage)rawMessages[0];
            var record = msg.GetRecords()[0];
            if (record == null) return;
            // The data is defined by the Record Type Definition (RTD) specification available from http://members.nfc-forum.org/specs/spec_list/
            if (record.Tnf != NdefRecord.TnfWellKnown) return;
            // Get the transmitted data
            var data = Encoding.ASCII.GetString(record.GetPayload());
            data = data.Substring(3, data.Length - 3);*/

            if (intent.Action != NfcAdapter.ActionTagDiscovered) return;
            var myTag = (Tag)intent.GetParcelableExtra(NfcAdapter.ExtraTag);
            if (myTag == null) return;
            var tagIdBytes = myTag.GetId();
            var data = ByteArrayToString(tagIdBytes);

            if (et != null)
            {
                if (et.HasFocus == true)
                {
                    et.Text = data;
                }
            }
            else if (password != null)
            {
                password.Enabled = true;
                if (password.HasFocus == true)
                {
                    password.Text = data;
                }
                password.Enabled = false;
            }

        }

        public static string ByteArrayToString(byte[] ba)
        {
            var shb = new SoapHexBinary(ba);
            return shb.ToString();
        }

        //Clases para La impresora
        private void FindPrinter()
        {

            try
            {
                mmDevice = (from bd in BluetoothAdapter.DefaultAdapter?.BondedDevices
                            where bd?.Name == deviceName
                            select bd).FirstOrDefault();
            }
            catch (System.Exception ex)
            {

            }
        }

        private void OpenPrinter()
        {

            try
            {
                if (mmDevice == null)
                    return;

                // Standard SerialPortService ID
                UUID uuid = UUID.FromString("00001101-0000-1000-8000-00805F9B34FB");
                mmSocket = mmDevice.CreateInsecureRfcommSocketToServiceRecord(uuid);
                mmSocket.Connect();
                mmOutputStream = mmSocket.OutputStream;
                mmInputStream = mmSocket.InputStream;

                //beginListenForData();

            }
            catch (System.Exception ex)
            {
                //myLabel.Text = ex.ToString ();
            }
        }
        private void beginListenForData()
        {
        }

        public void sendData()
        {
            string pdn_origen = "";
            string destino = "";
            string embarque = pedidoencaptura.Text.Replace("Pedido Actual: ", "").Trim();
            string bd = "tb_mstr_pedidos_nal";
            thisConnection.Open();
            if (Convert.ToInt32(embarque) < 400000)
            {
                bd = "tb_mstr_pedidos_exp";
            }
            pdn_origen = embarque;
            string Cadena = "SELECT pdn_pedorigen FROM " + bd + " WHERE pdn_folio = '" + embarque.Trim() + "'";
            SqlCommand cmd;
            cmd = new SqlCommand(Cadena);
            cmd.Connection = thisConnection;
            SqlDataReader datos;
            datos = cmd.ExecuteReader();
            while (datos.Read())
            {
                if (datos["pdn_pedorigen"].ToString().Trim() != "0" && datos["pdn_pedorigen"].ToString().Trim() != "")
                    pdn_origen = datos["pdn_pedorigen"].ToString();
            }
            thisConnection.Close();

            thisConnection.Open();
            Cadena = "   SELECT destino FROM  tb_mstr_trailer WHERE pdn_folio = '" + Convert.ToInt32(pdn_origen.Trim()) + "'";
            cmd = new SqlCommand(Cadena);
            cmd.Connection = thisConnection;
            SqlDataReader datos2;
            datos2 = cmd.ExecuteReader();
            destino = "PC/PA/SIN REPORTAR";
            while (datos2.Read())
            {
                destino = datos2["destino"].ToString().Trim();
            }
            thisConnection.Close();




            try
            {
                OpenPrinter();

                if (mmOutputStream == null)
                {
                    return;
                }

                BitmapFactory.Options options = new BitmapFactory.Options();
                options.InScaled = false;

                Bitmap bitmap = BitmapFactory.DecodeResource(Resources, Resource.Drawable.logomr, options);

                if (bitmap == null)
                {

                    return;
                }
                byte[] data = WoosimImage.PrintBitmap(0, 0, 384, 200, bitmap);
                bitmap.Recycle();

                mmOutputStream.Write(WoosimCmd.SetPageMode(), 0, WoosimCmd.SetPageMode().Length);
                mmOutputStream.Write(data, 0, data.Length);
                mmOutputStream.Write(WoosimCmd.PM_setStdMode(), 0, WoosimCmd.PM_setStdMode().Length);



                // the text typed by the user
                var msg = "SPLIT*" + pedidoencaptura.Text.Replace("Pedido Actual: ", "").Trim() + "*" + nosplit.Text.Replace("Split Numero: ", "") + ", " + Convert.ToInt32(total.Text) + "";
                byte[] barcode = System.Text.Encoding.GetEncoding(1252).GetBytes(msg);
                byte[] QRCode = WoosimBarcode.Create2DBarcodeQRCode(4, (sbyte)0x4d, 8, barcode);
                byte[] cmd_print = WoosimCmd.PrintData();
                string title1 = destino.ToUpper() + "\r\n " + pedidoencaptura.Text.Replace("Pedido Actual: ", "").Trim() + " \r\n SPLIT: " + nosplit.Text.Replace("Split Numero: ", "") + " \r\n CAJAS: " + Convert.ToInt32(total.Text) + " \r\n";
                string title2 = " \r\n  \r\n";
                ByteArrayOutputStream byteStream = new ByteArrayOutputStream(512);
                byte[] dBytes = System.Text.Encoding.GetEncoding(1252).GetBytes(title1);
                byte[] dBytes2 = System.Text.Encoding.GetEncoding(1252).GetBytes(title2);
                byteStream.Write(WoosimCmd.SetTextStyle(false, false, false, 3, 3));
                byteStream.Write(WoosimCmd.SetTextAlign(WoosimCmd.AlignCenter));
                byteStream.Write(dBytes);
                byteStream.Write(QRCode);
                byteStream.Write(dBytes2);
                byteStream.Write(cmd_print);
                mmOutputStream.Write(WoosimCmd.InitPrinter(), 0, WoosimCmd.InitPrinter().Length);
                mmOutputStream.Write(byteStream.ToByteArray(), 0, byteStream.ToByteArray().Length);


            }
            catch (System.Exception ex)
            {

            }
        }


        private string validafecadMAXIMOS()
        {
            //***************************************INSTRUCCION QUE VALIDA QUE LAS FECHAS DE CADUCIDAD ACTUALES NO SE PASEN DE LOS DIAS DE CARGA DE LA ORDEN**************************************************
            string Valor = "";
            ValiFechacad = "S";
            //Obtener los productos con su tipo de lo que se ha leido******************************************************************
            var productoscapturados = db.Query<xLote>("Select Tipo, Codigo, nombre FROM xLote GROUP BY Tipo, Codigo, nombre");
            db.Query<XLoteSug>("delete from[XLoteSug]");

            var allItems = db.Table<xLote>().ToList();
            int count = allItems.Count;
            int[] validados = new int[count + 1];
            int capturas = 0;
            foreach (var captu in productoscapturados)
            {
                int totalpro = 0;
                int totaldisponibles = 0;
                int totalusadas;
                int simulador = 0;
                int totaldis = 0;
                string fechaant = "";
                int totaldisreal = 0;
                int totalprodsimulado = 0;

                //traer el total de recibos vencidos para que no entren en la condicion
                var prodcapx = db.Query<xLote>("Select COUNT(ID) AS Cajas FROM xLote Where Codigo = '" + captu.Codigo.Trim() + "'");

                foreach (var capturadox in prodcapx)
                {
                    totalpro = Convert.ToInt32(capturadox.Cajas.ToString().Trim());

                }

                int resttotal = traerecibosvencidos(captu.Codigo.Trim(), captu.Tipo.Trim());

                totalpro = totalpro - resttotal;
                totalprodsimulado = totalpro;

                //Obtener los diferentes folios disponibles dependiendo el codigo y el tipo
                string todobien = "OK";
                int prod_cap = 0;
                int usadas = 0;
                int existefecant = 0;
                string cadena = "";
                string tipo = captu.Tipo.Trim();
                string prod = captu.Codigo.Trim();

                string diacadant = "";
                string mescadant = "";
                //traer nombre de producto para validar cuantos dias debo aumentar.
                string prodnom = captu.nombre.Trim();
                int diascad = 14;
                if (prodnom.Contains("BETABEL"))
                {
                    diascad = 60;
                }
                else if (prodnom.Contains("AJO"))
                {
                    diascad = 180;
                }
                else if (prodnom.Contains("ADEREZO") || prodnom.Contains("VINAGRETA") || prodnom.Contains("QUESO"))
                {
                    diascad = 90;
                }


                if (tipo == "PTC")
                {
                    cadena = "SELECT  (etiqueta - surtido) AS disponible, (CASE fecha_cad WHEN '' THEN  FORMAT( DATEADD(day, " + diascad + ", pti_fecha), 'dd/MM/yyyy', 'en-US' ) WHEN fecha_cad THEN fecha_cad END) AS fecha_cad, (CASE fecha_cad WHEN '' THEN  FORMAT( DATEADD(day, " + diascad + ", pti_fecha), 'yyyyMMdd', 'en-US' ) WHEN fecha_cad THEN FORMAT(convert(datetime,fecha_cad), 'yyyyMMdd', 'en-US' ) END) AS fecha_cadu, recibo, tarima, DATEDIFF(day, GETDATE(), (CASE fecha_cad WHEN '' THEN  FORMAT( DATEADD(day, " + diascad + ", pti_fecha), 'yyyyMMdd', 'en-US' ) WHEN fecha_cad THEN FORMAT(convert(datetime,fecha_cad), 'yyyyMMdd', 'en-US' ) END)) AS diasdisp, preautorizado  FROM TB_DET_TRAZABILIDAD Inner JOIN tb_mstr_recepcion_pt ON rpt_recibo = recibo WHERE (preautorizado = '' or preautorizado is null) AND PROD_CLAVE = '" + prod + "' AND pti_estatus_sur = '' AND tipo = 'PTC' AND (rpt_tipo != 'TR' OR (rpt_tipo != 'TR' AND rpt_inventario = 'S')) AND rpt_estatus = '' AND  (etiqueta - surtido) > 0 Order By fecha_cadu";
                }
                else
                {
                    cadena = "SELECT (num_cajas - cajas_sur) AS disponible, ISNULL(NULLIF(fechacad,' '), FORMAT( DATEADD(day, " + diascad + ", fecha), 'yyyyMMdd', 'en-US' )) AS fecha_cad, folio AS recibo, tarima, DATEDIFF(day, GETDATE(), fechacad) AS diasdisp, preautorizado FROM tb_det_eti_final Inner JOIN tb_mstr_ordenes_prod ON folio = ordp_folio WHERE (preautorizado = '' or preautorizado is null) AND cve_prod = '" + prod + "' AND estatus_sur != 'S' AND ordp_estatus != 'C' AND etiqueta = 'S' AND (num_cajas - cajas_sur) > 0 Order By fecha_cad";
                }

                SqlDataAdapter da = new SqlDataAdapter(cadena, thisConnection);
                DataSet ds = new DataSet();
                da.Fill(ds, "xlotes");
                DataTable xlote = ds.Tables["xlotes"];
                //Recorrido de cada uno de los folios y la validacion correspondiente hacia lo que tengo capturado************************

                string foliosAnt = "";

                foreach (DataRow row in xlote.Rows)
                {
                    usadas = 0;
                    int total_prod_simula = totalprodsimulado;
                    int TotalLeido = 0;
                    string Cadena = "Select Count(fecha) AS Total From Tb_Det_Etiqueta_Presplit " +
                                    "Where Eti_Recibo = '" + row["recibo"].ToString().Trim() + "' AND Eti_Producto = '" + captu.Codigo.Trim() + "' AND Eti_TarIni = '" + Convert.ToInt32(row["tarima"].ToString().Trim()) + "' AND Estatus = 'A'";

                    thisConnection.Open();
                    SqlCommand cmd = new SqlCommand(Cadena, thisConnection);
                    TotalLeido = Convert.ToInt32(cmd.ExecuteScalar());
                    int usadasant = 0;
                    thisConnection.Close();

                    row["disponible"] = Convert.ToInt32(row["disponible"].ToString().Trim()) - TotalLeido;
                    if (Convert.ToInt32(row["disponible"]) > 0)
                    {
                        if (totalpro > 0)
                        {
                            string diacad = traediafecadrec(row["fecha_cad"].ToString().Trim(), tipo);
                            string mescad = traemesfecadrec(row["fecha_cad"].ToString().Trim(), tipo);
                            var prodcap = db.Query<xLote>("Select COUNT(ID) AS Cajas FROM xLote Where Codigo = '" + captu.Codigo.Trim() + "' AND Folio = '" + row["recibo"].ToString().Trim() + "'  AND CAST(Tarima as int) = '" + Convert.ToInt32(row["tarima"].ToString().Trim()) + "'");

                            foreach (var capturado in prodcap)
                            {
                                usadas = Convert.ToInt32(capturado.Cajas.ToString().Trim());
                                usadasant = usadas;
                                totaldis = Convert.ToInt32(row["disponible"].ToString().Trim()) - Convert.ToInt32(capturado.Cajas.ToString().Trim());
                                simulador = simulador + totaldis;
                                totalpro = totalpro - usadas;
                                totaldisponibles = totaldisponibles + totaldis;
                                totaldisreal = totaldis;
                            }

                            if (totaldis > 0 && totalpro > 0)
                            {
                                var prodcapfecad = db.Query<xLote>("Select COUNT(ID) AS Cajas FROM xLote Where Codigo = '" + captu.Codigo.Trim() + "' AND diacad = '" + diacad + "'AND mescad = '" + mescad + "'");

                                foreach (var capturado in prodcapfecad)
                                {
                                    usadas = Convert.ToInt32(capturado.Cajas.ToString().Trim()) - usadasant;
                                    totaldis = Convert.ToInt32(totaldis) - Convert.ToInt32(capturado.Cajas.ToString().Trim());
                                    simulador = simulador + totaldis;
                                    totalpro = totalpro - usadas;
                                    totaldisponibles = totaldisponibles + totaldis;
                                }
                            }

                            if (totaldis > 0)
                            {
                                if (totalpro > 0)
                                {
                                    XLoteSug sugeridos = new XLoteSug { recibosug = row["recibo"].ToString().Trim(), fecrecsug = diacad + "/" + mescad, cveprod = prod, Tarima = row["tarima"].ToString().Trim(), Cajasdis = totaldisreal, Cajasusadas = usadas, foliomens = row["diasdisp"].ToString().Trim() };
                                    db.Insert(sugeridos);
                                    break;
                                }

                            }

                            //diacadant = diacad;
                            //mescadant = mescad;

                        }
                        else
                        {
                            break;
                        }
                    }
                }

                var loteSug = db.Query<XLoteSug>("Select  *  FROM XLoteSug Where cveprod = '" + captu.Codigo.Trim() + "' AND cajasdis != 0 LIMIT 1");
                foreach (var capturado in loteSug)
                {
                    string recibosug = capturado.recibosug;
                    string fecrecsug = capturado.fecrecsug;
                    string cveprod = capturado.cveprod;
                    string tarima = capturado.Tarima;
                    int cajasdis = capturado.Cajasdis;
                    int cajasusadas = capturado.Cajasusadas;
                    string diasdif = capturado.foliomens;
                    Mensajes mensa = new Mensajes { titulo = "Existe un folio anterior disponible", mensaje = "El recibo " + "\n\r" + capturado.recibosug.ToString().Trim() + " De la tarima  " + capturado.Tarima.Trim() + " Tiene  " + capturado.Cajasdis + " cajas disponibles del producto: " + captu.nombre.Trim() + " Con Fecha de Caducidad del " + capturado.fecrecsug + ", Dias disponibles " + diasdif + ", Favor de Buscar a personal de Camaras Frias para la autorizacion" };
                    db.Insert(mensa);
                    ValiFechacad = "N";

                    XLoteSug sugeridosact = new XLoteSug { recibosug = recibosug.ToString().Trim(), fecrecsug = fecrecsug, cveprod = cveprod, Tarima = tarima.ToString().Trim(), Cajasdis = cajasdis, Cajasusadas = cajasusadas, foliomens = "S" };
                    db.Insert(sugeridosact);
                    totalpro = 0;
                }
            }
            return Valor;
        }

    }

}