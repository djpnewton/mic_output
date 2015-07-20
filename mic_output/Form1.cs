using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace mic_output
{
    public partial class MainForm : Form
    {
        const int limit = 1000;
        List<FixedSizedList<int>> splValues = new List<FixedSizedList<int>>();

        public MainForm()
        {
            InitializeComponent();
        }

        private void btnGo_Click(object sender, EventArgs e)
        {
            if (serialPort1.IsOpen)
                serialPort1.Close();
            serialPort1.PortName = tbPort.Text;
            serialPort1.Open();
        }

        List<byte> _readBuffer = new List<byte>();
        private void serialPort1_DataReceived(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
        {
            try
            {
                var s = serialPort1.ReadLine();
                var vals = s.Split(new char[] { ',' });
                lock (splValues)
                {
                    for (int i = 0; i < vals.Length; i++)
                    {
                        if (splValues.Count < i + 1)
                            splValues.Add(new FixedSizedList<int>(limit));
                        splValues[i].Enqueue(Convert.ToInt32(vals[i]));
                    }
                }
            }
            catch (TimeoutException)
            {

            }
        }

        private void doubleBufferedPanel1_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;

            g.FillRectangle(Brushes.White, 0, 0, doubleBufferedPanel1.Width, doubleBufferedPanel1.Height);

            lock (splValues)
            {
                if (splValues.Count == 0)
                    return;

                int w = doubleBufferedPanel1.Width / splValues.Count;
                int h = doubleBufferedPanel1.Height;

                for (int i = 0; i < splValues.Count; i++)
                {
                    g.DrawRectangle(Pens.Red, w * i, 0, w - 1, h - 1);

                    // draw mic values
                    int[] values = splValues[i].Copy();
                    float L = -h + 10;
                    float x = w * (i + 1);
                    var originY = h - 5;

                    foreach (var v in values)
                    {
                        var originX = x;
                        var destX = originX + 0.5f;
                        var destY = originY + L * v / 512;
                        g.DrawLine(Pens.Black, originX, originY, destX, destY);
                        x -= 1;
                    }
                }
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (!serialPort1.IsOpen)
                return;

            doubleBufferedPanel1.Refresh();
        }
    }
}
