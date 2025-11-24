namespace UnidaqSample
{
    partial class AnalogChartForm
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
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea1 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.Legend legend1 = new System.Windows.Forms.DataVisualization.Charting.Legend();
            System.Windows.Forms.DataVisualization.Charting.Series series1 = new System.Windows.Forms.DataVisualization.Charting.Series();
            this.daqChart = new System.Windows.Forms.DataVisualization.Charting.Chart();
            ((System.ComponentModel.ISupportInitialize)(this.daqChart)).BeginInit();
            this.SuspendLayout();
            // 
            // daqChart
            // 
            chartArea1.Name = "ChartArea1";
            this.daqChart.ChartAreas.Add(chartArea1);
            this.daqChart.Dock = System.Windows.Forms.DockStyle.Fill;
            legend1.Name = "Legend1";
            this.daqChart.Legends.Add(legend1);
            this.daqChart.Location = new System.Drawing.Point(0, 0);
            this.daqChart.Name = "daqChart";
            series1.ChartArea = "ChartArea1";
            series1.Legend = "Legend1";
            series1.Name = "Series1";
            this.daqChart.Series.Add(series1);
            this.daqChart.Size = new System.Drawing.Size(800, 450);
            this.daqChart.TabIndex = 0;
            this.daqChart.Text = "chart1";
            // 
            // AnalogChartForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.daqChart);
            this.Name = "AnalogChartForm";
            this.Text = "AnalogChartForm-Show raw Data";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.AnalogChartForm_FormClosing);
            this.Load += new System.EventHandler(this.AnalogChartForm_Load);
            ((System.ComponentModel.ISupportInitialize)(this.daqChart)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.DataVisualization.Charting.Chart daqChart;
    }
}