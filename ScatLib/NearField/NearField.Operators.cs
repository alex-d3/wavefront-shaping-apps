using System;
using System.Numerics;

namespace ScatLib
{
    unsafe public partial class NearField : IEquatable<NearField>, IDisposable
    {
        public static void op_Assign(ref NearField target, NearField source)
        {
            if (target == null)
            {
                if (!source.assigned)
                    target = source;
                else
                    target = source.Clone();
            }
            else
            {
                target.Dispose();

                if (!source.assigned)
                    target = source;
                else
                    target = source.Clone();
            }
            target.assigned = true;
        }

        public static NearField operator +(NearField nf1, NearField nf2)
        {
            if (!nf1.PropertiesEquals(nf2))
                throw new FormatException("Fields have different properties.");

            Complex* e_field = (Complex*)UnmanagedMemory.Alloc(nf1.electric_field_size);

            for (int index = 0; index < nf1.electric_field_elem_count; ++index)
                e_field[index] = nf1.electric_field[index] + nf2.electric_field[index];

            if (!nf1.assigned)
                nf1.Dispose();
            if (!nf2.assigned)
                nf2.Dispose();

            return new NearField(e_field, nf1.nodes_x, nf1.nodes_y, nf1.step_x, nf1.step_y, nf1.min_x,
                nf1.min_y, nf1.wavelength);
        }

        public static NearField operator -(NearField nf1, NearField nf2)
        {
            if (!nf1.PropertiesEquals(nf2))
                throw new FormatException("Fields have different properties.");

            Complex* e_field = (Complex*)UnmanagedMemory.Alloc(nf1.electric_field_size);

            for (int index = 0; index < nf1.electric_field_elem_count; ++index)
                e_field[index] = nf1.electric_field[index] - nf2.electric_field[index];
            
            if (!nf1.assigned)
                nf1.Dispose();
            if (!nf2.assigned)
                nf2.Dispose();

            return new NearField(e_field, nf1.nodes_x, nf1.nodes_y, nf1.step_x, nf1.step_y, nf1.min_x,
                nf1.min_y, nf1.wavelength);
        }

        public static NearField operator *(NearField nf, Complex c)
        {
            Complex* e_field = (Complex*)UnmanagedMemory.Alloc(nf.electric_field_size);

            for (int index = 0; index < nf.electric_field_elem_count; ++index)
                e_field[index] = nf.electric_field[index] * c;

            if (!nf.assigned)
                nf.Dispose();

            return new NearField(e_field, nf.nodes_x, nf.nodes_y, nf.step_x, nf.step_y, nf.min_x,
                nf.min_y, nf.wavelength);
        }

        public static NearField operator /(NearField nf, Complex c)
        {
            Complex* e_field = (Complex*)UnmanagedMemory.Alloc(nf.electric_field_size);

            for (int index = 0; index < nf.electric_field_elem_count; ++index)
                e_field[index] = nf.electric_field[index] / c;

            if (!nf.assigned)
                nf.Dispose();

            return new NearField(e_field, nf.nodes_x, nf.nodes_y, nf.step_x, nf.step_y, nf.min_x,
                nf.min_y, nf.wavelength);
        }

        public static NearField operator *(Complex c, NearField nf)
        {
            return nf * c;
        }

        public static NearField operator *(NearField nf, double d)
        {
            return nf * new Complex(d, 0.0);
        }
        public static NearField operator *(double d, NearField nf)
        {
            return nf * new Complex(d, 0.0);
        }

        public static NearField operator /(Complex c, NearField nf)
        {
            return nf / c;
        }
    }
}