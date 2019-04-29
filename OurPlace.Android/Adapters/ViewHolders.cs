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
using Android.Graphics;
using Android.Media;
using Android.Provider;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using FFImageLoading;
using FFImageLoading.Views;
using SectionedRecyclerview.Droid;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OurPlace.Android.Adapters
{
    public class BaseViewHolder : RecyclerView.ViewHolder
    {
        public TextView Description { get; }

        public BaseViewHolder(View itemView) : base(itemView)
        {
            // Locate and cache view references:
            Description = itemView.FindViewById<TextView>(Resource.Id.projectDesc);
        }
    }

    public class CuratorViewHolder : RecyclerView.ViewHolder
    {
        public Button ApproveBtn { get; }
        public Button DeleteBtn { get; }

        public CuratorViewHolder(View itemView, Action<bool> listener) : base(itemView)
        {
            // Locate and cache view references:
            ApproveBtn = itemView.FindViewById<Button>(Resource.Id.approveBtn);
            DeleteBtn = itemView.FindViewById<Button>(Resource.Id.deleteBtn);
            ApproveBtn.Click += (sender, e) => listener(true);
            DeleteBtn.Click += (sender, e) => listener(false);
        }
    }

    public class LearningActivityViewHolder : SectionedViewHolder
    {
        public ImageViewAsync Image { get; }
        public TextView Name { get; }
        public TextView Description { get; }
        public ImageView TickIcon { get; }
        public TextView StatusText { get; }

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

    public class GridHeaderViewHolder : SectionedViewHolder
    {
        public TextView Title { get; }
        public TextView Description { get; }

        public GridHeaderViewHolder(View itemView) : base(itemView)
        {
            // Locate and cache view references:
            Title = itemView.FindViewById<TextView>(Resource.Id.headerTitle);
            Description = itemView.FindViewById<TextView>(Resource.Id.headerDesc);
        }
    }

    public class ButtonViewHolder : RecyclerView.ViewHolder
    {
        public Button Button { get; }

        public ButtonViewHolder(View itemView, Action<int> listener) : base(itemView)
        {
            // Locate and cache view references:
            Button = itemView.FindViewById<Button>(Resource.Id.finishBtn);
            Button.Click += (sender, e) => listener(AdapterPosition);
        }
    }

    public class TaskViewHolder : RecyclerView.ViewHolder
    {
        public ImageViewAsync TaskTypeIcon { get; protected set; }
        public TextView Title { get; protected set; }
        public ImageViewAsync TaskImage { get; protected set; }
        public TextView Description { get; protected set; }
        public View TtsButton { get; protected set; }
        public TextView LockedChildrenTease { get; protected set; }

        public TaskViewHolder(View itemView, Action<int> ttsAction) : base(itemView)
        {
            // Locate and cache view references:
            Description = itemView.FindViewById<TextView>(Resource.Id.taskDesc);
            Title = itemView.FindViewById<TextView>(Resource.Id.taskTitle);
            TtsButton = itemView.FindViewById<View>(Resource.Id.ttsBtn);
            TaskTypeIcon = itemView.FindViewById<ImageViewAsync>(Resource.Id.taskTypeIcon);
            TaskImage = itemView.FindViewById<ImageViewAsync>(Resource.Id.taskImage);
            LockedChildrenTease = itemView.FindViewById<TextView>(Resource.Id.taskLockedParent);

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

    public class ActivityViewHolder : TaskViewHolder
    {
        public Button Button { get; protected set; }

        public ActivityViewHolder(View itemView, Action<int> ttsAction, Action<int> btnListener) : base(itemView, ttsAction)
        {
            // Locate and cache view references:
            Button = itemView.FindViewById<Button>(Resource.Id.taskBtn);
            Button.Click += (sender, e) => btnListener(AdapterPosition);
        }
    }

    public class TaskViewHolderName : TaskViewHolder
    {
        public LinearLayout NameSection { get; protected set; }
        public TextView EnteredNames { get; protected set; }
        public Button EditNameBtn { get; protected set; }

        public TaskViewHolderName(View itemView, Action<int> ttsAction, Action changeNameAction) : base(itemView, ttsAction)
        {
            // Locate and cache view references:
            NameSection = itemView.FindViewById<LinearLayout>(Resource.Id.nameSection);
            EnteredNames = itemView.FindViewById<TextView>(Resource.Id.enteredName);
            EditNameBtn = itemView.FindViewById<Button>(Resource.Id.nameBtn);
            EditNameBtn.Click += (sender, e) => changeNameAction();
        }
    }

    public class TaskViewHolderInfo : TaskViewHolder
    {
        public Button Button { get; protected set; }

        public TaskViewHolderInfo(View itemView, Action<int> ttsAction, Action<int> listener) : base(itemView, ttsAction)
        {
            // Locate and cache view references:
            Button = itemView.FindViewById<Button>(Resource.Id.taskBtn);
            Button.Click += (sender, e) => listener(AdapterPosition);
        }
    }

    public class TaskViewHolderBtn : TaskViewHolder
    {
        public Button Button { get; protected set; }

        public TaskViewHolderBtn(View itemView, Action<int> ttsAction, Action<int> btnListener) : base(itemView, ttsAction)
        {
            // Locate and cache view references:
            Button = itemView.FindViewById<Button>(Resource.Id.taskBtn);
            Button.Click += (sender, e) => btnListener(AdapterPosition);
        }
    }

    public class TaskViewHolderCreatedTask : TaskViewHolder
    {
        public Button EditBtn { get; protected set; }
        public Button DeleteBtn { get; protected set; }
        public Button ManageChildrenBtn { get; protected set; }

        public TaskViewHolderCreatedTask(View itemView, Action<int> ttsAction, Action<int> deleteListener, Action<int> editListener, Action<int> manageChildListener) : base(itemView, ttsAction)
        {
            // Locate and cache view references:
            EditBtn = itemView.FindViewById<Button>(Resource.Id.editTaskBtn);
            EditBtn.Click += (sender, e) => editListener(AdapterPosition);
            DeleteBtn = itemView.FindViewById<Button>(Resource.Id.deleteTaskBtn);
            DeleteBtn.Click += (sender, e) => deleteListener(AdapterPosition);
            ManageChildrenBtn = itemView.FindViewById<Button>(Resource.Id.addChildTaskBtn);
            ManageChildrenBtn.Click += (sender, e) => manageChildListener(AdapterPosition);
        }
    }

    public class TaskViewHolderUploadCard : TaskViewHolder
    {
        public Button UploadBtn { get; protected set; }
        public Button DeleteBtn { get; protected set; }

        public TaskViewHolderUploadCard(View itemView, Action<int> ttsAction, Action<int> btnListener1, Action<int> btnListener2) : base(itemView, ttsAction)
        {
            // Locate and cache view references:
            UploadBtn = itemView.FindViewById<Button>(Resource.Id.uploadBtn);
            UploadBtn.Click += (sender, e) => btnListener1(AdapterPosition);
            DeleteBtn = itemView.FindViewById<Button>(Resource.Id.deleteBtn);
            DeleteBtn.Click += (sender, e) => btnListener2(AdapterPosition);
        }
    }

    public class TaskViewHolderResultList : TaskViewHolder
    {
        public LinearLayout ItemList { get; protected set; }
        public Button StartTaskButton { get; protected set; }

        protected List<string> Results;
        protected Action<int, int> OnItemTap;

        public TaskViewHolderResultList(View itemView, Action<int> ttsAction, Action<int> btnListener, Action<int, int> itemTapListener) : base(itemView, ttsAction)
        {
            // Locate and cache view references:
            ItemList = itemView.FindViewById<LinearLayout>(Resource.Id.resultListLayout);
            StartTaskButton = itemView.FindViewById<Button>(Resource.Id.taskBtn);
            StartTaskButton.Click += (sender, e) => btnListener(AdapterPosition);
            OnItemTap = itemTapListener;
            Results = new List<string>();
        }

        /// <summary>
        /// Populates the empty LinearLayout with images loaded from the given paths
        /// </summary>
        /// <param name="files">Collection of image paths</param>
        /// <param name="context">Activity context for image loading</param>
        public virtual void ShowResults(IEnumerable<string> files, Activity context)
        {
            ClearResults();

            if (files == null)
            {
                return;
            }

            foreach (string imagePath in files)
            {
                ImageViewAsync newImage = new ImageViewAsync(context);
                newImage.SetPadding(10, 10, 10, 10);

                ItemList.AddView(newImage);
                ImageService.Instance.LoadFile(imagePath)
                    .DownSampleInDip(width: 150)
                    .Into(newImage);

                newImage.Click += Item_Click;
            }
        }

        /// <summary>
        /// Called when an image is clicked
        /// </summary>
        protected void Item_Click(object sender, EventArgs e)
        {
            OnItemTap(AdapterPosition, ItemList.IndexOfChild((View)sender));
        }

        /// <summary>
        /// Removes all photos from the viewholder
        /// </summary>
        public void ClearResults()
        {
            Results.Clear();
            ItemList.RemoveAllViews();
        }
    }

    public class TaskViewHolderRecordVideo : TaskViewHolderResultList
    {
        public TaskViewHolderRecordVideo(View itemView, Action<int> ttsAction, Action<int> btnListener, Action<int, int> thumbnailTapListener)
            : base(itemView, ttsAction, btnListener, thumbnailTapListener)
        {
        }

        public override void ShowResults(IEnumerable<string> files, Activity context)
        {
            ClearResults();

            if (files == null)
            {
                return;
            }

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

            ItemList.AddView(newImage);
            newImage.SetImageBitmap(thumb);

            newImage.Click += Item_Click;
        }
    }

    public class TaskViewHolderRecordAudio : TaskViewHolderResultList
    {
        public TaskViewHolderRecordAudio(View itemView, Action<int> ttsAction, Action<int> btnListener, Action<int, int> itemTapListener)
            : base(itemView, ttsAction, btnListener, itemTapListener)
        {
        }

        public override void ShowResults(IEnumerable<string> files, Activity context)
        {
            ClearResults();

            if (files == null)
            {
                return;
            }

            for (int i = 0; i < files.Count(); i++)
            {
                Button openButton = new Button(context);
                openButton.SetPadding(10, 10, 10, 10);
                openButton.Text = string.Format(context.Resources.GetString(Resource.String.openAudioBtn), i + 1);
                openButton.Click += Item_Click;
                ItemList.AddView(openButton);
            }
        }
    }

    public class TaskViewHolderMultipleChoice : TaskViewHolder
    {
        public RadioGroup RadioGroup { get; protected set; }
        public ImageViewAsync Image { get; protected set; }

        public TaskViewHolderMultipleChoice(View itemView, Action<int> ttsAction) : base(itemView, ttsAction)
        {
            // Locate and cache view references:
            RadioGroup = itemView.FindViewById<RadioGroup>(Resource.Id.taskRadioGroup);
            Image = itemView.FindViewById<ImageViewAsync>(Resource.Id.taskImage);
        }
    }

    public class TaskViewHolderTextEntry : TaskViewHolder
    {
        public EditText TextField { get; protected set; }
        public ImageViewAsync Image { get; protected set; }

        public TaskViewHolderTextEntry(View itemView, Action<int> ttsAction, Action<object, int> textListener) : base(itemView, ttsAction)
        {
            // Locate and cache view references:
            TextField = itemView.FindViewById<EditText>(Resource.Id.taskTextEntry);
            Image = itemView.FindViewById<ImageViewAsync>(Resource.Id.taskImage);
            TextField.AfterTextChanged += (sender, e) => textListener(TextField, AdapterPosition);
        }
    }

    public class TaskViewHolderMap : TaskViewHolderBtn
    {
        public TextView EnteredLocationsView { get; protected set; }

        public TaskViewHolderMap(View itemView, Action<int> ttsAction, Action<int> btnListener) : base(itemView, ttsAction, btnListener)
        {
            EnteredLocationsView = itemView.FindViewById<TextView>(Resource.Id.chosenLocs);
        }
    }
}