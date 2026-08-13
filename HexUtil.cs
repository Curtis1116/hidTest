using System.Globalization;

namespace HidTest
{
    /// <summary>十六進位字串的解析與格式化。</summary>
    internal static class HexUtil
    {
        /// <summary>解析單一十六進位數值，接受 "FF00" 或 "0xFF00"。失敗時擲回例外。</summary>
        public static int ParseHex(string input)
        {
            if (!TryParseHex(input, out int value))
                throw new FormatException($"無法解析十六進位數值: '{input}'");
            return value;
        }

        public static bool TryParseHex(string? input, out int value)
        {
            value = 0;
            if (string.IsNullOrWhiteSpace(input)) return false;

            string s = input.Trim();
            if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) s = s.Substring(2);
            s = s.TrimStart('0');
            if (s.Length == 0) { value = 0; return true; }

            return int.TryParse(s, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value);
        }

        /// <summary>空白字串視為「未指定」而回傳 null；有值但格式錯誤則擲回例外。</summary>
        public static int? ParseHexOrNull(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return null;
            return ParseHex(input);
        }

        /// <summary>
        /// 解析一串十六進位位元組。接受空白、逗號或分號分隔，
        /// 亦接受連續字串 (例如 "112233" 等同於 "11 22 33")。
        /// </summary>
        public static byte[] ParseHexBytes(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return Array.Empty<byte>();

            var tokens = input.Split(new[] { ' ', '\t', ',', ';', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var result = new List<byte>();

            foreach (var raw in tokens)
            {
                string t = raw.Trim();
                if (t.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) t = t.Substring(2);
                if (t.Length == 0) continue;

                if (t.Length <= 2)
                {
                    result.Add(ParseByte(t));
                }
                else
                {
                    // 連續字串，例如 "112233"
                    if (t.Length % 2 != 0)
                        throw new FormatException($"位元組字串長度必須為偶數: '{raw}'");
                    for (int i = 0; i < t.Length; i += 2)
                        result.Add(ParseByte(t.Substring(i, 2)));
                }
            }

            return result.ToArray();
        }

        private static byte ParseByte(string token)
        {
            if (!byte.TryParse(token, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte b))
                throw new FormatException($"無法解析位元組: '{token}'");
            return b;
        }

        public static string ToHex(byte[] buffer, int offset, int count)
        {
            if (count <= 0) return string.Empty;
            return BitConverter.ToString(buffer, offset, count).Replace("-", " ");
        }
    }
}
