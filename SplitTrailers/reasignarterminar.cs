using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Text;
using Android.Text.Style;
using Android.Views;
using Android.Widget;
using Google.Android.Material.Dialog;
using Java.Util;
using SplitTrailers.Modal;
using SQLite;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace SplitTrailers
{
    [Activity(Label = "Reasignar Terminar Pedido")]
    public partial class reasignarterminar : Activity
    {
        public static string cvvehiculo, cvresponsable;
        public static string vehiculo, responsable;
        public static string imei, currentVersionName;
        public static string responsablereasignar;
        public string Nombre = "", Mtipo = "", MProd = "", MTar = "", MFol = "", mUser = "", user = "";
        public string Mtipo2 = "", MProd2 = "", MTar2 = "", MFol2 = "", CveCam = "", mOp = "A", Version = "";
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
        ArrayAdapter<String> comboAdapter;
        String[] strFrutas;

        DataTable CatProd;

        //traer los datos e id de cada uno de los elementos de la vista
        EditText pedido;
        TextView detalleped;
        TextView PedidosSurtidos;
        Button terminarcaptura;
        Button reasignarorden;

        TextView principal;
        TextView Secundario;

        string ordenacerrarreasignar = "";

        static int PICK_CONTACT_REQUEST = 1;
        public string Cancelado { get; private set; }


        protected override void OnCreate(Bundle savedInstanceState)
        {
            string contenido = "";
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.reasignar);
            //Recuperar datos de la pantalla anterior
            cvresponsable = Intent.GetStringExtra("cvresponsable");
            responsable = Intent.GetStringExtra("responsable");
            responsablereasignar = Intent.GetStringExtra("respreasig");
            imei = Intent.GetStringExtra("imei");
            currentVersionName = Intent.GetStringExtra("currentVersionName");

            DataTable responsables = new DataTable("responsables");



            principal = FindViewById<TextView>(Resource.Id.textoprincipal);
            Secundario = FindViewById<TextView>(Resource.Id.textosecundario);
            //LLenado Spinner 1buttonTerminarCarga

            reasignarorden = FindViewById<Button>(Resource.Id.buttonreasignar);
            reasignarorden.Click += Btnreasignar_Click;

            terminarcaptura = FindViewById<Button>(Resource.Id.buttonTerminarCarga);
            terminarcaptura.Click += Btnterminar_Click;

            if (responsablereasignar == null)
            {
                reasignarorden.Visibility = ViewStates.Invisible;

            }
            else
            {
                string temp_arm_resp = responsable;
                responsable = responsablereasignar;
                responsablereasignar = temp_arm_resp;

                terminarcaptura.Visibility = ViewStates.Invisible;

                principal.Text = "Reasignacion de Ordenes Por Cambio de Turno";
                Secundario.Text = "Ordenes Activas del Armador: " + responsable.Trim();

            }


            //Llenado Spinner 2
            thisConnection.Open();
            //Buscar el numero del que parte si es campo o proceso
            cmnd = thisConnection.CreateCommand();
            query = "SELECT folio FROM tb_det_acceso_celulares WHERE estado = 'A' AND folio != '' AND nom_usu = '" + responsable + "'";
            da = new SqlDataAdapter(query, thisConnection);
            da.Fill(ds, "responsables");
            responsables = ds.Tables["responsables"];
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




        }
        private void Btnreasignar_Click(object sender, EventArgs e)
        {
            if (ordenacerrarreasignar.Trim() == "0")
            {
                Toast.MakeText(this, "Seleccione Una Orden Valida", ToastLength.Long).Show();
            }
            else
            {
                #region MATERIAL DIALOG - ACEPTAR ORDEN DE REASIGNACIÓN
                // Construimos el título con color rojo y negritas
                var titleSpannable = new SpannableStringBuilder("Aceptar Orden de Reasignación");
                titleSpannable.SetSpan(new ForegroundColorSpan(Color.ParseColor("#DC3545")), 0, titleSpannable.Length(), SpanTypes.ExclusiveExclusive);
                titleSpannable.SetSpan(new StyleSpan(TypefaceStyle.Bold), 0, titleSpannable.Length(), SpanTypes.ExclusiveExclusive);

                // Construimos el mensaje con partes destacadas
                var mensajeSpannable = new SpannableStringBuilder();
                mensajeSpannable.Append("¿Desea aceptar la orden ");
                int startOrden = mensajeSpannable.Length();
                mensajeSpannable.Append(ordenacerrarreasignar.Trim());
                mensajeSpannable.SetSpan(new StyleSpan(TypefaceStyle.Bold), startOrden, mensajeSpannable.Length(), SpanTypes.ExclusiveExclusive);
                mensajeSpannable.Append(" para concluir con la carga?");
                mensajeSpannable.SetSpan(new ForegroundColorSpan(Color.ParseColor("#5F6368")), 0, mensajeSpannable.Length(), SpanTypes.ExclusiveExclusive);

                // Creamos el diálogo Material3
                var builder = new MaterialAlertDialogBuilder(this, Resource.Style.ThemeOverlay_Material3_MaterialAlertDialog);
                builder.SetTitle(titleSpannable);
                builder.SetIcon(Resource.Drawable.question);
                builder.SetMessage(mensajeSpannable);
                builder.SetCancelable(false);

                // Botones de acción
                builder.SetPositiveButton("Sí", ReasignarCarga);
                builder.SetNegativeButton("No", CancelaAction);

                // Crear y mostrar el diálogo
                var dialog = builder.Create();
                dialog.Show();

                // Personalización de botones
                dialog.Window.DecorView.Post(() =>
                {
                    var positiveButton = dialog.GetButton((int)DialogButtonType.Positive);
                    var negativeButton = dialog.GetButton((int)DialogButtonType.Negative);

                    positiveButton?.SetTextColor(Color.ParseColor("#C62828")); // Rojo Material
                    positiveButton?.SetAllCaps(false);

                    negativeButton?.SetTextColor(Color.ParseColor("#5F6368")); // Gris neutro
                    negativeButton?.SetAllCaps(false);
                });
                #endregion


            }
        }

        private void ReasignarCarga(object sender, DialogClickEventArgs e)
        {
            thisConnection.Open();
            string cadena = "UPDATE  tb_det_acceso_celulares SET estado = 'R' WHERE nom_usu = '" + responsable + "' AND folio = '" + ordenacerrarreasignar.Trim() + "' AND estado = 'A'";
            SqlCommand cmd = new SqlCommand(cadena, thisConnection);
            cmd.ExecuteNonQuery();

            string cadenaR = "INSERT INTO TB_REGISTRO_MOVIMIENTOS(FECHA,NOM_COMPU,NOM_USU,TIPO_MOV,OP_CLAVE,FOLIO,DETALLE,SISTEMA,MOV_FOLIO) " +
                                    "VALUES('" + System.DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + "','CEL " + imei + "','" + responsable + "','RO','7.10','','El Usuario " + responsable + " Reasigna la Orden " + ordenacerrarreasignar.Trim() + " al Usuario " + responsablereasignar + " Aceptar Orden de Reasignacio Imei: " + imei + "','SPLITTRAIL','" + ordenacerrarreasignar + "')";
            cmd = new SqlCommand(cadenaR, thisConnection);
            cmd.ExecuteNonQuery();
            thisConnection.Close();

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

            Toast.MakeText(this, "Orden de Venta Reasignada Correctamente para Continuar Armado", ToastLength.Long).Show();
        }

        private void Btnterminar_Click(object sender, EventArgs e)
        {

            if (ordenacerrarreasignar.Trim() == "0")
            {
                Toast.MakeText(this, "Seleccione Una Orden Valida", ToastLength.Long).Show();
            }
            else
            {
                #region MATERIAL DIALOG - TERMINAR CARGA EN LECTORA
                // Construimos el título con color rojo y negritas
                var titleSpannable = new SpannableStringBuilder("Terminar Carga en Lectora");
                titleSpannable.SetSpan(new ForegroundColorSpan(Color.ParseColor("#DC3545")), 0, titleSpannable.Length(), SpanTypes.ExclusiveExclusive);
                titleSpannable.SetSpan(new StyleSpan(TypefaceStyle.Bold), 0, titleSpannable.Length(), SpanTypes.ExclusiveExclusive);

                // Construimos el mensaje con parte destacada
                var mensajeSpannable = new SpannableStringBuilder();
                mensajeSpannable.Append("¿Desea terminar la carga en la orden ");
                int startOrden = mensajeSpannable.Length();
                mensajeSpannable.Append(ordenacerrarreasignar.Trim());
                mensajeSpannable.SetSpan(new StyleSpan(TypefaceStyle.Bold), startOrden, mensajeSpannable.Length(), SpanTypes.ExclusiveExclusive);
                mensajeSpannable.Append(" para todas las lectoras?");
                mensajeSpannable.SetSpan(new ForegroundColorSpan(Color.ParseColor("#5F6368")), 0, mensajeSpannable.Length(), SpanTypes.ExclusiveExclusive);

                // Creamos el diálogo Material3
                var builder = new MaterialAlertDialogBuilder(this, Resource.Style.ThemeOverlay_Material3_MaterialAlertDialog);
                builder.SetTitle(titleSpannable);
                builder.SetIcon(Resource.Drawable.question);
                builder.SetMessage(mensajeSpannable);
                builder.SetCancelable(false);

                // Botones de acción
                builder.SetPositiveButton("Sí", TerminarCarga);
                builder.SetNegativeButton("No", CancelaAction);

                // Crear y mostrar el diálogo
                var dialog = builder.Create();
                dialog.Show();

                // Personalizamos los botones después de mostrar el diálogo
                dialog.Window.DecorView.Post(() =>
                {
                    var positiveButton = dialog.GetButton((int)DialogButtonType.Positive);
                    var negativeButton = dialog.GetButton((int)DialogButtonType.Negative);

                    positiveButton?.SetTextColor(Color.ParseColor("#C62828")); // Rojo Material
                    positiveButton?.SetAllCaps(false);

                    negativeButton?.SetTextColor(Color.ParseColor("#5F6368")); // Gris neutro
                    negativeButton?.SetAllCaps(false);
                });
                #endregion

            }
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

                }
            };
            builder.Show();
        }



        private void CancelaAction(object sender, DialogClickEventArgs e)
        {
            return;
        }

        private void TerminarCarga(object sender, DialogClickEventArgs e)
        {
            thisConnection.Open();
            string cadena = "UPDATE tb_det_acceso_celulares SET estado = 'T' WHERE nom_usu = '" + responsable + "' AND sistema = 'SplitTrailer' AND folio = '" + ordenacerrarreasignar.Trim() + "' AND estado = 'A'";
            SqlCommand cmd = new SqlCommand(cadena, thisConnection);
            cmd.ExecuteNonQuery();
            thisConnection.Close();


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
        }

        private void spinner_ItemSelected2(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            Spinner spinner = (Spinner)sender;
            ordenacerrarreasignar = spinner.GetItemAtPosition(e.Position).ToString();


            if (ordenacerrarreasignar == "Seleccione una orden" || ordenacerrarreasignar == "Sin ordenes activas")
            {

                ordenacerrarreasignar = "0";


            }

            thisConnection.Open();
            string cadena = "SELECT A.prod_clave, B.prod_nombre, A.pdn_num_unidades, (SELECT { fn IFNULL(SUM(cajas), 0) } AS Expr1 FROM tb_det_split " +
                "WHERE(emb_folio = '" + ordenacerrarreasignar + "') AND(prod_clave = A.prod_clave) AND(estatus <> 'C')) + (SELECT { fn IFNULL(SUM(cajas), 0) } AS Expr2 FROM tb_det_embarque " +
                "WHERE(prod_clave = A.prod_clave) AND(emb_folio = '" + ordenacerrarreasignar + "') AND (OpCap = 'N') AND (Estatus <> 'C')) AS Surtidos FROM tb_det_pedidos AS A INNER JOIN " +
                "tb_cat_producto AS B ON A.prod_clave = B.prod_clave WHERE(A.pdn_folio = '" + ordenacerrarreasignar + "')";
            SqlDataAdapter da = new SqlDataAdapter(cadena, thisConnection);
            DataSet ds = new DataSet();
            da.Fill(ds, "CatProd");
            CatProd = new DataTable();
            CatProd = ds.Tables["CatProd"];
            thisConnection.Close();

            List<FlimStarInfo> lstFlimStar = detalle_pedido();
            var gvObject = FindViewById<GridView>(Resource.Id.gvCtrdetalle);
            gvObject.Adapter = new myGVItemAdapter(this, lstFlimStar);
            gvObject.ItemClick += new EventHandler<AdapterView.ItemClickEventArgs>(OnGridView_ItemClicked);
        }

        List<FlimStarInfo> listItem = new List<FlimStarInfo>();

        private void OnGridView_ItemClicked(object sender, AdapterView.ItemClickEventArgs e)
        {

        }

        List<FlimStarInfo> detalle_pedido()
        {
            listItem.Clear();
            foreach (DataRow row in CatProd.Rows)
            {
                string pedidos = row["pdn_num_unidades"].ToString().Trim();
                string surtidos = row["Surtidos"].ToString().Trim();
                listItem.Add(new FlimStarInfo()
                {
                    Name = row["prod_nombre"].ToString().Trim(),
                    Age = "Pedidos: " + pedidos + " Surtido: " + surtidos,
                    ImageID = Resource.Drawable.producto
                });
            }
            return listItem;
        }


    }
}