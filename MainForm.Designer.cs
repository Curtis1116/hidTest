namespace HidTest
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private Panel pnlTop = null!;
        private Label lblVid = null!;
        private TextBox txtVid = null!;
        private Label lblPid = null!;
        private TextBox txtPid = null!;
        private Button btnEnumerate = null!;
        private Button btnOpen = null!;
        private Button btnClose = null!;
        private Label lblStatus = null!;

        private SplitContainer split = null!;
        private TreeView treeDevices = null!;
        private Panel pnlTreeBottom = null!;
        private Button btnRefreshTree = null!;
        private Label lblTreeHint = null!;

        private GroupBox grpOut = null!;
        private Label lblOutUp = null!;
        private TextBox txtOutUsagePage = null!;
        private Label lblOutU = null!;
        private TextBox txtOutUsage = null!;
        private Label lblOutRid = null!;
        private TextBox txtOutReportId = null!;
        private CheckBox chkAutoRid = null!;
        private Label lblOutData = null!;
        private TextBox txtOutData = null!;
        private Button btnSend = null!;

        private GroupBox grpIn = null!;
        private Label lblInUp = null!;
        private TextBox txtInUsagePage = null!;
        private Label lblInU = null!;
        private TextBox txtInUsage = null!;
        private Label lblInRid = null!;
        private TextBox txtInReportId = null!;
        private CheckBox chkListen = null!;

        private Panel pnlLog = null!;
        private RichTextBox rtbLog = null!;
        private Panel pnlLogBottom = null!;
        private CheckBox chkAutoScroll = null!;
        private CheckBox chkTimestamp = null!;
        private Button btnClearLog = null!;
        private Button btnSaveLog = null!;

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();

            pnlTop = new Panel();
            lblVid = new Label();
            txtVid = new TextBox();
            lblPid = new Label();
            txtPid = new TextBox();
            btnEnumerate = new Button();
            btnOpen = new Button();
            btnClose = new Button();
            lblStatus = new Label();

            split = new SplitContainer();
            treeDevices = new TreeView();
            pnlTreeBottom = new Panel();
            btnRefreshTree = new Button();
            lblTreeHint = new Label();

            grpOut = new GroupBox();
            lblOutUp = new Label();
            txtOutUsagePage = new TextBox();
            lblOutU = new Label();
            txtOutUsage = new TextBox();
            lblOutRid = new Label();
            txtOutReportId = new TextBox();
            chkAutoRid = new CheckBox();
            lblOutData = new Label();
            txtOutData = new TextBox();
            btnSend = new Button();

            grpIn = new GroupBox();
            lblInUp = new Label();
            txtInUsagePage = new TextBox();
            lblInU = new Label();
            txtInUsage = new TextBox();
            lblInRid = new Label();
            txtInReportId = new TextBox();
            chkListen = new CheckBox();

            pnlLog = new Panel();
            rtbLog = new RichTextBox();
            pnlLogBottom = new Panel();
            chkAutoScroll = new CheckBox();
            chkTimestamp = new CheckBox();
            btnClearLog = new Button();
            btnSaveLog = new Button();

            ((System.ComponentModel.ISupportInitialize)split).BeginInit();
            split.Panel1.SuspendLayout();
            split.Panel2.SuspendLayout();
            split.SuspendLayout();
            pnlTop.SuspendLayout();
            pnlTreeBottom.SuspendLayout();
            grpOut.SuspendLayout();
            grpIn.SuspendLayout();
            pnlLog.SuspendLayout();
            pnlLogBottom.SuspendLayout();
            SuspendLayout();

            // ---------- 頂端：VID / PID / 動作 ----------
            pnlTop.Dock = DockStyle.Top;
            pnlTop.Height = 48;
            pnlTop.Padding = new Padding(10, 0, 10, 0);

            lblVid.AutoSize = true;
            lblVid.Text = "VID  0x";
            lblVid.Location = new Point(12, 16);

            txtVid.Location = new Point(62, 12);
            txtVid.Width = 70;
            txtVid.CharacterCasing = CharacterCasing.Upper;
            txtVid.MaxLength = 6;

            lblPid.AutoSize = true;
            lblPid.Text = "PID  0x";
            lblPid.Location = new Point(146, 16);

            txtPid.Location = new Point(196, 12);
            txtPid.Width = 70;
            txtPid.CharacterCasing = CharacterCasing.Upper;
            txtPid.MaxLength = 6;

            btnEnumerate.Text = "列舉裝置";
            btnEnumerate.Location = new Point(282, 10);
            btnEnumerate.Size = new Size(92, 27);

            btnOpen.Text = "開啟";
            btnOpen.Location = new Point(382, 10);
            btnOpen.Size = new Size(76, 27);

            btnClose.Text = "關閉";
            btnClose.Location = new Point(466, 10);
            btnClose.Size = new Size(76, 27);
            btnClose.Enabled = false;

            lblStatus.AutoSize = true;
            lblStatus.Text = "● 未連線";
            lblStatus.ForeColor = Color.Gray;
            lblStatus.Location = new Point(556, 16);

            pnlTop.Controls.Add(lblVid);
            pnlTop.Controls.Add(txtVid);
            pnlTop.Controls.Add(lblPid);
            pnlTop.Controls.Add(txtPid);
            pnlTop.Controls.Add(btnEnumerate);
            pnlTop.Controls.Add(btnOpen);
            pnlTop.Controls.Add(btnClose);
            pnlTop.Controls.Add(lblStatus);

            // ---------- 左側：裝置清單 ----------
            treeDevices.Dock = DockStyle.Fill;
            treeDevices.HideSelection = false;
            treeDevices.FullRowSelect = false;
            treeDevices.ShowLines = true;
            treeDevices.Indent = 18;
            treeDevices.ItemHeight = 20;

            btnRefreshTree.Text = "重新列舉";
            btnRefreshTree.Location = new Point(8, 5);
            btnRefreshTree.Size = new Size(92, 27);

            lblTreeHint.AutoSize = false;
            lblTreeHint.Text = "雙擊 Usage 節點可帶入設定";
            lblTreeHint.ForeColor = Color.Gray;
            lblTreeHint.Location = new Point(106, 11);
            lblTreeHint.Size = new Size(190, 18);

            pnlTreeBottom.Dock = DockStyle.Bottom;
            pnlTreeBottom.Height = 38;
            pnlTreeBottom.Controls.Add(btnRefreshTree);
            pnlTreeBottom.Controls.Add(lblTreeHint);

            // ---------- 右側：Data OUT ----------
            grpOut.Text = "Data OUT";
            grpOut.Dock = DockStyle.Top;
            // 先給定實際寬度，內部控制項的 Anchor 才會以正確的邊界距離計算
            grpOut.Size = new Size(684, 100);
            grpOut.Padding = new Padding(8, 4, 8, 4);

            lblOutUp.AutoSize = true;
            lblOutUp.Text = "UsagePage 0x";
            lblOutUp.Location = new Point(12, 29);

            txtOutUsagePage.Location = new Point(104, 25);
            txtOutUsagePage.Width = 58;
            txtOutUsagePage.CharacterCasing = CharacterCasing.Upper;
            txtOutUsagePage.MaxLength = 6;

            lblOutU.AutoSize = true;
            lblOutU.Text = "Usage 0x";
            lblOutU.Location = new Point(176, 29);

            txtOutUsage.Location = new Point(238, 25);
            txtOutUsage.Width = 58;
            txtOutUsage.CharacterCasing = CharacterCasing.Upper;
            txtOutUsage.MaxLength = 6;

            lblOutRid.AutoSize = true;
            lblOutRid.Text = "ReportID 0x";
            lblOutRid.Location = new Point(312, 29);

            txtOutReportId.Location = new Point(392, 25);
            txtOutReportId.Width = 44;
            txtOutReportId.CharacterCasing = CharacterCasing.Upper;
            txtOutReportId.MaxLength = 4;

            chkAutoRid.AutoSize = true;
            chkAutoRid.Checked = true;
            chkAutoRid.Text = "自動判斷 Report ID";
            chkAutoRid.Location = new Point(452, 27);

            lblOutData.AutoSize = true;
            lblOutData.Text = "Data (hex)";
            lblOutData.Location = new Point(12, 64);

            txtOutData.Location = new Point(104, 60);
            txtOutData.Width = 466;
            txtOutData.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtOutData.Font = new Font("Consolas", 9.75f);

            btnSend.Text = "送出";
            btnSend.Location = new Point(586, 58);
            btnSend.Size = new Size(80, 27);
            btnSend.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnSend.Enabled = false;

            grpOut.Controls.Add(lblOutUp);
            grpOut.Controls.Add(txtOutUsagePage);
            grpOut.Controls.Add(lblOutU);
            grpOut.Controls.Add(txtOutUsage);
            grpOut.Controls.Add(lblOutRid);
            grpOut.Controls.Add(txtOutReportId);
            grpOut.Controls.Add(chkAutoRid);
            grpOut.Controls.Add(lblOutData);
            grpOut.Controls.Add(txtOutData);
            grpOut.Controls.Add(btnSend);

            // ---------- 右側：Data IN ----------
            grpIn.Text = "Data IN";
            grpIn.Dock = DockStyle.Top;
            grpIn.Size = new Size(684, 66);
            grpIn.Padding = new Padding(8, 4, 8, 4);

            lblInUp.AutoSize = true;
            lblInUp.Text = "UsagePage 0x";
            lblInUp.Location = new Point(12, 29);

            txtInUsagePage.Location = new Point(104, 25);
            txtInUsagePage.Width = 58;
            txtInUsagePage.CharacterCasing = CharacterCasing.Upper;
            txtInUsagePage.MaxLength = 6;

            lblInU.AutoSize = true;
            lblInU.Text = "Usage 0x";
            lblInU.Location = new Point(176, 29);

            txtInUsage.Location = new Point(238, 25);
            txtInUsage.Width = 58;
            txtInUsage.CharacterCasing = CharacterCasing.Upper;
            txtInUsage.MaxLength = 6;

            lblInRid.AutoSize = true;
            lblInRid.Text = "ReportID 0x";
            lblInRid.Location = new Point(312, 29);

            txtInReportId.Location = new Point(392, 25);
            txtInReportId.Width = 44;
            txtInReportId.CharacterCasing = CharacterCasing.Upper;
            txtInReportId.MaxLength = 4;

            chkListen.AutoSize = true;
            chkListen.Text = "監聽 Data In";
            chkListen.Location = new Point(452, 27);
            chkListen.Enabled = false;

            grpIn.Controls.Add(lblInUp);
            grpIn.Controls.Add(txtInUsagePage);
            grpIn.Controls.Add(lblInU);
            grpIn.Controls.Add(txtInUsage);
            grpIn.Controls.Add(lblInRid);
            grpIn.Controls.Add(txtInReportId);
            grpIn.Controls.Add(chkListen);

            // ---------- 右側：Terminal ----------
            rtbLog.Dock = DockStyle.Fill;
            rtbLog.BackColor = Color.FromArgb(18, 18, 18);
            rtbLog.ForeColor = Color.Gainsboro;
            rtbLog.Font = new Font("Consolas", 9.75f);
            rtbLog.ReadOnly = true;
            rtbLog.WordWrap = false;
            rtbLog.DetectUrls = false;
            rtbLog.BorderStyle = BorderStyle.None;
            rtbLog.ScrollBars = RichTextBoxScrollBars.Both;

            chkAutoScroll.AutoSize = true;
            chkAutoScroll.Checked = true;
            chkAutoScroll.Text = "自動捲動";
            chkAutoScroll.Location = new Point(8, 9);

            chkTimestamp.AutoSize = true;
            chkTimestamp.Checked = true;
            chkTimestamp.Text = "顯示時間";
            chkTimestamp.Location = new Point(96, 9);

            btnClearLog.Text = "清除";
            btnClearLog.Size = new Size(72, 26);
            btnClearLog.Location = new Point(190, 5);

            btnSaveLog.Text = "存檔";
            btnSaveLog.Size = new Size(72, 26);
            btnSaveLog.Location = new Point(270, 5);

            pnlLogBottom.Dock = DockStyle.Bottom;
            pnlLogBottom.Height = 36;
            pnlLogBottom.Controls.Add(chkAutoScroll);
            pnlLogBottom.Controls.Add(chkTimestamp);
            pnlLogBottom.Controls.Add(btnClearLog);
            pnlLogBottom.Controls.Add(btnSaveLog);

            pnlLog.Dock = DockStyle.Fill;
            pnlLog.Padding = new Padding(4, 4, 0, 0);
            pnlLog.Controls.Add(rtbLog);
            pnlLog.Controls.Add(pnlLogBottom);

            // ---------- SplitContainer ----------
            split.Dock = DockStyle.Fill;
            split.Orientation = Orientation.Vertical;
            // Size 必須先給，否則 Panel1MinSize / Panel2MinSize / SplitterDistance
            // 會以 SplitContainer 的預設寬度 (150) 驗證而擲回例外。
            split.Size = new Size(1000, 652);
            split.SplitterWidth = 6;
            split.Panel1MinSize = 220;
            split.Panel2MinSize = 460;
            split.SplitterDistance = 310;

            split.Panel1.Controls.Add(treeDevices);
            split.Panel1.Controls.Add(pnlTreeBottom);

            split.Panel2.Controls.Add(pnlLog);
            split.Panel2.Controls.Add(grpIn);
            split.Panel2.Controls.Add(grpOut);

            // ---------- Form ----------
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1000, 700);
            MinimumSize = new Size(860, 560);
            Controls.Add(split);
            Controls.Add(pnlTop);
            Text = "HID 測試工具";
            StartPosition = FormStartPosition.CenterScreen;

            pnlLogBottom.ResumeLayout(false);
            pnlLogBottom.PerformLayout();
            pnlLog.ResumeLayout(false);
            grpIn.ResumeLayout(false);
            grpIn.PerformLayout();
            grpOut.ResumeLayout(false);
            grpOut.PerformLayout();
            pnlTreeBottom.ResumeLayout(false);
            pnlTop.ResumeLayout(false);
            pnlTop.PerformLayout();
            split.Panel1.ResumeLayout(false);
            split.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)split).EndInit();
            split.ResumeLayout(false);
            ResumeLayout(false);
        }
    }
}
