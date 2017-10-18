using System;
using System.Numerics;
using System.IO;
using System.Globalization;
using CsvHelper;

namespace ScatLib
{
    unsafe public partial class NearField : IEquatable<NearField>, IDisposable
    {
        public static NearField GenerateGaussianBeam(double beam_dx, double beam_dy, double beam_width,
            int nodes_x, int nodes_y, double step_x, double step_y, double min_x,
            double min_y, double wavelength, int projection = 0)
        {
            NearField nf = null;
            /* Create empty near field */
            NearField.op_Assign(ref nf, new NearField(nodes_x, nodes_y, step_x, step_y, min_x, min_y, wavelength));
            /* Generate field */
            for (int y = 0; y < nodes_y; y++)
            {
                for (int x = 0; x < nodes_x; x++)
                {
                    nf[projection, x, y] = new Complex(
                        Math.Exp(-((min_x + x * step_x - beam_dx) * (min_x + x * step_x - beam_dx)                        // Real
                        + (min_y + y * step_y - beam_dy) * (min_y + y * step_y - beam_dy)) / (beam_width * beam_width)),  // 
                        0.0);                                                                                             // Imaginary
                }
            }
            return nf;
        }

        public static void GenerateGaussianBeamBatch(string filelist, string outpath)
        {
            StreamReader sr;
            CsvReader reader;
            NearField nf;
            string filename, extension;

            /* Near field properties (temp) */
            int nodes_x, nodes_y;
            double step_x, step_y;
            double min_x, max_x;
            double min_y, max_y;
            double wavelength;

            sr = new StreamReader(filelist);
            reader = new CsvReader(sr);
            reader.Configuration.CultureInfo = CultureInfo.InvariantCulture;
            reader.Configuration.HasHeaderRecord = false;
            /* Check if the list file contains proper number of columns */
            if (reader.Read() && reader.CurrentRecord.Length != 11)
            {
                reader.Dispose();
                sr.Close();
                sr.Dispose();
                return;
            }

            while (reader.Read())
            {
                min_x = reader.GetField<double>(1);
                max_x = reader.GetField<double>(2);
                min_y = reader.GetField<double>(3);
                max_y = reader.GetField<double>(4);
                step_x = reader.GetField<double>(5);
                step_y = reader.GetField<double>(6);
                nodes_x = (int)Math.Floor((max_x - min_x) / step_x) + 1;
                nodes_y = (int)Math.Floor((max_y - min_y) / step_y) + 1;
                wavelength = reader.GetField<double>(10);

                nf = NearField.GenerateGaussianBeam(reader.GetField<double>(7), reader.GetField<double>(8),
                    reader.GetField<double>(9), nodes_x, nodes_y, step_x, step_y, min_x, min_y,
                    wavelength);

                filename = reader.GetField<string>(0);
                extension = filename.Substring(filename.Length - 3).ToUpper();
                if (extension.Equals("BIN"))
                {
                    nf.SaveToFile(string.Format("{0}\\{1}", outpath, filename));
                }
                else if (extension.Equals("CSV"))
                {
                    nf.Export(string.Format("{0}\\{1}", outpath, filename));
                }
                else
                    continue;

                nf.Dispose();
            }
            reader.Dispose();
            sr.Close();
            sr.Dispose();
        }
    }
}