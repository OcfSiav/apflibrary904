using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OCF_Ws.Model
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    namespace Siav.APFlibrary.Model
    {
        public class FieldsCard
        {
            public string Archivio { get; set; }
            public string CampoId { get; set; }
            public string DataDocumento { get; set; }
            public string Oggetto { get; set; }
            public string TipologiaDocumentale { get; set; }
            public List<string> MetaDati { get; set; }

        }
    }

}
