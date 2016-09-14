using System;
using System.ComponentModel;
using System.IO;
using Mina.Core.Session;

namespace Mina.Transport.File
{
    /// <summary>
    ///     文件配置
    /// </summary>
    public class FileSessionConfig : IoSessionConfig
    {
        public FileSessionConfig()
        {
            ReadSpan = 1000;
            BytesPerRead = 1024;
            ReaderIdleTime = 10*60;
        }

        /// <summary>
        ///     文件读取1次间隔
        /// </summary>
        public int ReadSpan { get; set; }

        /// <summary>
        ///     每次读取字节数
        /// </summary>
        public int BytesPerRead { get; set; }

        /// <summary>
        ///     循环读取
        /// </summary>
        public bool CycleRead { get; set; }
       

        public int GetIdleTime(IdleStatus status)
        {
            switch (status)
            {
                case IdleStatus.ReaderIdle:
                    return ReaderIdleTime;
                case IdleStatus.WriterIdle:
                    return WriteTimeout;
                case IdleStatus.BothIdle:
                    return BothIdleTime;
                default:
                    throw new ArgumentException("Unknown status", nameof(status));
            }
        }

        public long GetIdleTimeInMillis(IdleStatus status)
        {
            return GetIdleTime(status)*1000L;
        }

        public void SetIdleTime(IdleStatus status, int idleTime)
        {
            switch (status)
            {
                case IdleStatus.ReaderIdle:
                    ReaderIdleTime = idleTime;
                    break;
                case IdleStatus.WriterIdle:
                    WriteTimeout = idleTime;
                    break;
                case IdleStatus.BothIdle:
                    BothIdleTime = idleTime;
                    break;
                default:
                    throw new ArgumentException("Unknown status", nameof(status));
            }
        }

        public void SetAll(IoSessionConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));
            ReadBufferSize = config.ReadBufferSize;
            ThroughputCalculationInterval = config.ThroughputCalculationInterval;
            ReaderIdleTime = config.ReaderIdleTime;
            WriterIdleTime = config.WriterIdleTime;
            BothIdleTime = config.BothIdleTime;
            WriteTimeout = config.WriteTimeout;
        }

        public int ReadBufferSize { get; set; }
        public int ThroughputCalculationInterval { get; set; }
        public long ThroughputCalculationIntervalInMillis => ThroughputCalculationInterval*1000L;
        public int ReaderIdleTime { get; set; }
        public int WriterIdleTime { get; set; }
        public int BothIdleTime { get; set; }
        public int WriteTimeout { get; set; }
        public long WriteTimeoutInMillis => WriteTimeout*1000L;
    }
}