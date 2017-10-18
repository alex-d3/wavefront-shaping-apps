using System;
using System.Numerics;

namespace ScatLib
{
    unsafe public partial class NearField : IEquatable<NearField>, IDisposable
    {
        public NearField(string path)
        {
            string extension;
            extension = path.Substring(path.Length - 3);
            
            if (extension.Equals("bin", StringComparison.OrdinalIgnoreCase))
            {
                ReadFromFile(path);
            }
            else if (extension.Equals("csv", StringComparison.OrdinalIgnoreCase))
            {
                Import(path);
            }
            else
            {
                throw new FormatException("Unknown file format.");
            }
        }

        /// <summary>
        /// Constructor with zero field.
        /// </summary>
        /// <param name="nodes_x"></param>
        /// <param name="nodes_y"></param>
        /// <param name="step_x"></param>
        /// <param name="step_y"></param>
        /// <param name="min_x"></param>
        /// <param name="min_y"></param>
        /// <param name="wavelength"></param>
        public NearField(int nodes_x, int nodes_y, double step_x, double step_y, double min_x,
            double min_y, double wavelength)
        {
            this.nodes_x = nodes_x;
            this.nodes_y = nodes_y;
            this.step_x = step_x;
            this.step_y = step_y;
            this.min_x = min_x;
            this.max_x = min_x + (nodes_x - 1) * step_x;
            this.min_y = min_y;
            this.max_y = min_y + (nodes_y - 1) * step_y;
            this.wavelength = wavelength;

            // Unmanaged memory
            offset_Ey = nodes_y * nodes_x;
            offset_Ez = 2 * offset_Ey;
            electric_field_elem_count = nodes_x * nodes_y * dim;
            electric_field_size = sizeof(Complex) * electric_field_elem_count;
            electric_field = (Complex*)UnmanagedMemory.Alloc(electric_field_size);
        }

        public NearField(Complex[] e_field, int nodes_x, int nodes_y, double step_x, double step_y, double min_x,
            double min_y, double wavelength) : this(nodes_x, nodes_y, step_x, step_y, min_x, min_y, wavelength)
        {
            for (int i = 0; i < e_field.Length; ++i)
                electric_field[i] = e_field[i];
        }

        public NearField(Complex[][][] e_field, int nodes_x, int nodes_y, double step_x, double step_y, double min_x,
            double min_y, double wavelength) : this(nodes_x, nodes_y, step_x, step_y, min_x, min_y, wavelength)
        {
            int shift = 0;
            for (int p = 0; p < dim; ++p)
            {
                for (int y = 0; y < nodes_y; ++y)
                {
                    for (int x = 0; x < nodes_x; ++x)
                    {
                        electric_field[shift++] = e_field[p][y][x];
                    }
                }
            }
        }

        public NearField(NearField nf, bool empty) :
            this(nf.nodes_x, nf.nodes_y, nf.step_x, nf.step_y, nf.min_x, nf.min_y, nf.wavelength)
        {
            if (!empty)
            {
                UnmanagedMemory.Copy(nf.electric_field, electric_field, electric_field_size);
            }
        }

        private NearField(Complex* e_field, int nodes_x, int nodes_y, double step_x, double step_y, double min_x,
            double min_y, double wavelength)
        {
            this.nodes_x = nodes_x;
            this.nodes_y = nodes_y;
            this.step_x = step_x;
            this.step_y = step_y;
            this.min_x = min_x;
            this.max_x = min_x + (nodes_x - 1) * step_x;
            this.min_y = min_y;
            this.max_y = min_y + (nodes_y - 1) * step_y;
            this.wavelength = wavelength;

            // Unmanaged memory
            electric_field = e_field;
            e_field = null;
            electric_field_elem_count = dim * nodes_x * nodes_y;
            electric_field_size = sizeof(Complex) * electric_field_elem_count;
            offset_Ey = nodes_x * nodes_y;
            offset_Ez = 2 * offset_Ey;
        }

        ~NearField()
        {
            Dispose(false);
        }
    }
}