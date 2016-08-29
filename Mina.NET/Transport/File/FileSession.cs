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
        public static readonly ITransportMetadata Metadata
            = new DefaultTransportMetadata("mina", "file", false, true, typeof(FileEndPoint));

        private FileStream fileStream;

        private Thread readThread;

        public FileSession(FileConnector service, FileEndPoint endPoint)
            : base(service)
        {
            Config = service.SessionConfig;
            Processor = service;
            RemoteEndPoint = endPoint;
            FilterChain = new DefaultIoFilterChain(this);
        }

        /// <summary>
        ///     指示文件读取服务是否正在运行
        /// </summary>
        public bool Reading { get; private set; }

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
        /// 读取路径中的文件
        /// </summary>
        private void ReadPath()
        {
            var endPoint = (FileEndPoint) RemoteEndPoint;
            if (endPoint.PathType == PathType.Directory)
                ReadDirectory(endPoint.Path);
            else if (endPoint.PathType == PathType.File)
                ReadSingleFile(endPoint.Path);
            else
                FilterChain.FireExceptionCaught(
                    new FileNotFoundException($"Can not find file or directory {endPoint.Path}"));
        }

        /// <summary>
        ///     读取单个文件线程
        /// </summary>
        private void ReadSingleFile(string file)
        {
            var cfg = (FileSessionConfig) Config;
            try
            {
                using (fileStream = new FileStream(file, FileMode.OpenOrCreate, FileAccess.Read))
                {
                    while (Reading & fileStream.CanRead)
                    {
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
                        var sleep = cfg.ReadSpan > used.Milliseconds ? cfg.ReadSpan - used.Milliseconds : 0;
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
            var cfg = (FileSessionConfig) Config;
            var files = Directory.GetFiles(directory);
            try
            {
                foreach (var file in files)
                    using (fileStream = new FileStream(file, FileMode.Open, FileAccess.Read))
                    {
                        while (Reading & fileStream.CanRead)
                        {
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
                            var sleep = cfg.ReadSpan > used.Milliseconds ? cfg.ReadSpan - used.Milliseconds : 0;
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