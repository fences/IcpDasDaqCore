using IcpDas.Daq.Analog;
using IcpDas.Daq.Service;
using IcpDas.Daq.System;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

namespace UnidaqSample
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

      
        private async void MainForm_Load(object sender, EventArgs e)
        {
            rtbStatus.Multiline = true;

            DaqServices.Instance.LogInfo += (s, msg) => rtbStatus.AppendText(msg + "\r\n");
            await DaqServices.Instance.InitializeSystemAsync();
            if (DaqServices.Instance.Analog != null)
            {

                DaqServices.Instance.Analog.CardType = 0;
                DaqServices.Instance.Analog.UseMultiChannelOutput = true;
                DaqServices.Instance.Analog.UseParallel = false;
                DaqServices.Instance.Analog.RetryLimit = 5;

                double[] reg1 = new[] { 10.0, 0.5 };
                double[] reg2 = new[] { 1.0, 0.5, 0.3, 0.2, 0.1 };
                int movingAverageWindow = 5000;
                DaqServices.Instance.Analog.AddChannel("Sensor1 With MovingAvg Filter", 0, VoltageRange.Bipolar_10V, movingAverageWindow, reg1);
                DaqServices.Instance.Analog.AddChannel("Sensor2 With MovingAvg Filter", 1, VoltageRange.Bipolar_10V, movingAverageWindow, reg2);
                DaqServices.Instance.Analog.AddChannel("Sensor3", 3, VoltageRange.Bipolar_10V);
                DaqServices.Instance.Analog.AddChannel("Sensor4", 5, VoltageRange.Bipolar_10V);
                DaqServices.Instance.Analog.AddChannel("Sensor5", 6, VoltageRange.Bipolar_10V);
                DaqServices.Instance.Analog.AddChannel("Sensor6", 7, VoltageRange.Bipolar_10V);

            }

           

        }


        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {

            DaqServices.Instance.Dispose();
        }




        private void btnService_Click(object sender, EventArgs e)
        {
            if (DaqServices.Instance.Analog.State == AnalogState.Stopped)
            {
                DaqServices.Instance.Analog.Start(50000, 200);
                rtbStatus.AppendText("Analog Service Started." + "\r\n");
                btnService.Text = "Stop";
              
            }
            else
            {
                DaqServices.Instance.Analog.Stop();
                rtbStatus.AppendText("Analog Service Stopped." + "\r\n");
                btnService.Text = "Start";
             
            }
        }

        private void btnShowTable_Click(object sender, EventArgs e)
        {
            var monitor = new ShowAnalogFrom();
            monitor.Owner = this;
            monitor.Show();
        }

        private void btnShowChart_Click(object sender, EventArgs e)
        {
            var monitor = new AnalogChartForm();
            monitor.Owner = this;
            monitor.Show();
        }
    }
}
