using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Siav.APFlibrary.Model
{
    public class PersonaDaVerificare
    {
        public string Nome { get; set; }
        public string Cognome { get; set; }
        public string DataNascita { get; set; }
        public string Genere { get; set; }
        public string CodiceFiscale { get; set; }
        
        public PersonaDaVerificare(string sNome, string sCognome,string sDataNascita,string sGenere, string sCodiceFiscale){
            Nome= sNome;
            Cognome= sCognome;
            DataNascita= sDataNascita;
            Genere=sGenere;
            CodiceFiscale= sCodiceFiscale;
        }
    }
   
}
