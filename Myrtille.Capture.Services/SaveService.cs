using Myrtille.Capture.Services;
using Myrtille.Common.Capture;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Timers;

namespace Myrtille.Capture.Services
{
    public partial class SaveService : ServiceBase
    {
        VideoSaver saver = new VideoSaver(Properties.Settings.Default.ImageLogPath, Properties.Settings.Default.VideosavePath);
        private Timer timer1 = null;

        public SaveService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            new System.Threading.Thread(StartService).Start();
        }
        internal void StartService()
        {
            timer1 = new Timer();
            this.timer1.Interval = 5000; //every 30 secs
            this.timer1.Elapsed += new System.Timers.ElapsedEventHandler(this.timer1_Tick);
            timer1.Enabled = true;
            Library.WriteErrorLog("Test window service started");
            /* 
                This is the true composition root for a service,
                so initialize everything in here
            */
            Console.WriteLine("Starting service");
        }

        private void timer1_Tick(object sender, ElapsedEventArgs e)
        {
            //Write code here to do some job depends on your requirement
            Library.WriteErrorLog("Timer ticked and some job has been done successfully");


            // Get all subdirectories

            string[] subdirectoryEntries = Directory.GetDirectories(Properties.Settings.Default.ImageLogPath)
                            .Select(Path.GetFileName)
                            .ToArray();
            // Loop through them to see if they have any other subdirectories

            foreach (string subdirectory in subdirectoryEntries)
                saver.InQueueForSave(subdirectory);
        }

        protected override void OnStop()
        {
            timer1.Enabled = false;
            Library.WriteErrorLog("Test window service stopped");
        }
    }
}
