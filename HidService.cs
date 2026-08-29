using HidSharp;

namespace HidTest
{
    internal enum LogKind { Info, Out, In, Error }

    internal sealed class HidLogEventArgs : EventArgs
    {
        public LogKind Kind { get; }
        public string Text { get; }
        public HidLogEventArgs(LogKind kind, string text) { Kind = kind; Text = text; }
    }

    /// <summary>
    /// 封裝所有 HidSharp 操作。不依賴任何 UI，訊息一律透過 <see cref="Log"/> 事件送出，
    /// 因此 CLI 與 WinForms 兩種模式可共用同一份邏輯。
    /// </summary>
    internal sealed class HidService : IDisposable
    {
        private readonly Dictionary<string, HidStream> _openStreams = new();
        private readonly object _streamLock = new();
        private List<HidDevice> _devices = new();

        private Thread? _listenThread;
        private CancellationTokenSource? _listenCts;

        public event EventHandler<HidLogEventArgs>? Log;
        /// <summary>監聽執行緒結束時觸發 (含使用者主動停止與發生錯誤兩種情況)。</summary>
        public event EventHandler? ListeningStopped;

        public bool IsOpen => _devices.Count > 0;
        public bool IsListening => _listenThread is { IsAlive: true };

        private void Emit(LogKind kind, string text) => Log?.Invoke(this, new HidLogEventArgs(kind, text));

        #region 列舉

        /// <summary>列舉系統中所有 HID 介面，並讀出各自的 UsagePage / Usage。</summary>
        public static List<HidInterfaceInfo> Enumerate()
        {
            var result = new List<HidInterfaceInfo>();

            foreach (var d in DeviceList.Local.GetHidDevices())
            {
                result.Add(new HidInterfaceInfo
                {
                    Device = d,
                    VendorId = d.VendorID,
                    ProductId = d.ProductID,
                    ProductName = SafeGet(d.GetProductName),
                    Manufacturer = SafeGet(d.GetManufacturer),
                    SerialNumber = SafeGet(d.GetSerialNumber),
                    MaxInputReportLength = SafeGet(d.GetMaxInputReportLength, 0),
                    MaxOutputReportLength = SafeGet(d.GetMaxOutputReportLength, 0),
                    Usages = GetUsages(d),
                });
            }

            return result
                .OrderBy(i => i.VendorId)
                .ThenBy(i => i.ProductId)
                .ToList();
        }

        /// <summary>讀取裝置描述元中所有 top-level collection 的 (UsagePage, Usage)。</summary>
        public static IReadOnlyList<UsageInfo> GetUsages(HidDevice device)
        {
            var list = new List<UsageInfo>();
            try
            {
                var descriptor = device.GetReportDescriptor();
                foreach (var item in descriptor.DeviceItems)
                {
                    foreach (uint value in item.Usages.GetAllValues())
                    {
                        var info = new UsageInfo((int)(value >> 16), (int)(value & 0xFFFF));
                        if (!list.Contains(info)) list.Add(info);
                    }
                }
            }
            catch
            {
                // 部分系統介面 (例如被獨占開啟的裝置) 讀不到描述元，視為無 usage 資訊。
            }
            return list;
        }

        private static string SafeGet(Func<string> getter)
        {
            try { return getter() ?? string.Empty; } catch { return string.Empty; }
        }

        private static T SafeGet<T>(Func<T> getter, T fallback)
        {
            try { return getter(); } catch { return fallback; }
        }

        #endregion

        #region 連線

        /// <summary>以 VID/PID 搜尋裝置介面。回傳找到的介面數量。</summary>
        public int Open(int vid, int pid)
        {
            Close();
            _devices = DeviceList.Local.GetHidDevices(vid, pid).ToList();

            if (_devices.Count == 0)
            {
                Emit(LogKind.Error, $"[ERROR] 找不到裝置 HID:{vid:X4}:{pid:X4}");
                return 0;
            }

            Emit(LogKind.Info, $"[INFO] 找到 {_devices.Count} 個介面 (VID:{vid:X4} PID:{pid:X4})");
            foreach (var d in _devices)
            {
                string usages = string.Join(", ", GetUsages(d).Select(u => u.ToString()));
                if (usages.Length == 0) usages = "(無法讀取描述元)";
                Emit(LogKind.Info, $"[INFO]   {usages} | IN:{SafeGet(d.GetMaxInputReportLength, 0)} OUT:{SafeGet(d.GetMaxOutputReportLength, 0)}");
            }
            return _devices.Count;
        }

        public void Close()
        {
            StopListening();
            lock (_streamLock)
            {
                foreach (var s in _openStreams.Values)
                {
                    try { s.Dispose(); } catch { }
                }
                _openStreams.Clear();
            }
            _devices = new List<HidDevice>();
        }

        /// <summary>
        /// 核對目前已開啟的介面是否仍存在。任一介面被移除時，關閉該裝置的所有 stream。
        /// </summary>
        public bool CloseIfDisconnected(IEnumerable<HidDevice> availableDevices)
        {
            if (!IsOpen) return false;

            var availablePaths = new HashSet<string>(
                availableDevices.Select(d => d.DevicePath),
                StringComparer.OrdinalIgnoreCase);

            if (_devices.All(d => availablePaths.Contains(d.DevicePath))) return false;

            Close();
            Emit(LogKind.Info, "[INFO] 已開啟的 USB HID 裝置已移除，連線已自動關閉");
            return true;
        }

        /// <summary>挑選符合條件的介面。usagePage / usage 為 null 表示不過濾該項。</summary>
        public HidDevice? SelectDevice(bool forOutput, int? usagePage, int? usage)
        {
            return _devices.FirstOrDefault(d =>
                SafeGet(forOutput ? d.GetMaxOutputReportLength : d.GetMaxInputReportLength, 0) > 0 &&
                MatchesUsage(d, usagePage, usage));
        }

        private static bool MatchesUsage(HidDevice device, int? usagePage, int? usage)
        {
            if (usagePage == null && usage == null) return true;

            var usages = GetUsages(device);
            if (usages.Count == 0) return false;

            return usages.Any(u =>
                (usagePage == null || u.UsagePage == usagePage.Value) &&
                (usage == null || u.Usage == usage.Value));
        }

        private HidStream? GetOrOpenStream(HidDevice device)
        {
            lock (_streamLock)
            {
                if (_openStreams.TryGetValue(device.DevicePath, out var existing)) return existing;

                if (device.TryOpen(out var stream))
                {
                    string usages = string.Join(", ", GetUsages(device).Select(u => u.ToString()));
                    Emit(LogKind.Info, $"[INFO] 已開啟介面 {usages} | IN:{SafeGet(device.GetMaxInputReportLength, 0)} OUT:{SafeGet(device.GetMaxOutputReportLength, 0)}");
                    _openStreams[device.DevicePath] = stream;
                    return stream;
                }
            }

            Emit(LogKind.Error, "[ERROR] 無法開啟裝置 (可能被其他程式獨占，或權限不足)");
            return null;
        }

        private static bool UsesReportId(HidDevice device, bool output)
        {
            try
            {
                var descriptor = device.GetReportDescriptor();
                var reports = output ? descriptor.OutputReports : descriptor.InputReports;
                return reports.Any(r => r.ReportID != 0);
            }
            catch { return false; }
        }

        #endregion

        #region 寫入

        /// <summary>
        /// 送出一筆 Output Report。
        /// Windows 規則：送給 WriteFile 的緩衝區首位元組必為 Report ID (裝置不使用 ID 時填 0x00)，
        /// 且總長度必須等於 MaxOutputReportLength。
        /// </summary>
        public bool Write(int? usagePage, int? usage, int? reportId, byte[] data, bool autoReportId = true)
        {
            if (!IsOpen)
            {
                Emit(LogKind.Error, "[ERROR] 尚未開啟裝置");
                return false;
            }

            if (data.Length == 0)
            {
                Emit(LogKind.Error, "[ERROR] 沒有要送出的資料");
                return false;
            }

            var device = SelectDevice(forOutput: true, usagePage, usage);
            if (device == null)
            {
                Emit(LogKind.Error, $"[ERROR] 找不到符合條件的輸出介面{DescribeFilter(usagePage, usage)}");
                return false;
            }

            var stream = GetOrOpenStream(device);
            if (stream == null) return false;

            int maxLen = SafeGet(device.GetMaxOutputReportLength, 0);
            if (maxLen <= 0)
            {
                Emit(LogKind.Error, "[ERROR] 此介面不支援輸出報告");
                return false;
            }

            byte[] buffer = new byte[maxLen];

            List<byte> validIds = new();
            try
            {
                validIds = device.GetReportDescriptor().OutputReports.Select(r => r.ReportID).ToList();
            }
            catch { }
            bool useReportId = validIds.Any(id => id != 0);

            if (reportId.HasValue)
            {
                // 使用者明確指定 Report ID
                buffer[0] = (byte)reportId.Value;
                Array.Copy(data, 0, buffer, 1, Math.Min(data.Length, maxLen - 1));
            }
            else if (autoReportId && useReportId)
            {
                if (validIds.Contains(data[0]))
                {
                    // 首位元組本身就是有效的 Report ID
                    Array.Copy(data, 0, buffer, 0, Math.Min(data.Length, maxLen));
                }
                else
                {
                    buffer[0] = validIds.First(id => id != 0);
                    Array.Copy(data, 0, buffer, 1, Math.Min(data.Length, maxLen - 1));
                    Emit(LogKind.Info, $"[INFO] 自動補上 Report ID: 0x{buffer[0]:X2}");
                }
            }
            else
            {
                // 無 Report ID 模式：index 0 固定為 0x00，資料自 index 1 開始
                if (data.Length == maxLen && data[0] == 0)
                    Array.Copy(data, 0, buffer, 0, maxLen);
                else
                {
                    buffer[0] = 0x00;
                    Array.Copy(data, 0, buffer, 1, Math.Min(data.Length, maxLen - 1));
                }
            }

            if (data.Length > maxLen - 1 && !(data.Length == maxLen && buffer[0] == data[0]))
                Emit(LogKind.Info, $"[INFO] 資料超過報告長度，已截斷至 {maxLen} 位元組");

            if (buffer[0] != 0 || useReportId || reportId.HasValue)
                Emit(LogKind.Out, $"[OUT] ID: 0x{buffer[0]:X2} | {HexUtil.ToHex(buffer, 1, maxLen - 1)}");
            else
                Emit(LogKind.Out, $"[OUT] Raw ({maxLen} bytes): {HexUtil.ToHex(buffer, 0, maxLen)}");

            try
            {
                stream.Write(buffer);
                Emit(LogKind.Info, "[INFO] 寫入成功");
                return true;
            }
            catch (Exception ex)
            {
                Emit(LogKind.Error, $"[OUT ERROR] {ex.Message}");
                return false;
            }
        }

        #endregion

        #region 讀取 / 監聽

        /// <summary>單次讀取，逾時即結束 (對應 CLI 的 -in 不加 --loop)。</summary>
        public void ReadOnce(int? usagePage, int? usage, int? reportIdFilter, int timeoutMs = 3000)
        {
            var (device, stream) = PrepareRead(usagePage, usage);
            if (device == null || stream == null) return;

            int maxIn = SafeGet(device.GetMaxInputReportLength, 0);
            byte[] buffer = new byte[maxIn];
            bool useReportId = UsesReportId(device, output: false);

            stream.ReadTimeout = timeoutMs;
            Emit(LogKind.Info, $"[INFO] 讀取中 ({timeoutMs} ms 逾時)...");

            var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
            while (true)
            {
                try
                {
                    int count = stream.Read(buffer);
                    if (count <= 0) continue;
                    if (reportIdFilter.HasValue && buffer[0] != reportIdFilter.Value)
                    {
                        // 過濾掉不相符的 Report ID，在剩餘時間內繼續等
                        if (DateTime.UtcNow >= deadline) { Emit(LogKind.Info, "[ IN] 逾時"); break; }
                        continue;
                    }
                    EmitInput(buffer, count, useReportId || reportIdFilter.HasValue);
                    break;
                }
                catch (TimeoutException)
                {
                    Emit(LogKind.Info, "[ IN] 逾時");
                    break;
                }
                catch (Exception ex)
                {
                    Emit(LogKind.Error, $"[ IN ERROR] {ex.Message}");
                    break;
                }
            }
        }

        /// <summary>啟動背景監聽執行緒，持續讀取 Input Report。</summary>
        public bool StartListening(int? usagePage, int? usage, int? reportIdFilter)
        {
            if (IsListening)
            {
                Emit(LogKind.Info, "[INFO] 已在監聽中");
                return true;
            }

            var (device, stream) = PrepareRead(usagePage, usage);
            if (device == null || stream == null) return false;

            _listenCts = new CancellationTokenSource();
            var token = _listenCts.Token;

            _listenThread = new Thread(() => ListenLoop(device, stream, reportIdFilter, token))
            {
                IsBackground = true,
                Name = "HidListen",
            };

            string filter = reportIdFilter.HasValue ? $" (僅顯示 Report ID 0x{reportIdFilter.Value:X2})" : "";
            Emit(LogKind.Info, $"[INFO] 開始監聽 Data In{filter}");
            _listenThread.Start();
            return true;
        }

        public void StopListening()
        {
            var cts = _listenCts;
            var thread = _listenThread;
            if (cts == null || thread == null) return;

            cts.Cancel();
            if (!thread.Join(1500))
                Emit(LogKind.Info, "[INFO] 監聽執行緒未即時結束，將於背景自行終止");

            cts.Dispose();
            _listenCts = null;
            _listenThread = null;
        }

        private (HidDevice?, HidStream?) PrepareRead(int? usagePage, int? usage)
        {
            if (!IsOpen)
            {
                Emit(LogKind.Error, "[ERROR] 尚未開啟裝置");
                return (null, null);
            }

            var device = SelectDevice(forOutput: false, usagePage, usage);
            if (device == null)
            {
                Emit(LogKind.Error, $"[ERROR] 找不到符合條件的輸入介面{DescribeFilter(usagePage, usage)}");
                return (null, null);
            }

            if (SafeGet(device.GetMaxInputReportLength, 0) <= 0)
            {
                Emit(LogKind.Error, "[ERROR] 此介面不支援輸入報告");
                return (null, null);
            }

            var stream = GetOrOpenStream(device);
            return (device, stream);
        }

        private void ListenLoop(HidDevice device, HidStream stream, int? reportIdFilter, CancellationToken token)
        {
            int maxIn = SafeGet(device.GetMaxInputReportLength, 0);
            byte[] buffer = new byte[maxIn];
            bool useReportId = UsesReportId(device, output: false);

            try
            {
                // 用短逾時輪詢，讓取消旗標能及時生效
                stream.ReadTimeout = 300;

                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        int count = stream.Read(buffer);
                        if (count <= 0) continue;
                        if (reportIdFilter.HasValue && buffer[0] != reportIdFilter.Value) continue;
                        EmitInput(buffer, count, useReportId || reportIdFilter.HasValue);
                    }
                    catch (TimeoutException)
                    {
                        // 正常，繼續等下一筆
                    }
                    catch (Exception ex)
                    {
                        if (!token.IsCancellationRequested)
                            Emit(LogKind.Error, $"[ IN ERROR] {ex.Message}");
                        break;
                    }
                }
            }
            finally
            {
                Emit(LogKind.Info, "[INFO] 已停止監聽");
                ListeningStopped?.Invoke(this, EventArgs.Empty);
            }
        }

        private void EmitInput(byte[] buffer, int count, bool withReportId)
        {
            if (withReportId)
                Emit(LogKind.In, $"[ IN] ID: 0x{buffer[0]:X2} | {HexUtil.ToHex(buffer, 1, count - 1)}");
            else
                Emit(LogKind.In, $"[ IN] {HexUtil.ToHex(buffer, 0, count)}");
        }

        #endregion

        private static string DescribeFilter(int? usagePage, int? usage)
        {
            if (usagePage == null && usage == null) return "";
            var parts = new List<string>();
            if (usagePage != null) parts.Add($"UsagePage 0x{usagePage:X4}");
            if (usage != null) parts.Add($"Usage 0x{usage:X4}");
            return " (" + string.Join(" / ", parts) + ")";
        }

        public void Dispose() => Close();
    }
}
