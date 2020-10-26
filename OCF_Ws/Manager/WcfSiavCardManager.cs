using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;
using System.Security.Cryptography.X509Certificates;
using System.IO;
using OCF_Ws.Model;
using System.Net;
using System.Net.Security;
using System.Collections.Specialized;
using System.Globalization;
using OCF_Ws.WsCard;
using OCF_Ws.Model.Siav.APFlibrary.Model;
using OCF_Ws.Util;

namespace OCF_Ws.Manager
{
    public class WcfSiavCardManager : IDisposable
	{
		public LOLIB _Logger;
		public string _sLogId;
		bool mDisposed = false; public int lErr = 0;
		private static bool customXertificateValidation(object sender, X509Certificate cert, X509Chain chain, SslPolicyErrors error)
        {
            var certificate = (X509Certificate2)cert;
            return true;
        }
        public CardServiceContractClient siavWsCard;
        public WcfSiavCardManager(LOLIB Logger, string sLogId)
        {
            ServicePointManager.ServerCertificateValidationCallback += new System.Net.Security.RemoteCertificateValidationCallback(customXertificateValidation);
            siavWsCard = new CardServiceContractClient();
			_sLogId = sLogId;
			_Logger = Logger;
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
				GC.SuppressFinalize(this);
			}
			mDisposed = true;
		}
		public void InsertCardCas(List<KeyValuePair<int, String>> lfield, short iArchive, DocumentType oTypeDoc, WcfSiavLoginManager wcfSiavLoginManager, List<AgrafCard> agrafCards ,string sPathMainDoc,out Guid oCardGuid)
        {
            // call the WCF service contract for login to Archiflow
            ResultInfo oResult = ResultInfo.OK;

            List<Field> oFieldsSearch = new List<Field>();
            oCardGuid = new Guid();
            try
            {
                CardBundle oProtCardBundle = new CardBundle();
                var lookupconfigScarti = lfield.ToLookup(kvp => kvp.Key, kvp => kvp.Value);
                //            
                // set the archive id
                oProtCardBundle.ArchiveId = iArchive;

                // set the archive id
                oProtCardBundle.DocumentTypeId = oTypeDoc.DocumentTypeId;
                
                // Gestione indici della scheda
                List<Field> oProtFields = new List<Field>();
                // Loop over strings
                if (oProtCardBundle != null)
                {
                    foreach (Field oField in oTypeDoc.Indexes)
                    {
                        foreach (string x in lookupconfigScarti[((int)oField.FieldId)])
                        {
                            oField.FieldValue= x;
                            oProtFields.Add(oField);
                        }
                    }
                }
        
                oProtCardBundle.Indexes = new List<Field>();
                // set the indexes to the card
                oProtCardBundle.Indexes = oProtFields;

                // Gestione documento principale della scheda
                if (!string.IsNullOrEmpty(sPathMainDoc)) { 
                    Byte[] bytesDocx = File.ReadAllBytes(sPathMainDoc);
                    String file = Convert.ToBase64String(bytesDocx);
                
                    oProtCardBundle.MainDocument = new Document();
                    oProtCardBundle.MainDocument.DocumentExtension = Path.GetExtension(sPathMainDoc);
                    oProtCardBundle.MainDocument.DocumentTitle = Path.GetFileName(sPathMainDoc);
                    oProtCardBundle.MainDocument.Content = file;
                }
                oProtCardBundle.AgrafCards = new List<AgrafCard>();
                oProtCardBundle.AgrafCards = agrafCards;
                string strNote = "";
                string strMessage = "";

                //carico le spedizioni predefinite
                List<User> oUsers = new List<User>();
                List<Office> oOffices = new List<Office>();
                List<Group> oGroups = new List<Group>();
				// Modifica inserita al posto della versione commentata successiva
				User oUser = new User();
				oUser.NormalVisibility = true;
				oUser.Code = 1;
				oUsers.Add(oUser);													  
				/*
				SendObject oSendObject;

                oResult = siavWsCard.GetCardVisibility(wcfSiavLoginManager.oSessionInfo.SessionId, iArchive, oTypeDoc.DocumentTypeId, oProtFields, out oSendObject);

                foreach (SendEntity oSendEntity in oSendObject.SendEntities)
                {
                    switch (oSendEntity.EntityType)
                    {
                        case EntityType.User:

                            if (oSendEntity.SendingType != SendingType.None)
                            {
                                User oUser = new User();
                                oUser.Code = oSendEntity.Id;
                                oUser.NormalVisibility = true;

                                if (oSendEntity.SendingType == SendingType.Mail)
                                {
                                    oUser.SendMail = true;
                                }
                                oUsers.Add(oUser);
                            }
                            break;

                        case EntityType.OfficeEntity:
                            if (oSendEntity.SendingType != SendingType.None)
                            {
                                Office oOffice = new Office();
                                oOffice.Code = oSendEntity.Id;
                                oOffice.NormalVisibility = true;

                                if (oSendEntity.SendingType == SendingType.Mail)
                                {
                                    oOffice.SendMail = true;
                                }
                                oOffices.Add(oOffice);
                            }
                            break;

                        case EntityType.GroupEntity:
                            if (oSendEntity.SendingType != SendingType.None)
                            {
                                Group oGroup = new Group();
                                oGroup.Code = oSendEntity.Id;
                                oGroup.NormalVisibility = true;

                                if (oSendEntity.SendingType == SendingType.Mail)
                                {
                                    oGroup.SendMail = true;
                                }
                                oGroups.Add(oGroup);
                            }
                            break;
                    }
                }
				 */
                oResult = siavWsCard.InsertCard(out oCardGuid,wcfSiavLoginManager.oSessionInfo.SessionId, oProtCardBundle, oUsers, oGroups, oOffices, strNote, strMessage, false, false, true, false);

                if (oResult == ResultInfo.OK)
                {
                    //oEsito.Codice = "1";
                    //oEsito.Descrizione = "Inserimento eseguito con successo. GUID: " + oCardGuid;
                }
                else
                {
                    //oEsito.Codice = "0";
                    //oEsito.Descrizione = "Errore in fase di inserimento scheda";
                    throw new ArgumentException("Errore in fase di inserimento scheda");
                }
            }
            catch (FaultException<ArchiflowServiceExceptionDetail> ex)
            {
                oResult = ResultInfo.SERVER_ERROR;
                throw new ArgumentException(ex.Detail.Message);
            }
            catch (Exception ex)
            {
                oResult = ResultInfo.SERVER_ERROR;
                throw new ArgumentException(ex.Message);
            }
        }
        public Outcome GetCardVisibility(string sGuidCard, WcfSiavLoginManager wcfSiavLoginManager, out CardVisibility oCardVisibility)
        {
            oCardVisibility =new CardVisibility();
			Outcome oEsito = new Outcome();
            oEsito.iCode = 1;
            // create the connection info
            ResultInfo oResult = ResultInfo.OK;
            try
            {
                Guid oCardId;
                if (sGuidCard.Length > 12)                 // set the guid of the card
                    oCardId = new Guid("64556990-b196-425c-a0b9-" + int.Parse(sGuidCard.Substring(24,12)).ToString("000000000000"));
                else
                    oCardId = new Guid("64556990-b196-425c-a0b9-" + int.Parse(sGuidCard).ToString("000000000000"));

                // call the WCF service contract to get the a card bundle
                oResult = siavWsCard.GetExistingCardVisibilityCC(out oCardVisibility.Users, out oCardVisibility.Groups,
														out oCardVisibility.Offices, out oCardVisibility.UsersCC, out oCardVisibility.GroupsCC, out oCardVisibility.OfficesCC,wcfSiavLoginManager.oSessionInfo.SessionId, oCardId);
                if (oResult == ResultInfo.OK)
                {
                    Console.WriteLine("GetCardBundle call OK");
                }
                else {
                    oEsito.iCode = 0;
                    oEsito.sDescription = "Motivo: Problemi con la ricezione della scheda: " + sGuidCard;
                    throw new ArgumentException(oEsito.sDescription);
                }
            }
            catch (FaultException<ArchiflowServiceExceptionDetail> ex)
            {
                oEsito.iCode = 0;
                oEsito.sDescription = "Motivo: " + ex.Detail.Message;
                throw new ArgumentException(oEsito.sDescription);
            }
            return oEsito;
        }
        public bool SetAnag(string sGuidCard, WcfSiavLoginManager wcfSiavLoginManager,AgrafCard svAgrafCardRub,AgrafCard svAgrafCardApf, out int iAffected)
        {
            bool bResult = false;
            iAffected = 0;
            List<AgrafCard> oAgrafCards = new List<AgrafCard>();
            // create the connection info
            ResultInfo oResult = ResultInfo.OK;
            try
            {
                Guid oCardId;
                if (sGuidCard.Length > 12)                 // set the guid of the card
                    oCardId = new Guid("64556990-b196-425c-a0b9-" + int.Parse(sGuidCard.Substring(24, 12)).ToString("000000000000"));
                else
                    oCardId = new Guid("64556990-b196-425c-a0b9-" + int.Parse(sGuidCard).ToString("000000000000"));

                OCF_Ws.WsCard.SessionInfo oSessionInfo = new SessionInfo();
                oSessionInfo.SessionId = wcfSiavLoginManager.oSessionInfo.SessionId;
                oAgrafCards.Add(svAgrafCardRub);
                oAgrafCards.Add(svAgrafCardApf);
                oResult = siavWsCard.AddContactsToCard2(out iAffected,oSessionInfo, oAgrafCards, oCardId, false);
                if (oResult == ResultInfo.OK)
                {
                    bResult = true;
                }
                else
                {
                    throw new ArgumentException("Motivo: Problemi con la ricezione della scheda: " + sGuidCard);
                }
            }
            catch (FaultException<ArchiflowServiceExceptionDetail> ex)
            {
                throw new ArgumentException("Motivo: " + ex.Detail.Message);
            }
            return bResult;
        }
        public Outcome GetCard(string sGuidCard, WcfSiavLoginManager wcfSiavLoginManager, out CardBundle oCardBundle)
        {
			Outcome oEsito = new Outcome();
            oEsito.iCode = 1;
            oCardBundle = new CardBundle();
            // create the connection info
            ResultInfo oResult = ResultInfo.OK;
            try
            {
                Guid oCardId;
                if (sGuidCard.Length > 12)                 // set the guid of the card
                    oCardId = new Guid("64556990-b196-425c-a0b9-" + int.Parse(sGuidCard.Substring(24,12)).ToString("000000000000"));
                else
                    oCardId = new Guid("64556990-b196-425c-a0b9-" + int.Parse(sGuidCard).ToString("000000000000"));

                // call the WCF service contract to get the a card bundle
                oResult = siavWsCard.GetCardBundle(out oCardBundle,wcfSiavLoginManager.oSessionInfo.SessionId, oCardId, false);
                
                if (oResult == ResultInfo.OK)
                {
                    Console.WriteLine("GetCardBundle call OK");
                    //cardParts
                    if (oCardBundle != null) {
                        /*
                        Console.WriteLine("Card:Id={0};ArchiveID={1};DoctypeId={2};HasAdditives{3};" +
                                            "HasAttachments{4};HasDocument{5};IsReadOnly={6}",
                                                                                new object[]{oCardBundle.CardId,
                                                                                     oCardBundle.ArchiveId,
                                                                                     oCardBundle.DocumentTypeId,
                                                                                     oCardBundle.HasAdditionalData,
                                                                                     oCardBundle.HasAttachment,
                                                                                     oCardBundle.HasDocument,
                                                                                     oCardBundle.IsReadOnly});
                        Console.WriteLine("\tCard's indexes:");
                        foreach (Field oField in oCardBundle.Indexes)
                            Console.WriteLine("\t\tField:Id={0};Name={1};Value={2}", new object[]{oField.FieldId.ToString(),
                                                                                         oField.FieldDescription,
                                                                                         oField.FieldValue});
                        Console.WriteLine("\tCard's additives:");
                        foreach (Additive oAdditive in oCardBundle.Additives)
                            Console.WriteLine("\t\tAdditive:Id={0};Name={1};Value={2}", new object[]{oAdditive.AdditiveId.ToString(),
                                                                                         oAdditive.AdditiveName,
                                                                                         oAdditive.AdditiveValue});
                        if (oCardBundle.HasDocument)
                        {
                            Console.WriteLine("\tMain document:Title={0};Extension={1};IsSigned={2}", new object[]{
                                                                                         oCardBundle.MainDocument.DocumentTitle,
                                                                                         oCardBundle.MainDocument.DocumentExtension,
                                                                                         oCardBundle.MainDocument.IsSigned});
                            // save the main document content to disk
                            byte[] oDocBytes = Convert.FromBase64String(oCardBundle.MainDocument.Content);
                            File.WriteAllBytes(@"C:\temp\temp." + oCardBundle.MainDocument.DocumentExtension, oDocBytes);
                        }

                        Console.WriteLine("\tCard's attachments:");
                        if (oCardBundle.HasAttachment)
                        {
                            foreach (Attachment oAttachment in oCardBundle.Attachments)
                            {
                                if (oAttachment is InternalAttachment)
                                {
                                    InternalAttachment oIntAttachment = (InternalAttachment)oAttachment;
                                    Console.WriteLine("\t\tInternal attachment:Name={0};ArchiveId={1};Note={2}", new object[]{
                                                                                         oIntAttachment.Name,
                                                                                         oIntAttachment.ArchiveId,
                                                                                         oIntAttachment.Note});

                                }
                                else if (oAttachment is ExternalAttachment)
                                {
                                    ExternalAttachment oExtAttachment = (ExternalAttachment)oAttachment;
                                    Console.WriteLine("\t\tInternal attachment:Name={0};Note={2}", new object[]{
                                                                                         oExtAttachment.Name,
                                                                                         oExtAttachment.Note});
                                    // save the attachment content to disk
                                    byte[] oDocBytes = Convert.FromBase64String(oExtAttachment.Content);
                                    File.WriteAllBytes(@"C:\temp\" + oExtAttachment.Name, oDocBytes);
                                }

                            }
                        }
                         * */
                    }
                }
                else {
                    oEsito.iCode = 0;
                    oEsito.sDescription = "Motivo: Problemi con la ricezione della scheda: " + sGuidCard;
                    throw new ArgumentException(oEsito.sDescription);
                }
            }
            catch (FaultException<ArchiflowServiceExceptionDetail> ex)
            {
                oEsito.iCode = 0;
                oEsito.sDescription = "Motivo: " + ex.Detail.Message;
                throw new ArgumentException(oEsito.sDescription);
            }
            return oEsito;
        }
		public Outcome GetCard(string sGuidCard, string sConnection, out CardBundle oCardBundle)
		{
			Outcome oEsito = new Outcome();
			oEsito.iCode = 1;
			oCardBundle = new CardBundle();
			// create the connection info
			ResultInfo oResult = ResultInfo.OK;
			try
			{
				Guid oCardId;
				if (sGuidCard.Length > 12)                 // set the guid of the card
					oCardId = new Guid("64556990-b196-425c-a0b9-" + int.Parse(sGuidCard.Substring(24, 12)).ToString("000000000000"));
				else
					oCardId = new Guid("64556990-b196-425c-a0b9-" + int.Parse(sGuidCard).ToString("000000000000"));

				// call the WCF service contract to get the a card bundle
				oResult = siavWsCard.GetCardBundle(out oCardBundle, sConnection, oCardId, false);

				if (oResult == ResultInfo.OK)
				{
					Console.WriteLine("GetCardBundle call OK");
					//cardParts
					if (oCardBundle != null)
					{
					}
				}
				else
				{
					oEsito.iCode = 0;
					oEsito.sDescription = "Motivo: Problemi con la ricezione della scheda: " + sGuidCard;
					throw new ArgumentException(oEsito.sDescription);
				}
			}
			catch (FaultException<ArchiflowServiceExceptionDetail> ex)
			{
				oEsito.iCode = 0;
				oEsito.sDescription = "Motivo: " + ex.Detail.Message;
				throw new ArgumentException(oEsito.sDescription);
			}
			return oEsito;
		}
		public Outcome GetCard(string sGuidCard, WcfSiavLoginManager wcfSiavLoginManager, out WsCard.Card oCard)
        {
			Outcome oEsito = new Outcome();
            oEsito.iCode = 1;
            oCard= new WsCard.Card();
            // create the connection info
            ResultInfo oResult = ResultInfo.OK;
            try
            {
                Guid oCardId;
                if (sGuidCard.Length > 12)                 // set the guid of the card
                    oCardId = new Guid("64556990-b196-425c-a0b9-" + int.Parse(sGuidCard.Substring(24, 12)).ToString("000000000000"));
                else
                    oCardId = new Guid("64556990-b196-425c-a0b9-" + int.Parse(sGuidCard).ToString("000000000000"));

                // call the WCF service contract to get the a card bundle
                oResult = siavWsCard.GetCard(out oCard,wcfSiavLoginManager.oSessionInfo.SessionId, oCardId);

                if (oResult == ResultInfo.OK)
                {
                    Console.WriteLine("GetCard call OK");
                    //cardParts
                    if (oCard != null)
                    {
                     
                    }
                }
                else
                {
                    oEsito.iCode = 0;
                    oEsito.sDescription= "Motivo: Problemi con la ricezione della scheda: " + sGuidCard;
                    throw new ArgumentException(oEsito.sDescription);
                }
            }
            catch (FaultException<ArchiflowServiceExceptionDetail> ex)
            {
                oEsito.iCode = 0;
                oEsito.sDescription = "Motivo: " + ex.Detail.Message;
                throw new ArgumentException(oEsito.sDescription);
            }
            return oEsito;
        }
        public Outcome GetCard(string sIdProtocollo, string sIdArchivio, WcfSiavLoginManager wcfSiavLoginManager, out CardBundle oCardBundle)
        {
			Outcome oEsito = new Outcome();
            oEsito.iCode = 1;
            oCardBundle = new CardBundle();
            // create the connection info
            ResultInfo oResult = ResultInfo.OK;
            try
            {
                List<Guid> lGUID;
                //
                List<SchedaSearchParameter> lSchedaSearchParameter = new List<SchedaSearchParameter>();
                
                SchedaSearchParameter schedaSearchParameter = new SchedaSearchParameter();
                schedaSearchParameter.IdRiferimento= sIdProtocollo;
                schedaSearchParameter.ArchivioId = sIdArchivio;
                lSchedaSearchParameter.Add(schedaSearchParameter);
                this.getSearch(lSchedaSearchParameter, wcfSiavLoginManager, out lGUID);
                if (lGUID.Count > 0)
                {
                    oResult = siavWsCard.GetCardBundle(out oCardBundle,wcfSiavLoginManager.oSessionInfo.SessionId, lGUID[0], false);

                    if (oResult == ResultInfo.OK)
                    {
                        Console.WriteLine("GetCardBundle call OK");
                        //cardParts
                        if (oCardBundle != null)
                        {

                        }
                    }
                    else
                    {
                        oEsito.iCode = 0;
                        oEsito.sDescription = "Motivo: Problemi con la ricezione della scheda: " + sIdProtocollo;
                        throw new ArgumentException(oEsito.sDescription);
                    }
                }
            }
            catch (FaultException<ArchiflowServiceExceptionDetail> ex)
            {
                oEsito.iCode = 0;
                oEsito.sDescription = "Motivo: " + ex.Detail.Message;
                throw new ArgumentException(oEsito.sDescription);
            }
            return oEsito;
        }
        public Outcome GetCardMainDoc(string path, CardBundle oCardBundle, WcfSiavLoginManager wcfSiavLoginManager, out string sPathMainDoc)
        {
			Outcome oEsito = new Outcome();
            oEsito.iCode = 1;
            sPathMainDoc = null;
            string sGuidCard = "";
            // create the connection info
            ResultInfo oResult = ResultInfo.OK;
            try
            {
                if (oCardBundle != null)
                {
                    if (oCardBundle.HasDocument)
                    {
                        sGuidCard = oCardBundle.CardId.ToString();
                         // save the attachment content to disk
                        byte[] oDocBytes = Convert.FromBase64String(oCardBundle.MainDocument.Content);
                        sPathMainDoc = path + @"\" + oCardBundle.MainDocument.OriginalFileName;
                        File.WriteAllBytes(sPathMainDoc, oDocBytes);
                    }
                    else
                        throw new ArgumentException("La scheda non ha alcun documento principale.");
                }
                else
                {
                    oEsito.iCode = 0;
                    oEsito.sDescription = "Motivo: Problemi l'estrapolazione del documento principale per la scheda: " + sGuidCard;
                    throw new ArgumentException(oEsito.sDescription);
                }
            }
            catch (FaultException<ArchiflowServiceExceptionDetail> ex)
            {
                oEsito.iCode = 0;
                oEsito.sDescription = "Motivo: " + ex.Detail.Message;
                throw new ArgumentException(oEsito.sDescription);
            }
            return oEsito;
        }

        public Outcome GetCardInternalAttachments(string sGuidCard, WcfSiavLoginManager wcfSiavLoginManager, out List<WsCard.InternalAttachment> lPathAttachments)
        {
			Outcome oEsito = new Outcome();
            oEsito.iCode= 1;
            lPathAttachments = null;
            // create the connection info
            ResultInfo oResult = ResultInfo.OK;
            try
            {
                Guid oCardId;
                if (sGuidCard.Length > 12)                 // set the guid of the card
                    oCardId = new Guid("64556990-b196-425c-a0b9-" + int.Parse(sGuidCard.Substring(24, 12)).ToString("000000000000"));
                else
                    oCardId = new Guid("64556990-b196-425c-a0b9-" + int.Parse(sGuidCard).ToString("000000000000"));
                List<WsCard.Attachment> oAttachments;
                oResult = siavWsCard.GetCardAttachments(out oAttachments,wcfSiavLoginManager.oSessionInfo.SessionId, oCardId, true);

                if (oResult == ResultInfo.OK)
                {
                    if (oAttachments != null && oAttachments.Count > 0)
                    {
                        lPathAttachments = new List<WsCard.InternalAttachment>();
                        foreach (WsCard.Attachment oAttachment in oAttachments)
                        {
                            if (oAttachment is WsCard.InternalAttachment)
                            {
								WsCard.InternalAttachment oIntAttachment = (WsCard.InternalAttachment)oAttachment;
								WsCard.InternalAttachment internalAttachment = new WsCard.InternalAttachment();
                                internalAttachment.ArchiveId=oIntAttachment.ArchiveId;
                                internalAttachment.Name=oIntAttachment.Name;
                                internalAttachment.Note=oIntAttachment.Note;
                                internalAttachment.AttachmentCardId= oIntAttachment.AttachmentCardId;
                                lPathAttachments.Add(internalAttachment);
                            }
                        }
                    }
                }
                else
                {
                    oEsito.iCode = 0;
                    oEsito.sDescription= "Motivo: Problemi l'estrapolazione degli allegati della scheda: " + sGuidCard;
                    throw new ArgumentException(oEsito.sDescription);
                }
            }
            catch (FaultException<ArchiflowServiceExceptionDetail> ex)
            {
                oEsito.iCode = 0;
                oEsito.sDescription = "Motivo: " + ex.Detail.Message;
                throw new ArgumentException(oEsito.sDescription);
            }
            return oEsito;
        }

        public Outcome GetCardAdditionalFields(string sGuidCard, WcfSiavLoginManager wcfSiavLoginManager, out List<Model.AdditionalField> lAdditionalFields)
        {

			Outcome oEsito = new Outcome();
            oEsito.iCode = 1;
            lAdditionalFields = null;
            // create the connection info
            ResultInfo oResult = ResultInfo.OK;
            try
            {
                Guid oCardId;
                if (sGuidCard.Length > 12)                 // set the guid of the card
                    oCardId = new Guid("64556990-b196-425c-a0b9-" + int.Parse(sGuidCard.Substring(24, 12)).ToString("000000000000"));
                else
                    oCardId = new Guid("64556990-b196-425c-a0b9-" + int.Parse(sGuidCard).ToString("000000000000"));
                List<Additive> oAdditives;
                oResult = siavWsCard.GetCardAdditives(out oAdditives,wcfSiavLoginManager.oSessionInfo.SessionId, oCardId);

                if (oResult == ResultInfo.OK)
                {
                    Console.WriteLine("GetCardAdditives call OK");
                    Console.WriteLine("Card's Additives:");

                    if (oAdditives != null && oAdditives.Count > 0)
                    {
                        lAdditionalFields = new List<AdditionalField>();
                        foreach (Additive oAdditive in oAdditives)
                        {
                            Model.AdditionalField additionalField = new Model.AdditionalField();
                            additionalField.Id = oAdditive.AdditiveId.ToString();
                            additionalField.Name = oAdditive.AdditiveName;
                            additionalField.Value = oAdditive.AdditiveValue;
                            lAdditionalFields.Add(additionalField);
                        }
                    }
                }
                else
                {
                    oEsito.iCode = 0;
                    oEsito.sDescription = "Motivo: Problemi l'estrapolazione dei campi aggiuntivi della scheda: " + sGuidCard;
                    throw new ArgumentException(oEsito.sDescription);
                }
            }
            catch (FaultException<ArchiflowServiceExceptionDetail> ex)
            {
                oEsito.iCode = 0;
                oEsito.sDescription = "Motivo: " + ex.Detail.Message;
                throw new ArgumentException(oEsito.sDescription);
            }
            return oEsito;
        }

        public Outcome GetCardAttachments(string path, string sGuidCard, WcfSiavLoginManager wcfSiavLoginManager, out List<string> sPathAttachments)
        {
			Outcome oEsito = new Outcome();
            oEsito.iCode = 1;
            sPathAttachments = null;
            // create the connection info
            ResultInfo oResult = ResultInfo.OK;
            try
            {
                Guid oCardId;
                if (sGuidCard.Length > 12)                 // set the guid of the card
                    oCardId = new Guid("64556990-b196-425c-a0b9-" + int.Parse(sGuidCard.Substring(24, 12)).ToString("000000000000"));
                else
                    oCardId = new Guid("64556990-b196-425c-a0b9-" + int.Parse(sGuidCard).ToString("000000000000"));
                List<WsCard.Attachment> oAttachments;
                oResult = siavWsCard.GetCardAttachments(out oAttachments,wcfSiavLoginManager.oSessionInfo.SessionId, oCardId, true);

                if (oResult == ResultInfo.OK)
                {
                    if (oAttachments != null && oAttachments.Count > 0)
                    {
                        sPathAttachments = new List<string>();
                        foreach (WsCard.Attachment oAttachment in oAttachments)
                        {
                            if (oAttachment is ExternalAttachment)
                            {
                                ExternalAttachment oExtAttachment = (ExternalAttachment)oAttachment;
                                // save the attachment content to disk
                                byte[] oDocBytes = Convert.FromBase64String(oExtAttachment.Content);
                                string sAttachDocX = path + @"\" + oExtAttachment.Name;
                                File.WriteAllBytes(sAttachDocX, oDocBytes);
                                sPathAttachments.Add(sAttachDocX);
                            }
                        }
                    }
                }
                else
                {
                    oEsito.iCode = 0;
                    oEsito.sDescription = "Motivo: Problemi l'estrapolazione degli allegati della scheda: " + sGuidCard;
                    throw new ArgumentException(oEsito.sDescription);
                }
            }
            catch (FaultException<ArchiflowServiceExceptionDetail> ex)
            {
                oEsito.iCode = 0;
                oEsito.sDescription = "Motivo: " + ex.Detail.Message;
                throw new ArgumentException(oEsito.sDescription);
            }
            return oEsito;
        }
        public Outcome GetCardFull(string sGuidCard, WcfSiavLoginManager wcfSiavLoginManager, out WsCard.Card oCard)
        {
			Outcome oEsito = new Outcome();
            oEsito.iCode = 1;
            oCard = new WsCard.Card();
            // create the connection info
            ResultInfo oResult = ResultInfo.OK;
            try
            {
                Guid oCardId;
                if (sGuidCard.Length > 12)                 // set the guid of the card
                    oCardId = new Guid("64556990-b196-425c-a0b9-" + int.Parse(sGuidCard.Substring(24, 12)).ToString("000000000000"));
                else
                    oCardId = new Guid("64556990-b196-425c-a0b9-" + int.Parse(sGuidCard).ToString("000000000000"));

                // call the WCF service contract to get the a card bundle
                oResult = siavWsCard.GetCard(out oCard,wcfSiavLoginManager.oSessionInfo.SessionId, oCardId);

                if (oResult == ResultInfo.OK)
                {
                    
                }
                else
                {
                    oEsito.iCode = 0;
                    oEsito.sDescription = "Motivo: Problemi con la ricezione della scheda: " + sGuidCard;
                    throw new ArgumentException(oEsito.sDescription);
                }
            }
            catch (FaultException<ArchiflowServiceExceptionDetail> ex)
            {
                oEsito.iCode = 0;
                oEsito.sDescription = "Motivo: " + ex.Detail.Message;
                throw new ArgumentException(oEsito.sDescription);
            }
            return oEsito;
        }
        public Outcome GetIndexes(CardBundle oCardBundle, out NameValueCollection indexes)
        {
            indexes = new NameValueCollection();
			Outcome oEsito = new Outcome();
            oEsito.iCode = 1;
            // create the connection info
            try
            {
                if (oCardBundle != null)
                {
                    foreach (Field oField in oCardBundle.Indexes){
                        /*if (oField.FieldDataType.ToString() == "DtDate"){
                            DateTime FormattedDate = DateTime.ParseExact(oField.FieldValue, "dd/MM/yyyy", new CultureInfo("it-IT"));
                            oField.FieldValue = FormattedDate.ToString();
                        }*/
                        indexes.Add(oField.FieldDescription, oField.FieldValue);
                    }
                }
            }
            catch (FaultException<ArchiflowServiceExceptionDetail> ex)
            {
                oEsito.iCode = 0;
                oEsito.sDescription = "Motivo: " + ex.Detail.Message;
                throw new ArgumentException(oEsito.sDescription);
            }
            return oEsito;
        }
        public Outcome GetIndexes(Guid oGuidCard, WcfSiavLoginManager wcfSiavLoginManager, out NameValueCollection indexes)
        {
            indexes = new NameValueCollection();
			Outcome oEsito = new Outcome();
            oEsito.iCode = 1;
            // create the connection info
            List<Field> oTndexes;

            try
            {
                if (oGuidCard != null)
                {
                    ResultInfo oResult = ResultInfo.OK;
                    oResult = siavWsCard.GetCardIndexes(out oTndexes,wcfSiavLoginManager.oSessionInfo.SessionId, oGuidCard);

                    if (oResult == ResultInfo.OK)
                    {
                        if (oTndexes != null && oTndexes.Count > 0)
                        {
                            foreach (Field oField in oTndexes)
                                indexes.Add(oField.FieldDescription, oField.FieldValue);
                        }
                    }
                    else
                        throw new ArgumentException("Errore durante la chiamata che ritorna gli indici della scheda.");
                }
            }
            catch (FaultException<ArchiflowServiceExceptionDetail> ex)
            {
                oEsito.iCode = 0;
                oEsito.sDescription = "Motivo: " + ex.Detail.Message;
                throw new ArgumentException(oEsito.sDescription);
            }
            return oEsito;
        }
        public Outcome GetMainDoc(CardBundle oCardBundle, out MainDoc oMainDoc)
        {
            oMainDoc = new MainDoc();
			Outcome oEsito = new Outcome();
            oEsito.iCode= 1;
            // create the connection info
            try
            {
                //cardParts
                if (oCardBundle != null)
                {
                    if (oCardBundle.HasDocument)
                    {
                        oMainDoc.Extension = oCardBundle.MainDocument.DocumentExtension;
                        oMainDoc.Filename = oCardBundle.MainDocument.DocumentTitle;
                        oMainDoc.IsSigned = oCardBundle.MainDocument.IsSigned;
                        // save the main document content to disk
                        oMainDoc.oByte = Convert.FromBase64String(oCardBundle.MainDocument.Content);
                    }
                    else
                        throw new ArgumentException("La scheda non ha alcun documento principale.");
                }
            }
            catch (FaultException<ArchiflowServiceExceptionDetail> ex)
            {
                oEsito.iCode = 0;
                oEsito.sDescription = "Motivo: " + ex.Detail.Message;
                throw new ArgumentException(oEsito.sDescription);
            }
            return oEsito;
        }

        public Outcome GetMainDoc(Guid oGuidCard, WsLogin.SessionInfo oSessionInfo, out MainDoc oMainDoc)
        {
            oMainDoc = new MainDoc();
            ResultInfo oResult = ResultInfo.OK;
			Outcome oEsito = new Outcome();
            oEsito.iCode = 1;
            Document oMainDocument;

            // create the connection info
            try
            {
                //cardParts
                if (oGuidCard != null)
                {
                    oResult = siavWsCard.GetCardDocument(out oMainDocument, oSessionInfo.SessionId, oGuidCard, false);

                    if (oResult == ResultInfo.OK)
                    {
                        if (oMainDocument != null)
                        {
                            oMainDoc.Extension = oMainDocument.DocumentExtension;
                            oMainDoc.Filename = oMainDocument.DocumentTitle;
                            oMainDoc.IsSigned = oMainDocument.IsSigned;
                            // save the main document content to disk
                            oMainDoc.oByte = Convert.FromBase64String(oMainDocument.Content);
                        }
                        else
                            throw new ArgumentException("La scheda non ha alcun documento principale.");
                    }
                    else
                        throw new ArgumentException("Chimamata ricezione documento principale avvenuta con errore");
                }
            }
            catch (FaultException<ArchiflowServiceExceptionDetail> ex)
            {
                oEsito.iCode = 0;
                oEsito.sDescription = "Motivo: " + ex.Detail.Message;
                throw new ArgumentException(oEsito.sDescription);
            }
            return oEsito;
        }
        public Outcome getArchiveDoc(string archivio, WsLogin.SessionInfo oSessionInfo, out Archive oArchive)
        {
			Outcome oEsito = new Outcome();
            oEsito.iCode = 1;
            oArchive = new Archive();
            // call the WCF service contract for login to Archiflow
            ResultInfo oResult = ResultInfo.OK;

            try
            {
                List<Archive> oArchives;
                Boolean bFounded = false;

                // call the WCF service contract to get the all the archives in the system
                oResult = siavWsCard.GetArchives(out oArchives,oSessionInfo.SessionId, AccessLevel.AlAny, true);
                if (oResult == ResultInfo.OK)
                {
                    if (oArchives != null && oArchives.Count > 0)
                    {
                        
                        foreach (Archive oInArchive in oArchives)
                            if (oInArchive.ArchiveName.ToUpper() == archivio.ToUpper())
                            {
                                bFounded = true;
                                oArchive = oInArchive;
                                break;
                            }
                    }
                    if (bFounded)
                    {
                        oEsito.iCode = 1;
                    }
                    else
                    {
                        oEsito.iCode = 0;
                        oEsito.sDescription = "Archivio inesistente";
                        throw new ArgumentException(oEsito.sDescription);
                    }
                }
                else
                {
                    oEsito.iCode = 0;
                    oEsito.sDescription = "Nessun Archivio presente";
                    throw new ArgumentException(oEsito.sDescription);
                }
            }
            catch (FaultException<ArchiflowServiceExceptionDetail> ex)
            {
				oEsito.iCode = 0;
                oEsito.sDescription = "Motivo: " + ex.Detail.Message;
                oResult = ResultInfo.SERVER_ERROR;
                throw new ArgumentException(oEsito.sDescription);
            }
            return oEsito;
        }
        public Outcome getTypeDoc(string tipologiaDocumentale, WsLogin.SessionInfo oSessionInfo, out DocumentType oDocumentType)
        {
			Outcome oEsito = new Outcome();
            oEsito.iCode = 1;
            oDocumentType = new DocumentType();
            // call the WCF service contract for login to Archiflow
            ResultInfo oResult = ResultInfo.OK;

            try
            {
                List<DocumentType> oDocumentTypes;
                Boolean bFounded = false;

                DocumentType oDocumentTypeSearch = new DocumentType();
                        // call the WCF service contract to get the all the document types in the system
                oResult = siavWsCard.GetDocumentTypes(out oDocumentTypes,oSessionInfo.SessionId, AccessLevel.AlAny, true, true);
                if (oResult == ResultInfo.OK)
                {
                    if (oDocumentTypes != null && oDocumentTypes.Count > 0)
                    {
                        
                        foreach (DocumentType oInDocumentType in oDocumentTypes)
                            if (oInDocumentType.DocumentTypeName.ToUpper() == tipologiaDocumentale.ToUpper())
                            {
                                bFounded = true;
                                oDocumentType = oInDocumentType;
                                break;
                            }
                        if (bFounded)
                        {
                            oEsito.iCode = 1;
                        }
                        else
                        {
                            oEsito.iCode = 0;
                            oEsito.sDescription = "Tipologia Documentale inesistente";
                            throw new ArgumentException(oEsito.sDescription);
                        }
                    }
                    else
                    {
                        oEsito.iCode = 0;
                        oEsito.sDescription = "Nessuna Tipologia Documentale presente";
                        throw new ArgumentException(oEsito.sDescription);
                    }
                // call the WCF service contract to get the all the archives in the system
                }
                else
                {
                    oEsito.iCode = 0;
                    oEsito.sDescription = "Errore nella ricezione delle tipologie documentali";
                    throw new ArgumentException(oEsito.sDescription);
                }
            }
            catch (FaultException<ArchiflowServiceExceptionDetail> ex)
            {
                oEsito.iCode = 0;
                oEsito.sDescription = "Motivo: " + ex.Detail.Message;
                oResult = ResultInfo.SERVER_ERROR;
                throw new ArgumentException(oEsito.sDescription);
            }
            return oEsito;
        }
        public Outcome getTypeDoc(short idTipologiaDocumentale, WsLogin.SessionInfo oSessionInfo, out DocumentType oDocumentType)
        {
			Outcome oEsito = new Outcome();
            oEsito.iCode = 1;
            oDocumentType = new DocumentType();
            // call the WCF service contract for login to Archiflow
            ResultInfo oResult = ResultInfo.OK;

            try
            {
                List<DocumentType> oDocumentTypes;
                Boolean bFounded = false;

                DocumentType oDocumentTypeSearch = new DocumentType();
                // call the WCF service contract to get the all the document types in the system
                 oResult = siavWsCard.GetDocumentTypes(out oDocumentTypes,oSessionInfo.SessionId, AccessLevel.AlAny, true, true);
                if (oResult == ResultInfo.OK)
                {
                    if (oDocumentTypes != null && oDocumentTypes.Count > 0)
                    {

                        foreach (DocumentType oInDocumentType in oDocumentTypes)
                            if (oInDocumentType.DocumentTypeId == idTipologiaDocumentale)
                            {
                                bFounded = true;
                                oDocumentType = oInDocumentType;
                                break;
                            }
                        if (bFounded)
                        {
                            oEsito.iCode = 1;
                        }
                        else
                        {
                            oEsito.iCode = 0;
                            oEsito.sDescription = "Tipologia Documentale inesistente";
                            throw new ArgumentException(oEsito.sDescription);
                        }
                    }
                    else
                    {
                        oEsito.iCode = 0;
                        oEsito.sDescription = "Nessuna Tipologia Documentale presente";
                        throw new ArgumentException(oEsito.sDescription);
                    }
                    // call the WCF service contract to get the all the archives in the system
                }
                else
                {
                    oEsito.iCode = 0;
                    oEsito.sDescription = "Errore nella ricezione delle tipologie documentali";
                    throw new ArgumentException(oEsito.sDescription);
                }
            }
            catch (FaultException<ArchiflowServiceExceptionDetail> ex)
            {
                oEsito.iCode = 0;
                oEsito.sDescription = "Motivo: " + ex.Detail.Message;
                oResult = ResultInfo.SERVER_ERROR;
                throw new ArgumentException(oEsito.sDescription);
            }
            return oEsito;
        }
        public Outcome getSearch(List<SchedaSearchParameter> schedeSearchParameter, WcfSiavLoginManager wcfSiavLoginManager, out List<Guid> oCardGuids)
        {
			Outcome oEsito = new Outcome();
            /// add a reference to the Card WCF service called CardService into the project
            Boolean bProblemSearch = new Boolean();
            WsLogin.SessionInfo oSessionInfo = wcfSiavLoginManager.oSessionInfo;
            bProblemSearch = false;
            // call the WCF service contract for login to Archiflow
            ResultInfo oResult = ResultInfo.OK;
            oCardGuids = new List<Guid>();
            List<Guid> oCardTemp = new List<Guid>();
            try
            {
                for (int i = 0; i < schedeSearchParameter.Count; i++)
                {
                    List<Field> oFieldsSearch = new List<Field>();
                    List<Archive> oArchivesSearch = new List<Archive>();
                    SearchCriteria oSearchCriteria = new SearchCriteria();
                    Archive oArchiveSearch = new Archive();
                    DocumentType oDocumentTypeSearch = new DocumentType();
                    if (!string.IsNullOrEmpty(schedeSearchParameter[i].TipologiaDocumentale))
                    {
                        this.getTypeDoc(schedeSearchParameter[i].TipologiaDocumentale, oSessionInfo, out oDocumentTypeSearch);
                        // set the SearchCriteria document type
                        oSearchCriteria.DocumentType = oDocumentTypeSearch;
                    }
                    if (!string.IsNullOrEmpty(schedeSearchParameter[i].Archivio))
                    {
                        this.getArchiveDoc(schedeSearchParameter[i].Archivio, oSessionInfo, out oArchiveSearch);
                    }
                    else if (!string.IsNullOrEmpty(schedeSearchParameter[i].ArchivioId))
                    {
                        oDocumentTypeSearch.DocumentTypeId = 32767;
                        oSearchCriteria.DocumentType = oDocumentTypeSearch;
                        oArchiveSearch.ArchiveId = short.Parse(schedeSearchParameter[i].ArchivioId);
                    }
                    oArchivesSearch.Add(oArchiveSearch);
                    oSearchCriteria.Archives = oArchivesSearch;

                    if (String.IsNullOrEmpty(schedeSearchParameter[i].Oggetto) == false)
                    {
                        Field oFieldSearch = new Field();
                        oFieldSearch.FieldId = IdField.IfObj;
                        oFieldSearch.FieldValue = schedeSearchParameter[i].Oggetto;
                        oFieldSearch.FieldValueTo = schedeSearchParameter[i].Oggetto;
                        oFieldsSearch.Add(oFieldSearch);
                    }
                    
                    if (String.IsNullOrEmpty(schedeSearchParameter[i].CampoId) == false)
                    {
                        Field oFieldSearch = new Field();
                        oFieldSearch.FieldId = IdField.IfProtocol;
                        oFieldSearch.FieldValue = schedeSearchParameter[i].CampoId;
                        oFieldSearch.FieldValueTo = schedeSearchParameter[i].CampoId;
                        oFieldsSearch.Add(oFieldSearch);
                    }
                    if (String.IsNullOrEmpty(schedeSearchParameter[i].IdRiferimento) == false)
                    {
                        Field oFieldSearch = new Field();
                        oFieldSearch.FieldId = IdField.IfReference;
                        oFieldSearch.FieldValue = schedeSearchParameter[i].IdRiferimento;
                        oFieldSearch.FieldValueTo = schedeSearchParameter[i].IdRiferimento;
                        oFieldsSearch.Add(oFieldSearch);
                    }
                    if (String.IsNullOrEmpty(schedeSearchParameter[i].DataDocumento) == false)
                    {
                        Field oFieldSearch = new Field();
                        oFieldSearch.FieldId = IdField.IfDateDoc;
                        oFieldSearch.FieldValue = schedeSearchParameter[i].DataDocumento;
                        oFieldSearch.FieldValueTo = schedeSearchParameter[i].DataDocumento;
                        oFieldsSearch.Add(oFieldSearch);
                    }
                    if (schedeSearchParameter[i].MetaDati != null)
                    {
                        for (int j = 0; j < schedeSearchParameter[i].MetaDati.Count; j++)
                        {
                            if (String.IsNullOrEmpty(schedeSearchParameter[i].MetaDati[j]) == false)
                            {
                                Field oFieldSearch = new Field();
                                oFieldSearch.FieldId = getTypeIdField(j);
                                oFieldSearch.FieldValue = schedeSearchParameter[i].MetaDati[j];
                                oFieldSearch.FieldValueTo = schedeSearchParameter[i].MetaDati[j];
                                oFieldsSearch.Add(oFieldSearch);
                            }
                        }
                    }
                        // set the indexes values to search for
                    oSearchCriteria.Fields = oFieldsSearch;

                    // search only for cards in archives (not sorted)
                    oSearchCriteria.Context = SearchContext.ScArchive;

                    // index search 
                    oSearchCriteria.SearchType = SearchType.StIndexes;

                    // search for all cards (including the cards that has no main document attached)
                    oSearchCriteria.CardWithOutDoc = false;

                    // retrieve only the first 10 cards foundex
                    oSearchCriteria.MaxFounded = 0;

                    // call the WCF service contract to search for the cards
                    oResult = siavWsCard.SearchCards(out oCardTemp, oSessionInfo.SessionId, oSearchCriteria);
                    if (oResult == ResultInfo.OK)
                    {
                        oCardGuids.AddRange(oCardTemp);
                    }
                    else
                    {
                        bProblemSearch = true;
                        break;
                    }
                }
                
                if (bProblemSearch == false)
                {
                    oEsito.iCode = 1;
                    oEsito.sDescription = "Ricerca eseguita con successo.";
                }
                else
                {
                    oEsito.iCode = 0;
                    oEsito.sDescription = "Errore durante la ricerca della scheda";
                    throw new ArgumentException(oEsito.sDescription);
                }
            }
            catch (FaultException<ArchiflowServiceExceptionDetail> ex)
            {
                oEsito.iCode = 0;
                oEsito.sDescription = "Motivo: " + ex.Detail.Message;
                oResult = ResultInfo.SERVER_ERROR;
                throw new ArgumentException(oEsito.sDescription);
            }
            catch (Exception ex)
            {
                oEsito.iCode = 0;
                oEsito.sDescription = "Motivo: " + ex.Message;
                oResult = ResultInfo.SERVER_ERROR;
                throw new ArgumentException(oEsito.sDescription);
            }
            return oEsito;
        }
        public void FieldModify(CardBundle oCardBundle, WcfSiavLoginManager wcfSiavLoginManager,int iIndexField, string sValueField)
        {
            ResultInfo oResult = ResultInfo.OK;
            // set the values of the fields modify
            List<Field> oFields = new List<Field>();
           
            Field oField = new Field();
            oField.FieldId = getTypeIdField(iIndexField);
            oField.FieldValue = sValueField;
            //oField.FieldValueTo = sValueField;
            oFields.Add(oField);

            Archive oArchive = new Archive();
            oArchive.ArchiveId = oCardBundle.ArchiveId;
            DocumentType oDocumentType = new DocumentType();
            oDocumentType.DocumentTypeId = oCardBundle.DocumentTypeId;
            WsCard.SessionInfo oSessionInfo = new SessionInfo();
            oSessionInfo.SessionId = wcfSiavLoginManager.oSessionInfo.SessionId;
            // call the WCF service contract set the card indexes
            oResult = siavWsCard.SetCardIndexes(oSessionInfo, oCardBundle.CardId, false, oFields, oArchive, oDocumentType);

            if (oResult == ResultInfo.OK)
                Console.WriteLine("SetCardIndexes call OK");
            else
                throw new ArgumentException("Errore in fase di aggiornamento scheda");


        }
        public void Insert(List<String> sPathAttachment, string sPathMainDocument, WcfSiavLoginManager wcfSiavLoginManager, FieldsCard oFieldsCard, List<Group> oGroups, List<Office> oOffices,
                                        List<User> oUsers, DocumentType oDocumentType, Archive oArchive, string strNote, string strMessage, AgrafCard svAgrafCardRub,AgrafCard svAgrafCardApf, out Guid oCardGuid)
        {
            // call the WCF service contract for login to Archiflow
            ResultInfo oResult = ResultInfo.OK;
            
            List<Field> oFieldsSearch = new List<Field>();
            oCardGuid = new Guid();
            try
            {
                CardBundle oCardBundle = new CardBundle();

                // set the archive id
                oCardBundle.ArchiveId = oArchive.ArchiveId;

                // set the archive id
                oCardBundle.DocumentTypeId = oDocumentType.DocumentTypeId;

                // Gestione indici della scheda
                List<Field> oFields = new List<Field>();
                Field oField = new Field();
                // Loop over strings
                for (int i = 0; i < oFieldsCard.MetaDati.Count; i++)
                {
                    string sValue = oFieldsCard.MetaDati[i];
                    oField = new Field();
                    oField.FieldId = getTypeIdField(i + 4);
                    oField.FieldValue = sValue;
                    oFields.Add(oField);
                }

                oField = new Field();
                oField.FieldId = IdField.IfObj;
                oField.FieldValue = oFieldsCard.Oggetto;
                oFields.Add(oField);

                oField = new Field();
                oField.FieldId = IdField.IfDateDoc;
                oField.FieldValue = oFieldsCard.DataDocumento;
                oFields.Add(oField);

                oField = new Field();
                oField.FieldId = IdField.IfProtocol;
                oField.FieldValue = oFieldsCard.CampoId;
                oFields.Add(oField);

                // set the indexes to the card
                oCardBundle.Indexes = oFields;

                // Gestione documento principale della scheda
                Document oMainDocument = new Document();
                Byte[] bytes = File.ReadAllBytes(sPathMainDocument);
                String file = Convert.ToBase64String(bytes);
                oMainDocument.Content = file;
                oMainDocument.DocumentExtension = Path.GetExtension(sPathMainDocument);
                oMainDocument.OriginalFileName = Path.GetFileName(sPathMainDocument);
                oCardBundle.MainDocument = oMainDocument;
                oCardBundle.AgrafCards = new List<AgrafCard>();
                oCardBundle.AgrafCards.Add(svAgrafCardRub);
                oCardBundle.AgrafCards.Add(svAgrafCardApf);
                
                oResult = siavWsCard.InsertCard(out oCardGuid,wcfSiavLoginManager.oSessionInfo.SessionId, oCardBundle, oUsers, oGroups, oOffices, strNote, strMessage, false, false, true, false);
                if (oResult == ResultInfo.OK)
                {
                    if (sPathAttachment!= null)
                        if (sPathAttachment.Count > 0)
                        {
                            CardBundle oCardPred= null;
                            this.GetCard(oCardGuid.ToString(),wcfSiavLoginManager,out oCardPred);
                            foreach(string singlePath in sPathAttachment){
                                this.SetAttachment(oCardPred, wcfSiavLoginManager, singlePath,"");
                            }
                        }
                    //oEsito.Codice = "1";
                    //oEsito.Descrizione = "Inserimento eseguito con successo. GUID: " + oCardGuid;
                }
                else
                {
                    //oEsito.Codice = "0";
                    //oEsito.Descrizione = "Errore in fase di inserimento scheda";
                    throw new ArgumentException("Errore in fase di inserimento scheda");
                }
            }
            catch (FaultException<ArchiflowServiceExceptionDetail> ex)
            {
                oResult = ResultInfo.SERVER_ERROR;
                throw new ArgumentException(ex.Detail.Message);
            }
            catch (Exception ex)
            {
                oResult = ResultInfo.SERVER_ERROR;
                throw new ArgumentException(ex.Message);
            }
        }

        public void GetAnagrafFromCard(CardBundle oCardBundle, WcfSiavLoginManager wcfSiavLoginManager, out List<AgrafCard> agrafCards)
        {
            // call the WCF service contract for login to Archiflow
            ResultInfo oResult = ResultInfo.OK;

            try
            {
                agrafCards = new List<AgrafCard>();
                WsCard.SessionInfo oSessionInfo = new SessionInfo();
                oSessionInfo.SessionId = wcfSiavLoginManager.oSessionInfo.SessionId;
                oResult = siavWsCard.GetContacts2(out agrafCards,oSessionInfo, oCardBundle.CardId, Guid.Empty);
                if (oResult == ResultInfo.OK)
                {
                }
                else
                {
                    throw new ArgumentException("Errore in fase di lettura anagrafica dalla scheda sorgente");
                }
            }
            catch (FaultException<ArchiflowServiceExceptionDetail> ex)
            {
                oResult = ResultInfo.SERVER_ERROR;
                throw new ArgumentException(ex.Detail.Message);
            }
            catch (Exception ex)
            {
                oResult = ResultInfo.SERVER_ERROR;
                throw new ArgumentException(ex.Message);
            }
        }

        public void CheckValidCard(CardBundle oCardBundle, out bool isValid)
        {
            try
            {
                isValid = oCardBundle.IsValid;
            }
            catch (Exception ex)
            {
                throw new ArgumentException(ex.Message);
            }
        }
		public void InsertCardProvVal(CardBundle oCardBundle, HashSet<int> lFieldNotCopy, short iArchive, short iTypeDoc, WcfSiavLoginManager wcfSiavLoginManager, out Guid oCardGuid)
		{
			// call the WCF service contract for login to Archiflow
			ResultInfo oResult = ResultInfo.OK;

			List<Field> oFieldsSearch = new List<Field>();
			oCardGuid = new Guid();
			try
			{
				CardBundle oProtCardBundle = new CardBundle();

				// set the archive id
				oProtCardBundle.ArchiveId = iArchive;

				// set the archive id
				oProtCardBundle.DocumentTypeId = iTypeDoc;

				// Gestione indici della scheda
				List<Field> oProtFields = new List<Field>();
				// Loop over strings
				if (oCardBundle != null)
				{
					Field fCodUnivoco = new Field();
					foreach (Field oField in oCardBundle.Indexes)
					{
						if (!lFieldNotCopy.Contains((int)oField.FieldId))
							oProtFields.Add(oField);
						if ((int)oField.FieldId == 4)
						{
							if (oField.FieldValue.IndexOf("MI") != -1)
								oField.FieldValue = "UACF DI MILANO";
							else
								oField.FieldValue = "UACF DI ROMA";
						}
						if ((int)oField.FieldId == 8)
						{
							fCodUnivoco = oField;
						}
						if ((int)oField.FieldId == 15)
						{
							fCodUnivoco.FieldValue = oField.FieldValue;
							fCodUnivoco.FieldValueTo = oField.FieldValue;
							oProtFields.Add(fCodUnivoco);
						}
					}
				}
				oProtCardBundle.Indexes = new List<Field>();
				// set the indexes to the card
				oProtCardBundle.Indexes = oProtFields;

				// Gestione documento principale della scheda
				if (oCardBundle.HasDocument)
				{
					oProtCardBundle.MainDocument = new Document();
					oProtCardBundle.MainDocument.DocumentExtension = oCardBundle.MainDocument.DocumentExtension;
					oProtCardBundle.MainDocument.DocumentTitle = oCardBundle.MainDocument.DocumentTitle;
					oProtCardBundle.MainDocument.IsSigned = oCardBundle.MainDocument.IsSigned;
					oProtCardBundle.MainDocument.Content = oCardBundle.MainDocument.Content;
				}
				CardVisibility oCardVisibility = new CardVisibility();
				// Modifica inserita al posto della versione commentata successiva
				List<User> oUsers = new List<User>();
				User oUser = new User();
				oUser.NormalVisibility = true;

				oUser.Code = 1;
				oUsers.Add(oUser);
				oCardVisibility.Users = oUsers;
				/* Commmentato per implementazione nuova logica di calcolo visibilità)
				
				this.GetCardVisibility(oCardBundle.CardId.ToString(), wcfSiavLoginManager, out oCardVisibility);
				*/													  
				

				List<AgrafCard> agrafCards = new List<AgrafCard>();
				oProtCardBundle.AgrafCards = new List<AgrafCard>();
				WsCard.SessionInfo oSessionInfo = new SessionInfo();
				oSessionInfo.SessionId = wcfSiavLoginManager.oSessionInfo.SessionId;
				oResult = siavWsCard.GetContacts2(out agrafCards,oSessionInfo, oCardBundle.CardId, Guid.Empty);
				if (oResult == ResultInfo.OK)
				{
					oProtCardBundle.AgrafCards = agrafCards;
					string strNote = "";
					string strMessage = "";
					oResult = siavWsCard.InsertCard(out oCardGuid,wcfSiavLoginManager.oSessionInfo.SessionId, oProtCardBundle, oCardVisibility.Users, oCardVisibility.Groups, oCardVisibility.Offices, strNote, strMessage, false, false, true, false);

					if (oResult == ResultInfo.OK)
					{
						//oEsito.Codice = "1";
						//oEsito.Descrizione = "Inserimento eseguito con successo. GUID: " + oCardGuid;
					}
					else
					{
						//oEsito.Codice = "0";
						//oEsito.Descrizione = "Errore in fase di inserimento scheda";
						throw new ArgumentException("Errore in fase di inserimento scheda");
					}
				}
				else
				{
					//oEsito.Codice = "0";
					//oEsito.Descrizione = "Errore in fase di inserimento scheda";
					throw new ArgumentException("Errore in fase di lettura anagrafica dalla scheda sorgente");
				}


			}
			catch (FaultException<ArchiflowServiceExceptionDetail> ex)
			{
				oResult = ResultInfo.SERVER_ERROR;
				throw new ArgumentException(ex.Detail.Message);
			}
			catch (Exception ex)
			{
				oResult = ResultInfo.SERVER_ERROR;
				throw new ArgumentException(ex.Message);
			}
		}
		public void InsertCard(CardBundle oCardBundle, HashSet<int> lFieldNotCopy, short iArchive,short iTypeDoc, WcfSiavLoginManager wcfSiavLoginManager,  out Guid oCardGuid)
        {
            // call the WCF service contract for login to Archiflow
            ResultInfo oResult = ResultInfo.OK;

            List<Field> oFieldsSearch = new List<Field>();
            oCardGuid = new Guid();
            try
            {
                CardBundle oProtCardBundle = new CardBundle();

                // set the archive id
                oProtCardBundle.ArchiveId = iArchive;

                // set the archive id
                oProtCardBundle.DocumentTypeId = iTypeDoc;

                // Gestione indici della scheda
                List<Field> oProtFields = new List<Field>();
                // Loop over strings
                if (oCardBundle != null)
                {
                    foreach (Field oField in oCardBundle.Indexes){
                        if (!lFieldNotCopy.Contains((int)oField.FieldId))
                            oProtFields.Add(oField);
                        if ((int)oField.FieldId == 4)
                        {
                            if (oField.FieldValue.IndexOf("MI") != -1)
                                oField.FieldValue = "UACF DI MILANO";
                            else
                                oField.FieldValue = "UACF DI ROMA";
                        }	   
                    }

                    /*
                    List<string> lSubstituteValue,
                    if (lSubstituteValue != null){
                        for(int i=0; i< lSubstituteValue.Count;i++)
                        {
                            if (!string.IsNullOrEmpty(lSubstituteValue[i]))
                            {
                                foreach (Field oField in oCardBundle.Indexes)
                                {
                                    if (i == (int)oField.FieldId)
                                        oProtFields[i].FieldValue = lSubstituteValue[i];
                                }
                            }
                        }
                    }*/
                }
                oProtCardBundle.Indexes = new List<Field>();
                // set the indexes to the card
                oProtCardBundle.Indexes = oProtFields;

                // Gestione documento principale della scheda
                if (oCardBundle.HasDocument)
                {
                    oProtCardBundle.MainDocument = new Document();
                    oProtCardBundle.MainDocument.DocumentExtension = oCardBundle.MainDocument.DocumentExtension;
                    oProtCardBundle.MainDocument.DocumentTitle = oCardBundle.MainDocument.DocumentTitle;
                    oProtCardBundle.MainDocument.IsSigned = oCardBundle.MainDocument.IsSigned;
                    oProtCardBundle.MainDocument.Content = oCardBundle.MainDocument.Content;
                }
                CardVisibility oCardVisibility = new CardVisibility();
				// Modifica inserita al posto della versione commentata successiva
				List<User> oUsers = new List<User>();
				User oUser = new User();
				oUser.NormalVisibility = true;
				oUser.Code = 1;
				oUsers.Add(oUser);
				oCardVisibility.Users = oUsers;
				/* Commmentato per implementazione nuova logica di calcolo visibilità)
                this.GetCardVisibility(oCardBundle.CardId.ToString(), wcfSiavLoginManager, out oCardVisibility);
																									   */
                List<AgrafCard>  agrafCards = new List<AgrafCard>();
                oProtCardBundle.AgrafCards = new List<AgrafCard>();
                WsCard.SessionInfo oSessionInfo = new SessionInfo();
                oSessionInfo.SessionId = wcfSiavLoginManager.oSessionInfo.SessionId;
                oResult = siavWsCard.GetContacts2(out agrafCards,oSessionInfo, oCardBundle.CardId, Guid.Empty);
                if (oResult == ResultInfo.OK)
                {
                    oProtCardBundle.AgrafCards = agrafCards;
                    string strNote = "";
                    string strMessage = "";
                    oResult = siavWsCard.InsertCard(out oCardGuid,wcfSiavLoginManager.oSessionInfo.SessionId, oProtCardBundle, oCardVisibility.Users, oCardVisibility.Groups, oCardVisibility.Offices, strNote, strMessage, false, false, true, false);

                    if (oResult == ResultInfo.OK)
                    {
                        //oEsito.Codice = "1";
                        //oEsito.Descrizione = "Inserimento eseguito con successo. GUID: " + oCardGuid;
                    }
                    else
                    {
                        //oEsito.Codice = "0";
                        //oEsito.Descrizione = "Errore in fase di inserimento scheda";
                        throw new ArgumentException("Errore in fase di inserimento scheda");
                    }
                }
                else
                {
                    //oEsito.Codice = "0";
                    //oEsito.Descrizione = "Errore in fase di inserimento scheda";
                    throw new ArgumentException("Errore in fase di lettura anagrafica dalla scheda sorgente");
                }

                
            }
            catch (FaultException<ArchiflowServiceExceptionDetail> ex)
            {
                oResult = ResultInfo.SERVER_ERROR;
                throw new ArgumentException(ex.Detail.Message);
            }
            catch (Exception ex)
            {
                oResult = ResultInfo.SERVER_ERROR;
                throw new ArgumentException(ex.Message);
            }
        }
        public void InsertCardIngiunzione(CardBundle oCardBundle, HashSet<int> lFieldNotCopy, short iArchive, short iTypeDoc, WcfSiavLoginManager wcfSiavLoginManager, out Guid oCardGuid)
        {
            // call the WCF service contract for login to Archiflow
            ResultInfo oResult = ResultInfo.OK;

            List<Field> oFieldsSearch = new List<Field>();
            oCardGuid = new Guid();
            try
            {
                CardBundle oProtCardBundle = new CardBundle();

                // set the archive id
                oProtCardBundle.ArchiveId = iArchive;

                // set the archive id
                oProtCardBundle.DocumentTypeId = iTypeDoc;

                // Gestione indici della scheda
                List<Field> oProtFields = new List<Field>();
                // Loop over strings
                if (oCardBundle != null)
                {
                    foreach (Field oField in oCardBundle.Indexes)
                    {
                        if (!lFieldNotCopy.Contains((int)oField.FieldId))
                            oProtFields.Add(oField);
                        if ((int)oField.FieldId == 4)
                        {
                            oField.FieldValue = "AMMINISTRAZIONE E CONTABILITÀ";
                        }
                    }

                    /*
                    List<string> lSubstituteValue,
                    if (lSubstituteValue != null){
                        for(int i=0; i< lSubstituteValue.Count;i++)
                        {
                            if (!string.IsNullOrEmpty(lSubstituteValue[i]))
                            {
                                foreach (Field oField in oCardBundle.Indexes)
                                {
                                    if (i == (int)oField.FieldId)
                                        oProtFields[i].FieldValue = lSubstituteValue[i];
                                }
                            }
                        }
                    }*/
                }
                oProtCardBundle.Indexes = new List<Field>();
                // set the indexes to the card
                oProtCardBundle.Indexes = oProtFields;

                // Gestione documento principale della scheda
                if (oCardBundle.HasDocument)
                {
                    oProtCardBundle.MainDocument = new Document();
                    oProtCardBundle.MainDocument.DocumentExtension = oCardBundle.MainDocument.DocumentExtension;
                    oProtCardBundle.MainDocument.DocumentTitle = oCardBundle.MainDocument.DocumentTitle;
                    oProtCardBundle.MainDocument.IsSigned = oCardBundle.MainDocument.IsSigned;
                    oProtCardBundle.MainDocument.Content = oCardBundle.MainDocument.Content;
                }
                CardVisibility oCardVisibility = new CardVisibility();
				// Modifica inserita al posto della versione commentata successiva
				List<User> oUsers = new List<User>();
				User oUser = new User();
				oUser.NormalVisibility = true;

				oUser.Code = 1;
				oUsers.Add(oUser);
				oCardVisibility.Users = oUsers;
				/* Commmentato per implementazione nuova logica di calcolo visibilità)
                this.GetCardVisibility(oCardBundle.CardId.ToString(), wcfSiavLoginManager, out oCardVisibility);
				   */
                List<AgrafCard> agrafCards = new List<AgrafCard>();
                oProtCardBundle.AgrafCards = new List<AgrafCard>();
                WsCard.SessionInfo oSessionInfo = new SessionInfo();
                oSessionInfo.SessionId = wcfSiavLoginManager.oSessionInfo.SessionId;
                oResult = siavWsCard.GetContacts2(out agrafCards,oSessionInfo, oCardBundle.CardId, Guid.Empty);
                if (oResult == ResultInfo.OK)
                {
                    oProtCardBundle.AgrafCards = agrafCards;
                    string strNote = "";
                    string strMessage = "";
                    oResult = siavWsCard.InsertCard(out oCardGuid,wcfSiavLoginManager.oSessionInfo.SessionId, oProtCardBundle, oCardVisibility.Users, oCardVisibility.Groups, oCardVisibility.Offices, strNote, strMessage, false, false, true, false);

                    if (oResult == ResultInfo.OK)
                    {
                        //oEsito.Codice = "1";
                        //oEsito.Descrizione = "Inserimento eseguito con successo. GUID: " + oCardGuid;
                    }
                    else
                    {
                        //oEsito.Codice = "0";
                        //oEsito.Descrizione = "Errore in fase di inserimento scheda";
                        throw new ArgumentException("Errore in fase di inserimento scheda");
                    }
                }
                else
                {
                    //oEsito.Codice = "0";
                    //oEsito.Descrizione = "Errore in fase di inserimento scheda";
                    throw new ArgumentException("Errore in fase di lettura anagrafica dalla scheda sorgente");
                }


            }
            catch (FaultException<ArchiflowServiceExceptionDetail> ex)
            {
                oResult = ResultInfo.SERVER_ERROR;
                throw new ArgumentException(ex.Detail.Message);
            }
            catch (Exception ex)
            {
                oResult = ResultInfo.SERVER_ERROR;
                throw new ArgumentException(ex.Message);
            }
        }
        public void InsertNote(CardBundle oCardBundle, WcfSiavLoginManager wcfSiavLoginManager, string sTextAnnotation, string sAutor)
        {
            ResultInfo oResult = ResultInfo.OK;
            // set the values of the fields modify
            List<Annotation> oAnnotations = new List<Annotation>();

            Annotation oAnnotation = new Annotation();
            oAnnotation.Author = sAutor;
            oAnnotation.CardId = oCardBundle.CardId;
            oAnnotation.Date = DateTime.Now;
            oAnnotation.Text = sTextAnnotation;
            // the note is visible to all users
            oAnnotation.VisAll = true;
            oAnnotations.Add(oAnnotation);

            // call the WCF service contract to get the list items of a list
            oResult = siavWsCard.SetCardNotes(wcfSiavLoginManager.oSessionInfo.SessionId, oCardBundle.CardId, oAnnotations);
            if (oResult == ResultInfo.OK)
                Console.WriteLine("SetCardIndexes call OK");
            else
                throw new ArgumentException("Errore in fase di inserimento annotazione");
        }

        public void SetMainDoc(CardBundle oCard, WcfSiavLoginManager wcfSiavLoginManager, string strDocumentFullPath)
        {
            // create the connection info
            ResultInfo oResult = ResultInfo.OK;
            try
            {

                // build the document
                Document oDocument = new Document();
                oDocument.CardId = oCard.CardId;
                oDocument.DocumentExtension = Path.GetExtension(strDocumentFullPath);

                //read the file
                byte[] oBytes = File.ReadAllBytes(strDocumentFullPath);

                // convert to base64
                oDocument.Content = Convert.ToBase64String(oBytes);

                // call the WCF service contract to import the document into card
                oResult = siavWsCard.ImportDocument(wcfSiavLoginManager.oSessionInfo.SessionId, oCard.CardId, oDocument, true, false);

                if (oResult == ResultInfo.OK)
                {

                }
                else
                {
                    throw new ArgumentException("Errore durante l'aggiornamento del documento principale della scheda");
                }
            }
            catch (FaultException<ArchiflowServiceExceptionDetail> ex)
            {
                throw new ArgumentException(ex.Message);
            }
        }




		public void SetMainDoc(CardBundle oCard, WcfSiavLoginManager wcfSiavLoginManager, string documentName, string binaryContent)
		{
			// create the connection info
			ResultInfo oResult = ResultInfo.OK;
			try
			{

				// build the document
				Document oDocument = new Document();
				oDocument.CardId = oCard.CardId;
				oDocument.DocumentExtension = Path.GetExtension(documentName);
				oDocument.Content = binaryContent;

				// call the WCF service contract to import the document into card
				oResult = siavWsCard.ImportDocument(wcfSiavLoginManager.oSessionInfo.SessionId, oCard.CardId, oDocument, true, false);

				if (oResult == ResultInfo.OK)
				{

				}
				else
				{
					throw new ArgumentException("Errore durante l'aggiornamento del documento principale della scheda");
				}
			}
			catch (FaultException<ArchiflowServiceExceptionDetail> ex)
			{
				throw new ArgumentException(ex.Message);
			}
		}




		// da terminare!!!!!
		public bool SetAttachmentIntenal(Guid oCard, WcfSiavLoginManager wcfSiavLoginManager, Guid oGuidCard, string sNumeroProtocollo, string idArchive, string note)
        {
            bool bResult = false;
            // create the connection info
            ResultInfo oResult = ResultInfo.OK;
            try
            {
                WsCard.InternalAttachment oInternalAttachment = new WsCard.InternalAttachment();
                oInternalAttachment.ArchiveId = short.Parse(idArchive);
                oInternalAttachment.CardId = oGuidCard;
                // the referenced card progressive 
                oInternalAttachment.Name = sNumeroProtocollo;
                oInternalAttachment.Note = note;

                // call the WCF service contract to attach an external document to the card
                oResult = siavWsCard.AttachDocument(wcfSiavLoginManager.oSessionInfo.SessionId, oCard, oInternalAttachment, true, false);
                if (oResult == ResultInfo.OK)
                {
                    bResult = true;
                }
                else
                {
                    throw new ArgumentException("Errore durante l'aggiornamento degli indici della scheda");
                }
            }
            catch (FaultException<ArchiflowServiceExceptionDetail> ex)
            {
                throw new ArgumentException(ex.Message);
            }
            return bResult;
        }
        public bool SetAttachment(CardBundle oCard, WcfSiavLoginManager wcfSiavLoginManager, string strDocumentFullPath, string note)
        {
            bool bResult = false;
            // create the connection info
            ResultInfo oResult = ResultInfo.OK;
            try
            {

                ExternalAttachment oExternalAttachment = new ExternalAttachment();
                oExternalAttachment.CardId = oCard.CardId;
                oExternalAttachment.Name = Path.GetFileName(strDocumentFullPath);
                oExternalAttachment.Note = note;

                // read the file
                byte[] oBytes = File.ReadAllBytes(strDocumentFullPath);

                // convert to base64
                oExternalAttachment.Content = Convert.ToBase64String(oBytes);

                // call the WCF service contract to attach an external document to the card
                oResult = siavWsCard.AttachDocument(wcfSiavLoginManager.oSessionInfo.SessionId, oCard.CardId, oExternalAttachment, false, false);

                if (oResult == ResultInfo.OK)
                {
                    bResult = true;
                }
                else
                {
                    throw new ArgumentException("Errore durante l'aggiornamento degli indici della scheda");
                }
            }
            catch (FaultException<ArchiflowServiceExceptionDetail> ex)
            {
                throw new ArgumentException(ex.Message);
            }
            return bResult;
        }
		public bool SetAttachment(string SGuidCard, string sConnection, string strDocumentFullPath, string note, bool isInteroperability)
		{
			bool bResult = false;
			// create the connection info
			ResultInfo oResult = ResultInfo.OK;
			try
			{
				Guid gGuidCard = Guid.Parse(_Logger.FormatID(SGuidCard));
				ExternalAttachment oExternalAttachment = new ExternalAttachment();
				oExternalAttachment.CardId = gGuidCard;// Guid.Parse(_Logger.FormatID(SGuidCard));
				oExternalAttachment.Name = Path.GetFileName(strDocumentFullPath);
				oExternalAttachment.Note = note;
				oExternalAttachment.IsInteroperability = isInteroperability;

				// read the file
				byte[] oBytes = File.ReadAllBytes(strDocumentFullPath);

				// convert to base64
				oExternalAttachment.Content = Convert.ToBase64String(oBytes);

				// call the WCF service contract to attach an external document to the card
				oResult = siavWsCard.AttachDocument(sConnection, gGuidCard, oExternalAttachment, false, false);

				if (oResult == ResultInfo.OK)
				{
					bResult = true;
				}
				else
				{
					throw new ArgumentException("Errore durante l'aggiornamento degli indici della scheda");
				}
			}
			catch (FaultException<ArchiflowServiceExceptionDetail> ex)
			{
				throw new ArgumentException(ex.Message);
			}
			return bResult;
		}
		public bool SetAttachment(string SGuidCard, string sConnection,string sNameFile, byte[] oBytes, string note, bool isInteroperability)
		{
			bool bResult = false;
			// create the connection info
			ResultInfo oResult = ResultInfo.OK;
			try
			{
				Guid gGuidCard = Guid.Parse(_Logger.FormatID(SGuidCard));
				ExternalAttachment oExternalAttachment = new ExternalAttachment();
				oExternalAttachment.CardId = gGuidCard;// Guid.Parse(_Logger.FormatID(SGuidCard));
				oExternalAttachment.Name = sNameFile;
				oExternalAttachment.Note = note;
				oExternalAttachment.IsInteroperability = isInteroperability;
				// convert to base64
				oExternalAttachment.Content = Convert.ToBase64String(oBytes);

				// call the WCF service contract to attach an external document to the card
				oResult = siavWsCard.AttachDocument(sConnection, gGuidCard, oExternalAttachment, false, false);

				if (oResult == ResultInfo.OK)
				{
					bResult = true;
				}
				else
				{
					throw new ArgumentException("Errore durante l'aggiornamento degli indici della scheda");
				}
			}
			catch (FaultException<ArchiflowServiceExceptionDetail> ex)
			{
				throw new ArgumentException(ex.Message);
			}
			return bResult;
		}
		public bool SetIndexes(CardBundle oCard, WcfSiavLoginManager wcfSiavLoginManager, List<string> lIndexes)
        {
            bool bResult = false;
            // create the connection info
            ResultInfo oResult = ResultInfo.OK;
            try
            {
                
                List<Field> oFields = new List<Field>();
                Field oField; 

                for (int i = 0; i < lIndexes.Count; i++)
                {
                    if (lIndexes[i] !=null)
                    {
                        string sValue = lIndexes[i];
                        oField = new Field();
                        oField.FieldId = getTypeIdField(i + 4);
                        oField.FieldValue = sValue;
                        oFields.Add(oField);
                    }
                }
                Archive oArchive = new Archive();
                oArchive.ArchiveId = oCard.ArchiveId;
                DocumentType oDocumentType = new DocumentType();
                oDocumentType.DocumentTypeId = oCard.DocumentTypeId;
                WsCard.SessionInfo oSessionInfo = new SessionInfo();
                oSessionInfo.SessionId = wcfSiavLoginManager.oSessionInfo.SessionId;
                //set the the text "NEW VALUE 11"
                // in the first index (Key 11)
                // call the WCF service contract set the card indexes
                oResult = siavWsCard.SetCardIndexes1(oSessionInfo, oCard.CardId, false, oFields, oArchive, oDocumentType, true);
                if (oResult == ResultInfo.OK)
                {
                    bResult = true;
                }
                else
                {
                    throw new ArgumentException("Errore durante l'aggiornamento degli indici della scheda");
                }
            }
            catch (FaultException<ArchiflowServiceExceptionDetail> ex)
            {
                throw new ArgumentException(ex.Message);
            }
            return bResult;
        }
        public static  IdField getTypeIdField(int iTypeField)
        {
            IdField xTypeID;
            switch (iTypeField)
            {
                case 1:
                    xTypeID = IdField.IfProtocol;
                    break;
                case 4:
                    xTypeID = IdField.IfKey11;
                    break;
                case 5:
                    xTypeID = IdField.IfKey12;
                    break;
                case 6:
                    xTypeID = IdField.IfKey13;
                    break;
                case 7:
                    xTypeID = IdField.IfKey14;
                    break;
                case 8:
                    xTypeID = IdField.IfKey15;
                    break;
                case 9:
                    xTypeID = IdField.IfKey21;
                    break;
                case 10:
                    xTypeID = IdField.IfKey22;
                    break;
                case 11:
                    xTypeID = IdField.IfKey23;
                    break;
                case 12:
                    xTypeID = IdField.IfKey24;
                    break;
                case 13:
                    xTypeID = IdField.IfKey25;
                    break;
                case 14:
                    xTypeID = IdField.IfKey31;
                    break;
                case 15:
                    xTypeID = IdField.IfKey32;
                    break;
                case 16:
                    xTypeID = IdField.IfKey33;
                    break;
                case 17:
                    xTypeID = IdField.IfKey34;
                    break;
                case 18:
                    xTypeID = IdField.IfKey35;
                    break;
                case 19:
                    xTypeID = IdField.IfKey41;
                    break;
                case 20:
                    xTypeID = IdField.IfKey42;
                    break;
                case 21:
                    xTypeID = IdField.IfKey43;
                    break;
                case 22:
                    xTypeID = IdField.IfKey44;
                    break;
                case 23:
                    xTypeID = IdField.IfKey45;
                    break;

                default:
                    xTypeID = IdField.Unknown;
                    break;
            }
            return xTypeID;

        }
    }
}
