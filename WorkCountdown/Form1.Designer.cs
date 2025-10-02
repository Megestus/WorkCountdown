namespace WorkCountdown
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            lblStartTime = new Label();
            lblLunch = new Label();
            lblOffworkTime = new Label();
            lblCountdown = new Label();
            btnNewDay = new Button();
            btnClockOff = new Button();
            btnViewLogs = new Button();
            SuspendLayout();
            // 
            // lblStartTime
            // 
            lblStartTime.Font = new Font("Microsoft YaHei UI", 9F);
            lblStartTime.Location = new Point(12, 15);
            lblStartTime.Name = "lblStartTime";
            lblStartTime.Size = new Size(250, 23);
            lblStartTime.TabIndex = 0;
            lblStartTime.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblLunch
            // 
            lblLunch.Font = new Font("Microsoft YaHei UI", 9F);
            lblLunch.Location = new Point(12, 45);
            lblLunch.Name = "lblLunch";
            lblLunch.Size = new Size(250, 23);
            lblLunch.TabIndex = 1;
            lblLunch.Text = "午休时间 1小时";
            lblLunch.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblOffworkTime
            // 
            lblOffworkTime.Font = new Font("Microsoft YaHei UI", 9F);
            lblOffworkTime.Location = new Point(12, 75);
            lblOffworkTime.Name = "lblOffworkTime";
            lblOffworkTime.Size = new Size(250, 23);
            lblOffworkTime.TabIndex = 2;
            lblOffworkTime.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblCountdown
            // 
            lblCountdown.Font = new Font("Microsoft YaHei UI", 10.5F, FontStyle.Bold);
            lblCountdown.ForeColor = Color.DarkRed;
            lblCountdown.Location = new Point(12, 105);
            lblCountdown.Name = "lblCountdown";
            lblCountdown.Size = new Size(250, 28);
            lblCountdown.TabIndex = 3;
            lblCountdown.TextAlign = ContentAlignment.MiddleCenter;

            // 
            // btnNewDay
            // 
            btnNewDay.Font = new Font("Microsoft YaHei UI", 9F);
            btnNewDay.Location = new Point(12, 145);
            btnNewDay.Name = "btnNewDay";
            btnNewDay.Size = new Size(250, 30);
            btnNewDay.TabIndex = 4;
            btnNewDay.Text = "新的牛马一天";
            btnNewDay.UseVisualStyleBackColor = true;
            // 
            // btnClockOff
            // 
            btnClockOff.Font = new Font("Microsoft YaHei UI", 9F);
            btnClockOff.Location = new Point(12, 185);
            btnClockOff.Name = "btnClockOff";
            btnClockOff.Size = new Size(250, 30);
            btnClockOff.TabIndex = 5;
            btnClockOff.Text = "下班打卡";
            btnClockOff.UseVisualStyleBackColor = true;
            btnClockOff.Click += btnClockOff_Click;
            // 
            // btnViewLogs
            // 
            btnViewLogs.Font = new Font("Microsoft YaHei UI", 9F);
            btnViewLogs.Location = new Point(12, 225);
            btnViewLogs.Name = "btnViewLogs";
            btnViewLogs.Size = new Size(250, 30);
            btnViewLogs.TabIndex = 6;
            btnViewLogs.Text = "查看打卡日志";
            btnViewLogs.UseVisualStyleBackColor = true;
            btnViewLogs.Click += btnViewLogs_Click;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(274, 280);
            Controls.Add(btnViewLogs);
            Controls.Add(btnClockOff);
            Controls.Add(btnNewDay);
            Controls.Add(lblCountdown);
            Controls.Add(lblOffworkTime);
            Controls.Add(lblLunch);
            Controls.Add(lblStartTime);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "Form1";
            Text = "工作倒计时";
            Load += Form1_Load_1;
            ResumeLayout(false);
        }

        // 所有控件声明（确保不缺）
        private System.Windows.Forms.Label lblStartTime;
        private System.Windows.Forms.Label lblLunch;
        private System.Windows.Forms.Label lblOffworkTime;
        private System.Windows.Forms.Label lblCountdown;
        private System.Windows.Forms.Button btnNewDay;
        private System.Windows.Forms.Button btnClockOff;
        private System.Windows.Forms.Button btnViewLogs; // 必须声明
    }
}