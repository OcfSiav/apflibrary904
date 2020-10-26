using Novacode;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestLibrary
{
    public class FluxHelper
    {
        public Boolean CreateDocxMassive(string sNameFileDocx, string sNameFileXls, out string errTags)
        {
            Boolean bResult = false;
            try
            {
                ResourceFileManager resourceFileManager = ResourceFileManager.Instance;
                errTags = "";
                resourceFileManager.SetResources();
                string sPathWork = resourceFileManager._resourceManager.GetString("ExcelWorkFolder"); 
                ExcelDocumentReader excelDocumentReader = new ExcelDocumentReader(sNameFileXls);
                var columnNames = excelDocumentReader.GetColumnNames();
                //var getData = excelDocumentReader.getData<LinqToExcel.Row>();
                // Ciclo i singoli record estrapolati dal file excel
                Int16 iIndiceFile = 1;
                foreach (var a in excelDocumentReader.getData)
                {
                    //Apro il FileStyleUriParser template DocX da valorizzare
                    using (DocX document = DocX.Load(@sPathWork + sNameFileDocx))
                    {
                        // Ciclo tra i nomi delle colonne
                        foreach (var columnName in columnNames)
                        {
                            // sostituisco il valore dei nomi colonne formattandole come TAG che sia suppone sia presente nel template DOCX
                            document.ReplaceText("«" + columnName + "»", a[columnName].ToString());
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
                            break;
                        }
                        else
                        {
                            document.SaveAs(excelDocumentReader.sTransactionPath + iIndiceFile + ".docx");
                        }
                    }
                    iIndiceFile += 1;
                }
                bResult = true;
            }
            catch (Exception ex)
            { throw ex; }
            finally { }
            return bResult;
        }
            



    }
}
