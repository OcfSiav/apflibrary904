using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.IO;
using NLog;
using Newtonsoft.Json;

namespace Siav.APFlibrary.Manager
{
   
        [ClassInterface(ClassInterfaceType.AutoDual)]
        [ComVisible(true)]
        public class ConnectionManager: IDisposable
        {
            const string serverArchiflow = "RDS";
            const string databaseArchiflow = "ARCSQL50";
            bool mDisposed = false; public int lErr = 0;
			Logger logger;
            string sConnection;

		public ConnectionManager(Logger oLogger)
		{

			if (oLogger != null)
				logger = oLogger;
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
                    Console.WriteLine("sto chiamando il metodo Dispose per la classe ConnectionManager...");
                    //Supressing the Finalization method call
                    GC.SuppressFinalize(this);

                }
                mDisposed = true;
            }

            
            /*------------------------------------------------------------------------------------------------------------
             ' FUNZIONE: OPENUSERCONNECT        (NEW VERSION DI  GetGUIDConnectEx)
             ' DESCRIZIONE:Permette di ottenere una connessione a Svaol Controlla se la stringa di conessione è presente.Nel caso tale stringa fosse null,
             ' la funzione provvede alla creazione di una nuova connessione
             '
             ' INPUT
             ' UserId        : User ID dell'utente con cui connettersi
             ' Password      : Password dell'utente con cui connettersi
             ' stServer      : Server ARCHIFLOW di collegamento - Ex: proc.ArCard.Server (RDS) | Type: STRING
             ' stDatabase    : Database ARCHIFLOW di collegamento - Ex: proc.ArCard.DataBase (ARCSQL50) | Type: STRING
             ' sUserCode     : utente che apparirà nella storia della scheda | Type: STRING 
             '
             ' OUTPUT
             ' sConn: Restituisce una GUID di connessione valida verso SvAol | Type: STRING
             '
             */
		public string GetUserConnection(string sUsername, string sPassword)//, ref string LogId)
            {
                if (string.IsNullOrEmpty(sConnection)){
				sConnection = OpenUserConnect(sUsername, sPassword);//, ref LogId);
                }
                else if (CheckConnection())
                {
                    //return sConnection;
                }
                else
                {
				sConnection = OpenUserConnect(sUsername, sPassword);//, ref LogId);
                }
                return sConnection;
            }
            public bool CheckConnection()
            {
                SVAOLLib.Session oSession;

                oSession = new SVAOLLib.Session();
                //setto i server appropriati      
                oSession.Server = serverArchiflow;
                oSession.Database = databaseArchiflow;
                oSession.GUIDconnect= sConnection;
                bool bIsValid=false;
                bIsValid = oSession.IsValidSession();
                return bIsValid;
            }
		public string OpenUserConnect(string sUserName, string sPassword)
            {
                //Controllo se non è stato aperto già un File di Log

                //sConn è la stringa di connessione da restituire
                //string sConn = null;
                SVAOLLib.Session oSession;
				oSession = new SVAOLLib.Session();
                //setto i server appropriati      
                oSession.Server = serverArchiflow;
                oSession.Database = databaseArchiflow;

				//Effettuo il login
			try
                {
					sConnection = oSession.Login(sUserName, sPassword, "");
                }
                catch (Exception e)
                {
//                    Logger.WriteOnLog(LogId, "PROBLEMA DI CONNESSIONE", 1);
//                    Logger.WriteOnLog(LogId, e.Source + " " + e.Message, 1);
                    lErr = -1;
                    throw new Exception(String.Format("{0} >> {1}: {2}", "PROBLEMA DI CONNESSIONE", e.Source, e.Message), e);
                }

                if (sConnection != null)
                {
//                    Logger.WriteOnLog(LogId, "connessione riuscita! per l'utente " + sUserName, 3);
                }
//                else Logger.WriteOnLog(LogId, "ERRORE: connessione non riuscita!", 3);
                return sConnection;
            }

            /*------------------------------------------------------------------------------------------------------------
             ' FUNZIONE: CLOSECONNECT
             ' DESCRIZIONE:Chiude una connessione verso SvAol.
             '
             '
             '
             ' INPUT
             ' stGuidConnect: guid di connessione a svaol | Type: STRING
             ' OUTPUT
             '
             */

            public void CloseConnect()
            {
                //Controllo se non è stato aperto già un File di Log
//                if (LogId == null || LogId == "") LogId = LOL.LOLIB.CodeGen(null);
///               LOL.LOLIB Logger = new LOL.LOLIB();

                if (sConnection == null){
 //                   Logger.WriteOnLog(LogId, "ATTENZIONE:non è presente alcuna stringa di connessione!", 2);
                }
                else
                {
                    //istanzio la connessione da chiudere e le passo la stringa di connessione
                    SVAOLLib.Session oSession = new SVAOLLib.Session();

                    oSession.GUIDconnect = sConnection;

                    //chiudo la connessione
                    oSession.Logout();
   //                 Logger.WriteOnLog(LogId, "Connessione chiusa", 3);
                    Dispose();
                }																		 
            }

		public SVAOLLib.Offices GetOffices(string GuidConnect, string CardId, string sOffices)
		{
			SVAOLLib.Offices oOfficesNotice = new SVAOLLib.Offices();
			var oSession = new SVAOLLib.Session();
			//setto i server appropriati      
			oSession.Server = serverArchiflow;
			oSession.Database = databaseArchiflow;
			oSession.GUIDconnect = GuidConnect;
			try
			{
				CardVisibilityManager oCardVisibilityManager = new CardVisibilityManager(CardId, GuidConnect);
				logger.Debug("Card: " + CardId);
				logger.Debug("Uffici estratti: " + oCardVisibilityManager.getXMLVisibility());
				foreach (string sOffice in sOffices.Split('|'))
				{
					if (!string.IsNullOrEmpty(sOffice))
					{
						logger.Debug("Elaboro l'ufficio: " + sOffice);
						var oOffices = oCardVisibilityManager.getOfficesFromVisibility(sOffice);
						foreach (var oOff in oOffices)
						{
							oOfficesNotice.Add(oOff);
						}
					}
				}
					return oOfficesNotice;
			}
			catch (Exception e)
			{
				lErr = -1;
				logger.Error("ERRORE: " + e.Source + " - " + e.StackTrace + " - " + e.Message);
				throw new Exception(String.Format("{0}>>{1}>>{2}", "ERRORE : GetOffices", e.Source, e.Message), e);
			}
		}
		public SVAOLLib.Users GetUsers(string GuidConnect, string sUsers)
		{
			SVAOLLib.Users oUsersNotice = new SVAOLLib.Users();
			var oSession = new SVAOLLib.Session();
			//setto i server appropriati      
			oSession.Server = serverArchiflow;
			oSession.Database = databaseArchiflow;
			oSession.GUIDconnect = GuidConnect;
			try
			{
				foreach (string sUser in sUsers.Split('|'))
				{
					if (!string.IsNullOrEmpty(sUser))
					{
						logger.Debug("Elaboro il l'utente: " + sUser);
						var oUsersFound = oSession.GetAllUsers(0);
						foreach (var oUsr in oUsersFound)
						{
							if (oUsr.UserID == sUser)
							{
								logger.Debug("Utente Trovato");
								oUsersNotice.Add(oUsr);
								break;
							}
							logger.Debug("userid: " + oUsr.UserID);
						};
					}
				}
					return oUsersNotice;
			}
			catch (Exception e)
			{
				lErr = -1;
				logger.Error("ERRORE: " + e.Source + " - " + e.StackTrace + " - " + e.Message);
				throw new Exception(String.Format("{0}>>{1}>>{2}", "ERRORE : SendNotify", e.Source, e.Message), e);
			}
		}
		public SVAOLLib.Groups GetGroups(string GuidConnect, string sGroups)
		{
			SVAOLLib.Groups oGroupsNotice = new SVAOLLib.Groups();
			var oSession = new SVAOLLib.Session();
			//setto i server appropriati      
			oSession.Server = serverArchiflow;
			oSession.Database = databaseArchiflow;
			oSession.GUIDconnect = GuidConnect;

			try
			{
				foreach (string sGroup in sGroups.Split('|'))
				{
					if (!string.IsNullOrEmpty(sGroup))
					{
						var oGroupsFound = oSession.GetAllGroups(1);
						SVAOLLib.Group ogroup = new SVAOLLib.Group();
						foreach (var oGrp in oGroupsFound)
						{
							if (oGrp.Name == sGroup)
							{
								logger.Debug("Gruppo Trovato");
								oGroupsNotice.Add(oGrp);
								break;
							}
							logger.Debug("userid: " + oGrp.Name);
						};
					}
				}
					return oGroupsNotice;
			}
			catch (Exception e)
			{
				lErr = -1;
				logger.Error("ERRORE: " + e.Source + " - " + e.StackTrace + " - " + e.Message);
				throw new Exception(String.Format("{0}>>{1}>>{2}", "ERRORE : GetGroups", e.Source, e.Message), e);
			}
		}
		public Boolean SendNotify(string GuidConnect, string CardId, string sOffices, string sGroups, string sUsers, string sMessages)
		{
			SVAOLLib.Offices oOfficesNotice = new SVAOLLib.Offices();
			SVAOLLib.Users oUsersNotice = new SVAOLLib.Users();
			SVAOLLib.Groups oGroupsNotice = new SVAOLLib.Groups();
			var oSession = new SVAOLLib.Session();
			//setto i server appropriati      
			oSession.Server = serverArchiflow;
			oSession.Database = databaseArchiflow;
			oSession.GUIDconnect = GuidConnect;
			logger.Debug("Entro in connection->SendNotify");
			oOfficesNotice = this.GetOffices(GuidConnect, CardId, sOffices);
			oGroupsNotice = this.GetGroups(GuidConnect, sGroups);
			oUsersNotice = this.GetUsers(GuidConnect, sUsers);
			try
			{
				logger.Debug("Invio il messaggio: " + sMessages);
				oSession.SendMailMessage(oOfficesNotice, oGroupsNotice, oUsersNotice, sMessages);
				return true;
			}
			catch (Exception e)
			{
				lErr = -1;
				logger.Error("ERRORE: " + e.Source + " - " + e.StackTrace + " - " + e.Message);
				throw new Exception(String.Format("{0}>>{1}>>{2}", "ERRORE : SendNotify", e.Source, e.Message), e);
			}
		}
	}
}
