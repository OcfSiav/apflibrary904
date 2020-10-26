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
using NLog;

namespace Siav.APFlibrary.Manager
{

    [ClassInterface(ClassInterfaceType.AutoDual)]
    [ComVisible(true)]

    public class CardManager : IDisposable
    {
        bool mDisposed = false; public int lErr = 0;
        public SVAOLLib.Card thisCard { get; set; }
		Logger logger;

        public CardManager(Logger oLogger, string card="",string connection="")
        {
			if (oLogger!= null)
				logger = oLogger;

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


		public static SVAOLLib.Card SendCardFromId(string CardId, string GuidConnect, SVAOLLib.Offices oOffices, SVAOLLib.Groups oGroups, SVAOLLib.Users oUsers, string SAnnotation, string sMessage, SVAOLLib.Offices oMailOffices=null, SVAOLLib.Groups oMailGroups=null, SVAOLLib.Users oMailUsers=null)
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
		public static SVAOLLib.Card SendCardFromIdCC(string CardId, string GuidConnect, SVAOLLib.Offices oOffices, SVAOLLib.Groups oGroups, SVAOLLib.Users oUsers, string SAnnotation, string sMessage, SVAOLLib.Offices oMailOffices = null, SVAOLLib.Groups oMailGroups = null, SVAOLLib.Users oMailUsers = null)
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
		public void setCardVisibilityDefaultConnLess(string sConnection, string CardId, string LogId = "")
		{
			try
			{
				//logger.Debug("Aperta connessione:" + sConnection);
				CardVisibilityManager visibilityFromCard = new CardVisibilityManager(CardId, sConnection);
				//logger.Debug("Leggo la visibilità della scheda: " + CardId);
				SVAOLLib.Offices oOffices = new SVAOLLib.Offices();
				SVAOLLib.Users oUsers = new SVAOLLib.Users();
				//WcfGdpd.WsCard.Group dValue1 = (WcfGdpd.WsCard.Group)Convert.ChangeType(visibilityFromCard.getGroupFromVisibility(resourceFileManager.getConfigData("GroupVisibilityRiservato")), typeof(WcfGdpd.WsCard.Group));
				//var elio= Convert.ChangeType(visibilityFromCard.getGroupFromVisibility(resourceFileManager.getConfigData("GroupVisibilityRiservato")), typeof(WcfGdpd.WsCard.Group));
				var groupsWCF = visibilityFromCard.getGroupsFromSharePredefinite();
				//logger.Debug("Ricavo la visibilità predefinita dai gruppi: " + sGuidCard);
				var officesWCF = visibilityFromCard.getOfficesFromSharePredefinite();
				//logger.Debug("Ricavo la visibilità predefinita dagli uffici: " + sGuidCard);
				var usersWCF = visibilityFromCard.getUsersFromSharePredefinite();
				//logger.Debug("Ricavo la visibilità predefinita dagli utenti: " + sGuidCard);
				var groupsMailWCF = visibilityFromCard.getGroupsMailFromSharePredefinite();
				//logger.Debug("Ricavo la visibilità predefinita dai gruppi con notifica: " + sGuidCard);
				var officesMailWCF = visibilityFromCard.getOfficesMailFromSharePredefinite();
				//logger.Debug("Ricavo la visibilità predefinita dagli uffici con notifica: " + sGuidCard);
				var usersMailWCF = visibilityFromCard.getUsersMailFromSharePredefinite();
				//logger.Debug("Ricavo la visibilità predefinita dagli utenti con notifica: " + sGuidCard);
				//Mapper.Map(groupWCF, groupRiservato, Group, SVAOLLib.Group);
				CardManager.SendCardFromId(CardId, sConnection, officesWCF, groupsWCF, usersWCF, "", "", officesMailWCF, groupsMailWCF, usersMailWCF);
				//logger.Debug("Condivisione eseguita: " + sGuidCard);
			}
			catch (Exception e)
			{
				throw new Exception(String.Format("{0}>>{1}>>{2}", "ERRORE : SetSingleFieldValue", e.Source, e.Message), e);
			}
		}

		public void setCardVisibilityDefault(string oUserName, string oUserPwd, string CardId, string LogId="")
		{
			string sConnection = "";
			ConnectionManager connectionManager = new ConnectionManager(logger);
			try
			{
				sConnection = connectionManager.OpenUserConnect(oUserName, oUserPwd);
				//logger.Debug("Aperta connessione:" + sConnection);
				CardVisibilityManager visibilityFromCard = new CardVisibilityManager(CardId, sConnection);
				//logger.Debug("Leggo la visibilità della scheda: " + CardId);
				SVAOLLib.Offices oOffices = new SVAOLLib.Offices();
				SVAOLLib.Users oUsers = new SVAOLLib.Users();
				//WcfGdpd.WsCard.Group dValue1 = (WcfGdpd.WsCard.Group)Convert.ChangeType(visibilityFromCard.getGroupFromVisibility(resourceFileManager.getConfigData("GroupVisibilityRiservato")), typeof(WcfGdpd.WsCard.Group));
				//var elio= Convert.ChangeType(visibilityFromCard.getGroupFromVisibility(resourceFileManager.getConfigData("GroupVisibilityRiservato")), typeof(WcfGdpd.WsCard.Group));
				var groupsWCF = visibilityFromCard.getGroupsFromSharePredefinite();
				//logger.Debug("Ricavo la visibilità predefinita dai gruppi: " + sGuidCard);
				var officesWCF = visibilityFromCard.getOfficesFromSharePredefinite();
				//logger.Debug("Ricavo la visibilità predefinita dagli uffici: " + sGuidCard);
				var usersWCF = visibilityFromCard.getUsersFromSharePredefinite();
				//logger.Debug("Ricavo la visibilità predefinita dagli utenti: " + sGuidCard);
				var groupsMailWCF = visibilityFromCard.getGroupsMailFromSharePredefinite();
				//logger.Debug("Ricavo la visibilità predefinita dai gruppi con notifica: " + sGuidCard);
				var officesMailWCF = visibilityFromCard.getOfficesMailFromSharePredefinite();
				//logger.Debug("Ricavo la visibilità predefinita dagli uffici con notifica: " + sGuidCard);
				var usersMailWCF = visibilityFromCard.getUsersMailFromSharePredefinite();
				//logger.Debug("Ricavo la visibilità predefinita dagli utenti con notifica: " + sGuidCard);
				//Mapper.Map(groupWCF, groupRiservato, Group, SVAOLLib.Group);
				CardManager.SendCardFromId(CardId, sConnection, officesWCF, groupsWCF, usersWCF, "", "", officesMailWCF, groupsMailWCF, usersMailWCF);
				//logger.Debug("Condivisione eseguita: " + sGuidCard);
				connectionManager.CloseConnect();
			}
			catch (Exception e)
			{
				if (sConnection!="")
					connectionManager.CloseConnect();
				throw new Exception(String.Format("{0}>>{1}>>{2}", "ERRORE : SetSingleFieldValue", e.Source, e.Message), e);
			}
		}

		public static string SetFieldValue(ref SVAOLLib.Fields oFields, long lIdField, string stValue, string LogId)
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
		public static SVAOLLib.Card GetCardFromId(string GuidConnect, string CardId)
		{
			SVAOLLib.Card card = new SVAOLLib.Card();
			card.GUIDconnect = GuidConnect;
			Guid oCardId;
			if (CardId.Length > 12)                 // set the guid of the card
				oCardId = new Guid("64556990-b196-425c-a0b9-" + int.Parse(CardId.Substring(24, 12)).ToString("000000000000"));
			else
				oCardId = new Guid("64556990-b196-425c-a0b9-" + int.Parse(CardId).ToString("000000000000"));
			card.GuidCard = oCardId.ToString();
			card.LoadFromGuid();
			return card;
		}
		
		public Boolean AddVisibility(string GuidConnect, string sGuidCard, string sUsers, string sOffices, string sGroups, SVAOLLib.Offices oMailOffices = null, SVAOLLib.Groups oMailGroups = null, SVAOLLib.Users oMailUsers = null)
		{
			int iStateWf = 0;
			bool bResult = false;
			bool bWorkflowActice = false;

			SVAOLLib.Offices oOfficesNotice = new SVAOLLib.Offices();
			SVAOLLib.Users oUsersNotice = new SVAOLLib.Users();
			SVAOLLib.Groups oGroupsNotice = new SVAOLLib.Groups();
			if (oMailOffices == null)
				oMailOffices = new SVAOLLib.Offices();
			if (oMailGroups == null)
				oMailGroups = new SVAOLLib.Groups();
			if (oMailUsers == null)
				oMailUsers = new SVAOLLib.Users();
			var oSession = new SVAOLLib.Session();
			ConnectionManager oConnectionManager = new ConnectionManager(logger);
			logger.Debug("Entro in connection->AddVisibility");
			oOfficesNotice = oConnectionManager.GetOffices(GuidConnect, sGuidCard, sOffices);
			oGroupsNotice = oConnectionManager.GetGroups(GuidConnect, sGroups);
			oUsersNotice = oConnectionManager.GetUsers(GuidConnect, sUsers);
			try
			{
				SVAOLLib.Card card = GetCardFromId(GuidConnect, sGuidCard);
				if (card.ProcWF != 0)
				{
					iStateWf = (int)card.ProcWF;
					card.ModifyProcWf(SVAOLLib.svProcWF.svPWFNothing);
					bWorkflowActice = true;
				}
				card.Offices = oOfficesNotice;
				card.Users = oUsersNotice;
				card.Groups = oGroupsNotice;

				card.Send(oMailOffices, oMailGroups, oMailUsers, "", "");
				if (bWorkflowActice)
				{
					card.ModifyProcWf((SVAOLLib.svProcWF)iStateWf);
				}
				bResult = true;
			}
			catch (Exception e)
			{
				lErr = -1;
				logger.Error("ERRORE: " + e.Source + " - " + e.StackTrace + " - " + e.Message);
				throw new Exception(String.Format("{0}>>{1}>>{2}", "ERRORE : RemoveVisibility", e.Source, e.Message), e);
			}
			return bResult;
		}
		public Boolean RemoveVisibility(string GuidConnect, string sGuidCard, string sUsers, string sOffices, string sGroups)
		{
			bool bResult = false;
			SVAOLLib.Offices oOfficesNotice = new SVAOLLib.Offices();
			SVAOLLib.Users oUsersNotice = new SVAOLLib.Users();
			SVAOLLib.Groups oGroupsNotice = new SVAOLLib.Groups();
			var oSession = new SVAOLLib.Session();
			ConnectionManager oConnectionManager = new ConnectionManager(logger);
			logger.Debug("Entro in connection->RemoveVisibility");
			oOfficesNotice = oConnectionManager.GetOffices(GuidConnect, sGuidCard, sOffices);
			oGroupsNotice = oConnectionManager.GetGroups(GuidConnect, sGroups);
			oUsersNotice = oConnectionManager.GetUsers(GuidConnect, sUsers);
			try
			{
				SVAOLLib.Card card = GetCardFromId(GuidConnect, sGuidCard);
				card.RemoveVisibility(oOfficesNotice, oGroupsNotice, oUsersNotice);
				bResult= true;
			}
			catch (Exception e)
			{
				lErr = -1;
				logger.Error("ERRORE: " + e.Source + " - " + e.StackTrace + " - " + e.Message);
				throw new Exception(String.Format("{0}>>{1}>>{2}", "ERRORE : RemoveVisibility", e.Source, e.Message), e);
			}
				return bResult;
		}
		public Boolean RemoveAllVisibility(string GuidConnect, string sGuidCard)
		{
			bool bResult = false;
			var oSession = new SVAOLLib.Session();
			ConnectionManager oConnectionManager = new ConnectionManager(logger);
			logger.Debug("Entro in connection->RemoveAllVisibility");
			try
			{

				CardVisibilityManager visibilityFromCard = new CardVisibilityManager(sGuidCard, GuidConnect);
				//logger.Debug("Leggo la visibilità della scheda: " + CardId);
				//WcfGdpd.WsCard.Group dValue1 = (WcfGdpd.WsCard.Group)Convert.ChangeType(visibilityFromCard.getGroupFromVisibility(resourceFileManager.getConfigData("GroupVisibilityRiservato")), typeof(WcfGdpd.WsCard.Group));
				//var elio= Convert.ChangeType(visibilityFromCard.getGroupFromVisibility(resourceFileManager.getConfigData("GroupVisibilityRiservato")), typeof(WcfGdpd.WsCard.Group));
				var groupsWCF = visibilityFromCard.getGroupsFromSharePredefinite();
				//logger.Debug("Ricavo la visibilità predefinita dai gruppi: " + sGuidCard);
				var officesWCF = visibilityFromCard.getOfficesFromSharePredefinite();
				//logger.Debug("Ricavo la visibilità predefinita dagli uffici: " + sGuidCard);
				var usersWCF = visibilityFromCard.getUsersFromSharePredefinite();
				//logger.Debug("Ricavo la visibilità predefinita dagli utenti: " + sGuidCard);
				var groupsMailWCF = visibilityFromCard.getGroupsMailFromSharePredefinite();
				//logger.Debug("Ricavo la visibilità predefinita dai gruppi con notifica: " + sGuidCard);
				var officesMailWCF = visibilityFromCard.getOfficesMailFromSharePredefinite();
				//logger.Debug("Ricavo la visibilità predefinita dagli uffici con notifica: " + sGuidCard);
				var usersMailWCF = visibilityFromCard.getUsersMailFromSharePredefinite();

				SVAOLLib.Card card = GetCardFromId(GuidConnect, sGuidCard);
				card.RemoveVisibility(officesWCF, groupsWCF, usersWCF);
				bResult = true;
			}
			catch (Exception e)
			{
				lErr = -1;
				logger.Error("ERRORE: " + e.Source + " - " + e.StackTrace + " - " + e.Message);
				throw new Exception(String.Format("{0}>>{1}>>{2}", "ERRORE : RemoveVisibility", e.Source, e.Message), e);
			}
			return bResult;
		}
		public static Boolean SetSingleFieldValue(string GuidConnect, string CardId, long FieldId, string FieldValue, string LogId)
        {
            try
            {

                SVAOLLib.Card card = GetCardFromId(GuidConnect, CardId);
                SVAOLLib.Fields fields = (SVAOLLib.Fields)card.Fields;
                string result = SetFieldValue(ref fields, Convert.ToInt64(FieldId), FieldValue, LogId);
                card.Modify(0);
                return true;
            }
            catch (Exception e)
            {
                throw new Exception(String.Format("{0}>>{1}>>{2}", "ERRORE : SetSingleFieldValue", e.Source, e.Message), e);
            }
        }
        public static List<string> SearchCardsByField(string stGuidConnect, string stArchive, long lFieldID, Int16 idTipoDocTutti, string stFieldValueFrom, string stFieldValueTo, string userId, string LogId)
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

                    //Int16 idTipoDocumento = DocManager.GetIdDocTypeByName(stGuidConnect, "Tutti", LogId);
                    oDocType.Id = idTipoDocTutti;//idTipoDocumento;
                    oDocType.GUIDconnect = stGuidConnect;
                    oDocType.LoadDocumentTypeFromId();

                    Int16 idTipoArchivio = DocManager.GetIdArchiveByName(stGuidConnect, stArchive, LogId);
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
        public static List<string> SearchCardsByField(string stGuidConnect, string stArchive, string stDocType, long lFieldID, string stFieldValueFrom, string stFieldValueTo, string userId, string LogId)
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
                    Int16 idTipoDocumento = DocManager.GetIdDocTypeByName(stGuidConnect, stDocType, LogId);
                    oDocType.Id = idTipoDocumento;
                    oDocType.GUIDconnect = stGuidConnect;
                    oDocType.LoadDocumentTypeFromId();

                    Int16 idTipoArchivio = DocManager.GetIdArchiveByName(stGuidConnect, stArchive, LogId);
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
        public static Boolean SetExternalAttach(string stGuidConnect, string stguidCardTo, string stAttachName, object aBinaryData, string note, string LogId)
        {

            //Controllo se non è stato aperto già un File di Log
            bool newcon = false;

            //è la variabile da ritornare
            Boolean SetCirAtt = false;
            //inizializzo l'oggetto session
            SVAOLLib.Session oSession = new SVAOLLib.Session();
            try
            {    //controllo se sono già connesso, in cvaso contrario mi connetto e ritorno la stringa di connessione
                if (stGuidConnect.Length != 0)
                {
                    SVAOLLib.Attachment oAttachmentTo;
                    // Se la GUIDCard non è formattata lo faccio ora           
                    stguidCardTo = CardManager.FormatID(stguidCardTo, LogId);

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
                throw new Exception(String.Format("{0}>>{1}>>{2}", "ERRORE : SetCircleAttach", e.Source, e.Message), e);
            }
            finally
            {
                if (newcon) oSession.Logout();
            }

            return SetCirAtt;
        }
		static public string FormatID(string GuidID, string LogId)
		{
			Guid oCardId;
			if (GuidID.Length > 12)                 // set the guid of the card
				oCardId = new Guid("64556990-b196-425c-a0b9-" + int.Parse(GuidID.Substring(24, 12)).ToString("000000000000"));
			else
				oCardId = new Guid("64556990-b196-425c-a0b9-" + int.Parse(GuidID).ToString("000000000000"));
			return oCardId.ToString();
		}
		static public object GetSingleFieldValue(string GuidConnect, string CardId, long FieldId, string LogId, Boolean useInternalCard = false)
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
