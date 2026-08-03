using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Text;
using Android.Text.Style;
using Android.Views;
using Android.Views.InputMethods;
using Android.Widget;
using Google.Android.Material.Dialog;
using Java.Lang;
using Java.Net;
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
using System.Net;
using System.Net.Mail;
using System.Threading;

namespace SplitTrailers
{
    [Activity(Label = "Cancelar parcial")]

    public partial class CancelarParcial : Activity, Android.Text.ITextWatcher
    {
        public static int valido = 0, veces = 0;
        public static string cvvehiculo, cvresponsable;
        public static string vehiculo, responsable, pedidocancelar, cveresponsplit, responsplit;
        public static string imei, currentVersionName;
        public string Nombre = "", Mtipo = "", MProd = "", MTar = "", MFol = "", mUser = "", mAutoriza = "", user = "";
        public string cvecam = "", muser = "", mconcen = "1", Version = "15.3";
        public static string AutoPed = "N";
        public int proceso = 0;
        public static string EtiquetaExiste = "S", EtiquetaCapturada = "S";
        public static string HayExistencias = "S";
        public static string Surtidomayor = "S";
        public static string ValiFechacad = "S";
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
        string query = "", prod_clave = "", folio = "", tipo = "", cadena = "", prod_nombre = "";
        int tarima = 0, caja = 0, tarimaf = 0;
        bool find = false;
        ArrayAdapter<System.String> comboAdapter;
        System.String[] strFrutas;
        int FolioCampo = 0;


        DataTable CatProd = new DataTable();

        //Declarar los datos de los items en el layout CapturarSplit
        EditText foliocaptura;
        TextView total;
        TextView pedidoencaptura;
        Button Guardar;

        TextView nosplit;


        Int32 TotCaj;


        string valorfinal = "";


        //Datos supervisor
        EditText supervisor;
        EditText passwordsupervisor;

        //CheckBox Eliminar Caja
        CheckBox Eliminar_caja;


        //Radio button
        RadioButton etiblanca;
        RadioButton etiverde;


        //Variables de solicitud al servidor si realiza o no guardado de datos de la bd interna a la bd del servidor antes de borrar

        Context context;
        Runnable listener;
        private static string INFO_FILE = "http://192.168.123.4:81/EmbarquesApk/estado_respaldo.txt";
        private int respaldo_activo;


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
            vehiculo = Intent.GetStringExtra("camioneta");
            pedidocancelar = Intent.GetStringExtra("pedidocancelar");
            responsplit = Intent.GetStringExtra("responsablesplit");
            cveresponsplit = Intent.GetStringExtra("cveresponsplit");
            imei = Intent.GetStringExtra("imei");
            currentVersionName = Intent.GetStringExtra("currentVersionName");

            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.cancelarparcial);
            LoadConnection();
            TotCaj = 0;
            muser = SolicitarPed.responsable;
            cvecam = SolicitarPed.cvvehiculo;

            Eliminar_caja = FindViewById<CheckBox>(Resource.Id.Eliminar);

            foliocaptura = FindViewById<EditText>(Resource.Id.FolioCancelar);
            total = FindViewById<TextView>(Resource.Id.totalcapturadocancelar);
            pedidoencaptura = FindViewById<TextView>(Resource.Id.pedidoencaptura);
            nosplit = FindViewById<TextView>(Resource.Id.splitcantidad);
            Guardar = FindViewById<Button>(Resource.Id.GuardarCancelado);
            Guardar.Click += BtnGuardar_Click;
            Guardar.Enabled = false;


            pedidoencaptura.Text = pedidocancelar;


            foliocaptura.LongClickable = false;

            thisConnection.Open();
            cmnd = thisConnection.CreateCommand();
            cmnd.CommandText = "select inicio_campo from Tb_folio_campo";
            FolioCampo = Convert.ToInt32(cmnd.ExecuteScalar());
            thisConnection.Close();


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
                //nosplit.Text = "Split Numero: " + NoSplit(captu.folio.ToString());
                pedidoencaptura.Text = "Pedido Actual: " + captu.folio.ToString();
            }

            //tERMINA TRAER NUMERO DE SPLIT

            thisConnection.Close();

            //Llamar al Documento en el servidor para saber si la opcion para hacer el respaldo esta activa

            //getData();

            //****************************************Inicio Lectura de QR**************************************************************************************

            foliocaptura.AddTextChangedListener(this);


            List<FlimStarInfo> lstFlimStar = productocapturado();
            var gvObject = FindViewById<GridView>(Resource.Id.gvCtrCanceladoParcial);
            gvObject.Adapter = new myGVItemAdapter(this, lstFlimStar);
            gvObject.ItemClick += new EventHandler<AdapterView.ItemClickEventArgs>(OnGridView_ItemClicked);

            total.Text = TotCaj.ToString("##0");

            foliocaptura.KeyPress += Foliocaptura_KeyPress;
            //foliocaptura.KeyPress += onEditTextKeyPress;
        }

        public string GetDeviceID()
        {
            Android.Telephony.TelephonyManager mTelephonyMgr;
            mTelephonyMgr = (Android.Telephony.TelephonyManager)GetSystemService(TelephonyService);
            imei = mTelephonyMgr.DeviceId;

            if (imei == null)
            {
                var deviceId = CrossDeviceInfo.Current.Id;
                deviceId = deviceId.Substring(0, 15);
                return imei = deviceId;
            }
            else
            {
                return imei;
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
                context = this;

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
            Guardar.Enabled = false;


            var progressDialog = ProgressDialog.Show(this, "Espere Por Favor...", "Liberando Etiquetas y Modificando Split", true);


            new System.Threading.Thread(new ThreadStart(delegate
            {//LOAD METHOD TO GET ACCOUNT INFO

                db.Query<xLoteFinal>("delete from  [xLoteFinal]");
                //db.Query<Pedidos>("UPDATE [Pedidos] SET surtido = '0'");
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
                    /*var pedidos = db.Query<Pedidos>("SELECT * FROM [Pedidos] Where prod_clave = '" + mcod.ToString().Trim() + "'");
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
                    }*/


                    //Traer Split Donde se capturo la caja
                    string cadena = "Select split From tb_det_Etiqueta  Where emb_folio = '" + pedidocancelar + "' AND Eti_Lectura = '" + lectura + "' AND Estatus = 'A'";
                    SqlCommand cmd = new SqlCommand(cadena, thisConnection);
                    var SplitProd_actual = Convert.ToString(cmd.ExecuteScalar());


                    //Liberar Etiqueta del Detalle de Etiqueta
                    cadena = "UPDATE tb_det_Etiqueta SET Estatus = 'C', Obs = 'Cancelacion de Etiqueta Pedido: " + pedidocancelar + " Supervisor: " + cvresponsable + " Responsable Split: " + responsplit + "' Where emb_folio = '" + pedidocancelar + "' AND Eti_Lectura = '" + lectura + "' AND Estatus = 'A'";
                    cmd = new SqlCommand(cadena, thisConnection);
                    cmd.ExecuteNonQuery();


                    //Traer Estado Actual Del Split, Si ya fue Surtido o no
                    cadena = "Select estatus From tb_det_split  Where emb_folio = '" + pedidocancelar + "' AND tarima = '" + SplitProd_actual + "' AND prod_clave = '" + mcod + "' AND tipo_rec = '" + mtip + "' AND TARINI = '" + mtar + "'";
                    cmd = new SqlCommand(cadena, thisConnection);
                    var SplitSurtido = Convert.ToString(cmd.ExecuteScalar());


                    //Decremento el Desfase del detalle del split para que se pueda cerrar la orden...
                    cadena = "UPDATE tb_det_split SET cajas = cajas - 1 Where emb_folio = '" + pedidocancelar + "' AND tarima = '" + SplitProd_actual + "' AND prod_clave = '" + mcod + "' AND tipo_rec = '" + mtip + "' AND TARINI = '" + mtar + "'";
                    cmd = new SqlCommand(cadena, thisConnection);
                    cmd.ExecuteNonQuery();


                    // ACTUALIZO LAS CAJAS SURTIDAS DE ACUERDO AL FOLIO CODIGO Y TARIMA 
                    if (pedidocancelar.ToString().Trim().Length > 0)
                    {
                        if (mtip.ToString() == "PTC")
                            cadena = "UPDATE TB_DET_TRAZABILIDAD SET SURTIDO = SURTIDO - 1 WHERE PROD_CLAVE = '" + mcod.ToString() + "' AND RECIBO = '" + mfol.ToString() + "' " +
                                "AND TIPO = 'PTC' AND TARIMA = '" + Convert.ToInt32(mtar.ToString()).ToString() + "' ";

                        else
                            cadena = "UPDATE TB_DET_ETI_FINAL SET CAJAS_SUR = CAJAS_SUR - 1 WHERE CVE_PROD = '" + mcod.ToString().Trim() + "' AND FOLIO = '" + mfol.ToString() + "' " +
                                "AND TARIMA = '" + Convert.ToInt32(mtar.ToString()).ToString() + "' ";
                        cmd = new SqlCommand(cadena, thisConnection);
                        cmd.ExecuteNonQuery();
                    }

                    if (SplitSurtido.ToString().Trim() == "S")
                    {
                        cadena = "UPDATE TOP (1) tb_det_embarque SET cajas = cajas - 1  WHERE prod_clave = '" + mcod.ToString().Trim() + "' AND recibo  = '" + mfol.ToString().Trim() + "' " +
                       "AND tarima  = '" + Convert.ToInt32(mtar.ToString().Trim()).ToString() + "' AND emb_folio = '" + pedidocancelar.ToString().Trim() + "' AND Estatus != 'C' AND OpCap = 'X' AND cajas > 0";
                        cmd = new SqlCommand(cadena, thisConnection);
                        cmd.ExecuteNonQuery();
                    }
                }


                AgregaProdXPedido();

                /*AgregaTempSplit();

                if (ValiFechacad == "N")
                {
                    AgregaDetaEtiAdelantado();
                }
                */
                thisConnection.Close();

                #region MATERIAL DIALOG
                RunOnUiThread(() =>
                {
                    var alertDialog = new AndroidX.AppCompat.App.AlertDialog.Builder(this);
                    alertDialog.SetTitle(Html.FromHtml("<font color='#28A745'><b>ETIQUETAS LIBERADAS</b></font>", FromHtmlOptions.ModeLegacy));
                    alertDialog.SetIcon(Resource.Drawable.exito);
                    alertDialog.SetMessage(Html.FromHtml("<font color='#000000'>Las cajas han sido liberadas correctamente.<br><br><b>Favor de acomodarlas donde corresponda.</b></font>", FromHtmlOptions.ModeLegacy));
                    alertDialog.SetCancelable(false);

                    alertDialog.SetPositiveButton(Html.FromHtml("<font color='#28A745'><b>OK</b></font>", FromHtmlOptions.ModeLegacy), delegate
                    {
                        try
                        {
                            // Limpieza de tablas locales
                            db.Query<Pedidos>("DELETE FROM [Pedidos]");
                            db.Query<ConPedidos>("DELETE FROM [ConPedidos]");
                            db.Query<xLote>("DELETE FROM [xLote]");
                            db.Query<xLoteFinal>("DELETE FROM [xLoteFinal]");
                            db.Query<xprod>("DELETE FROM [xprod]");

                            // Redirección
                            var intent = new Intent(this, typeof(SolicitarPed));
                            intent.AddFlags(ActivityFlags.ClearTop | ActivityFlags.SingleTop);
                            intent.PutExtra("cvresponsable", cveresponsplit.ToString());
                            intent.PutExtra("responsable", responsplit.ToString());
                            StartActivity(intent);
                            Finish();
                        }
                        catch (Java.Lang.Exception ex)
                        {
                            Toast.MakeText(this, "Error al limpiar datos: " + ex.Message, ToastLength.Long).Show();
                        }
                    });

                    var dialog = alertDialog.Create();
                    dialog.Show();

                    // Personalización visual adicional (Material accent)
                    dialog.GetButton((int)Android.Content.DialogButtonType.Positive)?.SetTextColor(Android.Graphics.Color.ParseColor("#28A745"));
                    dialog.GetButton((int)Android.Content.DialogButtonType.Positive)?.SetAllCaps(false);

                    // Toast y ocultar progress dialog
                    Toast.MakeText(this, "Etiquetas liberadas correctamente.", ToastLength.Long).Show();
                    progressDialog.Hide();
                });
                #endregion

                #region ALERT GIALOG
                /*Android.App.AlertDialog.Builder alertDialog = new Android.App.AlertDialog.Builder(this);
                alertDialog.SetTitle(Html.FromHtml("<font color='#dc3545' size = 10>Etiquetas Liberadas</font>"));
                alertDialog.SetIcon(Resource.Drawable.exito);
                alertDialog.SetMessage(Html.FromHtml("<font color='#FFFFFF' size = 10>Las Cajas han sido liberadas, Favor de Acomodarlas donde Corresponda!!! </font>"));
                alertDialog.SetCancelable(false);
                alertDialog.SetNeutralButton("Ok", delegate
                {
                    alertDialog.Dispose();
                    db.Query<Pedidos>("delete from  [Pedidos]");
                    db.Query<ConPedidos>("delete from  [ConPedidos]");
                    db.Query<xLote>("delete from  [xLote]");
                    db.Query<xLoteFinal>("delete from  [xLoteFinal]");
                    db.Query<xprod>("delete from  [xprod]");



                    Intent intent = new Intent(this, typeof(SolicitarPed));
                    intent.AddFlags(ActivityFlags.ClearTop);
                    Intent.AddFlags(ActivityFlags.SingleTop);
                    //intent.PutExtra("cvcamioneta", cvvehiculo.ToString());
                    intent.PutExtra("cvresponsable", cveresponsplit.ToString());
                    //intent.PutExtra("camioneta", vehiculo.ToString());
                    intent.PutExtra("responsable", responsplit.ToString());
                    StartActivity(intent);
                    Finish();
                });
                RunOnUiThread(() => alertDialog.Show());

                RunOnUiThread(() => Toast.MakeText(this, "Etiquetas Liberadas Correctamente.", ToastLength.Long).Show()); //HIDE PROGRESS DIALOG 
                RunOnUiThread(() => progressDialog.Hide());*/
                #endregion
            })).Start();

        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.top_menu_captura, menu);
            return base.OnCreateOptionsMenu(menu);
        }

        private void AgregaProdXPedido()
        {
            string mnom = "";
            int total = 0;
            string folio = "";
            string prod_clave = "";
            string pedido = "";
            var pedidosproducto = db.Table<Pedidos>();
            foreach (var producto in pedidosproducto)
            {

                //Ver si hay registros del producto
                string Cadena = "Select CANTSURTIDO from TB_DET_SPLIT_PRODXPED where PDN_FOLIO = '" + producto.folio.ToString() + "' AND PROD_CLAVE = '" + producto.prod_clave.ToString() + "'";
                SqlCommand cmdx = new SqlCommand(Cadena, thisConnection);
                var valor = Convert.ToString(cmdx.ExecuteScalar());

                // termina ver si hay registros

                total = traetotal(producto.prod_clave.ToString().Trim());
                folio = producto.folio.ToString().Trim();
                prod_clave = producto.prod_clave.ToString().Trim();
                mnom = producto.nombre.ToString().Trim();
                mnom = mnom.Replace("'", " ");
                pedido = producto.pedido.ToString().Trim();

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
            AgregaRegistroPedidoAuto(folio, "Prod: (" + prod_clave + ") " + mnom + " Ped:" + pedido + " Sur:" + total);
            SendMail("jgalvan@mrlucky.com.mx", "Se detectaron posibles diferencias de <b>SPLIT</b> del <b>PRODUCTO: </b>" + mnom + " del PEDIDO: " + folio + " Consulta " + cadenainfomensaje, "POSIBLES DIFERENCIAS DE SPLIT " + folio);
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
                var gvObject = FindViewById<GridView>(Resource.Id.gvCtrCanceladoParcial);
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

                var progressDialog = ProgressDialog.Show(this, "Espere Por Favor...", "Validando Etiquetas A Liberar...", true);


                new System.Threading.Thread(new ThreadStart(delegate
                {//LOAD METHOD TO GET ACCOUNT INFO

                    //try
                    //{
                    string existenproductos = "NO";
                    var existeCapturado = db.Table<xprod>();
                    foreach (var captu in existeCapturado)
                    {
                        existenproductos = "SI";
                    }

                    if (existenproductos == "SI")
                    {
                        //insertarinfo();
                        if (validaestructuraetiqueta() == "SI")
                        {
                            db.Query<Mensajes>("delete from  [Mensajes]");
                            AutoPed = "N";
                            RunOnUiThread(() => Guardar.Enabled = false);

                            var validando = valida();
                            var producto = validaprod();
                            //var validandofec = validafecad();

                            if (Surtidomayor == "NR")
                            {
                                ImprimirDialogs(0);
                            }
                            else if (Surtidomayor == "N")
                            {
                                ImprimirDialogs(0);
                            }
                            else
                            {
                                if (Surtidomayor == "NR" || EtiquetaCapturada == "N")
                                {
                                    ImprimirDialogs(0);
                                }
                                else
                                {
                                    if (EtiquetaExiste == "S")
                                    {
                                        if (producto == "S" && (validando == "S"))
                                            RunOnUiThread(() => Guardar.Enabled = true);
                                        else
                                            RunOnUiThread(() => Guardar.Enabled = false);

                                        /*if (validando != "S" || producto != "S")
                                        {
                                            if (producto == "N" || HayExistencias == "S")
                                            {
                                                RunOnUiThread(() => Guardar.Enabled = true);
                                            }
                                        }*/
                                        if ((Guardar.Enabled == true) && (ValiFechacad == "N"))
                                        {
                                            RunOnUiThread(() => Guardar.Enabled = false);
                                            RunOnUiThread(() => fnShowCustomAlertDialogCancel());
                                        }
                                        ImprimirDialogs(0);
                                    }
                                    else
                                    {
                                        ImprimirDialogs(0);
                                    }


                                }

                            }

                        }
                        //insertarinfoMensaje();
                        List<FlimStarInfo> lstFlimStar = detalle_lote();
                        var gvObject = FindViewById<GridView>(Resource.Id.gvCtrCanceladoParcial);
                        RunOnUiThread(() => gvObject.Adapter = new myGVItemAdapter(this, lstFlimStar));
                        gvObject.ItemClick += new EventHandler<AdapterView.ItemClickEventArgs>(OnGridView_ItemClicked);

                    }
                    else
                    {
                        #region MATERIAL DIALOG
                        RunOnUiThread(() =>
                        {
                            var alertDialog = new AndroidX.AppCompat.App.AlertDialog.Builder(this);
                            alertDialog.SetTitle(Html.FromHtml("<font color='#DC3545'><b>SIN PRODUCTOS CAPTURADOS</b></font>", FromHtmlOptions.ModeLegacy));
                            alertDialog.SetIcon(Resource.Drawable.no);
                            alertDialog.SetMessage(Html.FromHtml("<font color='#000000'>No existen productos capturados para validar.</font>", FromHtmlOptions.ModeLegacy));
                            alertDialog.SetCancelable(false);

                            alertDialog.SetPositiveButton(Html.FromHtml("<font color='#DC3545'><b>OK</b></font>", FromHtmlOptions.ModeLegacy), delegate
                            {
                                alertDialog.Dispose();
                            });

                            var dialog = alertDialog.Create();
                            dialog.Show();

                            // Personalización visual del botón (color, estilo)
                            dialog.GetButton((int)Android.Content.DialogButtonType.Positive)?.SetTextColor(Android.Graphics.Color.ParseColor("#DC3545"));
                            dialog.GetButton((int)Android.Content.DialogButtonType.Positive)?.SetAllCaps(false);
                        });
                        #endregion

                        #region ALERT DIALOG
                        /*Android.App.AlertDialog.Builder alertDialogi = new Android.App.AlertDialog.Builder(this);
                        alertDialogi.SetTitle(Html.FromHtml("<font color='#dc3545' size = 10>Sin Productos Capturados</font>"));
                        alertDialogi.SetIcon(Resource.Drawable.no);
                        alertDialogi.SetMessage(Html.FromHtml("<font color='#FFFFFF' size = 10>No existen productos capturados para validar</font>"));
                        alertDialogi.SetCancelable(false);
                        alertDialogi.SetNeutralButton("Ok", delegate
                            {
                                alertDialogi.Dispose();
                            });
                        RunOnUiThread(() => alertDialogi.Show());*/
                        #endregion
                    }


                    mconcen = "1";
                    RunOnUiThread(() => Toast.MakeText(this, "Proceso Validado correctamente.", ToastLength.Long).Show()); //HIDE PROGRESS DIALOG 
                    RunOnUiThread(() => progressDialog.Hide());

                    /*}
                    catch
                    {

                        Android.App.AlertDialog.Builder alertDialog = new Android.App.AlertDialog.Builder(this);
                        alertDialog.SetTitle(Html.FromHtml("<font color='#dc3545' size = 10>Error en la validaciòn</font>"));
                        alertDialog.SetIcon(Resource.Drawable.nou);
                        alertDialog.SetMessage(Html.FromHtml("<font color='#FFFFFF' size = 10>Ocurrio un error inesperado durante la validación, Favor de Validar nuevamente</font>"));
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
                            //intent.PutExtra("responsable", responsable.ToString());
                            StartActivity(intent);
                            Finish();
                        });
                        RunOnUiThread(() => alertDialog.Show());

                        RunOnUiThread(() => Toast.MakeText(this, "Ocurrio un error en la validación", ToastLength.Long).Show()); //HIDE PROGRESS DIALOG 
                        RunOnUiThread(() => progressDialog.Hide());

                    }*/


                })).Start();



            }
            else
            {
                mconcen = "2";
                List<FlimStarInfo> lstFlimStar = detalle_pedido();
                var gvObject = FindViewById<GridView>(Resource.Id.gvCtrCanceladoParcial);
                gvObject.Adapter = new myGVItemAdapter(this, lstFlimStar);
                gvObject.ItemClick += new EventHandler<AdapterView.ItemClickEventArgs>(OnGridView_ItemClicked); //detalle_pedido
                Toast.MakeText(this, "Modo Concentrado Activado", ToastLength.Short).Show();

            }


            return base.OnOptionsItemSelected(item);
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
                        #region MATERIAL DIALOG
                        RunOnUiThread(() =>
                        {
                            var alertDialog = new AndroidX.AppCompat.App.AlertDialog.Builder(this);
                            alertDialog.SetTitle(Html.FromHtml(
                                $"<font color='#FFC107'><b>{captu.titulo}</b></font>",
                                FromHtmlOptions.ModeLegacy
                            ));
                            alertDialog.SetIcon(Resource.Drawable.warning);
                            alertDialog.SetMessage(Html.FromHtml(
                                $"<font color='#000000'>{captu.mensaje}</font>",
                                FromHtmlOptions.ModeLegacy
                            ));
                            alertDialog.SetCancelable(false);

                            alertDialog.SetPositiveButton(Html.FromHtml(
                                "<font color='#FFC107'><b>OK</b></font>",
                                FromHtmlOptions.ModeLegacy
                            ), delegate
                            {
                                alertDialog.Dispose();
                                ImprimirDialogs(mensaje + 1);
                            });

                            var dialog = alertDialog.Create();
                            dialog.Show();

                            // Personalización del botón (Material Design)
                            var btn = dialog.GetButton((int)Android.Content.DialogButtonType.Positive);
                            btn?.SetTextColor(Android.Graphics.Color.ParseColor("#FFC107"));
                            btn?.SetAllCaps(false);
                        });
                        #endregion
                        #region ALERT DIALOG
                        /*Android.App.AlertDialog.Builder alertDialog = new Android.App.AlertDialog.Builder(this);
                        alertDialog.SetTitle(Html.FromHtml("<font color='#ffc107' size = 10>" + captu.titulo.ToString() + "</font>"));
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
                        #region MATERIAL DIALOG
                        RunOnUiThread(() =>
                        {
                            var alertDialog = new AndroidX.AppCompat.App.AlertDialog.Builder(this);

                            alertDialog.SetTitle(Html.FromHtml(
                                $"<font color='#0DCAF0'><b>{captu.titulo}</b></font>",
                                FromHtmlOptions.ModeLegacy
                            ));

                            alertDialog.SetIcon(Resource.Drawable.Info);

                            alertDialog.SetMessage(Html.FromHtml(
                                $"<font color='#0D6EFD'>{captu.mensaje}</font>",
                                FromHtmlOptions.ModeLegacy
                            ));

                            alertDialog.SetCancelable(false);

                            alertDialog.SetPositiveButton(Html.FromHtml(
                                "<font color='#0D6EFD'><b>OK</b></font>",
                                FromHtmlOptions.ModeLegacy
                            ), delegate
                            {
                                alertDialog.Dispose();
                                ImprimirDialogs(mensaje + 1);
                            });

                            var dialog = alertDialog.Create();
                            dialog.Show();

                            // Personalización visual del botón OK (Material Design)
                            var btn = dialog.GetButton((int)Android.Content.DialogButtonType.Positive);
                            btn?.SetTextColor(Android.Graphics.Color.ParseColor("#0D6EFD"));
                            btn?.SetAllCaps(false);
                        });
                        #endregion

                        #region ALERT DIALOG
                        /*Android.App.AlertDialog.Builder alertDialog = new Android.App.AlertDialog.Builder(this);
                        alertDialog.SetTitle(Html.FromHtml("<font color='#0dcaf0' size = 10>" + captu.titulo.ToString() + "</font>"));
                        alertDialog.SetIcon(Resource.Drawable.Info);
                        alertDialog.SetMessage(Html.FromHtml("<font color='#0d6efd' size = 10>" + captu.mensaje.ToString() + "</font>"));
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
                        #region MATERIAL DIALOG
                        RunOnUiThread(() =>
                        {
                            var alertDialog = new AndroidX.AppCompat.App.AlertDialog.Builder(this);

                            // Título con tono ámbar (advertencia)
                            alertDialog.SetTitle(Html.FromHtml(
                                $"<font color='#FFC107'><b>{captu.titulo}</b></font>",
                                FromHtmlOptions.ModeLegacy
                            ));

                            alertDialog.SetIcon(Resource.Drawable.warning);

                            // Mensaje con un tono ligeramente más claro
                            alertDialog.SetMessage(Html.FromHtml(
                                $"<font color='#FFC929'>{captu.mensaje}</font>",
                                FromHtmlOptions.ModeLegacy
                            ));

                            alertDialog.SetCancelable(false);

                            // Botón principal
                            alertDialog.SetPositiveButton(Html.FromHtml(
                                "<font color='#FFC107'><b>OK</b></font>",
                                FromHtmlOptions.ModeLegacy
                            ), delegate
                            {
                                alertDialog.Dispose();
                                ImprimirDialogs(mensaje + 1);
                            });

                            var dialog = alertDialog.Create();
                            dialog.Show();

                            // Personalización del botón
                            var btn = dialog.GetButton((int)Android.Content.DialogButtonType.Positive);
                            btn?.SetTextColor(Android.Graphics.Color.ParseColor("#FFC107"));
                            btn?.SetAllCaps(false);
                        });
                        #endregion

                        #region ALERT DIALOG
                        /*Android.App.AlertDialog.Builder alertDialog = new Android.App.AlertDialog.Builder(this);
                        alertDialog.SetTitle(Html.FromHtml("<font color='#ffc107' size = 10>" + captu.titulo.ToString() + "</font>"));
                        alertDialog.SetIcon(Resource.Drawable.no);
                        alertDialog.SetMessage(Html.FromHtml("<font color='#ffc929' size = 10>" + captu.mensaje.ToString() + "</font>"));
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
                        #region MATERIAL DIALOG
                        RunOnUiThread(() =>
                        {
                            var alertDialog = new AndroidX.AppCompat.App.AlertDialog.Builder(this);

                            // Título en rojo para indicar error
                            alertDialog.SetTitle(Html.FromHtml(
                                $"<font color='#FF0000'><b>{captu.titulo}</b></font>",
                                FromHtmlOptions.ModeLegacy
                            ));

                            alertDialog.SetIcon(Resource.Drawable.no);

                            // Mensaje en blanco sobre fondo claro
                            alertDialog.SetMessage(Html.FromHtml(
                                $"<font color='#FFFFFF'>{captu.mensaje}</font>",
                                FromHtmlOptions.ModeLegacy
                            ));

                            alertDialog.SetCancelable(false);

                            // Botón principal
                            alertDialog.SetPositiveButton(Html.FromHtml(
                                "<font color='#FF0000'><b>OK</b></font>",
                                FromHtmlOptions.ModeLegacy
                            ), delegate
                            {
                                alertDialog.Dispose();
                                ImprimirDialogs(mensaje + 1);
                            });

                            var dialog = alertDialog.Create();
                            dialog.Show();

                            // Personalización del botón (color y estilo)
                            var btn = dialog.GetButton((int)Android.Content.DialogButtonType.Positive);
                            btn?.SetTextColor(Android.Graphics.Color.ParseColor("#FF0000"));
                            btn?.SetAllCaps(false);
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
            string dbPath = System.IO.Path.Combine(folder, "SplitTrailer_Cancelacion.db3");

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

        }

        List<FlimStarInfo> listItem = new List<FlimStarInfo>();
        private string cadenainfomensaje;

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
                    #region MATERIAL DIALOG
                    RunOnUiThread(() =>
                    {
                        var alertDialog = new AndroidX.AppCompat.App.AlertDialog.Builder(this);

                        // Título en rojo para error
                        alertDialog.SetTitle(Html.FromHtml(
                            "<font color='#DC3545'><b>Error en la estructura de Etiqueta</b></font>",
                            FromHtmlOptions.ModeLegacy
                        ));

                        alertDialog.SetIcon(Resource.Drawable.Info);

                        // Mensaje detallado en color ámbar
                        var mensaje = $"La etiqueta del producto {mcod} - {NOmprod} Recibo: {mfol} / Tarima {mtar} / Caja: {mcaj} " +
                                      "contiene un error en la tarima, recibo o folio, favor de informar al supervisor, validar la información, " +
                                      "retirar y reetiquetar la caja y leer la nueva etiqueta";

                        alertDialog.SetMessage(Html.FromHtml(
                            $"<font color='#FFC107'>{mensaje}</font>",
                            FromHtmlOptions.ModeLegacy
                        ));

                        alertDialog.SetCancelable(false);

                        // Botón principal
                        alertDialog.SetPositiveButton(Html.FromHtml(
                            "<font color='#DC3545'><b>OK</b></font>",
                            FromHtmlOptions.ModeLegacy
                        ), delegate
                        {
                            alertDialog.Dispose();

                            // Borrado de etiquetas capturadas
                            db.Query<xprod>(
                                $"DELETE FROM [xprod] WHERE Tipo = '{mtip}' AND Folio = '{mfol}' AND Codigo = '{mcod}' AND Tarima = '{mtar}' AND Cajas = '{mcaj}'"
                            );
                            db.Query<ConPedidos>(
                                $"UPDATE [ConPedidos] SET surtido = surtido - 1 WHERE prod_clave = '{mcod.Trim()}'"
                            );
                        });

                        var dialog = alertDialog.Create();
                        dialog.Show();

                        // Personalización del botón (color y estilo)
                        var btn = dialog.GetButton((int)Android.Content.DialogButtonType.Positive);
                        btn?.SetTextColor(Android.Graphics.Color.ParseColor("#DC3545"));
                        btn?.SetAllCaps(false);
                    });
                    #endregion

                    #region ALERT DIALOG
                    /*Android.App.AlertDialog.Builder alertDialog = new Android.App.AlertDialog.Builder(this);
                    alertDialog.SetTitle(Html.FromHtml("<font color='#dc3545' size = 10>Error en la estructura de Etiqueta</font>"));
                    alertDialog.SetIcon(Resource.Drawable.Info);
                    alertDialog.SetMessage(Html.FromHtml("<font color='#ffc107' size = 10>La etiqueta del producto " + mcod + " - " + NOmprod + " Recibo: " + mfol + " / Tarima " + mtar + " / Caja: " + mcaj + " contiene un error en la tarima, recibo o folio, favor de informar al supervisor, validar la informacion, retirar y reetiquetar la caja y leer  la nueva etiqueta</font>"));
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
            HayExistencias = "S";
            EtiquetaExiste = "S";
            EtiquetaCapturada = "S";
            db.Query<xLote>("delete from  [xLote]");
            string ok = "S";
            int tot = 0, totok = 0;
            thisConnection.Open();
            string mtip = "", mfol = "", mcod = "", mtar = "", mcaj = "", mfeccap = "";
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


                string nom = traenom(captu.Codigo.ToString().Trim());
                string lectura = mtip + mfol + mcod + mtar + mcaj;
                string fechacap = ValidaCaja(lectura).Trim();
                //string fechacappre = ValidaCajaPreesplit(lectura).Trim();
                if (fechacap.Length == 0)
                {
                    string Embcap = ValidaEmb(lectura).Trim();
                    Mensajes mensa = new Mensajes { titulo = "Etiqueta No Pertenece a Orden", mensaje = "Error Etiqueta NO LEIDA PARA LA ORDEN A CANCELAR!! " + "\n\r" + mtip + " | " + mfol + " | " + mcod + " | " + mtar + " | " + mcaj + "\n\r" };
                    db.Insert(mensa);
                    //Borrado de Etiquetas capturadas
                    db.Query<xprod>("delete from[xprod] Where Tipo = '" + mtip + "' AND Folio = '" + mfol + "' AND Codigo = '" + mcod + "' AND Tarima = '" + mtar + "' AND Cajas = '" + mcaj + "'");
                    db.Query<ConPedidos>("UPDATE [ConPedidos] SET surtido = surtido + " + 1 + " WHERE prod_clave = '" + mcod.ToString().Trim() + "'");
                    EtiquetaCapturada = "N";
                    ok = "N";
                    er = "S";
                }

                string cadena = "";


                if (mtip == "PTC")
                    cadena = "SELECT TOP(1) ETIQUETA AS PROD,SURTIDO,FECHA_CAD AS FECCAD, (CASE fecha_cad WHEN '' THEN  FORMAT( DATEADD(day, 15, pti_fecha), 'dd/MM/yyyy', 'en-US' ) WHEN fecha_cad THEN fecha_cad END) AS fecha_cad FROM TB_DET_TRAZABILIDAD WHERE PROD_CLAVE = '" + mcod + "' AND RECIBO = '" + mfol + "' " +
                             "AND TIPO = '" + mtip + "' AND TARIMA = '" + Convert.ToInt32(mtar).ToString() + "' ";

                else
                    cadena = "SELECT TOP(1) NUM_CAJAS AS PROD, CAJAS_SUR AS SURTIDO,NUM_LOTE AS FECCAD, ISNULL(fechacad, FORMAT( DATEADD(day, 15, fecha), 'yyyyMMdd', 'en-US' )) AS fecha_cad FROM TB_DET_ETI_FINAL WHERE CVE_PROD = '" + mcod + "' AND FOLIO = '" + mfol + "' " +
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

                    Mensajes mensa = new Mensajes { titulo = "Etiqueta No Existe", mensaje = "Error Etiqueta No Existe!! " + "\n\r" + mtip + " | " + mfol + " | " + mcod + " | " + mtar + " | " + mcaj + "\n\r" + "Día " + fechacap + "\n\r" + nom + " Informe al supervisor, Retire la caja, Reetiquete y Leala nuevamente" };
                    db.Insert(mensa);

                    db.Query<xprod>("delete from[xprod] Where Tipo = '" + mtip + "' AND Folio = '" + mfol + "' AND Codigo = '" + mcod + "' AND Tarima = '" + mtar + "' AND Cajas = '" + mcaj + "'");
                    db.Query<ConPedidos>("UPDATE [ConPedidos] SET surtido = surtido + " + 1 + " WHERE prod_clave = '" + mcod.ToString().Trim() + "'");

                    er = "S";
                    continue;
                }

                System.String diacaducidad = "";
                System.String mescaducidad = "";

                foreach (DataRow row in Info.Rows)
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

                    /*if ((mS + cant) > mP)
                    {
                        ok = "N";
                        //LbxCap.SelectedIndex = i;
                        HayExistencias = "NE";
                        er = "S";

                        if (amtip != mtip || amfol != mfol || amcod != mcod || amtar != mtar)
                        {
                            Mensajes mensa = new Mensajes { titulo = "Tarima Surtida Completamente", mensaje = "La Cantidad a Surtir Supera Por " + ((mS + cant) - mP) + " Cajas Lo Producido, \n\r Produ: " + mP + " | Surt: " + mS + " | Leidos: " + cant + "\n\r" + mtip + " | " + mfol + " | " + mcod + " | " + mtar + " \n\r" + nom + " Limpie los datos y Reinicie El Escaneado" };
                            db.Insert(mensa);
                        }

                        AgregaFolioSinExistencia(mtip, mfol, mcod, nom, mtar, cant.ToString());
                    }*/
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
                    //Registra en la base de datos SQLite
                    db.Insert(consecutivo);


                    totok++;
                }
                amtip = mtip;
                amfol = mfol;
                amcod = mcod;
                amtar = mtar;
                conta++;

                if (er == "S")
                {
                    tot++;
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

        private string ValidaCaja(string cadena)
        {
            /*string Cadena = "Select fecha_cap From tb_Det_Etiqueta " +
                           "Where Eti_Lectura = '" + cadena + "' AND Estatus != 'C'";*/
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
                    cadena = "SELECT (num_cajas - cajas_sur) AS disponible, ISNULL(fechacad, FORMAT( DATEADD(day, 15, fecha), 'yyyyMMdd', 'en-US' )) AS fecha_cad, folio AS recibo, tarima FROM tb_det_eti_final Inner JOIN tb_mstr_ordenes_prod ON folio = ordp_folio WHERE cve_prod = '" + prod + "' AND estatus_sur != 'S' AND ordp_estatus != 'C' AND (num_cajas - cajas_sur) > 0 Order By fechacad";
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
                                    Mensajes mensa = new Mensajes { titulo = "Existe un folio anterior disponible", mensaje = "El recibo " + "\n\r" + capturado.recibosug.ToString().Trim() + " De la tarima  " + capturado.Tarima.Trim() + " Tiene  " + capturado.Cajasdis + " cajas disponibles del producto: " + captu.nombre.Trim() + " Con Fecha de Caducidad del" + capturado.fecrecsug };
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
            string ok = "S", nom = "";
            Surtidomayor = "S";

            var productoscapturadosx = db.Table<xprod>();

            var productoscapturados = db.Table<ConPedidos>();
            foreach (var captu in productoscapturados)
            {
                /*if (Convert.ToInt32(captu.pedido) > Convert.ToInt32(captu.surtido))
                {

                    Mensajes mensa = new Mensajes { titulo = "Error En el Producto", mensaje = "Producto " + captu.nombre.ToString() + "  Surtido es menor al Pedido" + "\n\r" + nom + "\n\r" + " Pedidos: " + captu.pedido.ToString() + "  Surtidos: " + captu.surtido.ToString() + " Favor de Limpiar E Iniciar La Captura Nuevamente" };
                    db.Insert(mensa);
                    Surtidomayor = "NR";

                    ok = "N";
                }*/
                /*if (Convert.ToInt32(captu.pedido) < Convert.ToInt32(captu.surtido))
                {
                    Mensajes mensa2 = new Mensajes { titulo = "Error En el Producto", mensaje = "Producto " + captu.nombre.ToString() + " Surtido es Mayor al Pedido " + "\n\r" + nom + "\n\r" + " Pedidos: " + captu.pedido.ToString() + "  Surtidos: " + captu.surtido.ToString() + " Favor de Leer Etiquetas de Este Producto" };
                    db.Insert(mensa2);

                    Surtidomayor = "NR";
                }*/
            }


            return ok;
        }

        void fnShowCustomAlertDialog()
        {
            //Inflate layout
            View view = LayoutInflater.Inflate(Resource.Layout.frmsupervisor, null);
            AlertDialog builder = new AlertDialog.Builder(this).Create();
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

                    AutoPed = "S";
                    Guardar.Enabled = true;
                    builder.Dismiss();
                }

            };
            builder.Show();
        }

        void fnShowCustomAlertDialogCancel()
        {
            #region MATERIAL DIALOG PERSONALIZADO
            RunOnUiThread(() =>
            {
                // Inflamos el layout personalizado
                View view = LayoutInflater.Inflate(Resource.Layout.frmsupervisor, null);

                // Creamos el diálogo usando MaterialAlertDialogBuilder
                var builder = new MaterialAlertDialogBuilder(this, Resource.Style.ThemeOverlay_Material3_MaterialAlertDialog);
                builder.SetView(view);
                builder.SetCancelable(false);

                var dialog = builder.Create();
                dialog.Show();

                // Referencias a los controles del layout
                TextView titulo = view.FindViewById<TextView>(Resource.Id.titleLogin);
                EditText password = view.FindViewById<EditText>(Resource.Id.txtPassword);
                Button buttonAceptar = view.FindViewById<Button>(Resource.Id.btnLoginLL);
                Button buttonClear = view.FindViewById<Button>(Resource.Id.btnClearLL);

                // Configuración inicial
                password.LongClickable = false;
                titulo.Text = "Autorización Folios Adelantados";

                // Botón Clear / Cancelar
                buttonClear.Click += delegate
                {
                    dialog.Dismiss();
                };

                // Botón Aceptar
                buttonAceptar.Click += delegate
                {
                    try
                    {
                        thisConnection.Open();
                        string cadena = "SELECT usuario, password FROM tb_Autoriza_OdeP WHERE password = @password AND clave = 'EM'";
                        using (SqlCommand cmd = new SqlCommand(cadena, thisConnection))
                        {
                            cmd.Parameters.AddWithValue("@password", password.Text.Trim());
                            var mAutoriza = Convert.ToString(cmd.ExecuteScalar()) ?? string.Empty;

                            if (string.IsNullOrEmpty(mAutoriza.Trim()))
                            {
                                Toast.MakeText(this, "PASSWORD INCORRECTO!!!", ToastLength.Short).Show();
                            }
                            else
                            {
                                Guardar.Enabled = true;
                                dialog.Dismiss();
                            }
                        }
                    }
                    catch (Java.Lang.Exception ex)
                    {
                        Toast.MakeText(this, $"Error: {ex.Message}", ToastLength.Long).Show();
                    }
                    finally
                    {
                        thisConnection.Close();
                    }
                };
            });
            #endregion

            #region ALERT DIALOG
            //Inflate layout
            /*View view = LayoutInflater.Inflate(Resource.Layout.frmsupervisor, null);
            AlertDialog builder = new AlertDialog.Builder(this).Create();
            builder.SetView(view);
            builder.SetCanceledOnTouchOutside(false);
            TextView titulo = view.FindViewById<TextView>(Resource.Id.titleLogin);
            EditText password = view.FindViewById<EditText>(Resource.Id.txtPassword);
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
            builder.Show();*/
            #endregion
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

            if (respaldo_activo == 1)
            {
                string horaactual = DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss");
                string pedido_actual = pedidoencaptura.Text.Trim().Replace("Pedido Actual: ", "");
                string numerosplit = nosplit.Text.Trim().Replace("Split Numero: ", "");
                string cadenainfomensaje = "";

                thisConnection.Open();
                try
                {


                    var relXprod = db.Table<xprod>();
                    foreach (var captu in relXprod)
                    {
                        string lectura = captu.Tipo + captu.Folio + captu.Codigo + captu.Tarima + captu.Cajas;


                        cadenainfomensaje = "insert into Tb_Etiqueta_Capturada_Validar(Fecha, emb_folio, fecha_cap, Eti_Lectura, Eti_Recibo, Eti_Producto, Eti_Caja, Eti_TarIni, Eti_TarFin, Cve_Camioneta, FecCap, Version, Imei, Split, veces) " +
                                       "Values ('" + horaactual + "', '" + pedido_actual + "','" + captu.fecha_captura + "','" + lectura + "', '" + captu.Folio + "', '" + captu.Codigo + "', '" + captu.Cajas + "', '" + captu.Tarima + "', '" + captu.Tarima + "', '', '" + captu.fecha_captura + "', '" + Version + "', '" + imei + "', '" + numerosplit + "', '" + veces + "')";
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
                     "And estatus != '' Group By prod_clave Order by prod_clave";
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
            var recibosatrasusa = db.Query<XLoteSug>("Select * FROM [XLoteSug] Where Cajasusadas != 0 Order By recibosug, cveprod ASC");
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
                    var recibosCapturados = db.Query<xLote>("Select * FROM [xLoteFinal] Where Codigo = '" + recibos.cveprod.ToString().Trim() + "' Order By Pedido, codigo ASC");
                    foreach (var reccapturado in recibosCapturados)
                    {
                        if (Convert.ToInt32(reccapturado.Cajas) > 0)
                        {
                            string cadena = "insert into tb_det_folio_adelantado (responsable, fecha, emb_folio, recibo_cap, fecreccap, recibo_sug, fecrecsug, prod_clave, producto, cantidad, autorizo, tarimacap, tarimasug) " +
                                "Values('" + responsable + "','" + DateTime.Now.ToString("dd/MM/yyyy") + "','" + reccapturado.Pedido + "', '" + reccapturado.Folio + "', '" + reccapturado.diacad + "/" + reccapturado.mescad + "','" + recibos.recibosug + "', '" + recibos.fecrecsug + "', '" + recibos.cveprod + "', '" + reccapturado.nombre + "', '" + reccapturado.Cajas + "', '" + mAutoriza.Trim() + "', '" + reccapturado.Tarima.Trim() + "', '" + recibos.Tarima.Trim() + "')";
                            SqlCommand cmd = new SqlCommand(cadena, thisConnection);
                            cmd.ExecuteNonQuery();
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

        public void Foliocaptura_KeyPress(object sender, View.KeyEventArgs e)
        {
            // Detectar cuando se presiona la tecla Enter
            if (e.Event.Action == KeyEventActions.Down && e.KeyCode == Keycode.Enter)
            {
                // Validar si está en modo concentrado
                if (mconcen == "2")
                {
                    #region MATERIAL DIALOG
                    RunOnUiThread(() =>
                    {
                        // Construimos el título con color y negritas
                        var titleSpannable = new SpannableStringBuilder("Modo Concentrado Activado");
                        titleSpannable.SetSpan(new ForegroundColorSpan(Color.ParseColor("#DC3545")), 0, titleSpannable.Length(), SpanTypes.ExclusiveExclusive);
                        titleSpannable.SetSpan(new StyleSpan(TypefaceStyle.Bold), 0, titleSpannable.Length(), SpanTypes.ExclusiveExclusive);

                        // Construimos el mensaje
                        var mensajeSpannable = new SpannableStringBuilder();
                        mensajeSpannable.Append("Está consultando el ");
                        int startConcen = mensajeSpannable.Length();
                        mensajeSpannable.Append("concentrado");
                        mensajeSpannable.SetSpan(new StyleSpan(TypefaceStyle.Bold), startConcen, mensajeSpannable.Length(), SpanTypes.ExclusiveExclusive);
                        mensajeSpannable.Append(", no se puede capturar código.");
                        mensajeSpannable.SetSpan(new ForegroundColorSpan(Color.ParseColor("#5F6368")), 0, mensajeSpannable.Length(), SpanTypes.ExclusiveExclusive);

                        // Crear el diálogo con Material Design 3
                        var builder = new MaterialAlertDialogBuilder(this, Resource.Style.ThemeOverlay_Material3_MaterialAlertDialog);
                        builder.SetTitle(titleSpannable);
                        builder.SetIcon(Resource.Drawable.no);
                        builder.SetMessage(mensajeSpannable);
                        builder.SetCancelable(false);

                        // Botón principal
                        builder.SetPositiveButton("Entendido", (s, ev) =>
                        {
                            foliocaptura.SetSelection(0, foliocaptura.Text.Length);
                            foliocaptura.RequestFocus();
                            valorfinal = foliocaptura.Text;
                        });

                        var dialog = builder.Create();
                        dialog.Show();

                        // Personalizamos el botón
                        dialog.Window.DecorView.Post(() =>
                        {
                            var positiveButton = dialog.GetButton((int)DialogButtonType.Positive);
                            positiveButton?.SetTextColor(Color.ParseColor("#DC3545"));
                            positiveButton?.SetAllCaps(false);
                        });
                    });
                    #endregion

                    e.Handled = true; // Evita que el Enter inserte salto de línea
                    return;
                }

                // Si no está en modo concentrado, procesar el folio normalmente
                string folio = foliocaptura.Text;
                Guardar.Enabled = false;

                if (folio != valorfinal && !string.IsNullOrEmpty(folio))
                {
                    etiquetablanca();
                }

                e.Handled = true; // Evita que el Enter haga un salto o cambie el foco
            }
        }


        public void OnTextChanged(Java.Lang.ICharSequence s, int start, int before, int count)
        {
            /*if (mconcen == "2")
            {
                #region MATERIAL DIALOG
                RunOnUiThread(() =>
                {
                    var alertDialog = new AndroidX.AppCompat.App.AlertDialog.Builder(this);

                    // Título en rojo para advertencia
                    alertDialog.SetTitle(Html.FromHtml(
                        "<font color='#DC3545'><b>Modo concentrado Activado</b></font>",
                        FromHtmlOptions.ModeLegacy
                    ));

                    alertDialog.SetIcon(Resource.Drawable.no);

                    // Mensaje en blanco sobre fondo oscuro
                    alertDialog.SetMessage(Html.FromHtml(
                        "<font color='#FFFFFF'>Está consultando el concentrado, no se puede capturar código.</font>",
                        FromHtmlOptions.ModeLegacy
                    ));

                    alertDialog.SetCancelable(false);

                    // Botón principal
                    alertDialog.SetPositiveButton(Html.FromHtml(
                        "<font color='#DC3545'><b>OK</b></font>",
                        FromHtmlOptions.ModeLegacy
                    ), delegate
                    {
                        alertDialog.Dispose();

                        // Selecciona todo el texto y enfoca el campo
                        foliocaptura.SetSelection(0, foliocaptura.Text.Length);
                        foliocaptura.RequestFocus();

                        valorfinal = foliocaptura.Text;
                    });

                    var dialog = alertDialog.Create();
                    dialog.Show();

                    // Personalización del botón (color y estilo)
                    var btn = dialog.GetButton((int)Android.Content.DialogButtonType.Positive);
                    btn?.SetTextColor(Android.Graphics.Color.ParseColor("#DC3545"));
                    btn?.SetAllCaps(false);
                });
                #endregion

                return;
                #region ALERT DIALOG
                /*Android.App.AlertDialog.Builder alertDialog = new Android.App.AlertDialog.Builder(this);
                alertDialog.SetTitle(Html.FromHtml("<font color='#dc3545' size = 10>Modo concentrado Activado</font>"));
                alertDialog.SetIcon(Resource.Drawable.no);
                alertDialog.SetMessage(Html.FromHtml("<font color='#FFFFFF' size = 10>Esta consultando el concentrado no se puede capturar codigo. </font>"));
                alertDialog.SetCancelable(false);
                alertDialog.SetNeutralButton("Ok", delegate
                {
                    alertDialog.Dispose();
                    foliocaptura.SetSelection(0, foliocaptura.Text.Length);
                    foliocaptura.RequestFocus();
                    valorfinal = foliocaptura.Text;
                });
                alertDialog.Show();
                return;
                #endregion
            } // esta consultando el concentrado no se puede capturar codigo

            string folio = foliocaptura.Text;
            Guardar.Enabled = false;

            if (folio != valorfinal && folio != "")
            {
                //var TxtCod = (EditText)sender;
                etiquetablanca();
            }*/
        }



        public void etiquetablanca()
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
            /*string restocaptura = foliocaptura.Text.Replace(mfol, "").Replace(mcod, "");
            if (restocaptura.Length == 6)
            {
                if (mfol.Length == 5)
                {
                    mtip = "PTC";
                }
                mcaj = restocaptura.Substring(3, 3);
                mtar = restocaptura.Substring(0, 3);
            }
            else
            {
                mtip = "PTC";
                mcaj = restocaptura.Substring(4, 3);
                mtar = restocaptura.Substring(0, 2);
            }*/
            string restocaptura = foliocaptura.Text.Trim().Replace(mfol, "").Replace(mcod, "");
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





            /*if (tam > 20) //Etiqueta de Campo que no es Aguilares y Proceso Planta
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
            if (repetido(mtip, mfol, mcod, mtar, mcaj) != "S")
            {
                string lectura2 = mtip + mfol + mcod + mtar + mcaj;
                lectura2 = lectura2.Trim();

                try
                {
                    xprod Pedidoscapturados = new xprod { Tipo = mtip, Folio = mfol, Codigo = mcod, Tarima = mtar, Cajas = mcaj, fecha_captura = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"), Lecturabd = lectura2 };
                    db.Insert(Pedidoscapturados);

                    int totalx = traetotal(mcod);

                    totalx = totalx - 1;

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
            foliocaptura.SetSelection(0, foliocaptura.Text.Length);
            foliocaptura.RequestFocus();
            valorfinal = foliocaptura.Text;



            List<FlimStarInfo> lstFlimStar = listItem;
            var gvObject = FindViewById<GridView>(Resource.Id.gvCtrCanceladoParcial);
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

                string lectura2 = mtip + mfol + mcod + mtar + mcaj;
                lectura2 = lectura2.Trim();

                try
                {
                    xprod Pedidoscapturados = new xprod { Tipo = mtip, Folio = mfol, Codigo = mcod, Tarima = mtar, Cajas = mcaj, fecha_captura = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"), Lecturabd = lectura2 };
                    db.Insert(Pedidoscapturados);

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

                    if (totalx < 1)
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
                catch
                {
                    Toast.MakeText(this, "Duplicidad Evitada", ToastLength.Short).Show();
                }


            }
            foliocaptura.SetSelection(0, foliocaptura.Text.Length);
            foliocaptura.RequestFocus();
            valorfinal = foliocaptura.Text;



            List<FlimStarInfo> lstFlimStar = listItem;
            var gvObject = FindViewById<GridView>(Resource.Id.gvCtrCanceladoParcial);
            gvObject.Adapter = new myGVItemAdapter(this, lstFlimStar);
        }

        public void etiquetaverde()
        {
            int tam = foliocaptura.Text.Length;
            string mcaj = "", mtar = "", mcod = "", mfol = "", mtip = "", Ent = "N";
            /*if (foliocaptura.Text.Trim().Contains(" ") == true)
            {
                if (tam < 18)
                {
                    mtar = foliocaptura.Text.Substring(tam - 3, 3);
                    mfol = foliocaptura.Text.Substring(0, 4);
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
                string mtari = foliocaptura.Text.Substring(tam - 4, 4);
                mtar = foliocaptura.Text.Substring(tam - 4, 2);
                mfol = foliocaptura.Text.Substring(0, 6);
                mcod = foliocaptura.Text.Replace(mfol, "");
                mcod = mcod.Replace(mtari, "");
                mtip = "PTC";

            }*/
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
                disponible++;
                int n = 1;
                while (n < disponible)
                {
                    if (n.ToString().Length == 1)
                    {
                        mcaj = "00" + n.ToString();
                    }
                    else
                    {
                        mcaj = "0" + n.ToString();
                    }

                    string lectura = mtip + mfol + mcod + mtar + mcaj;
                    thisConnection.Open();
                    string fechacap = ValidaCaja(lectura).Trim();
                    string fechacappre = ValidaCajaPreesplit(lectura).Trim();
                    thisConnection.Close();
                    if (fechacap.Length > 0)
                    {
                        n++;
                    }
                    else if (fechacappre.Length > 0)
                    {
                        n++;
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
                                xprod Pedidoscapturados = new xprod { Tipo = mtip, Folio = mfol, Codigo = mcod, Tarima = mtar, Cajas = mcaj, fecha_captura = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"), Lecturabd = lectura2 };
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
                        foliocaptura.SetSelection(0, foliocaptura.Text.Length);
                        foliocaptura.RequestFocus();
                        valorfinal = foliocaptura.Text;

                        n++;
                    }



                }


            }
            else
            {
                foliocaptura.SetSelection(0, foliocaptura.Text.Length);
                foliocaptura.RequestFocus();
                valorfinal = foliocaptura.Text;
            }

            List<FlimStarInfo> lstFlimStar = listItem;
            var gvObject = FindViewById<GridView>(Resource.Id.gvCtrCanceladoParcial);
            gvObject.Adapter = new myGVItemAdapter(this, lstFlimStar);
        }

        public void AfterTextChanged(IEditable s)
        {

        }

        public void BeforeTextChanged(ICharSequence s, int start, int count, int after)
        {

        }
    }
}