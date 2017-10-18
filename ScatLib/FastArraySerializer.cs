using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using System.Runtime.InteropServices;

namespace ScatLib
{
    public static unsafe class FastArraySerializer
    {
        [StructLayout(LayoutKind.Explicit)]
        private struct Union
        {
            [FieldOffset(0)]
            public byte[] bytes;
            [FieldOffset(0)]
            public double[] doubles;
            [FieldOffset(0)]
            public int[] ints;
            [FieldOffset(0)]
            public Complex[] complex;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct ArrayHeader
        {
            public UIntPtr type;
            public UIntPtr length;
        }

        private static readonly UIntPtr BYTE_ARRAY_TYPE;
        private static readonly UIntPtr DOUBLE_ARRAY_TYPE;
        private static readonly UIntPtr INT_ARRAY_TYPE;
        private static readonly UIntPtr COMPLEX_ARRAY_TYPE;

        static FastArraySerializer()
        {
            fixed (void* pBytes = new byte[1])
            fixed (void* pDoubles = new double[1])
            fixed (void* pInts = new int[1])
            fixed (void* pComplex = new Complex[1])
            {
                BYTE_ARRAY_TYPE = getHeader(pBytes)->type;
                DOUBLE_ARRAY_TYPE = getHeader(pDoubles)->type;
                INT_ARRAY_TYPE = getHeader(pInts)->type;
                COMPLEX_ARRAY_TYPE = getHeader(pComplex)->type;
            }
        }

        public static void AsByteArray(this double[] doubles, Action<byte[]> action)
        {
            if (doubles.handleNullOrEmptyArray(action))
                return;

            var union = new Union { doubles = doubles };
            union.doubles.toByteArray();
            try
            {
                action(union.bytes);
            }
            finally
            {
                union.bytes.toDoubleArray();
            }
        }

        public static void AsByteArray(this int[] ints, Action<byte[]> action)
        {
            if (ints.handleNullOrEmptyArray(action))
                return;

            var union = new Union { ints = ints };
            union.ints.toByteArray();
            try
            {
                action(union.bytes);
            }
            finally
            {
                union.bytes.toIntArray();
            }
        }

        public static void AsByteArray(this Complex[] complex, Action<byte[]> action)
        {
            if (complex.handleNullOrEmptyArray(action))
                return;

            var union = new Union { complex = complex };
            union.complex.toByteArray();
            try
            {
                action(union.bytes);
            }
            finally
            {
                union.bytes.toComplexArray();
            }
        }

        public static void AsDoubleArray(this byte[] bytes, Action<double[]> action)
        {
            if (bytes.handleNullOrEmptyArray(action))
                return;

            var union = new Union { bytes = bytes };
            union.bytes.toDoubleArray();
            try
            {
                action(union.doubles);
            }
            finally
            {
                union.doubles.toByteArray();
            }
        }

        public static void AsIntArray(this byte[] bytes, Action<int[]> action)
        {
            if (bytes.handleNullOrEmptyArray(action))
                return;

            var union = new Union { bytes = bytes };
            union.bytes.toIntArray();
            try
            {
                action(union.ints);
            }
            finally
            {
                union.ints.toByteArray();
            }
        }

        public static void AsComplexArray(this byte[] bytes, Action<Complex[]> action)
        {
            if (bytes.handleNullOrEmptyArray(action))
                return;

            var union = new Union { bytes = bytes };
            union.bytes.toComplexArray();
            try
            {
                action(union.complex);
            }
            finally
            {
                union.complex.toByteArray();
            }
        }

        public static bool handleNullOrEmptyArray<TSrc, TDst>(this TSrc[] array, Action<TDst[]> action)
        {
            if (array == null)
            {
                action(null);
                return true;
            }

            if (array.Length == 0)
            {
                action(new TDst[0]);
                return true;
            }

            return false;
        }

        private static ArrayHeader* getHeader(void* pBytes)
        {
            return (ArrayHeader*)pBytes - 1;
        }

        private static void toDoubleArray(this byte[] bytes)
        {
            fixed (void* pArray = bytes)
            {
                var pHeader = getHeader(pArray);

                pHeader->type = DOUBLE_ARRAY_TYPE;
                pHeader->length = (UIntPtr)(bytes.Length / sizeof(double));
            }
        }

        private static void toIntArray(this byte[] bytes)
        {
            fixed (void* pArray = bytes)
            {
                var pHeader = getHeader(pArray);

                pHeader->type = INT_ARRAY_TYPE;
                pHeader->length = (UIntPtr)(bytes.Length / sizeof(int));
            }
        }

        private static void toComplexArray(this byte[] bytes)
        {
            fixed (void* pArray = bytes)
            {
                var pHeader = getHeader(pArray);

                pHeader->type = COMPLEX_ARRAY_TYPE;
                pHeader->length = (UIntPtr)(bytes.Length / sizeof(Complex));
            }
        }

        private static void toByteArray(this double[] doubles)
        {
            fixed (void* pArray = doubles)
            {
                var pHeader = getHeader(pArray);

                pHeader->type = BYTE_ARRAY_TYPE;
                pHeader->length = (UIntPtr)(doubles.Length * sizeof(double));
            }
        }

        private static void toByteArray(this int[] ints)
        {
            fixed (void* pArray = ints)
            {
                var pHeader = getHeader(pArray);

                pHeader->type = BYTE_ARRAY_TYPE;
                pHeader->length = (UIntPtr)(ints.Length * sizeof(int));
            }
        }

        private static void toByteArray(this Complex[] complex)
        {
            fixed (void* pArray = complex)
            {
                var pHeader = getHeader(pArray);

                pHeader->type = BYTE_ARRAY_TYPE;
                pHeader->length = (UIntPtr)(complex.Length * sizeof(Complex));
            }
        }
    }
}
