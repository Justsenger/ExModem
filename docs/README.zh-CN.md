<h1>
  <img src="https://github.com/Justsenger/ExModem/blob/main/img/logo.png?raw=true" width="32" alt="ExModem logo">
  ExModem
</h1>

<div align="center">

**一款图形化的蜂窝模块（Modem）管理工具，让搭载高通基带的 Windows 设备也能轻松收发短信、管理联系人、切换 4G/5G。**

</div>

<p align="center">
  <a href="https://github.com/Justsenger/ExModem/releases/latest"><img src="https://img.shields.io/github/v/release/Justsenger/ExModem.svg?style=flat-square" alt="Latest release"></a>
  <a href="https://github.com/Justsenger/ExModem/releases"><img src="https://img.shields.io/github/downloads/Justsenger/ExModem/total.svg?style=flat-square" alt="Downloads"></a>
  <a href="https://github.com/Justsenger/ExModem/issues"><img src="https://img.shields.io/github/issues/Justsenger/ExModem.svg?style=flat-square" alt="Issues"></a>
  <a href="https://github.com/Justsenger/ExModem/blob/main/LICENSE"><img src="https://img.shields.io/github/license/Justsenger/ExModem.svg?style=flat-square" alt="License"></a>
</p>

**中文**

---

ExModem 通过 Windows 的 [MobileBroadband WinRT API](https://learn.microsoft.com/en-us/uwp/api/windows.networking.networkoperators) 向高通基带直接下发裸 [QMI](https://en.wikipedia.org/wiki/Qualcomm_MSM_Interface) 命令，把原本被系统藏起来的蜂窝高级能力（网络模式切换、短信收发、SIM 卡读写）做成了一个图形化、开箱即用的工具。

它最初是为了解决一个真实的痛点：**很多搭载高通骁龙的 Windows on ARM 设备（如 Surface Pro 5G），插上 SIM 卡后在 5G 独立组网（SA）下收不到、也发不出短信。** ExModem 找到了原因并提供了一键解决方案（详见[技术文档](#-技术文档)）。

由于个人时间和精力有限，项目可能存在未经测试的场景或错误。如果您在使用中遇到任何问题，欢迎通过 [Issues](https://github.com/Justsenger/ExModem/issues) 提出！

> [!WARNING]
> ExModem 直接向基带下发底层命令。虽然所有操作都经过实测验证，但不同机型/固件/运营商的行为可能不同。请知悉风险后使用。

## 🎨 界面一览

ExModem 使用 [WPF-UI](https://github.com/lepoco/wpfui) 框架，提供流畅现代的界面，支持浅色 / 深色主题并跟随系统自动切换。

目前支持的语言：**简体中文、繁體中文、English、Русский、日本語**（首次启动跟随系统语言）。

> 📸 界面截图稍后补充。

## 🚀 快速开始

---
### 1. 下载与运行
- **下载**：前往 [Releases 页面](https://github.com/Justsenger/ExModem/releases/latest)下载最新版本。
- **运行**：解压后直接运行 `ExModem.exe`（首次会请求管理员权限，因为访问基带直通通道需要提权）。

> [!NOTE]
> 适用于搭载**高通骁龙基带**的 Windows 设备（Surface Pro X / 9 5G / 11 等，以及其它高通 WoA 平台）。非高通基带暂不支持。

---
### 2. 构建（可选）
1. 安装 [.NET 8 SDK](https://dotnet.microsoft.com/zh-cn/download) 与 .NET 8 桌面运行时。
2. 克隆本仓库后，在项目目录执行：
   ```pwsh
   cd src
   dotnet publish -c Release
   ```
3. 产物在 `src/bin/publish/`。发布过程会自动裁剪 WinRT 投影体积（详见[体积优化](#体积优化)），无需任何额外配置。

## 📖 技术文档

> 这部分根据高通 QMI 文档、Windows 蜂窝协议栈以及大量实机探测编写，可能存在疏漏，欢迎指正。

---
### 为什么 5G 下收不到短信？

> [!NOTE]
> 这是 ExModem 诞生的核心原因。

现代 5G 独立组网（SA）取消了传统的 CS（电路交换）域，短信只能走 **SMS-over-NAS** 或 **SMS-over-IMS** 承载。手机能正常收发，是因为它们在 SA 下注册了 IMS 并声明了短信承载；而很多 Windows 设备的 IMS/短信承载没有在 SA 下建立起来，导致 MT（下行）短信无处投递、MO（上行）短信报 `NETWORK_NOT_READY`。

叠加部分 Windows 版本的 `SmsRouter`（系统短信分发组件）与基带的绑定存在回归，问题更明显。

**解决办法很简单：把基带切回 4G（LTE）。** LTE 有成熟的 SMS-over-SGs 承载，网络会直接把积压的短信投递下来，发送也立即可用。ExModem 把这个切换做成了一个开关，并能在「高速 5G」与「短信 4G」之间智能/手动切换。

---
### 网络模式（4G / 5G）

ExModem 通过 QMI NAS 的 `SET_SYSTEM_SELECTION_PREFERENCE` 修改基带的 `mode_pref`：

- **短信模式（LTE，`0x3C`）**：锁定 4G，可正常收发短信，并保持基本上网。
- **高速模式（5G，`0x7F`）**：放开全部制式，优先 5G，享受高速但收不到短信。

> [!NOTE]
> `0x3C ⇄ 0x7F` 之间切换**数据零中断**（实测无掉网窗口），切到 5G 时会有约 10~30 秒由 LTE 平滑爬升到 5G 的过程，全程有网。

#### 智能自动切换

开启后，ExModem 监测蜂窝网卡吞吐（滞回策略）：空闲时切到 LTE（保证短信实时），检测到大流量时升到 5G（提速）。等验证码时挂着、看视频时自动提速，无需手动操作。

---
### 短信

ExModem 的短信采用**混合方案**，免打包即可收发：

- **接收**：读取系统短信库（`ChatMessageStore`），无需后台触发器或受限能力；同时把 **SIM 卡上存储的短信**（经 QMI WMS 直读）按号码、按时间合并进同一会话。
- **发送**：优先走系统接口（`SmsDevice2`），失败则回退到 QMI WMS 自建 PDU 直发（支持 GSM-7 与中文 UCS2）。
- **桌面通知**：新短信弹出系统 Toast（进通知中心、有声音），验证码自动识别并附「一键复制」按钮。

> [!IMPORTANT]
> 短信收发需在 **4G（LTE）** 下进行（见上文）。在设置好之前，发送会提示先切到 LTE。

---
### 联系人

统一管理**本机联系人**（系统 `ContactStore`，免打包可读写）与 **SIM 卡联系人**（QMI UIM 直读/写 `EF_ADN` 电话簿）：

- 支持新增 / 修改 / 删除，可选择保存到本机或 SIM 卡。
- 支持联系人头像（本机），按拼音 A–Z 分组。
- 短信页会自动用联系人名称与头像显示来信号码。

---
### 状态参数

「网络」页实时显示：当前网络（4G/5G）、运营商、注册状态、信号强度、短信中心号码、设备制造商 / 型号 / 固件版本等，可一键复制全部参数用于诊断。

---
### 技术实现

> [!NOTE]
> ExModem **不需要打包成 MSIX**，也不需要任何受限能力声明。

核心是一条 **QMI/QMUX 直通通道**：通过 `MobileBroadbandModem.GetDeviceService()` 拿到高通厂商设备服务（MBIM_SERVICE_QMUX_EXT），用 `OpenCommandSession` 直接收发裸 QMI 帧。在此之上封装了：

- **NAS（0x03）**：网络模式切换。
- **WMS（0x05）**：SIM/设备短信的列出、读取、删除、发送。
- **UIM（0x0B）**：SIM 电话簿 `EF_ADN`、短信容量 `EF_SMS` 的读写。

短信接收与系统短信库读取、联系人本机读写则走对应的 WinRT API（`ChatMessageStore` / `ContactStore`）。

#### 体积优化

应用为框架依赖发布。`net8.0-windows10.0.19041.0` 这个 TFM 会自动塞入约 24MB 的 WinRT 投影 `Microsoft.Windows.SDK.NET.dll`，而应用只用到其中极少数 API。ExModem 在发布时用 `illink` 对该程序集（及 `System.Drawing.Common` 等）做**成员级裁剪**——以应用实际调用为根，仅保留用到的成员——把它从 ~24MB 压到 ~0.4MB，整个发布包压缩后不到 1MB。该过程在 `dotnet publish` 时自动完成，不开 `PublishTrimmed`、不影响 WPF。

---
### 已知边界

- **打电话**：不支持。基带不暴露 CS 域拨号（`DIAL_CALL` 返回 `NO_RADIO`），语音仅 VoLTE/IMS 走够不着的承载，且无音频路由——三条路均不通。
- **5G NSA**：理论可收发短信（锚定 LTE），但取决于网络是否提供 NSA；纯 SA 网络无法收发。
- **非高通基带**：Intel/Fibocom 等暂未实现（已预留后端抽象接口）。

## 🌐 多语言

界面与运行时文案全部本地化，支持简体中文、繁體中文、English、Русский、日本語五种语言，可在设置中切换（切换后重启生效），首次启动自动跟随系统显示语言。欢迎通过 PR 改进或新增语言翻译。

## 🤝 贡献

欢迎任何形式的贡献！
- **测试与反馈**：帮助完善不同机型 / 固件 / 运营商下的兼容性。
- **报告 Bug**：通过 [Issues](https://github.com/Justsenger/ExModem/issues) 提交。
- **代码贡献**：Fork 项目并提交 Pull Request。

## ❤️ 支持项目

如果你觉得这个项目对你有帮助，欢迎考虑赞助！

[![Ko-fi](https://img.shields.io/badge/Sponsor-Ko--fi-F16061?style=for-the-badge&logo=ko-fi&logoColor=white)](https://ko-fi.com/saniye) &nbsp;&nbsp; [![爱发电](https://img.shields.io/badge/Sponsor-爱发电-633991?style=for-the-badge&logo=afdian&logoColor=white)](https://afdian.com/a/saniye)

## 📄 许可

本项目基于仓库根目录的 [LICENSE](https://github.com/Justsenger/ExModem/blob/main/LICENSE) 开源。
