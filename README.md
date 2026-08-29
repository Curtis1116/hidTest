# hidtest

HID 裝置測試工具，基於 .NET Framework 4.8 與 HidSharp。**同一個執行檔同時提供圖形介面與命令列**：

- 直接雙擊或不帶參數執行 → 開啟 Windows Form 圖形介面
- 帶參數執行 → 命令列模式（語法與舊版相容）

## 🌟 功能特色

- **裝置列舉**：樹狀列出系統中所有 USB HID 裝置，並展開其底下每個介面的 Usage Page / Usage 與報告長度。
- **熱插拔偵測**：USB HID 裝置插入或拔除時自動重新列舉；已開啟的裝置被拔除時自動停止監聽並關閉連線。
- **精確過濾**：以 VID/PID 定位裝置，再以 **UsagePage + Usage** 選定介面（舊版僅比對 UsagePage）。
- **資料輸出**：送出十六進位位元組，支援手動指定或自動判斷 Report ID。
- **資料輸入**：可勾選持續監聽 Input Report，並依 Report ID 過濾。
- **終端機式記錄**：彩色分類（OUT / IN / INFO / ERROR）、毫秒時間戳、可清除與存檔。
- **平台支援**：支援 Windows。

## 🚀 快速開始

### 環境需求

- 開發：Visual Studio 2022（含 .NET Framework 4.8 targeting pack）或可建置 net48 的 .NET SDK
- 執行：Windows 與 **.NET Framework 4.8**（非 self-contained）

### 編譯與發行

```bash
git clone https://github.com/Curtis1116/hidTest.git
cd hidTest

# 開發時直接執行圖形介面
dotnet run

# 產生單一 exe（相依 DLL 由 Costura 嵌入）
dotnet build -c Release
```

發行輸出位於：

```
bin\Release\net48\
```

Release 建置的交付檔為 `hidtest.exe`；HidSharp 等託管相依 DLL 會由 Costura 嵌入 exe。執行檔不包含 .NET runtime，因此目標機器必須已安裝 .NET Framework 4.8。

## 🖥 圖形介面

```
┌─ HID 測試工具 ────────────────────────────────────────────────────┐
│ VID 0x[04A5]  PID 0x[800A]   [列舉裝置] [開啟] [關閉]  ● 已連線   │
├──────────────────────┬────────────────────────────────────────────┤
│ 裝置清單             │ ─ Data OUT ──────────────────────────────  │
│ ▼ 04A5:800A ZOWIE    │  UsagePage 0x[FF03] Usage 0x[0000]         │
│   ├ UP 0xFF03/U 0x0  │  ReportID 0x[  ]  ☑ 自動判斷 Report ID     │
│   │   IN 0  OUT 16   │  Data (hex) [11 22 33      ]      [送出]   │
│   └ UP 0xFF04/U 0x0  │ ─ Data IN ───────────────────────────────  │
│       IN 16 OUT 0    │  UsagePage 0x[FF04] Usage 0x[0000]         │
│                      │  ReportID 0x[  ]      ☑ 監聽 Data In       │
│                      ├────────────────────────────────────────────┤
│                      │ 20:54:12  [OUT] ID: 0x00 | 11 22 33        │
│  [重新列舉]          │ 20:54:12  [ IN] ID: 0x01 | AA BB CC        │
│                      │ ☑自動捲動 ☑顯示時間  [清除]  [存檔]        │
└──────────────────────┴────────────────────────────────────────────┘
```

操作流程：

1. 按 **列舉裝置** 瀏覽系統中的 HID 介面（清單不顯示 device path）。
2. **雙擊** 任一 Usage 節點，會自動把 VID / PID / UsagePage / Usage 帶入右側欄位。
3. 按 **開啟** 連線，之後即可送出資料或勾選 **監聽 Data In**。

其他細節：

- Usage 欄位留空 = 不過濾該項；兩個都留空則取第一個可用介面。
- `Data (hex)` 接受 `11 22 33`、`0x11,0x22`、`112233` 等寫法，按 <kbd>Enter</kbd> 即送出，<kbd>↑</kbd>/<kbd>↓</kbd> 可叫回最近 20 筆輸入。
- 記錄視窗超過 5000 行會自動裁掉最舊的部分。

## ⌨️ 命令列模式

```bash
hidtest <VID> <PID> [-out [選項] <HEX_BYTES>] [-in [選項]]
hidtest /list      # 列出所有 HID 介面與其 Usage
hidtest -help      # 說明
hidtest            # 開啟圖形介面
```

### 選項（緊接在 `-out` / `-in` 之後）

| 選項 | 說明 |
|---|---|
| `--usage <HEX>` | 篩選 Usage Page，例如 `--usage FF00` |
| `--usage-id <HEX>` | 篩選 Usage，例如 `--usage-id 01` |
| `--rid <HEX>` | 指定 Report ID。寫入時強制作為首位元組；讀取時僅顯示該 ID 的報告 |
| `--loop` | 僅適用於 `-in`，持續監聽直到按 Ctrl+C |

### 範例

```bash
# 列出所有介面
hidtest /list

# 送出資料到 UsagePage 0xFF00 的介面，Report ID 為 1
hidtest 0x1FC9 0x00A4 -out 11 22 33 --usage FF00 --rid 01

# 持續監聽 Report ID 1 的輸入
hidtest 0x1FC9 0x00A4 -in --usage FF00 --rid 01 --loop
```

`-in` 不加 `--loop` 時為單次讀取，3 秒逾時。

## 🏗 專案結構

| 檔案 | 職責 |
|---|---|
| `Program.cs` | 進入點，依有無參數切換 CLI / GUI |
| `ConsoleApp.cs` | 命令列模式的參數解析與流程 |
| `HidService.cs` | 所有 HidSharp 操作（列舉、開啟、寫入、背景監聽），不依賴 UI |
| `HidDeviceInfo.cs` | 列舉結果的資料模型 |
| `HexUtil.cs` | 十六進位解析與格式化 |
| `MainForm.cs` / `MainForm.Designer.cs` | 圖形介面 |

CLI 與 GUI 共用 `HidService`，訊息一律透過 `Log` 事件送出：CLI 印到主控台，GUI 寫入終端機視窗。

## 📝 實作備註

- 執行檔為 console 子系統，CLI 模式才能讓 shell 正常等待輸出與 Ctrl+C；無參數啟動 GUI 時程式會隱藏並卸離自己的主控台視窗（若主控台是從 cmd/PowerShell 繼承而來則只卸離、不隱藏）。
- Windows 寫入 HID 的規則：送給 `WriteFile` 的緩衝區首位元組必須是 Report ID（裝置不使用 ID 時填 `0x00`），且長度必須等於 `MaxOutputReportLength`。這段邏輯集中在 `HidService.Write()`。
- 監聽採 300 ms 逾時輪詢搭配 `CancellationToken`，取消勾選即可即時停止。

## 🛠 技術棧

- **語言**：C#（latest）
- **框架**：.NET Framework 4.8（`net48`，Windows Forms）
- **函式庫**：[HidSharp](https://www.zerogpoint.org/hidsharp/) 2.6.4

## 📄 授權條款

本專案採用 [MIT License](LICENSE) 授權。

Copyright (c) 2026 huachun
