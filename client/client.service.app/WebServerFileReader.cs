using client.service.ui.api.service.webServer;
using common.libs.extends;

namespace client.service.app
{
    public class WebServerFileReader : IWebServerFileReader
    {
        public byte[] Read(string fileName)
        {
            using Stream fileStream = FileSystem.Current.OpenAppPackageFileAsync($"public/{fileName}").Result;
            using StreamReader reader = new StreamReader(fileStream);
            return reader.ReadToEnd().ToBytes();
        }
    }
}
