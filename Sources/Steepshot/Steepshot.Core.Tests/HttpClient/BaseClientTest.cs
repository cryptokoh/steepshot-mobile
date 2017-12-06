﻿using System;
using System.Threading;
using NUnit.Framework;
using Steepshot.Core.Models.Requests;

namespace Steepshot.Core.Tests.HttpClient
{
    [TestFixture]
    public class BaseClientTest : BaseTests
    {
        [Test, Sequential]
        public void GetPostVotersTest(
            [Values(KnownChains.Steem, KnownChains.Golos)] KnownChains apiName,
            [Values("@steepshot/steepshot-some-stats-and-explanations", "@anatolich/utro-dobroe-gospoda-i-damy-khochu-chtoby-opyatx-bylo-leto-plyazh-i-solncze--2017-11-08-02-10-33")] string url)
        {
            var count = 40;
            var request = new VotersRequest(url, VotersType.All)
            {
                Limit = count,
                Offset = string.Empty,

            };
            var task = Api[apiName].GetPostVoters(request, CancellationToken.None);
            task.Wait();
            var responce = task.Result;
            Assert.IsTrue(responce.Success);
            var result = responce.Result;
            Assert.IsTrue(result.Results.Count == count);
        }

        [Test, Sequential]
        public void GetPostVotersCancelTestTest(
            [Values(KnownChains.Steem, KnownChains.Golos)] KnownChains apiName,
            [Values("@steepshot/steepshot-some-stats-and-explanations", "@steepshot/steepshot-nekotorye-statisticheskie-dannye-i-otvety-na-voprosy")] string url)
        {
            try
            {
                var count = 40;
                var request = new VotersRequest(url, VotersType.All)
                {
                    Limit = count,
                    Offset = string.Empty
                };

                var token = new CancellationTokenSource(50);
                var task = Api[apiName].GetPostVoters(request, token.Token);
                task.Wait(token.Token);
                var responce = task.Result;
                Assert.IsTrue(responce.Success);
                var result = responce.Result;
                Assert.IsTrue(result.Results.Count == count);
            }
            catch (OperationCanceledException)
            {
                Assert.Pass();
                // go up
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }
}