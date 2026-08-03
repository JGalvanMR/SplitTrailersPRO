using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Text;
using Android.Text.Style;
using Android.Widget;
using Google.Android.Material.Dialog;
using Plugin.DeviceInfo;
using SplitTrailers.Modal;
using SQLite;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
namespace SplitTrailers
{
    [Activity(Label = "Detalle Split")]
    public partial class detalle_split_cancelar : Activity
    {
        public static string crcancelar, split, pedidocancelar, detallesplit;
        public static string imei, currentVersionName;
        public static SQLiteConnection db;
        SqlConnection thisConnection = new SqlConnection(MainActivity.cadenaConexion);
        SqlDataAdapter da;
        DataSet ds = new DataSet();
        SqlCommand cmnd = new SqlCommand();
        SqlCommand cmnd1 = new SqlCommand();

        EditText pedidocan;
        TextView cansplit;
        TextView usuario;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            crcancelar = Intent.GetStringExtra("respcancel");
            pedidocancelar = Intent.GetStringExtra("pedidocancel");
            split = Intent.GetStringExtra("splitnocancel");
            imei = Intent.GetStringExtra("imei");
            currentVersionName = Intent.GetStringExtra("currentVersionName");



            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.detalle_split_cancelar);


            List<FlimStarInfo> lstFlimStar = ConsSplit();
            var gvObject = FindViewById<GridView>(Resource.Id.gvCtrcapturasplit);
            gvObject.Adapter = new myGVItemAdapter(this, lstFlimStar);
            gvObject.ItemClick += new EventHandler<AdapterView.ItemClickEventArgs>(OnGridView_ItemClicked); ; //detalle_pedido



        }

        private void OnGridView_ItemClicked(object sender, AdapterView.ItemClickEventArgs e)
        {
            split = e.View.FindViewById<TextView>(Resource.Id.txtName).Text;
            detallesplit = e.View.FindViewById<TextView>(Resource.Id.txtAge).Text;

            pedidocancelar = pedidocan.Text.Trim();

            #region MATERIAL DIALOG - Cancelar Producto del Split
            // Construimos el título con color y negritas
            var titleSpannable = new SpannableStringBuilder("Cancelar Producto del Split");
            titleSpannable.SetSpan(new ForegroundColorSpan(Color.ParseColor("#DC3545")), 0, titleSpannable.Length(), SpanTypes.ExclusiveExclusive);
            titleSpannable.SetSpan(new StyleSpan(TypefaceStyle.Bold), 0, titleSpannable.Length(), SpanTypes.ExclusiveExclusive);

            // Construimos el mensaje con partes destacadas
            var mensajeSpannable = new SpannableStringBuilder();
            mensajeSpannable.Append("¿Desea cancelar el producto ");
            int startProducto = mensajeSpannable.Length();
            mensajeSpannable.Append(detallesplit);
            mensajeSpannable.SetSpan(new StyleSpan(TypefaceStyle.Bold), startProducto, mensajeSpannable.Length(), SpanTypes.ExclusiveExclusive);

            mensajeSpannable.Append(" del Split número ");
            int startSplit = mensajeSpannable.Length();
            mensajeSpannable.Append(split);
            mensajeSpannable.SetSpan(new StyleSpan(TypefaceStyle.Bold), startSplit, mensajeSpannable.Length(), SpanTypes.ExclusiveExclusive);

            mensajeSpannable.Append("?");

            // Aplicamos un color neutro Material en el texto del mensaje
            mensajeSpannable.SetSpan(new ForegroundColorSpan(Color.ParseColor("#202124")), 0, mensajeSpannable.Length(), SpanTypes.ExclusiveExclusive);

            // Creamos el diálogo con estilo Material3
            var builder = new MaterialAlertDialogBuilder(this, Resource.Style.ThemeOverlay_Material3_MaterialAlertDialog);
            builder.SetTitle(titleSpannable);
            builder.SetIcon(Resource.Drawable.question);
            builder.SetMessage(mensajeSpannable);

            // Botones de acción
            builder.SetPositiveButton("Sí", SaveAction);
            builder.SetNegativeButton("No", CancelaAction);

            // Crear y mostrar el diálogo
            var dialog = builder.Create();
            dialog.Show();

            // Personalizamos los botones luego de mostrarlo
            dialog.Window.DecorView.Post(() =>
            {
                var positiveButton = dialog.GetButton((int)DialogButtonType.Positive);
                var negativeButton = dialog.GetButton((int)DialogButtonType.Negative);

                positiveButton?.SetTextColor(Color.ParseColor("#DC3545")); // Rojo Material para "Sí"
                positiveButton?.SetAllCaps(false);

                negativeButton?.SetTextColor(Color.ParseColor("#5F6368")); // Gris suave para "No"
                negativeButton?.SetAllCaps(false);
            });
            #endregion

            #region ALERT DIALOG
            /*Android.App.AlertDialog.Builder alertDialog = new Android.App.AlertDialog.Builder(this);
            alertDialog.SetTitle(Html.FromHtml("<font color='#dc3545' size = 10>Cancelar Producto del Split</font>"));
            alertDialog.SetIcon(Resource.Drawable.question);
            alertDialog.SetMessage(Html.FromHtml("<font color='#000000' size = 10>¿Desea Cancelar el producto " + detallesplit + " del Splir Numero " + split + "?</font>"));
            alertDialog.SetPositiveButton(Html.FromHtml("<font face = 'Comic Sans MS, arial' color='#dc3545' size = '10'>Sí</font>"), SaveAction);
            alertDialog.SetNegativeButton(Html.FromHtml("<font face = 'Comic Sans MS, arial' color='#dc3545' size = '10'>No</font>"), CancelaAction);
            alertDialog.Create();
            alertDialog.Show();*/
            #endregion
        }

        private void SaveAction(object sender, DialogClickEventArgs e)
        {
            thisConnection.Open();
            string cadena = "UPDATE tb_det_Etiqueta SET Estatus = 'C' WHERE emb_folio = '" + pedidocancelar.ToString() + "' AND Split = '" + split.ToString() + "'";
            SqlCommand cmd = new SqlCommand(cadena, thisConnection);
            cmd.ExecuteNonQuery();

            string cadenados = "UPDATE tb_det_split SET estatus = 'C' WHERE emb_folio = '" + pedidocancelar.ToString() + "' AND tarima = '" + split.ToString() + "'";
            SqlCommand cmddos = new SqlCommand(cadenados, thisConnection);
            cmddos.ExecuteNonQuery();

            string Cadena = "Select * From tb_det_split WHERE emb_folio = '" + pedidocancelar.ToString() + "' AND tarima = '" + split.ToString() + "' ";
            SqlDataAdapter da = new SqlDataAdapter(Cadena, thisConnection);
            DataSet ds = new DataSet();
            da.Fill(ds, "Ped");
            DataTable Ped = ds.Tables["Ped"];
            foreach (DataRow row in Ped.Rows)
            {
                if (row["tipo_rec"].ToString().Trim() == "PTC")
                    cadena = "UPDATE TB_DET_TRAZABILIDAD SET SURTIDO = SURTIDO - " + row["cajas"].ToString().Trim() + " WHERE PROD_CLAVE = '" + row["prod_clave"].ToString().Trim() + "' AND RECIBO = '" + row["no_lote"].ToString().Trim() + "' " +
                        "AND TIPO = 'PTC' AND TARIMA = '" + Convert.ToInt32(row["TARINI"].ToString().Trim()).ToString() + "' ";

                else
                    cadena = "UPDATE TB_DET_ETI_FINAL SET CAJAS_SUR = CAJAS_SUR - " + row["cajas"].ToString().Trim() + " WHERE CVE_PROD = '" + row["prod_clave"].ToString().Trim() + "' AND FOLIO = '" + row["no_lote"].ToString().Trim() + "' " +
                        "AND TARIMA = '" + Convert.ToInt32(row["TARINI"].ToString().Trim()).ToString() + "' ";
                cmd = new SqlCommand(cadena, thisConnection);
                cmd.ExecuteNonQuery();


            }

            //Android.Telephony.TelephonyManager mTelephonyMgr;
            //mTelephonyMgr = (Android.Telephony.TelephonyManager)GetSystemService(TelephonyService);
            //IMEI number  
            //string imei = GetDeviceID();


            string cadenas = "INSERT INTO TB_REGISTRO_MOVIMIENTOS(FECHA,NOM_COMPU,NOM_USU,TIPO_MOV,OP_CLAVE,FOLIO,DETALLE,SISTEMA,MOV_FOLIO) " +
                            "VALUES('" + System.DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + "','CEL " + imei + "','" + crcancelar.Trim() + "','C','7.10','" +
                            pedidocancelar.ToString().Trim() + "','Cancelacion Split " + split.ToString() + "','SPLIT','" + pedidocancelar.ToString().Trim() + "')";
            //MessageBox.Show(cadena);
            SqlCommand cmds = new SqlCommand(cadenas, thisConnection);
            cmds.ExecuteNonQuery();


            thisConnection.Close();

            #region MATERIAL DIALOG - Split Cancelado
            // Construimos el título con color y negritas
            var titleSpannable = new SpannableStringBuilder("Split Cancelado");
            titleSpannable.SetSpan(new ForegroundColorSpan(Color.ParseColor("#DC3545")), 0, titleSpannable.Length(), SpanTypes.ExclusiveExclusive);
            titleSpannable.SetSpan(new StyleSpan(TypefaceStyle.Bold), 0, titleSpannable.Length(), SpanTypes.ExclusiveExclusive);

            // Construimos el mensaje con color suave y estilo
            var mensajeSpannable = new SpannableStringBuilder("¡Split cancelado correctamente!");
            mensajeSpannable.SetSpan(new ForegroundColorSpan(Color.ParseColor("#202124")), 0, mensajeSpannable.Length(), SpanTypes.ExclusiveExclusive);
            mensajeSpannable.SetSpan(new StyleSpan(TypefaceStyle.Bold), 0, mensajeSpannable.Length(), SpanTypes.ExclusiveExclusive);

            // Creamos el diálogo usando Material Design 3
            var builder = new MaterialAlertDialogBuilder(this, Resource.Style.ThemeOverlay_Material3_MaterialAlertDialog);
            builder.SetTitle(titleSpannable);
            builder.SetIcon(Resource.Drawable.exito);
            builder.SetMessage(mensajeSpannable);
            builder.SetCancelable(false);

            // Botón principal
            builder.SetPositiveButton("Ok", (s, e) =>
            {
                pedidocan.Text = "";
                cansplit.Text = "000|000";

                List<FlimStarInfo> lstFlimStar = ConsSplit();
                lstFlimStar.Clear();

                var gvObject = FindViewById<GridView>(Resource.Id.gvCtrCancel);
                gvObject.Adapter = new myGVItemAdapter(this, null);
                gvObject.Adapter = null;
                gvObject.Adapter = new myGVItemAdapter(this, lstFlimStar);
            });

            // Crear y mostrar el diálogo
            var dialog = builder.Create();
            dialog.Show();

            // Personalizar el botón tras mostrar el diálogo
            dialog.Window.DecorView.Post(() =>
            {
                var positiveButton = dialog.GetButton((int)DialogButtonType.Positive);
                positiveButton?.SetTextColor(Color.ParseColor("#00695C")); // Verde Material (éxito)
                positiveButton?.SetAllCaps(false);
            });
            #endregion

            #region ALERT DIALOG
            /*Android.App.AlertDialog.Builder alertDialog = new Android.App.AlertDialog.Builder(this);
            alertDialog.SetTitle(Html.FromHtml("<font color='#dc3545' size = 10>Split Cancelado</font>"));
            alertDialog.SetIcon(Resource.Drawable.exito);
            alertDialog.SetMessage(Html.FromHtml("<font color='#FFFFFF' size = 10>Split Cancelado Correctamente!!! </font>"));
            alertDialog.SetCancelable(false);
            alertDialog.SetNeutralButton("Ok", delegate
            {
                alertDialog.Dispose();
                pedidocan.Text = "";
                cansplit.Text = "000|000";
                List<FlimStarInfo> lstFlimStar = ConsSplit();
                lstFlimStar.Clear();
                var gvObject = FindViewById<GridView>(Resource.Id.gvCtrCancel);
                gvObject.Adapter = new myGVItemAdapter(this, null);
                gvObject.Adapter = null;
                gvObject.Adapter = new myGVItemAdapter(this, lstFlimStar);

            });
            alertDialog.Show();*/
            #endregion
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

        private void CancelaAction(object sender, DialogClickEventArgs e)
        {
            return;
        }

        List<FlimStarInfo> listItem = new List<FlimStarInfo>();

        List<FlimStarInfo> GetFlimStarInformation()
        {
            throw new NotImplementedException();
        }

        List<FlimStarInfo> ConsSplit()
        {

            int cantidadsplit = 0;
            thisConnection.Open();
            listItem.Clear();
            string contenido = "";
            //thisConnection.Open();
            string cadena = "Select * from tb_det_split where emb_folio = '" + pedidocancelar.Trim() + "' AND tarima = '" + split.Trim() + "'";
            SqlDataAdapter da = new SqlDataAdapter(cadena, thisConnection);
            DataSet ds = new DataSet();
            da.Fill(ds, "ConsPed");
            DataTable ConsPed = ds.Tables["ConsPed"];

            foreach (DataRow Row in ConsPed.Rows)
            {

                listItem.Add(new FlimStarInfo()
                {
                    Name = Row["nom_prod"].ToString().Trim(),
                    Age = Row["cajas"].ToString().Trim() + " ! " + Row["prod_clave"].ToString().Trim() + " ! " + Row["no_lote"].ToString().Trim() + " ! " + Row["tarima"].ToString().Trim() + Row["tipo"].ToString().Trim(),
                    ImageID = Resource.Drawable.producto
                });
                cantidadsplit++;
            }


            //LbxCons.Font = new Font(LbxCons.Font.Name, 7);   ;
            thisConnection.Close();

            return listItem;
        }

    }
}