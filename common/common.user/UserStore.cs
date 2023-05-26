using common.libs.database;
using common.server;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace common.user
{
    public sealed class UserStore : IUserStore
    {
        private readonly IConfigDataProvider<UserStoreModel> configDataProvider;
        UserStoreModel storeModel = new UserStoreModel { Users = new Dictionary<ulong, UserInfo>() };

        public UserStore(IConfigDataProvider<UserStoreModel> configDataProvider)
        {
            this.configDataProvider = configDataProvider;
            var result = configDataProvider.Load().Result;
            if (result != null)
            {
                storeModel = result;
            }
            AppDomain.CurrentDomain.ProcessExit += (object sender, EventArgs e) =>
            {
                configDataProvider.Save(storeModel).Wait();
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
        public IEnumerable<UserInfo> Get(int p = 1, int ps = 10, byte sort = 0, string account = "")
        {
            var values = storeModel.Users.Values.Select(c => c);
            if (string.IsNullOrWhiteSpace(account) == false)
            {
                values = values.Where(c => c.Account.Contains(account));
            }

            byte sortType = (byte)(sort >> 7);
            byte sortField = (byte)(sort & 0b01111111);
            if (sortField == UserInfo.SortID)
            {
                if (sortType == UserInfo.SortDesc)
                    values = values.OrderByDescending(c => c.ID);
                else
                    values = values.OrderBy(c => c.ID);
            }
            else if (sortField == UserInfo.SortAddTime)
            {
                if (sortType == UserInfo.SortDesc)
                    values = values.OrderByDescending(c => c.AddTime);
                else
                    values = values.OrderBy(c => c.AddTime);
            }
            else if (sortField == UserInfo.SortEndTime)
            {
                if (sortType == UserInfo.SortDesc)
                    values = values.OrderByDescending(c => c.EndTime);
                else
                    values = values.OrderBy(c => c.EndTime);
            }
            else if (sortField == UserInfo.SortNetFlow)
            {
                if (sortType == UserInfo.SortDesc)
                    values = values.OrderByDescending(c => c.NetFlow);
                else
                    values = values.OrderBy(c => c.NetFlow);
            }
            else if (sortField == UserInfo.SortSignLimit)
            {
                if (sortType == UserInfo.SortDesc)
                    values = values.OrderByDescending(c => c.SignLimit);
                else
                    values = values.OrderBy(c => c.SignLimit);
            }

            return values.Skip((p - 1) * ps).Take(ps);
        }

        public bool Get(ulong uid, out UserInfo user)
        {
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
                    if (storeModel.Users.Values.FirstOrDefault(c => c.Account == user.Account) != null)
                    {
                        return false;
                    }

                    if (storeModel.Users.Count > 0)
                    {
                        user.ID = storeModel.Users.Values.Max(c => c.ID) + 1;
                    }
                    else
                    {
                        user.ID = 1;
                    }

                    user.AddTime = DateTime.UtcNow;
                    storeModel.Users.TryAdd(user.ID, user);

                    configDataProvider.Save(storeModel);
                    return true;
                }
                else
                {
                    if (storeModel.Users.TryGetValue(user.ID, out UserInfo _user))
                    {
                        if (string.IsNullOrWhiteSpace(user.Password) == false)
                        {
                            _user.Password = user.Password;
                        }
                        _user.Access = user.Access;
                        _user.NetFlowType = user.NetFlowType;
                        _user.NetFlow = user.NetFlow;
                        _user.EndTime = user.EndTime;
                        _user.SignLimitType = user.SignLimitType;
                        _user.SignLimit = user.SignLimit;
                        configDataProvider.Save(storeModel);
                        return true;
                    }
                }
            }
            return false;
        }
        public bool UpdatePassword(ulong id, string password)
        {
            if (storeModel.Users.TryGetValue(id, out UserInfo _user))
            {
                _user.Password = password;
                configDataProvider.Save(storeModel);
                return true;
            }
            return false;
        }
        public bool Remove(ulong id)
        {
            if (storeModel.Users.Remove(id, out UserInfo user))
            {
                foreach (IConnection item in user.Connections.Values)
                {
                    item?.Disponse();
                }
                configDataProvider.Save(storeModel);
                return true;
            }
            return false;
        }

        public UserInfo DefaultUser()
        {
            return new UserInfo
            {
                ID = 0,
            };
        }
    }

    [Table("users")]
    public sealed class UserStoreModel
    {
        public Dictionary<ulong, UserInfo> Users { get; set; }
    }
}
