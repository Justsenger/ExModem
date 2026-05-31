using System.Reflection;

namespace ExModem.Tools
{
    public static class Utils
    {
        // 版本/作者全部从项目文件(csproj)生成的程序集元数据读取,改一处即可。
        //   Version  ← <Version>(AssemblyInformationalVersion)
        //   Author   ← <Company>(AssemblyCompany)
        private static readonly Assembly Asm = Assembly.GetExecutingAssembly();

        public static string Version
        {
            get
            {
                var info = Asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
                // InformationalVersion 可能带 "+commit" 后缀,去掉
                if (!string.IsNullOrEmpty(info))
                {
                    int plus = info.IndexOf('+');
                    return plus >= 0 ? info.Substring(0, plus) : info;
                }
                return Asm.GetName().Version?.ToString() ?? "1.0";
            }
        }

        public static string Author
            => Asm.GetCustomAttribute<AssemblyCompanyAttribute>()?.Company is { Length: > 0 } c ? c : "Justsenger";

        // 带 V 前缀,给 banner 之类直接显示用
        public static string VersionDisplay => "V" + Version;
    }
}
