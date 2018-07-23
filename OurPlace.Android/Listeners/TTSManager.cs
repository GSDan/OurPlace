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
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Speech.Tts;
using Android.Widget;
using static Android.Speech.Tts.TextToSpeech;

namespace OurPlace.Android.Listeners
{
    public class TTSManager : UtteranceProgressListener, IOnInitListener
    {
        private string currentSpeech;
        private string previousSpeech;
        private bool speaking = false;
        private TextToSpeech speaker;
        private Context context;
        private bool ready = false;

        public TTSManager(Context activity)
        {
            context = activity;
            speaker = new TextToSpeech(context, this);
            speaker.SetOnUtteranceProgressListener(this);
        }

        private void Speak()
        {
            if (!ready) return;

            if (currentSpeech == previousSpeech && speaking)
            {
                speaker.Stop();
                speaking = false;
            }
            else
            {
                // Because Android is stupid and breaking changes are great fun
                if (Build.VERSION.SdkInt >= BuildVersionCodes.Lollipop)
                {
                    speaker.Speak(new Java.Lang.String(currentSpeech), QueueMode.Flush, null, currentSpeech);
                }
                else
                {
#pragma warning disable CS0618 // Type or member is obsolete
                    speaker.Speak(currentSpeech, QueueMode.Flush, null);
#pragma warning restore CS0618 // Type or member is obsolete
                }
                speaking = true;
            }
        }

        public void ReadText(string given)
        {
            previousSpeech = currentSpeech;
            currentSpeech = given;

            if (ready)
            {
                // Speaker is ready, play immediately
                Speak();
            }
        }

        public override void OnDone(string utteranceId)
        {
            speaking = false;
        }

        public override void OnError(string utteranceId)
        {
            speaking = false;
            Toast.MakeText(context, Resource.String.ttsError, ToastLength.Long).Show();
        }

        public override void OnStart(string utteranceId)
        {
            speaking = true;
        }

        public void OnInit([GeneratedEnum] OperationResult status)
        {
            ready = true;
            if(!string.IsNullOrWhiteSpace(currentSpeech))
            {
                Speak();
            }
        }

        public void Clean()
        {
            if(speaker != null)
            {
                if (speaking) speaker.Stop();
                speaker.Shutdown();
            }
        }
    }
}