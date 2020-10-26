using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.IO;

namespace OCF_Ws.Manager
{
   
        [ClassInterface(ClassInterfaceType.AutoDual)]
        [ComVisible(true)]
        public class ConnectionManager : IDisposable
        {
            const string serverArchiflow = "RDS";
            const string databaseArchiflow = "ARCSQL50";
            bool mDisposed = false; public int lErr = 0;
            string sConnection;

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
				throw new ArgumentException("LOGIN NON ESEGUITO.",e);
				//throw new Exception(String.Format("{0} >> {1}: {2}", "PROBLEMA DI CONNESSIONE", e.Source, e.Message), e);
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
        }
}
