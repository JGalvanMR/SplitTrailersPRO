using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Net.Wifi;
using Android.OS;
using Android.Support.V4.App;
using Android.Support.V4.Content;
using Android.Telephony;
using Android.Text;
using Android.Text.Style;
using Android.Widget;
using AndroidX.AppCompat.App;
using Google.Android.Material.AppBar;
using Google.Android.Material.Dialog;
using Google.Android.Material.Internal;
using Google.Android.Material.TextField;
using Java.IO;
using Java.Lang;
using Java.Net;
using Java.Util;
using Org.Json;
using Plugin.DeviceInfo;
using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Net;
using System.Threading;

namespace SplitTrailers
{
    [Activity(Label = "SplitTrailers", Theme = "@style/AppTheme", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        //public static string cadenaConexion = "Persist Security Info=False;user id=sa; password=Gabira2026$;Initial Catalog =GAB_Irapuato_Prueba; server=tcp:192.168.123.6,1433; Connect Timeout = 130";
        //public static string cadenaConexion = "Persist Security Info=False;user id=sa; password=Gabira2026$;Initial Catalog =GAB_Irapuato; server=tcp:189.206.160.206,2352; MultipleActiveResultSets=true; Connect Timeout = 130";
        public static string cadenaConexion = "Persist Security Info=False;user id=sa; password=Gabira2026$;Initial Catalog =GAB_Irapuato; server=tcp:192.168.123.6,1433; MultipleActiveResultSets=true; Connect Timeout = 130";

        public static Int32 foliocampo = 0;

        public static string veh = "";
        public static int captura = 0;
        SqlCommand cmnd = new SqlCommand();
        SqlDataReader reader;
        SqlCommand cmnd1 = new SqlCommand();
        SqlDataReader reader1;
        System.String[] strFrutas;
        ArrayAdapter<System.String> comboAdapter;
        SqlDataAdapter da;
        SqlDataAdapter da1;
        public static DataTable camionetas = new DataTable("camionetas");
        public static DataTable responsables = new DataTable("responsables");
        public static DataTable vehiculos = new DataTable("vehiculos");
        public static DataTable version = new DataTable("version");
        public static DataTable formulario = new DataTable("formulario");
        string query = "";

        public static DataTable Pedidostotales = new DataTable("formulario");

        DataSet ds = new DataSet();
        DataSet ds1 = new DataSet();
        public static string vehiculo = "";
        public static string responsablesplit = "";
        public static string imei = "";
        public static string ip = "";
        SqlConnection thisConnection;

        TextView versionapp;


        //Variables del servicio Web
        Context context;
        Runnable listener;
        //private static string INFO_FILE = "http://mrlucky.com.mx/ventasnew/SplitTrailer/version.txt";
        private static string INFO_FILE = "http://192.168.123.4:81/EmbarquesApk/APK_SplitTrailers/version.txt";
        private int currentVersionCode;
        private string currentVersionName;
        private int latestVersionCode;
        private string latestVersionName;
        private string downloadURL;




        protected override void OnCreate(Bundle savedInstanceState)
        {

            StrictMode.ThreadPolicy policy = new StrictMode.ThreadPolicy.Builder().PermitAll().Build();
            StrictMode.SetThreadPolicy(policy);


            base.OnCreate(savedInstanceState);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);


            Button log = FindViewById<Button>(Resource.Id.btnlogin);
            log.Click += Btnlogin_Click;

            thisConnection = new SqlConnection(cadenaConexion);
            /*aGREGAR COLUMNAS AL DATATABLE
            Pedidostotales.Columns.Add("PEDIDO", typeof(string));     //0
            Pedidostotales.Columns.Add("ESTADO", typeof(string));      //1
            Pedidostotales.Columns.Add("NOMUSUARIO", typeof(string));       //2
            Pedidostotales.Columns.Add("SPLIT", typeof(string));    //3
            //aGREGAR COLUMNAS AL DATATABLE
            thisConnection.Open();
            //Buscar el numero del que parte si es campo o proceso
            cmnd = thisConnection.CreateCommand();
            query = "SELECT GETDATE()";
            SqlCommand cmd = new SqlCommand(query, thisConnection);
            string FECHAEmb = Convert.ToDateTime(cmd.ExecuteScalar()).ToString("dd/MM/yyyy");

            query = "SELECT A.PDN_FOLIO, (CASE WHEN MT.no_trailer is not null  OR MT.no_trailer != '' THEN 'AMARILLO' WHEN A.pdn_estatus = ' ' THEN 'ROJO'  ELSE 'AZUL' END) as ESTADOPDN, nom_usu FROM TB_MSTR_PEDIDOS_NAL A LEFT JOIN tb_mstr_trailer MT On  A.placacaja = MT.no_trailer AND fecha = '"+ FECHAEmb + "'  LEFT JOIN tb_det_acceso_celulares ON folio = A.PDN_FOLIO AND estado = 'A' WHERE PDN_FECHA = '" + FECHAEmb + "' AND PDN_SURTIDO = ' ' AND PDN_ESTATUS <> 'C' AND CNTE_CLAVE <> 'AJUST' AND CNTE_CLAVE <> 'BASUR' AND CNTE_CLAVE <> 'PERDI' AND CNTE_CLAVE <> 'VMEN1' OR (PDN_TIPO = 'TRA' AND PDN_SITUACION <> 'MAQ') AND A.pdn_surtido = ''ORDER BY estadopdn";
            cmd = new SqlCommand(query);
            cmd.Connection = thisConnection;
            SqlDataReader Info;
            Info = cmd.ExecuteReader();
            while (Info.Read())
            {

                //Int32 Tot = Fn_Tot_Cajas(Info["PDN_FOLIO"].ToString());
                //Int32 TotS = Fn_Tot_Surtido(Info["PDN_FOLIO"].ToString());
                //Int32 TotS = Fn_Tot_Surtido(Info["PDN_FOLIO"].ToString());

                string querytwo = "SELECT SUM ((AA.PDN_NUM_UNIDADES/BB.PROD_NUM_TARIMAS) - FLOOR((AA.PDN_NUM_UNIDADES/BB.PROD_NUM_TARIMAS))) FROM TB_DET_PEDIDOS AA, TB_CAT_PRODUCTO BB WHERE AA.PDN_FOLIO =  '"+Info["PDN_FOLIO"].ToString().Trim()+"' AND AA.PROD_CLAVE = BB.PROD_CLAVE";
                SqlCommand cmdx = new SqlCommand(querytwo, thisConnection);
                string splitgen = Convert.ToDecimal(cmdx.ExecuteScalar()).ToString();
                if (Convert.ToDecimal(splitgen) > 1)
                Pedidostotales.Rows.Add(Info["PDN_FOLIO"].ToString().Trim(), Info["ESTADOPDN"].ToString().Trim(), Info["nom_usu"].ToString().Trim(), splitgen);
            }
            thisConnection.Close();

            int rows = Pedidostotales.Rows.Count;*/

            thisConnection.Open();
            cmnd = thisConnection.CreateCommand();
            cmnd.CommandText = "select inicio_campo from Tb_folio_campo";
            foliocampo = Convert.ToInt32(cmnd.ExecuteScalar());
            ds.Clear();
            thisConnection.Close();


            thisConnection.Open();
            //Buscar el numero del que parte si es campo o proceso
            cmnd = thisConnection.CreateCommand();
            //LLenado de datatable 1




            //Llenado Spinner 2

            query = "SELECT cve_capsplit, nom_capsplit, cve_cancel FROM TB_RESPON_SPLIT WHERE status = 'A' ORDER BY NOM_CAPSPLIT";
            da = new SqlDataAdapter(query, thisConnection);
            da.Fill(ds, "responsables");
            responsables = ds.Tables["responsables"];
            thisConnection.Close();

            Spinner spinner2 = FindViewById<Spinner>(Resource.Id.spinner2);
            System.Collections.ArrayList listaFrutas2 = new System.Collections.ArrayList();

            strFrutas = new System.String[responsables.Rows.Count + 1];
            strFrutas[0] = "Seleccione un Responsable";
            for (int i = 1; i <= responsables.Rows.Count; i++)
            {
                int x = i - 1;
                strFrutas[i] = responsables.Rows[x]["nom_capsplit"].ToString();
            }


            Collections.AddAll(listaFrutas2, strFrutas);
            comboAdapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleSpinnerItem, strFrutas);
            spinner2.Adapter = comboAdapter;
            spinner2.ItemSelected += new EventHandler<AdapterView.ItemSelectedEventArgs>(spinner_ItemSelected2);


            //Inicio de Validacion de Actualizacion *******************************************************************
            try
            {
                getData();
            }
            catch
            {

            }

            versionapp = FindViewById<TextView>(Resource.Id.versionapp);
            versionapp.Text = "Split Trailers - Versión: " + currentVersionName;
            if (isNewVersionAvailable())
            {
                //Crea mensaje con datos de versión.
                string msj = "Nueva Version: " + isNewVersionAvailable();
                msj += "\nActual Version: " + currentVersionName + "(" + currentVersionCode + ")";
                msj += "\nUltima Version: " + latestVersionName + "(" + latestVersionCode + ")";
                msj += "\nDesea Actualizar?";
                //Crea ventana de alerta.

                #region MATERIAL DIALOG
                // 🔹 Construimos el título con color rojo y negritas
                var titleSpannable = new SpannableStringBuilder("Actualización Disponible");
                titleSpannable.SetSpan(new ForegroundColorSpan(Color.ParseColor("#DC3545")), 0, titleSpannable.Length(), SpanTypes.ExclusiveExclusive);
                titleSpannable.SetSpan(new StyleSpan(TypefaceStyle.Bold), 0, titleSpannable.Length(), SpanTypes.ExclusiveExclusive);

                // 🔹 Construimos el mensaje con color negro
                var mensajeSpannable = new SpannableStringBuilder(msj);
                mensajeSpannable.SetSpan(new ForegroundColorSpan(Color.ParseColor("#000000")), 0, mensajeSpannable.Length(), SpanTypes.ExclusiveExclusive);

                // 🔹 Creamos el diálogo usando el ThemeOverlay oficial de Material3
                var builder = new MaterialAlertDialogBuilder(this, Resource.Style.ThemeOverlay_Material3_MaterialAlertDialog);
                builder.SetTitle(titleSpannable);
                builder.SetIcon(Resource.Drawable.update);
                builder.SetMessage(mensajeSpannable);
                builder.SetCancelable(false);

                // 🔹 Botones Positivo y Negativo
                builder.SetPositiveButton("Sí", SaveAction);
                builder.SetNegativeButton("No", CancelaAction);

                // 🔹 Crear y mostrar el diálogo
                var dialog = builder.Create();
                dialog.Show();

                // 🔹 Personalizamos los botones después de mostrarlo
                dialog.Window.DecorView.Post(() =>
                {
                    var positiveButton = dialog.GetButton((int)DialogButtonType.Positive);
                    var negativeButton = dialog.GetButton((int)DialogButtonType.Negative);

                    positiveButton?.SetTextColor(Color.ParseColor("#DC3545")); // Rojo Material
                    positiveButton?.SetAllCaps(false);

                    negativeButton?.SetTextColor(Color.ParseColor("#DC3545")); // Rojo Material
                    negativeButton?.SetAllCaps(false);
                });
                #endregion


                //Muestra la ventana esperando respuesta.

            }


            //Validar Hora actual del sistema vs Hora del servidor
            thisConnection.Open();
            cmnd = thisConnection.CreateCommand();
            cmnd.CommandText = "select GETDATE()";
            DateTime horaservidor = Convert.ToDateTime(cmnd.ExecuteScalar());
            thisConnection.Close();

            TimeSpan span = horaservidor.Subtract(DateTime.Now);
            int totalhoras = (span.Days * 24) + span.Hours;

            if (totalhoras != 0)
            {
                #region MATERIAL DIALOG
                // 🔹 Construimos el título con color amarillo claro y negritas
                var titleSpannable = new SpannableStringBuilder("DIFERENCIA EN FECHAS/HORAS");
                titleSpannable.SetSpan(new ForegroundColorSpan(Color.ParseColor("#E5FA7A")), 0, titleSpannable.Length(), SpanTypes.ExclusiveExclusive);
                titleSpannable.SetSpan(new StyleSpan(TypefaceStyle.Bold), 0, titleSpannable.Length(), SpanTypes.ExclusiveExclusive);

                // 🔹 Construimos el mensaje con color amarillo y negritas
                var mensajeSpannable = new SpannableStringBuilder("La fecha/Hora de la lectora es muy diferente a la fecha/Hora del servidor, no se puede utilizar el sistema hasta coincidir las horas");
                mensajeSpannable.SetSpan(new ForegroundColorSpan(Color.ParseColor("#F6F87A")), 0, mensajeSpannable.Length(), SpanTypes.ExclusiveExclusive);
                mensajeSpannable.SetSpan(new StyleSpan(TypefaceStyle.Bold), 0, mensajeSpannable.Length(), SpanTypes.ExclusiveExclusive);

                // 🔹 Creamos el diálogo usando el ThemeOverlay oficial de Material3
                var builder = new MaterialAlertDialogBuilder(this, Resource.Style.ThemeOverlay_Material3_MaterialAlertDialog);
                builder.SetTitle(titleSpannable);
                builder.SetIcon(Resource.Drawable.no);
                builder.SetMessage(mensajeSpannable);
                builder.SetCancelable(false);

                // 🔹 Botón principal
                builder.SetPositiveButton("Entendido", (s, e) => { Finish(); });

                // 🔹 Crear y mostrar el diálogo
                var dialog = builder.Create();
                dialog.Show();

                // 🔹 Personalizamos el botón después de mostrarlo
                dialog.Window.DecorView.Post(() =>
                {
                    var positiveButton = dialog.GetButton((int)DialogButtonType.Positive);
                    positiveButton?.SetTextColor(Color.ParseColor("#FFA000")); // Naranja Material
                    positiveButton?.SetAllCaps(false);
                });
                #endregion

            }


            //Termino de Validacion de Actualizacion*********************************************************************************************************

            // Referencia al MaterialToolbar
            MaterialToolbar toolbar = FindViewById<MaterialToolbar>(Resource.Id.toolbar);
            // Asigna como ActionBar usando SupportActionBar
            SetSupportActionBar(toolbar);

            // Ahora SupportActionBar no es null
            SupportActionBar.Title = "Split Trailers";
            SupportActionBar.SetDisplayHomeAsUpEnabled(false); // si quieres back button

        }

        private void CancelaAction(object sender, DialogClickEventArgs e)
        {
            Finish();
        }

        public Int32 Fn_Tot_Cajas(string var_folio)
        {
            Int32 Tot = 0;
            string Cadena = "";
            Cadena = "SELECT SUM(PDN_NUM_UNIDADES) FROM TB_DET_PEDIDOS WHERE PDN_FOLIO = '" + var_folio + "' AND PDN_TIPO = 'NAL'";

            DataSet ds = new DataSet();
            SqlDataAdapter da = new SqlDataAdapter(Cadena, thisConnection);
            SqlCommand cmd;
            cmd = new SqlCommand(Cadena);
            cmd.Connection = thisConnection;
            SqlDataReader DetPed;
            DetPed = cmd.ExecuteReader();
            while (DetPed.Read())
            {
                if (!string.IsNullOrEmpty(DetPed[0].ToString()))
                    Tot = Convert.ToInt32(DetPed[0]);
            }
            //if (var_tipo == "NAL")
            //    foreach (DataRow row1 in Det_PedNal.Select("PDN_FOLIO = '" + var_folio + "' AND PDN_TIPO = '" + var_tipo + "'"))
            //        Tot = Tot + Convert.ToInt32(row1["PDN_NUM_UNIDADES"]);
            //else
            //    foreach (DataRow row1 in Det_PedExp.Select("PDN_FOLIO = '" + var_folio + "' AND PDN_TIPO = '" + var_tipo + "'"))
            //        Tot = Tot + Convert.ToInt32(row1["PDN_NUM_UNIDADES"]);
            return Tot;
        }

        public Int32 Fn_Tot_Surtido(string var_folio)
        {
            Int32 Tot = 0;
            string Cadena = "";
            Cadena = "SELECT EMB_FOLIO, CANT_SUR FROM TB_PED_EMBARQUE WHERE EMB_FOLIO = '" + var_folio + "'";
            DataSet ds = new DataSet();
            SqlDataAdapter da = new SqlDataAdapter(Cadena, thisConnection); //DBGAB
            SqlCommand cmd;
            cmd = new SqlCommand(Cadena);
            cmd.Connection = thisConnection;
            SqlDataReader DetPed;
            DetPed = cmd.ExecuteReader();
            while (DetPed.Read())
            {
                Tot = Tot + Convert.ToInt32(DetPed[1]);
                string Ped = DetPed[0].ToString();
            }
            return Tot;
        }

        private void SaveAction(object sender, DialogClickEventArgs e)
        {
            downloadApp();
        }

        /*void Btnlogin_Click(object sender, EventArgs e)
        {
            if (responsablesplit == "Seleccione un Responsable")
            {
                Toast.MakeText(this, "Por favor, asegurese de seleccionar un responsable y volver a intentarlo", ToastLength.Long).Show();
                return;
            }

            if (vehiculo == "Seleccione un vehiculo")
            {
                Toast.MakeText(this, "Por favor, asegurese de seleccionar un vehiculo y volver a intentarlo", ToastLength.Long).Show();
                return;
            }

            EditText pass = FindViewById<EditText>(Resource.Id.password);

            if (pass.Text.Length == 0) {
                Toast.MakeText(this, "Por favor, asegurese de ingresar una contraseña y volver intentarlo", ToastLength.Long).Show();
                return;
            }


            var responsable = "";
            if (responsables.Rows.Count != 0)
            {
                for (int i = 0; i < responsables.Rows.Count; i++)
                {
                    if ((responsables.Rows[i]["nom_capsplit"].ToString() == responsablesplit) && (responsables.Rows[i]["cve_cancel"].ToString() == pass.Text.ToString().Trim()))
                    {
                        responsable = responsables.Rows[i]["cve_capsplit"].ToString();
                    }
                }
            }
            else
            {
                Toast.MakeText(this, "Por favor, Seleccione un responsable", ToastLength.Long).Show();
                return;
            }

            if (responsable.Trim().Length > 0)
            {
                //obtener Ip del telefono
                WifiManager wifiManager = (WifiManager)this.GetSystemService(Service.WifiService);
                ip = GetIPAddress();
                //obtener Imei del telefono
                Android.Telephony.TelephonyManager mTelephonyMgr;
                mTelephonyMgr = (Android.Telephony.TelephonyManager)GetSystemService(TelephonyService);
                imei = mTelephonyMgr.DeviceId;
                //Termina obtener datos



                //Registro de Ingreso Al Sistema.
                thisConnection.Open();
                string cadena = "INSERT INTO TB_REGISTRO_MOVIMIENTOS(FECHA,NOM_COMPU,NOM_USU,TIPO_MOV,OP_CLAVE,FOLIO,DETALLE,SISTEMA,MOV_FOLIO) " +
                            "VALUES('" + System.DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + "','CEL " + imei + "','"+ responsablesplit + "','E','" + ip + "','','Ingreso a sistema SPLIT TRAILER Imei: " + imei + ", Ip: " + ip + " ','SPLITTRAIL','')";
                SqlCommand cmd = new SqlCommand(cadena, thisConnection);
                cmd.ExecuteNonQuery();
                thisConnection.Close();

                Intent intent = new Intent(this, typeof(SolicitarPed));
                intent.PutExtra("cvresponsable", responsable.ToString());
                intent.PutExtra("responsable", responsablesplit.ToString());
                StartActivity(intent);
                Finish();
            }
            else {

                Toast.MakeText(this, "Contraseña Invalida para este usuario", ToastLength.Long).Show();
                return;
            }

            //******************************************************
        }*/

        void Btnlogin_Click(object sender, EventArgs e)
        {
            if (responsablesplit == "Seleccione un Responsable")
            {
                Toast.MakeText(this, "Por favor, asegurese de seleccionar un responsable y volver a intentarlo", ToastLength.Long).Show();
                return;
            }

            if (vehiculo == "Seleccione un vehiculo")
            {
                Toast.MakeText(this, "Por favor, asegurese de seleccionar un vehiculo y volver a intentarlo", ToastLength.Long).Show();
                return;
            }

            EditText pass = FindViewById<EditText>(Resource.Id.password);

            if (pass.Text.Length == 0)
            {
                Toast.MakeText(this, "Por favor, asegurese de ingresar una contraseña y volver intentarlo", ToastLength.Long).Show();
                return;
            }


            var responsable = "";
            if (responsables.Rows.Count != 0)
            {
                for (int i = 0; i < responsables.Rows.Count; i++)
                {
                    if ((responsables.Rows[i]["nom_capsplit"].ToString() == responsablesplit) && (responsables.Rows[i]["cve_cancel"].ToString() == pass.Text.ToString().Trim()))
                    {
                        responsable = responsables.Rows[i]["cve_capsplit"].ToString();
                    }
                }
            }
            else
            {
                Toast.MakeText(this, "Por favor, Seleccione un responsable", ToastLength.Long).Show();
                return;
            }

            if (responsable.Trim().Length > 0)
            {
                //obtener Ip del telefono
                WifiManager wifiManager = (WifiManager)this.GetSystemService(Service.WifiService);
                ip = GetIPAddress();
                //obtener Imei del telefono
                imei = GetDeviceID();
                //Termina obtener datos

                //Valido inicio de sesion activa********************************************************************************
                thisConnection.Open();
                string Cadena = "Select imei from tb_det_acceso_celulares where nom_usu = '" + responsablesplit.Trim() + "' AND folio = '' AND estado = 'A'";
                //string Cadena = "Select imei from tb_det_acceso_celulares where nom_usu = '" + responsablesplit.Trim() + "' AND folio = ''";
                SqlCommand cmdx = new SqlCommand(Cadena, thisConnection);
                string valor = Convert.ToString(cmdx.ExecuteScalar());
                thisConnection.Close();


                if (valor.Trim().Length > 0)
                {
                    if (valor == imei)
                    {
                        thisConnection.Open();
                        string cadena = "INSERT INTO TB_REGISTRO_MOVIMIENTOS(FECHA,NOM_COMPU,NOM_USU,TIPO_MOV,OP_CLAVE,FOLIO,DETALLE,SISTEMA,MOV_FOLIO) " +
                                    "VALUES('" + System.DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + "','CEL " + imei + "','" + responsablesplit + "','E','" + ip + "','','Ingreso a sistema SPLIT TRAILER Imei: " + imei + ", Ip: " + ip + " ','SPLITTRAIL','')";
                        SqlCommand cmd = new SqlCommand(cadena, thisConnection);
                        cmd.ExecuteNonQuery();
                        thisConnection.Close();

                        Intent intent = new Intent(this, typeof(SolicitarPed));
                        intent.PutExtra("cvresponsable", responsable.ToString());
                        intent.PutExtra("responsable", responsablesplit.ToString());
                        intent.PutExtra("currentVersionName", currentVersionName.ToString().Trim());
                        intent.PutExtra("imei", imei.ToString().Trim());
                        StartActivity(intent);
                        Finish();
                    }
                    else
                    {
                        #region MATERIAL DIALOG
                        // 🔹 Construimos el título con color amarillo y negritas
                        var titleSpannable = new SpannableStringBuilder("Sesión Activa en otro Equipo");
                        titleSpannable.SetSpan(new ForegroundColorSpan(Color.ParseColor("#FCEC70")), 0, titleSpannable.Length(), SpanTypes.ExclusiveExclusive);
                        titleSpannable.SetSpan(new StyleSpan(TypefaceStyle.Bold), 0, titleSpannable.Length(), SpanTypes.ExclusiveExclusive);

                        // 🔹 Construimos el mensaje con color azul claro
                        var mensajeSpannable = new SpannableStringBuilder("No puede iniciar otra sesión, debido a que hay un equipo con su sesión activa, favor de cerrar su sesión anterior e intentarlo de nuevo");
                        mensajeSpannable.SetSpan(new ForegroundColorSpan(Color.ParseColor("#E0F1FA")), 0, mensajeSpannable.Length(), SpanTypes.ExclusiveExclusive);

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
                            positiveButton?.SetTextColor(Color.ParseColor("#FFA000")); // Naranja Material
                            positiveButton?.SetAllCaps(false);
                        });
                        #endregion

                    }
                }
                else
                {

                    thisConnection.Open();
                    Cadena = "Select nom_usu from tb_det_acceso_celulares where imei = '" + imei.Trim() + "' AND Folio = '' AND estado = 'A'";
                    cmdx = new SqlCommand(Cadena, thisConnection);
                    string nombre = Convert.ToString(cmdx.ExecuteScalar());
                    thisConnection.Close();

                    if (nombre.Trim().Length > 0)
                    {
                        #region MATERIAL DIALOG
                        // 🔹 Construimos el título con color amarillo y negritas
                        var titleSpannable = new SpannableStringBuilder("Sesión Activa en Este Equipo");
                        titleSpannable.SetSpan(new ForegroundColorSpan(Color.ParseColor("#FCEC70")), 0, titleSpannable.Length(), SpanTypes.ExclusiveExclusive);
                        titleSpannable.SetSpan(new StyleSpan(TypefaceStyle.Bold), 0, titleSpannable.Length(), SpanTypes.ExclusiveExclusive);

                        // 🔹 Construimos el mensaje con color azul claro y negritas para el nombre
                        var mensajeSpannable = new SpannableStringBuilder("No puede iniciar sesión debido a que este equipo se encuentra actualmente en uso por ");
                        int startNombre = mensajeSpannable.Length();
                        mensajeSpannable.Append(nombre);
                        mensajeSpannable.SetSpan(new StyleSpan(TypefaceStyle.Bold), startNombre, mensajeSpannable.Length(), SpanTypes.ExclusiveExclusive);
                        mensajeSpannable.SetSpan(new ForegroundColorSpan(Color.ParseColor("#E0F1FA")), 0, mensajeSpannable.Length(), SpanTypes.ExclusiveExclusive);

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
                            positiveButton?.SetTextColor(Color.ParseColor("#FFA000")); // Naranja Material
                            positiveButton?.SetAllCaps(false);
                        });
                        #endregion
                    }
                    else
                    {
                        //Registro de Ingreso Al Sistema.
                        thisConnection.Open();
                        string cadena = "INSERT INTO TB_REGISTRO_MOVIMIENTOS(FECHA,NOM_COMPU,NOM_USU,TIPO_MOV,OP_CLAVE,FOLIO,DETALLE,SISTEMA,MOV_FOLIO) " +
                                    "VALUES('" + System.DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + "','CEL " + imei + "','" + responsablesplit + "','E','" + ip + "','','Ingreso a sistema SPLIT TRAILER Imei: " + imei + ", Ip: " + ip + " ','SPLITTRAIL','')";
                        SqlCommand cmd = new SqlCommand(cadena, thisConnection);
                        cmd.ExecuteNonQuery();

                        cadena = "INSERT INTO  tb_det_acceso_celulares ( fecha, imei, nom_usu, sistema, folio, version, estado) " +
                                    "VALUES('" + System.DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + "','" + imei + "','" + responsablesplit + "','SplitTrailer','','" + currentVersionName + "','A')";
                        cmd = new SqlCommand(cadena, thisConnection);
                        cmd.ExecuteNonQuery();

                        thisConnection.Close();

                        Intent intent = new Intent(this, typeof(SolicitarPed));
                        intent.PutExtra("cvresponsable", responsable.ToString());
                        intent.PutExtra("responsable", responsablesplit.ToString());
                        intent.PutExtra("currentVersionName", currentVersionName.ToString().Trim());
                        intent.PutExtra("imei", imei.ToString().Trim());
                        StartActivity(intent);
                        Finish();
                    }
                }
            }
            else
            {

                Toast.MakeText(this, "Contraseña Invalida para este usuario", ToastLength.Long).Show();
                return;
            }
        }

        private void spinner_ItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            Spinner spinner = (Spinner)sender;
            vehiculo = spinner.GetItemAtPosition(e.Position).ToString();
        }

        private void spinner_ItemSelected2(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            Spinner spinner = (Spinner)sender;
            responsablesplit = spinner.GetItemAtPosition(e.Position).ToString();
        }

        private void getData()
        {
            try
            {
                context = this;
                // Datos locales
                System.Console.WriteLine("AutoUpdater", "GetData");
                Android.Content.PM.PackageInfo pckginfo = context.PackageManager.GetPackageInfo(context.PackageName, 0);

                currentVersionCode = pckginfo.VersionCode;
                currentVersionName = pckginfo.VersionName;

                // Datos remotos
                string data = downloadHttp(new URL(INFO_FILE));
                JSONObject json = new JSONObject(data.ToString());
                latestVersionCode = json.GetInt("versionCode");
                latestVersionName = json.OptString("versionName");
                downloadURL = json.GetString("downloadURL");
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
            BufferedReader reader = new BufferedReader(new InputStreamReader(c.InputStream));
            Java.Lang.StringBuilder stringBuilder = new Java.Lang.StringBuilder();
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                stringBuilder.Append(line + "\n");
            }
            return stringBuilder.ToString();
        }
        public bool isNewVersionAvailable()
        {
            return latestVersionCode > currentVersionCode;
        }

        private string downloadApp()
        {
            var progressDialog = ProgressDialog.Show(this, "Espere Por Favor...", "Descargando Actualizacion", true);
            new System.Threading.Thread(new ThreadStart(delegate
            {//LOAD METHOD TO GET ACCOUNT INFO
                try
                {
                    var pathToNewFolder = Android.OS.Environment.ExternalStorageDirectory.AbsolutePath + "/SplitTrailer";
                    Directory.CreateDirectory(pathToNewFolder);

                    var webClient = new WebClient();
                    webClient.DownloadFileCompleted += (s, ex) =>
                    {
                        RunOnUiThread(() => Toast.MakeText(this, "Aplicacion Actualizada.", ToastLength.Long).Show()); //HIDE PROGRESS DIALOG 
                        RunOnUiThread(() => progressDialog.Hide());
                        Intent intentx = new Intent(Intent.ActionView);
                        intentx.SetDataAndType(Android.Net.Uri.FromFile(new Java.IO.File(Android.OS.Environment.ExternalStorageDirectory.AbsolutePath + "/SplitTrailer/SplitTrailer.apk")), "application/vnd.android.package-archive");
                        intentx.SetFlags(ActivityFlags.NewTask);
                        StartActivity(intentx);
                        Finish();
                    };

                    var folder = Android.OS.Environment.ExternalStorageDirectory.AbsolutePath + "/SplitTrailer";
                    webClient.DownloadFileAsync(new System.Uri("http://192.168.123.4:81/EmbarquesApk/APK_SplitTrailers/SplitTrailers.apk"), folder + "/SplitTrailer.apk");
                }
                catch (System.IO.IOException e)
                {
                    RunOnUiThread(() => progressDialog.Hide());
                    RunOnUiThread(() => Toast.MakeText(this, e.ToString(), ToastLength.Long).Show()); //HIDE PROGRESS DIALOG 
                }

            })).Start();
            return "1";
        }

        public string GetIPAddress()
        {
            IPAddress[] adresses = Dns.GetHostAddresses(Dns.GetHostName());

            if (adresses != null && adresses[0] != null)
            {
                return adresses[0].ToString();
            }
            else
            {
                return null;
            }
        }

        public string GetDeviceID()
        {
            Android.Telephony.TelephonyManager mTelephonyMgr;
            mTelephonyMgr = (Android.Telephony.TelephonyManager)GetSystemService(TelephonyService);
            string uniqueID = UUID.RandomUUID().ToString();
            //imei = mTelephonyMgr.DeviceId;
            imei = uniqueID;

            var deviceId = CrossDeviceInfo.Current.Id;

            if (imei == null || imei.Length > 17)
            {
                deviceId = deviceId.Substring(0, 15);
                imei = deviceId;
            }

            return imei;
        }
    }
}

