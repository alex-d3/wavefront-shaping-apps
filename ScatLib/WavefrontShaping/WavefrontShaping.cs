using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Numerics;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;

namespace ScatLib.WavefrontShaping
{
    public static class WavefrontShaping
    {
        /// <summary>
        /// Calculates electric field energy based on decomposition coefficients in certain basis.
        /// </summary>
        /// <param name="roi_mat">Matrix of the ROI.</param>
        /// <param name="coefs">Decomposition coefficients.</param>
        /// <returns></returns>
        private static double FieldEnergy(Matrix<Complex> roi_mat, Vector<Complex> coefs)
        {
            double en_roi = (coefs.Conjugate() * roi_mat * coefs).Real;
            return en_roi;
        }

        /// <summary>
        /// Calculates electric field energy based on decomposition coefficients in certain basis.
        /// </summary>
        /// <param name="coefs">Decomposition coefficients.</param>
        /// <returns></returns>
        private static double FieldEnergy(Vector<Complex> coefs)
        {
            double en = coefs.ConjugateDotProduct(coefs).Real;
            return en;
        }

        /// <summary>
        /// Calculate enhancement (energy ratio) with changing one of decomposition coefficients.
        /// </summary>
        /// <param name="roi_mat_sca"></param>
        /// <param name="coefsScat"></param>
        /// <param name="i"></param>
        /// <param name="c"></param>
        /// <returns></returns>
        private static double Function(Matrix<Complex> roi_mat_sca, ref Vector<Complex> coefsScat, int i, Complex c)
        {
            Complex c_old = coefsScat[i];
            coefsScat[i] = c;
            double en_frac = FieldEnergy(roi_mat_sca, coefsScat) / FieldEnergy(coefsScat);
            coefsScat[i] = c_old;
            return en_frac;
        }

        private static double Function(Matrix<Complex> roi_mat_sca, Vector<Complex> coefsScat)
        {
            double en_frac = FieldEnergy(roi_mat_sca, coefsScat) / FieldEnergy(coefsScat);
            return en_frac;
        }

        private static Vector<double> GradientP(Matrix<Complex> roi_mat, ref Vector<Complex> coefsScat, ref Vector<double> grad)
        {
            double h = 0.001;
            double rho, phi;

            for (int i = 0; i < coefsScat.Count; ++i)
            {
                rho = coefsScat[i].Magnitude;
                phi = coefsScat[i].Phase;

                grad[i] = (Function(roi_mat, ref coefsScat, i,
                    new Complex(rho * Math.Cos(phi + h), rho * Math.Sin(phi + h))) -
                    Function(roi_mat, ref coefsScat, i,
                    new Complex(rho * Math.Cos(phi - h), rho * Math.Sin(phi - h)))) / (2.0 * h);
            }

            return grad;
        }

        private static Vector<double> GradientM(Matrix<Complex> roi_mat, ref Vector<Complex> coefsScat, ref Vector<double> grad)
        {
            double h = 0.001;
            double rho, phi;

            for (int i = 0; i < coefsScat.Count; ++i)
            {
                rho = coefsScat[i].Magnitude;
                phi = coefsScat[i].Phase;

                grad[i] = (Function(roi_mat, ref coefsScat, i,
                    new Complex((rho + h) * Math.Cos(phi), (rho + h) * Math.Sin(phi))) -
                    Function(roi_mat, ref coefsScat, i,
                    new Complex((rho - h) * Math.Cos(phi), (rho - h) * Math.Sin(phi)))) / (2.0 * h);
            }

            return grad;
        }

        // ProgressUpdate delegate (default delegate is {}), stop reason, cancellation token
        public static NearField Focus(Basis bas, Rectangle roi, double t0 = 100.0, int iter_max = 10000,
            double l2norm_stop = 1.0E-6, double a = 0.4, double b = 0.8)
        {
            Matrix<Complex> roi_mat_sca = bas.CalculateDotProductMatrix(roi.X, roi.Y, roi.Width, roi.Height, NearFieldType.Scattered);

            Vector<Complex> C0 = Vector<Complex>.Build.Dense(bas.UsedFields, new Complex(1.0, 0.0));
            Vector<Complex> C1 = Vector<Complex>.Build.Dense(bas.UsedFields);
            Vector<double> grad = Vector<double>.Build.Dense(bas.UsedFields);
            double F1, F0;
            int counter = 0;
            double l2norm;
            double t, rho, phi;

            // Phase optimization

            while (((l2norm = GradientP(roi_mat_sca, ref C0, ref grad).L2Norm()) > l2norm_stop) && (++counter <= iter_max))
            {
                t = t0;
                for (int i = 0; i < grad.Count; ++i)
                {
                    rho = C0[i].Magnitude;
                    phi = C0[i].Phase;

                    C1[i] = new Complex(rho * Math.Cos(phi + t * grad[i]), rho * Math.Sin(phi + t * grad[i]));
                }
                F0 = Function(roi_mat_sca, C0);
                F1 = Function(roi_mat_sca, C1);
                while (F1 < F0 + a * t * grad.DotProduct(grad))
                {
                    t *= b;
                    for (int i = 0; i < grad.Count; ++i)
                    {
                        rho = C0[i].Magnitude;
                        phi = C0[i].Phase;

                        C1[i] = new Complex(rho * Math.Cos(phi + t * grad[i]), rho * Math.Sin(phi + t * grad[i]));
                    }
                    F1 = Function(roi_mat_sca, C1);
                }

                C1.CopyTo(C0);
            }

            // Magnitude optimization

            counter = 0;
            while (((l2norm = GradientM(roi_mat_sca, ref C0, ref grad).L2Norm()) > l2norm_stop) && (++counter <= iter_max))
            {
                t = 100.0;
                for (int i = 0; i < grad.Count; ++i)
                {
                    rho = C0[i].Magnitude;
                    phi = C0[i].Phase;

                    C1[i] = new Complex((rho + t * grad[i]) * Math.Cos(phi), (rho + t * grad[i]) * Math.Sin(phi));
                }
                F0 = Function(roi_mat_sca, C0);
                F1 = Function(roi_mat_sca, C1);
                while (F1 < F0 + a * t * grad.DotProduct(grad))
                {
                    t *= b;
                    for (int i = 0; i < grad.Count; ++i)
                    {
                        rho = C0[i].Magnitude;
                        phi = C0[i].Phase;

                        C1[i] = new Complex((rho + t * grad[i]) * Math.Cos(phi), (rho + t * grad[i]) * Math.Sin(phi));
                    }
                    F1 = Function(roi_mat_sca, C1);
                }

                C1.CopyTo(C0);
            }

            return bas.Compose(C1.AsArray(), NearFieldType.Scattered);
        }
    }

    //public class FocusingResult
    //{
    //    private Basis bas;
    //    private NearField nf = null;
    //    private Vector<Complex> coefs;

    //    public FocusingResult(ref Vector<Complex> decomp_coef, NearFieldType type, Basis basis)
    //    {
    //        bas = basis;
    //        coefs = decomp_coef;
    //    }

    //    public FocusingResult(Vector<Complex> decomp_coef, NearFieldType type, Basis basis)
    //    {
    //        coefs = Vector<Complex>.Build.DenseOfVector(decomp_coef);
    //    }

    //    public Vector<Complex> DecompositionCoefficients
    //    {
    //        get
    //        {
    //            return coefs.Clone();
    //        }
    //    }

    //    public NearField Field
    //    {
    //        get
    //        {
    //            if (nf == null)
    //            {
                    
    //            }

    //            return nf;
    //        }
    //    }
    //}
}
