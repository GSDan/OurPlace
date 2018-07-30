#region copyright
/*
    OurPlace is a mobile learning platform, designed to support communities
    in creating and sharing interactive learning activities about the places they care most about.
    https://github.com/GSDan/OurPlace
    Copyright (C) 2018 Dan Richardson

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see https://www.gnu.org/licenses.
*/
#endregion
using Android.App;
using Android.Gms.Maps;
using Android.Gms.Maps.Model;
using Android.Graphics;
using Android.Media;
using Android.Provider;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using FFImageLoading;
using FFImageLoading.Views;
using Newtonsoft.Json;
using OurPlace.Common.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace OurPlace.Android.Adapters
{
    public class BaseViewHolder : RecyclerView.ViewHolder
    {
        public TextView Description { get; private set; }

        public BaseViewHolder(View itemView) : base(itemView)
        {
            // Locate and cache view references:
            Description = itemView.FindViewById<TextView>(Resource.Id.projectDesc);
        }
    }

    public class CuratorViewHolder : RecyclerView.ViewHolder
    {
        public Button ApproveBtn { get; private set; }
        public Button DeleteBtn { get; private set; }

        public CuratorViewHolder(View itemView, Action<bool> listener) : base(itemView)
        {
            // Locate and cache view references:
            ApproveBtn = itemView.FindViewById<Button>(Resource.Id.approveBtn);
            DeleteBtn = itemView.FindViewById<Button>(Resource.Id.deleteBtn);
            ApproveBtn.Click += (sender, e) => listener(true);
            DeleteBtn.Click += (sender, e) => listener(false);
        }
    }

    public class LearningActivityViewHolder : SectionedRecyclerview.Droid.SectionedViewHolder
    {
        public ImageViewAsync Image { get; private set; }
        public TextView Name { get; private set; }
        public TextView Description { get; private set; }
        public ImageView TickIcon { get; private set; }
        public TextView StatusText { get; private set; }

        public LearningActivityViewHolder(View itemView, Action<int> listener) : base(itemView)
        {
            // Locate and cache view references:
            Image = itemView.FindViewById<ImageViewAsync>(Resource.Id.projGridImage);
            Name = itemView.FindViewById<TextView>(Resource.Id.projGridTitle);
            Description = itemView.FindViewById<TextView>(Resource.Id.projGridDesc);
            TickIcon = ItemView.FindViewById<ImageView>(Resource.Id.activityStatusTick);
            StatusText = ItemView.FindViewById<TextView>(Resource.Id.activityStatusText);

            itemView.Click += (sender, e) => listener(AdapterPosition);
        }
    }

    public class GridHeaderViewHolder : SectionedRecyclerview.Droid.SectionedViewHolder
    {
        public TextView Title { get; private set; }
        public TextView Description { get; private set; }

        public GridHeaderViewHolder(View itemView) : base(itemView)
        {
            // Locate and cache view references:
            Title = itemView.FindViewById<TextView>(Resource.Id.headerTitle);
            Description = itemView.FindViewById<TextView>(Resource.Id.headerDesc);
        }
    }

    public class ButtonViewHolder : RecyclerView.ViewHolder
    {
        public Button Button { get; private set; }

        public ButtonViewHolder(View itemView, Action<int> listener) : base(itemView)
        {
            // Locate and cache view references:
            Button = itemView.FindViewById<Button>(Resource.Id.finishBtn);
            Button.Click += (sender, e) => listener(AdapterPosition);
        }
    }

    public class TaskViewHolder : RecyclerView.ViewHolder
    {
        public TextView Title { get; protected set; }
        public TextView Description { get; protected set; }
        public View ttsBtn { get; protected set; }
        public TextView LockedChildrenTease { get; protected set; }

        public TaskViewHolder(View itemView, Action<int> ttsAction) : base(itemView)
        {
            // Locate and cache view references:
            Description = itemView.FindViewById<TextView>(Resource.Id.taskDesc);
            Title = itemView.FindViewById<TextView>(Resource.Id.taskTitle);
            ttsBtn = itemView.FindViewById<View>(Resource.Id.ttsBtn);
            LockedChildrenTease = itemView.FindViewById<TextView>(Resource.Id.taskLockedParent);

            if(ttsBtn != null)
            {
                if (ttsAction == null)
                {
                    ttsBtn.Visibility = ViewStates.Invisible;
                }
                else
                {
                    ttsBtn.Visibility = ViewStates.Visible;
                    ttsBtn.Click += (sender, e) => ttsAction(AdapterPosition);
                }
            }
        }
    }

    public class ActivityViewHolder : TaskViewHolder
    {
        public ImageViewAsync Image { get; protected set; }
        public Button Button { get; protected set; }

        public ActivityViewHolder(View itemView, Action<int> ttsAction, Action<int> btnListener) : base(itemView, ttsAction)
        {
            // Locate and cache view references:
            Image = itemView.FindViewById<ImageViewAsync>(Resource.Id.activityIcon);
            Button = itemView.FindViewById<Button>(Resource.Id.taskBtn);

            Button.Click += (sender, e) => btnListener(AdapterPosition);
        }
    }

    public class TaskViewHolder_Name : TaskViewHolder
    {
        public LinearLayout NameSection { get; protected set; }
        public TextView EnteredNames { get; protected set; }
        public Button EditNameBtn { get; protected set; }

        public TaskViewHolder_Name(View itemView, Action<int> ttsAction, Action changeNameAction) : base(itemView, ttsAction)
        {
            // Locate and cache view references:
            NameSection = itemView.FindViewById<LinearLayout>(Resource.Id.nameSection);
            EnteredNames = itemView.FindViewById<TextView>(Resource.Id.enteredName);
            EditNameBtn = itemView.FindViewById<Button>(Resource.Id.nameBtn);
            EditNameBtn.Click += (sender, e) => changeNameAction();
        }
    }

    public class TaskViewHolder_Info : TaskViewHolder
    {
        public ImageViewAsync Image { get; protected set; }
        public Button Button { get; protected set; }

        public TaskViewHolder_Info(View itemView, Action<int> ttsAction, Action<int> listener) : base(itemView, ttsAction)
        {
            // Locate and cache view references:
            Image = itemView.FindViewById<ImageViewAsync>(Resource.Id.taskImage);
            Button = itemView.FindViewById<Button>(Resource.Id.taskBtn);

            Button.Click += (sender, e) => listener(AdapterPosition);
        }
    }

    public class TaskViewHolder_Btn : TaskViewHolder
    {
        public ImageViewAsync Image { get; protected set; }
        public Button Button { get; protected set; }

        public TaskViewHolder_Btn(View itemView, Action<int> ttsAction, Action<int> btnListener) : base(itemView, ttsAction)
        {
            // Locate and cache view references:
            Image = itemView.FindViewById<ImageViewAsync>(Resource.Id.taskImage);
            Button = itemView.FindViewById<Button>(Resource.Id.taskBtn);

            Button.Click += (sender, e) => btnListener(AdapterPosition);
        }
    }

    public class TaskViewHolder_CreatedTask : TaskViewHolder
    {
        public ImageViewAsync Image { get; protected set; }
        public Button EditBtn { get; protected set; }
        public Button DeleteBtn { get; protected set; }
        public Button ManageChildrenBtn { get; protected set; }

        public TaskViewHolder_CreatedTask(View itemView, Action<int> ttsAction, Action<int> deleteListener, Action<int> editListener, Action<int> manageChildListener) : base(itemView, ttsAction)
        {
            // Locate and cache view references:
            Image = itemView.FindViewById<ImageViewAsync>(Resource.Id.taskImage);
            EditBtn = itemView.FindViewById<Button>(Resource.Id.editTaskBtn);
            EditBtn.Click += (sender, e) => editListener(AdapterPosition);
            DeleteBtn = itemView.FindViewById<Button>(Resource.Id.deleteTaskBtn);
            DeleteBtn.Click += (sender, e) => deleteListener(AdapterPosition);
            ManageChildrenBtn = itemView.FindViewById<Button>(Resource.Id.addChildTaskBtn);
            ManageChildrenBtn.Click += (sender, e) => manageChildListener(AdapterPosition);
        }
    }

    public class TaskViewHolder_UploadCard : TaskViewHolder
    {
        public ImageViewAsync Image { get; protected set; }
        public Button UploadBtn { get; protected set; }
        public Button DeleteBtn { get; protected set; }

        public TaskViewHolder_UploadCard(View itemView, Action<int> ttsAction, Action<int> btnListener1, Action<int> btnListener2) : base(itemView, ttsAction)
        {
            // Locate and cache view references:
            Image = itemView.FindViewById<ImageViewAsync>(Resource.Id.taskImage);
            UploadBtn = itemView.FindViewById<Button>(Resource.Id.uploadBtn);
            UploadBtn.Click += (sender, e) => btnListener1(AdapterPosition);
            DeleteBtn = itemView.FindViewById<Button>(Resource.Id.deleteBtn);
            DeleteBtn.Click += (sender, e) => btnListener2(AdapterPosition);
        }
    }

    public class TaskViewHolder_Photo : TaskViewHolder
    {
        public ImageViewAsync TaskImage { get; protected set; }
        public LinearLayout ImageList { get; protected set; }
        public Button Button { get; protected set; }

        protected List<string> addedPhotos;
        protected Action<int, int> onPhotoTap;

        public TaskViewHolder_Photo(View itemView, Action<int> ttsAction, Action<int> btnListener, Action<int, int> photoTapListener) : base(itemView, ttsAction)
        {
            // Locate and cache view references:
            TaskImage = itemView.FindViewById<ImageViewAsync>(Resource.Id.taskImage);
            ImageList = itemView.FindViewById<LinearLayout>(Resource.Id.takenPhotoLayout);
            Button = itemView.FindViewById<Button>(Resource.Id.taskBtn);

            addedPhotos = new List<string>();

            Button.Click += (sender, e) => btnListener(AdapterPosition);
            onPhotoTap = photoTapListener;
        }

        /// <summary>
        /// Populates the empty LinearLayout with images loaded from the given paths
        /// </summary>
        /// <param name="files">Collection of image paths</param>
        /// <param name="context">Activity context for image loading</param>
        public virtual void ShowResults(IEnumerable<string> files, Activity context)
        {
            ClearResults();

            if (files == null) return;

            foreach (string imagePath in files)
            {
                ImageViewAsync newImage = new ImageViewAsync(context);
                newImage.SetPadding(10, 10, 10, 10);

                ImageList.AddView(newImage);
                ImageService.Instance.LoadFile(imagePath)
                    .DownSampleInDip(width: 150)
                    .IntoAsync(newImage);

                newImage.Click += NewImage_Click;
            }
        }

        /// <summary>
        /// Called when an image is clicked
        /// </summary>
        protected void NewImage_Click(object sender, EventArgs e)
        {
            onPhotoTap(AdapterPosition, ImageList.IndexOfChild((View)sender));
        }

        /// <summary>
        /// Removes all photos from the viewholder
        /// </summary>
        public void ClearResults()
        {
            addedPhotos.Clear();
            ImageList.RemoveAllViews();
        }
    }

    public class TaskViewHolder_RecordVideo : TaskViewHolder_Photo
    {
        public TaskViewHolder_RecordVideo(View itemView, Action<int> ttsAction, Action<int> btnListener, Action<int, int> thumbnailTapListener) 
            : base(itemView, ttsAction, btnListener, thumbnailTapListener)
        {
        }

        public override void ShowResults(IEnumerable<string> files, Activity context)
        {
            ClearResults();

            if (files == null) return;

            foreach (string imagePath in files)
            {
                var suppress = LoadThumbIntoImageList(imagePath, context);
            }
        }

        private async Task LoadThumbIntoImageList(string imagePath, Activity context)
        {
            Bitmap thumb = await ThumbnailUtils.CreateVideoThumbnailAsync(imagePath, ThumbnailKind.MiniKind);
            ImageViewAsync newImage = new ImageViewAsync(context);
            newImage.SetPadding(10, 10, 10, 10);

            ImageList.AddView(newImage);
            newImage.SetImageBitmap(thumb);

            newImage.Click += NewImage_Click;
        }
    }

    //public class TaskViewHolder_RecordVideo : TaskViewHolder
    //{
    //    public ImageViewAsync Image { get; protected set; }
    //    public ImageViewAsync Overlay { get; protected set; }
    //    public Button Button { get; protected set; }

    //    public TaskViewHolder_RecordVideo(View itemView, Action<int> ttsAction, Action<int> btnListener, Action<int> launchVideoListener) : base(itemView, ttsAction)
    //    {
    //        // Locate and cache view references:
    //        Image = itemView.FindViewById<ImageViewAsync>(Resource.Id.taskImage);
    //        Overlay = itemView.FindViewById<ImageViewAsync>(Resource.Id.playIcon);
    //        Button = itemView.FindViewById<Button>(Resource.Id.taskBtn);

    //        Button.Click += (sender, e) => btnListener(AdapterPosition);
    //        Overlay.Click += (sender, e) => launchVideoListener(AdapterPosition);
    //    }

    //    public async void ManageVideoThumb(string videoPaths, string defaultImageUrl)
    //    {
    //        string[] paths = JsonConvert.DeserializeObject<string[]>(videoPaths);

    //        if(paths != null && paths.Length > 0)
    //        {
    //            Bitmap thumb = await ThumbnailUtils.CreateVideoThumbnailAsync(paths[0], ThumbnailKind.MiniKind);
    //            Image.SetImageBitmap(thumb);
    //            Overlay.Visibility = ViewStates.Visible;
    //        }
    //        else
    //        {
    //            ImageService.Instance.LoadUrl(defaultImageUrl).Into(Image);
    //            Overlay.Visibility = ViewStates.Invisible;
    //        }
    //    }
    //}

    public class TaskViewHolder_RecordAudio : TaskViewHolder
    {
        public ImageViewAsync Image { get; protected set; }
        public Button PlaybackButton { get; protected set; }
        public Button RecordButton { get; protected set; }

        public TaskViewHolder_RecordAudio(View itemView, Action<int> ttsAction, Action<int> btnListener, Action<int, int> playAudioListener) : base(itemView, ttsAction)
        {
            // Locate and cache view references:
            Image = itemView.FindViewById<ImageViewAsync>(Resource.Id.taskImage);
            PlaybackButton = itemView.FindViewById<Button>(Resource.Id.playBackBtn);
            RecordButton = itemView.FindViewById<Button>(Resource.Id.recordBtn);

            RecordButton.Click += (sender, e) => btnListener(AdapterPosition);
            PlaybackButton.Click += (sender, e) => playAudioListener(AdapterPosition, 0);
        }
    }

    public class TaskViewHolder_MultipleChoice : TaskViewHolder
    {
        public RadioGroup RadioGroup { get; protected set; }
        public ImageViewAsync Image { get; protected set; }

        public TaskViewHolder_MultipleChoice(View itemView, Action<int> ttsAction) : base(itemView, ttsAction)
        {
            // Locate and cache view references:
            RadioGroup = itemView.FindViewById<RadioGroup>(Resource.Id.taskRadioGroup);
            Image = itemView.FindViewById<ImageViewAsync>(Resource.Id.taskImage);
        }
    }

    public class TaskViewHolder_TextEntry : TaskViewHolder
    {
        public EditText TextField { get; protected set; }
        public ImageViewAsync Image { get; protected set; }

        public TaskViewHolder_TextEntry(View itemView, Action<int> ttsAction, Action<object, int> textListener) : base(itemView, ttsAction)
        {
            // Locate and cache view references:
            TextField = itemView.FindViewById<EditText>(Resource.Id.taskTextEntry);
            Image = itemView.FindViewById<ImageViewAsync>(Resource.Id.taskImage);
            TextField.AfterTextChanged += (sender, e) => textListener(TextField, AdapterPosition);
        }
    }

    public class TaskViewHolder_Map : TaskViewHolder_Btn
    {
        public TextView EnteredLocsTextView { get; protected set; }

        public TaskViewHolder_Map(View itemView, Action<int> ttsAction, Action<int> btnListener) : base(itemView, ttsAction, btnListener)
        {
            EnteredLocsTextView = itemView.FindViewById<TextView>(Resource.Id.chosenLocs);
        }
    }
}