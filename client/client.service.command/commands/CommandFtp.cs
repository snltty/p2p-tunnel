using System;
using System.Collections.Generic;
using System.CommandLine;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;

namespace client.service.command.commands
{
    internal class CommandFtp : CommandBase
    {
        public CommandFtp(RootCommand rootCommand)
        {
            Command ftp = new Command("ftp", "文件传输相关命令") { };
            rootCommand.Add(ftp);


            Command list = new Command("list", "文件列表") { };
            ftp.Add(list);
            Argument<string> listpath = new Argument<string>("path", description: "相对路径或绝对路径,为空则当前目录", getDefaultValue: () => string.Empty);
            list.Add(listpath);
            Argument<int> listid = new Argument<int>("id", description: "目标客户端,0本地", getDefaultValue: () => 0);
            list.Add(listid);
            list.SetHandler(HandlerList, listpath, listid);

            Command info = new Command("info", "上传下载列表") { };
            ftp.Add(info);
            info.SetHandler(HandlerInfo);


            Command create = new Command("create", "创建文件夹") { };
            ftp.Add(create);
            Argument<string> createpath = new Argument<string>("path", description: "目录名");
            list.Add(createpath);
            Argument<int> createid = new Argument<int>("id", description: "目标客户端,0本地", getDefaultValue: () => 0);
            list.Add(createid);
            list.SetHandler((path, id) =>
            {
                string remotePath = id == 0 ? "ftp/LocalCreate" : "ftp/RemoteCreate";
                string data = id == 0 ? path : new { ID = id, Path = path }.ToJsonPipeline();
                JsonNode res = JsonNode.Parse(Request(remotePath, data));
                PrintRequestState(res);

            }, createpath, createid);


            Command delete = new Command("delete", "删除文件/文件夹") { };
            ftp.Add(delete);
            Argument<string> deletepath = new Argument<string>("path", description: "目录名");
            list.Add(deletepath);
            Argument<int> deleteid = new Argument<int>("id", description: "目标客户端,0本地", getDefaultValue: () => 0);
            list.Add(deleteid);
            list.SetHandler((path, id) =>
            {
                string remotePath = id == 0 ? "ftp/LocalDelete" : "ftp/RemoteDelete";
                string data = id == 0 ? path : new { ID = id, Path = path }.ToJsonPipeline();
                JsonNode res = JsonNode.Parse(Request(remotePath, data));
                PrintRequestState(res);

            }, deletepath, deleteid);


            Command upload = new Command("upload", "上传文件") { };
            ftp.Add(upload);
            Argument<int> connectId = new Argument<int>("id", "目标客户端id");
            upload.Add(connectId);
            Argument<string> path = new Argument<string>("source", "文件或文件夹路径");
            upload.Add(path);
            Argument<string> targetPath = new Argument<string>("target", description: "上传到目标客户端目录,空则上传到目标当前目录", getDefaultValue: () => string.Empty);
            upload.Add(targetPath);
            upload.SetHandler((connectId, path, targetPath) =>
            {
                JsonNode res = JsonNode.Parse(Request("ftp/upload", new { ID = connectId, Path = path, TargetPath = targetPath }.ToJsonPipeline()));
                PrintRequestState(res);
            }, connectId, path, targetPath);


            Command download = new Command("download", "下载文件") { };
            ftp.Add(download);
            Argument<int> downloadConnectId = new Argument<int>("id", "目标客户端id");
            upload.Add(downloadConnectId);
            Argument<string> downloadPath = new Argument<string>("source", "文件或文件夹路径");
            upload.Add(downloadPath);
            Argument<string> downloadTargetPath = new Argument<string>("target", description: "下载到本地目录,空则下载到本地当前目录", getDefaultValue: () => string.Empty);
            upload.Add(downloadTargetPath);
            upload.SetHandler((connectId, path, targetPath) =>
            {
                JsonNode res = JsonNode.Parse(Request("ftp/download", new { ID = connectId, Path = path, TargetPath = targetPath }.ToJsonPipeline()));
                PrintRequestState(res);
            }, downloadConnectId, downloadPath, downloadTargetPath);

        }

        private void HandlerList(string listpath, int listid)
        {
            string filepath = listpath, path = string.Empty, data = string.Empty;
            bool isLocal = listid == 0;

            if (Path.IsPathRooted(listpath))
            {
                path = isLocal ? "SetLocalPath" : "SetRemotePath";
                data = isLocal ? filepath : new { ID = listid, Path = filepath }.ToJsonPipeline();
                JsonNode.Parse(Request($"ftp/{path}", data));
                filepath = string.Empty;
            }

            path = isLocal ? "LocalList" : "RemoteList";
            data = isLocal ? filepath : new { ID = listid, Path = filepath }.ToJsonPipeline();


            JsonNode res = JsonNode.Parse(Request($"ftp/{path}", data));
            if (res.Root["Code"].GetValue<int>() == 0)
            {
                var filesData = isLocal ? res.Root["Content"]["Data"].AsArray() : res.Root["Content"].AsArray();

                var files = new List<List<object>> {
                            new List<object>{"名称","大小"}
                        }.Concat(filesData.Select(c => new List<object> {
                                    c["Name"].ToString() ,
                                    c["Length"].ToString() ,
                                }).ToList()).ToList();
                PrintTable(files);
            }
            else
            {
                Console.WriteLine(res.Root["Content"].GetValue<string>());
            }

        }
        private void HandlerInfo()
        {
            var pos = Console.GetCursorPosition();
            while (true)
            {
                JsonNode res = JsonNode.Parse(Request("ftp/info"));
                if (res.Root["Code"].GetValue<int>() == 0)
                {
                    var uploads = res.Root["Content"]["Uploads"].AsArray();
                    var downloads = res.Root["Content"]["Downloads"].AsArray();

                    int uploadLast = uploads.Where(c => c["State"].GetValue<int>() == (int)UploadStates.Wait).Count();
                    var uploadTable = new List<List<object>> {
                            new List<object>{ "文件名", "文件大小", "已上传", "速度" }
                        }.Concat(uploads.Where(c => c["State"].GetValue<int>() == (int)UploadStates.Uploading).Select(c => new List<object> {
                            c["FileName"].ToString(),
                            SizeFormat(double.Parse(c["TotalLength"].ToString())),
                            SizeFormat(double.Parse(c["IndexLength"].ToString())),
                            SizeFormat(double.Parse(c["Speed"].ToString()))
                        })).ToList();

                    int downloadLast = downloads.Where(c => c["State"].GetValue<int>() == (int)UploadStates.Wait).Count();
                    var downloadTable = new List<List<object>> {
                            new List<object>{ "文件名", "文件大小", "已下载", "速度" }
                        }.Concat(downloads.Where(c => c["State"].GetValue<int>() == (int)UploadStates.Uploading).Select(c => new List<object> {
                            c["FileName"].ToString(),
                            SizeFormat(double.Parse(c["TotalLength"].ToString())),
                            SizeFormat(double.Parse(c["IndexLength"].ToString())),
                            SizeFormat(double.Parse(c["Speed"].ToString()))
                        })).ToList();

                    Console.SetCursorPosition(pos.Left, pos.Top);
                    PrintTable(uploadTable);
                    Console.WriteLine($"上传等待中:{uploadLast}");
                    Console.WriteLine();
                    Console.WriteLine();
                    PrintTable(downloadTable);
                    Console.WriteLine($"下载等待中:{downloadLast}");

                    Console.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                }
                else
                {
                    PrintRequestState(res);
                    break;
                }
                System.Threading.Thread.Sleep(1000);
            }
        }

        private string[] sizeUnits = new string[] { "B", "KB", "MB", "GB", "TB" };
        private string SizeFormat(double size)
        {
            int i;
            for (i = 0; i < sizeUnits.Length; i++)
            {
                if (size < 1024)
                {
                    break;
                }
                size /= 1024;
            }
            return $"{size:N2}{sizeUnits[i]}";
        }
    }

    [Flags]
    internal enum UploadStates : byte
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


    [Flags]
    public enum FileType : byte
    {
        [Description("文件夹")]
        Folder = 0,
        [Description("文件")]
        File = 1
    }
}
