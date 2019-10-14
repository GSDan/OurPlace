using Android.Content;
using Android.Support.V7.Widget;
using Android.Views;
using FFImageLoading;
using FFImageLoading.Transformations;
using OurPlace.Common;
using OurPlace.Common.Models;
using System;
using System.Collections.Generic;

namespace OurPlace.Android.Adapters
{
    public class ActivityAdapter : RecyclerView.Adapter
    {
        public event EventHandler<int> ItemClick;
        public List<LearningActivity> Data { get; private set; }
        private readonly Context context;

        public ActivityAdapter(Context context, List<LearningActivity> data)
        {
            this.context = context;
            Data = data;
        }

        public override long GetItemId(int position)
        {
            return Data[position].Id;
        }

        public override int ItemCount => Data?.Count ?? 0;

        private void OnClick(int position)
        {
            ItemClick?.Invoke(this, position);
        }

        public override int GetItemViewType(int position)
        {
            if (position == 0)
            {
                return 0;
            }

            return position >= Data.Count ? 2 : 1;
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            View itemView = LayoutInflater.From(context).Inflate(Resource.Layout.TaskCard_Btn, parent, false);
            TaskViewHolderBtn vh = new TaskViewHolderBtn(itemView, null, OnClick)
            {
                Button = { Text = context.Resources.GetString(Resource.String.ChooseBtn) }
            };
            return vh;
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            if (!(holder is TaskViewHolderBtn vh))
            {
                return;
            }

            vh.Title.Text = Data[position].Name;
            vh.Description.Text = Data[position].Description;

            if (string.IsNullOrWhiteSpace(Data[position].ImageUrl))
            {
                ImageService.Instance.LoadCompiledResource("logoRect")
                    .DownSampleInDip(width: 70)
                    .Transform(new CircleTransformation())
                    .IntoAsync(vh.TaskTypeIcon);
            }
            else
            {
                ImageService.Instance.LoadUrl(ServerUtils.GetUploadUrl(Data[position].ImageUrl))
                    .DownSampleInDip(width: 70)
                    .Transform(new CircleTransformation())
                    .IntoAsync(vh.TaskTypeIcon);
            }
        }
    }
}