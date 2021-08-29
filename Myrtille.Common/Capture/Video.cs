using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;

namespace Myrtille.Common.Capture
{

    public class Frame
    {
        public string id;
    }
    public  class VideoSaver
    {
        static Image bmp1;
        static List<string> filesName;
        static ConcurrentQueue<Frame> Queue = new ConcurrentQueue<Frame>();
        static string ImageLogPath;
        static string VideosavePath;
        public VideoSaver(string imageLogPath, string videosavePath)
        {
            ImageLogPath = imageLogPath;
            VideosavePath= videosavePath;

            Thread doSaving = new Thread(DoSave);
            doSaving.Start();
        }

         void DoSave()
        {
            while (true)
            {

                if (Queue.TryDequeue(out Frame item))
                {
                    string inputName = item.id;

                    // Library.WriteErrorLog("start Converting: " + inputName);

                    var ext = new List<string> { "jpg", "gif", "png" };
                    filesName = Directory
                        .EnumerateFiles(ImageLogPath + "\\" + inputName, "*.*", SearchOption.TopDirectoryOnly)
                        //.Where(s => ext.Contains(Path.GetExtension(s).TrimStart('.').ToLowerInvariant())
                        .OrderBy(f => f).ToList();

                    //var firstFrame = "D:\\Recivedlog\\e74f905b-77c1-4463-97b1-0335d96740c2\\2021-08-14_14-10-59-986";
                    var firstFrame = filesName.First();
                    var img =PictureHelper.BinaryDeserializeObject(firstFrame);
                    bmp1 =PictureHelper.GetImageFromByteArray(img.Data);

                    // bmp.Save("sdcfg.png",System.Drawing.Imaging.ImageFormat.Bmp);
                    try
                    {
                    GenerateVideo(item.id);
                    }
                    finally
                    {
                        string fullpath = ImageLogPath +"\\"+ item.id;
                        // If directory does not exist, don't even try   
                        if (Directory.Exists(fullpath))
                        {
                            var dir = new DirectoryInfo(fullpath);
                            dir.Attributes = dir.Attributes & ~FileAttributes.ReadOnly;
                            dir.Delete(true);
                        }
                    }
                    // Library.WriteErrorLog("finish Converting: " + inputName);
                }
            }
        }
        public  void GenerateVideo(string OutputName)
        {
            //Process proc = new Process();
            using (var proc = new Process())
            {
                proc.StartInfo.FileName = "ffmpeg.exe";
                String arg = "-f image2pipe -framerate " + "3" + " -i pipe:.bmp -pix_fmt yuv420p -qscale:v 5 -vcodec libx264 -bufsize 30000k -y " + VideosavePath + "\\" + OutputName + ".mp4";
                proc.StartInfo.Arguments = arg;// "-f image2pipe -i pipe:.jpg -vcodec libx264  -maxrate " + bitrate + "k -bt 10 -r " + fps + " -an -y test.mp4"; //+ rtmp;
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardInput = true;
                proc.StartInfo.RedirectStandardOutput = true;

                proc.Start();

                BinaryWriter writer = new BinaryWriter(proc.StandardInput.BaseStream);


                foreach (string name in filesName)
                {
                    //img = Image.FromFile(@"image1.jpg");
                    //img.Save(writer.BaseStream, System.Drawing.Imaging.ImageFormat.Jpeg);
                    // img.Dispose();

                    //var imgg = BinaryDeserializeObject("D:\\Recivedlog\\e74f905b-77c1-4463-97b1-0335d96740c2\\2021-08-14_14-10-59-986");
                    var imgg = PictureHelper.BinaryDeserializeObject(name);
                    Image bmp2 = PictureHelper.GetImageFromByteArray(imgg.Data);

                    bmp1 = PictureHelper.MergeTwoImages(bmp1, bmp2, imgg.PosX, imgg.PosY);
                    Image frame = (Image)bmp1.Clone();
                    frame.Save(writer.BaseStream, System.Drawing.Imaging.ImageFormat.Jpeg);
                    //bmp.Save("sdcfg.png", System.Drawing.Imaging.ImageFormat.Bmp);

                    frame.Dispose();

                };
                writer.Close();
            }
            //Console.ReadKey();
        }


        public void InQueueForSave(string path)
        {
            Queue.Enqueue(new Frame() { id = path });
        }

    }
}
