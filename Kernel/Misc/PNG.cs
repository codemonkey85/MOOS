﻿using System.Drawing;
using System.Runtime.InteropServices;

namespace Kernel.Misc
{
    public unsafe class PNG : Image
    {
        public enum LodePNGColorType
        {
            LCT_GREY = 0, /*greyscale: 1,2,4,8,16 bit*/
            LCT_RGB = 2, /*RGB: 8,16 bit*/
            LCT_PALETTE = 3, /*palette: 1,2,4,8 bit*/
            LCT_GREY_ALPHA = 4, /*greyscale with alpha: 8,16 bit*/
            LCT_RGBA = 6 /*RGB with alpha: 8,16 bit*/
        }

        public PNG(byte[] file,LodePNGColorType type = LodePNGColorType.LCT_RGBA ,uint bitDepth = 8)
        {
            fixed (byte* p = file)
            {
                lodepng_decode_memory(out uint* _out, out uint w, out uint h, p, file.Length, type, bitDepth);

                if (_out == null) for (; ; ) Native.Hlt();
                RawData = new uint[w * h];
                fixed (uint* pdata = RawData)
                    Native.Movsd(pdata, _out, w * h);
                Allocator.Free((System.IntPtr)_out);
                Width = (int)w;
                Height = (int)h;
                Bpp = 4;
            }
        }

        [DllImport("*")]
        public static extern void lodepng_decode_memory(out uint* _out, out uint w, out uint h, byte* _in, int insize, LodePNGColorType colortype, uint bitdepth);
    }
}
