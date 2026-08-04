using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Text;
using Android.Text.Style;
using Android.Views.InputMethods;
using Android.Widget;
using Google.Android.Material.Dialog;
using Java.Util;
using SplitTrailers.Helpers; // <-- AGREGADO
using SplitTrailers.Models;
using SQLite;
using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;

namespace SplitTrailers
{
    [Activity(Label = "Solicitud Reetiquetado")]
    public class solicitarreimpresion : Activity
    {
        string prod_clave = "", folio = "", foliox = "", Tarima = "", tipo = "PTC", cajaselec = "", responsable = "", cveresponsable = "", embarque = "";
        public static string imei, currentVersionName;
        bool find = false;
        int tarima = 0, caja = 0, es_campo = 0;
        int producido;
        int surtido;

        SqlConnection thisConnection = new SqlConnection(MainActivity.cadenaConexion);
        SqlCommand cmnd = new SqlCommand();
        TextView myEditText;
        String[] strFrutas;
        String[] strTarima;
        String[] strCajas;
        SqlCommand cmnd1 = new SqlCommand();
        SqlDataReader reader1;
        ArrayAdapter<String> comboAdapter;
        ArrayAdapter<String> comboAdapterTarima;
        ArrayAdapter<String> comboAdapterCaja;
        string query = "";
        SqlDataAdapter da;
        Button btnguardar;
        public static SQLiteConnection db;

        DataSet dstar = new DataSet();
        DataSet dscaj = new DataSet();
        public static DataTable productos = new DataTable("productos");
        public static DataTable tarimacaja = new DataTable("tarimacaja");
        public static DataTable productosnew = new DataTable("productos");
        public static DataTable tarimas = new DataTable("tarimas");
        public static DataTable cajas = new DataTable("cajas");
        DataSet ds = new DataSet();
        public static string producto = "";

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.EtiquetaError);
            // Create your application here
            btnguardar = FindViewById<Button>(Resource.Id.btnsave);
            btnguardar.Click += Btnguardar_Click;
            btnguardar.Enabled = false;

            LoadConnection();

            myEditText = FindViewById<TextView>(Resource.Id.folio);

            thisConnection.Open();
            cmnd = thisConnection.CreateCommand();
            cmnd.CommandText = "select inicio_campo from Tb_folio_campo";
            es_campo = Convert.ToInt32(cmnd.ExecuteScalar());
            ds.Clear();
            thisConnection.Close();

            cveresponsable = Intent.GetStringExtra("cvresponsable");
            embarque = Intent.GetStringExtra("embarque");
            responsable = Intent.GetStringExtra("responsable");
            imei = Intent.GetStringExtra("imei");
            currentVersionName = Intent.GetStringExtra("currentVersionName");

            myEditText.EditorAction += (sender, e) =>
            {
                foliox = myEditText.Text.Trim();
                folio = foliox;
                if (e.ActionId == ImeAction.Done || e.ActionId == ImeAction.Next)
                {
                    if (foliox != "")
                    {
                        if (foliox.Length == 5 || foliox.Length == 6)
                        {
                            //Buscar el numero del que parte si es campo o proceso
                            cmnd = thisConnection.CreateCommand();

                            if (foliox.Length == 5 || Convert.ToInt32(foliox) >= es_campo)
                            {
                                tipo = "PTC";
                                ReciboPTC();
                            }
                            else
                            {
                                tipo = "PTP";
                                ReciboPTP();
                            }
                        }
                        else
                        {
                            Spinner spinner = FindViewById<Spinner>(Resource.Id.spinner3);
                            ds.Clear();
                            System.Collections.ArrayList listaFrutas = new System.Collections.ArrayList();
                            //Buscar el numero del que parte si es campo o proceso
                            strFrutas = new String[] { "Ingrese un Folio válido" };
                            Collections.AddAll(listaFrutas, strFrutas);
                            comboAdapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleSpinnerItem, strFrutas);
                            spinner.Adapter = comboAdapter;
                            spinner.ItemSelected += new EventHandler<AdapterView.ItemSelectedEventArgs>(spinner_ItemSelected);
                        }
                    }
                }
            };
        }

        private void spinner_Click(object sender, EventArgs e)
        {
            TextView folix = FindViewById<TextView>(Resource.Id.folio);
            foliox = folix.Text.Trim();
            if (foliox != "")
            {
                if (foliox.Length == 5 || foliox.Length == 6)
                {
                    if (foliox.Length == 5 || Convert.ToInt32(foliox) >= es_campo)
                    {
                        tipo = "PTC";
                        ReciboPTC();
                    }
                    else
                    {
                        tipo = "PTP";
                        ReciboPTP();
                    }
                }
                else
                {
                    Spinner spinner = FindViewById<Spinner>(Resource.Id.spinner3);
                    ds.Clear();
                    System.Collections.ArrayList listaFrutas = new System.Collections.ArrayList();
                    strFrutas = new String[] { "Ingrese un Folio válido" };
                    Collections.AddAll(listaFrutas, strFrutas);
                    comboAdapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleSpinnerItem, strFrutas);
                    spinner.Adapter = comboAdapter;
                    spinner.ItemSelected += new EventHandler<AdapterView.ItemSelectedEventArgs>(spinner_ItemSelected);
                }
            }
        }

        private void Btnguardar_Click(object sender, EventArgs e)
        {
            find = false;
            TextView fol = FindViewById<TextView>(Resource.Id.folio);
            folio = fol.Text.Trim();
            int f = Convert.ToInt32(folio.Length);
            TextView tar = FindViewById<TextView>(Resource.Id.folio);
            if (tar.Text.Trim() == "")
            {
                tar.Text = "0";
            }
            tarima = Convert.ToInt32(tar.Text.Trim());
            TextView box = FindViewById<TextView>(Resource.Id.folio);
            if (box.Text.Trim() == "")
            {
                box.Text = "0";
            }
            caja = Convert.ToInt32(box.Text.Trim());

            prod_clave = prod_clave.Trim();

            if (Convert.ToString(folio).Trim() == "")
            {
                Toast.MakeText(this, "Favor de escribir el folio", ToastLength.Short).Show();
                fol.RequestFocus();
                return;
            }
            else if (Convert.ToInt32(folio) == 0)
            {
                Toast.MakeText(this, "El folio no puede estar en 0", ToastLength.Short).Show();
                fol.RequestFocus();
                fol.SetSelectAllOnFocus(true);
                return;
            }
            else if ((f != 5) && (f != 6))
            {
                Toast.MakeText(this, "Ingrese un folio válido", ToastLength.Short).Show();
                fol.RequestFocus();
                fol.SetSelectAllOnFocus(true);
                return;
            }

            if (Convert.ToString(prod_clave).Trim() == "Ingrese un Folio válido" || Convert.ToString(prod_clave).Trim() == "Ingrese un Folio")
            {
                Toast.MakeText(this, "Favor de escribir un folio válido", ToastLength.Short).Show();
                return;
            }

            if (Convert.ToString(tarima).Trim() == "")
            {
                Toast.MakeText(this, "Favor de escribir el número de tarima", ToastLength.Short).Show();
                tar.RequestFocus();
                return;
            }
            else if (Convert.ToInt32(tarima) == 0)
            {
                Toast.MakeText(this, "La tarima no puede estar en 0", ToastLength.Short).Show();
                tar.RequestFocus();
                tar.SetSelectAllOnFocus(true);
                return;
            }

            if (Convert.ToString(caja).Trim() == "")
            {
                Toast.MakeText(this, "Favor de escribir el número de caja", ToastLength.Short).Show();
                box.RequestFocus();
                return;
            }
            else if (Convert.ToInt32(caja) == 0)
            {
                Toast.MakeText(this, "El número de la caja no puede estar en 0", ToastLength.Short).Show();
                box.RequestFocus();
                box.SetSelectAllOnFocus(true);
                return;
            }
            string lectura = tipo.Trim() + folio.Trim() + prod_clave.Trim() + Tarima.ToString().Trim() + cajaselec.ToString().Trim();

            thisConnection.Open();
            cmnd1 = thisConnection.CreateCommand();
            cmnd1.CommandText = "insert into Tb_Det_Sol_Reetiquetado (Fecha, emb_folio, fecha_cap, Lectura, Recibo, Producto, Caja, TarIni, TarFin, Cve_Camioneta, Estatus, Obs, armador, autorizo, origen) values" +
                " ('" + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + "', '" + embarque + "',  GETDATE(), '" + lectura + "', '" + folio + "', '" + prod_clave + "', '" + cajaselec + "', '" + Tarima + "', '" + Tarima + "', '', 'A', 'SOLICITUD DE REIMPRESION POR ERROR DE LECTURA, O DAÑO EN ETIQUETA', '" + responsable + "', '', 'EMB')";
            reader1 = cmnd1.ExecuteReader();
            reader1.Dispose();

            string cadena = "insert into tb_det_Etiqueta(fecha,emb_folio, fecha_cap, Eti_Lectura, Eti_Recibo, Eti_Producto, Eti_Caja, Eti_TarIni, Eti_TarFin, Cve_Camioneta, FecCap, Version, Imei, Split, Estatus, Obs) " +
                                    "Values('" + System.DateTime.Now.ToString("dd/MM/yyyy") + "','" + embarque + "', GETDATE() ,'" + lectura + "','" + folio + "','" + prod_clave + "','" + cajaselec + "','" + Tarima + "','" + Tarima + "',''," +
                                    "'" + System.DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + "','" + currentVersionName + "','" + imei + "', '0', 'A', 'LEIDA POR REETIQUETADO ERROR/MALA IMPRESION' )";
            SqlCommand cmd = new SqlCommand(cadena, thisConnection);
            cmd.ExecuteNonQuery();

            thisConnection.Close();

            // Diálogo simplificado con Helper
            DialogHelper.ShowSuccessDialog(this,
                message: "La solicitud de reimpresión se realizó correctamente.",
                positiveText: "Entendido",
                positiveAction: (s, e) =>
                {
                    myEditText.Text = "";
                    Spinner spinner = FindViewById<Spinner>(Resource.Id.spinner3);
                    Spinner spinnerT = FindViewById<Spinner>(Resource.Id.tarimareim);
                    Spinner spinnerC = FindViewById<Spinner>(Resource.Id.cajareim);

                    ds.Clear();
                    System.Collections.ArrayList listaFrutas = new System.Collections.ArrayList();

                    strFrutas = new String[] { "Ingrese un Folio válido" };
                    strTarima = new String[] { "Ingrese un Folio válido" };
                    strCajas = new String[] { "Ingrese un Folio válido" };

                    Collections.AddAll(listaFrutas, strFrutas);

                    comboAdapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleSpinnerItem, strFrutas);
                    comboAdapterTarima = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleSpinnerItem, strTarima);
                    comboAdapterCaja = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleSpinnerItem, strCajas);

                    spinner.Adapter = comboAdapter;
                    spinner.ItemSelected += new EventHandler<AdapterView.ItemSelectedEventArgs>(spinner_ItemSelected);

                    spinnerT.Adapter = comboAdapterTarima;
                    spinnerT.ItemSelected += new EventHandler<AdapterView.ItemSelectedEventArgs>(spinnertarima_ItemSelected);

                    spinnerC.Adapter = comboAdapterCaja;
                    spinnerC.ItemSelected += new EventHandler<AdapterView.ItemSelectedEventArgs>(spinnercaja_ItemSelected);
                });
        }

        private void spinner_ItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            string canttabla = productosnew.Rows.Count.ToString();
            Spinner spinner = (Spinner)sender;
            prod_clave = spinner.GetItemAtPosition(e.Position).ToString();
            string[] arr = prod_clave.Split(' ');
            prod_clave = arr[0];
            tarimas = null;

            if (prod_clave != "Ingrese")
            {
                Spinner spinnerTarima = FindViewById<Spinner>(Resource.Id.tarimareim);
                dstar.Clear();
                System.Collections.ArrayList listaTarima = new System.Collections.ArrayList();
                listaTarima.Clear();

                thisConnection.Open();
                if (tipo == "PTP")
                {
                    string CadenaTarima = "Select TARIMA,NUM_LOTE,NUM_CAJAS,CAJAS_SUR, FECHA, FECHACAD, LOTE_CONTROL, isnull(ConseReimp, 0) as ConseReimp FROM TB_DET_ETI_FINAL WHERE FOLIO = '" + folio.Trim() + "' AND CVE_PROD = '" + prod_clave.ToString() + "' ORDER BY TARIMA";
                    da = new SqlDataAdapter(CadenaTarima, thisConnection);
                    da.Fill(dstar, "tarimas");
                    tarimas = dstar.Tables["tarimas"];

                    strTarima = new String[tarimas.Rows.Count];
                    for (int i = 0; i < tarimas.Rows.Count; i++)
                    {
                        string fechacad = "";
                        if (tarimas.Rows[i]["FECHA"].ToString().Trim().Length > 0)
                        {
                            fechacad = Convert.ToDateTime(tarimas.Rows[i]["FECHA"].ToString().Trim()).ToString("dd/MM/yyyy");
                        }

                        int x = i;
                        strTarima[i] = tarimas.Rows[x]["TARIMA"].ToString().Trim() + " - Cad: " + fechacad + " - Prod: " + tarimas.Rows[x]["NUM_CAJAS"].ToString().Trim() + " - Surt: " + tarimas.Rows[x]["CAJAS_SUR"].ToString().Trim();
                    }
                }
                if (tipo == "PTC")
                {
                    string CadenaTarima = "Select TARIMA,'' AS NUM_LOTE,ETIQUETA,SURTIDO, PTI_FECHA, FECHA_CAD, LOTE, isnull(ConseReimp, 0) as ConseReimp FROM TB_DET_TRAZABILIDAD WHERE RECIBO = '" + folio.Trim() + "' AND PROD_CLAVE = '" + prod_clave.ToString() + "' ORDER BY TARIMA";
                    da = new SqlDataAdapter(CadenaTarima, thisConnection);
                    da.Fill(dstar, "tarimas");
                    tarimas = dstar.Tables["tarimas"];

                    strTarima = new String[tarimas.Rows.Count];
                    for (int i = 0; i < tarimas.Rows.Count; i++)
                    {
                        int x = i;
                        strTarima[i] = tarimas.Rows[x]["TARIMA"].ToString().Trim() + " - Cad: " + tarimas.Rows[i]["FECHA_CAD"].ToString().Trim() + " - Prod: " + tarimas.Rows[x]["ETIQUETA"].ToString().Trim() + " - Surt: " + tarimas.Rows[x]["SURTIDO"].ToString().Trim();
                    }
                }
                thisConnection.Close();

                Collections.AddAll(listaTarima, strTarima);
                comboAdapterTarima = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleSpinnerItem, strTarima);
                spinnerTarima.Adapter = comboAdapterTarima;
                spinnerTarima.ItemSelected += new EventHandler<AdapterView.ItemSelectedEventArgs>(spinnertarima_ItemSelected);

                canttabla = productosnew.Rows.Count.ToString();
            }
        }

        private void spinnertarima_ItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            Spinner spinnerT = (Spinner)sender;
            Tarima = spinnerT.GetItemAtPosition(e.Position).ToString();

            if (Tarima != "Ingrese un Folio válido")
            {
                string cajas = "";

                string caducidad;

                Tarima = Tarima.Replace("Cad: ", "");
                Tarima = Tarima.Replace("Prod: ", "");
                Tarima = Tarima.Replace("Surt: ", "");
                Tarima = Tarima.Replace("Surt: ", "");
                Tarima = Tarima.Replace(" ", "");
                string[] arr = Tarima.Split('-');
                tarima = Convert.ToInt32(arr[0]);
                caducidad = arr[1];
                producido = Convert.ToInt16(arr[2]);
                surtido = Convert.ToInt16(arr[3]);

                string mcaj = "", mtar = "", mcod = prod_clave.Trim(), mfol = folio, mtip = tipo.Trim(), Ent = "N";

                SqlCommand cmd;
                string cadena = "";
                if (tipo == "PTP")
                {
                    mtar = tarima.ToString().Trim().PadLeft(3, '0');
                    cadena = "SELECT isnull(ConseReimp, 0) as ConseReimp FROM TB_DET_ETI_FINAL WHERE FOLIO = '" + folio.Trim() + "' AND CVE_PROD = '" + prod_clave.ToString() + "' AND TARIMA = '" + tarima.ToString().Trim() + "'";
                }
                else
                {
                    mtar = tarima.ToString().Trim().PadLeft(2, '0');
                    cadena = "SELECT isnull(ConseReimp, 0) as ConseReimp FROM TB_DET_TRAZABILIDAD WHERE RECIBO = '" + folio.Trim() + "' AND PROD_CLAVE = '" + prod_clave.ToString() + "' AND TARIMA = '" + tarima.ToString().Trim() + "'";
                }

                Tarima = mtar;
                thisConnection.Open();
                cmd = new SqlCommand(cadena, thisConnection);
                int conse = Convert.ToInt32(cmd.ExecuteScalar());
                thisConnection.Close();

                int disponible = producido + conse;

                DataTable Foliosleidos = new DataTable();
                string CadenaFolios = "Select Eti_Lectura, fecha_cap From tb_Det_Etiqueta " +
                               "WHERE (Eti_Producto = '" + mcod + "') AND (Eti_Recibo = '" + mfol + "') AND (Eti_TarIni = '" + mtar + "')";
                SqlDataAdapter da = new SqlDataAdapter(CadenaFolios, thisConnection);
                DataSet ds = new DataSet();
                da.Fill(ds, "Foliosleidos");
                Foliosleidos = ds.Tables["Foliosleidos"];

                DataTable FoliosleidosPresplit = new DataTable();
                string CadenaFoliospreesplit = "Select Eti_Lectura, fecha_cap From tb_Det_Etiqueta " +
                               "WHERE (Eti_Producto = '" + mcod + "') AND (Eti_Recibo = '" + mfol + "') AND (Eti_TarIni = '" + mtar + "')";
                SqlDataAdapter dapre = new SqlDataAdapter(CadenaFoliospreesplit, thisConnection);
                DataSet dspre = new DataSet();
                dapre.Fill(dspre, "FoliosleidosPresplit");
                FoliosleidosPresplit = dspre.Tables["FoliosleidosPresplit"];

                DataTable Foliossolreeti = new DataTable();
                string CadenaFoliossolreeti = "Select Lectura, fecha_cap From Tb_Det_Sol_Reetiquetado " +
                               "WHERE (Producto = '" + mcod + "') AND (Recibo = '" + mfol + "') AND (TarIni = '" + mtar + "') AND Estatus = 'A'";
                SqlDataAdapter dasolreeti = new SqlDataAdapter(CadenaFoliossolreeti, thisConnection);
                DataSet dssolreeti = new DataSet();
                dasolreeti.Fill(dssolreeti, "Foliossolreeti");
                Foliossolreeti = dssolreeti.Tables["Foliossolreeti"];

                if ((producido - surtido) > 0)
                {
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
                        string fechacap = ValidaCajaEtiVerde(lectura, Foliosleidos).Trim();
                        string fechacappre = ValidaCajaPreesplitVerde(lectura, FoliosleidosPresplit).Trim();
                        string fechasol = ValidaCajasolreetiqueta(lectura, Foliossolreeti).Trim();
                        thisConnection.Close();
                        if (fechacap.Length > 0)
                        {
                            n++;
                        }
                        else if (fechacappre.Length > 0)
                        {
                            n++;
                        }
                        else if (fechasol.Length > 0)
                        {
                            n++;
                        }
                        else
                        {
                            string cad = mtip + " | " + mfol + " | " + mcod + " | " + mtar + " | " + mcaj;
                            if (repetido(mtip, mfol, mcod, mtar, mcaj) != "S")
                            {
                                cajas = cajas + mcaj + "*";
                            }
                            else
                            {
                                disponible--;
                            }
                            n++;
                        }
                    }
                    cajas = cajas.TrimEnd('*');
                    strCajas = cajas.Split('*');
                    Spinner spinnerCaja = FindViewById<Spinner>(Resource.Id.cajareim);
                    System.Collections.ArrayList listaCaja = new System.Collections.ArrayList();
                    strCajas = new String[] { };
                    string[] words = cajas.Split('*');
                    Collections.AddAll(listaCaja, words);
                    comboAdapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleSpinnerItem, words);
                    spinnerCaja.Adapter = comboAdapter;
                    spinnerCaja.ItemSelected += new EventHandler<AdapterView.ItemSelectedEventArgs>(spinnercaja_ItemSelected);
                    btnguardar.Enabled = true;
                }
                else
                {
                    Spinner spinnerCaja = FindViewById<Spinner>(Resource.Id.cajareim);
                    ds.Clear();
                    System.Collections.ArrayList listaCaja = new System.Collections.ArrayList();
                    strCajas = new String[] { "Sin Cajas Disponibles" };
                    Collections.AddAll(listaCaja, strCajas);
                    comboAdapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleSpinnerItem, strCajas);
                    spinnerCaja.Adapter = comboAdapter;
                    spinnerCaja.ItemSelected += new EventHandler<AdapterView.ItemSelectedEventArgs>(spinnercaja_ItemSelected);
                }
            }
        }

        private void spinnercaja_ItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            Spinner spinner = (Spinner)sender;
            cajaselec = spinner.GetItemAtPosition(e.Position).ToString();
        }

        private void ReciboPTP()
        {
            Spinner spinner = FindViewById<Spinner>(Resource.Id.spinner3);
            ds.Clear();
            System.Collections.ArrayList listaFrutas = new System.Collections.ArrayList();
            listaFrutas.Clear();

            string estado = "S";

            thisConnection.Open();
            string Cadena = "Select ordp_fecha, ordp_linea, ordp_turno, ordp_responsable, ordp_estatus FROM TB_MSTR_ORDENES_PROD WHERE ORDP_FOLIO = '" + folio.Trim() + "'";

            SqlCommand cmd;
            cmd = new SqlCommand(Cadena);
            cmd.Connection = thisConnection;
            SqlDataReader datos;
            datos = cmd.ExecuteReader();
            while (datos.Read())
            {
                string c;
                string cadenaresponsable = "";
                int d;
                c = datos["ordp_linea"].ToString().Trim();
                d = c.ToString().Length;
                cadenaresponsable = datos["ordp_linea"].ToString().Substring(d - 2, 2).Trim() + " ";
                cadenaresponsable = cadenaresponsable + datos["ORDP_TURNO"].ToString() + " ";
                cadenaresponsable = cadenaresponsable + Convert.ToDateTime(datos["ORDP_FECHA"]).ToString("dd");
                estado = datos["ordp_estatus"].ToString().Trim();
            }

            if (estado == "C")
            {
                // Diálogo: Orden de Producción Cancelada
                DialogHelper.ShowWarningDialog(this,
                    message: "La solicitud de reimpresión no se puede realizar debido a que la orden de producción fue cancelada.",
                    positiveText: "Entendido",
                    positiveAction: (s, e) => { myEditText.Text = ""; });
                return;
            }

            thisConnection.Close();
            Cadena = "Select A.PROD_CLAVE, B.PROD_NOMBRE, A.FODP_UNIDADES, B.PROD_NOMB_INGLES, B.PROD_CODEGTIN FROM TB_DET_FINAL_ODP A, TB_CAT_PRODUCTO B WHERE A.ORDP_FOLIO = '" + folio.Trim() + "' AND A.PROD_CLAVE = B.PROD_CLAVE";
            thisConnection.Open();
            da = new SqlDataAdapter(Cadena, thisConnection);
            da.Fill(ds, "productos");
            productosnew = ds.Tables["productos"];
            thisConnection.Close();

            strFrutas = new String[productosnew.Rows.Count];
            for (int i = 0; i < productosnew.Rows.Count; i++)
            {
                int x = i;
                strFrutas[i] = productosnew.Rows[x]["PROD_CLAVE"].ToString().Trim();
            }

            Collections.AddAll(listaFrutas, strFrutas);
            comboAdapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleSpinnerItem, strFrutas);
            spinner.Adapter = comboAdapter;
            spinner.ItemSelected += new EventHandler<AdapterView.ItemSelectedEventArgs>(spinner_ItemSelected);
        }

        private void ReciboPTC()
        {
            Spinner spinner = FindViewById<Spinner>(Resource.Id.spinner3);
            ds.Clear();
            System.Collections.ArrayList listaFrutas = new System.Collections.ArrayList();
            listaFrutas.Clear();

            string estado = "S";

            thisConnection.Open();
            cmnd = thisConnection.CreateCommand();
            cmnd.CommandText = "select RPT_ESTATUS from tb_mstr_recepcion_pt WHERE A.RPT_RECIBO = '" + folio.Trim() + "'";
            estado = cmnd.ExecuteScalar().ToString().Trim();
            ds.Clear();
            thisConnection.Close();

            if (estado == "F")
            {
                // Diálogo: Recepción de Producto Terminado Cancelada
                DialogHelper.ShowWarningDialog(this,
                    message: "La solicitud de reimpresión no se puede realizar debido a que la recepción de producto terminado fue cancelada.",
                    positiveText: "Entendido",
                    positiveAction: (s, e) => { myEditText.Text = ""; });
                return;
            }

            string Cadena = "Select A.PROD_CLAVE, B.PROD_NOMBRE, A.RPTD_CANTIDAD, B.PROD_NOMB_INGLES, B.PROD_CODEGTIN FROM TB_DET_RECEPCION_PT A, TB_CAT_PRODUCTO B WHERE A.RPT_RECIBO = '" + folio.Trim() + "' AND A.PROD_CLAVE = B.PROD_CLAVE";

            thisConnection.Open();
            da = new SqlDataAdapter(Cadena, thisConnection);
            da.Fill(ds, "productos");
            productosnew = ds.Tables["productos"];
            thisConnection.Close();

            strFrutas = new String[productosnew.Rows.Count];
            for (int i = 0; i < productosnew.Rows.Count; i++)
            {
                int x = i;
                strFrutas[i] = productosnew.Rows[x]["PROD_CLAVE"].ToString().Trim();
            }

            Collections.AddAll(listaFrutas, strFrutas);
            comboAdapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleSpinnerItem, strFrutas);
            spinner.Adapter = comboAdapter;
            spinner.ItemSelected += new EventHandler<AdapterView.ItemSelectedEventArgs>(spinner_ItemSelected);
        }

        private string ValidaCajaEtiVerde(string cadena, DataTable foliosleidos)
        {
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
            string Valor = "";
            DataRow[] datos = foliosleidos.Select("Eti_Lectura = '" + cadena + "'");
            if (datos.Length > 0)
            {
                Valor = datos[0].ItemArray[1].ToString();
            }
            return Valor;
        }

        private string ValidaCajasolreetiqueta(string cadena, DataTable foliosleidos)
        {
            string Valor = "";
            DataRow[] datos = foliosleidos.Select("Lectura = '" + cadena + "'");
            if (datos.Length > 0)
            {
                Valor = datos[0].ItemArray[1].ToString();
            }
            return Valor;
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

        private void LoadConnection()
        {
            string folder = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);
            string dbPath = System.IO.Path.Combine(folder, "Split_Trailer.db3");

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
    }
}