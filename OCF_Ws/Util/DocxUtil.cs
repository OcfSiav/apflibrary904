
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Specialized;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using System.Xml;
using System.Security.Policy;
using System.Security;
using System.Security.Permissions;
using System.Reflection;
using System.Globalization;
using Xceed.Words.NET;

namespace OCF_Ws.Util
{
    public class DocxUtil
	{
        public string WorkingFolder { get; set; }

        private static bool customXertificateValidation(object sender, X509Certificate cert, X509Chain chain, SslPolicyErrors error)
        {
            var certificate = (X509Certificate2)cert;
            return true;
        }
        public Boolean FileMaterialize(string path, Byte[]oByte)
        {
            bool bResult = false;
            try
            {
                // Delete the file if it exists.
                if (File.Exists(path))
                {
                    File.Delete(path);
                }

                // Create the file.
                using (FileStream fs = File.Create(path))
                {
                    // Add some information to the file.
                    fs.Write(oByte, 0, oByte.Length);
                    bResult= true;
                }
            }
            catch (Exception ex)
            {
                throw new ArgumentException(ex.Message);
            }
            return bResult;
        }

        

        public Boolean CheckWordInPhrase(string sPhrase, string sWordToCheck)
        {
            string[] stringArrayToCheck = sWordToCheck.Split(' ');
            bool bFoundAll = true;
            foreach (string x in stringArrayToCheck)
            {
                if (!sPhrase.ToUpper().Contains(x.ToUpper()))
                {
                    bFoundAll = false;
                    break;
                    // Process...
                }
            }
            return bFoundAll;
        }

        public string CreateDocx(NameValueCollection collectionForReplace, string sPathTemplateDocx, LOLIB oLogger, string LogId, out string errTags)
        {
			string sPath = "";
            try
            {
                errTags = "";
                string cfSelected = string.Empty;
                using (DocX document = DocX.Load(sPathTemplateDocx))
                {
                    for(int i=0;i<collectionForReplace.Count;i++)
					{
						oLogger.WriteOnLog(LogId, "REPLACE Nome campo: " + "«" + collectionForReplace.GetKey(i) + "»" + " -> " + collectionForReplace.GetValues(i)[0], 3);
						document.ReplaceText("«" + collectionForReplace.GetKey(i) + "»", collectionForReplace.GetValues(i)[0]);
					}
                    // Verifico che tutti i TAG presenti all'interno del file docx siano stati valorizzati
                    var lineBreaks = document.FindUniqueByPattern("«*\\b[^»]*»", System.Text.RegularExpressions.RegexOptions.None);
                    // Qualora siano presenti restituisco i tag non ripristinati come segnalazione di una anomalia
                    if (lineBreaks.Count > 0)
                    {
                        foreach (var tagErr in lineBreaks)
                        {
                            errTags += tagErr + " ";
                        }
                        throw new ArgumentException ("Sono stati trovati i seguenti tag non sostituiti: " + errTags);
                        //break;
                    }
                    else
					{
						sPath = sPathTemplateDocx.ToLower().Replace(".docx", "_Completed.docx");
						document.SaveAs(sPath);
                    }
                }
            }
            catch (Exception ex)
            { throw new ArgumentException (ex.Message); }
            finally { }
            return sPath;
        }
    }
}
