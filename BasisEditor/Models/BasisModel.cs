using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ScatLib;

namespace BasisEditor.Models
{
    public interface IBasisModel
    {
        void SaveBasis(string path);
    }

    public class BasisNearFieldInfo
    {
        
    }

    class BasisModel : IBasisModel
    {
        private Basis basis;

        public void RemoveField()
        {

        }

        public void SaveBasis(string path)
        {

        }
    }
}
