<h1>
  <img src="https://github.com/Justsenger/ExModem/blob/main/img/logo.png?raw=true" width="32" alt="ExModem logo">
  ExModem
</h1>

<div align="center">

**一款图形化的蜂窝模块（Modem）管理工具，轻松收发短信、管理联系人、切换 4G/5G 等功能。**

</div>

<p align="center">
  <a href="https://github.com/Justsenger/ExModem/releases/latest"><img src="https://img.shields.io/github/v/release/Justsenger/ExModem.svg?style=flat-square" alt="Latest release"></a>
  <a href="https://github.com/Justsenger/ExModem/releases"><img src="https://img.shields.io/github/downloads/Justsenger/ExModem/total.svg?style=flat-square&color=brightgreen" alt="Downloads"></a>
  <a href="https://github.com/Justsenger/ExModem/blob/main/LICENSE"><img src="https://img.shields.io/badge/license-GPL--3.0-blue?style=flat-square" alt="License"></a>
</p>

[English](../README.md) | **简体中文** | [繁體中文](README.zh-Hant.md) | [Русский](README.ru.md) | [日本語](README.ja.md)

---

ExModem 使用 Windows 的 [MobileBroadband WinRT API](https://learn.microsoft.com/en-us/uwp/api/windows.networking.networkoperators) 结合高通 [QMI](https://en.wikipedia.org/wiki/Qualcomm_MSM_Interface) 命令，力图将 Windows 上的 Modem 能力开发到极致。

适配环境：**Surface Pro 11 5G + 中国移动校园流量卡 + 4G / 5G 网络环境**。

由于个人时间和精力有限，项目可能存在未经测试的场景或错误。如果您在使用中遇到任何问题，欢迎通过 [Issues](https://github.com/Justsenger/ExModem/issues) 提出！

## 🎨 界面一览

ExModem 使用 [WPF-UI](https://github.com/lepoco/wpfui) 框架，界面现代流畅，支持浅色 / 深色主题并跟随系统自动切换。支持**简体中文、繁體中文、English、Русский、日本語**五种语言，首次启动自动跟随系统语言。

![界面1](https://github.com/Justsenger/ExModem/blob/main/img/01.png?raw=true)
![界面2](https://github.com/Justsenger/ExModem/blob/main/img/02.png?raw=true)
![界面3](https://github.com/Justsenger/ExModem/blob/main/img/03.png?raw=true)
![界面4](https://github.com/Justsenger/ExModem/blob/main/img/04.png?raw=true)
![界面5](https://github.com/Justsenger/ExModem/blob/main/img/05.png?raw=true)

## ✨ 功能

### 📶 网络
查看当前网络、运营商、信号强度等实时状态；一键在 **4G**（可收发短信）与 **5G**（高速上网）之间切换。也可开启**智能自动切换**：空闲时用 4G 保证短信实时，大流量时自动切到 5G 提速。

### 💬 短信
像聊天软件一样收发短信：按号码归整成会话，收发气泡一目了然；自动识别短信里的**验证码**并一键复制；收到新短信弹出系统通知。

### 👤 联系人
统一管理**本机**与 **SIM 卡**里的联系人：新增、修改、删除，可设置头像、按拼音首字母分组；短信页会自动用联系人的名字和头像显示号码。

> [!CAUTION]
> **不支持通话。** 这类 Windows 蜂窝设备的基带大多被厂商配置为「纯数据」模块，没有语音线路：既不提供传统电路域（CS）的拨号能力，语音也只能走运营商的 VoLTE/IMS——而这条承载并未向应用开放，设备本身也缺少音频通路。因此无论用哪种方式都无法拨打 / 接听电话，这是硬件与固件层面的限制，并非软件能够解决。

## 🤝 贡献

欢迎任何形式的贡献！
- **测试与反馈**：帮助完善不同机型 / 固件 / 运营商下的兼容性。
- **报告 Bug**：通过 [Issues](https://github.com/Justsenger/ExModem/issues) 提交。
- **代码贡献**：Fork 项目并提交 Pull Request。

## ❤️ 支持项目

如果你觉得这个项目对你有帮助，欢迎考虑赞助！

[![Ko-fi](https://img.shields.io/badge/Sponsor-Ko--fi-F16061?style=for-the-badge&logo=ko-fi&logoColor=white)](https://ko-fi.com/saniye) &nbsp;&nbsp; [![爱发电](https://img.shields.io/badge/Sponsor-爱发电-633991?style=for-the-badge&logo=afdian&logoColor=white)](https://afdian.com/a/saniye)

## 📄 许可

本项目基于 [GPL-3.0](../LICENSE) 许可证开源。
