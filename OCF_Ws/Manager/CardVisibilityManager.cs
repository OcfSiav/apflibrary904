using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Xml;
using System.Xml.Serialization;
using OCF_Ws.Model;

namespace OCF_Ws.Manager
{
	public class CardVisibilityManager
	{
		string sVisibilityXml;
		List<SENDOBJECTSENDENTITIESSENDENTITY> oUsersInArchiveTypeDoc;
		public CardVisibilityManager(string sGuidCard, string strConnection)
		{
			Guid oCardId;
			if (sGuidCard.Length > 12)                 // set the guid of the card
				oCardId = new Guid("64556990-b196-425c-a0b9-" + int.Parse(sGuidCard.Substring(24, 12)).ToString("000000000000"));
			else
				oCardId = new Guid("64556990-b196-425c-a0b9-" + int.Parse(sGuidCard).ToString("000000000000"));
			SVAOLLib.Card oCard = new SVAOLLib.Card();
			oCard.GUIDconnect = strConnection;
			oCard.GuidCard = oCardId.ToString();
			oCard.LoadFromGuid();
			
			sVisibilityXml = oCard.GetVisibilityAsXML();
			sVisibilityXml = sVisibilityXml.Substring(0, sVisibilityXml.Length - 1);
			using (XmlReader xmlReader = XmlReader.Create(new StringReader(sVisibilityXml)))
			{
				XmlSerializer serializer = new XmlSerializer(typeof(SENDOBJECT));
				SENDOBJECT deserializedVisEntity = serializer.Deserialize(xmlReader) as SENDOBJECT;
				oUsersInArchiveTypeDoc = (from item in deserializedVisEntity.Items
										  where item.GetType().ToString() == "Siav.APFlibrary.Model.SENDOBJECTSENDENTITIES"
										  let cardsdoctypes = item as SENDOBJECTSENDENTITIES
										  from docType in cardsdoctypes.SENDENTITY
										  select docType).ToList();
			}
		}

		public SVAOLLib.Users getUsersFromSharePredefinite()
		{
			SVAOLLib.Users oUsers = new SVAOLLib.Users();
			var sData = oUsersInArchiveTypeDoc.Where(b => b.TYPE == "0" && (b.SENDINGTYPE == "1" || b.SENDINGTYPE == "2"));
			foreach (var oUserVis in sData)
			{
				SVAOLLib.User oUser = new SVAOLLib.User();
				oUser.Code = short.Parse(oUserVis.ID);
				oUsers.Add(oUser);
			}
			return oUsers;
		}

		public SVAOLLib.Users getUsersMailFromSharePredefinite()
		{
			SVAOLLib.Users oUsers = new SVAOLLib.Users();
			var sData = oUsersInArchiveTypeDoc.Where(b => b.TYPE == "0" && b.SENDINGTYPE == "2");
			foreach (var oUserVis in sData)
			{
				SVAOLLib.User oUser = new SVAOLLib.User();
				oUser.Code = short.Parse(oUserVis.ID);
				oUsers.Add(oUser);
			}
			return oUsers;
		}

		public SVAOLLib.Groups getGroupsFromSharePredefinite()
		{
			SVAOLLib.Groups oGroups = new SVAOLLib.Groups();
			var sData = oUsersInArchiveTypeDoc.Where(b => b.TYPE == "2" && (b.SENDINGTYPE == "1" || b.SENDINGTYPE == "2"));
			foreach (var oGroupVis in sData)
			{
				SVAOLLib.Group oGroup = new SVAOLLib.Group();
				oGroup.Code = short.Parse(oGroupVis.ID);
				oGroups.Add(oGroup);
			}
			return oGroups;
		}

		public SVAOLLib.Groups getGroupsMailFromSharePredefinite()
		{
			SVAOLLib.Groups oGroups = new SVAOLLib.Groups();
			var sData = oUsersInArchiveTypeDoc.Where(b => b.TYPE == "2" && b.SENDINGTYPE == "2");
			foreach (var oGroupVis in sData)
			{
				SVAOLLib.Group oGroup = new SVAOLLib.Group();
				oGroup.Code = short.Parse(oGroupVis.ID);
				oGroups.Add(oGroup);
			}
			return oGroups;
		}

		public SVAOLLib.Offices getOfficesFromSharePredefinite()
		{
			SVAOLLib.Offices oOffices = new SVAOLLib.Offices();
			var sData = oUsersInArchiveTypeDoc.Where(b => b.TYPE == "1" && (b.SENDINGTYPE == "1" || b.SENDINGTYPE == "2"));
			foreach (var oOfficeVis in sData)
			{
				SVAOLLib.Office oOffice = new SVAOLLib.Office();
				oOffice.Code = short.Parse(oOfficeVis.ID);
				oOffices.Add(oOffice);
			}
			return oOffices;
		}

		public SVAOLLib.Offices getOfficesMailFromSharePredefinite()
		{
			SVAOLLib.Offices oOffices = new SVAOLLib.Offices();
			var sData = oUsersInArchiveTypeDoc.Where(b => b.TYPE == "1" && b.SENDINGTYPE == "2");
			foreach (var oOfficeVis in sData)
			{
				SVAOLLib.Office oOffice = new SVAOLLib.Office();
				oOffice.Code = short.Parse(oOfficeVis.ID);
				oOffices.Add(oOffice);
			}
			return oOffices;
		}

		public SVAOLLib.Group getGroupFromVisibility(string sGroupName)
		{
			SVAOLLib.Group oGroup = new SVAOLLib.Group();
			var sData = oUsersInArchiveTypeDoc.Where(x => x.TYPE == "2" && x.DESCRIPTION.ToUpper() == sGroupName.ToUpper()).FirstOrDefault();
			//var sData = oUsersInArchiveTypeDoc.FirstOrDefault(x => x.DESCRIPTION.ToUpper() == sGroupName.ToUpper() && x.TYPE == "2");
			oGroup.Code = short.Parse(sData.ID);
			return oGroup;
		}

		public SVAOLLib.Offices getOfficesFromVisibility(string sOfficeName)
		{
			SVAOLLib.Offices oOffices = new SVAOLLib.Offices();

			var sData = oUsersInArchiveTypeDoc.Where(x => x.TYPE == "1" && x.DESCRIPTION.ToUpper() == sOfficeName.ToUpper());
			foreach (var oOfficeVis in sData)
			{
				SVAOLLib.Office oOffice = new SVAOLLib.Office();
				oOffice.Code = short.Parse(oOfficeVis.ID);
				oOffices.Add(oOffice);
			}
			return oOffices;
		}

		public SVAOLLib.User getUserFromVisibility(string sUserName)
		{

			var sData = oUsersInArchiveTypeDoc.Where(x => x.TYPE == "0" && x.DESCRIPTION.ToUpper() == sUserName.ToUpper()).FirstOrDefault();
			SVAOLLib.User oUser = new SVAOLLib.User();
			oUser.Code = short.Parse(sData.ID);
			return oUser;
		}
	}

}
