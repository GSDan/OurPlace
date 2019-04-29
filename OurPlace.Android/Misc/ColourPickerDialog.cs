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
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Views;
using System;

// http://stackoverflow.com/questions/16650419/draw-in-canvas-by-finger-android

namespace OurPlace.Android
{
    public interface OnColorChangedListener
    {
        void colorChanged(int colour);
    }

    public class ColourPickerDialog : Dialog, OnColorChangedListener
    {
        private OnColorChangedListener mListener;
        private int mInitialColor;

        public ColourPickerDialog(Context context, OnColorChangedListener listener, int initialColor) : base(context)
        {
            mListener = listener;
            mInitialColor = initialColor;
        }

        public class ColorPickerView : View
        {
            private Paint mPaint;
            private Paint mCenterPaint;
            private int[] mColors;
            private OnColorChangedListener mListener;
            private bool mTrackingCenter;
            private bool mHighlightCenter;

            public ColorPickerView(Context c, OnColorChangedListener l, int color) : base(c)
            {
                mListener = l;
                mColors = new int[] {
                    Color.White.ToArgb(),
                    Color.Black.ToArgb(),
                    Color.Red.ToArgb(),
                    Color.Yellow.ToArgb(),
                    Color.Pink.ToArgb(),
                    Color.Green.ToArgb(),
                    Color.Purple.ToArgb(),
                    Color.Blue.ToArgb()
                };
                Shader s = new SweepGradient(0, 0, mColors, null);

                mPaint = new Paint(PaintFlags.AntiAlias);
                mPaint.SetShader(s);
                mPaint.SetStyle(Paint.Style.Stroke);
                mPaint.StrokeWidth = 32;

                mCenterPaint = new Paint(PaintFlags.AntiAlias);
                mCenterPaint.Color = new Color(color);
                mCenterPaint.StrokeWidth = 5;
            }

            protected override void OnDraw(Canvas canvas)
            {
                float r = CENTER_X - mPaint.StrokeWidth * 0.5f;

                canvas.Translate(CENTER_X, CENTER_X);

                canvas.DrawOval(new RectF(-r, -r, r, r), mPaint);
                canvas.DrawCircle(0, 0, CENTER_RADIUS, mCenterPaint);

                if (mTrackingCenter)
                {
                    int c = mCenterPaint.Color;
                    mCenterPaint.SetStyle(Paint.Style.Stroke);

                    if (mHighlightCenter)
                    {
                        mCenterPaint.Alpha = 0xFF;
                    }
                    else
                    {
                        mCenterPaint.Alpha = 0x80;
                    }
                    canvas.DrawCircle(0, 0,
                            CENTER_RADIUS + mCenterPaint.StrokeWidth,
                            mCenterPaint);

                    mCenterPaint.SetStyle(Paint.Style.Fill);
                    mCenterPaint.Color = new Color(c);
                }
            }

            protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
            {
                base.OnMeasure(widthMeasureSpec, heightMeasureSpec);
                SetMeasuredDimension(CENTER_X * 2, CENTER_Y * 2);
            }

            private int CENTER_X = 400;
            private int CENTER_Y = 400;
            private int CENTER_RADIUS = 128;

            private int floatToByte(float x)
            {
                int n = (int)Math.Round(x);
                return n;
            }

            private int pinToByte(int n)
            {
                if (n < 0)
                {
                    n = 0;
                }
                else if (n > 255)
                {
                    n = 255;
                }
                return n;
            }

            private int ave(int s, int d, float p)
            {
                return s + (int)Math.Round(p * (d - s));
            }

            private int interpColor(int[] colors, float unit)
            {
                if (unit <= 0)
                {
                    return colors[0];
                }
                if (unit >= 1)
                {
                    return colors[colors.Length - 1];
                }

                float p = unit * (colors.Length - 1);
                int i = (int)p;
                p -= i;

                // now p is just the fractional part [0...1) and i is the index
                int c0 = colors[i];
                int c1 = colors[i + 1];
                int a = ave(Color.GetAlphaComponent(c0), Color.GetAlphaComponent(c1), p);
                int r = ave(Color.GetRedComponent(c0), Color.GetRedComponent(c1), p);
                int g = ave(Color.GetGreenComponent(c0), Color.GetGreenComponent(c1), p);
                int b = ave(Color.GetBlueComponent(c0), Color.GetBlueComponent(c1), p);

                return Color.Argb(a, r, g, b);
            }

            private int rotateColor(int color, float rad)
            {
                float deg = rad * 180 / (float)Math.PI;
                int r = Color.GetRedComponent(color);
                int g = Color.GetGreenComponent(color);
                int b = Color.GetBlueComponent(color);

                ColorMatrix cm = new ColorMatrix();
                ColorMatrix tmp = new ColorMatrix();

                cm.SetRGB2YUV();
                tmp.SetRotate(0, deg);
                cm.PostConcat(tmp);
                tmp.SetYUV2RGB();
                cm.PostConcat(tmp);

                float[] a = cm.GetArray();

                int ir = floatToByte(a[0] * r + a[1] * g + a[2] * b);
                int ig = floatToByte(a[5] * r + a[6] * g + a[7] * b);
                int ib = floatToByte(a[10] * r + a[11] * g + a[12] * b);

                return Color.Argb(Color.GetAlphaComponent(color), pinToByte(ir), pinToByte(ig), pinToByte(ib));
            }

            public override bool OnTouchEvent(MotionEvent e)
            {
                float x = e.GetX() - CENTER_X;
                float y = e.GetY() - CENTER_Y;
                bool inCenter = Math.Sqrt(x * x + y * y) <= CENTER_RADIUS;

                switch (e.Action)
                {
                    case MotionEventActions.Down:
                        mTrackingCenter = inCenter;
                        if (inCenter)
                        {
                            mHighlightCenter = true;
                            Invalidate();
                        }
                        break;

                    case MotionEventActions.Move:
                        if (mTrackingCenter)
                        {
                            if (mHighlightCenter != inCenter)
                            {
                                mHighlightCenter = inCenter;
                                Invalidate();
                            }
                        }
                        else
                        {
                            float angle = (float)Math.Atan2(y, x);
                            // need to turn angle [-PI ... PI] into unit [0....1]
                            float unit = angle / (2 * (float)Math.PI);
                            if (unit < 0)
                            {
                                unit += 1;
                            }
                            mCenterPaint.Color = new Color(interpColor(mColors, unit));
                            Invalidate();
                        }
                        break;

                    case MotionEventActions.Up:
                        if (mTrackingCenter)
                        {
                            if (inCenter)
                            {
                                mListener.colorChanged(mCenterPaint.Color);
                            }
                            mTrackingCenter = false;    // so we draw w/o halo
                            Invalidate();
                        }
                        break;
                }
                return true;
            }
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(new ColorPickerView(Context, this, mInitialColor));
            SetTitle("Pick a Color");
        }

        public void colorChanged(int colour)
        {
            mListener.colorChanged(colour);
            Dismiss();
        }
    }
}