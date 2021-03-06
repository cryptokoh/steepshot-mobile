﻿using System;
using CoreGraphics;
using Foundation;
using PureLayout.Net;
using Steepshot.Core.Localization;
using Steepshot.Core.Models.Common;
using Steepshot.Core.Models.Enums;
using Steepshot.Core.Presenters;
using Steepshot.Core.Utils;
using Steepshot.iOS.CustomViews;
using Steepshot.iOS.Helpers;
using Steepshot.iOS.ViewControllers;
using Steepshot.Core.Extensions;
using UIKit;
using Steepshot.Core.Models.Requests;
using System.Globalization;

namespace Steepshot.iOS.Views
{
    public class PowerManipulationViewController : BaseViewController
    {
        private readonly WalletPresenter _walletPresenter;
        private readonly PowerAction _powerAction;
        private double _powerAmount;
        private SearchTextField _amountTextField;
        private UILabel _firstTokenText = new UILabel();
        private UILabel _secondTokenText = new UILabel();
        private UIActivityIndicatorView _loader = new UIActivityIndicatorView();
        private UIButton _actionButton = new UIButton();
        private UILabel errorMessage;

        public PowerManipulationViewController(WalletPresenter presenter, PowerAction action)
        {
            _walletPresenter = presenter;
            _powerAction = action;
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            SetBackButton();
            CreateView();
            AmountEditOnTextChanged(null, null);
        }

        private void AmountEditOnTextChanged(object sender, EventArgs e)
        {
            var spAvailiable = _walletPresenter.Balances[0].EffectiveSp - _walletPresenter.Balances[0].DelegatedToMe;
            var amountEdit = _amountTextField.GetDoubleValue();
            var amountAvailable = _walletPresenter.Balances[0].Value;
            errorMessage.Hidden = true;

            if (amountEdit <= (_powerAction == PowerAction.PowerUp ? amountAvailable : spAvailiable))
            {
                switch (_powerAction)
                {
                    case PowerAction.PowerUp:
                        UpdateTokenValues(_walletPresenter.Balances[0].Value.ToBalanceValueString(), (amountAvailable - amountEdit).ToBalanceValueString(), _walletPresenter.Balances[0].EffectiveSp.ToBalanceValueString(), (_walletPresenter.Balances[0].EffectiveSp + amountEdit).ToBalanceValueString());
                        break;
                    case PowerAction.PowerDown:
                        UpdateTokenValues(_walletPresenter.Balances[0].Value.ToBalanceValueString(), (amountAvailable + amountEdit).ToBalanceValueString(), spAvailiable.ToBalanceValueString(), (spAvailiable - amountEdit).ToBalanceValueString());
                        break;
                }
                _powerAmount = amountEdit;
            }
            else
            {
                UpdateTokenValues(_walletPresenter.Balances[0].Value.ToBalanceValueString(), AppSettings.LocalizationManager.GetText(LocalizationKeys.AmountLimit), _walletPresenter.Balances[0].EffectiveSp.ToBalanceValueString(), AppSettings.LocalizationManager.GetText(LocalizationKeys.AmountLimit));
                _powerAmount = -1;
                errorMessage.Hidden = false;
            }
        }

        private void UpdateTokenValues(string currTokenOne, string nextTokenOne, string currTokenTwo, string nextTokenTwo)
        {
            var at = new NSMutableAttributedString();
            at.Append(new NSAttributedString($"{currTokenOne} >", Constants.PowerManipulationTextStyle));
            at.Append(new NSAttributedString($" {nextTokenOne}", Constants.PowerManipulatioSelectedTextStyle));
            _firstTokenText.AttributedText = at;

            var at2 = new NSMutableAttributedString();
            at2.Append(new NSAttributedString($"{currTokenTwo} >", Constants.PowerManipulationTextStyle));
            at2.Append(new NSAttributedString($" {nextTokenTwo}", Constants.PowerManipulatioSelectedTextStyle));
            _secondTokenText.AttributedText = at2;
        }

        private void MaxBtnOnClick(object sender, EventArgs e)
        {
            if (_powerAction == PowerAction.PowerDown)
                ShowAlert(LocalizationKeys.MinSP);

            var maxPowerDown = _walletPresenter.Balances[0].EffectiveSp - _walletPresenter.Balances[0].DelegatedToMe - 3;
            maxPowerDown = maxPowerDown < 0 ? 0 : maxPowerDown;

            _amountTextField.Text = _powerAction == PowerAction.PowerUp ? _walletPresenter.Balances[0].Value.ToBalanceValueString() : maxPowerDown.ToBalanceValueString();
            AmountEditOnTextChanged(null, null);
        }

        private void PowerBtnOnClick(object sender, EventArgs e)
        {
            if (_powerAmount <= 0)
                return;

            if (string.IsNullOrEmpty(_walletPresenter.Balances[0].UserInfo.ActiveKey))
            {
                NavigationController.PushViewController(new LoginViewController(false), true);
                return;
            }

            DoPowerAction();
        }

        private void DoPowerAction()
        {
            var at = new NSMutableAttributedString();
            at.Append(new NSAttributedString($"Are you sure you want to {_powerAction.GetDescription()} {_amountTextField.GetDoubleValue().ToString(CultureInfo.InvariantCulture)} {CurrencyType.Steem.ToString()}?", Constants.DialogPopupTextStyle));
            Popups.TransferDialogPopup.Create(NavigationController, at, ContinuePowerAction);
        }

        private async void ContinuePowerAction(bool shouldContinue)
        {
            if(shouldContinue)
            {
                _loader.StartAnimating();
                _actionButton.Enabled = false;

                var model = new BalanceModel(_powerAmount, _walletPresenter.Balances[0].MaxDecimals, _walletPresenter.Balances[0].CurrencyType)
                {
                    UserInfo = _walletPresenter.Balances[0].UserInfo
                };

                var response = await _walletPresenter.TryPowerUpOrDown(model, _powerAction);

                _loader.StopAnimating();
                _actionButton.Enabled = true;

                if (response.IsSuccess)
                {
                    _walletPresenter.UpdateWallet?.Invoke();
                    NavigationController.PopViewController(true);
                }
                else
                    ShowAlert(response.Exception);
            }
        }

        private void CreateView()
        {
            View.UserInteractionEnabled = true;

            var viewTap = new UITapGestureRecognizer(() =>
            {
                _amountTextField.ResignFirstResponder();
            });

            View.AddGestureRecognizer(viewTap);

            View.BackgroundColor = Constants.R250G250B250;

            var topBackground = new UIView();
            topBackground.BackgroundColor = UIColor.White;
            View.AddSubview(topBackground);

            topBackground.AutoPinEdgeToSuperviewEdge(ALEdge.Top, 20);
            topBackground.AutoPinEdgeToSuperviewEdge(ALEdge.Left);
            topBackground.AutoPinEdgeToSuperviewEdge(ALEdge.Right);

            var steemView = new UIView();
            topBackground.AddSubview(steemView);

            var label = new UILabel();
            label.Text = "Steem";
            label.Font = Constants.Semibold14;
            steemView.AddSubview(label);

            label.AutoAlignAxisToSuperviewAxis(ALAxis.Horizontal);
            label.AutoPinEdgeToSuperviewEdge(ALEdge.Left);

            _firstTokenText.BaselineAdjustment = UIBaselineAdjustment.AlignCenters;
            _firstTokenText.AdjustsFontSizeToFitWidth = true;
            _firstTokenText.TextAlignment = UITextAlignment.Right;
            steemView.AddSubview(_firstTokenText);

            _firstTokenText.AutoAlignAxis(ALAxis.Horizontal, label);
            _firstTokenText.AutoPinEdgeToSuperviewEdge(ALEdge.Right);
            _firstTokenText.AutoPinEdge(ALEdge.Left, ALEdge.Right, label, 5);
            _firstTokenText.SetContentHuggingPriority(1, UILayoutConstraintAxis.Horizontal);

            steemView.AutoSetDimension(ALDimension.Height, 70);
            steemView.AutoPinEdgeToSuperviewEdge(ALEdge.Left, 20);
            steemView.AutoPinEdgeToSuperviewEdge(ALEdge.Right, 20);
            steemView.AutoPinEdgeToSuperviewEdge(ALEdge.Top);

            var separator = new UIView();
            separator.BackgroundColor = Constants.R245G245B245;

            topBackground.AddSubview(separator);

            separator.AutoSetDimension(ALDimension.Height, 1);
            separator.AutoPinEdgeToSuperviewEdge(ALEdge.Left, 20);
            separator.AutoPinEdgeToSuperviewEdge(ALEdge.Right, 20);
            separator.AutoPinEdge(ALEdge.Top, ALEdge.Bottom, steemView);

            var spView = new UIView();
            topBackground.AddSubview(spView);

            var label2 = new UILabel();
            label2.Text = "Steem Power";
            label2.Font = Constants.Semibold14;
            spView.AddSubview(label2);

            label2.AutoAlignAxisToSuperviewAxis(ALAxis.Horizontal);
            label2.AutoPinEdgeToSuperviewEdge(ALEdge.Left);

            _secondTokenText.BaselineAdjustment = UIBaselineAdjustment.AlignCenters;
            _secondTokenText.AdjustsFontSizeToFitWidth = true;
            _secondTokenText.TextAlignment = UITextAlignment.Right;
            spView.AddSubview(_secondTokenText);

            _secondTokenText.AutoAlignAxisToSuperviewAxis(ALAxis.Horizontal);
            _secondTokenText.AutoPinEdgeToSuperviewEdge(ALEdge.Right);
            _secondTokenText.AutoPinEdge(ALEdge.Left, ALEdge.Right, label2, 5);
            _secondTokenText.SetContentHuggingPriority(1, UILayoutConstraintAxis.Horizontal);

            spView.AutoSetDimension(ALDimension.Height, 70);
            spView.AutoPinEdge(ALEdge.Top, ALEdge.Bottom, separator);
            spView.AutoPinEdgeToSuperviewEdge(ALEdge.Left, 20);
            spView.AutoPinEdgeToSuperviewEdge(ALEdge.Right, 20);
            spView.AutoPinEdgeToSuperviewEdge(ALEdge.Bottom);

            var amountBackground = new UIView();
            amountBackground.BackgroundColor = UIColor.White;
            View.AddSubview(amountBackground);

            amountBackground.AutoPinEdge(ALEdge.Top, ALEdge.Bottom, topBackground, 10);
            amountBackground.AutoPinEdgeToSuperviewEdge(ALEdge.Left);
            amountBackground.AutoPinEdgeToSuperviewEdge(ALEdge.Right);

            var amountLabel = new UILabel();
            amountLabel.Text = AppSettings.LocalizationManager.GetText(LocalizationKeys.Amount);
            amountLabel.Font = Constants.Semibold14;
            amountBackground.AddSubview(amountLabel);

            amountLabel.AutoPinEdgeToSuperviewEdge(ALEdge.Top, 15);
            amountLabel.AutoPinEdgeToSuperviewEdge(ALEdge.Left, 20);

            _amountTextField = new SearchTextField(AppSettings.LocalizationManager.GetText(LocalizationKeys.TransferAmountHint), new AmountFieldDelegate(), false);
            _amountTextField.EditingChanged += AmountEditOnTextChanged;
            _amountTextField.KeyboardType = UIKeyboardType.DecimalPad;
            _amountTextField.Layer.CornerRadius = 25;
            amountBackground.AddSubview(_amountTextField);

            _amountTextField.AutoPinEdgeToSuperviewEdge(ALEdge.Left, 20);
            _amountTextField.AutoPinEdge(ALEdge.Top, ALEdge.Bottom, amountLabel, 16);
            _amountTextField.AutoSetDimension(ALDimension.Height, 50);
            _amountTextField.AutoPinEdgeToSuperviewEdge(ALEdge.Bottom, 20);

            _amountTextField.TouchUpOutside += (object sender, EventArgs e) =>
            {
                _amountTextField.ResignFirstResponder();
            };

            errorMessage = new UILabel
            {
                Font = Constants.Semibold14,
                TextColor = Constants.R255G34B5,
                Text = AppSettings.LocalizationManager.GetText(LocalizationKeys.AmountLimitFull),
                Hidden = true,
            };
            amountBackground.AddSubview(errorMessage);

            errorMessage.AutoPinEdge(ALEdge.Top, ALEdge.Bottom, _amountTextField);
            errorMessage.AutoPinEdge(ALEdge.Left, ALEdge.Left, _amountTextField);
            errorMessage.AutoPinEdge(ALEdge.Right, ALEdge.Right, _amountTextField);

            var max = new UIButton();
            max.SetTitle(AppSettings.LocalizationManager.GetText(LocalizationKeys.Max), UIControlState.Normal);
            max.SetTitleColor(UIColor.Black, UIControlState.Normal);
            max.Font = Constants.Semibold14;
            max.Layer.BorderWidth = 1;
            max.Layer.BorderColor = Constants.R245G245B245.CGColor;
            max.Layer.CornerRadius = 25;

            amountBackground.AddSubview(max);

            max.AutoPinEdgeToSuperviewEdge(ALEdge.Right, 20);
            max.AutoPinEdge(ALEdge.Left, ALEdge.Right, _amountTextField, 10);
            max.AutoSetDimensionsToSize(new CGSize(80, 50));
            max.AutoAlignAxis(ALAxis.Horizontal, _amountTextField);
            max.TouchDown += MaxBtnOnClick;

            _actionButton.SetTitle(AppSettings.LocalizationManager.GetText(_powerAction == PowerAction.PowerUp ? LocalizationKeys.PowerUp : LocalizationKeys.PowerDown), UIControlState.Normal);
            _actionButton.SetTitleColor(UIColor.White, UIControlState.Normal);
            _actionButton.SetTitleColor(UIColor.Clear, UIControlState.Disabled);

            View.AddSubview(_actionButton);

            _actionButton.AutoPinEdge(ALEdge.Top, ALEdge.Bottom, amountBackground, 30);
            _actionButton.AutoPinEdgeToSuperviewEdge(ALEdge.Left, 20);
            _actionButton.AutoPinEdgeToSuperviewEdge(ALEdge.Right, 20);
            _actionButton.AutoSetDimension(ALDimension.Height, 50);

            _actionButton.LayoutIfNeeded();
            _actionButton.TouchDown += PowerBtnOnClick;

            _loader.ActivityIndicatorViewStyle = UIActivityIndicatorViewStyle.White;
            _loader.HidesWhenStopped = true;
            _actionButton.AddSubview(_loader);
            _loader.AutoCenterInSuperview();

            Constants.CreateGradient(_actionButton, 25);
            Constants.CreateShadowFromZeplin(_actionButton, Constants.R231G72B0, 0.3f, 0, 10, 20, 0);
        }

        private void SetBackButton()
        {
            var leftBarButton = new UIBarButtonItem(UIImage.FromBundle("ic_back_arrow"), UIBarButtonItemStyle.Plain, GoBack);
            NavigationItem.SetLeftBarButtonItem(leftBarButton, true);
            NavigationController.NavigationBar.TintColor = Constants.R15G24B30;

            NavigationItem.Title = AppSettings.LocalizationManager.GetText(_powerAction == PowerAction.PowerUp ? LocalizationKeys.PowerUp : LocalizationKeys.PowerDown);

            NavigationController.NavigationBar.Translucent = false;
        }
    }
}
