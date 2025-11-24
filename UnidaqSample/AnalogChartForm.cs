using DevExpress.XtraCharts;
using IcpDas.Daq.Analog;
using IcpDas.Daq.Service;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace UnidaqSample
{
    public partial class AnalogChartForm : Form
    {
        public AnalogChartForm()
        {
            InitializeComponent();
            SetupChartStyle();

        }

        private const int MAX_HISTORY_POINTS = 50;

        private void AnalogChartForm_Load(object sender, EventArgs e)
        {
            if (DaqServices.Instance.Analog == null || DaqServices.Instance.Analog.Channels.Count == 0)
            {
                MessageBox.Show("No Analog!");
                this.Close();
                return;
            }

            InitializeChartSeries();


            DaqServices.Instance.AnalogData.DataUpdated += OnAnalogDataUpdated;
        }
        private void SetupChartStyle()
        {
            daqChart.Titles.Clear();
            daqChart.Titles.Add("Real-Time Analog Input");

            var area = daqChart.ChartAreas[0];

         
            area.AxisX.LabelStyle.Format = "0";
            area.AxisX.MajorGrid.LineColor = System.Drawing.Color.LightGray;
            area.AxisX.Title = "Time (Blocks)";

        
            area.AxisY.MajorGrid.LineColor = System.Drawing.Color.LightGray;
            area.AxisY.Title = "Voltage (V)";
            area.AxisY.LabelStyle.Format = "0.00000";

            area.AxisY.IsStartedFromZero = false;


          
            daqChart.AntiAliasing = System.Windows.Forms.DataVisualization.Charting.AntiAliasingStyles.Graphics;
        }

        private void InitializeChartSeries()
        {
            daqChart.Series.Clear();
            var channels = DaqServices.Instance.Analog.Channels;

            foreach (var ch in channels)
            {
                var series = new System.Windows.Forms.DataVisualization.Charting.Series(ch.Name)
                {
                    ChartType = SeriesChartType.FastLine, 
                    BorderWidth = 2,
                    XValueType = ChartValueType.Int32
                };
                daqChart.Series.Add(series);
            }
        }

     
        private void OnAnalogDataUpdated(object sender, AnalogMultiChannelDataEventArgs e)
        {

            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => UpdateChartData(e)));
            }
            else
            {
                UpdateChartData(e);
            }
        }

        private void UpdateChartData(AnalogMultiChannelDataEventArgs data)
        {
            if (daqChart.IsDisposed) return;
            daqChart.SuspendLayout();


            for (int i = 0; i < data.Channels.Count; i++)
            {
                string name = data.Channels[i].Name;

     
                float sum = 0;
                int count = data.DataMatrix.GetLength(0);
                for (int k = 0; k < count; k++) sum += data.RawDataMatrix[k, i];
                float avg = sum / count;

               
                daqChart.Series[name].Points.AddY(avg);

              
                if (daqChart.Series[name].Points.Count > MAX_HISTORY_POINTS)
                    daqChart.Series[name].Points.RemoveAt(0);
            }

            daqChart.ResumeLayout();
        }

        private void AnalogChartForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            DaqServices.Instance.AnalogData.DataUpdated -= OnAnalogDataUpdated;
        }
    }
}
