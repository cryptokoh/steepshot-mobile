﻿using System;

namespace Steepshot.Core.Models.Responses
{
    public class AccountHistoryResponse
    {
        public DateTime DateTime { get; set; }
        public OperationType Type { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public string Amount { get; set; }
        public string RewardSteem { get; set; }
        public string RewardSp { get; set; }
        public string RewardSbd { get; set; }
        public string Memo { get; set; }

        public enum OperationType
        {
            Transfer,
            PowerUp,
            PowerDown,
            ClaimReward
        }
    }
}
