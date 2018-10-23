﻿using System;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Square.Picasso;
using Steepshot.Core.Models.Common;

namespace Steepshot.Utils.Media
{
    public class ImageProducer : Java.Lang.Object, IMediaProducer, ITarget
    {
        private readonly Context _context;
        private MediaModel _media;
        public event Action<WeakReference<Bitmap>> Draw;
        public event Action<ColorDrawable> PreDraw;

        public ImageProducer(Context context)
        {
            _context = context;
        }

        public virtual void Prepare(SurfaceTexture st, MediaModel media)
        {
            if (media == null)
                return;

            _media = media;

            Picasso.With(_context)
                .LoadWithProxy(media, Style.ScreenWidth)
                .Placeholder(new ColorDrawable(Style.R245G245B245))
                .NoFade()
                .Priority(Picasso.Priority.High)
                .Into(this);
        }

        public void OnBitmapFailed(Drawable p0)
        {
            Picasso.With(_context)
                .Load(_media.Thumbnails.Mini)
                .NoFade()
                .Priority(Picasso.Priority.High)
                .Into(this);
        }

        public void OnBitmapLoaded(Bitmap p0, Picasso.LoadedFrom p1)
        {
            var weakBmp = new WeakReference<Bitmap>(p0);
            Draw?.Invoke(weakBmp);
        }

        public void OnPrepareLoad(Drawable p0)
        {
            PreDraw?.Invoke((ColorDrawable)p0);
        }

        public virtual void Play()
        {
            Picasso.With(_context)
                .LoadWithProxy(_media, Style.ScreenWidth)
                .Placeholder(new ColorDrawable(Style.R245G245B245))
                .NoFade()
                .Priority(Picasso.Priority.High)
                .Into(this);
        }

        public virtual void Pause()
        {
        }

        public virtual void Stop()
        {
        }
    }
}
