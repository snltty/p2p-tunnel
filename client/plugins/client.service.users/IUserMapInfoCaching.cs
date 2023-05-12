using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations.Schema;

namespace client.service.users
{
    public interface IUserMapInfoCaching
    {
        public bool Add(UserMapInfo map);
        public bool Get(ulong userid, out UserMapInfo map);
    }
    public sealed class UserMapInfoCaching: IUserMapInfoCaching
    {
        private ConcurrentDictionary<ulong, UserMapInfo> mapDic = new ConcurrentDictionary<ulong, UserMapInfo>();
        private readonly Config config;
        public UserMapInfoCaching()
        {

        }

        public bool Add(UserMapInfo map)
        {
            mapDic.AddOrUpdate(map.ID, map, (a, b) => map);
            return true;
        }
        public bool Get(ulong userid, out UserMapInfo map)
        {
            return mapDic.TryGetValue(userid, out map);
        }
    }

    public sealed class UserMapInfo
    {
        public ulong ID { get; set; }
        public uint Access { get; set; }
    }

    [Table("users")]
    public sealed class UserStoreModel
    {
        public ConcurrentDictionary<ulong, UserMapInfo> Users { get; set; }
    }
}
