using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace client.service.hijack
{
    public interface IHijackPlatform
    {
        public bool Run();
        public void Kill();
    }

    public sealed class HijackWindows : IHijackPlatform
    {
        public void Kill()
        {
            throw new NotImplementedException();
        }

        public bool Run()
        {
            throw new NotImplementedException();
        }
    }


    public sealed class HijackLinux : IHijackPlatform
    {
        public void Kill()
        {
            throw new NotImplementedException();
        }

        public bool Run()
        {
            throw new NotImplementedException();
        }
    }

    public sealed class HijackMacOs : IHijackPlatform
    {
        public void Kill()
        {
            throw new NotImplementedException();
        }

        public bool Run()
        {
            throw new NotImplementedException();
        }
    }
}
