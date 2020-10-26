using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using OCF_Ws.WsLogin;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using OCF_Ws.Model;

namespace OCF_Ws.Manager
{
	public class WcfSiavLoginManager
	{
		public OCF_Ws.WsLogin.LoginServiceContractClient siavWsLogin;
		public OCF_Ws.WsLogin.SessionInfo oSessionInfo;
		public WcfSiavLoginManager()
		{
			try
			{
				this.siavWsLogin = new OCF_Ws.WsLogin.LoginServiceContractClient();
			}
			catch (Exception ex)
			{
				throw new Exception(String.Format("{0} >> {1}: {2}", "ERRORE: WcfSiavLoginManager", ex.Source, ex.Message), ex);
			}
		}
		public Outcome Login(string userName, string password)
		{
			Outcome oEsito = new Outcome();
			// create the connection info
			ConnectionInfo oConnectionInfo = new ConnectionInfo();

			// optionally, set the date format and language (default "dd/mm/yyyy" and Italian)
			oConnectionInfo.DateFormat = "dd/mm/yyyy";
			oConnectionInfo.Language = OCF_Ws.WsLogin.Language.Italian;

			// set the archiflow domain (the list of available domains may be obtained by calling the GetDomains method)
			oConnectionInfo.WorkflowDomain = "siav";
			OCF_Ws.WsLogin.ResultInfo oResult = OCF_Ws.WsLogin.ResultInfo.OK;
			try
			{
				oResult = siavWsLogin.Login(out this.oSessionInfo, userName, password, oConnectionInfo);
				if (oResult == OCF_Ws.WsLogin.ResultInfo.OK)
				{
					oEsito.iCode = 1;
				}
				else
				{
					oEsito.iCode = 0;
					oEsito.sDescription = "Login fallito: Errore durante la fase di accreditamento";
					throw new ArgumentException(oEsito.sDescription);
				}
			}
			catch (FaultException<ArchiflowServiceExceptionDetail> ex)
			{
				oEsito.iCode = 0;
				oEsito.sDescription= "Motivo: " + ex.Detail.Message;
				throw new ArgumentException(oEsito.sDescription);
			}
			return oEsito;
		}

		public Outcome Logout()
		{
			Outcome oEsito = new Outcome();
			WsLogin.ResultInfo oResult = WsLogin.ResultInfo.OK;
			try
			{
				oResult = siavWsLogin.Logout(oSessionInfo);
				if (oResult == ResultInfo.OK)
				{
					Console.WriteLine("Logout OK");
				}
				else
				{
					oEsito.iCode = 0;
					oEsito.sDescription= "Logout fallito: Errore durante la fase di chiusura della sessione";
					throw new ArgumentException(oEsito.sDescription);
				}
			}
			catch (FaultException<ArchiflowServiceExceptionDetail> ex)
			{
				oEsito.iCode = 0;
				oEsito.sDescription= "Motivo: " + ex.Detail.Message;
				throw new ArgumentException(oEsito.sDescription);
			}
			return oEsito;
		}
	}

}
