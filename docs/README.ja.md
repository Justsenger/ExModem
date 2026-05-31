<h1>
  <img src="https://github.com/Justsenger/ExModem/blob/main/img/logo.png?raw=true" width="32" alt="ExModem logo">
  ExModem
</h1>

<div align="center">

**グラフィカルなセルラーモデム管理ツール。SMS の送受信、連絡先の管理、4G/5G の切り替えを手軽に。**

</div>

<p align="center">
  <a href="https://github.com/Justsenger/ExModem/releases/latest"><img src="https://img.shields.io/github/v/release/Justsenger/ExModem.svg?style=flat-square" alt="Latest release"></a>
  <a href="https://github.com/Justsenger/ExModem/blob/main/LICENSE"><img src="https://img.shields.io/badge/license-GPL--3.0-blue?style=flat-square" alt="License"></a>
</p>

[English](../README.md) | [简体中文](README.zh-CN.md) | [繁體中文](README.zh-Hant.md) | [Русский](README.ru.md) | **日本語**

---

ExModem は Windows の [MobileBroadband WinRT API](https://learn.microsoft.com/en-us/uwp/api/windows.networking.networkoperators) と Qualcomm の [QMI](https://en.wikipedia.org/wiki/Qualcomm_MSM_Interface) コマンドを組み合わせ、Windows 上でのモデム機能を極限まで引き出すことを目指しています。

動作確認環境：**Surface Pro 11 5G ＋ China Mobile の学割データ SIM ＋ 4G / 5G ネットワーク**。

個人開発で時間が限られているため、未検証のケースや不具合がある場合があります。問題が起きたら、お気軽に [Issue](https://github.com/Justsenger/ExModem/issues) を立ててください！

## 🎨 スクリーンショット

ExModem は [WPF-UI](https://github.com/lepoco/wpfui) を採用し、モダンで滑らかな UI とライト / ダークテーマ（システム連動）を備えています。**简体中文・繁體中文・English・Русский・日本語**の 5 言語に対応し、初回起動時はシステムの言語に従います。

![スクリーンショット1](https://github.com/Justsenger/ExModem/blob/main/img/01.png?raw=true)
![スクリーンショット2](https://github.com/Justsenger/ExModem/blob/main/img/02.png?raw=true)
![スクリーンショット3](https://github.com/Justsenger/ExModem/blob/main/img/03.png?raw=true)
![スクリーンショット4](https://github.com/Justsenger/ExModem/blob/main/img/04.png?raw=true)
![スクリーンショット5](https://github.com/Justsenger/ExModem/blob/main/img/05.png?raw=true)

## ✨ 機能

### 📶 ネットワーク
現在のネットワーク・通信事業者・信号強度などの状態をリアルタイム表示。**4G**（SMS 可）と **5G**（高速通信）をワンクリックで切り替えできます。**スマート自動切替**も搭載：待機中は 4G を維持して SMS をリアルタイムに受け取り、高負荷時は自動で 5G に上げて高速化します。

### 💬 メッセージ
チャットアプリのように SMS を送受信：番号ごとに会話へまとめ、送受信の吹き出しで見やすく表示。SMS 内の**認証コード**を自動認識してワンタップでコピー。新着 SMS はシステム通知でお知らせします。

### 👤 連絡先
**本体**と **SIM カード**の連絡先をまとめて管理：追加・編集・削除、アイコン設定、ピンイン頭文字でのグループ分けに対応。メッセージ画面では着信番号を連絡先の名前とアイコンで自動表示します。

> [!CAUTION]
> **通話には対応していません。** この種の Windows セルラー端末のベースバンドは、メーカーによって「データ専用」モジュールとして構成されていることが多く、音声回線がありません。従来の回線交換（CS）による発信ができず、音声は事業者の VoLTE/IMS 経由でしか動作しませんが、その経路はアプリに公開されておらず、端末側にも音声経路がありません。そのため、いかなる方法でも発信・着信はできません。これはハードウェア／ファームウェアの制約であり、ソフトウェアで解決できるものではありません。

## 🤝 コントリビュート

あらゆる形の貢献を歓迎します！
- **テストとフィードバック**：機種 / ファームウェア / 通信事業者ごとの互換性向上にご協力ください。
- **バグ報告**：[Issues](https://github.com/Justsenger/ExModem/issues) からどうぞ。
- **コード**：Fork して Pull Request を送ってください。

## ❤️ 支援

このプロジェクトが役に立ったら、ぜひスポンサーをご検討ください！

[![Ko-fi](https://img.shields.io/badge/Sponsor-Ko--fi-F16061?style=for-the-badge&logo=ko-fi&logoColor=white)](https://ko-fi.com/saniye) &nbsp;&nbsp; [![Afdian](https://img.shields.io/badge/Sponsor-爱发电-633991?style=for-the-badge&logo=afdian&logoColor=white)](https://afdian.com/a/saniye)

## 📄 ライセンス

本プロジェクトは [GPL-3.0](../LICENSE) ライセンスで公開されています。
