﻿using System;
using System.Runtime.InteropServices;

namespace Oars
{
    public enum LoopOptions : int
    {
        Once = 0x01,
        NonBlock = 0x02,
        NoExitOnEmpy = 0x04
    }

    public sealed class EventBase : IDisposable
    {
        public IntPtr Handle { get; private set; }

        public EventBase()
        {
            Handle = event_base_new();
        }

        public void Dispatch()
        {
            ThrowIfDisposed();

            event_base_dispatch(Handle);
        }

        public void Loop(LoopOptions options)
        {
            event_base_loop(Handle, (int)options);
        }

        public bool LoopExit()
        {
            // TODO pass in null for timeval
            return LoopExit(TimeSpan.FromSeconds(0.01));
        }

        public bool LoopExit(TimeSpan timeout)
        {
            ThrowIfDisposed();

            var tv = timeval.FromTimeSpan(timeout);
            return !(event_base_loopexit(Handle, ref tv) < 0);
        }

        public bool LoopBreak()
        {
            ThrowIfDisposed();

            return !(event_base_loopbreak(Handle) < 0);
        }

        public void Dispose()
        {
            ThrowIfDisposed();

            event_base_free(Handle);
            Handle = IntPtr.Zero;
        }

        public DateTime GetTimeOfDayCached()
        {
            ThrowIfDisposed();

            timeval tv;

            if (event_base_gettimeofday_cached(Handle, out tv) < 0)
                throw new Exception("event_core failed to get time!");

            return tv.ToDateTime();
        }

        public bool GotExit
        {
            get
            {
                ThrowIfDisposed();
                return event_base_got_exit(Handle) != 0;
            }
        }

        public bool GotBreak
        {
            get
            {
                ThrowIfDisposed();
                return event_base_got_break(Handle) != 0;
            }
        }

        void ThrowIfDisposed()
        {
            if (Handle == IntPtr.Zero)
                throw new ObjectDisposedException("Event");
        }

        [DllImport("event_core")]
        private static extern IntPtr event_base_new();

        [DllImport("event_core")]
        private static extern int event_base_free(IntPtr event_base);

        [DllImport("event_core")]
        private static extern int event_base_dispatch(IntPtr event_base);

        [DllImport("event_core")]
        private static extern int event_base_loop(IntPtr event_base, int flags);

        [DllImport("event_core")]
        private static extern int event_base_loopexit(IntPtr event_base, ref timeval timeval);

        [DllImport("event_core")]
        private static extern int event_base_loopbreak(IntPtr event_base);

        [DllImport("event_core")]
        private static extern int event_base_gettimeofday_cached(IntPtr event_base, out timeval timeval);

        [DllImport("event_core")]
        private static extern int event_base_got_exit(IntPtr event_base);

        [DllImport("event_core")]
        private static extern int event_base_got_break(IntPtr event_base);
    }
}
