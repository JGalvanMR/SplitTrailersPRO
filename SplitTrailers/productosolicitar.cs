using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Text;
using Android.Text.Style;
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
using System.Net;
using System.Net.Mail;

namespace SplitTrailers
{
    [Activity(Label = "Solicitar Producto")]
    class productosolicitar : Activity
    {
        public static SQLiteConnection db;
        SqlConnection thisConnection = new SqlConnection(MainActivity.cadenaConexion);
        SqlDataAdapter da;
        DataSet ds = new DataSet();
        SqlCommand cmnd = new SqlCommand();
        SqlCommand cmnd1 = new SqlCommand();
        SqlDataReader reader1;
        string query = "", prod_clave = "", folio = "", tipo = "", cadena = "", prod_nombre = "";
        int tarima = 0, caja = 0, tarimaf = 0;
        bool find = false;
        string pedidoprincipal = "";
        public static string cvvehiculo, cvresponsable, responsable, imei, currentVersionName;

        int faltante = 0;

        TextView pedidoencaptura;
        EditText comentario;
        Button Guardar;
        TextView porarmartexto;
        EditText faltanteporarmar;

        ArrayAdapter<String> comboAdapter;
        String[] strFrutas;

        string valorproducto = "";
        string claveprodcuto = "";

        DataTable responsables = new DataTable("responsables");
        DataTable Inven = new DataTable();
        DataTable Semanas = new DataTable();

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.SolicitarProducto);
            pedidoencaptura = FindViewById<TextView>(Resource.Id.textpedsol);

            comentario = FindViewById<EditText>(Resource.Id.obsprod);


            porarmartexto = FindViewById<TextView>(Resource.Id.porarmar);
            faltanteporarmar = FindViewById<EditText>(Resource.Id.cantidadfaltante);


            LoadConnection();

            CreaTable();



            pedidoprincipal = Intent.GetStringExtra("ordenventa");
            pedidoencaptura.Text = "Pedido Actual: " + pedidoprincipal.ToString();


            cvvehiculo = Intent.GetStringExtra("cvcamioneta");
            cvresponsable = Intent.GetStringExtra("cvresponsable");
            responsable = Intent.GetStringExtra("responsable");

            currentVersionName = Intent.GetStringExtra("currentVersionName");
            imei = Intent.GetStringExtra("imei");


            db.Query<Pedidos>("delete from  [Pedidos]");
            db.Query<ConPedidos>("delete from  [ConPedidos]");
            db.Query<xLote>("delete from  [xLote]");
            db.Query<xLoteFinal>("delete from  [xLoteFinal]");
            db.Query<xprod>("delete from  [xprod]");

            productosporsurtir();


            Guardar = FindViewById<Button>(Resource.Id.btnsolicitarprod);
            Guardar.Click += BtnGuardar_Click;
            Guardar.Enabled = false;
            Int32 anio = DateTime.Now.Year;
            thisConnection.Open();
            String Cadena = "SELECT semana, ano, fecha1, fecha2, generado, indica FROM tb_cat_semanas Where ano >= '" + (anio - 1).ToString() + "' and ano <= '" + (anio + 1).ToString() + "'";
            DataSet ds = new DataSet();
            SqlDataAdapter da = new SqlDataAdapter(Cadena, thisConnection);
            da.Fill(ds, "Semanas");

            Semanas = ds.Tables["Semanas"];
            thisConnection.Close();



        }

        private void BtnGuardar_Click(object sender, EventArgs e)
        {

            int productoexistente = 0;
            //validar si existe un envio previo del correo!
            DateTime horaenvio = DateTime.Now;
            DateTime horaactual = DateTime.Now;
            int existe = 0;
            thisConnection.Open();
            string Cadena = "SELECT GETDATE()";
            SqlCommand cmd = new SqlCommand(Cadena, thisConnection);
            SqlDataReader reader1;
            reader1 = cmd.ExecuteReader();
            while (reader1.Read())
            {
                horaactual = reader1.GetSqlDateTime(0).Value;
            }

            Cadena = "SELECT TOP (1) fecha FROM tb_det_sol_producto WHERE nom_usu = '" + responsable.ToString().Trim() + "' AND producto = '" + claveprodcuto + "' AND ord_vent = '" + pedidoprincipal.ToString().Trim() + "' Order by fecha DESC";
            cmd = new SqlCommand(Cadena, thisConnection);
            reader1 = cmd.ExecuteReader();
            while (reader1.Read())
            {
                existe = 1;
                horaenvio = reader1.GetSqlDateTime(0).Value;
            }
            thisConnection.Close();
            double differenceInMinuntos = 0.0;
            string formatted = "";

            if (existe == 1)
            {
                TimeSpan ts = horaactual - horaenvio;
                differenceInMinuntos = ts.TotalMinutes;
            }


            if (faltanteporarmar.Text == "0")
            {
                Toast.MakeText(this, "Debe Ingresar Un Valor Valido Para el Faltante\r\n", ToastLength.Short).Show();
                return;
            }

            if (Convert.ToInt32(faltanteporarmar.Text) > faltante)
            {
                Toast.MakeText(this, "La cantidad por Armar No Puede Superar Al Faltante Real\r\n", ToastLength.Short).Show();
                return;
            }

            if (existe == 0 || differenceInMinuntos > 15.00)
            {

                String mBody = "";
                mBody = "Buen dia <br/> Soy el armador " + responsable.ToString().Trim() + "<br/>";
                mBody += "<p> Solicito el siguiente Producto Para Continuar con el armado de la orden " + pedidoprincipal.ToString().Trim() + "<br/>";
                mBody += "<font style='color:red'>" + valorproducto.Trim() + "</font> Con un faltante de  <font style='color:green'> " + faltante + " Total de Cajas, Del Cual Requiero '" + faltanteporarmar.Text + "' Cajas Para los Split.</font><br/>";
                if (comentario.Text.Trim().Length > 0)
                {
                    mBody += "Incluyo las siguientes observaciones para mayor entendimiento: <font style='color:red'>" + comentario.Text + "</font><br/>";
                }

                Genera();
                mBody += "<font style='color:blue'>Detalle del producto en el monitor de Caducidades:</font><br/>";



                mBody += "<table style = 'width:100%'><tr><th style='font-size:70%;'>FOLIO</th><th style='font-size:70%;'>Fecha ELA</th><th style='font-size:70%;'>LOTE</th><th style='font-size:70%;'>FECHA CAD</th><th style='font-size:70%;'>DIAS/T</th><th style='font-size:70%;'>CANTFISICO</th><th style='font-size:70%;'>EXISTENCIA</th><th style='font-size:70%;'>UBICACION</th><th style='font-size:70%;'>PRESPLIT</th></tr>";


                foreach (DataRow fila in Inven.Rows)
                {
                    mBody += "<tr><td style='color:#456789;font-size:70%;'>" + fila["Nombre"].ToString() + "</td><td style='text-align: center; vertical-align: middle; color:#456789;font-size:70%;'>" + fila["FechaEla"].ToString() + "</td><td style='text-align: center; vertical-align: middle; color:#456789;font-size:70%;'>" + fila["Lote"].ToString() + "</td><td style='text-align: center; vertical-align: middle; color:#456789;font-size:70%;'>" + fila["FecCad"].ToString() + "</td><td style='text-align: center; vertical-align: middle;  color:#456789;font-size:70%;'>" + fila["Dias"].ToString() + "</td><td style=' text-align: center; vertical-align: middle; color:#456789;font-size:70%;'>" + fila["Existencia"].ToString() + "</td><td style='text-align: center; vertical-align: middle; color:#456789;font-size:70%;'>" + fila["Cantidad"].ToString() + "</td><td style='text-align: center; vertical-align: middle; color:#456789;font-size:70%;'>" + fila["Ubica"].ToString() + "</td><td style='text-align: center; vertical-align: middle; color:#456789;font-size:70%;'>" + fila["Presplit"].ToString() + "</td></tr>";
                    if (Convert.ToInt32(fila["Dias"].ToString()) > 8 && (fila["Existencia"].ToString().Trim() != "" || fila["Existencia"].ToString().Trim() != "0"))
                    {
                        productoexistente = productoexistente + 1;
                    }
                }

                mBody += "</table>";


                mBody += "Para cualquier aclaracion, Informacion o comentario, favor de solicitar al Supervisor en turno<br/>Gracias<br/><font style='color:blue'>Correo Enviado Desde Sistema Split Trailer</font>";




                if (productoexistente > 0)
                {
                    // DIÁLOGO: Producto Disponible en Inventario
                    DialogHelper.ShowWarningDialog(this,
                        message: "Existen " + productoexistente + " disponibles para carga. Por favor, cargue el producto y reinicie el proceso.",
                        positiveText: "Entendido");
                    return;
                }
                else
                {
                    string TB = "tb_mstr_pedidos_nal";

                    if (Convert.ToInt32(pedidoprincipal.ToString().Trim()) < 400000)
                    {
                        TB = "tb_mstr_pedidos_Exp";
                    }

                    string vendedor = "";
                    thisConnection.Open();
                    Cadena = "SELECT usu_email FROM " + TB + " JOIN tb_cat_usuarios ON usu_login = pdn_elaboro WHERE (pdn_folio = '" + Convert.ToInt32(pedidoprincipal.ToString().Trim()) + "')";
                    cmd = new SqlCommand(Cadena, thisConnection);
                    reader1 = cmd.ExecuteReader();
                    while (reader1.Read())
                    {
                        vendedor = reader1.GetString(0).ToString().Trim();
                    }
                    thisConnection.Close();

                    SendMail("jgonzalez@mrlucky.com.mx; supervisorcamfrias@mrlucky.com.mx; produccion@mrlucky.com.mx; ensaladas@mrlucky.com.mx; embarques@mrlucky.com.mx; fresco@mrlucky.com.mx; mprima@mrlucky.com.mx; " + vendedor, mBody, "Solicitud de producto para Orden de Venta " + pedidoprincipal);
                    //SendMail("dmunoz@mrlucky.com.mx; logistica@mrlucky.com.mx", mBody, "Solicitud de producto para Orden de Venta " + pedidoprincipal);
                    thisConnection.Open();
                    string cadena = "INSERT INTO   tb_det_sol_producto (fecha, imei, nom_usu, producto, observaciones, cantidad, ord_vent) " +
                               "VALUES(GETDATE(),'" + imei + "','" + responsable + "','" + claveprodcuto + "','" + comentario.Text.Trim() + "','" + faltante + "','" + pedidoprincipal.ToString().Trim() + "')";
                    cmd = new SqlCommand(cadena, thisConnection);
                    cmd.ExecuteNonQuery();
                    thisConnection.Close();


                    // DIÁLOGO: Solicitud Enviada
                    DialogHelper.ShowSuccessDialog(this,
                        message: "El correo fue enviado correctamente.",
                        positiveText: "Entendido",
                        positiveAction: (s, e) => { Finish(); });
                    return;
                }

            }
            else
            {
                // DIÁLOGO: No se puede enviar solicitud (por tiempo)
                DialogHelper.ShowErrorDialog(this,
                    message: "La solicitud no se puede completar debido a que aún no han pasado 15 minutos.",
                    positiveText: "Entendido");
                return;
            }
        }


        //***********************************************************************CODIGO EXPERIMENTAL PARA MANDAR UNA COPIA DEL PRODUCTO SEGUN EL MONITOR DE CADUCIADES***************************************************************************************************************************************

        private void CreaTable()
        {
            Inven.Columns.Add("Nombre", typeof(string));
            Inven.Columns.Add("FechaEla", typeof(string)); //int
            Inven.Columns.Add("Lote", typeof(string));
            Inven.Columns.Add("FecCad", typeof(string));
            Inven.Columns.Add("FecCadTeo", typeof(string));
            Inven.Columns.Add("Dias", typeof(int));
            Inven.Columns.Add("Existencia", typeof(int));
            Inven.Columns.Add("Cantidad", typeof(int));
            Inven.Columns.Add("Conse", typeof(int));
            Inven.Columns.Add("Prod", typeof(string));
            Inven.Columns.Add("CvePro", typeof(string));
            Inven.Columns.Add("Tipo", typeof(string));
            Inven.Columns.Add("FechaCad", typeof(string));
            Inven.Columns.Add("Ubica", typeof(string));
            Inven.Columns.Add("Tarima", typeof(string));
            Inven.Columns.Add("Presplit", typeof(int));

        }


        private void Genera()
        {
            Inven.Rows.Clear();
            thisConnection.Open();
            DataSet ds = new DataSet();
            DataTable Info = new DataTable();
            string Cadena = "SELECT RECIBO,PROD_CLAVE, TARIMA, SUM(CAJAS) as CAJAS FROM tb_det_embarque " +
                            " WHERE SUBSTRING(FECHACAP,1,10) = '" + System.DateTime.Now.ToString("dd-MM-yyyy") + "' AND Estatus != 'C' " +
                            " AND ((CONVERT(INT,SUBSTRING(FECHACAP,12,2)) > 8 AND (CONVERT(INT,SUBSTRING(FECHACAP,12,2)) != 12) AND SUBSTRING(FECHACAP,21,1) = 'a') OR SUBSTRING(FECHACAP,21,1) = 'p') AND prod_clave = '" + claveprodcuto + "'" +
                            " GROUP BY PROD_CLAVE,RECIBO, TARIMA" +
                            " ORDER BY PROD_CLAVE,RECIBO, TARIMA";
            ds = new DataSet();
            Info = new DataTable();
            SqlDataAdapter da = new SqlDataAdapter(Cadena, thisConnection);
            da.Fill(ds, "Surtido");
            DataTable Surtido = new DataTable();
            Surtido = ds.Tables["Surtido"];
            Cadena = "SELECT PROD_CLAVE, SUM(CAJAS) AS CAJAS FROM tb_det_split WHERE estatus = 'A' AND prod_clave = '" + claveprodcuto + "' GROUP BY prod_clave ORDER BY prod_clave";
            ds = new DataSet();
            Info = new DataTable();
            da = new SqlDataAdapter(Cadena, thisConnection);
            da.Fill(ds, "SPLIT");
            DataTable SPLIT = ds.Tables["SPLIT"];
            Cadena = "SELECT  PROD_CLAVE,SUM(ETIQUETA) AS CAJAS FROM TB_DET_TRAZABILIDAD WHERE PTI_FECHA =  '" + System.DateTime.Now.ToString("dd-MM-yyyy") + "' AND prod_clave = '" + claveprodcuto + "' AND tipo = 'PTC'" +
                     " GROUP BY PROD_CLAVE ORDER BY PROD_CLAVE ";
            ds = new DataSet();
            DataTable PTC = new DataTable();
            da = new SqlDataAdapter(Cadena, thisConnection);
            da.Fill(ds, "PTC");
            PTC = ds.Tables["PTC"];
            //string Cadena = "SELECT PROD_NOMBRE, RECIBO, TARIMA, PTI_FECHA, LOTE, FECHA_CAD, ETIQUETA, SURTIDO, PROD_CLAVE FROM TB_DET_TRAZABILIDAD WHERE PTI_ESTATUS_SUR =  ' ' AND TIPO = 'PTC'  ORDER BY PROD_NOMBRE,RECIBO,PTI_CLAVE ";
            Cadena = "SELECT C.PROD_NOMBRE, A.RECIBO, A.TARIMA, A.PTI_FECHA, A.LOTE, A.FECHA_CAD, A.ETIQUETA, A.SURTIDO, A.PROD_CLAVE, A.UBICACION " +
                            " FROM TB_DET_TRAZABILIDAD A, tb_mstr_recepcion_pt B, tb_cat_producto C " +
                            " WHERE A.PTI_ESTATUS_SUR =  ' ' AND A.prod_clave = '" + claveprodcuto + "' AND A.TIPO = 'PTC'  AND A.recibo = B.rpt_recibo AND A.PROD_CLAVE = C.PROD_CLAVE AND B.rpt_estatus = ' ' AND (B.rpt_tipo != 'TR' OR (B.rpt_tipo = 'TR' AND B.RPT_INVENTARIO = 'S'))" +
                            " ORDER BY PROD_NOMBRE,RECIBO,PTI_CLAVE ";
            ds = new DataSet();
            Info = new DataTable();
            da = new SqlDataAdapter(Cadena, thisConnection);
            da.Fill(ds, "Info");
            Info = ds.Tables["Info"];

            Cadena = "SELECT Eti_Recibo,Eti_Producto, Eti_TarIni, COUNT(Eti_Caja) as CAJAS FROM Tb_Det_Etiqueta_Presplit  WHERE Fecha = '" + System.DateTime.Now.ToString("dd-MM-yyyy") + "' AND Eti_Producto = '" + claveprodcuto + "' AND Estatus = 'A'  GROUP BY Eti_Producto,Eti_Recibo, Eti_TarIni ORDER BY Eti_Producto, Eti_Recibo, Eti_TarIni";
            ds = new DataSet();
            DataTable TempPresplit = new DataTable();
            da = new SqlDataAdapter(Cadena, thisConnection);
            da.Fill(ds, "TempPresplit");
            TempPresplit = ds.Tables["TempPresplit"];

            Cadena = "SELECT prod_clave, inv_teorico, inv_fisico " +
                     "FROM tb_mstr_inventario_fisico " +
                     "WHERE invpt_fecha = '" + System.DateTime.Now.ToString("dd-MM-yyyy") + "' AND prod_clave = '" + claveprodcuto + "' ORDER BY PROD_CLAVE ";
            ds = new DataSet();
            da = new SqlDataAdapter(Cadena, thisConnection);
            da.Fill(ds, "Teo");
            DataTable Teorico = new DataTable();
            Teorico = ds.Tables["Teo"];
            //dataGridView2.DataSource = Inven ;
            string Mnom = "", Nprod = "";
            int INI = 1, totp = 0, totg = 0, tott = 0;
            int prodPTC = 0;
            Int32 Teo = 0, Fisi = 0, Surti = 0;
            foreach (DataRow row in Info.Rows)
            {
                if (INI == 1)
                {
                    Mnom = row["PROD_NOMBRE"].ToString();
                    Nprod = row["prod_clave"].ToString();
                    //Inven.Rows.Add(Mnom, "", "", "", "", 0, 0, 0, 1, Mnom, Nprod, ""); se bloqueo el 8 de abril 2017 original
                    Inven.Rows.Add(Mnom, "", "", "", "", 0, 0, 0, 1, "", "", "");
                    INI = 0;
                }

                prodPTC = 1;
                //if (Mnom == "TOMATE ORGANICO MR.LUCKY CHERRY 12/10 OZ          ")
                //{ 
                //}

                string fol = row["RECIBO"].ToString();
                if (Mnom != row["PROD_NOMBRE"].ToString())
                {
                    Teo = 0; Fisi = 0; Surti = 0;
                    foreach (DataRow Row in Teorico.Select("PROD_CLAVE = '" + Nprod + "'"))
                    {
                        Teo = Convert.ToInt32(Row["INV_TEORICO"]);
                        Fisi = Convert.ToInt32(Row["INV_FISICO"]);
                    }
                    foreach (DataRow Row in Surtido.Select("prod_clave = '" + Nprod + "'"))
                    {
                        Surti = Surti + Convert.ToInt32(Row["Cajas"]);
                    }
                    foreach (DataRow Row in SPLIT.Select("prod_clave = '" + Nprod + "'"))
                    {
                        Surti = Surti + Convert.ToInt32(Row["cajas"]);
                    }
                    foreach (DataRow Row in PTC.Select("Prod_CLAVE = '" + Nprod + "'"))
                    {
                        tott = Convert.ToInt32(Row["Cajas"]);
                    }
                    //Inven.Rows.Add("TOTAL " + Mnom, "", "Teorico:", (Teo + tott).ToString(), "", Fisi + tott, 0, totp, 3, Mnom, Nprod, "", "99991231"); se bloqueo el 8 de abril 2017 original
                    Inven.Rows.Add("TOTAL " + Mnom, "", "", "", "", (Teo + tott - Surti).ToString(), Fisi + tott - Surti, totp, 3, Mnom, Nprod, "", "99991231");
                    Nprod = row["prod_clave"].ToString();
                    Mnom = row["PROD_NOMBRE"].ToString();
                    //Inven.Rows.Add(Mnom, "", "", Mnom, "", 0, 0, 0, 1, Mnom, Nprod, ""); se bloqueo el 8 de abril 2017 original
                    Inven.Rows.Add(Mnom, "", "", "", "", 0, 0, 0, 1, "", "", "");
                    totp = 0; tott = 0;
                }

                totp = totp + (Convert.ToInt32(row["ETIQUETA"]) - Convert.ToInt32(row["SURTIDO"]));
                totg = totg + (Convert.ToInt32(row["ETIQUETA"]) - Convert.ToInt32(row["SURTIDO"]));
                //if (Convert.ToDateTime(row["PTI_FECHA"]).ToString("dd-MM-yyyy") == System.DateTime.Now.ToString("dd-MM-yyyy"))
                //    tott = tott + (Convert.ToInt32(row["ETIQUETA"])); // - Convert.ToInt32(row["SURTIDO"]));
                TimeSpan Mdias = TimeSpan.Zero;
                DateTime FecCad = Convert.ToDateTime(row["PTI_FECHA"]);
                string Ubica = Convert.ToString(row["ubicacion"]);
                string fechacaduemulada = "";
                if (row["FECHA_CAD"].ToString().Trim().Length > 0)
                {
                    string[] Fechacaducidad = row["FECHA_CAD"].ToString().Split('/');
                    fechacaduemulada = Fechacaducidad[1] + "/" + Fechacaducidad[0] + "/" + Fechacaducidad[2];

                    string PRUEBA = row["FECHA_CAD"].ToString().Trim();

                    try
                    {
                        Mdias = Convert.ToDateTime(row["FECHA_CAD"].ToString().Trim()) - System.DateTime.Now.AddDays(-1);
                        FecCad = Convert.ToDateTime(row["FECHA_CAD"]);
                    }
                    catch
                    {
                        Mdias = Convert.ToDateTime(fechacaduemulada.ToString().Trim()) - System.DateTime.Now.AddDays(-1);
                        FecCad = Convert.ToDateTime(fechacaduemulada);
                    }

                }
                else
                {
                    if (Mnom.Contains("BETABEL"))
                        FecCad = FecCad.AddDays(60);
                    else
                        if (Mnom.Contains("AJO"))
                        FecCad = FecCad.AddDays(180);
                    else
                            if (Mnom.Contains("ADEREZO") || Mnom.Contains("VINAGRETA") || Mnom.Contains("QUESO"))
                        FecCad = FecCad.AddDays(90);
                    else
                        FecCad = FecCad.AddDays(14);
                    Mdias = FecCad - System.DateTime.Now.AddDays(-1);
                }

                int SURTISPRESPLIT = 0;
                foreach (DataRow Row in TempPresplit.Select("Eti_Recibo = '" + row["RECIBO"].ToString().Trim() + "' AND Eti_Producto = '" + Nprod.Trim() + "' AND Eti_TarIni = '" + row["TARIMA"].ToString().Trim() + "' "))
                {
                    SURTISPRESPLIT = Convert.ToInt32(Row["CAJAS"]);
                }
                string MNewFec = "";
                try
                {
                    MNewFec = (row["FECHA_CAD"].ToString().Trim().Length > 0) ? Convert.ToDateTime(fechacaduemulada).ToString("yyyyMMdd") : FecCad.ToString("yyyyMMdd");
                }
                catch
                {
                    MNewFec = (row["FECHA_CAD"].ToString().Trim().Length > 0) ? Convert.ToDateTime(fechacaduemulada).ToString("yyyyMMdd") : FecCad.ToString("yyyyMMdd");
                }

                Inven.Rows.Add(row["RECIBO"].ToString() + "-" + row["TARIMA"].ToString().Trim(), Convert.ToDateTime(row["PTI_FECHA"].ToString()).ToString("dd/MM/yyyy"), row["LOTE"].ToString(), (row["FECHA_CAD"].ToString().Trim().Length > 0) ? row["FECHA_CAD"] : FecCad.ToString("dd-MM-yyyy"), "", Mdias.Days, row["ETIQUETA"], (Convert.ToInt32(row["ETIQUETA"]) - Convert.ToInt32(row["SURTIDO"])), 2, Mnom, row["PROD_CLAVE"], "PTC", MNewFec, Ubica, row["TARIMA"].ToString().Trim(), SURTISPRESPLIT);

            }
            Teo = 0; Fisi = 0; Surti = 0;
            foreach (DataRow Row in Teorico.Select("PROD_CLAVE = '" + Nprod + "'"))
            {
                Teo = Convert.ToInt32(Row["INV_TEORICO"]);
                Fisi = Convert.ToInt32(Row["INV_FISICO"]);
            }
            foreach (DataRow Row in Surtido.Select("prod_clave = '" + Nprod + "'"))
            {
                Surti = Surti + Convert.ToInt32(Row["Cajas"]);
            }
            foreach (DataRow Row in SPLIT.Select("prod_clave = '" + Nprod + "'"))
            {
                Surti = Surti + Convert.ToInt32(Row["cajas"]);
            }
            foreach (DataRow Row in PTC.Select("prod_clave = '" + Nprod + "'"))
            {
                tott = Convert.ToInt32(Row["Cajas"]);
            }
            //Inven.Rows.Add("TOTAL " + Mnom, "", "", "TOTAL " + Mnom, "", 0, 0, totp, 3, Mnom, Nprod, "", "99991231"); se bloqueo el 8 de abril 2017 original
            if (prodPTC > 0)
            {
                Inven.Rows.Add("TOTAL " + Mnom, "", "", "", "", (Teo + tott - Surti).ToString(), Fisi + tott - Surti, totp, 3, Mnom, Nprod, "", "99991231");
            }
            // RECIBOS DE PRODUCCION
            totp = 0;
            //totg = 0;
            tott = 0;
            INI = 1;//
            Cadena = "SELECT  B.CVE_PROD,SUM(B.NUM_CAJAS) AS CAJAS FROM TB_DET_ETI_FINAL B WHERE B.FECHA =  '" + System.DateTime.Now.ToString("dd-MM-yyyy") + "' AND B.cve_prod = '" + claveprodcuto + "'" +
                     " GROUP BY CVE_PROD ORDER BY CVE_PROD ";
            ds = new DataSet();
            DataTable PTP = new DataTable();
            da = new SqlDataAdapter(Cadena, thisConnection);
            da.Fill(ds, "PTP");
            PTP = ds.Tables["PTP"];
            Cadena = "SELECT A.PROD_NOMBRE, B.FOLIO, B.TARIMA, B.FECHA, B.NUM_LOTE, B.NUM_CAJAS, B.CAJAS_SUR, B.CVE_PROD, B.UBICACION, b.fechacad FROM TB_DET_ETI_FINAL B, TB_CAT_PRODUCTO A WHERE B.ESTATUS_SUR =  ' ' AND B.CVE_PROD = A.PROD_CLAVE AND B.ETIQUETA = 'S' AND B.cve_prod = '" + claveprodcuto + "' ORDER BY A.PROD_NOMBRE,B.FOLIO,B.TARIMA ";
            ds = new DataSet();
            DataTable Info2 = new DataTable();
            da = new SqlDataAdapter(Cadena, thisConnection);
            da.Fill(ds, "Info2");
            Info2 = ds.Tables["Info2"];
            //datos = cmd.ExecuteReader();
            //while (datos.Read())
            //{
            //    Var_LoteSem = datos["semana"].ToString() + "-" + OleFE.Value.ToString("ddd").ToUpper();
            //}
            //Var_LoteSem = (Var_LoteSem.Trim().Length > 0) ? Var_LoteSem.Substring(0, 5) : ""; 
            int valorPTP = 0;
            foreach (DataRow row in Info2.Rows)
            {
                valorPTP = 1;
                if (INI == 1)
                {
                    Mnom = row["PROD_NOMBRE"].ToString();
                    Nprod = row["cve_prod"].ToString();
                    Inven.Rows.Add(Mnom, "", "", "", "", 0, 0, 0, 1, "", "");
                    INI = 0;
                }

                if (Mnom != row["PROD_NOMBRE"].ToString())
                {
                    Teo = 0; Fisi = 0; Surti = 0;
                    foreach (DataRow Row in Teorico.Select("PROD_CLAVE = '" + Nprod + "'"))
                    {
                        Teo = Convert.ToInt32(Row["INV_TEORICO"]);
                        Fisi = Convert.ToInt32(Row["INV_FISICO"]);
                    }
                    foreach (DataRow Row in Surtido.Select("prod_clave = '" + Nprod + "'"))
                    {
                        Surti = Surti + Convert.ToInt32(Row["Cajas"]);
                    }
                    foreach (DataRow Row in SPLIT.Select("prod_clave = '" + Nprod + "'"))
                    {
                        Surti = Surti + Convert.ToInt32(Row["cajas"]);
                    }
                    foreach (DataRow Row in PTP.Select("cve_prod = '" + Nprod + "'"))
                    {
                        tott = Convert.ToInt32(Row["Cajas"]);
                    }
                    //Inven.Rows.Add("TOTAL " + Mnom, "", "Teorico:", (Teo + tott).ToString(), "", Fisi + tott, 0, totp, 3, Mnom, Nprod, "", "99991231"); se bloqueo el 8 de abril 2017 original
                    //if (Nprod == "16001LES18")
                    //    MessageBox.Show("hOLA");
                    Inven.Rows.Add("TOTAL " + Mnom, "", "", "", "", (Teo + tott - Surti).ToString(), Fisi + tott - Surti, totp, 3, Mnom, Nprod, "", "99991231");
                    //Inven.Rows.Add("TOTAL " + Mnom, "", "Teorico:", (Teo).ToString(), "", Fisi, 0, totp, 3, Mnom, Nprod, "", "99991231");
                    Mnom = row["PROD_NOMBRE"].ToString();
                    Nprod = row["cve_prod"].ToString();
                    //Inven.Rows.Add(Mnom, "", "", "", "", 0, 0, 0, 1, Mnom, Nprod);se bloqueo el 8 de abril 2017 original
                    Inven.Rows.Add(Mnom, "", "", "", "", 0, 0, 0, 1, "", "");
                    totp = 0; tott = 0;
                }
                totp = totp + (Convert.ToInt32(row["NUM_CAJAS"]) - Convert.ToInt32(row["CAJAS_SUR"]));
                totg = totg + (Convert.ToInt32(row["NUM_CAJAS"]) - Convert.ToInt32(row["CAJAS_SUR"]));
                //if (Convert.ToDateTime(row["FECHA"]).ToString("dd-MM-yyyy") == System.DateTime.Now.ToString("dd-MM-yyyy"))
                //   tott = tott + (Convert.ToInt32(row["NUM_CAJAS"])); // - Convert.ToInt32(row["CAJAS_SUR"]));
                TimeSpan Mdias = TimeSpan.Zero;
                DateTime FecCad = Convert.ToDateTime(row["FECHA"]);
                string Ubica = Convert.ToString(row["ubicacion"]);
                string Mlot = "", Mfeca = "";
                if (row["NUM_LOTE"].ToString().Trim().Length > 0)
                {
                    int Mtam = row["NUM_LOTE"].ToString().Trim().Length;
                    if (row["fechacad"].ToString().Trim().Length > 0)
                        Mfeca = row["fechacad"].ToString().Substring(4, 2) + "/" + row["fechacad"].ToString().Substring(6, 2) + "/" + row["fechacad"].ToString().Substring(0, 4);
                    else
                        Mfeca = ConviertetoFecha(row["NUM_LOTE"].ToString().Substring((Mtam == 12) ? 7 : 6, 5));

                    string Mfol = row["FOLIO"].ToString();
                    Mdias = Convert.ToDateTime(Mfeca) - System.DateTime.Now.AddDays(-1);
                }
                else
                {
                    if (Mnom.Contains("BETABEL"))
                        FecCad = FecCad.AddDays(60);
                    else
                        if (Mnom.Contains("AJO"))
                        FecCad = FecCad.AddDays(180);
                    else
                            if (Mnom.Contains("ADEREZO") || Mnom.Contains("VINAGRETA") || Mnom.Contains("QUESO"))
                        FecCad = FecCad.AddDays(90);
                    else
                        FecCad = FecCad.AddDays(14);
                    Mdias = FecCad - System.DateTime.Now.AddDays(-1);
                    Mfeca = FecCad.ToString("MM-dd-yyyy");
                }
                Mlot = Lote(row["Fecha"].ToString());  //row["NUM_LOTE"].ToString().Substring(0, 4);
                //    Mdias = Convert.ToDateTime(row["FECHA_CAD"]) - System.DateTime.Now;
                string MNewFec = Convert.ToDateTime(Mfeca).ToString("yyyyMMdd");
                int SURTISPRESPLIT = 0;
                foreach (DataRow Row in TempPresplit.Select("Eti_Recibo = '" + row["FOLIO"].ToString().Trim() + "' AND Eti_Producto = '" + Nprod.Trim() + "' AND Eti_TarIni = '" + row["TARIMA"].ToString().Trim() + "' "))
                {
                    SURTISPRESPLIT = Convert.ToInt32(Row["CAJAS"]);
                }




                Inven.Rows.Add(row["FOLIO"].ToString() + "-" + row["TARIMA"].ToString().Trim(), Convert.ToDateTime(row["FECHA"].ToString()).ToString("dd/MM/yyyy"), Mlot, Mfeca, "", Mdias.Days, row["NUM_CAJAS"], (Convert.ToInt32(row["NUM_CAJAS"]) - Convert.ToInt32(row["CAJAS_SUR"])), 2, Mnom, Nprod, "PTP", MNewFec, Ubica, row["TARIMA"].ToString().Trim(), SURTISPRESPLIT);
            }
            Teo = 0; Fisi = 0; Surti = 0;
            foreach (DataRow Row in Teorico.Select("PROD_CLAVE = '" + Nprod + "'"))
            {
                Teo = Convert.ToInt32(Row["INV_TEORICO"]);
                Fisi = Convert.ToInt32(Row["INV_FISICO"]);
            }
            foreach (DataRow Row in Surtido.Select("prod_clave = '" + Nprod + "'"))
            {
                Surti = Surti + Convert.ToInt32(Row["Cajas"]);
            }
            foreach (DataRow Row in SPLIT.Select("prod_clave = '" + Nprod + "'"))
            {
                Surti = Surti + Convert.ToInt32(Row["cajas"]);
            }
            foreach (DataRow Row in PTP.Select("cve_prod = '" + Nprod + "'"))
            {
                tott = Convert.ToInt32(Row["Cajas"]);
            }

            if (valorPTP > 0)
            {
                Inven.Rows.Add("TOTAL " + Mnom, "", "", "", "", (Teo + tott - Surti).ToString(), Fisi + tott - Surti, totp, 3, Mnom, Nprod, "", "99991231");
            }


            Inven.DefaultView.Sort = "Prod, Conse, FechaCad ASC";
            Inven = Inven.DefaultView.ToTable();

            thisConnection.Close();
        }



        private string ConviertetoFecha(string FEC)
        {
            string mdia = FEC.Substring(3, 2);
            string mmes = FEC.Substring(0, 3);
            string nmes = "";
            if (mmes == "ENE")
                nmes = "01";
            if (mmes == "FEB")
                nmes = "02";
            if (mmes == "MAR")
                nmes = "03";
            if (mmes == "ABR")
                nmes = "04";
            if (mmes == "MAY")
                nmes = "05";
            if (mmes == "JUN")
                nmes = "06";
            if (mmes == "JUL")
                nmes = "07";
            if (mmes == "AGO")
                nmes = "08";
            if (mmes == "SEP")
                nmes = "09";
            if (mmes == "OCT")
                nmes = "10";
            if (mmes == "NOV")
                nmes = "11";
            if (mmes == "DIC")
                nmes = "12";
            int MES = System.DateTime.Now.Month;
            int anio = System.DateTime.Now.Year + (MES == 12 && nmes == "01" ? 1 : 0);
            //if (Convert.ToInt32(nmes) < MES)
            //    anio++;
            string cad = mdia + "/" + nmes + "/" + anio.ToString();
            return cad;
        }


        private string Lote(string Fecha)
        {
            string Cad = "";
            foreach (DataRow Row in Semanas.Select("fecha1 <= '" + Fecha + "' AND fecha2 >= '" + Fecha + "'"))
            {
                Cad = Row["semana"].ToString() + "-" + Convert.ToDateTime(Fecha).ToString("ddd").ToUpper();
            }
            Cad = (Cad.Trim().Length > 0) ? Cad.Substring(0, 5) : "";
            return Cad;
        }



        //***********************************************************************CODIGO EXPERIMENTAL PARA MANDAR UNA COPIA DEL PRODUCTO SEGUN EL MONITOR DE CADUCIADES***************************************************************************************************************************************


        public void SendMail(string Dest, string mBody, string mAsunto)
        {
            MailMessage msg = new MailMessage();
            MailMessage email = new MailMessage();

            string[] destinatarios = Dest.Split(';');
            foreach (string destinos in destinatarios)
            {
                email.To.Add(new MailAddress(destinos));
            }
            email.CC.Add(new MailAddress("mdelrio@mrlucky.com.mx"));
            email.CC.Add(new MailAddress("ahernandez@mrlucky.com.mx"));
            email.CC.Add(new MailAddress("logistica@mrlucky.com.mx"));
            email.CC.Add(new MailAddress("jgalvan@mrlucky.com.mx"));
            email.CC.Add(new MailAddress("ricardo.cortes@mrlucky.com.mx"));
            email.CC.Add(new MailAddress("embarques@mrlucky.com.mx"));

            email.To.Add(new MailAddress("gcamacho@mrlucky.com.mx"));

            email.From = new MailAddress("embarques@mrlucky.com.mx"); //
            email.Subject = mAsunto; //"Mensaje de Prueba";
            email.Body = mBody;  //"Información de la factura";
            email.IsBodyHtml = true;
            email.Priority = MailPriority.Normal;



            SmtpClient smtp = new SmtpClient();
            smtp.Host = "mail1.mrlucky.com.mx";
            smtp.Port = 587;
            smtp.EnableSsl = true;
            smtp.UseDefaultCredentials = false;
            smtp.Credentials = new NetworkCredential("embarques", "3\\<\\S>FCp8J3,x6@");

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



        private void LoadConnection()
        {
            string folder = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);
            string dbPath = System.IO.Path.Combine(folder, "Split_Trailer_Cancelacion.db3");

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


        public void productosporsurtir()
        {
            string mped = pedidoprincipal.Trim();
            db.Query<ConPedidos>("UPDATE [ConPedidos] SET surtido = 0");

            string Tipoped = "NAL";

            if (pedidoprincipal.Length > 0)
            {
                if (Convert.ToInt32(pedidoprincipal) < 300000)
                {
                    Tipoped = "EXP";

                }
            }



            thisConnection.Open();
            string Cadena = "Select a.pdn_folio,a.prod_clave,b.prod_nombre,a.pdn_num_unidades From tb_det_pedidos A, tb_Cat_producto B " +
                "where a.pdn_folio = '" + pedidoprincipal.Trim() + "' and a.prod_clave = b.prod_clave and A.pdn_Tipo = '" + Tipoped + "'";
            SqlDataAdapter da = new SqlDataAdapter(Cadena, thisConnection);
            DataSet ds = new DataSet();
            da.Fill(ds, "Ped");
            DataTable Ped = ds.Tables["Ped"];
            thisConnection.Close();

            string hay = "N";


            if (Ped.Rows.Count == 0)
            {
                // Diálogo: Pedido Inexistente
                DialogHelper.ShowErrorDialog(this,
                    message: "El pedido " + pedidoprincipal.Trim() + " no existe o no se ha dado de alta.",
                    positiveText: "Entendido");
                return;
            }

            foreach (DataRow row in Ped.Rows)
            {

                string mnom = row["prod_nombre"].ToString().Trim();
                mnom = mnom.Replace("'", " ");

                Pedidos Pedidoscapturados = new Pedidos { folio = row["pdn_folio"].ToString().Trim(), prod_clave = row["prod_clave"].ToString().Trim(), nombre = mnom, pedido = Convert.ToInt32(row["pdn_num_unidades"]), surtido = 0 };
                //Registra en la base de datos SQLite
                db.Insert(Pedidoscapturados);


                var encontrado = 0;
                var queryx = db.Table<ConPedidos>();
                foreach (var captu in queryx)
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




            string cadena = "Select * From tb_det_pedidos A, tb_Cat_producto B where a.pdn_folio = '" + pedidoprincipal.Trim() + "' and a.prod_clave = b.prod_clave";
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


            db.Query<Pedidos>("Delete FROM ConPedidos Where pedido = surtido");
            var queryCancelar = db.Query<Pedidos>("Delete FROM Pedidos Where pedido = surtido");


            Spinner spinner2 = FindViewById<Spinner>(Resource.Id.spinnerprodfal);
            System.Collections.ArrayList listaFrutas2 = new System.Collections.ArrayList();

            int i = 0;


            var query = db.Table<Pedidos>();
            foreach (var captu in query)
            {
                i++;
            }


            strFrutas = new System.String[i + 1];
            int x = 0;


            strFrutas[x] = "Seleccione un Producto";

            query = db.Table<Pedidos>();
            foreach (var captu in query)
            {
                x++;
                strFrutas[x] = captu.prod_clave + "-" + captu.nombre;
            }



            Collections.AddAll(listaFrutas2, strFrutas);
            comboAdapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleSpinnerItem, strFrutas);
            spinner2.Adapter = comboAdapter;
            spinner2.ItemSelected += new EventHandler<AdapterView.ItemSelectedEventArgs>(spinner_ItemSelected2);
        }


        private void spinner_ItemSelected2(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            Spinner spinner = (Spinner)sender;
            valorproducto = spinner.GetItemAtPosition(e.Position).ToString();
            string[] partes = valorproducto.Split('-');
            if (valorproducto != "Seleccione un Producto")
            {
                claveprodcuto = partes[0];
                Guardar.Enabled = true;
            }

            faltante = 0;
            var query = db.Query<Pedidos>("Select * FROM [Pedidos] Where prod_clave = '" + claveprodcuto + "'");
            foreach (var captu in query)
            {
                faltante = Convert.ToInt32(captu.pedido) - Convert.ToInt32(captu.surtido);
            }

            porarmartexto.Text = "Faltante Por Armar de un Total de: " + faltante;
            faltanteporarmar.Text = "0";
        }
    }
}