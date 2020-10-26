using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using System.Xml;
using System.IO;
using System.Xml.Serialization;
using Newtonsoft.Json;
using ConversionServices.Util;
using ConversionServices.Manager;
using OCF_Ws.Manager;
using ConversionServices.Model;

namespace ConversionServices.Action
{
	public class WsAction : IDisposable
	{
		bool mDisposed = false; public int lErr = 0;
		string LogId;
		LOLIB Logger;
		Rijndael oRijndael;
		string sWorkingFolder;
		ResourceFileManager resourceFileManager;
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
		
		public WsAction(string sUser)
		{
			ServicePointManager.ServerCertificateValidationCallback += new System.Net.Security.RemoteCertificateValidationCallback(customXertificateValidation);
			resourceFileManager = ResourceFileManager.Instance;
			resourceFileManager.SetResources();
			Logger = new LOLIB();
			oRijndael = new Rijndael();
			if (LogId == null || LogId == "") LogId = LOLIB.CodeGen(sUser);
			string sPathWork = resourceFileManager.getConfigData("WorkFolder");
			sWorkingFolder = sPathWork + @"\" + LogId;
			System.IO.Directory.CreateDirectory(sWorkingFolder);
			Logger.WriteOnLog(LogId, "Creazione della directory di lavoro: " + sWorkingFolder, 3);
		}
		private static bool customXertificateValidation(object sender, X509Certificate cert, X509Chain chain, SslPolicyErrors error)
		{
			var certificate = (X509Certificate2)cert;
			return true;
		}
		public Outcome base64ToPdfA(MainDocument mainDoc, out MainDocument mainDocOut)
		{
			string strMessage = "";
			mainDocOut = new MainDocument();
			Outcome esito = new Outcome();
			esito.iCode = 1;
			esito.sTransactionId = LogId;
			try
			{
				if (mainDoc.Validate(out strMessage))
				{
					Logger.WriteOnLog(LogId, "Oggetto da convertire: " + mainDoc.ToXml(), 3);
					Convert2PdfManager oConvert2PdfManager = new Convert2PdfManager();
					oConvert2PdfManager.ConvertMainDoc(mainDoc, Logger, sWorkingFolder, LogId,out mainDocOut);
				}
				else
				{
					Logger.WriteOnLog(LogId, "Oggetto NON Convertito: " + mainDoc.ToXml(), 3);
					esito.iCode = 0;
				}
			}
			catch (Exception e)
			{
				esito.iCode = 0;
				strMessage = e.Message;
				Logger.WriteOnLog(LogId, e.Source + " -> " + e.StackTrace + " -> " + e.Message, 3);
			}
			finally
			{
				Logger.WriteOnLog(LogId, "Fine elaborazione", 3);
				esito.sDescription = strMessage;
				if (esito.iCode == 1)
				{
					Logger.WriteOnLog(LogId, esito.iCode + " - " + strMessage, 3);
					Logger.RenameFileLog(LogId, "OK_" + LogId);
				}
				else
				{
					Logger.WriteOnLog(LogId, esito.iCode + " - " + strMessage, 3);
					Logger.RenameFileLog(LogId, "KO_" + LogId);
				}
			}
			return esito;
		}
		
	}
}