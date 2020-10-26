using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.IO;
using System.Globalization;
using System.Collections;
using System.Xml;
using System.Xml.Serialization;
using OCF_Ws.Util;

namespace OCF_Ws.Manager
{

    [ClassInterface(ClassInterfaceType.AutoDual)]
    [ComVisible(true)]

    public class CardManager : IDisposable
    {
        bool mDisposed = false; public int lErr = 0;
        public SVAOLLib.Card thisCard { get; set; }
		public LOLIB _Logger;
		public string _sLogId;
		ResourceFileManager resourceFileManager;

		public CardManager(LOLIB Logger, string sLogId,string card="",string connection="")
		{
			resourceFileManager = ResourceFileManager.Instance;
			resourceFileManager.SetResources();

			_sLogId = sLogId;
			_Logger = Logger;
			if (card != "" && connection != "")
            {
                SVAOLLib.Card sampleCard = new SVAOLLib.Card();
                sampleCard.GUIDconnect = connection;
                sampleCard.GuidCard = card;
                sampleCard.LoadFromGuid();
                thisCard = sampleCard;
            }
        }

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
                Console.WriteLine("sto chiamando il metodo Dispose per la classe CardManager...");
                //Supressing the Finalization method call
                GC.SuppressFinalize(this);

            }
            mDisposed = true;
        }


		public List<dynamic> GetIndiciScheda(string stGuidConnect, string sGuidCard)
		{
			List<dynamic> oModelCard = new List<dynamic>();
			dynamic index="";
			index = new DynamicDictionary();
			SVAOLLib.Card oCard;
			
			oCard = new SVAOLLib.Card();
			SVAOLLib.Session oSession = new SVAOLLib.Session();
			oSession.GUIDconnect = stGuidConnect;
			oCard.GUIDconnect = stGuidConnect;
			oCard.GuidCard = _Logger.FormatID(sGuidCard);
			oCard.LoadFromGuid();
			
			List<string> IndiciScheda = new List<string>();
			string[] buffer = new string[21];
			IndiciScheda = buffer.ToList();
			foreach (SVAOLLib.Field oField in (SVAOLLib.Fields)oCard.Fields)
			{
				index.id = oField.Id;
				index.description = oField.Description;
				if (oField.Value != null)
					index.value = oField.Value.ToString();
				else
					index.value = "";
				oModelCard.Add(index);
			}
			return oModelCard;
		}

		public List<dynamic> GetAttachmentExt(string stGuidConnect, SVAOLLib.Card gCard, List<string> idAttachments, Boolean addBinary = true)
		{
			//Controllo se non è stato aperto già un File di Log
			bool newcon = false;
			//controllo se sono già connesso, in caso contrario mi connetto e ritorno la stringa di connessione
			SVAOLLib.Session oSession = new SVAOLLib.Session();
			oSession.GUIDconnect = stGuidConnect;
			SVAOLLib.Attachment oAttachmentFrom = new SVAOLLib.Attachment();
			oAttachmentFrom.GUIDconnect = stGuidConnect;
			oAttachmentFrom.GuidCard = gCard.GuidCard;

			dynamic objAttachment;
			Object aBinaryData = new Object();
			List<dynamic> objAttachments = new List<dynamic>();
			try
			{
				int iCount;
				object[,] attachments = (object[,])gCard.AttachmentsAsArray;
				for (iCount = 1; iCount < attachments.GetLength(0); iCount++)
				{
					if (Convert.ToInt32(attachments[iCount, 3]) == 0)
					{
						objAttachment = new DynamicDictionary();
						objAttachment.id = attachments[iCount, 1].ToString();
						objAttachment.nomefile = attachments[iCount, 4].ToString();
						objAttachment.note = attachments[iCount, 2].ToString();
						objAttachment.binarycontent = Convert.ToBase64String((byte[])(aBinaryData));
						objAttachments.Add(objAttachment);
					}
				}
				return objAttachments;
			}
			catch (Exception e)
			{
				_Logger.WriteOnLog(_sLogId, "ERRORE NELL'ESECUZIONE DI : GetAttachmentExt", 1);
				_Logger.WriteOnLog(_sLogId, e.Source + "  " + e.Message, 1);
				return null;
				throw new Exception(String.Format("{0}>>{1}>>{2}", "GetAttachmentExt", e.Source, e.Message), e);
			}
			finally
			{
				if (newcon) oSession.Logout();
			}
		}

		public string GetArchiveNameByCode(string stGuidConnect, short Code)
		{
			//Controllo se non è stato aperto già un File di Log
			string sArchiveName ="";
			//istanzio l'oggetto SvAol.Session
			SVAOLLib.Session oSession = new SVAOLLib.Session();
			try
			{
				oSession.GUIDconnect = stGuidConnect;
				//Inizializzo gli oggetti SvAol.
				SVAOLLib.Archives oArchives = new SVAOLLib.Archives();
				//Recupero tutti gli Archivi.
				oArchives = (SVAOLLib.Archives)oSession.GetArchives(0, 0);//il secondo argomento è false
				//Cerco l'archivio con nome #stName#.
				foreach (SVAOLLib.Archive oArchive in oArchives)
				{
					if (oArchive.Id == Code)
					{
						_Logger.WriteOnLog(_sLogId, "NomeArchivio: " + _Logger.ToJson(oArchive.Description), 3);
						sArchiveName = oArchive.Description;
						break;
					}
				}
			}
			catch (Exception e)
			{
				_Logger.WriteOnLog(_sLogId, e.StackTrace, 1);
				_Logger.WriteOnLog(_sLogId, "ERRORE NELL'ESECUZIONE DI : GetArchiveByName", 1);
				_Logger.WriteOnLog(_sLogId, e.Source + " " + e.Message, 1);
				
				throw new Exception(String.Format("{0}>>{1}>>{2}", "ERRORE : GetArchiveByName", e.Source, e.Message), e);
			}
			finally
			{
			}
			return sArchiveName;
		}
		public string GetTypeDocumentNameByCode(string stGuidConnect, short Code)
		{
			string sNameDocumentType = "";
			//istanzio l'oggetto SvAol.Session
			SVAOLLib.Session oSession = new SVAOLLib.Session();
			try
			{
				oSession.GUIDconnect = stGuidConnect;
				//Inizializzo gli oggetti SvAol.
				SVAOLLib.DocumentTypes oArchiveDocumentTypes = new SVAOLLib.DocumentTypes();
				//Recupero tutti gli Archivi.
				oArchiveDocumentTypes = (SVAOLLib.DocumentTypes)oSession.GetDocTypes(0, 0);//il secondo argomento è false
																		  //Cerco l'archivio con nome #stName#.
				foreach (SVAOLLib.DocumentType oDocumentType in oArchiveDocumentTypes)
				{
					if (oDocumentType.Id == Code)
					{
						sNameDocumentType = oDocumentType.Description;
						break;
					}
				}
			}
			catch (Exception e)
			{
				_Logger.WriteOnLog(_sLogId, "ERRORE NELL'ESECUZIONE DI : GetTypeDocumentNameByCode", 1);
				_Logger.WriteOnLog(_sLogId, e.Source + " " + e.Message, 1);
				throw new Exception(String.Format("{0}>>{1}>>{2}", "ERRORE : GetTypeDocumentNameByCode", e.Source, e.Message), e);
			}
			finally
			{
			}
			return sNameDocumentType;
		}
		/*
		  public bool setCardV1(string strConnection, Int16 idTipoDocumento, Int16 idTipoArchivio, string sDateReceivementDoc, string sEsito , comunicazioneDatiContatto deserializedVisEntity,ref string LogId, out string sGuidCard)
		  {
			  bool bInsertResult = false;
			  try
			  {
				  SVAOLLib.Offices oMailOffices = new SVAOLLib.Offices();
				  SVAOLLib.Offices oOffices = new SVAOLLib.Offices();
				  SVAOLLib.Groups oMailGroups = new SVAOLLib.Groups();
				  SVAOLLib.Groups oGroups = new SVAOLLib.Groups();

				  SVAOLLib.Users oMailUsers = new SVAOLLib.Users();
				  SVAOLLib.Users oUsers = new SVAOLLib.Users();

				  SVAOLLib.Card oCard = new SVAOLLib.Card();
				  SVAOLLib.Session oSession = new SVAOLLib.Session();
				  SVAOLLib.Fields oFields = new SVAOLLib.Fields();
				  SVAOLLib.Archive oArchive = new SVAOLLib.Archive();
				  SVAOLLib.DocumentType oTypeDocument = new SVAOLLib.DocumentType();

				  oTypeDocument.Id = idTipoDocumento;
				  oTypeDocument.GUIDconnect = strConnection;
				  oTypeDocument.LoadDocumentTypeFromId();
				  oArchive.GUIDconnect = strConnection;
				  oArchive.Id = idTipoArchivio;
				  oArchive.LoadFromId();
				  if (string.IsNullOrEmpty(sEsito))
					  sEsito = "0";
				  foreach (SVAOLLib.Field oField in oTypeDocument.Fields)
				  {
					  if (oField.Id == SVAOLLib.svIdField.svIfKey11)
					  {
						  oField.Value = deserializedVisEntity.tipo_autenticazione.ToString();
						  if (sEsito.Substring(0, 1) != "1")
							  oField.Value = "";
						  _Logger.WriteOnLog(LogId, "tipo_autenticazione: " + oField.Value, 3);
					  }
					  else if (oField.Id == SVAOLLib.svIdField.svIfKey12)
					  {
						  oField.Value = resourceFileManager.getConfigData(deserializedVisEntity.TitolareResponsabileTrattamento.Censimento.ToString());
						  if (sEsito.Substring(0, 1) != "1")
							  oField.Value = "";

						  _Logger.WriteOnLog(LogId, "TitolareResponsabileTrattamento.Censimento: " + oField.Value, 3);
					  }
					  else if (oField.Id == SVAOLLib.svIdField.svIfKey13)
					  {
						  oField.Value = deserializedVisEntity.TitolareResponsabileTrattamento.tit_res.Denominazione;
						  _Logger.WriteOnLog(LogId, "TitolareResponsabileTrattamento.tit_res.Denominazione: " + oField.Value, 3);
					  }
					  else if (oField.Id == SVAOLLib.svIdField.svIfKey14)
					  {
						  oField.Value = deserializedVisEntity.TitolareResponsabileTrattamento.tit_res.cfpiva;
						  _Logger.WriteOnLog(LogId, "TitolareResponsabileTrattamento.tit_res.cfpiva: " + oField.Value, 3);
					  }
					  else if (oField.Id == SVAOLLib.svIdField.svIfKey15)
					  {
						  if (deserializedVisEntity.ResponsabileProtezioneDati.tipo_dpo == null || sEsito.Substring(0, 1) != "1")
						  {
							  oField.Value = "";
						  }
						  else
							  oField.Value = deserializedVisEntity.ResponsabileProtezioneDati.tipo_dpo.ToString();
						  _Logger.WriteOnLog(LogId, "ResponsabileProtezioneDati.tipo_dpo: " + oField.Value, 3);
					  }
					  else if (oField.Id == SVAOLLib.svIdField.svIfKey21)
					  {
						  if (deserializedVisEntity.ResponsabileProtezioneDati.tipo_dpo_persona == null || sEsito.Substring(0, 1) != "1")
						  {
							  oField.Value = "";
						  }
						  else
							  oField.Value = resourceFileManager.getConfigData(deserializedVisEntity.ResponsabileProtezioneDati.tipo_dpo_persona.ToString());
						  _Logger.WriteOnLog(LogId, "ResponsabileProtezioneDati.tipo_dpo_persona: " + oField.Value, 3);
					  }
					  else if (oField.Id == SVAOLLib.svIdField.svIfKey22)
					  {
						  oField.Value = deserializedVisEntity.ResponsabileProtezioneDati.dpo_PersonaGiuridica.Denominazione;
						  _Logger.WriteOnLog(LogId, "ResponsabileProtezioneDati.dpo_PersonaGiuridica.Denominazione: " + oField.Value, 3);
					  }
					  else if (oField.Id == SVAOLLib.svIdField.svIfKey23)
					  {
						  oField.Value = deserializedVisEntity.ResponsabileProtezioneDati.dpo_PersonaGiuridica.cfpiva;
						  _Logger.WriteOnLog(LogId, "ResponsabileProtezioneDati.dpo_PersonaGiuridica.cfpiva: " + oField.Value, 3);
					  }
					  else if (oField.Id == SVAOLLib.svIdField.svIfKey24)
					  {
						  oField.Value = deserializedVisEntity.ResponsabileProtezioneDati.dpo_personaFisica.Cognome + " " + deserializedVisEntity.ResponsabileProtezioneDati.dpo_personaFisica.Nome;
						  _Logger.WriteOnLog(LogId, "ResponsabileProtezioneDati.dpo_personaFisica.Nome: " + oField.Value, 3);

					  }
					  else if (oField.Id == SVAOLLib.svIdField.svIfKey25)
					  {
						  oField.Value = deserializedVisEntity.ResponsabileProtezioneDati.dpo_personaFisica.cf;
						  _Logger.WriteOnLog(LogId, "ResponsabileProtezioneDati.dpo_personaFisica.cf: " + oField.Value, 3);
					  }
					  else if (oField.Id == SVAOLLib.svIdField.svIfKey31)
					  {
						  oField.Value = deserializedVisEntity.ResponsabileProtezioneDati.contatti.Telefono;
						  _Logger.WriteOnLog(LogId, "ResponsabileProtezioneDati.contatti.Telefono: " + oField.Value, 3);
					  }
					  else if (oField.Id == SVAOLLib.svIdField.svIfKey32)
					  {
						  oField.Value = deserializedVisEntity.ResponsabileProtezioneDati.contatti.Mobile;
						  _Logger.WriteOnLog(LogId, "ResponsabileProtezioneDati.contatti.Mobile: " + oField.Value, 3);
					  }
					  else if (oField.Id == SVAOLLib.svIdField.svIfKey33)
					  {
						  oField.Value = deserializedVisEntity.ResponsabileProtezioneDati.contatti.Email;
						  _Logger.WriteOnLog(LogId, "ResponsabileProtezioneDati.contatti.Email: " + oField.Value, 3);
					  }
					  else if (oField.Id == SVAOLLib.svIdField.svIfKey34)
					  {
						  oField.Value = deserializedVisEntity.ResponsabileProtezioneDati.contatti.Pec;
						  _Logger.WriteOnLog(LogId, "ResponsabileProtezioneDati.contatti.Pec: " + oField.Value, 3);
					  }
					  else if (oField.Id == SVAOLLib.svIdField.svIfKey35)
					  {
						  // ex sito web
						  // diventa stato campo lista
						  // di default il valore è "ATTIVA"
						  if (sEsito.Substring(0, 1) == "1")
							  oField.Value = resourceFileManager.getConfigData("StateActive"); 
						  else
							  oField.Value = resourceFileManager.getConfigData("StateBad");
						  //oField.Value = deserializedVisEntity.ModalitaPubblicazione.DescrizioneSitoWeb;
						  _Logger.WriteOnLog(LogId, "ModalitaPubblicazione.DescrizioneSitoWeb: " + oField.Value, 3);
					  }
					  else if (oField.Id == SVAOLLib.svIdField.svIfKey41)
					  {   // ex altro
						  // diventa note il campo resta sempre vuoto
						  //oField.Value = deserializedVisEntity.ModalitaPubblicazione.DescrizioneAltro;
						  _Logger.WriteOnLog(LogId, "ModalitaPubblicazione.DescrizioneAltro: " + oField.Value + " - Tale campo non riempie più i campi della scheda", 3);
					  }
					  else if (oField.Id == SVAOLLib.svIdField.svIfKey42)
					  {
						  oField.Value = sEsito.Replace(" -","");
						  _Logger.WriteOnLog(LogId, "Valorizzo l'esito dei controlli eseguiti: " + oField.Value, 3);
					  }
					  else if (oField.Id == SVAOLLib.svIdField.svIfKey43)
					  {
						  if (deserializedVisEntity.GruppoImprenditoriale.tipo_sogg == null || sEsito.Substring(0, 1) != "1")
						  {
							  oField.Value = "";
						  }
						  else
							  oField.Value = resourceFileManager.getConfigData(deserializedVisEntity.GruppoImprenditoriale.tipo_sogg.ToString() + deserializedVisEntity.GruppoImprenditoriale.altro_dpo.ToString().Replace("/", ""));
						  _Logger.WriteOnLog(LogId, "Chiave Mista: " + deserializedVisEntity.GruppoImprenditoriale.tipo_sogg.ToString() + deserializedVisEntity.GruppoImprenditoriale.altro_dpo.ToString().Replace("/", ""), 3);
					  }
					  else if (oField.Id == SVAOLLib.svIdField.svIfKey44)
					  {
						  oField.Value = deserializedVisEntity.GruppoImprenditoriale.rif_aut_estera_den_soc;
						  _Logger.WriteOnLog(LogId, "GruppoImprenditoriale.rif_aut_estera_den_soc: " + oField.Value, 3);
					  }
					  else if (oField.Id == SVAOLLib.svIdField.svIfKey45)
					  {
						  oField.Value = deserializedVisEntity.GruppoImprenditoriale.rif_aut_estera_stato;
						  _Logger.WriteOnLog(LogId, "GruppoImprenditoriale.rif_aut_estera_stato: " + oField.Value, 3);
					  }
					  else if (oField.Id == SVAOLLib.svIdField.svIfObj)
					  {
						  oField.Value = resourceFileManager.getConfigData("OGGETTO_DPO");
						  _Logger.WriteOnLog(LogId, "OGGETTO_DPO: " + oField.Value, 3);
					  }
					  else if (oField.Id == SVAOLLib.svIdField.svIfProtocol)
					  {
						  oField.Value = deserializedVisEntity.id_comunicazione;
						  _Logger.WriteOnLog(LogId, "id_comunicazione: " + oField.Value, 3);
					  }
					  else if (oField.Id == SVAOLLib.svIdField.svIfDateDoc)
					  {
						  oField.Value = sDateReceivementDoc;
						  _Logger.WriteOnLog(LogId, "DATA COMUNICAZIONE: " + oField.Value, 3);
					  }
					  SVAOLLib.Field oFieldSelected = oField;
					  oFields.Add(oFieldSelected);
				  }
				  _Logger.WriteOnLog(LogId, "Fine associazione Campi alla card", 3);

				  oSession.GUIDconnect = strConnection;
				  oCard.GUIDconnect = strConnection;
				  oCard.Archive = oArchive;
				  oCard.DocType = oTypeDocument;
				  oCard.Fields = oFields;

				  var SendObjxml = oCard.GetVisibilityAsXML();
				  _Logger.WriteOnLog(LogId, "SendObjxml: " + _Logger.ToJson(SendObjxml), 3);

				  _Logger.WriteOnLog(LogId, "Calcolo la visibilità di default", 3);

				  var oVisibility = UtilAction.getEntityVisibilityFromCard(SendObjxml);
					  oUsers = UtilAction.getUsersFromSharePredefinite(oVisibility);
				  _Logger.WriteOnLog(LogId, "oUsers: " + oUsers.Count, 3);
				  oGroups = UtilAction.getGroupsFromSharePredefinite(oVisibility);
				  _Logger.WriteOnLog(LogId, "oGroups: " + oGroups.Count, 3);
				  oOffices = UtilAction.getOfficesFromSharePredefinite(oVisibility);
				  _Logger.WriteOnLog(LogId, "oOffices: " + oOffices.Count, 3);

				  oMailUsers = UtilAction.getUsersMailFromSharePredefinite(oVisibility);
					  oMailGroups = UtilAction.getGroupsMailFromSharePredefinite(oVisibility);
					  oMailOffices = UtilAction.getOfficesMailFromSharePredefinite(oVisibility);
				  oCard.Offices = oOffices;
				  oCard.Users = oUsers;
				  oCard.Groups = oGroups;
				  // Inserisco la card in Archivio
				  oCard.Insert(oMailOffices, oMailGroups, oMailUsers, "", "", 0);
				  //Ottengo il progressivo assouto della scheda appena inserita
				  sGuidCard = oCard.GuidCard;
				  _Logger.WriteOnLog(LogId, "Inserimento card eseguito, guid: " + sGuidCard, 3);
				  bInsertResult = true;
			  }
			  catch (Exception e)
			  {
				  _Logger.WriteOnLog(LogId, e.Source + " " + e.Message, 3);
				  throw new Exception(String.Format("{0} >> {1}: {2}", "ERRORE : SetAddCardVisibility" + e.StackTrace, e.Source, e.Message), e);
			  }
			  return bInsertResult;
		  }
		  /// <summary>

		  /// Costruisce il link alla scheda documentale per il progressivo assoluto specificato.

		  /// E' importante che nel fileXml1 venga impostata la variabile ROOT_URL con l'indirizzo

		  /// dell'ambiente Archiflow Web, e.g. http://eslq77:81/archiflow/

		  /// </summary>

		  /// <param name="stGuidConnect">GUID connection per la session SVAOL</param>

		  /// <param name="CardAbsId">Il progressivo assoluto della scheda di interesse</param>

		  /// <param name="LogId">Nome del file di log</param>

		  /// <returns>HyperLink alla scheda documentale</returns>
		  /// 
					 */

		public SVAOLLib.Card SendCardFromId(string CardId, string GuidConnect, SVAOLLib.Offices oOffices, SVAOLLib.Groups oGroups, SVAOLLib.Users oUsers, string SAnnotation, string sMessage, SVAOLLib.Offices oMailOffices=null, SVAOLLib.Groups oMailGroups=null, SVAOLLib.Users oMailUsers=null)
		{
			int iStateWf = 0;
			bool bWorkflowActice = false;
			if (oMailOffices == null)
				oMailOffices = new SVAOLLib.Offices();
			if (oMailGroups == null)
				oMailGroups = new SVAOLLib.Groups();
			if (oMailUsers == null)
				oMailUsers = new SVAOLLib.Users();
			SVAOLLib.Card card = new SVAOLLib.Card();
			
			card.GUIDconnect = GuidConnect;
			Guid oCardId;
			if (CardId.Length > 12)                 // set the guid of the card
				oCardId = new Guid("64556990-b196-425c-a0b9-" + int.Parse(CardId.Substring(24, 12)).ToString("000000000000"));
			else
				oCardId = new Guid("64556990-b196-425c-a0b9-" + int.Parse(CardId).ToString("000000000000"));
			card.GuidCard = oCardId.ToString();
			card.LoadFromGuid();
			if (card.ProcWF!=0)
			{
				iStateWf = (int)card.ProcWF;
				card.ModifyProcWf(SVAOLLib.svProcWF.svPWFNothing);
				bWorkflowActice = true;
			}
			card.Offices = oOffices;
			card.Users = oUsers;
			card.Groups = oGroups;
			card.Send(oMailOffices, oMailGroups, oMailUsers, SAnnotation, sMessage);
			if (bWorkflowActice)
			{
				card.ModifyProcWf((SVAOLLib.svProcWF)iStateWf);
			}
			return card;
		}
		public SVAOLLib.Card SendCardFromIdCC(string CardId, string GuidConnect, SVAOLLib.Offices oOffices, SVAOLLib.Groups oGroups, SVAOLLib.Users oUsers, string SAnnotation, string sMessage, SVAOLLib.Offices oMailOffices = null, SVAOLLib.Groups oMailGroups = null, SVAOLLib.Users oMailUsers = null)
		{
			int iStateWf = 0;
			bool bWorkflowActice = false;
			if (oMailOffices == null)
				oMailOffices = new SVAOLLib.Offices();
			if (oMailGroups == null)
				oMailGroups = new SVAOLLib.Groups();
			if (oMailUsers == null)
				oMailUsers = new SVAOLLib.Users();
			SVAOLLib.Card card = new SVAOLLib.Card();

			card.GUIDconnect = GuidConnect;
			Guid oCardId;
			if (CardId.Length > 12)                 // set the guid of the card
				oCardId = new Guid("64556990-b196-425c-a0b9-" + int.Parse(CardId.Substring(24, 12)).ToString("000000000000"));
			else
				oCardId = new Guid("64556990-b196-425c-a0b9-" + int.Parse(CardId).ToString("000000000000"));
			card.GuidCard = oCardId.ToString();
			card.LoadFromGuid();
			if (card.ProcWF != 0)
			{
				iStateWf = (int)card.ProcWF;
				card.ModifyProcWf(SVAOLLib.svProcWF.svPWFNothing);
				bWorkflowActice = true;
			}
			card.SendCC(oMailOffices, oMailGroups, oMailUsers, oOffices, oGroups, oUsers, SAnnotation, sMessage,0,0,0);
			if (bWorkflowActice)
			{
				card.ModifyProcWf((SVAOLLib.svProcWF)iStateWf);
			}
			return card;
		}
		public string SetFieldValue(ref SVAOLLib.Fields oFields, long lIdField, string stValue)
        {
            //Controllo se non è stato aperto già un File di Log
            string SetFVal = "";  //valore di ritorno
            int iCount, imax;
            imax = oFields.Count;

            try
            {

                for (iCount = 1; iCount < imax + 1; iCount++)
                {

                    //Campo protocollo interno

                    if ((long)((SVAOLLib.Field)oFields.Item(iCount)).Id == lIdField)
                    {
                        long ss = (long)((SVAOLLib.Field)oFields.Item(iCount)).Id;
                        ((SVAOLLib.Field)oFields.Item(iCount)).Value = stValue;
                        SetFVal = stValue;

                        break;
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception(String.Format("{0}>>{1}>>{2}", "ERRORE : SetFieldValue", e.Source, e.Message), e);

            }
            return SetFVal;

        }
		public SVAOLLib.Card GetCardFromId(string GuidConnect, string CardId)
		{
			SVAOLLib.Card oCard = new SVAOLLib.Card();
			oCard.GUIDconnect = GuidConnect;
			oCard.GuidCard = _Logger.FormatID(CardId);
			oCard.LoadFromGuid();
			return oCard;
		}
		public Boolean SetSingleFieldValue(string GuidConnect, string CardId, long FieldId, string FieldValue)
        {
            try
            {

                SVAOLLib.Card card = GetCardFromId(GuidConnect, CardId);
                SVAOLLib.Fields fields = (SVAOLLib.Fields)card.Fields;
                string result = SetFieldValue(ref fields, Convert.ToInt64(FieldId), FieldValue);
                card.Modify(0);
                return true;
            }
            catch (Exception e)
            {
                throw new Exception(String.Format("{0}>>{1}>>{2}", "ERRORE : SetSingleFieldValue", e.Source, e.Message), e);
            }
        }
        public List<string> SearchCardsByField(string stGuidConnect, string stArchive, long lFieldID, Int16 idTipoDocTutti, string stFieldValueFrom, string stFieldValueTo, string userId)
        {
            List<string> lResult = new List<string>();
            //istanzio i Manager che mi servono
            //Inizializzo l'oggetto SvAol.Session
            SVAOLLib.Session oSession = new SVAOLLib.Session();
            SVAOLLib.User oUser = new SVAOLLib.User();

            try
            {
                if (stGuidConnect != null)
                {
                    oUser.GUIDconnect = stGuidConnect;
                    oUser.UserID = userId.ToUpper();
                    oUser.LoadFromUserID(userId.ToUpper());
                    //istanzio gli oggetti svaol
                    SVAOLLib.Archive oArchive = new SVAOLLib.Archive();
                    SVAOLLib.DocumentType oDocType = new SVAOLLib.DocumentType();
                    SVAOLLib.SearchCriteria oSearchCriteria = new SVAOLLib.SearchCriteria();
                    SVAOLLib.Archives oArchives = new SVAOLLib.Archives();
                    SVAOLLib.Fields oFields = new SVAOLLib.Fields();
                    object[] aCardsAsArray;
                    long lCardsCount, lSearchRes;

                    oDocType.Id = idTipoDocTutti;//idTipoDocumento;
                    oDocType.GUIDconnect = stGuidConnect;
                    oDocType.LoadDocumentTypeFromId();
					Int16 idTipoArchivio = 0;
					using (var oDocManager = new DocManager(_Logger, _sLogId))
					{
						idTipoArchivio = oDocManager.GetIdArchiveByName(stGuidConnect, stArchive);
					}
					oArchive.GUIDconnect = stGuidConnect;
                    oArchive.Id = idTipoArchivio;
                    oArchive.LoadFromId();

                    //Inizializzo l'oggetto Session
                    oSession.GUIDconnect = stGuidConnect;
                    //Aggiungo l'archivio all'oggetto relativo ai criteri di ricerca
                    oArchives.Add(oArchive, null);
                    oSearchCriteria.Archives = oArchives;
                    //Aggiungo il tipo documento all'oggetto relativo ai criteri di ricerca
                    oSearchCriteria.DocType = oDocType;
                    //Inserisco l'ambito della ricerca
                    oSearchCriteria.Context = SVAOLLib.svContextSearch.svCsBoth;
                    //Inserisco il tipo di ricerca
                    oSearchCriteria.SearchType = SVAOLLib.svSearchType.svStIndexes;
                    //Inserisco l'informazione sull'utente collegato
                    oSearchCriteria.CntUser = oUser.Code;

                    //Chiave di ricerca
                    foreach (SVAOLLib.Field oField in (SVAOLLib.Fields)oDocType.Fields)
                    {
                        if ((long)oField.Id == lFieldID)
                        {
                            oField.Value = stFieldValueFrom;
                            oField.ValueTo = stFieldValueTo;
                            oFields.Add(oField, null);
                            break;
                        }
                    }

                    //Eseguo la ricerca
                    oSearchCriteria.Fields = oFields;
                    aCardsAsArray = (object[])oSession.SearchAsArray(oSearchCriteria, 0);

                    lSearchRes = (long)oSearchCriteria.SearchResult;
                    lCardsCount = aCardsAsArray.GetUpperBound(0);

                    // Controllo che mi sia tornata una Card valida
                    if (lCardsCount > 0)
                    {
                        //assegno la GuidCard della scheda trovata
                        for (int i = 1; i < lCardsCount + 1; i++)
                        {
                            lResult.Add(aCardsAsArray[i].ToString().Substring(24));
                        }
                    }
                }

            }
            catch (Exception e)
            {
                throw new Exception(String.Format("{0} >> {1}: {2}", "ERRORE: SearchCardByField", e.Source, e.Message), e);
            }
            finally
            {
            }
            return lResult;
        }
		public List<string> SearchCardsByField(string stGuidConnect, short shArchive, short shDocType, long lFieldID, string stFieldValueFrom, string stFieldValueTo, string userId)
		{
			//Controllo se non è stato aperto già un File di Log
			List<string> lResult = new List<string>();
			//istanzio i Manager che mi servono
			//Inizializzo l'oggetto SvAol.Session
			SVAOLLib.Session oSession = new SVAOLLib.Session();
			SVAOLLib.User oUser = new SVAOLLib.User();

			try
			{
				if (stGuidConnect != null)
				{
					oUser.GUIDconnect = stGuidConnect;
					oUser.UserID = userId.ToUpper();
					oUser.LoadFromUserID(userId.ToUpper());
					//istanzio gli oggetti svaol
					SVAOLLib.Archive oArchive = new SVAOLLib.Archive();
					SVAOLLib.DocumentType oDocType = new SVAOLLib.DocumentType();
					SVAOLLib.SearchCriteria oSearchCriteria = new SVAOLLib.SearchCriteria();
					SVAOLLib.Archives oArchives = new SVAOLLib.Archives();
					SVAOLLib.Fields oFields = new SVAOLLib.Fields();
					object[] aCardsAsArray;
					long lCardsCount, lSearchRes;

					//recupero l'archivioo e il tipo documento
					oDocType.Id = shDocType;
					oDocType.GUIDconnect = stGuidConnect;
					oDocType.LoadDocumentTypeFromId();

					oArchive.GUIDconnect = stGuidConnect;
					oArchive.Id = shArchive;
					oArchive.LoadFromId();

					//Inizializzo l'oggetto Session
					oSession.GUIDconnect = stGuidConnect;
					//Aggiungo l'archivio all'oggetto relativo ai criteri di ricerca
					oArchives.Add(oArchive, null);
					oSearchCriteria.Archives = oArchives;
					//Aggiungo il tipo documento all'oggetto relativo ai criteri di ricerca
					oSearchCriteria.DocType = oDocType;
					//Inserisco l'ambito della ricerca
					oSearchCriteria.Context = SVAOLLib.svContextSearch.svCsBoth;
					//Inserisco il tipo di ricerca
					oSearchCriteria.SearchType = SVAOLLib.svSearchType.svStIndexes;
					//Inserisco l'informazione sull'utente collegato
					oSearchCriteria.CntUser = oUser.Code;

					//Chiave di ricerca
					foreach (SVAOLLib.Field oField in (SVAOLLib.Fields)oDocType.Fields)
					{
						
						if ((long)oField.Id == lFieldID)
						{
							oField.Value = stFieldValueFrom;
							oField.ValueTo = stFieldValueTo;
							oFields.Add(oField, null);
							break;
						}
					}

					//Eseguo la ricerca
					oSearchCriteria.Fields = oFields;
					aCardsAsArray = (object[])oSession.SearchAsArray(oSearchCriteria, 0);

					lSearchRes = (long)oSearchCriteria.SearchResult;
					lCardsCount = aCardsAsArray.GetUpperBound(0);

					// Controllo che mi sia tornata una Card valida
					if (lCardsCount > 0)
					{
						//assegno la GuidCard della scheda trovata
						for (int i = 1; i < lCardsCount + 1; i++)
						{
							lResult.Add(aCardsAsArray[i].ToString().Substring(24));
						}
					}
				}

			}
			catch (Exception e)
			{
				throw new Exception(String.Format("{0} >> {1}: {2}", "ERRORE: SearchCardByField", e.Source, e.Message), e);
			}
			finally
			{
			}
			return lResult;
		}
		public List<string> SearchCardsByField(string stGuidConnect, string stArchive, string stDocType, long lFieldID, string stFieldValueFrom, string stFieldValueTo, string userId)
        {
            //Controllo se non è stato aperto già un File di Log
            List<string> lResult = new List<string>();
            //istanzio i Manager che mi servono
            //Inizializzo l'oggetto SvAol.Session
            SVAOLLib.Session oSession = new SVAOLLib.Session();
            SVAOLLib.User oUser = new SVAOLLib.User();

            try
            {
                if (stGuidConnect != null)
                {
                    oUser.GUIDconnect = stGuidConnect;
                    oUser.UserID = userId.ToUpper();
                    oUser.LoadFromUserID(userId.ToUpper());
                    //istanzio gli oggetti svaol
                    SVAOLLib.Archive oArchive = new SVAOLLib.Archive();
                    SVAOLLib.DocumentType oDocType = new SVAOLLib.DocumentType();
                    SVAOLLib.SearchCriteria oSearchCriteria = new SVAOLLib.SearchCriteria();
                    SVAOLLib.Archives oArchives = new SVAOLLib.Archives();
                    SVAOLLib.Fields oFields = new SVAOLLib.Fields();
                    object[] aCardsAsArray;
                    long lCardsCount, lSearchRes;

					//recupero l'archivioo e il tipo documento
					Int16 idTipoDocumento = 0;
					Int16 idTipoArchivio = 0;
					using (var oDocManager = new DocManager(_Logger, _sLogId))
					{
						idTipoDocumento = oDocManager.GetIdDocTypeByName(stGuidConnect, stDocType);
					}
					oDocType.Id = idTipoDocumento;
                    oDocType.GUIDconnect = stGuidConnect;
                    oDocType.LoadDocumentTypeFromId();
					using (var oDocManager = new DocManager(_Logger, _sLogId))
					{
						idTipoArchivio = oDocManager.GetIdArchiveByName(stGuidConnect, stArchive);
					}
                    oArchive.GUIDconnect = stGuidConnect;
                    oArchive.Id = idTipoArchivio;
                    oArchive.LoadFromId();

                    //Inizializzo l'oggetto Session
                    oSession.GUIDconnect = stGuidConnect;
                    //Aggiungo l'archivio all'oggetto relativo ai criteri di ricerca
                    oArchives.Add(oArchive, null);
                    oSearchCriteria.Archives = oArchives;
                    //Aggiungo il tipo documento all'oggetto relativo ai criteri di ricerca
                    oSearchCriteria.DocType = oDocType;
                    //Inserisco l'ambito della ricerca
                    oSearchCriteria.Context = SVAOLLib.svContextSearch.svCsBoth;
                    //Inserisco il tipo di ricerca
                    oSearchCriteria.SearchType = SVAOLLib.svSearchType.svStIndexes;
                    //Inserisco l'informazione sull'utente collegato
                    oSearchCriteria.CntUser = oUser.Code;

                    //Chiave di ricerca
                    foreach (SVAOLLib.Field oField in (SVAOLLib.Fields)oDocType.Fields)
                    {
                        if ((long)oField.Id == lFieldID)
                        {
                            oField.Value = stFieldValueFrom;
                            oField.ValueTo = stFieldValueTo;
                            oFields.Add(oField, null);
                            break;
                        }
                    }

                    //Eseguo la ricerca
                    oSearchCriteria.Fields = oFields;
                    aCardsAsArray = (object[])oSession.SearchAsArray(oSearchCriteria, 0);

                    lSearchRes = (long)oSearchCriteria.SearchResult;
                    lCardsCount = aCardsAsArray.GetUpperBound(0);

                    // Controllo che mi sia tornata una Card valida
                    if (lCardsCount > 0)
                    {
                        //assegno la GuidCard della scheda trovata
                        for (int i = 1; i < lCardsCount + 1; i++)
                        {
                            lResult.Add(aCardsAsArray[i].ToString().Substring(24));
                        }
                    }
                }

            }
            catch (Exception e)
            {
                throw new Exception(String.Format("{0} >> {1}: {2}", "ERRORE: SearchCardByField", e.Source, e.Message), e);
            }
            finally
            {
            }
            return lResult;
        }
        public Boolean SetExternalAttach(string stGuidConnect, string stguidCardTo, string stAttachName, object aBinaryData, string note)
        {

            //Controllo se non è stato aperto già un File di Log

            //è la variabile da ritornare
            Boolean SetCirAtt = false;
            try
            {    //controllo se sono già connesso, in cvaso contrario mi connetto e ritorno la stringa di connessione
                if (stGuidConnect.Length != 0)
                {
                    SVAOLLib.Attachment oAttachmentTo;
                    // Se la GUIDCard non è formattata lo faccio ora           
                    stguidCardTo = _Logger.FormatID(stguidCardTo);

                    // Imposto l'allegato della scheda
                    oAttachmentTo = new SVAOLLib.Attachment();
                    oAttachmentTo.GuidCard = stguidCardTo;
                    oAttachmentTo.GUIDconnect = stGuidConnect;

                    oAttachmentTo.Note = note;
                    oAttachmentTo.IsInternal = 0;
                    oAttachmentTo.Name = stAttachName;
                    // Inserisco l'allegato esterno della Card FROM nella Card TO.
                    int q = ((byte[])aBinaryData).GetUpperBound(0) + 1;
                    oAttachmentTo.InsertExternal(aBinaryData, q, 0, q, 0, 0);
                    SetCirAtt = true;
                }
            }
            catch (Exception e)
            {
                throw new Exception(String.Format("{0}>>{1}>>{2}", "ERRORE : SetExternalAttach: " + e.StackTrace, e.Source, e.Message), e);
            }
            finally
            {
            }

            return SetCirAtt;
        }
		public Boolean SetInternalAttach(string stGuidConnect, string stguidCardTo, string sGUIDAttachment, string note, string internalNote, bool bBiunivoc)
		{

			//Controllo se non è stato aperto già un File di Log

			//è la variabile da ritornare
			Boolean SetCirAtt = false;
			try
			{    //controllo se sono già connesso, in cvaso contrario mi connetto e ritorno la stringa di connessione
				if (stGuidConnect.Length != 0)
				{
					stguidCardTo = _Logger.FormatID(stguidCardTo);

					SVAOLLib.Card oCard = new SVAOLLib.Card(); ;
					oCard.GUIDconnect = stGuidConnect;
					oCard.GuidCard = stguidCardTo;
					oCard.LoadFromGuid();
					string sNumProt= this.GetSingleFieldValue(stGuidConnect, sGUIDAttachment, 0).ToString();
					SVAOLLib.Attachment oAttachmentTo;
					// Se la GUIDCard non è formattata lo faccio ora           
					sGUIDAttachment = _Logger.FormatID(sGUIDAttachment);
					// Imposto l'allegato della scheda
					oAttachmentTo = new SVAOLLib.Attachment();
					oAttachmentTo.Archive = oCard.Archive;
					oAttachmentTo.GuidCard = stguidCardTo;
					oAttachmentTo.GUIDconnect = stGuidConnect;
					oAttachmentTo.GUIDAttachment = sGUIDAttachment;
					oAttachmentTo.Note = note;
					oAttachmentTo.InternalNote = internalNote;
					oAttachmentTo.IsInternal = 1;
					oAttachmentTo.Name = sNumProt;
					int iBiunivoc = bBiunivoc ? 1 : 0;
					oAttachmentTo.InsertInternal(iBiunivoc);
					SetCirAtt = true;
				}
			}
			catch (Exception e)
			{
				throw new Exception(String.Format("{0}>>{1}>>{2}", "ERRORE : SetInternalAttach: " + e.StackTrace, e.Source, e.Message), e);
			}
			finally
			{
			}

			return SetCirAtt;
		}
		
		public object GetSingleFieldValue(string GuidConnect, string CardId, long FieldId,Boolean useInternalCard = false)
        {
            object result = null;
            SVAOLLib.Card card;

            try
            {
                card = GetCardFromId(GuidConnect, CardId);
                SVAOLLib.Fields fields = (SVAOLLib.Fields)card.Fields;

                foreach (SVAOLLib.Field f in fields)
                {
                    if ((long)f.Id == FieldId)
                    {
                        result = f.Value;
                        break;
                    }
                }
            }
            catch (Exception ex)
			{
				throw new Exception(String.Format("{0} >> {1}: {2}", "ERRORE: FormatID", ex.Source, ex.Message), ex);
			}
            finally
            {
            }
            if (string.IsNullOrEmpty((string)result) == false)
            {
                return result;
            }
            else
                return "";
        }
    }
}
