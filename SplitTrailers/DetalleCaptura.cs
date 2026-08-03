using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.OS;
using Android.Text;
using Android.Text.Style;
using Android.Widget;
using Google.Android.Material.Dialog;
using Plugin.DeviceInfo;
using SplitTrailers.Modal;
using SplitTrailers.Models;
using SQLite;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;

namespace SplitTrailers
{

    [Activity(Label = "Detalle de Orden", LaunchMode = LaunchMode.SingleTask)]
    public partial class DetalleCaptura : SolicitarPed
    {
        public static string cvvehiculo, cvresponsable, responsablesplit;
        public static string vehiculo, responsable;
        public static string imei, currentVersionName;
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
        ArrayAdapter<String> comboAdapter;
        String[] strFrutas;
        public string tb_tabla = "tb_mstr_pedidos_nal";
        public string tipoembarque = "NAL";

        int diasmincarga = 0;


        //traer los datos e id de cada uno de los elementos de la vista
        TextView pedido;
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
        int count = 0;

        public string Cancelado { get; private set; }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            string contenido = "";
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.InformeCambioOrden);
            var ordenventa = Intent.Extras.GetInt("ORDENVENTA", -1);
            responsable = Intent.Extras.GetString("RESPONSABLE");
            cvvehiculo = Intent.Extras.GetString("cvcamioneta");
            cvresponsable = Intent.Extras.GetString("cvresponsable");
            vehiculo = Intent.Extras.GetString("RESPONSABLE");
            imei = Intent.GetStringExtra("imei");
            currentVersionName = Intent.GetStringExtra("currentVersionName");

            LoadConnection();

            //Android.Telephony.TelephonyManager mTelephonyMgr;
            //mTelephonyMgr = (Android.Telephony.TelephonyManager)GetSystemService(TelephonyService);
            //IMEI number  
            //imei = GetDeviceID();

            pedido = FindViewById<TextView>(Resource.Id.PedidoInf);
            pedido.Text = ordenventa.ToString();
            PedidosSurtidos = FindViewById<TextView>(Resource.Id.textTOTALESCA);
            TextView usuario = FindViewById<TextView>(Resource.Id.usuarioInf);
            usuario.Text = responsable.Trim() + " - Split Trailer";

            db.Query<Pedidos>("delete from  [Pedidos]");
            db.Query<ConPedidos>("delete from  [ConPedidos]");
            db.Query<xLote>("delete from  [xLote]");
            db.Query<xLoteFinal>("delete from  [xLoteFinal]");
            db.Query<xprod>("delete from  [xprod]");


            List<FlimStarInfo> lstFlimStar = ConsSplitParcial();
            var gvObject = FindViewById<GridView>(Resource.Id.gvCtrlInf);
            gvObject.Adapter = new myGVItemAdapter(this, lstFlimStar);
            gvObject.ItemClick += new EventHandler<AdapterView.ItemClickEventArgs>(OnGridView_ItemClicked); ; //detalle_pedido


            string consultaActualizar = "UPDATE tb_mstr_pedidos_nal SET pdn_situacion = '' WHERE pdn_folio = '" + ordenventa + "'";
            thisConnection.Open();
            SqlCommand cmd = new SqlCommand(consultaActualizar, thisConnection);
            cmd.ExecuteNonQuery();

            consultaActualizar = "INSERT INTO TB_REGISTRO_MOVIMIENTOS(FECHA,NOM_COMPU,NOM_USU,TIPO_MOV,OP_CLAVE,FOLIO,DETALLE,SISTEMA,MOV_FOLIO) " +
                            "VALUES(GETDATE(),'CEL " + imei + "','" + responsable.Trim() + "','V','7.10','" +
                            ordenventa + "','Revision Cambio','SPLITRA','" + ordenventa + "')";
            //MessageBox.Show(cadena);
            cmd = new SqlCommand(consultaActualizar, thisConnection);
            cmd.ExecuteNonQuery();

            thisConnection.Close();
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

        private void LoadConnection()
        {
            string folder = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);
            string dbPath = System.IO.Path.Combine(folder, "Split_Trailer_Cambios.db3");

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

        public interface IBackButtonHandler
        {
            bool HandleBackButton();
        }

        public override void OnBackPressed()
        {
            Intent intent = new Intent(this, typeof(SolicitarPed));
            intent.PutExtra("cvresponsable", cvresponsable.ToString());
            intent.PutExtra("responsable", responsable.ToString());
            intent.PutExtra("imei", imei.Trim());
            intent.PutExtra("currentVersionName", currentVersionName.Trim());
            StartActivity(intent);
            Finish();
        }



        private void OnGridView_ItemClicked(object sender, AdapterView.ItemClickEventArgs e)
        {

        }

        List<FlimStarInfo> listItem = new List<FlimStarInfo>();
        List<FlimStarInfo> ConsSplitParcial()
        {
            string mped = pedido.Text.ToString().Trim();
            db.Query<ConPedidos>("UPDATE [ConPedidos] SET surtido = 0");

            string Tipoped = "NAL";

            if (pedido.Text.Length > 0)
            {
                if (Convert.ToInt32(pedido.Text) < 300000)
                {
                    Tipoped = "EXP";

                }
            }



            thisConnection.Open();
            string Cadena = "Select a.pdn_folio,a.prod_clave,b.prod_nombre,a.pdn_num_unidades From tb_det_pedidos A, tb_Cat_producto B " +
                "where a.pdn_folio = '" + pedido.Text.Trim() + "' and a.prod_clave = b.prod_clave and A.pdn_Tipo = '" + Tipoped + "'";
            SqlDataAdapter da = new SqlDataAdapter(Cadena, thisConnection);
            DataSet ds = new DataSet();
            da.Fill(ds, "Ped");
            DataTable Ped = ds.Tables["Ped"];
            thisConnection.Close();

            string hay = "N";


            if (Ped.Rows.Count == 0)
            {
                #region MATERIAL DIALOG - Pedido Inexistente
                // Construimos el título con color rojo y negritas
                var titleSpannable = new SpannableStringBuilder("Pedido Inexistente");
                titleSpannable.SetSpan(new ForegroundColorSpan(Color.ParseColor("#DC3545")), 0, titleSpannable.Length(), SpanTypes.ExclusiveExclusive);
                titleSpannable.SetSpan(new StyleSpan(TypefaceStyle.Bold), 0, titleSpannable.Length(), SpanTypes.ExclusiveExclusive);

                // Construimos el mensaje con color neutro y negritas en el número de pedido
                var mensajeSpannable = new SpannableStringBuilder();
                mensajeSpannable.Append("El pedido ");
                int startPedido = mensajeSpannable.Length();
                mensajeSpannable.Append(pedido.Text.Trim());
                mensajeSpannable.SetSpan(new StyleSpan(TypefaceStyle.Bold), startPedido, mensajeSpannable.Length(), SpanTypes.ExclusiveExclusive);
                mensajeSpannable.Append(" no existe o no se ha dado de alta.");
                mensajeSpannable.SetSpan(new ForegroundColorSpan(Color.ParseColor("#202124")), 0, mensajeSpannable.Length(), SpanTypes.ExclusiveExclusive);

                // Crear el diálogo con estilo Material Design 3
                var builder = new MaterialAlertDialogBuilder(this, Resource.Style.ThemeOverlay_Material3_MaterialAlertDialog);
                builder.SetTitle(titleSpannable);
                builder.SetIcon(Resource.Drawable.no);
                builder.SetMessage(mensajeSpannable);
                builder.SetCancelable(false);

                // Botón principal
                builder.SetPositiveButton("Ok", (s, e) => { });

                // Crear y mostrar el diálogo
                var dialog = builder.Create();
                dialog.Show();

                // Personalización del botón tras mostrar el diálogo
                dialog.Window.DecorView.Post(() =>
                {
                    var positiveButton = dialog.GetButton((int)DialogButtonType.Positive);
                    positiveButton?.SetTextColor(Color.ParseColor("#00695C")); // Verde Material (consistente con tus otros diálogos)
                    positiveButton?.SetAllCaps(false);
                });
                #endregion

            }


            foreach (DataRow row in Ped.Rows)
            {

                string mnom = row["prod_nombre"].ToString().Trim();
                mnom = mnom.Replace("'", " ");

                Pedidos Pedidoscapturados = new Pedidos { folio = row["pdn_folio"].ToString().Trim(), prod_clave = row["prod_clave"].ToString().Trim(), nombre = mnom, pedido = Convert.ToInt32(row["pdn_num_unidades"]), surtido = 0 };
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








            thisConnection.Open();


            Cadena = "Select isnull(SUM(a.pdn_num_unidades), 0) AS Pedidos From tb_det_pedidos A, tb_Cat_producto B " +
                                "where a.pdn_folio = '" + mped.Trim() + "' and a.prod_clave = b.prod_clave";
            SqlCommand cmd = new SqlCommand(Cadena, thisConnection);
            int cantped = Convert.ToInt32(cmd.ExecuteScalar());




            string cadena = "Select * From tb_det_pedidos A, tb_Cat_producto B where a.pdn_folio = '" + pedido.Text.Trim() + "' and a.prod_clave = b.prod_clave";
            da = new SqlDataAdapter(cadena, thisConnection);
            ds = new DataSet();
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

            cadena = "Select prod_clave, ISNULL(SUM(cajas), 0) as cajas from tb_det_embarque Where emb_folio = '" + mped.Trim() + "'" +
                     " AND estatus != 'C' AND OpCap = 'N' Group By prod_clave Order by prod_clave";
            da = new SqlDataAdapter(cadena, thisConnection);
            ds = new DataSet();
            da.Fill(ds, "PedSuremb");
            var PedSurEmb = ds.Tables["PedSuremb"];

            int Cp = 0, Cs = 0, sur = 0;
            thisConnection.Close();
            foreach (DataRow Row in ConsPed.Rows)
            {
                sur = 0;
                foreach (DataRow row in PedSur.Select("prod_clave = '" + Row["prod_clave"].ToString() + "'"))
                    sur = Convert.ToInt32(row["Cajas"]);
                foreach (DataRow row in PedSurEmb.Select("prod_clave = '" + Row["prod_clave"].ToString() + "'"))
                    sur = sur + Convert.ToInt32(row["Cajas"]);
                db.Query<Pedidos>("UPDATE [Pedidos] SET surtido = '" + sur + "' WHERE prod_clave = '" + Row["prod_clave"].ToString().Trim() + "'");
                db.Query<ConPedidos>("UPDATE [ConPedidos] SET surtido = '" + sur + "' WHERE prod_clave = '" + Row["prod_clave"].ToString().Trim() + "'");

                Cp += Convert.ToInt32(Row["pdn_num_unidades"]);
                Cs += sur;
            }


            List<FlimStarInfo> lstFlimStar = detalle_pedido(pedido.Text.ToString().Trim(), "Acumulado");
            var gvObject = FindViewById<GridView>(Resource.Id.gvCtrlInf);
            gvObject.Adapter = new myGVItemAdapter(this, lstFlimStar);
            gvObject.ItemClick += new EventHandler<AdapterView.ItemClickEventArgs>(OnGridView_ItemClicked); //detalle_pedido

            return listItem;
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
                            Age = "Pedidos: " + captu.pedido + " Surtido: " + captu.surtido,
                            ImageID = Resource.Drawable.producto
                        });
                    }
                }

            }
            else
            {

                //Borrar la informacion que no se debe cancelarConPedidos
                //db.Query<Pedidos>("Delete FROM ConPedidos Where pedido >= surtido");
                //var queryCancelar = db.Query<Pedidos>("Delete FROM Pedidos Where pedido >= surtido");


                var query = db.Table<Pedidos>();
                foreach (var captu in query)
                {
                    listItem.Add(new FlimStarInfo()
                    {
                        Name = captu.nombre,
                        Age = "Pedidos: " + captu.pedido + " Surtido: " + captu.surtido,
                        ImageID = Resource.Drawable.producto
                    });

                }

            }

            //LbxCons.Font = new Font(LbxCons.Font.Name, 7);   ;
            thisConnection.Close();

            return listItem;
        }











    }
}