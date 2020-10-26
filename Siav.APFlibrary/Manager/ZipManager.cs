using Ionic.Zip;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Siav.APFlibrary.Manager
{
    class ZipManager
    {
        public bool Decompress(string sPathFileZip, string sPathDestination, string sPassword=""){
            bool bReturn = false;
            using (var zip = ZipFile.Read(sPathFileZip))
            {
                zip.Password=sPassword;
                foreach (var entry in zip.Entries)
                    entry.Extract(sPathDestination, ExtractExistingFileAction.OverwriteSilently);
            }
            
            return bReturn;

        }
    }
}
