using common.server.model;

namespace server.messengers.singnin
{

    public interface ISignInValidatorHandler
    {
        public void LoadValidator(ISignInValidator validator);
        public void LoadAccess(ISignInAccess access);
        public SignInResultInfo.SignInResultInfoCodes Validate(ref UserInfo user);
        public EnumServiceAccess Access();
    }

    public interface ISignInValidator
    {
        public SignInResultInfo.SignInResultInfoCodes Validate(UserInfo user);
    }

    public interface ISignInAccess
    {
        public EnumServiceAccess Access();
    }
}
