using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using HidSharp;
using HidSharp.Reports;

namespace HidTest
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Usage: hidtest <VID> <PID> [-out [--usage <HEX>] <HEX_BYTES>] [-in [--usage <HEX>]]");
                return;
            }

            try
            {
                // 1. Parse VID/PID
                int vid = ParseHex(args[0]);
                int pid = ParseHex(args[1]);

                // 2. Search all matching devices
                var loader = DeviceList.Local;
                var hidDevices = loader.GetHidDevices(vid, pid).ToList();

                if (hidDevices.Count == 0)
                {
                    Console.WriteLine($"[ERROR] Device not found. VID:0x{vid:X4} PID:0x{pid:X4}");
                    return;
                }

                // 3. Pre-scan for all required interfaces and open them
                var openStreams = new Dictionary<string, HidStream>();
                var deviceMap = new Dictionary<string, HidDevice>();

                try
                {
                    // Execute commands
                    for (int i = 2; i < args.Length; i++)
                    {
                        if (args[i].Equals("-out", StringComparison.OrdinalIgnoreCase))
                        {
                            int? usagePage = null;
                            if (i + 2 < args.Length && args[i + 1].Equals("--usage", StringComparison.OrdinalIgnoreCase))
                            {
                                usagePage = ParseHex(args[i + 2]);
                                i += 2;
                            }

                            List<byte> outBytes = new List<byte>();
                            i++;
                            while (i < args.Length && !args[i].StartsWith("-"))
                            {
                                outBytes.Add((byte)ParseHex(args[i]));
                                i++;
                            }
                            i--;

                            if (outBytes.Count > 0)
                            {
                                var outDevice = hidDevices.FirstOrDefault(d => 
                                    d.GetMaxOutputReportLength() > 0 && 
                                    (usagePage == null || GetUsagePage(d) == usagePage.Value));

                                if (outDevice == null)
                                {
                                    string usageStr = usagePage.HasValue ? $" with UsagePage 0x{usagePage:X4}" : "";
                                    Console.WriteLine($"[ERROR] No output-capable interface found{usageStr}.");
                                    continue;
                                }

                                var stream = GetOrOpenStream(outDevice, openStreams);
                                if (stream != null)
                                {
                                    PerformWrite(outDevice, stream, outBytes);
                                }
                            }
                        }
                        else if (args[i].Equals("-in", StringComparison.OrdinalIgnoreCase))
                        {
                            int? usagePage = null;
                            if (i + 2 < args.Length && args[i + 1].Equals("--usage", StringComparison.OrdinalIgnoreCase))
                            {
                                usagePage = ParseHex(args[i + 2]);
                                i += 2;
                            }

                            var inDevice = hidDevices.FirstOrDefault(d => 
                                d.GetMaxInputReportLength() > 0 && 
                                (usagePage == null || GetUsagePage(d) == usagePage.Value));

                            if (inDevice == null)
                            {
                                string usageStr = usagePage.HasValue ? $" with UsagePage 0x{usagePage:X4}" : "";
                                Console.WriteLine($"[ERROR] No input-capable interface found{usageStr}.");
                                continue;
                            }

                            var stream = GetOrOpenStream(inDevice, openStreams);
                            if (stream != null)
                            {
                                PerformRead(inDevice, stream);
                            }
                        }
                    }
                }
                finally
                {
                    // 4. Close all streams
                    foreach (var stream in openStreams.Values)
                    {
                        stream.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EXCEPTION] {ex.Message}");
            }
        }

        static HidStream? GetOrOpenStream(HidDevice device, Dictionary<string, HidStream> openStreams)
        {
            if (openStreams.TryGetValue(device.DevicePath, out var existingStream))
            {
                return existingStream;
            }

            if (device.TryOpen(out var newStream))
            {
                openStreams[device.DevicePath] = newStream;
                return newStream;
            }

            Console.WriteLine($"[ERROR] Failed to open device: {device.DevicePath.Substring(Math.Max(0, device.DevicePath.Length - 40))}");
            return null;
        }

        static void PerformWrite(HidDevice device, HidStream stream, List<byte> data)
        {
            int maxLen = device.GetMaxOutputReportLength();
            byte[] reportBuffer = new byte[maxLen];
            
            var descriptor = device.GetReportDescriptor();
            var validReportIds = descriptor.OutputReports.Select(r => r.ReportID).ToList();

            bool firstByteIsValidId = validReportIds.Contains(data[0]);
            
            if (!firstByteIsValidId)
            {
                if (validReportIds.Count == 1)
                {
                    byte requiredId = validReportIds[0];
                    reportBuffer[0] = requiredId;
                    Array.Copy(data.ToArray(), 0, reportBuffer, 1, Math.Min(data.Count, maxLen - 1));
                }
                else if (validReportIds.Contains(0) && data.Count < maxLen)
                {
                    reportBuffer[0] = 0;
                    Array.Copy(data.ToArray(), 0, reportBuffer, 1, Math.Min(data.Count, maxLen - 1));
                }
                else
                {
                    Array.Copy(data.ToArray(), 0, reportBuffer, 0, Math.Min(data.Count, maxLen));
                }
            }
            else
            {
                Array.Copy(data.ToArray(), 0, reportBuffer, 0, Math.Min(data.Count, maxLen));
            }

            Console.WriteLine($"[OUT] {BitConverter.ToString(reportBuffer).Replace("-", " ")}");
            try
            {
                stream.Write(reportBuffer);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[OUT ERROR] {ex.Message} (MaxLength: {maxLen})");
            }
        }

        static void PerformRead(HidDevice device, HidStream stream)
        {
            int maxIn = device.GetMaxInputReportLength();
            byte[] buffer = new byte[maxIn];
            stream.ReadTimeout = 3000;
            try
            {
                int count = stream.Read(buffer);
                if (count > 0)
                {
                    Console.WriteLine($"[ IN] {BitConverter.ToString(buffer, 0, count).Replace("-", " ")}");
                }
                else
                {
                    Console.WriteLine("[ IN] No data read.");
                }
            }
            catch (TimeoutException)
            {
                Console.WriteLine("[ IN] Read timeout (3s).");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ IN ERROR] {ex.Message} (MaxLength: {maxIn})");
            }
        }

        static int GetUsagePage(HidDevice device)
        {
            try
            {
                var descriptor = device.GetReportDescriptor();
                foreach (var deviceItem in descriptor.DeviceItems)
                {
                    foreach (var usage in deviceItem.Usages.GetAllValues())
                    {
                        return (int)(usage >> 16);
                    }
                }
            }
            catch { }
            return 0;
        }

        static int ParseHex(string input)
        {
            if (input.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                return int.Parse(input.Substring(2), NumberStyles.HexNumber);
            }
            return int.Parse(input, NumberStyles.HexNumber);
        }
    }
}
