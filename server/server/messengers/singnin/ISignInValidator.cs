using common.server.model;

namespace server.messengers.singnin
{

    public interface ISignInMiddlewareHandler
    {
        public ISignInMiddlewareHandler Use(SignInMiddleware middle);
        public SignInResultInfo.SignInResultInfoCodes Validate(ref UserInfo user);
        public void Access(SignInCacheInfo cache);
    }

    public abstract class SignInMiddleware
    {
        public SignInMiddleware Next { get; set; }
        public abstract SignInResultInfo.SignInResultInfoCodes Validate(UserInfo user);
        public abstract void Access(SignInCacheInfo cache);
    }


}
