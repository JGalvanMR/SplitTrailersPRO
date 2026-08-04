using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Text;
using Android.Text.Style;
using Android.Widget;
using Google.Android.Material.Dialog;
using Plugin.DeviceInfo;
using SplitTrailers.Helpers; // <-- Agregar
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
            gvObject.ItemClick += new EventHandler<AdapterView.ItemClickEventArgs>(OnGridView_ItemClicked);
        }

        private void OnGridView_ItemClicked(object sender, AdapterView.ItemClickEventArgs e)
        {
            split = e.View.FindViewById<TextView>(Resource.Id.txtName).Text;
            detallesplit = e.View.FindViewById<TextView>(Resource.Id.txtAge).Text;

            pedidocancelar = pedidocan.Text.Trim();

            // ====== Diálogo de confirmación ======
            // Reemplazamos por DialogHelper.ShowConfirmDialog
            DialogHelper.ShowConfirmDialog(this,
                title: "Cancelar Producto del Split",
                message: $"¿Desea cancelar el producto {detallesplit} del Split número {split}?",
                positiveText: "Sí",
                negativeText: "No",
                positiveAction: SaveAction,
                negativeAction: CancelaAction);
        }

        private void SaveAction(object sender, DialogClickEventArgs e)
        {
            // Lógica de cancelación (sin cambios)
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

            // Registro de movimiento (sin cambios)
            string cadenas = "INSERT INTO TB_REGISTRO_MOVIMIENTOS(FECHA,NOM_COMPU,NOM_USU,TIPO_MOV,OP_CLAVE,FOLIO,DETALLE,SISTEMA,MOV_FOLIO) " +
                            "VALUES('" + System.DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + "','CEL " + imei + "','" + crcancelar.Trim() + "','C','7.10','" +
                            pedidocancelar.ToString().Trim() + "','Cancelacion Split " + split.ToString() + "','SPLIT','" + pedidocancelar.ToString().Trim() + "')";
            SqlCommand cmds = new SqlCommand(cadenas, thisConnection);
            cmds.ExecuteNonQuery();

            thisConnection.Close();

            // ====== Diálogo de éxito ======
            DialogHelper.ShowSuccessDialog(this,
                message: "¡Split cancelado correctamente!",
                positiveText: "Ok",
                positiveAction: (s, ev) =>
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

            thisConnection.Close();
            return listItem;
        }
    }
}