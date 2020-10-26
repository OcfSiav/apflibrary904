using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Siav.APFlibrary.Entity;
using Siav.APFlibrary.Model;
using AspNet.Identity.Oracle;
using log4net.Repository.Hierarchy;
using NLog;

namespace Siav.APFlibrary.Manager
{
	public class AnagrafManager
	{
		NLog.Logger nLog;

		public OracleDatabase Database { get; private set; }
		private AnagModel recordAnagrafico;
		public AnagrafManager(NLog.Logger log)
		{
			nLog = log;
			recordAnagrafico = new AnagModel(new OracleDatabase(), nLog);
		}
		public AnagrafManager(OracleDatabase database, NLog.Logger log)
		{
			nLog = log;
			Database = database;
			recordAnagrafico = new AnagModel(database, nLog);
		}
		public List<IdentityAgraf> getIdAnagrafEntity(string sId,string sRubrica)
		{
			// If you have some performance issues, then you can implement the IQueryable.
			var x = recordAnagrafico.GetIdAgraf(sId, sRubrica);
			return x != null ? x : null;
		}
		public List<AgrafIndexbook> getGetIdIndexBook(string sRubrica)
		{
			// If you have some performance issues, then you can implement the IQueryable.
			var x = recordAnagrafico.GetIdIndexBook(sRubrica);
			return x != null ? x : null;
		}
		public List<AgrafTag> GetTagIFromIndexbool(string sId)
		{
			// If you have some performance issues, then you can implement the IQueryable.
			var x = recordAnagrafico.GetTagIFromIndexbool(sId);
			return x != null ? x : null;
		}
		public bool SetEnableDisableAgrafEntity(string sId, string sRubrica, bool bValue)
		{
			int iResult = 0;
			bool bResult = false;
			var x = this.getIdAnagrafEntity(sId, sRubrica);
			if (x != null)
			{
				iResult = recordAnagrafico.SetEnableDisableAgrafEntity(x[0].id, bValue);
				if (iResult>0)
				{
					bResult = true;
				}
				else
				{
					bResult = false;
				}
			}
			else
			{
				bResult = false;
			}

			return bResult;
		}
		public List<IdentityAgraf> GetLastVersion(string sId)
		{
			// If you have some performance issues, then you can implement the IQueryable.
			var x = recordAnagrafico.GetLastVersion(sId);
			return x != null ? x : null;
		}
		
	}
}