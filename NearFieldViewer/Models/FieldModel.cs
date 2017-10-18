using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ScatLib;

namespace NearFieldViewer.Models
{
    public class FieldModel
    {
        private NearField nf;

        public NearField Field
        {
            get
            {
                return nf;
            }
            set
            {
                nf = value;
            }
        }
    }
}
