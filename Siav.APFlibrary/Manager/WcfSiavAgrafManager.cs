using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using Siav.APFlibrary.Model;
using Siav.APFlibrary.SiavWsAgraf;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Collections.Specialized;
using Siav.APFlibrary.SiavWsCard;
using NLog;
using Newtonsoft.Json;
using Siav.APFlibrary.Entity;

namespace Siav.APFlibrary.Manager
{
    public class WcfSiavAgrafManager
    {
        public List<AgrafIndexbook> listRubriche;
        //public NameValueCollection idRubriche;
		//public NameValueCollection tagsRubriche;
		//public List<Siav.APFlibrary.Entity.AgrafTag> lAgrafTag;
		public AgrafServiceClient siavWsAgraf;
		public Logger logger;
		public Siav.APFlibrary.SiavWsLogin.SessionInfo oSessionInfo;
        private static bool customXertificateValidation(object sender, X509Certificate cert, X509Chain chain, SslPolicyErrors error)
        {
            var certificate = (X509Certificate2)cert;
            return true;
        }
		public static string DotNetToOracle(string text)
		{
			Guid guid = new Guid(text);
			return BitConverter.ToString(guid.ToByteArray()).Replace("-", "");
		}
		public static byte[] ParseHex(string text)
		{
			// Not the most efficient code in the world, but
			// it works...
			byte[] ret = new byte[text.Length / 2];
			for (int i = 0; i < ret.Length; i++)
			{
				ret[i] = Convert.ToByte(text.Substring(i * 2, 2), 16);
			}
			return ret;
		}
		public static string OracleToDotNet(string text)
		{
			byte[] bytes = ParseHex(text);
			Guid guid = new Guid(bytes);
			return guid.ToString("N").ToUpperInvariant();
		}
		public string ToJson(object value)
		{
			var settings = new JsonSerializerSettings
			{
				ReferenceLoopHandling = ReferenceLoopHandling.Ignore
			};

			return JsonConvert.SerializeObject(value, Newtonsoft.Json.Formatting.Indented, settings);
		}
		public WcfSiavAgrafManager(Logger logger)
        {
            ServicePointManager.ServerCertificateValidationCallback += new System.Net.Security.RemoteCertificateValidationCallback(customXertificateValidation);
            this.siavWsAgraf = new AgrafServiceClient();
            //this.idRubriche = new NameValueCollection();
            this.listRubriche = new List<AgrafIndexbook>();
            //this.tagsRubriche = new NameValueCollection();
            //this.lAgrafTag = new List<Siav.APFlibrary.Entity.AgrafTag>();
			this.logger = logger;

		}
        public void LoadRubriche()
        {
            try
            {
                List<IndexBook> listInfo = siavWsAgraf.ReadAllIndexBook();
				this.logger.Debug("Anagrafica: "  + "->" +  this.ToJson(listInfo));

				if (listInfo.Count> 0)
                   foreach (IndexBook singleIndexBook in listInfo)
                   {
                        AgrafIndexbook oAgrafIndexBook = new AgrafIndexbook();
                        oAgrafIndexBook.Id = singleIndexBook.IndexBookId.ToString();
                        oAgrafIndexBook.Nome = singleIndexBook.Name.ToString();
                        //idRubriche.Add(singleIndexBook.Name.ToString(), singleIndexBook.IndexBookId.ToString());
                        AnagrafManager AnagModel = new AnagrafManager(logger);
                        this.logger.Debug("Ricerco rubrica: " + singleIndexBook.Name.ToString() + " - " + DotNetToOracle(singleIndexBook.IndexBookId.ToString()));
                        var result = AnagModel.GetTagIFromIndexbool(DotNetToOracle(singleIndexBook.IndexBookId.ToString()));
                        this.logger.Debug("Risultato ricerca -> " + this.ToJson(result));
                        if (result != null)
						{
							if (result.Count > 0)
							{
                                oAgrafIndexBook.lAgrafTag= result;
							}
                            listRubriche.Add(oAgrafIndexBook);
                        }
                        /*	 Eliminato perchè il servizio web non torna i dati dei tag
						foreach (IndexBookTag singleTag in singleIndexBook.IndexBookTags){
							if (this.logger  != null)
							{
								this.logger.Debug(singleIndexBook.Name.ToString() + "->" +  singleTag.IndexBookTagId.ToString());
							}
							tagsRubriche.Add(singleIndexBook.Name.ToString(),  singleTag.IndexBookTagId.ToString());
                       }   */
                        this.logger.Debug("Rubriche: " + "->" + this.ToJson(listRubriche));
                    }
                else
                   throw new ArgumentException("Non è stato possibile caricare alcuna rubrica");
                /*GenericEntitySearch oaSearcher = new GenericEntitySearch();
                oaSearcher.Status = Status.Active;
                oaSearcher.OrderBy = OrderByOptions.Name;
                oaSearcher.TaxId = "BBDLCU72T15L719L";
                oaSearcher.IndexBookId = Guid.Parse("f8d102e55187d6418b42c5036b4f2806");
                List<GenericEntity> oListEntityOut = siavWsAgraf.ReadGenericEntityByEntity(10, 1, oaSearcher,out nCount);*/
            }
            catch (FaultException<RegistryServiceExceptionDetail> fex)
            {
                throw new ArgumentException(fex.Detail.Message);
            }
            catch (Exception  ex)
            {
                throw new ArgumentException(ex.Message + " - " + ex.StackTrace + " - " + ex.Source);
            }
        }
		public List<GenericEntity> GetUsersUpdated(string sCf, Guid sRubricaId, string sEntityType)
		{
			int nCount = 0;
			try
			{
				GenericEntitySearch oaSearcher = new GenericEntitySearch();
				oaSearcher.Status = Status.Active;
				oaSearcher.OrderBy = OrderByOptions.Name;
				oaSearcher.TaxId = sCf;
				oaSearcher.IsLatestVersion = true;
				oaSearcher.IndexBookId = sRubricaId;
				//this.idRubriche[sRubricaName]);
				oaSearcher.EntityTypeName = sEntityType;
				List<GenericEntity> oListEntityOut = siavWsAgraf.ReadGenericEntityByEntity(0, 0, oaSearcher, out nCount);
				return oListEntityOut;
			}
			catch (Exception ex)
			{
				throw new ArgumentException(ex.Message);
			}
		}
		public List<GenericEntity> GetUsersForCas(string sCf, string sRubricaName, string sObjectToSearch)
        {
            int nCount = 0;
            try
            {
                GenericEntitySearch oaSearcher = new GenericEntitySearch();
                oaSearcher.Status = Status.Active;
                oaSearcher.OrderBy = OrderByOptions.Name;
                oaSearcher.TaxId = sCf;
                oaSearcher.IsLatestVersion = true;
                oaSearcher.IndexBookId = Guid.Parse(listRubriche.Find(x => x.Nome.Contains(sRubricaName)).Id); 
                //this.idRubriche[sRubricaName]);
                oaSearcher.EntityTypeName = sObjectToSearch;
                List<GenericEntity> oListEntityOut = siavWsAgraf.ReadGenericEntityByEntity(0, 0, oaSearcher, out nCount);
                return oListEntityOut;
            }
            catch (Exception ex)
            {
                throw new ArgumentException(ex.Message);
            }
        }
		public List<GenericEntity> GetCompany(string sPiva, string sRubricaName, string sObjectToSearch)
		{
			int nCount = 0;
			try
			{
				GenericEntitySearch oaSearcher = new GenericEntitySearch();
				oaSearcher.Status = Status.Active;
				oaSearcher.OrderBy = OrderByOptions.Name;
				oaSearcher.VatId = sPiva;
				oaSearcher.IsLatestVersion = true;
                oaSearcher.IndexBookId = Guid.Parse(listRubriche.Find(x => x.Nome.Contains(sRubricaName)).Id);
                //oaSearcher.IndexBookId = Guid.Parse(this.idRubriche[sRubricaName]);
				oaSearcher.EntityTypeName = sObjectToSearch;
				List<GenericEntity> oListEntityOut = siavWsAgraf.ReadGenericEntityByEntity(0, 0, oaSearcher, out nCount);
				return oListEntityOut;
			}
			catch (Exception ex)
			{
				throw new ArgumentException(ex.Message);
			}
		}
		public List<GenericEntity> GetUsers(string sCodOcf, string sRubricaName, string sObjectToSearch)
        {
            int nCount = 0;
            try
            {
                GenericEntitySearch oaSearcher = new GenericEntitySearch();
                oaSearcher.Status = Status.Active;
                oaSearcher.OrderBy = OrderByOptions.Name;
                //oaSearcher.TaxId = sCf;
                oaSearcher.GenericEntityExternalId = sCodOcf.ToUpper();     // GLADAMO 28/09/2020 aggiunto .ToUpper()
                oaSearcher.IsLatestVersion = true;
                //oaSearcher.IndexBookId = Guid.Parse(this.idRubriche[sRubricaName]);
                oaSearcher.IndexBookId = Guid.Parse(listRubriche.Find(x => x.Nome.Contains(sRubricaName)).Id);
                oaSearcher.EntityTypeName = sObjectToSearch;
                List<GenericEntity> oListEntityOut = siavWsAgraf.ReadGenericEntityByEntity(0, 0, oaSearcher, out nCount);
                return oListEntityOut;
            }
            catch (Exception ex)
            {
                throw new ArgumentException(ex.Message);
            }
        }
    }

}

