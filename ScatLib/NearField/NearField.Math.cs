using System;
using System.Runtime.CompilerServices;
using System.Numerics;
using System.Threading.Tasks;
using System.Drawing;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;
using System.Collections;
using System.Collections.Generic;

namespace ScatLib
{
    unsafe public partial class NearField : IEquatable<NearField>, IDisposable
    {
        public NearField Conjugate()
        {
            NearField nf = new NearField(nodes_x, nodes_y, step_x, step_y, min_x, min_y, wavelength);

            for (int index = 0; index < electric_field_size / sizeof(Complex); ++index)
            {
                nf.electric_field[index] = electric_field[index].Conjugate();
            }
            return nf;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Complex ConjugateDotProduct(NearField other, int x, int y)
        {
            Complex m1 = electric_field[y * nodes_x + x].Conjugate();
            Complex m1o = other.electric_field[y * nodes_x + x];
            Complex m2 = electric_field[offset_Ey + y * nodes_x + x].Conjugate();
            Complex m2o = other.electric_field[offset_Ey + y * nodes_x + x];
            Complex m3 = electric_field[offset_Ez + y * nodes_x + x].Conjugate();
            Complex m3o = other.electric_field[offset_Ez + y * nodes_x + x];
            Complex res = m1 * m1o + m2 * m2o + m3 * m3o;
            return res;

            //return electric_field[y * nodes_x + x].Conjugate() * other.electric_field[y * nodes_x + x] +
            //    electric_field[offset_Ey + y * nodes_x + x].Conjugate() * other.electric_field[offset_Ey + y * nodes_x + x] +
            //    electric_field[offset_Ez + y * nodes_x + x].Conjugate() * other.electric_field[offset_Ez + y * nodes_x + x];
        }

        /* this -- first multiplicand (m -- row), other -- second multiplicand (n -- column).
         * c_mn = int (E*_m E_n) dr = (E_n, E_m) */
        public Complex DotProduct(NearField other)
        {
            if (!PropertiesEquals(other))
            {
                throw new FormatException("Fields have different properties.");
            }

            Complex result;

            Complex[] intx = new Complex[nodes_y];
            Complex temp;

            /* Integration over X */
            for (int y = 0; y < nodes_y; ++y)
            {
                temp = Complex.Zero;

                for (int x = 1; x < nodes_x - 1; x += 2)
                {
                    temp += ConjugateDotProduct(other, x - 1, y) + 4.0 * ConjugateDotProduct(other, x, y) +
                        ConjugateDotProduct(other, x + 1, y);
                }

                intx[y] = temp * step_x / 3.0;
            }
            result = Complex.Zero;

            /* Integration over Y */

            for (int y = 1; y < nodes_y - 1; y += 2)
            {
                result += intx[y - 1] + 4.0 * intx[y] + intx[y + 1];
            }

            result *= step_y / 3.0;

            return result;
        }

        /// <summary>
        /// Partial dot product.
        /// </summary>
        /// <param name="other"></param>
        /// <param name="node_x_start"></param>
        /// <param name="node_y_start"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public Complex DotProduct(NearField other, int node_x_start, int node_y_start, int width, int height)
        {
            if (!PropertiesEquals(other))
            {
                throw new Exception("Fields have different properties.");
            }
            if (width < 0 || height < 0)
            {
                throw new Exception("Width or height has negative value.");
            }
            if ((node_x_start + width > nodes_x) || (node_y_start + height > nodes_y))
            {
                throw new Exception("Chosen region goes beyond the borders of the current field.");
            }

            Complex result;

            Complex[] intx = new Complex[nodes_y];
            Complex temp;

            /* Integration over X */
            for (int y = node_y_start; y < node_y_start + height; ++y)
            {
                temp = Complex.Zero;

                for (int x = node_x_start + 1; x < node_x_start + width - 1; x += 2)
                {
                    temp += ConjugateDotProduct(other, x - 1, y) + 4.0 * ConjugateDotProduct(other, x, y) +
                        ConjugateDotProduct(other, x + 1, y);
                }

                intx[y] = temp * step_x / 3.0;
            }
            result = Complex.Zero;

            /* Integration over Y */

            for (int y = node_y_start + 1; y < node_y_start + height - 1; y += 2)
            {
                result += intx[y - 1] + 4.0 * intx[y] + intx[y + 1];
            }

            result *= step_y / 3.0;

            return result;
        }

        public double GetElectricFieldEnergy()
        {
            return DotProduct(this).Real;
        }

        public double GetElectricFieldEnergy(int node_x_start, int node_y_start, int width, int height)
        {
            return DotProduct(this, node_x_start, node_y_start, width, height).Real;
        }

        // Analysis part

        public static double CalculateEnergyDifferenceRatio(NearField nf_ref, NearField nf_rec)
        {
            NearField diff = null;
            NearField.op_Assign(ref diff, nf_ref - nf_rec);

            double numerator = diff.DotProduct(diff).Real;
            double denominator = nf_ref.DotProduct(nf_ref).Real;

            return numerator / denominator;
        }

        //public void CalculateFWHM(out double x_min, out double x_max, out double y_min, out double y_max,
        //    out double area)
        //{
        //    int xmin, xmax, ymin, ymax;

        //    double int_hm = GetMaxIntensity(out xmin, out ymin) / 2.0;
        //    int points = 0;
        //    xmax = xmin;
        //    ymax = ymin;

        //    CountPixelsFWHM(xmin, ymin, ref int_hm, ref xmin, ref xmax, ref ymin, ref ymax, ref points);

        //    x_min = xmin * step_x + min_x;
        //    x_max = xmax * step_x + min_x;
        //    y_min = ymin * step_y + min_y;
        //    y_max = ymax * step_y + min_y;

        //    area = points * step_x * step_y;
        //}

        // Наверное, хрень. Рекурсия не тру?
        //private void CountPixelsFWHM(int x, int y, ref double int_hm,
        //    ref int x_min, ref int x_max,
        //    ref int y_min, ref int y_max, ref int pixels)
        //{

        //    if (this[x - 1, y] > int_hm)
        //        CountPixelsFWHM(x - 1, y, ref int_hm, ref x_min, ref x_max, ref y_min, ref y_max, ref pixels);
        //    if (this[x, y - 1] > int_hm)
        //        CountPixelsFWHM(x, y - 1, ref int_hm, ref x_min, ref x_max, ref y_min, ref y_max, ref pixels);
        //    if (this[x + 1, y] > int_hm)
        //        CountPixelsFWHM(x + 1, y, ref int_hm, ref x_min, ref x_max, ref y_min, ref y_max, ref pixels);
        //    if (this[x, y + 1] > int_hm)
        //        CountPixelsFWHM(x, y + 1, ref int_hm, ref x_min, ref x_max, ref y_min, ref y_max, ref pixels);

        //    if (x < x_min)
        //        x_min = x;
        //    if (x > x_max)
        //        x_max = x;
        //    if (y < y_min)
        //        y_min = y;
        //    if (y > y_max)
        //        y_max = y;

        //    ++pixels;
        //}

        public FWHM CalculateFWHM()
        {
            int x, y;
            double int_hm = GetMaxIntensity(out x, out y) / 2.0;
            return CalculateFWHM(x, y, int_hm);
        }

        public FWHM CalculateFWHM(int x, int y, double int_hm)
        {
            int xmin = x, xmax = x, ymin = y, ymax = y;
            int points = 0;
            Queue<Point> nodes = new Queue<Point>(32);
            BitArray visited = new BitArray(nodes_x * nodes_y);

            if (this[x, y] >= int_hm)
                nodes.Enqueue(new Point(x, y));

            Point w, e;
            while (nodes.Count > 0)
            {
                w = nodes.Dequeue();
                e = w;

                while (this[w.X, w.Y] >= int_hm && !visited[w.Y * nodes_x + w.X])
                {
                    if (w.X - 1 >= 0)
                        w.X--;
                    else
                        break;
                }
                while (this[e.X, e.Y] >= int_hm && !visited[e.Y * nodes_x + e.X])
                {
                    if (e.X + 1 < nodes_x)
                        e.X++;
                    else
                        break;
                }

                while (w.X <= e.X && !visited[w.Y * nodes_x + w.X])
                {
                    points++;
                    visited[w.Y * nodes_x + w.X] = true;

                    if (w.X < xmin)
                        xmin = w.X;
                    if (w.X > xmax)
                        xmax = w.X;
                    if (w.Y < ymin)
                        ymin = w.Y;
                    if (w.Y > ymax)
                        ymax = w.Y;

                    if (w.Y + 1 < nodes_y && this[w.X, w.Y + 1] >= int_hm && !visited[(w.Y + 1) * nodes_x + w.X])
                        nodes.Enqueue(new Point(w.X, w.Y + 1));
                    if (w.Y - 1 < nodes_y && this[w.X, w.Y - 1] >= int_hm && !visited[(w.Y - 1) * nodes_x + w.X])
                        nodes.Enqueue(new Point(w.X, w.Y - 1));

                    w.X++;
                }
            }

            FWHM res = new FWHM(
                new PointD(x * step_x + min_x, y * step_y + min_y),
                new RectangleF((float)(xmin * step_x + min_x), (float)(ymin * step_y + min_y),
                    (float)((xmax - xmin) * step_x), (float)((ymax - ymin) * step_y)),
                points * step_x * step_y, int_hm);

            return res;
        }

        public double GetMaxIntensity()
        {
            double int_max = 0.0;
            double val_x, val_y, val_z, val;

            for (int y = 0; y < nodes_y; ++y)
            {
                for (int x = 0; x < nodes_x; ++x)
                {
                    val_x = electric_field[y * nodes_x + x].MagnitudeSquared();
                    val_y = electric_field[offset_Ey + y * nodes_x + x].MagnitudeSquared();
                    val_z = electric_field[offset_Ez + y * nodes_x + x].MagnitudeSquared();
                    val = val_x + val_y + val_z;

                    if (val > int_max)
                        int_max = val;
                }
            }

            return int_max;
        }

        public double GetMaxIntensity(out int peak_x, out int peak_y)
        {
            double int_max = 0.0;
            double val_x, val_y, val_z, val;
            peak_x = 0;
            peak_y = 0;

            for (int y = 0; y < nodes_y; ++y)
            {
                for (int x = 0; x < nodes_x; ++x)
                {
                    val_x = electric_field[y * nodes_x + x].MagnitudeSquared();
                    val_y = electric_field[offset_Ey + y * nodes_x + x].MagnitudeSquared();
                    val_z = electric_field[offset_Ez + y * nodes_x + x].MagnitudeSquared();
                    val = val_x + val_y + val_z;

                    if (val > int_max)
                    {
                        int_max = val;
                        peak_x = x;
                        peak_y = y;
                    }
                }
            }

            return int_max;
        }

        public static Complex[][] CalculateDotProductArray(NearField[] fields)
        {
            Complex[][] dotprodmatrix = new Complex[fields.Length][];
            for (int i = 0; i < fields.Length; ++i)
                dotprodmatrix[i] = new Complex[fields.Length];

            if (Environment.ProcessorCount > 1)
            {
                int i = 0;
                int pCount = Environment.ProcessorCount;
                //pCount = 1;
                Task[] tasks = new Task[pCount];

                for (int p = 0; p < pCount && i < fields.Length; ++p, ++i)
                {
                    int index = i;

                    tasks[p] = new Task(new Action(() => {
                        while (index < fields.Length)
                        {
                            for (int j = 0; j <= index; ++j)
                            {
                                dotprodmatrix[index][j] = fields[index].DotProduct(fields[j]);
                                if (index != j)
                                    dotprodmatrix[j][index] = dotprodmatrix[index][j].Conjugate();
                            }

                            index = System.Threading.Interlocked.Increment(ref i);
                        }
                    }));
                }

                --i; // Зачем?

                for (int p = 0; p < pCount; p++)
                    tasks[p].Start();

                Task.WaitAll(tasks);
            }
            else
            {
                for (int i = 0; i < fields.Length; i++)
                {
                    for (int j = 0; j <= i; j++)
                    {
                        dotprodmatrix[i][j] = fields[i].DotProduct(fields[j]);
                        if (i != j)
                            dotprodmatrix[j][i] = dotprodmatrix[i][j].Conjugate();
                    }
                }
            }

            return dotprodmatrix;
        }

        public static Matrix<Complex> CalculateDotProductMatrix(NearField[] fields)
        {
            return Matrix<Complex>.Build.DenseOfRowArrays(CalculateDotProductArray(fields));
        }

        //public static NearField Focus(Basis bas, Rectangle roi)
        //{
        //    // MathNet
        //    double t = 1.0, a = 0.4, b = 0.8;

        //    Matrix<Complex> roi_mat_sca = bas.CalculateDotProductMatrix(roi.X, roi.Y, roi.Width, roi.Height, NearFieldType.Scattered);

        //    Vector<Complex> C0 = Vector<Complex>.Build.Dense(bas.UsedFields, new Complex(1.0, 0.0));
        //    Vector<Complex> C1 = Vector<Complex>.Build.Dense(bas.UsedFields);
        //    Vector<double> grad = Vector<double>.Build.Dense(bas.UsedFields);
        //    double F1, F0;

        //    // Phase optimization

        //    int counter = 0;
        //    double l2norm = 0.0;
        //    double rho, phi;
        //    while (((l2norm = GradientP(roi_mat_sca, ref C0, ref grad).L2Norm()) > 1.0E-6) && (++counter <= 10000))
        //    {
        //        t = 100.0;
        //        for (int i = 0; i < grad.Count; ++i)
        //        {
        //            rho = C0[i].Magnitude;
        //            phi = C0[i].Phase;

        //            C1[i] = new Complex(rho * Math.Cos(phi + t * grad[i]), rho * Math.Sin(phi + t * grad[i]));
        //        }
        //        F0 = Function(roi_mat_sca, C0);
        //        F1 = Function(roi_mat_sca, C1);
        //        while (F1 < F0 + a * t * grad.DotProduct(grad))
        //        {
        //            t *= b;
        //            for (int i = 0; i < grad.Count; ++i)
        //            {
        //                rho = C0[i].Magnitude;
        //                phi = C0[i].Phase;

        //                C1[i] = new Complex(rho * Math.Cos(phi + t * grad[i]), rho * Math.Sin(phi + t * grad[i]));
        //            }
        //            F1 = Function(roi_mat_sca, C1);
        //        }

        //        C1.CopyTo(C0);
        //    }

        //    // Magnitude optimization

        //    counter = 0;
        //    while (((l2norm = GradientM(roi_mat_sca, ref C0, ref grad).L2Norm()) > 1.0E-6) && (++counter <= 10000))
        //    {
        //        t = 100.0;
        //        for (int i = 0; i < grad.Count; ++i)
        //        {
        //            rho = C0[i].Magnitude;
        //            phi = C0[i].Phase;

        //            C1[i] = new Complex((rho + t * grad[i]) * Math.Cos(phi), (rho + t * grad[i]) * Math.Sin(phi));
        //        }
        //        F0 = Function(roi_mat_sca, C0);
        //        F1 = Function(roi_mat_sca, C1);
        //        while (F1 < F0 + a * t * grad.DotProduct(grad))
        //        {
        //            t *= b;
        //            for (int i = 0; i < grad.Count; ++i)
        //            {
        //                rho = C0[i].Magnitude;
        //                phi = C0[i].Phase;

        //                C1[i] = new Complex((rho + t * grad[i]) * Math.Cos(phi), (rho + t * grad[i]) * Math.Sin(phi));
        //            }
        //            F1 = Function(roi_mat_sca, C1);
        //        }

        //        C1.CopyTo(C0);
        //    }

        //    return bas.Compose(C1.AsArray(), NearFieldType.Scattered);
        //}

        //private static double EnergyFraction(Basis bas, Rectangle roi, Complex[] coefsScat)
        //{
        //    NearField nf = null;
        //    NearField.op_Assign(ref nf, bas.Compose(coefsScat, NearFieldType.Scattered));
        //    double en_frac = nf.DotProduct(nf, roi.X, roi.Y, roi.Width, roi.Height).Real /
        //        nf.DotProduct(nf).Real;
        //    nf.Dispose();

        //    return en_frac;
        //}

        //private static double Function(Basis bas, Rectangle roi, ref Complex[] coefsScat, int i, Complex c)
        //{
        //    Complex c_old = coefsScat[i];
        //    coefsScat[i] = c;

        //    NearField nf = null;
        //    NearField.op_Assign(ref nf, bas.Compose(coefsScat, NearFieldType.Scattered));
        //    double en_frac = nf.DotProduct(nf, roi.X, roi.Y, roi.Width, roi.Height).Real /
        //        nf.DotProduct(nf).Real;
        //    nf.Dispose();
        //    coefsScat[i] = c_old;

        //    return en_frac;
        //}

        //private static void GradientM(ref Complex[] grad, Basis bas, Rectangle roi, Complex[] coefsScat)
        //{
        //    double h = 0.001;
        //    double rho, phi;

        //    for (int i = 0; i < coefsScat.Length; ++i)
        //    {
        //        rho = coefsScat[i].Magnitude;
        //        phi = coefsScat[i].Phase;

        //        grad[i] = (Function(bas, roi, ref coefsScat, i,
        //            new Complex((rho + h) * Math.Cos(phi), (rho + h) * Math.Sin(phi))) -
        //            Function(bas, roi, ref coefsScat, i,
        //            new Complex((rho - h) * Math.Cos(phi), (rho - h) * Math.Sin(phi)))) / (2.0 * h);
        //    }
        //}

        //private static void GradientP(ref Complex[] grad, Basis bas, Rectangle roi, ref Complex[] coefsScat)
        //{
        //    double h = 0.001;
        //    double rho, phi;

        //    for (int i = 0; i < coefsScat.Length; ++i)
        //    {
        //        rho = coefsScat[i].Magnitude;
        //        phi = coefsScat[i].Phase;

        //        grad[i] = (Function(bas, roi, ref coefsScat, i,
        //            new Complex(rho * Math.Cos(phi + h), rho * Math.Sin(phi + h))) -
        //            Function(bas, roi, ref coefsScat, i,
        //            new Complex(rho * Math.Cos(phi - h), rho * Math.Sin(phi - h)))) / (2.0 * h);
        //    }
        //}

        //// MathNet version

        //private static double FieldEnergy(Matrix<Complex> roi_mat, Vector<Complex> coefs)
        //{
        //    double en_roi = (coefs.Conjugate() * roi_mat * coefs).Real;

        //    return en_roi;
        //}

        //private static double FieldEnergy(Vector<Complex> coefs)
        //{
        //    double en = coefs.ConjugateDotProduct(coefs).Real;

        //    return en;
        //}

        //private static double Function(Matrix<Complex> roi_mat_sca, ref Vector<Complex> coefsScat, int i, Complex c)
        //{
        //    Complex c_old = coefsScat[i];
        //    coefsScat[i] = c;

        //    double en_frac = FieldEnergy(roi_mat_sca, coefsScat) / FieldEnergy(coefsScat);

        //    coefsScat[i] = c_old;

        //    return en_frac;
        //}

        //private static double Function(Matrix<Complex> roi_mat_sca, Vector<Complex> coefsScat)
        //{
        //    double en_frac = FieldEnergy(roi_mat_sca, coefsScat) / FieldEnergy(coefsScat);

        //    return en_frac;
        //}

        //private static Vector<double> GradientP(Matrix<Complex> roi_mat, ref Vector<Complex> coefsScat, ref Vector<double> grad)
        //{
        //    double h = 0.001;
        //    double rho, phi;

        //    for (int i = 0; i < coefsScat.Count; ++i)
        //    {
        //        rho = coefsScat[i].Magnitude;
        //        phi = coefsScat[i].Phase;
                
        //        grad[i] = (Function(roi_mat, ref coefsScat, i,
        //            new Complex(rho * Math.Cos(phi + h), rho * Math.Sin(phi + h))) -
        //            Function(roi_mat, ref coefsScat, i,
        //            new Complex(rho * Math.Cos(phi - h), rho * Math.Sin(phi - h)))) / (2.0 * h);
        //    }

        //    return grad;
        //}

        //private static Vector<double> GradientM(Matrix<Complex> roi_mat, ref Vector<Complex> coefsScat, ref Vector<double> grad)
        //{
        //    double h = 0.001;
        //    double rho, phi;

        //    for (int i = 0; i < coefsScat.Count; ++i)
        //    {
        //        rho = coefsScat[i].Magnitude;
        //        phi = coefsScat[i].Phase;

        //        grad[i] = (Function(roi_mat, ref coefsScat, i,
        //            new Complex((rho + h) * Math.Cos(phi), (rho + h) * Math.Sin(phi))) -
        //            Function(roi_mat, ref coefsScat, i,
        //            new Complex((rho - h) * Math.Cos(phi), (rho - h) * Math.Sin(phi)))) / (2.0 * h);
        //    }

        //    return grad;
        //}
    }

    public struct FWHM
    {
        private PointD m_coordsMax;
        private RectangleF m_box;
        private double m_area;
        private double m_intensityMax;
        
        public FWHM(PointD coordsMax, RectangleF box, double area, double intensityMax)
        {
            m_coordsMax = coordsMax;
            m_box = box;
            m_area = area;
            m_intensityMax = intensityMax;
        }

        public PointD MaxCoordinates
        {
            get
            {
                return m_coordsMax;
            }
        }

        public RectangleF Box
        {
            get
            {
                return m_box;
            }
        }

        public double Intensity
        {
            get
            {
                return m_intensityMax;
            }
        }

        public double AspectRatio
        {
            get
            {
                return m_box.Width / m_box.Height;
            }
        }

        public float HorizontalFWHM
        {
            get
            {
                return m_box.Width;
            }
        }

        public float VerticalFWHM
        {
            get
            {
                return m_box.Height;
            }
        }

        public double Area
        {
            get
            {
                return m_area;
            }
        }
    }

    //public struct PointD
    //{
    //    private double m_x;
    //    private double m_y;

    //    public PointD(double x, double y)
    //    {
    //        m_x = x;
    //        m_y = y;
    //    }

    //    public double X
    //    {
    //        get
    //        {
    //            return m_x;
    //        }
    //        set
    //        {
    //            m_x = value;
    //        }
    //    }

    //    public double Y
    //    {
    //        get
    //        {
    //            return m_y;
    //        }
    //        set
    //        {
    //            m_y = value;
    //        }
    //    }
    //}
}