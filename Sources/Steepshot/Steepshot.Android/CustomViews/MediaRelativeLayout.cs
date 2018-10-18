using System;
using Android.Content;
using Android.Runtime;
using Android.Support.Design.Widget;
using Android.Util;
using Android.Widget;

namespace Steepshot.CustomViews
{
    public class MediaRelativeLayout : RelativeLayout
    {
        private MediaViewPager _mediaViewPager;
        private ImageView _mediaTypeIco;
        private TabLayout _tab;

        #region Constructors

        protected MediaRelativeLayout(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
        }

        public MediaRelativeLayout(Context context) : this(context, null)
        {
        }

        public MediaRelativeLayout(Context context, IAttributeSet attrs) : this(context, attrs, 0)
        {
        }

        public MediaRelativeLayout(Context context, IAttributeSet attrs, int defStyleAttr) : this(context, attrs, defStyleAttr, 0)
        {
        }

        public MediaRelativeLayout(Context context, IAttributeSet attrs, int defStyleAttr, int defStyleRes) : base(context, attrs, defStyleAttr, defStyleRes)
        {
            //Inflate(context, Resource.Layout.MediaRelativeLayout, this);
        }

        #endregion

        protected override void OnFinishInflate()
        {
            base.OnFinishInflate();

            _mediaViewPager = FindViewById<MediaViewPager>(Resource.Id.media_view_pager);
            _mediaTypeIco = FindViewById<ImageView>(Resource.Id.media_type_image);
            _tab = FindViewById<TabLayout>(Resource.Id.media_tab_layout);
        }
    }
}