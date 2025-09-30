using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace WorkCountdown
{
    public partial class LogForm : Form
    {
        private List<WorkRecord> logs;
        // 直接初始化控件，避免null
        private DataGridView dataGridViewLogs = new DataGridView();
        private DataGridViewTextBoxColumn colDate = new DataGridViewTextBoxColumn();
        private DataGridViewTextBoxColumn colStartTime = new DataGridViewTextBoxColumn();
        private DataGridViewTextBoxColumn colOffworkTime = new DataGridViewTextBoxColumn();
        private DataGridViewTextBoxColumn colDuration = new DataGridViewTextBoxColumn();
        private Button btnClose = new Button();

        public LogForm(List<WorkRecord> workLogs)
        {
            InitializeComponent();
            logs = workLogs ?? new List<WorkRecord>(); // 处理null参数
            LoadLogData();
        }

        // 加载日志数据到表格
        private void LoadLogData()
        {
            dataGridViewLogs.Rows.Clear();

            // 按日期倒序排列（最新在前）
            logs.Sort((x, y) => DateTime.Compare(DateTime.Parse(y.Date), DateTime.Parse(x.Date)));

            foreach (var record in logs)
            {
                string workDuration = "未记录完整";
                // 计算实际工作时长（扣除1小时午休）
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

                // 添加行到表格
                dataGridViewLogs.Rows.Add(
                    record.Date,
                    record.StartTime,
                    record.OffworkTime,
                    workDuration
                );
            }
        }

        // 设计器初始化代码（完整无缺失）
        private void InitializeComponent()
        {
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewLogs)).BeginInit();
            this.SuspendLayout();
            // 
            // dataGridViewLogs
            // 
            this.dataGridViewLogs.AllowUserToAddRows = false;
            this.dataGridViewLogs.AllowUserToDeleteRows = false;
            this.dataGridViewLogs.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridViewLogs.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colDate,
            this.colStartTime,
            this.colOffworkTime,
            this.colDuration});
            this.dataGridViewLogs.Location = new System.Drawing.Point(12, 12);
            this.dataGridViewLogs.Name = "dataGridViewLogs";
            this.dataGridViewLogs.ReadOnly = true;
            this.dataGridViewLogs.RowTemplate.Height = 25;
            this.dataGridViewLogs.Size = new System.Drawing.Size(640, 350);
            this.dataGridViewLogs.TabIndex = 0;
            // 
            // colDate
            // 
            this.colDate.HeaderText = "日期";
            this.colDate.Name = "colDate";
            this.colDate.ReadOnly = true;
            this.colDate.Width = 120;
            // 
            // colStartTime
            // 
            this.colStartTime.HeaderText = "上班时间";
            this.colStartTime.Name = "colStartTime";
            this.colStartTime.ReadOnly = true;
            this.colStartTime.Width = 150;
            // 
            // colOffworkTime
            // 
            this.colOffworkTime.HeaderText = "下班时间";
            this.colOffworkTime.Name = "colOffworkTime";
            this.colOffworkTime.ReadOnly = true;
            this.colOffworkTime.Width = 150;
            // 
            // colDuration
            // 
            this.colDuration.HeaderText = "实际工作时长(已扣午休)";
            this.colDuration.Name = "colDuration";
            this.colDuration.ReadOnly = true;
            this.colDuration.Width = 180;
            // 
            // btnClose
            // 
            this.btnClose.Location = new System.Drawing.Point(577, 368);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(75, 23);
            this.btnClose.TabIndex = 1;
            this.btnClose.Text = "关闭";
            this.btnClose.UseVisualStyleBackColor = true;
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
            // 
            // LogForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(664, 403);
            this.Controls.Add(this.btnClose);
            this.Controls.Add(this.dataGridViewLogs);
            this.Name = "LogForm";
            this.Text = "打卡日志记录";
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewLogs)).EndInit();
            this.ResumeLayout(false);
        }

        // 关闭按钮事件
        private void btnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}