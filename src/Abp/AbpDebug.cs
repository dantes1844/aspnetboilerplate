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
            Debug.WriteLine($"============>>>{DateTime.Now:yyyy-MM-dd HH:mm:ss} 调试信息:");
            Debug.WriteLine($"{message}");
        }
    }
}
