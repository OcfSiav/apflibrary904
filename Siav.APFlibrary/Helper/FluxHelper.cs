
using Siav.APFlibrary.Manager;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Specialized;
using NPOI.HSSF.UserModel;
using NPOI.HSSF.Model;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using System.Xml;
using System.Security.Policy;
using System.Security;
using System.Security.Permissions;
using System.Reflection;
using System.Globalization;

namespace Siav.APFlibrary.Helper
{
    public class FluxHelper
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
	 public IEnumerable<X509Certificate2> EnumPdfSigners(string pdfFile = null, byte[] pdfBytes = null)
		{
			var pdfSign = new Siav.Sign.Pdf.PdfSign();
			using (pdfSign)
			{
				if (pdfFile != null)
					pdfSign.OpenFile(pdfFile);
				else if (pdfBytes != null)
					pdfSign.Open(pdfBytes);
				else
					return Enumerable.Empty<X509Certificate2>();

				return from signer in pdfSign.GetSignatures(Validate: false)
					   where signer.HasSignature
					   select signer.SignerCertificate;
			}
		}

		public Boolean GetSampleRecords(ExcelDocumentReader excelDocumentReader, string sColumnIdSubject, out List<string> lCodiceFiscale)
        {
            Boolean bResult = false;
            lCodiceFiscale=new List<string>();
            try
            {
                ResourceFileManager resourceFileManager = ResourceFileManager.Instance;
                resourceFileManager.SetResources();
                int iRowSelected = int.Parse(resourceFileManager.getConfigData("IscReadSampleCount"));
                //string sColumnIdSubject = resourceFileManager._resourceManager.GetString("IscSearchValueUniqueFromAnag");
                var columnNames = excelDocumentReader.GetColumnNames();
                //var getData = excelDocumentReader.getData<LinqToExcel.Row>();
                // Ciclo i singoli record estrapolati dal file excel
                Int16 iIndiceFile = 1;
                foreach (var a in excelDocumentReader.getData)
                {
                    // Ciclo tra i nomi delle colonne
                    foreach (var columnName in columnNames)
                    {
                        // Cerco nella lista il codice fiscale per individuare univocamente il soggetto
                        if (columnName == sColumnIdSubject) { 
                              lCodiceFiscale.Add(a[columnName].ToString());
                              break;
                        }
                    }
                    iIndiceFile += 1;
                    if (iRowSelected < iIndiceFile)
                        break;
                }
                bResult = true;
            }
            catch (Exception ex)
            { throw ex; }
            finally { }
            return bResult;
        }
        public void Convert2Pdf(string sPathDocx, out string sPathPdf)
        {
            sPathPdf = "";
            string sUrlPathConvert = string.Empty;
            try
            {
                ResourceFileManager resourceFileManager = ResourceFileManager.Instance;
                resourceFileManager.SetResources();
                ServicePointManager.ServerCertificateValidationCallback += new System.Net.Security.RemoteCertificateValidationCallback(customXertificateValidation);
                if (resourceFileManager.getConfigData("isTest") == "SI")
                    sUrlPathConvert = resourceFileManager.getConfigData("UrlServerConverterPDF");
                else
                    sUrlPathConvert = resourceFileManager.getConfigData("UrlServerConverterPDFProd");

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(sUrlPathConvert);
                // byte[] bytes;
                Byte[] bytesDocx = File.ReadAllBytes(sPathDocx);
                String file = Convert.ToBase64String(bytesDocx);
                string sBodyXml = "<?xml version=\"1.0\" encoding=\"utf-8\"?><CONVERT_DOCUMENT xmlns:dt=\"urn:schemas-microsoft-com:datatypes\"><EXT dt:dt=\"string\">DOCX</EXT><DOCUMENT dt:dt=\"bin.base64\">" + file +
                                    "</DOCUMENT></CONVERT_DOCUMENT>";
                //
                byte[] requestBytes = System.Text.Encoding.ASCII.GetBytes(sBodyXml);
                request.Method = "POST";
                request.ContentType = "text/xml;charset=utf-8";
                request.ContentLength = requestBytes.Length;
                Stream requestStream = request.GetRequestStream();
                requestStream.Write(requestBytes, 0, requestBytes.Length);
                requestStream.Close();

                HttpWebResponse res = (HttpWebResponse)request.GetResponse();
                StreamReader sr = new StreamReader(res.GetResponseStream(), System.Text.Encoding.Default);
                string backstr = sr.ReadToEnd();
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(backstr);
                XmlNode node = doc.DocumentElement.SelectSingleNode("/CONVERTED_DOCUMENT");
                sPathPdf = Path.GetDirectoryName(sPathDocx) + @"\" + Guid.NewGuid() + ".pdf";
                File.WriteAllBytes(sPathPdf, Convert.FromBase64String(node.InnerText));
            }
            catch (Exception ex)
            { throw ex; }
            finally { }
        }
        public Boolean GetSetTerFromAnag(ExcelDocumentReader excelDocumentReader, string sIdAnag, out string sValueAnag)
        {
            Boolean bResult = false;
            sValueAnag = "";
            try
            {
                ResourceFileManager resourceFileManager = ResourceFileManager.Instance;
                resourceFileManager.SetResources();
                var columnNames = excelDocumentReader.GetColumnNames();
                //var getData = excelDocumentReader.getData<LinqToExcel.Row>();
                // Ciclo i singoli record estrapolati dal file excel
                foreach (var a in excelDocumentReader.getData)
                {
                    // Cerco nella lista il codice fiscale per individuare univocamente il soggetto
                    if (a[resourceFileManager.getConfigData("CancAnagCodOCFNameField")].ToString() == sIdAnag)
                    {
                        sValueAnag = a[resourceFileManager.getConfigData("CancAnagSezTerNameField")].ToString();
                        break;
                    }
                }
                bResult = true;
            }
            catch (Exception ex)
            { throw ex; }
            finally { }
            return bResult;
        }
        public Boolean GetDataFromAnag(ExcelDocumentReader excelDocumentReader, string sColumnName, string sIdAnag, out string sValueAnag)
        {
            Boolean bResult = false;
            sValueAnag = "";
            try
            {
                ResourceFileManager resourceFileManager = ResourceFileManager.Instance;
                resourceFileManager.SetResources();
                var columnNames = excelDocumentReader.GetColumnNames();
                //var getData = excelDocumentReader.getData<LinqToExcel.Row>();
                // Ciclo i singoli record estrapolati dal file excel
                foreach (var a in excelDocumentReader.getData)
                {
                    // Cerco nella lista il codice fiscale per individuare univocamente il soggetto
                    if (a[resourceFileManager.getConfigData("CancAnagCodOCFNameField")].ToString() == sIdAnag)
                    {
                        sValueAnag = a[a.ColumnNames.Count() + int.Parse(resourceFileManager.getConfigData(sColumnName))].ToString();
                        break;
                    }
                }
                bResult = true;
            }
            catch (Exception ex)
            { throw ex; }
            finally { }
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
        public Boolean GetCsvRecordKV(ExcelDocumentReader excelDocumentReader, string sColumnIdSubject, string cfFilter, out NameValueCollection lfieldXls)
        {
            Boolean bResult = false;
            lfieldXls = new NameValueCollection();
            try
            {
                ResourceFileManager resourceFileManager = ResourceFileManager.Instance;
                resourceFileManager.SetResources();
                var columnNames = excelDocumentReader.GetColumnNames();
                //var getData = excelDocumentReader.getData<LinqToExcel.Row>();
                // Ciclo i singoli record estrapolati dal file excel
                var listColumns = columnNames.Cast<string>().ToList();
                foreach (var a in excelDocumentReader.getData)
                {
                    // Ciclo tra i nomi delle colonne
                    foreach (var columnName in columnNames)
                    {
                        // Cerco nella lista il codice fiscale per individuare univocamente il soggetto
                        if (columnName.ToUpper().Trim() == sColumnIdSubject.ToUpper().Trim())
                        {
                            if (a[columnName].ToString().ToUpper().Trim() == cfFilter.ToUpper().Trim())
                            {
                                for (int i = 0; i < a.Count; i++) {
                                    DateTime dateValue;
                                    if (DateTime.TryParseExact(a[i].ToString(), "dd/MM/yyyy hh:mm:ss", new CultureInfo("it-IT"),DateTimeStyles.None,out dateValue))
                                        lfieldXls.Add(listColumns[i].ToString().Replace(" ", "_"), a[i].ToString().Substring(0,10));
                                    else
                                        lfieldXls.Add(listColumns[i].ToString().Replace(" ", "_"), a[i].ToString());
                                }
                                    
                            }
                            break;
                        }
                    }
                    if (lfieldXls.Count>0)
                        break;
                }
                bResult = true;
            }
            catch (Exception ex)
            { throw ex; }
            finally { }
            return bResult;
        }
        public Boolean GetCsvRecord(ExcelDocumentReader excelDocumentReader,string sColumnIdSubject, string cfFilter, out string sVauleAnag)
        {
            Boolean bResult = false;
            sVauleAnag = "";
            try
            {
                ResourceFileManager resourceFileManager = ResourceFileManager.Instance;
                resourceFileManager.SetResources();
                var columnNames = excelDocumentReader.GetColumnNames();
                //var getData = excelDocumentReader.getData<LinqToExcel.Row>();
                // Ciclo i singoli record estrapolati dal file excel
                var listColumns = columnNames.Cast<string>().ToList();
                foreach (var a in excelDocumentReader.getData)
                {
                    // Ciclo tra i nomi delle colonne
                    foreach (var columnName in columnNames)
                    {
                        // Cerco nella lista il codice fiscale per individuare univocamente il soggetto
                        if (columnName == sColumnIdSubject)
                        {
                            if (a[columnName].ToString() == cfFilter)
                            {
                                for (int i = 0; i < a.Count; i++)
                                    sVauleAnag += listColumns[i].ToString() + "|" + a[i].ToString() + "|";
                            } 
                            break;
                        }
                    }
                    if (sVauleAnag !="")
                        break;
                }
                bResult = true;
            }
            catch (Exception ex)
            { throw ex; }
            finally { }
            return bResult;
        }

        public NameValueCollection CreateDocxMassiveCanc(ExcelDocumentReader excelDocumentReader, NameValueCollection collectionForReplace, string sPathTemplateDocx1, string sPathTemplateDocx2, string sColumnIdName, out string errTags)
        {
            NameValueCollection oResult = new NameValueCollection();
            try
            {
                errTags = "";
                string cfSelected = string.Empty;
                var columnNames = excelDocumentReader.GetColumnNames();
                ResourceFileManager resourceFileManager = ResourceFileManager.Instance;
                resourceFileManager.SetResources();
                string sColumnIdSubject = resourceFileManager.getConfigData(sColumnIdName);
                //var getData = excelDocumentReader.getData<LinqToExcel.Row>();
                // Ciclo i singoli record estrapolati dal file excel
                Int16 iIndiceFile = 1;
                foreach (var a in excelDocumentReader.getData)
                {
                    if (a[resourceFileManager.getConfigData("CancAnagSezTerNameField")].ToString() == resourceFileManager.getConfigData("CancAnagSez1TerValueField"))
                    {
                    //Apro il FileStyleUriParser template DocX da valorizzare
                        using (Novacode.DocX document = Novacode.DocX.Load(sPathTemplateDocx1))
                        {
                            // Ciclo tra i nomi delle colonne
                            foreach (var columnName in columnNames)
                            {
                                // sostituisco il valore dei nomi colonne formattandole come TAG che sia suppone sia presente nel template DOCX
                                document.ReplaceText("«" + columnName + "»", a[columnName].ToString());
                                if (columnName == sColumnIdSubject)
                                {
                                    cfSelected = a[columnName].ToString();
                                }
                            }
                            for(int i=0;i<collectionForReplace.Count;i++)
                                document.ReplaceText("«" + collectionForReplace.GetKey(i) + "»", collectionForReplace.GetValues(i)[0]);
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
                                document.SaveAs(excelDocumentReader.sTransactionPath + iIndiceFile + ".docx");
                                oResult.Add(cfSelected,excelDocumentReader.sTransactionPath + iIndiceFile + ".docx");
                            }
                        }
                    }
                    else if (a[resourceFileManager.getConfigData("CancAnagSezTerNameField")].ToString() == resourceFileManager.getConfigData("CancAnagSez2TerValueField"))
                    {
                        //Apro il FileStyleUriParser template DocX da valorizzare
                        using (Novacode.DocX document = Novacode.DocX.Load(sPathTemplateDocx2))
                        {
                            // Ciclo tra i nomi delle colonne
                            foreach (var columnName in columnNames)
                            {
                                // sostituisco il valore dei nomi colonne formattandole come TAG che sia suppone sia presente nel template DOCX
                                document.ReplaceText("«" + columnName + "»", a[columnName].ToString());
                                if (columnName == sColumnIdSubject)
                                {
                                    cfSelected = a[columnName].ToString();
                                }
                            }
                            for(int i=0;i<collectionForReplace.Count;i++)
                                document.ReplaceText("«" + collectionForReplace.GetKey(i) + "»", collectionForReplace.GetValues(i)[0]);
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
                                document.SaveAs(excelDocumentReader.sTransactionPath + iIndiceFile + ".docx");
                                oResult.Add(cfSelected,excelDocumentReader.sTransactionPath + iIndiceFile + ".docx");
                            }
                        }
                    }
                    else if (!string.IsNullOrEmpty(a[resourceFileManager.getConfigData("CancAnagSezTerNameField")].ToString()))
                        throw new ArgumentException ("Valore Sezione territoriale non riconosciuto.");
                    iIndiceFile += 1;
                }
            }
            catch (Exception ex)
            { throw new ArgumentException (ex.Message); }
            finally { }
            return oResult;
        }
        public NameValueCollection CreateDocxMassive(ExcelDocumentReader excelDocumentReader, NameValueCollection collectionForReplace, string sPathTemplateDocx,string sColumnIdName, out string errTags)
        {
            NameValueCollection oResult = new NameValueCollection();
            try
            {
                errTags = "";
                string cfSelected = string.Empty;
                var columnNames = excelDocumentReader.GetColumnNames();
                ResourceFileManager resourceFileManager = ResourceFileManager.Instance;
                resourceFileManager.SetResources();
                string sColumnIdSubject = resourceFileManager.getConfigData(sColumnIdName);
                //var getData = excelDocumentReader.getData<LinqToExcel.Row>();
                // Ciclo i singoli record estrapolati dal file excel
                Int16 iIndiceFile = 1;
                foreach (var a in excelDocumentReader.getData)
                {
                    //Apro il FileStyleUriParser template DocX da valorizzare
                    using (Novacode.DocX document = Novacode.DocX.Load(sPathTemplateDocx))
                    {
                        // Ciclo tra i nomi delle colonne
                        foreach (var columnName in columnNames)
                        {
                            // sostituisco il valore dei nomi colonne formattandole come TAG che sia suppone sia presente nel template DOCX
                            document.ReplaceText("«" + columnName + "»", a[columnName].ToString());
                            if (columnName == sColumnIdSubject)
                            {
                                cfSelected = a[columnName].ToString();
                            }
                        }
                        for(int i=0;i<collectionForReplace.Count;i++)
                            document.ReplaceText("«" + collectionForReplace.GetKey(i) + "»", collectionForReplace.GetValues(i)[0]);
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
                            document.SaveAs(excelDocumentReader.sTransactionPath + iIndiceFile + ".docx");
                            oResult.Add(cfSelected,excelDocumentReader.sTransactionPath + iIndiceFile + ".docx");
                        }
                    }
                    iIndiceFile += 1;
                }
            }
            catch (Exception ex)
            { throw new ArgumentException (ex.Message); }
            finally { }
            return oResult;
        }

        public string CreateReportMassive(string path, string idComunicazioneMax, List<String> lData)
        {
            NameValueCollection oResult = new NameValueCollection();
            string pathfilename = "";
            try
            {
                Guid filename = new Guid();
                // Create and display the value of two GUIDs.
                filename = Guid.NewGuid();
                pathfilename = path + @"\" + filename.ToString() + ".xls";

                HSSFWorkbook wb;
                HSSFSheet sh;
                wb = HSSFWorkbook.Create(InternalWorkbook.CreateWorkbook());

                // create sheet
                sh = (HSSFSheet)wb.CreateSheet(idComunicazioneMax.Replace("/","_"));
                string[] arrData=null;
                int iRowExc = 0;
                var headerCellStyle = wb.CreateCellStyle();
                var detailSubtotalFont = wb.CreateFont();
                detailSubtotalFont.Boldweight = (short)NPOI.SS.UserModel.FontBoldWeight.Bold;
                //headerCellStyle.FillForegroundColor= NPOI.SS.Util.HSSFColor.AQUA.index;
                headerCellStyle.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Center;
                headerCellStyle.BorderBottom = NPOI.SS.UserModel.BorderStyle.Medium;
                headerCellStyle.BorderLeft = NPOI.SS.UserModel.BorderStyle.Medium;
                headerCellStyle.BorderRight = NPOI.SS.UserModel.BorderStyle.Medium;
                headerCellStyle.SetFont(detailSubtotalFont);
                var detailSubtotalFontLight = wb.CreateFont();
                var DataCellStyle = wb.CreateCellStyle();
                detailSubtotalFontLight.Boldweight = (short)NPOI.SS.UserModel.FontBoldWeight.Normal;
                //                      DataCellStyle.FillForegroundColor = NPOI.SS.Util.HSSFColor.AQUA.index;
                DataCellStyle.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Left;
                DataCellStyle.BorderTop = NPOI.SS.UserModel.BorderStyle.Thin;
                DataCellStyle.BorderBottom = NPOI.SS.UserModel.BorderStyle.Thin;
                DataCellStyle.BorderLeft = NPOI.SS.UserModel.BorderStyle.Thin;
                DataCellStyle.BorderRight = NPOI.SS.UserModel.BorderStyle.Thin;

                DataCellStyle.SetFont(detailSubtotalFontLight);
                                                
                for (int iRow = 0; iRow < lData.Count; iRow++)
                {
                    var r = sh.CreateRow(iRowExc);
                    string sData = lData[iRow];
                    arrData = sData.Split('|');
                    int iRowListValue=1;
                    if (iRow == 0)
                    {
                        int iHeader=0;

                        for(int iColumn=0; iColumn<arrData.Length/2;iColumn++){
                            var newCell = r.CreateCell(iColumn);
                            newCell.SetCellValue(arrData[iHeader]);
                            newCell.SetCellType(NPOI.SS.UserModel.CellType.String);
                            newCell.CellStyle = headerCellStyle;
                            iHeader = iHeader + 2;
                        }
                        iRowExc++;
                        r = sh.CreateRow(iRowExc);
                    }
                    for(int iColumn=0; iColumn<arrData.Length/2;iColumn++){
                        var newCell = r.CreateCell(iColumn);
                        newCell.SetCellValue(arrData[iRowListValue]);
                        newCell.SetCellType(NPOI.SS.UserModel.CellType.String);
                        newCell.CellStyle = DataCellStyle;
                        iRowListValue = iRowListValue + 2;
                    }
                    iRowExc++;
                }
                using (var fs = new FileStream(pathfilename, FileMode.Create, FileAccess.Write))
                {
                    for (int iColumn = 0; iColumn < arrData.Length / 2; iColumn++) {
                        sh.AutoSizeColumn(iColumn);
                    }
                    
                    wb.Write(fs);
                    fs.Close();
                }
            }
            catch (Exception ex)
            { throw new ArgumentException(ex.Message); }
            finally { }
            return pathfilename;
        }
        public static AppDomain CreateWorkerAppDomain()
        {
            const string BaseName = "ExcelDocumentReader";

            string defDirectory = Path.GetDirectoryName(new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath);
            string configFileName = Path.Combine(defDirectory, "Siav.APFlibrary.dll.config");

            // 

            AppDomainSetup setup = AppDomain.CurrentDomain.SetupInformation;

            Evidence evidence = AppDomain.CurrentDomain.Evidence;

            var permissions = new PermissionSet(PermissionState.Unrestricted);
            permissions.AddPermission(new SecurityPermission(SecurityPermissionFlag.AllFlags/*.Execution*/));
            permissions.AddPermission(new UIPermission(PermissionState.Unrestricted));
            permissions.AddPermission(new FileIOPermission(PermissionState.None) { AllFiles = FileIOPermissionAccess.AllAccess });

            setup.ApplicationBase = defDirectory;
            setup.ConfigurationFile = configFileName;

            // create the app domain 
            return AppDomain.CreateDomain(BaseName, evidence, setup, permissions);
        }
    }
}
