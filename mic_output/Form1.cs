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
        const int sampleCount = 50;
        int valueMax = 200;
        List<FixedSizedList<int>> splValues = new List<FixedSizedList<int>>();
        List<float> splValuesAvg = new List<float>();
        object ptdLock = new object();
        int peakTimeDiff = 1000000;

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
                if (s.StartsWith("R"))
                    RawData(s.Substring(2));
                else if (s.StartsWith("P"))
                    PeakData(s.Substring(2));
            }
            catch (TimeoutException)
            {

            }
            catch (FormatException)
            {
                serialPort1.Close();
                serialPort1.Open();
            }
        }

        private void PeakData(string s)
        {
            var vals = s.Split(new char[] { ',' });
            if (vals.Length == 5)
            {
                var microsL = Convert.ToInt32(vals[1]);
                var microsR = Convert.ToInt32(vals[3]);
                lock (ptdLock)
                    peakTimeDiff = microsL - microsR;
            }
        }

        private void RawData(string s)
        {
            //valueMax = UInt16.MaxValue;
            valueMax = 1024;
            // skip time
            s = s.Substring(s.IndexOf(',') + 1);
            var vals = s.Split(new char[] { ',' });
            lock (splValues)
            {
                for (int i = 0; i < vals.Length; i++)
                {
                    if (splValues.Count < i + 1)
                    {
                        splValues.Add(new FixedSizedList<int>(limit));
                        splValuesAvg.Add(0);
                    }
                    var v = Convert.ToInt32(vals[i]);
                    splValues[i].Enqueue(v);
                    splValuesAvg[i] = splValuesAvg[i] + (v - splValuesAvg[i]) / sampleCount;
                }
            }
        }

        Pen level = new Pen(Brushes.Green, 3);
        private void doubleBufferedPanel1_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;

            g.FillRectangle(Brushes.White, 0, 0, doubleBufferedPanel1.Width, doubleBufferedPanel1.Height);

            DrawSPL(g);

            DrawDirection(g);
        }

        private Point PolarToRectangular(Point offset, double radius, double theta)
        {
            double sin = Math.Sin(theta);

            // This is faster then:
            // double cos = Math.Cos(theta);
            double cos = -Math.Sqrt(1 - (sin * sin));

            Int32 x = offset.X + (Int32)Math.Round(radius * cos);
            Int32 y = offset.Y + (Int32)Math.Round(radius * sin);

            return new Point(x, y);
        }

        private void DrawDirection(Graphics g)
        {
            int diff;
            lock (ptdLock)
                diff = peakTimeDiff;
            if (diff != 1000000)
            {
                int w = doubleBufferedPanel1.Width;
                int h = doubleBufferedPanel1.Height;

                float s_min = -5000;
                float s_max = 5000;
                float a_min = 5;
                float a_max = 175;

                var angle = (diff - s_min) / (s_max - s_min) * (a_max - a_min) + a_min;
                var p1 = new Point(w / 2, 10);
                var p2 = PolarToRectangular(p1, 200, angle * Math.PI / 180.0);
                g.DrawLine(Pens.Red, p1, p2);
                g.DrawString(diff.ToString(), doubleBufferedPanel1.Font, Brushes.Black, new PointF(5, 5));
            }
        }

        private void DrawSPL(Graphics g)
        {
            lock (splValues)
            {
                const int space = 5;

                if (splValues.Count == 0)
                     return;

                int w = doubleBufferedPanel1.Width / splValues.Count;
                int h = doubleBufferedPanel1.Height;

                for (int i = 0; i < splValues.Count; i++)
                {
                    g.DrawRectangle(Pens.Red, w * i, 0, w - 1, h - 1);

                    // draw mic values
                    int[] values = splValues[i].Copy();
                    float L = -h + space * 2;
                    float r = w * (i + 1);
                    float x = w * i;
                    var originY = h - space;

                    var xnext = r;
                    foreach (var v in values)
                    {
                        var originX = xnext;
                        var destX = originX + 0.5f;
                        var destY = originY + L * v / valueMax;
                        if (destX < r - w)
                            break;
                        g.DrawLine(Pens.Black, originX, originY, destX, destY);
                        xnext -= 1;
                    }

                    // draw mic avg
                    var avg = splValuesAvg[i];
                    var y = h - h * avg / valueMax - space;
                    g.DrawLine(level, x, y, x + w, y);

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
