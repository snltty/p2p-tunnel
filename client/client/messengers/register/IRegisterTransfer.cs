using common.server.model;
using System.Threading.Tasks;

namespace client.messengers.register
{
    public interface IRegisterTransfer
    {
        void Exit();
        Task<CommonTaskResponseInfo<bool>> Register(bool autoReg = false);
    }
}
