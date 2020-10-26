using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using Siav.APFlibrary.Model;
using Siav.APFlibrary.SiavWsLogin;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
namespace Siav.APFlibrary.Manager
{
    public class WcfSiavLoginManager
    {
        public LoginServiceContractClient siavWsLogin;
        public Siav.APFlibrary.SiavWsLogin.SessionInfo oSessionInfo;
        private static bool customXertificateValidation(object sender, X509Certificate cert, X509Chain chain, SslPolicyErrors error)
        {
            var certificate = (X509Certificate2)cert;
            return true;
        }
        public WcfSiavLoginManager()
        {
            ServicePointManager.ServerCertificateValidationCallback += new System.Net.Security.RemoteCertificateValidationCallback(customXertificateValidation);
            this.siavWsLogin = new LoginServiceContractClient();
        }
        public Esito Login(string userName, string password)
        {
            Esito oEsito = new Esito();
            // create the connection info
            ConnectionInfo oConnectionInfo = new ConnectionInfo();

            // optionally, set the date format and language (default "dd/mm/yyyy" and Italian)
            oConnectionInfo.DateFormat = "dd/mm/yyyy";
            oConnectionInfo.Language = Siav.APFlibrary.SiavWsLogin.Language.Italian;

            // set the archiflow domain (the list of available domains may be obtained by calling the GetDomains method)
            oConnectionInfo.WorkflowDomain = "siav";
            Siav.APFlibrary.SiavWsLogin.ResultInfo oResult = Siav.APFlibrary.SiavWsLogin.ResultInfo.OK;
            try
            {
                oResult = siavWsLogin.Login(userName, password, oConnectionInfo,out this.oSessionInfo);
                if (oResult == Siav.APFlibrary.SiavWsLogin.ResultInfo.OK)
                {
                    oEsito.Codice = "1";
                }
                else
                {
                    oEsito.Codice = "0";
                    oEsito.Descrizione = "Login fallito: Errore durante la fase di accreditamento";
                    throw new ArgumentException(oEsito.Descrizione);
                }
            }
            catch (FaultException<ArchiflowServiceExceptionDetail> ex)
            {
                oEsito.Codice = "0";
                oEsito.Descrizione = "Motivo: " + ex.Detail.Message;
                throw new ArgumentException(oEsito.Descrizione);
            }
            return oEsito;
        }

        public Esito Logout()
        {
            Esito oEsito = new Esito();
            Siav.APFlibrary.SiavWsLogin.ResultInfo oResult = Siav.APFlibrary.SiavWsLogin.ResultInfo.OK;
            try
            {
                oResult = siavWsLogin.Logout(oSessionInfo);
                if (oResult == ResultInfo.OK)
                {
                    Console.WriteLine("Logout OK");
                }
                else
                {
                    oEsito.Codice = "0";
                    oEsito.Descrizione = "Logout fallito: Errore durante la fase di chiusura della sessione";
                    throw new ArgumentException(oEsito.Descrizione);
                }
            }
            catch (FaultException<ArchiflowServiceExceptionDetail> ex)
            {
                oEsito.Codice = "0";
                oEsito.Descrizione = "Motivo: " + ex.Detail.Message;
                throw new ArgumentException(oEsito.Descrizione);
            }
            return oEsito;
        }
    }
    
}
