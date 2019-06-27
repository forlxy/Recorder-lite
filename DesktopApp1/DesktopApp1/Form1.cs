using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using NAudio.Lame;
using NAudio.Wave;

// This is the code for your desktop app.
// Press Ctrl+F5 (or go to Debug > Start Without Debugging) to run your app.


namespace DesktopApp1
{
    public partial class Form1 : Form
    {
        /// <summary>
        /// 
        /// </summary>
        // To support flashing.
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool FlashWindowEx(ref FLASHWINFO pwfi);

        //Flash both the window caption and taskbar button.
        //This is equivalent to setting the FLASHW_CAPTION | FLASHW_TRAY flags. 
        public const UInt32 FLASHW_ALL = 3;

        // Flash continuously until the window comes to the foreground. 
        public const UInt32 FLASHW_TIMERNOFG = 12;
        public string lastPath = "";
        [StructLayout(LayoutKind.Sequential)]
        public struct FLASHWINFO
        {
            public UInt32 cbSize;
            public IntPtr hwnd;
            public UInt32 dwFlags;
            public UInt32 uCount;
            public UInt32 dwTimeout;
        }

        // Do the flashing - this does not involve a raincoat.
        public static bool FlashWindowEx(Form form)
        {
            IntPtr hWnd = form.Handle;
            FLASHWINFO fInfo = new FLASHWINFO();

            fInfo.cbSize = Convert.ToUInt32(Marshal.SizeOf(fInfo));
            fInfo.hwnd = hWnd;
            fInfo.dwFlags = FLASHW_ALL | FLASHW_TIMERNOFG;
            fInfo.uCount = UInt32.MaxValue;
            fInfo.dwTimeout = 0;

            return FlashWindowEx(ref fInfo);
        }
        /////////////////////////////////////////////////////////////////
        // Create class-level accessible variables to store the audio recorder and capturer instance
        private LameMP3FileWriter RecordedAudioWriter = null;
        private WasapiLoopbackCapture CaptureInstance = null;
        //create a new Timer
        private Timer mytimer = new Timer(); 
        private int duration = 0;
        // Start or stop the stopwatch.
        private DateTime startTime;
        private DateTime endTime;
        private Timer timer = new Timer();
        private bool pathChanged = false;

        int minute = 0;
        int second = 0;
        int hour = 0;
        int bitRate = 128;
        // Define the output wav file of the recorded audio
        private string outputFilePath = @"D:\Â¼Òô\recording_" + DateTime.Now.ToString("MMddHHmmss") + ".mp3";
        public Form1()
        {
            // Define the border style of the form to a dialog box.
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            InitializeComponent();
            label2.Text = outputFilePath;
            // Enable "Start button" and disable "Stop Button"
            this.button1.Enabled = true;
            this.button2.Enabled = false;
            lastPath = outputFilePath;
            FileInfo fi = new FileInfo(outputFilePath);
            var di = fi.Directory;
            if (!di.Exists)
                di.Create();
        }

        //Stop timer will trigger this function
        private void mytimer_Tick(object sender, EventArgs e)
        {
            // Stop recording !
            this.CaptureInstance.StopRecording();

            // Enable "Start button" and disable "Stop Button"
            this.button1.Enabled = true;
            this.button2.Enabled = false;
            this.checkBox1.Enabled = true;
            this.textBox1.Enabled = true;
            this.textBox2.Enabled = true;
            this.textBox3.Enabled = true;
            this.label1.Enabled = true;
            this.button3.Enabled = true;
            //disable the timer here so it won't fire again...
            mytimer.Enabled = false;
            timer.Enabled = false;

            duration = 0;
            this.textBox1.Text = "";
            this.textBox2.Text = "";
            this.textBox3.Text = "";
            lastPath = outputFilePath;
            if (!pathChanged & !checkBox2.Checked)
                outputFilePath = @"D:\Â¼Òô\recording_" + DateTime.Now.ToString("MMddHHmmss") + ".mp3";
            else if (!checkBox2.Checked)
                outputFilePath = outputFilePath.Substring(0, outputFilePath.Length - 4) + "_1" + ".mp3";
            label2.Text = outputFilePath;
            FlashWindowEx(this);
        }

        // Display the new elapsed time.
        private void tmrClock_Tick(object sender, EventArgs e)
        {
            TimeSpan elapsed = DateTime.Now - startTime;

            int precision = 0; // Specify how many digits past the decimal point
            const int TIMESPAN_SIZE = 7; // it always has seven digits
                                         // convert the digitsToShow into a rounding/truncating mask
            int factor = (int)Math.Pow(10, (TIMESPAN_SIZE - precision));

            elapsed = new TimeSpan(((long)Math.Round((1.0 * elapsed.Ticks / factor)) * factor));
            // Start with the days if greater than 0.
            string text = "";
            if (elapsed.Days > 0)
                text += elapsed.Days.ToString() + ".";

            // Compose the rest of the elapsed time.
            text +=
                elapsed.Hours.ToString("00") + ":" +
                elapsed.Minutes.ToString("00") + ":" +
                elapsed.Seconds.ToString("00");


            if (this.checkBox1.Checked && duration!=0)
            {
                TimeSpan leftSpan = endTime - DateTime.Now;

                leftSpan = new TimeSpan(((long)Math.Round((1.0 * leftSpan.Ticks / factor)) * factor));
               
                this.textBox1.Text = (leftSpan.Seconds).ToString();
                this.textBox2.Text = (leftSpan.Minutes).ToString();
                this.textBox3.Text = (leftSpan.Hours).ToString();
            }

            this.timeLabel.Text = text;
        }
        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            // Click on the link below to continue learning how to build a desktop app using WinForms!
            System.Diagnostics.Process.Start("http://aka.ms/dotnet-get-started-desktop");

        }
        //Start
        private void button1_Click(object sender, EventArgs e)
        {

            this.timeLabel.Text = "00:00:00";

            startTime = DateTime.Now;
            endTime = DateTime.Now;
            timer.Interval = 1000; //set the interval to x second.
            timer.Tick += new EventHandler(tmrClock_Tick);
            timer.Start();
            //No timer
            if (duration == 0)
            {
                this.checkBox1.Checked = false;
            }
            else //Has a timer
            {
                endTime = endTime.AddSeconds(duration);

                mytimer.Interval = duration * 1000 + 100; //set the interval to x second.
                mytimer.Tick += new EventHandler(mytimer_Tick);
                mytimer.Start();
            }


            // Redefine the capturer instance with a new instance of the LoopbackCapture class
            this.CaptureInstance = new WasapiLoopbackCapture();

            // Redefine the audio writer instance with the given configuration
            this.RecordedAudioWriter = new LameMP3FileWriter(outputFilePath, CaptureInstance.WaveFormat, bitRate);

            // When the capturer receives audio, start writing the buffer into the mentioned file
            this.CaptureInstance.DataAvailable += (s, a) =>
            {
                this.RecordedAudioWriter.Write(a.Buffer, 0, a.BytesRecorded);
                if (RecordedAudioWriter.Position > CaptureInstance.WaveFormat.AverageBytesPerSecond * 36000)
                {
                    CaptureInstance.StopRecording();
                }
            };

            // When the Capturer Stops
            this.CaptureInstance.RecordingStopped += (s, a) =>
            {
                this.RecordedAudioWriter.Dispose();
                this.RecordedAudioWriter = null;
                CaptureInstance.Dispose();
            };

            // Enable "Stop button" and disable "Start Button"
            this.button1.Enabled = false;
            this.button2.Enabled = true;
            this.checkBox1.Enabled = false;
            this.textBox1.Enabled = false;
            this.textBox2.Enabled = false;
            this.textBox3.Enabled = false;
            this.label1.Enabled = false;
            this.button3.Enabled = false;
            // Start recording !
            this.CaptureInstance.StartRecording();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            // Stop recording !
            this.CaptureInstance.StopRecording();

            // Enable "Start button" and disable "Stop Button"
            this.button1.Enabled = true;
            this.button2.Enabled = false;
            this.checkBox1.Enabled = true;
            this.textBox1.Enabled = true;
            this.textBox2.Enabled = true;
            this.textBox3.Enabled = true;
            this.label1.Enabled = true;
            this.button3.Enabled = true;
            mytimer.Stop();
            mytimer.Enabled = false;
            timer.Enabled = false;
            duration = 0;
            FlashWindowEx(this);
            this.textBox1.Text = "";
            this.textBox2.Text = "";
            this.textBox3.Text = "";
            lastPath = outputFilePath;
            if (!pathChanged & !checkBox2.Checked)
                outputFilePath = @"D:\Â¼Òô\recording_" + DateTime.Now.ToString("MMddHHmmss") + ".mp3";
            else if(!checkBox2.Checked)
                outputFilePath = outputFilePath.Substring(0, outputFilePath.Length - 4) + "_1" + ".mp3";
            label2.Text = outputFilePath;
        }


        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (this.checkBox1.Checked)
            {
                if (!Int32.TryParse(this.textBox1.Text, out second))
                {
                    second = 0;
                }
                if (!Int32.TryParse(this.textBox2.Text, out minute))
                {
                    minute = 0;
                }
                if (!Int32.TryParse(this.textBox3.Text, out hour))
                {
                    hour = 0;
                }
                duration = second + minute * 60 + hour * 60 * 60;
            }
            else
            {
                duration = 0;
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            pathChanged = true;
            // Displays a SaveFileDialog so the user can save the Image  
            // assigned to Button2.  
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();
            saveFileDialog1.Filter = "MP3|*.mp3";
            saveFileDialog1.Title = "±£´æÂ¼ÒôÎÄ¼þ";
            saveFileDialog1.ShowDialog();
            // If the file name is not an empty string open it for saving.  
            if (saveFileDialog1.FileName != "")
            {
                outputFilePath = Path.GetFullPath(saveFileDialog1.FileName);
                label2.Text = outputFilePath;
            }
        }

        private void textBox1_TextChanged_1(object sender, EventArgs e)
        {
            this.checkBox1.Checked = true;
            if (this.checkBox1.Checked)
            {
                if (!Int32.TryParse(this.textBox1.Text, out second))
                {
                    second = 0;
                }
            }
            duration = second + minute * 60 + hour * 60 * 60;
        }
        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            this.checkBox1.Checked = true;
            if (this.checkBox1.Checked)
            {
                if (!Int32.TryParse(this.textBox2.Text, out minute))
                {
                    minute = 0;
                }
            }
            duration = second + minute * 60 + hour * 60 * 60;
        }

        private void textBox3_TextChanged(object sender, EventArgs e)
        {
            this.checkBox1.Checked = true;
            if (this.checkBox1.Checked)
            {
                if (!Int32.TryParse(this.textBox3.Text, out hour))
                {
                    hour = 0;
                }
            }
            duration = second + minute * 60 + hour * 60 * 60;
        }

        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox3.Checked)
            {
                bitRate = 256;
            }
            else
            {
                bitRate = 128;
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            WaveOutEvent waveOut = new WaveOutEvent();
            Mp3FileReader mp3Reader = new Mp3FileReader(lastPath);
            waveOut.Init(mp3Reader);
            waveOut.Play();

        }

        private void progressBar1_Click(object sender, EventArgs e)
        {

        }
    }
}
