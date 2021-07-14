using System;

namespace LockControlSample
{
    public class LockInstanceEventArgs : EventArgs
    {
        public LockInstanceEventArgs(object reference)
        {
            this.Reference = reference;
        }

        public object Reference { get; }
    }
}
