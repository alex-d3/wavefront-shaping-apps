using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;

namespace ScatLib
{
    public static class NearFieldExtensions
    {
        public static Complex[][] CalculateDotProductArray(this NearField[] fields)
        {
            return NearField.CalculateDotProductArray(fields);
        }

        public static Matrix<Complex> CalculateDotProductMatrix(this NearField[] fields)
        {
            return NearField.CalculateDotProductMatrix(fields);
        }
    }
}
