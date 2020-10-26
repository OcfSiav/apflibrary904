using OCF_Ws.Manager;
using OCF_Ws.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace OCF_Ws.Util
{
	class UtilCardVisibility
	{

		public static List<SENDOBJECTSENDENTITIESSENDENTITY> getUsersInArchiveTypeDoc(string visibility, string LogId)
		{
			List<SENDOBJECTSENDENTITIESSENDENTITY> oUsersInArchiveTypeDoc;
			List<string> oUsers = new List<string>();
			visibility = visibility.Substring(0, visibility.Length - 1);
			//List<SENDOBJECTSENDENTITIESSENDENTITY> oUsersInArchiveTypeDoc;
			using (XmlReader xmlReader = XmlReader.Create(new StringReader(visibility)))
			{
				XmlSerializer serializer = new XmlSerializer(typeof(SENDOBJECT));
				SENDOBJECT deserializedVisEntity = serializer.Deserialize(xmlReader) as SENDOBJECT;

				oUsersInArchiveTypeDoc = (from item in deserializedVisEntity.Items
										  where item.GetType().ToString() == "OCF_Ws.Model.SENDOBJECTSENDENTITIES"
										  let cardsdoctypes = item as SENDOBJECTSENDENTITIES
										  from docType in cardsdoctypes.SENDENTITY
										  select docType).ToList();


				//Logger.WriteOnLog(LogId, "oUsersInArchiveTypeDoc: " + Logger.ToJson(oUsersInArchiveTypeDoc), 3);
				//Logger.WriteOnLog(LogId, "deserializedVisEntity: " + LOLIB.SerializeToString((Object)deserializedVisEntity), 3);
			}
			return oUsersInArchiveTypeDoc;
		}
		public static List<string> getUsersVisibility(List<SENDOBJECTSENDENTITIESSENDENTITY> oUsersInArchiveTypeDoc, string LogId)
		{
			LOLIB Logger;
			Logger = new LOLIB();
			List<string> oUsers = new List<string>();
			var sData = oUsersInArchiveTypeDoc.Where(b => b.STATUS == "1" && b.CC == "0" && b.TYPE == "0");
			foreach (var oUserVis in sData)
			{
				//Logger.WriteOnLog(LogId, "Uffici: " + oUserVis.DESCRIPTION, 3);
				oUsers.Add(oUserVis.DESCRIPTION);
			}
			return oUsers;
		}
		public static List<string> getUsersVisibilityCC(List<SENDOBJECTSENDENTITIESSENDENTITY> oUsersInArchiveTypeDoc, string LogId)
		{
			LOLIB Logger;
			Logger = new LOLIB();
			List<string> oUsers = new List<string>();

			var sData = oUsersInArchiveTypeDoc.Where(b => b.STATUS == "1" && b.CC == "1" && b.TYPE == "0");
			foreach (var oUserVis in sData)
			{
				//Logger.WriteOnLog(LogId, "UfficiCC: " + oUserVis.DESCRIPTION, 3);
				oUsers.Add(oUserVis.DESCRIPTION);
			}
			return oUsers;
		}
		public static List<string> getUfficiCodiceUO(List<SENDOBJECTSENDENTITIESSENDENTITY> oUsersInArchiveTypeDoc, string LogId)
		{
			LOLIB Logger;
			Logger = new LOLIB();
			List<string> oUsers = new List<string>();

			var sData = oUsersInArchiveTypeDoc.Where(b => b.STATUS == "1" && b.CC == "0" && b.TYPE == "1");
			foreach (var oUserVis in sData)
			{
				//Logger.WriteOnLog(LogId, "Utenti: " + oUserVis.DESCRIPTION, 3);
				oUsers.Add(oUserVis.DESCRIPTION);
			}
			return oUsers;
		}
		public static List<string> getUfficiCodiceUOCC(List<SENDOBJECTSENDENTITIESSENDENTITY> oUsersInArchiveTypeDoc, string LogId)
		{
			LOLIB Logger;
			Logger = new LOLIB();
			List<string> oUsers = new List<string>();
			var sData = oUsersInArchiveTypeDoc.Where(b => b.STATUS == "1" && b.CC == "1" && b.TYPE == "1");
			foreach (var oUserVis in sData)
			{
				//Logger.WriteOnLog(LogId, "UtentiCC: " + oUserVis.DESCRIPTION, 3);
				oUsers.Add(oUserVis.DESCRIPTION);
			}
			return oUsers;
		}
		public static List<string> getGruppiDescrizione(List<SENDOBJECTSENDENTITIESSENDENTITY> oUsersInArchiveTypeDoc, string LogId)
		{
			LOLIB Logger;
			Logger = new LOLIB();
			List<string> oUsers = new List<string>();

			var sData = oUsersInArchiveTypeDoc.Where(b => b.STATUS == "1" && b.CC == "0" && b.TYPE == "2");
			foreach (var oUserVis in sData)
			{
				//Logger.WriteOnLog(LogId, "Gruppi: " + oUserVis.DESCRIPTION, 3);
				oUsers.Add(oUserVis.DESCRIPTION);
			}
			return oUsers;
		}
		public static List<string> getGruppiDescrizioneCC(List<SENDOBJECTSENDENTITIESSENDENTITY> oUsersInArchiveTypeDoc, string LogId)
		{
			LOLIB Logger;
			Logger = new LOLIB();
			List<string> oUsers = new List<string>();

			var sData = oUsersInArchiveTypeDoc.Where(b => b.STATUS == "1" && b.CC == "1" && b.TYPE == "2");
			foreach (var oUserVis in sData)
			{
				//Logger.WriteOnLog(LogId, "GruppiCC: " + oUserVis.DESCRIPTION, 3);
				oUsers.Add(oUserVis.DESCRIPTION);
			}
			return oUsers;
		}
	}
}


