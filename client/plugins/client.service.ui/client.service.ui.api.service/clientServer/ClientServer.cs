using client.service.ui.api.clientServer;
using common.libs;
using common.libs.extends;
using common.server.servers.pipeLine;
using common.server.servers.websocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace client.service.ui.api.service.clientServer
{
    public class ClientServer : IClientServer
    {
        private readonly Dictionary<string, PluginPathCacheInfo> plugins = new();
        private readonly Dictionary<string, IClientConfigure> settingPlugins = new();

        private readonly Config config;
        private readonly ServiceProvider serviceProvider;
        private WebSocketServer server;

        public ClientServer(Config config, ServiceProvider serviceProvider)
        {
            this.config = config;
            this.serviceProvider = serviceProvider;
        }

        public void LoadPlugins(Assembly[] assemblys)
        {

            Type voidType = typeof(void);

            IEnumerable<Type> types = assemblys.SelectMany(c => c.GetTypes());
            foreach (Type item in types.Where(c => c.GetInterfaces().Contains(typeof(IClientService))))
            {
                string path = item.Name.Replace("ClientService", "");
                object obj = serviceProvider.GetService(item);
                foreach (MethodInfo method in item.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
                {
                    string key = $"{path}/{method.Name}".ToLower();
                    if (!plugins.ContainsKey(key))
                    {
                        plugins.TryAdd(key, new PluginPathCacheInfo
                        {
                            IsVoid = method.ReturnType == voidType,
                            Method = method,
                            Target = obj,
                            IsTask = method.ReturnType.GetProperty("IsCompleted") != null && method.ReturnType.GetMethod("GetAwaiter") != null,
                            IsTaskResult = method.ReturnType.GetProperty("Result") != null
                        });
                    }
                }
            }
            foreach (Type item in types.Where(c => c.GetInterfaces().Contains(typeof(IClientConfigure))))
            {
                if (!settingPlugins.ContainsKey(item.Name))
                    settingPlugins.Add(item.Name, (IClientConfigure)serviceProvider.GetService(item));
            }
        }
        public void Websocket()
        {
            server = new();
            server.Start(config.Websocket.BindIp, config.Websocket.Port);
            server.OnMessage = (connection, frame, message) =>
            {
                var req = message.DeJson<ClientServiceRequestInfo>();
                var resp = OnMessage(req).Result.ToJson().ToBytes();
                connection.SendFrameText(resp);
            };
        }

        public IClientConfigure GetConfigure(string className)
        {
            settingPlugins.TryGetValue(className, out IClientConfigure plugin);
            return plugin;
        }
        public IEnumerable<ClientServiceConfigureInfo> GetConfigures()
        {
            return settingPlugins.Select(c => new ClientServiceConfigureInfo
            {
                Name = c.Value.Name,
                Author = c.Value.Author,
                Desc = c.Value.Desc,
                ClassName = c.Value.GetType().Name,
                Enable = c.Value.Enable
            });
        }
        public IEnumerable<string> GetServices()
        {
            return plugins.Select(c => c.Value.Target.GetType().Name).Distinct();
        }

        public void Notify(ClientServiceResponseInfo resp)
        {
            byte[] msg = resp.ToJson().ToBytes();
            foreach (var item in server.Connections)
            {
                item.SendFrameText(msg);
            }
        }
        public async Task<ClientServiceResponseInfo> OnMessage(ClientServiceRequestInfo model)
        {
            model.Path = model.Path.ToLower();
            if (!plugins.ContainsKey(model.Path))
            {
                return new ClientServiceResponseInfo
                {
                    Content = "not exists this path",
                    RequestId = model.RequestId,
                    Path = model.Path,
                    Code = ClientServiceResponseCodes.Error
                };
            }

            PluginPathCacheInfo plugin = plugins[model.Path];
            try
            {
                ClientServiceParamsInfo param = new ClientServiceParamsInfo
                {
                    RequestId = model.RequestId,
                    Content = model.Content,
                    Path = model.Path,
                    //Connection = connection
                };
                dynamic resultAsync = plugin.Method.Invoke(plugin.Target, new object[] { param });
                object resultObject = null;
                if (!plugin.IsVoid)
                {
                    if (plugin.IsTask)
                    {
                        await resultAsync.ConfigureAwait(false);
                        if (plugin.IsTaskResult)
                        {
                            resultObject = resultAsync.Result;
                        }
                    }
                    else
                    {
                        resultObject = resultAsync;
                    }
                }
                return new ClientServiceResponseInfo
                {
                    Content = param.Code != ClientServiceResponseCodes.Error ? resultObject : param.ErrorMessage,
                    RequestId = param.RequestId,
                    Path = param.Path,
                    Code = param.Code
                };
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex);
                return new ClientServiceResponseInfo
                {
                    Content = ex.Message,
                    RequestId = model.RequestId,
                    Path = model.Path,
                    Code = ClientServiceResponseCodes.Error
                };
            }
        }


        private const string pipeName = "client.cmd";
        public void NamedPipe()
        {
            PipelineServer pipelineServer = new PipelineServer(pipeName, (string message) =>
            {
                return (OnMessage(message.DeJson<ClientServiceRequestInfo>()).Result).ToJson();
            });
            pipelineServer.BeginAccept();
        }
    }


    public struct PluginPathCacheInfo
    {
        public object Target { get; set; }
        public MethodInfo Method { get; set; }
        public bool IsVoid { get; set; }
        public bool IsTask { get; set; }
        public bool IsTaskResult { get; set; }
    }
}
