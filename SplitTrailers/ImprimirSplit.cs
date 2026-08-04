using Android.App;
using Android.Bluetooth;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Text;
using Android.Text.Style;
using Android.Views.InputMethods;
using Android.Widget;
using AndroidX.AppCompat.App;

//Librerias de la impresion Bluetooth
using Com.Woosim.Printer;
using Google.Android.Material.AppBar;
using Google.Android.Material.Dialog;
using Java.IO;
using Java.Util;
using Plugin.DeviceInfo;
using SplitTrailers.Modal;
using SQLite;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using SplitTrailers.Helpers;


namespace SplitTrailers
{
    [Activity(Label = "Imprimir Split")]
    public partial class ImprimirSplit : AppCompatActivity
    {
        public static string crcancelar, split, pedidocancelar, ordven, responsplit, cveresponsplit, parcial, currentVersionName, imei;
        public static SQLiteConnection db;
        SqlConnection thisConnection = new SqlConnection(MainActivity.cadenaConexion);
        SqlDataAdapter da;
        DataSet ds = new DataSet();
        SqlCommand cmnd = new SqlCommand();
        SqlCommand cmnd1 = new SqlCommand();

        EditText pedidoImp;
        TextView totalsplitimp;
        TextView userprint;

        TextView textoSplit;

        //Variables de la Impresora
        string deviceName = "WOOSIM";
        //string deviceName = "WO0SIM5";
        private BluetoothAdapter mBluetoothAdapter = null;
        private BluetoothDevice mmDevice = null;
        private BluetoothSocket mmSocket = null;
        private Stream mmOutputStream;
        private Stream mmInputStream;

        int splitnumero = 0;
        int cajasnumero = 0;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            crcancelar = Intent.GetStringExtra("respimprimir");
            responsplit = Intent.GetStringExtra("responsable");
            imei = Intent.GetStringExtra("imei");
            currentVersionName = Intent.GetStringExtra("currentVersionName");


            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.ImprimirSplit);

            pedidoImp = FindViewById<EditText>(Resource.Id.pedidoprint);
            userprint = FindViewById<TextView>(Resource.Id.userprint);
            totalsplitimp = FindViewById<TextView>(Resource.Id.totalsplitimp);

            textoSplit = FindViewById<TextView>(Resource.Id.textosplit);
            userprint.Text = responsplit;

            pedidoImp.EditorAction += (sender, e) =>
            {
                if (e.ActionId == ImeAction.Done || e.ActionId == ImeAction.Next)
                {
                    thisConnection.Open();
                    string Cadena = "Select emb_folio from tb_mstr_embarque Where emb_folio = '" + pedidoImp.Text.Trim() + "' AND sts = 'T' AND hora_fin != '--:--'";
                    SqlCommand embcerr = new SqlCommand(Cadena, thisConnection);
                    string embcer = Convert.ToString(embcerr.ExecuteScalar());
                    thisConnection.Close();
                    if (embcer.Trim().Length > 0)
                    {
                        // Diálogo de embarque cerrado(advertencia)
                        DialogHelper.ShowWarningDialog(this,
                            message: $"El embarque {pedidoImp.Text.Trim()} está cerrado y no se puede imprimir.",
                            positiveText: "Ok");
                        pedidoImp.Text = "";
                        pedidoImp.RequestFocus();
                        #region MATERIAL DIALOG - Embarque Cerrado
                        /*// Construcción del título con color naranja y negritas
                        var titleSpannable = new SpannableStringBuilder("Embarque Cerrado");
                        titleSpannable.SetSpan(new ForegroundColorSpan(Color.ParseColor("#FA993E")), 0, titleSpannable.Length(), SpanTypes.ExclusiveExclusive);
                        titleSpannable.SetSpan(new StyleSpan(TypefaceStyle.Bold), 0, titleSpannable.Length(), SpanTypes.ExclusiveExclusive);

                        // Construcción del mensaje con color dorado y énfasis en el número de embarque
                        var mensajeSpannable = new SpannableStringBuilder();
                        mensajeSpannable.Append("El embarque ");
                        int startPedido = mensajeSpannable.Length();
                        mensajeSpannable.Append(pedidoImp.Text.Trim());
                        mensajeSpannable.SetSpan(new StyleSpan(TypefaceStyle.Bold), startPedido, mensajeSpannable.Length(), SpanTypes.ExclusiveExclusive);
                        mensajeSpannable.Append(" está cerrado y no se puede imprimir.");
                        mensajeSpannable.SetSpan(new ForegroundColorSpan(Color.ParseColor("#5F6368")), 0, mensajeSpannable.Length(), SpanTypes.ExclusiveExclusive);

                        // Crear el diálogo Material Design 3
                        var builder = new MaterialAlertDialogBuilder(this, Resource.Style.ThemeOverlay_Material3_MaterialAlertDialog);
                        builder.SetTitle(titleSpannable);
                        builder.SetIcon(Resource.Drawable.warning);
                        builder.SetMessage(mensajeSpannable);
                        builder.SetCancelable(false);

                        // Botón principal
                        builder.SetPositiveButton("Ok", (s, e) => { });

                        // Crear y mostrar el diálogo
                        var dialog = builder.Create();
                        dialog.Show();

                        // Personalizar el botón luego de mostrar el diálogo
                        dialog.Window.DecorView.Post(() =>
                        {
                            var positiveButton = dialog.GetButton((int)DialogButtonType.Positive);
                            positiveButton?.SetTextColor(Color.ParseColor("#E65100")); // Naranja Material (coherente con advertencias)
                            positiveButton?.SetAllCaps(false);
                        });*/
                        #endregion
                    }


                    List<FlimStarInfo> lstFlimStar = ConsSplit();
                    var gvObject = FindViewById<GridView>(Resource.Id.gvCtrimprimir);
                    gvObject.Adapter = new myGVItemAdapter(this, lstFlimStar);
                    gvObject.ItemClick += new EventHandler<AdapterView.ItemClickEventArgs>(OnGridView_ItemClicked);

                    pedidoImp.SetSelection(0, pedidoImp.Text.Length);
                    pedidoImp.RequestFocus();
                }

            };

            // Referencia al MaterialToolbar
            MaterialToolbar toolbar = FindViewById<MaterialToolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);
            SupportActionBar.Title = "INGRESAR PEDIDO A IMPRIMIR";
            SupportActionBar.SetDisplayHomeAsUpEnabled(false); // si quieres back button
        }




        private void OnGridView_ItemClicked(object sender, AdapterView.ItemClickEventArgs e)
        {

            split = e.View.FindViewById<TextView>(Resource.Id.txtName).Text;
            split = split.Replace("Split Numero: ", "");

            string caj = e.View.FindViewById<TextView>(Resource.Id.txtAge).Text;
            caj = caj.Replace("Cajas: ", "");

            splitnumero = Convert.ToInt32(split);
            cajasnumero = Convert.ToInt32(caj);

            pedidocancelar = pedidoImp.Text.Trim();

            // Diálogo de confirmación para imprimir
            DialogHelper.ShowConfirmDialog(this,
                title: "Imprimir Split",
                message: $"¿Desea imprimir el Split número {split}?",
                positiveText: "Sí",
                negativeText: "No",
                positiveAction: SaveAction,
                negativeAction: CancelaAction);
            #region MATERIAL DIALOG - Imprimir Split
            /*// Construcción del título con color rojo Material y negritas
            var titleSpannable = new SpannableStringBuilder("Imprimir Split");
            titleSpannable.SetSpan(new ForegroundColorSpan(Color.ParseColor("#DC3545")), 0, titleSpannable.Length(), SpanTypes.ExclusiveExclusive);
            titleSpannable.SetSpan(new StyleSpan(TypefaceStyle.Bold), 0, titleSpannable.Length(), SpanTypes.ExclusiveExclusive);

            // Construcción del mensaje con énfasis en el número del Split
            var mensajeSpannable = new SpannableStringBuilder();
            mensajeSpannable.Append("¿Desea imprimir el Split número ");
            int startSplit = mensajeSpannable.Length();
            mensajeSpannable.Append(split.ToString());
            mensajeSpannable.SetSpan(new StyleSpan(TypefaceStyle.Bold), startSplit, mensajeSpannable.Length(), SpanTypes.ExclusiveExclusive);
            mensajeSpannable.Append("?");

            // Crear el diálogo Material Design 3
            var builder = new MaterialAlertDialogBuilder(this, Resource.Style.ThemeOverlay_Material3_MaterialAlertDialog);
            builder.SetTitle(titleSpannable);
            builder.SetIcon(Resource.Drawable.question);
            builder.SetMessage(mensajeSpannable);
            builder.SetCancelable(false);

            // Botones con acciones personalizadas
            builder.SetPositiveButton("Sí", SaveAction);
            builder.SetNegativeButton("No", CancelaAction);

            // Crear y mostrar el diálogo
            var dialog = builder.Create();
            dialog.Show();

            // Personalización de botones
            dialog.Window.DecorView.Post(() =>
            {
                var positiveButton = dialog.GetButton((int)DialogButtonType.Positive);
                var negativeButton = dialog.GetButton((int)DialogButtonType.Negative);

                positiveButton?.SetTextColor(Color.ParseColor("#DC3545")); // Rojo Material para confirmación
                negativeButton?.SetTextColor(Color.ParseColor("#5F6368")); // Gris neutro para cancelar

                positiveButton?.SetAllCaps(false);
                negativeButton?.SetAllCaps(false);
            });*/
            #endregion


        }

        private void SaveAction(object sender, DialogClickEventArgs e)
        {
            try
            {
                FindPrinter();
                sendData();
                sendData();
            }
            catch (SystemException ex)
            {

                Toast.MakeText(this, "Error al Imprimir - " + ex.ToString() + "", ToastLength.Short).Show();
            }


            //Android.Telephony.TelephonyManager mTelephonyMgr;
            //mTelephonyMgr = (Android.Telephony.TelephonyManager)GetSystemService(TelephonyService);
            //IMEI number  

            thisConnection.Open();
            string cadenas = "INSERT INTO TB_REGISTRO_MOVIMIENTOS(FECHA,NOM_COMPU,NOM_USU,TIPO_MOV,OP_CLAVE,FOLIO,DETALLE,SISTEMA,MOV_FOLIO) " +
                            "VALUES('" + System.DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + "','CEL " + imei + "','" + responsplit.Trim() + "','IM','7.10','" +
                            pedidoImp.Text.ToString().Trim() + "','Impresion Split " + split.ToString() + "','SPLIT','" + pedidoImp.Text.ToString().Trim() + "')";
            //MessageBox.Show(cadena);
            SqlCommand cmds = new SqlCommand(cadenas, thisConnection);
            cmds.ExecuteNonQuery();
            thisConnection.Close();

            // Diálogo de éxito
            DialogHelper.ShowSuccessDialog(this,
                message: "¡Split impreso correctamente!",
                positiveText: "Aceptar",
                positiveAction: (s, ev) =>
                {
                    // Lógica de limpieza y regreso
                    Intent databack = new Intent();
                    databack.PutExtra("pedido_cancelar", pedidoImp.Text.Trim());
                    pedidoImp.Text = "";
                    totalsplitimp.Text = "000|000";
                    List<FlimStarInfo> lstFlimStar = ConsSplit();
                    lstFlimStar.Clear();
                    var gvObject = FindViewById<GridView>(Resource.Id.gvCtrimprimir);
                    gvObject.Adapter = new myGVItemAdapter(this, null);
                    gvObject.Adapter = null;
                    gvObject.Adapter = new myGVItemAdapter(this, lstFlimStar);
                    SetResult(Result.Ok, databack);
                    Finish();
                });
            #region MATERIAL DIALOG - Split Impreso
            /*// Construcción del título (color rojo + negritas)
            var titleSpannable = new SpannableStringBuilder("Split Impreso");
            titleSpannable.SetSpan(new ForegroundColorSpan(Color.ParseColor("#DC3545")), 0, titleSpannable.Length(), SpanTypes.ExclusiveExclusive);
            titleSpannable.SetSpan(new StyleSpan(TypefaceStyle.Bold), 0, titleSpannable.Length(), SpanTypes.ExclusiveExclusive);

            // Construcción del mensaje con color suave y énfasis en éxito
            var mensajeSpannable = new SpannableStringBuilder("¡Split impreso correctamente!");
            mensajeSpannable.SetSpan(new ForegroundColorSpan(Color.ParseColor("#5F6368")), 0, mensajeSpannable.Length(), SpanTypes.ExclusiveExclusive);
            mensajeSpannable.SetSpan(new StyleSpan(TypefaceStyle.Bold), 0, mensajeSpannable.Length(), SpanTypes.ExclusiveExclusive);

            // Crear el diálogo con el tema Material 3
            var builder = new MaterialAlertDialogBuilder(this, Resource.Style.ThemeOverlay_Material3_MaterialAlertDialog);
            builder.SetTitle(titleSpannable);
            builder.SetIcon(Resource.Drawable.exito);
            builder.SetMessage(mensajeSpannable);
            builder.SetCancelable(false);

            // Botón de confirmación
            builder.SetPositiveButton("Aceptar", (s, e) =>
            {
                // Lógica posterior al cierre del diálogo
                Intent databack = new Intent();
                databack.PutExtra("pedido_cancelar", pedidoImp.Text.Trim());

                pedidoImp.Text = "";
                totalsplitimp.Text = "000|000";
                List<FlimStarInfo> lstFlimStar = ConsSplit();
                lstFlimStar.Clear();

                var gvObject = FindViewById<GridView>(Resource.Id.gvCtrimprimir);
                gvObject.Adapter = new myGVItemAdapter(this, null);
                gvObject.Adapter = null;
                gvObject.Adapter = new myGVItemAdapter(this, lstFlimStar);

                // Intent de regreso
                SetResult(Result.Ok, databack);
                Finish();
            });

            // Crear y mostrar el diálogo
            var dialog = builder.Create();
            dialog.Show();

            // Personalización del botón
            dialog.Window.DecorView.Post(() =>
            {
                var positiveButton = dialog.GetButton((int)DialogButtonType.Positive);
                positiveButton?.SetTextColor(Color.ParseColor("#00695C")); // Verde Material (éxito)
                positiveButton?.SetAllCaps(false);
            });*/
            #endregion


        }

        //string imei = "";
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
            //thisConnection.Open();
            ordven = pedidoImp.Text.ToString();

            if (ordven.Length > 0)
            {
                if (Convert.ToInt32(ordven) < 300000)
                {
                    ordven = "0" + ordven.ToString().Trim();

                }
            }



            string cadena = "   SELECT sum(cajas) AS cajas, tarima, emb_folio, estatus, HORAF FROM Tb_Det_Split WHERE NOM_CAPSPLIT = '" + responsplit.Trim() + "' and emb_folio = '" + ordven.Trim() + "' GROUP BY emb_folio, tarima, estatus, HORAF";
            SqlDataAdapter da = new SqlDataAdapter(cadena, thisConnection);
            DataSet ds = new DataSet();
            da.Fill(ds, "ConsPed");
            DataTable ConsPed = ds.Tables["ConsPed"];

            foreach (DataRow Row in ConsPed.Rows)
            {
                Existe = "S";

                if (Row["estatus"].ToString().Trim() != "")
                {
                    listItem.Add(new FlimStarInfo()
                    {
                        Name = "Split Numero: " + Row["tarima"].ToString().Trim(),
                        Age = "Cajas: " + Row["cajas"].ToString(),
                        ImageID = Resource.Drawable.impresoraverde
                    });

                }
                else
                {
                    listItem.Add(new FlimStarInfo()
                    {
                        Name = "Split Numero: " + Row["tarima"].ToString().Trim(),
                        Age = "Cajas: " + Row["cajas"].ToString(),
                        ImageID = Resource.Drawable.impresorarojo
                    });
                }



                cantidadsplit++;
            }

            totalsplitimp.Text = cantidadsplit.ToString();

            if (Existe != "S")
            {
                // Diálogo de error (pedido sin split)
                DialogHelper.ShowErrorDialog(this,
                    message: $"El pedido: {pedidoImp.Text.Trim()} no cuenta con split disponible.",
                    positiveText: "Ok");
                #region MATERIAL DIALOG - Pedido sin Split
                /*// Construcción del título (color rojo + negritas)
                var titleSpannable = new SpannableStringBuilder("Pedido sin Split");
                titleSpannable.SetSpan(new ForegroundColorSpan(Color.ParseColor("#DC3545")), 0, titleSpannable.Length(), SpanTypes.ExclusiveExclusive);
                titleSpannable.SetSpan(new StyleSpan(TypefaceStyle.Bold), 0, titleSpannable.Length(), SpanTypes.ExclusiveExclusive);

                // Construcción del mensaje con color neutro y énfasis en el pedido
                var mensajeSpannable = new SpannableStringBuilder();
                mensajeSpannable.Append("El pedido ");
                int startPedido = mensajeSpannable.Length();
                mensajeSpannable.Append(ordven.Trim());
                mensajeSpannable.SetSpan(new StyleSpan(TypefaceStyle.Bold), startPedido, mensajeSpannable.Length(), SpanTypes.ExclusiveExclusive);
                mensajeSpannable.Append(" no cuenta con split disponible.");
                mensajeSpannable.SetSpan(new ForegroundColorSpan(Color.ParseColor("#5F6368")), 0, mensajeSpannable.Length(), SpanTypes.ExclusiveExclusive);

                // Crear el diálogo con el tema Material 3
                var builder = new MaterialAlertDialogBuilder(this, Resource.Style.ThemeOverlay_Material3_MaterialAlertDialog);
                builder.SetTitle(titleSpannable);
                builder.SetIcon(Resource.Drawable.no);
                builder.SetMessage(mensajeSpannable);
                builder.SetCancelable(false);

                // Botón principal
                builder.SetPositiveButton("Entendido", (s, e) => { });

                // Crear y mostrar el diálogo
                var dialog = builder.Create();
                dialog.Show();

                // Personalización del botón
                dialog.Window.DecorView.Post(() =>
                {
                    var positiveButton = dialog.GetButton((int)DialogButtonType.Positive);
                    positiveButton?.SetTextColor(Color.ParseColor("#B71C1C")); // Rojo oscuro Material
                    positiveButton?.SetAllCaps(false);
                });*/
                #endregion

            }

            //LbxCons.Font = new Font(LbxCons.Font.Name, 7);   ;
            thisConnection.Close();

            return listItem;
        }


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
            string embarque = pedidoImp.Text.Replace("Pedido Actual: ", "").Trim();
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
                var msg = "SPLIT*" + pedidoImp.Text.Replace("Pedido Actual: ", "").Trim() + "*" + splitnumero + ", " + cajasnumero + "";
                byte[] barcode = System.Text.Encoding.GetEncoding(1252).GetBytes(msg);
                byte[] QRCode = WoosimBarcode.Create2DBarcodeQRCode(4, (sbyte)0x4d, 8, barcode);
                byte[] cmd_print = WoosimCmd.PrintData();
                string title1 = destino.ToUpper() + "\r\n " + pedidoImp.Text.Replace("Pedido Actual: ", "").Trim() + " \r\n SPLIT: " + splitnumero + " \r\n CAJAS: " + cajasnumero + " \r\n";
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



    }
}