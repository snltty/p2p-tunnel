using common.libs.extends;
using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace common.libs.database
{
    /// <summary>
    /// 配置文件缓存
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IConfigDataProvider<T> where T : class, new()
    {
        /// <summary>
        /// 加载
        /// </summary>
        /// <returns></returns>
        Task<T> Load();
        /// <summary>
        /// 加载
        /// </summary>
        /// <returns></returns>
        Task<string> LoadString();
        /// <summary>
        /// 保存
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        Task Save(T model);
        /// <summary>
        /// 保存
        /// </summary>
        /// <param name="jsonStr"></param>
        /// <returns></returns>
        Task Save(string jsonStr);
    }

    /// <summary>
    /// 配置文件的文件缓存
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ConfigDataFileProvider<T> : IConfigDataProvider<T> where T : class, new()
    {
        public async Task<T> Load()
        {
            string fileName = GetTableName(typeof(T));
            if (File.Exists(fileName))
            {
                string str = (await File.ReadAllTextAsync(fileName).ConfigureAwait(false));
                return str.DeJson<T>();
            }
            return default;
        }
        public async Task<string> LoadString()
        {
            string fileName = GetTableName(typeof(T));
            if (File.Exists(fileName))
            {
                return (await File.ReadAllTextAsync(fileName).ConfigureAwait(false));
            }
            return string.Empty;
        }

        public async Task Save(T model)
        {
            string fileName = GetTableName(typeof(T));
            await File.WriteAllTextAsync(fileName, model.ToJsonIndented(), Encoding.UTF8).ConfigureAwait(false);
        }
        public async Task Save(string jsonStr)
        {
            string fileName = GetTableName(typeof(T));
            await File.WriteAllTextAsync(fileName, jsonStr, Encoding.UTF8).ConfigureAwait(false);
        }

        private string GetTableName(Type type)
        {
            var attrs = type.GetCustomAttributes(typeof(TableAttribute), false);
            if (attrs.Length > 0)
            {
                return $"{(attrs[0] as TableAttribute).Name}.json";
            }
            return $"{type.Name}.json";
        }
    }
}
