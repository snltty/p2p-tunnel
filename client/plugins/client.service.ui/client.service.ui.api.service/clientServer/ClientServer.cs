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
    /// <summary>
    /// 前段接口服务
    /// </summary>
    public sealed class ClientServer : IClientServer
    {
        private readonly Dictionary<string, PluginPathCacheInfo> plugins = new();
        private readonly Dictionary<string, IClientConfigure> settingPlugins = new();

        private readonly Config config;
        private readonly ServiceProvider serviceProvider;
        private WebSocketServer server;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="config"></param>
        /// <param name="serviceProvider"></param>
        public ClientServer(Config config, ServiceProvider serviceProvider)
        {
            this.config = config;
            this.serviceProvider = serviceProvider;
        }

        /// <summary>
        /// 加载插件
        /// </summary>
        /// <param name="assemblys"></param>
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
                if (settingPlugins.ContainsKey(item.Name) == false)
                    settingPlugins.Add(item.Name, (IClientConfigure)serviceProvider.GetService(item));
            }
        }
        /// <summary>
        /// 开启websockt
        /// </summary>
        public void Websocket()
        {
            server = new();
            server.Start(config.Websocket.BindIp, config.Websocket.Port);
            server.OnMessage = (connection, frame, message) =>
            {
                var req = message.DeJson<ClientServiceRequestInfo>();
                OnMessage(req).ContinueWith((result) =>
                {
                    var resp = result.Result.ToJson().ToBytes();
                    connection.SendFrameText(resp);
                });
            };
        }

        /// <summary>
        /// 获取插件配置列表
        /// </summary>
        /// <param name="className"></param>
        /// <returns></returns>
        public IClientConfigure GetConfigure(string className)
        {
            settingPlugins.TryGetValue(className, out IClientConfigure plugin);
            return plugin;
        }
        /// <summary>
        /// 获取某个插件配置
        /// </summary>
        /// <returns></returns>
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
        /// <summary>
        /// 获取服务列表
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> GetServices()
        {
            return plugins.Select(c => c.Value.Target.GetType().Name).Distinct();
        }

        /// <summary>
        /// 发送通知
        /// </summary>
        /// <param name="resp"></param>
        public void Notify(ClientServiceResponseInfo resp)
        {
            byte[] msg = resp.ToJson().ToBytes();
            foreach (var item in server.Connections)
            {
                item.SendFrameText(msg);
            }
        }

        /// <summary>
        /// 收到消息
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
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
                    Content = model.Content
                };
                dynamic resultAsync = plugin.Method.Invoke(plugin.Target, new object[] { param });
                object resultObject = null;
                if (plugin.IsVoid == false)
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
                    Code = param.Code,
                    Content = param.Code != ClientServiceResponseCodes.Error ? resultObject : param.ErrorMessage,
                    RequestId = model.RequestId,
                    Path = model.Path,
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
        /// <summary>
        /// 开启具名管道
        /// </summary>
        public void NamedPipe()
        {
            PipelineServer pipelineServer = new PipelineServer(pipeName, (string message) =>
            {
                return (OnMessage(message.DeJson<ClientServiceRequestInfo>()).Result).ToJson();
            });
            pipelineServer.BeginAccept();
        }
    }

    /// <summary>
    /// 前段接口缓存
    /// </summary>
    public struct PluginPathCacheInfo
    {
        /// <summary>
        /// 对象
        /// </summary>
        public object Target { get; set; }
        /// <summary>
        /// 方法
        /// </summary>
        public MethodInfo Method { get; set; }
        /// <summary>
        /// 是否void
        /// </summary>
        public bool IsVoid { get; set; }
        /// <summary>
        /// 是否task
        /// </summary>
        public bool IsTask { get; set; }
        /// <summary>
        /// 是否task result
        /// </summary>
        public bool IsTaskResult { get; set; }
    }
}
