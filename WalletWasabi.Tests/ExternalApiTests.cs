﻿using WalletWasabi.Logging;
using WalletWasabi.WebClients.BlockCypher;
using WalletWasabi.WebClients.BlockCypher.Models;
using WalletWasabi.WebClients.SmartBit;
using WalletWasabi.WebClients.SmartBit.Models;
using NBitcoin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace WalletWasabi.Tests
{
	public class ExternalApiTests : IClassFixture<SharedFixture>
	{
		public SharedFixture SharedFixture { get; }

		public ExternalApiTests(SharedFixture sharedFixture)
		{
			SharedFixture = sharedFixture;
		}

		[Theory]
		[InlineData("test")]
		[InlineData("main")]
		public async Task SmartBitTestsAsync(string networkString)
		{
			if(!await TestAsync("https://api.smartbit.com.au/v1/blockchain/stats"))
			{
				return; // If website doesn't work, don't bother failing.
			}

			var network = Network.GetNetwork(networkString);
			using (var client = new SmartBitClient(network))
			{
				IEnumerable<SmartBitExchangeRate> rates = rates = await client.GetExchangeRatesAsync(CancellationToken.None);
				
				Assert.Contains("AUD", rates.Select(x => x.Code));
				Assert.Contains("USD", rates.Select(x => x.Code));

				var restoreLogMinLevel = Logger.MinimumLevel;
				Logger.SetMinimumLevel(LogLevel.Critical);
				await Assert.ThrowsAsync<HttpRequestException>(async () => await client.PushTransactionAsync(new Transaction(), CancellationToken.None));
				Logger.SetMinimumLevel(restoreLogMinLevel);
			}
		}

		[Theory]
		[InlineData("test")]
		[InlineData("main")]
		public async Task BlockCypherTestsAsync(string networkString)
		{
			if (!await TestAsync("https://api.blockcypher.com/v1/btc/main"))
			{
				return; // If website doesn't work, don't bother failing.
			}

			var network = Network.GetNetwork(networkString);
			using (var client = new BlockCypherClient(network))
			{
				BlockCypherGeneralInformation response = null;
				try
				{
					response = await client.GetGeneralInformationAsync(CancellationToken.None);
				}
				catch // stupid CI internet conenction sometimes fails
				{
					await Task.Delay(3000);
					response = await client.GetGeneralInformationAsync(CancellationToken.None);
				}
				Assert.NotNull(response.Hash);
				Assert.NotNull(response.LastForkHash);
				Assert.NotNull(response.PreviousHash);
				Assert.True(response.UnconfirmedCount > 0);
				Assert.InRange(response.LowFee.FeePerK, Money.Zero, response.MediumFee.FeePerK);
				Assert.InRange(response.MediumFee.FeePerK, response.LowFee.FeePerK, response.HighFee.FeePerK);
				Assert.InRange(response.HighFee.FeePerK, response.MediumFee.FeePerK, new Money(0.1m, MoneyUnit.BTC));
				Assert.True(response.Height >= 491999);
				Assert.Equal(new Uri(client.BaseAddress.ToString().Replace("http", "https") + "/blocks/" + response.Hash.ToString()), response.LatestUrl);
				Assert.Equal(new Uri(client.BaseAddress.ToString().Replace("http", "https") + "/blocks/" + response.PreviousHash.ToString()), response.PreviousUrl);
				if (network == Network.Main)
				{
					Assert.Equal("BTC.main", response.Name);
				}
				else
				{
					Assert.Equal("BTC.test3", response.Name);
				}
				Assert.True(response.PeerCount > 0);
			}
		}

		private async Task<bool> TestAsync(string uri)
		{
			try
			{
				using (var client = new HttpClient())
				using (var res = await client.GetAsync(uri))
				{
					if (res.StatusCode == HttpStatusCode.OK)
					{
						return true;
					}
				}
			}
			catch
			{
				Logger.LogWarning<ExternalApiTests>($"Uri wasn't reachable: {uri}");
			}
			return false;
		}
	}
}
