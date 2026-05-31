<h1>
  <img src="https://github.com/Justsenger/ExModem/blob/main/img/logo.png?raw=true" width="32" alt="ExModem logo">
  ExModem
</h1>

<div align="center">

**Графический менеджер сотового модема — приём и отправка SMS, управление контактами и переключение 4G/5G одним кликом.**

</div>

<p align="center">
  <a href="https://github.com/Justsenger/ExModem/releases/latest"><img src="https://img.shields.io/github/v/release/Justsenger/ExModem.svg?style=flat-square" alt="Latest release"></a>
  <a href="https://github.com/Justsenger/ExModem/blob/main/LICENSE"><img src="https://img.shields.io/badge/license-GPL--3.0-blue?style=flat-square" alt="License"></a>
</p>

[English](../README.md) | [简体中文](README.zh-CN.md) | [繁體中文](README.zh-Hant.md) | **Русский** | [日本語](README.ja.md)

---

ExModem использует [MobileBroadband WinRT API](https://learn.microsoft.com/en-us/uwp/api/windows.networking.networkoperators) от Windows вместе с командами Qualcomm [QMI](https://en.wikipedia.org/wiki/Qualcomm_MSM_Interface), стремясь раскрыть возможности модема в Windows по максимуму.

Проверенное окружение: **Surface Pro 11 5G + студенческая дата-SIM China Mobile + сети 4G / 5G**.

Это проект одного человека с ограниченным временем, поэтому возможны непротестированные сценарии и ошибки. Если вы столкнулись с проблемой — пожалуйста, откройте [Issue](https://github.com/Justsenger/ExModem/issues)!

## 🎨 Скриншоты

ExModem построен на [WPF-UI](https://github.com/lepoco/wpfui): современный плавный интерфейс со светлой / тёмной темой, следующей за системой. Доступно пять языков — **简体中文, 繁體中文, English, Русский, 日本語** — при первом запуске язык выбирается по системному.

![Скриншот 1](https://github.com/Justsenger/ExModem/blob/main/img/01.png?raw=true)
![Скриншот 2](https://github.com/Justsenger/ExModem/blob/main/img/02.png?raw=true)
![Скриншот 3](https://github.com/Justsenger/ExModem/blob/main/img/03.png?raw=true)
![Скриншот 4](https://github.com/Justsenger/ExModem/blob/main/img/04.png?raw=true)
![Скриншот 5](https://github.com/Justsenger/ExModem/blob/main/img/05.png?raw=true)

## ✨ Возможности

### 📶 Сеть
Просмотр состояния в реальном времени — текущая сеть, оператор, уровень сигнала и др.; переключение между **4G** (с поддержкой SMS) и **5G** (высокоскоростные данные) одним кликом. Можно включить **умное автопереключение**: в простое оставаться на 4G ради мгновенных SMS, а под нагрузкой автоматически подниматься до 5G ради скорости.

### 💬 Сообщения
Приём и отправка SMS как в мессенджере: сообщения группируются в переписки по номеру, с понятными «пузырями» входящих/исходящих; **коды подтверждения** распознаются автоматически с копированием в один тап; новые сообщения вызывают системное уведомление.

### 👤 Контакты
Управление контактами **телефона** и **SIM-карты** в одном месте: добавление, изменение, удаление, установка фото, группировка по первой букве пиньиня. На странице сообщений входящие номера автоматически показываются с именем и фото контакта.

> [!CAUTION]
> **Голосовые вызовы не поддерживаются.** Модемы этих сотовых устройств на Windows обычно поставляются производителем как модули «только данные» без голосовой линии: нет традиционного набора через коммутацию каналов (CS), а голос работал бы только через VoLTE/IMS оператора — этот канал не доступен приложениям, и у устройства также нет аудиотракта. Поэтому совершать или принимать вызовы невозможно никаким способом. Это ограничение уровня оборудования/прошивки, а не то, что можно решить программно.

## 🤝 Участие

Любой вклад приветствуется!
- **Тестирование и отзывы**: помогите улучшить совместимость с разными устройствами / прошивками / операторами.
- **Сообщить об ошибке**: через [Issues](https://github.com/Justsenger/ExModem/issues).
- **Код**: сделайте форк и откройте Pull Request.

## ❤️ Поддержать

Если проект оказался полезным, рассмотрите возможность спонсорства!

[![Ko-fi](https://img.shields.io/badge/Sponsor-Ko--fi-F16061?style=for-the-badge&logo=ko-fi&logoColor=white)](https://ko-fi.com/saniye) &nbsp;&nbsp; [![Afdian](https://img.shields.io/badge/Sponsor-爱发电-633991?style=for-the-badge&logo=afdian&logoColor=white)](https://afdian.com/a/saniye)

## 📄 Лицензия

Распространяется под лицензией [GPL-3.0](../LICENSE).
