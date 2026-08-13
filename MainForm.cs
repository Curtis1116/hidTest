using System.Text;

namespace HidTest
{
    public partial class MainForm : Form
    {
        private const int MaxLogLines = 5000;
        private const int TrimLogLines = 1000;

        private readonly HidService _service = new();
        private readonly List<string> _sendHistory = new();
        private int _historyIndex = -1;
        private bool _suppressListenEvent;

        private static readonly Color ColorInfo = Color.FromArgb(150, 150, 150);
        private static readonly Color ColorOut = Color.FromArgb(106, 217, 126);
        private static readonly Color ColorIn = Color.FromArgb(94, 190, 230);
        private static readonly Color ColorError = Color.FromArgb(240, 100, 100);

        /// <summary>TreeView 子節點所攜帶的介面資訊。</summary>
        private sealed record UsageNodeTag(int VendorId, int ProductId, int? UsagePage, int? Usage, bool HasInput, bool HasOutput);

        public MainForm()
        {
            InitializeComponent();

            _service.Log += OnServiceLog;
            _service.ListeningStopped += OnListeningStopped;

            btnEnumerate.Click += (_, _) => RefreshDeviceTree();
            btnRefreshTree.Click += (_, _) => RefreshDeviceTree();
            btnOpen.Click += BtnOpen_Click;
            btnClose.Click += BtnClose_Click;
            btnSend.Click += (_, _) => SendData();
            btnClearLog.Click += (_, _) => rtbLog.Clear();
            btnSaveLog.Click += BtnSaveLog_Click;

            chkListen.CheckedChanged += ChkListen_CheckedChanged;
            treeDevices.NodeMouseDoubleClick += TreeDevices_NodeMouseDoubleClick;
            txtOutData.KeyDown += TxtOutData_KeyDown;

            Load += (_, _) =>
            {
                AppendLog(LogKind.Info, "[INFO] HID 測試工具 — 先輸入 VID/PID 後按「開啟」，或按「列舉裝置」瀏覽。");
                RefreshDeviceTree();
            };

            FormClosing += (_, _) =>
            {
                _service.Log -= OnServiceLog;
                _service.ListeningStopped -= OnListeningStopped;
                _service.Dispose();
            };
        }

        #region 記錄視窗

        private void OnServiceLog(object? sender, HidLogEventArgs e)
        {
            if (IsDisposed) return;
            if (InvokeRequired)
            {
                try { BeginInvoke(new Action(() => AppendLog(e.Kind, e.Text))); } catch (ObjectDisposedException) { }
            }
            else AppendLog(e.Kind, e.Text);
        }

        private void AppendLog(LogKind kind, string text)
        {
            if (rtbLog.IsDisposed) return;

            TrimLogIfNeeded();

            string line = chkTimestamp.Checked
                ? $"{DateTime.Now:HH:mm:ss.fff}  {text}"
                : text;

            rtbLog.SelectionStart = rtbLog.TextLength;
            rtbLog.SelectionLength = 0;
            rtbLog.SelectionColor = kind switch
            {
                LogKind.Out => ColorOut,
                LogKind.In => ColorIn,
                LogKind.Error => ColorError,
                _ => ColorInfo,
            };
            rtbLog.AppendText(line + Environment.NewLine);
            rtbLog.SelectionColor = rtbLog.ForeColor;

            if (chkAutoScroll.Checked)
            {
                rtbLog.SelectionStart = rtbLog.TextLength;
                rtbLog.ScrollToCaret();
            }
        }

        private void TrimLogIfNeeded()
        {
            int lineCount = rtbLog.Lines.Length;
            if (lineCount <= MaxLogLines) return;

            int cutIndex = rtbLog.GetFirstCharIndexFromLine(TrimLogLines);
            if (cutIndex <= 0) return;

            rtbLog.Select(0, cutIndex);
            rtbLog.SelectedText = string.Empty;
        }

        private void BtnSaveLog_Click(object? sender, EventArgs e)
        {
            using var dialog = new SaveFileDialog
            {
                Filter = "文字檔 (*.txt)|*.txt|所有檔案 (*.*)|*.*",
                FileName = $"hidtest_{DateTime.Now:yyyyMMdd_HHmmss}.txt",
            };

            if (dialog.ShowDialog(this) != DialogResult.OK) return;

            try
            {
                File.WriteAllText(dialog.FileName, rtbLog.Text, Encoding.UTF8);
                AppendLog(LogKind.Info, $"[INFO] 已儲存至 {dialog.FileName}");
            }
            catch (Exception ex)
            {
                AppendLog(LogKind.Error, $"[ERROR] 儲存失敗: {ex.Message}");
            }
        }

        #endregion

        #region 裝置列舉

        private async void RefreshDeviceTree()
        {
            btnEnumerate.Enabled = false;
            btnRefreshTree.Enabled = false;
            treeDevices.Nodes.Clear();
            treeDevices.Nodes.Add("列舉中...");

            try
            {
                var interfaces = await Task.Run(HidService.Enumerate);
                PopulateTree(interfaces);
                AppendLog(LogKind.Info, $"[INFO] 列舉完成，共 {interfaces.Count} 個 HID 介面");
            }
            catch (Exception ex)
            {
                treeDevices.Nodes.Clear();
                AppendLog(LogKind.Error, $"[ERROR] 列舉失敗: {ex.Message}");
            }
            finally
            {
                btnEnumerate.Enabled = true;
                btnRefreshTree.Enabled = true;
            }
        }

        private void PopulateTree(List<HidInterfaceInfo> interfaces)
        {
            treeDevices.BeginUpdate();
            treeDevices.Nodes.Clear();

            foreach (var group in interfaces.GroupBy(i => i.GroupKey))
            {
                var first = group.First();
                var parent = new TreeNode(first.GroupTitle);

                foreach (var info in group)
                {
                    string io = $"IN {info.MaxInputReportLength}  OUT {info.MaxOutputReportLength}";

                    if (info.Usages.Count == 0)
                    {
                        var unknown = new TreeNode($"(無法讀取描述元)   {io}")
                        {
                            Tag = new UsageNodeTag(info.VendorId, info.ProductId, null, null,
                                info.MaxInputReportLength > 0, info.MaxOutputReportLength > 0),
                            ForeColor = Color.Gray,
                        };
                        parent.Nodes.Add(unknown);
                        continue;
                    }

                    foreach (var u in info.Usages)
                    {
                        var child = new TreeNode($"UP 0x{u.UsagePage:X4} / U 0x{u.Usage:X4}   {io}")
                        {
                            Tag = new UsageNodeTag(info.VendorId, info.ProductId, u.UsagePage, u.Usage,
                                info.MaxInputReportLength > 0, info.MaxOutputReportLength > 0),
                        };
                        parent.Nodes.Add(child);
                    }
                }

                treeDevices.Nodes.Add(parent);
            }

            treeDevices.ExpandAll();
            if (treeDevices.Nodes.Count > 0) treeDevices.Nodes[0].EnsureVisible();
            treeDevices.EndUpdate();
        }

        private void TreeDevices_NodeMouseDoubleClick(object? sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Node?.Tag is not UsageNodeTag tag) return;

            txtVid.Text = $"{tag.VendorId:X4}";
            txtPid.Text = $"{tag.ProductId:X4}";

            string up = tag.UsagePage.HasValue ? $"{tag.UsagePage.Value:X4}" : string.Empty;
            string usage = tag.Usage.HasValue ? $"{tag.Usage.Value:X4}" : string.Empty;

            // 只填入該介面實際支援的方向；兩者皆不支援時兩邊都填，交給使用者決定
            bool fillOut = tag.HasOutput || !tag.HasInput;
            bool fillIn = tag.HasInput || !tag.HasOutput;

            if (fillOut)
            {
                txtOutUsagePage.Text = up;
                txtOutUsage.Text = usage;
            }
            if (fillIn)
            {
                txtInUsagePage.Text = up;
                txtInUsage.Text = usage;
            }

            AppendLog(LogKind.Info, $"[INFO] 已帶入 VID:{tag.VendorId:X4} PID:{tag.ProductId:X4}" +
                                    (tag.UsagePage.HasValue ? $" UP:0x{tag.UsagePage.Value:X4} U:0x{tag.Usage!.Value:X4}" : ""));
        }

        #endregion

        #region 連線

        private void BtnOpen_Click(object? sender, EventArgs e)
        {
            if (!TryGetHex(txtVid, "VID", required: true, out int? vid)) return;
            if (!TryGetHex(txtPid, "PID", required: true, out int? pid)) return;

            bool ok = _service.Open(vid!.Value, pid!.Value) > 0;
            SetConnectedState(ok);
        }

        private void BtnClose_Click(object? sender, EventArgs e)
        {
            SetListenChecked(false);
            _service.Close();
            SetConnectedState(false);
            AppendLog(LogKind.Info, "[INFO] 已關閉裝置");
        }

        private void SetConnectedState(bool connected)
        {
            btnOpen.Enabled = !connected;
            btnClose.Enabled = connected;
            btnSend.Enabled = connected;
            chkListen.Enabled = connected;
            txtVid.Enabled = !connected;
            txtPid.Enabled = !connected;

            lblStatus.Text = connected ? "● 已連線" : "● 未連線";
            lblStatus.ForeColor = connected ? Color.SeaGreen : Color.Gray;
        }

        #endregion

        #region 送出 / 監聽

        private void TxtOutData_KeyDown(object? sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Enter:
                    e.SuppressKeyPress = true;
                    SendData();
                    break;

                case Keys.Up when _sendHistory.Count > 0:
                    e.SuppressKeyPress = true;
                    _historyIndex = _historyIndex < 0 ? _sendHistory.Count - 1 : Math.Max(0, _historyIndex - 1);
                    txtOutData.Text = _sendHistory[_historyIndex];
                    txtOutData.SelectionStart = txtOutData.TextLength;
                    break;

                case Keys.Down when _historyIndex >= 0:
                    e.SuppressKeyPress = true;
                    if (_historyIndex < _sendHistory.Count - 1)
                    {
                        _historyIndex++;
                        txtOutData.Text = _sendHistory[_historyIndex];
                    }
                    else
                    {
                        _historyIndex = -1;
                        txtOutData.Clear();
                    }
                    txtOutData.SelectionStart = txtOutData.TextLength;
                    break;
            }
        }

        private void SendData()
        {
            if (!_service.IsOpen)
            {
                AppendLog(LogKind.Error, "[ERROR] 尚未開啟裝置");
                return;
            }

            if (!TryGetHex(txtOutUsagePage, "OUT UsagePage", required: false, out int? usagePage)) return;
            if (!TryGetHex(txtOutUsage, "OUT Usage", required: false, out int? usage)) return;
            if (!TryGetHex(txtOutReportId, "OUT Report ID", required: false, out int? reportId)) return;

            byte[] data;
            try
            {
                data = HexUtil.ParseHexBytes(txtOutData.Text);
            }
            catch (Exception ex)
            {
                AppendLog(LogKind.Error, $"[ERROR] {ex.Message}");
                return;
            }

            if (data.Length == 0)
            {
                AppendLog(LogKind.Error, "[ERROR] 請輸入要送出的十六進位資料");
                return;
            }

            _service.Write(usagePage, usage, reportId, data, chkAutoRid.Checked);

            string text = txtOutData.Text.Trim();
            _sendHistory.Remove(text);
            _sendHistory.Add(text);
            if (_sendHistory.Count > 20) _sendHistory.RemoveAt(0);
            _historyIndex = -1;
        }

        private void ChkListen_CheckedChanged(object? sender, EventArgs e)
        {
            if (_suppressListenEvent) return;

            if (!chkListen.Checked)
            {
                _service.StopListening();
                return;
            }

            if (!TryGetHex(txtInUsagePage, "IN UsagePage", required: false, out int? usagePage) ||
                !TryGetHex(txtInUsage, "IN Usage", required: false, out int? usage) ||
                !TryGetHex(txtInReportId, "IN Report ID", required: false, out int? reportId))
            {
                SetListenChecked(false);
                return;
            }

            if (!_service.StartListening(usagePage, usage, reportId))
                SetListenChecked(false);
        }

        private void OnListeningStopped(object? sender, EventArgs e)
        {
            if (IsDisposed) return;
            if (InvokeRequired)
            {
                try { BeginInvoke(new Action(() => SetListenChecked(false))); } catch (ObjectDisposedException) { }
            }
            else SetListenChecked(false);
        }

        /// <summary>更新監聽勾選狀態但不觸發 CheckedChanged 的處理常式。</summary>
        private void SetListenChecked(bool value)
        {
            _suppressListenEvent = true;
            chkListen.Checked = value;
            _suppressListenEvent = false;
        }

        #endregion

        /// <summary>讀取十六進位輸入欄；格式錯誤時標紅並回報。</summary>
        private bool TryGetHex(TextBox box, string fieldName, bool required, out int? value)
        {
            value = null;
            string text = box.Text.Trim();

            if (text.Length == 0)
            {
                if (!required)
                {
                    box.BackColor = SystemColors.Window;
                    return true;
                }
                box.BackColor = Color.MistyRose;
                AppendLog(LogKind.Error, $"[ERROR] {fieldName} 不可為空");
                return false;
            }

            if (!HexUtil.TryParseHex(text, out int parsed))
            {
                box.BackColor = Color.MistyRose;
                AppendLog(LogKind.Error, $"[ERROR] {fieldName} 不是有效的十六進位值: '{text}'");
                return false;
            }

            box.BackColor = SystemColors.Window;
            value = parsed;
            return true;
        }
    }
}
