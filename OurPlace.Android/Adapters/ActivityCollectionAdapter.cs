using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using OurPlace.Common.Models;

namespace OurPlace.Android.Adapters
{
    class ActivityCollectionAdapter : BaseAdapter
    {
        private readonly Context context;
        private List<LearningActivity> data;

        public Action SaveProgress;

        public ActivityCollectionAdapter(Context context, ActivityCollection thisCollection, Action save)
        {
            this.context = context;

            if (thisCollection.Activities != null)
            {
                data = (List<LearningActivity>)thisCollection.Activities;
            }
            else
            {
                data = new List<LearningActivity>();
            }

            SaveProgress = save;
        }


        public override Java.Lang.Object GetItem(int position)
        {
            return position;
        }

        public override long GetItemId(int position)
        {
            return position;
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            var view = convertView;
            ActivityCollectionAdapterViewHolder holder = null;

            if (view != null)
                holder = view.Tag as ActivityCollectionAdapterViewHolder;

            if (holder == null)
            {
                view = LayoutInflater.From(context)
                            .Inflate(Resource.Layout.ActivityCollectionCard, parent, false);

                holder = new ActivityCollectionAdapterViewHolder(view, null);
                //holder.Title = view.FindViewById<TextView>(Resource.Id.text);
                view.Tag = holder;
            }


            //fill in your items
            //holder.Title.Text = "new text here";

            return view;
        }

        public override int Count
        {
            get
            {
                if (data == null)
                {
                    return 2;
                }

                return data.Count + 2;
            }
        }

    }

    class ActivityCollectionAdapterViewHolder : RecyclerView.ViewHolder
    {
        public TextView Title { get; protected set; }
        public ImageView ActivityIcon { get; protected set; }
        public TextView Description { get; protected set; }
        public TextView LocationText { get; protected set; }
        public Button LocationButton { get; protected set; }
        public View TtsButton { get; protected set; }
        public Button RemoveButton { get; protected set; }

        public ActivityCollectionAdapterViewHolder(View itemView, Action<int> ttsAction): base(itemView)
        {
            //Title = itemView.FindViewById<TextView>(Resource.Id.activityTitle);
            //Description = itemView.FindViewById<TextView>(Resource.Id.activityDesc);
            //LocationText = itemView.FindViewById<TextView>(Resource.Id.locationDesc);
            //ActivityIcon = itemView.FindViewById<ImageView>(Resource.Id.activityIcon);
            //LocationButton = itemView.FindViewById<Button>(Resource.Id.viewLocButton);
            //RemoveButton = itemView.FindViewById<Button>(Resource.Id.removeBtn);
            TtsButton = itemView.FindViewById<View>(Resource.Id.ttsBtn);

            if (TtsButton == null)
            {
                return;
            }

            if (ttsAction == null)
            {
                TtsButton.Visibility = ViewStates.Invisible;
            }
            else
            {
                TtsButton.Visibility = ViewStates.Visible;
                TtsButton.Click += (sender, e) => ttsAction(AdapterPosition);
            }
        }
    }
}