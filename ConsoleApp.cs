namespace HidTest
{
    /// <summary>
    /// 命令列模式。維持與舊版相同的參數語法，僅將實作改為呼叫 <see cref="HidService"/>。
    /// </summary>
    internal static class ConsoleApp
    {
        public static int Run(string[] args)
        {
            string firstArg = args[0].ToLowerInvariant().Trim();

            if (firstArg is "-help" or "--help" or "-h" or "/?" or "help")
            {
                PrintUsage();
                return 0;
            }

            if (firstArg is "/list" or "-list" or "--list")
            {
                ListDevices();
                return 0;
            }

            if (args.Length < 2)
            {
                Console.WriteLine("[ERROR] 參數不足。請執行 hidtest -help 查看用法。");
                return 1;
            }

            try
            {
                int vid = HexUtil.ParseHex(args[0]);
                int pid = HexUtil.ParseHex(args[1]);

                using var service = new HidService();
                service.Log += (_, e) => Console.WriteLine(e.Text);

                if (service.Open(vid, pid) == 0) return 1;

                for (int i = 2; i < args.Length; i++)
                {
                    string command = args[i].ToLowerInvariant();
                    if (command != "-out" && command != "-in") continue;

                    int? usagePage = null;
                    int? usage = null;
                    int? reportId = null;
                    bool loop = false;
                    var outBytes = new List<byte>();

                    while (i + 1 < args.Length)
                    {
                        string next = args[i + 1];

                        if (next.Equals("--usage", StringComparison.OrdinalIgnoreCase) && i + 2 < args.Length)
                        {
                            usagePage = HexUtil.ParseHex(args[i + 2]);
                            i += 2;
                        }
                        else if (next.Equals("--usage-id", StringComparison.OrdinalIgnoreCase) && i + 2 < args.Length)
                        {
                            usage = HexUtil.ParseHex(args[i + 2]);
                            i += 2;
                        }
                        else if ((next.Equals("--rid", StringComparison.OrdinalIgnoreCase) ||
                                  next.Equals("--report-id", StringComparison.OrdinalIgnoreCase)) && i + 2 < args.Length)
                        {
                            reportId = HexUtil.ParseHex(args[i + 2]);
                            i += 2;
                        }
                        else if (next.Equals("--loop", StringComparison.OrdinalIgnoreCase))
                        {
                            loop = true;
                            i += 1;
                        }
                        else if (command == "-out" && !next.StartsWith("-"))
                        {
                            outBytes.AddRange(HexUtil.ParseHexBytes(next));
                            i += 1;
                        }
                        else break;
                    }

                    if (command == "-out")
                    {
                        service.Write(usagePage, usage, reportId, outBytes.ToArray());
                    }
                    else if (loop)
                    {
                        if (!service.StartListening(usagePage, usage, reportId)) continue;

                        Console.WriteLine("[INFO] 監聽中... (按 Ctrl+C 結束)");
                        using var quit = new ManualResetEventSlim(false);
                        ConsoleCancelEventHandler handler = (_, e) => { e.Cancel = true; quit.Set(); };
                        Console.CancelKeyPress += handler;
                        service.ListeningStopped += (_, _) => quit.Set();
                        quit.Wait();
                        Console.CancelKeyPress -= handler;
                        service.StopListening();
                    }
                    else
                    {
                        service.ReadOnce(usagePage, usage, reportId);
                    }
                }

                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EXCEPTION] {ex.GetType().Name}: {ex.Message}");
                return 1;
            }
        }

        private static void ListDevices()
        {
            var interfaces = HidService.Enumerate();
            Console.WriteLine($"找到 {interfaces.Count} 個 HID 介面：");
            Console.WriteLine($"{"VID:PID",-11} {"UsagePage",-11} {"Usage",-8} {"In/Out",-9} 名稱");

            foreach (var info in interfaces)
            {
                string name = string.IsNullOrWhiteSpace(info.ProductName) ? "(未知裝置)" : info.ProductName;
                string io = $"{info.MaxInputReportLength}/{info.MaxOutputReportLength}";

                if (info.Usages.Count == 0)
                {
                    Console.WriteLine($"{info.VendorId:X4}:{info.ProductId:X4}   {"-",-11} {"-",-8} {io,-9} {name}");
                    continue;
                }

                foreach (var u in info.Usages)
                {
                    Console.WriteLine($"{info.VendorId:X4}:{info.ProductId:X4}   0x{u.UsagePage:X4}      0x{u.Usage:X4}   {io,-9} {name}");
                }
            }
        }

        public static void PrintUsage()
        {
            Console.WriteLine("HID 測試工具 v2.0  (不帶參數執行即開啟圖形介面)");
            Console.WriteLine("用法: hidtest <VID> <PID> [指令與參數]");
            Console.WriteLine("      hidtest /list   (列出系統中所有 HID 介面與其 Usage)");
            Console.WriteLine("      hidtest -help   (顯示此說明畫面)");
            Console.WriteLine("      hidtest         (無參數：開啟 Windows Form 圖形介面)");
            Console.WriteLine();
            Console.WriteLine("指令:");
            Console.WriteLine("  -out <BYTES>      傳送十六進制位元組資料至裝置");
            Console.WriteLine("                    範例: -out 11 22 33");
            Console.WriteLine("  -in               從裝置讀取輸入資料");
            Console.WriteLine();
            Console.WriteLine("可選參數 (緊接在指令後):");
            Console.WriteLine("  --usage <HEX>     篩選 Usage Page (例如: --usage FF00)");
            Console.WriteLine("  --usage-id <HEX>  篩選 Usage (例如: --usage-id 01)");
            Console.WriteLine("  --rid <HEX>       指定 Report ID (例如: --rid 01)");
            Console.WriteLine("                    * 寫入時: 強制將該 ID 作為首位元組，後接資料");
            Console.WriteLine("                    * 讀取時: 僅顯示該 ID 的輸入報告，過濾其他報告");
            Console.WriteLine("  --loop            (僅適用於 -in) 持續監聽輸入，按 Ctrl+C 結束");
            Console.WriteLine();
            Console.WriteLine("範例 (BenQ USB AUDIO, VID: 0x1FC9, PID: 0x00A4):");
            Console.WriteLine("  hidtest /list");
            Console.WriteLine("  hidtest 0x1FC9 0x00A4 -out 11 22 33 --usage FF00 --rid 01");
            Console.WriteLine("  hidtest 0x1FC9 0x00A4 -in --usage FF00 --rid 01 --loop");
        }
    }
}
