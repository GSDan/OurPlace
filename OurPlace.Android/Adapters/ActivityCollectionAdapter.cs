using Android.Content;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using OurPlace.Common.Models;
using OurPlace.Common;
using System;
using System.Collections.Generic;
using FFImageLoading;
using FFImageLoading.Transformations;
using System.Linq;
using System.Globalization;

namespace OurPlace.Android.Adapters
{
    class ActivityCollectionAdapter : RecyclerView.Adapter, ItemTouchHelperAdapter
    {
        public event EventHandler<int> EditCollectionClick;
        public event EventHandler<int> FinishClick;
        public event EventHandler<int> OpenLocationClick;
        public event EventHandler<int> DeleteItemClick;
        public event EventHandler<int> OpenItemClick;

        public ActivityCollection Collection { get; private set; }
        private readonly Action saveProgress;
        private readonly Context context;
        private readonly bool editingMode;

        public ActivityCollectionAdapter(Context context, ActivityCollection thisCollection, Action save, bool editing)
        {
            this.context = context;
            Collection = thisCollection;
            saveProgress = save;
            editingMode = editing;
        }

        public override long GetItemId(int position)
        {
            if (position == 0 && editingMode)
            {
                return -2;
            }

            if (position > Collection.Activities?.Count)
            {
                return -1;
            }

            return Collection.Activities[position].Id;
        }

        public override int GetItemViewType(int position)
        {
            if (position == 0 && editingMode)
            {
                return 0;
            }

            return position > Collection.Activities?.Count ? 2 : 1;
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            if (position == 0 && editingMode)
            {
                if (!(holder is ActivityViewHolder avh))
                {
                    return;
                }

                avh.Title.Text = Collection.Name;
                avh.Description.Text = Collection.Description;

                if (string.IsNullOrWhiteSpace(Collection.ImageUrl)) return;

                if (Collection.ImageUrl.StartsWith("upload"))
                {
                    ImageService.Instance.LoadUrl(ServerUtils.GetUploadUrl(Collection.ImageUrl))
                        .Transform(new CircleTransformation())
                        .Into(avh.TaskTypeIcon);
                }
                else
                {
                    ImageService.Instance.LoadFile(Collection.ImageUrl)
                        .Transform(new CircleTransformation())
                        .Into(avh.TaskTypeIcon);
                }

                return;
            }

            if (position > Collection.Activities?.Count)
            {
                // Allow the collection to be submitted if there's at least two activities
                ButtonViewHolder bvh = holder as ButtonViewHolder;
                bvh.Button.Enabled = Collection.Activities.Count > 1;
                return;
            }

            if(editingMode)
            {
                position--;
            }

            if (!(holder is ActivityCollectionAdapterViewHolder vh))
            {
                return;
            }

            LearningActivity thisActivity = Collection.Activities[position];

            vh.Title.Text = thisActivity.Name;
            vh.Description.Text = thisActivity.Description;

            if(thisActivity.Places?.Any() ?? false)
            {
                vh.LocationButton.Visibility = ViewStates.Visible;
                vh.LocationText.Text = string.Format(CultureInfo.CurrentCulture,
                    context.GetString(Resource.String.createCollectionActivityLocation),
                    thisActivity.Places.FirstOrDefault()?.Name);
            }
            else
            {
                vh.LocationButton.Visibility = ViewStates.Gone;
                vh.LocationText.SetText(Resource.String.createCollectionActivityNoLocation);
            }

            if (string.IsNullOrWhiteSpace(thisActivity.ImageUrl))
            {
                ImageService.Instance.LoadCompiledResource("logoRect")
                    .DownSampleInDip(width: 70)
                    .Transform(new CircleTransformation())
                    .IntoAsync(vh.ActivityIcon);
            }
            else
            {
                ImageService.Instance.LoadUrl(ServerUtils.GetUploadUrl(thisActivity.ImageUrl))
                    .DownSampleInDip(width: 70)
                    .Transform(new CircleTransformation())
                    .IntoAsync(vh.ActivityIcon);
            }

            vh.HandleView.Visibility = (editingMode) ? ViewStates.Visible : ViewStates.Gone;

            if(!editingMode)
            {
                vh.RemoveButton.SetText(Resource.String.collectionStartActivity);
            }
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            switch (viewType)
            {
                case 0:
                    {
                        // The Collection details at the top of the list
                        View activityView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.TaskCard_Activity, parent, false);
                        ActivityViewHolder avh = new ActivityViewHolder(activityView, null, OnEditCollectionClick);
                        return avh;
                    }
                case 2:
                    {
                        // The finish button at the bottom of the list
                        View finishView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.TaskCard_Finish, parent, false);
                        ButtonViewHolder bvh = new ButtonViewHolder(finishView, OnFinishClick);
                        return bvh;
                    }
            }

            View itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.ActivityCollectionCard, parent, false);

            if(editingMode) 
            {
                return new ActivityCollectionAdapterViewHolder(itemView, null, OnDeleteItemClick, OnOpenLocationClick);
            }

            return new ActivityCollectionAdapterViewHolder(itemView, null, OnOpenItemClick, OnOpenLocationClick);
        }

        public override int ItemCount
        {
            get
            {
                if (Collection.Activities == null)
                {
                    return (editingMode)? 2 : 0;
                }

                return Collection.Activities.Count + ((editingMode) ? 2 : 0);
            }
        }

        private void OnEditCollectionClick(int position)
        {
            EditCollectionClick?.Invoke(this, position);
        }

        private void OnOpenLocationClick(int position)
        {
            OpenLocationClick?.Invoke(this, position);
        }

        private void OnDeleteItemClick(int position)
        {
            DeleteItemClick?.Invoke(this, position);
        }

        private void OnOpenItemClick(int position)
        {
            OpenItemClick?.Invoke(this, position);
        }

        private void OnFinishClick(int position)
        {
            FinishClick?.Invoke(this, position);
        }

        public bool onItemMove(int fromPosition, int toPosition)
        {
            // Account for the header and finish cards
            int dataFrom = fromPosition - 1;

            if (dataFrom < 0 || dataFrom >= Collection.Activities.Count)
            {
                return false;
            }

            int dataTo = Math.Min(toPosition - 1, Collection.Activities.Count - 1);
            dataTo = Math.Max(dataTo, 0);

            Collection.Activities.Swap(dataFrom, dataTo);
            NotifyItemMoved(fromPosition, toPosition);

            saveProgress?.Invoke();

            return true;
        }

        public void UpdateActivity(ActivityCollection newColl)
        {
            Collection = newColl;
            NotifyItemChanged(0);
        }

        public void onItemDismiss(int position)
        {
            throw new NotImplementedException();
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
        public ImageView HandleView { get; protected set; }

        public ActivityCollectionAdapterViewHolder(View itemView, Action<int> ttsAction, Action<int> deleteListener, Action<int> openLocationListener) : base(itemView)
        {
            Title = itemView.FindViewById<TextView>(Resource.Id.activityTitle);
            Description = itemView.FindViewById<TextView>(Resource.Id.activityDesc);
            LocationText = itemView.FindViewById<TextView>(Resource.Id.locationDesc);
            ActivityIcon = itemView.FindViewById<ImageView>(Resource.Id.activityIcon);
            LocationButton = itemView.FindViewById<Button>(Resource.Id.viewLocButton);
            RemoveButton = itemView.FindViewById<Button>(Resource.Id.removeBtn);
            TtsButton = itemView.FindViewById<View>(Resource.Id.ttsBtn);
            HandleView = itemView.FindViewById<ImageView>(Resource.Id.handle);

            LocationButton.Click += (sender, e) => openLocationListener(AdapterPosition);
            RemoveButton.Click += (sender, e) => deleteListener(AdapterPosition);

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