using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using ScatLib;

namespace BasisBuilder
{
    class Program
    {
        static void Main(string[] args)
        {
            string[] paths_x, paths_y;
            NearField[] fields_x, fields_y;
            Basis bas;

            if (args.Length != 3)
            {
                Console.WriteLine("Usage: BasisBuilder [input fields dir] [output fields dir] [output basis file .basbin]");
                Console.WriteLine("Example: BasisBuilder c:/fields_x/ c:/fields_y/ c:/basis/b.basbin");
                return;
            }
            else
            {
                for (int i = 0; i < args.Length - 1; i++)
                {
                    if (!Directory.Exists(args[i]))
                    {
                        Console.WriteLine("Directory \"{0}\" does not exist.", args[i]);
                        return;
                    }
                }
                if (!Directory.Exists(Path.GetDirectoryName(args[args.Length - 1])))
                {
                    return;
                }
            }

            paths_x = Directory.EnumerateFiles(args[0], "*.bin", SearchOption.TopDirectoryOnly).ToArray();
            paths_y = Directory.EnumerateFiles(args[1], "*.bin", SearchOption.TopDirectoryOnly).ToArray();

            if (paths_x.Length != paths_y.Length)
            {
                Console.WriteLine("Different quantity of files: input = {0}, output = {1}", paths_x.Length, paths_y.Length);
                return;
            }

            fields_x = new NearField[paths_x.Length];
            fields_y = new NearField[paths_y.Length];

            for (int i = 0; i < paths_x.Length; i++)
            {
                Console.WriteLine("{0}: {1}\t{2}", i + 1, Path.GetFileName(paths_x[i]), Path.GetFileName(paths_y[i]));
                NearField.op_Assign(ref fields_x[i], new NearField(paths_x[i]));
                NearField.op_Assign(ref fields_y[i], new NearField(paths_y[i]));
            }

            Console.WriteLine("Basis building started...");

            bas = new Basis(fields_x, fields_y);
            bas.Save(args[2]);

            for (int i = 0; i < fields_x.Length; i++)
            {
                fields_x[i].Dispose();
                fields_y[i].Dispose();
            }
            bas.Dispose();

            Console.WriteLine("Done.");
        }
    }
}
