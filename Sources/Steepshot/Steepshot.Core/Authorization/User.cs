﻿using System;
using System.Collections.Generic;
using System.Linq;
using Steepshot.Core.Models.Enums;
using Steepshot.Core.Models.Responses;
using Steepshot.Core.Utils;

namespace Steepshot.Core.Authorization
{
    public sealed class User
    {
        private readonly UserManager _data;

        public UserInfo UserInfo { get; private set; }

        public bool IsDev
        {
            get => UserInfo.IsDev;
            set
            {
                UserInfo.IsDev = value;
                if (HasPostingPermission)
                    _data.Update(UserInfo);
            }
        }

        public bool IsNsfw
        {
            get => UserInfo.IsNsfw;
            set
            {
                UserInfo.IsNsfw = value;
                if (HasPostingPermission)
                    _data.Update(UserInfo);
            }
        }

        public bool IsLowRated
        {
            get => UserInfo.IsLowRated;
            set
            {
                UserInfo.IsLowRated = value;
                if (HasPostingPermission)
                    _data.Update(UserInfo);
            }
        }

        public bool ShowVotingSlider
        {
            get => UserInfo.ShowVotingSlider;
            set
            {
                UserInfo.ShowVotingSlider = value;
                if (HasPostingPermission)
                    _data.Update(UserInfo);
            }
        }

        public bool ShowFooter
        {
            get => UserInfo.ShowFooter;
            set
            {
                UserInfo.ShowFooter = value;
                if (HasPostingPermission)
                    _data.Update(UserInfo);
            }
        }

        public string DefaultPhotoDirectory
        {
            get => UserInfo.DefaultPhotoDirectory;
            set
            {
                UserInfo.DefaultPhotoDirectory = value;
                if (HasPostingPermission)
                    _data.Update(UserInfo);
            }
        }

        public short VotePower
        {
            get => UserInfo.VotePower;
            set
            {
                UserInfo.VotePower = value;
                if (HasPostingPermission)
                    _data.Update(UserInfo);
            }
        }

        public HashSet<string> PostBlackList => UserInfo.PostBlackList;

        public PushSettings PushSettings
        {
            get => UserInfo.PushSettings;
            set
            {
                UserInfo.PushSettings = value;
                if (HasPostingPermission)
                    _data.Update(UserInfo);
            }
        }

        public List<string> WatchedUsers => UserInfo.WatchedUsers;
        public string PushesPlayerId
        {
            get => UserInfo.PushesPlayerId;
            set
            {
                UserInfo.PushesPlayerId = value;
                if (HasPostingPermission)
                    _data.Update(UserInfo);
            }
        }

        public bool IsFirstRun
        {
            get
            {
                var res = UserInfo.IsFirstRun;
                if (res)
                    IsFirstRun = false;
                return res;
            }
            set
            {
                UserInfo.IsFirstRun = value;
                if (HasPostingPermission)
                    _data.Update(UserInfo);
            }
        }

        public string ActiveKey
        {
            get => UserInfo.ActiveKey;
            set
            {
                UserInfo.ActiveKey = value;
                if (HasPostingPermission)
                    _data.Update(UserInfo);
            }
        }

        public string Login => UserInfo.Login;

        public KnownChains Chain => UserInfo.Chain;

        public AccountInfoResponse AccountInfo
        {
            get => UserInfo.AccountInfo;
            set => UserInfo.AccountInfo = value;
        }

        public bool HasPostingPermission => !string.IsNullOrEmpty(UserInfo?.PostingKey);

        public bool HasActivePermission => !string.IsNullOrEmpty(UserInfo?.ActiveKey);
        
        public Dictionary<string, string> Integration => UserInfo.Integration;
        

        public User()
        {
            _data = AppSettings.DataProvider;
        }

        public void Load()
        {
            var users = GetAllAccounts();
            if (users.Any())
            {
                var last = users[0];
                for (var i = 1; i < users.Count; i++)
                {
                    if (last.LoginTime < users[i].LoginTime)
                        last = users[i];
                }
                UserInfo = last;
            }
            else
            {
                UserInfo = new UserInfo();
            }
        }

        public void AddAndSwitchUser(string login, string pass, AccountInfoResponse accountInfo)
        {
            if (!string.IsNullOrEmpty(Login) && UserInfo.PostingKey == null)
            {
                UserInfo.PostingKey = pass;
                Save();
                return;
            }

            var userInfo = new UserInfo
            {
                Login = login,
                AccountInfo = accountInfo,
                Chain = accountInfo.Chains,
                PostingKey = pass,
            };

            _data.Insert(userInfo);
            UserInfo = userInfo;
        }

        public void AddActiveKey(string pass)
        {
            UserInfo.ActiveKey = pass;
            Save();
        }

        public void SwitchUser(UserInfo userInfo)
        {
            var user = _data.Select().FirstOrDefault(x => x.Login == userInfo.Login && x.Chain == userInfo.Chain);
            if (user != null)
            {
                UserInfo = user;
                UserInfo.LoginTime = DateTime.Now;
                Save();
            }
        }

        public void Delete()
        {
            if (UserInfo != null)
            {
                _data.Delete(UserInfo);
                UserInfo = new UserInfo();
            }
        }

        public void Delete(UserInfo userInfo)
        {
            _data.Delete(userInfo);
            if (UserInfo.Id == userInfo.Id)
                UserInfo = new UserInfo();
        }

        public List<UserInfo> GetAllAccounts()
        {
            var items = _data.Select();
            return items;
        }

        public void Save()
        {
            _data.Update(UserInfo);
        }
    }
}
