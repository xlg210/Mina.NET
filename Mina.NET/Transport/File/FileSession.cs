using System;
using System.IO;
using System.Net;
using System.Threading;
using Mina.Core.Buffer;
using Mina.Core.Filterchain;
using Mina.Core.Service;
using Mina.Core.Session;

namespace Mina.Transport.File
{
    public class FileSession : AbstractIoSession
    {
        public const string FieldRound = "FileSession.Round";

        public static readonly ITransportMetadata Metadata
            = new DefaultTransportMetadata("mina", "file", false, true, typeof(FileEndPoint));

        private FileStream fileStream;

        private Thread readThread;

        public FileSession(FileConnector service, FileEndPoint endPoint)
            : base(service)
        {
            base.Config = service.SessionConfig;
            Processor = service;
            RemoteEndPoint = endPoint;
            FilterChain = new DefaultIoFilterChain(this);
        }

        /// <summary>
        ///     指示文件读取服务是否正在运行
        /// </summary>
        public bool Reading { get; private set; }

        public new FileSessionConfig Config => (FileSessionConfig) base.Config;

        public override IoProcessor Processor { get; }
        public override IoFilterChain FilterChain { get; }
        public override ITransportMetadata TransportMetadata => Metadata;
        public override EndPoint LocalEndPoint => null;
        public override EndPoint RemoteEndPoint { get; }

        /// <summary>
        ///     开启文件读取服务
        /// </summary>
        public void Start()
        {
            if (!Reading)
            {
                Reading = true;
                readThread = new Thread(ReadPath);
                readThread.Start();
            }
        }

        /// <summary>
        ///     停止文件读取服务
        /// </summary>
        public void Stop()
        {
            Reading = false;
        }

        /// <summary>
        ///     读取路径中的文件
        /// </summary>
        private void ReadPath()
        {
            while (Reading)
            {
                var round = GetAttribute(FieldRound, 0);
                SetAttribute(FieldRound, ++round);
                var endPoint = (FileEndPoint) RemoteEndPoint;
                if (endPoint.PathType == PathType.Directory)
                    ReadDirectory(endPoint.Path);
                else if (endPoint.PathType == PathType.File)
                    ReadSingleFile(endPoint.Path);
                else
                    FilterChain.FireExceptionCaught(
                        new FileNotFoundException($"Can not find file or directory {endPoint.Path}"));
                if (!Config.CycleRead) break;
            }
        }

        /// <summary>
        ///     读取单个文件线程
        /// </summary>
        private void ReadSingleFile(string file)
        {
            try
            {
                using (fileStream = new FileStream(file, FileMode.OpenOrCreate, FileAccess.Read))
                {
                    while (Reading & fileStream.CanRead)
                    {
                        if (ReadSuspended)
                        {
                            //暂停读取
                            Thread.Sleep(Math.Min(100, Config.ReadSpan));
                            continue;
                        }
                        var data = new byte[1024];
                        var begin = DateTime.Now;
                        var readBytes = fileStream.Read(data, 0, 1024);
                        if (readBytes == 0) break;
                        try
                        {
                            FilterChain.FireMessageReceived(IoBuffer.Wrap(data, 0, readBytes));
                        }
                        catch (Exception ex)
                        {
                            FilterChain.FireExceptionCaught(ex);
                        }
                        var used = DateTime.Now - begin;
                        var sleep = Config.ReadSpan > used.Milliseconds ? Config.ReadSpan - used.Milliseconds : 0;
                        Thread.Sleep(sleep);
                    }
                }
            }
            catch (Exception e)
            {
                FilterChain.FireExceptionCaught(e);
            }
        }

        /// <summary>
        ///     读取目录所有文件
        /// </summary>
        private void ReadDirectory(string directory)
        {
            var files = Directory.GetFiles(directory);
            try
            {
                foreach (var file in files)
                    using (fileStream = new FileStream(file, FileMode.Open, FileAccess.Read))
                    {
                        while (Reading & fileStream.CanRead)
                        {
                            if (ReadSuspended)
                            {
                                //暂停读取
                                Thread.Sleep(Math.Min(100, Config.ReadSpan));
                                continue;
                            }
                            var data = new byte[1024];
                            var begin = DateTime.Now;
                            var readBytes = fileStream.Read(data, 0, 1024);
                            if (readBytes == 0) break;
                            try
                            {
                                FilterChain.FireMessageReceived(IoBuffer.Wrap(data, 0, readBytes));
                            }
                            catch (Exception ex)
                            {
                                FilterChain.FireExceptionCaught(ex);
                            }
                            var used = DateTime.Now - begin;
                            var sleep = Config.ReadSpan > used.Milliseconds ? Config.ReadSpan - used.Milliseconds : 0;
                            Thread.Sleep(sleep);
                        }
                    }
            }
            catch (Exception e)
            {
                FilterChain.FireExceptionCaught(e);
            }
        }
    }
}