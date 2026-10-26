using System;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using Microsoft.Win32;

namespace BraltiDNS
{
    public class MainForm : Form
    {
        // UI - Header & Hero
        private Label lblHeroStatus;
        private Label lblHeroDesc;
        private Button btnHeroConnect;

        // UI - Settings Rows
        private ComboBox cmbPresets;
        private Button btnStartupToggle;
        private Label lblStartupStatusText;
        private Button btnServiceToggle;
        private Label lblServiceStatusText;

        // UI - Tabs & Advanced
        private TabControl tabControl;
        private TextBox txtArguments;
        private TextBox txtLogs;

        // UI - System Tray
        private NotifyIcon trayIcon;
        private ContextMenuStrip trayMenu;
        private ToolStripMenuItem itemProtectionToggle;
        private ToolStripMenuItem itemStartupToggle;
        private ToolStripMenuItem itemServiceToggle;

        // Process Management
        private Process dpiProcess = null;
        private bool isRunning = false;
        private bool isExiting = false;
        private bool isInitializing = true;
        private string configPath;

        // Sleek Curated Color Palette
        private Color clrBg = Color.FromArgb(15, 18, 24);         // Deep Slate Background
        private Color clrCard = Color.FromArgb(24, 28, 38);       // Card Surface
        private Color clrCardBorder = Color.FromArgb(38, 46, 62); // Subtle Outline
        private Color clrAccent = Color.FromArgb(0, 200, 150);    // Vibrant Emerald / Teal
        private Color clrAccentDark = Color.FromArgb(0, 150, 110);
        private Color clrDanger = Color.FromArgb(239, 68, 68);    // Soft Red
        private Color clrPrimary = Color.FromArgb(59, 130, 246);  // Modern Blue
        private Color clrMuted = Color.FromArgb(148, 163, 184);   // Slate Gray
        private Color clrText = Color.FromArgb(248, 250, 252);    // Crisp Off-White

        private const string WebsiteUrl = "https://bralti.com";

        [STAThread]
        public static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            bool startMin = false;
            foreach (string arg in args)
            {
                if (arg.Equals("--minimized", StringComparison.OrdinalIgnoreCase) ||
                    arg.Equals("/min", StringComparison.OrdinalIgnoreCase) ||
                    arg.Equals("-m", StringComparison.OrdinalIgnoreCase))
                {
                    startMin = true;
                }
            }
            Application.Run(new MainForm(startMin));
        }

        public MainForm(bool startMinimized)
        {
            configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "braltidns_config.ini");
            InitializeWindow();
            InitializeComponents();
            LoadConfig();
            UpdateStartupStatusUI();
            UpdateServiceStatusUI();

            if (startMinimized)
            {
                this.WindowState = FormWindowState.Minimized;
                this.ShowInTaskbar = false;
            }

            // Zero-Click Auto-Start: Immediately activate protection on launch
            if (IsServiceRunning())
            {
                SetStatus(true, true);
            }
            else
            {
                StartProtection();
            }

            isInitializing = false;

            // Launch Notification (700ms gives Windows Explorer time to register tray icon)
            System.Windows.Forms.Timer launchTimer = new System.Windows.Forms.Timer();
            launchTimer.Interval = 700;
            launchTimer.Tick += (s, ev) => {
                launchTimer.Stop();
                launchTimer.Dispose();
                string statusMsg;
                if (IsServiceRunning())
                    statusMsg = "bralti dns Service is active in background.";
                else
                    statusMsg = "bralti dns is active (" + GetShortPresetName() + ").";

                ShowTrayNotification("bralti dns", statusMsg);
            };
            launchTimer.Start();
        }

        private void InitializeWindow()
        {
            this.Text = "bralti dns";
            this.Size = new Size(460, 680);
            this.MinimumSize = new Size(440, 640);
            this.MaximumSize = new Size(540, 750);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = clrBg;
            this.ForeColor = clrText;
            this.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;

            string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"src\icon.ico");
            if (File.Exists(iconPath))
            {
                try { this.Icon = new Icon(iconPath); } catch { }
            }
        }

        private void InitializeComponents()
        {
            InitializeTrayIcon();

            // 1. Header (Brand Title & Website Button cleanly separated to eliminate any overlap)
            TableLayoutPanel pnlHeader = new TableLayoutPanel();
            pnlHeader.Dock = DockStyle.Top;
            pnlHeader.Height = 56;
            pnlHeader.BackColor = clrBg;
            pnlHeader.ColumnCount = 2;
            pnlHeader.RowCount = 1;
            pnlHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60f));
            pnlHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40f));
            pnlHeader.Padding = new Padding(14, 10, 14, 10);

            Label lblLogo = new Label();
            lblLogo.Text = "🛡️ bralti dns";
            lblLogo.Font = new Font("Segoe UI", 14f, FontStyle.Bold);
            lblLogo.ForeColor = clrText;
            lblLogo.Dock = DockStyle.Fill;
            lblLogo.TextAlign = ContentAlignment.MiddleLeft;
            lblLogo.Cursor = Cursors.Hand;
            lblLogo.Click += (s, e) => OpenWebsite();
            pnlHeader.Controls.Add(lblLogo, 0, 0);

            Button btnHeaderSite = new Button();
            btnHeaderSite.Text = "🌐 bralti.com";
            btnHeaderSite.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            btnHeaderSite.ForeColor = clrAccent;
            btnHeaderSite.BackColor = Color.FromArgb(20, 26, 36);
            btnHeaderSite.FlatStyle = FlatStyle.Flat;
            btnHeaderSite.FlatAppearance.BorderColor = clrAccentDark;
            btnHeaderSite.Size = new Size(115, 32);
            btnHeaderSite.Anchor = AnchorStyles.Right;
            btnHeaderSite.Cursor = Cursors.Hand;
            btnHeaderSite.Click += (s, e) => OpenWebsite();
            pnlHeader.Controls.Add(btnHeaderSite, 1, 0);

            this.Controls.Add(pnlHeader);

            // 2. Tabs: Dashboard vs Logs & Config
            tabControl = new TabControl();
            tabControl.Dock = DockStyle.Fill;
            tabControl.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            tabControl.Padding = new Point(20, 6);

            TabPage tabDashboard = new TabPage("⚡ Dashboard");
            tabDashboard.BackColor = clrBg;
            BuildDashboard(tabDashboard);
            tabControl.TabPages.Add(tabDashboard);

            TabPage tabLogs = new TabPage("📜 Logs & Config");
            tabLogs.BackColor = clrBg;
            BuildLogsTab(tabLogs);
            tabControl.TabPages.Add(tabLogs);

            this.Controls.Add(tabControl);

            this.FormClosing += new FormClosingEventHandler(MainForm_FormClosing);
            this.Resize += new EventHandler(MainForm_Resize);
        }

        private void BuildDashboard(TabPage page)
        {
            Panel container = new Panel();
            container.Dock = DockStyle.Fill;
            container.AutoScroll = true;
            container.Padding = new Padding(16, 8, 16, 16);

            int y = 6;

            // ================= HERO SECTION (PRIMARY ACTION) =================
            Panel pnlHero = new Panel();
            pnlHero.Size = new Size(410, 150);
            pnlHero.Location = new Point(7, y);
            pnlHero.BackColor = clrCard;
            pnlHero.BorderStyle = BorderStyle.None;
            pnlHero.Paint += (s, e) => DrawSubtleBorder(e.Graphics, pnlHero);

            lblHeroStatus = new Label();
            lblHeroStatus.Text = "● PROTECTED";
            lblHeroStatus.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            lblHeroStatus.ForeColor = clrAccent;
            lblHeroStatus.TextAlign = ContentAlignment.MiddleCenter;
            lblHeroStatus.Size = new Size(390, 22);
            lblHeroStatus.Location = new Point(10, 14);
            pnlHero.Controls.Add(lblHeroStatus);

            btnHeroConnect = new Button();
            btnHeroConnect.Text = "🛑 STOP PROTECTION";
            btnHeroConnect.Font = new Font("Segoe UI", 11.5f, FontStyle.Bold);
            btnHeroConnect.ForeColor = Color.White;
            btnHeroConnect.BackColor = clrDanger;
            btnHeroConnect.FlatStyle = FlatStyle.Flat;
            btnHeroConnect.FlatAppearance.BorderSize = 0;
            btnHeroConnect.Size = new Size(270, 52);
            btnHeroConnect.Location = new Point(70, 44);
            btnHeroConnect.Cursor = Cursors.Hand;
            btnHeroConnect.Click += (s, e) => ToggleProtection();
            pnlHero.Controls.Add(btnHeroConnect);

            lblHeroDesc = new Label();
            lblHeroDesc.Text = "Protected via Turkey Preset (Yandex DNS 1253 & TTL 5)";
            lblHeroDesc.Font = new Font("Segoe UI", 8.5f, FontStyle.Regular);
            lblHeroDesc.ForeColor = clrMuted;
            lblHeroDesc.TextAlign = ContentAlignment.MiddleCenter;
            lblHeroDesc.Size = new Size(390, 20);
            lblHeroDesc.Location = new Point(10, 110);
            pnlHero.Controls.Add(lblHeroDesc);

            container.Controls.Add(pnlHero);
            y += 162;

            // ================= PRESET SELECTOR =================
            Panel pnlPreset = new Panel();
            pnlPreset.Size = new Size(410, 75);
            pnlPreset.Location = new Point(7, y);
            pnlPreset.BackColor = clrCard;
            pnlPreset.Paint += (s, e) => DrawSubtleBorder(e.Graphics, pnlPreset);

            Label lblPresetTag = new Label();
            lblPresetTag.Text = "NETWORK PRESET";
            lblPresetTag.Font = new Font("Segoe UI", 7.5f, FontStyle.Bold);
            lblPresetTag.ForeColor = clrMuted;
            lblPresetTag.Location = new Point(14, 10);
            lblPresetTag.AutoSize = true;
            pnlPreset.Controls.Add(lblPresetTag);

            cmbPresets = new ComboBox();
            cmbPresets.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbPresets.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            cmbPresets.BackColor = Color.FromArgb(18, 22, 30);
            cmbPresets.ForeColor = clrText;
            cmbPresets.FlatStyle = FlatStyle.Flat;
            cmbPresets.Size = new Size(380, 28);
            cmbPresets.Location = new Point(14, 32);
            cmbPresets.Items.AddRange(new object[] {
                "🇹🇷 [Default] Turkey Standard (-5, TTL 5, DNS 1253)",
                "🇹🇷 Turkey - Superonline Alt 1 (TTL 3)",
                "🇹🇷 Turkey - Superonline Alt 2 (Mode -5)",
                "🇹🇷 Turkey - Aggressive (Mode -9 & DNS 1253)",
                "🛡️ Compatibility Mode (-1)",
                "🛠️ Custom Arguments"
            });
            cmbPresets.SelectedIndex = 0;
            cmbPresets.SelectedIndexChanged += new EventHandler(CmbPresets_SelectedIndexChanged);
            pnlPreset.Controls.Add(cmbPresets);

            container.Controls.Add(pnlPreset);
            y += 87;

            // ================= ROW 1: APP STARTUP TOGGLE BUTTON =================
            Panel pnlStartupRow = new Panel();
            pnlStartupRow.Size = new Size(410, 68);
            pnlStartupRow.Location = new Point(7, y);
            pnlStartupRow.BackColor = clrCard;
            pnlStartupRow.Paint += (s, e) => DrawSubtleBorder(e.Graphics, pnlStartupRow);

            Label lblStTitle = new Label();
            lblStTitle.Text = "🚀 App Windows Startup";
            lblStTitle.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            lblStTitle.ForeColor = clrText;
            lblStTitle.Location = new Point(14, 13);
            lblStTitle.AutoSize = true;
            pnlStartupRow.Controls.Add(lblStTitle);

            lblStartupStatusText = new Label();
            lblStartupStatusText.Text = "Disabled";
            lblStartupStatusText.Font = new Font("Segoe UI", 8f, FontStyle.Regular);
            lblStartupStatusText.ForeColor = clrMuted;
            lblStartupStatusText.Location = new Point(15, 36);
            lblStartupStatusText.AutoSize = true;
            pnlStartupRow.Controls.Add(lblStartupStatusText);

            btnStartupToggle = new Button();
            btnStartupToggle.Text = "Enable";
            btnStartupToggle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            btnStartupToggle.ForeColor = Color.White;
            btnStartupToggle.BackColor = clrPrimary;
            btnStartupToggle.FlatStyle = FlatStyle.Flat;
            btnStartupToggle.FlatAppearance.BorderSize = 0;
            btnStartupToggle.Size = new Size(110, 36);
            btnStartupToggle.Location = new Point(285, 16);
            btnStartupToggle.Cursor = Cursors.Hand;
            btnStartupToggle.Click += (s, e) => ToggleAppStartupAction();
            pnlStartupRow.Controls.Add(btnStartupToggle);

            container.Controls.Add(pnlStartupRow);
            y += 80;

            // ================= ROW 2: UNIFIED WINDOWS SERVICE BUTTON =================
            Panel pnlServiceRow = new Panel();
            pnlServiceRow.Size = new Size(410, 68);
            pnlServiceRow.Location = new Point(7, y);
            pnlServiceRow.BackColor = clrCard;
            pnlServiceRow.Paint += (s, e) => DrawSubtleBorder(e.Graphics, pnlServiceRow);

            Label lblSvcTitle = new Label();
            lblSvcTitle.Text = "📦 Windows Service Mode";
            lblSvcTitle.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            lblSvcTitle.ForeColor = clrText;
            lblSvcTitle.Location = new Point(14, 13);
            lblSvcTitle.AutoSize = true;
            pnlServiceRow.Controls.Add(lblSvcTitle);

            lblServiceStatusText = new Label();
            lblServiceStatusText.Text = "Not Installed";
            lblServiceStatusText.Font = new Font("Segoe UI", 8f, FontStyle.Regular);
            lblServiceStatusText.ForeColor = clrMuted;
            lblServiceStatusText.Location = new Point(15, 36);
            lblServiceStatusText.AutoSize = true;
            pnlServiceRow.Controls.Add(lblServiceStatusText);

            btnServiceToggle = new Button();
            btnServiceToggle.Text = "Install";
            btnServiceToggle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            btnServiceToggle.ForeColor = Color.White;
            btnServiceToggle.BackColor = Color.FromArgb(45, 60, 85);
            btnServiceToggle.FlatStyle = FlatStyle.Flat;
            btnServiceToggle.FlatAppearance.BorderSize = 0;
            btnServiceToggle.Size = new Size(110, 36);
            btnServiceToggle.Location = new Point(285, 16);
            btnServiceToggle.Cursor = Cursors.Hand;
            btnServiceToggle.Click += (s, e) => ToggleServiceAction();
            pnlServiceRow.Controls.Add(btnServiceToggle);

            container.Controls.Add(pnlServiceRow);
            y += 80;

            // ================= MINIMAL FOOTER =================
            Panel pnlFooter = new Panel();
            pnlFooter.Size = new Size(410, 45);
            pnlFooter.Location = new Point(7, y);
            pnlFooter.BackColor = Color.Transparent;

            Label lblTrayHint = new Label();
            lblTrayHint.Text = "Runs silently in Windows System Tray (⌃) when closed";
            lblTrayHint.Font = new Font("Segoe UI", 8f);
            lblTrayHint.ForeColor = clrMuted;
            lblTrayHint.TextAlign = ContentAlignment.MiddleCenter;
            lblTrayHint.Dock = DockStyle.Fill;
            pnlFooter.Controls.Add(lblTrayHint);

            container.Controls.Add(pnlFooter);

            page.Controls.Add(container);
            UpdateArgumentsFromPreset();
        }

        private void BuildLogsTab(TabPage page)
        {
            Panel card = new Panel();
            card.Dock = DockStyle.Fill;
            card.Padding = new Padding(16);

            Label lblCmd = new Label();
            lblCmd.Text = "Active Command Arguments:";
            lblCmd.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            lblCmd.ForeColor = clrMuted;
            lblCmd.Dock = DockStyle.Top;
            lblCmd.Height = 22;
            card.Controls.Add(lblCmd);

            txtArguments = new TextBox();
            txtArguments.Dock = DockStyle.Top;
            txtArguments.Height = 45;
            txtArguments.Multiline = true;
            txtArguments.Font = new Font("Consolas", 9f);
            txtArguments.BackColor = Color.FromArgb(12, 14, 18);
            txtArguments.ForeColor = Color.FromArgb(0, 255, 180);
            card.Controls.Add(txtArguments);

            Panel topBar = new Panel();
            topBar.Dock = DockStyle.Top;
            topBar.Height = 40;

            Label lblLogTitle = new Label();
            lblLogTitle.Text = "Live Console Output:";
            lblLogTitle.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            lblLogTitle.ForeColor = clrMuted;
            lblLogTitle.Location = new Point(0, 15);
            lblLogTitle.AutoSize = true;
            topBar.Controls.Add(lblLogTitle);

            Button btnClear = new Button();
            btnClear.Text = "Clear Log";
            btnClear.Font = new Font("Segoe UI", 8f);
            btnClear.ForeColor = clrMuted;
            btnClear.BackColor = clrCard;
            btnClear.FlatStyle = FlatStyle.Flat;
            btnClear.Size = new Size(80, 26);
            btnClear.Location = new Point(310, 10);
            btnClear.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnClear.Click += (s, e) => txtLogs.Clear();
            topBar.Controls.Add(btnClear);

            card.Controls.Add(topBar);

            txtLogs = new TextBox();
            txtLogs.Dock = DockStyle.Fill;
            txtLogs.Multiline = true;
            txtLogs.ReadOnly = true;
            txtLogs.ScrollBars = ScrollBars.Both;
            txtLogs.Font = new Font("Consolas", 8.5f);
            txtLogs.BackColor = Color.FromArgb(10, 12, 16);
            txtLogs.ForeColor = Color.FromArgb(180, 220, 255);
            card.Controls.Add(txtLogs);

            page.Controls.Add(card);
        }

        private void DrawSubtleBorder(Graphics g, Panel p)
        {
            using (Pen pen = new Pen(clrCardBorder, 1))
            {
                g.DrawRectangle(pen, 0, 0, p.Width - 1, p.Height - 1);
            }
        }

        private void InitializeTrayIcon()
        {
            trayMenu = new ContextMenuStrip();
            trayMenu.Font = new Font("Segoe UI", 9.5f);

            ToolStripMenuItem itemOpen = new ToolStripMenuItem("🖥️ Open bralti dns", null, (s, e) => RestoreWindow());
            itemProtectionToggle = new ToolStripMenuItem("⚡ Start / Stop Protection", null, (s, e) => ToggleProtection());
            ToolStripMenuItem itemPresetTurkey = new ToolStripMenuItem("🇹🇷 Apply Turkey Default Preset", null, (s, e) => {
                cmbPresets.SelectedIndex = 0;
                if (!isRunning && !IsServiceRunning()) StartProtection();
            });
            ToolStripSeparator sep1 = new ToolStripSeparator();
            itemStartupToggle = new ToolStripMenuItem("🚀 Toggle Windows Startup", null, (s, e) => ToggleAppStartupAction());
            itemServiceToggle = new ToolStripMenuItem("📦 Install Windows Service", null, (s, e) => ToggleServiceAction());
            ToolStripSeparator sep2 = new ToolStripSeparator();
            ToolStripMenuItem itemWeb = new ToolStripMenuItem("🌐 Visit bralti.com", null, (s, e) => OpenWebsite());
            ToolStripSeparator sep3 = new ToolStripSeparator();
            ToolStripMenuItem itemExit = new ToolStripMenuItem("🚪 Exit", null, (s, e) => {
                isExiting = true;
                StopProtection();
                trayIcon.Visible = false;
                Application.Exit();
            });

            trayMenu.Items.AddRange(new ToolStripItem[] {
                itemOpen, itemProtectionToggle, itemPresetTurkey, sep1, itemStartupToggle, itemServiceToggle, sep2, itemWeb, sep3, itemExit
            });

            trayIcon = new NotifyIcon();
            trayIcon.Text = "bralti dns - Stopped";
            if (this.Icon != null)
                trayIcon.Icon = this.Icon;
            else
                trayIcon.Icon = SystemIcons.Shield;

            trayIcon.ContextMenuStrip = trayMenu;
            trayIcon.Visible = true;
            trayIcon.DoubleClick += (s, e) => RestoreWindow();
            trayIcon.MouseClick += (s, e) => {
                if (e.Button == MouseButtons.Left)
                    RestoreWindow();
            };
        }

        // ==================== Actions & Toggles ====================

        private void ToggleProtection()
        {
            if (isRunning)
            {
                StopProtection();
            }
            else if (IsServiceRunning())
            {
                RunSc("stop \"GoodbyeDPI\"");
                UpdateServiceStatusUI();
                SetStatus(false);
                ShowTrayNotification("bralti dns", "🛑 Service stopped.");
            }
            else
            {
                StartProtection();
            }
        }

        private void StartProtection()
        {
            if (isRunning && dpiProcess != null && !dpiProcess.HasExited) return;

            string arch = Environment.Is64BitOperatingSystem ? "x86_64" : "x86";
            string exePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, arch, "goodbyedpi.exe");

            if (!File.Exists(exePath))
            {
                AppendLog("❌ Error: goodbyedpi.exe not found at: " + exePath);
                MessageBox.Show("Could not find core engine binary at:\n" + exePath, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            KillOrphanProcesses();

            try
            {
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = exePath;
                psi.Arguments = txtArguments != null ? txtArguments.Text.Trim() : "-5 --set-ttl 5 --dns-addr 77.88.8.8 --dns-port 1253 --dnsv6-addr 2a02:6b8::feed:0ff --dnsv6-port 1253";
                psi.WorkingDirectory = Path.GetDirectoryName(exePath);
                psi.CreateNoWindow = true;
                psi.UseShellExecute = false;
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError = true;

                Process proc = new Process();
                proc.StartInfo = psi;
                proc.EnableRaisingEvents = true;

                proc.OutputDataReceived += (s, e) => {
                    if (!string.IsNullOrEmpty(e.Data))
                        AppendLog(e.Data);
                };
                proc.ErrorDataReceived += (s, e) => {
                    if (!string.IsNullOrEmpty(e.Data))
                        AppendLog("[ERR] " + e.Data);
                };
                proc.Exited += (s, e) => {
                    if (!isExiting)
                    {
                        this.BeginInvoke(new Action(() => {
                            if (dpiProcess == proc)
                            {
                                SetStatus(false);
                                AppendLog("⚠️ Engine process terminated.");
                            }
                        }));
                    }
                };

                proc.Start();
                proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();

                dpiProcess = proc;
                SetStatus(true);
                AppendLog("🚀 bralti dns started!");
                AppendLog("Arguments: " + psi.Arguments);

                ShowTrayNotification("bralti dns", "🚀 Protection active! (" + GetShortPresetName() + ").");
            }
            catch (Exception ex)
            {
                AppendLog("❌ Launch exception: " + ex.Message);
                MessageBox.Show("Failed to start service:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                SetStatus(false);
            }
        }

        private void StopProtection()
        {
            try
            {
                if (dpiProcess != null)
                {
                    dpiProcess.EnableRaisingEvents = false;
                    if (!dpiProcess.HasExited)
                    {
                        dpiProcess.Kill();
                        dpiProcess.WaitForExit(1000);
                    }
                    dpiProcess.Dispose();
                    dpiProcess = null;
                }
            }
            catch { }

            KillOrphanProcesses();
            SetStatus(false);
            AppendLog("🛑 Protection stopped.");
            ShowTrayNotification("bralti dns", "🛑 Protection stopped.");
        }

        private void StopProtectionSilently()
        {
            try
            {
                if (dpiProcess != null)
                {
                    dpiProcess.EnableRaisingEvents = false;
                    if (!dpiProcess.HasExited)
                    {
                        dpiProcess.Kill();
                        dpiProcess.WaitForExit(1000);
                    }
                    dpiProcess.Dispose();
                    dpiProcess = null;
                }
            }
            catch { }

            KillOrphanProcesses();
            isRunning = false;
        }

        private void KillOrphanProcesses()
        {
            try
            {
                Process[] procs = Process.GetProcessesByName("goodbyedpi");
                foreach (Process p in procs)
                {
                    try { p.Kill(); } catch { }
                }
            }
            catch { }
        }

        private void SetStatus(bool running, bool isService = false)
        {
            isRunning = running;
            if (running)
            {
                btnHeroConnect.Text = "🛑 STOP PROTECTION";
                btnHeroConnect.BackColor = clrDanger;
                lblHeroStatus.Text = "● PROTECTED";
                lblHeroStatus.ForeColor = clrAccent;
                lblHeroDesc.Text = isService
                    ? "Protected via background Windows Service"
                    : "Protected via Turkey Preset (" + GetShortPresetName() + ")";

                SetTrayText("bralti dns - Active (" + GetShortPresetName() + ")");
                if (itemProtectionToggle != null) itemProtectionToggle.Text = "🛑 Stop Protection";
            }
            else
            {
                btnHeroConnect.Text = "⚡ START PROTECTION";
                btnHeroConnect.BackColor = clrAccent;
                lblHeroStatus.Text = "○ DISCONNECTED";
                lblHeroStatus.ForeColor = clrMuted;
                lblHeroDesc.Text = "Ready to bypass DPI using Turkey preset";

                SetTrayText("bralti dns - Stopped");
                if (itemProtectionToggle != null) itemProtectionToggle.Text = "⚡ Start Protection";
            }
        }

        // ==================== Service Management ====================

        private void ToggleServiceAction()
        {
            if (IsServiceInstalled())
                RemoveWindowsService();
            else
                InstallWindowsService();
        }

        private void InstallWindowsService()
        {
            if (isRunning) StopProtection();

            string arch = Environment.Is64BitOperatingSystem ? "x86_64" : "x86";
            string exePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, arch, "goodbyedpi.exe");
            string args = txtArguments != null ? txtArguments.Text.Trim() : "-5 --set-ttl 5 --dns-addr 77.88.8.8 --dns-port 1253 --dnsv6-addr 2a02:6b8::feed:0ff --dnsv6-port 1253";

            AppendLog("📦 Installing bralti dns Windows Service...");
            RunSc("stop \"GoodbyeDPI\"");
            RunSc("delete \"GoodbyeDPI\"");

            string createCmd = string.Format("create \"GoodbyeDPI\" binPath= \"\\\"{0}\\\" {1}\" start= \"auto\"", exePath, args);
            string createRes = RunSc(createCmd);
            AppendLog(createRes);

            RunSc("description \"GoodbyeDPI\" \"bralti dns Circumvention Service - bralti.com\"");
            string startRes = RunSc("start \"GoodbyeDPI\"");
            AppendLog(startRes);

            UpdateServiceStatusUI();

            if (IsServiceRunning())
            {
                SetStatus(true, true);
                ShowTrayNotification("bralti dns Service", "✅ Windows Service running in background!");
                MessageBox.Show("Windows Service installed successfully!\nIt runs automatically on boot.", "Service Installed", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("Service result:\n" + startRes, "Service Status", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void RemoveWindowsService()
        {
            AppendLog("🗑️ Removing Windows Service...");
            RunSc("stop \"GoodbyeDPI\"");
            RunSc("delete \"GoodbyeDPI\"");
            RunSc("stop \"WinDivert\"");
            RunSc("delete \"WinDivert\"");
            RunSc("stop \"WinDivert14\"");
            RunSc("delete \"WinDivert14\"");

            AppendLog("Service removed.");
            UpdateServiceStatusUI();
            SetStatus(false);

            ShowTrayNotification("bralti dns Service", "🗑️ Windows Service removed.");
            MessageBox.Show("Windows Service removed.", "Service Removed", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private string RunSc(string arguments)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo("sc.exe", arguments);
                psi.CreateNoWindow = true;
                psi.UseShellExecute = false;
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError = true;
                using (Process p = Process.Start(psi))
                {
                    string output = p.StandardOutput.ReadToEnd();
                    p.WaitForExit(3000);
                    return output.Trim();
                }
            }
            catch (Exception ex)
            {
                return "Error: " + ex.Message;
            }
        }

        private bool IsServiceInstalled()
        {
            string outStr = RunSc("query \"GoodbyeDPI\"");
            return outStr.Contains("SERVICE_NAME: GoodbyeDPI");
        }

        private bool IsServiceRunning()
        {
            string outStr = RunSc("query \"GoodbyeDPI\"");
            return outStr.Contains("STATE") && outStr.Contains("RUNNING");
        }

        private void UpdateServiceStatusUI()
        {
            bool installed = IsServiceInstalled();
            bool running = IsServiceRunning();

            if (lblServiceStatusText != null)
            {
                if (running)
                {
                    lblServiceStatusText.Text = "Running in background (Auto-start on boot)";
                    lblServiceStatusText.ForeColor = clrAccent;
                }
                else if (installed)
                {
                    lblServiceStatusText.Text = "Installed (Stopped)";
                    lblServiceStatusText.ForeColor = Color.FromArgb(245, 158, 11);
                }
                else
                {
                    lblServiceStatusText.Text = "Not installed (Optional background mode)";
                    lblServiceStatusText.ForeColor = clrMuted;
                }
            }

            if (btnServiceToggle != null)
            {
                btnServiceToggle.Text = installed ? "Remove" : "Install";
                btnServiceToggle.BackColor = installed ? clrDanger : Color.FromArgb(45, 60, 85);
            }

            if (itemServiceToggle != null)
            {
                itemServiceToggle.Text = installed ? "🗑️ Remove Windows Service" : "📦 Install Windows Service";
            }
        }

        // ==================== App Startup (GUI App Only) ====================

        private void ToggleAppStartupAction()
        {
            bool currentState = IsStartupEnabledInRegistry();
            bool newState = !currentState;
            ToggleStartupRegistry(newState);
            UpdateStartupStatusUI();

            if (newState)
                ShowTrayNotification("App Startup", "✅ Bralti DNS will launch into System Tray on login.");
            else
                ShowTrayNotification("App Startup", "⚪ Bralti DNS startup disabled.");
        }

        private void UpdateStartupStatusUI()
        {
            bool enabled = IsStartupEnabledInRegistry();
            if (lblStartupStatusText != null)
            {
                if (enabled)
                {
                    lblStartupStatusText.Text = "Enabled (Launches into System Tray on login)";
                    lblStartupStatusText.ForeColor = clrAccent;
                }
                else
                {
                    lblStartupStatusText.Text = "Disabled (Manual launch only)";
                    lblStartupStatusText.ForeColor = clrMuted;
                }
            }

            if (btnStartupToggle != null)
            {
                btnStartupToggle.Text = enabled ? "Disable" : "Enable";
                btnStartupToggle.BackColor = enabled ? Color.FromArgb(120, 35, 45) : clrPrimary;
            }

            if (itemStartupToggle != null)
            {
                itemStartupToggle.Text = enabled ? "❌ Disable Windows Startup" : "🚀 Enable Windows Startup";
            }
        }

        private bool IsStartupEnabledInRegistry()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", false))
                {
                    if (key != null)
                        return key.GetValue("BraltiDNS") != null;
                }
            }
            catch { }
            return false;
        }

        private void ToggleStartupRegistry(bool enable)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    if (key != null)
                    {
                        string appName = "BraltiDNS";
                        if (enable)
                        {
                            string exePath = "\"" + Application.ExecutablePath + "\" --minimized";
                            key.SetValue(appName, exePath);
                        }
                        else
                        {
                            key.DeleteValue(appName, false);
                        }
                    }
                }
                SaveConfig();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not update registry: " + ex.Message, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void OpenWebsite()
        {
            try
            {
                Process.Start(new ProcessStartInfo(WebsiteUrl) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not open website: " + ex.Message, "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        // ==================== Presets & Config ====================

        private void CmbPresets_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateArgumentsFromPreset();
            if (!isInitializing)
            {
                RestartProtectionForNewPreset();
            }
        }

        private void RestartProtectionForNewPreset()
        {
            string presetName = GetShortPresetName();

            // Scenario 1: Windows Service is currently running
            if (IsServiceRunning())
            {
                if (lblHeroStatus != null)
                {
                    lblHeroStatus.Text = "● RESTARTING SERVICE...";
                    lblHeroStatus.ForeColor = Color.FromArgb(245, 158, 11);
                }
                if (lblHeroDesc != null)
                {
                    lblHeroDesc.Text = "Applying " + presetName + "...";
                }
                Application.DoEvents();

                AppendLog("🔄 Preset changed to: " + presetName);
                AppendLog("Stopping Windows Service to apply new parameters...");
                RunSc("stop \"GoodbyeDPI\"");
                Thread.Sleep(300);

                string arch = Environment.Is64BitOperatingSystem ? "x86_64" : "x86";
                string exePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, arch, "goodbyedpi.exe");
                string newArgs = txtArguments != null ? txtArguments.Text.Trim() : "-5";

                // Reconfigure the service with the new arguments
                RunSc(string.Format("config \"GoodbyeDPI\" binPath= \"\\\"{0}\\\" {1}\"", exePath, newArgs));
                RunSc("start \"GoodbyeDPI\"");
                Thread.Sleep(300);

                UpdateServiceStatusUI();
                SetStatus(true, true);
                AppendLog("✅ Service restarted with " + presetName + "!");
                ShowTrayNotification("bralti dns Service", "🔄 Applied " + presetName + " & restarted service!");
            }
            // Scenario 2: GUI Process protection is active OR user changed preset
            else
            {
                if (lblHeroStatus != null)
                {
                    lblHeroStatus.Text = "● APPLYING PRESET...";
                    lblHeroStatus.ForeColor = Color.FromArgb(245, 158, 11);
                }
                if (lblHeroDesc != null)
                {
                    lblHeroDesc.Text = "Applying " + presetName + "...";
                }
                Application.DoEvents();

                AppendLog("🔄 Preset changed to: " + presetName);
                if (isRunning || dpiProcess != null)
                {
                    AppendLog("Restarting protection with new parameters...");
                    StopProtectionSilently();
                    Thread.Sleep(300);
                }

                StartProtection();
                // Ensure state and button are explicitly synchronized to STOP PROTECTION
                SetStatus(true);
            }
        }

        private void UpdateArgumentsFromPreset()
        {
            if (txtArguments == null) return;
            switch (cmbPresets.SelectedIndex)
            {
                case 0: // Turkey Default
                    txtArguments.Text = "-5 --set-ttl 5 --dns-addr 77.88.8.8 --dns-port 1253 --dnsv6-addr 2a02:6b8::feed:0ff --dnsv6-port 1253";
                    break;
                case 1: // Superonline Alt 1
                    txtArguments.Text = "--set-ttl 3";
                    break;
                case 2: // Superonline Alt 2
                    txtArguments.Text = "-5";
                    break;
                case 3: // Aggressive
                    txtArguments.Text = "-9 --dns-addr 77.88.8.8 --dns-port 1253 --dnsv6-addr 2a02:6b8::feed:0ff --dnsv6-port 1253";
                    break;
                case 4: // Standard Mode 1
                    txtArguments.Text = "-1";
                    break;
                case 5: // Custom
                    break;
            }
            SaveConfig();
        }

        private void SetTrayText(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                trayIcon.Text = "bralti dns";
                return;
            }
            if (text.Length > 63)
            {
                text = text.Substring(0, 60) + "...";
            }
            trayIcon.Text = text;
        }

        public void ShowTrayNotification(string title, string message)
        {
            try
            {
                if (trayIcon != null)
                {
                    trayIcon.Visible = true;
                    trayIcon.BalloonTipTitle = title;
                    trayIcon.BalloonTipText = message;
                    trayIcon.BalloonTipIcon = ToolTipIcon.Info;
                    trayIcon.ShowBalloonTip(4000);
                }
            }
            catch { }
        }

        private string GetShortPresetName()
        {
            if (cmbPresets == null) return "Active";
            switch (cmbPresets.SelectedIndex)
            {
                case 0: return "Turkey Default";
                case 1: return "Superonline 1";
                case 2: return "Superonline 2";
                case 3: return "Aggressive -9";
                case 4: return "Compatible -1";
                case 5: return "Custom";
                default: return "Active";
            }
        }

        private void AppendLog(string message)
        {
            if (txtLogs == null || txtLogs.IsDisposed) return;
            if (txtLogs.InvokeRequired)
            {
                txtLogs.BeginInvoke(new Action<string>(AppendLog), message);
                return;
            }
            txtLogs.AppendText(string.Format("[{0:HH:mm:ss}] {1}\r\n", DateTime.Now, message));
        }

        private void RestoreWindow()
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
            this.ShowInTaskbar = true;
            this.BringToFront();
            this.Activate();
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.Hide();
                this.ShowInTaskbar = false;
                ShowTrayNotification("bralti dns", "bralti dns is running in the system tray (⌃).");
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!isExiting && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide();
                this.ShowInTaskbar = false;
                ShowTrayNotification("bralti dns", "bralti dns is running in the system tray. Right-click the icon to exit.");
            }
            else
            {
                StopProtection();
                trayIcon.Visible = false;
            }
        }

        private void SaveConfig()
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("[Settings]");
                sb.AppendLine("PresetIndex=" + (cmbPresets != null ? cmbPresets.SelectedIndex : 0));
                sb.AppendLine("Arguments=" + (txtArguments != null ? txtArguments.Text.Replace("\r", "").Replace("\n", " ") : ""));
                File.WriteAllText(configPath, sb.ToString(), Encoding.UTF8);
            }
            catch { }
        }

        private void LoadConfig()
        {
            try
            {
                if (!File.Exists(configPath)) return;
                string[] lines = File.ReadAllLines(configPath, Encoding.UTF8);
                foreach (string line in lines)
                {
                    string[] parts = line.Split(new char[] { '=' }, 2);
                    if (parts.Length != 2) continue;
                    string key = parts[0].Trim();
                    string val = parts[1].Trim();

                    if (key == "PresetIndex" && cmbPresets != null)
                    {
                        int idx;
                        if (int.TryParse(val, out idx) && idx >= 0 && idx < cmbPresets.Items.Count)
                            cmbPresets.SelectedIndex = idx;
                    }
                    else if (key == "Arguments" && txtArguments != null && !string.IsNullOrEmpty(val))
                    {
                        txtArguments.Text = val;
                    }
                }
            }
            catch { }
        }
    }
}
