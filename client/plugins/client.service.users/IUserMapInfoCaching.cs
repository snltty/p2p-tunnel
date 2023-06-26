using common.libs.database;
using common.libs.extends;
using System;
using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace client.service.users
{
    public interface IUserMapInfoCaching
    {
        public Task<bool> Add(UserMapInfo map);
        public bool Get(ulong userid, out UserMapInfo map);
    }

    [Table("users")]
    public class UserMapInfoCaching : IUserMapInfoCaching
    {
        public ConcurrentDictionary<ulong, UserMapInfo> Users { get; set; } = new ConcurrentDictionary<ulong, UserMapInfo>();

        private readonly IConfigDataProvider<UserMapInfoCaching> configDataProvider;
        public UserMapInfoCaching() { }
        public UserMapInfoCaching(IConfigDataProvider<UserMapInfoCaching> configDataProvider)
        {
            this.configDataProvider = configDataProvider;
            UserMapInfoCaching cache = ReadConfig().Result;
            if (cache != null)
            {
                Users = cache.Users;
            }
        }

        public async Task<bool>Add(UserMapInfo map)
        {
            Users.AddOrUpdate(map.ID, map, (a, b) => map);
            await SaveConfig();
            return true;
        }
        public bool Get(ulong userid, out UserMapInfo map)
        {
            return Users.TryGetValue(userid, out map);
        }

        public async Task<UserMapInfoCaching> ReadConfig()
        {
            UserMapInfoCaching config = await configDataProvider.Load() ?? new UserMapInfoCaching();
            return config;
        }
        public async Task SaveConfig()
        {
            await configDataProvider.Save(this).ConfigureAwait(false);
        }
    }

    public sealed class UserMapInfo
    {
        public ulong ID { get; set; }
        public uint Access { get; set; }
        [JsonIgnore]
        public ulong  ConnectionId { get; set; }
    }

}
