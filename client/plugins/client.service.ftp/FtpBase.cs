using client.service.ftp.extends;
using client.service.ftp.commands;
using common.libs;
using common.libs.extends;
using Microsoft.Extensions.DependencyInjection;
using common.server;
using common.server.model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using client.messengers.clients;

namespace client.service.ftp
{
    public class FtpBase
    {
        public Dictionary<FtpCommand, IFtpCommandPluginBase> Plugins { get; } = new Dictionary<FtpCommand, IFtpCommandPluginBase>();

        protected virtual string SocketPath { get; set; }
        protected virtual string RootPath { get; set; }

        private FileSaveManager Downloads { get; } = new();
        private FileSaveManager Uploads { get; } = new();
        private NumberSpace fileIdNs = new NumberSpace(0);

        protected readonly MessengerSender messengerSender;
        protected readonly Config config;
        private readonly IClientInfoCaching clientInfoCaching;
        private readonly ServiceProvider serviceProvider;
        private readonly WheelTimer<FileSaveInfo> wheelTimer = new WheelTimer<FileSaveInfo>();

        protected FtpBase(ServiceProvider serviceProvider, MessengerSender messengerSender, Config config, IClientInfoCaching clientInfoCaching)
        {
            this.messengerSender = messengerSender;
            this.config = config;
            this.clientInfoCaching = clientInfoCaching;
            this.serviceProvider = serviceProvider;

            clientInfoCaching.OnOffline.Sub((client) =>
            {
                Downloads.Clear(client.Id);
                Uploads.Clear(client.Id);
            });
        }

        protected void LoadPlugins(Assembly[] assemblys, Type type)
        {
            foreach (Type item in ReflectionHelper.GetInterfaceSchieves(assemblys, type))
            {
                IFtpCommandPluginBase obj = (IFtpCommandPluginBase)serviceProvider.GetService(item);
                if (!Plugins.ContainsKey(obj.Cmd))
                {
                    Plugins.TryAdd(obj.Cmd, obj);
                }
            }
        }

        public IEnumerable<FileSaveInfo> GetUploads()
        {
            return Uploads.Caches.Values.SelectMany(c => c.Values);
        }
        public IEnumerable<FileSaveInfo> GetDownloads()
        {
            return Downloads.Caches.Values.SelectMany(c => c.Values);
        }

        protected List<string> Create(string currentPath, string path)
        {
            return path.CreateDir(currentPath, RootPath);
        }
        protected List<string> Delete(string currentPath, string path)
        {
            return path.ClearDir(currentPath, RootPath);
        }

        protected async Task Upload(string currentPath, string paths, ClientInfo client, string targetCurrentPath = "")
        {
            await Upload(currentPath, paths.Split(Helper.SeparatorChar), client, targetCurrentPath);
        }
        protected async Task Upload(string currentPath, IEnumerable<string> paths, ClientInfo client, string targetCurrentPath = "")
        {
            await Task.Run(async () =>
            {
                if (string.IsNullOrWhiteSpace(targetCurrentPath))
                {
                    targetCurrentPath = await GetRemoteCurrentPath(client).ConfigureAwait(false);
                }
                if (string.IsNullOrWhiteSpace(targetCurrentPath))
                {
                    Logger.Instance.Error($" Upload fail,empty target path");
                    return;
                }
                foreach (string item in paths)
                {
                    if (!string.IsNullOrWhiteSpace(item))
                    {
                        string filepath = item;
                        if (!Path.IsPathRooted(filepath))
                        {
                            filepath = Path.Combine(currentPath, item);
                        }

                        if (Directory.Exists(filepath))
                        {
                            List<FileUploadInfo> files = new();
                            GetFiles(files, new DirectoryInfo(filepath));
                            foreach (FileUploadInfo file in files.Where(c => c.Type == FileType.File))
                            {
                                AppendUpload(file.Path, targetCurrentPath, client);
                            }
                        }
                        else if (File.Exists(filepath))
                        {
                            AppendUpload(filepath, targetCurrentPath, client);
                        }
                    }
                }
            }).ConfigureAwait(false);
        }
        private void AppendUpload(string path, string targetCurrentPath, ClientInfo client)
        {
            System.IO.FileInfo file = new System.IO.FileInfo(path);
            try
            {
                //同时上传重复的文件
                if (Uploads.Contains(client.Id, file.FullName))
                {
                    return;
                }

                Uploads.Add(new FileSaveInfo
                {
                    FullName = file.FullName,
                    IndexLength = 0,
                    TotalLength = file.Length,
                    Md5 = fileIdNs.Increment(),
                    ClientId = client.Id,
                    State = UploadStates.Wait,
                    CacheFullName = Path.Join(targetCurrentPath, file.Name)
                });
                WaitToUpload();
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($" Upload {ex}");
            }
        }
        private void Upload(FileSaveInfo save)
        {
            if (save == null) return;

            clientInfoCaching.Get(save.ClientId, out ClientInfo client);

            FtpFileCommand cmd = new FtpFileCommand
            {
                Md5 = save.Md5,
                Size = save.TotalLength,
                FullName = save.CacheFullName
            };
            save.State = UploadStates.Uploading;
            int packSize = config.SendPacketSize; //每个包大小
            int packCount = (int)(save.TotalLength / packSize);
            int lastPackSize = (int)(save.TotalLength - (packCount * packSize));

            save.Timeout = wheelTimer.NewTimeout(new WheelTimerTimeoutTask<FileSaveInfo>
            {
                Callback = Timeout,
                State = save,
            }, 1000, true);

            IConnection connection = client.OnlineConnection;
            Task.Run(async () =>
            {
                try
                {
                    cmd.ToBytes();
                    byte[] sendData = new byte[cmd.MetaData.Length + packSize];
                    byte[] readData = new byte[packSize];

                    using FileStream fs = new FileStream(save.FullName, FileMode.Open, FileAccess.Read, FileShare.Read, config.ReadWriteBufferSize, FileOptions.SequentialScan);

                    int index = 0;
                    while (index < packCount)
                    {
                        if (!save.Check())
                        {
                            return;
                        }

                        fs.Read(readData, 0, readData.Length);
                        cmd.WriteData(readData, sendData);
                        bool res = await SendOnlyTcp(sendData, connection).ConfigureAwait(false);
                        if (res == false)
                        {
                            save.State = UploadStates.Error;
                        }
                        save.IndexLength += packSize;

                        index++;
                    }
                    if (!save.Check())
                    {
                        return;
                    }
                    if (lastPackSize > 0)
                    {
                        sendData = new byte[cmd.MetaData.Length + lastPackSize];
                        readData = new byte[lastPackSize];
                        fs.Read(readData, 0, readData.Length);
                        cmd.WriteData(readData, sendData);
                        if (!await SendOnlyTcp(sendData, connection).ConfigureAwait(false))
                        {
                            save.State = UploadStates.Error;
                        }
                        save.IndexLength += lastPackSize;
                    }
                    fs.Close();
                    fs.Dispose();
                    WaitToUpload();
                }
                catch (Exception ex)
                {
                    Logger.Instance.Debug(ex);
                    save.Disponse();
                }
            }, save.Token.Token);
        }

        protected async ValueTask OnFile(FtpFileCommand cmd, FtpPluginParamWrap wrap)
        {
            try
            {
                FileSaveInfo fs = Downloads.Get(wrap.Client.Id, cmd.Md5);
                if (fs == null)
                {
                    string dir = Path.GetDirectoryName(cmd.FullName);
                    if (!Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }

                    fs = new FileSaveInfo
                    {
                        Stream = null,
                        IndexLength = 0,
                        TotalLength = cmd.Size,
                        FullName = cmd.FullName,
                        CacheFullName = Path.Combine(dir, $"{cmd.Md5}.downloading"),
                        ClientId = wrap.Client.Id,
                        Md5 = cmd.Md5,
                        State = UploadStates.Wait
                    };
                    fs.Timeout = wheelTimer.NewTimeout(new WheelTimerTimeoutTask<FileSaveInfo>
                    {
                        State = fs,
                        Callback = Timeout
                    }, 1000, true);
                    Downloads.Add(fs);
                }
                else if (fs.Token.IsCancellationRequested)
                {
                    return;
                }
                if (cmd.ReadData.Length == 0)
                {
                    return;
                }

                if (fs.Stream == null)
                {
                    fs.CacheFullName.TryDeleteFile();
#if NET5_0
                    fs.Stream = new FileStream(fs.CacheFullName,FileMode.Create,FileAccess.Write,FileShare.Read, config.ReadWriteBufferSize,false);
                    //string path, FileMode mode, FileAccess access, FileShare share, int bufferSize, FileOptions options
#else
                    fs.Stream = new FileStream(fs.CacheFullName, new FileStreamOptions
                    {
                        Mode = FileMode.Create,
                        Access = FileAccess.Write,
                        Share = FileShare.Read,
                        BufferSize = config.ReadWriteBufferSize,
                        PreallocationSize = cmd.Size,
                        Options = FileOptions.SequentialScan
                    });
#endif

                    fs.Stream.Seek(cmd.Size - 1, SeekOrigin.Begin);
                    fs.Stream.WriteByte(new byte());
                    fs.Stream.Seek(0, SeekOrigin.Begin);
                }

                fs.Stream.Write(cmd.ReadData.Span);
                fs.IndexLength += cmd.ReadData.Length;

                if (fs.IndexLength >= cmd.Size)
                {
                    await SendOnlyTcp(new FtpFileEndCommand { Md5 = cmd.Md5 }, wrap.Connection.FromConnection).ConfigureAwait(false);
                    fs.Stream.Flush();
                    Downloads.Remove(wrap.Client.Id, cmd.Md5);
                    File.Move(fs.CacheFullName, fs.FullName, true);
                }
            }
            catch (Exception ex)
            {
                await SendOnlyTcp(new FtpFileErrorCommand { Md5 = cmd.Md5, Msg = ex.Message }, wrap.Connection.FromConnection).ConfigureAwait(false);
                Downloads.Remove(wrap.Client.Id, cmd.Md5, true);
                Logger.Instance.Error(ex);
            }
        }
        public void OnFileEnd(FtpFileEndCommand cmd, FtpPluginParamWrap wrap)
        {
            Uploads.Remove(wrap.Client.Id, cmd.Md5);
        }
        public void OnFileError(FtpFileErrorCommand cmd, FtpPluginParamWrap wrap)
        {
            Uploads.Remove(wrap.Client.Id, cmd.Md5);
        }
        public async Task OnFileUploadCancel(FtpCancelCommand cmd, FtpPluginParamWrap wrap)
        {
            await OnFileUploadCancel(cmd, wrap.Client).ConfigureAwait(false);
        }
        public async Task OnFileUploadCancel(FtpCancelCommand cmd, ClientInfo client)
        {
            Uploads.Remove(client.Id, cmd.Md5);
            await SendOnlyTcp(new FtpCanceledCommand { Md5 = cmd.Md5 }, client).ConfigureAwait(false);
        }

        public void OnFileUploadCanceled(FtpCanceledCommand cmd, FtpPluginParamWrap wrap)
        {
            Downloads.Remove(wrap.Client.Id, cmd.Md5, true);
        }

        public async Task<FtpResultInfo> RemoteCreate(string path, ClientInfo client)
        {
            MessageResponeInfo resp = await SendReplyTcp(new FtpCreateCommand { Path = path }, client).ConfigureAwait(false);
            return FtpResultInfo.FromBytes(resp.Data);
        }
        public async Task<FtpResultInfo> RemoteCancel(ulong md5, ClientInfo client)
        {
            MessageResponeInfo resp = await SendReplyTcp(new FtpCancelCommand { Md5 = md5 }, client).ConfigureAwait(false);
            return FtpResultInfo.FromBytes(resp.Data);
        }
        public async Task<FtpResultInfo> RemoteDelete(string path, ClientInfo client)
        {
            MessageResponeInfo resp = await SendReplyTcp(new FtpDelCommand { Path = path }, client).ConfigureAwait(false);
            return FtpResultInfo.FromBytes(resp.Data);
        }
        public async Task<string> GetRemoteCurrentPath(ClientInfo client)
        {
            MessageResponeInfo resp = await SendReplyTcp(new FtpCurrentPathCommand { }, client).ConfigureAwait(false);
            if (resp.Code == MessageResponeCodes.OK)
            {
                return FtpResultInfo.FromBytes(resp.Data).ReadData.Span.GetString();
            }

            return string.Empty;
        }
        public async Task<FtpResultInfo> SetRemoteCurrentPath(string path, ClientInfo client)
        {
            MessageResponeInfo resp = await SendReplyTcp(new FtpSetCurrentPathCommand { Path = path }, client).ConfigureAwait(false);
            return FtpResultInfo.FromBytes(resp.Data);
        }

        protected async Task<bool> SendOnlyTcp(IFtpCommandBase data, ClientInfo client)
        {
            return await SendOnlyTcp(data, client.OnlineConnection).ConfigureAwait(false);
        }
        protected async Task<bool> SendOnlyTcp(IFtpCommandBase data, IConnection connection)
        {
            return await SendOnlyTcp(data.ToBytes(), connection).ConfigureAwait(false);
        }
        protected async Task<bool> SendOnlyTcp(byte[] data, IConnection connection)
        {
            return await messengerSender.SendOnly(new MessageRequestWrap
            {
                Memory = data,
                Path = SocketPath,
                Connection = connection
            }).ConfigureAwait(false);
        }
        protected async Task<MessageResponeInfo> SendReplyTcp(IFtpCommandBase data, ClientInfo client)
        {
            return await messengerSender.SendReply(new MessageRequestWrap
            {
                Memory = data.ToBytes(),
                Path = SocketPath,
                Connection = client.OnlineConnection,
            }).ConfigureAwait(false);
        }

        private void GetFiles(List<FileUploadInfo> files, DirectoryInfo path)
        {
            files.Add(new FileUploadInfo { Path = path.FullName, Type = FileType.Folder });
            foreach (var dir in path.GetDirectories())
            {
                GetFiles(files, dir);
            }
            files.AddRange(path.GetFiles().Select(c => new FileUploadInfo
            {
                Path = c.FullName,
                Type = FileType.File
            }));
        }

        private void Timeout(WheelTimerTimeout<FileSaveInfo> task)
        {
            if (task.Task.State.TotalLength > 0 && task.Task.State.IndexLength > 0)
            {
                task.Task.State.Speed = (task.Task.State.IndexLength - task.Task.State.LastLength);
                task.Task.State.LastLength = task.Task.State.IndexLength;
            }
        }
        private void WaitToUpload()
        {
            if (!Uploads.Caches.IsEmpty)
            {
                IEnumerable<FileSaveInfo> saves = Uploads.Caches.SelectMany(c => c.Value.Values);
                int uploadCount = saves.Count(c => c.State == UploadStates.Uploading);
                int waitCount = saves.Count(c => c.State == UploadStates.Wait);
                if (waitCount > 0 && uploadCount < config.UploadNum)
                {
                    Upload(saves.FirstOrDefault(c => c.State == UploadStates.Wait));
                }
            }
        }
    }

    [Flags]
    public enum FileType : byte
    {
        [Description("文件夹")]
        Folder = 0,
        [Description("文件")]
        File = 1
    }


    public class FileInfoWrap
    {
        public FileInfo[] Data { get; set; } = Array.Empty<FileInfo>();

        public byte[] ToBytes()
        {
            int length = 0, dataLength = Data.Length;
            byte[][] dataBytes = new byte[dataLength][];
            for (int i = 0; i < dataBytes.Length; i++)
            {
                dataBytes[i] = Data[i].ToBytes();
                length += dataBytes[i].Length;

            }

            int index = 0;
            var lengthBytes = dataLength.ToBytes();
            length += lengthBytes.Length;

            var bytes = new byte[length];

            Array.Copy(lengthBytes, 0, bytes, index, lengthBytes.Length);
            index += lengthBytes.Length;

            for (int i = 0; i < dataBytes.Length; i++)
            {
                Array.Copy(dataBytes[i], 0, bytes, index, dataBytes[i].Length);
                index += dataBytes[i].Length;
            }
            return bytes;

        }

        public void DeBytes(ReadOnlyMemory<byte> data)
        {
            var span = data.Span;
            int index = 0;

            int length = span.Slice(index, 4).ToInt32();
            index += 4;

            Data = new FileInfo[length];
            for (int i = 0; i < length; i++)
            {
                Data[i] = new FileInfo();
                int tempIndex = Data[i].DeBytes(data.Slice(index));
                index += tempIndex;
            }
        }
    }

    public class FileInfo
    {
        public DateTime LastAccessTime { get; set; }
        public DateTime CreationTime { get; set; }
        public DateTime LastWriteTime { get; set; }
        public string Name { get; set; }
        public long Length { get; set; }
        public FileType Type { get; set; }

        public byte[] ToBytes()
        {
            var nameBytes = Name.ToBytes();
            var lengthBytes = Length.ToBytes();
            var lastWriteBytes = LastWriteTime.Ticks.ToBytes();
            var createBytes = CreationTime.Ticks.ToBytes();
            var lastAccessBytes = LastAccessTime.Ticks.ToBytes();
            var bytes = new byte[1 + 8 + 1 + nameBytes.Length + 8 + 8 + 8];

            int index = 0;

            bytes[index] = (byte)Type;
            index += 1;

            Array.Copy(lengthBytes, 0, bytes, index, lengthBytes.Length);
            index += lengthBytes.Length;

            bytes[index] = (byte)nameBytes.Length;
            Array.Copy(nameBytes, 0, bytes, index + 1, nameBytes.Length);
            index += 1 + nameBytes.Length;

            Array.Copy(lastWriteBytes, 0, bytes, index, lastWriteBytes.Length);
            index += 8;

            Array.Copy(createBytes, 0, bytes, index, createBytes.Length);
            index += 8;

            Array.Copy(lastAccessBytes, 0, bytes, index, lastAccessBytes.Length);
            index += 8;

            return bytes;
        }
        public int DeBytes(ReadOnlyMemory<byte> data)
        {
            var span = data.Span;
            int index = 0;

            Type = (FileType)span[index];
            index += 1;

            Length = span.Slice(index, 8).ToInt64();
            index += 8;

            Name = span.Slice(index + 1, span[index]).GetString();
            index += 1 + span[index];

            LastWriteTime = new DateTime(span.Slice(index, 8).ToInt64());
            index += 8;

            CreationTime = new DateTime(span.Slice(index, 8).ToInt64());
            index += 8;

            LastAccessTime = new DateTime(span.Slice(index, 8).ToInt64());
            index += 8;

            return index;

        }

    }

    public struct FileUploadInfo
    {
        public string Path { get; set; }
        public FileType Type { get; set; }
    }

    [Flags]
    public enum UploadStates : byte
    {
        [Description("等待中")]
        Wait = 0,
        [Description("上传中")]
        Uploading = 1,
        [Description("已取消")]
        Canceled = 2,
        [Description("出错")]
        Error = 3
    }

    public class FileSaveInfo
    {

        public ulong Md5 { get; set; }
        public long TotalLength { get; set; }
        public long IndexLength { get; set; } = 0;
        /// <summary>
        /// 本地文件完整路径
        /// </summary>
        [JsonIgnore]
        public string FullName { get; set; } = string.Empty;
        public string FileName
        {
            get
            {
                return Path.GetFileName(FullName);
            }
        }

        public long Speed { get; set; } = 0;
        public UploadStates State { get; set; } = UploadStates.Wait;

        [JsonIgnore]
        public FileStream Stream { get; set; }
        [JsonIgnore]
        public ulong ClientId { get; set; }
        /// <summary>
        /// 缓存文件名，客户端表示为上传到的远程的路径，服务端表示为临时文件名
        /// </summary>
        [JsonIgnore]
        public string CacheFullName { get; set; } = string.Empty;
        [JsonIgnore]
        public long LastLength { get; set; } = 0;

        [JsonIgnore]
        public CancellationTokenSource Token { get; set; }

        [JsonIgnore]
        public WheelTimerTimeout<FileSaveInfo> Timeout { get; set; }

        bool disposed = false;
        public void Disponse(bool deleteFile = false)
        {
            if (disposed) return;
            disposed = true;

            if (Token != null)
            {
                Token.Cancel();
                Token = null;
            }

            if (Stream != null)
            {
                Stream.Dispose();
                Stream = null;
            }

            if (deleteFile)
            {
                CacheFullName.TryDeleteFile();
            }

            Timeout?.Cancel();
            GCHelper.Gc(this);
        }

        public bool Check()
        {
            if (disposed) return false;
            if (Token.IsCancellationRequested || State != UploadStates.Uploading)
            {
                Disponse();
                return false;
            }
            return true;
        }
    }

    public class FileSaveManager
    {
        public ConcurrentDictionary<ulong, ConcurrentDictionary<ulong, FileSaveInfo>> Caches { get; } = new();

        public FileSaveInfo Get(ulong clientId, ulong md5)
        {
            Caches.TryGetValue(clientId, out ConcurrentDictionary<ulong, FileSaveInfo> ipDic);
            if (ipDic != null)
            {
                ipDic.TryGetValue(md5, out FileSaveInfo fs);
                return fs;
            }
            return null;
        }

        public void Add(FileSaveInfo info)
        {
            Caches.TryGetValue(info.ClientId, out ConcurrentDictionary<ulong, FileSaveInfo> ipDic);
            if (ipDic == null)
            {
                ipDic = new ConcurrentDictionary<ulong, FileSaveInfo>();
                Caches.TryAdd(info.ClientId, ipDic);
            }
            info.Token = new CancellationTokenSource();
            ipDic.AddOrUpdate(info.Md5, info, (a, b) => info);
        }

        public bool Contains(ulong clientId, string fileFullName)
        {
            if (Caches.TryGetValue(clientId, out ConcurrentDictionary<ulong, FileSaveInfo> ipDic))
            {
                return ipDic.Values.FirstOrDefault(c => c.FullName == fileFullName) != null;
            }
            return false;
        }

        public void Remove(ulong clientId, ulong md5, bool deleteFile = false)
        {
            if (Caches.TryGetValue(clientId, out ConcurrentDictionary<ulong, FileSaveInfo> ipDic))
            {
                if (ipDic.TryRemove(md5, out FileSaveInfo save))
                {
                    save.Disponse(deleteFile);
                }
            }
        }

        public void Clear(ulong clientId)
        {
            if (Caches.TryRemove(clientId, out ConcurrentDictionary<ulong, FileSaveInfo> ipDic))
            {
                foreach (var item in ipDic.Values)
                {
                    item.Disponse();
                }
                ipDic.Clear();
            }
        }
    }

    public class FtpPluginParamWrap
    {
        public IConnection Connection { get; set; }
        public ClientInfo Client { get; set; }
    }

    public class FtpResultInfo
    {
        public FtpResultCodes Code { get; set; } = FtpResultCodes.OK;

        public byte[] Data { get; set; } = Helper.EmptyArray;

        public ReadOnlyMemory<byte> ReadData { get; set; }

        [Flags]
        public enum FtpResultCodes : byte
        {
            [Description("成功")]
            OK = 0,
            [Description("禁用了")]
            DISABLE = 1,
            [Description("目录不可空")]
            PATH_REQUIRED = 2,
            [Description("出错了")]
            UNKNOW = 255
        }

        public byte[] ToBytes()
        {
            byte[] result = new byte[1 + Data.Length];
            result[0] = (byte)Code;
            Array.Copy(Data, 0, result, 1, Data.Length);

            return result;
        }

        public static FtpResultInfo FromBytes(ReadOnlyMemory<byte> bytes)
        {
            return new FtpResultInfo
            {
                Code = (FtpResultCodes)bytes.Span[0],
                ReadData = bytes.Slice(1, bytes.Length - 1)
            };
        }
    }
}
