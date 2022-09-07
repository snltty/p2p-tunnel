using client.service.ui.api.clientServer;
using common.libs;
using common.libs.extends;
using System;
using System.Collections.Generic;
using System.Linq;

namespace client.service.logger
{
    public class LoggerClientService : IClientService
    {
        public List<LoggerModel> Data { get; } = new List<LoggerModel>();

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
                PageIndex = model.PageIndex,
                PageSize = model.PageSize,
                Count = Data.Count(),
                Data = res.Skip((model.PageIndex - 1) * model.PageSize).Take(model.PageSize)
            };
        }

        public void Clear(ClientServiceParamsInfo arg)
        {
            Data.Clear();
        }
    }

    public class PageParamsInfo
    {
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int Type { get; set; } = -1;
    }
    public class PageInfo
    {
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int Count { get; set; } = 10;
        public IEnumerable<LoggerModel> Data { get; set; } = Array.Empty<LoggerModel>();
    }

}
