using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using MathNet.Numerics;
using System.Diagnostics;

namespace ScatLib
{
    //public enum UnitsOfLength { Arbitrary, Meters, Micrometers, Nanometers }

    unsafe public partial class NearField : IEquatable<NearField>, IDisposable
    {
        /* Field stored in three arrays for each component Ex (0), Ey (1), Ez (2).
         * Arrays of rows.
         * electric_field[Projection][y][x]
         */

        private static byte[] signature = GetSignature();

        private int nodes_x, nodes_y;
        private double step_x, step_y;
        private double min_x, max_x;
        private double min_y, max_y;
        private double wavelength;
        //private NearFieldFlag contains;
        //private NearFieldType field_type;
        // Unsafe memory allocation
        private bool assigned = false;   // Used to replace assignment operator with custom static function
        private Complex* electric_field;
        //private Complex* magnetic_field;
        //private Complex* ref_index_map;
        private int electric_field_size;
        private int electric_field_elem_count;
        private int offset_Ey, offset_Ez;
        // IDisposable
        private bool disposed = false;

        private static readonly int dim = 3;    // Dimension of a vector

        #region Properties
        public int NodesX
        {
            get { return nodes_x; }
        }
        public int NodesY
        {
            get { return nodes_y; }
        }
        public double StepX
        {
            get { return step_x; }
        }
        public double StepY
        {
            get { return step_y; }
        }
        public double MinX
        {
            get { return min_x; }
        }
        public double MaxX
        {
            get { return max_x; }
        }
        public double MinY
        {
            get { return min_y; }
        }
        public double MaxY
        {
            get { return max_y; }
        }
        /// <summary>
        /// Returns wavenumber of light k=2π/λ.
        /// </summary>
        public double Wavenumber
        {
            get { return 2.0 * Math.PI / wavelength; }
        }
        /// <summary>
        /// Returns wavelength λ.
        /// </summary>
        public double Wavelength
        {
            get { return wavelength; }
        }

        /// <summary>
        /// Returns the complex electric field in rows array [projection][y][x].
        /// </summary>
        public Complex[][][] ElectricFieldRowsArray
        {
            get
            {
                Complex[][][] e_field = new Complex[dim][][];

                for (int p = 0; p < dim; p++)
                {
                    e_field[p] = new Complex[nodes_y][];
                    for (int y = 0; y < nodes_y; y++)
                    {
                        e_field[p][y] = new Complex[nodes_x];
                        for (int x = 0; x < nodes_x; x++)
                        {
                            e_field[p][y][x] = electric_field[p * offset_Ey + y * nodes_x + x];
                        }
                    }
                }

                return e_field;
            }
        }

        public Complex this[int projection, int x, int y]
        {
            get
            {
                return electric_field[projection * offset_Ey + y * nodes_x + x];
            }
            set
            {
                electric_field[projection * offset_Ey + y * nodes_x + x] = value;
            }
        }

        /// <summary>
        /// Intensity.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public double this[int x, int y]
        {
            get
            {
//#if DEBUG
//                Debug.WriteLine("y * nodes_x + x             = {0}", y * nodes_x + x);
//                Debug.WriteLine("offset_Ey + y * nodes_x + x = {0}", offset_Ey + y * nodes_x + x);
//                Debug.WriteLine("offset_Ez + y * nodes_x + x = {0}\n", offset_Ez + y * nodes_x + x);
//#endif
                return electric_field[y * nodes_x + x].MagnitudeSquared() +
                    electric_field[offset_Ey + y * nodes_x + x].MagnitudeSquared() +
                    electric_field[offset_Ez + y * nodes_x + x].MagnitudeSquared();
            }
        }

        public double ElectricFieldEnergy
        {
            get
            {
                return DotProduct(this).Real;
            }
        }

        public bool IsAssigned
        {
            get
            {
                return assigned;
            }
            set
            {
                assigned = value;
            }
        }
        #endregion

        #region IEquatable<NearField> and similar methods
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return true;

            NearField nf = obj as NearField;
            if (ReferenceEquals(null, nf))
                return false;
            else
                return Equals(obj);
        }

        public bool Equals(NearField other)
        {
            if (ReferenceEquals(null, other))
                return false;

            if (!PropertiesEquals(other))
                return false;

            for (int i = 0; i < electric_field_elem_count; i++)
            {
                if (electric_field[i] != other.electric_field[i])
                    return false;
            }

            return true;
        }

        public bool PropertiesEquals(NearField other)
        {
            if (other == null)
                return false;
            if (nodes_x != other.nodes_x)
                return false;
            if (nodes_y != other.nodes_y)
                return false;
            if (min_x != other.min_x)
                return false;
            if (min_y != other.min_y)
                return false;
            if (max_x != other.max_x)
                return false;
            if (max_y != other.max_y)
                return false;
            if (step_x != other.step_x)
                return false;
            if (step_y != other.step_y)
                return false;
            if (wavelength != other.wavelength)
                return false;

            return true;
        }
        #endregion

        #region IDisposable
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    // Free other state (managed objects)
                }
                UnmanagedMemory.Free(electric_field);
                electric_field = null;

                disposed = true;
            }
        }
        #endregion

        public override int GetHashCode()
        {
            unchecked
            {
                int hash;
                hash = nodes_x.GetHashCode() * 397;
                hash ^= nodes_y.GetHashCode();
                hash ^= step_x.GetHashCode();
                hash ^= step_y.GetHashCode();
                hash ^= min_x.GetHashCode();
                hash ^= max_x.GetHashCode();
                hash ^= min_y.GetHashCode();
                hash ^= max_y.GetHashCode();
                hash ^= wavelength.GetHashCode();

                for (int i = 0; i < electric_field_elem_count; i++)
                    hash ^= electric_field[i].GetHashCode();

                return hash;
            }
        }

        public NearField Clone()
        {
            return new NearField(this, false);
        }
    }
}
