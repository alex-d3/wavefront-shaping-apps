using System;
using System.IO;
using System.Numerics;
using System.Globalization;
using CsvHelper;

namespace ScatLib
{
    unsafe public partial class NearField : IEquatable<NearField>, IDisposable
    {
        public void SaveToFile(string full_path)
        {
            double[] params_double = new double[] { step_x, step_y, min_x, max_x, min_y, max_y, wavelength };
            int[] params_int = new int[] { nodes_x, nodes_y, dim };

            using (UnmanagedMemoryStream ms = new UnmanagedMemoryStream((byte*)electric_field, electric_field_size,
                electric_field_size, FileAccess.Read))
            using (FileStream fs = new FileStream(full_path, FileMode.Create, FileAccess.Write,
                FileShare.None, 2 << 18, FileOptions.SequentialScan))
            {
                fs.Write(signature, 0, signature.Length);
                params_double.AsByteArray(bytes => fs.Write(bytes, 0, bytes.Length));
                params_int.AsByteArray(bytes => fs.Write(bytes, 0, bytes.Length));

                ms.CopyTo(fs);
            }

            // Писать (читать) в файл (из файла) внутри сериализатора напрямую. По идее, данные автоматически записываются
            // в буффер, а потом .NET сама определяет когда его записать на диск. Должно уменьшиться количество операций.
            // Для RAID массивов лучше работает прямая запись без буфферизации.
        }

        public void ReadFromFile(string full_path)
        {
            byte[] signature_file = new byte[signature.Length];
            double[] params_double = new double[7];
            int[] params_int = new int[3];

            if (electric_field != null)
            {
                UnmanagedMemory.Free(electric_field);
                electric_field = null;
            }

            using (FileStream fs = new FileStream(full_path, FileMode.Open, FileAccess.Read,
                FileShare.None, 2 << 18, FileOptions.SequentialScan))
            {
                fs.Read(signature_file, 0, signature_file.Length);

                for (int i = 0; i < signature.Length; i++)
                {
                    if (signature[i] != signature_file[i])
                        return;
                }
                /* Read parameters */
                params_double.AsByteArray(bytes => fs.Read(bytes, 0, bytes.Length));
                params_int.AsByteArray(bytes => fs.Read(bytes, 0, bytes.Length));

                step_x = params_double[0];
                step_y = params_double[1];
                min_x = params_double[2];
                max_x = params_double[3];
                min_y = params_double[4];
                max_y = params_double[5];
                wavelength = params_double[6];

                nodes_x = params_int[0];
                nodes_y = params_int[1];

                electric_field_elem_count = dim * nodes_x * nodes_y;
                electric_field_size = sizeof(Complex) * electric_field_elem_count;
                offset_Ey = nodes_y * nodes_x;
                offset_Ez = 2 * offset_Ey;

                /* Create arrays and read the data */
                electric_field = (Complex*)UnmanagedMemory.Alloc(electric_field_size);

                using (UnmanagedMemoryStream ms = new UnmanagedMemoryStream((byte*)electric_field,
                    electric_field_size, electric_field_size, FileAccess.Write))
                {
                    fs.CopyTo(ms);
                }
            }
        }

        public void Export(string full_path)
        {
            using (StreamWriter sw = new StreamWriter(full_path))
            using (CsvWriter writer = new CsvWriter(sw))
            {
                string[] headers = { "X", "Y", "Ex (Re)", "Ex (Im)", "Ey (Re)", "Ey (Im)", "Ez (Re)", "Ez (Im)" };

                writer.Configuration.CultureInfo = CultureInfo.InvariantCulture;

                /* Write near field properties */
                writer.WriteField("Nodes X, Y");
                writer.WriteField<int>(nodes_x);
                writer.WriteField<int>(nodes_y);
                writer.NextRecord();
                writer.WriteField("Min X, Y");
                writer.WriteField(min_x.ToString("E7", CultureInfo.InvariantCulture));
                writer.WriteField(min_y.ToString("E7", CultureInfo.InvariantCulture));
                writer.NextRecord();
                writer.WriteField("Max X, Y");
                writer.WriteField(max_x.ToString("E7", CultureInfo.InvariantCulture));
                writer.WriteField(max_y.ToString("E7", CultureInfo.InvariantCulture));
                writer.NextRecord();
                writer.WriteField("Step X, Y");
                writer.WriteField(step_x.ToString("E7", CultureInfo.InvariantCulture));
                writer.WriteField(step_y.ToString("E7", CultureInfo.InvariantCulture));
                writer.NextRecord();
                writer.WriteField("Wavelength");
                writer.WriteField(wavelength.ToString("E7", CultureInfo.InvariantCulture));
                writer.NextRecord();

                /* Write headers */
                for (int i = 0; i < headers.Length; i++)
                {
                    writer.WriteField<string>(headers[i]);
                }
                writer.NextRecord();

                /* Write data */
                for (int y = 0; y < nodes_y; y++)
                {
                    for (int x = 0; x < nodes_x; x++)
                    {
                        writer.WriteField<double>((min_x + x * step_x));
                        writer.WriteField<double>((min_y + y * step_y));
                        writer.WriteField(electric_field[y * nodes_x + x].Real.ToString("E7", CultureInfo.InvariantCulture));
                        writer.WriteField(electric_field[y * nodes_x + x].Imaginary.ToString("E7", CultureInfo.InvariantCulture));
                        writer.WriteField(electric_field[offset_Ey + y * nodes_x + x].Real.ToString("E7", CultureInfo.InvariantCulture));
                        writer.WriteField(electric_field[offset_Ey + y * nodes_x + x].Imaginary.ToString("E7", CultureInfo.InvariantCulture));
                        writer.WriteField(electric_field[offset_Ez + y * nodes_x + x].Real.ToString("E7", CultureInfo.InvariantCulture));
                        writer.WriteField(electric_field[offset_Ez + y * nodes_x + x].Imaginary.ToString("E7", CultureInfo.InvariantCulture));
                        writer.NextRecord();
                    }
                }
            }
        }

        public void Import(string full_path)
        {
            using (StreamReader sr = new StreamReader(full_path))
            using (CsvReader reader = new CsvReader(sr))
            {
                reader.Configuration.CultureInfo = CultureInfo.InvariantCulture;
                reader.Configuration.HasHeaderRecord = false;
                /* Read electric field properties */
                reader.Read();
                nodes_x = reader.GetField<int>(1);
                nodes_y = reader.GetField<int>(2);
                reader.Read();
                min_x = reader.GetField<double>(1);
                min_y = reader.GetField<double>(2);
                reader.Read();
                max_x = reader.GetField<double>(1);
                max_y = reader.GetField<double>(2);
                reader.Read();
                step_x = reader.GetField<double>(1);
                step_y = reader.GetField<double>(2);
                reader.Read();
                wavelength = reader.GetField<double>(1);
                reader.Read();
                reader.Read();

                offset_Ey = nodes_y * nodes_x;
                offset_Ez = 2 * offset_Ey;
                electric_field_elem_count = dim * nodes_y * nodes_x;
                electric_field_size = sizeof(Complex) * electric_field_elem_count;
                /* Read data */
                electric_field = (Complex*)UnmanagedMemory.Alloc(electric_field_size);

                for (int y = 0; y < nodes_y; y++)
                {
                    for (int x = 0; x < nodes_x; x++)
                    {
                        electric_field[y * nodes_x + x] =
                            new Complex(reader.GetField<double>(2), reader.GetField<double>(3));
                        electric_field[offset_Ey + y * nodes_x + x] =
                            new Complex(reader.GetField<double>(4), reader.GetField<double>(5));
                        electric_field[offset_Ez + y * nodes_x + x] =
                            new Complex(reader.GetField<double>(6), reader.GetField<double>(7));
                        reader.Read();
                    }
                }
            }
        }

        public static NearField ParseMSTM(string path, double wavelength)
        {
            Complex[] electric_field;
            string[] temp;
            StreamReader sr;
            string[] delimiter = { " " };
            int skip; // Skip some lines

            /* Near field properties (temp) */
            int nodes_x = 0, nodes_y = 0;
            double step_x = 0.0, step_y = 0.0;
            double min_x = 0.0, max_x = 0.0;
            double min_y = 0.0, max_y = 0.0;

            sr = new StreamReader(path);
            temp = sr.ReadLine().Split(delimiter, StringSplitOptions.RemoveEmptyEntries);
            if (temp.Length == 2)
            {
                nodes_x = Convert.ToInt32(temp[0]);
                nodes_y = Convert.ToInt32(temp[1]);
                skip = Convert.ToInt32(sr.ReadLine());
                /* Skip lines with overlapping spheres */
                for (int i = 0; i < skip; i++) { sr.ReadLine(); }
            }
            else
            {
                sr.Close();
                sr.Dispose();
                return null;
            }
            
            // Create arrays. Array contains 3 subarrays for Ex, Ey, Ez. Each of them contains
            // arrays for rows [1, nfi.NodesX].
            electric_field = new Complex[3 * nodes_y * nodes_x];

            int N = nodes_x * nodes_y;
            int x = 0;              // Initial X and Y indices
            int y = 0;

            for (int i = 0; i < N; i++)
            {
                temp = sr.ReadLine().Split(delimiter, StringSplitOptions.RemoveEmptyEntries);

                if (i <= nodes_y)
                {
                    if (i == 0)
                    {
                        step_x -= Convert.ToDouble(temp[0], CultureInfo.InvariantCulture);
                        step_y -= Convert.ToDouble(temp[1], CultureInfo.InvariantCulture);

                        min_x = Convert.ToDouble(temp[0], CultureInfo.InvariantCulture);
                        min_y = Convert.ToDouble(temp[1], CultureInfo.InvariantCulture);
                    }
                    else if (i == 1)
                    {
                        step_y += Convert.ToDouble(temp[1], CultureInfo.InvariantCulture);
                    }
                    else if (i == nodes_y)
                    {
                        step_x += Convert.ToDouble(temp[0], CultureInfo.InvariantCulture);
                    }
                }

                electric_field[y * nodes_x + x] = new Complex(Convert.ToDouble(temp[2], CultureInfo.InvariantCulture),
                    Convert.ToDouble(temp[3], CultureInfo.InvariantCulture));
                electric_field[nodes_y * nodes_x + y * nodes_x + x] = new Complex(Convert.ToDouble(temp[4], CultureInfo.InvariantCulture),
                    Convert.ToDouble(temp[5], CultureInfo.InvariantCulture));
                electric_field[2 * nodes_y * nodes_x + y * nodes_x + x] = new Complex(Convert.ToDouble(temp[6], CultureInfo.InvariantCulture),
                    Convert.ToDouble(temp[7], CultureInfo.InvariantCulture));

                ++y;
                if (y == nodes_y)
                {
                    y = 0;
                    ++x;
                }
            }

            sr.Close();
            sr.Dispose();

            max_x = Convert.ToDouble(temp[0], CultureInfo.InvariantCulture);
            max_y = Convert.ToDouble(temp[1], CultureInfo.InvariantCulture);

            return new NearField(electric_field, nodes_x, nodes_y, step_x, step_y, min_x,
                min_y, wavelength);
        }

        /// <summary>
        /// Modified version of parser. Ex -> Hy, Ey -> Hx, Ez -> Hz
        /// </summary>
        /// <param name="path">Path to the MSTM data file.</param>
        /// <param name="wavelength">Wavelength of the light.</param>
        /// <param name="mod">Dummy parameter.</param>
        /// <returns></returns>
        public static NearField ParseMSTM(string path, double wavelength, bool mod)
        {
            Complex[] electric_field;
            string[] temp;
            StreamReader sr;
            string[] delimiter = { " " };
            int skip; // Skip some lines

            /* Near field properties (temp) */
            int nodes_x = 0, nodes_y = 0;
            double step_x = 0.0, step_y = 0.0;
            double min_x = 0.0, max_x = 0.0;
            double min_y = 0.0, max_y = 0.0;

            sr = new StreamReader(path);
            temp = sr.ReadLine().Split(delimiter, StringSplitOptions.RemoveEmptyEntries);
            if (temp.Length == 2)
            {
                nodes_x = Convert.ToInt32(temp[0]);
                nodes_y = Convert.ToInt32(temp[1]);
                skip = Convert.ToInt32(sr.ReadLine());
                /* Skip lines with overlapping spheres */
                for (int i = 0; i < skip; i++) { sr.ReadLine(); }
            }
            else
            {
                sr.Close();
                sr.Dispose();
                return null;
            }

            // Create arrays. Array contains 3 subarrays for Ex, Ey, Ez. Each of them contains
            // arrays for rows [1, nfi.NodesX].
            electric_field = new Complex[3 * nodes_y * nodes_x];

            int N = nodes_x * nodes_y;
            int x = 0;              // Initial X and Y indices
            int y = 0;

            for (int i = 0; i < N; i++)
            {
                temp = sr.ReadLine().Split(delimiter, StringSplitOptions.RemoveEmptyEntries);

                if (i <= nodes_y)
                {
                    if (i == 0)
                    {
                        step_x -= Convert.ToDouble(temp[0], CultureInfo.InvariantCulture);
                        step_y -= Convert.ToDouble(temp[1], CultureInfo.InvariantCulture);

                        min_x = Convert.ToDouble(temp[0], CultureInfo.InvariantCulture);
                        min_y = Convert.ToDouble(temp[1], CultureInfo.InvariantCulture);
                    }
                    else if (i == 1)
                    {
                        step_y += Convert.ToDouble(temp[1], CultureInfo.InvariantCulture);
                    }
                    else if (i == nodes_y)
                    {
                        step_x += Convert.ToDouble(temp[0], CultureInfo.InvariantCulture);
                    }
                }

                // 0 (x) 1 (y) 2 (Ex Re) 3 (Ex Im) 4 (Ey Re) 5 (Ey Im) 6 (Ez Re) 7 (Ez Im)
                // 8 (Hx Re) 9 (Hx Im) 10 (Hy Re) 11 (Hy Im) 12 (Hz Re) 13 (Hz Im)

                // Ex -> Hy
                electric_field[y * nodes_x + x] = new Complex(Convert.ToDouble(temp[10], CultureInfo.InvariantCulture),
                    Convert.ToDouble(temp[11], CultureInfo.InvariantCulture));
                // Ey -> Hx
                electric_field[nodes_y * nodes_x + y * nodes_x + x] = new Complex(Convert.ToDouble(temp[8], CultureInfo.InvariantCulture),
                    Convert.ToDouble(temp[9], CultureInfo.InvariantCulture));
                // Ez
                electric_field[2 * nodes_y * nodes_x + y * nodes_x + x] = new Complex(Convert.ToDouble(temp[12], CultureInfo.InvariantCulture),
                    Convert.ToDouble(temp[13], CultureInfo.InvariantCulture));

                ++y;
                if (y == nodes_y)
                {
                    y = 0;
                    ++x;
                }
            }

            sr.Close();
            sr.Dispose();

            max_x = Convert.ToDouble(temp[0], CultureInfo.InvariantCulture);
            max_y = Convert.ToDouble(temp[1], CultureInfo.InvariantCulture);

            return new NearField(electric_field, nodes_x, nodes_y, step_x, step_y, min_x,
                min_y, wavelength);
        }
    }
}