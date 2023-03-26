using common.libs.database;
using common.libs.extends;
using common.server.model;
using server.messengers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace server.service.validators
{
    internal class UserStore : IUserStore
    {
        private readonly IConfigDataProvider<UserStoreModel> configDataProvider;
        UserStoreModel storeModel = new UserStoreModel { Users = new Dictionary<ulong, UserInfo>() };
        public UserInfo DefaultUser { get; } = new UserInfo
        {
            ID = 0,
        };

        public UserStore(IConfigDataProvider<UserStoreModel> configDataProvider)
        {
            this.configDataProvider = configDataProvider;
            configDataProvider.Load().ContinueWith((result) =>
            {
                if (result.Result != null)
                {
                    storeModel = result.Result;
                }
            });
            AppDomain.CurrentDomain.ProcessExit += (object sender, EventArgs e) =>
            {
                configDataProvider.Save(storeModel);
            };
        }
        public int Count()
        {
            return storeModel.Users.Count;
        }

        public IEnumerable<UserInfo> Get()
        {
            return storeModel.Users.Values;
        }
        public IEnumerable<UserInfo> Get(int p = 1, int ps = 10)
        {
            return storeModel.Users.Values.Skip((p - 1) * ps).Take(ps);
        }

        public bool Get(ulong uid, out UserInfo user)
        {
            if (uid == 0)
            {
                user = DefaultUser;
                return true;
            }

            return storeModel.Users.TryGetValue(uid, out user);
        }
        public bool Get(string account, string password, out UserInfo user)
        {
            user = storeModel.Users.Values.FirstOrDefault(c => c.Account == account && c.Password == password);
            return user != null;
        }


        public bool Add(UserInfo user)
        {
            lock (storeModel)
            {
                if (user.ID == 0)
                {
                    if (string.IsNullOrWhiteSpace(user.Account))
                    {
                        return false;
                    }

                    user.ID = storeModel.Users.Values.Max(c => c.ID) + 1;
                    storeModel.Users.TryAdd(user.ID, user);

                    configDataProvider.Save(storeModel);
                    return true;
                }
                else
                {
                    if (storeModel.Users.TryGetValue(user.ID, out UserInfo _user) == false)
                    {
                        _user.Account = user.Account;
                        _user.Password = user.Password;
                        _user.Access = user.Access;
                        _user.NetFlow = user.NetFlow;
                        _user.EndTime = user.EndTime;
                        configDataProvider.Save(storeModel);
                        return true;
                    }
                }
            }
            return false;
        }
        public bool Remove(ulong id)
        {
            return storeModel.Users.Remove(id, out UserInfo user);
        }

       
    }

    [Table("users")]
    public class UserStoreModel
    {
        public Dictionary<ulong, UserInfo> Users { get; set; }
    }
}
