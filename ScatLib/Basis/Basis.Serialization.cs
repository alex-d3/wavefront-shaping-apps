using System;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Factorization;
using System.IO;
using System.Linq;

namespace ScatLib
{
    public sealed partial class Basis : IDisposable
    {
        public void Save(string path)
        {
            string dir = Path.GetDirectoryName(path);

            using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write,
                FileShare.None, 2 << 18, FileOptions.SequentialScan))
            {
                WriteSignature(fs);
                WriteBasisSize(fs);
                WriteFields(fs, dir);
                WriteSingularNumbers(fs);
                WriteConversionCoefficients(fs);
            }
        }

        private void Open(string path)
        {
            string dir = Path.GetDirectoryName(path);
            byte[] buffer = new byte[1024];

            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read,
                FileShare.None, 2 << 18, FileOptions.SequentialScan))
            {
                CheckSignature(fs, ref buffer);
                ReadBasisSize(fs, ref buffer);
                ReadFields(fs, dir, ref buffer);
                ReadSingularNumbers(fs);
                ReadConversionCoefficients(fs, ref buffer);
            }

            used_fields = basis_size;
        }

        //private void Open(string path)
        //{
        //    /* Read:
        //     * 1. Signature (8 bytes)
        //     * 2. Number of length and path records (4 bytes)
        //     * 3. Lengths of relative paths to files
        //     * X. Conversion coefficients row and columns count
        //     * 4. Paths to files
        //     * 5. Singular numbers 'wElems'
        //     * X. Conversion coefficients arrays
        //     *  */
        //    string dirX = Path.Combine(Path.GetDirectoryName(path), "IN_FIELDS");
        //    string dirY = Path.Combine(Path.GetDirectoryName(path), "OUT_FIELDS");
        //    //string directory = path.Substring(0, path.LastIndexOf('\\'));
        //    //byte[] signature_ref = FileSignature.Basis;
        //    byte[] buffer = new byte[2 << 17];

        //    int array_length;
        //    //int[] rel_path_byteCount;
        //    int[] x_filenames_byteCount, y_filenames_byteCount;
        //    int conv_coefs_row_count, conv_coefs_column_count;

        //    using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read,
        //        FileShare.None, 2 << 18, FileOptions.SequentialScan))
        //    {
        //        // 1.Signature
        //        fs.Read(buffer, 0, signature.Length);

        //        for (int i = 0; i < signature.Length; ++i)
        //        {
        //            if (signature[i] != buffer[i])
        //                return;
        //        }
        //        // 2. Number of length and path records
        //        fs.Read(buffer, 0, sizeof(int));
        //        array_length = BitConverter.ToInt32(buffer, 0);

        //        //rel_path_byteCount = new int[array_length];
        //        x_filenames_byteCount = new int[array_length];
        //        y_filenames_byteCount = new int[array_length];
        //        x_basisFiles = new string[array_length];
        //        y_basisFiles = new string[array_length];

        //        // 3. Lengths of relative paths to files
        //        //rel_path_byteCount.AsByteArray(bytes => fs.Read(bytes, 0, bytes.Length));
        //        x_filenames_byteCount.AsByteArray(bytes => fs.Read(bytes, 0, bytes.Length));
        //        y_filenames_byteCount.AsByteArray(bytes => fs.Read(bytes, 0, bytes.Length));

        //        // X. Conversion coefficients row and columns count
        //        fs.Read(buffer, 0, 2 * sizeof(int));
        //        conv_coefs_row_count = BitConverter.ToInt32(buffer, 0);
        //        conv_coefs_column_count = BitConverter.ToInt32(buffer, sizeof(int));

        //        conv_coefs_inc = new Complex[conv_coefs_row_count][];
        //        conv_coefs_scat = new Complex[conv_coefs_row_count][];

        //        for (int i = 0; i < conv_coefs_row_count; i++)
        //        {
        //            conv_coefs_inc[i] = new Complex[conv_coefs_column_count];
        //            conv_coefs_scat[i] = new Complex[conv_coefs_column_count];
        //        }

        //        // 4. Paths to files
        //        // x basis files
        //        for (int i = 0; i < x_basisFiles.Length; ++i)
        //        {
        //            fs.Read(buffer, 0, x_filenames_byteCount[i]);
        //            x_basisFiles[i] = Encoding.Unicode.GetString(buffer, 0, x_filenames_byteCount[i]);
        //        }
        //        // y basis files
        //        for (int i = 0; i < y_basisFiles.Length; ++i)
        //        {
        //            fs.Read(buffer, 0, y_filenames_byteCount[i]);
        //            y_basisFiles[i] = Encoding.Unicode.GetString(buffer, 0, y_filenames_byteCount[i]);
        //        }

        //        // 5. Singular numbers 'wElems'
        //        wElems = new Complex[array_length];
        //        wElems.AsByteArray(bytes => fs.Read(bytes, 0, bytes.Length));

        //        // X. Conversion coefficients arrays
        //        for (int i = 0; i < conv_coefs_row_count; i++)
        //            conv_coefs_inc[i].AsByteArray(bytes => { fs.Read(bytes, 0, bytes.Length); });
        //        for (int i = 0; i < conv_coefs_row_count; i++)
        //            conv_coefs_scat[i].AsByteArray(bytes => { fs.Read(bytes, 0, bytes.Length); });
        //    }

        //    // Open basis fields
        //    x_basisNF = new NearField[array_length];
        //    y_basisNF = new NearField[array_length];

        //    for (int i = 0; i < array_length; ++i)
        //    {
        //        NearField.op_Assign(ref x_basisNF[i], new NearField(Path.Combine(dirX, x_basisFiles[i])));
        //        NearField.op_Assign(ref y_basisNF[i], new NearField(Path.Combine(dirY, y_basisFiles[i])));
        //    }

        //    basis_size = array_length;
        //}

        //public void Save(string path)
        //{
        //    /* Save:
        //     * 1. Signature (8 bytes)
        //     * 2. Number of length and path records (4 bytes)
        //     * 3. Lengths of relative paths to files
        //     * X. Conversion coefficients row and columns count
        //     * 4. Paths to files
        //     * 5. Singular numbers 'wElems'
        //     * X. Conversion coefficients arrays
        //     *  */
        //    int[] x_filenames_byteCount, y_filenames_byteCount;
        //    string dir = Path.GetDirectoryName(path);
        //    // Save fields
        //    string dir_in_fields = Path.Combine(dir, "IN_FIELDS");
        //    string dir_out_fields = Path.Combine(dir, "OUT_FIELDS");

        //    Directory.CreateDirectory(dir_in_fields);
        //    Directory.CreateDirectory(dir_out_fields);

        //    for (int i = 0; i < x_basisNF.Length; ++i)
        //    {
        //        x_basisNF[i].SaveToFile(Path.Combine(dir_in_fields, x_basisFiles[i]));
        //        y_basisNF[i].SaveToFile(Path.Combine(dir_out_fields, y_basisFiles[i]));
        //    }

        //    // Save basis information
        //    x_filenames_byteCount = new int[x_basisFiles.Length];
        //    y_filenames_byteCount = new int[y_basisFiles.Length];
        //    for (int i = 0; i < x_basisFiles.Length; ++i)
        //    {
        //        x_filenames_byteCount[i] = Encoding.Unicode.GetByteCount(x_basisFiles[i]);
        //        y_filenames_byteCount[i] = Encoding.Unicode.GetByteCount(y_basisFiles[i]);
        //    }

        //    using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write,
        //        FileShare.None, 2 << 18, FileOptions.SequentialScan))
        //    {
        //        // 1. Signature
        //        fs.Write(signature, 0, signature.Length);
        //        // 2. Number of length and path records
        //        fs.Write(BitConverter.GetBytes(x_filenames_byteCount.Length), 0, sizeof(int));
        //        // 3. Lengths of relative paths to files
        //        x_filenames_byteCount.AsByteArray(bytes => fs.Write(bytes, 0, bytes.Length));
        //        y_filenames_byteCount.AsByteArray(bytes => fs.Write(bytes, 0, bytes.Length));
        //        // X. Conversion coefficients row and columns count
        //        byte[] temp = BitConverter.GetBytes(conv_coefs_inc.Length);
        //        fs.Write(temp, 0, temp.Length);
        //        temp = BitConverter.GetBytes(conv_coefs_inc[0].Length);
        //        fs.Write(temp, 0, temp.Length);
        //        // 4. Paths to files
        //        for (int i = 0; i < x_basisFiles.Length; ++i)
        //            fs.Write(Encoding.Unicode.GetBytes(x_basisFiles[i]), 0, x_filenames_byteCount[i]);

        //        for (int i = 0; i < y_basisFiles.Length; ++i)
        //            fs.Write(Encoding.Unicode.GetBytes(y_basisFiles[i]), 0, y_filenames_byteCount[i]);
        //        // 5. Singular numbers 'wElems'
        //        wElems.AsByteArray(bytes => fs.Write(bytes, 0, bytes.Length));
        //        // X. Conversion coefficients arrays
        //        for (int i = 0; i < conv_coefs_inc.Length; i++)
        //            conv_coefs_inc[i].AsByteArray(bytes => { fs.Write(bytes, 0, bytes.Length); });
        //        for (int i = 0; i < conv_coefs_scat.Length; i++)
        //            conv_coefs_scat[i].AsByteArray(bytes => { fs.Write(bytes, 0, bytes.Length); });
        //    }
        //}

        private void WriteSignature(Stream stream)
        {
            stream.Write(signature, 0, signature.Length);
        }

        private bool CheckSignature(Stream stream, ref byte[] buffer)
        {
            if (buffer.Length < signature.Length)
                buffer = new byte[signature.Length];

            stream.Read(buffer, 0, signature.Length);

            for (int i = 0; i < signature.Length; ++i)
                if (signature[i] != buffer[i])
                    return false;

            return true;
        }

        private void WriteBasisSize(Stream stream)
        {
            stream.Write(BitConverter.GetBytes(basis_size), 0, sizeof(int));
        }

        private void ReadBasisSize(Stream stream, ref byte[] buffer)
        {
            if (buffer.Length < sizeof(int))
                buffer = new byte[sizeof(int)];

            stream.Read(buffer, 0, sizeof(int));
            basis_size = BitConverter.ToInt32(buffer, 0);
        }

        private void WriteFields(Stream stream, string dir)
        {
            string in_dir = Path.Combine(dir, "IN_FIELDS");
            string out_dir = Path.Combine(dir, "OUT_FIELDS");
            string[] in_filenames = new string[basis_size];
            string[] out_filenames = new string[basis_size];
            int[] in_filenames_byteCount = new int[basis_size];
            int[] out_filenames_byteCount = new int[basis_size];

            for (int i = 0; i < basis_size; i++)
            {
                in_filenames[i] = string.Format("in_basis_{0}.bin", i.ToString("D3"));
                out_filenames[i] = string.Format("out_basis_{0}.bin", i.ToString("D3"));

                in_filenames_byteCount[i] = Encoding.Unicode.GetByteCount(in_filenames[i]);
                out_filenames_byteCount[i] = Encoding.Unicode.GetByteCount(out_filenames[i]);
            }

            //stream.Write(BitConverter.GetBytes(basis_size), 0, sizeof(int));
            in_filenames_byteCount.AsByteArray(bytes => stream.Write(bytes, 0, bytes.Length));
            out_filenames_byteCount.AsByteArray(bytes => stream.Write(bytes, 0, bytes.Length));

            for (int i = 0; i < basis_size; i++)
                stream.Write(Encoding.Unicode.GetBytes(in_filenames[i]), 0, in_filenames_byteCount[i]);
                

            for (int i = 0; i < basis_size; i++)
                stream.Write(Encoding.Unicode.GetBytes(out_filenames[i]), 0, out_filenames_byteCount[i]);

            if (!Directory.Exists(in_dir))
                Directory.CreateDirectory(in_dir);

            if (!Directory.Exists(out_dir))
                Directory.CreateDirectory(out_dir);

            for (int i = 0; i < basis_size; i++)
            {
                x_basisNF[i].SaveToFile(Path.Combine(in_dir, in_filenames[i]));
                y_basisNF[i].SaveToFile(Path.Combine(out_dir, out_filenames[i]));
            }
        }

        private void ReadFields(Stream stream, string dir, ref byte[] buffer)
        {
            string[] in_filenames, out_filenames;
            int[] in_filenames_byteCount, out_filenames_byteCount;

            string in_dir = Path.Combine(dir, "IN_FIELDS");
            string out_dir = Path.Combine(dir, "OUT_FIELDS");

            //if (buffer.Length < sizeof(int))
            //    buffer = new byte[sizeof(int)];

            //stream.Read(buffer, 0, sizeof(int));
            //basis_size = BitConverter.ToInt32(buffer, 0);

            in_filenames = new string[basis_size];
            out_filenames = new string[basis_size];
            in_filenames_byteCount = new int[in_filenames.Length];
            out_filenames_byteCount = new int[out_filenames.Length];

            in_filenames_byteCount.AsByteArray(bytes => stream.Read(bytes, 0, bytes.Length));
            out_filenames_byteCount.AsByteArray(bytes => stream.Read(bytes, 0, bytes.Length));

            int max_size = Math.Max(in_filenames_byteCount.Max<int>(), out_filenames_byteCount.Max<int>());
            if (buffer.Length < max_size)
                buffer = new byte[max_size];

            for (int i = 0; i < basis_size; i++)
            {
                stream.Read(buffer, 0, in_filenames_byteCount[i]);
                in_filenames[i] = Encoding.Unicode.GetString(buffer, 0, in_filenames_byteCount[i]);
            }

            for (int i = 0; i < basis_size; i++)
            {
                stream.Read(buffer, 0, out_filenames_byteCount[i]);
                out_filenames[i] = Encoding.Unicode.GetString(buffer, 0, out_filenames_byteCount[i]);
            }

            x_basisNF = new NearField[basis_size];
            y_basisNF = new NearField[basis_size];
            for (int i = 0; i < basis_size; i++)
            {
                NearField.op_Assign(ref x_basisNF[i], new NearField(Path.Combine(in_dir, in_filenames[i])));
                NearField.op_Assign(ref y_basisNF[i], new NearField(Path.Combine(out_dir, out_filenames[i])));
            }
        }

        private void WriteSingularNumbers(Stream stream)
        {
            wElems.AsByteArray(bytes => stream.Write(bytes, 0, bytes.Length));
        }

        private void ReadSingularNumbers(Stream stream)
        {
            wElems = new Complex[basis_size];
            wElems.AsByteArray(bytes => stream.Read(bytes, 0, bytes.Length));
        }

        private void WriteConversionCoefficients(Stream stream)
        {
            stream.Write(BitConverter.GetBytes(conv_coefs_inc.Length), 0, sizeof(int));
            stream.Write(BitConverter.GetBytes(conv_coefs_inc[0].Length), 0, sizeof(int));

            for (int i = 0; i < conv_coefs_inc.Length; i++)
                conv_coefs_inc[i].AsByteArray(bytes => stream.Write(bytes, 0, bytes.Length));
            for (int i = 0; i < conv_coefs_scat.Length; i++)
                conv_coefs_scat[i].AsByteArray(bytes => stream.Write(bytes, 0, bytes.Length));
        }

        private void ReadConversionCoefficients(Stream stream, ref byte[] buffer)
        {
            int conv_coefs_row_count, conv_coefs_column_count;

            if (buffer.Length < 2 * sizeof(int))
                buffer = new byte[2 * sizeof(int)];

            stream.Read(buffer, 0, 2 * sizeof(int));
            conv_coefs_row_count = BitConverter.ToInt32(buffer, 0);
            conv_coefs_column_count = BitConverter.ToInt32(buffer, sizeof(int));

            conv_coefs_inc = new Complex[conv_coefs_row_count][];
            conv_coefs_scat = new Complex[conv_coefs_row_count][];

            for (int i = 0; i < conv_coefs_row_count; i++)
            {
                conv_coefs_inc[i] = new Complex[conv_coefs_column_count];
                conv_coefs_inc[i].AsByteArray(bytes => stream.Read(bytes, 0, bytes.Length));
            }
            for (int i = 0; i < conv_coefs_row_count; i++)
            {
                conv_coefs_scat[i] = new Complex[conv_coefs_column_count];
                conv_coefs_scat[i].AsByteArray(bytes => stream.Read(bytes, 0, bytes.Length));
            }
        }
    }
}