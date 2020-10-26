using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Runtime.InteropServices;
using System.IO;
using System.Net;
using SVAOLLib;
using Siav.APFlibrary.Model;

namespace Siav.APFlibrary.Manager
{
    [ClassInterface(ClassInterfaceType.AutoDual)]
    [ComVisible(true)]
    public class DocManager : IDisposable
    {
        bool mDisposed = false; public int lErr = 0;

        public void Dispose()
        {
            
            Dispose(true);
        }
        
        private void Dispose(bool bDispoing)
        {
            if (mDisposed)
                return;

            if (bDispoing)
            {
                Console.WriteLine("sto chiamando il metodo Dispose per la classe DocManager...");
                //Supressing the Finalization method call
                GC.SuppressFinalize(this);

            }
            mDisposed = true;
        }
        static public void CopyAttachment(string stGuidConnect, string stGuidCardFrom, string stGuidCardTo, string LogId)
        {
            //Controllo se non è stato aperto già un File di Log
            bool newcon = false;
            //istanzio l'oggetto SvAol.Session
            SVAOLLib.Session oSession = new SVAOLLib.Session();
            try
            {
                if (stGuidConnect.Length != 0)
                {
                    //Assegno la stringa di connessione
                    oSession.GUIDconnect = stGuidConnect;

                    // dichiaro l'oggetto SvAol.Card per la Card
                    SVAOLLib.Card oCard = new SVAOLLib.Card();

                    //Imposto la scheda con GUID Card.
                    oCard.GuidCard = CardManager.FormatID(stGuidCardFrom, LogId);
                    oCard.GUIDconnect = stGuidConnect;
                    oCard.LoadFromGuid();
                    SVAOLLib.Attachments oAttachments = oCard.Attachments;

                    foreach(Attachment oAttachment in oAttachments ){
                        if (oAttachment.IsInternal==0){
                            Attachment oAttachmentNew = new Attachment();
                            oAttachmentNew.GUIDconnect = stGuidConnect;
                            oAttachmentNew.GuidCard = stGuidCardTo;
                            oAttachmentNew.Name = oAttachment.Name;
                            oAttachmentNew.Note = oAttachment.Note;
                            oAttachmentNew.IsInternal = 0;
                            var oAttach = oAttachment.ViewAsArray();
                            oAttachmentNew.InsertExternal(oAttach,oAttach.GetUpperBound(0) + 1,0,oAttach.GetUpperBound(0) + 1,0);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception(String.Format("{0}>>{1}>>{2}", "GetMainDoc", e.Source, e.Message), e);
            }
            finally
            {
                if (newcon) oSession.Logout();
            }
        }
		static public MainDoc GetMainDoc(string stGuidConnect, string stGuidCard, string LogId)
		{
			MainDoc oMainDoc = new MainDoc();
			//Controllo se non è stato aperto già un File di Log
			bool newcon = false;
			//istanzio l'oggetto SvAol.Session
			SVAOLLib.Session oSession = new SVAOLLib.Session();
			try
			{
				if (stGuidConnect.Length != 0)
				{
					//Assegno la stringa di connessione
					oSession.GUIDconnect = stGuidConnect;

					// dichiaro l'oggetto SvAol.Card per la Card
					SVAOLLib.Card oCard = new SVAOLLib.Card();

					//Imposto la scheda con GUID Card.
					oCard.GuidCard = CardManager.FormatID(stGuidCard, LogId);
					oCard.GUIDconnect = stGuidConnect;
					oCard.LoadFromGuid();
					SVAOLLib.Document oDocumento = oCard.Document;

					if (oDocumento.FileSize > 0)
					{
						oMainDoc.Filename = oDocumento.Name + "." + oDocumento.Extension;
						oMainDoc.Extension = oDocumento.Extension;
						if (oDocumento.IsSigned == 0 && oDocumento.IsSignedPdf == 0)
						{
							oMainDoc.IsSigned = false;

							oMainDoc.oByte = (byte[])(oDocumento.ViewAsArray(0, 0));
						}
						else
						{
							oMainDoc.IsSigned = true;
							oMainDoc.oByte = (byte[])(oDocumento.GetSignedDocumentAsArray());
						}

					}
				}
			}
			catch (Exception e)
			{
				throw new Exception(String.Format("{0}>>{1}>>{2}", "GetMainDoc", e.Source, e.Message), e);
			}
			finally
			{
				if (newcon) oSession.Logout();
			}
			return oMainDoc;
		}
		static public void CopyMainDoc(string stGuidConnect, string stGuidCardFrom, string stGuidCardTo, string LogId)
        {
            //Controllo se non è stato aperto già un File di Log
            bool newcon = false;
            //istanzio l'oggetto SvAol.Session
            SVAOLLib.Session oSession = new SVAOLLib.Session();
            try
            {
                if (stGuidConnect.Length != 0)
                {
                    //Assegno la stringa di connessione
                    oSession.GUIDconnect = stGuidConnect;

                    // dichiaro l'oggetto SvAol.Card per la Card
                    SVAOLLib.Card oCard = new SVAOLLib.Card();

                    //Imposto la scheda con GUID Card.
                    oCard.GuidCard = CardManager.FormatID(stGuidCardFrom, LogId);
                    oCard.GUIDconnect = stGuidConnect;
                    oCard.LoadFromGuid();
                    SVAOLLib.Document oDocumento = oCard.Document;

                    if (oDocumento.FileSize > 0)
                    {
                       
                        if (oDocumento.IsSigned == 0 && oDocumento.IsSignedPdf == 0)
                        {
                            SetMainDocByteArr(stGuidConnect, stGuidCardTo, (byte[])(oDocumento.ViewAsArray(0, 0)), oDocumento.Name + "." + oDocumento.Extension, LogId);
                        }
                        else
                        {
                            SetMainDocByteArr(stGuidConnect, stGuidCardTo, (byte[])(oDocumento.GetSignedDocumentAsArray()), oDocumento.Name + "." + oDocumento.Extension, LogId);
                        }
                        
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception(String.Format("{0}>>{1}>>{2}", "GetMainDoc", e.Source, e.Message), e);
            }
            finally
            {
                if (newcon) oSession.Logout();
            }
        }
	 

        public static bool SetMainDocByteArr(string stGuidConnect, string stGuidCard, byte[] docData, string sDocName, string LogId)
        {
            //Controllo se non è stato aperto già un File di Log
            SVAOLLib.Session oSession = null;
            bool newcon = false;
            bool SetMD = false; // valore da restituire

            try
            {   //controllo se sono già connesso, in caso contrario mi connetto e ritorno la stringa di connessione
                if (stGuidConnect != null)
                {
                    //istanzio gli oggetti Svaol
                    oSession = new SVAOLLib.Session();
                    SVAOLLib.Card oCard = new SVAOLLib.Card();
                    SVAOLLib.Document oDocument = null;
                    //assegno la connessione
                    oSession.GUIDconnect = stGuidConnect;
                    Object varData = new Object();
                    // Se la GUIDCard non è formattata lo faccio ora           
                    stGuidCard = CardManager.FormatID(stGuidCard, LogId);

                    //Imposto la scheda a cui aggiungere il documento
                    oCard.GUIDconnect = stGuidConnect;
                    oCard.GuidCard = stGuidCard;

                    oDocument = (SVAOLLib.Document)oCard.Document;
                    //Specifico il file da importare
                    oDocument.Name = sDocName;

                    // Imposto l'estensione
                    string ext = Path.GetExtension(sDocName);
                    oDocument.Extension = ext.Substring(1, ext.Length - 1);
                    oDocument.Insert(docData, docData.GetUpperBound(0) + 1, 0, docData.GetUpperBound(0) + 1, 1, 0);
                    SetMD = true;
                }
            }
            catch (Exception e)
            {
                throw new Exception(String.Format("{0}>>{1}>>{2}", "ERRORE : SetMainDoc", e.Source, e.Message), e);
            }
            finally
            {
                if (newcon) oSession.Logout();
            }
            return SetMD;
        }
        public static Int16 GetIdArchiveByName(string stGuidConnect, string stName, string LogId)
        {
            //Controllo se non è stato aperto già un File di Log
            Int16 iResult = 0;
            //istanzio l'oggetto SvAol.Session
            SVAOLLib.Session oSession = new SVAOLLib.Session();
            try
            {
                
                ResourceFileManager resourceFileManager = ResourceFileManager.Instance;
                resourceFileManager.SetResources();
                string sValueFromKey = "";
                if (string.IsNullOrEmpty(sValueFromKey))
                {
                    //controllo se sono già connesso, in caso contrario mi connetto e ritorno la stringa di connessione
                    oSession.GUIDconnect = stGuidConnect;

                    //Inizializzo gli oggetti SvAol necessari        
                    SVAOLLib.Archives oArchives = new SVAOLLib.Archives();

                    //Recupero tutti i Tipi documento.
                    oArchives = (SVAOLLib.Archives)oSession.GetArchives(0, 0); //il secondo 0 sta per false

                    //Cerco il Tipo Documento con nome #stName#.
                    foreach (SVAOLLib.Archive oArc in oArchives)
                    {
                        // ERU modifica // if (oDoc.Description.ToUpper() == stName)
                        if (oArc.Description.ToUpper() == stName.ToUpper())
                        {
                            iResult = oArc.Id;
                            break;
                        }
                    }
                }
                else
                    iResult = Int16.Parse(sValueFromKey);
            }
            catch (Exception e)
            {
                throw new Exception(String.Format("{0}>>{1}>>{2}", "ERRORE : GetIdArchiveByName", e.Source, e.Message), e);
            }
            return iResult;
        }

        public static Int16 GetIdDocTypeByName(string stGuidConnect, string stName, string LogId)
        {
            //Controllo se non è stato aperto già un File di Log
            Int16 iResult = 0;
            //istanzio l'oggetto SvAol.Session
            SVAOLLib.Session oSession = new SVAOLLib.Session();
            try
            {
                ResourceFileManager resourceFileManager = ResourceFileManager.Instance;
                resourceFileManager.SetResources();
				string sValueFromKey = "";
                if (string.IsNullOrEmpty(sValueFromKey))
                {
                    //controllo se sono già connesso, in caso contrario mi connetto e ritorno la stringa di connessione
                    oSession.GUIDconnect = stGuidConnect;
                    //Inizializzo gli oggetti SvAol necessari        
                    SVAOLLib.DocumentTypes oDocuments = new SVAOLLib.DocumentTypes();
                    //Recupero tutti i Tipi documento.
                    oDocuments = (SVAOLLib.DocumentTypes)oSession.GetDocTypes(0, 0); //il secondo 0 sta per false

                    //Cerco il Tipo Documento con nome #stName#.
                    foreach (SVAOLLib.DocumentType oDoc in oDocuments)
                    {
                        // ERU modifica // if (oDoc.Description.ToUpper() == stName)
                        if (oDoc.Description.ToUpper() == stName.ToUpper())
                        {
                            iResult = oDoc.Id;

                            break;
                        }
                    }
                }
                else
                    iResult = Int16.Parse(sValueFromKey);
            }
            catch (Exception e)
            {
                throw new Exception(String.Format("{0}>>{1}>>{2}", "ERRORE : GetIdDocTypeByName", e.Source, e.Message), e);
            }
            return iResult;
        }
        public static void GetAllOffices(string stGuidConnect, ref SVAOLLib.Offices oOffices, SVAOLLib.Offices result, string LogId)
        {
            //Controllo se non è stato aperto già un File di Log
            SVAOLLib.Office oUfficio = new Office();
            SVAOLLib.Office oOfficeRet = new Office();
            string nomeUfficio = "";
            SVAOLLib.Offices childoUfficio = new SVAOLLib.Offices();
            //istanzio l'oggetto SvAol.Session
            SVAOLLib.Session oSession = new SVAOLLib.Session();
            try
            {
                SVAOLLib.Office oOffice = new Office();
                //controllo se sono già connesso, in caso contrario mi connetto e ritorno la stringa di connessione
                oSession.GUIDconnect = stGuidConnect;
                //Logger.WriteOnLog(LogId, "Ho trovato n. uffici: " + oOffices.Count, 3);
                for (int i = 1; i <= oOffices.Count; i++)
                {
                    oUfficio = oOffices.Item(i);
                    nomeUfficio = oUfficio.Name;
                    //Logger.WriteOnLog(LogId, "Verifico ufficio: " + nomeUfficio, 3);
                    result.Add(oUfficio);
                    childoUfficio = oUfficio.OfficesChild;
                    //Logger.WriteOnLog(LogId, "Ho trovato n. uffici: " + childoUfficio.Count, 3);
                    if (childoUfficio.Count > 0)
                    {
                        GetAllOffices(stGuidConnect, ref childoUfficio, result, LogId);
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception(String.Format("{0}>>{1}>>{2}", "ERRORE : GetOfficeFromName", e.Source, e.Message), e);
            }
            //return oOfficeRet;
        }
  
	 
    }
}