using common.server.model;
using System.Threading.Tasks;

namespace client.messengers.signin
{
    public interface ISignInTransfer
    {
        void Exit();
        Task<CommonTaskResponseInfo<bool>> SignIn();
    }
}
