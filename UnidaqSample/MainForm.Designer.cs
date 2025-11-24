namespace UnidaqSample
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.rtbStatus = new System.Windows.Forms.RichTextBox();
            this.panel1 = new System.Windows.Forms.Panel();
            this.btnShowTable = new System.Windows.Forms.Button();
            this.btnShowChart = new System.Windows.Forms.Button();
            this.btnService = new System.Windows.Forms.Button();
            this.tableLayoutPanel1.SuspendLayout();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 2;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20.38835F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 79.61165F));
            this.tableLayoutPanel1.Controls.Add(this.panel1, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.rtbStatus, 0, 1);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 2;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(618, 318);
            this.tableLayoutPanel1.TabIndex = 1;
            // 
            // rtbStatus
            // 
            this.tableLayoutPanel1.SetColumnSpan(this.rtbStatus, 2);
            this.rtbStatus.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rtbStatus.Location = new System.Drawing.Point(3, 162);
            this.rtbStatus.Name = "rtbStatus";
            this.rtbStatus.Size = new System.Drawing.Size(612, 153);
            this.rtbStatus.TabIndex = 0;
            this.rtbStatus.Text = "";
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.btnService);
            this.panel1.Controls.Add(this.btnShowChart);
            this.panel1.Controls.Add(this.btnShowTable);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel1.Location = new System.Drawing.Point(3, 3);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(120, 153);
            this.panel1.TabIndex = 2;
            // 
            // btnShowTable
            // 
            this.btnShowTable.Location = new System.Drawing.Point(9, 83);
            this.btnShowTable.Name = "btnShowTable";
            this.btnShowTable.Size = new System.Drawing.Size(97, 23);
            this.btnShowTable.TabIndex = 0;
            this.btnShowTable.Text = "Show Table";
            this.btnShowTable.UseVisualStyleBackColor = true;
            this.btnShowTable.Click += new System.EventHandler(this.btnShowTable_Click);
            // 
            // btnShowChart
            // 
            this.btnShowChart.Location = new System.Drawing.Point(9, 112);
            this.btnShowChart.Name = "btnShowChart";
            this.btnShowChart.Size = new System.Drawing.Size(97, 23);
            this.btnShowChart.TabIndex = 2;
            this.btnShowChart.Text = "Show Graph";
            this.btnShowChart.UseVisualStyleBackColor = true;
            this.btnShowChart.Click += new System.EventHandler(this.btnShowChart_Click);
            // 
            // btnService
            // 
            this.btnService.Location = new System.Drawing.Point(9, 9);
            this.btnService.Name = "btnService";
            this.btnService.Size = new System.Drawing.Size(97, 23);
            this.btnService.TabIndex = 3;
            this.btnService.Text = "Start Service";
            this.btnService.UseVisualStyleBackColor = true;
            this.btnService.Click += new System.EventHandler(this.btnService_Click);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(618, 318);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "ICP DAS Daq Sample";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.panel1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.RichTextBox rtbStatus;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Button btnShowChart;
        private System.Windows.Forms.Button btnShowTable;
        private System.Windows.Forms.Button btnService;
    }
}

