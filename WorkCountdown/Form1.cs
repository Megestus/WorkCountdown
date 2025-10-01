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
        // 常量定义
        private const string SAVE_FILE = "start_time_log.json"; // 日志文件保存路径
        private const int WORK_HOURS = 8; // 标准工作时长（小时）

        // 计时器组件
        private System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer(); // 主工作计时器
        private System.Windows.Forms.Timer dailyResetTimer = new System.Windows.Forms.Timer(); // 跨天重置计时器

        // 时间变量
        private DateTime startTime; // 工作开始时间
        private DateTime offworkTime; // 预计下班时间

        // UI组件
        private NotifyIcon trayIcon = new NotifyIcon(); // 系统托盘图标

        // 状态变量
        private string currentDate; // 用于跟踪当前逻辑日期（基于凌晨4点逻辑）

        public Form1()
        {
            InitializeComponent();
            InitializeTimer(); // 初始化主计时器
            InitializeDailyResetTimer(); // 初始化跨天重置计时器
            InitializeTrayIcon(); // 初始化托盘图标
            SetFormIcon(); // 设置窗口图标

            // 记录当前逻辑日期（基于凌晨4点的逻辑日期）
            currentDate = GetLogicalDate(DateTime.Now);

            // 检查是否需要重置（处理程序启动时的日期变更）
            CheckAndResetForNewDay();

            // 初始UI提示文本
            lblStartTime.Text = "请点击「新的牛马一天」开始工作计时";
            lblOffworkTime.Text = "预计下班时间: 未开始";
            lblCountdown.Text = "倒计时: 未开始";
        }

        /// <summary>
        /// 初始化跨天重置计时器（设置为凌晨4点重置）
        /// </summary>
        private void InitializeDailyResetTimer()
        {
            // 计算距离下次凌晨4点的毫秒数
            var now = DateTime.Now;
            var nextResetTime = GetNextResetTime(now);
            var interval = nextResetTime - now;

            // 设置初始间隔（精确到下次凌晨4点）
            dailyResetTimer.Interval = (int)interval.TotalMilliseconds;
            dailyResetTimer.Tick += DailyResetTimer_Tick;
            dailyResetTimer.Start();
        }

        /// <summary>
        /// 跨天重置计时器事件处理
        /// </summary>
        private void DailyResetTimer_Tick(object sender, EventArgs e)
        {
            // 执行重置操作
            CheckAndResetForNewDay();

            // 重置计时器为24小时后再次触发（后续每天固定间隔）
            dailyResetTimer.Interval = 86400000; // 24小时 = 86400000毫秒
        }

        /// <summary>
        /// 检查并处理新的一天重置（基于凌晨4点的逻辑）
        /// </summary>
        private void CheckAndResetForNewDay()
        {
            string todayStr = GetLogicalDate(DateTime.Now);

            // 如果逻辑日期已变更，执行重置操作
            if (todayStr != currentDate)
            {
                currentDate = todayStr;

                // 停止当前工作计时器
                timer.Stop();

                // 重置UI显示
                lblStartTime.Text = "新的一天开始了，请点击「新的牛马一天」开始工作计时";
                lblOffworkTime.Text = "预计下班时间: 未开始";
                lblCountdown.Text = "倒计时: 未开始";

                // 显示系统通知
                trayIcon.ShowBalloonTip(5000, "工作倒计时", "新的一天开始了！", ToolTipIcon.Info);
            }
        }

        /// <summary>
        /// 获取基于凌晨4点的逻辑日期
        /// 逻辑：凌晨3点属于前一天，凌晨5点属于当天
        /// </summary>
        /// <param name="time">当前时间</param>
        /// <returns>逻辑日期字符串（yyyy-MM-dd）</returns>
        private string GetLogicalDate(DateTime time)
        {
            DateTime logicalDate = time.Hour < 4 ? time.AddDays(-1) : time;
            return logicalDate.ToString("yyyy-MM-dd");
        }

        /// <summary>
        /// 计算下次凌晨4点重置时间
        /// </summary>
        /// <param name="now">当前时间</param>
        /// <returns>下次重置时间</returns>
        private DateTime GetNextResetTime(DateTime now)
        {
            DateTime resetTime = now.Date.AddHours(4); // 今天凌晨4点
            if (now >= resetTime)
            {
                resetTime = resetTime.AddDays(1); // 如果已经过了今天凌晨4点，则设置为明天凌晨4点
            }
            return resetTime;
        }

        /// <summary>
        /// 设置窗口图标（从嵌入式资源加载）
        /// </summary>
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

        /// <summary>
        /// 初始化主工作计时器
        /// </summary>
        private void InitializeTimer()
        {
            timer.Interval = 1000; // 1秒间隔
            timer.Tick += Timer_Tick;
            timer.Stop(); // 初始状态为停止
        }

        /// <summary>
        /// 初始化系统托盘图标
        /// </summary>
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

            // 设置托盘图标属性
            trayIcon.Text = "工作倒计时";

            // 创建托盘右键菜单
            ContextMenuStrip trayMenu = new ContextMenuStrip();
            trayMenu.Items.Add("显示窗口", null, ShowWindow);
            trayMenu.Items.Add("查看日志", null, OpenLogForm);
            trayMenu.Items.Add("退出程序", null, QuitApp);
            trayIcon.ContextMenuStrip = trayMenu;

            trayIcon.Visible = true;
            trayIcon.DoubleClick += ShowWindow; // 双击托盘图标显示窗口
        }

        /// <summary>
        /// 从嵌入式资源获取图标
        /// </summary>
        /// <param name="iconFileName">图标文件名</param>
        /// <returns>图标对象，失败返回null</returns>
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

        /// <summary>
        /// 打开日志查看窗口
        /// </summary>
        private void OpenLogForm(object sender, EventArgs e)
        {
            List<WorkRecord> logs = LoadLogs();
            LogForm logForm = new LogForm(logs, SAVE_FILE);
            logForm.ShowDialog();
        }

        /// <summary>
        /// 加载或初始化当天的工作开始时间
        /// </summary>
        private void LoadOrInitStartTime()
        {
            DateTime now = DateTime.Now;
            List<WorkRecord> logs = LoadLogs();
            string todayStr = GetLogicalDate(now); // 使用逻辑日期
            WorkRecord todayRecord = logs.Find(item => item.Date == todayStr);

            if (todayRecord != null && !string.IsNullOrEmpty(todayRecord.StartTime))
            {
                // 如果已有当天记录，使用保存的开始时间
                startTime = DateTime.Parse(todayRecord.StartTime);
            }
            else
            {
                // 如果没有当天记录，创建新记录
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

            // 计算预计下班时间（工作8小时+1小时午休）
            offworkTime = startTime.AddHours(WORK_HOURS + 1);
            UpdateLabels();
        }

        /// <summary>
        /// 更新界面标签显示
        /// </summary>
        private void UpdateLabels()
        {
            lblStartTime.Text = $"工作开始时间: {startTime:yyyy-MM-dd HH:mm:ss}";
            lblOffworkTime.Text = $"预计下班时间: {offworkTime:yyyy-MM-dd HH:mm:ss}";
        }

        /// <summary>
        /// 主计时器Tick事件 - 每秒执行一次
        /// </summary>
        private void Timer_Tick(object sender, EventArgs e)
        {
            // 每次计时检查是否需要重置（处理运行中的日期变更）
            CheckAndResetForNewDay();

            DateTime now = DateTime.Now;
            DateTime lunchStart = now.Date.AddHours(12); // 午休开始时间：12:00
            DateTime lunchEnd = now.Date.AddHours(13);   // 午休结束时间：13:00

            // 计算午休时间扣除
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

            // 计算实际工作时间和剩余时间
            TimeSpan elapsed = now - startTime - lunchTimePassed;
            TimeSpan totalWork = TimeSpan.FromHours(WORK_HOURS);
            TimeSpan remaining = totalWork - elapsed;

            // 午休时间特殊处理
            if (lunchStart <= now && now < lunchEnd)
            {
                lblCountdown.Text = "当前为午休时间，倒计时已暂停 🛑";
            }
            else
            {
                // 检查是否到达下班时间
                if (remaining.TotalSeconds <= 0)
                {
                    lblCountdown.Text = "下班时间到啦！🎉";
                    timer.Stop();
                    trayIcon.ShowBalloonTip(5000, "工作倒计时", "下班时间到啦！🎉", ToolTipIcon.Info);
                    return;
                }

                // 正常倒计时显示
                lblCountdown.Text = $"倒计时: {remaining.Hours:D2}:{remaining.Minutes:D2}:{remaining.Seconds:D2}";
            }
        }

        /// <summary>
        /// 从文件加载工作记录日志
        /// </summary>
        /// <returns>工作记录列表</returns>
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

        /// <summary>
        /// 保存工作记录到文件
        /// </summary>
        /// <param name="logs">工作记录列表</param>
        private void SaveLogs(List<WorkRecord> logs)
        {
            try
            {
                string json = JsonConvert.SerializeObject(logs, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(SAVE_FILE, json);
            }
            catch { } // 静默处理保存错误
        }

        /// <summary>
        /// "新的牛马一天"按钮点击事件 - 开始工作计时
        /// </summary>
        private void btnNewDay_Click(object sender, EventArgs e)
        {
            LoadOrInitStartTime();
            timer.Start(); // 启动计时器
        }

        /// <summary>
        /// "下班打卡"按钮点击事件 - 记录下班时间
        /// </summary>
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
                // 如果没有开始记录，创建完整记录
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
            timer.Stop(); // 停止计时器
        }

        /// <summary>
        /// "查看日志"按钮点击事件
        /// </summary>
        private void btnViewLogs_Click(object sender, EventArgs e)
        {
            OpenLogForm(sender, e);
        }

        /// <summary>
        /// 显示主窗口（从托盘恢复）
        /// </summary>
        private void ShowWindow(object sender, EventArgs e)
        {
            Show();
            WindowState = FormWindowState.Normal;
            Activate();
        }

        /// <summary>
        /// 退出应用程序
        /// </summary>
        private void QuitApp(object sender, EventArgs e)
        {
            trayIcon.Visible = false;
            Application.Exit();
        }

        /// <summary>
        /// 窗体关闭事件处理 - 实现最小化到托盘
        /// </summary>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true; // 取消关闭操作
                Hide(); // 隐藏窗口
                trayIcon.ShowBalloonTip(2000, "工作倒计时", "程序已最小化到托盘", ToolTipIcon.Info);
            }
            base.OnFormClosing(e);
        }

        // 窗体加载事件（空实现）
        private void Form1_Load(object sender, EventArgs e) 
        { 

        }
        private void Form1_Load_1(object sender, EventArgs e) 
        { 

        }
    }

    /// <summary>
    /// 工作记录数据模型类
    /// </summary>
    public class WorkRecord
    {
        public string Date { get; set; } = "";           // 日期（逻辑日期）
        public string StartTime { get; set; } = "";      // 开始工作时间
        public string OffworkTime { get; set; } = "";    // 下班打卡时间
    }
}