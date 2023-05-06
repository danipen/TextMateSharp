using System;
using System.Runtime.InteropServices;

namespace TextMateSharp.Internal.Oniguruma
{
    internal unsafe interface IOnigInterop
    {
        IntPtr onigwrap_create(char* pattern, int len, int ignoreCase, int multiline);
        IntPtr onigwrap_region_new();
        void onigwrap_region_free(IntPtr region);
        void onigwrap_free(IntPtr regex);
        int onigwrap_search(IntPtr regex, char* text, int offset, int length, IntPtr region);
        int onigwrap_num_regs(IntPtr region);
        int onigwrap_pos(IntPtr region, int nth);
        int onigwrap_len(IntPtr region, int nth);
    }

    internal unsafe class OnigInterop
    {
        internal static IOnigInterop Instance { get; private set; }

        static OnigInterop()
        {
            Instance = CreateInterop();
        }

        static IOnigInterop CreateInterop()
        {
            if (!IsWindowsPlatform())
                return new InteropUnix();

            if (Environment.Is64BitProcess)
                return new InteropWin64();

            return new InteropWin32();
        }

        static bool IsWindowsPlatform()
        {
            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.Win32Windows:
                case PlatformID.Win32NT:
                case PlatformID.Win32S:
                    return true;
                default:
                    return false;
            }
        }

        internal unsafe class InteropWin64 : IOnigInterop
        {
            const string ONIGWRAP = "onigwrap-x64";
            const CharSet charSet = CharSet.Unicode;
            const CallingConvention convention = CallingConvention.Cdecl;

            IntPtr IOnigInterop.onigwrap_create(char* pattern, int len, int ignoreCase, int multiline)
            {
                return onigwrap_create(pattern, len, ignoreCase, multiline);
            }

            void IOnigInterop.onigwrap_free(IntPtr regex)
            {
                onigwrap_free(regex);
            }

            int IOnigInterop.onigwrap_len(IntPtr region, int nth)
            {
                return onigwrap_len(region, nth);
            }

            int IOnigInterop.onigwrap_num_regs(IntPtr region)
            {
                return onigwrap_num_regs(region);
            }

            int IOnigInterop.onigwrap_pos(IntPtr region, int nth)
            {
                return onigwrap_pos(region, nth);
            }

            void IOnigInterop.onigwrap_region_free(IntPtr region)
            {
                onigwrap_region_free(region);
            }

            IntPtr IOnigInterop.onigwrap_region_new()
            {
                return onigwrap_region_new();
            }

            int IOnigInterop.onigwrap_search(IntPtr regex, char* text, int offset, int length, IntPtr region)
            {
                return onigwrap_search(regex, text, offset, length, region);
            }


            [DllImport(ONIGWRAP, CharSet = charSet, CallingConvention = convention)]
            static extern IntPtr onigwrap_create(char* pattern, int len, int ignoreCase, int multiline);

            [DllImport(ONIGWRAP, CharSet = charSet, CallingConvention = convention)]
            static extern IntPtr onigwrap_region_new();

            [DllImport(ONIGWRAP, CharSet = charSet, CallingConvention = convention)]
            static extern void onigwrap_region_free(IntPtr region);

            [DllImport(ONIGWRAP, CharSet = charSet, CallingConvention = convention)]
            static extern void onigwrap_free(IntPtr regex);

            [DllImport(ONIGWRAP, CharSet = charSet, CallingConvention = convention)]
            static extern int onigwrap_search(IntPtr regex, char* text, int offset, int length, IntPtr region);

            [DllImport(ONIGWRAP, CharSet = charSet, CallingConvention = convention)]
            static extern int onigwrap_num_regs(IntPtr region);

            [DllImport(ONIGWRAP, CharSet = charSet, CallingConvention = convention)]
            static extern int onigwrap_pos(IntPtr region, int nth);

            [DllImport(ONIGWRAP, CharSet = charSet, CallingConvention = convention)]
            static extern int onigwrap_len(IntPtr region, int nth);
        }
        internal unsafe class InteropWin32 : IOnigInterop
        {
            const string ONIGWRAP = "onigwrap-x86";
            const CharSet charSet = CharSet.Unicode;
            const CallingConvention convention = CallingConvention.Cdecl;

            IntPtr IOnigInterop.onigwrap_create(char* pattern, int len, int ignoreCase, int multiline)
            {
                return onigwrap_create(pattern, len, ignoreCase, multiline);
            }

            void IOnigInterop.onigwrap_free(IntPtr regex)
            {
                onigwrap_free(regex);
            }

            int IOnigInterop.onigwrap_len(IntPtr region, int nth)
            {
                return onigwrap_len(region, nth);
            }

            int IOnigInterop.onigwrap_num_regs(IntPtr region)
            {
                return onigwrap_num_regs(region);
            }

            int IOnigInterop.onigwrap_pos(IntPtr region, int nth)
            {
                return onigwrap_pos(region, nth);
            }

            void IOnigInterop.onigwrap_region_free(IntPtr region)
            {
                onigwrap_region_free(region);
            }

            IntPtr IOnigInterop.onigwrap_region_new()
            {
                return onigwrap_region_new();
            }

            int IOnigInterop.onigwrap_search(IntPtr regex, char* text, int offset, int length, IntPtr region)
            {
                return onigwrap_search(regex, text, offset, length, region);
            }


            [DllImport(ONIGWRAP, CharSet = charSet, CallingConvention = convention)]
            static extern IntPtr onigwrap_create(char* pattern, int len, int ignoreCase, int multiline);

            [DllImport(ONIGWRAP, CharSet = charSet, CallingConvention = convention)]
            static extern IntPtr onigwrap_region_new();

            [DllImport(ONIGWRAP, CharSet = charSet, CallingConvention = convention)]
            static extern void onigwrap_region_free(IntPtr region);

            [DllImport(ONIGWRAP, CharSet = charSet, CallingConvention = convention)]
            static extern void onigwrap_free(IntPtr regex);

            [DllImport(ONIGWRAP, CharSet = charSet, CallingConvention = convention)]
            static extern int onigwrap_search(IntPtr regex, char* text, int offset, int length, IntPtr region);

            [DllImport(ONIGWRAP, CharSet = charSet, CallingConvention = convention)]
            static extern int onigwrap_num_regs(IntPtr region);

            [DllImport(ONIGWRAP, CharSet = charSet, CallingConvention = convention)]
            static extern int onigwrap_pos(IntPtr region, int nth);

            [DllImport(ONIGWRAP, CharSet = charSet, CallingConvention = convention)]
            static extern int onigwrap_len(IntPtr region, int nth);
        }
        internal unsafe class InteropUnix : IOnigInterop
        {
            const string ONIGWRAP = "onigwrap";
            const CharSet charSet = CharSet.Unicode;
            const CallingConvention convention = CallingConvention.Cdecl;

            IntPtr IOnigInterop.onigwrap_create(char* pattern, int len, int ignoreCase, int multiline)
            {
                return onigwrap_create(pattern, len, ignoreCase, multiline);
            }

            void IOnigInterop.onigwrap_free(IntPtr regex)
            {
                onigwrap_free(regex);
            }

            int IOnigInterop.onigwrap_len(IntPtr region, int nth)
            {
                return onigwrap_len(region, nth);
            }

            int IOnigInterop.onigwrap_num_regs(IntPtr region)
            {
                return onigwrap_num_regs(region);
            }

            int IOnigInterop.onigwrap_pos(IntPtr region, int nth)
            {
                return onigwrap_pos(region, nth);
            }

            void IOnigInterop.onigwrap_region_free(IntPtr region)
            {
                onigwrap_region_free(region);
            }

            IntPtr IOnigInterop.onigwrap_region_new()
            {
                return onigwrap_region_new();
            }

            int IOnigInterop.onigwrap_search(IntPtr regex, char* text, int offset, int length, IntPtr region)
            {
                return onigwrap_search(regex, text, offset, length, region);
            }


            [DllImport(ONIGWRAP, CharSet = charSet, CallingConvention = convention)]
            static extern IntPtr onigwrap_create(char* pattern, int len, int ignoreCase, int multiline);

            [DllImport(ONIGWRAP, CharSet = charSet, CallingConvention = convention)]
            static extern IntPtr onigwrap_region_new();

            [DllImport(ONIGWRAP, CharSet = charSet, CallingConvention = convention)]
            static extern void onigwrap_region_free(IntPtr region);

            [DllImport(ONIGWRAP, CharSet = charSet, CallingConvention = convention)]
            static extern void onigwrap_free(IntPtr regex);

            [DllImport(ONIGWRAP, CharSet = charSet, CallingConvention = convention)]
            static extern int onigwrap_search(IntPtr regex, char* text, int offset, int length, IntPtr region);

            [DllImport(ONIGWRAP, CharSet = charSet, CallingConvention = convention)]
            static extern int onigwrap_num_regs(IntPtr region);

            [DllImport(ONIGWRAP, CharSet = charSet, CallingConvention = convention)]
            static extern int onigwrap_pos(IntPtr region, int nth);

            [DllImport(ONIGWRAP, CharSet = charSet, CallingConvention = convention)]
            static extern int onigwrap_len(IntPtr region, int nth);
        }
    }
}