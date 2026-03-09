# hidtest

一個簡潔、強大的 HID 裝置測試命令列工具 (CLI)，基於 .NET 8 與 HidSharp 開發。

## 🌟 功能特色

- **裝置過濾**：透過 Vendor ID (VID) 與 Product ID (PID) 精確搜尋 HID 裝置。
- **介面選擇**：支援透過 Usage Page 過濾特定輸入或輸出介面。
- **資料讀寫**：
  - **輸出 (-out)**：發送十六進位字串至裝置。支援自動判斷 Report ID 或手動指定。
  - **輸入 (-in)**：從裝置讀取資料並以十六進位格式顯示。
- **平台支援**：支援 Windows。

## 🚀 快速開始

### 環境需求

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

### 編譯與執行

1. 複製儲存庫：
   ```bash
   git clone https://github.com/Curtis1116/hidTest.git
   cd hidTest
   ```

2. 編譯專案：
   ```bash
   dotnet build -c Release
   ```

3. 執行程式：
   ```bash
   dotnet run -- <VID> <PID> [命令]
   ```

## 📖 使用說明

```bash
hidtest <VID> <PID> [-out [--usage <HEX>] <HEX_BYTES>] [-in [--usage <HEX>]]
```

### 參數說明

- `<VID>`: 裝置的 Vendor ID (支援十進位或 0x 開頭的十六進位)。
- `<PID>`: 裝置的 Product ID (支援十進位或 0x 開頭的十六進位)。
- `-out`: 執行輸出操作。
  - `--usage <HEX>` (選用): 指定輸出的 Usage Page。
  - `<HEX_BYTES>`: 要發送的十六進位資料（空格分隔，例如 `01 02 FF`）。
- `-in`: 執行輸入操作。
  - `--usage <HEX>` (選用): 指定輸入的 Usage Page。

### 使用範例

**1. 發送資料至特定裝置：**
```bash
hidtest 0x1234 0x5678 -out 01 02 03
```

**2. 從特定 Usage Page 的介面讀取資料：**
```bash
hidtest 0x1234 0x5678 -in --usage 0xFF00
```

**3. 同時發送與讀取：**
```bash
hidtest 0x1234 0x5678 -out 01 AA BB -in
```

## 🛠 技術棧

- **語言**：C# 12
- **框架**：.NET 8.0
- **函式庫**：[HidSharp](https://www.zerogpoint.org/hidsharp/) (2.6.4)

## 📄 授權條款

本專案採用 [MIT License](LICENSE) 授權。

Copyright (c) 2026 huachun
