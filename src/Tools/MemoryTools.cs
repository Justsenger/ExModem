using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ExModem.Tools
{
    // 把进程工作集压回系统(空闲/最小化到托盘时调)。.NET/WPF 默认不主动归还工作集,
    // 任务管理器看着虚高;SetProcessWorkingSetSize(-1,-1) 让系统尽量回收物理页,数字立降。
    // 注意:这只是把页换出到备用列表,下次访问会缺页换回——所以只在空闲时压。
    public static class MemoryTools
    {
        [DllImport("kernel32.dll")]
        private static extern bool SetProcessWorkingSetSize(IntPtr proc, IntPtr min, IntPtr max);

        public static void Trim()
        {
            try
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                SetProcessWorkingSetSize(Process.GetCurrentProcess().Handle, (IntPtr)(-1), (IntPtr)(-1));
            }
            catch { }
        }
    }
}
