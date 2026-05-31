<h1>
  <img src="https://github.com/Justsenger/ExModem/blob/main/img/logo.png?raw=true" width="32" alt="ExModem logo">
  ExModem
</h1>

<div align="center">

**一款圖形化的蜂巢式模組（Modem）管理工具，輕鬆收發簡訊、管理聯絡人、切換 4G/5G 等功能。**

</div>

<p align="center">
  <a href="https://github.com/Justsenger/ExModem/releases/latest"><img src="https://img.shields.io/github/v/release/Justsenger/ExModem.svg?style=flat-square" alt="Latest release"></a>
  <a href="https://github.com/Justsenger/ExModem/blob/main/LICENSE"><img src="https://img.shields.io/badge/license-GPL--3.0-blue?style=flat-square" alt="License"></a>
</p>

[English](../README.md) | [简体中文](README.zh-CN.md) | **繁體中文** | [Русский](README.ru.md) | [日本語](README.ja.md)

---

ExModem 使用 Windows 的 [MobileBroadband WinRT API](https://learn.microsoft.com/en-us/uwp/api/windows.networking.networkoperators) 結合高通 [QMI](https://en.wikipedia.org/wiki/Qualcomm_MSM_Interface) 指令，力求將 Windows 上的 Modem 能力發揮到極致。

測試環境：**Surface Pro 11 5G + 中國移動校園資費 SIM + 4G / 5G 網路環境**。

由於個人時間與精力有限，專案可能存在未經測試的情境或錯誤。如果您在使用中遇到任何問題，歡迎透過 [Issues](https://github.com/Justsenger/ExModem/issues) 提出！

## 🎨 介面一覽

ExModem 採用 [WPF-UI](https://github.com/lepoco/wpfui) 框架，介面現代流暢，支援淺色 / 深色主題並跟隨系統自動切換。支援**简体中文、繁體中文、English、Русский、日本語**五種語言，首次啟動自動跟隨系統語言。

![介面1](https://github.com/Justsenger/ExModem/blob/main/img/01.png?raw=true)
![介面2](https://github.com/Justsenger/ExModem/blob/main/img/02.png?raw=true)
![介面3](https://github.com/Justsenger/ExModem/blob/main/img/03.png?raw=true)
![介面4](https://github.com/Justsenger/ExModem/blob/main/img/04.png?raw=true)
![介面5](https://github.com/Justsenger/ExModem/blob/main/img/05.png?raw=true)

## ✨ 功能

### 📶 網路
檢視目前網路、電信業者、訊號強度等即時狀態；一鍵在 **4G**（可收發簡訊）與 **5G**（高速上網）之間切換。也可開啟**智慧自動切換**：閒置時使用 4G 確保簡訊即時，大流量時自動切到 5G 提速。

### 💬 簡訊
像聊天軟體一樣收發簡訊：依號碼歸整為對話，收發氣泡一目了然；自動辨識簡訊中的**驗證碼**並一鍵複製；收到新簡訊彈出系統通知。

### 👤 聯絡人
統一管理**本機**與 **SIM 卡**裡的聯絡人：新增、修改、刪除，可設定大頭貼、依拼音首字母分組；簡訊頁會自動以聯絡人的名稱與大頭貼顯示號碼。

> [!CAUTION]
> **不支援通話。** 這類 Windows 行動裝置的基頻大多被廠商設定為「純資料」模組，沒有語音線路：既不提供傳統電路交換（CS）撥號能力，語音也只能走電信業者的 VoLTE/IMS——而這條承載並未向應用程式開放，裝置本身也缺少音訊通道。因此無論用哪種方式都無法撥打 / 接聽電話，這是硬體與韌體層面的限制，並非軟體能夠解決。

## 🤝 貢獻

歡迎任何形式的貢獻！
- **測試與回饋**：協助完善不同機型 / 韌體 / 電信業者下的相容性。
- **回報 Bug**：透過 [Issues](https://github.com/Justsenger/ExModem/issues) 提交。
- **程式碼貢獻**：Fork 專案並提交 Pull Request。

## ❤️ 支持專案

如果您覺得這個專案對您有幫助，歡迎考慮贊助！

[![Ko-fi](https://img.shields.io/badge/Sponsor-Ko--fi-F16061?style=for-the-badge&logo=ko-fi&logoColor=white)](https://ko-fi.com/saniye) &nbsp;&nbsp; [![愛發電](https://img.shields.io/badge/Sponsor-爱发电-633991?style=for-the-badge&logo=afdian&logoColor=white)](https://afdian.com/a/saniye)

## 📄 授權

本專案基於 [GPL-3.0](../LICENSE) 授權條款開源。
