using common.server.model;

namespace server.messengers.singnin
{
    public interface ISignInValidator
    {
        public SignInResultInfo.SignInResultInfoCodes Validate(ref UserInfo user);
    }
}
