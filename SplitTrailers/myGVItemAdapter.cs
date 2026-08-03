using Android.App;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;

namespace SplitTrailers
{
    public class myGVItemAdapter : BaseAdapter<SplitTrailers.Modal.FlimStarInfo>
    {
        Activity _CurrentContext;
        List<SplitTrailers.Modal.FlimStarInfo> _lstFlimStarInfo;

        public myGVItemAdapter(Activity currentContext, List<SplitTrailers.Modal.FlimStarInfo> lstFlimInfo)
        {
            _CurrentContext = currentContext;
            _lstFlimStarInfo = lstFlimInfo;
        }

        public override long GetItemId(int position)
        {
            return position;
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            try
            {
                var item = _lstFlimStarInfo[position];
                if (convertView == null)
                    convertView = _CurrentContext.LayoutInflater.Inflate(Resource.Layout.custGridViewItem, null);

                convertView.FindViewById<TextView>(Resource.Id.txtName).Text = item.Name;
                convertView.FindViewById<TextView>(Resource.Id.txtAge).Text = item.Age.ToString();
                convertView.FindViewById<ImageView>(Resource.Id.imgPers).SetImageResource(item.ImageID);


            }
            catch (Exception e)
            {

                var error = e;
            }


            return convertView;
        }

        public override int Count
        {
            get { return _lstFlimStarInfo == null ? -1 : _lstFlimStarInfo.Count; }
        }

        public override SplitTrailers.Modal.FlimStarInfo this[int position] => _lstFlimStarInfo == null ? null : _lstFlimStarInfo[position];

    }
}