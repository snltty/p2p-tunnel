using client.service.ui.api.clientServer;
using common.libs;
using common.libs.extends;
using System;
using System.Collections.Generic;
using System.Linq;

namespace client.service.logger
{
    /// <summary>
    /// 日志
    /// </summary>
    public sealed class LoggerClientService : IClientService
    {
        /// <summary>
        /// 日志
        /// </summary>
        public List<LoggerModel> Data { get; } = new List<LoggerModel>();

        /// <summary>
        /// 获取日志列表
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        public PageInfo List(ClientServiceParamsInfo arg)
        {
            PageParamsInfo model = arg.Content.DeJson<PageParamsInfo>();

            IEnumerable<LoggerModel> res = Data.OrderByDescending(c => c.Time);
            if (model.Type >= 0)
            {
                res = res.Where(c => c.Type == (LoggerTypes)model.Type);
            }

            return new PageInfo
            {
                Page = model.Page,
                PageSize = model.PageSize,
                Count = Data.Count(),
                Data = res.Skip((model.Page - 1) * model.PageSize).Take(model.PageSize)
            };
        }

        /// <summary>
        /// 清除日志
        /// </summary>
        /// <param name="arg"></param>
        public void Clear(ClientServiceParamsInfo arg)
        {
            Data.Clear();
        }
    }

    /// <summary>
    /// 日志分页参数
    /// </summary>
    public sealed class PageParamsInfo
    {
        /// <summary>
        /// 当前页
        /// </summary>
        public int Page { get; set; } = 1;
        /// <summary>
        /// 页大小
        /// </summary>
        public int PageSize { get; set; } = 10;
        /// <summary>
        /// 日志类型
        /// </summary>
        public int Type { get; set; } = -1;
    }
    /// <summary>
    /// 日志分页返回
    /// </summary>
    public sealed class PageInfo
    {
        /// <summary>
        /// 当前页
        /// </summary>
        public int Page { get; set; } = 1;
        /// <summary>
        /// 页大小
        /// </summary>
        public int PageSize { get; set; } = 10;
        /// <summary>
        /// 总数
        /// </summary>
        public int Count { get; set; } = 10;
        /// <summary>
        /// 数据
        /// </summary>
        public IEnumerable<LoggerModel> Data { get; set; } = Array.Empty<LoggerModel>();
    }

}
