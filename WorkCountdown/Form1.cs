using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using System.Text.Json;
using System.Reflection;
using System.Drawing;
using System.Runtime.InteropServices;


namespace WorkCountdown
{
    public partial class Form1 : Form
    {

        // Windows API 常量和方法
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        /// <summary>
        /// 设置窗口黑暗模式
        /// </summary>
        private void SetWindowDarkMode(IntPtr handle, bool darkMode)
        {
            try
            {
                int darkModeValue = darkMode ? 1 : 0;
                DwmSetWindowAttribute(handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkModeValue, sizeof(int));
            }
            catch
            {
                // 如果 API 调用失败，忽略错误（可能是不支持的操作系统版本）
            }
        }



        private const string SAVE_FILE = "start_time_log.json";
        private const string SETTINGS_FILE = "app_settings.json";
        private const int WORK_HOURS = 8;
        private System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
        private System.Windows.Forms.Timer dailyResetTimer = new System.Windows.Forms.Timer();
        private DateTime startTime;
        private DateTime offworkTime;
        private NotifyIcon trayIcon = new NotifyIcon();
        private string currentDate;

        // ========== 暂停功能相关变量 ==========
        private bool isPaused = false;
        private DateTime pauseStartTime;
        private TimeSpan totalPausedTime = TimeSpan.Zero;
        private Button btnPause = new Button();

        // ========== 黑夜模式相关变量 ==========
        private bool isDarkMode = false;
        private Button btnToggleTheme = new Button();
        private bool isProcessingClockOff = false;
        private bool isLogFormOpen = false;

        // 浅色模式 - 使用系统颜色
        private readonly Color LightBackColor = SystemColors.Window;
        private readonly Color LightForeColor = SystemColors.WindowText;
        private readonly Color LightButtonBackColor = SystemColors.Control;

        // 深色模式 - 使用 Windows 深色主题颜色
        private readonly Color DarkBackColor = Color.FromArgb(32, 32, 32);
        private readonly Color DarkForeColor = Color.FromArgb(255, 255, 255);
        private readonly Color DarkButtonBackColor = Color.FromArgb(51, 51, 51);

        public Form1()
        {
            InitializeComponent();

            // 加载设置
            LoadSettings();

            // 设置窗口初始大小
            this.Size = new Size(300, 400);
            this.MinimumSize = new Size(300, 400);
            this.MaximizeBox = false;

            InitializeTimer();
            InitializeDailyResetTimer();
            InitializeTrayIcon();
            SetFormIcon();

            // 调整所有按钮布局为垂直排列
            ArrangeButtonsVertically();

            // 确保窗口句柄已创建后再应用主题
            this.HandleCreated += (s, e) => {
                ApplyTheme();
            };

            currentDate = GetLogicalDate(DateTime.Now);
            CheckAndResetForNewDay();

            lblStartTime.Text = "请点击「新的牛马一天」开始工作计时";
            lblOffworkTime.Text = "预计下班时间: 未开始";
            lblCountdown.Text = "倒计时: 未开始";
        }

        /// <summary>
        /// 加载应用设置
        /// </summary>
        private void LoadSettings()
        {
            if (!File.Exists(SETTINGS_FILE))
                return;

            try
            {
                string json = File.ReadAllText(SETTINGS_FILE);
                var settings = JsonSerializer.Deserialize<AppSettings>(json);
                if (settings != null)
                {
                    isDarkMode = settings.IsDarkMode;
                }
            }
            catch
            {
                // 如果加载失败，使用默认设置
                isDarkMode = false;
            }
        }

        /// <summary>
        /// 保存应用设置
        /// </summary>
        private void SaveSettings()
        {
            try
            {
                var settings = new AppSettings { IsDarkMode = isDarkMode };
                string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SETTINGS_FILE, json);
            }
            catch { }
        }

        /// <summary>
        /// 应用主题
        /// </summary>
        private void ApplyTheme()
        {
            Color backColor, foreColor, buttonBackColor;

            if (isDarkMode)
            {
                backColor = DarkBackColor;
                foreColor = DarkForeColor;
                buttonBackColor = DarkButtonBackColor;
                btnToggleTheme.Text = "☀️ 白天模式";

                // 设置窗口黑暗模式
                SetWindowDarkMode(this.Handle, true);
            }
            else
            {
                backColor = LightBackColor;
                foreColor = LightForeColor;
                buttonBackColor = LightButtonBackColor;
                btnToggleTheme.Text = "🌙 黑夜模式";

                // 关闭窗口黑暗模式
                SetWindowDarkMode(this.Handle, false);
            }

            // 应用颜色到窗体
            this.BackColor = backColor;
            this.ForeColor = foreColor;

            // 应用颜色到所有控件
            ApplyColorToControl(this, backColor, foreColor, buttonBackColor);
        }

        /// <summary>
        /// 递归应用颜色到控件及其子控件
        /// </summary>
        private void ApplyColorToControl(Control control, Color backColor, Color foreColor, Color buttonBackColor)
        {
            foreach (Control ctrl in control.Controls)
            {
                // 设置控件颜色
                if (ctrl is Button || ctrl is Panel)
                {
                    ctrl.BackColor = buttonBackColor;
                    ctrl.ForeColor = foreColor;
                }
                else if (ctrl is Label || ctrl is TextBox)
                {
                    ctrl.BackColor = backColor;
                    ctrl.ForeColor = foreColor;
                }

                // 递归设置子控件
                if (ctrl.HasChildren)
                {
                    ApplyColorToControl(ctrl, backColor, foreColor, buttonBackColor);
                }
            }
        }

        /// <summary>
        /// 切换主题按钮点击事件
        /// </summary>
        private void btnToggleTheme_Click(object sender, EventArgs e)
        {
            isDarkMode = !isDarkMode;
            ApplyTheme();
            SaveSettings();
        }

        /// <summary>
        /// 将所有按钮垂直排列
        /// </summary>
        private void ArrangeButtonsVertically()
        {
            int buttonWidth = 200;
            int buttonHeight = 30;
            int startY = 150;
            int spacing = 10;

            // 调整"新的牛马一天"按钮
            btnNewDay.Location = new System.Drawing.Point(50, startY);
            btnNewDay.Size = new System.Drawing.Size(buttonWidth, buttonHeight);
            btnNewDay.Click -= btnNewDay_Click;
            btnNewDay.Click += btnNewDay_Click;

            // 初始化并调整暂停按钮
            InitializePauseButton();
            btnPause.Location = new System.Drawing.Point(50, startY + buttonHeight + spacing);
            btnPause.Size = new System.Drawing.Size(buttonWidth, buttonHeight);

            // 调整"下班打卡"按钮
            btnClockOff.Location = new System.Drawing.Point(50, startY + (buttonHeight + spacing) * 2);
            btnClockOff.Size = new System.Drawing.Size(buttonWidth, buttonHeight);
            btnClockOff.Click -= btnClockOff_Click;
            btnClockOff.Click += btnClockOff_Click;

            // 调整"查看日志"按钮
            btnViewLogs.Location = new System.Drawing.Point(50, startY + (buttonHeight + spacing) * 3);
            btnViewLogs.Size = new System.Drawing.Size(buttonWidth, buttonHeight);
            btnViewLogs.Click -= btnViewLogs_Click;
            btnViewLogs.Click += btnViewLogs_Click;

            // 添加主题切换按钮
            btnToggleTheme.Location = new System.Drawing.Point(50, startY + (buttonHeight + spacing) * 4);
            btnToggleTheme.Size = new System.Drawing.Size(buttonWidth, buttonHeight);
            btnToggleTheme.UseVisualStyleBackColor = true;
            btnToggleTheme.Click += btnToggleTheme_Click;
            this.Controls.Add(btnToggleTheme);

            // 调整窗体大小以适应新布局
            this.Height = Math.Max(this.Height, startY + (buttonHeight + spacing) * 5 + 50);
        }

        /// <summary>
        /// 初始化暂停按钮
        /// </summary>
        private void InitializePauseButton()
        {
            btnPause.Name = "btnPause";
            btnPause.Text = "暂停计时";
            btnPause.UseVisualStyleBackColor = true;
            btnPause.Click += btnPause_Click;
            this.Controls.Add(btnPause);
        }

        /// <summary>
        /// 暂停/继续按钮点击事件
        /// </summary>
        private void btnPause_Click(object sender, EventArgs e)
        {
            if (!timer.Enabled)
            {
                MessageBox.Show("请先开始计时", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (!isPaused)
            {
                isPaused = true;
                pauseStartTime = DateTime.Now;
                btnPause.Text = "继续计时";
                lblCountdown.Text = "计时已暂停 ⏸️";
            }
            else
            {
                isPaused = false;
                totalPausedTime += DateTime.Now - pauseStartTime;
                btnPause.Text = "暂停计时";
            }
        }

        /// <summary>
        /// 重置暂停状态
        /// </summary>
        private void ResetPauseState()
        {
            isPaused = false;
            totalPausedTime = TimeSpan.Zero;
            btnPause.Text = "暂停计时";
        }

        /// <summary>
        /// 计时器Tick事件
        /// </summary>
        private void Timer_Tick(object sender, EventArgs e)
        {
            CheckAndResetForNewDay();

            if (isPaused)
            {
                return;
            }

            DateTime now = DateTime.Now;
            DateTime lunchStart = now.Date.AddHours(12);
            DateTime lunchEnd = now.Date.AddHours(13);

            TimeSpan lunchTimePassed = TimeSpan.Zero;
            if (startTime < lunchEnd && now > lunchStart)
            {
                DateTime lunchCoverStart = startTime > lunchStart ? startTime : lunchStart;
                DateTime lunchCoverEnd = now < lunchEnd ? now : lunchEnd;
                if (lunchCoverEnd > lunchCoverStart)
                {
                    lunchTimePassed = lunchCoverEnd - lunchCoverStart;
                }
            }

            TimeSpan elapsed = now - startTime - lunchTimePassed - totalPausedTime;
            TimeSpan totalWork = TimeSpan.FromHours(WORK_HOURS);
            TimeSpan remaining = totalWork - elapsed;

            if (lunchStart <= now && now < lunchEnd)
            {
                lblCountdown.Text = "当前为午休时间，倒计时已暂停 🛑";
            }
            else
            {
                if (remaining.TotalSeconds <= 0)
                {
                    lblCountdown.Text = "下班时间到啦！🎉";
                    timer.Stop();
                    ResetPauseState();
                    trayIcon.ShowBalloonTip(2000, "工作倒计时", "下班时间到啦！🎉", ToolTipIcon.Info);
                    return;
                }

                lblCountdown.Text = $"倒计时: {remaining.Hours:D2}:{remaining.Minutes:D2}:{remaining.Seconds:D2}";
            }
        }

        /// <summary>
        /// "新的牛马一天"按钮事件
        /// </summary>
        private void btnNewDay_Click(object sender, EventArgs e)
        {
            ResetPauseState();
            LoadOrInitStartTime();
            timer.Start();
            UpdateLabels();
            trayIcon.ShowBalloonTip(3000, "工作倒计时", "新的牛马一天开始了！", ToolTipIcon.Info);
        }

        /// <summary>
        /// 跨天重置方法
        /// </summary>
        private void CheckAndResetForNewDay()
        {
            string todayStr = GetLogicalDate(DateTime.Now);

            if (todayStr != currentDate)
            {
                currentDate = todayStr;
                timer.Stop();
                ResetPauseState();

                lblStartTime.Text = "新的一天开始了，请点击「新的牛马一天」开始工作计时";
                lblOffworkTime.Text = "预计下班时间: 未开始";
                lblCountdown.Text = "倒计时: 未开始";

                trayIcon.ShowBalloonTip(2000, "工作倒计时", "新的一天开始了！", ToolTipIcon.Info);
            }
        }

        // ========== 以下是原有方法保持不变 ==========

        private void InitializeDailyResetTimer()
        {
            var now = DateTime.Now;
            var nextResetTime = GetNextResetTime(now);
            var interval = nextResetTime - now;

            dailyResetTimer.Interval = (int)interval.TotalMilliseconds;
            dailyResetTimer.Tick += DailyResetTimer_Tick;
            dailyResetTimer.Start();
        }

        private void DailyResetTimer_Tick(object sender, EventArgs e)
        {
            CheckAndResetForNewDay();
            dailyResetTimer.Interval = 86400000;
        }

        private string GetLogicalDate(DateTime time)
        {
            DateTime logicalDate = time.Hour < 4 ? time.AddDays(-1) : time;
            return logicalDate.ToString("yyyy-MM-dd");
        }

        private DateTime GetNextResetTime(DateTime now)
        {
            DateTime resetTime = now.Date.AddHours(4);
            if (now >= resetTime)
            {
                resetTime = resetTime.AddDays(1);
            }
            return resetTime;
        }

        private void SetFormIcon()
        {
            try
            {
                Icon windowIcon = GetEmbeddedIcon("favicon.ico");
                if (windowIcon != null)
                {
                    this.Icon = windowIcon;
                }
                else
                {
                    this.Icon = SystemIcons.Application;
                }
            }
            catch
            {
                this.Icon = SystemIcons.Application;
            }
        }

        private void InitializeTimer()
        {
            timer.Interval = 1000;
            timer.Tick += Timer_Tick;
            timer.Stop();
        }

        private void InitializeTrayIcon()
        {
            trayIcon = new NotifyIcon();

            try
            {
                Icon trayIconFromResource = GetEmbeddedIcon("favicon.ico");
                if (trayIconFromResource != null)
                {
                    trayIcon.Icon = trayIconFromResource;
                }
                else
                {
                    trayIcon.Icon = SystemIcons.Application;
                }
            }
            catch (Exception ex)
            {
                trayIcon.Icon = SystemIcons.Application;
            }

            trayIcon.Text = "工作倒计时";
            ContextMenuStrip trayMenu = new ContextMenuStrip();
            trayMenu.Items.Add("显示窗口", null, ShowWindow);
            trayMenu.Items.Add("查看日志", null, OpenLogForm);
            trayMenu.Items.Add("退出程序", null, QuitApp);
            trayIcon.ContextMenuStrip = trayMenu;
            trayIcon.Visible = true;
            trayIcon.DoubleClick += ShowWindow;
        }

        private Icon GetEmbeddedIcon(string iconFileName)
        {
            try
            {
                Assembly currentAssembly = Assembly.GetExecutingAssembly();
                string resourceFullName = $"{currentAssembly.GetName().Name}.{iconFileName}";

                using (Stream iconStream = currentAssembly.GetManifestResourceStream(resourceFullName))
                {
                    if (iconStream != null)
                    {
                        return new Icon(iconStream);
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        private void OpenLogForm(object sender, EventArgs e)
        {
            if (isLogFormOpen)
                return;

            try
            {
                isLogFormOpen = true;
                this.Enabled = false;

                List<WorkRecord> logs = LoadLogs();
                LogForm logForm = new LogForm(logs, SAVE_FILE, isDarkMode);

                logForm.FormClosed += (s, args) => {
                    isLogFormOpen = false;
                    this.Enabled = true;
                };

                logForm.Show(this);
            }
            catch (Exception ex)
            {
                isLogFormOpen = false;
                this.Enabled = true;
                MessageBox.Show($"打开日志窗口时出错: {ex.Message}");
            }
        }

        private void LoadOrInitStartTime()
        {
            DateTime now = DateTime.Now;
            List<WorkRecord> logs = LoadLogs();
            string todayStr = GetLogicalDate(now);
            WorkRecord todayRecord = logs.Find(item => item.Date == todayStr);

            if (todayRecord != null && !string.IsNullOrEmpty(todayRecord.StartTime))
            {
                startTime = DateTime.Parse(todayRecord.StartTime);
            }
            else
            {
                startTime = now;
                todayRecord = new WorkRecord
                {
                    Date = todayStr,
                    StartTime = now.ToString("yyyy-MM-dd HH:mm:ss"),
                    OffworkTime = ""
                };
                logs.Add(todayRecord);
                SaveLogs(logs);
            }

            offworkTime = startTime.AddHours(WORK_HOURS + 1);
        }

        private void UpdateLabels()
        {
            lblStartTime.Text = $"工作开始时间: {startTime:yyyy-MM-dd HH:mm:ss}";
            lblOffworkTime.Text = $"预计下班时间: {offworkTime:yyyy-MM-dd HH:mm:ss}";
        }

        private List<WorkRecord> LoadLogs()
        {
            if (!File.Exists(SAVE_FILE))
                return new List<WorkRecord>();

            try
            {
                string json = File.ReadAllText(SAVE_FILE);
                var result = JsonSerializer.Deserialize<List<WorkRecord>>(json);
                return result != null ? result : new List<WorkRecord>();
            }
            catch
            {
                return new List<WorkRecord>();
            }
        }

        private void SaveLogs(List<WorkRecord> logs)
        {
            try
            {
                string json = JsonSerializer.Serialize(logs, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SAVE_FILE, json);
            }
            catch { }
        }

        private void btnClockOff_Click(object sender, EventArgs e)
        {
            if (isProcessingClockOff) return;

            try
            {
                isProcessingClockOff = true;

                DateTime now = DateTime.Now;
                List<WorkRecord> logs = LoadLogs();
                string todayStr = GetLogicalDate(now);
                WorkRecord todayRecord = logs.Find(item => item.Date == todayStr);

                if (todayRecord != null)
                {
                    todayRecord.OffworkTime = now.ToString("yyyy-MM-dd HH:mm:ss");
                }
                else
                {
                    todayRecord = new WorkRecord
                    {
                        Date = todayStr,
                        StartTime = now.ToString("yyyy-MM-dd HH:mm:ss"),
                        OffworkTime = now.ToString("yyyy-MM-dd HH:mm:ss")
                    };
                    logs.Add(todayRecord);
                }

                SaveLogs(logs);
                MessageBox.Show(this, $"下班时间已记录:\n{now:yyyy-MM-dd HH:mm:ss}", "下班打卡",
                               MessageBoxButtons.OK, MessageBoxIcon.Information);
                timer.Stop();
                ResetPauseState();
                lblCountdown.Text = "已打卡下班";
            }
            finally
            {
                isProcessingClockOff = false;
            }
        }

        private void btnViewLogs_Click(object sender, EventArgs e)
        {
            OpenLogForm(sender, e);
        }

        private void ShowWindow(object sender, EventArgs e)
        {
            Show();
            WindowState = FormWindowState.Normal;
            Activate();
        }

        private void QuitApp(object sender, EventArgs e)
        {
            trayIcon.Visible = false;
            Application.Exit();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
                trayIcon.ShowBalloonTip(1000, "工作倒计时", "程序已最小化到托盘", ToolTipIcon.Info);
            }
            base.OnFormClosing(e);
        }

        private void Form1_Load(object sender, EventArgs e) { }
        private void Form1_Load_1(object sender, EventArgs e) { }
    }

    public class WorkRecord
    {
        public string Date { get; set; } = "";
        public string StartTime { get; set; } = "";
        public string OffworkTime { get; set; } = "";
    }

    public class AppSettings
    {
        public bool IsDarkMode { get; set; } = false;
    }
}