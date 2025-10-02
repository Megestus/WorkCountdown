using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using System.Text.Json;
using System.Reflection;

namespace WorkCountdown
{
    public partial class Form1 : Form
    {
        private const string SAVE_FILE = "start_time_log.json";
        private const int WORK_HOURS = 8;
        private System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
        private System.Windows.Forms.Timer dailyResetTimer = new System.Windows.Forms.Timer();
        private DateTime startTime;
        private DateTime offworkTime;
        private NotifyIcon trayIcon = new NotifyIcon();
        private string currentDate;

        // ========== 暂停功能相关变量 ==========
        private bool isPaused = false;                    // 暂停状态标志
        private DateTime pauseStartTime;                  // 暂停开始时间
        private TimeSpan totalPausedTime = TimeSpan.Zero; // 累计暂停时间
        private Button btnPause = new Button();           // 暂停按钮

        public Form1()
        {
            InitializeComponent();

            // 设置窗口初始大小
            this.Size = new Size(280, 350);
            // 设置最小大小和最大大小相同
            this.MinimumSize = new Size(280, 350);
            this.MaximizeBox = false;
            this.MinimizeBox = false; // 可选，是否禁用最小化按钮

            InitializeTimer();
            InitializeDailyResetTimer();
            InitializeTrayIcon();
            SetFormIcon();

            // 调整所有按钮布局为垂直排列
            ArrangeButtonsVertically();

            currentDate = GetLogicalDate(DateTime.Now);
            CheckAndResetForNewDay();

            lblStartTime.Text = "请点击「新的牛马一天」开始工作计时";
            lblOffworkTime.Text = "预计下班时间: 未开始";
            lblCountdown.Text = "倒计时: 未开始";
        }

        /// <summary>
        /// 将所有按钮垂直排列
        /// </summary>
        private void ArrangeButtonsVertically()
        {
            int buttonWidth = 200;
            int buttonHeight = 30;
            int startY = 150; // 起始Y坐标
            int spacing = 10; // 按钮间距

            // 调整"新的牛马一天"按钮
            btnNewDay.Location = new System.Drawing.Point(35, startY);
            btnNewDay.Size = new System.Drawing.Size(buttonWidth, buttonHeight);
            // 确保事件绑定正确
            btnNewDay.Click += btnNewDay_Click;

            // 初始化并调整暂停按钮
            InitializePauseButton();
            btnPause.Location = new System.Drawing.Point(35, startY + buttonHeight + spacing);
            btnPause.Size = new System.Drawing.Size(buttonWidth, buttonHeight);

            // 调整"下班打卡"按钮
            btnClockOff.Location = new System.Drawing.Point(35, startY + (buttonHeight + spacing) * 2);
            btnClockOff.Size = new System.Drawing.Size(buttonWidth, buttonHeight);
            // 确保事件只绑定一次
            btnClockOff.Click -= btnClockOff_Click; // 先移除可能存在的绑定
            btnClockOff.Click += btnClockOff_Click;

            // 调整"查看日志"按钮
            btnViewLogs.Location = new System.Drawing.Point(35, startY + (buttonHeight + spacing) * 3);
            btnViewLogs.Size = new System.Drawing.Size(buttonWidth, buttonHeight);
            // 确保事件绑定正确
            btnViewLogs.Click += btnViewLogs_Click;

            // 调整窗体大小以适应新布局
            this.Height = Math.Max(this.Height, startY + (buttonHeight + spacing) * 4 + 50);
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

            // 将暂停按钮添加到窗体
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
                // 进入暂停状态
                isPaused = true;
                pauseStartTime = DateTime.Now;
                btnPause.Text = "继续计时";
                lblCountdown.Text = "计时已暂停 ⏸️";

                //trayIcon.ShowBalloonTip(1500, "工作倒计时", "计时已暂停", ToolTipIcon.Info);
            }
            else
            {
                // 结束暂停状态
                isPaused = false;
                totalPausedTime += DateTime.Now - pauseStartTime;
                btnPause.Text = "暂停计时";

                //trayIcon.ShowBalloonTip(1500, "工作倒计时", "计时已恢复", ToolTipIcon.Info);
            }
        }

        /// <summary>
        /// 重置暂停状态（在新的一天或计时结束时调用）
        /// </summary>
        private void ResetPauseState()
        {
            isPaused = false;
            totalPausedTime = TimeSpan.Zero;
            btnPause.Text = "暂停计时";
        }

        /// <summary>
        /// 修改后的计时器Tick事件 - 增加暂停处理逻辑
        /// </summary>
        private void Timer_Tick(object sender, EventArgs e)
        {
            // 每次计时检查是否需要重置
            CheckAndResetForNewDay();

            // 如果处于暂停状态，不更新倒计时
            if (isPaused)
            {
                return;
            }

            DateTime now = DateTime.Now;

            // 计算午休时间
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

            // 计算实际工作时间（扣除午休和累计暂停时间）
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
                    // 重置暂停状态
                    ResetPauseState();
                    trayIcon.ShowBalloonTip(2000, "工作倒计时", "下班时间到啦！🎉", ToolTipIcon.Info);
                    return;
                }

                lblCountdown.Text = $"倒计时: {remaining.Hours:D2}:{remaining.Minutes:D2}:{remaining.Seconds:D2}";
            }
        }

        /// <summary>
        /// 修改"新的牛马一天"按钮事件 - 重置暂停状态
        /// </summary>
        private void btnNewDay_Click(object sender, EventArgs e)
        {
            // 重置暂停状态
            ResetPauseState();

            // 加载或初始化开始时间
            LoadOrInitStartTime();

            // 启动计时器
            timer.Start();

            // 更新界面显示
            UpdateLabels();

            // 显示开始消息
            trayIcon.ShowBalloonTip(3000, "工作倒计时", "新的牛马一天开始了！", ToolTipIcon.Info);
        }

        /// <summary>
        /// 修改跨天重置方法 - 重置暂停状态
        /// </summary>
        private void CheckAndResetForNewDay()
        {
            string todayStr = GetLogicalDate(DateTime.Now);

            if (todayStr != currentDate)
            {
                currentDate = todayStr;
                timer.Stop();
                ResetPauseState(); // 重置暂停状态

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

        private bool isLogFormOpen = false;

        private void OpenLogForm(object sender, EventArgs e)
        {
            // 防止重复打开
            if (isLogFormOpen)
                return;

            try
            {
                isLogFormOpen = true;

                List<WorkRecord> logs = LoadLogs();
                LogForm logForm = new LogForm(logs, SAVE_FILE);

                // 设置表单关闭事件
                logForm.FormClosed += (s, args) => {
                    isLogFormOpen = false;
                };

                // 使用 Show 而不是 ShowDialog，并将主窗体作为所有者
                logForm.Show(this);
            }
            catch (Exception ex)
            {
                isLogFormOpen = false;
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

            offworkTime = startTime.AddHours(WORK_HOURS + 1); // +1 小时午休时间
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

        private bool isProcessingClockOff = false;

        private void btnClockOff_Click(object sender, EventArgs e)
        {
            // 防止重复处理
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

                // 使用更简洁的消息框
                MessageBox.Show(this, $"下班时间已记录:\n{now:yyyy-MM-dd HH:mm:ss}", "下班打卡",
                               MessageBoxButtons.OK, MessageBoxIcon.Information);

                timer.Stop();
                ResetPauseState(); // 重置暂停状态
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
}