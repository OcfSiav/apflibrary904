using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OCF_Ws.Model
{
    public class SchedaSearchParameter 
    {
        public string Archivio { get; set; }
        public string ArchivioId { get; set; }
        public string CampoId { get; set; }
        public string IdRiferimento { get; set; }
        
        public string DataDocumento { get; set; }
        public string Oggetto { get; set; }
        public string TipologiaDocumentale { get; set; }
        public List<string> MetaDati { get; set; }

    }
}
