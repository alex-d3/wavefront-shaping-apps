using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Runtime.InteropServices;

namespace ProtoBuf_test
{
    unsafe class FastArray<T>
    {
        public FastArray(int c)
        {
            Type t = typeof(int*);

            if (t.IsValueType && !t.IsPrimitive)
            {

                FieldInfo[] fi = t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            }
            else
            {

            }
        }
    }
}