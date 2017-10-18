using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Factorization;
using System.IO;

namespace ScatLib
{
    public sealed partial class Basis : IDisposable
    {
        public enum CoefficientType
        {
            Incident,
            Scattered
        };

        private bool disposed = false;
        private static byte[] signature = GetSignature();

        private string[] x_basisFiles, y_basisFiles;
        private NearField[] x_basisNF, y_basisNF;
        private Complex[] wElems;               // Singular numbers
        private Complex[][] conv_coefs_inc;     // Conversion coefficients (incident, E(inc) -> E(inc, bas))
        private Complex[][] conv_coefs_scat;    // Conversion coefficients (scattered, E(sca) -> E(sca, bas))
        //private Matrix<Complex> conv_coefs_inc_mat;
        //private Matrix<Complex> conv_coefs_sca_mat;
        //private Matrix<Complex> singularNumbers;

        private int basis_size;
        private int used_fields;


        #region IDisposable
        ~Basis()
        {
            Dispose(false);
        }

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
                    for (int i = 0; i < x_basisNF.Length; ++i)
                    {
                        x_basisNF[i].Dispose();
                        y_basisNF[i].Dispose();
                    }
                }

                disposed = true;
            }
        }
        #endregion

        

        //private void SaveFields()
        //{

        //}

        private void BuildBasis(NearField[] x_fields, NearField[] y_fields)
        {
            Matrix<Complex> dotProdX, dotProdY;

            Svd<Complex> f_x, f_r;
            Matrix<Complex> Wnorm_x, Wnorm_r, R;
            Matrix<Complex> Wnorm_x_Left, Wnorm_x_Right;
            Matrix<Complex> psiMat, fMat;

            if (x_fields.Length != y_fields.Length)
                throw new Exception("Input arrays have different sizes.");

            basis_size = x_fields.Length;

            dotProdX = NearField.CalculateDotProductMatrix(x_fields);
            dotProdY = NearField.CalculateDotProductMatrix(y_fields);

            /* Calculate PSI and F factors */
            f_x = dotProdX.Svd();
            Wnorm_x = f_x.W.Clone();
            // Normalization
            for (int i = 0; i < Wnorm_x.RowCount; i++)
            {
                Wnorm_x[i, i] = Wnorm_x[i, i].SquareRoot();
            }
            // Check pseudo-inverse
            Wnorm_x = Wnorm_x.PseudoInverse();

            Wnorm_x_Left = Wnorm_x;
            Wnorm_x_Right = Wnorm_x;

            R = Wnorm_x_Left * f_x.VT * dotProdY * f_x.U * Wnorm_x_Right;
            f_r = R.Svd();

            Wnorm_r = f_r.W.Clone();
            for (int i = 0; i < Wnorm_r.RowCount; i++)
            {
                Wnorm_r[i, i] = Wnorm_r[i, i].SquareRoot();
            }
            Wnorm_r = Wnorm_r.Inverse();

            psiMat = f_x.U * Wnorm_x_Right * f_r.U;
            //fMat = f_x.U * Wnorm_x * f_r.U * Wnorm_r;
            fMat = psiMat * Wnorm_r;

            /* Save conversion coefficients */
            conv_coefs_inc = psiMat.ToRowArrays();
            conv_coefs_scat = fMat.ToRowArrays();

            //conv_coefs_inc_mat = psiMat;    // Copy of the reference!
            //conv_coefs_sca_mat = fMat;      // Copy of the reference!

            /* Calculate PSI and F fields */
            x_basisFiles = new string[basis_size];
            y_basisFiles = new string[basis_size];
            x_basisNF = new NearField[basis_size];
            y_basisNF = new NearField[basis_size];

            int nodes_x, nodes_y;
            double step_x, step_y, min_x, min_y, max_x, max_y, wavelength;
            nodes_x = x_fields[0].NodesX;
            nodes_y = x_fields[0].NodesY;
            step_x = x_fields[0].StepX;
            step_y = x_fields[0].StepY;
            min_x = x_fields[0].MinX;
            max_x = x_fields[0].MaxX;
            min_y = x_fields[0].MinY;
            max_y = x_fields[0].MaxY;
            wavelength = x_fields[0].Wavelength;

            for (int i = 0; i < basis_size; i++)
            {
                x_basisFiles[i] = string.Format("in_basis_{0}.bin", i.ToString("D3"));
                y_basisFiles[i] = string.Format("out_basis_{0}.bin", i.ToString("D3"));

                NearField.op_Assign(ref x_basisNF[i], new NearField(nodes_x, nodes_y, step_x, step_y, min_x,
                    min_y, wavelength));
                NearField.op_Assign(ref y_basisNF[i], new NearField(nodes_x, nodes_y, step_x, step_y, min_x,
                    min_y, wavelength));
            }

            if (Environment.ProcessorCount > 1) // Multithread
            {
                int i = 0;
                int pCount = Environment.ProcessorCount;

                Task[] tasks = new Task[pCount];

                for (int p = 0; p < pCount && i < basis_size; ++p, ++i)
                {
                    int index = i;
                    tasks[p] = new Task(new Action(() =>
                    {
                        while (index < basis_size)
                        {
                            for (int j = 0; j < psiMat.RowCount; ++j)
                            {
                                NearField.op_Assign(ref x_basisNF[index], x_basisNF[index] + x_fields[j] * psiMat[j, index]);
                                NearField.op_Assign(ref y_basisNF[index], y_basisNF[index] + y_fields[j] * fMat[j, index]);
                            }

                            index = System.Threading.Interlocked.Increment(ref i);
                        }
                    }));
                }

                --i; // Чтоб не пропустить один файл под номером i = Environment.ProcessorCount

                for (int p = 0; p < pCount; p++)
                    tasks[p].Start();

                Task.WaitAll(tasks);
            }
            else   // Single thread
            {
                for (int i = 0; i < basis_size; i++)
                {
                    for (int j = 0; j < psiMat.RowCount; j++)
                    {
                        NearField.op_Assign(ref x_basisNF[i], x_basisNF[i] + x_fields[j] * psiMat[j, i]);
                        NearField.op_Assign(ref y_basisNF[i], y_basisNF[i] + y_fields[j] * fMat[j, i]);
                    }
                }
            }

            wElems = f_r.W.Diagonal().ToArray();
            for (int i = 0; i < wElems.Length; i++)
            {
                wElems[i] = wElems[i].SquareRoot();
            }
        }

        // Вроде переделал
        private void BuildBasis(NearField[] x_fields, NearField[] y_fields, int f_count)
        {
            Matrix<Complex> dotProdX, dotProdY;

            Svd<Complex> f_x, f_r;
            Matrix<Complex> Wnorm_x, Wnorm_r, R;
            Matrix<Complex> Wnorm_x_Left, Wnorm_x_Right;
            Matrix<Complex> psiMat, fMat;

            //NearField[] x_basisFields, y_basisFields;

            if (x_fields.Length != y_fields.Length)
                throw new Exception("Input arrays have different sizes.");

            dotProdX = NearField.CalculateDotProductMatrix(x_fields);
            dotProdY = NearField.CalculateDotProductMatrix(y_fields);

            /* Calculate PSI and F factors */
            f_x = dotProdX.Svd();
            Wnorm_x = f_x.W.Clone();
            // Normalization
            for (int i = 0; i < Wnorm_x.RowCount; i++)
            {
                Wnorm_x[i, i] = Wnorm_x[i, i].SquareRoot();
            }
            // Check pseudo-inverse
            Wnorm_x = Wnorm_x.PseudoInverse();

            /* Modification allows basis reduction */
            if (Wnorm_x.RowCount == f_count && Wnorm_x.ColumnCount == f_count)
            {
                Wnorm_x_Left = Wnorm_x;
                Wnorm_x_Right = Wnorm_x;
            }
            else
            {
                Wnorm_x_Left = Wnorm_x.SubMatrix(0, f_count, 0, Wnorm_x.ColumnCount);
                Wnorm_x_Right = Wnorm_x.SubMatrix(0, Wnorm_x.RowCount, 0, f_count);
            }

            R = Wnorm_x_Left * f_x.VT * dotProdY * f_x.U * Wnorm_x_Right;
            f_r = R.Svd();

            Wnorm_r = f_r.W.Clone();
            for (int i = 0; i < Wnorm_r.RowCount; i++)
            {
                Wnorm_r[i, i] = Wnorm_r[i, i].SquareRoot();
            }
            Wnorm_r = Wnorm_r.Inverse();

            psiMat = f_x.U * Wnorm_x_Right * f_r.U;
            //fMat = f_x.U * Wnorm_x * f_r.U * Wnorm_r;
            fMat = psiMat * Wnorm_r;

            /* Save conversion coefficients */
            conv_coefs_inc = psiMat.ToRowArrays();
            conv_coefs_scat = fMat.ToRowArrays();

            /* Calculate PSI and F fields */
            x_basisFiles = new string[f_count];
            y_basisFiles = new string[f_count];
            x_basisNF = new NearField[f_count];
            y_basisNF = new NearField[f_count];

            int nodes_x, nodes_y;
            double step_x, step_y, min_x, min_y, max_x, max_y, wavelength;
            nodes_x = x_fields[0].NodesX;
            nodes_y = x_fields[0].NodesY;
            step_x = x_fields[0].StepX;
            step_y = x_fields[0].StepY;
            min_x = x_fields[0].MinX;
            max_x = x_fields[0].MaxX;
            min_y = x_fields[0].MinY;
            max_y = x_fields[0].MaxY;
            wavelength = x_fields[0].Wavelength;

            for (int i = 0; i < f_count; i++)
            {
                x_basisFiles[i] = string.Format("in_basis_{0}.bin", i.ToString("D3"));
                y_basisFiles[i] = string.Format("out_basis_{0}.bin", i.ToString("D3"));

                NearField.op_Assign(ref x_basisNF[i], new NearField(nodes_x, nodes_y, step_x, step_y, min_x,
                    min_y, wavelength));
                NearField.op_Assign(ref y_basisNF[i], new NearField(nodes_x, nodes_y, step_x, step_y, min_x,
                    min_y, wavelength));
            }

            if (Environment.ProcessorCount > 1)
            {
                int i = 0;
                int pCount = Environment.ProcessorCount;
                
                Task[] tasks = new Task[pCount];

                for (int p = 0; p < pCount && i < f_count; p++, i++)
                {
                    int index = i;
                    tasks[p] = new Task(new Action(() =>
                    {
                        while (index < f_count)
                        {
                            for (int j = 0; j < psiMat.RowCount; j++)
                            {
                                NearField.op_Assign(ref x_basisNF[index], x_basisNF[index] + x_fields[j] * psiMat[j, index]);
                                NearField.op_Assign(ref y_basisNF[index], y_basisNF[index] + y_fields[j] * fMat[j, index]);
                            }
#if DEBUG
                            System.Diagnostics.Debug.WriteLine(index);
#endif

                            index = System.Threading.Interlocked.Increment(ref i);
                        }
                    }));
                }

                --i; // Чтоб не пропустить один файл под номером i = Environment.ProcessorCount

                for (int p = 0; p < pCount; p++)
                    tasks[p].Start();

                Task.WaitAll(tasks);
            }
            else
            {
                for (int i = 0; i < f_count; i++)
                {
                    for (int j = 0; j < psiMat.RowCount; j++)
                    {
                        NearField.op_Assign(ref x_basisNF[i], x_basisNF[i] + x_fields[j] * psiMat[j, i]);
                        NearField.op_Assign(ref y_basisNF[i], y_basisNF[i] + y_fields[j] * fMat[j, i]);
                    }
                }
            }

            wElems = f_r.W.Diagonal().ToArray();
            for (int i = 0; i < wElems.Length; i++)
            {
                wElems[i] = wElems[i].SquareRoot();
            }
        }

        private static byte[] GetSignature()
        {
            byte[] type_id = Encoding.ASCII.GetBytes("BSB"); // Basis Binary
            int major = 1, minor = 0; // File format version

            byte[] sign = new byte[type_id.Length + 2 * sizeof(int)];

            Buffer.BlockCopy(type_id, 0, sign, 0, type_id.Length);
            Buffer.BlockCopy(BitConverter.GetBytes(major), 0,
                sign, type_id.Length, sizeof(int));
            Buffer.BlockCopy(BitConverter.GetBytes(minor), 0,
                sign, type_id.Length + sizeof(int), sizeof(int));

            return sign;
        }

        public Complex[] GetSingularNumbers()
        {
            Complex[] array = new Complex[wElems.Length];
            wElems.CopyTo(array, 0);
            return array;
        }

        public Complex[][] GetConversionCoefficients(CoefficientType type)
        {
            switch (type)
            {
                case CoefficientType.Incident:
                    return conv_coefs_inc;
                case CoefficientType.Scattered:
                    return conv_coefs_scat;
            }

            return null;
        }

        public int UsedFields
        {
            get
            {
                return used_fields;
            }
            set
            {
                if (value > 0 && value <= basis_size)
                {
                    used_fields = value;
                }
                else
                {
                    used_fields = basis_size;
                }
            }
        }

   
    }
}
