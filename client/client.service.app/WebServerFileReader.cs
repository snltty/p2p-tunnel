using client.service.ui.api.service.webServer;
using common.libs.extends;

namespace client.service.app
{
    public sealed class WebServerFileReader : IWebServerFileReader
    {
        DateTime lastModified = DateTime.Now;
        public byte[] Read(string fileName, out DateTime lastModified)
        {
            lastModified = this.lastModified;
            fileName = Path.Join("public/web", fileName);
            using Stream fileStream = FileSystem.Current.OpenAppPackageFileAsync(fileName).Result;
            using StreamReader reader = new StreamReader(fileStream);
            return reader.ReadToEnd().ToBytes();
        }
    }
}
