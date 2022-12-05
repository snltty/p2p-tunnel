using common.server.model;
using System.Threading.Tasks;

namespace client.messengers.register
{
    /// <summary>
    /// 注册接口
    /// </summary>
    public interface IRegisterTransfer
    {
        /// <summary>
        /// 退出
        /// </summary>
        void Exit();
        /// <summary>
        /// 注册
        /// </summary>
        /// <param name="autoReg">强制自动注册</param>
        /// <returns></returns>
        Task<CommonTaskResponseInfo<bool>> Register(bool autoReg = false);
    }
}
