using common.libs.extends;
using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace common.libs.database
{
    public interface IConfigDataProvider<T> where T : class
    {
        Task<T> Load();
        Task Save(T model);
    }

    public class ConfigDataFileProvider<T> : IConfigDataProvider<T> where T : class
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

        public async Task Save(T model)
        {
            string fileName = GetTableName(typeof(T));
            await File.WriteAllTextAsync(fileName, model.ToJson(), Encoding.UTF8).ConfigureAwait(false);
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
