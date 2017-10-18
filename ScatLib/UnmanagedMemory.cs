using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace ScatLib
{
    unsafe internal static class UnmanagedMemory
    {
        private static int processHeap = GetProcessHeap();

        /// <summary>
        /// Allocates a memory block of the given size. The allocated memory is automatically initialized to zero.
        /// </summary>
        /// <param name="size">Desired size of a memory block.</param>
        /// <returns>Pointer of a memory block.</returns>
        public static void* Alloc(int size)
        {
            void* result = HeapAlloc(processHeap, HEAP_ZERO_MEMORY, size);
            if (result == null) throw new OutOfMemoryException();
            return result;
        }
        /// <summary>
        /// Copies count bytes from src to dst. The source and destination blocks are permitted to overlap.
        /// </summary>
        /// <param name="src">Source pointer.</param>
        /// <param name="dst">Destination pointer.</param>
        /// <param name="count">Number of bytes to be copied.</param>
        public static void Copy(void* src, void* dst, int count)
        {
            byte* ps = (byte*)src;
            byte* pd = (byte*)dst;
            if (ps > pd)
            {
                for (; count != 0; count--) *pd++ = *ps++;
            }
            else if (ps < pd)
            {
                for (ps += count, pd += count; count != 0; count--) *--pd = *--ps;
            }
        }
        /// <summary>
        /// Frees a memory block.
        /// </summary>
        /// <param name="block">Pointer of a memory block.</param>
        public static void Free(void* block)
        {
            if (!HeapFree(processHeap, 0, block)) throw new InvalidOperationException();
        }
        /// <summary>
        /// Re-allocates a memory block. If the reallocation request is for a larger size,
        /// the additional region of memory is automatically initialized to zero.
        /// </summary>
        /// <param name="block">Pointer of a memory block.</param>
        /// <param name="size">Desired size of a memory block.</param>
        /// <returns></returns>
        public static void* ReAlloc(void* block, int size)
        {
            void* result = HeapReAlloc(processHeap, HEAP_ZERO_MEMORY, block, size);
            if (result == null) throw new OutOfMemoryException();
            return result;
        }
        /// <summary>
        /// Returns the size of a memory block.
        /// </summary>
        /// <param name="block">Pointer of a memory block.</param>
        /// <returns>Size of a memory block.</returns>
        public static int SizeOf(void* block)
        {
            int result = HeapSize(processHeap, 0, block);
            if (result == -1) throw new InvalidOperationException();
            return result;
        }

        // Heap API flags
        private const int HEAP_ZERO_MEMORY = 0x00000008;
        // Heap API functions
        [DllImport("kernel32")]
        static extern int GetProcessHeap();
        [DllImport("kernel32")]
        static extern void* HeapAlloc(int hHeap, int flags, int size);
        [DllImport("kernel32")]
        static extern bool HeapFree(int hHeap, int flags, void* block);
        [DllImport("kernel32")]
        static extern void* HeapReAlloc(int hHeap, int flags, void* block, int size);
        [DllImport("kernel32")]
        static extern int HeapSize(int hHeap, int flags, void* block);

    }
}
