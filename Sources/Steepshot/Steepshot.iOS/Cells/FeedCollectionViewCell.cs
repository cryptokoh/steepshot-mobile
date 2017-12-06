﻿using System;
using System.Linq;
using FFImageLoading;
using FFImageLoading.Work;
using Foundation;
using Steepshot.Core;
using Steepshot.Core.Models.Common;
using Steepshot.Core.Models.Responses;
using Steepshot.Core.Presenters;
using Steepshot.Core.Utils;
using Steepshot.iOS.Helpers;
using Steepshot.iOS.ViewControllers;
using UIKit;

namespace Steepshot.iOS.Cells
{
    public partial class FeedCollectionViewCell : BaseProfileCell
    {
        protected FeedCollectionViewCell(IntPtr handle) : base(handle)
        {

        }
        public static readonly NSString Key = new NSString(nameof(FeedCollectionViewCell));
        public static readonly UINib Nib;

        static FeedCollectionViewCell()
        {
            Nib = UINib.FromName(nameof(FeedCollectionViewCell), NSBundle.MainBundle);
        }

        private bool _isButtonBinded;
        public event VoteEventHandler<OperationResult<VoteResponse>> Voted;
        public event VoteEventHandler<OperationResult<VoteResponse>> Flagged;
        public event HeaderTappedHandler GoToProfile;
        public event HeaderTappedHandler GoToComments;
        public event HeaderTappedHandler GoToVoters;
        public event ImagePreviewHandler ImagePreview;
        private Post _currentPost;

        public bool IsVotedSet => Voted != null;
        public bool IsFlaggedSet => Flagged != null;
        public bool IsGoToProfileSet => GoToProfile != null;
        public bool IsGoToCommentsSet => GoToComments != null;
        public bool IsGoToVotersSet => GoToVoters != null;
        public bool IsImagePreviewSet => ImagePreview != null;
        private IScheduledWork _scheduledWorkAvatar;
        private IScheduledWork _scheduledWorkBody;

        public override void UpdateCell(Post post, NSMutableAttributedString comment)
        {
            _currentPost = post;
            avatarImage.Image = null;
            _scheduledWorkAvatar?.Cancel();

            bodyImage.Image = null;
            _scheduledWorkBody?.Cancel();

            _scheduledWorkAvatar = ImageService.Instance.LoadUrl(_currentPost.Avatar, TimeSpan.FromDays(30))
                                                     .WithCache(FFImageLoading.Cache.CacheType.All)
                                                     .Retry(2, 200)
                                                     .DownSample(width: 20)
                                                     .Into(avatarImage);

            var photo = _currentPost.Photos?.FirstOrDefault();
            if (photo != null)
                _scheduledWorkBody = ImageService.Instance.LoadUrl(photo, Steepshot.iOS.Helpers.Constants.ImageCacheDuration)
                                                         .WithCache(FFImageLoading.Cache.CacheType.All)
                                                         .Retry(2, 200)
                                                         .DownSample((int)UIScreen.MainScreen.Bounds.Width)
                                                         .Into(bodyImage);

            cellText.Text = _currentPost.Author;
            rewards.Hidden = !BasePresenter.User.IsNeedRewards;
            rewards.Text = BaseViewController.ToFormatedCurrencyString(_currentPost.TotalPayoutReward);

            netVotes.Text = $"{_currentPost.NetVotes} {Localization.Messages.Likes}";
            likeButton.Selected = _currentPost.Vote;
            flagButton.Selected = _currentPost.Flag;
            commentText.AttributedText = comment;
            var buttonTitle = _currentPost.Children == 0 ? Localization.Messages.PostFirstComment : string.Format(Localization.Messages.ViewComments, _currentPost.Children);
            viewCommentButton.SetTitle(buttonTitle, UIControlState.Normal);
            likeButton.Enabled = true;
            flagButton.Enabled = true;
            postTimeStamp.Text = _currentPost.Created.ToPostTime();

            imageWidth.Constant = UIScreen.MainScreen.Bounds.Width;
            imageHeight.Constant = PhotoHeight.Get(_currentPost.ImageSize);
            if (_currentPost.ImageSize.Width != 0)
                bodyImage.ContentMode = UIViewContentMode.ScaleAspectFill;
            else
                bodyImage.ContentMode = UIViewContentMode.ScaleAspectFit;

            if (!_isButtonBinded)
            {
                avatarImage.Layer.CornerRadius = avatarImage.Frame.Size.Width / 2;
                UITapGestureRecognizer tap = new UITapGestureRecognizer(() =>
                {
                    var photoUrl = _currentPost.Photos?.FirstOrDefault();
                    if (photoUrl != null)
                        ImagePreview(bodyImage.Image, photoUrl);
                });
                bodyImage.AddGestureRecognizer(tap);

                UITapGestureRecognizer imageTap = new UITapGestureRecognizer(() =>
                {
                    GoToProfile(_currentPost.Author);
                });
                UITapGestureRecognizer textTap = new UITapGestureRecognizer(() =>
                {
                    GoToProfile(_currentPost.Author);
                });
                UITapGestureRecognizer moneyTap = new UITapGestureRecognizer(() =>
                {
                    GoToProfile(_currentPost.Author);
                });
                avatarImage.AddGestureRecognizer(imageTap);
                cellText.AddGestureRecognizer(textTap);
                rewards.AddGestureRecognizer(moneyTap);

                UITapGestureRecognizer commentTap = new UITapGestureRecognizer(() =>
                {
                    GoToComments(_currentPost.Url);
                });
                commentView.AddGestureRecognizer(commentTap);

                UITapGestureRecognizer netVotesTap = new UITapGestureRecognizer(() =>
                {
                    GoToVoters(_currentPost.Url);
                });
                netVotes.AddGestureRecognizer(netVotesTap);

                flagButton.TouchDown += FlagButton_TouchDown;
                likeButton.TouchDown += LikeTap;
                _isButtonBinded = true;
            }
        }

        private void LikeTap(object sender, EventArgs e)
        {
            likeButton.Enabled = false;
            Voted(!likeButton.Selected, _currentPost, VotedAction);
        }

        private void VotedAction(Post post, OperationResult<VoteResponse> operationResult)
        {
            if (string.Equals(post.Url, _currentPost.Url, StringComparison.OrdinalIgnoreCase) && operationResult.Success)
            {
                likeButton.Selected = operationResult.Result.IsSuccess;
                flagButton.Selected = _currentPost.Flag;
                rewards.Text = BaseViewController.ToFormatedCurrencyString(operationResult.Result.NewTotalPayoutReward);
                netVotes.Text = $"{_currentPost.NetVotes.ToString()} {Localization.Messages.Likes}";
            }
            likeButton.Enabled = true;
        }

        private void FlagButton_TouchDown(object sender, EventArgs e)
        {
            flagButton.Enabled = false;
            Flagged?.Invoke(!flagButton.Selected, _currentPost, FlaggedAction);
        }

        private void FlaggedAction(Post post, OperationResult<VoteResponse> result)
        {
            if (result.Success && string.Equals(post.Url, _currentPost.Url, StringComparison.OrdinalIgnoreCase))
            {
                flagButton.Selected = result.Result.IsSuccess;
                likeButton.Selected = _currentPost.Vote;
                netVotes.Text = $"{_currentPost.NetVotes.ToString()} {Localization.Messages.Likes}";
                rewards.Text = BaseViewController.ToFormatedCurrencyString(result.Result.NewTotalPayoutReward);
            }
            flagButton.Selected = _currentPost.Flag;
            flagButton.Enabled = true;
        }
    }
}