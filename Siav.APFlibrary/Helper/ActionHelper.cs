using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Xml;
using System.Xml.Serialization;
using Siav.APFlibrary.Model;

namespace Siav.APFlibrary.Helper
{
	public static class UtilAction
	{

		public static List<SENDOBJECTSENDENTITIESSENDENTITY> getEntityVisibilityFromCard(string sVisibilityXml)
		{
			//File.WriteAllText(@"c:\tmp\pippo.xml", SendObjxml);
			sVisibilityXml = sVisibilityXml.Substring(0, sVisibilityXml.Length - 1);
			using (XmlReader xmlReader = XmlReader.Create(new StringReader(sVisibilityXml)))
			{
				XmlSerializer serializer = new XmlSerializer(typeof(SENDOBJECT));
				SENDOBJECT deserializedVisEntity = serializer.Deserialize(xmlReader) as SENDOBJECT;
				var sData = (from item in deserializedVisEntity.Items
							 where item.GetType().ToString() == "Siav.APFlibrary.Model.SENDOBJECTSENDENTITIES"
							 let cardsdoctypes = item as SENDOBJECTSENDENTITIES
							 from docType in cardsdoctypes.SENDENTITY
							 select docType).ToList();
				return sData;
			}
		}

		public static SVAOLLib.Users getUsersFromSharePredefinite(List<SENDOBJECTSENDENTITIESSENDENTITY> oUsersInArchiveTypeDoc)
		{
			SVAOLLib.Users oUsers = new SVAOLLib.Users();
			var sData = oUsersInArchiveTypeDoc.Where(b => b.TYPE == "0" && b.SENDINGTYPE == "1");
			//(from item in oUsersInArchiveTypeDoc  where item.TYPE == "0" && item.SENDINGTYPE == "1" select oUsersInArchiveTypeDoc);
			foreach (var oUserVis in sData)
			{
				SVAOLLib.User oUser = new SVAOLLib.User();
				oUser.Code = short.Parse(oUserVis.ID);
				oUsers.Add(oUser);
			}
			return oUsers;
		}

		public static SVAOLLib.Users getUsersMailFromSharePredefinite(List<SENDOBJECTSENDENTITIESSENDENTITY> oUsersInArchiveTypeDoc)
		{
			SVAOLLib.Users oUsers = new SVAOLLib.Users();
			var sData = oUsersInArchiveTypeDoc.Where(b => b.TYPE == "0" && b.SENDINGTYPE == "2");
			/*
            var sData = (from item in oUsersInArchiveTypeDoc
                             where item.TYPE == "0" && item.SENDINGTYPE == "2"
                             select oUsersInArchiveTypeDoc).SingleOrDefault();*/
			foreach (var oUserVis in sData)
			{
				SVAOLLib.User oUser = new SVAOLLib.User();
				oUser.Code = short.Parse(oUserVis.ID);
				oUsers.Add(oUser);
			}
			return oUsers;
		}


		public static SVAOLLib.Groups getGroupsFromSharePredefinite(List<SENDOBJECTSENDENTITIESSENDENTITY> oUsersInArchiveTypeDoc)
		{
			SVAOLLib.Groups oGroups = new SVAOLLib.Groups();
			var sData = oUsersInArchiveTypeDoc.Where(b => b.TYPE == "2" && b.SENDINGTYPE == "1");

			/* var sData = (from item in oUsersInArchiveTypeDoc
							  where item.TYPE == "2" && item.SENDINGTYPE == "1"
							  select oUsersInArchiveTypeDoc).SingleOrDefault();*/
			foreach (var oGroupVis in sData)
			{
				SVAOLLib.Group oGroup = new SVAOLLib.Group();
				oGroup.Code = short.Parse(oGroupVis.ID);
				oGroups.Add(oGroup);
			}
			return oGroups;
		}

		public static SVAOLLib.Groups getGroupsMailFromSharePredefinite(List<SENDOBJECTSENDENTITIESSENDENTITY> oUsersInArchiveTypeDoc)
		{
			SVAOLLib.Groups oGroups = new SVAOLLib.Groups();
			var sData = oUsersInArchiveTypeDoc.Where(b => b.TYPE == "2" && b.SENDINGTYPE == "2");
			/*
            var sData = (from item in oUsersInArchiveTypeDoc
                             where item.TYPE == "2" && item.SENDINGTYPE == "2"
                             select oUsersInArchiveTypeDoc).SingleOrDefault();*/
			foreach (var oGroupVis in sData)
			{
				SVAOLLib.Group oGroup = new SVAOLLib.Group();
				oGroup.Code = short.Parse(oGroupVis.ID);
				oGroups.Add(oGroup);
			}
			return oGroups;
		}

		public static SVAOLLib.Offices getOfficesFromSharePredefinite(List<SENDOBJECTSENDENTITIESSENDENTITY> oUsersInArchiveTypeDoc)
		{
			SVAOLLib.Offices oOffices = new SVAOLLib.Offices();
			var sData = oUsersInArchiveTypeDoc.Where(b => b.TYPE == "1" && b.SENDINGTYPE == "1");
			/*
            var sData = (from item in oUsersInArchiveTypeDoc
                             where item.TYPE == "1" && item.SENDINGTYPE == "1"
                             select oUsersInArchiveTypeDoc).SingleOrDefault();*/
			foreach (var oOfficeVis in sData)
			{
				SVAOLLib.Office oOffice = new SVAOLLib.Office();
				oOffice.Code = short.Parse(oOfficeVis.ID);
				oOffices.Add(oOffice);
			}
			return oOffices;
		}

		public static SVAOLLib.Offices getOfficesMailFromSharePredefinite(List<SENDOBJECTSENDENTITIESSENDENTITY> oUsersInArchiveTypeDoc)
		{
			SVAOLLib.Offices oOffices = new SVAOLLib.Offices();
			var sData = oUsersInArchiveTypeDoc.Where(b => b.TYPE == "1" && b.SENDINGTYPE == "2");
			/*
            var sData = (from item in oUsersInArchiveTypeDoc
                             where item.TYPE == "1" && item.SENDINGTYPE == "2"
                             select oUsersInArchiveTypeDoc).SingleOrDefault();*/
			foreach (var oOfficeVis in sData)
			{
				SVAOLLib.Office oOffice = new SVAOLLib.Office();
				oOffice.Code = short.Parse(oOfficeVis.ID);
				oOffices.Add(oOffice);
			}
			return oOffices;
		}


		public static SVAOLLib.Group getGroupFromVisibility(List<SENDOBJECTSENDENTITIESSENDENTITY> oUsersInArchiveTypeDoc, string sGroupName)
		{
			SVAOLLib.Group oGroup = new SVAOLLib.Group();
			var sData = oUsersInArchiveTypeDoc.Where(x => x.TYPE == "2" && x.DESCRIPTION.ToUpper() == sGroupName.ToUpper()).FirstOrDefault();
			//var sData = oUsersInArchiveTypeDoc.FirstOrDefault(x => x.DESCRIPTION.ToUpper() == sGroupName.ToUpper() && x.TYPE == "2");
			oGroup.Code = short.Parse(sData.ID);
			return oGroup;
		}

		public static SVAOLLib.Offices getOfficesFromVisibility(List<SENDOBJECTSENDENTITIESSENDENTITY> oUsersInArchiveTypeDoc, string sOfficeName)
		{
			SVAOLLib.Offices oOffices = new SVAOLLib.Offices();

			var sData = oUsersInArchiveTypeDoc.Where(x => x.TYPE == "1" && x.DESCRIPTION.ToUpper() == sOfficeName.ToUpper());


			/*  (from item in oUsersInArchiveTypeDoc
						   where item.TYPE == "1" && item.DESCRIPTION.ToUpper() == sOfficeName.ToUpper()
						   select oUsersInArchiveTypeDoc).SingleOrDefault();*/

			/*  var exclusionKeys = oUsersInArchiveTypeDoc.Select(x => x.TYPE == "1");
			  var sData = oUsersInArchiveTypeDoc.Where(x => !exclusionKeys.Contains(x.DESCRIPTION.ToUpper() == sOfficeName.ToUpper()));*/

			/* var sData = from h in oUsersInArchiveTypeDoc
						 where (h.TYPE == "1") &&
									(h.DESCRIPTION.ToUpper() == sOfficeName.ToUpper())
						 select oUsersInArchiveTypeDoc.SingleOrDefault();*/




			foreach (var oOfficeVis in sData)
			{
				SVAOLLib.Office oOffice = new SVAOLLib.Office();
				oOffice.Code = short.Parse(oOfficeVis.ID);
				oOffices.Add(oOffice);
			}
			return oOffices;
		}

		public static SVAOLLib.User getUserFromVisibility(List<SENDOBJECTSENDENTITIESSENDENTITY> oUsersInArchiveTypeDoc, string sUserName)
		{

			var sData = oUsersInArchiveTypeDoc.Where(x => x.TYPE == "0" && x.DESCRIPTION.ToUpper() == sUserName.ToUpper()).FirstOrDefault();
			//var sData = oUsersInArchiveTypeDoc.FirstOrDefault(x => x.DESCRIPTION.ToUpper() == sUserName.ToUpper() && x.TYPE == "0");
			SVAOLLib.User oUser = new SVAOLLib.User();
			oUser.Code = short.Parse(sData.ID);
			return oUser;
		}


		public static List<KeyValuePair<string, String>> getSystemVisibility(string sKeyValueTable)
		{
			List<KeyValuePair<string, String>> systemVisibility;
			systemVisibility = new List<KeyValuePair<string, String>>();

			var oArrayData = sKeyValueTable.Split('|');

			for (int i = 0; i < oArrayData.Length; i++)
			{
				if (i % 2 == 0)
				{
					string sCleanArrayData = oArrayData[i].Replace("<", "").Replace(">", "").ToLower().Trim();
					switch (sCleanArrayData)
					{
						case "avop":
							systemVisibility.Add(new KeyValuePair<string, String>("avop", oArrayData[i + 1].ToString()));
							break;
						case "avup":
							systemVisibility.Add(new KeyValuePair<string, String>("avup", oArrayData[i + 1].ToString()));
							break;
						case "avgp":
							systemVisibility.Add(new KeyValuePair<string, String>("avgp", oArrayData[i + 1].ToString()));
							break;
						case "avo":
							systemVisibility.Add(new KeyValuePair<string, String>("avo", oArrayData[i + 1].ToString()));
							break;
						case "avu":
							systemVisibility.Add(new KeyValuePair<string, String>("avu", oArrayData[i + 1].ToString()));
							break;
						case "avg":
							systemVisibility.Add(new KeyValuePair<string, String>("avg", oArrayData[i + 1].ToString()));
							break;
					}
				}
			}
			return systemVisibility;
		}
	}
    
}