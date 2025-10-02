using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using System.Text.Json;
using System.Drawing;
using System.Runtime.InteropServices;

namespace WorkCountdown
{
    /// <summary>
    /// 打卡日志记录窗体 - 用于查看和编辑工作记录（支持黑夜模式）
    /// </summary>
    public partial class LogForm : Form
    {
        // 数据成员
        private List<WorkRecord> logs;           // 工作记录列表
        private string logFilePath;              // 日志文件保存路径
        private bool isDarkMode;                 // 是否启用黑夜模式

        // Windows API
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        // UI控件成员
        private DataGridView dataGridViewLogs = new DataGridView();
        private DataGridViewTextBoxColumn colDate = new DataGridViewTextBoxColumn();
        private DataGridViewTextBoxColumn colStartTime = new DataGridViewTextBoxColumn();
        private DataGridViewTextBoxColumn colOffworkTime = new DataGridViewTextBoxColumn();
        private DataGridViewTextBoxColumn colDuration = new DataGridViewTextBoxColumn();
        private Button btnClose = new Button();
        private Button btnSave = new Button();

        // 颜色定义
        private readonly Color LightBackColor = SystemColors.Control;
        private readonly Color LightForeColor = SystemColors.ControlText;
        private readonly Color LightButtonBackColor = SystemColors.ControlLight;
        private readonly Color DarkBackColor = Color.FromArgb(45, 45, 48);
        private readonly Color DarkForeColor = Color.White;
        private readonly Color DarkButtonBackColor = Color.FromArgb(63, 63, 70);
        private readonly Color DarkGridBackColor = Color.FromArgb(37, 37, 38);
        private readonly Color DarkGridForeColor = Color.White;
        private readonly Color DarkGridHeaderBackColor = Color.FromArgb(51, 51, 51);
        private readonly Color DarkGridSelectionBackColor = Color.FromArgb(75, 75, 80);
        private readonly Color DarkGridSelectionForeColor = Color.White;

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
                // 如果 API 调用失败，忽略错误
            }
        }

        /// <summary>
        /// 构造函数 - 初始化日志窗体
        /// </summary>
        /// <param name="workLogs">工作记录数据</param>
        /// <param name="filePath">日志文件路径</param>
        /// <param name="darkMode">是否启用黑夜模式</param>
        public LogForm(List<WorkRecord> workLogs, string filePath, bool darkMode = false)
        {
            logs = workLogs ?? new List<WorkRecord>();
            logFilePath = filePath;
            isDarkMode = darkMode;
            InitializeComponent();
            LoadLogData();

            // 确保窗口句柄已创建后再应用主题
            this.HandleCreated += (s, e) => {
                ApplyTheme();
            };
        }

        /// <summary>
        /// 应用主题
        /// </summary>
        private void ApplyTheme()
        {
            Color backColor, foreColor, buttonBackColor, gridBackColor, gridForeColor, gridHeaderBackColor, gridSelectionBackColor, gridSelectionForeColor;

            if (isDarkMode)
            {
                backColor = DarkBackColor;
                foreColor = DarkForeColor;
                buttonBackColor = DarkButtonBackColor;
                gridBackColor = DarkGridBackColor;
                gridForeColor = DarkGridForeColor;
                gridHeaderBackColor = DarkGridHeaderBackColor;
                gridSelectionBackColor = DarkGridSelectionBackColor;
                gridSelectionForeColor = DarkGridSelectionForeColor;

                // 设置窗口黑暗模式
                SetWindowDarkMode(this.Handle, true);
            }
            else
            {
                backColor = LightBackColor;
                foreColor = LightForeColor;
                buttonBackColor = LightButtonBackColor;
                gridBackColor = SystemColors.Window;
                gridForeColor = SystemColors.ControlText;
                gridHeaderBackColor = SystemColors.Control;
                gridSelectionBackColor = SystemColors.Highlight;
                gridSelectionForeColor = SystemColors.HighlightText;

                // 关闭窗口黑暗模式
                SetWindowDarkMode(this.Handle, false);
            }

            // 应用颜色到窗体
            this.BackColor = backColor;
            this.ForeColor = foreColor;

            // 应用颜色到按钮
            btnClose.BackColor = buttonBackColor;
            btnClose.ForeColor = foreColor;
            btnSave.BackColor = buttonBackColor;
            btnSave.ForeColor = foreColor;

            // 应用颜色到DataGridView
            dataGridViewLogs.BackgroundColor = gridBackColor;
            dataGridViewLogs.ForeColor = gridForeColor;
            dataGridViewLogs.DefaultCellStyle.BackColor = gridBackColor;
            dataGridViewLogs.DefaultCellStyle.ForeColor = gridForeColor;
            dataGridViewLogs.DefaultCellStyle.SelectionBackColor = gridSelectionBackColor;
            dataGridViewLogs.DefaultCellStyle.SelectionForeColor = gridSelectionForeColor;
            dataGridViewLogs.ColumnHeadersDefaultCellStyle.BackColor = gridHeaderBackColor;
            dataGridViewLogs.ColumnHeadersDefaultCellStyle.ForeColor = foreColor;
            dataGridViewLogs.EnableHeadersVisualStyles = false;
        }

        /// <summary>
        /// 手动实现窗体控件初始化（替代设计器生成的代码）
        /// </summary>
        private void InitializeComponent()
        {
            this.SuspendLayout();

            // 初始化DataGridView
            dataGridViewLogs = new DataGridView();
            ((System.ComponentModel.ISupportInitialize)(dataGridViewLogs)).BeginInit();

            // 初始化列
            colDate = new DataGridViewTextBoxColumn();
            colStartTime = new DataGridViewTextBoxColumn();
            colOffworkTime = new DataGridViewTextBoxColumn();
            colDuration = new DataGridViewTextBoxColumn();

            // 初始化按钮
            btnClose = new Button();
            btnSave = new Button();

            // ========== 配置DataGridView ==========
            dataGridViewLogs.AllowUserToAddRows = false;         // 禁止用户添加行
            dataGridViewLogs.AllowUserToDeleteRows = false;      // 禁止用户删除行
            dataGridViewLogs.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewLogs.Columns.AddRange(new DataGridViewColumn[] {
                colDate, colStartTime, colOffworkTime, colDuration
            });
            dataGridViewLogs.Location = new System.Drawing.Point(12, 12);
            dataGridViewLogs.Name = "dataGridViewLogs";
            dataGridViewLogs.ReadOnly = false;                   // 允许编辑
            dataGridViewLogs.RowTemplate.Height = 25;
            dataGridViewLogs.Size = new System.Drawing.Size(640, 350);
            dataGridViewLogs.TabIndex = 0;

            // 绑定事件
            dataGridViewLogs.CellDoubleClick += dataGridViewLogs_CellDoubleClick;
            dataGridViewLogs.CellEndEdit += dataGridViewLogs_CellEndEdit;

            // ========== 配置列属性 ==========
            colDate.HeaderText = "日期";
            colDate.Name = "colDate";
            colDate.Width = 120;

            colStartTime.HeaderText = "上班时间";
            colStartTime.Name = "colStartTime";
            colStartTime.Width = 150;

            colOffworkTime.HeaderText = "下班时间";
            colOffworkTime.Name = "colOffworkTime";
            colOffworkTime.Width = 150;

            colDuration.HeaderText = "实际工作时长(已扣午休)";
            colDuration.Name = "colDuration";
            colDuration.ReadOnly = true;                         // 时长列只读（自动计算）
            colDuration.Width = 180;

            // ========== 配置关闭按钮 ==========
            btnClose.Location = new System.Drawing.Point(577, 368);
            btnClose.Name = "btnClose";
            btnClose.Size = new System.Drawing.Size(75, 23);
            btnClose.TabIndex = 1;
            btnClose.Text = "关闭";
            btnClose.UseVisualStyleBackColor = true;
            btnClose.Click += btnClose_Click;

            // ========== 配置保存按钮 ==========
            btnSave.Location = new System.Drawing.Point(496, 368);
            btnSave.Name = "btnSave";
            btnSave.Size = new System.Drawing.Size(75, 23);
            btnSave.TabIndex = 2;
            btnSave.Text = "保存修改";
            btnSave.UseVisualStyleBackColor = true;
            btnSave.Click += btnSave_Click;

            // ========== 配置窗体属性 ==========
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(664, 403);
            this.Controls.Add(btnSave);
            this.Controls.Add(btnClose);
            this.Controls.Add(dataGridViewLogs);
            this.Name = "LogForm";
            this.Text = "打卡日志记录";
            this.FormBorderStyle = FormBorderStyle.FixedDialog; // 固定大小
            this.MaximizeBox = false; // 禁用最大化

            ((System.ComponentModel.ISupportInitialize)(dataGridViewLogs)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        /// <summary>
        /// 加载日志数据到DataGridView并计算工作时长
        /// </summary>
        private void LoadLogData()
        {
            dataGridViewLogs.Rows.Clear();

            // 按日期降序排序（最新的记录在前面）
            logs.Sort((x, y) => DateTime.Compare(DateTime.Parse(y.Date), DateTime.Parse(x.Date)));

            foreach (var record in logs)
            {
                string workDuration = "未记录完整";

                // 只有同时有上班和下班时间时才计算工作时长
                if (!string.IsNullOrEmpty(record.StartTime) && !string.IsNullOrEmpty(record.OffworkTime))
                {
                    if (DateTime.TryParse(record.StartTime, out DateTime start) &&
                        DateTime.TryParse(record.OffworkTime, out DateTime end))
                    {
                        TimeSpan duration = end - start;

                        // 扣除1小时午休时间（如果总时长超过1小时）
                        if (duration.TotalHours > 1)
                        {
                            duration = duration - TimeSpan.FromHours(1);
                        }

                        // 格式化为 HH:mm:ss 显示
                        workDuration = $"{duration.Hours:D2}:{duration.Minutes:D2}:{duration.Seconds:D2}";
                    }
                }

                // 添加行到DataGridView
                dataGridViewLogs.Rows.Add(
                    record.Date,
                    record.StartTime,
                    record.OffworkTime,
                    workDuration
                );
            }
        }

        /// <summary>
        /// DataGridView单元格双击事件 - 进入编辑模式
        /// </summary>
        private void dataGridViewLogs_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            // 只允许编辑前3列（日期、上班时间、下班时间），第4列（时长）只读
            if (e.RowIndex >= 0 && e.ColumnIndex >= 0 && e.ColumnIndex != 3)
            {
                dataGridViewLogs.BeginEdit(true);
            }
        }

        /// <summary>
        /// DataGridView单元格编辑完成事件 - 更新数据模型
        /// </summary>
        private void dataGridViewLogs_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            // 验证行索引有效性
            if (e.RowIndex >= 0 && e.RowIndex < logs.Count)
            {
                var record = logs[e.RowIndex];
                var cellValue = dataGridViewLogs.Rows[e.RowIndex].Cells[e.ColumnIndex].Value?.ToString() ?? "";

                // 根据列索引更新对应的字段
                switch (e.ColumnIndex)
                {
                    case 0: // 日期列
                        if (DateTime.TryParse(cellValue, out DateTime date))
                        {
                            record.Date = date.ToString("yyyy-MM-dd");
                        }
                        break;
                    case 1: // 上班时间列
                        if (DateTime.TryParse(cellValue, out DateTime startTime))
                        {
                            record.StartTime = startTime.ToString("yyyy-MM-dd HH:mm:ss");
                        }
                        break;
                    case 2: // 下班时间列
                        if (DateTime.TryParse(cellValue, out DateTime offworkTime))
                        {
                            record.OffworkTime = offworkTime.ToString("yyyy-MM-dd HH:mm:ss");
                        }
                        break;
                }

                // 更新工作时长显示
                UpdateDuration(e.RowIndex);
            }
        }

        /// <summary>
        /// 更新指定行的工作时长显示
        /// </summary>
        /// <param name="rowIndex">行索引</param>
        private void UpdateDuration(int rowIndex)
        {
            if (rowIndex >= 0 && rowIndex < logs.Count)
            {
                var record = logs[rowIndex];
                string workDuration = "未记录完整";

                // 重新计算工作时长
                if (!string.IsNullOrEmpty(record.StartTime) && !string.IsNullOrEmpty(record.OffworkTime))
                {
                    if (DateTime.TryParse(record.StartTime, out DateTime start) &&
                        DateTime.TryParse(record.OffworkTime, out DateTime end))
                    {
                        TimeSpan duration = end - start;

                        // 扣除1小时午休
                        if (duration.TotalHours > 1)
                        {
                            duration = duration - TimeSpan.FromHours(1);
                        }

                        workDuration = $"{duration.Hours:D2}:{duration.Minutes:D2}:{duration.Seconds:D2}";
                    }
                }

                // 更新时长列显示
                dataGridViewLogs.Rows[rowIndex].Cells[3].Value = workDuration;
            }
        }

        /// <summary>
        /// 保存按钮点击事件 - 将修改保存到文件
        /// </summary>
        private void btnSave_Click(object sender, EventArgs e)
        {
            try
            {
                // 将修改后的日志序列化并写入文件
                string json = JsonSerializer.Serialize(logs, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(logFilePath, json);
                MessageBox.Show(this, "日志修改已成功保存到文件", "保存成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"保存失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 关闭按钮点击事件 - 关闭窗体
        /// </summary>
        private void btnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// 窗体加载事件 - 确保主题正确应用
        /// </summary>
        private void LogForm_Load(object sender, EventArgs e)
        {
            // 确保主题正确应用
            ApplyTheme();
        }
    }
}