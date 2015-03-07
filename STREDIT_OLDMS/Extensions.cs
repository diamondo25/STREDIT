using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System
{
    public static class Extensions
    {

        public static byte RollLeft(this byte pThis, int pCount)
        {
            uint overflow = ((uint)pThis) << (pCount % 8);
            return (byte)((overflow & 0xFF) | (overflow >> 8));
        }

        public static byte RollRight(this byte pThis, int pCount)
        {
            uint overflow = (((uint)pThis) << 8) >> (pCount % 8);
            return (byte)((overflow & 0xFF) | (overflow >> 8));
        }



        public static uint RollRight(this uint pThis, int pCount)
        {
            return (pThis >> pCount) | (pThis << (32 - pCount));


            uint res = pThis;
            for (int i = pCount & 0x1F; i > 0; --i)
            {
                uint temp = res & 1;
                res >>= 1;
                if (temp > 0)
                    res |= 0x80000000;
            }

            return res;
        }


        public static uint RollLeft(this uint pThis, int pCount)
        {
            return (pThis << pCount) | (pThis >> (32 - pCount));

            uint res = pThis;
            for (int i = pCount & 0x1F; i > 0; --i)
            {
                uint temp = res & 1;
                res *= 2;
                if (temp > 0)
                {
                    res -= (res & 0xFF);
                    res = res | 1;
                }
            }

            return res;
        }
    }
}
