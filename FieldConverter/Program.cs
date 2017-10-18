using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using ScatLib;
using System.Numerics;

namespace FieldConverter
{
    class Program
    {
        static void Main(string[] args)
        {
            double wavelenght = 0.0;
            string[] inputFields;
            NearField nf;
            bool useModParser = false;

            if (args.Length != 3 && args.Length != 4)
            {
                Console.WriteLine("Usage: FieldConverter [wavelength] [input dir with .dat files] [output dir]");
                Console.WriteLine("Example: FieldConverter 0.6328 c:/fields/ c:/converted/");
                return;
            }
            else
            {
                if (!double.TryParse(args[0], out wavelenght))
                {
                    Console.WriteLine("Wrong number format. Try to change the decimal separator.");
                    return;
                }

                for (int i = 1; i < (args.Length == 3 ? args.Length : args.Length - 1); i++)
                {
                    if (!Directory.Exists(args[i]))
                    {
                        Console.WriteLine("Directory \"{0}\" does not exist.", args[i]);
                        return;
                    }
                }
            }

            if (args.Length == 4 && args[3] == "-H")
            {
                useModParser = true;
            }
            
            inputFields = Directory.EnumerateFiles(args[1], "*.dat", SearchOption.TopDirectoryOnly).ToArray();
            
            for (int i = 0; i < inputFields.Length; i++)
            {
                nf = null;
                if (useModParser)
                    NearField.op_Assign(ref nf, NearField.ParseMSTM(inputFields[i], wavelenght, true));
                else
                    NearField.op_Assign(ref nf, NearField.ParseMSTM(inputFields[i], wavelenght));
                

                nf.SaveToFile(Path.Combine(args[2], Path.GetFileNameWithoutExtension(inputFields[i]) + ".bin"));
                nf.Dispose();

                Console.WriteLine("{0}: {1} \t [CONVERTED]", i + 1, Path.GetFileName(inputFields[i]));
            }
        }
    }
}
