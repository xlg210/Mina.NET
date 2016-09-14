using System;
using System.IO;
using System.Net;
using Mina.Core.Future;
using Mina.Core.Service;
using Mina.Core.Session;
using Mina.Core.Write;

namespace Mina.Transport.File
{
    /// <summary>
    ///     文件连接(读取)器,读取文件，无法对文件进行写入操作
    /// </summary>
    public class FileConnector : AbstractIoConnector, IoProcessor<FileSession>
    {
        private readonly IdleStatusChecker idleStatusChecker;
        private FileEndPoint endPoint;
        private FileStream fileStream;

        public FileConnector(IoSessionConfig sessionConfig) : base(sessionConfig)
        {
            idleStatusChecker = new IdleStatusChecker(() => ManagedSessions.Values);
        }

        public FileConnector() : base(new FileSessionConfig())
        {
            idleStatusChecker = new IdleStatusChecker(() => ManagedSessions.Values);
        }


        public new FileSessionConfig SessionConfig => (FileSessionConfig) base.SessionConfig;

        public override ITransportMetadata TransportMetadata
            => new DefaultTransportMetadata("mina", "file", false, true, typeof(FileEndPoint));

        protected override IConnectFuture Connect0(EndPoint remoteEP, EndPoint localEP,
            Action<IoSession, IConnectFuture> sessionInitializer)
        {
            endPoint = remoteEP as FileEndPoint;
            if (endPoint == null) throw new ArgumentException("EndPoint must be FileEndPoint!");
            IConnectFuture future = new DefaultConnectFuture();
            var session = new FileSession(this, endPoint);
            InitSession(session, future, sessionInitializer);

            try
            {
                session.Processor.Add(session);
            }
            catch (IOException ex)
            {
                return DefaultConnectFuture.NewFailedFuture(ex);
            }

            idleStatusChecker.Start();

            return future;
        }

        /// <summary>
        ///     Disposes.
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
                idleStatusChecker.Dispose();
            base.Dispose(disposing);
        }

        #region IoProcessor

        public void Add(IoSession session)
        {
            Add(session as FileSession);
        }

        public void Write(IoSession session, IWriteRequest writeRequest)
        {
            //Not Support
        }

        public void Flush(IoSession session)
        {
            //Not Support
        }

        public void Remove(IoSession session)
        {
            Remove(session as FileSession);
        }

        public void UpdateTrafficControl(IoSession session)
        {
            //Not Support
        }

        public void Add(FileSession session)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session) + " can not be null!");
            session.Service.FilterChainBuilder.BuildFilterChain(session.FilterChain);

            var serviceSupport = session.Service as IoServiceSupport;
            if (serviceSupport != null)
                serviceSupport.FireSessionCreated(session);
            session.Start();
        }

        public void Write(FileSession session, IWriteRequest writeRequest)
        {
            //Not Support
        }

        public void Flush(FileSession session)
        {
            //Not Support
        }

        public void Remove(FileSession session)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session) + " can not be null!");
            session.Stop();
            var support = session.Service as IoServiceSupport;
            if (support != null)
                support.FireSessionDestroyed(session);
        }

        public void UpdateTrafficControl(FileSession session)
        {
            //Not Support
        }

        #endregion
    }
}