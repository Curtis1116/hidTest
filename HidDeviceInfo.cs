using HidSharp;

namespace HidTest
{
    /// <summary>單一 HID 介面 (top-level collection) 的摘要資訊，供列舉清單使用。</summary>
    internal sealed class HidInterfaceInfo
    {
        public HidDevice Device { get; set; } = null!;
        public int VendorId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string Manufacturer { get; set; } = string.Empty;
        public string SerialNumber { get; set; } = string.Empty;
        public int MaxInputReportLength { get; set; }
        public int MaxOutputReportLength { get; set; }

        /// <summary>此介面宣告的 (UsagePage, Usage) 組合；描述元無法讀取時為空清單。</summary>
        public IReadOnlyList<UsageInfo> Usages { get; set; } = Array.Empty<UsageInfo>();

        /// <summary>裝置分組的鍵值：同一實體裝置的多個介面會共用。</summary>
        public string GroupKey => $"{VendorId:X4}:{ProductId:X4}|{SerialNumber}";

        public string GroupTitle
        {
            get
            {
                string name = !string.IsNullOrWhiteSpace(ProductName) ? ProductName : "(未知裝置)";

                // 產品名常已含廠商名 (例如 "BenQ ZOWIE Gaming Mouse")，避免重複顯示
                string maker = "";
                if (!string.IsNullOrWhiteSpace(Manufacturer) &&
                    name.IndexOf(Manufacturer, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    maker = Manufacturer + " ";
                }

                string serial = string.IsNullOrWhiteSpace(SerialNumber) ? "" : $" ({SerialNumber})";
                return $"{VendorId:X4}:{ProductId:X4}  {maker}{name}{serial}";
            }
        }
    }

    internal readonly struct UsageInfo
    {
        public int UsagePage { get; }
        public int Usage { get; }

        public UsageInfo(int usagePage, int usage)
        {
            UsagePage = usagePage;
            Usage = usage;
        }

        public override string ToString() => $"UP 0x{UsagePage:X4} / U 0x{Usage:X4}";
    }
}
