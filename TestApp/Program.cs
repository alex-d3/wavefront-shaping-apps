using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using ScatLib;
using MathNet.Numerics.LinearAlgebra;
using System.Numerics;
using ScatLib.WavefrontShaping;

namespace TestApp
{
    class Program
    {
        static void Main(string[] args)
        {
            //NearField[] x_fields, y_fields;

            //string[] y_files = Directory.EnumerateFiles(@"d:\Documents\PhD\KNU\Tasks\Task_10_New_program\Fields_440_30x30_X\",
            //    "*.dat", SearchOption.TopDirectoryOnly).ToArray();

            //y_fields = new NearField[y_files.Length];

            //for (int i = 0; i < y_fields.Length; ++i)
            //{
            //    NearField.op_Assign(ref y_fields[i], NearField.ParseMSTM(y_files[i], 0.440));
            //    //y_fields[i].SaveToFile(y_files[i].Replace("dat", "bin"));
            //}

            ////----------------
            //x_fields = new NearField[81];
            //int[] order = { -1, -2, -3, -4, 0, 1, 2, 3, 4 };
            //double beam_width = 0.6328 * y_fields[0].Wavenumber;
            //for (int x = 0, i = 0; x < order.Length; x++)
            //{
            //    for (int y = 0; y < order.Length; y++, i++)
            //    {
            //        NearField.op_Assign(ref x_fields[i],
            //            NearField.GenerateGaussianBeam(order[x] * y_fields[0].Wavelength * y_fields[0].Wavenumber,
            //            order[y] * y_fields[0].Wavelength * y_fields[0].Wavenumber, beam_width, y_fields[0].NodesX,
            //            y_fields[0].NodesY, y_fields[0].StepX, y_fields[0].StepY, y_fields[0].MinX,
            //            y_fields[0].MinY, y_fields[0].Wavelength));

            //        x_fields[i].SaveToFile(
            //            string.Format("d:/Documents/PhD/KNU/Tasks/Task_10_New_program/Fields_440_30x30_X/Incident/nf_sp1872_111_{0}{1}_{2}{3}_440_X.dat",
            //            order[x] < 0 ? "m" : "p", Math.Abs(order[x]), order[y] < 0 ? "m" : "p", Math.Abs(order[y])));
            //    }
            //}
            //----------------

            //string[] x_files = Directory.EnumerateFiles(@"d:\Documents\PhD\KNU\Tasks\Task_10_New_program\Fields\",
            //    "*.bin", SearchOption.TopDirectoryOnly).ToArray();
            //x_fields = new NearField[x_files.Length];

            //for (int i = 0; i < x_fields.Length; ++i)
            //{
            //    NearField.op_Assign(ref x_fields[i], new NearField(x_files[i]));
            //}

            //Basis basis = new Basis(x_fields, y_fields);
            //basis.Save(@"d:\Documents\PhD\KNU\Tasks\Task_10_New_program\Basis_81f_440X\basis_81f.basbin");

            //-------------------

            Basis basis = new Basis(@"d:\Documents\PhD\KNU\Tasks\Task_10_New_program\Basis_81f_440X\basis_81f.basbin");
            basis.UsedFields = 81;

            System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();

            int x_max, y_max;
            List<OptResult> results = new List<OptResult>((240 - 20) / 10); // (161 - 4) / 9 for 0.6328 20x20
            for (int x1 = 20; x1 <= 220; x1 += 10) // 4, 148, 9
            {
                NearField nf = null;

                stopwatch.Start();
                // 76 for 0.6328
                NearField.op_Assign(ref nf, WavefrontShaping.Focus(basis, new System.Drawing.Rectangle(x1, 120, 9, 9),
                    1000, 10000, 1E-8));
                stopwatch.Stop();
                Console.WriteLine(stopwatch.Elapsed.ToString());
                nf.SaveToFile(string.Format(@"d:\Documents\PhD\KNU\Tasks\Task_10_New_program\Fields_440_30x30_X\Focus_AxisX\{0}.bin", x1.ToString("D3")));
                stopwatch.Reset();

                double average_energy = 0.0;
                OptResult temp_res = new OptResult();

                temp_res.PeakIntensity = nf.GetMaxIntensity(out x_max, out y_max);
                temp_res.PeakX = x_max;
                temp_res.PeakY = y_max;

                for (int y = 0; y < nf.NodesY; ++y)
                {
                    for (int x = 0; x < nf.NodesX; ++x)
                    {
                        average_energy += (nf[0, x, y] * Complex.Conjugate(nf[0, x, y]) +
                            nf[1, x, y] * Complex.Conjugate(nf[1, x, y]) +
                            nf[2, x, y] * Complex.Conjugate(nf[2, x, y])).Real;
                    }
                }

                average_energy /= nf.NodesX * nf.NodesY;
                temp_res.AverageEnergySingle = average_energy;
                temp_res.EnhancementSingle = temp_res.PeakIntensity / temp_res.AverageEnergySingle;

                //double fwhm_x_min = 0.0, fwhm_x_max = 0.0, fwhm_y_min = 0.0, fwhm_y_max = 0.0, fwhm_area = 0.0;
                FWHM fwhm = nf.CalculateFWHM();
                //nf.CalculateFWHM(out fwhm_x_min, out fwhm_x_max, out fwhm_y_min, out fwhm_y_max, out fwhm_area);

                temp_res.XminFWHM = fwhm.Box.Width;
                temp_res.YminFWHM = fwhm.Box.Height;
                temp_res.AreaFWHM = fwhm.Area;
                temp_res.Energy = nf.GetElectricFieldEnergy();

                results.Add(temp_res);

                nf.Dispose();
            }

            int i = 20;
            foreach (OptResult res in results)
            {
                Console.WriteLine("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}", i,
                    res.PeakX.ToString(),
                    res.PeakY.ToString(), res.PeakIntensity.ToString("E4"),
                    res.AverageEnergySingle.ToString("E4"), res.XminFWHM.ToString("F2"), res.YminFWHM.ToString("F2"),
                    res.AreaFWHM.ToString(), res.Energy);

                i += 10;
            }

            basis.Dispose();
            Console.ReadKey();
            //-------------------

            //NearField nf = null;
            ////System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
            ////stopwatch.Start();
            //NearField.op_Assign(ref nf, NearField.Focus(basis, new System.Drawing.Rectangle(76, 76, 9, 9)));
            ////stopwatch.Stop();
            //nf.Export("d:/foc_temp.csv");
            //nf.Dispose();

            //Console.WriteLine("Focusing time: {0}", stopwatch.Elapsed.ToString());
        }
    }

    public struct OptResult
    {
        public string FileName { get; set; }
        public double PeakIntensity { get; set; }
        public double AverageEnergySingle { get; set; }
        public double EnhancementSingle { get; set; }
        public double XminFWHM { get; set; }
        public double YminFWHM { get; set; }
        public double XmaxFWHM { get; set; }
        public double YmaxFWHM { get; set; }
        public double AreaFWHM { get; set; }
        public int PeakX { get; set; }
        public int PeakY { get; set; }

        public double Energy { get; set; }
    }
}
