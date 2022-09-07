using common.libs;
using System;
using System.IO;
using System.Net;

namespace client.service.ui.api.service.webServer
{
    public interface IWebServer
    {
        public void Start();
    }

    public interface IWebServerFileReader
    {
        public byte[] Read(string fileName);
    }

    public class WebServerFileReader : IWebServerFileReader
    {
        private readonly Config config;
        public WebServerFileReader(Config config)
        {
            this.config = config;
        }

        public byte[] Read(string fileName)
        {
            fileName = Path.Join(config.Web.Root, fileName);
            if (File.Exists(fileName))
            {
                return File.ReadAllBytes(fileName);
            }
            return Helper.EmptyArray;
        }
    }
}
