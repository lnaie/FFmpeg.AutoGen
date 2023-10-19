using FFmpeg.AutoGen.Abstractions;
using System.Drawing;
using System.Drawing.Imaging;
using System;
using System.Linq;
using System.Reflection;
using System.IO;
using FFmpeg.AutoGen.Bindings.DynamicallyLinked;

namespace TestConsoleApp1
{
    internal class Program
    {
        static void Main(string[] args)
        {
            int stopTime = 1;
            if (args.Length > 0)
                stopTime = int.Parse(args[0]);

            Console.WriteLine("Current directory: " + Environment.CurrentDirectory);
            Console.WriteLine("Running in {0}-bit mode.", Environment.Is64BitProcess ? "64" : "32");

            // Used with FFmpeg.AutoGen.Bindings.DynamicallyLoaded 
            //DynamicallyLoadedBindings.LibrariesPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
            //DynamicallyLoadedBindings.Initialize();

            // Used with FFmpeg.AutoGen.Bindings.DynamicallyLinked
            DynamicallyLinkedBindings.Initialize();

            Console.WriteLine($"FFmpeg version info: {ffmpeg.av_version_info()}");

            Console.WriteLine("Decoding...");
            DecodeFrameToImage(stopTime);
        }

        private static unsafe void DecodeFrameToImage(int stopTime)
        {
            // decode all frames from url, please not it might local resorce, e.g. string url = "../../sample_mpeg4.mp4";
            var url = "http://clips.vorwaerts-gmbh.de/big_buck_bunny.mp4"; // be advised this file holds 1440 frames
            using var vsd = new VideoStreamDecoder(url, AVHWDeviceType.AV_HWDEVICE_TYPE_NONE);

            Console.WriteLine($"codec name: {vsd.CodecName}");

            var info = vsd.GetContextInfo();
            info.ToList().ForEach(x => Console.WriteLine($"{x.Key} = {x.Value}"));
            Console.WriteLine($"fps: {vsd.FrameRate}");

            var sourceSize = vsd.FrameSize;
            var sourcePixelFormat = vsd.PixelFormat;
            var destinationSize = sourceSize;
            var destinationPixelFormat = AVPixelFormat.AV_PIX_FMT_BGR24;
            using var vfc = new VideoFrameConverter(sourceSize, sourcePixelFormat, destinationSize, destinationPixelFormat);

            var stopFrame = stopTime * vsd.FrameRate;
            var frameNumber = 0;

            while (vsd.TryDecodeNextFrame(out var frame))
            {
                if (frameNumber < stopFrame)
                {
                    Console.WriteLine($"frame: {frameNumber}");
                    frameNumber++;
                    continue;
                }

                var f = vfc.Convert(frame);
                using (var bitmap = new Bitmap(f.width, f.height, f.linesize[0], PixelFormat.Format24bppRgb, (IntPtr)f.data[0]))
                    bitmap.Save($"frame.{stopTime}.jpg", ImageFormat.Jpeg);
                break;
            }
        }
    }
}

