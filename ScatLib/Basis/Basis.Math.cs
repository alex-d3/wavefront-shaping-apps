using System;
using System.Numerics;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;

namespace ScatLib
{
    public sealed partial class Basis : IDisposable
    {
        public Complex[] Decompose(NearField nf, NearFieldType sourceType)
        {
            NearField[] basisFields;
            Complex[] coefs;

            switch (sourceType)
            {
                case NearFieldType.Incident:
                    basisFields = x_basisNF;
                    break;
                case NearFieldType.Scattered:
                    basisFields = y_basisNF;
                    break;
                default:
                    basisFields = null;
                    throw new Exception("Invalid field type.");
            }

            coefs = new Complex[used_fields];

            for (int i = 0; i < used_fields; ++i)
                coefs[i] = basisFields[i].DotProduct(nf);

            return coefs;
        }

        public NearField Compose(Complex[] coef, NearFieldType sourceType)
        {
            if (coef.Length != used_fields)
                throw new Exception(string.Format("The coefficients number ({0}) is not equal to the used basis fields number ({1} of {2})",
                    coef.Length, used_fields, basis_size));

            NearField[] inputFields = null;
            NearField result = null;

            switch (sourceType)
            {
                case NearFieldType.Incident:
                    inputFields = x_basisNF;
                    break;
                case NearFieldType.Scattered:
                    inputFields = y_basisNF;
                    break;
            }

            NearField.op_Assign(ref result, new NearField(inputFields[0].NodesX, inputFields[0].NodesY,
                inputFields[0].StepX, inputFields[0].StepY, inputFields[0].MinX, inputFields[0].MinY,
                inputFields[0].Wavelength));

            for (int i = 0; i < used_fields; ++i)
                NearField.op_Assign(ref result, result +  inputFields[i] * coef[i]);

            return result;
        }

        public NearField Compose(Vector<Complex> coefs, NearFieldType sourceType, NearField[] nonBasisFields)
        {
            if (coefs.Count != used_fields)
                throw new Exception(string.Format("The coefficients number ({0}) is not equal to the used basis fields number ({1} of {2})",
                    coefs.Count, used_fields, basis_size));

            NearField result = null;
            Matrix<Complex> conversion_coefs = null;
            switch (sourceType)
            {
                case NearFieldType.Incident:
                    conversion_coefs = Matrix<Complex>.Build.DenseOfRowArrays(conv_coefs_inc);
                    break;
                case NearFieldType.Scattered:
                    conversion_coefs = Matrix<Complex>.Build.DenseOfRowArrays(conv_coefs_scat);
                    break;
            }

            NearField.op_Assign(ref result, new NearField(nonBasisFields[0].NodesX, nonBasisFields[0].NodesY,
                nonBasisFields[0].StepX, nonBasisFields[0].StepY, nonBasisFields[0].MinX, nonBasisFields[0].MinY,
                nonBasisFields[0].Wavelength));

            if (used_fields < basis_size)
                conversion_coefs = conversion_coefs.SubMatrix(0, conversion_coefs.RowCount, 0, used_fields);


            Vector<Complex> res = conversion_coefs * coefs;

            for (int i = 0; i < used_fields; ++i)
                NearField.op_Assign(ref result, result + nonBasisFields[i] * res[i]);

            return result;
        }

        // Стоит переделать
        public Complex[][] CalculateDotProduct2DArray(int x0, int y0, int width, int height, NearFieldType type)
        {
            Complex[][] roi_en_matrix;
            NearField[] fields;

            if (type == NearFieldType.Incident)
                fields = x_basisNF;
            else
                fields = y_basisNF;

            roi_en_matrix = new Complex[fields.Length][];

            for (int i = 0; i < fields.Length; i++)
                roi_en_matrix[i] = new Complex[fields.Length];

            for (int i = 0; i < fields.Length; i++)
            {
                roi_en_matrix[i] = new Complex[fields.Length];
                for (int j = 0; j <= i; j++)
                {
                    roi_en_matrix[i][j] = fields[i].DotProduct(fields[j], x0, y0, width, height);
                    if (i != j)
                        roi_en_matrix[j][i] = Complex.Conjugate(roi_en_matrix[i][j]);
                }
            }

            return roi_en_matrix;
        }

        // Стоит переделать
        public Matrix<Complex> CalculateDotProductMatrix(int x0, int y0, int width, int height, NearFieldType type)
        {
            return Matrix<Complex>.Build.DenseOfRowArrays(CalculateDotProduct2DArray(x0, y0, width, height, type));
        }
    }
}