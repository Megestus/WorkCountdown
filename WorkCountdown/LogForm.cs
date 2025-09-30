using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace WorkCountdown
{
    // 确保类声明包含partial关键字
    public partial class LogForm : Form
    {
        private List<WorkRecord> logs;
        private string logFilePath; // 新增：日志文件路径
        private DataGridView dataGridViewLogs = new DataGridView();
        private DataGridViewTextBoxColumn colDate = new DataGridViewTextBoxColumn();
        private DataGridViewTextBoxColumn colStartTime = new DataGridViewTextBoxColumn();
        private DataGridViewTextBoxColumn colOffworkTime = new DataGridViewTextBoxColumn();
        private DataGridViewTextBoxColumn colDuration = new DataGridViewTextBoxColumn();
        private Button btnClose = new Button();
        private Button btnSave = new Button();

        // 新增：接收日志文件路径的构造函数
        public LogForm(List<WorkRecord> workLogs, string filePath)
        {
            // 手动调用初始化方法（替代设计器生成的调用）
            InitializeComponent();
            logs = workLogs ?? new List<WorkRecord>();
            logFilePath = filePath; // 保存文件路径
            LoadLogData();
        }

        private void LoadLogData()
        {
            dataGridViewLogs.Rows.Clear();
            logs.Sort((x, y) => DateTime.Compare(DateTime.Parse(y.Date), DateTime.Parse(x.Date)));

            foreach (var record in logs)
            {
                string workDuration = "未记录完整";
                if (!string.IsNullOrEmpty(record.StartTime) && !string.IsNullOrEmpty(record.OffworkTime))
                {
                    if (DateTime.TryParse(record.StartTime, out DateTime start) &&
                        DateTime.TryParse(record.OffworkTime, out DateTime end))
                    {
                        TimeSpan duration = end - start;
                        if (duration.TotalHours > 1)
                        {
                            duration = duration - TimeSpan.FromHours(1);
                        }
                        workDuration = $"{duration.Hours:D2}:{duration.Minutes:D2}:{duration.Seconds:D2}";
                    }
                }

                dataGridViewLogs.Rows.Add(
                    record.Date,
                    record.StartTime,
                    record.OffworkTime,
                    workDuration
                );
            }
        }

        // 手动实现InitializeComponent方法（之前缺失的部分）
        private void InitializeComponent()
        {
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

            // 配置DataGridView
            dataGridViewLogs.AllowUserToAddRows = false;
            dataGridViewLogs.AllowUserToDeleteRows = false;
            dataGridViewLogs.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewLogs.Columns.AddRange(new DataGridViewColumn[] {
                colDate, colStartTime, colOffworkTime, colDuration
            });
            dataGridViewLogs.Location = new System.Drawing.Point(12, 12);
            dataGridViewLogs.Name = "dataGridViewLogs";
            dataGridViewLogs.ReadOnly = false;
            dataGridViewLogs.RowTemplate.Height = 25;
            dataGridViewLogs.Size = new System.Drawing.Size(640, 350);
            dataGridViewLogs.TabIndex = 0;
            dataGridViewLogs.CellDoubleClick += dataGridViewLogs_CellDoubleClick;
            dataGridViewLogs.CellEndEdit += dataGridViewLogs_CellEndEdit;

            // 配置列
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
            colDuration.ReadOnly = true;
            colDuration.Width = 180;

            // 配置关闭按钮
            btnClose.Location = new System.Drawing.Point(577, 368);
            btnClose.Name = "btnClose";
            btnClose.Size = new System.Drawing.Size(75, 23);
            btnClose.TabIndex = 1;
            btnClose.Text = "关闭";
            btnClose.UseVisualStyleBackColor = true;
            btnClose.Click += btnClose_Click;

            // 配置保存按钮
            btnSave.Location = new System.Drawing.Point(496, 368);
            btnSave.Name = "btnSave";
            btnSave.Size = new System.Drawing.Size(75, 23);
            btnSave.TabIndex = 2;
            btnSave.Text = "保存修改";
            btnSave.UseVisualStyleBackColor = true;
            btnSave.Click += btnSave_Click;

            // 配置窗体
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(664, 403);
            this.Controls.Add(btnSave);
            this.Controls.Add(btnClose);
            this.Controls.Add(dataGridViewLogs);
            this.Name = "LogForm";
            this.Text = "打卡日志记录";

            ((System.ComponentModel.ISupportInitialize)(dataGridViewLogs)).EndInit();
            this.ResumeLayout(false);
        }

        // 双击编辑事件
        private void dataGridViewLogs_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex >= 0 && e.ColumnIndex != 3)
            {
                dataGridViewLogs.BeginEdit(true);
            }
        }

        // 编辑完成事件
        private void dataGridViewLogs_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.RowIndex < logs.Count)
            {
                var record = logs[e.RowIndex];
                var cellValue = dataGridViewLogs.Rows[e.RowIndex].Cells[e.ColumnIndex].Value?.ToString() ?? "";

                switch (e.ColumnIndex)
                {
                    case 0:
                        if (DateTime.TryParse(cellValue, out DateTime date))
                        {
                            record.Date = date.ToString("yyyy-MM-dd");
                        }
                        break;
                    case 1:
                        if (DateTime.TryParse(cellValue, out DateTime startTime))
                        {
                            record.StartTime = startTime.ToString("yyyy-MM-dd HH:mm:ss");
                        }
                        break;
                    case 2:
                        if (DateTime.TryParse(cellValue, out DateTime offworkTime))
                        {
                            record.OffworkTime = offworkTime.ToString("yyyy-MM-dd HH:mm:ss");
                        }
                        break;
                }
                UpdateDuration(e.RowIndex);
            }
        }

        // 更新时长
        private void UpdateDuration(int rowIndex)
        {
            if (rowIndex >= 0 && rowIndex < logs.Count)
            {
                var record = logs[rowIndex];
                string workDuration = "未记录完整";

                if (!string.IsNullOrEmpty(record.StartTime) && !string.IsNullOrEmpty(record.OffworkTime))
                {
                    if (DateTime.TryParse(record.StartTime, out DateTime start) &&
                        DateTime.TryParse(record.OffworkTime, out DateTime end))
                    {
                        TimeSpan duration = end - start;
                        if (duration.TotalHours > 1)
                        {
                            duration = duration - TimeSpan.FromHours(1);
                        }
                        workDuration = $"{duration.Hours:D2}:{duration.Minutes:D2}:{duration.Seconds:D2}";
                    }
                }

                dataGridViewLogs.Rows[rowIndex].Cells[3].Value = workDuration;
            }
        }

        // 保存按钮事件（新增：写入文件逻辑）
        private void btnSave_Click(object sender, EventArgs e)
        {
            try
            {
                // 将修改后的日志序列化并写入文件
                string json = JsonConvert.SerializeObject(logs, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(logFilePath, json);
                MessageBox.Show("日志修改已成功保存到文件", "保存成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
