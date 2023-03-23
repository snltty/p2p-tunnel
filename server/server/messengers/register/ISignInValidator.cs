using common.server.model;

namespace server.messengers.singnin
{
    public interface ISignInValidator
    {
        public (SignInResultInfo.SignInResultInfoCodes, string) Validate(ref UserInfo user);
    }


    public abstract class SignInMiddleware
    {
        public abstract string Validate(UserInfo user);
    }

    public class SignInMiddlewareWrap
    {
        public SignInMiddleware Current { get; set; }
        public SignInMiddlewareWrap Next { get; set; }
    }

    public class SignInMiddlewareHandler
    {
        SignInMiddlewareWrap last;

        public SignInMiddlewareHandler Use(SignInMiddleware middle)
        {
            if (last == null)
            {
                last = new SignInMiddlewareWrap { Current = middle };
            }
            else
            {
                last.Next = new SignInMiddlewareWrap { Current = middle };
                last = last.Next;
            }
            return this;
        }

        public string Execute(UserInfo user)
        {
            SignInMiddlewareWrap current = last;
            while (current != null)
            {
                string res = current.Current.Validate(user);
                if (!string.IsNullOrWhiteSpace(res))
                {
                    return res;
                }
                current = current.Next;
            }

            return string.Empty;
        }
    }
}
