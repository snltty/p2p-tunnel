using common.libs;
using common.libs.database;
using common.libs.extends;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace client.service.app
{
    public class ConfigDataFileProvider<T> : IConfigDataProvider<T> where T : class, new()
    {
        public async Task<T> Load()
        {
            string fileName = GetTableName(typeof(T));
            await Move(fileName);

            string targetFile = Path.Combine(FileSystem.Current.AppDataDirectory, fileName);
            string content = File.ReadAllText(targetFile);
            return content.DeJson<T>();
        }
        public async Task<string> LoadString()
        {
            string fileName = GetTableName(typeof(T));
            await Move(fileName);

            string targetFile = Path.Combine(FileSystem.Current.AppDataDirectory, fileName);
            string content = File.ReadAllText(targetFile);
            return content;
        }

        private async Task Move(string fileName)
        {
            string targetFile = Path.Combine(FileSystem.Current.AppDataDirectory, fileName);
            if (!File.Exists(targetFile))
            {
                string content = await ReadPackage(fileName);
                File.WriteAllText(targetFile, content, Encoding.UTF8);
            }
        }
        private async Task<string> ReadPackage(string fileName)
        {
            using Stream fileStream = await FileSystem.Current.OpenAppPackageFileAsync($"public/{fileName}");
            using StreamReader reader = new StreamReader(fileStream, Encoding.UTF8);
            return reader.ReadToEnd();
        }


        public async Task Save(T model)
        {
            string fileName = GetTableName(typeof(T));
            string targetFile = Path.Combine(FileSystem.Current.AppDataDirectory, fileName);

            string content = model.ToJson();
            File.WriteAllText(targetFile, content, Encoding.UTF8);

            await Task.CompletedTask;
        }
        public async Task Save(string jsonStr)
        {
            string fileName = GetTableName(typeof(T));
            string targetFile = Path.Combine(FileSystem.Current.AppDataDirectory, fileName);

            File.WriteAllText(targetFile, jsonStr, Encoding.UTF8);

            await Task.CompletedTask;
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
