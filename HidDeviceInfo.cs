using HidSharp;

namespace HidTest
{
    /// <summary>單一 HID 介面 (top-level collection) 的摘要資訊，供列舉清單使用。</summary>
    internal sealed class HidInterfaceInfo
    {
        public required HidDevice Device { get; init; }
        public int VendorId { get; init; }
        public int ProductId { get; init; }
        public string ProductName { get; init; } = string.Empty;
        public string Manufacturer { get; init; } = string.Empty;
        public string SerialNumber { get; init; } = string.Empty;
        public int MaxInputReportLength { get; init; }
        public int MaxOutputReportLength { get; init; }

        /// <summary>此介面宣告的 (UsagePage, Usage) 組合；描述元無法讀取時為空清單。</summary>
        public IReadOnlyList<UsageInfo> Usages { get; init; } = Array.Empty<UsageInfo>();

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

                return $"{VendorId:X4}:{ProductId:X4}  {maker}{name}";
            }
        }
    }

    internal readonly record struct UsageInfo(int UsagePage, int Usage)
    {
        public override string ToString() => $"UP 0x{UsagePage:X4} / U 0x{Usage:X4}";
    }
}
