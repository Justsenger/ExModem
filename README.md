<h1>
  <img src="https://github.com/Justsenger/ExModem/blob/main/img/logo.png?raw=true" width="32" alt="ExModem logo">
  ExModem
</h1>

<div align="center">

**A graphical cellular-modem manager — send & receive SMS, manage contacts, and switch between 4G/5G with ease.**

</div>

<p align="center">
  <a href="https://github.com/Justsenger/ExModem/releases/latest"><img src="https://img.shields.io/badge/release-V1.0-blue?style=flat-square" alt="Latest release"></a>
  <a href="https://github.com/Justsenger/ExModem/releases"><img src="https://aged-moon-0505.shalingye.workers.dev/?repo=Justsenger/ExModem" alt="Downloads"></a>
  <a href="https://github.com/Justsenger/ExModem/blob/main/LICENSE"><img src="https://img.shields.io/badge/license-GPL--3.0-blue?style=flat-square" alt="License"></a>
</p>

**English** | [简体中文](docs/README.zh-CN.md) | [繁體中文](docs/README.zh-Hant.md) | [Русский](docs/README.ru.md) | [日本語](docs/README.ja.md)

---

ExModem uses Windows' [MobileBroadband WinRT API](https://learn.microsoft.com/en-us/uwp/api/windows.networking.networkoperators) together with Qualcomm [QMI](https://en.wikipedia.org/wiki/Qualcomm_MSM_Interface) commands, aiming to push the modem capabilities of Windows to their limit.

Tested environment: **Surface Pro 11 5G + a China Mobile campus data SIM + 4G / 5G networks**.

As a one-person project with limited time, there may be untested scenarios or bugs. If you run into any problem, you're welcome to open an [Issue](https://github.com/Justsenger/ExModem/issues)!

## 🎨 Screenshots

ExModem is built with [WPF-UI](https://github.com/lepoco/wpfui) for a modern, fluid interface, with light / dark themes that follow the system. It ships in five languages — **简体中文, 繁體中文, English, Русский, 日本語** — and follows your system language on first launch.

![Screenshot 1](https://github.com/Justsenger/ExModem/blob/main/img/01.png?raw=true)
![Screenshot 2](https://github.com/Justsenger/ExModem/blob/main/img/02.png?raw=true)
![Screenshot 3](https://github.com/Justsenger/ExModem/blob/main/img/03.png?raw=true)
![Screenshot 4](https://github.com/Justsenger/ExModem/blob/main/img/04.png?raw=true)
![Screenshot 5](https://github.com/Justsenger/ExModem/blob/main/img/05.png?raw=true)

## ✨ Features

### 📶 Network
View real-time status — current network, carrier, signal strength and more; switch between **4G** (SMS-capable) and **5G** (high-speed data) with a single click. You can also enable **smart auto-switching**: stay on 4G while idle to keep SMS working in real time, and automatically move up to 5G under heavy traffic for speed.

### 💬 Messages
Send and receive SMS like a chat app: messages are grouped into conversations by number, with clear sent/received bubbles; **verification codes** are detected automatically with one-tap copy; new messages raise a system notification.

### 👤 Contacts
Manage both **phone** and **SIM card** contacts in one place: add, edit, delete, set avatars, and group by pinyin initial. The Messages page automatically shows the contact's name and avatar for incoming numbers.

> [!CAUTION]
> **Voice calls are not supported.** The basebands in these Windows cellular devices are mostly provisioned by the vendor as data-only modules with no voice line: there is no traditional circuit-switched (CS) dialing, and voice would only work over the carrier's VoLTE/IMS — a bearer that is not exposed to apps, and the device also lacks an audio path. Therefore calls cannot be placed or received by any means. This is a hardware/firmware limitation, not something software can solve.

## 🤝 Contributing

Contributions of any kind are welcome!
- **Testing & feedback**: help improve compatibility across devices / firmware / carriers.
- **Report bugs**: via [Issues](https://github.com/Justsenger/ExModem/issues).
- **Code**: fork the project and open a Pull Request.

## ❤️ Support

If you find this project helpful, please consider sponsoring!

[![Ko-fi](https://img.shields.io/badge/Sponsor-Ko--fi-F16061?style=for-the-badge&logo=ko-fi&logoColor=white)](https://ko-fi.com/saniye) &nbsp;&nbsp; [![Afdian](https://img.shields.io/badge/Sponsor-爱发电-633991?style=for-the-badge&logo=afdian&logoColor=white)](https://afdian.com/a/saniye)

## 📄 License

Released under the [GPL-3.0](LICENSE) license.
