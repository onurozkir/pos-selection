using Microsoft.Extensions.Configuration;
using PayTR.PosSelection.Infrastructure.Factory.Interfaces;
using PayTR.PosSelection.Infrastructure.Interfaces.PosRatios;
using PayTR.PosSelection.Infrastructure.Interfaces.PriceCalculator;
using PayTR.PosSelection.Infrastructure.Interfaces.Redis;
using PayTR.PosSelection.Infrastructure.Models.PosSelection.Requests;
using StackExchange.Redis;

namespace PayTR.PosSelection.Tests.Units
{
    public class PosSelectionServiceTests
    {
        // apiden alınan full list mock için
        private const string PosRatiosJson = """
[
{"pos_name":"Garanti","card_type":"credit","card_brand":"bonus","installment":3,"currency":"TRY","commission_rate":0.026,"min_fee":0,"priority":6},
{"pos_name":"Garanti","card_type":"credit","card_brand":"bonus","installment":6,"currency":"TRY","commission_rate":0.027,"min_fee":0,"priority":6},
{"pos_name":"Garanti","card_type":"credit","card_brand":"bonus","installment":9,"currency":"TRY","commission_rate":0.032,"min_fee":0,"priority":5},
{"pos_name":"Garanti","card_type":"credit","card_brand":"bonus","installment":3,"currency":"USD","commission_rate":0.0322,"min_fee":0,"priority":6},
{"pos_name":"Garanti","card_type":"credit","card_brand":"bonus","installment":6,"currency":"EUR","commission_rate":0.033,"min_fee":0.5,"priority":6},

{"pos_name":"YapiKredi","card_type":"credit","card_brand":"world","installment":3,"currency":"TRY","commission_rate":0.024,"min_fee":0,"priority":7},
{"pos_name":"YapiKredi","card_type":"credit","card_brand":"world","installment":6,"currency":"TRY","commission_rate":0.028,"min_fee":0,"priority":7},
{"pos_name":"YapiKredi","card_type":"credit","card_brand":"world","installment":12,"currency":"TRY","commission_rate":0.031,"min_fee":0,"priority":7},
{"pos_name":"YapiKredi","card_type":"credit","card_brand":"world","installment":3,"currency":"USD","commission_rate":0.0315,"min_fee":0,"priority":7},
{"pos_name":"YapiKredi","card_type":"credit","card_brand":"world","installment":6,"currency":"EUR","commission_rate":0.0335,"min_fee":0.5,"priority":7},

{"pos_name":"Akbank","card_type":"credit","card_brand":"axess","installment":3,"currency":"TRY","commission_rate":0.023,"min_fee":0,"priority":5},
{"pos_name":"Akbank","card_type":"credit","card_brand":"axess","installment":6,"currency":"TRY","commission_rate":0.028,"min_fee":0,"priority":5},
{"pos_name":"Akbank","card_type":"credit","card_brand":"axess","installment":12,"currency":"TRY","commission_rate":0.031,"min_fee":0,"priority":6},
{"pos_name":"Akbank","card_type":"credit","card_brand":"axess","installment":3,"currency":"USD","commission_rate":0.031,"min_fee":0,"priority":5},
{"pos_name":"Akbank","card_type":"credit","card_brand":"axess","installment":6,"currency":"EUR","commission_rate":0.033,"min_fee":0.3,"priority":5},

{"pos_name":"Isbank","card_type":"credit","card_brand":"maximum","installment":1,"currency":"TRY","commission_rate":0.018,"min_fee":0,"priority":4},
{"pos_name":"Isbank","card_type":"credit","card_brand":"maximum","installment":3,"currency":"TRY","commission_rate":0.025,"min_fee":0,"priority":4},
{"pos_name":"Isbank","card_type":"credit","card_brand":"maximum","installment":6,"currency":"TRY","commission_rate":0.029,"min_fee":0,"priority":4},
{"pos_name":"Isbank","card_type":"credit","card_brand":"maximum","installment":3,"currency":"USD","commission_rate":0.0312,"min_fee":0,"priority":4},
{"pos_name":"Isbank","card_type":"credit","card_brand":"maximum","installment":9,"currency":"EUR","commission_rate":0.034,"min_fee":0.5,"priority":4},

{"pos_name":"Vakifbank","card_type":"credit","card_brand":"world","installment":3,"currency":"TRY","commission_rate":0.0255,"min_fee":0,"priority":4},
{"pos_name":"Vakifbank","card_type":"credit","card_brand":"world","installment":6,"currency":"TRY","commission_rate":0.028,"min_fee":0,"priority":4},
{"pos_name":"Vakifbank","card_type":"credit","card_brand":"world","installment":9,"currency":"TRY","commission_rate":0.033,"min_fee":0,"priority":4},
{"pos_name":"Vakifbank","card_type":"credit","card_brand":"world","installment":3,"currency":"USD","commission_rate":0.032,"min_fee":0,"priority":4},
{"pos_name":"Vakifbank","card_type":"credit","card_brand":"world","installment":9,"currency":"EUR","commission_rate":0.036,"min_fee":0.5,"priority":4},

{"pos_name":"Ziraat","card_type":"debit","card_brand":"bankkart","installment":1,"currency":"TRY","commission_rate":0.012,"min_fee":0,"priority":3},
{"pos_name":"Ziraat","card_type":"credit","card_brand":"bankkart","installment":3,"currency":"TRY","commission_rate":0.0245,"min_fee":0,"priority":3},
{"pos_name":"Ziraat","card_type":"credit","card_brand":"bankkart","installment":6,"currency":"TRY","commission_rate":0.0285,"min_fee":0,"priority":3},
{"pos_name":"Ziraat","card_type":"credit","card_brand":"bankkart","installment":3,"currency":"USD","commission_rate":0.031,"min_fee":0,"priority":3},
{"pos_name":"Ziraat","card_type":"credit","card_brand":"bankkart","installment":6,"currency":"EUR","commission_rate":0.0335,"min_fee":0.4,"priority":3},

{"pos_name":"Halkbank","card_type":"credit","card_brand":"paraf","installment":3,"currency":"TRY","commission_rate":0.0248,"min_fee":0,"priority":4},
{"pos_name":"Halkbank","card_type":"credit","card_brand":"paraf","installment":6,"currency":"TRY","commission_rate":0.0279,"min_fee":0,"priority":4},
{"pos_name":"Halkbank","card_type":"credit","card_brand":"paraf","installment":9,"currency":"TRY","commission_rate":0.034,"min_fee":0,"priority":4},
{"pos_name":"Halkbank","card_type":"credit","card_brand":"paraf","installment":3,"currency":"USD","commission_rate":0.032,"min_fee":0,"priority":4},
{"pos_name":"Halkbank","card_type":"credit","card_brand":"paraf","installment":6,"currency":"EUR","commission_rate":0.0342,"min_fee":0.5,"priority":4},

{"pos_name":"QNB","card_type":"credit","card_brand":"cardfinans","installment":3,"currency":"TRY","commission_rate":0.0229,"min_fee":0,"priority":6},
{"pos_name":"QNB","card_type":"credit","card_brand":"cardfinans","installment":6,"currency":"TRY","commission_rate":0.0275,"min_fee":0,"priority":6},
{"pos_name":"QNB","card_type":"credit","card_brand":"cardfinans","installment":9,"currency":"TRY","commission_rate":0.0335,"min_fee":0,"priority":5},
{"pos_name":"QNB","card_type":"credit","card_brand":"cardfinans","installment":3,"currency":"USD","commission_rate":0.0314,"min_fee":0,"priority":5},
{"pos_name":"QNB","card_type":"credit","card_brand":"cardfinans","installment":6,"currency":"EUR","commission_rate":0.034,"min_fee":0.5,"priority":5},

{"pos_name":"Denizbank","card_type":"credit","card_brand":"bonus","installment":3,"currency":"TRY","commission_rate":0.0258,"min_fee":0,"priority":5},
{"pos_name":"Denizbank","card_type":"credit","card_brand":"bonus","installment":6,"currency":"TRY","commission_rate":0.0288,"min_fee":0,"priority":5},
{"pos_name":"Denizbank","card_type":"credit","card_brand":"bonus","installment":3,"currency":"USD","commission_rate":0.031,"min_fee":0,"priority":5},
{"pos_name":"Denizbank","card_type":"credit","card_brand":"bonus","installment":6,"currency":"USD","commission_rate":0.032,"min_fee":0,"priority":5},
{"pos_name":"Denizbank","card_type":"credit","card_brand":"bonus","installment":3,"currency":"EUR","commission_rate":0.0318,"min_fee":0.4,"priority":5},

{"pos_name":"KuveytTurk","card_type":"debit","card_brand":"saglam","installment":1,"currency":"TRY","commission_rate":0.013,"min_fee":0,"priority":3},
{"pos_name":"KuveytTurk","card_type":"credit","card_brand":"saglam","installment":3,"currency":"TRY","commission_rate":0.02,"min_fee":2,"priority":4},
{"pos_name":"KuveytTurk","card_type":"credit","card_brand":"saglam","installment":6,"currency":"TRY","commission_rate":0.026,"min_fee":0,"priority":4},
{"pos_name":"KuveytTurk","card_type":"credit","card_brand":"saglam","installment":3,"currency":"USD","commission_rate":0.0305,"min_fee":1.5,"priority":4},
{"pos_name":"KuveytTurk","card_type":"credit","card_brand":"saglam","installment":6,"currency":"EUR","commission_rate":0.0325,"min_fee":0.4,"priority":4}
]
""";

        private readonly IConfiguration _configuration;

        public PosSelectionServiceTests()
        {
            var configDict = new Dictionary<string, string?>
            {
                { "PosSelectionMultiplier:TRY", "1.00" },
                { "PosSelectionMultiplier:USD", "1.01" }
            };

            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configDict)
                .Build();
        }

        private Infrastructure.Services.PosSelection CreateService()
        {
            var redis = new FakeRedisClient();
            // redis setlenir
            redis.Seed("pos-ratios:current", "20251120");
            redis.Seed("pos-ratios:20251120", PosRatiosJson);

            var factory = new FakePriceCalculatorFactory();
            var repo = new FakePosRatios(PosRatiosJson);

            return new Infrastructure.Services.PosSelection(redis, factory, _configuration, repo);
        }

        // 1) TRY, credit, 6 taksit (amount = 362.22)
        [Fact]
        public async Task SelectBestPos_TryCredit6Installment_ReturnsExpectedKuveytTurk()
        {
            var service = CreateService();

            var request = new Infrastructure.Models.PosSelection.Requests.PosSelection
            {
                Amount = 362.22m,
                Installment = 6,
                Currency = "TRY",
                CardType = "credit",
                CardBrand = null
            };

            var result = await service.SelectBestPosAsync(request, CancellationToken.None);

            Assert.NotNull(result);
            Assert.NotNull(result!.OverallMin);

            var o = result.OverallMin;

            Assert.Equal(362.22m, result.Filters.Amount);
            Assert.Equal(6, result.Filters.Installment);
            Assert.Equal("TRY", result.Filters.Currency);
            Assert.Equal("credit", result.Filters.CardType);

            Assert.Equal("KuveytTurk", o.PosName);
            Assert.Equal("credit", o.CardType);
            Assert.Equal("saglam", o.CardBrand);
            Assert.Equal(6, o.Installment);
            Assert.Equal("TRY", o.Currency);
            Assert.Equal(0.0260m, o.CommissionRate);
            Assert.Equal(9.42m, o.Price);
            Assert.Equal(371.64m, o.PayableTotal);
        }

        // 2) USD, credit, 3 taksit, bonus (amount = 395.00)
        [Fact]
        public async Task SelectBestPos_UsdCredit3InstallmentBonus_ReturnsExpectedDenizbank()
        {
            var service = CreateService();

            var request = new Infrastructure.Models.PosSelection.Requests.PosSelection
            {
                Amount = 395.00m,
                Installment = 3,
                Currency = "USD",
                CardType = "credit",
                CardBrand = "bonus"
            };

            var result = await service.SelectBestPosAsync(request, CancellationToken.None);

            Assert.NotNull(result);
            Assert.NotNull(result!.OverallMin);

            var o = result.OverallMin;

            Assert.Equal("Denizbank", o.PosName);
            Assert.Equal("credit", o.CardType);
            Assert.Equal("bonus", o.CardBrand);
            Assert.Equal(3, o.Installment);
            Assert.Equal("USD", o.Currency);
            Assert.Equal(0.0310m, o.CommissionRate);
            Assert.Equal(12.37m, o.Price);
            Assert.Equal(407.37m, o.PayableTotal);
        }

        // 3) TRY, credit, 3 taksit — min_fee etkisi (amount = 60.00)
        [Fact]
        public async Task SelectBestPos_TryCredit3Installment_MinFeeScenario_ReturnsQnb()
        {
            var service = CreateService();

            var request = new Infrastructure.Models.PosSelection.Requests.PosSelection
            {
                Amount = 60.00m,
                Installment = 3,
                Currency = "TRY",
                CardType = "credit",
                CardBrand = null
            };

            var result = await service.SelectBestPosAsync(request, CancellationToken.None);

            Assert.NotNull(result);
            Assert.NotNull(result!.OverallMin);

            var o = result.OverallMin;

            Assert.Equal("QNB", o.PosName);
            Assert.Equal("credit", o.CardType);
            Assert.Equal("cardfinans", o.CardBrand);
            Assert.Equal(3, o.Installment);
            Assert.Equal("TRY", o.Currency);
            Assert.Equal(0.0229m, o.CommissionRate);
            Assert.Equal(1.37m, o.Price);
            Assert.Equal(61.37m, o.PayableTotal);
        }

        // 4) TRY, credit, 12 taksit — tie-breaker (priority) (amount = 100.00)
        [Fact]
        public async Task SelectBestPos_TryCredit12Installment_TieBreakerByPriority_ReturnsYapiKredi()
        {
            var service = CreateService();

            var request = new Infrastructure.Models.PosSelection.Requests.PosSelection
            {
                Amount = 100.00m,
                Installment = 12,
                Currency = "TRY",
                CardType = "credit",
                CardBrand = null
            };

            var result = await service.SelectBestPosAsync(request, CancellationToken.None);

            Assert.NotNull(result);
            Assert.NotNull(result!.OverallMin);

            var o = result.OverallMin;
            
            Assert.Equal("YapiKredi", o.PosName);
            Assert.Equal("world", o.CardBrand);
            Assert.Equal(0.0310m, o.CommissionRate);
            Assert.Equal(3.10m, o.Price);
            Assert.Equal(103.10m, o.PayableTotal);
        }

        #region Fakes

        /// <summary>
        /// mock redis
        /// </summary>
        private class FakeRedisClient : IRedisClient
        {
            private readonly Dictionary<string, string> _store = new(StringComparer.OrdinalIgnoreCase);
            private IRedisClient _redisClientImplementation;

            public void Seed(string key, string value) => _store[key] = value;

            public Task<string?> Get(string key)
            {
                _store.TryGetValue(key, out var value);
                return Task.FromResult<string?>(value);
            }

            public Task<bool> Delete(string key)
            {
                return Task.FromResult(true);
            }

            public Task<bool> DeleteAllKey(string key)
            {
               
                return Task.FromResult(true);
            }

            public Task<bool> Set(string key, string value)
            {
                _store[key] = value;
                return Task.FromResult(true);
            }

            public Task<bool> Set(string key, string value, TimeSpan expiration, When when)
            {
                if (when == When.NotExists && _store.ContainsKey(key))
                    return Task.FromResult(false);

                _store[key] = value;
                return Task.FromResult(true);
            }
        }

        /// <summary>
        /// mcok db
        /// </summary>
        private class FakePosRatios : IPosRatios
        {
            private readonly string _json;

            public FakePosRatios(string json)
            {
                _json = json;
            }

            public Task<bool> InsertVersion(int version, string ratiosJson, DateTimeOffset posReceiveFinishDate,
                CancellationToken cancellationToken)
            {
                return Task.FromResult(true);
            }

            public Task<string> GetLastVersion(CancellationToken cancellationToken)
            {
                return Task.FromResult(_json);
            }
        }

        /// <summary>
        /// mock facatory
        /// </summary>
        private class FakePriceCalculatorFactory : IPriceCalculatorFactory
        {
            public IPriceCalculator GetCalculator(string currency)
            {
                return new GenericPriceCalculator(currency);
            }
        }

        /// <summary>
        /// calculator mock
        /// </summary>
        private class GenericPriceCalculator : IPriceCalculator
        {
            public string Currency { get; }

            public decimal Multiplier { get; set; } = 1m;

            public GenericPriceCalculator(string currency)
            {
                Currency = currency;
            }

            public decimal Calculate(decimal amount, decimal commissionRate, decimal minFee)
            {
                var raw = amount * commissionRate * Multiplier;
                var cost = Math.Max(raw, minFee);
                return Math.Round(cost, 2, MidpointRounding.AwayFromZero);
            }
        }

        #endregion
    }
}
