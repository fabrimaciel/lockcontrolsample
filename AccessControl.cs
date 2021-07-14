using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace LockControlSample
{

    public class AccessControl : IDisposable
    {
        private static readonly object WildCartReference = new object();
        private readonly Queue<LockInfo> locks = new Queue<LockInfo>();

        public event EventHandler<LockInstanceEventArgs> LockInstanceCreated;

        public event EventHandler<LockInstanceEventArgs> LockInstanceReleased;

        ~AccessControl() => this.Dispose(false);

        public IDisposable CreateLocker(object reference) =>
            this.CreateLocker(reference, System.Threading.Timeout.InfiniteTimeSpan);

        public IDisposable CreateLocker(object reference, TimeSpan timeout)
        {
            if (reference == null)
            {
                reference = WildCartReference;
            }

            LockInfo info;
            LockInfo currentInfo = null;
            var isNew = false;

            lock (this.locks)
            {
                while (this.locks.Count > 0)
                {
                    currentInfo = this.locks.Peek();
                    if (currentInfo.Count <= 0)
                    {
                        this.locks.Dequeue().Dispose();
                    }
                    else
                    {
                        break;
                    }
                }

                info = this.locks.FirstOrDefault(f => f.Reference == reference);

                if (info == null)
                {
                    isNew = true;
                    info = new LockInfo(this, reference);
                    this.locks.Enqueue(info);
                }
                else
                {
                    info.Count++;
                }
            }

            if (currentInfo != null && currentInfo.Reference != reference)
            {
                if ((timeout == Timeout.InfiniteTimeSpan && !currentInfo.AllDone.WaitOne()) ||
                    (timeout != Timeout.InfiniteTimeSpan && !currentInfo.AllDone.WaitOne(timeout)))
                {
                    this.Release(currentInfo);
                }
            }

            if (isNew)
            {
                this.OnCreateLockInstance(reference);
            }

            return new ReleaseController(this, info);
        }

        protected virtual void OnCreateLockInstance(object reference)
        {
            this.LockInstanceCreated?.Invoke(this, new LockInstanceEventArgs(reference));
        }

        protected virtual void OnReleseLockInstance(object reference)
        {
            this.LockInstanceReleased?.Invoke(this, new LockInstanceEventArgs(reference));
        }

        private void Release(LockInfo info)
        {
            info.Count--;

            lock (this.locks)
            {
                while (this.locks.Count > 0)
                {
                    if (this.locks.Peek().Count <= 0)
                    {
                        this.locks.Dequeue().Dispose();
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            while (this.locks.Count > 0)
            {
                this.Release(this.locks.Peek());
            }
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        private sealed class LockInfo : IDisposable
        {
            private readonly AccessControl accessControl;
            private bool disposed;

            public LockInfo(AccessControl accessControl, object reference)
            {
                this.accessControl = accessControl;
                this.Reference = reference;
                this.AllDone = new ManualResetEvent(false);
                this.Count = 1;
            }

            public object Reference { get; }

            public int Count { get; set; }

            public ManualResetEvent AllDone { get; }

            public void Dispose()
            {
                if (!this.disposed)
                {
                    this.disposed = true;
                    this.accessControl.OnReleseLockInstance(this.Reference);
                    this.AllDone.Set();
                }
            }
        }

        private sealed class ReleaseController : IDisposable
        {
            private readonly AccessControl accessControl;
            private readonly LockInfo info;
            private bool disposed = false;

            public ReleaseController(AccessControl accessControl, LockInfo info)
            {
                this.accessControl = accessControl;
                this.info = info;
            }

            ~ReleaseController() => this.Dispose();

            public void Dispose()
            {
                if (!this.disposed)
                {
                    this.disposed = true;
                    accessControl.Release(info);
                }
            }
        }
    }
}
