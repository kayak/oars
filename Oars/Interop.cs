using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace Oars
{
    internal struct timeval
    {
        public int tv_sec;
        public int tv_usec;

        public static timeval FromTimeSpan(TimeSpan ts)
        {
            var sex = (int)(ts.Ticks / 10000000);
            var usex = (int)(ts.Ticks % 10000000) / 10;
            
            return new timeval() { tv_sec = sex, tv_usec = usex };
        }

        public TimeSpan ToTimeSpan()
        {
            return new TimeSpan(tv_usec * 10L + tv_sec * 10000000L);
        }

        public DateTime ToDateTime()
        {
            return new DateTime(1970, 1, 1).AddSeconds(tv_sec).AddMilliseconds(tv_usec / 1000);
        }
    }

    // represents an IPv4 end point
    [StructLayout(LayoutKind.Sequential)]
    unsafe struct sockaddr_in
    {
        public static short StructureLength = 16;

        public short sin_family;
        public ushort sin_port;
        public in_addr sin_addr;
        public fixed byte sin_zero[8];

        public static sockaddr_in FromIPEndPoint(IPEndPoint ep)
        {
            if (ep.AddressFamily != AddressFamily.InterNetwork)
                throw new ArgumentException("endpoint.AddressFamily must be AddressFamily.InterNetwork");

            // works on linux.
            short family = (short)ep.AddressFamily;
			
            // works on Darwin (possibly other BSDs?)
            if (OperatingSystem.Platform == PlatformID.MacOSX)
                family = IPAddress.HostToNetworkOrder(family);
				
            return new sockaddr_in()
            {
                sin_family = family,
                sin_port = (ushort)IPAddress.HostToNetworkOrder((short)ep.Port),
                sin_addr = new in_addr() { s_addr = 0 } // sorry, localhost only for now!
            };
        }

        public IPEndPoint ToIPEndPoint()
        {
            //var port = IPAddress.NetworkToHostOrder(sin_port);
            var port = sin_port;
            return new IPEndPoint(new IPAddress(IPAddress.NetworkToHostOrder(sin_addr.s_addr)), port);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    struct in_addr
    {
        public long s_addr;
    }
	
	static class OperatingSystem
	{
        static bool gotPlatform;
        static PlatformID platform;

        // Environment.OSVersion.Platform never returns 
        // PlatformID.MacOSX on Mono, so we're going to hack it...
        public static PlatformID Platform
        {
            get
            {
                if (!gotPlatform)
                {
                    gotPlatform = true;

                    platform = Environment.OSVersion.Platform;

                    if (platform == PlatformID.Unix && IsRunningOnMac())
                        platform = PlatformID.MacOSX;
                }

                return platform;
            }
        }
			
        [DllImport("libc")]
        static extern int uname(IntPtr buf); 
		
        // crazy bullshit!
        // http://go-mono.com/forums/#nabble-td1549244%7Ca1549244
		static bool IsRunningOnMac()
		{
		    IntPtr buf = IntPtr.Zero;
		    try
		    {
		        buf = Marshal.AllocHGlobal(8192);
		        if (uname(buf) == 0)
		        {
		            string os = Marshal.PtrToStringAnsi(buf);
		            if (os == "Darwin") return true;
		        }
		    }
		    catch { }
		    finally
		    {
		        if (buf != IntPtr.Zero) Marshal.FreeHGlobal(buf);
		    }
		    return false;
		}
    }

    public static class FDExtensions
    {
        public static int Close(this IntPtr fd)
        {
            return close(fd);
        }

        public static int Recv(this IntPtr fd, ArraySegment<byte> buffer, int flags)
        {
            unsafe
            {
                fixed (byte* ptr = &(buffer.Array[buffer.Offset]))
                    return recv(fd, ptr, buffer.Count, flags);
            }
        }

        public static int Send(this IntPtr fd, ArraySegment<byte> buffer, int flags)
        {
            unsafe {
                fixed (byte *ptr = &(buffer.Array[buffer.Offset]))
                    return send(fd, ptr, buffer.Count, flags);
            }
        }

        [DllImport("libc")]
        static extern int close(IntPtr fd);

        [DllImport("libc")]
        static unsafe extern int send(IntPtr fd, byte* buffer, int length, int flags);

        [DllImport("libc")]
        static unsafe extern int recv(IntPtr fd, byte* buffer, int length, int flags);
    }

    // lifted this from Mono.Unix/Stdlib.cs
    public enum Errno : int
    {
        // errors & their values liberally copied from
        // FC2 /usr/include/asm/errno.h

        //EPERM = 1, // Operation not permitted 
        //ENOENT = 2, // No such file or directory 
        //ESRCH = 3, // No such process 
        //EINTR = 4, // Interrupted system call 
        //EIO = 5, // I/O error 
        //ENXIO = 6, // No such device or address 
        //E2BIG = 7, // Arg list too long 
        //ENOEXEC = 8, // Exec format error 
        //EBADF = 9, // Bad file number 
        //ECHILD = 10, // No child processes 
        EAGAIN = 11, // Try again 
        //ENOMEM = 12, // Out of memory 
        //EACCES = 13, // Permission denied 
        //EFAULT = 14, // Bad address 
        //ENOTBLK = 15, // Block device required 
        //EBUSY = 16, // Device or resource busy 
        //EEXIST = 17, // File exists 
        //EXDEV = 18, // Cross-device link 
        //ENODEV = 19, // No such device 
        //ENOTDIR = 20, // Not a directory 
        //EISDIR = 21, // Is a directory 
        //EINVAL = 22, // Invalid argument 
        //ENFILE = 23, // File table overflow 
        //EMFILE = 24, // Too many open files 
        //ENOTTY = 25, // Not a typewriter 
        //ETXTBSY = 26, // Text file busy 
        //EFBIG = 27, // File too large 
        //ENOSPC = 28, // No space left on device 
        //ESPIPE = 29, // Illegal seek 
        //EROFS = 30, // Read-only file system 
        //EMLINK = 31, // Too many links 
        //EPIPE = 32, // Broken pipe 
        //EDOM = 33, // Math argument out of domain of func 
        //ERANGE = 34, // Math result not representable 
        //EDEADLK = 35, // Resource deadlock would occur 
        //ENAMETOOLONG = 36, // File name too long 
        //ENOLCK = 37, // No record locks available 
        //ENOSYS = 38, // Function not implemented 
        //ENOTEMPTY = 39, // Directory not empty 
        //ELOOP = 40, // Too many symbolic links encountered 
        //EWOULDBLOCK = EAGAIN, // Operation would block 
        //ENOMSG = 42, // No message of desired type 
        //EIDRM = 43, // Identifier removed 
        //ECHRNG = 44, // Channel number out of range 
        //EL2NSYNC = 45, // Level 2 not synchronized 
        //EL3HLT = 46, // Level 3 halted 
        //EL3RST = 47, // Level 3 reset 
        //ELNRNG = 48, // Link number out of range 
        //EUNATCH = 49, // Protocol driver not attached 
        //ENOCSI = 50, // No CSI structure available 
        //EL2HLT = 51, // Level 2 halted 
        //EBADE = 52, // Invalid exchange 
        //EBADR = 53, // Invalid request descriptor 
        //EXFULL = 54, // Exchange full 
        //ENOANO = 55, // No anode 
        //EBADRQC = 56, // Invalid request code 
        //EBADSLT = 57, // Invalid slot 

        //EDEADLOCK = EDEADLK,

        //EBFONT = 59, // Bad font file format 
        //ENOSTR = 60, // Device not a stream 
        //ENODATA = 61, // No data available 
        //ETIME = 62, // Timer expired 
        //ENOSR = 63, // Out of streams resources 
        //ENONET = 64, // Machine is not on the network 
        //ENOPKG = 65, // Package not installed 
        //EREMOTE = 66, // Object is remote 
        //ENOLINK = 67, // Link has been severed 
        //EADV = 68, // Advertise error 
        //ESRMNT = 69, // Srmount error 
        //ECOMM = 70, // Communication error on send 
        //EPROTO = 71, // Protocol error 
        //EMULTIHOP = 72, // Multihop attempted 
        //EDOTDOT = 73, // RFS specific error 
        //EBADMSG = 74, // Not a data message 
        //EOVERFLOW = 75, // Value too large for defined data type 
        //ENOTUNIQ = 76, // Name not unique on network 
        //EBADFD = 77, // File descriptor in bad state 
        //EREMCHG = 78, // Remote address changed 
        //ELIBACC = 79, // Can not access a needed shared library 
        //ELIBBAD = 80, // Accessing a corrupted shared library 
        //ELIBSCN = 81, // .lib section in a.out corrupted 
        //ELIBMAX = 82, // Attempting to link in too many shared libraries 
        //ELIBEXEC = 83, // Cannot exec a shared library directly 
        //EILSEQ = 84, // Illegal byte sequence 
        //ERESTART = 85, // Interrupted system call should be restarted 
        //ESTRPIPE = 86, // Streams pipe error 
        //EUSERS = 87, // Too many users 
        //ENOTSOCK = 88, // Socket operation on non-socket 
        //EDESTADDRREQ = 89, // Destination address required 
        //EMSGSIZE = 90, // Message too long 
        //EPROTOTYPE = 91, // Protocol wrong type for socket 
        //ENOPROTOOPT = 92, // Protocol not available 
        //EPROTONOSUPPORT = 93, // Protocol not supported 
        //ESOCKTNOSUPPORT = 94, // Socket type not supported 
        //EOPNOTSUPP = 95, // Operation not supported on transport endpoint 
        //EPFNOSUPPORT = 96, // Protocol family not supported 
        //EAFNOSUPPORT = 97, // Address family not supported by protocol 
        //EADDRINUSE = 98, // Address already in use 
        //EADDRNOTAVAIL = 99, // Cannot assign requested address 
        //ENETDOWN = 100, // Network is down 
        //ENETUNREACH = 101, // Network is unreachable 
        //ENETRESET = 102, // Network dropped connection because of reset 
        //ECONNABORTED = 103, // Software caused connection abort 
        //ECONNRESET = 104, // Connection reset by peer 
        //ENOBUFS = 105, // No buffer space available 
        //EISCONN = 106, // Transport endpoint is already connected 
        //ENOTCONN = 107, // Transport endpoint is not connected 
        //ESHUTDOWN = 108, // Cannot send after transport endpoint shutdown 
        //ETOOMANYREFS = 109, // Too many references: cannot splice 
        //ETIMEDOUT = 110, // Connection timed out 
        //ECONNREFUSED = 111, // Connection refused 
        //EHOSTDOWN = 112, // Host is down 
        //EHOSTUNREACH = 113, // No route to host 
        //EALREADY = 114, // Operation already in progress 
        //EINPROGRESS = 115, // Operation now in progress 
        //ESTALE = 116, // Stale NFS file handle 
        //EUCLEAN = 117, // Structure needs cleaning 
        //ENOTNAM = 118, // Not a XENIX named type file 
        //ENAVAIL = 119, // No XENIX semaphores available 
        //EISNAM = 120, // Is a named type file 
        //EREMOTEIO = 121, // Remote I/O error 
        //EDQUOT = 122, // Quota exceeded 

        //ENOMEDIUM = 123, // No medium found 
        //EMEDIUMTYPE = 124, // Wrong medium type 
    }
}
