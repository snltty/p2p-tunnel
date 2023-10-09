using common.server;
using common.server.model;
using System.Collections.Generic;
using System.Reflection;

namespace server.messengers.signin
{

    public interface ISignInValidatorHandler
    {
        public void LoadValidator(Assembly[] assemblys);
        /// <summary>
        /// 登入前验证
        /// </summary>
        /// <param name="model"></param>
        /// <param name="access"></param>
        /// <returns></returns>
        public SignInResultInfo.SignInResultInfoCodes Validate(SignInParamsInfo model, ref uint access);
        /// <summary>
        /// 登入后
        /// </summary>
        /// <param name="cache"></param>
        public void Validated(SignInCacheInfo cache);
    }

    public interface ISignInValidator
    {
        /// <summary>
        /// 执行顺序，升序
        /// </summary>
        public EnumSignInValidatorOrder Order { get; }
        /// <summary>
        /// 登入前验证
        /// </summary>
        /// <param name="args"></param>
        /// <param name="access"></param>
        /// <returns></returns>
        public SignInResultInfo.SignInResultInfoCodes Validate(Dictionary<string, string> args, ref uint access);
        /// <summary>
        /// 登入成功后
        /// </summary>
        /// <param name="cache"></param>
        public void Validated(SignInCacheInfo cache);
    }

    public enum EnumSignInValidatorOrder : byte
    {
        None = 0,
        Level1 = 1,
        Level2 = 2,
        Level3 = 3,
        Level4 = 4,
        Level5 = 5,
        Level6 = 6,
        Level7 = 7,
        Level8 = 8,
        Level9 = 9,
    }
}
