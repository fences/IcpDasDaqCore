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

namespace UnidaqSample
{
    public partial class ShowAnalogFrom : Form
    {
        public ShowAnalogFrom()
        {
            InitializeComponent();
            SetupCustomUI();
        }

        private void ShowAnalogFrom_Load(object sender, EventArgs e)
        {
           
        }


        private DataGridView _grid;
        private Dictionary<string, int> _rowMap = new Dictionary<string, int>();


        private void SetupCustomUI()
        {
          
            _grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White
            };

       
            _grid.Columns.Add("colName", "Channel Name");
            _grid.Columns.Add("colIndex", "Channel Index");
            _grid.Columns.Add("colValue", "Value (Raw)");
            _grid.Columns.Add("colAvg", "Regression"); 

            this.Controls.Add(_grid);

        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            InitializeGridRows();
          
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {

            DaqServices.Instance.AnalogData.DataUpdated -= AnalogData_DataUpdated;
            base.OnFormClosing(e);
        }

        private void InitializeGridRows()
        {
            var analogManager = DaqServices.Instance.Analog;

            if (analogManager == null)
            {
                MessageBox.Show("Analog Controller is not initialized yet!");
                return;
            }
            DaqServices.Instance.AnalogData.DataUpdated += AnalogData_DataUpdated;


            _grid.Rows.Clear();
            _rowMap.Clear();
            foreach (var ch in analogManager.Channels)
            {
             
                int rowIndex = _grid.Rows.Add();
                _grid.Rows[rowIndex].Cells[0].Value = ch.Name;
                _grid.Rows[rowIndex].Cells[1].Value = ch.Index;
                _grid.Rows[rowIndex].Cells[2].Value = "Waiting...";
                _rowMap[ch.Name] = rowIndex;
            }
        }

        private void AnalogData_DataUpdated(object sender, IcpDas.Daq.Analog.AnalogMultiChannelDataEventArgs e)
        {

            if (e == null)  return;

            for (int i = 0; i < e.Channels.Count; i++)
            {
                if (_rowMap.TryGetValue(e.Channels[i].Name, out int rowIndex))
                {
                    _grid.Rows[rowIndex].Cells[2].Value = e.GetChannelData(i).RawDataMatrix.Average().ToString("F5");
                    _grid.Rows[rowIndex].Cells[3].Value = e.GetChannelData(i).DataMatrix.Average().ToString("F5");
                }
            }
        }





    }
}
