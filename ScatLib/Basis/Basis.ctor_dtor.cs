using System;

namespace ScatLib
{
    public sealed partial class Basis : IDisposable
    {
        public Basis(string path)
        {
            Open(path);
        }

        public Basis(string[] x_inputFields, string[] y_inputFields, int f_count)
        {
            // Возможно, стоит сделать всё в блоках try-finally
            NearField[] x_fields, y_fields;

            x_fields = new NearField[x_inputFields.Length];
            y_fields = new NearField[y_inputFields.Length];

            for (int i = 0; i < x_inputFields.Length; i++)
            {
                NearField.op_Assign(ref x_fields[i], new NearField(x_inputFields[i]));
                NearField.op_Assign(ref y_fields[i], new NearField(y_inputFields[i]));
            }

            BuildBasis(x_fields, y_fields, f_count);

            for (int i = 0; i < x_fields.Length; i++)
            {
                x_fields[i].Dispose();
                y_fields[i].Dispose();
            }
        }

        public Basis(string[] x_inputFields, string[] y_inputFields)
            : this(x_inputFields, y_inputFields, x_inputFields.Length)
        {
        }

        public Basis(NearField[] x_fields, NearField[] y_fields, int f_count)
        {
            BuildBasis(x_fields, y_fields, f_count);
        }

        public Basis(NearField[] x_fields, NearField[] y_fields)
        {
            BuildBasis(x_fields, y_fields);

            used_fields = basis_size;
        }
    }
}