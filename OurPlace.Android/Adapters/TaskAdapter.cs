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
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using FFImageLoading;
using Newtonsoft.Json;
using OurPlace.Common;
using OurPlace.Common.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace OurPlace.Android.Adapters
{
    public class TaskAdapter : RecyclerView.Adapter
    {
        const int TASK = 0;
        const int TASK_PHOTO = 1;
        const int TASK_MULTIPLECHOICE = 2;
        const int TASK_DRAWING = 3;
        const int TASK_TEXTENTRY = 4;
        const int TASK_VIDEO = 5;
        const int TASK_AUDIO = 6;
        const int TASK_LOCATIONHUNT = 7;
        const int TASK_LOCATIONMARKER = 8;
        const int TASK_INFO = 9;
        const int TASK_LISTEN = 10;
        const int FINISH = 11;
        const int CURATE = 12;
        const int NAMES = 13;

        public List<AppTask> items;
        private readonly Dictionary<int, List<AppTask>> hiddenChildren;
        private readonly Dictionary<string, int> viewTypes;
        public event EventHandler<int> ItemClick;
        public event EventHandler<int> TextEntered;
        public event EventHandler<bool> Approved;
        public event EventHandler<int> SpeakText;
        public event EventHandler<int> ChangeName;
        public Action<int, int> ShowMedia;
        public bool onBind = false;
        public bool curator;
        private bool reqName;
        private string description;
        private string names;
        Activity context;

        public TaskAdapter(Activity context, List<AppTask> data, string description, bool curator, bool reqName) : base()
        {
            this.context = context;
            this.curator = curator;
            this.description = description;
            this.reqName = reqName;
            items = data;

            if (items == null) items = new List<AppTask>();

            hiddenChildren = new Dictionary<int, List<AppTask>>();
            AppTask[] temp = data.ToArray();
            foreach (AppTask parent in temp)
            {
                if (parent.TaskType.IdName == "INFO") parent.IsCompleted = true;

                if (parent.ChildAppTasks?.Count > 0)
                {
                    hiddenChildren.Add(parent.Id, parent.ChildAppTasks);
                    CheckForChildren(items.IndexOf(parent));
                }
            }

            // Add curator controls to the start of the list
            if (curator)
            {
                items.Insert(0, null);
            }

            items.Insert(0, null);
            items.Add(null);

            viewTypes = new Dictionary<string, int>
            {
                {"TAKE_PHOTO", TASK_PHOTO},
                {"MATCH_PHOTO", TASK_PHOTO},
                {"MULT_CHOICE", TASK_MULTIPLECHOICE},
                {"DRAW", TASK_DRAWING },
                {"DRAW_PHOTO", TASK_DRAWING },
                {"ENTER_TEXT", TASK_TEXTENTRY },
                {"TAKE_VIDEO", TASK_VIDEO },
                {"REC_AUDIO", TASK_AUDIO },
                {"LOC_HUNT", TASK_LOCATIONHUNT },
                {"MAP_MARK", TASK_LOCATIONMARKER },
                {"INFO", TASK_INFO },
                {"LISTEN_AUDIO", TASK_LISTEN }
            };
        }

        public override int ItemCount
        {
            get
            {
                return items.Count;
            }
        }

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
            items[position].IsCompleted = true;
            CheckForChildren(position);
        }

        private void OnMediaClick(int taskIndex, int pathIndex)
        {
            ShowMedia?.Invoke(taskIndex, pathIndex);
            items[taskIndex].IsCompleted = true;
            CheckForChildren(taskIndex);
        }

        private void OnChangeNameClick()
        {
            ChangeName?.Invoke(this, -1);
        }

        public AppTask GetTaskWithId(int id)
        {
            return items.FirstOrDefault(t => t != null && t.Id == id);
        }

        public int GetIndexWithId(int id)
        {
            return items.IndexOf(items.FirstOrDefault(t => t != null && t.Id == id));
        }

        public bool IsPositionAChildView(int position)
        {
            if (position >= items.Count || position <= 0 || (curator && position == 1) || position == items.Count - 1)
            {
                // Can't be a child
                return false;
            }

            return items[position].IsChild;
        }

        /// <summary>
        /// Called when a task has returned new JSON data
        /// sucessfully.
        /// </summary>
        /// <param name="taskId">The task's Id</param>
        /// <param name="json">New JSON data</param>
        public void OnFileReturned(int taskId, string json, bool possibleSourceImage)
        {
            int i = GetIndexWithId(taskId);

            AppTask thisTask = GetTaskWithId(taskId);

            List<string> files = JsonConvert.DeserializeObject<List<string>>(items[i].CompletionData.JsonData);
            if (files == null) files = new List<string>();

            files.Add(json);

            items[i].IsCompleted = true;
            items[i].CompletionData.JsonData = JsonConvert.SerializeObject(files);

            if (possibleSourceImage)
            {
                // Check if any other tasks are waiting for this photo
                NotifyDataSetChanged();
            }

            ImageService.Instance.InvalidateCacheEntryAsync(json, FFImageLoading.Cache.CacheType.All, true);
            NotifyItemChanged(i);

            // Tell the Finished button to update, just in case this is
            // the last unfinished task.
            NotifyItemChanged(items.Count - 1);

            CheckForChildren(i);
        }

        private void CheckForChildren(int position)
        {
            AppTask parent = items[position];
            bool hasChildren = parent.ChildTasks != null && parent.ChildTasks.Count() > 0;

            if (hasChildren && !parent.IsCompleted)
            {
                if (!hiddenChildren.ContainsKey(parent.Id))
                {
                    // Parent task is no longer complete, hide children
                    hiddenChildren[parent.Id] = new List<AppTask>();
                    foreach(LearningTask child in parent.ChildTasks)
                    {
                        AppTask childProgress = GetTaskWithId(child.Id);
                        if(childProgress != null)
                        {
                            hiddenChildren[parent.Id].Add(childProgress);
                            items.Remove(childProgress);
                        }
                    }
                    NotifyDataSetChanged();
                }
            }
            else if (hasChildren && items[position].IsCompleted)
            {
                // Show the child tasks if they're hidden
                if (hiddenChildren.ContainsKey(items[position].Id))
                {
                    var children = hiddenChildren[items[position].Id];
                    int nextPos = position + 1;
                    foreach(AppTask child in children)
                    {
                        if(!items.Exists(t => t?.Id == child.Id))
                        {
                            items.Insert(nextPos++, child);
                        }
                    }
                    hiddenChildren.Remove(items[position].Id);
                    NotifyDataSetChanged();
                }
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
            int ind = items.IndexOf(thisTask);
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
            onBind = true;

            if (curator)
            {
                if (position == 0)
                    return;
            }

            if (position == 0 || (curator && position == 1))
            {
                // Activity description + entered names (if required)
                vh = holder as TaskViewHolder_Name;
                vh.Title.Text = description;
                vh.Description.Text = context.Resources.GetString(Resource.String.TasksTitle);

                ((TaskViewHolder_Name)vh).NameSection.Visibility = (reqName) ? ViewStates.Visible : ViewStates.Gone;
                ((TaskViewHolder_Name)vh).EnteredNames.Text = names;
                return;
            }

            if (position == items.Count - 1)
            {
                // Finish button
                ButtonViewHolder bvh = holder as ButtonViewHolder;
                bvh.Button.Enabled = true;
                return;
            }

            string taskType = items[position].TaskType.IdName;
             if (thisType == typeof(TaskViewHolder_Info))
            {
                AdditionalInfoData taskInfo = JsonConvert.DeserializeObject<AdditionalInfoData>(items[position].JsonData);
                vh = holder as TaskViewHolder_Info;
                items[position].IsCompleted = true;
                if (!string.IsNullOrWhiteSpace(taskInfo.ImageUrl))
                {
                    ((TaskViewHolder_Info)vh).Image.Visibility = ViewStates.Visible;
                    ((TaskViewHolder_Info)vh).Description.SetPadding(16, 16, 16, 16);
                    ImageService.Instance.LoadUrl(ServerUtils.GetUploadUrl(taskInfo.ImageUrl))
                        .DownSampleInDip(300)
                        .Into(((TaskViewHolder_Info)vh).Image);
                }
                else
                {
                    ((TaskViewHolder_Info)vh).Image.Visibility = ViewStates.Gone;
                    ((TaskViewHolder_Info)vh).Description.SetPadding(16, 16, 150, 16); //make room for TTS button
                }

                ((TaskViewHolder_Info)vh).Button.Visibility =
                    (string.IsNullOrWhiteSpace(taskInfo.ExternalUrl)) ? ViewStates.Gone : ViewStates.Visible;
            }
            else if (thisType == typeof(TaskViewHolder_RecordAudio))
            {
                vh = holder as TaskViewHolder_RecordAudio;
                ((TaskViewHolder_RecordAudio)vh).StartTaskButton.Text = context.Resources.GetString(Resource.String.StartBtn);
                ImageService.Instance.LoadUrl(items[position].TaskType.IconUrl).Into(((TaskViewHolder_RecordAudio)vh).TaskImage);

                if (!string.IsNullOrWhiteSpace(items[position].CompletionData.JsonData))
                {
                    List<string> audioPaths = JsonConvert.DeserializeObject<List<string>>(items[position].CompletionData.JsonData);

                    ((TaskViewHolder_RecordAudio)vh).ShowResults(audioPaths, context);
                    items[position].IsCompleted = true;
                }
                else
                {
                    items[position].IsCompleted = false;
                    ((TaskViewHolder_RecordAudio)vh).ClearResults();
                }
            }
            else if (thisType == typeof(TaskViewHolder_RecordVideo))
            {
                vh = holder as TaskViewHolder_RecordVideo;
                ((TaskViewHolder_RecordVideo)vh).StartTaskButton.Text = context.Resources.GetString(Resource.String.RecBtn);
                ImageService.Instance.LoadUrl(items[position].TaskType.IconUrl).Into(((TaskViewHolder_RecordVideo)vh).TaskImage);

                if (!string.IsNullOrWhiteSpace(items[position].CompletionData.JsonData))
                {
                    List<string> videoPaths = JsonConvert.DeserializeObject<List<string>>(items[position].CompletionData.JsonData);

                    ((TaskViewHolder_RecordVideo)vh).ShowResults(videoPaths, context);
                    items[position].IsCompleted = true;
                }
                else
                {
                    items[position].IsCompleted = false;
                    ((TaskViewHolder_RecordVideo)vh).ClearResults();
                }
            }
            else if (thisType == typeof(TaskViewHolder_ResultList))
            {
                vh = holder as TaskViewHolder_ResultList;

                bool btnEnabled = true;
                string btnText = context.Resources.GetString(Resource.String.TaskBtn);

                if (taskType == "DRAW" || taskType == "DRAW_PHOTO")
                {
                    btnText = context.Resources.GetString(Resource.String.StartDrawBtn);

                    if (taskType == "DRAW_PHOTO")
                    {
                        int idResult = -1;
                        if (int.TryParse(items[position].JsonData, out idResult))
                        {
                            btnEnabled = GetTaskWithId(idResult).IsCompleted;
                        }
                        else
                        {
                            btnEnabled = true;
                        }

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

                ((TaskViewHolder_ResultList)vh).StartTaskButton.Text = btnText;
                ((TaskViewHolder_ResultList)vh).StartTaskButton.Enabled = btnEnabled;

                ImageService.Instance.LoadUrl(items[position].TaskType.IconUrl).Into(((TaskViewHolder_ResultList)vh).TaskImage);
                
                if (!string.IsNullOrWhiteSpace(items[position].CompletionData.JsonData))
                {
                    List<string> photoPaths = JsonConvert.DeserializeObject<List<string>>(items[position].CompletionData.JsonData);

                    ((TaskViewHolder_ResultList)vh).ShowResults(photoPaths, context);
                    items[position].IsCompleted = true;
                }
                else
                {
                    items[position].IsCompleted = false;
                    ((TaskViewHolder_ResultList)vh).ClearResults();
                }
            }
            else if (thisType == typeof(TaskViewHolder_Btn))
            {
                vh = holder as TaskViewHolder_Btn;
                ImageService.Instance.LoadUrl(items[position].TaskType.IconUrl).Into(((TaskViewHolder_Btn)vh).Image);
                string btnText = context.Resources.GetString(Resource.String.TaskBtn);

                if (taskType == "LISTEN_AUDIO")
                {
                    btnText = context.Resources.GetString(Resource.String.ListenBtn);
                }

                ((TaskViewHolder_Btn)vh).Button.Text = btnText;
            }
            else if (thisType == typeof(TaskViewHolder_TextEntry))
            {
                vh = holder as TaskViewHolder_TextEntry;
                ((TaskViewHolder_TextEntry)vh).TextField.Text = items[position].CompletionData.JsonData;
                items[position].IsCompleted = !string.IsNullOrWhiteSpace(((TaskViewHolder_TextEntry)vh).TextField.Text);
                ImageService.Instance.LoadUrl(items[position].TaskType.IconUrl).Into(((TaskViewHolder_TextEntry)vh).Image);
            }
            else if (thisType == typeof(TaskViewHolder_MultipleChoice))
            {
                vh = holder as TaskViewHolder_MultipleChoice;
                RadioGroup radios = ((TaskViewHolder_MultipleChoice)vh).RadioGroup;

                string[] choices = JsonConvert.DeserializeObject<string[]>(items[position].JsonData);
                int answeredInd = -1;
                int.TryParse(items[position].CompletionData.JsonData, out answeredInd);

                if (radios.ChildCount == 0)
                {
                    int index = 0;
                    foreach (string option in choices)
                    {
                        RadioButton rad = new RadioButton(context);
                        rad.Text = option;
                        rad.SetPadding(0, 0, 0, 5);
                        rad.TextSize = 16;
                        radios.AddView(rad);

                        if (items[position].IsCompleted && answeredInd == index)
                        {
                            ((RadioButton)radios.GetChildAt(answeredInd)).Checked = true;
                        }
                        index++;
                    }
                }

                if (answeredInd == -1)
                {
                    items[position].IsCompleted = false;
                }

                radios.CheckedChange += (object sender, RadioGroup.CheckedChangeEventArgs e) =>
                {
                    items[position].IsCompleted = true;
                    int radioButtonID = radios.CheckedRadioButtonId;
                    View radioButton = radios.FindViewById(radioButtonID);
                    int idx = radios.IndexOfChild(radioButton);
                    items[position].CompletionData.JsonData = idx.ToString();
                    NotifyItemChanged(items.Count - 1);
                };

                ImageService.Instance.LoadUrl(items[position].TaskType.IconUrl).Into(((TaskViewHolder_MultipleChoice)vh).Image);
            }
            else if (thisType == typeof(TaskViewHolder_Map))
            {
                vh = holder as TaskViewHolder_Map;
                TaskViewHolder_Map mapHolder = ((TaskViewHolder_Map)vh);
                ImageService.Instance.LoadUrl(items[position].TaskType.IconUrl).Into(((TaskViewHolder_Map)vh).Image);

                if (taskType == "MAP_MARK")
                {
                    List<Map_Location> points = JsonConvert.DeserializeObject<List<Map_Location>>(items[position].CompletionData.JsonData);

                    if (points != null && points.Count > 0)
                    {
                        mapHolder.EnteredLocsTextView.Visibility = ViewStates.Visible;
                        mapHolder.EnteredLocsTextView.Text = string.Format(
                            context.Resources.GetString(Resource.String.ChosenLocations), 
                            points.Count, 
                            (points.Count > 1) ? "s" : "");
                        items[position].IsCompleted = true;
                    }
                    else
                    {
                        mapHolder.EnteredLocsTextView.Visibility = ViewStates.Gone;
                        items[position].IsCompleted = false;
                    }
                }
            }
            else
            {
                vh = holder as TaskViewHolder;
            }

            vh.Description.Text = items[position].Description;
            vh.Title.Text = items[position].TaskType.DisplayName;

            bool hasChildren = items[position].ChildTasks != null && items[position].ChildTasks.Count() > 0;

            if (hasChildren && !items[position].IsCompleted)
            {
                vh.LockedChildrenTease.Visibility = ViewStates.Visible;
                int childCount = items[position].ChildTasks.Count();
                vh.LockedChildrenTease.Text = string.Format(
                    context.GetString(Resource.String.taskLockedParent),
                    childCount,
                    (childCount > 1)? "s" : "");
            }
            else if(hasChildren && items[position].IsCompleted)
            {
                vh.LockedChildrenTease.Visibility = ViewStates.Visible;
                vh.LockedChildrenTease.Text = context.GetString(Resource.String.taskUnlockedParent);
            }
            else
            {
                vh.LockedChildrenTease.Visibility = ViewStates.Gone;
            }

            onBind = false;
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

                Console.WriteLine("Deleting file " + info.FullName);

                info.Delete();
                paths.RemoveAt(fileIndex);
                items[taskIndex].CompletionData.JsonData = JsonConvert.SerializeObject(paths);

                if (paths.Count == 0)
                {
                    items[taskIndex].IsCompleted = false;
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

        private void HandlePhotoDeletion(int taskIndex, int photoIndex)
        {
            new AlertDialog.Builder(context)
                .SetTitle(Resource.String.deleteTitle)
                .SetMessage(Resource.String.deleteMessage)
                .SetNegativeButton(Resource.String.dialog_cancel, (a, e) => { })
                .SetPositiveButton(Resource.String.DeleteBtn, (a, e) =>
                {
                    try
                    {
                        List<string> paths = JsonConvert.DeserializeObject<List<string>>(items[taskIndex].CompletionData.JsonData);
                        string deletePath = paths[photoIndex];

                        FileInfo info = new FileInfo(deletePath);

                        if (info.Exists)
                        {
                            info.Delete();
                        }

                        paths.RemoveAt(photoIndex);
                        items[taskIndex].CompletionData.JsonData = JsonConvert.SerializeObject(paths);

                        if (paths.Count == 0)
                        {
                            items[taskIndex].IsCompleted = false;
                            CheckForChildren(taskIndex);
                        }

                        NotifyItemChanged(taskIndex);
                    }
                    catch (Exception ex)
                    {
                        Toast.MakeText(context, "Deletion error: " + ex.Message, ToastLength.Long).Show();
                    }
                })
                .Show();
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            // Inflate the CardView for the task:
            View itemView = null;

            switch (viewType)
            {
                case TASK_LOCATIONHUNT:
                case TASK_LISTEN:
                    itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.TaskCard_Btn, parent, false);
                    TaskViewHolder_Btn bvh = new TaskViewHolder_Btn(itemView, OnSpeakClick, OnClick);
                    return bvh;
                case TASK_INFO:
                    itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.TaskCard_Info, parent, false);
                    TaskViewHolder_Info ivh = new TaskViewHolder_Info(itemView, OnSpeakClick, OnClick);
                    return ivh;
                case TASK_DRAWING:
                case TASK_PHOTO:
                    itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.TaskCard_ResultList, parent, false);
                    TaskViewHolder_ResultList pvh = new TaskViewHolder_ResultList(itemView, OnSpeakClick, OnClick, OnMediaClick);
                    return pvh;
                case TASK_MULTIPLECHOICE:
                    itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.TaskCard_MultipleChoice, parent, false);
                    TaskViewHolder_MultipleChoice vmh = new TaskViewHolder_MultipleChoice(itemView, OnSpeakClick);
                    return vmh;
                case TASK_LOCATIONMARKER:
                    itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.TaskCard_Map, parent, false);
                    TaskViewHolder_Map mvh = new TaskViewHolder_Map(itemView, OnSpeakClick, OnClick);
                    return mvh;
                case TASK_TEXTENTRY:
                    itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.TaskCard_TextEntry, parent, false);
                    TaskViewHolder_TextEntry tvh = new TaskViewHolder_TextEntry(itemView, OnSpeakClick, OnText);
                    return tvh;
                case TASK_VIDEO:
                    itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.TaskCard_ResultList, parent, false);
                    TaskViewHolder_RecordVideo vvh = new TaskViewHolder_RecordVideo(itemView, OnSpeakClick, OnClick, OnMediaClick);
                    return vvh;
                case TASK_AUDIO:
                    itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.TaskCard_ResultList, parent, false);
                    TaskViewHolder_RecordAudio avh = new TaskViewHolder_RecordAudio(itemView, OnSpeakClick, OnClick, OnMediaClick);
                    return avh;
                case FINISH:
                    itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.TaskCard_Finish, parent, false);
                    ButtonViewHolder fbvh = new ButtonViewHolder(itemView, OnClick);
                    return fbvh;
                case CURATE:
                    itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.CuratorCard, parent, false);
                    CuratorViewHolder curateCard = new CuratorViewHolder(itemView, OnCurate);
                    return curateCard;
                case NAMES:
                    itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.TaskCard_Name, parent, false);
                    TaskViewHolder_Name nameCard = new TaskViewHolder_Name(itemView, OnSpeakClick, OnChangeNameClick);
                    return nameCard;
                default:
                    itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.TaskCard, parent, false);
                    TaskViewHolder vh = new TaskViewHolder(itemView, null);
                    return vh;
            }
        }

        public override int GetItemViewType(int position)
        {
            if (curator)
            {
                if (position == 0) return CURATE;
                if (position == 1) return NAMES;
            }

            if (position == 0) return NAMES;
            if (position >= items.Count - 1) return FINISH;

            if (viewTypes.ContainsKey(items[position].TaskType.IdName))
                return viewTypes[items[position].TaskType.IdName];
            return 0; // Default to base layout (All types will populate this correctly)
        }
    }

}