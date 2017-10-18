using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Xml;
using ScatLib;
using ScatLib.WavefrontShaping;
using System.Drawing;
using System.Numerics;

namespace Focus
{
    class Program
    {
        static void Main(string[] args)
        {
            // Focus.exe [basis] [test list] [out folder] [optional: zx plane fields]
            //XmlDocument xmlDoc = new XmlDocument();
            //xmlDoc.Load("H:/Documents/PhD/KNU/Tasks/Task_14_Fields/test_points.xml");
            //XmlNodeList nodeList = xmlDoc.DocumentElement.SelectNodes("/testPoints/testPoint");

            //nodeList[0].SelectSingleNode("/x").Value

            //if (args.Length != 3)
            //{
            //    Console.WriteLine("Usage: Focus.exe c:/basis/b.basbin c:/out c:/zx_fields");
            //    return;
            //}

            // x, y, basis, out file, zx folder

            if (args.Length != 5)
            {
                Console.WriteLine("Usage: Focus.exe [x node] [y node] [basis] [out file] [zx folder]");
                Console.WriteLine("ROI size: 9x9 fixed. Binary input files allower only.");
                Console.WriteLine("Example: Focus.exe 76 76 c:/basis/b.basbin c:/out.bin c:/zx_fields");
                return;
            }

            int node_x, node_y;
            int roi_w = 9, roi_h = 9;
            string s_nodeX = args[0], s_nodeY = args[1];
            string s_basisPath = args[2];
            string s_outFile = args[3];
            string s_zxFolder = args[4];

            if (!int.TryParse(s_nodeX, out node_x) || !int.TryParse(s_nodeY, out node_y))
            {
                Console.Error.WriteLine("Node X or Y has wrong format.");
                return;
            }
            if (!File.Exists(s_basisPath))
            {
                Console.Error.WriteLine("File \"{0}\" does not exist.", s_basisPath);
                return;
            }
            if (!Directory.Exists(Path.GetDirectoryName(s_outFile)))
            {
                Console.Error.WriteLine("Directory \"{0}\" does not exist.", Path.GetDirectoryName(s_outFile));
                return;
            }
            if (Path.GetExtension(s_outFile).ToUpper() != ".BIN" && Path.GetExtension(s_outFile).ToUpper() != ".CSV")
            {
                string ext = Path.GetExtension(s_outFile).ToUpper();
                bool b = ext == ".CSV";
                Console.Error.WriteLine("Unsupported output file format. Use .bin or .csv instead.");
                return;
            }
            if (!Directory.Exists(s_zxFolder))
            {
                Console.Error.WriteLine("Directory \"{0}\" does not exist.", s_zxFolder);
                return;
            }

            Basis bas = new Basis(s_basisPath);

            string[] zxFiles = Directory.EnumerateFiles(s_zxFolder, "*.bin", SearchOption.TopDirectoryOnly).ToArray();

            if (bas.UsedFields != zxFiles.Length)
            {
                Console.Error.WriteLine("Quantities of basis ({0}) and ZX ({1}) fields are not equal.",
                    bas.UsedFields, zxFiles.Length);
                return;
            }

            NearField nf = null;
            NearField.op_Assign(ref nf, WavefrontShaping.Focus(bas, new Rectangle(node_x, node_y, roi_w, roi_h)));

            Complex[] decomp_coefs = bas.Decompose(nf, NearFieldType.Scattered);
            Complex[][] conv_coefs = bas.GetConversionCoefficients(Basis.CoefficientType.Scattered);

            NearField[] zxFields = new NearField[zxFiles.Length];

            for (int i = 0; i < zxFields.Length; i++)
                NearField.op_Assign(ref zxFields[i], new NearField(zxFiles[i]));

            NearField summary = new NearField(zxFields[0], true);

            for (int i = 0; i < conv_coefs.Length; i++)
            {
                Complex coef = Complex.Zero;

                for (int j = 0; j < conv_coefs[i].Length; j++)
                {
                    coef += conv_coefs[i][j] * decomp_coefs[j];
                }

                NearField.op_Assign(ref summary, summary + zxFields[i] * coef);
            }

            if (Path.GetExtension(s_outFile).ToUpper() == ".BIN")
            {
                summary.SaveToFile(s_outFile);
            }
            else if (Path.GetExtension(s_outFile).ToUpper() == ".CSV")
            {
                summary.Export(s_outFile);
            }

            summary.Dispose();
            bas.Dispose();
            for (int i = 0; i < zxFields.Length; i++)
                zxFields[i].Dispose();
        }
    }
}
