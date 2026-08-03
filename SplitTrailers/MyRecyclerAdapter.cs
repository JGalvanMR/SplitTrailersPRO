using Android.App;
using Android.Content.Res;
using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using SplitTrailers.Modal;
using System;
using System.Collections.Generic;

namespace SplitTrailers
{
    public class MyRecyclerAdapter : RecyclerView.Adapter
    {
        private Activity _currentContext;
        private List<FlimStarInfo> _lstFlimStarInfo;

        public event EventHandler<int> ItemClick;

        public MyRecyclerAdapter(Activity currentContext, List<FlimStarInfo> lstFlimInfo)
        {
            _currentContext = currentContext;
            _lstFlimStarInfo = lstFlimInfo ?? new List<FlimStarInfo>();
            System.Diagnostics.Debug.WriteLine($"🔹 Adaptador creado con {_lstFlimStarInfo.Count} items");
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🔹 OnCreateViewHolder llamado");

                // Inflar el layout
                View itemView = LayoutInflater.From(parent.Context)
                    .Inflate(Resource.Layout.cust_recycler_item, parent, false);

                if (itemView == null)
                {
                    System.Diagnostics.Debug.WriteLine("❌ ERROR: itemView es null después de inflar");
                    return null;
                }

                System.Diagnostics.Debug.WriteLine("✅ Layout inflado correctamente");
                return new MyViewHolder(itemView, OnClick);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ ERROR en OnCreateViewHolder: {ex.Message}");
                return null;
            }
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"🔹 OnBindViewHolder posición {position}");

                if (holder is MyViewHolder myViewHolder &&
                    _lstFlimStarInfo != null &&
                    position < _lstFlimStarInfo.Count)
                {
                    var item = _lstFlimStarInfo[position];
                    myViewHolder.BindData(item);
                    System.Diagnostics.Debug.WriteLine($"✅ Item bindeado: {item.Name}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"❌ ERROR: Holder inválido o posición fuera de rango");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ ERROR en OnBindViewHolder: {ex.Message}");
            }
        }

        public override int ItemCount
        {
            get
            {
                var count = _lstFlimStarInfo?.Count ?? 0;
                System.Diagnostics.Debug.WriteLine($"🔹 ItemCount llamado: {count}");
                return count;
            }
        }

        public void UpdateData(List<FlimStarInfo> newData)
        {
            _lstFlimStarInfo = newData ?? new List<FlimStarInfo>();
            NotifyDataSetChanged();
            System.Diagnostics.Debug.WriteLine($"🔹 Datos actualizados: {_lstFlimStarInfo.Count} items");
        }

        public FlimStarInfo GetItem(int position)
        {
            return _lstFlimStarInfo != null && position < _lstFlimStarInfo.Count ?
                   _lstFlimStarInfo[position] : null;
        }

        private void OnClick(int position)
        {
            ItemClick?.Invoke(this, position);
        }

        // ViewHolder simplificado
        public class MyViewHolder : RecyclerView.ViewHolder
        {
            private readonly ImageView _imgPers;
            private readonly TextView _txtName;
            private readonly TextView _txtAge;

            public MyViewHolder(View itemView, Action<int> clickListener) : base(itemView)
            {
                try
                {
                    _imgPers = itemView.FindViewById<ImageView>(Resource.Id.imgPers);
                    _txtName = itemView.FindViewById<TextView>(Resource.Id.txtName);
                    _txtAge = itemView.FindViewById<TextView>(Resource.Id.txtAge);

                    itemView.Click += (sender, e) =>
                    {
                        if (AdapterPosition != RecyclerView.NoPosition)
                            clickListener?.Invoke(AdapterPosition);
                    };
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error en MyViewHolder: {ex.Message}");
                }
            }

            public void BindData(FlimStarInfo item)
            {
                try
                {
                    _txtName.Text = item.Name ?? "Sin nombre";
                    _txtAge.Text = item.Age?.ToString() ?? "Sin información";

                    if (item.ImageID != 0)
                    {
                        _imgPers.SetImageResource(item.ImageID);
                    }
                    else
                    {
                        _imgPers.SetImageResource(Android.Resource.Drawable.IcMenuGallery);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error en BindData: {ex.Message}");
                }
            }
        }
    }
}