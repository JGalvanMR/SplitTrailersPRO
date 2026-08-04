using Android.App;
using Android.Content;
using Android.OS;
using Android.Telephony;
using Android.Text;
using Android.Views;
using Android.Views.InputMethods;
using Android.Widget;
using Google.Android.Material.Dialog;
using Plugin.DeviceInfo;
using SplitTrailers.Helpers; // <--- Agregado
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
    [Activity(Label = "Cancelar Split")]
    public partial class CancelarSplit : Activity
    {
        public static string crcancelar, split, pedidocancelar, ordven, responsplit, cveresponsplit, parcial, imei, currentVersionName;
        public static SQLiteConnection db;
        SqlConnection thisConnection = new SqlConnection(MainActivity.cadenaConexion);
        SqlDataAdapter da;
        DataSet ds = new DataSet();
        SqlCommand cmnd = new SqlCommand();
        SqlCommand cmnd1 = new SqlCommand();

        EditText pedidocan;
        TextView cansplit;
        TextView usuario;

        TextView textoSplit;
        Button CapturarCancelados;

        RadioButton splitCompleto;
        RadioButton SplitParcial;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            crcancelar = Intent.GetStringExtra("respcancel");
            responsplit = Intent.GetStringExtra("responsable");
            cveresponsplit = Intent.GetStringExtra("cvresponsable");
            parcial = Intent.GetStringExtra("Parcial");
            imei = Intent.GetStringExtra("imei");
            currentVersionName = Intent.GetStringExtra("currentVersionName");

            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.CancelarSplit);

            LoadConnection();

            pedidocan = FindViewById<EditText>(Resource.Id.pedidocancelar);
            usuario = FindViewById<TextView>(Resource.Id.usercancel);
            cansplit = FindViewById<TextView>(Resource.Id.SplitCargados);

            CapturarCancelados = FindViewById<Button>(Resource.Id.capturacancelacion);
            textoSplit = FindViewById<TextView>(Resource.Id.textosplit);

            splitCompleto = FindViewById<RadioButton>(Resource.Id.radio_Completa);
            SplitParcial = FindViewById<RadioButton>(Resource.Id.radio_Parcial);

            if (parcial == "B")
            {
                SplitParcial.Enabled = false;
            }

            CapturarCancelados.Visibility = ViewStates.Invisible;
            CapturarCancelados.Click += Btnlogin_Click;

            pedidocan.EditorAction += (sender, e) =>
            {
                if (e.ActionId == ImeAction.Done || e.ActionId == ImeAction.Next)
                {
                    if (splitCompleto.Checked == true)
                    {
                        thisConnection.Open();
                        string Cadena = "Select emb_folio from tb_mstr_embarque Where emb_folio = '" + pedidocan.Text.Trim() + "' AND sts = 'T' AND hora_fin != '--:--'";
                        SqlCommand embcerr = new SqlCommand(Cadena, thisConnection);
                        string embcer = Convert.ToString(embcerr.ExecuteScalar());
                        thisConnection.Close();

                        if (embcer.Trim().Length > 0)
                        {
                            // Diálogo de embarque cerrado (advertencia)
                            RunOnUiThread(() =>
                            {
                                DialogHelper.ShowWarningDialog(this,
                                    message: $"El Embarque: {pedidocan.Text.Trim()} está cerrado y no se puede cargar",
                                    positiveText: "OK");
                            });

                            pedidocan.Text = "";
                            pedidocan.RequestFocus();
                            return;
                        }

                        List<FlimStarInfo> lstFlimStar = ConsSplit();
                        var gvObject = FindViewById<GridView>(Resource.Id.gvCtrCancel);
                        gvObject.Adapter = new myGVItemAdapter(this, lstFlimStar);
                        gvObject.ItemClick += new EventHandler<AdapterView.ItemClickEventArgs>(OnGridView_ItemClicked);

                        pedidocan.SetSelection(0, pedidocan.Text.Length);
                        pedidocan.RequestFocus();
                    }
                    else
                    {
                        db.Query<Pedidos>("delete from  [Pedidos]");
                        db.Query<ConPedidos>("delete from  [ConPedidos]");
                        db.Query<xLote>("delete from  [xLote]");
                        db.Query<xLoteFinal>("delete from  [xLoteFinal]");
                        db.Query<xprod>("delete from  [xprod]");

                        textoSplit.Text = "Detalle de Desfase En Split Capturado";

                        List<FlimStarInfo> lstFlimStar = ConsSplitParcial();
                        var gvObject = FindViewById<GridView>(Resource.Id.gvCtrCancel);
                        gvObject.Adapter = new myGVItemAdapter(this, lstFlimStar);
                        gvObject.ItemClick += new EventHandler<AdapterView.ItemClickEventArgs>(OnGridView_ItemClicked);

                        pedidocan.SetSelection(0, pedidocan.Text.Length);
                        pedidocan.RequestFocus();
                    }
                }
            };
        }

        private void LoadConnection()
        {
            string folder = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);
            string dbPath = System.IO.Path.Combine(folder, "Split_Trailer_Cancelacion.db3");

            bool exist = File.Exists(dbPath);
            db = new SQLiteConnection(dbPath);

            if (!exist)
            {
                db.CreateTable<Pedidos>();
                db.CreateTable<ConPedidos>();
                db.CreateTable<xLote>();
                db.CreateTable<xLoteFinal>();
                db.CreateTable<xprod>();
                db.CreateTable<Mensajes>();
                db.CreateTable<XLoteSug>();
            }
        }

        void Btnlogin_Click(object sender, EventArgs e)
        {
            Intent intent = new Intent(this, typeof(CancelarParcial));
            intent.PutExtra("cvresponsable", crcancelar.ToString());
            intent.PutExtra("pedidocancelar", pedidocan.Text.Trim());
            intent.PutExtra("responsablesplit", responsplit.Trim());
            intent.PutExtra("cveresponsplit", cveresponsplit.Trim());
            intent.PutExtra("imei", imei.Trim());
            intent.PutExtra("currentVersionName", currentVersionName.Trim());
            StartActivity(intent);
        }

        private void OnGridView_ItemClicked(object sender, AdapterView.ItemClickEventArgs e)
        {
            if (splitCompleto.Checked == true)
            {
                split = e.View.FindViewById<TextView>(Resource.Id.txtName).Text;
                split = split.Replace("Split Numero: ", "");
                pedidocancelar = pedidocan.Text.Trim();

                // Diálogo de confirmación para cancelar split
                RunOnUiThread(() =>
                {
                    DialogHelper.ShowConfirmDialog(this,
                        title: "Cancelar Split",
                        message: $"¿Desea Cancelar el Split Número {split}?",
                        positiveText: "Sí",
                        negativeText: "No",
                        positiveAction: SaveAction,
                        negativeAction: CancelaAction);
                });
            }
        }

        private void SaveAction(object sender, DialogClickEventArgs e)
        {
            string seccion = "";
            thisConnection.Open();
            string cadena = "";
            SqlCommand cmd;

            string Cadena = "Select * From tb_det_split WHERE emb_folio = '" + ordven.ToString() + "' AND tarima = '" + split.ToString() + "' AND estatus != 'C' ";
            SqlDataAdapter da = new SqlDataAdapter(Cadena, thisConnection);
            DataSet ds = new DataSet();
            da.Fill(ds, "Ped");
            DataTable Ped = ds.Tables["Ped"];

            foreach (DataRow row in Ped.Rows)
            {
                string sts = row["estatus"].ToString().Trim();

                if (row["tipo_rec"].ToString().Trim() == "PTC")
                {
                    cadena = "UPDATE TB_DET_TRAZABILIDAD SET SURTIDO = SURTIDO - " + row["cajas"].ToString().Trim() + ",  pti_estatus_sur = '' WHERE PROD_CLAVE = '" + row["prod_clave"].ToString().Trim() + "' AND RECIBO = '" + row["no_lote"].ToString().Trim() + "' " +
                        "AND TIPO = 'PTC' AND TARIMA = '" + Convert.ToInt32(row["TARINI"].ToString().Trim()).ToString() + "' ";
                }
                else
                {
                    cadena = "UPDATE TB_DET_ETI_FINAL SET CAJAS_SUR = CAJAS_SUR - " + row["cajas"].ToString().Trim() + ", estatus_sur = ''  WHERE CVE_PROD = '" + row["prod_clave"].ToString().Trim() + "' AND FOLIO = '" + row["no_lote"].ToString().Trim() + "' " +
                        "AND TARIMA = '" + Convert.ToInt32(row["TARINI"].ToString().Trim()).ToString() + "' ";
                }

                cadena += " UPDATE tb_det_split SET estatus = 'C' WHERE emb_folio = '" + ordven.ToString() + "' AND tarima = '" + split.ToString() + "' AND no_lote =  '" + row["no_lote"].ToString().Trim() + "' AND prod_clave = '" + row["prod_clave"].ToString().Trim() + "' AND TARINI = '" + row["TARINI"].ToString().Trim() + "' ";
                cadena += " UPDATE tb_det_Etiqueta SET Estatus = 'C' WHERE emb_folio = '" + pedidocancelar.ToString() + "' AND Split = '" + split.ToString() + "' AND Eti_Recibo = '" + row["no_lote"].ToString().Trim() + "' AND Eti_Producto  = '" + row["prod_clave"].ToString().Trim() + "'AND Eti_TarIni = '" + Convert.ToInt32(row["TARINI"].ToString().Trim()).ToString() + "' ";

                cmd = new SqlCommand(cadena, thisConnection);
                cmd.ExecuteNonQuery();

                if (sts == "S")
                {
                    try
                    {
                        Cadena = "Select Top (1) seccion from tb_det_embarque WHERE prod_clave = '" + row["prod_clave"].ToString().Trim() + "' AND recibo  = '" + row["no_lote"].ToString().Trim() + "' " +
                           "AND tarima  = '" + Convert.ToInt32(row["TARINI"].ToString().Trim()).ToString() + "' AND emb_folio = '" + pedidocancelar.ToString().Trim() + "' AND Estatus != 'C' AND OpCap = 'X' AND cajas >= '" + Convert.ToInt32(row["cajas"].ToString().Trim()) + "'";
                        cmd = new SqlCommand(Cadena, thisConnection);
                        seccion = cmd.ExecuteScalar().ToString().Trim();
                    }
                    catch
                    {
                        seccion = "";
                    }

                    if (seccion.Trim().Length > 0)
                    {
                        string Cadenax = "Select ISNULL(SUM(cajas), 0 ) from tb_det_embarque WHERE prod_clave = '" + row["prod_clave"].ToString().Trim() + "' AND recibo  = '" + row["no_lote"].ToString().Trim() + "' " +
                           "AND tarima  = '" + Convert.ToInt32(row["TARINI"].ToString().Trim()).ToString() + "' AND emb_folio = '" + pedidocancelar.ToString().Trim() + "' AND Estatus != 'C' AND OpCap = 'X' AND seccion = '" + seccion + "'";
                        cmd = new SqlCommand(Cadenax, thisConnection);
                        int cantidad_Emb = Convert.ToInt32(cmd.ExecuteScalar());

                        int cantidad_actual = cantidad_Emb - Convert.ToInt32(row["cajas"].ToString().Trim());

                        string complemento = "";

                        if (cantidad_actual == 0)
                        {
                            complemento = ", Estatus = 'C'";
                        }

                        cadena = "UPDATE  tb_det_embarque SET cajas = '" + cantidad_actual + "'" + complemento + "  WHERE prod_clave = '" + row["prod_clave"].ToString().Trim() + "' AND recibo  = '" + row["no_lote"].ToString().Trim() + "' " +
                               "AND tarima  = '" + Convert.ToInt32(row["TARINI"].ToString().Trim()).ToString() + "' AND emb_folio = '" + pedidocancelar.ToString().Trim() + "' AND Estatus != 'C' AND OpCap = 'X'  AND seccion = '" + seccion + "'";
                        cmd = new SqlCommand(cadena, thisConnection);
                        cmd.ExecuteNonQuery();
                    }
                }
            }

            string cadenas = "INSERT INTO TB_REGISTRO_MOVIMIENTOS(FECHA,NOM_COMPU,NOM_USU,TIPO_MOV,OP_CLAVE,FOLIO,DETALLE,SISTEMA,MOV_FOLIO) " +
                            "VALUES('" + System.DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + "','CEL " + imei + "','" + crcancelar.Trim() + "','C','7.10','" +
                            pedidocancelar.ToString().Trim() + "','Cancelacion Split " + split.ToString() + "','SPLIT','" + pedidocancelar.ToString().Trim() + "')";
            SqlCommand cmds = new SqlCommand(cadenas, thisConnection);
            cmds.ExecuteNonQuery();

            thisConnection.Close();

            // Diálogo de éxito (Split Cancelado)
            RunOnUiThread(() =>
            {
                DialogHelper.ShowSuccessDialog(this,
                    message: "Split Cancelado Correctamente!!!",
                    positiveText: "OK",
                    positiveAction: (s, ev) =>
                    {
                        Intent databack = new Intent();
                        databack.PutExtra("pedido_cancelar", pedidocan.Text.Trim());

                        pedidocan.Text = "";
                        cansplit.Text = "000|000";

                        List<FlimStarInfo> lstFlimStar = ConsSplit();
                        lstFlimStar.Clear();
                        var gvObject = FindViewById<GridView>(Resource.Id.gvCtrCancel);
                        gvObject.Adapter = new myGVItemAdapter(this, null);
                        gvObject.Adapter = null;
                        gvObject.Adapter = new myGVItemAdapter(this, lstFlimStar);

                        SetResult(Result.Ok, databack);
                        Finish();
                    });
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
            string Existe = "N";
            int cantidadsplit = 0;
            thisConnection.Open();
            listItem.Clear();
            string contenido = "";
            ordven = pedidocan.Text.ToString();

            if (ordven.Length > 0)
            {
                if (Convert.ToInt32(ordven) < 300000)
                {
                    ordven = "0" + ordven.ToString().Trim();
                }
            }

            string cadena = "Select DISTINCT(tarima) AS NoSplit from tb_det_split where emb_folio = '" + ordven.Trim() + "' AND estatus != 'C'";
            SqlDataAdapter da = new SqlDataAdapter(cadena, thisConnection);
            DataSet ds = new DataSet();
            da.Fill(ds, "ConsPed");
            DataTable ConsPed = ds.Tables["ConsPed"];

            foreach (DataRow Row in ConsPed.Rows)
            {
                Existe = "S";
                listItem.Add(new FlimStarInfo()
                {
                    Name = "Split Numero: " + Row["NoSplit"].ToString().Trim(),
                    Age = "Para Cancelar de Clic Aqui",
                    ImageID = Resource.Drawable.producto
                });
                cantidadsplit++;
            }

            cansplit.Text = cantidadsplit.ToString();

            if (Existe != "S")
            {
                // Diálogo de error (Pedido sin Split)
                RunOnUiThread(() =>
                {
                    DialogHelper.ShowErrorDialog(this,
                        message: $"El pedido: {pedidocan.Text.Trim()} no cuenta con split disponible",
                        positiveText: "OK");
                });
            }

            thisConnection.Close();
            return listItem;
        }

        List<FlimStarInfo> ConsSplitParcial()
        {
            string mped = pedidocan.Text.Trim();
            db.Query<ConPedidos>("UPDATE [ConPedidos] SET surtido = 0");

            string Tipoped = "NAL";

            if (pedidocan.Text.Length > 0)
            {
                if (Convert.ToInt32(pedidocan.Text) < 300000)
                {
                    Tipoped = "EXP";
                }
            }

            thisConnection.Open();
            string Cadena = "Select a.pdn_folio,a.prod_clave,b.prod_nombre,a.pdn_num_unidades From tb_det_pedidos A, tb_Cat_producto B " +
                "where a.pdn_folio = '" + pedidocan.Text.Trim() + "' and a.prod_clave = b.prod_clave and A.pdn_Tipo = '" + Tipoped + "'";
            SqlDataAdapter da = new SqlDataAdapter(Cadena, thisConnection);
            DataSet ds = new DataSet();
            da.Fill(ds, "Ped");
            DataTable Ped = ds.Tables["Ped"];
            thisConnection.Close();

            string hay = "N";

            if (Ped.Rows.Count == 0)
            {
                // Diálogo de error (Pedido Inexistente)
                RunOnUiThread(() =>
                {
                    DialogHelper.ShowErrorDialog(this,
                        message: $"El pedido: {pedidocan.Text.Trim()} no existe o no se ha dado de alta",
                        positiveText: "OK");
                });

                pedidocan.Text = "";
                pedidocan.RequestFocus();
            }

            foreach (DataRow row in Ped.Rows)
            {
                string mnom = row["prod_nombre"].ToString().Trim();
                mnom = mnom.Replace("'", " ");

                Pedidos Pedidoscapturados = new Pedidos { folio = row["pdn_folio"].ToString().Trim(), prod_clave = row["prod_clave"].ToString().Trim(), nombre = mnom, pedido = Convert.ToInt32(row["pdn_num_unidades"]), surtido = 0 };
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
                    db.Insert(consecutivo);
                }

                hay = "S";
            }

            thisConnection.Open();

            Cadena = "Select isnull(SUM(a.pdn_num_unidades), 0) AS Pedidos From tb_det_pedidos A, tb_Cat_producto B " +
                                "where a.pdn_folio = '" + mped.Trim() + "' and a.prod_clave = b.prod_clave";
            SqlCommand cmd = new SqlCommand(Cadena, thisConnection);
            int cantped = Convert.ToInt32(cmd.ExecuteScalar());

            string cadena = "Select * From tb_det_pedidos A, tb_Cat_producto B where a.pdn_folio = '" + pedidocan.Text.Trim() + "' and a.prod_clave = b.prod_clave";
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

            List<FlimStarInfo> lstFlimStar = detalle_pedido(pedidocan.Text.Trim(), "Acumulado");
            var gvObject = FindViewById<GridView>(Resource.Id.gvCtrCancel);
            gvObject.Adapter = new myGVItemAdapter(this, lstFlimStar);
            gvObject.ItemClick += new EventHandler<AdapterView.ItemClickEventArgs>(OnGridView_ItemClicked);

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
                var query = db.Table<Pedidos>();
                foreach (var captu in query)
                {
                    CapturarCancelados.Visibility = ViewStates.Visible;
                    listItem.Add(new FlimStarInfo()
                    {
                        Name = captu.nombre,
                        Age = "Pedidos: " + captu.pedido + " Surtido: " + captu.surtido,
                        ImageID = Resource.Drawable.producto
                    });
                }
            }

            thisConnection.Close();
            return listItem;
        }
    }
}