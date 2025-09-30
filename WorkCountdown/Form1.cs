using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using Newtonsoft.Json;
using System.Reflection;

namespace WorkCountdown
{
    public partial class Form1 : Form
    {
        private const string SAVE_FILE = "start_time_log.json";
        private const int WORK_HOURS = 8;
        private System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
        private System.Windows.Forms.Timer dailyResetTimer = new System.Windows.Forms.Timer(); // 跨天重置计时器
        private DateTime startTime;
        private DateTime offworkTime;
        private NotifyIcon trayIcon = new NotifyIcon();
        private string currentDate; // 用于跟踪当前日期

        public Form1()
        {
            InitializeComponent();
            InitializeTimer();
            InitializeDailyResetTimer(); // 初始化跨天重置计时器
            InitializeTrayIcon();
            SetFormIcon();

            // 记录当前日期（基于凌晨4点的逻辑日期）
            currentDate = GetLogicalDate(DateTime.Now);

            // 检查是否需要重置
            CheckAndResetForNewDay();

            // 初始提示
            lblStartTime.Text = "请点击「新的牛马一天」开始工作计时";
            lblOffworkTime.Text = "预计下班时间: 未开始";
            lblCountdown.Text = "倒计时: 未开始";
        }

        // 初始化跨天重置计时器（设置为凌晨4点重置）
        private void InitializeDailyResetTimer()
        {
            // 计算距离下次凌晨4点的毫秒数
            var now = DateTime.Now;
            var nextResetTime = GetNextResetTime(now);
            var interval = nextResetTime - now;

            // 设置初始间隔
            dailyResetTimer.Interval = (int)interval.TotalMilliseconds;
            dailyResetTimer.Tick += DailyResetTimer_Tick;
            dailyResetTimer.Start();
        }

        // 跨天重置计时器事件
        private void DailyResetTimer_Tick(object sender, EventArgs e)
        {
            // 执行重置操作
            CheckAndResetForNewDay();

            // 重置计时器为24小时后再次触发
            dailyResetTimer.Interval = 86400000; // 24小时 = 86400000毫秒
        }

        // 检查并处理重置（基于凌晨4点的逻辑）
        private void CheckAndResetForNewDay()
        {
            string todayStr = GetLogicalDate(DateTime.Now);

            // 如果逻辑日期已变更，执行重置操作
            if (todayStr != currentDate)
            {
                currentDate = todayStr;

                // 停止当前计时器
                timer.Stop();

                // 重置UI显示
                lblStartTime.Text = "新的一天开始了，请点击「新的牛马一天」开始工作计时";
                lblOffworkTime.Text = "预计下班时间: 未开始";
                lblCountdown.Text = "倒计时: 未开始";

                // 显示通知
                trayIcon.ShowBalloonTip(5000, "工作倒计时", "新的一天开始了！", ToolTipIcon.Info);
            }
        }

        // 获取基于凌晨4点的逻辑日期
        // 例如：凌晨3点属于前一天，凌晨5点属于当天
        private string GetLogicalDate(DateTime time)
        {
            DateTime logicalDate = time.Hour < 4 ? time.AddDays(-1) : time;
            return logicalDate.ToString("yyyy-MM-dd");
        }

        // 计算下次凌晨4点的时间
        private DateTime GetNextResetTime(DateTime now)
        {
            DateTime resetTime = now.Date.AddHours(4); // 今天凌晨4点
            if (now >= resetTime)
            {
                resetTime = resetTime.AddDays(1); // 如果已经过了今天凌晨4点，则设置为明天凌晨4点
            }
            return resetTime;
        }

        // 设置窗口图标
        private void SetFormIcon()
        {
            try
            {
                // 读取嵌入式资源中的图标
                Icon windowIcon = GetEmbeddedIcon("favicon.ico");
                if (windowIcon != null)
                {
                    this.Icon = windowIcon;
                }
                else
                {
                    this.Icon = SystemIcons.Application; // 备用：系统默认图标
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

        // 初始化托盘图标
        private void InitializeTrayIcon()
        {
            trayIcon = new NotifyIcon();

            try
            {
                // 从嵌入式资源读取图标（无需外部文件）
                Icon trayIconFromResource = GetEmbeddedIcon("favicon.ico");
                if (trayIconFromResource != null)
                {
                    trayIcon.Icon = trayIconFromResource;
                    Console.WriteLine("✅ 从嵌入式资源加载托盘图标成功");
                }
                else
                {
                    trayIcon.Icon = SystemIcons.Application; // 备用：系统默认图标
                    Console.WriteLine("❌ 未找到嵌入式图标，使用系统默认图标");
                }
            }
            catch (Exception ex)
            {
                trayIcon.Icon = SystemIcons.Application;
                Console.WriteLine($"❌ 加载托盘图标出错: {ex.Message}");
            }

            // 后续托盘菜单、交互逻辑不变（保持原来的）
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
                // 获取当前程序的程序集（EXE本身）
                Assembly currentAssembly = Assembly.GetExecutingAssembly();
                // 嵌入式资源的完整名称格式：项目命名空间.图标文件名（必须准确）
                string resourceFullName = $"{currentAssembly.GetName().Name}.{iconFileName}";

                // 从程序集中读取资源流
                using (Stream iconStream = currentAssembly.GetManifestResourceStream(resourceFullName))
                {
                    if (iconStream != null)
                    {
                        // 将资源流转换为Icon对象
                        return new Icon(iconStream);
                    }
                }

                // 若未找到资源，打印提示（方便调试）
                Console.WriteLine($"❌ 未找到嵌入式资源: {resourceFullName}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 读取嵌入式图标出错: {ex.Message}");
                return null;
            }
        }




        private void OpenLogForm(object sender, EventArgs e)
        {
            List<WorkRecord> logs = LoadLogs();
            LogForm logForm = new LogForm(logs);
            logForm.ShowDialog();
        }

        private void LoadOrInitStartTime()
        {
            DateTime now = DateTime.Now;
            List<WorkRecord> logs = LoadLogs();
            string todayStr = GetLogicalDate(now); // 使用逻辑日期
            WorkRecord todayRecord = logs.Find(item => item.Date == todayStr);

            if (todayRecord != null && !string.IsNullOrEmpty(todayRecord.StartTime))
            {
                startTime = DateTime.Parse(todayRecord.StartTime);
            }
            else
            {
                todayRecord = new WorkRecord
                {
                    Date = todayStr,
                    StartTime = now.ToString("yyyy-MM-dd HH:mm:ss"),
                    OffworkTime = ""
                };
                logs.Add(todayRecord);
                SaveLogs(logs);
                startTime = now;
            }

            offworkTime = startTime.AddHours(WORK_HOURS + 1);
            UpdateLabels();
        }

        private void UpdateLabels()
        {
            lblStartTime.Text = $"工作开始时间: {startTime:yyyy-MM-dd HH:mm:ss}";
            lblOffworkTime.Text = $"预计下班时间: {offworkTime:yyyy-MM-dd HH:mm:ss}";
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            // 每次计时检查是否需要重置
            CheckAndResetForNewDay();

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

            TimeSpan elapsed = now - startTime - lunchTimePassed;
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
                    trayIcon.ShowBalloonTip(5000, "工作倒计时", "下班时间到啦！🎉", ToolTipIcon.Info);
                    return;
                }

                lblCountdown.Text = $"倒计时: {remaining.Hours:D2}:{remaining.Minutes:D2}:{remaining.Seconds:D2}";
            }
        }

        private List<WorkRecord> LoadLogs()
        {
            if (!File.Exists(SAVE_FILE))
                return new List<WorkRecord>();

            try
            {
                string json = File.ReadAllText(SAVE_FILE);
                var result = JsonConvert.DeserializeObject<List<WorkRecord>>(json);
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
                string json = JsonConvert.SerializeObject(logs, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(SAVE_FILE, json);
            }
            catch { }
        }

        private void btnNewDay_Click(object sender, EventArgs e)
        {
            LoadOrInitStartTime();
            timer.Start();
        }

        private void btnClockOff_Click(object sender, EventArgs e)
        {
            DateTime now = DateTime.Now;
            List<WorkRecord> logs = LoadLogs();
            string todayStr = GetLogicalDate(now); // 使用逻辑日期
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
            MessageBox.Show($"下班时间已记录:\n{now:yyyy-MM-dd HH:mm:ss}", "下班打卡");
            timer.Stop();
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
                trayIcon.ShowBalloonTip(2000, "工作倒计时", "程序已最小化到托盘", ToolTipIcon.Info);
            }
            base.OnFormClosing(e);
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void Form1_Load_1(object sender, EventArgs e)
        {

        }
    }

    public class WorkRecord
    {
        public string Date { get; set; } = "";
        public string StartTime { get; set; } = "";
        public string OffworkTime { get; set; } = "";
    }
}
