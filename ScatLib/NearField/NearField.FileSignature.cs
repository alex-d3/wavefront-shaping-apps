using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Diagnostics;

namespace ScatLib
{
    unsafe public partial class NearField : IEquatable<NearField>, IDisposable
    {
        private static byte[] GetSignature()
        {
            byte[] type_id = Encoding.ASCII.GetBytes("NFB"); // Near Field Binary
            int major = 1, minor = 0; // File format version

            byte[] sign = new byte[type_id.Length + 2 * sizeof(int)];

            Buffer.BlockCopy(type_id, 0, sign, 0, type_id.Length);
            Buffer.BlockCopy(BitConverter.GetBytes(major), 0,
                sign, type_id.Length, sizeof(int));
            Buffer.BlockCopy(BitConverter.GetBytes(minor), 0,
                sign, type_id.Length + sizeof(int), sizeof(int));

            return sign;
        }
    }
}
