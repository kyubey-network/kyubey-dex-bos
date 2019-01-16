using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using Microsoft.EntityFrameworkCore;
using Andoromeda.CleosNet.Client;
using Andoromeda.Kyubey.Models;
using Newtonsoft.Json;

namespace Andoromeda.Kyubey.Dex.Contract.TIP
{
    public class FavoriteTokenTests
    {
        private CleosClient client;
        private Config config;
        private KyubeyContext db;

        public FavoriteTokenTests()
        {
            client = new CleosClient();
            config = JsonConvert.DeserializeObject<Config>(File.ReadAllText("config.json"));
            client.CreateWalletAsync("default", "/home/cleos-net/wallet/wallet.key").Wait();
            client.ImportPrivateKeyToWalletAsync(config.PrivateKey, "default").Wait();
            var builder = new DbContextOptionsBuilder();
            builder.UseMySql(config.MySql);
            db = new KyubeyContext(builder.Options);
        }

        [Theory]
        [InlineData("YDAPP")]
        [InlineData("KBY")]
        public async Task AddFavoriteTest(string token)
        {
            var jobPosition = Convert.ToUInt64((await db.Constants.AsNoTracking().SingleAsync(x => x.Id == "action_pos")).Value);
            var addFavResult = await client.PushActionAsync(config.ContractAccount, "addfav", config.TestAccount, "active", new object[] { token });
            Assert.True(addFavResult.IsSucceeded);

            await Task.Delay(10000);
            // Assert timer hang
            Assert.True(Convert.ToUInt64((await db.Constants.AsNoTracking().SingleAsync(x => x.Id == "action_pos")).Value) > jobPosition);
            Assert.True(await db.Favorites.AnyAsync(x => x.Account == config.TestAccount && x.TokenId == token));
        }

        [Theory]
        [InlineData("YDAPP")]
        [InlineData("KBY")]
        public async Task RemoveFavoriteTest(string token)
        {
            var jobPosition = Convert.ToUInt64((await db.Constants.AsNoTracking().SingleAsync(x => x.Id == "action_pos")).Value);
            var addFavResult = await client.PushActionAsync(config.ContractAccount, "removefav", config.TestAccount, "active", new object[] { token });
            Assert.True(addFavResult.IsSucceeded);

            await Task.Delay(10000);
            // Assert timer hang
            Assert.True(Convert.ToUInt64((await db.Constants.AsNoTracking().SingleAsync(x => x.Id == "action_pos")).Value) > jobPosition);
            Assert.True(!await db.Favorites.AnyAsync(x => x.Account == config.TestAccount && x.TokenId == token));
        }
    }
}
