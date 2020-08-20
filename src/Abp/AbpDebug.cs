using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Abp
{
    public static class AbpDebug
    {
        public static void WriteLine(string message)
        {
            Debug.WriteLine($"===>>>调试信息:{message}");
        }
    }
}
