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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Android.App;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using FFImageLoading;
using FFImageLoading.Cache;
using Microsoft.AppCenter.Analytics;
using Newtonsoft.Json;
using OurPlace.Common;
using OurPlace.Common.Models;

namespace OurPlace.Android.Adapters
{
    public class TaskAdapter : RecyclerView.Adapter
    {
        private const int TaskPhoto = 1;
        private const int TaskMultipleChoice = 2;
        private const int TaskDrawing = 3;
        private const int TaskTextEntry = 4;
        private const int TaskVideo = 5;
        private const int TaskAudio = 6;
        private const int TaskLocationHunt = 7;
        private const int TaskLocationMarker = 8;
        private const int TaskInfo = 9;
        private const int TaskListen = 10;
        private const int TaskScan = 11;
        private const int Finish = 12;
        private const int Curate = 13;
        private const int Names = 14;

        public List<AppTask> Items;
        private readonly Dictionary<int, List<AppTask>> hiddenChildren;
        private readonly Dictionary<string, int> viewTypes;
        public event EventHandler<int> ItemClick;
        public event EventHandler<int> TextEntered;
        public event EventHandler<bool> Approved;
        public event EventHandler<int> SpeakText;
        public event EventHandler<int> ChangeName;
        public Action<int, int> ShowMedia;
        public bool OnBind;
        public bool Curator;
        private readonly bool reqName;
        private readonly string description;
        private string names;
        private readonly Activity context;

        public TaskAdapter(Activity context, List<AppTask> data, string description, bool curator, bool reqName)
        {
            this.context = context;
            this.Curator = curator;
            this.description = description;
            this.reqName = reqName;

            Items = data ?? new List<AppTask>();

            hiddenChildren = new Dictionary<int, List<AppTask>>();
            if (data != null)
            {
                AppTask[] temp = data.ToArray();
                foreach (AppTask parent in temp)
                {
                    if (parent.TaskType.IdName == "INFO")
                    {
                        parent.IsCompleted = true;
                    }

                    if (!(parent.ChildAppTasks?.Count > 0))
                    {
                        continue;
                    }

                    hiddenChildren.Add(parent.Id, parent.ChildAppTasks);
                    CheckForChildren(Items.IndexOf(parent));
                }
            }

            // Add curator controls to the start of the list
            if (curator)
            {
                Items.Insert(0, null);
            }

            Items.Insert(0, null);
            Items.Add(null);

            viewTypes = new Dictionary<string, int>
            {
                {"TAKE_PHOTO", TaskPhoto},
                {"MATCH_PHOTO", TaskPhoto},
                {"MULT_CHOICE", TaskMultipleChoice},
                {"DRAW", TaskDrawing },
                {"DRAW_PHOTO", TaskDrawing },
                {"ENTER_TEXT", TaskTextEntry },
                {"TAKE_VIDEO", TaskVideo },
                {"REC_AUDIO", TaskAudio },
                {"LOC_HUNT", TaskLocationHunt },
                {"MAP_MARK", TaskLocationMarker },
                {"INFO", TaskInfo },
                {"LISTEN_AUDIO", TaskListen },
                {"SCAN_QR", TaskScan }
            };
        }

        public override int ItemCount => Items.Count;

        private void OnClick(int position)
        {
            ItemClick?.Invoke(this, position);
        }

        private void OnSpeakClick(int position)
        {
            SpeakText?.Invoke(this, position);
        }

        private void OnCurate(bool approved)
        {
            Approved?.Invoke(this, approved);
        }

        private void OnText(object obj, int position)
        {
            TextEntered?.Invoke(obj, position);
            Items[position].IsCompleted = true;
            CheckForChildren(position);
        }

        private void OnMediaClick(int taskIndex, int pathIndex)
        {
            ShowMedia?.Invoke(taskIndex, pathIndex);
            Items[taskIndex].IsCompleted = true;
            CheckForChildren(taskIndex);
        }

        private void OnChangeNameClick()
        {
            ChangeName?.Invoke(this, -1);
        }

        public AppTask GetTaskWithId(int id)
        {
            return Items.FirstOrDefault(t => t != null && t.Id == id);
        }

        public int GetIndexWithId(int id)
        {
            return Items.IndexOf(Items.FirstOrDefault(t => t != null && t.Id == id));
        }

        public bool IsPositionAChildView(int position)
        {
            if (position >= Items.Count || position <= 0 || (Curator && position == 1) || position == Items.Count - 1)
            {
                // Can't be a child
                return false;
            }

            return Items[position].IsChild;
        }

        /// <summary>
        /// Called when a task has returned new JSON data successfully.
        /// </summary>
        /// <param name="taskId">The task's Id</param>
        /// <param name="json">New JSON data</param>
        public void OnFileReturned(int taskId, string json, bool possibleSourceImage)
        {
            int i = GetIndexWithId(taskId);

            List<string> files = JsonConvert.DeserializeObject<List<string>>(Items[i].CompletionData.JsonData) ?? new List<string>();

            files.Add(json);

            Items[i].IsCompleted = true;
            Items[i].CompletionData.JsonData = JsonConvert.SerializeObject(files);

            if (possibleSourceImage)
            {
                // Check if any other tasks are waiting for this photo
                NotifyDataSetChanged();
            }

            ImageService.Instance.InvalidateCacheEntryAsync(json, CacheType.All, true);
            NotifyItemChanged(i);

            // Tell the Finished button to update, just in case this is
            // the last unfinished task.
            NotifyItemChanged(Items.Count - 1);

            CheckForChildren(i);
        }

        public void CheckForChildren(int position)
        {
            AppTask parent = Items[position];
            bool hasChildren = parent.ChildTasks != null && parent.ChildTasks.Any();

            if (hasChildren && !parent.IsCompleted)
            {
                if (hiddenChildren.ContainsKey(parent.Id))
                {
                    return;
                }

                // Parent task is no longer complete, hide children
                hiddenChildren[parent.Id] = new List<AppTask>();
                foreach(LearningTask child in parent.ChildTasks)
                {
                    AppTask childProgress = GetTaskWithId(child.Id);
                    if (childProgress == null)
                    {
                        continue;
                    }

                    hiddenChildren[parent.Id].Add(childProgress);
                    Items.Remove(childProgress);
                }
                NotifyDataSetChanged();
            }
            else if (hasChildren && Items[position].IsCompleted && hiddenChildren.ContainsKey(Items[position].Id))
            {
                // Show the child tasks if they're hidden
                var children = hiddenChildren[Items[position].Id];
                int nextPos = position + 1;
                foreach(AppTask child in children)
                {
                    if(!Items.Exists(t => t?.Id == child.Id))
                    {
                        Items.Insert(nextPos++, child);
                    }
                }
                hiddenChildren.Remove(Items[position].Id);
                NotifyDataSetChanged();
            }
        }

        /// <summary>
        /// Called when the MapActivity has returned
        /// </summary>
        /// <param name="taskId">Learning Task ID</param>
        /// <param name="locationsJson">List of Map_Location objects in json</param>
        /// <param name="isPoly">If the locations join in a loop</param>
        public void OnMapReturned(int taskId, string locationsJson, bool isPoly)
        {
            AppTask thisTask = GetTaskWithId(taskId);
            List<Map_Location> locations = JsonConvert.DeserializeObject<List<Map_Location>>(locationsJson);
            thisTask.CompletionData.JsonData = JsonConvert.SerializeObject(locations);
            OnGenericTaskReturned(taskId, true);
        }

        public void OnGenericTaskReturned(int taskId, bool complete)
        {
            AppTask thisTask = GetTaskWithId(taskId);
            thisTask.IsCompleted = complete;
            int ind = Items.IndexOf(thisTask);
            NotifyItemChanged(ind);
            CheckForChildren(ind);
        }

        public void UpdateNames(string updatedNames)
        {
            names = updatedNames;
            NotifyDataSetChanged();
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            TaskViewHolder vh;

            Type thisType = holder.GetType();
            OnBind = true;

            if (Curator)
            {
                if (position == 0)
                {
                    return;
                }
            }

            if (position == 0 || (Curator && position == 1))
            {
                // Activity description + entered names (if required)
                vh = holder as TaskViewHolderName;
                if (vh == null)
                {
                    return;
                }

                vh.Title.Text = description;
                vh.Description.Text = context.Resources.GetString(Resource.String.TasksTitle);

                ((TaskViewHolderName) vh).NameSection.Visibility =
                    (reqName) ? ViewStates.Visible : ViewStates.Gone;
                ((TaskViewHolderName) vh).EnteredNames.Text = names;

                return;
            }

            if (position == Items.Count - 1)
            {
                // Finish button
                if (holder is ButtonViewHolder bvh)
                {
                    bvh.Button.Enabled = true;
                }

                return;
            }

            string taskType = Items[position].TaskType.IdName;
             if (thisType == typeof(TaskViewHolderInfo))
            {
                AdditionalInfoData taskInfo = JsonConvert.DeserializeObject<AdditionalInfoData>(Items[position].JsonData);
                vh = holder as TaskViewHolderInfo;
                Items[position].IsCompleted = true;

                if (!string.IsNullOrWhiteSpace(taskInfo.ImageUrl))
                {
                    Items[position].ImageUrl = taskInfo.ImageUrl;
                }

                TaskViewHolderInfo taskViewHolderInfo = (TaskViewHolderInfo) vh;
                if (taskViewHolderInfo != null)
                {
                    taskViewHolderInfo.Button.Visibility =
                        (string.IsNullOrWhiteSpace(taskInfo.ExternalUrl)) ? ViewStates.Gone : ViewStates.Visible;
                } 
            }
            else if (thisType == typeof(TaskViewHolderRecordAudio))
            {
                vh = holder as TaskViewHolderRecordAudio;
                if (vh != null)
                {
                    ((TaskViewHolderRecordAudio) vh).StartTaskButton.Text =
                        context.Resources.GetString(Resource.String.StartBtn);

                    if (!string.IsNullOrWhiteSpace(Items[position].CompletionData.JsonData))
                    {
                        List<string> audioPaths =
                            JsonConvert.DeserializeObject<List<string>>(Items[position].CompletionData.JsonData);

                        ((TaskViewHolderRecordAudio) vh).ShowResults(audioPaths, context);
                        Items[position].IsCompleted = true;
                    }
                    else
                    {
                        Items[position].IsCompleted = false;
                        ((TaskViewHolderRecordAudio) vh).ClearResults();
                    }
                }
            }
            else if (thisType == typeof(TaskViewHolderRecordVideo))
            {
                vh = holder as TaskViewHolderRecordVideo;
                if (vh != null)
                {
                    ((TaskViewHolderRecordVideo) vh).StartTaskButton.Text =
                        context.Resources.GetString(Resource.String.RecBtn);

                    if (!string.IsNullOrWhiteSpace(Items[position].CompletionData.JsonData))
                    {
                        List<string> videoPaths =
                            JsonConvert.DeserializeObject<List<string>>(Items[position].CompletionData.JsonData);

                        ((TaskViewHolderRecordVideo) vh).ShowResults(videoPaths, context);
                        Items[position].IsCompleted = true;
                    }
                    else
                    {
                        Items[position].IsCompleted = false;
                        ((TaskViewHolderRecordVideo) vh).ClearResults();
                    }
                }
            }
            else if (thisType == typeof(TaskViewHolderResultList))
            {
                vh = holder as TaskViewHolderResultList;

                bool btnEnabled = true;
                string btnText;

                if (taskType == "DRAW" || taskType == "DRAW_PHOTO")
                {
                    btnText = context.Resources.GetString(Resource.String.StartDrawBtn);

                    if (taskType == "DRAW_PHOTO")
                    {
                        btnEnabled = !int.TryParse(Items[position].JsonData, out var idResult) || GetTaskWithId(idResult).IsCompleted;

                        if (!btnEnabled)
                        {
                            btnText = context.Resources.GetString(Resource.String.TaskBtnNotReady);
                        }
                    }
                }
                else
                {
                    btnText = context.Resources.GetString(Resource.String.TakePhotoBtn);
                }

                if (vh != null)
                {
                    ((TaskViewHolderResultList) vh).StartTaskButton.Text = btnText;
                    ((TaskViewHolderResultList) vh).StartTaskButton.Enabled = btnEnabled;

                    if (!string.IsNullOrWhiteSpace(Items[position].CompletionData.JsonData))
                    {
                        List<string> photoPaths =
                            JsonConvert.DeserializeObject<List<string>>(Items[position].CompletionData.JsonData);

                        ((TaskViewHolderResultList) vh).ShowResults(photoPaths, context);
                        Items[position].IsCompleted = true;
                    }
                    else
                    {
                        Items[position].IsCompleted = false;
                        ((TaskViewHolderResultList) vh).ClearResults();
                    }
                }
            }
            else if (thisType == typeof(TaskViewHolderBtn))
            {
                vh = holder as TaskViewHolderBtn;
                string btnText = context.Resources.GetString(Resource.String.TaskBtn);

                if (taskType == "LISTEN_AUDIO")
                {
                    btnText = context.Resources.GetString(Resource.String.ListenBtn);
                }

                TaskViewHolderBtn viewHolderBtn = (TaskViewHolderBtn)vh;
                if (viewHolderBtn != null)
                {
                    viewHolderBtn.Button.Text = btnText;
                }
            }
            else if (thisType == typeof(TaskViewHolderTextEntry))
            {
                vh = holder as TaskViewHolderTextEntry;
                if (vh != null)
                {
                    ((TaskViewHolderTextEntry) vh).TextField.Text = Items[position].CompletionData.JsonData;
                    Items[position].IsCompleted =
                        !string.IsNullOrWhiteSpace(((TaskViewHolderTextEntry) vh).TextField.Text);
                }
            }
            else
             {
                 if (thisType == typeof(TaskViewHolderMultipleChoice))
                 {
                     vh = holder as TaskViewHolderMultipleChoice;
                     RadioGroup radios = ((TaskViewHolderMultipleChoice)vh)?.RadioGroup;

                     string[] choices = JsonConvert.DeserializeObject<string[]>(Items[position].JsonData);
                     int.TryParse(Items[position].CompletionData.JsonData, out var answeredInd);

                     if (radios != null && radios.ChildCount == 0)
                     {
                         int index = 0;
                         foreach (string option in choices)
                         {
                             RadioButton rad = new RadioButton(context) { Text = option };
                             rad.SetPadding(0, 0, 0, 5);
                             rad.TextSize = 16;
                             radios.AddView(rad);

                             if (Items[position].IsCompleted && answeredInd == index)
                             {
                                 ((RadioButton)radios.GetChildAt(answeredInd)).Checked = true;
                             }
                             index++;
                         }
                     }

                     if (answeredInd == -1)
                     {
                         Items[position].IsCompleted = false;
                     }

                     if (radios != null)
                     {
                         radios.CheckedChange += (sender, e) =>
                         {
                             Items[position].IsCompleted = true;
                             int radioButtonId = radios.CheckedRadioButtonId;
                             View radioButton = radios.FindViewById(radioButtonId);
                             int idx = radios.IndexOfChild(radioButton);
                             Items[position].CompletionData.JsonData = idx.ToString();
                             NotifyItemChanged(Items.Count - 1);
                         };
                     }
                 }
                 else if (thisType == typeof(TaskViewHolderMap))
                 {
                     vh = holder as TaskViewHolderMap;
                     TaskViewHolderMap mapHolder = ((TaskViewHolderMap)vh);

                     if (taskType == "MAP_MARK")
                     {
                         List<Map_Location> points = JsonConvert.DeserializeObject<List<Map_Location>>(Items[position].CompletionData.JsonData);

                         if (points != null && points.Count > 0)
                         {
                             if (mapHolder != null)
                             {
                                 mapHolder.EnteredLocationsView.Visibility = ViewStates.Visible;
                                 mapHolder.EnteredLocationsView.Text = string.Format(
                                     context.Resources.GetString(Resource.String.ChosenLocations),
                                     points.Count,
                                     (points.Count > 1) ? "s" : "");
                             }

                             Items[position].IsCompleted = true;
                         }
                         else
                         {
                             if (mapHolder != null)
                            {
                                mapHolder.EnteredLocationsView.Visibility = ViewStates.Gone;
                            }

                            Items[position].IsCompleted = false;
                         }
                     }
                 }
                 else
                 {
                     vh = holder as TaskViewHolder;
                 }
             }

            // These apply to all task types:

            ImageService.Instance.LoadUrl(Items[position].TaskType.IconUrl).Into(((TaskViewHolder)holder).TaskTypeIcon);

            if (!string.IsNullOrWhiteSpace(Items[position].ImageUrl))
            {
                vh.TaskImage.Visibility = ViewStates.Visible;
                ImageService.Instance.LoadUrl(ServerUtils.GetUploadUrl(Items[position].ImageUrl))
                    .DownSampleInDip(300)
                    .Into(vh.TaskImage);
            }
            else
            {
                if (vh != null)
                {
                    vh.TaskImage.Visibility = ViewStates.Gone;
                }
            }

            vh.Description.Text = Items[position].Description;
            vh.Title.Text = Items[position].TaskType.DisplayName;

            bool hasChildren = Items[position].ChildTasks != null && Items[position].ChildTasks.Any();

            if (hasChildren && !Items[position].IsCompleted)
            {
                vh.LockedChildrenTease.Visibility = ViewStates.Visible;
                int childCount = Items[position].ChildTasks.Count();
                vh.LockedChildrenTease.Text = string.Format(
                    context.GetString(Resource.String.taskLockedParent),
                    childCount,
                    (childCount > 1)? "s" : "");
            }
            else if(hasChildren && Items[position].IsCompleted)
            {
                vh.LockedChildrenTease.Visibility = ViewStates.Visible;
                vh.LockedChildrenTease.Text = context.GetString(Resource.String.taskUnlockedParent);
            }
            else
            {
                vh.LockedChildrenTease.Visibility = ViewStates.Gone;
            }

            OnBind = false;
        }

        public void DeleteFile(int taskId, int fileIndex)
        {
            try
            {
                AppTask thisTask = GetTaskWithId(taskId);
                int taskIndex = GetIndexWithId(taskId);

                List<string> paths = JsonConvert.DeserializeObject<List<string>>(thisTask.CompletionData.JsonData);
                string deletePath = paths[fileIndex];

                FileInfo info = new FileInfo(deletePath);

                if (!info.Exists)
                {
                    throw new Exception("File doesn't exist");
                }

                Dictionary<string, string> properties = new Dictionary<string, string>
                {
                    {"TaskId", taskId.ToString() }
                };
                Analytics.TrackEvent("TaskAdapter_DeleteFile", properties);

                info.Delete();
                paths.RemoveAt(fileIndex);
                Items[taskIndex].CompletionData.JsonData = JsonConvert.SerializeObject(paths);

                if (paths.Count == 0)
                {
                    Items[taskIndex].IsCompleted = false;
                    CheckForChildren(taskIndex);
                }

                info.Refresh();
                Console.WriteLine("Deletion success = " + !info.Exists);

                NotifyItemChanged(taskIndex);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            // Inflate the CardView for the task:
            View itemView;

            switch (viewType)
            {
                case TaskScan:
                case TaskListen:
                case TaskLocationHunt:
                    itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.TaskCard_Btn, parent, false);
                    TaskViewHolderBtn bvh = new TaskViewHolderBtn(itemView, OnSpeakClick, OnClick);
                    return bvh;
                case TaskInfo:
                    itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.TaskCard_Info, parent, false);
                    TaskViewHolderInfo ivh = new TaskViewHolderInfo(itemView, OnSpeakClick, OnClick);
                    return ivh;
                case TaskDrawing:
                case TaskPhoto:
                    itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.TaskCard_ResultList, parent, false);
                    TaskViewHolderResultList pvh = new TaskViewHolderResultList(itemView, OnSpeakClick, OnClick, OnMediaClick);
                    return pvh;
                case TaskMultipleChoice:
                    itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.TaskCard_MultipleChoice, parent, false);
                    TaskViewHolderMultipleChoice vmh = new TaskViewHolderMultipleChoice(itemView, OnSpeakClick);
                    return vmh;
                case TaskLocationMarker:
                    itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.TaskCard_Map, parent, false);
                    TaskViewHolderMap mvh = new TaskViewHolderMap(itemView, OnSpeakClick, OnClick);
                    return mvh;
                case TaskTextEntry:
                    itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.TaskCard_TextEntry, parent, false);
                    TaskViewHolderTextEntry tvh = new TaskViewHolderTextEntry(itemView, OnSpeakClick, OnText);
                    return tvh;
                case TaskVideo:
                    itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.TaskCard_ResultList, parent, false);
                    TaskViewHolderRecordVideo vvh = new TaskViewHolderRecordVideo(itemView, OnSpeakClick, OnClick, OnMediaClick);
                    return vvh;
                case TaskAudio:
                    itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.TaskCard_ResultList, parent, false);
                    TaskViewHolderRecordAudio avh = new TaskViewHolderRecordAudio(itemView, OnSpeakClick, OnClick, OnMediaClick);
                    return avh;
                case Finish:
                    itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.TaskCard_Finish, parent, false);
                    ButtonViewHolder finishViewHolder = new ButtonViewHolder(itemView, OnClick);
                    return finishViewHolder;
                case Curate:
                    itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.CuratorCard, parent, false);
                    CuratorViewHolder curateCard = new CuratorViewHolder(itemView, OnCurate);
                    return curateCard;
                case Names:
                    itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.TaskCard_Name, parent, false);
                    TaskViewHolderName nameCard = new TaskViewHolderName(itemView, OnSpeakClick, OnChangeNameClick);
                    return nameCard;
                default:
                    itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.TaskCard, parent, false);
                    TaskViewHolder vh = new TaskViewHolder(itemView, null);
                    return vh;
            }
        }

        public override int GetItemViewType(int position)
        {
            if (Curator)
            {
                switch (position)
                {
                    case 0:
                        return Curate;
                    case 1:
                        return Names;
                }
            }

            if (position == 0)
            {
                return Names;
            }

            if (position >= Items.Count - 1)
            {
                return Finish;
            }

            return viewTypes.ContainsKey(Items[position].TaskType.IdName) ? viewTypes[Items[position].TaskType.IdName] : 0;
        }
    }

}