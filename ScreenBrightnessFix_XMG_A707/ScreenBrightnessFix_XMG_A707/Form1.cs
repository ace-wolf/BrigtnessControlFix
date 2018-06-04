using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ScreenBrightnessFix_XMG_A707
{
    public partial class Form1 : Form
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
        string[] arguments;


        byte[] bLevels; //array of valid level values

        enum KeyModifier
        {
            None = 0,
            Alt = 1,
            Control = 2,
            Shift = 4,
            WinKey = 8
        }

        public Form1()
        {
            InitializeComponent();
            RegisterHotKey(this.Handle, 9932, (int)KeyModifier.Control, Keys.F8.GetHashCode());
            RegisterHotKey(this.Handle, 9933, (int)KeyModifier.Control, Keys.F9.GetHashCode());
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            bLevels = GetBrightnessLevels(); //get the level array for this system
            if (bLevels.Count() == 0) //"WmiMonitorBrightness" is not supported by the system
            {
                Application.Exit();
            }
            else
            {
                trackBar1.TickFrequency = bLevels.Count(); //adjust the trackbar ticks according the number of possible brightness levels
                trackBar1.Maximum = bLevels.Count() - 1;
                trackBar1.Update();
                trackBar1.Refresh();
                check_brightness();
               

            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            UnregisterHotKey(this.Handle, 9932);
            UnregisterHotKey(this.Handle, 9933);
        }


        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            if (m.Msg == 0x0312)
            {
               
                Keys key = (Keys)(((int)m.LParam >> 16) & 0xFFFF);                  // The key of the hotkey that was pressed.
                KeyModifier modifier = (KeyModifier)((int)m.LParam & 0xFFFF);       // The modifier of the hotkey that was pressed.
                int id = m.WParam.ToInt32();                                        // The id of the hotkey that was pressed.

                if (id == 9932)
                {
                    check_brightness();
                    int value = trackBar1.Value;
                    value = value - 5;
                    if (value < trackBar1.Minimum) value = trackBar1.Minimum;
                    trackBar1.Value = value;
                    SetBrightness(bLevels[trackBar1.Value]);
                    label1.Text = trackBar1.Value.ToString() + "%";
                    this.Visible = true;
                    timer1.Stop();
                    timer1.Start();
                }

                if (id == 9933)
                {
                    check_brightness();
                    int value = trackBar1.Value;
                    value = value + 5;
                    if (value > trackBar1.Maximum) value = trackBar1.Maximum;
                    trackBar1.Value = value;
                    SetBrightness(bLevels[trackBar1.Value]);
                    label1.Text = trackBar1.Value.ToString() + "%";
                    this.Visible = true;
                    timer1.Stop();
                    timer1.Start();
                }

            }
        }


        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            SetBrightness(bLevels[trackBar1.Value]);
            label1.Text = trackBar1.Value.ToString() + "%";
            timer1.Stop();
            timer1.Start();
        }



        private void check_brightness()
        {
            int iBrightness = GetBrightness(); //get the actual value of brightness
            int i = Array.IndexOf(bLevels, (byte)iBrightness);
            if (i < 0) i = 1;
            trackBar1.Value = i;
            label1.Text = trackBar1.Value.ToString() + "%";
        }






        //get the actual percentage of brightness
        static int GetBrightness()
        {
            //define scope (namespace)
            System.Management.ManagementScope s = new System.Management.ManagementScope("root\\WMI");

            //define query
            System.Management.SelectQuery q = new System.Management.SelectQuery("WmiMonitorBrightness");

            //output current brightness
            System.Management.ManagementObjectSearcher mos = new System.Management.ManagementObjectSearcher(s, q);

            System.Management.ManagementObjectCollection moc = mos.Get();

            //store result
            byte curBrightness = 0;
            foreach (System.Management.ManagementObject o in moc)
            {
                curBrightness = (byte)o.GetPropertyValue("CurrentBrightness");
                break; //only work on the first object
            }

            moc.Dispose();
            mos.Dispose();

            return (int)curBrightness;
        }

        //array of valid brightness values in percent
        static byte[] GetBrightnessLevels()
        {
            //define scope (namespace)
            System.Management.ManagementScope s = new System.Management.ManagementScope("root\\WMI");

            //define query
            System.Management.SelectQuery q = new System.Management.SelectQuery("WmiMonitorBrightness");

            //output current brightness
            System.Management.ManagementObjectSearcher mos = new System.Management.ManagementObjectSearcher(s, q);
            byte[] BrightnessLevels = new byte[0];

            try
            {
                System.Management.ManagementObjectCollection moc = mos.Get();

                //store result


                foreach (System.Management.ManagementObject o in moc)
                {
                    BrightnessLevels = (byte[])o.GetPropertyValue("Level");
                    break; //only work on the first object
                }

                moc.Dispose();
                mos.Dispose();

            }
            catch (Exception)
            {
                MessageBox.Show("Sorry, Your System does not support this brightness control...");

            }

            return BrightnessLevels;
        }

        static void SetBrightness(byte targetBrightness)
        {
            //define scope (namespace)
            System.Management.ManagementScope s = new System.Management.ManagementScope("root\\WMI");

            //define query
            System.Management.SelectQuery q = new System.Management.SelectQuery("WmiMonitorBrightnessMethods");

            //output current brightness
            System.Management.ManagementObjectSearcher mos = new System.Management.ManagementObjectSearcher(s, q);

            System.Management.ManagementObjectCollection moc = mos.Get();

            foreach (System.Management.ManagementObject o in moc)
            {
                o.InvokeMethod("WmiSetBrightness", new Object[] { UInt32.MaxValue, targetBrightness }); //note the reversed order - won't work otherwise!
                break; //only work on the first object
            }

            moc.Dispose();
            mos.Dispose();
        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            this.Visible = false;
            this.Width = 69;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            timer1.Stop();
            this.Visible = false;
        }

        private void trackBar1_ValueChanged(object sender, EventArgs e)
        {
            panel2.Height = trackBar1.Value;
        }

        private void byLennartWToolStripMenuItem_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("https://9js.de/");
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
    }


}
