using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace client.service.ui.api.service.webServer
{
    /// <summary>
    /// 本地web管理端服务器
    /// </summary>
    public class WebServer : IWebServer
    {
        private readonly Config config;
        private readonly IWebServerFileReader webServerFileReader;
        public WebServer(Config config, IWebServerFileReader webServerFileReader)
        {
            this.config = config;
            this.webServerFileReader = webServerFileReader;
        }

        public void Start()
        {
            HttpListener http = new HttpListener();
            http.Prefixes.Add($"http://{config.Web.BindIp}:{config.Web.Port}/");
            http.Start();
            http.BeginGetContext(Callback, http);
        }
        private void Callback(IAsyncResult result)
        {
            HttpListener http = result.AsyncState as HttpListener;
            HttpListenerContext context = http.EndGetContext(result);

            HttpListenerRequest request = context.Request;
            using HttpListenerResponse response = context.Response;
            using Stream stream = response.OutputStream;
            try
            {
                response.Headers["Server"] = "snltty";

                string path = request.Url.AbsolutePath;
                //默认页面
                if (path == "/") path = "index.html";

                byte[] bytes = webServerFileReader.Read(path);
                if (bytes.Length > 0)
                {
                    response.ContentLength64 = bytes.Length;
                    response.ContentType = GetContentType(path);
                    stream.Write(bytes, 0, bytes.Length);
                }
                else
                {
                    response.StatusCode = (int)HttpStatusCode.NotFound;
                }
            }
            catch (Exception)
            {
                response.StatusCode = (int)HttpStatusCode.BadRequest;
            }
            stream.Close();
            stream.Dispose();

            http.BeginGetContext(Callback, http);
        }


        private Dictionary<string, string> types = new Dictionary<string, string> {
            { ".png","image/png"},
            { ".jpg","image/jpg"},
            { ".jpeg","image/jpeg"},
            { ".gif","image/gif"},
            { ".svg","image/svg+xml"},
            { ".ico","image/x-icon"},
            { ".js","text/javascript; charset=utf-8"},
            { ".html","text/html; charset=utf-8"},
            { ".css","text/css; charset=utf-8"},
            { ".pac","application/x-ns-proxy-autoconfig; charset=utf-8"},
        };
        private string GetContentType(string path)
        {
            string ext = Path.GetExtension(path);
            if (types.ContainsKey(ext))
            {
                return types[ext];
            }
            return "application/octet-stream";
        }
    }

}
