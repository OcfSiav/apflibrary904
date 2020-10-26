using Siav.APFlibrary.Helper;
using Siav.APFlibrary.Manager;
using Siav.APFlibrary.Model;
using Siav.APFlibrary.SiavWsCard;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Specialized;
using Siav.APFlibrary.SiavWsAgraf;

using NLog;
using NLog.Config;
using NLog.Targets;
using System.Xml.Linq;
using System.IO;
using System.Text.RegularExpressions;
using System.Net.Mail;
using Newtonsoft.Json;
using Oracle.ManagedDataAccess.Client;
using System.Xml;
using System.Xml.Xsl;
using System.Threading;
using FatturaElettronica.Extensions;

namespace Siav.APFlibrary.Action
{
	public class GenComMassive
	{
		private static Logger logger = LogManager.GetCurrentClassLogger();
		string IdSessioneTransazione;
		ResourceFileManager resourceFileManager;
		string sWorkingFolder;
		string sIdIndexTagRubricaFornitori;
		string sIdIndexTagRubricaApf;
		string sIdIndexTagRubricaGen;
		Rijndael oRijndael;
		// Load the XML into memory

		// Metodo di verifica, di seguito i controlli effettuati in questo STEP:
		// Estrapolazione del file Excel
		// Verifica contenuto del file Excel se presente almeno un record
		// Verifica di ricerca all'interno del documentale per verificare l'esistenza di record.
		// in caso di errore
		// Creazione di 1 documento DOCX per eseguire un test che verifica che tutti i tag sono sostituiti
		public GenComMassive()
		{
			oRijndael = new Rijndael();
			LoggingConfiguration config = new LoggingConfiguration();
			resourceFileManager = ResourceFileManager.Instance;
			resourceFileManager.SetResources();
			logger.Debug("Caricamento file di risorse terminato");
			string sPathWork = string.Empty;
			string sPathLog = string.Empty;
			if (resourceFileManager.getConfigData("isTest") == "SI")
			{
				sPathWork = resourceFileManager.getConfigData("ExcelWorkFolder");
				sPathLog = @resourceFileManager.getConfigData("PathLog");
			}
			else
			{
				sPathWork = resourceFileManager.getConfigData("ExcelWorkFolderProd");
				sPathLog = @resourceFileManager.getConfigData("PathLogProd");
			}

			FileTarget fileTarget = new FileTarget();
			config.AddTarget("file", fileTarget);
			this.IdSessioneTransazione = Guid.NewGuid().ToString();
			// Step 3. Set target properties
			fileTarget.FileName = sPathLog + @"/" + this.IdSessioneTransazione + "_${date:format=yyyyMMddHH}.log";
			fileTarget.Layout = "${date:format=yyyy MM dd HHmmss} " + this.IdSessioneTransazione + " ${uppercase:${level}} ${message}";
			fileTarget.LineEnding = LineEndingMode.Default;
			fileTarget.AutoFlush = true;
			fileTarget.KeepFileOpen = false;
			fileTarget.ConcurrentWrites = true;
			fileTarget.ArchiveFileName = "${date:format=yyyyMMddHH}_{#####}.log";
			fileTarget.ArchiveEvery = FileArchivePeriod.Day;
			fileTarget.ArchiveNumbering = ArchiveNumberingMode.Sequence;
			fileTarget.MaxArchiveFiles = 720;

			// Step 4. Define rules
			LoggingRule rule2 = new LoggingRule("*", LogLevel.Debug, fileTarget);
			config.LoggingRules.Add(rule2);
			// Step 5. Activate the configuration
			LogManager.Configuration = config;
			logger.Debug("Configurazione Log Terminata");
			sWorkingFolder = sPathWork + @"\" + Guid.NewGuid().ToString();
			if (resourceFileManager.getConfigData("isTest") == "SI")
			{
				sIdIndexTagRubricaApf = resourceFileManager.getConfigData("idIndexTagRubricaApf").ToString();
				sIdIndexTagRubricaGen = resourceFileManager.getConfigData("idIndexTagRubricaGen").ToString();
				sIdIndexTagRubricaFornitori = resourceFileManager.getConfigData("idIndexTagRubricaFor").ToString();
			}
			else
			{
				sIdIndexTagRubricaApf = resourceFileManager.getConfigData("idIndexTagRubricaApfProd").ToString();
				sIdIndexTagRubricaGen = resourceFileManager.getConfigData("idIndexTagRubricaGenProd").ToString();
				sIdIndexTagRubricaFornitori = resourceFileManager.getConfigData("idIndexTagRubricaForProd").ToString();
			}
			System.IO.Directory.CreateDirectory(sWorkingFolder);
			logger.Debug("Creazione della directory di lavoro: " + sWorkingFolder);

		}

		public object[] VerifyStep1Cancellazione(string GUIdcard, out string sOutput)
		{
			List<String> sResult = new List<string>();
			sOutput = "";
			bool isInError = false;
			WcfSiavLoginManager siavLogin = null;
			WcfSiavAgrafManager wcfSiavAgrafManager = null;
			WcfSiavCardManager siavCardManager = null;
			CardBundle oCardBundle = null;
			Boolean bAllSezTerr = false;
			CardAction cardAction = new CardAction();
			String sDocIdTemplate1 = "";
			String sDocIdTemplate2 = "";
			String sIdComMaxivaForObj = "";
			String sObjForObj = "";
			String sPathTemplate1 = "";
			String sPathTemplate2 = "";
			string sLog = string.Empty;
			string sSezTer = "";
			string sErrorObj = "";
			try
			{
				siavLogin = new WcfSiavLoginManager();
				wcfSiavAgrafManager = new WcfSiavAgrafManager(logger);
				FluxHelper fluxHelper = new FluxHelper();
				string oUserName = resourceFileManager.getConfigData("UserFlux");
				string oUserPwd = oRijndael.Decrypt(resourceFileManager.getConfigData("PasswordFlux"));
				string sModelDocX = "";

				MainDoc oMainDoc;
				NameValueCollection IscMassiveCardIndexes;
				// Esegue il login
				siavLogin.Login(oUserName, oUserPwd);
				logger.Debug("Sessione login Archiflow: " + siavLogin.oSessionInfo.SessionId);

				siavCardManager = new WcfSiavCardManager(logger);
				// Recupera la scheda ove è stato avviato il processo
				siavCardManager.GetCard(GUIdcard, siavLogin, out oCardBundle);

				// Recupera il documento principale della scheda 
				siavCardManager.GetMainDoc(oCardBundle, out oMainDoc);
				// Recupero gli indici della scheda 
				siavCardManager.GetIndexes(oCardBundle, out IscMassiveCardIndexes);
				logger.Debug("Recuperati i dati dalla scheda che ha avviato il processo : " + GUIdcard);

				// Materializza il file sul filesystem
				string sAnagraphicXlsSource = this.sWorkingFolder + @"\" + oMainDoc.Filename + "_AnagSource." + oMainDoc.Extension;
				logger.Debug("Scrivo il documento principale: " + sAnagraphicXlsSource);
				fluxHelper.FileMaterialize(sAnagraphicXlsSource, oMainDoc.oByte);

				// Estraggo i dati dal file Excel                
				ExcelDocumentReader excelDocumentReader = new ExcelDocumentReader(sAnagraphicXlsSource, sWorkingFolder);

				//ExcelDocumentReader excelDocumentReader = new ExcelDocumentReader(sAnagraphicXlsSource, sWorkingFolder);
				logger.Debug("Lettura del file excel: " + sAnagraphicXlsSource);

				// Verifica ricerca presenza record
				List<string> lCodiceFiscale = new List<string>();
				logger.Debug("Sono stati letti " + excelDocumentReader.getData.Count() + " di record");

				if (excelDocumentReader.getData.Count() > 0)
				{
					// Lista di codici fiscali da verificare
					//logger.Debug("Estrapolo i primi 10 record da verificare.");
					//string sColumnIdSubject = resourceFileManager.getConfigData("CancAnagCodOCFNameField");
					//fluxHelper.GetSampleRecords(excelDocumentReader, sColumnIdSubject,out lCodiceFiscale);
					//if (lCodiceFiscale.Count > 0)
					//{
					List<Guid> lGUID = new List<Guid>();
					//foreach(var singleCf in lCodiceFiscale){
					//    List<SchedaSearchParameter> lSchedaSearchParameter = new List<SchedaSearchParameter>();

					//    List<string> lMetadati;
					//    SchedaSearchParameter schedaSearchParameter = new SchedaSearchParameter();
					//    schedaSearchParameter.Archivio = resourceFileManager.getConfigData("IscArchiveSearchUnique");
					//    schedaSearchParameter.TipologiaDocumentale = resourceFileManager.getConfigData("CancTypeDocSearchUnique");
					//    schedaSearchParameter.Oggetto = resourceFileManager.getConfigData("CancObjSearchUnique");
					//    string[] arr = new string[20]; 
					//    arr[int.Parse(resourceFileManager.getConfigData("CancIndSchedSearchUnique"))] = singleCf;
					//    logger.Debug("Inizio ricerca record");
					//    logger.Debug("  Cerco per Archivio: " + resourceFileManager.getConfigData("IscArchiveSearchUnique"));
					//    logger.Debug("  Cerco per TipologiaDocumentale: " + resourceFileManager.getConfigData("CancTypeDocSearchUnique"));
					//    logger.Debug("  Cerco per Oggetto: " + resourceFileManager.getConfigData("CancObjSearchUnique"));
					//    // CANCELLAZIONE A DOMANDA - COMUNICAZIONE DELIBERA
					//    logger.Debug("  Cerco per Codice OCF: " + singleCf);
					//    logger.Debug("Fine ricerca record");

					//    lMetadati = new List<string>(arr);
					//    schedaSearchParameter.MetaDati = lMetadati;
					//    lSchedaSearchParameter.Add(schedaSearchParameter);
					//    siavCardManager.getSearch(lSchedaSearchParameter, siavLogin, out lGUID);
					//    if (lGUID.Count > 0)
					//    {
					//        sLog = "Sono state trovate schede già inserite a sistema";
					//        // notificare problema di occorrenza trovata
					//        throw new ArgumentException(sLog);
					//    }
					//}
					logger.Debug("Inizio Ricerca documento modelli ");

					List<string> lMetadatiModelli;
					// ricerco tra le tipologie Modelli, il documento necessario alla creazione della comunicazione
					SchedaSearchParameter schedaModSearchParameter = new SchedaSearchParameter();
					schedaModSearchParameter.Archivio = resourceFileManager.getConfigData("ArchiveTypeDocModelli");
					schedaModSearchParameter.TipologiaDocumentale = resourceFileManager.getConfigData("TypeDocModelli");
					string[] arrModelli = new string[20];
					arrModelli[int.Parse(resourceFileManager.getConfigData("ModIndSearchTemplateName"))] = IscMassiveCardIndexes[resourceFileManager.getConfigData("IscNameSchedaTemplateName")];
					arrModelli[int.Parse(resourceFileManager.getConfigData("ModIndSearchTemplateVer"))] = IscMassiveCardIndexes[resourceFileManager.getConfigData("IscNameSchedaTemplateVer")];
					arrModelli[int.Parse(resourceFileManager.getConfigData("ModIndSearchTemplateTypeProcess"))] = resourceFileManager.getConfigData("CancTypeProcess");
					if (IscMassiveCardIndexes[resourceFileManager.getConfigData("CancMassiveObjNameIndex")] == resourceFileManager.getConfigData("CancObj4SezTerr1") ||
						IscMassiveCardIndexes[resourceFileManager.getConfigData("CancMassiveObjNameIndex")] == resourceFileManager.getConfigData("CancObj4SezTerr2"))
					{
						bAllSezTerr = true;
					}
					else if (IscMassiveCardIndexes[resourceFileManager.getConfigData("IscNameUffMitt")].ToUpper().IndexOf("UACF MI") != -1)
						sSezTer = resourceFileManager.getConfigData("SezTerII");
					else
						sSezTer = resourceFileManager.getConfigData("SezTerI");

					arrModelli[int.Parse(resourceFileManager.getConfigData("ModIndSearchSezTer"))] = sSezTer;
					logger.Debug("  Cerco per Archivio: " + resourceFileManager.getConfigData("ArchiveTypeDocModelli"));
					logger.Debug("  Cerco per TipologiaDocumentale: " + resourceFileManager.getConfigData("TypeDocModelli"));
					logger.Debug("  Cerco per Nome Modello: " + IscMassiveCardIndexes[resourceFileManager.getConfigData("IscNameSchedaTemplateName")]);
					logger.Debug("  Cerco per Versione Modello: " + IscMassiveCardIndexes[resourceFileManager.getConfigData("IscNameSchedaTemplateVer")]);
					logger.Debug("  Cerco per Tipo Processo: " + resourceFileManager.getConfigData("IscTypeProcess"));
					logger.Debug("  Cerco per UACF: " + sSezTer);
					logger.Debug("Fine ricerca record");

					if (bAllSezTerr == false)
					{
						List<SchedaSearchParameter> lSchedaModSearchParameter = new List<SchedaSearchParameter>();
						lMetadatiModelli = new List<string>(arrModelli);
						schedaModSearchParameter.MetaDati = lMetadatiModelli;
						lSchedaModSearchParameter.Add(schedaModSearchParameter);
						siavCardManager.getSearch(lSchedaModSearchParameter, siavLogin, out lGUID);
					}
					else
					{
						// Carico i modelli di entrambe le sezioni territoriali
						arrModelli[int.Parse(resourceFileManager.getConfigData("ModIndSearchSezTer"))] = resourceFileManager.getConfigData("SezTerII");
						List<SchedaSearchParameter> lSchedaModSearchParameterSez1 = new List<SchedaSearchParameter>();
						lMetadatiModelli = new List<string>(arrModelli);
						schedaModSearchParameter.MetaDati = lMetadatiModelli;
						lSchedaModSearchParameterSez1.Add(schedaModSearchParameter);
						siavCardManager.getSearch(lSchedaModSearchParameterSez1, siavLogin, out lGUID);
						// Ricerca su entrambe le sezioni territoriali
						if (lGUID.Count > 0)
						{
							arrModelli[int.Parse(resourceFileManager.getConfigData("ModIndSearchSezTer"))] = resourceFileManager.getConfigData("SezTerI");
							List<SchedaSearchParameter> lSchedaModSearchParameterSez2 = new List<SchedaSearchParameter>();
							lMetadatiModelli = new List<string>(arrModelli);
							schedaModSearchParameter.MetaDati = lMetadatiModelli;
							lSchedaModSearchParameterSez2.Add(schedaModSearchParameter);
							sDocIdTemplate2 = lGUID[0].ToString();

							siavCardManager.getSearch(lSchedaModSearchParameterSez2, siavLogin, out lGUID);
							if (lGUID.Count > 0)
							{
								sDocIdTemplate1 = lGUID[0].ToString();
							}
							else
							{
								sLog = "Non è stato trovato alcun modello per la UACF DI ROMA.";
								// notificare problema di occorrenza trovata
								throw new ArgumentException(sLog);
							}
						}
						else
						{
							sLog = "Non è stato trovato alcun modello per la UACF DI MILANO.";
							// notificare problema di occorrenza trovata
							throw new ArgumentException(sLog);
						}
					}
					CardVisibility oCardVisibility = new CardVisibility();
					siavCardManager.GetCardVisibility(GUIdcard, siavLogin, out oCardVisibility);
					logger.Debug("Sono stati trovati numero: " + lGUID.Count + " di documenti tipo modelli");

					if (lGUID.Count > 0)
					{
						logger.Debug("Gestione dati aggiuntivi");
						NameValueCollection addStringForReplace = new NameValueCollection();
						addStringForReplace.Add("DataSys", DateTime.Now.ToString("dd/MM/yyyy"));
						// Inserire casistica OMPAG con Ingiunzione

						// Fine casistica OMPAG con Ingiunzione
						logger.Debug("Carico i dati dal file excel");
						// Crea tutti i documenti principali delle singole comunicazioni

						wcfSiavAgrafManager.LoadRubriche();

						string sError = "";
						logger.Debug("Recuper l'oggetto Archivio: " + resourceFileManager.getConfigData("IscArchiveSearchUnique").ToString() + " e tipologia documentale" + resourceFileManager.getConfigData("CancTypeDocSearchUnique").ToString());
						NameValueCollection schedeComMax = null;
						if (bAllSezTerr == false)
						{
							cardAction.MainDocMaterializeFromCard(sWorkingFolder, siavLogin, fluxHelper, siavCardManager, lGUID[0].ToString(), out sModelDocX);
							logger.Debug("Creo i documenti massivi per ogni singola comunicazione da creare");
							schedeComMax = fluxHelper.CreateDocxMassive(excelDocumentReader, addStringForReplace, sModelDocX, "IscSearchValueUniqueFromAnag", out sError);
						}
						else
						{
							// Per i casi :
							// OMESSO PAGAMENTO - AVVIO DEL PROCEDIMENTO CANCELLAZIONE E INGIUNZIONE
							// OMESSO PAGAMENTO - AVVIO DEL PROCEDIMENTO CANCELLAZIONE
							// N. Delibera Misura Contributi -> «Num_Delibera Misura Contributi_Scheda»
							// N. Delibera Termini Contributi -> «Num_Delibera Termini Contributi_Scheda»
							// Data Delibera Termini -> «Data Delibera Termini_Scheda»

							// REPLACE N. Delibera Misura Contributi
							addStringForReplace.Add("Num_Delibera Misura Contributi_Scheda", IscMassiveCardIndexes[resourceFileManager.getConfigData("CancNumDelMisContr")]);
							// REPLACE N. Delibera Termini Contributi
							addStringForReplace.Add("Num_Delibera Termini Contributi_Scheda", IscMassiveCardIndexes[resourceFileManager.getConfigData("CancNumDelTerContr")]);
							// REPLACE Data Delibera Termini
							addStringForReplace.Add("Data Delibera Termini_Scheda", IscMassiveCardIndexes[resourceFileManager.getConfigData("CancDataDelTer")]);

							cardAction.MainDocMaterializeFromCard(sWorkingFolder, siavLogin, fluxHelper, siavCardManager, sDocIdTemplate1, out sPathTemplate1);
							cardAction.MainDocMaterializeFromCard(sWorkingFolder, siavLogin, fluxHelper, siavCardManager, sDocIdTemplate2, out sPathTemplate2);
							schedeComMax = fluxHelper.CreateDocxMassiveCanc(excelDocumentReader, addStringForReplace, sPathTemplate1, sPathTemplate2, "IscSearchValueUniqueFromAnag", out sError);
						}
						logger.Debug("Avvio la creazione delle singole schede documentali, in tutto: " + schedeComMax.Count);
						Guid outCard = new Guid();
						Archive oArchive;
						DocumentType oDocumentType;
						siavCardManager.getArchiveDoc(resourceFileManager.getConfigData("IscArchiveSearchUnique").ToString(), siavLogin.oSessionInfo, out oArchive);
						siavCardManager.getTypeDoc(resourceFileManager.getConfigData("CancTypeDocSearchUnique").ToString(), siavLogin.oSessionInfo, out oDocumentType);

						//siavCardManager.getTypeDoc();
						for (int i = 0; i < schedeComMax.Count; i++)
						{
							NameValueCollection lfieldXls = new NameValueCollection();
							logger.Debug("Inizio lavorazione scheda " + i);

							AgrafCard svAgrafCardRub = new AgrafCard();
							svAgrafCardRub.CardContacts = new List<AgrafCardContact>();
							svAgrafCardRub.Tag = sIdIndexTagRubricaGen;
							AgrafCard svAgrafCardApf = new AgrafCard();
							svAgrafCardApf.CardContacts = new List<AgrafCardContact>();

							svAgrafCardApf.Tag = sIdIndexTagRubricaApf;
							logger.Debug("Dati rubrica caricati");

							Model.Siav.APFlibrary.Model.FieldsCard oFieldCard = new Model.Siav.APFlibrary.Model.FieldsCard();
							oFieldCard.Oggetto = IscMassiveCardIndexes.GetValues(resourceFileManager.getConfigData("IscMassiveObjName"))[0];
							sObjForObj = oFieldCard.Oggetto;
							string[] oMetadati = new string[20];
							string sNote = "";
							string sMessage = "";
							string sStato = "";
							// Id di riferimento comunicazione massiva
							oMetadati[int.Parse(resourceFileManager.getConfigData("IscIndSchedIdComMax"))] = IscMassiveCardIndexes.GetValues(resourceFileManager.getConfigData("lscMassiveId"))[0];
							sIdComMaxivaForObj = oMetadati[int.Parse(resourceFileManager.getConfigData("IscIndSchedIdComMax"))];
							// Sez terr di riferimento
							if (bAllSezTerr)
							{
								string sValueSezTer = "";
								fluxHelper.GetSetTerFromAnag(excelDocumentReader, schedeComMax.GetKey(i).Trim(), out sValueSezTer);
								if (sValueSezTer.ToUpper().IndexOf("MILANO") != -1)
									sSezTer = resourceFileManager.getConfigData("SezTerII");
								else
									sSezTer = resourceFileManager.getConfigData("SezTerI");
							}
							oMetadati[int.Parse(resourceFileManager.getConfigData("IscNameSchedSezTer"))] = sSezTer;
							// Ufficio Mittente
							oMetadati[int.Parse(resourceFileManager.getConfigData("IscIndSchedUffMitt"))] = IscMassiveCardIndexes[resourceFileManager.getConfigData("IscNameUffMitt")];
							// Mezzo di trasmissione
							oMetadati[int.Parse(resourceFileManager.getConfigData("IscIndSchedMezTrasm"))] = IscMassiveCardIndexes[resourceFileManager.getConfigData("IscNameMezTrasm")];
							// AnagraficaXls
							string sValueXls = "";
							string sColumnNameAnagraf = resourceFileManager.getConfigData("IscSearchValueUniqueFromAnag");
							fluxHelper.GetCsvRecordKV(excelDocumentReader, sColumnNameAnagraf, schedeComMax.GetKey(i).Trim(), out lfieldXls);

							fluxHelper.GetCsvRecord(excelDocumentReader, sColumnNameAnagraf, schedeComMax.GetKey(i).Trim(), out sValueXls);
							/*
								  Implementazione per la gestione del campo  Mezzo di spedizione

								Il cmapo report anagrafico è così composto:
								COGNOME|MOXXXTTI|NOME|PXXXXXO|CODICE_FISCALE|xxxxx2232y3sywywsw|INDIRIZZO|VIA DI SANTA CROCE IN GERUSALEMME 91|CAP|00185|LOCALITA|ROMA|COMUNE|ROMA|PROVINCIA|RM|NUM_DELIBERA|993|DATA_DELIBERA|10/01/2019|CODICE CONSOB|90270|TIPO ISCRIZIONE||CODICE OCF|03E4A03A-8F14-4897-BE99-14B94B661A0A|NAZIONE INDIRIZZO||AREA TARIFFARIA|AM|

								*/
							string sMezzoDiTrasmissione = "";
							if (IscMassiveCardIndexes[resourceFileManager.getConfigData("IscNameMezTrasm").ToUpper()] == resourceFileManager.getConfigData("sCheckMezDiTrasmPEC").ToUpper())
							{
								sMezzoDiTrasmissione = resourceFileManager.getConfigData("sCheckMezDiTrasmPEC").ToUpper();
							}
							else if (IscMassiveCardIndexes[resourceFileManager.getConfigData("IscNameMezTrasm").ToUpper()] == resourceFileManager.getConfigData("sCheckMezDiTrasmRacAR").ToUpper())
							{
								sMezzoDiTrasmissione = resourceFileManager.getConfigData("sMezDiTrasmRacAR").ToUpper();
							}
							else
							{
								sMezzoDiTrasmissione = IscMassiveCardIndexes[resourceFileManager.getConfigData("IscNameMezTrasm").ToUpper()];
							}
							sValueXls += "Mezzo di trasmissione|" + sMezzoDiTrasmissione + "|";
							oMetadati[int.Parse(resourceFileManager.getConfigData("IscIndSchedAnagXls"))] = sValueXls;
							// Generazione Codice univoco
							// Individuazione della configurazione del codice univoco
							string sConfigurationCancCU = resourceFileManager.getConfigData("CancConfigCUStructure");
							// CancCADCD=CANCELLAZIONE DOMANDA COMUNICAZIONE|CancOPADPDCEI=AVVIO CANCELLAZIONE INGIUNZIONE|CancOPCDDC=OMESSO COMUNICAZIONE CANCELLAZIONE|CancOPCDDCEI=OMESSO COMUNICAZIONE CANCELLAZIONE|CancOPLA=OMESSO LETTERA ACCOMPAGNATORIA
							string[] sValueConfiguration = sConfigurationCancCU.Split('|');
							string sConfigurationCUIdentified = "";
							foreach (string sValueConf in sValueConfiguration)
							{
								string sCheckConfigurationCU = resourceFileManager.getConfigData(sValueConf);
								// Ricerca tutte le parole presenti in base al  parametro di configurazione che confrontate all'oggetto
								// identificano il tipo di documento trattato
								if (fluxHelper.CheckWordInPhrase(oFieldCard.Oggetto, sCheckConfigurationCU))
								{
									sConfigurationCUIdentified = sValueConf;
									break;
								}
							}
							string sCodiceUnivoco = "";
							sCodiceUnivoco = resourceFileManager.getConfigData(sConfigurationCUIdentified + "_conf");
							logger.Debug("Codice Univoco: " + sCodiceUnivoco + " - Letto dalla chiave: " + sConfigurationCUIdentified + "_conf");
							string[] sFormatValue = sCodiceUnivoco.Split('|');
							foreach (string sValue in sFormatValue)
							{

								string sValueNormalized = sValue.Replace(" ", "_");
								string sCheckCodiceUnivoco = sCodiceUnivoco;
								logger.Debug("sValueNormalized: " + sValueNormalized + " - sColumnNameAnagraf: " + sColumnNameAnagraf + "_conf");
								sCodiceUnivoco = sCodiceUnivoco.Replace(sValueNormalized, lfieldXls[sValueNormalized].Trim());
								if (sCodiceUnivoco == sCheckCodiceUnivoco)
									throw new ArgumentException("Non è stato possibile generare il codice univoco, il valore non identificato è: " + sValueNormalized + " i nomi dei campi nel foglio Excel attesi sono i seguenti: " + resourceFileManager.getConfigData(sConfigurationCUIdentified + "_conf").Replace('|', ' '));
							}
							oMetadati[int.Parse(resourceFileManager.getConfigData("IscIndCodiceUnivoco"))] = sCodiceUnivoco;
							List<Office> oOffices = new List<Office>();
							logger.Debug("Metadati caricati");
							if (!string.IsNullOrEmpty(schedeComMax.GetKey(i).ToString()))
							{

								// Ricerca all'interno della rubrica generica
								List<GenericEntity> anagUsersRubricaGen;
								if (IscMassiveCardIndexes[resourceFileManager.getConfigData("IscDescObjForAttach")] == resourceFileManager.getConfigData("CancValueObjForAttach1"))
								{
									anagUsersRubricaGen = wcfSiavAgrafManager.GetUsers(schedeComMax.GetKey(i).Trim(), resourceFileManager.getConfigData("NomeRubricaCompleta"), resourceFileManager.getConfigData("NomeCompanyRubricaSearch"));
								}
								else
								{
									anagUsersRubricaGen = wcfSiavAgrafManager.GetUsers(schedeComMax.GetKey(i).Trim(), resourceFileManager.getConfigData("NomeRubricaCompleta"), resourceFileManager.getConfigData("NomePersonRubricaSearch"));
								}
								//List<GenericEntity> anagUsersRubricaGen = wcfSiavAgrafManager.GetUsers(schedeComMax.GetKey(i).Trim(), resourceFileManager.getConfigData("NomeRubricaCompleta"), resourceFileManager.getConfigData("NomePersonRubricaSearch"));
								if (anagUsersRubricaGen.Count > 1)
								{
									// caso errore trovati più utenti con lo stesso codice fiscale
									sNote = "Sono stati trovati n°:" + anagUsersRubricaGen.Count + " record per il codice OCF:" + schedeComMax.GetKey(i) + ", verificare l'anagrafica " + resourceFileManager.getConfigData("NomeRubricaCompleta");
									logger.Debug(sNote);
									sStato = resourceFileManager.getConfigData("IscStatoSingleCom");
									isInError = true;
								}
								else if (anagUsersRubricaGen.Count == 1)
								{
									logger.Debug("Associazione anagrafica con rubrica generica avvenuta con successo");

									// caso corretto -- implementazione algoritmo Associazione con Anagrafica
									AgrafCardContact svCardContactRg = new AgrafCardContact();
									svCardContactRg.EntityId = new AgrafCardContactId();
									svCardContactRg.EntityId.ContactId = new AgrafEntityId();
									svCardContactRg.EntityId.ContactId.EntityId = anagUsersRubricaGen[0].EntityId.Id;
									svCardContactRg.EntityId.ContactId.Version = (int)anagUsersRubricaGen[0].EntityId.Version;
									svAgrafCardRub.CardContacts.Add(svCardContactRg);
									// Destinatario
									oMetadati[int.Parse(resourceFileManager.getConfigData("IscIndDestinatario"))] = anagUsersRubricaGen[0].Name + " " + anagUsersRubricaGen[0].Person.LastName;
								}
								else
								{
									// caso in cui non è stato trovato l'utente per la scheda
									sNote = "Non è stato trovato alcun record per il codice OCF:" + schedeComMax.GetKey(i) + ", verificare l'anagrafica " + resourceFileManager.getConfigData("NomeRubricaCompleta");
									logger.Debug(sNote);
									sStato = resourceFileManager.getConfigData("IscStatoSingleCom");
									isInError = true;
								}
								// Ricerca all'interno della rubrica Aspiranti e promotori finanziari
								List<GenericEntity> anagUsersAspPF;// = wcfSiavAgrafManager.GetUsers(schedeComMax.GetKey(i).Trim(), resourceFileManager.getConfigData("NomeRubricaAspPf"), resourceFileManager.getConfigData("NomePersonRubricaSearch"));
								if (IscMassiveCardIndexes[resourceFileManager.getConfigData("IscDescObjForAttach")] == resourceFileManager.getConfigData("CancValueObjForAttach1"))
								{
									anagUsersAspPF = wcfSiavAgrafManager.GetUsers(schedeComMax.GetKey(i).Trim(), resourceFileManager.getConfigData("NomeRubricaAspPf"), resourceFileManager.getConfigData("NomeCompanyRubricaSearch"));
								}
								else
								{
									anagUsersAspPF = wcfSiavAgrafManager.GetUsers(schedeComMax.GetKey(i).Trim(), resourceFileManager.getConfigData("NomeRubricaAspPf"), resourceFileManager.getConfigData("NomePersonRubricaSearch"));
								}
								if (anagUsersAspPF.Count > 1)
								{
									// caso errore trovati più utenti con lo stesso codice fiscale
									sNote = "Sono stati trovati n°:" + anagUsersAspPF.Count + " record per il codice OCF:" + schedeComMax.GetKey(i) + ", verificare l'anagrafica " + resourceFileManager.getConfigData("NomeRubricaAspPf");
									logger.Debug(sNote);
									sStato = resourceFileManager.getConfigData("IscStatoSingleCom");
									isInError = true;
								}
								else if (anagUsersAspPF.Count == 1)
								{
									logger.Debug("Associazione anagrafica con rubrica Promotori finanziari avvenuta con successo");

									// caso corretto -- implementazione algoritmo Associazione con Anagrafica
									AgrafCardContact svCardContactApf = new AgrafCardContact();
									svCardContactApf.EntityId = new AgrafCardContactId();
									svCardContactApf.EntityId.ContactId = new AgrafEntityId();
									svCardContactApf.EntityId.ContactId.EntityId = anagUsersAspPF[0].EntityId.Id;
									svCardContactApf.EntityId.ContactId.Version = (int)anagUsersAspPF[0].EntityId.Version;
									svAgrafCardApf.CardContacts.Add(svCardContactApf);
									// Codice Fiscale
									oMetadati[int.Parse(resourceFileManager.getConfigData("IscIndSchedCodFisc"))] = anagUsersRubricaGen[0].TaxID;
									// codice APF 
									oMetadati[int.Parse(resourceFileManager.getConfigData("IscIndSchedCodApf"))] = anagUsersRubricaGen[0].GenericEntityExternalId;
									// Codice Consob 
									oMetadati[int.Parse(resourceFileManager.getConfigData("IscIndSchedCodConsob"))] = anagUsersRubricaGen[0].GenericEntityExternalId2;
									// Classifica
									oMetadati[int.Parse(resourceFileManager.getConfigData("IscIndSchedClassifica"))] = anagUsersRubricaGen[0].GenericEntityExternalId3;
								}
								else
								{
									// caso in cui non è stato trovato l'utente per la scheda
									sNote = "Non è stato trovato alcun record per il codice OCF:" + schedeComMax.GetKey(i) + ", verificare l'anagrafica " + resourceFileManager.getConfigData("NomeRubricaAspPf");
									logger.Debug(sNote);
									sStato = resourceFileManager.getConfigData("IscStatoSingleCom");
									isInError = true;
								}
								// Codice Fiscale
								oMetadati[int.Parse(resourceFileManager.getConfigData("IscIndSchedCodApf"))] = schedeComMax.GetKey(i).Trim();

								// Stato
								oMetadati[int.Parse(resourceFileManager.getConfigData("IscIndSchedStato"))] = sStato;
								oFieldCard.MetaDati = new List<string>(oMetadati);
								// Gestione caso invio delibera
								// se il valore è = CancObjAttachDelib
								List<string> sPathAttachmentsCard = new List<string>();
								if (IscMassiveCardIndexes[resourceFileManager.getConfigData("CancMassiveObjNameIndex")] == resourceFileManager.getConfigData("CancObjAttachDelib") ||
									IscMassiveCardIndexes[resourceFileManager.getConfigData("CancMassiveObjNameIndex")] == resourceFileManager.getConfigData("CancObjAttachDelib1"))
								{
									siavCardManager.GetCardAttachments(sWorkingFolder, oCardBundle.CardId.ToString(), siavLogin, out sPathAttachmentsCard);
									if (sPathAttachmentsCard == null)
									{
										throw new ArgumentException("Non è stato trovato alcun allegato, si richiede l'inserimento del documento.");
									}
								}
								// fine gestione delibera 
								// Se è di tipo lettera accompagnatoria, recupero il documento principale della scheda protocollata e lo aggiungo tra gli allegati.
								if (IscMassiveCardIndexes[resourceFileManager.getConfigData("CancMassiveObjNameIndex")] == resourceFileManager.getConfigData("CancObjAttachLettAcc"))
								{
									string sPathMainDoc = "";
									string sGuidCardMainDoc = "";
									CardBundle oCardBundleMainDoc;
									// Ricerca documento principale protocollato
									List<SchedaSearchParameter> lSchedaSearchParameterMD = new List<SchedaSearchParameter>();

									List<string> lMetadati;
									SchedaSearchParameter schedaSearchParameter = new SchedaSearchParameter();
									schedaSearchParameter.Archivio = resourceFileManager.getConfigData("CancArcCardInProt");
									schedaSearchParameter.TipologiaDocumentale = resourceFileManager.getConfigData("CancDocTypeCardInProt");
									string sDataXls = "";
									fluxHelper.GetDataFromAnag(excelDocumentReader, "IndexNumProtFromExcel", schedeComMax.GetKey(i).Trim(), out sDataXls);
									schedaSearchParameter.IdRiferimento = sDataXls.Trim();
									string[] arr = new string[20];
									logger.Debug("Inizio ricerca record");
									logger.Debug("  Cerco per Archivio: " + resourceFileManager.getConfigData("IscArchiveSearchUnique"));
									logger.Debug("  Cerco per TipologiaDocumentale: " + resourceFileManager.getConfigData("IscTypeDocSearchUnique"));
									logger.Debug("  Cerco per Protocollo: " + schedaSearchParameter.IdRiferimento);
									logger.Debug("Fine ricerca record");

									lMetadati = new List<string>(arr);
									schedaSearchParameter.MetaDati = lMetadati;
									lSchedaSearchParameterMD.Add(schedaSearchParameter);
									siavCardManager.getSearch(lSchedaSearchParameterMD, siavLogin, out lGUID);
									if (lGUID.Count > 0)
									{
										sGuidCardMainDoc = lGUID[0].ToString();
									}
									else
									{
										sLog = "Non è stata individuata la scheda protocollata";
										// notificare problema di occorrenza trovata
										throw new ArgumentException(sLog);
									}

									siavCardManager.GetCard(sGuidCardMainDoc, siavLogin, out oCardBundleMainDoc);
									siavCardManager.GetCardMainDoc(sWorkingFolder, oCardBundleMainDoc, siavLogin, out sPathMainDoc);
									if (string.IsNullOrEmpty(sPathMainDoc))
									{
										throw new ArgumentException("Non è stato trovato alcun allegato, si richiede l'inserimento del documento.");
									}
									else
										sPathAttachmentsCard.Add(sPathMainDoc);
								}
								logger.Debug("Avvio il processo di inseriemento");
								siavCardManager.Insert(sPathAttachmentsCard, schedeComMax.GetValues(i)[0], siavLogin, oFieldCard, oCardVisibility.Groups, oCardVisibility.Offices, oCardVisibility.Users, oDocumentType, oArchive, sNote, sMessage, svAgrafCardRub, svAgrafCardApf, out outCard);
								sResult.Add(outCard.ToString());

								logger.Debug("Scheda inserita, il suo id: " + outCard.ToString());
								logger.Debug("Fine lavorazione scheda " + i);
							}
							else
							{
								logger.Debug("Scheda NON inserita, il suo codice OCF è vuoto");
							}
						}
						sOutput = "Sono state lavorate " + schedeComMax.Count + " schede. ";
						logger.Debug(sOutput);
						//Codice Promotore	Cognome	Nome	Indirizzo	Località	Cap	Provincia	Nr. raccomandata	Data Spedizione	Data ricezione cartolina	Causale mancata ricezione	Nr. Protocollo	Data protocollo	Spese di notifica
					}
					else
					{// errore nessun modello trovato
						sLog = "Non è stato trovato alcun modello.";
						throw new ArgumentException(sLog);
					}
					if (isInError)
					{
						siavCardManager.FieldModify(oCardBundle, siavLogin, 22, "COMPLETATO CON ERRORI");
						sOutput += "Attività completata con errori.";
					}
					else
					{
						siavCardManager.FieldModify(oCardBundle, siavLogin, 22, "COMPLETATO");
						sOutput += "Attività completata.";
					}
					siavCardManager.InsertNote(oCardBundle, siavLogin, sOutput + " Per maggiori informazioni consultare il log per l'id transazione: " + this.IdSessioneTransazione, resourceFileManager.getConfigData("AutorAnnotation"));
					sLog = sOutput;
					siavLogin.Logout();
					siavLogin.siavWsLogin.Close();
					siavLogin = null;
					siavCardManager.siavWsCard.Close();
					siavCardManager = null;
					wcfSiavAgrafManager.siavWsAgraf.Close();
					wcfSiavAgrafManager = null;
				}
				else
				{
					sLog = "Non è stato trovato alcun campo identificativo all'interno del file Excel.";
					throw new ArgumentException(sLog);
				}
				//}
				//else
				//{
				//    sLog = "Non è stato trovato alcun record all'interno del file Excel.";
				//    throw new ArgumentException(sLog);
				//} 
			}
			catch (Exception ex)
			{
				sErrorObj = "ERRORE Processo CANCELLAZIONE. ";
				// sOutput è formato da IDSessioneTransazione|Descrizione anomalia;
				sOutput = this.IdSessioneTransazione + "|" + ex.Message;
				try
				{
					logger.Error(ex.ToString());
					if (siavCardManager != null)
					{
						siavCardManager.InsertNote(oCardBundle, siavLogin, ex.Message + " Per maggiori informazioni consultare il log per l'id transazione: " + this.IdSessioneTransazione, resourceFileManager.getConfigData("AutorAnnotation"));
						siavCardManager.siavWsCard.Abort();
					}
					if (siavLogin != null)
					{
						siavLogin.Logout();
						siavLogin.siavWsLogin.Close();
					}
					if (wcfSiavAgrafManager != null)
					{
						wcfSiavAgrafManager.siavWsAgraf.Abort();
					}
				}
				catch (Exception e) { }
			}
			finally
			{
				logger.Debug("Fine elaborazione");
				if (resourceFileManager.getConfigData("isTest") != "SI")
				{
					SendMail sendMail = new SendMail();
					string sReceiverEmail = resourceFileManager.getConfigData("emailDestinatario");
					string sSenderNameEmail = resourceFileManager.getConfigData("emailNomeMittente");
					string sObject = sErrorObj + "Comunicazione Massiva " + sObjForObj + " : Inserimento schede in predisposizione " + sSezTer;
					if (sLog == "") sLog = "Problema con l'esecuzione dell'algoritmo di sistema, prego contattare il supporto tecnico.";
					string sbodyMsg = sLog + " Comunicazione Massiva: " + sIdComMaxivaForObj;
					sendMail.SENDMAIL(sReceiverEmail, sSenderNameEmail, sObject, sbodyMsg, "");
				}
				//siavLogin.Logout();
			}
			return sResult.Select(s => (object)s).ToArray();
		}
		public object[] Step1(string GUIdcard, out string sOutput)
		{
			List<String> sResult = new List<string>();
			sOutput = "";
			bool isInError = false;
			WcfSiavLoginManager siavLogin = null;
			WcfSiavAgrafManager wcfSiavAgrafManager = null;
			WcfSiavCardManager siavCardManager = null;
			CardBundle oCardBundle = null;
			string sLog = string.Empty;
			String sIdComMaxivaForObj = "";
			String sObjForObj = "";
			string sSezTer = "";
			string sErrorObj = "";
			List<string> sPathAttachmentsCard = null;
			try
			{
				siavLogin = new WcfSiavLoginManager();
				wcfSiavAgrafManager = new WcfSiavAgrafManager(logger);
				FluxHelper fluxHelper = new FluxHelper();
				string oUserName = resourceFileManager.getConfigData("UserFlux");
				string oUserPwd = oRijndael.Decrypt(resourceFileManager.getConfigData("PasswordFlux"));
				string sModelDocX = "";

				MainDoc oMainDoc;
				NameValueCollection IscMassiveCardIndexes;
				// Esegue il login
				siavLogin.Login(oUserName, oUserPwd);
				logger.Debug("Sessione login Archiflow: " + siavLogin.oSessionInfo.SessionId);

				siavCardManager = new WcfSiavCardManager(logger);
				// Recupera la scheda ove è stato avviato il processo
				siavCardManager.GetCard(GUIdcard, siavLogin, out oCardBundle);

				// Recupera il documento principale della scheda 
				siavCardManager.GetMainDoc(oCardBundle, out oMainDoc);
				// Recupero gli indici della scheda 
				siavCardManager.GetIndexes(oCardBundle, out IscMassiveCardIndexes);
				logger.Debug("Recuperati i dati dalla scheda che ha avviato il processo : " + GUIdcard);

				// Materializza il file sul filesystem
				string sAnagraphicXlsSource = this.sWorkingFolder + @"\" + oMainDoc.Filename + "_AnagSource." + oMainDoc.Extension;
				logger.Debug("Scrivo il documento principale: " + sAnagraphicXlsSource);
				fluxHelper.FileMaterialize(sAnagraphicXlsSource, oMainDoc.oByte);

				// Estraggo i dati dal file Excel  
				ExcelDocumentReader excelDocumentReader = new ExcelDocumentReader(sAnagraphicXlsSource, sWorkingFolder);
				//ExcelDocumentReader excelDocumentReader = new ExcelDocumentReader(sAnagraphicXlsSource, sWorkingFolder);
				logger.Debug("Lettura del file excel: " + sAnagraphicXlsSource);

				// Verifica ricerca presenza record
				List<string> lCodiceFiscale = new List<string>();
				logger.Debug("Sono stati letti " + excelDocumentReader.getData.Count() + " di record");

				if (excelDocumentReader.getData.Count() > 0)
				{
					// Lista di codici fiscali da verificare
					//logger.Debug("Estrapolo i primi 10 record da verificare.");
					//string sColumnIdSubject = resourceFileManager.getConfigData("IscSearchValueUniqueFromAnag");
					//fluxHelper.GetSampleRecords(excelDocumentReader, sColumnIdSubject,out lCodiceFiscale);
					//if (lCodiceFiscale.Count > 0)
					//{

					List<Guid> lGUID = new List<Guid>();
					/*
					foreach(var singleCf in lCodiceFiscale){
						List<SchedaSearchParameter> lSchedaSearchParameter = new List<SchedaSearchParameter>();

						List<string> lMetadati;
						SchedaSearchParameter schedaSearchParameter = new SchedaSearchParameter();
						schedaSearchParameter.Archivio = resourceFileManager.getConfigData("IscArchiveSearchUnique");
						schedaSearchParameter.TipologiaDocumentale = resourceFileManager.getConfigData("IscTypeDocSearchUnique");
						schedaSearchParameter.Oggetto = resourceFileManager.getConfigData("IscObjSearchUnique");
						string[] arr = new string[20]; 
						arr[int.Parse(resourceFileManager.getConfigData("IscIndSchedSearchUnique"))] = singleCf;
						logger.Debug("Inizio ricerca record");
						logger.Debug("  Cerco per Archivio: " + resourceFileManager.getConfigData("IscArchiveSearchUnique"));
						logger.Debug("  Cerco per TipologiaDocumentale: " +  resourceFileManager.getConfigData("IscTypeDocSearchUnique"));
						logger.Debug("  Cerco per Oggetto: " + resourceFileManager.getConfigData("IscObjSearchUnique"));
						logger.Debug("  Cerco per Codice OCF: " + singleCf);
						logger.Debug("Fine ricerca record");

						lMetadati = new List<string>(arr);
						schedaSearchParameter.MetaDati = lMetadati;
						lSchedaSearchParameter.Add(schedaSearchParameter);
						siavCardManager.getSearch(lSchedaSearchParameter, siavLogin, out lGUID);
						if (lGUID.Count > 0)
						{
							sLog = "Sono state trovate schede già inserite a sistema";
							// notificare problema di occorrenza trovata
							throw new ArgumentException(sLog);
						}
					}
					*/
					logger.Debug("Inizio Ricerca documento modelli ");

					List<SchedaSearchParameter> lSchedaModSearchParameter = new List<SchedaSearchParameter>();
					List<string> lMetadatiModelli;
					// ricerco tra le tipologie Modelli, il documento necessario alla creazione della comunicazione
					SchedaSearchParameter schedaModSearchParameter = new SchedaSearchParameter();
					schedaModSearchParameter.Archivio = resourceFileManager.getConfigData("ArchiveTypeDocModelli");
					schedaModSearchParameter.TipologiaDocumentale = resourceFileManager.getConfigData("TypeDocModelli");
					string[] arrModelli = new string[20];
					arrModelli[int.Parse(resourceFileManager.getConfigData("ModIndSearchTemplateName"))] = IscMassiveCardIndexes[resourceFileManager.getConfigData("IscNameSchedaTemplateName")];
					arrModelli[int.Parse(resourceFileManager.getConfigData("ModIndSearchTemplateVer"))] = IscMassiveCardIndexes[resourceFileManager.getConfigData("IscNameSchedaTemplateVer")];
					arrModelli[int.Parse(resourceFileManager.getConfigData("ModIndSearchTemplateTypeProcess"))] = resourceFileManager.getConfigData("IscTypeProcess");
					if (IscMassiveCardIndexes[resourceFileManager.getConfigData("IscNameUffMitt")].ToUpper().IndexOf("UACF MI") != -1)
						sSezTer = resourceFileManager.getConfigData("SezTerII");
					else
						sSezTer = resourceFileManager.getConfigData("SezTerI");

					arrModelli[int.Parse(resourceFileManager.getConfigData("ModIndSearchSezTer"))] = sSezTer;
					logger.Debug("  Cerco per Archivio: " + resourceFileManager.getConfigData("ArchiveTypeDocModelli"));
					logger.Debug("  Cerco per TipologiaDocumentale: " + resourceFileManager.getConfigData("TypeDocModelli"));
					logger.Debug("  Cerco per Nome Modello: " + IscMassiveCardIndexes[resourceFileManager.getConfigData("IscNameSchedaTemplateName")]);
					logger.Debug("  Cerco per Versione Modello: " + IscMassiveCardIndexes[resourceFileManager.getConfigData("IscNameSchedaTemplateVer")]);
					logger.Debug("  Cerco per Tipo Processo: " + resourceFileManager.getConfigData("IscTypeProcess"));
					logger.Debug("  Cerco per UACF: " + sSezTer);
					logger.Debug("Fine ricerca record");

					lMetadatiModelli = new List<string>(arrModelli);
					schedaModSearchParameter.MetaDati = lMetadatiModelli;
					lSchedaModSearchParameter.Add(schedaModSearchParameter);
					siavCardManager.getSearch(lSchedaModSearchParameter, siavLogin, out lGUID);
					CardVisibility oCardVisibility = new CardVisibility();
					siavCardManager.GetCardVisibility(GUIdcard, siavLogin, out oCardVisibility);
					logger.Debug("Sono stati trovati numero: " + lGUID.Count + " di documenti tipo modelli");
					if (lGUID.Count > 0)
					{
						foreach (var singleGuid in lGUID)
						{
							CardBundle oCardModelBundle;
							MainDoc oMainDocModel;
							// Recupera la scheda ove è stato avviato il processo
							siavCardManager.GetCard(singleGuid.ToString(), siavLogin, out oCardModelBundle);
							// Recupera il documento principale della scheda 
							logger.Debug("Recupero il documento principale della scheda");
							siavCardManager.GetMainDoc(oCardModelBundle, out oMainDocModel);
							//		
							sPathAttachmentsCard = new List<string>();
							if (IscMassiveCardIndexes[resourceFileManager.getConfigData("IscDescObjForAttach")] == resourceFileManager.getConfigData("IssValueObjForAttach1") ||
								IscMassiveCardIndexes[resourceFileManager.getConfigData("IscDescObjForAttach")] == resourceFileManager.getConfigData("IssValueObjForAttach2"))
							{
								siavCardManager.GetCardAttachments(sWorkingFolder, oCardBundle.CardId.ToString(), siavLogin, out sPathAttachmentsCard);
								if (sPathAttachmentsCard == null)
								{
									throw new ArgumentException("Non è stato trovato alcun allegato, si richiede l'inserimento del documento.");
								}
							}
							// Materializza il file sul filesystem
							logger.Debug("Scrivo il modello in: " + sModelDocX);
							sModelDocX = sWorkingFolder + @"\" + oMainDocModel.Filename + "_ModelSource." + oMainDocModel.Extension;
							fluxHelper.FileMaterialize(sModelDocX, oMainDocModel.oByte);
							string sError = "";

							logger.Debug("Gestione dati aggiuntivi");
							NameValueCollection addStringForReplace = new NameValueCollection();
							addStringForReplace.Add("DataSys", DateTime.Now.ToString("dd/MM/yyyy"));
							logger.Debug("Carico i dati dal file excel");
							// Crea tutti i documenti principali delle singole comunicazioni

							wcfSiavAgrafManager.LoadRubriche();

							logger.Debug("Creo i documenti massivi per ogni singola comunicazione da creare");
							NameValueCollection schedeComMax = fluxHelper.CreateDocxMassive(excelDocumentReader, addStringForReplace, sModelDocX, "IscSearchValueUniqueFromAnag", out sError);
							Guid outCard = new Guid();
							Archive oArchive;
							DocumentType oDocumentType;
							siavCardManager.getArchiveDoc(resourceFileManager.getConfigData("IscArchiveSearchUnique").ToString(), siavLogin.oSessionInfo, out oArchive);
							siavCardManager.getTypeDoc(resourceFileManager.getConfigData("IscTypeDocSearchUnique").ToString(), siavLogin.oSessionInfo, out oDocumentType);
							logger.Debug("Recuper l'oggetto Archivio: " + resourceFileManager.getConfigData("IscArchiveSearchUnique").ToString() + " e tipologia documentale" + resourceFileManager.getConfigData("IscTypeDocSearchUnique").ToString());
							logger.Debug("Avvio la creazione delle singole schede documentali, in tutto: " + schedeComMax.Count);

							//siavCardManager.getTypeDoc();
							for (int i = 0; i < schedeComMax.Count; i++)
							{
								NameValueCollection lfieldXls = new NameValueCollection();
								logger.Debug("Inizio lavorazione scheda " + i);

								AgrafCard svAgrafCardRub = new AgrafCard();
								svAgrafCardRub.CardContacts = new List<AgrafCardContact>();
								svAgrafCardRub.Tag = sIdIndexTagRubricaGen;
								AgrafCard svAgrafCardApf = new AgrafCard();
								svAgrafCardApf.CardContacts = new List<AgrafCardContact>();
								svAgrafCardApf.Tag = sIdIndexTagRubricaApf;
								logger.Debug("Dati rubrica caricati");

								Model.Siav.APFlibrary.Model.FieldsCard oFieldCard = new Model.Siav.APFlibrary.Model.FieldsCard();
								oFieldCard.Oggetto = IscMassiveCardIndexes.GetValues(resourceFileManager.getConfigData("IscMassiveObjName"))[0];
								sObjForObj = oFieldCard.Oggetto;
								string[] oMetadati = new string[20];
								string sNote = "";
								string sMessage = "";
								string sStato = "";
								// Id di riferimento comunicazione massiva
								oMetadati[int.Parse(resourceFileManager.getConfigData("IscIndSchedIdComMax"))] = IscMassiveCardIndexes.GetValues(resourceFileManager.getConfigData("lscMassiveId"))[0];
								sIdComMaxivaForObj = oMetadati[int.Parse(resourceFileManager.getConfigData("IscIndSchedIdComMax"))];
								// Sez terr di riferimento
								oMetadati[int.Parse(resourceFileManager.getConfigData("IscNameSchedSezTer"))] = sSezTer;
								// Ufficio Mittente
								oMetadati[int.Parse(resourceFileManager.getConfigData("IscIndSchedUffMitt"))] = IscMassiveCardIndexes[resourceFileManager.getConfigData("IscNameUffMitt")];
								// Mezzo di trasmissione
								oMetadati[int.Parse(resourceFileManager.getConfigData("IscIndSchedMezTrasm"))] = IscMassiveCardIndexes[resourceFileManager.getConfigData("IscNameMezTrasm")];
								// AnagraficaXls
								string sValueXls = "";
								string sColumnNameAnagraf = resourceFileManager.getConfigData("IscSearchValueUniqueFromAnag");
								logger.Debug("sColumnNameAnagraf= " + sColumnNameAnagraf);
								logger.Debug("schedeComMax.GetKey(i).Trim()= " + schedeComMax.GetKey(i).Trim());

								fluxHelper.GetCsvRecordKV(excelDocumentReader, sColumnNameAnagraf, schedeComMax.GetKey(i).Trim(), out lfieldXls);
								fluxHelper.GetCsvRecord(excelDocumentReader, sColumnNameAnagraf, schedeComMax.GetKey(i).Trim(), out sValueXls);
								/*
								  Implementazione per la gestione del campo  Mezzo di spedizione

								Il cmapo report anagrafico è così composto:
								COGNOME|MOXXXTTI|NOME|PXXXXXO|CODICE_FISCALE|xxxxx2232y3sywywsw|INDIRIZZO|VIA DI SANTA CROCE IN GERUSALEMME 91|CAP|00185|LOCALITA|ROMA|COMUNE|ROMA|PROVINCIA|RM|NUM_DELIBERA|993|DATA_DELIBERA|10/01/2019|CODICE CONSOB|90270|TIPO ISCRIZIONE||CODICE OCF|03E4A03A-8F14-4897-BE99-14B94B661A0A|NAZIONE INDIRIZZO||AREA TARIFFARIA|AM|

								*/
								string sMezzoDiTrasmissione = "";
								if (IscMassiveCardIndexes[resourceFileManager.getConfigData("IscNameMezTrasm").ToUpper()] == resourceFileManager.getConfigData("sCheckMezDiTrasmPEC").ToUpper())
								{
									sMezzoDiTrasmissione = resourceFileManager.getConfigData("sCheckMezDiTrasmPEC").ToUpper();
								}
								else if (IscMassiveCardIndexes[resourceFileManager.getConfigData("IscNameMezTrasm").ToUpper()] == resourceFileManager.getConfigData("sCheckMezDiTrasmRacAR").ToUpper())
								{
									sMezzoDiTrasmissione = resourceFileManager.getConfigData("sMezDiTrasmRacAR").ToUpper();
								}
								else
								{
									sMezzoDiTrasmissione = IscMassiveCardIndexes[resourceFileManager.getConfigData("IscNameMezTrasm").ToUpper()];
								}
								sValueXls += "Mezzo di trasmissione|" + sMezzoDiTrasmissione + "|";

								oMetadati[int.Parse(resourceFileManager.getConfigData("IscIndSchedAnagXls"))] = sValueXls;
								// Generazione Codice univoco
								string sCodiceUnivoco = resourceFileManager.getConfigData("IscFormatCodiceUnivoco");
								string[] sFormatValue = sCodiceUnivoco.Split('|');

								foreach (string sValue in sFormatValue)
								{
									string sValueNormalized = sValue.Replace(" ", "_");
									logger.Debug("sValueNormalized= " + sValueNormalized);
									string sCheckCodiceUnivoco = sCodiceUnivoco;
									logger.Debug("sCheckCodiceUnivoco= " + sCheckCodiceUnivoco);
									logger.Debug("sCodiceUnivoco= " + sCodiceUnivoco);
									logger.Debug("lfieldXls[sValueNormalized]= " + lfieldXls[sValueNormalized]);
									logger.Debug("lfieldXls count= " + lfieldXls.Count);
									sCodiceUnivoco = sCodiceUnivoco.Replace(sValueNormalized, lfieldXls[sValueNormalized].Trim());
									if (sCodiceUnivoco == sCheckCodiceUnivoco)
										throw new ArgumentException("Non è stato possibile generare il codice univoco, il valore non identificato è: " + sValueNormalized + " i nomi dei campi nel foglio Excel attesi sono i seguenti: " + resourceFileManager.getConfigData("IscFormatCodiceUnivoco").Replace('|', ' '));
								}
								oMetadati[int.Parse(resourceFileManager.getConfigData("IscIndCodiceUnivoco"))] = sCodiceUnivoco;

								List<Office> oOffices = new List<Office>();
								logger.Debug("Metadati caricati");
								if (!string.IsNullOrEmpty(schedeComMax.GetKey(i).ToString()))
								{
									// Ricerca all'interno della rubrica generica
									// Gestione rubrica Persona fisica o giuridica
									//
									List<GenericEntity> anagUsersRubricaGen;
									if (IscMassiveCardIndexes[resourceFileManager.getConfigData("IscDescObjForAttach")] == resourceFileManager.getConfigData("IssValueObjForAttach1"))
									{
										anagUsersRubricaGen = wcfSiavAgrafManager.GetUsers(schedeComMax.GetKey(i).Trim(), resourceFileManager.getConfigData("NomeRubricaCompleta"), resourceFileManager.getConfigData("NomeCompanyRubricaSearch"));

									}
									else
									{
										anagUsersRubricaGen = wcfSiavAgrafManager.GetUsers(schedeComMax.GetKey(i).Trim(), resourceFileManager.getConfigData("NomeRubricaCompleta"), resourceFileManager.getConfigData("NomePersonRubricaSearch"));
									}
									if (anagUsersRubricaGen.Count > 1)
									{
										// caso errore trovati più utenti con lo stesso codice fiscale
										sNote = "Sono stati trovati n°:" + anagUsersRubricaGen.Count + " record per il codice OCF:" + schedeComMax.GetKey(i) + ", verificare l'anagrafica " + resourceFileManager.getConfigData("NomeRubricaCompleta");
										logger.Debug(sNote);
										sStato = resourceFileManager.getConfigData("IscStatoSingleCom");
										isInError = true;
									}
									else if (anagUsersRubricaGen.Count == 1)
									{
										logger.Debug("Associazione anagrafica con rubrica generica avvenuta con successo");

										// caso corretto -- implementazione algoritmo Associazione con Anagrafica
										AgrafCardContact svCardContactRg = new AgrafCardContact();
										svCardContactRg.EntityId = new AgrafCardContactId();
										svCardContactRg.EntityId.ContactId = new AgrafEntityId();
										svCardContactRg.EntityId.ContactId.EntityId = anagUsersRubricaGen[0].EntityId.Id;
										svCardContactRg.EntityId.ContactId.Version = (int)anagUsersRubricaGen[0].EntityId.Version;
										svAgrafCardRub.CardContacts.Add(svCardContactRg);
										// Destinatario
										oMetadati[int.Parse(resourceFileManager.getConfigData("IscIndDestinatario"))] = anagUsersRubricaGen[0].Name + " " + anagUsersRubricaGen[0].Person.LastName;
									}
									else
									{
										// caso in cui non è stato trovato l'utente per la scheda
										sNote = "Non è stato trovato alcun record per il codice OCF:" + schedeComMax.GetKey(i) + ", verificare l'anagrafica " + resourceFileManager.getConfigData("NomeRubricaCompleta");
										logger.Debug(sNote);
										sStato = resourceFileManager.getConfigData("IscStatoSingleCom");
										isInError = true;
									}
									// Ricerca all'interno della rubrica Aspiranti e promotori finanziari
									List<GenericEntity> anagUsersAspPF;
									if (IscMassiveCardIndexes[resourceFileManager.getConfigData("IscDescObjForAttach")] == resourceFileManager.getConfigData("IssValueObjForAttach1"))
									{
										anagUsersAspPF = wcfSiavAgrafManager.GetUsers(schedeComMax.GetKey(i).Trim(), resourceFileManager.getConfigData("NomeRubricaAspPf"), resourceFileManager.getConfigData("NomeCompanyRubricaSearch"));

									}
									else
									{
										anagUsersAspPF = wcfSiavAgrafManager.GetUsers(schedeComMax.GetKey(i).Trim(), resourceFileManager.getConfigData("NomeRubricaAspPf"), resourceFileManager.getConfigData("NomePersonRubricaSearch"));
									}
									if (anagUsersAspPF.Count > 1)
									{
										// caso errore trovati più utenti con lo stesso codice fiscale
										sNote = "Sono stati trovati n°:" + anagUsersAspPF.Count + " record per il codice OCF:" + schedeComMax.GetKey(i) + ", verificare l'anagrafica " + resourceFileManager.getConfigData("NomeRubricaAspPf");
										logger.Debug(sNote);
										sStato = resourceFileManager.getConfigData("IscStatoSingleCom");
										isInError = true;
									}
									else if (anagUsersAspPF.Count == 1)
									{
										logger.Debug("Associazione anagrafica con rubrica Promotori finanziari avvenuta con successo");

										// caso corretto -- implementazione algoritmo Associazione con Anagrafica
										AgrafCardContact svCardContactApf = new AgrafCardContact();
										svCardContactApf.EntityId = new AgrafCardContactId();
										svCardContactApf.EntityId.ContactId = new AgrafEntityId();
										svCardContactApf.EntityId.ContactId.EntityId = anagUsersAspPF[0].EntityId.Id;
										svCardContactApf.EntityId.ContactId.Version = (int)anagUsersAspPF[0].EntityId.Version;
										svAgrafCardApf.CardContacts.Add(svCardContactApf);
										// Codice Fiscale
										oMetadati[int.Parse(resourceFileManager.getConfigData("IscIndSchedCodFisc"))] = anagUsersRubricaGen[0].TaxID;
										// codice APF 
										oMetadati[int.Parse(resourceFileManager.getConfigData("IscIndSchedCodApf"))] = anagUsersRubricaGen[0].GenericEntityExternalId;
										// Codice Consob 
										oMetadati[int.Parse(resourceFileManager.getConfigData("IscIndSchedCodConsob"))] = anagUsersRubricaGen[0].GenericEntityExternalId2;
										// Classifica
										oMetadati[int.Parse(resourceFileManager.getConfigData("IscIndSchedClassifica"))] = anagUsersRubricaGen[0].GenericEntityExternalId3;
										// Partita IVA
										//oMetadati[int.Parse(resourceFileManager.getConfigData("IscIndSchedPIva"))] = anagUsersRubricaGen[0].VatID;

									}
									else
									{
										// caso in cui non è stato trovato l'utente per la scheda
										sNote = "Non è stato trovato alcun record per il codice OCF:" + schedeComMax.GetKey(i) + ", verificare l'anagrafica " + resourceFileManager.getConfigData("NomeRubricaAspPf");
										logger.Debug(sNote);
										sStato = resourceFileManager.getConfigData("IscStatoSingleCom");
										isInError = true;
									}
									// Codice OCF
									oMetadati[int.Parse(resourceFileManager.getConfigData("IscIndSchedCodApf"))] = schedeComMax.GetKey(i).Trim();

									// Stato
									oMetadati[int.Parse(resourceFileManager.getConfigData("IscIndSchedStato"))] = sStato;
									oFieldCard.MetaDati = new List<string>(oMetadati);
									logger.Debug("Avvio il processo di inseriemento");
									siavCardManager.Insert(sPathAttachmentsCard, schedeComMax.GetValues(i)[0], siavLogin, oFieldCard, oCardVisibility.Groups, oCardVisibility.Offices, oCardVisibility.Users, oDocumentType, oArchive, sNote, sMessage, svAgrafCardRub, svAgrafCardApf, out outCard);
									sResult.Add(outCard.ToString());
									logger.Debug("Scheda inserita, il suo id: " + outCard.ToString());
									logger.Debug("Fine lavorazione scheda " + i);
								}
								else
								{
									logger.Debug("Scheda NON inserita, il suo codice OCF è vuoto");
								}
							}
							// Fine ciclo For
							sOutput = "Sono state lavorate " + schedeComMax.Count + " schede. ";
							logger.Debug(sOutput);

						}
						//Codice Promotore	Cognome	Nome	Indirizzo	Località	Cap	Provincia	Nr. raccomandata	Data Spedizione	Data ricezione cartolina	Causale mancata ricezione	Nr. Protocollo	Data protocollo	Spese di notifica
					}
					else
					{// errore nessun modello trovato
						sLog = "Non è stato trovato alcun modello.";
						throw new ArgumentException(sLog);
					}
					if (isInError)
					{
						siavCardManager.FieldModify(oCardBundle, siavLogin, 22, "COMPLETATO CON ERRORI");
						sOutput += "Attività completata con errori.";
					}
					else
					{
						siavCardManager.FieldModify(oCardBundle, siavLogin, 22, "COMPLETATO");
						sOutput += "Attività completata.";
					}
					siavCardManager.InsertNote(oCardBundle, siavLogin, sOutput + " Per maggiori informazioni consultare il log per l'id transazione: " + this.IdSessioneTransazione, resourceFileManager.getConfigData("AutorAnnotation"));
					sLog = sOutput;
					siavLogin.Logout();
					siavLogin.siavWsLogin.Close();
					siavLogin = null;
					siavCardManager.siavWsCard.Close();
					siavCardManager = null;
					wcfSiavAgrafManager.siavWsAgraf.Close();
					wcfSiavAgrafManager = null;
					//}
					//else
					//    {
					//        sLog = "Non è stato trovato alcun campo identificativo all'interno del file Excel.";
					//        throw new ArgumentException(sLog);
					//    }
				}
				else
				{
					sLog = "Non è stato trovato alcun record all'interno del file Excel.";
					throw new ArgumentException(sLog);
				}
			}
			catch (Exception ex)
			{
				sErrorObj = "ERRORE Processo ISCRIZIONE. ";
				// sOutput è formato da IDSessioneTransazione|Descrizione anomalia;
				sOutput = this.IdSessioneTransazione + "|" + ex.Message;
				try
				{
					logger.Error(ex.ToString());
					if (siavCardManager != null)
					{
						siavCardManager.InsertNote(oCardBundle, siavLogin, ex.Message + " Per maggiori informazioni consultare il log per l'id transazione: " + this.IdSessioneTransazione, resourceFileManager.getConfigData("AutorAnnotation"));
						siavCardManager.siavWsCard.Abort();
					}
					if (siavLogin != null)
					{
						siavLogin.Logout();
						siavLogin.siavWsLogin.Close();
					}
					if (wcfSiavAgrafManager != null)
					{
						wcfSiavAgrafManager.siavWsAgraf.Abort();
					}
				}
				catch (Exception e) { }
			}
			finally
			{
				logger.Debug("Fine elaborazione");
				if (resourceFileManager.getConfigData("isTest") != "SI")
				{
					SendMail sendMail = new SendMail();
					string sReceiverEmail = resourceFileManager.getConfigData("emailDestinatario");
					string sSenderNameEmail = resourceFileManager.getConfigData("emailNomeMittente");
					string sObject = sErrorObj + "Comunicazione Massiva " + sObjForObj + " : Inserimento schede in predisposizione " + sSezTer;
					if (sLog == "") sLog = "Problema con l'esecuzione dell'algoritmo di sistema, prego contattare il supporto tecnico.";
					string sbodyMsg = sLog + " Comunicazione Massiva: " + sIdComMaxivaForObj;
					sendMail.SENDMAIL(sReceiverEmail, sSenderNameEmail, sObject, sbodyMsg, "");
				}    //siavLogin.Logout();
			}
			return sResult.Select(s => (object)s).ToArray();
		}

		System.Reflection.Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
		{
			throw new NotImplementedException();
		}
		public bool IsValid(string emailaddress)
		{
			try
			{
				MailAddress m = new MailAddress(emailaddress);

				return true;
			}
			catch (FormatException)
			{
				return false;
			}
		}
		public bool SendEmailFromCard(string sMitt, string sDest, string sObject, string sMessage, string sBcc)
		{
			bool bResult = true;
			try
			{
				logger.Debug("Mittente: " + sMitt);
				logger.Debug("Destinatario: " + sDest);
				logger.Debug("Oggetto: " + sObject);
				logger.Debug("Corpo del messaggio: " + sMessage);
				logger.Debug("BCC: " + sBcc);

				// Verifica dati mittente
				string sSender = "";
				if (string.IsNullOrEmpty(sMitt))
					throw new ArgumentNullException("Manca il parametro Mittente");
				// verifico se è un indirizzo email
				if (IsValid(sMitt))
				{
					logger.Debug("Mittente validato: " + sMitt);
					sSender = sMitt;
				}
				else
				{
					// Non è un indirizzo email allora provo a identificare il valore attraverso una naming convention della chiave sul file di risorse:
					string sKeyToFind = "EmailAddress_" + RemoveSpecialCharHelper.RemoveSpecialCharacters(sMitt);
					logger.Debug("Mittente - Cerco nel file di risorse la chiave: " + sKeyToFind);
					sSender = resourceFileManager.getConfigData(sKeyToFind);
					logger.Debug("Leggo il valore: " + sSender);

				}
				// Verifico se è stato individuato il mittente
				if (string.IsNullOrEmpty(sSender))
					throw new ArgumentNullException("Non è stato trovato nulla per il mittente: " + sMitt);

				// Verifica dati destinatari
				string sRecipient = "";

				if (string.IsNullOrEmpty(sDest))
					throw new ArgumentNullException("Manca il parametro Destinatario");
				if (sDest.IndexOf(";") == -1)
				{
					sDest += ";";
				}
				var iteratorDestlist = sDest.Split(';').Distinct(StringComparer.InvariantCultureIgnoreCase).ToList();
				foreach (var sValue in iteratorDestlist)
				{
					if (!string.IsNullOrEmpty(sValue))
					{
						string sResourceValue = "";
						if (IsValid(sValue))
						{
							logger.Debug("Destinatario validato: " + sValue);
							sRecipient += sValue + ";";
						}
						else
						{
							// Non è un indirizzo email allora provo a identificare il valore attraverso una naming convention della chiave sul file di risorse:
							string sKeyToFind = "EmailAddress_" + RemoveSpecialCharHelper.RemoveSpecialCharacters(sValue);
							logger.Debug("Destinatario - Cerco nel file di risorse la chiave: " + sKeyToFind);
							sResourceValue = resourceFileManager.getConfigData(sKeyToFind);
							logger.Debug("Leggo il valore: " + sResourceValue);
							sRecipient += sResourceValue;
						}
						// Verifico se è stato individuato il destinatario
						if (string.IsNullOrEmpty(sResourceValue))
							throw new ArgumentNullException("Non è stato trovato nulla per il destinatario: " + sValue);
					}
				}

				// Verifica dati BCC
				string sBccRecipient = "";

				if (!string.IsNullOrEmpty(sBcc))
				{
					if (sBcc.IndexOf(";") == -1)
					{
						sBcc += ";";
					}
					var iteratorBcclist = sDest.Split(';').Distinct(StringComparer.InvariantCultureIgnoreCase).ToList();
					foreach (var sValue in iteratorBcclist)
					{
						if (!string.IsNullOrEmpty(sValue))
						{
							string sResourceValue = "";
							if (IsValid(sValue))
							{
								logger.Debug("Bcc validato: " + sValue);
								sBccRecipient += sValue + ";";
							}
							else
							{
								// Non è un indirizzo email allora provo a identificare il valore attraverso una naming convention della chiave sul file di risorse:
								string sKeyToFind = "EmailAddress_" + RemoveSpecialCharHelper.RemoveSpecialCharacters(sValue);
								logger.Debug("Bcc - Cerco nel file di risorse la chiave: " + sKeyToFind);
								sResourceValue = resourceFileManager.getConfigData(sKeyToFind);
								logger.Debug("Leggo il valore: " + sResourceValue);
								sBccRecipient += sResourceValue;
							}
							// Verifico se è stato individuato il destinatario
							if (string.IsNullOrEmpty(sResourceValue))
								throw new ArgumentNullException("Non è stato trovato nulla per il destinatario: " + sValue);
						}
					}
				}
				if (string.IsNullOrEmpty(sObject))
					throw new ArgumentNullException("Manca il parametro Oggetto");
				if (string.IsNullOrEmpty(sMessage))
					throw new ArgumentNullException("Manca il parametro Messaggio");
				SendMail sendMail = new SendMail();
				sendMail.SENDMAIL2(sRecipient, sSender, sObject, sMessage, sBccRecipient);
			}
			catch (Exception ex)
			{
				try
				{
					logger.Error(ex.ToString());
					bResult = false;
				}
				catch (Exception e) { }
			}
			finally
			{
				logger.Debug("Fine elaborazione");
				//siavLogin.Logout();
			}
			return bResult;
		}
		public bool SendEmailFromFlux(string sObject, string sMessage)
		{
			bool bResult = true;
			try
			{
				SendMail sendMail = new SendMail();
				string sReceiverEmail = resourceFileManager.getConfigData("emailDestinatario");
				string sSenderNameEmail = resourceFileManager.getConfigData("emailNomeMittente");
				sendMail.SENDMAIL(sReceiverEmail, sSenderNameEmail, sObject, sMessage, "");
			}
			catch (Exception ex)
			{
				try
				{
					logger.Error(ex.ToString());
					bResult = false;
				}
				catch (Exception e) { }
			}
			finally
			{
				logger.Debug("Fine elaborazione");
				//siavLogin.Logout();
			}
			return bResult;
		}
		public object[] Step1Prova_Valutativa(string GUIdcard, out string sOutput)
		{
			List<String> sResult = new List<string>();
			sOutput = "";
			bool isInError = false;
			WcfSiavLoginManager siavLogin = null;
			WcfSiavAgrafManager wcfSiavAgrafManager = null;
			WcfSiavCardManager siavCardManager = null;
			CardBundle oCardBundle = null;
			String sIdComMaxivaForObj = "";
			String sObjForObj = "";
			string sLog = string.Empty;
			string sSezTer = "";
			string sErrorObj = "";
			try
			{
				siavLogin = new WcfSiavLoginManager();
				wcfSiavAgrafManager = new WcfSiavAgrafManager(logger);
				FluxHelper fluxHelper = new FluxHelper();
				string oUserName = resourceFileManager.getConfigData("UserFlux");
				string oUserPwd = oRijndael.Decrypt(resourceFileManager.getConfigData("PasswordFlux"));
				string sModelDocX = "";

				MainDoc oMainDoc;
				NameValueCollection IscMassiveCardIndexes;
				// Esegue il login
				siavLogin.Login(oUserName, oUserPwd);
				logger.Debug("Sessione login Archiflow: " + siavLogin.oSessionInfo.SessionId);

				siavCardManager = new WcfSiavCardManager(logger);
				// Recupera la scheda ove è stato avviato il processo
				siavCardManager.GetCard(GUIdcard, siavLogin, out oCardBundle);

				// Recupera il documento principale della scheda 
				siavCardManager.GetMainDoc(oCardBundle, out oMainDoc);
				// Recupero gli indici della scheda 
				siavCardManager.GetIndexes(oCardBundle, out IscMassiveCardIndexes);
				logger.Debug("Recuperati i dati dalla scheda che ha avviato il processo : " + GUIdcard);

				// Materializza il file sul filesystem
				string sAnagraphicXlsSource = this.sWorkingFolder + @"\" + oMainDoc.Filename + "_AnagSource." + oMainDoc.Extension;
				logger.Debug("Scrivo il documento principale: " + sAnagraphicXlsSource);
				fluxHelper.FileMaterialize(sAnagraphicXlsSource, oMainDoc.oByte);

				// Estraggo i dati dal file Excel                
				ExcelDocumentReader excelDocumentReader = new ExcelDocumentReader(sAnagraphicXlsSource, sWorkingFolder);
				logger.Debug("Lettura del file excel: " + sAnagraphicXlsSource);

				// Verifica ricerca presenza record
				List<string> lCodiceFiscale = new List<string>();
				List<string> lSede = new List<string>();
				List<string> lData = new List<string>();
				logger.Debug("Sono stati letti " + excelDocumentReader.getData.Count() + " di record");

				if (excelDocumentReader.getData.Count() > 0)
				{
					// Lista di codici fiscali da verificare
					//logger.Debug("Estrapolo i primi 10 record da verificare.");
					//string sColumnIdSubject = resourceFileManager.getConfigData("PvalSearchValueUniqueFromAnag");

					//fluxHelper.GetSampleRecords(excelDocumentReader, sColumnIdSubject, out lCodiceFiscale);
					//fluxHelper.GetSampleRecords(excelDocumentReader, resourceFileManager.getConfigData("PvalExcelNameSede"), out lSede);
					//fluxHelper.GetSampleRecords(excelDocumentReader, resourceFileManager.getConfigData("PvalExcelNameDataEsame"), out lData);

					//if (lCodiceFiscale.Count > 0)
					//{
					List<Guid> lGUID = new List<Guid>();
					//                            foreach (var singleCf in lCodiceFiscale)
					//for (int indexRow = 0; indexRow < lCodiceFiscale.Count; indexRow++)
					//    {
					//        List<SchedaSearchParameter> lSchedaSearchParameter = new List<SchedaSearchParameter>();

					//        List<string> lMetadati;
					//        SchedaSearchParameter schedaSearchParameter = new SchedaSearchParameter();
					//        schedaSearchParameter.Archivio = resourceFileManager.getConfigData("IscArchiveSearchUnique");
					//        schedaSearchParameter.TipologiaDocumentale = resourceFileManager.getConfigData("PvalTypeDocSearchUnique");
					//        schedaSearchParameter.Oggetto = resourceFileManager.getConfigData("PvalObjSearchUnique");
					//        string[] arr = new string[24];
					//        arr[int.Parse(resourceFileManager.getConfigData("IscIndSchedSearchUnique"))] = lCodiceFiscale[indexRow];
					//        // Sede
					//        arr[int.Parse(resourceFileManager.getConfigData("PvalIndSchedSearchUniqueSede"))] = lSede[indexRow];
					//        // Data Esame
					//        arr[int.Parse(resourceFileManager.getConfigData("PvalIndSchedSearchUniqueDataEsame"))] = lData[indexRow];

					//        logger.Debug("Inizio ricerca record");
					//        logger.Debug("  Cerco per Archivio: " + resourceFileManager.getConfigData("IscArchiveSearchUnique"));
					//        logger.Debug("  Cerco per TipologiaDocumentale: " + resourceFileManager.getConfigData("PvalTypeDocSearchUnique"));
					//        logger.Debug("  Cerco per Oggetto: " + resourceFileManager.getConfigData("PvalObjSearchUnique"));
					//        logger.Debug("  Cerco per Sede: " + lSede[indexRow]);
					//        logger.Debug("  Cerco per Data Esame: " + lData[indexRow]);
					//        logger.Debug("  Cerco per Codice OCF: " + lCodiceFiscale[indexRow]);
					//        logger.Debug("Fine ricerca record");

					//        lMetadati = new List<string>(arr);
					//        schedaSearchParameter.MetaDati = lMetadati;
					//        lSchedaSearchParameter.Add(schedaSearchParameter);
					//        siavCardManager.getSearch(lSchedaSearchParameter, siavLogin, out lGUID);
					//        if (lGUID.Count > 0)
					//        {
					//            sLog = "Sono state trovate schede già inserite a sistema";
					//            // notificare problema di occorrenza trovata
					//            throw new ArgumentException(sLog);
					//        }
					//    }
					logger.Debug("Inizio Ricerca documento modelli ");

					List<SchedaSearchParameter> lSchedaModSearchParameter = new List<SchedaSearchParameter>();
					List<string> lMetadatiModelli;
					// ricerco tra le tipologie Modelli, il documento necessario alla creazione della comunicazione
					SchedaSearchParameter schedaModSearchParameter = new SchedaSearchParameter();
					schedaModSearchParameter.Archivio = resourceFileManager.getConfigData("ArchiveTypeDocModelli");
					schedaModSearchParameter.TipologiaDocumentale = resourceFileManager.getConfigData("TypeDocModelli");
					string[] arrModelli = new string[20];
					arrModelli[int.Parse(resourceFileManager.getConfigData("ModIndSearchTemplateName"))] = IscMassiveCardIndexes[resourceFileManager.getConfigData("IscNameSchedaTemplateName")];
					arrModelli[int.Parse(resourceFileManager.getConfigData("ModIndSearchTemplateVer"))] = IscMassiveCardIndexes[resourceFileManager.getConfigData("IscNameSchedaTemplateVer")];
					arrModelli[int.Parse(resourceFileManager.getConfigData("ModIndSearchTemplateTypeProcess"))] = resourceFileManager.getConfigData("PvalTypeProcess");

					if (IscMassiveCardIndexes[resourceFileManager.getConfigData("IscNameUffMitt")].ToUpper().IndexOf("UACF MI") != -1)
						sSezTer = resourceFileManager.getConfigData("SezTerII");
					else
						sSezTer = resourceFileManager.getConfigData("SezTerI");

					arrModelli[int.Parse(resourceFileManager.getConfigData("ModIndSearchSezTer"))] = sSezTer;
					logger.Debug("  Cerco per Archivio: " + resourceFileManager.getConfigData("ArchiveTypeDocModelli"));
					logger.Debug("  Cerco per TipologiaDocumentale: " + resourceFileManager.getConfigData("TypeDocModelli"));
					logger.Debug("  Cerco per Nome Modello: " + IscMassiveCardIndexes[resourceFileManager.getConfigData("IscNameSchedaTemplateName")]);
					logger.Debug("  Cerco per Versione Modello: " + IscMassiveCardIndexes[resourceFileManager.getConfigData("IscNameSchedaTemplateVer")]);
					logger.Debug("  Cerco per Tipo Processo: " + resourceFileManager.getConfigData("PvalTypeProcess"));
					logger.Debug("  Cerco per UACF: " + sSezTer);
					logger.Debug("Fine ricerca record");

					lMetadatiModelli = new List<string>(arrModelli);
					schedaModSearchParameter.MetaDati = lMetadatiModelli;
					lSchedaModSearchParameter.Add(schedaModSearchParameter);
					siavCardManager.getSearch(lSchedaModSearchParameter, siavLogin, out lGUID);
					CardVisibility oCardVisibility = new CardVisibility();
					siavCardManager.GetCardVisibility(GUIdcard, siavLogin, out oCardVisibility);
					logger.Debug("Sono stati trovati numero: " + lGUID.Count + " di documenti tipo modelli");
					if (lGUID.Count > 0)
					{
						foreach (var singleGuid in lGUID)
						{
							CardBundle oCardModelBundle;
							MainDoc oMainDocModel;
							// Recupera la scheda ove è stato avviato il processo
							siavCardManager.GetCard(singleGuid.ToString(), siavLogin, out oCardModelBundle);
							// Recupera il documento principale della scheda 
							logger.Debug("Recupero il documento principale della scheda");
							siavCardManager.GetMainDoc(oCardModelBundle, out oMainDocModel);
							// Materializza il file sul filesystem
							logger.Debug("Scrivo il modello in: " + sModelDocX);
							sModelDocX = sWorkingFolder + @"\" + oMainDocModel.Filename + "_ModelSource." + oMainDocModel.Extension;
							fluxHelper.FileMaterialize(sModelDocX, oMainDocModel.oByte);
							string sError = "";

							logger.Debug("Gestione dati aggiuntivi");
							NameValueCollection addStringForReplace = new NameValueCollection();
							addStringForReplace.Add("DataSys", DateTime.Now.ToString("dd/MM/yyyy"));
							addStringForReplace.Add("Data Delibera_Scheda", IscMassiveCardIndexes[resourceFileManager.getConfigData("PvalNameFieldDataDel")]);
							addStringForReplace.Add("N Delibera_Scheda", IscMassiveCardIndexes[resourceFileManager.getConfigData("PvalNameFieldNumDel")]);
							logger.Debug("Carico i dati dal file excel");
							// Crea tutti i documenti principali delle singole comunicazioni

							wcfSiavAgrafManager.LoadRubriche();

							logger.Debug("Creo i documenti massivi per ogni singola comunicazione da creare");
							NameValueCollection schedeComMax = fluxHelper.CreateDocxMassive(excelDocumentReader, addStringForReplace, sModelDocX, "PvalSearchValueUniqueFromAnag", out sError);
							Guid outCard = new Guid();
							Archive oArchive;
							DocumentType oDocumentType;
							siavCardManager.getArchiveDoc(resourceFileManager.getConfigData("IscArchiveSearchUnique").ToString(), siavLogin.oSessionInfo, out oArchive);
							siavCardManager.getTypeDoc(resourceFileManager.getConfigData("PvalTypeDocSearchUnique").ToString(), siavLogin.oSessionInfo, out oDocumentType);
							logger.Debug("Recuper l'oggetto Archivio: " + resourceFileManager.getConfigData("IscArchiveSearchUnique").ToString() + " e tipologia documentale" + resourceFileManager.getConfigData("PvalTypeDocSearchUnique").ToString());
							logger.Debug("Avvio la creazione delle singole schede documentali, in tutto: " + schedeComMax.Count);

							//siavCardManager.getTypeDoc();
							for (int i = 0; i < schedeComMax.Count; i++)
							{
								NameValueCollection lfieldXls = new NameValueCollection();
								logger.Debug("Inizio lavorazione scheda " + i);

								AgrafCard svAgrafCardRub = new AgrafCard();
								svAgrafCardRub.CardContacts = new List<AgrafCardContact>();
								svAgrafCardRub.Tag = sIdIndexTagRubricaGen;
								AgrafCard svAgrafCardApf = new AgrafCard();
								svAgrafCardApf.CardContacts = new List<AgrafCardContact>();
								svAgrafCardApf.Tag = sIdIndexTagRubricaApf;
								logger.Debug("Dati rubrica caricati");

								Model.Siav.APFlibrary.Model.FieldsCard oFieldCard = new Model.Siav.APFlibrary.Model.FieldsCard();
								oFieldCard.Oggetto = IscMassiveCardIndexes.GetValues(resourceFileManager.getConfigData("IscMassiveObjName"))[0];
								sObjForObj = oFieldCard.Oggetto;
								string[] oMetadati = new string[20];
								string sNote = "";
								string sMessage = "";
								string sStato = "";

								// Id di riferimento comunicazione massiva
								oMetadati[int.Parse(resourceFileManager.getConfigData("PvalIndSchedIdComMax"))] = IscMassiveCardIndexes.GetValues(resourceFileManager.getConfigData("lscMassiveId"))[0];
								sIdComMaxivaForObj = oMetadati[int.Parse(resourceFileManager.getConfigData("PvalIndSchedIdComMax"))];
								// Sez terr di riferimento
								oMetadati[int.Parse(resourceFileManager.getConfigData("PvalNameSchedSezTer"))] = sSezTer;
								// Ufficio Mittente
								oMetadati[int.Parse(resourceFileManager.getConfigData("IscIndSchedUffMitt"))] = IscMassiveCardIndexes[resourceFileManager.getConfigData("IscNameUffMitt")];
								// Mezzo di trasmissione
								oMetadati[int.Parse(resourceFileManager.getConfigData("PvalIndSchedMezTrasm"))] = IscMassiveCardIndexes[resourceFileManager.getConfigData("PvalNameMezTrasm")];
								// AnagraficaXls
								string sValueXls = "";
								string sColumnNameAnagraf = resourceFileManager.getConfigData("PvalSearchValueUniqueFromAnag");
								fluxHelper.GetCsvRecordKV(excelDocumentReader, sColumnNameAnagraf, schedeComMax.GetKey(i).Trim(), out lfieldXls);
								fluxHelper.GetCsvRecord(excelDocumentReader, sColumnNameAnagraf, schedeComMax.GetKey(i).Trim(), out sValueXls);
								List<string> sDataAnagrafica;
								sDataAnagrafica = sValueXls.Split('|').ToList();
								Dictionary<string, string> dictionary = new Dictionary<string, string>();
								for (int iIndAnagField = 0; iIndAnagField < sDataAnagrafica.Count - 1; iIndAnagField = iIndAnagField + 2)
								{
									dictionary.Add(sDataAnagrafica[iIndAnagField].ToUpper(), sDataAnagrafica[iIndAnagField + 1]);
								}
								// Sede esame
								if (dictionary.ContainsKey("SEDE".ToUpper()))
								{
									string value = dictionary["SEDE"];
									oMetadati[int.Parse(resourceFileManager.getConfigData("PvalIndSchedSedEsam"))] = value;
								}
								// Data esame
								if (dictionary.ContainsKey("GIORNO"))
								{
									string value = dictionary["GIORNO"];
									oMetadati[int.Parse(resourceFileManager.getConfigData("PvalIndSchedDatEsam"))] = value;
								}
								// Tornata esame
								if (dictionary.ContainsKey("TORNATA"))
								{
									string value = dictionary["TORNATA"];
									oMetadati[int.Parse(resourceFileManager.getConfigData("PvalIndSchedTornEsam"))] = value;
								}
								// Codice tornata
								if (dictionary.ContainsKey("CODICE TORNATA"))
								{
									string value = dictionary["CODICE TORNATA"];
									oMetadati[int.Parse(resourceFileManager.getConfigData("PvalIndSchedCodTorn"))] = value;
								}
								/*
								  Implementazione per la gestione del campo  Mezzo di spedizione

								Il cmapo report anagrafico è così composto:
								COGNOME|MOXXXTTI|NOME|PXXXXXO|CODICE_FISCALE|xxxxx2232y3sywywsw|INDIRIZZO|VIA DI SANTA CROCE IN GERUSALEMME 91|CAP|00185|LOCALITA|ROMA|COMUNE|ROMA|PROVINCIA|RM|NUM_DELIBERA|993|DATA_DELIBERA|10/01/2019|CODICE CONSOB|90270|TIPO ISCRIZIONE||CODICE OCF|03E4A03A-8F14-4897-BE99-14B94B661A0A|NAZIONE INDIRIZZO||AREA TARIFFARIA|AM|

								*/
								string sMezzoDiTrasmissione = "";
								if (IscMassiveCardIndexes[resourceFileManager.getConfigData("PvalNameMezTrasm").ToUpper()] == resourceFileManager.getConfigData("sCheckMezDiTrasmPEC").ToUpper())
								{
									sMezzoDiTrasmissione = resourceFileManager.getConfigData("sCheckMezDiTrasmPEC").ToUpper();
								}
								else if (IscMassiveCardIndexes[resourceFileManager.getConfigData("PvalNameMezTrasm").ToUpper()] == resourceFileManager.getConfigData("sCheckMezDiTrasmRacAR").ToUpper())
								{
									sMezzoDiTrasmissione = resourceFileManager.getConfigData("sMezDiTrasmRacAR").ToUpper();
								}
								else
								{
									sMezzoDiTrasmissione = IscMassiveCardIndexes[resourceFileManager.getConfigData("PvalNameMezTrasm").ToUpper()];
								}
								sValueXls += "Mezzo di trasmissione|" + sMezzoDiTrasmissione + "|";
								oMetadati[int.Parse(resourceFileManager.getConfigData("PvalIndSchedAnagXls"))] = sValueXls;

								// Generazione Codice univoco
								string sCodiceUnivoco = resourceFileManager.getConfigData("PvalFormatCodiceUnivoco");
								string[] sFormatValue = sCodiceUnivoco.Split('|');
								foreach (string sValue in sFormatValue)
								{
									string sValueNormalized = sValue.Replace(" ", "_");
									string sCheckCodiceUnivoco = sCodiceUnivoco;
									// Inserire modifica dell'eliminazione del carattere : solo nel caso in cui si elabori i dati dell'ora
									if (sValueNormalized.ToUpper() == "ORA")
									{
										sCodiceUnivoco = sCodiceUnivoco.Replace(sValueNormalized, lfieldXls[sValueNormalized].Trim().Replace(":", ""));
									}
									else
									{
										sCodiceUnivoco = sCodiceUnivoco.Replace(sValueNormalized, lfieldXls[sValueNormalized].Trim());
									}

									if (sCodiceUnivoco == sCheckCodiceUnivoco)
										throw new ArgumentException("Non è stato possibile generare il codice univoco, il valore non identificato è: " + sValueNormalized + " i nomi dei campi nel foglio Excel attesi sono i seguenti: " + resourceFileManager.getConfigData("PvalFormatCodiceUnivoco").Replace('|', ' '));
								}
								oMetadati[int.Parse(resourceFileManager.getConfigData("PvalIndCodiceUnivoco"))] = sCodiceUnivoco;
								// Sede esame
								//oMetadati[int.Parse(resourceFileManager.getConfigData("PvalIndSchedSedEsam"))] = IscMassiveCardIndexes[resourceFileManager.getConfigData("PvalNameSchedSedEsam")];
								// Data esame
								//oMetadati[int.Parse(resourceFileManager.getConfigData("PvalIndSchedDatEsam"))] = IscMassiveCardIndexes[resourceFileManager.getConfigData("PvalNameSchedDatEsam")];
								// Tornata esame
								//oMetadati[int.Parse(resourceFileManager.getConfigData("PvalIndSchedTornEsam"))] = IscMassiveCardIndexes[resourceFileManager.getConfigData("PvalNameSchedTornEsam")];
								// Codice tornata
								//oMetadati[int.Parse(resourceFileManager.getConfigData("PvalIndSchedCodTorn"))] = IscMassiveCardIndexes[resourceFileManager.getConfigData("PvalNameSchedCodTorn")];


								List<Office> oOffices = new List<Office>();
								logger.Debug("Metadati caricati");
								if (!string.IsNullOrEmpty(schedeComMax.GetKey(i).ToString()))
								{
									// Ricerca all'interno della rubrica generica
									List<GenericEntity> anagUsersRubricaGen = wcfSiavAgrafManager.GetUsers(schedeComMax.GetKey(i).Trim(), resourceFileManager.getConfigData("NomeRubricaCompleta"), resourceFileManager.getConfigData("NomePersonRubricaSearch"));
									if (anagUsersRubricaGen.Count > 1)
									{
										// caso errore trovati più utenti con lo stesso codice fiscale
										sNote = "Sono stati trovati n°:" + anagUsersRubricaGen.Count + " record per il codice OCF:" + schedeComMax.GetKey(i) + ", verificare l'anagrafica " + resourceFileManager.getConfigData("NomeRubricaCompleta");
										logger.Debug(sNote);
										sStato = resourceFileManager.getConfigData("IscStatoSingleCom");
										isInError = true;
									}
									else if (anagUsersRubricaGen.Count == 1)
									{
										logger.Debug("Associazione anagrafica con rubrica generica avvenuta con successo");

										// caso corretto -- implementazione algoritmo Associazione con Anagrafica
										AgrafCardContact svCardContactRg = new AgrafCardContact();
										svCardContactRg.EntityId = new AgrafCardContactId();
										svCardContactRg.EntityId.ContactId = new AgrafEntityId();
										svCardContactRg.EntityId.ContactId.EntityId = anagUsersRubricaGen[0].EntityId.Id;
										svCardContactRg.EntityId.ContactId.Version = (int)anagUsersRubricaGen[0].EntityId.Version;
										svAgrafCardRub.CardContacts.Add(svCardContactRg);
										// Destinatario
										oMetadati[int.Parse(resourceFileManager.getConfigData("IscIndDestinatario"))] = anagUsersRubricaGen[0].Name + " " + anagUsersRubricaGen[0].Person.LastName;
									}
									else
									{
										// caso in cui non è stato trovato l'utente per la scheda
										sNote = "Non è stato trovato alcun record per il codice OCF:" + schedeComMax.GetKey(i) + ", verificare l'anagrafica " + resourceFileManager.getConfigData("NomeRubricaCompleta");
										logger.Debug(sNote);
										sStato = resourceFileManager.getConfigData("IscStatoSingleCom");
										isInError = true;
									}
									// Ricerca all'interno della rubrica Aspiranti e promotori finanziari
									List<GenericEntity> anagUsersAspPF = wcfSiavAgrafManager.GetUsers(schedeComMax.GetKey(i).Trim(), resourceFileManager.getConfigData("NomeRubricaAspPf"), resourceFileManager.getConfigData("NomePersonRubricaSearch"));
									if (anagUsersAspPF.Count > 1)
									{
										// caso errore trovati più utenti con lo stesso codice fiscale
										sNote = "Sono stati trovati n°:" + anagUsersAspPF.Count + " record per il codice OCF:" + schedeComMax.GetKey(i) + ", verificare l'anagrafica " + resourceFileManager.getConfigData("NomeRubricaAspPf");
										logger.Debug(sNote);
										sStato = resourceFileManager.getConfigData("IscStatoSingleCom");
										isInError = true;
									}
									else if (anagUsersAspPF.Count == 1)
									{
										logger.Debug("Associazione anagrafica con rubrica Promotori finanziari avvenuta con successo");

										// caso corretto -- implementazione algoritmo Associazione con Anagrafica
										AgrafCardContact svCardContactApf = new AgrafCardContact();
										svCardContactApf.EntityId = new AgrafCardContactId();
										svCardContactApf.EntityId.ContactId = new AgrafEntityId();
										svCardContactApf.EntityId.ContactId.EntityId = anagUsersAspPF[0].EntityId.Id;
										svCardContactApf.EntityId.ContactId.Version = (int)anagUsersAspPF[0].EntityId.Version;
										svAgrafCardApf.CardContacts.Add(svCardContactApf);
										// codice APF 
										oMetadati[int.Parse(resourceFileManager.getConfigData("IscIndSchedCodApf"))] = anagUsersRubricaGen[0].GenericEntityExternalId;
										// Codice Consob 
										//oMetadati[int.Parse(resourceFileManager.getConfigData("IscIndSchedCodConsob"))] = anagUsersRubricaGen[0].GenericEntityExternalId2;
										// Classifica
										oMetadati[int.Parse(resourceFileManager.getConfigData("IscIndSchedClassifica"))] = anagUsersRubricaGen[0].GenericEntityExternalId3;
										// Codice Fiscale
										oMetadati[int.Parse(resourceFileManager.getConfigData("IscIndSchedCodFisc"))] = anagUsersRubricaGen[0].TaxID;
									}
									else
									{
										// caso in cui non è stato trovato l'utente per la scheda
										sNote = "Non è stato trovato alcun record per il codice OCF:" + schedeComMax.GetKey(i) + ", verificare l'anagrafica " + resourceFileManager.getConfigData("NomeRubricaAspPf");
										logger.Debug(sNote);
										sStato = resourceFileManager.getConfigData("IscStatoSingleCom");
										isInError = true;
									}
									// codice APF 
									oMetadati[int.Parse(resourceFileManager.getConfigData("IscIndSchedCodApf"))] = schedeComMax.GetKey(i).Trim();

									// Stato
									oMetadati[int.Parse(resourceFileManager.getConfigData("IscIndSchedStato"))] = sStato;
									oFieldCard.MetaDati = new List<string>(oMetadati);
									logger.Debug("Avvio il processo di inseriemento");
									siavCardManager.Insert(null, schedeComMax.GetValues(i)[0], siavLogin, oFieldCard, oCardVisibility.Groups, oCardVisibility.Offices, oCardVisibility.Users, oDocumentType, oArchive, sNote, sMessage, svAgrafCardRub, svAgrafCardApf, out outCard);
									sResult.Add(outCard.ToString());

									logger.Debug("Scheda inserita, il suo id: " + outCard.ToString());
									logger.Debug("Fine lavorazione scheda " + i);
								}
								else
								{
									logger.Debug("Scheda NON inserita, il suo codice OCF è vuoto");
								}
							}
							sOutput = "Sono state lavorate " + schedeComMax.Count + " schede. ";
							logger.Debug(sOutput);
						}
						//Codice Promotore	Cognome	Nome	Indirizzo	Località	Cap	Provincia	Nr. raccomandata	Data Spedizione	Data ricezione cartolina	Causale mancata ricezione	Nr. Protocollo	Data protocollo	Spese di notifica
					}
					else
					{// errore nessun modello trovato
						sLog = "Non è stato trovato alcun modello.";
						throw new ArgumentException(sLog);
					}
					if (isInError)
					{
						siavCardManager.FieldModify(oCardBundle, siavLogin, 22, "COMPLETATO CON ERRORI");
						sOutput += "Attività completata con errori.";
					}
					else
					{
						siavCardManager.FieldModify(oCardBundle, siavLogin, 22, "COMPLETATO");
						sOutput += "Attività completata.";
					}
					siavCardManager.InsertNote(oCardBundle, siavLogin, sOutput + " Per maggiori informazioni consultare il log per l'id transazione: " + this.IdSessioneTransazione, resourceFileManager.getConfigData("AutorAnnotation"));
					sLog = sOutput;
					siavLogin.Logout();
					siavLogin.siavWsLogin.Close();
					siavLogin = null;
					siavCardManager.siavWsCard.Close();
					siavCardManager = null;
					wcfSiavAgrafManager.siavWsAgraf.Close();
					wcfSiavAgrafManager = null;
				}
				else
				{
					sLog = "Non è stato trovato alcun campo identificativo all'interno del file Excel.";
					throw new ArgumentException(sLog);
				}
				//}
				//else
				//{
				//    sLog = "Non è stato trovato alcun record all'interno del file Excel.";
				//    throw new ArgumentException(sLog);
				//}
			}
			catch (Exception ex)
			{
				sErrorObj = "ERRORE Processo PROVA VALUTATIVA. ";
				// sOutput è formato da IDSessioneTransazione|Descrizione anomalia;
				sOutput = this.IdSessioneTransazione + "|" + ex.Message;
				try
				{
					logger.Error(ex.ToString());
					if (siavCardManager != null)
					{
						siavCardManager.InsertNote(oCardBundle, siavLogin, ex.Message + " Per maggiori informazioni consultare il log per l'id transazione: " + this.IdSessioneTransazione, resourceFileManager.getConfigData("AutorAnnotation"));
						siavCardManager.siavWsCard.Abort();
					}
					if (siavLogin != null)
					{
						siavLogin.Logout();
						siavLogin.siavWsLogin.Close();
					}
					if (wcfSiavAgrafManager != null)
					{
						wcfSiavAgrafManager.siavWsAgraf.Abort();
					}
				}
				catch (Exception e) { }
			}
			finally
			{
				logger.Debug("Fine elaborazione");
				if (resourceFileManager.getConfigData("isTest") != "SI")
				{
					SendMail sendMail = new SendMail();
					string sReceiverEmail = resourceFileManager.getConfigData("emailDestinatario");
					string sSenderNameEmail = resourceFileManager.getConfigData("emailNomeMittente");
					string sObject = sErrorObj + "Comunicazione Massiva " + sObjForObj + " : Inserimento schede in predisposizione " + sSezTer;
					if (sLog == "") sLog = "Problema con l'esecuzione dell'algoritmo di sistema, prego contattare il supporto tecnico.";
					string sbodyMsg = sLog + " Comunicazione Massiva: " + sIdComMaxivaForObj;
					sendMail.SENDMAIL(sReceiverEmail, sSenderNameEmail, sObject, sbodyMsg, "");
					//siavLogin.Logout();
				}
			}
			return sResult.Select(s => (object)s).ToArray();
		}
		public object[] Step1Ingiunzione(string GUIdcard, out string sOutput)
		{
			List<String> sResult = new List<string>();
			sOutput = "";
			bool isInError = false;
			WcfSiavLoginManager siavLogin = null;
			WcfSiavAgrafManager wcfSiavAgrafManager = null;
			WcfSiavCardManager siavCardManager = null;
			CardBundle oCardBundle = null;
			String sIdComMaxivaForObj = "";
			String sObjForObj = "";
			string sLog = string.Empty;
			string sSezTer = "";
			string sErrorObj = "";
			try
			{
				siavLogin = new WcfSiavLoginManager();
				wcfSiavAgrafManager = new WcfSiavAgrafManager(logger);
				FluxHelper fluxHelper = new FluxHelper();
				string oUserName = resourceFileManager.getConfigData("UserFlux");
				string oUserPwd = oRijndael.Decrypt(resourceFileManager.getConfigData("PasswordFlux"));
				string sModelDocX = "";

				MainDoc oMainDoc;
				NameValueCollection IngMassiveCardIndexes;
				// Esegue il login
				siavLogin.Login(oUserName, oUserPwd);
				logger.Debug("Sessione login Archiflow: " + siavLogin.oSessionInfo.SessionId);

				siavCardManager = new WcfSiavCardManager(logger);
				// Recupera la scheda ove è stato avviato il processo
				siavCardManager.GetCard(GUIdcard, siavLogin, out oCardBundle);

				// Recupera il documento principale della scheda 
				siavCardManager.GetMainDoc(oCardBundle, out oMainDoc);
				// Recupero gli indici della scheda 
				siavCardManager.GetIndexes(oCardBundle, out IngMassiveCardIndexes);
				logger.Debug("Recuperati i dati dalla scheda che ha avviato il processo : " + GUIdcard);

				// Materializza il file sul filesystem
				string sAnagraphicXlsSource = this.sWorkingFolder + @"\" + oMainDoc.Filename + "_AnagSource." + oMainDoc.Extension;
				logger.Debug("Scrivo il documento principale: " + sAnagraphicXlsSource);
				fluxHelper.FileMaterialize(sAnagraphicXlsSource, oMainDoc.oByte);

				// Estraggo i dati dal file Excel                
				ExcelDocumentReader excelDocumentReader = new ExcelDocumentReader(sAnagraphicXlsSource, sWorkingFolder);
				logger.Debug("Lettura del file excel: " + sAnagraphicXlsSource);

				// Verifica ricerca presenza record
				List<string> lCodiceFiscale = new List<string>();
				logger.Debug("Sono stati letti " + excelDocumentReader.getData.Count() + " di record");

				if (excelDocumentReader.getData.Count() > 0)
				{
					// Lista di codici fiscali da verificare
					//logger.Debug("Estrapolo i primi 10 record da verificare.");
					//string sColumnIdSubject = resourceFileManager.getConfigData("PvalSearchValueUniqueFromAnag");

					//fluxHelper.GetSampleRecords(excelDocumentReader, sColumnIdSubject, out lCodiceFiscale);
					//if (lCodiceFiscale.Count > 0)
					//{
					List<Guid> lGUID = new List<Guid>();
					//foreach (var singleCf in lCodiceFiscale)
					//{
					//    List<SchedaSearchParameter> lSchedaSearchParameter = new List<SchedaSearchParameter>();

					//    List<string> lMetadati;
					//    SchedaSearchParameter schedaSearchParameter = new SchedaSearchParameter();
					//    schedaSearchParameter.Archivio = resourceFileManager.getConfigData("IscArchiveSearchUnique");
					//    schedaSearchParameter.TipologiaDocumentale = resourceFileManager.getConfigData("IngTypeDocSearchUnique");
					//    schedaSearchParameter.Oggetto = resourceFileManager.getConfigData("IngObjSearchUnique");
					//    string[] arr = new string[20];
					//    arr[int.Parse(resourceFileManager.getConfigData("IngIndSchedSearchUnique"))] = singleCf;
					//    logger.Debug("Inizio ricerca record");
					//    logger.Debug("  Cerco per Archivio: " + resourceFileManager.getConfigData("IscArchiveSearchUnique"));
					//    logger.Debug("  Cerco per TipologiaDocumentale: " + resourceFileManager.getConfigData("IngTypeDocSearchUnique"));
					//    logger.Debug("  Cerco per Oggetto: " + resourceFileManager.getConfigData("IngObjSearchUnique"));
					//    logger.Debug("  Cerco per Codice OCF: " + singleCf);
					//    logger.Debug("Fine ricerca record");

					//    lMetadati = new List<string>(arr);
					//    schedaSearchParameter.MetaDati = lMetadati;
					//    lSchedaSearchParameter.Add(schedaSearchParameter);
					//    siavCardManager.getSearch(lSchedaSearchParameter, siavLogin, out lGUID);
					//    if (lGUID.Count > 0)
					//    {
					//        sLog = "Sono state trovate schede già inserite a sistema";
					//        // notificare problema di occorrenza trovata
					//        throw new ArgumentException(sLog);
					//    }
					//}
					logger.Debug("Inizio Ricerca documento modelli ");

					List<SchedaSearchParameter> lSchedaModSearchParameter = new List<SchedaSearchParameter>();
					List<string> lMetadatiModelli;
					// ricerco tra le tipologie Modelli, il documento necessario alla creazione della comunicazione
					SchedaSearchParameter schedaModSearchParameter = new SchedaSearchParameter();
					schedaModSearchParameter.Archivio = resourceFileManager.getConfigData("ArchiveTypeDocModelli");
					schedaModSearchParameter.TipologiaDocumentale = resourceFileManager.getConfigData("TypeDocModelli");
					string[] arrModelli = new string[20];
					arrModelli[int.Parse(resourceFileManager.getConfigData("ModIndSearchTemplateName"))] = IngMassiveCardIndexes[resourceFileManager.getConfigData("IscNameSchedaTemplateName")];
					arrModelli[int.Parse(resourceFileManager.getConfigData("ModIndSearchTemplateVer"))] = IngMassiveCardIndexes[resourceFileManager.getConfigData("IscNameSchedaTemplateVer")];
					arrModelli[int.Parse(resourceFileManager.getConfigData("ModIndSearchTemplateTypeProcess"))] = resourceFileManager.getConfigData("IngTypeProcess");

					sSezTer = resourceFileManager.getConfigData("IngSezTer");

					arrModelli[int.Parse(resourceFileManager.getConfigData("ModIndSearchSezTer"))] = sSezTer;
					logger.Debug("  Cerco per Archivio: " + resourceFileManager.getConfigData("ArchiveTypeDocModelli"));
					logger.Debug("  Cerco per TipologiaDocumentale: " + resourceFileManager.getConfigData("TypeDocModelli"));
					logger.Debug("  Cerco per Nome Modello: " + IngMassiveCardIndexes[resourceFileManager.getConfigData("IscNameSchedaTemplateName")]);
					logger.Debug("  Cerco per Versione Modello: " + IngMassiveCardIndexes[resourceFileManager.getConfigData("IscNameSchedaTemplateVer")]);
					logger.Debug("  Cerco per Tipo Processo: " + resourceFileManager.getConfigData("IngTypeProcess"));
					logger.Debug("  Cerco per Sezione Territoriale: " + sSezTer);
					logger.Debug("Fine ricerca record");

					lMetadatiModelli = new List<string>(arrModelli);
					schedaModSearchParameter.MetaDati = lMetadatiModelli;
					lSchedaModSearchParameter.Add(schedaModSearchParameter);
					siavCardManager.getSearch(lSchedaModSearchParameter, siavLogin, out lGUID);
					CardVisibility oCardVisibility = new CardVisibility();
					siavCardManager.GetCardVisibility(GUIdcard, siavLogin, out oCardVisibility);
					logger.Debug("Sono stati trovati numero: " + lGUID.Count + " di documenti tipo modelli");
					if (lGUID.Count > 0)
					{
						foreach (var singleGuid in lGUID)
						{
							CardBundle oCardModelBundle;
							MainDoc oMainDocModel;
							// Recupera la scheda ove è stato avviato il processo
							siavCardManager.GetCard(singleGuid.ToString(), siavLogin, out oCardModelBundle);
							// Recupera il documento principale della scheda 
							logger.Debug("Recupero il documento principale della scheda");
							siavCardManager.GetMainDoc(oCardModelBundle, out oMainDocModel);
							// Materializza il file sul filesystem
							logger.Debug("Scrivo il modello in: " + sModelDocX);
							sModelDocX = sWorkingFolder + @"\" + oMainDocModel.Filename + "_ModelSource." + oMainDocModel.Extension;
							fluxHelper.FileMaterialize(sModelDocX, oMainDocModel.oByte);
							string sError = "";

							logger.Debug("Gestione dati aggiuntivi");
							NameValueCollection addStringForReplace = new NameValueCollection();
							addStringForReplace.Add("DataSys", DateTime.Now.ToString("dd/MM/yyyy"));
							//addStringForReplace.Add("Num_Delibera Misura Contributi_Scheda", IngMassiveCardIndexes[resourceFileManager.getConfigData("IngReplaceFieldNumDelMSC")]);
							//addStringForReplace.Add("Num_Delibera Termini Contributi_Scheda", IngMassiveCardIndexes[resourceFileManager.getConfigData("IngReplaceFieldNumDelTSC")]);
							//addStringForReplace.Add("Data Delibera Termini_Scheda", IngMassiveCardIndexes[resourceFileManager.getConfigData("IngReplaceFieldDatDTS")]);
							// REPLACE N. Delibera Misura Contributi
							addStringForReplace.Add("Num_Delibera Misura Contributi_Scheda", IngMassiveCardIndexes[resourceFileManager.getConfigData("CancNumDelMisContr")]);
							// REPLACE N. Delibera Termini Contributi
							addStringForReplace.Add("Num_Delibera Termini Contributi_Scheda", IngMassiveCardIndexes[resourceFileManager.getConfigData("CancNumDelTerContr")]);
							// REPLACE Data Delibera Termini
							addStringForReplace.Add("Data Delibera Termini_Scheda", IngMassiveCardIndexes[resourceFileManager.getConfigData("CancDataDelTer")]);


							logger.Debug("Carico i dati dal file excel");
							// Crea tutti i documenti principali delle singole comunicazioni

							wcfSiavAgrafManager.LoadRubriche();

							logger.Debug("Creo i documenti massivi per ogni singola comunicazione da creare");
							NameValueCollection schedeComMax = fluxHelper.CreateDocxMassive(excelDocumentReader, addStringForReplace, sModelDocX, "IngSearchValueUniqueFromAnag", out sError);
							Guid outCard = new Guid();
							Archive oArchive;
							DocumentType oDocumentType;
							siavCardManager.getArchiveDoc(resourceFileManager.getConfigData("IscArchiveSearchUnique").ToString(), siavLogin.oSessionInfo, out oArchive);
							siavCardManager.getTypeDoc(resourceFileManager.getConfigData("IngTypeDocSearchUnique").ToString(), siavLogin.oSessionInfo, out oDocumentType);
							logger.Debug("Recuper l'oggetto Archivio: " + resourceFileManager.getConfigData("IscArchiveSearchUnique").ToString() + " e tipologia documentale" + resourceFileManager.getConfigData("IngTypeDocSearchUnique").ToString());
							logger.Debug("Avvio la creazione delle singole schede documentali, in tutto: " + schedeComMax.Count);

							//siavCardManager.getTypeDoc();
							for (int i = 0; i < schedeComMax.Count; i++)
							{
								NameValueCollection lfieldXls = new NameValueCollection();
								logger.Debug("Inizio lavorazione scheda " + i);

								AgrafCard svAgrafCardRub = new AgrafCard();
								svAgrafCardRub.CardContacts = new List<AgrafCardContact>();
								svAgrafCardRub.Tag = sIdIndexTagRubricaGen;
								AgrafCard svAgrafCardApf = new AgrafCard();
								svAgrafCardApf.CardContacts = new List<AgrafCardContact>();
								svAgrafCardApf.Tag = sIdIndexTagRubricaApf;
								logger.Debug("Dati rubrica caricati");

								Model.Siav.APFlibrary.Model.FieldsCard oFieldCard = new Model.Siav.APFlibrary.Model.FieldsCard();
								oFieldCard.Oggetto = IngMassiveCardIndexes.GetValues(resourceFileManager.getConfigData("IscMassiveObjName"))[0];
								sObjForObj = oFieldCard.Oggetto;
								string[] oMetadati = new string[20];
								string sNote = "";
								string sMessage = "";
								string sStato = "";

								// Id di riferimento comunicazione massiva
								oMetadati[int.Parse(resourceFileManager.getConfigData("IngIndSchedIdComMax"))] = IngMassiveCardIndexes.GetValues(resourceFileManager.getConfigData("lscMassiveId"))[0];
								sIdComMaxivaForObj = oMetadati[int.Parse(resourceFileManager.getConfigData("IngIndSchedIdComMax"))];
								// Sez terr di riferimento
								//(oMetadati[int.Parse(resourceFileManager.getConfigData("PvalNameSchedSezTer"))] = sSezTer;
								// Ufficio Mittente
								oMetadati[int.Parse(resourceFileManager.getConfigData("IscIndSchedUffMitt"))] = IngMassiveCardIndexes[resourceFileManager.getConfigData("IscNameUffMitt")];
								// Mezzo di trasmissione
								oMetadati[int.Parse(resourceFileManager.getConfigData("PvalIndSchedMezTrasm"))] = IngMassiveCardIndexes[resourceFileManager.getConfigData("PvalNameMezTrasm")];
								// AnagraficaXls
								string sValueXls = "";
								string sColumnNameAnagraf = resourceFileManager.getConfigData("IngSearchValueUniqueFromAnag");
								fluxHelper.GetCsvRecordKV(excelDocumentReader, sColumnNameAnagraf, schedeComMax.GetKey(i).Trim(), out lfieldXls);
								fluxHelper.GetCsvRecord(excelDocumentReader, sColumnNameAnagraf, schedeComMax.GetKey(i).Trim(), out sValueXls);
								List<string> sDataAnagrafica;
								sDataAnagrafica = sValueXls.Split('|').ToList();
								/*
								Dictionary<string, string> dictionary = new Dictionary<string, string>();
								for (int iIndAnagField = 0; iIndAnagField < sDataAnagrafica.Count - 1; iIndAnagField = iIndAnagField + 2)
								{
									dictionary.Add(sDataAnagrafica[iIndAnagField].ToUpper(), sDataAnagrafica[iIndAnagField + 1]);
								}
								// Sede esame
								if (dictionary.ContainsKey("SEDE".ToUpper()))
								{
									string value = dictionary["SEDE"];
									oMetadati[int.Parse(resourceFileManager.getConfigData("PvalIndSchedSedEsam"))] = value;
								}
								// Data esame
								if (dictionary.ContainsKey("GIORNO"))
								{
									string value = dictionary["GIORNO"];
									oMetadati[int.Parse(resourceFileManager.getConfigData("PvalIndSchedDatEsam"))] = value;
								}
								// Tornata esame
								if (dictionary.ContainsKey("TORNATA"))
								{
									string value = dictionary["TORNATA"];
									oMetadati[int.Parse(resourceFileManager.getConfigData("PvalIndSchedTornEsam"))] = value;
								}
								// Codice tornata
								if (dictionary.ContainsKey("CODICE TORNATA"))
								{
									string value = dictionary["CODICE TORNATA"];
									oMetadati[int.Parse(resourceFileManager.getConfigData("PvalIndSchedCodTorn"))] = value;
								}
								/*
								  Implementazione per la gestione del campo  Mezzo di spedizione

								Il cmapo report anagrafico è così composto:
								COGNOME|MOXXXTTI|NOME|PXXXXXO|CODICE_FISCALE|xxxxx2232y3sywywsw|INDIRIZZO|VIA DI SANTA CROCE IN GERUSALEMME 91|CAP|00185|LOCALITA|ROMA|COMUNE|ROMA|PROVINCIA|RM|NUM_DELIBERA|993|DATA_DELIBERA|10/01/2019|CODICE CONSOB|90270|TIPO ISCRIZIONE||CODICE OCF|03E4A03A-8F14-4897-BE99-14B94B661A0A|NAZIONE INDIRIZZO||AREA TARIFFARIA|AM|

								*/
								string sMezzoDiTrasmissione = "";
								if (IngMassiveCardIndexes[resourceFileManager.getConfigData("PvalNameMezTrasm").ToUpper()] == resourceFileManager.getConfigData("sCheckMezDiTrasmPEC").ToUpper())
								{
									sMezzoDiTrasmissione = resourceFileManager.getConfigData("sCheckMezDiTrasmPEC").ToUpper();
								}
								else if (IngMassiveCardIndexes[resourceFileManager.getConfigData("PvalNameMezTrasm").ToUpper()] == resourceFileManager.getConfigData("sCheckMezDiTrasmRacAR").ToUpper())
								{
									sMezzoDiTrasmissione = resourceFileManager.getConfigData("sMezDiTrasmRacAR").ToUpper();
								}
								else
								{
									sMezzoDiTrasmissione = IngMassiveCardIndexes[resourceFileManager.getConfigData("PvalNameMezTrasm").ToUpper()];
								}
								sValueXls += "Mezzo di trasmissione|" + sMezzoDiTrasmissione + "|";


								oMetadati[int.Parse(resourceFileManager.getConfigData("IngIndSchedAnagXls"))] = sValueXls;
								// Generazione Codice univoco
								string sCodiceUnivoco = resourceFileManager.getConfigData("IngFormatCodiceUnivoco");
								string[] sFormatValue = sCodiceUnivoco.Split('|');
								foreach (string sValue in sFormatValue)
								{
									string sValueNormalized = sValue.Replace(" ", "_");
									string sCheckCodiceUnivoco = sCodiceUnivoco;
									sCodiceUnivoco = sCodiceUnivoco.Replace(sValueNormalized, lfieldXls[sValueNormalized].Trim());
									if (sCodiceUnivoco == sCheckCodiceUnivoco)
										throw new ArgumentException("Non è stato possibile generare il codice univoco, il valore non identificato è: " + sValueNormalized + " i nomi dei campi nel foglio Excel attesi sono i seguenti: " + resourceFileManager.getConfigData("IngFormatCodiceUnivoco").Replace('|', ' '));
								}
								oMetadati[int.Parse(resourceFileManager.getConfigData("IngIndCodiceUnivoco"))] = sCodiceUnivoco;
								// Sede esame
								//oMetadati[int.Parse(resourceFileManager.getConfigData("PvalIndSchedSedEsam"))] = IscMassiveCardIndexes[resourceFileManager.getConfigData("PvalNameSchedSedEsam")];
								// Data esame
								//oMetadati[int.Parse(resourceFileManager.getConfigData("PvalIndSchedDatEsam"))] = IscMassiveCardIndexes[resourceFileManager.getConfigData("PvalNameSchedDatEsam")];
								// Tornata esame
								//oMetadati[int.Parse(resourceFileManager.getConfigData("PvalIndSchedTornEsam"))] = IscMassiveCardIndexes[resourceFileManager.getConfigData("PvalNameSchedTornEsam")];
								// Codice tornata
								//oMetadati[int.Parse(resourceFileManager.getConfigData("PvalIndSchedCodTorn"))] = IscMassiveCardIndexes[resourceFileManager.getConfigData("PvalNameSchedCodTorn")];


								List<Office> oOffices = new List<Office>();
								logger.Debug("Metadati caricati");
								if (!string.IsNullOrEmpty(schedeComMax.GetKey(i).ToString()))
								{
									// Ricerca all'interno della rubrica generica
									List<GenericEntity> anagUsersRubricaGen = wcfSiavAgrafManager.GetUsers(schedeComMax.GetKey(i).Trim(), resourceFileManager.getConfigData("NomeRubricaCompleta"), resourceFileManager.getConfigData("NomePersonRubricaSearch"));
									if (anagUsersRubricaGen.Count > 1)
									{
										// caso errore trovati più utenti con lo stesso codice fiscale
										sNote = "Sono stati trovati n°:" + anagUsersRubricaGen.Count + " record per il codice OCF:" + schedeComMax.GetKey(i) + ", verificare l'anagrafica " + resourceFileManager.getConfigData("NomeRubricaCompleta");
										logger.Debug(sNote);
										sStato = resourceFileManager.getConfigData("IscStatoSingleCom");
										isInError = true;
									}
									else if (anagUsersRubricaGen.Count == 1)
									{
										logger.Debug("Associazione anagrafica con rubrica generica avvenuta con successo");

										// caso corretto -- implementazione algoritmo Associazione con Anagrafica
										AgrafCardContact svCardContactRg = new AgrafCardContact();
										svCardContactRg.EntityId = new AgrafCardContactId();
										svCardContactRg.EntityId.ContactId = new AgrafEntityId();
										svCardContactRg.EntityId.ContactId.EntityId = anagUsersRubricaGen[0].EntityId.Id;
										svCardContactRg.EntityId.ContactId.Version = (int)anagUsersRubricaGen[0].EntityId.Version;
										svAgrafCardRub.CardContacts.Add(svCardContactRg);
										// Destinatario
										oMetadati[int.Parse(resourceFileManager.getConfigData("IscIndDestinatario"))] = anagUsersRubricaGen[0].Name + " " + anagUsersRubricaGen[0].Person.LastName;
									}
									else
									{
										// caso in cui non è stato trovato l'utente per la scheda
										sNote = "Non è stato trovato alcun record per il codice OCF:" + schedeComMax.GetKey(i) + ", verificare l'anagrafica " + resourceFileManager.getConfigData("NomeRubricaCompleta");
										logger.Debug(sNote);
										sStato = resourceFileManager.getConfigData("IscStatoSingleCom");
										isInError = true;
									}
									// Ricerca all'interno della rubrica Aspiranti e promotori finanziari
									List<GenericEntity> anagUsersAspPF = wcfSiavAgrafManager.GetUsers(schedeComMax.GetKey(i).Trim(), resourceFileManager.getConfigData("NomeRubricaAspPf"), resourceFileManager.getConfigData("NomePersonRubricaSearch"));
									if (anagUsersAspPF.Count > 1)
									{
										// caso errore trovati più utenti con lo stesso codice fiscale
										sNote = "Sono stati trovati n°:" + anagUsersAspPF.Count + " record per il codice OCF:" + schedeComMax.GetKey(i) + ", verificare l'anagrafica " + resourceFileManager.getConfigData("NomeRubricaAspPf");
										logger.Debug(sNote);
										sStato = resourceFileManager.getConfigData("IscStatoSingleCom");
										isInError = true;
									}
									else if (anagUsersAspPF.Count == 1)
									{
										logger.Debug("Associazione anagrafica con rubrica Promotori finanziari avvenuta con successo");

										// caso corretto -- implementazione algoritmo Associazione con Anagrafica
										AgrafCardContact svCardContactApf = new AgrafCardContact();
										svCardContactApf.EntityId = new AgrafCardContactId();
										svCardContactApf.EntityId.ContactId = new AgrafEntityId();
										svCardContactApf.EntityId.ContactId.EntityId = anagUsersAspPF[0].EntityId.Id;
										svCardContactApf.EntityId.ContactId.Version = (int)anagUsersAspPF[0].EntityId.Version;
										svAgrafCardApf.CardContacts.Add(svCardContactApf);
										// codice APF 
										oMetadati[int.Parse(resourceFileManager.getConfigData("IscIndSchedCodApf"))] = anagUsersRubricaGen[0].GenericEntityExternalId;
										// Codice Consob 
										oMetadati[int.Parse(resourceFileManager.getConfigData("IscIndSchedCodConsob"))] = anagUsersRubricaGen[0].GenericEntityExternalId2;
										// Classifica
										oMetadati[int.Parse(resourceFileManager.getConfigData("IscIndSchedClassifica"))] = anagUsersRubricaGen[0].GenericEntityExternalId3;
										// Codice Fiscale
										oMetadati[int.Parse(resourceFileManager.getConfigData("IscIndSchedCodFisc"))] = anagUsersRubricaGen[0].TaxID;
									}
									else
									{
										// caso in cui non è stato trovato l'utente per la scheda
										sNote = "Non è stato trovato alcun record per il codice OCF:" + schedeComMax.GetKey(i) + ", verificare l'anagrafica " + resourceFileManager.getConfigData("NomeRubricaAspPf");
										logger.Debug(sNote);
										sStato = resourceFileManager.getConfigData("IscStatoSingleCom");
										isInError = true;
									}
									// codice APF 
									oMetadati[int.Parse(resourceFileManager.getConfigData("IscIndSchedCodApf"))] = schedeComMax.GetKey(i).Trim();

									// Stato
									oMetadati[int.Parse(resourceFileManager.getConfigData("IscIndSchedStato"))] = sStato;
									oFieldCard.MetaDati = new List<string>(oMetadati);
									logger.Debug("Avvio il processo di inseriemento");
									siavCardManager.Insert(null, schedeComMax.GetValues(i)[0], siavLogin, oFieldCard, oCardVisibility.Groups, oCardVisibility.Offices, oCardVisibility.Users, oDocumentType, oArchive, sNote, sMessage, svAgrafCardRub, svAgrafCardApf, out outCard);
									sResult.Add(outCard.ToString());

									logger.Debug("Scheda inserita, il suo id: " + outCard.ToString());
									logger.Debug("Fine lavorazione scheda " + i);
								}
								else
								{
									logger.Debug("Scheda NON inserita, il suo codice OCF è vuoto");
								}
							}
							sOutput = "Sono state lavorate " + schedeComMax.Count + " schede. ";
							logger.Debug(sOutput);
						}
						//Codice Promotore	Cognome	Nome	Indirizzo	Località	Cap	Provincia	Nr. raccomandata	Data Spedizione	Data ricezione cartolina	Causale mancata ricezione	Nr. Protocollo	Data protocollo	Spese di notifica
					}
					else
					{// errore nessun modello trovato
						sLog = "Non è stato trovato alcun modello.";
						throw new ArgumentException(sLog);
					}
					if (isInError)
					{
						siavCardManager.FieldModify(oCardBundle, siavLogin, 22, "COMPLETATO CON ERRORI");
						sOutput += "Attività completata con errori.";
					}
					else
					{
						siavCardManager.FieldModify(oCardBundle, siavLogin, 22, "COMPLETATO");
						sOutput += "Attività completata.";
					}
					siavCardManager.InsertNote(oCardBundle, siavLogin, sOutput + " Per maggiori informazioni consultare il log per l'id transazione: " + this.IdSessioneTransazione, resourceFileManager.getConfigData("AutorAnnotation"));
					sLog = sOutput;
					siavLogin.Logout();
					siavLogin.siavWsLogin.Close();
					siavLogin = null;
					siavCardManager.siavWsCard.Close();
					siavCardManager = null;
					wcfSiavAgrafManager.siavWsAgraf.Close();
					wcfSiavAgrafManager = null;
					//}
					//else
					//{
					//    sLog = "Non è stato trovato alcun campo identificativo all'interno del file Excel.";
					//    throw new ArgumentException(sLog);
					//}
				}
				else
				{
					sLog = "Non è stato trovato alcun record all'interno del file Excel.";
					throw new ArgumentException(sLog);
				}
			}
			catch (Exception ex)
			{
				sErrorObj = "ERRORE Processo INGIUNZIONE. ";
				// sOutput è formato da IDSessioneTransazione|Descrizione anomalia;
				sOutput = this.IdSessioneTransazione + "|" + ex.Message;
				try
				{
					logger.Error(ex.ToString());
					if (siavCardManager != null)
					{
						siavCardManager.InsertNote(oCardBundle, siavLogin, ex.Message + " Per maggiori informazioni consultare il log per l'id transazione: " + this.IdSessioneTransazione, resourceFileManager.getConfigData("AutorAnnotation"));
						siavCardManager.siavWsCard.Abort();
					}
					if (siavLogin != null)
					{
						siavLogin.Logout();
						siavLogin.siavWsLogin.Close();
					}
					if (wcfSiavAgrafManager != null)
					{
						wcfSiavAgrafManager.siavWsAgraf.Abort();
					}
				}
				catch (Exception e) { }
			}
			finally
			{
				logger.Debug("Fine elaborazione");
				if (resourceFileManager.getConfigData("isTest") != "SI")
				{
					SendMail sendMail = new SendMail();
					string sReceiverEmail = resourceFileManager.getConfigData("emailDestinatario");
					string sSenderNameEmail = resourceFileManager.getConfigData("emailNomeMittente");
					string sObject = sErrorObj + "Comunicazione Massiva " + sObjForObj + " : Inserimento schede in predisposizione " + sSezTer;
					if (sLog == "") sLog = "Problema con l'esecuzione dell'algoritmo di sistema, prego contattare il supporto tecnico.";
					string sbodyMsg = sLog + " Comunicazione Massiva: " + sIdComMaxivaForObj;
					sendMail.SENDMAIL(sReceiverEmail, sSenderNameEmail, sObject, sbodyMsg, "");
					//siavLogin.Logout();
				}
			}
			return sResult.Select(s => (object)s).ToArray();
		}
		public Boolean ApprovalGeneralSecretary(string GUIdcard)
		{
			Boolean bResult = new Boolean();
			WcfSiavLoginManager siavLogin = null;
			WcfSiavCardManager siavCardManager = null;
			bResult = false;
			try
			{
				if (string.IsNullOrEmpty(GUIdcard))
				{
					siavLogin = new WcfSiavLoginManager();
					siavCardManager = new WcfSiavCardManager(logger);
					string oUserName = resourceFileManager.getConfigData("UserFlux");
					string oUserPwd = oRijndael.Decrypt(resourceFileManager.getConfigData("PasswordFlux"));
					// Esegue il login
					siavLogin.Login(oUserName, oUserPwd);
					var lCardNote = siavCardManager.getNote(GUIdcard, siavLogin);
				}
				logger.Debug("Sessione login Archiflow: " + siavLogin.oSessionInfo.SessionId);
				siavLogin.Logout();
				siavLogin.siavWsLogin.Close();
				siavLogin = null;
			}
			catch (Exception ex)
			{
				try
				{
					logger.Error(ex.ToString());
					if (siavLogin != null)
					{
						siavLogin.Logout();
						siavLogin.siavWsLogin.Close();
					}
				}
				catch (Exception e) { }
			}
			finally
			{
				logger.Debug("Fine elaborazione");
			}
			return bResult;
		}
		public Boolean AddVisibility(string sGuidCard, string sUsers, string sOffices, string sGroups)
		{
			logger.Debug("Metodo SendMessage");
			logger.Debug("sUsers " + sUsers);
			logger.Debug("sOffices " + sOffices);
			logger.Debug("sGroups " + sGroups);
			string oUserName = resourceFileManager.getConfigData("UserFlux");
			string oUserPwd = oRijndael.Decrypt(resourceFileManager.getConfigData("PasswordFlux"));
			string sConnection = "";
			Boolean bResult = new Boolean();
			ConnectionManager connectionManager = new ConnectionManager(logger);
			try
			{
				sConnection = connectionManager.OpenUserConnect(oUserName, oUserPwd);
				logger.Debug("Apertura connessione: " + sConnection);

				CardManager oCardManager = new CardManager(logger);//sConnection
				bResult = oCardManager.AddVisibility(sConnection, sGuidCard, sUsers, sOffices, sGroups);
				connectionManager.CloseConnect();
			}
			catch (Exception ex)
			{
				try
				{
					if (!string.IsNullOrEmpty(sConnection))
					{
						connectionManager.CloseConnect();
					}
					logger.Error(ex.ToString());

				}
				catch (Exception e) { }
			}
			finally
			{
				logger.Debug("Fine elaborazione");
			}
			return bResult;
		}
		public Boolean RemoveAllVisibility(string sGuidCard)
		{
			logger.Debug("Metodo RemoveAllVisibility");
			string oUserName = resourceFileManager.getConfigData("UserFlux");
			string oUserPwd = oRijndael.Decrypt(resourceFileManager.getConfigData("PasswordFlux"));
			string sConnection = "";
			Boolean bResult = new Boolean();
			ConnectionManager connectionManager = new ConnectionManager(logger);
			try
			{
				sConnection = connectionManager.OpenUserConnect(oUserName, oUserPwd);
				logger.Debug("Apertura connessione: " + sConnection);

				CardManager oCardManager = new CardManager(logger);//sConnection
				bResult = oCardManager.RemoveAllVisibility(sConnection, sGuidCard);
				connectionManager.CloseConnect();
			}
			catch (Exception ex)
			{
				try
				{
					if (!string.IsNullOrEmpty(sConnection))
					{
						connectionManager.CloseConnect();
					}
					logger.Error(ex.ToString());

				}
				catch (Exception e) { }
			}
			finally
			{
				logger.Debug("Fine elaborazione");
			}
			return bResult;
		}
		public Boolean RemoveVisibility(string sGuidCard, string sUsers, string sOffices, string sGroups)
		{
			logger.Debug("Metodo RemoveVisibility");
			logger.Debug("sUsers " + sUsers);
			logger.Debug("sOffices " + sOffices);
			logger.Debug("sGroups " + sGroups);
			string oUserName = resourceFileManager.getConfigData("UserFlux");
			string oUserPwd = oRijndael.Decrypt(resourceFileManager.getConfigData("PasswordFlux"));
			string sConnection = "";
			Boolean bResult = new Boolean();
			ConnectionManager connectionManager = new ConnectionManager(logger);
			try
			{
				sConnection = connectionManager.OpenUserConnect(oUserName, oUserPwd);
				logger.Debug("Apertura connessione: " + sConnection);

				CardManager oCardManager = new CardManager(logger);//sConnection
				bResult = oCardManager.RemoveVisibility(sConnection, sGuidCard, sUsers, sOffices, sGroups);
				connectionManager.CloseConnect();
			}
			catch (Exception ex)
			{
				try
				{
					if (!string.IsNullOrEmpty(sConnection))
					{
						connectionManager.CloseConnect();
					}
					logger.Error(ex.ToString());

				}
				catch (Exception e) { }
			}
			finally
			{
				logger.Debug("Fine elaborazione");
			}
			return bResult;
		}
		public Boolean SendMessage(string sGuidCard, string sUsers, string sOffices, string sGroups, string sMessage)
		{
			logger.Debug("Metodo SendMessage");
			logger.Debug("sUsers " + sUsers);
			logger.Debug("sOffices " + sOffices);
			logger.Debug("sGroups " + sGroups);
			string oUserName = resourceFileManager.getConfigData("UserFlux");
			string oUserPwd = oRijndael.Decrypt(resourceFileManager.getConfigData("PasswordFlux"));
			string sConnection = "";
			Boolean bResult = new Boolean();
			ConnectionManager connectionManager = new ConnectionManager(logger);
			try
			{
				sConnection = connectionManager.OpenUserConnect(oUserName, oUserPwd);
				logger.Debug("Apertura connessione: " + sConnection);

				//CardVisibilityManager visibilityFromCard = new CardVisibilityManager(sGuidCard, "");//sConnection
				bResult = connectionManager.SendNotify(sConnection, sGuidCard, sOffices, sGroups, sUsers, sMessage);
				connectionManager.CloseConnect();
			}
			catch (Exception ex)
			{
				try
				{
					if (!string.IsNullOrEmpty(sConnection))
					{
						connectionManager.CloseConnect();
					}
					logger.Error(ex.ToString());

				}
				catch (Exception e) { }
			}
			finally
			{
				logger.Debug("Fine elaborazione");
			}
			return bResult;
		}
		public Boolean SendNotify(string GUIdcard, string sMessage, string sSubject, string sAuthor, string emailTo, Boolean isCardAnnotation, string ArchEmailToUser, string ArchEmailToOffice, string ArchEmailToGroup)
		{
			Boolean bResult = new Boolean();
			WcfSiavLoginManager siavLogin = null;
			WcfSiavCardManager siavCardManager = null;
			CardBundle oCardBundle = null;
			bResult = false;
			try
			{
				if (isCardAnnotation)
				{
					siavLogin = new WcfSiavLoginManager();
					siavCardManager = new WcfSiavCardManager(logger);

					string oUserName = resourceFileManager.getConfigData("UserFlux");
					string oUserPwd = oRijndael.Decrypt(resourceFileManager.getConfigData("PasswordFlux"));

					// Esegue il login
					siavLogin.Login(oUserName, oUserPwd);

					siavCardManager.GetCard(GUIdcard, siavLogin, out oCardBundle);
					logger.Debug("Card id: " + oCardBundle.CardId);
					siavCardManager.InsertNote(oCardBundle, siavLogin, sMessage, sAuthor);
					logger.Debug("Il messaggio: " + sMessage + " è stato inserito.");
				}
				logger.Debug("Sessione login Archiflow: " + siavLogin.oSessionInfo.SessionId);
				siavLogin.Logout();
				siavLogin.siavWsLogin.Close();
				siavLogin = null;
			}
			catch (Exception ex)
			{
				try
				{
					logger.Error(ex.ToString());
					if (siavLogin != null)
					{
						siavLogin.Logout();
						siavLogin.siavWsLogin.Close();
					}
				}
				catch (Exception e) { }
			}
			finally
			{
				logger.Debug("Fine elaborazione");
			}
			return bResult;
		}
		public object[] getUserOfficesFromFlux(string sInstanceFlux, out string userName)
		{
			List<String> lResult = new List<string>();
			WcfSiavLoginManager siavLogin = null;
			WcfSiavChartManager siavChart = null;
			userName = "";
			try
			{
				siavLogin = new WcfSiavLoginManager();
				siavChart = new WcfSiavChartManager();
				logger.Debug("Caricamento file di risorse terminato");

				string oUserName = resourceFileManager.getConfigData("UserFlux");
				string oUserPwd = oRijndael.Decrypt(resourceFileManager.getConfigData("PasswordFlux"));

				// Esegue il login
				siavLogin.Login(oUserName, oUserPwd);
				AspNet.Identity.Oracle.OracleDatabase oOracleDb = new AspNet.Identity.Oracle.OracleDatabase();
				WorkFlowManager oWorkFlowManager = new WorkFlowManager(oOracleDb);
				logger.Debug("Istanza flusso: " + sInstanceFlux);

				var username = oWorkFlowManager.FindUserNameFromProcess(sInstanceFlux);
				logger.Debug("Nome utente individuato: " + username.ToString());

				SiavWsChart.User oUser = siavChart.getUser(siavLogin, username.ToString());
				//userName = oUser.Name;
				userName = username;
				foreach (Siav.APFlibrary.SiavWsChart.Office oOffice in oUser.Offices)
				{
					lResult.Add(oOffice.Name);

				}
				//SiavWsChart.SendObject oSendObject =  siavChart.getUserOffices(siavLogin, oUser.Code);
				//Helper.UserHelper uh = new UserHelper();
				//lResult = uh.getUserOffices(oSendObject);
				logger.Debug("Sessione login Archiflow: " + siavLogin.oSessionInfo.SessionId);
				siavLogin.Logout();
				siavLogin.siavWsLogin.Close();
				siavLogin = null;
				siavChart.siavWsChart.Close();
				siavChart = null;
			}
			catch (Exception ex)
			{
				// sOutput è formato da IDSessioneTransazione|Descrizione anomalia;
				//sOutput = idSessioneTransazione + "|" + ex.Message;
				try
				{
					logger.Error(ex.ToString());
					if (siavLogin != null)
					{
						siavLogin.Logout();
						siavLogin.siavWsLogin.Close();
					}
					if (siavChart != null)
					{
						siavChart.siavWsChart.Abort();
					}
				}
				catch (Exception e) { }
			}
			finally
			{
				logger.Debug("Fine elaborazione");
				//siavLogin.Logout();
			}
			return (object[])lResult.ToArray();
		}
		public Boolean CheckCardsInError(string IdComMaxiva)
		{
			WcfSiavLoginManager siavLogin = null;
			WcfSiavCardManager siavCardManager = null;
			bool bResult = false;
			try
			{
				List<Guid> lGUID;
				siavLogin = new WcfSiavLoginManager();
				string oUserName = resourceFileManager.getConfigData("UserFlux");
				string oUserPwd = oRijndael.Decrypt(resourceFileManager.getConfigData("PasswordFlux"));

				// Esegue il login
				siavLogin.Login(oUserName, oUserPwd);
				logger.Debug("Sessione login Archiflow: " + siavLogin.oSessionInfo.SessionId);

				siavCardManager = new WcfSiavCardManager(logger);

				List<SchedaSearchParameter> lSchedaSearchParameter = new List<SchedaSearchParameter>();

				List<string> lMetadati;
				SchedaSearchParameter schedaSearchParameter = new SchedaSearchParameter();
				schedaSearchParameter.Archivio = resourceFileManager.getConfigData("IscArcCardInError");
				schedaSearchParameter.TipologiaDocumentale = resourceFileManager.getConfigData("IscDocTypeCardInError");
				string[] arr = new string[20];
				arr[int.Parse(resourceFileManager.getConfigData("IscIndexFieldCardInError"))] = IdComMaxiva;
				arr[int.Parse(resourceFileManager.getConfigData("IscIndexStatoCardInError"))] = resourceFileManager.getConfigData("IscValueStatoCardInError");

				logger.Debug("Inizio ricerca record");
				logger.Debug("  Cerco per Archivio: " + resourceFileManager.getConfigData("IscArcCardInError"));
				logger.Debug("  Cerco per TipologiaDocumentale: " + resourceFileManager.getConfigData("IscDocTypeCardInError"));
				logger.Debug("  Cerco per ID comunicazione massiva: " + resourceFileManager.getConfigData("IscIndexFieldCardInError"));
				logger.Debug("  Cerco per STATO: " + resourceFileManager.getConfigData("IscValueStatoCardInError"));

				logger.Debug("Fine ricerca record");

				lMetadati = new List<string>(arr);
				schedaSearchParameter.MetaDati = lMetadati;
				lSchedaSearchParameter.Add(schedaSearchParameter);
				siavCardManager.getSearch(lSchedaSearchParameter, siavLogin, out lGUID);
				if (lGUID.Count == 0)
				{
					bResult = true;
				}
				siavLogin.Logout();
				siavLogin.siavWsLogin.Close();
				siavLogin = null;
				siavCardManager.siavWsCard.Close();
				siavCardManager = null;
			}
			catch (Exception ex)
			{
				try
				{
					logger.Error(ex.ToString());
					if (siavCardManager != null)
					{
						siavCardManager.siavWsCard.Abort();
					}
					if (siavLogin != null)
					{
						siavLogin.Logout();
						siavLogin.siavWsLogin.Close();
					}
				}
				catch (Exception e) { }
			}
			finally
			{
				logger.Debug("Fine elaborazione");
				//siavLogin.Logout();
			}
			return bResult;
		}
		public Boolean CheckCardsInErrorIngiunzione(string IdComMaxiva)
		{
			WcfSiavLoginManager siavLogin = null;
			WcfSiavCardManager siavCardManager = null;
			bool bResult = false;
			try
			{
				List<Guid> lGUID;
				siavLogin = new WcfSiavLoginManager();
				string oUserName = resourceFileManager.getConfigData("UserFlux");
				string oUserPwd = oRijndael.Decrypt(resourceFileManager.getConfigData("PasswordFlux"));

				// Esegue il login
				siavLogin.Login(oUserName, oUserPwd);
				logger.Debug("Sessione login Archiflow: " + siavLogin.oSessionInfo.SessionId);

				siavCardManager = new WcfSiavCardManager(logger);

				List<SchedaSearchParameter> lSchedaSearchParameter = new List<SchedaSearchParameter>();

				List<string> lMetadati;
				SchedaSearchParameter schedaSearchParameter = new SchedaSearchParameter();
				schedaSearchParameter.Archivio = resourceFileManager.getConfigData("IngArcCardInError");
				schedaSearchParameter.TipologiaDocumentale = resourceFileManager.getConfigData("IngDocTypeCardInError");
				string[] arr = new string[20];
				arr[int.Parse(resourceFileManager.getConfigData("IngIndexFieldCardInError"))] = IdComMaxiva;
				arr[int.Parse(resourceFileManager.getConfigData("IngIndexStatoCardInError"))] = resourceFileManager.getConfigData("IngValueStatoCardInError");

				logger.Debug("Inizio ricerca record");
				logger.Debug("  Cerco per Archivio: " + resourceFileManager.getConfigData("IngArcCardInError"));
				logger.Debug("  Cerco per TipologiaDocumentale: " + resourceFileManager.getConfigData("IngDocTypeCardInError"));
				logger.Debug("  Cerco per ID comunicazione massiva: " + resourceFileManager.getConfigData("IngIndexFieldCardInError"));
				logger.Debug("  Cerco per STATO: " + resourceFileManager.getConfigData("IngValueStatoCardInError"));

				logger.Debug("Fine ricerca record");

				lMetadati = new List<string>(arr);
				schedaSearchParameter.MetaDati = lMetadati;
				lSchedaSearchParameter.Add(schedaSearchParameter);
				siavCardManager.getSearch(lSchedaSearchParameter, siavLogin, out lGUID);
				if (lGUID.Count == 0)
				{
					bResult = true;
				}
				siavLogin.Logout();
				siavLogin.siavWsLogin.Close();
				siavLogin = null;
				siavCardManager.siavWsCard.Close();
				siavCardManager = null;
			}
			catch (Exception ex)
			{
				try
				{
					logger.Error(ex.ToString());
					if (siavCardManager != null)
					{
						siavCardManager.siavWsCard.Abort();
					}
					if (siavLogin != null)
					{
						siavLogin.Logout();
						siavLogin.siavWsLogin.Close();
					}
				}
				catch (Exception e) { }
			}
			finally
			{
				logger.Debug("Fine elaborazione");
				//siavLogin.Logout();
			}
			return bResult;
		}
		public Boolean CheckCardsInErrorCancellazione(string IdComMaxiva)
		{
			WcfSiavLoginManager siavLogin = null;
			WcfSiavCardManager siavCardManager = null;
			bool bResult = false;
			try
			{
				List<Guid> lGUID;
				siavLogin = new WcfSiavLoginManager();
				string oUserName = resourceFileManager.getConfigData("UserFlux");
				string oUserPwd = oRijndael.Decrypt(resourceFileManager.getConfigData("PasswordFlux"));

				// Esegue il login
				siavLogin.Login(oUserName, oUserPwd);
				logger.Debug("Sessione login Archiflow: " + siavLogin.oSessionInfo.SessionId);

				siavCardManager = new WcfSiavCardManager(logger);

				List<SchedaSearchParameter> lSchedaSearchParameter = new List<SchedaSearchParameter>();

				List<string> lMetadati;
				SchedaSearchParameter schedaSearchParameter = new SchedaSearchParameter();
				schedaSearchParameter.Archivio = resourceFileManager.getConfigData("CancArcCardInError");
				schedaSearchParameter.TipologiaDocumentale = resourceFileManager.getConfigData("CancDocTypeCardInError");
				string[] arr = new string[20];
				arr[int.Parse(resourceFileManager.getConfigData("CancIndexFieldCardInError"))] = IdComMaxiva;
				arr[int.Parse(resourceFileManager.getConfigData("CancIndexStatoCardInError"))] = resourceFileManager.getConfigData("CancValueStatoCardInError");

				logger.Debug("Inizio ricerca record");
				logger.Debug("  Cerco per Archivio: " + resourceFileManager.getConfigData("CancArcCardInError"));
				logger.Debug("  Cerco per TipologiaDocumentale: " + resourceFileManager.getConfigData("CancDocTypeCardInError"));
				logger.Debug("  Cerco per ID comunicazione massiva: " + resourceFileManager.getConfigData("CancIndexFieldCardInError"));
				logger.Debug("  Cerco per STATO: " + resourceFileManager.getConfigData("CancValueStatoCardInError"));

				logger.Debug("Fine ricerca record");

				lMetadati = new List<string>(arr);
				schedaSearchParameter.MetaDati = lMetadati;
				lSchedaSearchParameter.Add(schedaSearchParameter);
				siavCardManager.getSearch(lSchedaSearchParameter, siavLogin, out lGUID);
				if (lGUID.Count == 0)
				{
					bResult = true;
				}
				siavLogin.Logout();
				siavLogin.siavWsLogin.Close();
				siavLogin = null;
				siavCardManager.siavWsCard.Close();
				siavCardManager = null;
			}
			catch (Exception ex)
			{
				try
				{
					logger.Error(ex.ToString());
					if (siavCardManager != null)
					{
						siavCardManager.siavWsCard.Abort();
					}
					if (siavLogin != null)
					{
						siavLogin.Logout();
						siavLogin.siavWsLogin.Close();
					}
				}
				catch (Exception e) { }
			}
			finally
			{
				logger.Debug("Fine elaborazione");
				//siavLogin.Logout();
			}
			return bResult;
		}
		public Boolean CheckCardsInErrorProvaValutativa(string IdComMaxiva)
		{
			WcfSiavLoginManager siavLogin = null;
			WcfSiavCardManager siavCardManager = null;
			bool bResult = false;
			try
			{
				List<Guid> lGUID;
				siavLogin = new WcfSiavLoginManager();
				string oUserName = resourceFileManager.getConfigData("UserFlux");
				string oUserPwd = oRijndael.Decrypt(resourceFileManager.getConfigData("PasswordFlux"));

				// Esegue il login
				siavLogin.Login(oUserName, oUserPwd);
				logger.Debug("Sessione login Archiflow: " + siavLogin.oSessionInfo.SessionId);

				siavCardManager = new WcfSiavCardManager(logger);

				List<SchedaSearchParameter> lSchedaSearchParameter = new List<SchedaSearchParameter>();

				List<string> lMetadati;
				SchedaSearchParameter schedaSearchParameter = new SchedaSearchParameter();
				schedaSearchParameter.Archivio = resourceFileManager.getConfigData("PvalArcCardInError");
				schedaSearchParameter.TipologiaDocumentale = resourceFileManager.getConfigData("PvalDocTypeCardInError");
				string[] arr = new string[20];
				arr[int.Parse(resourceFileManager.getConfigData("PvalIndexFieldCardInError"))] = IdComMaxiva;
				arr[int.Parse(resourceFileManager.getConfigData("PvalIndexStatoCardInError"))] = resourceFileManager.getConfigData("PvalValueStatoCardInError");

				logger.Debug("Inizio ricerca record");
				logger.Debug("  Cerco per Archivio: " + resourceFileManager.getConfigData("PvalArcCardInError"));
				logger.Debug("  Cerco per TipologiaDocumentale: " + resourceFileManager.getConfigData("PvalDocTypeCardInError"));
				logger.Debug("  Cerco per ID comunicazione massiva: " + resourceFileManager.getConfigData("PvalIndexFieldCardInError"));
				logger.Debug("  Cerco per STATO: " + resourceFileManager.getConfigData("PvalValueStatoCardInError"));



				lMetadati = new List<string>(arr);
				schedaSearchParameter.MetaDati = lMetadati;
				lSchedaSearchParameter.Add(schedaSearchParameter);
				siavCardManager.getSearch(lSchedaSearchParameter, siavLogin, out lGUID);
				logger.Debug("Fine ricerca record, trovati n:" + lGUID.Count);
				if (lGUID.Count == 0)
				{
					lGUID = new List<Guid>();
					lSchedaSearchParameter = new List<SchedaSearchParameter>();

					lMetadati = new List<string>();
					schedaSearchParameter = new SchedaSearchParameter();
					schedaSearchParameter.Archivio = resourceFileManager.getConfigData("PvalArcCardInError");
					schedaSearchParameter.TipologiaDocumentale = resourceFileManager.getConfigData("PvalDocTypeCardInError");
					arr = new string[24];
					arr[int.Parse(resourceFileManager.getConfigData("PvalIndexFieldCardInError"))] = IdComMaxiva;
					arr[int.Parse(resourceFileManager.getConfigData("PvalIndexStatoCardInError"))] = resourceFileManager.getConfigData("PvalValueStatoCardInReadyToProt");

					logger.Debug("Inizio ricerca record");
					logger.Debug("  Cerco per Archivio: " + resourceFileManager.getConfigData("PvalArcCardInError"));
					logger.Debug("  Cerco per TipologiaDocumentale: " + resourceFileManager.getConfigData("PvalDocTypeCardInError"));
					logger.Debug("  Cerco per ID comunicazione massiva: " + resourceFileManager.getConfigData("PvalIndexFieldCardInError"));
					logger.Debug("  Cerco per STATO: " + resourceFileManager.getConfigData("PvalValueStatoCardInReadyToProt"));
					lMetadati = new List<string>(arr);
					schedaSearchParameter.MetaDati = lMetadati;
					lSchedaSearchParameter.Add(schedaSearchParameter);
					siavCardManager.getSearch(lSchedaSearchParameter, siavLogin, out lGUID);
					int CardReadyToProt = lGUID.Count();

					lGUID = new List<Guid>();
					logger.Debug("  Numero di schede pronte per essere protocollate: " + CardReadyToProt);

					lSchedaSearchParameter = new List<SchedaSearchParameter>();

					lMetadati = new List<string>();
					schedaSearchParameter = new SchedaSearchParameter();
					schedaSearchParameter.Archivio = resourceFileManager.getConfigData("PvalArcCardInError");
					schedaSearchParameter.TipologiaDocumentale = resourceFileManager.getConfigData("PvalDocTypeCardInError");
					arr = new string[24];
					arr[int.Parse(resourceFileManager.getConfigData("PvalIndexFieldCardInError"))] = IdComMaxiva;

					logger.Debug("Inizio ricerca record");
					logger.Debug("  Cerco per Archivio: " + resourceFileManager.getConfigData("PvalArcCardInError"));
					logger.Debug("  Cerco per TipologiaDocumentale: " + resourceFileManager.getConfigData("PvalDocTypeCardInError"));
					logger.Debug("  Cerco per ID comunicazione massiva: " + resourceFileManager.getConfigData("PvalIndexFieldCardInError"));
					lMetadati = new List<string>(arr);
					schedaSearchParameter.MetaDati = lMetadati;
					lSchedaSearchParameter.Add(schedaSearchParameter);
					siavCardManager.getSearch(lSchedaSearchParameter, siavLogin, out lGUID);
					int CardInFlux = lGUID.Count();
					logger.Debug("  Numero di schede presenti nel flusso: " + CardInFlux);
					if (CardInFlux == CardReadyToProt)
						bResult = true;
				}
				siavLogin.Logout();
				siavLogin.siavWsLogin.Close();
				siavLogin = null;
				siavCardManager.siavWsCard.Close();
				siavCardManager = null;
			}
			catch (Exception ex)
			{
				try
				{
					logger.Error(ex.ToString());
					if (siavCardManager != null)
					{
						siavCardManager.siavWsCard.Abort();
					}
					if (siavLogin != null)
					{
						siavLogin.Logout();
						siavLogin.siavWsLogin.Close();
					}
				}
				catch (Exception e) { }
			}
			finally
			{
				logger.Debug("Fine elaborazione");
				//siavLogin.Logout();
			}
			return bResult;
		}

		public Boolean ConvertMainDocument2Pdf(string GUIdcard)
		{
			WcfSiavLoginManager siavLogin = null;
			WcfSiavCardManager siavCardManager = null;
			bool bResult = false;
			CardBundle oCardBundle = null;

			try
			{
				siavLogin = new WcfSiavLoginManager();
				FluxHelper fluxHelper = new FluxHelper();
				string oUserName = resourceFileManager.getConfigData("UserFlux");
				string oUserPwd = oRijndael.Decrypt(resourceFileManager.getConfigData("PasswordFlux"));

				MainDoc oMainDoc;
				// Esegue il login
				siavLogin.Login(oUserName, oUserPwd);
				logger.Debug("Sessione login Archiflow: " + siavLogin.oSessionInfo.SessionId);

				siavCardManager = new WcfSiavCardManager(logger);
				// Recupera la scheda ove è stato avviato il processo
				siavCardManager.GetCard(GUIdcard, siavLogin, out oCardBundle);

				// Recupera il documento principale della scheda 
				siavCardManager.GetMainDoc(oCardBundle, out oMainDoc);

				// Materializza il file sul filesystem
				string sFileDocx2Convert = this.sWorkingFolder + @"\" + oMainDoc.Filename + "_Docx2Conv." + oMainDoc.Extension;
				logger.Debug("Scrivo il documento principale: " + sFileDocx2Convert);
				fluxHelper.FileMaterialize(sFileDocx2Convert, oMainDoc.oByte);
				WsOcf.MainDocument InputDoc = new WsOcf.MainDocument();
				WsOcf.MainDocument OutputDoc = new WsOcf.MainDocument();
				InputDoc.Filename = oMainDoc.Filename + "." + oMainDoc.Extension;
				InputDoc.BinaryContent = Convert.ToBase64String(oMainDoc.oByte);
				WsOcf.ServicesClient convertDoc = new WsOcf.ServicesClient();
				convertDoc.base64ToPdfA(out OutputDoc, InputDoc);
				siavCardManager.SetMainDoc(oCardBundle, siavLogin, OutputDoc.Filename, OutputDoc.BinaryContent);
				siavLogin.Logout();
				siavLogin.siavWsLogin.Close();
				siavLogin = null;
				siavCardManager.siavWsCard.Close();
				siavCardManager = null;
				bResult = true;

			}
			catch (Exception ex)
			{
				try
				{
					logger.Error(ex.ToString());
					if (siavCardManager != null)
					{
						siavCardManager.siavWsCard.Abort();
					}
					if (siavLogin != null)
					{
						siavLogin.Logout();
						siavLogin.siavWsLogin.Close();
					}
				}
				catch (Exception e) { }
			}
			finally
			{
				logger.Debug("Fine elaborazione");
				//siavLogin.Logout();
			}
			return bResult;
		}


		public String InsertInProtocolArchive(string IdComMaxiva)
		{
			WcfSiavLoginManager siavLogin = null;
			WcfSiavCardManager siavCardManager = null;
			CardBundle oCardBundleIscMassive = null;
			int iInsertInProtocol = 0;
			string sResult = "";
			try
			{
				List<Guid> lGUID;
				siavLogin = new WcfSiavLoginManager();
				string oUserName = resourceFileManager.getConfigData("UserFlux");
				string oUserPwd = oRijndael.Decrypt(resourceFileManager.getConfigData("PasswordFlux"));

				// Esegue il login
				siavLogin.Login(oUserName, oUserPwd);
				logger.Debug("Sessione login Archiflow: " + siavLogin.oSessionInfo.SessionId);

				siavCardManager = new WcfSiavCardManager(logger);

				// Sezione per ricavare il cardBundle della scheda massiva generante
				List<Guid> lGUIDIscMassive;
				List<SchedaSearchParameter> lIscMassiveSP = new List<SchedaSearchParameter>();

				List<string> lIscMassiveMet;
				SchedaSearchParameter IscMassiveSP = new SchedaSearchParameter();
				IscMassiveSP.Archivio = resourceFileManager.getConfigData("IscArchiveMassive");
				IscMassiveSP.TipologiaDocumentale = resourceFileManager.getConfigData("IscTypeDocMassive");
				string[] arrIscMassiveMet = new string[20];
				// Individuo tutti i record appartenenti al batch della comunicazione massiva
				//arrIscMassiveMet[int.Parse("1")] = IdComMaxiva;
				IscMassiveSP.IdRiferimento = IdComMaxiva;
				logger.Debug("Inizio ricerca record");
				logger.Debug("  Cerco per Archivio: " + IscMassiveSP.Archivio);
				logger.Debug("  Cerco per TipologiaDocumentale: " + IscMassiveSP.TipologiaDocumentale);
				logger.Debug("  Cerco per ID comunicazione massiva: " + IdComMaxiva);
				logger.Debug("Fine ricerca record");

				lIscMassiveMet = new List<string>(arrIscMassiveMet);
				IscMassiveSP.MetaDati = lIscMassiveMet;
				lIscMassiveSP.Add(IscMassiveSP);
				siavCardManager.getSearch(lIscMassiveSP, siavLogin, out lGUIDIscMassive);
				foreach (Guid singleIscMassive in lGUIDIscMassive)
				{
					siavCardManager.GetCard(singleIscMassive.ToString(), siavLogin, out oCardBundleIscMassive);
				}
				//
				List<SchedaSearchParameter> lSchedaSearchParameter = new List<SchedaSearchParameter>();

				// Individuo tutti i record presenti nella tipologia AUTO con lo stato pronto per la protocollazione
				List<string> lMetadati;
				SchedaSearchParameter schedaSearchParameter = new SchedaSearchParameter();
				schedaSearchParameter.Archivio = resourceFileManager.getConfigData("IscArcCardInError");
				schedaSearchParameter.TipologiaDocumentale = resourceFileManager.getConfigData("IscDocTypeCardInError");
				string[] arr = new string[20];
				arr[int.Parse(resourceFileManager.getConfigData("IscIndexFieldCardInError"))] = IdComMaxiva;
				arr[int.Parse(resourceFileManager.getConfigData("IscIndexStatoCardInError"))] = resourceFileManager.getConfigData("IscValueStatoCardInReadyToProt");

				logger.Debug("Inizio ricerca record");
				logger.Debug("  Cerco per Archivio: " + resourceFileManager.getConfigData("IscArcCardInError"));
				logger.Debug("  Cerco per TipologiaDocumentale: " + resourceFileManager.getConfigData("IscDocTypeCardInError"));
				logger.Debug("  Cerco per ID comunicazione massiva: " + IdComMaxiva);
				logger.Debug("  Cerco per STATO: " + resourceFileManager.getConfigData("IscValueStatoCardInReadyToProt"));

				logger.Debug("Fine ricerca record");

				lMetadati = new List<string>(arr);
				schedaSearchParameter.MetaDati = lMetadati;
				lSchedaSearchParameter.Add(schedaSearchParameter);
				siavCardManager.getSearch(lSchedaSearchParameter, siavLogin, out lGUID);
				List<int> lOnlyNumericPartGuid = new List<int>();
				foreach (Guid sSingleValue in lGUID)
				{
					lOnlyNumericPartGuid.Add(int.Parse(sSingleValue.ToString().Substring(25)));
				}
				lOnlyNumericPartGuid.Sort();
				short shArchiveToProtId = 0;
				DocumentType oDocType = new DocumentType();
				Archive oArchive = new Archive();
				short shTypeDocToProtId = 0;
				// Estraggo le schede da protocollare dall'ambiente di predisposizione
				siavCardManager.getArchiveDoc(resourceFileManager.getConfigData("IscArcCardInProt"), siavLogin.oSessionInfo, out oArchive);
				siavCardManager.getTypeDoc(resourceFileManager.getConfigData("IscDocTypeCardInProt"), siavLogin.oSessionInfo, out oDocType);
				shArchiveToProtId = oArchive.ArchiveId;
				shTypeDocToProtId = oDocType.DocumentTypeId;
				foreach (int singleGuid in lOnlyNumericPartGuid)
				{
					logger.Debug("Sto elaborando la scheda per eseguire la protocollazione con id: " + singleGuid.ToString());
					List<Guid> lIfExistsGUID;
					List<SchedaSearchParameter> lIfExistsSchedaSearchParameter = new List<SchedaSearchParameter>();
					CardBundle oCardBundleProt;
					siavCardManager.GetCard(singleGuid.ToString(), siavLogin, out oCardBundleProt);
					NameValueCollection IscProtCardIndexes;
					siavCardManager.GetIndexes(oCardBundleProt, out IscProtCardIndexes);

					List<string> lIfExistsMetadati;
					SchedaSearchParameter IfExistsSchedaSearchParameter = new SchedaSearchParameter();
					IfExistsSchedaSearchParameter.Archivio = resourceFileManager.getConfigData("IscArcCardInProt");
					IfExistsSchedaSearchParameter.TipologiaDocumentale = resourceFileManager.getConfigData("IscDocTypeCardInProt");
					string[] IfExistsArr = new string[20];

					IfExistsSchedaSearchParameter.Oggetto = resourceFileManager.getConfigData("IscObjSearchUnique");
					IfExistsArr[int.Parse(resourceFileManager.getConfigData("IscIndSchedSearchUnique"))] = IscProtCardIndexes[resourceFileManager.getConfigData("IscFieldProt")];

					logger.Debug("Inizio ricerca record");
					logger.Debug("  Cerco per Archivio: " + resourceFileManager.getConfigData("IscArcCardInProt"));
					logger.Debug("  Cerco per TipologiaDocumentale: " + resourceFileManager.getConfigData("IscDocTypeCardInProt"));
					logger.Debug("  Cerco per " + resourceFileManager.getConfigData("IscFieldProt") + ": " + IscProtCardIndexes[resourceFileManager.getConfigData("IscFieldProt")]);
					//logger.Debug("  Cerco per Codice OCF: " + IscProtCardIndexes[resourceFileManager.getConfigData("IscFieldProt")]);
					logger.Debug("  Cerco per Oggetto: " + resourceFileManager.getConfigData("IscObjSearchUnique"));
					logger.Debug("Fine ricerca record");

					lIfExistsMetadati = new List<string>(IfExistsArr);
					IfExistsSchedaSearchParameter.MetaDati = lIfExistsMetadati;
					lIfExistsSchedaSearchParameter.Add(IfExistsSchedaSearchParameter);
					// Ricerca se la scheda sia già stata protocollata
					siavCardManager.getSearch(lIfExistsSchedaSearchParameter, siavLogin, out lIfExistsGUID);
					// Inserimento scheda protocollo
					if (lIfExistsGUID.Count == 0)
					{
						Guid oCardProtocol;
						CardBundle oCardBundle = null;
						HashSet<int> fieldDataToErase = new HashSet<int>();
						CardManager OCardManager = new CardManager(logger);
						List<string> lfieldDataToErase = resourceFileManager.getConfigData("IscFieldNoProt").Split(',').ToList();
						foreach (string singleValue in lfieldDataToErase)
						{
							fieldDataToErase.Add(int.Parse(singleValue));
						}
						siavCardManager.GetCard(singleGuid.ToString(), siavLogin, out oCardBundle);
						siavCardManager.InsertCard(oCardBundle, fieldDataToErase, shArchiveToProtId, shTypeDocToProtId, siavLogin, out oCardProtocol);
						OCardManager.setCardVisibilityDefault(oUserName, oUserPwd, oCardProtocol.ToString());

						siavCardManager.siavWsCard.MakePressMark(siavLogin.oSessionInfo.SessionId, oCardProtocol, resourceFileManager.getConfigData("EtichettaElettronica").ToString(), true);
						string[] oMetadatiPred = new string[20];
						for (int i = 0; i < oMetadatiPred.Length; i++)
						{
							oMetadatiPred[i] = null;
						}
						string sProtData = "";
						NameValueCollection IndexesProtCard = null;
						CardBundle oCardBundleProtocol;
						siavCardManager.GetCard(oCardProtocol.ToString(), siavLogin, out oCardBundleProtocol);
						List<string> sPathAttachments = new List<string>();
						siavCardManager.GetCardAttachments(sWorkingFolder, oCardBundle.CardId.ToString(), siavLogin, out sPathAttachments);
						if (sPathAttachments != null)
							foreach (string singleAttach in sPathAttachments)
							{
								siavCardManager.SetAttachment(oCardBundleProtocol, siavLogin, singleAttach, "");
							}

						siavCardManager.GetIndexes(oCardBundleProtocol, out IndexesProtCard);
						sProtData = IndexesProtCard[resourceFileManager.getConfigData("InsNameFieldIdProt")] + " - " + IndexesProtCard[resourceFileManager.getConfigData("InsNameFieldDataProt")];
						oMetadatiPred[int.Parse(resourceFileManager.getConfigData("IscIndProtGen"))] = sProtData;
						siavCardManager.SetIndexes(oCardBundle, siavLogin, new List<string>(oMetadatiPred));
						iInsertInProtocol++;
						logger.Debug("Inserita la scheda:" + oCardProtocol.ToString());
						sResult += oCardProtocol.ToString() + "|";
					}
					else if (lIfExistsGUID.Count > 0)
					{
						// Record trovato, notifica l'informazione se il record appartiene ad un invio massivo diverso da quello processato
						bool isCardInvalid = false;// check che verifica se la scheda trovata non sia annullata, perchè in tal caso l'inserimento della scheda DEVE avvenire
						foreach (Guid idGUID in lIfExistsGUID)
						{
							NameValueCollection IndexesProtCardCheck = null;
							CardBundle oCardBundleCheck = null;
							siavCardManager.GetCard(idGUID.ToString(), siavLogin, out oCardBundleCheck);
							bool checkCardValid;
							siavCardManager.CheckValidCard(oCardBundleCheck, out checkCardValid);
							if (checkCardValid)
							{
								isCardInvalid = true;
								siavCardManager.GetIndexes(oCardBundleCheck, out IndexesProtCardCheck);
								if (IndexesProtCardCheck[resourceFileManager.getConfigData("InsNameFieldComMassiva")] == IdComMaxiva)
								{
									// Non fare nulla
								}
								else
								{
									// Notifica il record individuato
									siavCardManager.InsertNote(oCardBundleIscMassive, siavLogin, "È stata trovata una scheda già protocollata, il suo protocollo assoluto è: " + int.Parse(lIfExistsGUID[0].ToString().Substring(25)) + ". La scheda in predisposizione avente il protocollo assoluto: " + singleGuid + " non è stata protocollata.", resourceFileManager.getConfigData("AutorAnnotation"));
								}
								break;
							}
						}
						if (isCardInvalid == false)
						{
							Guid oCardProtocol;
							CardBundle oCardBundle = null;
							HashSet<int> fieldDataToErase = new HashSet<int>();
							List<string> lfieldDataToErase = resourceFileManager.getConfigData("IscFieldNoProt").Split(',').ToList();
							foreach (string singleValue in lfieldDataToErase)
							{
								fieldDataToErase.Add(int.Parse(singleValue));
							}
							siavCardManager.GetCard(singleGuid.ToString(), siavLogin, out oCardBundle);
							siavCardManager.InsertCard(oCardBundle, fieldDataToErase, shArchiveToProtId, shTypeDocToProtId, siavLogin, out oCardProtocol);
							CardManager oCardManager = new CardManager(logger);
							oCardManager.setCardVisibilityDefault(oUserName, oUserPwd, oCardProtocol.ToString());
							siavCardManager.siavWsCard.MakePressMark(siavLogin.oSessionInfo.SessionId, oCardProtocol, resourceFileManager.getConfigData("EtichettaElettronica").ToString(), true);
							string[] oMetadatiPred = new string[20];
							for (int i = 0; i < oMetadatiPred.Length; i++)
							{
								oMetadatiPred[i] = null;
							}
							string sProtData = "";
							NameValueCollection IndexesProtCard = null;
							CardBundle oCardBundleProtocol;
							siavCardManager.GetCard(oCardProtocol.ToString(), siavLogin, out oCardBundleProtocol);
							siavCardManager.GetIndexes(oCardBundleProtocol, out IndexesProtCard);
							sProtData = IndexesProtCard[resourceFileManager.getConfigData("InsNameFieldIdProt")] + " - " + IndexesProtCard[resourceFileManager.getConfigData("InsNameFieldDataProt")];
							oMetadatiPred[int.Parse(resourceFileManager.getConfigData("IscIndProtGen"))] = sProtData;
							siavCardManager.SetIndexes(oCardBundle, siavLogin, new List<string>(oMetadatiPred));
							iInsertInProtocol++;
							logger.Debug("Inserita la scheda:" + oCardProtocol.ToString());
							sResult += oCardProtocol.ToString() + "|";
						}
					}

				}
				// Inserimento note scheda padre massiva
				siavCardManager.InsertNote(oCardBundleIscMassive, siavLogin, "Sono state inserire in Archivio ufficiale numero: " + iInsertInProtocol + " protocolli.", resourceFileManager.getConfigData("AutorAnnotation"));
				// Aggiornamento dello stato scheda padre massiva con il valore "Completato"
				string[] oMetadati = new string[20];
				for (int i = 0; i < oMetadati.Length; i++)
				{
					oMetadati[i] = null;
				}
				oMetadati[int.Parse(resourceFileManager.getConfigData("IscStatoFieldMassive"))] = resourceFileManager.getConfigData("IscStatoCompletedValueMassive");
				siavCardManager.SetIndexes(oCardBundleIscMassive, siavLogin, new List<string>(oMetadati));

				// Verifica inserimento dati confrontando il numero delle schede in predisposizione con quelle protocollate
				lSchedaSearchParameter = new List<SchedaSearchParameter>();

				lMetadati = null;
				schedaSearchParameter = new SchedaSearchParameter();
				schedaSearchParameter.Archivio = resourceFileManager.getConfigData("IscArcCardInError");
				schedaSearchParameter.TipologiaDocumentale = resourceFileManager.getConfigData("IscDocTypeCardInError");
				arr = new string[20];
				arr[int.Parse(resourceFileManager.getConfigData("IscIndexFieldCardInError"))] = IdComMaxiva;

				logger.Debug("Inizio ricerca record");
				logger.Debug("  Cerco per Archivio: " + resourceFileManager.getConfigData("IscArcCardInError"));
				logger.Debug("  Cerco per TipologiaDocumentale: " + resourceFileManager.getConfigData("IscDocTypeCardInError"));
				logger.Debug("  Cerco per ID comunicazione massiva: " + IdComMaxiva);
				logger.Debug("Fine ricerca record");

				lMetadati = new List<string>(arr);
				schedaSearchParameter.MetaDati = lMetadati;
				lSchedaSearchParameter.Add(schedaSearchParameter);
				List<Guid> lGuidPredIsc;
				List<Guid> lGuidProtIsc;
				logger.Debug("lSchedaSearchParameter PRED: " + ToJson(lSchedaSearchParameter));

				siavCardManager.getSearch(lSchedaSearchParameter, siavLogin, out lGuidPredIsc);
				logger.Debug("lGuidPredIsc: " + ToJson(lGuidPredIsc));
				schedaSearchParameter.Archivio = resourceFileManager.getConfigData("IscArcCardInProt");
				schedaSearchParameter.TipologiaDocumentale = resourceFileManager.getConfigData("IscDocTypeCardInProt");
				logger.Debug("lSchedaSearchParameter PROT: " + ToJson(lSchedaSearchParameter));
				siavCardManager.getSearch(lSchedaSearchParameter, siavLogin, out lGuidProtIsc);
				logger.Debug("lGuidProtIsc: " + ToJson(lGuidProtIsc));

				if ((lGuidProtIsc != null) && (lGuidProtIsc.Count > 0))
				{
					logger.Debug("Ho trovato Record: " + lGuidProtIsc.Count + " nel protocollo");
				}
				else
					logger.Debug("NON ho trovato Record in lGuidProtIsc nel protocollo");
				if ((lGuidPredIsc != null) && (lGuidPredIsc.Count > 0))
				{
					logger.Debug("Ho trovato Record: " + lGuidPredIsc.Count + " in predisposizione");
				}
				else
					logger.Debug("NON ho trovato Record in lGuidPredIsc in predisposizione");

				if ((lGuidProtIsc != null) && (lGuidProtIsc.Count > 0) && (lGuidPredIsc != null) && (lGuidPredIsc.Count > 0))
				{

					if (lGuidPredIsc.Count == lGuidProtIsc.Count)
					{
						logger.Debug("Creo il report massivo");

						List<int> lOnlyNumericPredPartGuid = new List<int>();
						foreach (Guid sSingleValue in lGuidPredIsc)
						{
							lOnlyNumericPredPartGuid.Add(int.Parse(sSingleValue.ToString().Substring(25)));
						}

						List<string> lDataForReport = new List<string>();
						lOnlyNumericPredPartGuid.Sort();
						foreach (long sSingleValue in lOnlyNumericPredPartGuid)
						{
							NameValueCollection IndexesForReport = null;
							CardBundle oCardBundleForReport;
							string singleRecordReportData = "";
							siavCardManager.GetCard(sSingleValue.ToString(), siavLogin, out oCardBundleForReport);
							siavCardManager.GetIndexes(oCardBundleForReport, out IndexesForReport);
							string sAppo = IndexesForReport[resourceFileManager.getConfigData("InsNamePredFieldIdProt")];
							string numProt = string.Empty;
							string dataProt = string.Empty;
							if (!string.IsNullOrEmpty(sAppo))
							{
								numProt = sAppo.Substring(0, sAppo.IndexOf("-"));
								dataProt = sAppo.Substring(sAppo.IndexOf("-") + 2, sAppo.Length - 2 - sAppo.IndexOf("-"));
							}

							singleRecordReportData = IndexesForReport[resourceFileManager.getConfigData("InsNameFiledAnagXls")] +
													 resourceFileManager.getConfigData("InsNameFieldIdProt") + "|" +
													 numProt + "|" +
													 resourceFileManager.getConfigData("InsNameFieldDataProt") + "|" +
													 dataProt + "|" +
													 resourceFileManager.getConfigData("InsNameFieldEmpty");
							logger.Debug("Record: " + singleRecordReportData);
							lDataForReport.Add(singleRecordReportData);
						}

						// Generazione del report
						FluxHelper fluxHelper = new FluxHelper();
						string note = resourceFileManager.getConfigData("IscNoteReportMassivo");
						// Inserimento report come allegato scheda massiva

						string pathAttachment = fluxHelper.CreateReportMassive(this.sWorkingFolder, IdComMaxiva, lDataForReport);
						logger.Debug("Path report massivo: " + pathAttachment);

						siavCardManager.SetAttachment(oCardBundleIscMassive, siavLogin, pathAttachment, note);
						//
					}
				}
				siavLogin.Logout();
				siavLogin.siavWsLogin.Close();
				siavLogin = null;
				siavCardManager.siavWsCard.Close();
				siavCardManager = null;
			}
			catch (Exception ex)
			{
				try
				{
					siavCardManager.InsertNote(oCardBundleIscMassive, siavLogin, "Sono stati riscontrati degli errori, prego informare gli amministratori di sistema.", resourceFileManager.getConfigData("AutorAnnotation"));
					logger.Error(ex.ToString());
					if (siavCardManager != null)
					{
						siavCardManager.siavWsCard.Abort();
					}
					if (siavLogin != null)
					{
						siavLogin.Logout();
						siavLogin.siavWsLogin.Close();
					}
					sResult = "";
				}
				catch (Exception e) { }
			}
			finally
			{

				logger.Debug("Fine elaborazione");
				//siavLogin.Logout();
			}
			return sResult;
		}
		public String InsertInProtocolArchiveIngiunzione(string IdComMaxiva)
		{
			WcfSiavLoginManager siavLogin = null;
			WcfSiavCardManager siavCardManager = null;
			CardBundle oCardBundleIscMassive = null;
			int iInsertInProtocol = 0;
			string sResult = "";
			try
			{
				List<Guid> lGUID;
				siavLogin = new WcfSiavLoginManager();
				string oUserName = resourceFileManager.getConfigData("UserFlux");
				string oUserPwd = oRijndael.Decrypt(resourceFileManager.getConfigData("PasswordFlux"));

				// Esegue il login
				siavLogin.Login(oUserName, oUserPwd);
				logger.Debug("Sessione login Archiflow: " + siavLogin.oSessionInfo.SessionId);

				siavCardManager = new WcfSiavCardManager(logger);

				// Sezione per ricavare il cardBundle della scheda massiva generante
				List<Guid> lGUIDIscMassive;
				List<SchedaSearchParameter> lIscMassiveSP = new List<SchedaSearchParameter>();

				List<string> lIscMassiveMet;
				SchedaSearchParameter IscMassiveSP = new SchedaSearchParameter();
				IscMassiveSP.Archivio = resourceFileManager.getConfigData("IngArchiveMassive");
				IscMassiveSP.TipologiaDocumentale = resourceFileManager.getConfigData("IngTypeDocMassive");
				string[] arrIscMassiveMet = new string[20];
				//arrIscMassiveMet[int.Parse("1")] = IdComMaxiva;
				IscMassiveSP.IdRiferimento = IdComMaxiva;
				logger.Debug("Inizio ricerca record");
				logger.Debug("  Cerco per Archivio: " + IscMassiveSP.Archivio);
				logger.Debug("  Cerco per TipologiaDocumentale: " + IscMassiveSP.TipologiaDocumentale);
				logger.Debug("  Cerco per ID comunicazione massiva: " + IdComMaxiva);
				logger.Debug("Fine ricerca record");

				lIscMassiveMet = new List<string>(arrIscMassiveMet);
				IscMassiveSP.MetaDati = lIscMassiveMet;
				lIscMassiveSP.Add(IscMassiveSP);
				siavCardManager.getSearch(lIscMassiveSP, siavLogin, out lGUIDIscMassive);
				foreach (Guid singleIscMassive in lGUIDIscMassive)
				{
					siavCardManager.GetCard(singleIscMassive.ToString(), siavLogin, out oCardBundleIscMassive);
				}
				//
				List<SchedaSearchParameter> lSchedaSearchParameter = new List<SchedaSearchParameter>();

				List<string> lMetadati;
				SchedaSearchParameter schedaSearchParameter = new SchedaSearchParameter();
				schedaSearchParameter.Archivio = resourceFileManager.getConfigData("IngArcCardInError");
				schedaSearchParameter.TipologiaDocumentale = resourceFileManager.getConfigData("IngDocTypeCardInError");
				string[] arr = new string[20];
				arr[int.Parse(resourceFileManager.getConfigData("IngIndexFieldCardInError"))] = IdComMaxiva;
				arr[int.Parse(resourceFileManager.getConfigData("IngIndexStatoCardInError"))] = resourceFileManager.getConfigData("IngValueStatoCardInReadyToProt");

				logger.Debug("Inizio ricerca record");
				logger.Debug("  Cerco per Archivio: " + resourceFileManager.getConfigData("IngArcCardInError"));
				logger.Debug("  Cerco per TipologiaDocumentale: " + resourceFileManager.getConfigData("IngDocTypeCardInError"));
				logger.Debug("  Cerco per ID comunicazione massiva: " + IdComMaxiva);
				logger.Debug("  Cerco per STATO: " + resourceFileManager.getConfigData("IngValueStatoCardInReadyToProt"));

				logger.Debug("Fine ricerca record");

				lMetadati = new List<string>(arr);
				schedaSearchParameter.MetaDati = lMetadati;
				lSchedaSearchParameter.Add(schedaSearchParameter);
				siavCardManager.getSearch(lSchedaSearchParameter, siavLogin, out lGUID);
				List<int> lOnlyNumericPartGuid = new List<int>();
				foreach (Guid sSingleValue in lGUID)
				{
					lOnlyNumericPartGuid.Add(int.Parse(sSingleValue.ToString().Substring(25)));
				}
				lOnlyNumericPartGuid.Sort();
				short shArchiveToProtId = 0;
				DocumentType oDocType = new DocumentType();
				Archive oArchive = new Archive();
				short shTypeDocToProtId = 0;
				// Estraggo le schede da protocollare dall'ambiente di predisposizione
				siavCardManager.getArchiveDoc(resourceFileManager.getConfigData("IngArcCardInProt"), siavLogin.oSessionInfo, out oArchive);
				siavCardManager.getTypeDoc(resourceFileManager.getConfigData("IngDocTypeCardInProt"), siavLogin.oSessionInfo, out oDocType);
				shArchiveToProtId = oArchive.ArchiveId;
				shTypeDocToProtId = oDocType.DocumentTypeId;
				foreach (int singleGuid in lOnlyNumericPartGuid)
				{
					logger.Debug("Sto elaborando la scheda per eseguire la protocollazione con id: " + singleGuid.ToString());
					List<Guid> lIfExistsGUID;
					List<SchedaSearchParameter> lIfExistsSchedaSearchParameter = new List<SchedaSearchParameter>();
					CardBundle oCardBundleProt;
					siavCardManager.GetCard(singleGuid.ToString(), siavLogin, out oCardBundleProt);
					NameValueCollection IscProtCardIndexes;
					siavCardManager.GetIndexes(oCardBundleProt, out IscProtCardIndexes);

					List<string> lIfExistsMetadati;
					SchedaSearchParameter IfExistsSchedaSearchParameter = new SchedaSearchParameter();
					IfExistsSchedaSearchParameter.Archivio = resourceFileManager.getConfigData("IngArcCardInProt");
					IfExistsSchedaSearchParameter.TipologiaDocumentale = resourceFileManager.getConfigData("IngDocTypeCardInProt");
					string[] IfExistsArr = new string[20];

					IfExistsSchedaSearchParameter.Oggetto = resourceFileManager.getConfigData("IngObjSearchUnique");
					IfExistsArr[int.Parse(resourceFileManager.getConfigData("IngIndSchedSearchUnique"))] = IscProtCardIndexes[resourceFileManager.getConfigData("IngFieldProt")];

					logger.Debug("Inizio ricerca record");
					logger.Debug("  Cerco per Archivio: " + resourceFileManager.getConfigData("IngArcCardInProt"));
					logger.Debug("  Cerco per TipologiaDocumentale: " + resourceFileManager.getConfigData("IngDocTypeCardInProt"));
					logger.Debug("  Cerco per " + resourceFileManager.getConfigData("IngFieldProt") + ": " + IscProtCardIndexes[resourceFileManager.getConfigData("IngFieldProt")]);

					//logger.Debug("  Cerco per Codice OCF: " + IscProtCardIndexes[resourceFileManager.getConfigData("IngFieldProt")]);
					logger.Debug("  Cerco per Oggetto: " + resourceFileManager.getConfigData("IngObjSearchUnique"));
					logger.Debug("Fine ricerca record");

					lIfExistsMetadati = new List<string>(IfExistsArr);
					IfExistsSchedaSearchParameter.MetaDati = lIfExistsMetadati;
					lIfExistsSchedaSearchParameter.Add(IfExistsSchedaSearchParameter);
					// Ricerca se la scheda sia già stata protocollata
					siavCardManager.getSearch(lIfExistsSchedaSearchParameter, siavLogin, out lIfExistsGUID);
					// Inserimento scheda protocollo
					if (lIfExistsGUID.Count == 0)
					{
						Guid oCardProtocol;
						CardBundle oCardBundle = null;
						HashSet<int> fieldDataToErase = new HashSet<int>();
						List<string> lfieldDataToErase = resourceFileManager.getConfigData("IngFieldNoProt").Split(',').ToList();
						foreach (string singleValue in lfieldDataToErase)
						{
							fieldDataToErase.Add(int.Parse(singleValue));
						}
						siavCardManager.GetCard(singleGuid.ToString(), siavLogin, out oCardBundle);
						siavCardManager.InsertCardIngiunzione(oCardBundle, fieldDataToErase, shArchiveToProtId, shTypeDocToProtId, siavLogin, out oCardProtocol);
						CardManager oCardManager = new CardManager(logger);
						oCardManager.setCardVisibilityDefault(oUserName, oUserPwd, oCardProtocol.ToString());
						siavCardManager.siavWsCard.MakePressMark(siavLogin.oSessionInfo.SessionId, oCardProtocol, resourceFileManager.getConfigData("EtichettaElettronica").ToString(), true);
						string[] oMetadatiPred = new string[20];
						for (int i = 0; i < oMetadatiPred.Length; i++)
						{
							oMetadatiPred[i] = null;
						}
						string sProtData = "";
						NameValueCollection IndexesProtCard = null;
						CardBundle oCardBundleProtocol;
						siavCardManager.GetCard(oCardProtocol.ToString(), siavLogin, out oCardBundleProtocol);
						siavCardManager.GetIndexes(oCardBundleProtocol, out IndexesProtCard);
						sProtData = IndexesProtCard[resourceFileManager.getConfigData("InsNameFieldIdProt")] + " - " + IndexesProtCard[resourceFileManager.getConfigData("InsNameFieldDataProt")];
						oMetadatiPred[int.Parse(resourceFileManager.getConfigData("IngIndProtGen"))] = sProtData;
						siavCardManager.SetIndexes(oCardBundle, siavLogin, new List<string>(oMetadatiPred));
						iInsertInProtocol++;
						logger.Debug("Inserita la scheda:" + oCardProtocol.ToString());
						sResult += oCardProtocol.ToString() + "|";
					}
					else if (lIfExistsGUID.Count > 0)
					{
						// Record trovato, notifica l'informazione se il record appartiene ad un invio massivo diverso da quello processato
						bool isCardInvalid = false;// check che verifica se la scheda trovata non sia annullata, perchè in tal caso l'inserimento della scheda DEVE avvenire
						foreach (Guid idGUID in lIfExistsGUID)
						{
							NameValueCollection IndexesProtCardCheck = null;
							CardBundle oCardBundleCheck = null;
							siavCardManager.GetCard(idGUID.ToString(), siavLogin, out oCardBundleCheck);
							bool checkCardValid;
							siavCardManager.CheckValidCard(oCardBundleCheck, out checkCardValid);
							if (checkCardValid)
							{
								isCardInvalid = true;
								siavCardManager.GetIndexes(oCardBundleCheck, out IndexesProtCardCheck);
								if (IndexesProtCardCheck[resourceFileManager.getConfigData("InsNameFieldComMassiva")] == IdComMaxiva)
								{
									// Non fare nulla
								}
								else
								{
									// Notifica il record individuato
									siavCardManager.InsertNote(oCardBundleIscMassive, siavLogin, "È stata trovata una scheda già protocollata, il suo protocollo assoluto è: " + int.Parse(lIfExistsGUID[0].ToString().Substring(25)) + ". La scheda in predisposizione avente il protocollo assoluto: " + singleGuid + " non è stata protocollata.", resourceFileManager.getConfigData("AutorAnnotation"));
								}
								break;
							}
						}
						if (isCardInvalid == false)
						{
							Guid oCardProtocol;
							CardBundle oCardBundle = null;
							HashSet<int> fieldDataToErase = new HashSet<int>();
							List<string> lfieldDataToErase = resourceFileManager.getConfigData("IngFieldNoProt").Split(',').ToList();
							foreach (string singleValue in lfieldDataToErase)
							{
								fieldDataToErase.Add(int.Parse(singleValue));
							}
							siavCardManager.GetCard(singleGuid.ToString(), siavLogin, out oCardBundle);
							siavCardManager.InsertCardIngiunzione(oCardBundle, fieldDataToErase, shArchiveToProtId, shTypeDocToProtId, siavLogin, out oCardProtocol);
							CardManager oCardManager = new CardManager(logger);
							oCardManager.setCardVisibilityDefault(oUserName, oUserPwd, oCardProtocol.ToString());
							siavCardManager.siavWsCard.MakePressMark(siavLogin.oSessionInfo.SessionId, oCardProtocol, resourceFileManager.getConfigData("EtichettaElettronica").ToString(), true);
							string[] oMetadatiPred = new string[20];
							for (int i = 0; i < oMetadatiPred.Length; i++)
							{
								oMetadatiPred[i] = null;
							}
							string sProtData = "";
							NameValueCollection IndexesProtCard = null;
							CardBundle oCardBundleProtocol;
							siavCardManager.GetCard(oCardProtocol.ToString(), siavLogin, out oCardBundleProtocol);
							siavCardManager.GetIndexes(oCardBundleProtocol, out IndexesProtCard);
							sProtData = IndexesProtCard[resourceFileManager.getConfigData("InsNameFieldIdProt")] + " - " + IndexesProtCard[resourceFileManager.getConfigData("InsNameFieldDataProt")];
							oMetadatiPred[int.Parse(resourceFileManager.getConfigData("IngIndProtGen"))] = sProtData;
							siavCardManager.SetIndexes(oCardBundle, siavLogin, new List<string>(oMetadatiPred));
							iInsertInProtocol++;
							logger.Debug("Inserita la scheda:" + oCardProtocol.ToString());
							sResult += oCardProtocol.ToString() + "|";
						}
					}
				}
				// Inserimento note scheda padre massiva
				siavCardManager.InsertNote(oCardBundleIscMassive, siavLogin, "Sono state inserire in Archivio ufficiale numero: " + iInsertInProtocol + " protocolli.", resourceFileManager.getConfigData("AutorAnnotation"));
				// Aggiornamento dello stato scheda padre massiva con il valore "Completato"
				string[] oMetadati = new string[20];
				for (int i = 0; i < oMetadati.Length; i++)
				{
					oMetadati[i] = null;
				}
				oMetadati[int.Parse(resourceFileManager.getConfigData("IngStatoFieldMassive"))] = resourceFileManager.getConfigData("IscStatoCompletedValueMassive");
				siavCardManager.SetIndexes(oCardBundleIscMassive, siavLogin, new List<string>(oMetadati));

				// Verifica inserimento dati confrontando il numero delle schede in predisposizione con quelle protocollate
				lSchedaSearchParameter = new List<SchedaSearchParameter>();

				lMetadati = null;
				schedaSearchParameter = new SchedaSearchParameter();
				schedaSearchParameter.Archivio = resourceFileManager.getConfigData("IngArcCardInError");
				schedaSearchParameter.TipologiaDocumentale = resourceFileManager.getConfigData("IngDocTypeCardInError");
				arr = new string[20];
				arr[int.Parse(resourceFileManager.getConfigData("IngIndexFieldCardInError"))] = IdComMaxiva;

				logger.Debug("Inizio ricerca record");
				logger.Debug("  Cerco per Archivio: " + resourceFileManager.getConfigData("IngArcCardInError"));
				logger.Debug("  Cerco per TipologiaDocumentale: " + resourceFileManager.getConfigData("IngDocTypeCardInError"));
				logger.Debug("  Cerco per ID comunicazione massiva: " + IdComMaxiva);
				logger.Debug("Fine ricerca record");

				lMetadati = new List<string>(arr);
				schedaSearchParameter.MetaDati = lMetadati;
				lSchedaSearchParameter.Add(schedaSearchParameter);
				List<Guid> lGuidPredIsc;
				List<Guid> lGuidProtIsc;
				siavCardManager.getSearch(lSchedaSearchParameter, siavLogin, out lGuidPredIsc);
				schedaSearchParameter.Archivio = resourceFileManager.getConfigData("IngArcCardInProt");
				schedaSearchParameter.TipologiaDocumentale = resourceFileManager.getConfigData("IngDocTypeCardInProt");
				siavCardManager.getSearch(lSchedaSearchParameter, siavLogin, out lGuidProtIsc);
				if ((lGuidProtIsc != null) && (lGuidProtIsc.Count > 0))
				{
					logger.Debug("Ho trovato Record: " + lGuidProtIsc.Count + " nel protocollo");
				}
				else
					logger.Debug("NON ho trovato Record in lGuidProtIsc nel protocollo");
				if ((lGuidPredIsc != null) && (lGuidPredIsc.Count > 0))
				{
					logger.Debug("Ho trovato Record: " + lGuidPredIsc.Count + " in predisposizione");
				}
				else
					logger.Debug("NON ho trovato Record in lGuidPredIsc in predisposizione");

				if ((lGuidProtIsc != null) && (lGuidProtIsc.Count > 0) && (lGuidPredIsc != null) && (lGuidPredIsc.Count > 0))
				{

					if (lGuidPredIsc.Count == lGuidProtIsc.Count)
					{
						logger.Debug("Creo il report massivo");

						List<int> lOnlyNumericPredPartGuid = new List<int>();
						foreach (Guid sSingleValue in lGuidPredIsc)
						{
							lOnlyNumericPredPartGuid.Add(int.Parse(sSingleValue.ToString().Substring(25)));
						}

						List<string> lDataForReport = new List<string>();
						lOnlyNumericPredPartGuid.Sort();
						foreach (long sSingleValue in lOnlyNumericPredPartGuid)
						{
							NameValueCollection IndexesForReport = null;
							CardBundle oCardBundleForReport;
							string singleRecordReportData = "";
							siavCardManager.GetCard(sSingleValue.ToString(), siavLogin, out oCardBundleForReport);
							siavCardManager.GetIndexes(oCardBundleForReport, out IndexesForReport);
							string sAppo = IndexesForReport[resourceFileManager.getConfigData("InsNamePredFieldIdProt")];
							string numProt = string.Empty;
							string dataProt = string.Empty;
							if (!string.IsNullOrEmpty(sAppo))
							{
								numProt = sAppo.Substring(0, sAppo.IndexOf("-"));
								dataProt = sAppo.Substring(sAppo.IndexOf("-") + 2, sAppo.Length - 2 - sAppo.IndexOf("-"));
							}

							singleRecordReportData = IndexesForReport[resourceFileManager.getConfigData("InsNameFiledAnagXls")] +
													 resourceFileManager.getConfigData("InsNameFieldIdProt") + "|" +
													 numProt + "|" +
													 resourceFileManager.getConfigData("InsNameFieldDataProt") + "|" +
													 dataProt + "|" +
													 resourceFileManager.getConfigData("InsNameFieldEmpty");
							logger.Debug("Record: " + singleRecordReportData);
							lDataForReport.Add(singleRecordReportData);
						}

						// Generazione del report
						FluxHelper fluxHelper = new FluxHelper();
						string note = resourceFileManager.getConfigData("IscNoteReportMassivo");
						// Inserimento report come allegato scheda massiva

						string pathAttachment = fluxHelper.CreateReportMassive(this.sWorkingFolder, IdComMaxiva, lDataForReport);
						logger.Debug("Path report massivo: " + pathAttachment);

						siavCardManager.SetAttachment(oCardBundleIscMassive, siavLogin, pathAttachment, note);
						//
					}
				}
				siavLogin.Logout();
				siavLogin.siavWsLogin.Close();
				siavLogin = null;
				siavCardManager.siavWsCard.Close();
				siavCardManager = null;
			}
			catch (Exception ex)
			{
				try
				{
					siavCardManager.InsertNote(oCardBundleIscMassive, siavLogin, "Sono stati riscontrati degli errori, prego informare gli amministratori di sistema.", resourceFileManager.getConfigData("AutorAnnotation"));
					logger.Error(ex.ToString());
					if (siavCardManager != null)
					{
						siavCardManager.siavWsCard.Abort();
					}
					if (siavLogin != null)
					{
						siavLogin.Logout();
						siavLogin.siavWsLogin.Close();
					}
					sResult = "";
				}
				catch (Exception e) { }
			}
			finally
			{

				logger.Debug("Fine elaborazione");
				//siavLogin.Logout();
			}
			return sResult;
		}

		public String InsertInProtocolArchiveProvaValutativa(string IdComMaxiva)
		{
			WcfSiavLoginManager siavLogin = null;
			WcfSiavCardManager siavCardManager = null;
			CardBundle oCardBundlePvalMassive = null;
			int iInsertInProtocol = 0;
			string sResult = "";
			try
			{
				List<Guid> lGUID;
				siavLogin = new WcfSiavLoginManager();
				string oUserName = resourceFileManager.getConfigData("UserFlux");
				string oUserPwd = oRijndael.Decrypt(resourceFileManager.getConfigData("PasswordFlux"));

				// Esegue il login
				siavLogin.Login(oUserName, oUserPwd);
				logger.Debug("Sessione login Archiflow: " + siavLogin.oSessionInfo.SessionId);

				siavCardManager = new WcfSiavCardManager(logger);

				// Sezione per ricavare il cardBundle della scheda massiva generante
				List<Guid> lGUIDPvalMassive;
				List<SchedaSearchParameter> lIscMassiveSP = new List<SchedaSearchParameter>();

				List<string> lIscMassiveMet;
				SchedaSearchParameter PvalMassiveSP = new SchedaSearchParameter();
				PvalMassiveSP.Archivio = resourceFileManager.getConfigData("PvalArchiveMassive");
				PvalMassiveSP.TipologiaDocumentale = resourceFileManager.getConfigData("PvalTypeDocMassive");
				string[] arrIscMassiveMet = new string[20];
				//arrIscMassiveMet[int.Parse("1")] = IdComMaxiva;
				PvalMassiveSP.IdRiferimento = IdComMaxiva;
				logger.Debug("Inizio ricerca record");
				logger.Debug("  Cerco per Archivio: " + PvalMassiveSP.Archivio);
				logger.Debug("  Cerco per TipologiaDocumentale: " + PvalMassiveSP.TipologiaDocumentale);
				logger.Debug("  Cerco per ID comunicazione massiva: " + IdComMaxiva);
				logger.Debug("Fine ricerca record");

				lIscMassiveMet = new List<string>(arrIscMassiveMet);
				PvalMassiveSP.MetaDati = lIscMassiveMet;
				lIscMassiveSP.Add(PvalMassiveSP);
				siavCardManager.getSearch(lIscMassiveSP, siavLogin, out lGUIDPvalMassive);
				foreach (Guid singlePvalMassive in lGUIDPvalMassive)
				{
					siavCardManager.GetCard(singlePvalMassive.ToString(), siavLogin, out oCardBundlePvalMassive);
				}
				//

				List<SchedaSearchParameter> lSchedaSearchParameter = new List<SchedaSearchParameter>();

				List<string> lMetadati;
				SchedaSearchParameter schedaSearchParameter = new SchedaSearchParameter();
				schedaSearchParameter.Archivio = resourceFileManager.getConfigData("PvalArcCardInError");
				schedaSearchParameter.TipologiaDocumentale = resourceFileManager.getConfigData("PvalDocTypeCardInError");
				string[] arr = new string[24];
				arr[int.Parse(resourceFileManager.getConfigData("PvalIndexFieldCardInError"))] = IdComMaxiva;
				//arr[int.Parse(resourceFileManager.getConfigData("PvalIndexStatoCardInError"))] = resourceFileManager.getConfigData("PvalValueStatoCardInReadyToProt");

				logger.Debug("Inizio ricerca record");
				logger.Debug("  Cerco per Archivio: " + resourceFileManager.getConfigData("PvalArcCardInError"));
				logger.Debug("  Cerco per TipologiaDocumentale: " + resourceFileManager.getConfigData("PvalDocTypeCardInError"));
				logger.Debug("  Cerco per ID comunicazione massiva: " + resourceFileManager.getConfigData("PvalIndexFieldCardInError"));
				//logger.Debug("  Cerco per STATO: " + resourceFileManager.getConfigData("PvalValueStatoCardInReadyToProt"));

				logger.Debug("Fine ricerca record");

				lMetadati = new List<string>(arr);
				schedaSearchParameter.MetaDati = lMetadati;
				lSchedaSearchParameter.Add(schedaSearchParameter);
				siavCardManager.getSearch(lSchedaSearchParameter, siavLogin, out lGUID);
				List<int> lOnlyNumericPartGuid = new List<int>();
				foreach (Guid sSingleValue in lGUID)
				{
					lOnlyNumericPartGuid.Add(int.Parse(sSingleValue.ToString().Substring(25)));
				}
				lOnlyNumericPartGuid.Sort();
				short shArchiveToProtId = 0;
				DocumentType oDocType = new DocumentType();
				Archive oArchive = new Archive();
				short shTypeDocToProtId = 0;
				// Estraggo le schede da protocollare dall'ambiente di predisposizione
				siavCardManager.getArchiveDoc(resourceFileManager.getConfigData("PvalArcCardInProt"), siavLogin.oSessionInfo, out oArchive);
				siavCardManager.getTypeDoc(resourceFileManager.getConfigData("PvalDocTypeCardInProt"), siavLogin.oSessionInfo, out oDocType);
				shArchiveToProtId = oArchive.ArchiveId;
				shTypeDocToProtId = oDocType.DocumentTypeId;
				foreach (int singleGuid in lOnlyNumericPartGuid)
				{
					logger.Debug("Sto elaborando la scheda per eseguire la protocollazione con id: " + singleGuid.ToString());
					List<Guid> lIfExistsGUID;
					List<SchedaSearchParameter> lIfExistsSchedaSearchParameter = new List<SchedaSearchParameter>();
					CardBundle oCardBundleProt;
					siavCardManager.GetCard(singleGuid.ToString(), siavLogin, out oCardBundleProt);
					NameValueCollection PvalProtCardIndexes;
					siavCardManager.GetIndexes(oCardBundleProt, out PvalProtCardIndexes);

					List<string> lIfExistsMetadati;
					SchedaSearchParameter IfExistsSchedaSearchParameter = new SchedaSearchParameter();
					IfExistsSchedaSearchParameter.Archivio = resourceFileManager.getConfigData("PvalArcCardInProt");
					IfExistsSchedaSearchParameter.TipologiaDocumentale = resourceFileManager.getConfigData("PvalDocTypeCardInProt");
					string[] IfExistsArr = new string[24];

					IfExistsSchedaSearchParameter.Oggetto = resourceFileManager.getConfigData("PvalObjSearchUnique");
					// Filtro per:
					// Codice Univoco
					IfExistsArr[int.Parse(resourceFileManager.getConfigData("PvalIndSchedSearchUnique"))] = PvalProtCardIndexes[resourceFileManager.getConfigData("PvalFieldProt")];
					// Sede
					//IfExistsArr[int.Parse(resourceFileManager.getConfigData("PvalIndSchedSearchUniqueSede"))] = PvalProtCardIndexes[resourceFileManager.getConfigData("PvalNameSchedSedEsam")];
					// Data Esame
					//IfExistsArr[int.Parse(resourceFileManager.getConfigData("PvalIndSchedSearchUniqueDataEsame"))] = PvalProtCardIndexes[resourceFileManager.getConfigData("PvalNameSchedDatEsam")];
					logger.Debug("Inizio ricerca record");
					logger.Debug("  Cerco per Archivio: " + resourceFileManager.getConfigData("PvalArcCardInProt"));
					logger.Debug("  Cerco per TipologiaDocumentale: " + resourceFileManager.getConfigData("PvalDocTypeCardInProt"));
					logger.Debug("  Cerco per " + resourceFileManager.getConfigData("PvalFieldProt") + ": " + PvalProtCardIndexes[resourceFileManager.getConfigData("PvalFieldProt")]);

					//logger.Debug("  Cerco per Codice OCF: " + PvalProtCardIndexes[resourceFileManager.getConfigData("PvalFieldProt")]);
					//logger.Debug("  Cerco per Sede: " + PvalProtCardIndexes[resourceFileManager.getConfigData("PvalNameSchedSedEsam")]);
					//logger.Debug("  Cerco per Data Esame: " + PvalProtCardIndexes[resourceFileManager.getConfigData("PvalNameSchedDatEsam")]);
					logger.Debug("  Cerco per Oggetto: " + resourceFileManager.getConfigData("PvalObjSearchUnique"));
					logger.Debug("Fine ricerca record");

					lIfExistsMetadati = new List<string>(IfExistsArr);
					IfExistsSchedaSearchParameter.MetaDati = lIfExistsMetadati;
					lIfExistsSchedaSearchParameter.Add(IfExistsSchedaSearchParameter);
					// Ricerca se la scheda sia già stata protocollata
					siavCardManager.getSearch(lIfExistsSchedaSearchParameter, siavLogin, out lIfExistsGUID);
					// Inserimento scheda protocollo
					if (lIfExistsGUID.Count == 0)
					{
						Guid oCardProtocol;
						CardBundle oCardBundle = null;
						HashSet<int> fieldDataToErase = new HashSet<int>();
						List<string> lfieldDataToErase = resourceFileManager.getConfigData("PvalFieldNoProt").Split(',').ToList();
						foreach (string singleValue in lfieldDataToErase)
						{
							fieldDataToErase.Add(int.Parse(singleValue));
						}
						siavCardManager.GetCard(singleGuid.ToString(), siavLogin, out oCardBundle);
						siavCardManager.InsertCardProvVal(oCardBundle, fieldDataToErase, shArchiveToProtId, shTypeDocToProtId, siavLogin, out oCardProtocol);
						CardManager oCardManager = new CardManager(logger);
						oCardManager.setCardVisibilityDefault(oUserName, oUserPwd, oCardProtocol.ToString());
						siavCardManager.siavWsCard.MakePressMark(siavLogin.oSessionInfo.SessionId, oCardProtocol, resourceFileManager.getConfigData("EtichettaElettronica").ToString(), true);
						string[] oMetadatiPred = new string[20];
						for (int i = 0; i < oMetadatiPred.Length; i++)
						{
							oMetadatiPred[i] = null;
						}
						string sProtData = "";
						NameValueCollection IndexesProtCard = null;
						CardBundle oCardBundleProtocol;
						siavCardManager.GetCard(oCardProtocol.ToString(), siavLogin, out oCardBundleProtocol);
						siavCardManager.GetIndexes(oCardBundleProtocol, out IndexesProtCard);
						sProtData = IndexesProtCard[resourceFileManager.getConfigData("PvalNameFieldIdProt")] + " - " + IndexesProtCard[resourceFileManager.getConfigData("PvalNameFieldDataProt")];
						oMetadatiPred[int.Parse(resourceFileManager.getConfigData("PvalIndProtGen"))] = sProtData;
						siavCardManager.SetIndexes(oCardBundle, siavLogin, new List<string>(oMetadatiPred));
						iInsertInProtocol++;
						logger.Debug("Inserita la scheda:" + oCardProtocol.ToString());
						sResult += oCardProtocol.ToString() + "|";
					}
					else if (lIfExistsGUID.Count > 0)
					{
						// Record trovato, notifica l'informazione se il record appartiene ad un invio massivo diverso da quello processato
						bool isCardInvalid = false;// check che verifica se la scheda trovata non sia annullata, perchè in tal caso l'inserimento della scheda DEVE avvenire
						foreach (Guid idGUID in lIfExistsGUID)
						{
							NameValueCollection IndexesProtCardCheck = null;
							CardBundle oCardBundleCheck = null;
							siavCardManager.GetCard(idGUID.ToString(), siavLogin, out oCardBundleCheck);
							bool checkCardValid;
							siavCardManager.CheckValidCard(oCardBundleCheck, out checkCardValid);
							if (checkCardValid)
							{
								isCardInvalid = true;
								siavCardManager.GetIndexes(oCardBundleCheck, out IndexesProtCardCheck);
								if (IndexesProtCardCheck[resourceFileManager.getConfigData("InsNameFieldComMassiva")] == IdComMaxiva)
								{
									// Non fare nulla
								}
								else
								{
									// Notifica il record individuato
									siavCardManager.InsertNote(oCardBundlePvalMassive, siavLogin, "È stata trovata una scheda già protocollata, il suo protocollo assoluto è: " + int.Parse(lIfExistsGUID[0].ToString().Substring(25)) + ". La scheda in predisposizione avente il protocollo assoluto: " + singleGuid + " non è stata protocollata.", resourceFileManager.getConfigData("AutorAnnotation"));
								}
								break;
							}
						}
						if (isCardInvalid == false)
						{
							Guid oCardProtocol;
							CardBundle oCardBundle = null;
							HashSet<int> fieldDataToErase = new HashSet<int>();
							List<string> lfieldDataToErase = resourceFileManager.getConfigData("PvalFieldNoProt").Split(',').ToList();
							foreach (string singleValue in lfieldDataToErase)
							{
								fieldDataToErase.Add(int.Parse(singleValue));
							}
							siavCardManager.GetCard(singleGuid.ToString(), siavLogin, out oCardBundle);
							siavCardManager.InsertCardProvVal(oCardBundle, fieldDataToErase, shArchiveToProtId, shTypeDocToProtId, siavLogin, out oCardProtocol);
							CardManager oCardManager = new CardManager(logger);
							oCardManager.setCardVisibilityDefault(oUserName, oUserPwd, oCardProtocol.ToString());
							siavCardManager.siavWsCard.MakePressMark(siavLogin.oSessionInfo.SessionId, oCardProtocol, resourceFileManager.getConfigData("EtichettaElettronica").ToString(), true);
							string[] oMetadatiPred = new string[20];
							for (int i = 0; i < oMetadatiPred.Length; i++)
							{
								oMetadatiPred[i] = null;
							}
							string sProtData = "";
							NameValueCollection IndexesProtCard = null;
							CardBundle oCardBundleProtocol;
							siavCardManager.GetCard(oCardProtocol.ToString(), siavLogin, out oCardBundleProtocol);
							siavCardManager.GetIndexes(oCardBundleProtocol, out IndexesProtCard);
							sProtData = IndexesProtCard[resourceFileManager.getConfigData("PvalNameFieldIdProt")] + " - " + IndexesProtCard[resourceFileManager.getConfigData("PvalNameFieldDataProt")];
							oMetadatiPred[int.Parse(resourceFileManager.getConfigData("PvalIndProtGen"))] = sProtData;
							siavCardManager.SetIndexes(oCardBundle, siavLogin, new List<string>(oMetadatiPred));
							iInsertInProtocol++;
							logger.Debug("Inserita la scheda:" + oCardProtocol.ToString());
							sResult += oCardProtocol.ToString() + "|";
						}
					}

				}
				// Inserimento note scheda padre massiva
				siavCardManager.InsertNote(oCardBundlePvalMassive, siavLogin, "Sono state inserite in Archivio ufficiale numero: " + iInsertInProtocol + " protocolli.", resourceFileManager.getConfigData("AutorAnnotation"));
				// Aggiornamento dello stato scheda padre massiva con il valore "Completato"
				string[] oMetadati = new string[20];
				for (int i = 0; i < oMetadati.Length; i++)
				{
					oMetadati[i] = null;
				}
				oMetadati[int.Parse(resourceFileManager.getConfigData("PvalStatoFieldMassive"))] = resourceFileManager.getConfigData("IscStatoCompletedValueMassive");
				siavCardManager.SetIndexes(oCardBundlePvalMassive, siavLogin, new List<string>(oMetadati));

				// Verifica inserimento dati confrontando il numero delle schede in predisposizione con quelle protocollate
				lSchedaSearchParameter = new List<SchedaSearchParameter>();

				lMetadati = null;
				schedaSearchParameter = new SchedaSearchParameter();
				schedaSearchParameter.Archivio = resourceFileManager.getConfigData("PvalArcCardInError");
				schedaSearchParameter.TipologiaDocumentale = resourceFileManager.getConfigData("PvalDocTypeCardInError");
				arr = new string[20];
				arr[int.Parse(resourceFileManager.getConfigData("PvalIndexFieldCardInError"))] = IdComMaxiva;

				logger.Debug("Inizio ricerca record");
				logger.Debug("  Cerco per Archivio: " + resourceFileManager.getConfigData("PvalArcCardInError"));
				logger.Debug("  Cerco per TipologiaDocumentale: " + resourceFileManager.getConfigData("PvalDocTypeCardInError"));
				logger.Debug("  Cerco per ID comunicazione massiva: " + IdComMaxiva);
				logger.Debug("Fine ricerca record");

				lMetadati = new List<string>(arr);
				schedaSearchParameter.MetaDati = lMetadati;
				lSchedaSearchParameter.Add(schedaSearchParameter);
				List<Guid> lGuidPredIsc;
				List<Guid> lGuidProtIsc;
				siavCardManager.getSearch(lSchedaSearchParameter, siavLogin, out lGuidPredIsc);
				schedaSearchParameter.Archivio = resourceFileManager.getConfigData("PvalArcCardInProt");
				schedaSearchParameter.TipologiaDocumentale = resourceFileManager.getConfigData("PvalDocTypeCardInProt");
				siavCardManager.getSearch(lSchedaSearchParameter, siavLogin, out lGuidProtIsc);
				if ((lGuidProtIsc != null) && (lGuidProtIsc.Count > 0))
				{
					logger.Debug("Ho trovato Record: " + lGuidProtIsc.Count + " nel protocollo");
				}
				else
					logger.Debug("NON ho trovato Record in lGuidProtIsc nel protocollo");
				if ((lGuidPredIsc != null) && (lGuidPredIsc.Count > 0))
				{
					logger.Debug("Ho trovato Record: " + lGuidPredIsc.Count + " in predisposizione");
				}
				else
					logger.Debug("NON ho trovato Record in lGuidPredIsc in predisposizione");
				if ((lGuidProtIsc != null) && (lGuidProtIsc.Count > 0) && (lGuidPredIsc != null) && (lGuidPredIsc.Count > 0))
				{
					if (lGuidPredIsc.Count == lGuidProtIsc.Count)
					{
						logger.Debug("Creo il report massivo");

						List<int> lOnlyNumericPredPartGuid = new List<int>();
						foreach (Guid sSingleValue in lGuidPredIsc)
						{
							lOnlyNumericPredPartGuid.Add(int.Parse(sSingleValue.ToString().Substring(25)));
						}

						List<string> lDataForReport = new List<string>();
						lOnlyNumericPredPartGuid.Sort();
						foreach (long sSingleValue in lOnlyNumericPredPartGuid)
						{
							NameValueCollection IndexesForReport = null;
							CardBundle oCardBundleForReport;
							string singleRecordReportData = "";
							siavCardManager.GetCard(sSingleValue.ToString(), siavLogin, out oCardBundleForReport);
							siavCardManager.GetIndexes(oCardBundleForReport, out IndexesForReport);

							string sAppo = IndexesForReport[resourceFileManager.getConfigData("InsNamePredFieldIdProt")];
							string numProt = string.Empty;
							string dataProt = string.Empty;
							if (!string.IsNullOrEmpty(sAppo))
							{
								numProt = sAppo.Substring(0, sAppo.IndexOf("-"));
								dataProt = sAppo.Substring(sAppo.IndexOf("-") + 2, sAppo.Length - 2 - sAppo.IndexOf("-"));
							}

							singleRecordReportData = IndexesForReport[resourceFileManager.getConfigData("PvalNameFiledAnagXls")] +
													 resourceFileManager.getConfigData("InsNameFieldIdProt") + "|" +
													 numProt + "|" +
													 resourceFileManager.getConfigData("InsNameFieldDataProt") + "|" +
													 dataProt + "|" +
													 resourceFileManager.getConfigData("InsNameFieldEmpty");
							logger.Debug("Record: " + singleRecordReportData);
							lDataForReport.Add(singleRecordReportData);
						}

						// Generazione del report
						FluxHelper fluxHelper = new FluxHelper();
						string note = resourceFileManager.getConfigData("IscNoteReportMassivo");
						// Inserimento report come allegato scheda massiva

						string pathAttachment = fluxHelper.CreateReportMassive(this.sWorkingFolder, IdComMaxiva, lDataForReport);
						logger.Debug("Path report massivo: " + pathAttachment);

						siavCardManager.SetAttachment(oCardBundlePvalMassive, siavLogin, pathAttachment, note);
						//
					}
				}
				siavLogin.Logout();
				siavLogin.siavWsLogin.Close();
				siavLogin = null;
				siavCardManager.siavWsCard.Close();
				siavCardManager = null;
			}
			catch (Exception ex)
			{
				try
				{
					siavCardManager.InsertNote(oCardBundlePvalMassive, siavLogin, "Sono stati riscontrati degli errori, prego informare gli amministratori di sistema.", resourceFileManager.getConfigData("AutorAnnotation"));
					logger.Error(ex.ToString());
					if (siavCardManager != null)
					{
						siavCardManager.siavWsCard.Abort();
					}
					if (siavLogin != null)
					{
						siavLogin.Logout();
						siavLogin.siavWsLogin.Close();
					}
					sResult = "";
				}
				catch (Exception e) { }
			}
			finally
			{

				logger.Debug("Fine elaborazione");
				//siavLogin.Logout();
			}
			return sResult;
		}
		public String GetBiggerCardsReadyToProtocolProvaValutativa(string IdComMaxiva, bool withState)
		{
			WcfSiavLoginManager siavLogin = null;
			WcfSiavCardManager siavCardManager = null;
			string sResult = "";
			try
			{
				List<Guid> lGUID;
				siavLogin = new WcfSiavLoginManager();
				string oUserName = resourceFileManager.getConfigData("UserFlux");
				string oUserPwd = oRijndael.Decrypt(resourceFileManager.getConfigData("PasswordFlux"));

				// Esegue il login
				siavLogin.Login(oUserName, oUserPwd);
				logger.Debug("Sessione login Archiflow: " + siavLogin.oSessionInfo.SessionId);

				siavCardManager = new WcfSiavCardManager(logger);

				List<SchedaSearchParameter> lSchedaSearchParameter = new List<SchedaSearchParameter>();

				List<string> lMetadati;
				SchedaSearchParameter schedaSearchParameter = new SchedaSearchParameter();
				schedaSearchParameter.Archivio = resourceFileManager.getConfigData("PvalArcCardInError");
				schedaSearchParameter.TipologiaDocumentale = resourceFileManager.getConfigData("PvalDocTypeCardInError");
				string[] arr = new string[20];
				arr[int.Parse(resourceFileManager.getConfigData("PvalIndexFieldCardInError"))] = IdComMaxiva;
				if (withState)
					arr[int.Parse(resourceFileManager.getConfigData("PvalIndexStatoCardInError"))] = resourceFileManager.getConfigData("PvalValueStatoCardInReadyToProt");

				logger.Debug("Inizio ricerca record");
				logger.Debug("  Cerco per Archivio: " + resourceFileManager.getConfigData("PvalArcCardInError"));
				logger.Debug("  Cerco per TipologiaDocumentale: " + resourceFileManager.getConfigData("PvalDocTypeCardInError"));
				logger.Debug("  Cerco per ID comunicazione massiva: " + IdComMaxiva);
				if (withState)
					logger.Debug("  Cerco per STATO: " + resourceFileManager.getConfigData("PvalValueStatoCardInReadyToProt"));

				logger.Debug("Fine ricerca record");

				lMetadati = new List<string>(arr);
				schedaSearchParameter.MetaDati = lMetadati;
				lSchedaSearchParameter.Add(schedaSearchParameter);
				siavCardManager.getSearch(lSchedaSearchParameter, siavLogin, out lGUID);
				//lGUID.Sort();
				if (lGUID.Count > 0)
				{
					List<int> lOnlyNumericPartGuid = new List<int>();
					foreach (Guid sSingleValue in lGUID)
					{
						lOnlyNumericPartGuid.Add(int.Parse(sSingleValue.ToString().Substring(25)));
					}
					lOnlyNumericPartGuid.Reverse();
					sResult = lOnlyNumericPartGuid[0].ToString();
				}
				siavLogin.Logout();
				siavLogin.siavWsLogin.Close();
				siavLogin = null;
				siavCardManager.siavWsCard.Close();
				siavCardManager = null;
			}
			catch (Exception ex)
			{
				try
				{
					logger.Error(ex.ToString());
					if (siavCardManager != null)
					{
						siavCardManager.siavWsCard.Abort();
					}
					if (siavLogin != null)
					{
						siavLogin.Logout();
						siavLogin.siavWsLogin.Close();
					}
				}
				catch (Exception e) { }
			}
			finally
			{
				logger.Debug("Fine elaborazione");
				//siavLogin.Logout();
			}
			return sResult;
		}

		public String GetBiggerCardsReadyToProtocolCancellazione(string IdComMaxiva, bool withState)
		{
			WcfSiavLoginManager siavLogin = null;
			WcfSiavCardManager siavCardManager = null;
			string sResult = "";
			try
			{
				List<Guid> lGUID;
				siavLogin = new WcfSiavLoginManager();
				string oUserName = resourceFileManager.getConfigData("UserFlux");
				string oUserPwd = oRijndael.Decrypt(resourceFileManager.getConfigData("PasswordFlux"));

				// Esegue il login
				siavLogin.Login(oUserName, oUserPwd);
				logger.Debug("Sessione login Archiflow: " + siavLogin.oSessionInfo.SessionId);

				siavCardManager = new WcfSiavCardManager(logger);

				List<SchedaSearchParameter> lSchedaSearchParameter = new List<SchedaSearchParameter>();

				List<string> lMetadati;
				SchedaSearchParameter schedaSearchParameter = new SchedaSearchParameter();
				schedaSearchParameter.Archivio = resourceFileManager.getConfigData("CancArcCardInError");
				schedaSearchParameter.TipologiaDocumentale = resourceFileManager.getConfigData("CancDocTypeCardInError");
				string[] arr = new string[20];
				arr[int.Parse(resourceFileManager.getConfigData("CancIndexFieldCardInError"))] = IdComMaxiva;
				if (withState)
					arr[int.Parse(resourceFileManager.getConfigData("CancIndexStatoCardInError"))] = resourceFileManager.getConfigData("CancValueStatoCardInReadyToProt");

				logger.Debug("Inizio ricerca record");
				logger.Debug("  Cerco per Archivio: " + resourceFileManager.getConfigData("CancArcCardInError"));
				logger.Debug("  Cerco per TipologiaDocumentale: " + resourceFileManager.getConfigData("CancDocTypeCardInError"));
				logger.Debug("  Cerco per ID comunicazione massiva: " + IdComMaxiva);
				if (withState)
					logger.Debug("  Cerco per STATO: " + resourceFileManager.getConfigData("CancValueStatoCardInReadyToProt"));

				logger.Debug("Fine ricerca record");

				lMetadati = new List<string>(arr);
				schedaSearchParameter.MetaDati = lMetadati;
				lSchedaSearchParameter.Add(schedaSearchParameter);
				siavCardManager.getSearch(lSchedaSearchParameter, siavLogin, out lGUID);
				//lGUID.Sort();
				if (lGUID.Count > 0)
				{
					List<int> lOnlyNumericPartGuid = new List<int>();
					foreach (Guid sSingleValue in lGUID)
					{
						lOnlyNumericPartGuid.Add(int.Parse(sSingleValue.ToString().Substring(25)));
					}
					lOnlyNumericPartGuid.Reverse();
					sResult = lOnlyNumericPartGuid[0].ToString();
				}
				siavLogin.Logout();
				siavLogin.siavWsLogin.Close();
				siavLogin = null;
				siavCardManager.siavWsCard.Close();
				siavCardManager = null;
			}
			catch (Exception ex)
			{
				try
				{
					logger.Error(ex.ToString());
					if (siavCardManager != null)
					{
						siavCardManager.siavWsCard.Abort();
					}
					if (siavLogin != null)
					{
						siavLogin.Logout();
						siavLogin.siavWsLogin.Close();
					}
				}
				catch (Exception e) { }
			}
			finally
			{
				logger.Debug("Fine elaborazione");
				//siavLogin.Logout();
			}
			return sResult;
		}
		public String GetLowerCardsReadyToProtocolCancellazione(string IdComMaxiva)
		{
			WcfSiavLoginManager siavLogin = null;
			WcfSiavCardManager siavCardManager = null;
			string sResult = "";
			try
			{
				List<Guid> lGUID;
				siavLogin = new WcfSiavLoginManager();
				string oUserName = resourceFileManager.getConfigData("UserFlux");
				string oUserPwd = oRijndael.Decrypt(resourceFileManager.getConfigData("PasswordFlux"));

				// Esegue il login
				siavLogin.Login(oUserName, oUserPwd);
				logger.Debug("Sessione login Archiflow: " + siavLogin.oSessionInfo.SessionId);

				siavCardManager = new WcfSiavCardManager(logger);

				List<SchedaSearchParameter> lSchedaSearchParameter = new List<SchedaSearchParameter>();

				List<string> lMetadati;
				SchedaSearchParameter schedaSearchParameter = new SchedaSearchParameter();
				schedaSearchParameter.Archivio = resourceFileManager.getConfigData("CancArcCardInError");
				schedaSearchParameter.TipologiaDocumentale = resourceFileManager.getConfigData("CancDocTypeCardInError");
				string[] arr = new string[20];
				arr[int.Parse(resourceFileManager.getConfigData("CancIndexFieldCardInError"))] = IdComMaxiva;
				arr[int.Parse(resourceFileManager.getConfigData("CancIndexStatoCardInError"))] = resourceFileManager.getConfigData("CancValueStatoCardInReadyToProt");

				logger.Debug("Inizio ricerca record");
				logger.Debug("  Cerco per Archivio: " + resourceFileManager.getConfigData("CancArcCardInError"));
				logger.Debug("  Cerco per TipologiaDocumentale: " + resourceFileManager.getConfigData("CancDocTypeCardInError"));
				logger.Debug("  Cerco per ID comunicazione massiva: " + IdComMaxiva);
				logger.Debug("  Cerco per STATO: " + resourceFileManager.getConfigData("CancValueStatoCardInReadyToProt"));

				logger.Debug("Fine ricerca record");

				lMetadati = new List<string>(arr);
				schedaSearchParameter.MetaDati = lMetadati;
				lSchedaSearchParameter.Add(schedaSearchParameter);
				siavCardManager.getSearch(lSchedaSearchParameter, siavLogin, out lGUID);
				//lGUID.Sort();
				if (lGUID.Count > 0)
				{
					List<int> lOnlyNumericPartGuid = new List<int>();
					foreach (Guid sSingleValue in lGUID)
					{
						lOnlyNumericPartGuid.Add(int.Parse(sSingleValue.ToString().Substring(25)));
					}
					lOnlyNumericPartGuid.Sort();
					sResult = lOnlyNumericPartGuid[0].ToString();
				}
				siavLogin.Logout();
				siavLogin.siavWsLogin.Close();
				siavLogin = null;
				siavCardManager.siavWsCard.Close();
				siavCardManager = null;
			}
			catch (Exception ex)
			{
				try
				{
					logger.Error(ex.ToString());
					if (siavCardManager != null)
					{
						siavCardManager.siavWsCard.Abort();
					}
					if (siavLogin != null)
					{
						siavLogin.Logout();
						siavLogin.siavWsLogin.Close();
					}
				}
				catch (Exception e) { }
			}
			finally
			{
				logger.Debug("Fine elaborazione");
				//siavLogin.Logout();
			}
			return sResult;
		}
		public String GetLowerCardsReadyToProtocol(string IdComMaxiva)
		{
			WcfSiavLoginManager siavLogin = null;
			WcfSiavCardManager siavCardManager = null;
			string sResult = "";
			try
			{
				List<Guid> lGUID;
				siavLogin = new WcfSiavLoginManager();
				string oUserName = resourceFileManager.getConfigData("UserFlux");
				string oUserPwd = oRijndael.Decrypt(resourceFileManager.getConfigData("PasswordFlux"));

				// Esegue il login
				siavLogin.Login(oUserName, oUserPwd);
				logger.Debug("Sessione login Archiflow: " + siavLogin.oSessionInfo.SessionId);

				siavCardManager = new WcfSiavCardManager(logger);

				List<SchedaSearchParameter> lSchedaSearchParameter = new List<SchedaSearchParameter>();

				List<string> lMetadati;
				SchedaSearchParameter schedaSearchParameter = new SchedaSearchParameter();
				schedaSearchParameter.Archivio = resourceFileManager.getConfigData("IscArcCardInError");
				schedaSearchParameter.TipologiaDocumentale = resourceFileManager.getConfigData("IscDocTypeCardInError");
				string[] arr = new string[20];
				arr[int.Parse(resourceFileManager.getConfigData("IscIndexFieldCardInError"))] = IdComMaxiva;
				arr[int.Parse(resourceFileManager.getConfigData("IscIndexStatoCardInError"))] = resourceFileManager.getConfigData("IscValueStatoCardInReadyToProt");

				logger.Debug("Inizio ricerca record");
				logger.Debug("  Cerco per Archivio: " + resourceFileManager.getConfigData("IscArcCardInError"));
				logger.Debug("  Cerco per TipologiaDocumentale: " + resourceFileManager.getConfigData("IscDocTypeCardInError"));
				logger.Debug("  Cerco per ID comunicazione massiva: " + IdComMaxiva);
				logger.Debug("  Cerco per STATO: " + resourceFileManager.getConfigData("IscValueStatoCardInReadyToProt"));

				logger.Debug("Fine ricerca record");

				lMetadati = new List<string>(arr);
				schedaSearchParameter.MetaDati = lMetadati;
				lSchedaSearchParameter.Add(schedaSearchParameter);
				siavCardManager.getSearch(lSchedaSearchParameter, siavLogin, out lGUID);
				//lGUID.Sort();
				if (lGUID.Count > 0)
				{
					List<int> lOnlyNumericPartGuid = new List<int>();
					foreach (Guid sSingleValue in lGUID)
					{
						lOnlyNumericPartGuid.Add(int.Parse(sSingleValue.ToString().Substring(25)));
					}
					lOnlyNumericPartGuid.Sort();
					sResult = lOnlyNumericPartGuid[0].ToString();
				}
				siavLogin.Logout();
				siavLogin.siavWsLogin.Close();
				siavLogin = null;
				siavCardManager.siavWsCard.Close();
				siavCardManager = null;
			}
			catch (Exception ex)
			{
				try
				{
					logger.Error(ex.ToString());
					if (siavCardManager != null)
					{
						siavCardManager.siavWsCard.Abort();
					}
					if (siavLogin != null)
					{
						siavLogin.Logout();
						siavLogin.siavWsLogin.Close();
					}
				}
				catch (Exception e) { }
			}
			finally
			{
				logger.Debug("Fine elaborazione");
				//siavLogin.Logout();
			}
			return sResult;
		}
		public String GetBiggerCardsReadyToProtocol(string IdComMaxiva, bool withState)
		{
			WcfSiavLoginManager siavLogin = null;
			WcfSiavCardManager siavCardManager = null;
			string sResult = "";
			try
			{
				List<Guid> lGUID;
				siavLogin = new WcfSiavLoginManager();
				string oUserName = resourceFileManager.getConfigData("UserFlux");
				string oUserPwd = oRijndael.Decrypt(resourceFileManager.getConfigData("PasswordFlux"));

				// Esegue il login
				siavLogin.Login(oUserName, oUserPwd);
				logger.Debug("Sessione login Archiflow: " + siavLogin.oSessionInfo.SessionId);

				siavCardManager = new WcfSiavCardManager(logger);

				List<SchedaSearchParameter> lSchedaSearchParameter = new List<SchedaSearchParameter>();

				List<string> lMetadati;
				SchedaSearchParameter schedaSearchParameter = new SchedaSearchParameter();
				schedaSearchParameter.Archivio = resourceFileManager.getConfigData("IscArcCardInError");
				schedaSearchParameter.TipologiaDocumentale = resourceFileManager.getConfigData("IscDocTypeCardInError");
				string[] arr = new string[20];
				arr[int.Parse(resourceFileManager.getConfigData("IscIndexFieldCardInError"))] = IdComMaxiva;
				if (withState)
					arr[int.Parse(resourceFileManager.getConfigData("IscIndexStatoCardInError"))] = resourceFileManager.getConfigData("IscValueStatoCardInReadyToProt");

				logger.Debug("Inizio ricerca record");
				logger.Debug("  Cerco per Archivio: " + resourceFileManager.getConfigData("IscArcCardInError"));
				logger.Debug("  Cerco per TipologiaDocumentale: " + resourceFileManager.getConfigData("IscDocTypeCardInError"));
				logger.Debug("  Cerco per ID comunicazione massiva: " + IdComMaxiva);
				if (withState)
					logger.Debug("  Cerco per STATO: " + resourceFileManager.getConfigData("IscValueStatoCardInReadyToProt"));

				logger.Debug("Fine ricerca record");

				lMetadati = new List<string>(arr);
				schedaSearchParameter.MetaDati = lMetadati;
				lSchedaSearchParameter.Add(schedaSearchParameter);
				siavCardManager.getSearch(lSchedaSearchParameter, siavLogin, out lGUID);
				//lGUID.Sort();
				if (lGUID.Count > 0)
				{
					List<int> lOnlyNumericPartGuid = new List<int>();
					foreach (Guid sSingleValue in lGUID)
					{
						lOnlyNumericPartGuid.Add(int.Parse(sSingleValue.ToString().Substring(25)));
					}
					lOnlyNumericPartGuid.Reverse();
					sResult = lOnlyNumericPartGuid[0].ToString();
				}
				siavLogin.Logout();
				siavLogin.siavWsLogin.Close();
				siavLogin = null;
				siavCardManager.siavWsCard.Close();
				siavCardManager = null;
			}
			catch (Exception ex)
			{
				try
				{
					logger.Error(ex.ToString());
					if (siavCardManager != null)
					{
						siavCardManager.siavWsCard.Abort();
					}
					if (siavLogin != null)
					{
						siavLogin.Logout();
						siavLogin.siavWsLogin.Close();
					}
				}
				catch (Exception e) { }
			}
			finally
			{
				logger.Debug("Fine elaborazione");
				//siavLogin.Logout();
			}
			return sResult;
		}
		public String GetBiggerCardsReadyToProtocolIngiunzione(string IdComMaxiva, bool withState)
		{
			WcfSiavLoginManager siavLogin = null;
			WcfSiavCardManager siavCardManager = null;
			string sResult = "";
			try
			{
				List<Guid> lGUID;
				siavLogin = new WcfSiavLoginManager();
				string oUserName = resourceFileManager.getConfigData("UserFlux");
				string oUserPwd = oRijndael.Decrypt(resourceFileManager.getConfigData("PasswordFlux"));

				// Esegue il login
				siavLogin.Login(oUserName, oUserPwd);
				logger.Debug("Sessione login Archiflow: " + siavLogin.oSessionInfo.SessionId);

				siavCardManager = new WcfSiavCardManager(logger);

				List<SchedaSearchParameter> lSchedaSearchParameter = new List<SchedaSearchParameter>();

				List<string> lMetadati;
				SchedaSearchParameter schedaSearchParameter = new SchedaSearchParameter();
				schedaSearchParameter.Archivio = resourceFileManager.getConfigData("IngArcCardInError");
				schedaSearchParameter.TipologiaDocumentale = resourceFileManager.getConfigData("IngDocTypeCardInError");
				string[] arr = new string[20];
				arr[int.Parse(resourceFileManager.getConfigData("IngIndexFieldCardInError"))] = IdComMaxiva;
				if (withState)
					arr[int.Parse(resourceFileManager.getConfigData("IngIndexStatoCardInError"))] = resourceFileManager.getConfigData("IngValueStatoCardInReadyToProt");

				logger.Debug("Inizio ricerca record");
				logger.Debug("  Cerco per Archivio: " + resourceFileManager.getConfigData("IngArcCardInError"));
				logger.Debug("  Cerco per TipologiaDocumentale: " + resourceFileManager.getConfigData("IngDocTypeCardInError"));
				logger.Debug("  Cerco per ID comunicazione massiva: " + IdComMaxiva);
				if (withState)
					logger.Debug("  Cerco per STATO: " + resourceFileManager.getConfigData("IngValueStatoCardInReadyToProt"));

				logger.Debug("Fine ricerca record");

				lMetadati = new List<string>(arr);
				schedaSearchParameter.MetaDati = lMetadati;
				lSchedaSearchParameter.Add(schedaSearchParameter);
				siavCardManager.getSearch(lSchedaSearchParameter, siavLogin, out lGUID);
				//lGUID.Sort();
				if (lGUID.Count > 0)
				{
					List<int> lOnlyNumericPartGuid = new List<int>();
					foreach (Guid sSingleValue in lGUID)
					{
						lOnlyNumericPartGuid.Add(int.Parse(sSingleValue.ToString().Substring(25)));
					}
					lOnlyNumericPartGuid.Reverse();
					sResult = lOnlyNumericPartGuid[0].ToString();
				}
				siavLogin.Logout();
				siavLogin.siavWsLogin.Close();
				siavLogin = null;
				siavCardManager.siavWsCard.Close();
				siavCardManager = null;
			}
			catch (Exception ex)
			{
				try
				{
					logger.Error(ex.ToString());
					if (siavCardManager != null)
					{
						siavCardManager.siavWsCard.Abort();
					}
					if (siavLogin != null)
					{
						siavLogin.Logout();
						siavLogin.siavWsLogin.Close();
					}
				}
				catch (Exception e) { }
			}
			finally
			{
				logger.Debug("Fine elaborazione");
				//siavLogin.Logout();
			}
			return sResult;
		}
		public String GetLowerCardsReadyToProtocolProvaValutativa(string IdComMaxiva)
		{
			WcfSiavLoginManager siavLogin = null;
			WcfSiavCardManager siavCardManager = null;
			string sResult = "";
			try
			{
				List<Guid> lGUID;
				siavLogin = new WcfSiavLoginManager();
				string oUserName = resourceFileManager.getConfigData("UserFlux");
				string oUserPwd = oRijndael.Decrypt(resourceFileManager.getConfigData("PasswordFlux"));

				// Esegue il login
				siavLogin.Login(oUserName, oUserPwd);
				logger.Debug("Sessione login Archiflow: " + siavLogin.oSessionInfo.SessionId);

				siavCardManager = new WcfSiavCardManager(logger);

				List<SchedaSearchParameter> lSchedaSearchParameter = new List<SchedaSearchParameter>();

				List<string> lMetadati;
				SchedaSearchParameter schedaSearchParameter = new SchedaSearchParameter();
				schedaSearchParameter.Archivio = resourceFileManager.getConfigData("PvalArcCardInError");
				schedaSearchParameter.TipologiaDocumentale = resourceFileManager.getConfigData("PvalDocTypeCardInError");
				string[] arr = new string[20];
				arr[int.Parse(resourceFileManager.getConfigData("PvalIndexFieldCardInError"))] = IdComMaxiva;
				arr[int.Parse(resourceFileManager.getConfigData("PvalIndexStatoCardInError"))] = "";
				logger.Debug("Inizio ricerca record");
				logger.Debug("  Cerco per Archivio: " + resourceFileManager.getConfigData("PvalArcCardInError"));
				logger.Debug("  Cerco per TipologiaDocumentale: " + resourceFileManager.getConfigData("PvalDocTypeCardInError"));
				logger.Debug("  Cerco per ID comunicazione massiva: " + IdComMaxiva);
				logger.Debug("  Cerco per STATO: ");

				logger.Debug("Fine ricerca record");

				lMetadati = new List<string>(arr);
				schedaSearchParameter.MetaDati = lMetadati;
				lSchedaSearchParameter.Add(schedaSearchParameter);
				siavCardManager.getSearch(lSchedaSearchParameter, siavLogin, out lGUID);
				//lGUID.Sort();
				if (lGUID.Count > 0)
				{
					List<int> lOnlyNumericPartGuid = new List<int>();
					foreach (Guid sSingleValue in lGUID)
					{
						lOnlyNumericPartGuid.Add(int.Parse(sSingleValue.ToString().Substring(25)));
					}
					lOnlyNumericPartGuid.Sort();
					sResult = lOnlyNumericPartGuid[0].ToString();
				}
				siavLogin.Logout();
				siavLogin.siavWsLogin.Close();
				siavLogin = null;
				siavCardManager.siavWsCard.Close();
				siavCardManager = null;
			}
			catch (Exception ex)
			{
				try
				{
					logger.Error(ex.ToString());
					if (siavCardManager != null)
					{
						siavCardManager.siavWsCard.Abort();
					}
					if (siavLogin != null)
					{
						siavLogin.Logout();
						siavLogin.siavWsLogin.Close();
					}
				}
				catch (Exception e) { }
			}
			finally
			{
				logger.Debug("Fine elaborazione");
				//siavLogin.Logout();
			}
			return sResult;
		}
		public Boolean CheckProtocolCard(string IdComMaxiva)
		{
			WcfSiavLoginManager siavLogin = null;
			WcfSiavCardManager siavCardManager = null;
			bool bResult = false;
			try
			{
				List<Guid> lGUID;
				siavLogin = new WcfSiavLoginManager();
				string oUserName = resourceFileManager.getConfigData("UserFlux");
				string oUserPwd = oRijndael.Decrypt(resourceFileManager.getConfigData("PasswordFlux"));

				// Esegue il login
				siavLogin.Login(oUserName, oUserPwd);
				logger.Debug("Sessione login Archiflow: " + siavLogin.oSessionInfo.SessionId);

				siavCardManager = new WcfSiavCardManager(logger);

				List<SchedaSearchParameter> lSchedaSearchParameter = new List<SchedaSearchParameter>();

				List<string> lMetadati;
				SchedaSearchParameter schedaSearchParameter = new SchedaSearchParameter();
				schedaSearchParameter.Archivio = resourceFileManager.getConfigData("IscArcCardInError");
				schedaSearchParameter.TipologiaDocumentale = resourceFileManager.getConfigData("IscDocTypeCardInError");
				string[] arr = new string[20];
				// ID comunicazione massiva
				arr[int.Parse(resourceFileManager.getConfigData("IscIndexFieldCardInError"))] = IdComMaxiva;
				// Stato
				arr[int.Parse(resourceFileManager.getConfigData("IscIndexStatoCardInProt"))] = resourceFileManager.getConfigData("IscValueStatoCardInProt");

				logger.Debug("Inizio ricerca record");
				logger.Debug("  Cerco per Archivio: " + resourceFileManager.getConfigData("IscArcCardInError"));
				logger.Debug("  Cerco per TipologiaDocumentale: " + resourceFileManager.getConfigData("IscDocTypeCardInError"));
				logger.Debug("  Cerco per ID comunicazione massiva: " + resourceFileManager.getConfigData("IscIndexFieldCardInError"));
				logger.Debug("  Cerco per STATO: " + resourceFileManager.getConfigData("IscValueStatoCardInProt"));
				logger.Debug("Fine ricerca record");

				lMetadati = new List<string>(arr);
				schedaSearchParameter.MetaDati = lMetadati;
				lSchedaSearchParameter.Add(schedaSearchParameter);
				siavCardManager.getSearch(lSchedaSearchParameter, siavLogin, out lGUID);
				if (lGUID.Count == 0)
				{
					bResult = true;
				}
				siavLogin.Logout();
				siavLogin.siavWsLogin.Close();
				siavLogin = null;
				siavCardManager.siavWsCard.Close();
				siavCardManager = null;
			}
			catch (Exception ex)
			{
				try
				{
					logger.Error(ex.ToString());
					if (siavCardManager != null)
					{
						siavCardManager.siavWsCard.Abort();
					}
					if (siavLogin != null)
					{
						siavLogin.Logout();
						siavLogin.siavWsLogin.Close();
					}
				}
				catch (Exception e) { }
			}
			finally
			{
				logger.Debug("Fine elaborazione");
				//siavLogin.Logout();
			}
			return bResult;
		}
		public Boolean CheckProtocolCardIngiunzione(string IdComMaxiva)
		{
			WcfSiavLoginManager siavLogin = null;
			WcfSiavCardManager siavCardManager = null;
			bool bResult = false;
			try
			{
				List<Guid> lGUID;
				siavLogin = new WcfSiavLoginManager();
				string oUserName = resourceFileManager.getConfigData("UserFlux");
				string oUserPwd = oRijndael.Decrypt(resourceFileManager.getConfigData("PasswordFlux"));

				// Esegue il login
				siavLogin.Login(oUserName, oUserPwd);
				logger.Debug("Sessione login Archiflow: " + siavLogin.oSessionInfo.SessionId);

				siavCardManager = new WcfSiavCardManager(logger);

				List<SchedaSearchParameter> lSchedaSearchParameter = new List<SchedaSearchParameter>();

				List<string> lMetadati;
				SchedaSearchParameter schedaSearchParameter = new SchedaSearchParameter();
				schedaSearchParameter.Archivio = resourceFileManager.getConfigData("IngArcCardInError");
				schedaSearchParameter.TipologiaDocumentale = resourceFileManager.getConfigData("IngDocTypeCardInError");
				string[] arr = new string[20];
				// ID comunicazione massiva
				arr[int.Parse(resourceFileManager.getConfigData("IngIndexFieldCardInError"))] = IdComMaxiva;
				// Stato
				arr[int.Parse(resourceFileManager.getConfigData("IngIndexStatoCardInProt"))] = resourceFileManager.getConfigData("IngValueStatoCardInProt");

				logger.Debug("Inizio ricerca record");
				logger.Debug("  Cerco per Archivio: " + resourceFileManager.getConfigData("IngArcCardInError"));
				logger.Debug("  Cerco per TipologiaDocumentale: " + resourceFileManager.getConfigData("IngDocTypeCardInError"));
				logger.Debug("  Cerco per ID comunicazione massiva: " + resourceFileManager.getConfigData("IngIndexFieldCardInError"));
				logger.Debug("  Cerco per STATO: " + resourceFileManager.getConfigData("IngValueStatoCardInProt"));
				logger.Debug("Fine ricerca record");

				lMetadati = new List<string>(arr);
				schedaSearchParameter.MetaDati = lMetadati;
				lSchedaSearchParameter.Add(schedaSearchParameter);
				siavCardManager.getSearch(lSchedaSearchParameter, siavLogin, out lGUID);
				if (lGUID.Count == 0)
				{
					bResult = true;
				}
				siavLogin.Logout();
				siavLogin.siavWsLogin.Close();
				siavLogin = null;
				siavCardManager.siavWsCard.Close();
				siavCardManager = null;
			}
			catch (Exception ex)
			{
				try
				{
					logger.Error(ex.ToString());
					if (siavCardManager != null)
					{
						siavCardManager.siavWsCard.Abort();
					}
					if (siavLogin != null)
					{
						siavLogin.Logout();
						siavLogin.siavWsLogin.Close();
					}
				}
				catch (Exception e) { }
			}
			finally
			{
				logger.Debug("Fine elaborazione");
				//siavLogin.Logout();
			}
			return bResult;
		}
		public Boolean CheckProtocolCardCancellazione(string IdComMaxiva)
		{
			WcfSiavLoginManager siavLogin = null;
			WcfSiavCardManager siavCardManager = null;
			bool bResult = false;
			try
			{
				List<Guid> lGUID;
				siavLogin = new WcfSiavLoginManager();
				string oUserName = resourceFileManager.getConfigData("UserFlux");
				string oUserPwd = oRijndael.Decrypt(resourceFileManager.getConfigData("PasswordFlux"));

				// Esegue il login
				siavLogin.Login(oUserName, oUserPwd);
				logger.Debug("Sessione login Archiflow: " + siavLogin.oSessionInfo.SessionId);

				siavCardManager = new WcfSiavCardManager(logger);

				List<SchedaSearchParameter> lSchedaSearchParameter = new List<SchedaSearchParameter>();

				List<string> lMetadati;
				SchedaSearchParameter schedaSearchParameter = new SchedaSearchParameter();
				schedaSearchParameter.Archivio = resourceFileManager.getConfigData("CancArcCardInError");
				schedaSearchParameter.TipologiaDocumentale = resourceFileManager.getConfigData("CancDocTypeCardInError");
				string[] arr = new string[20];
				// ID comunicazione massiva
				arr[int.Parse(resourceFileManager.getConfigData("CancIndexFieldCardInError"))] = IdComMaxiva;
				// Stato
				arr[int.Parse(resourceFileManager.getConfigData("CancIndexStatoCardInProt"))] = resourceFileManager.getConfigData("CancValueStatoCardInProt");

				logger.Debug("Inizio ricerca record");
				logger.Debug("  Cerco per Archivio: " + resourceFileManager.getConfigData("CancArcCardInError"));
				logger.Debug("  Cerco per TipologiaDocumentale: " + resourceFileManager.getConfigData("CancDocTypeCardInError"));
				logger.Debug("  Cerco per ID comunicazione massiva: " + resourceFileManager.getConfigData("CancIndexFieldCardInError"));
				logger.Debug("  Cerco per STATO: " + resourceFileManager.getConfigData("CancValueStatoCardInProt"));
				logger.Debug("Fine ricerca record");

				lMetadati = new List<string>(arr);
				schedaSearchParameter.MetaDati = lMetadati;
				lSchedaSearchParameter.Add(schedaSearchParameter);
				siavCardManager.getSearch(lSchedaSearchParameter, siavLogin, out lGUID);
				if (lGUID.Count == 0)
				{
					bResult = true;
				}
				siavLogin.Logout();
				siavLogin.siavWsLogin.Close();
				siavLogin = null;
				siavCardManager.siavWsCard.Close();
				siavCardManager = null;
			}
			catch (Exception ex)
			{
				try
				{
					logger.Error(ex.ToString());
					if (siavCardManager != null)
					{
						siavCardManager.siavWsCard.Abort();
					}
					if (siavLogin != null)
					{
						siavLogin.Logout();
						siavLogin.siavWsLogin.Close();
					}
				}
				catch (Exception e) { }
			}
			finally
			{
				logger.Debug("Fine elaborazione");
				//siavLogin.Logout();
			}
			return bResult;
		}
		public int GetNumberSigns(string sGuidCard)
		{
			CardBundle oCardBundle = null;
			WcfSiavLoginManager siavLogin = null;
			WcfSiavCardManager siavCardManager = null;
			MainDoc oMainDoc = new MainDoc();
			int iResult = 0;
			try
			{
				siavLogin = new WcfSiavLoginManager();
				string oUserName = resourceFileManager.getConfigData("UserFlux");
				string oUserPwd = oRijndael.Decrypt(resourceFileManager.getConfigData("PasswordFlux"));

				// Esegue il login
				siavLogin.Login(oUserName, oUserPwd);
				logger.Debug("Sessione login Archiflow: " + siavLogin.oSessionInfo.SessionId);
				siavCardManager = new WcfSiavCardManager(logger);
				siavCardManager.GetCard(sGuidCard, siavLogin, out oCardBundle);
				logger.Debug("Scheda trovata: " + oCardBundle.CardId);
				if (oCardBundle != null)
				{
					siavCardManager.GetMainDoc(oCardBundle, out oMainDoc);
					logger.Debug("Maindoc trovato");
					string sPAthPdf = this.sWorkingFolder + @"\" + oMainDoc.Filename + "_countCert." + oMainDoc.Extension;
					logger.Debug("Scrivo il documento principale: " + sPAthPdf);
					FluxHelper oFluxHelper = new FluxHelper();
					oFluxHelper.FileMaterialize(sPAthPdf, oMainDoc.oByte);
					foreach (var cert in oFluxHelper.EnumPdfSigners(sPAthPdf))
					{
						logger.Debug("Serial Number: " + cert.SerialNumber);
						logger.Debug("Nome: " + cert.SubjectName.Format(true));
						iResult++;
					}

				}
			}
			catch (Exception ex)
			{
				try
				{
					logger.Debug(String.Format("{0}>>{1}>>{2}", "ERRORE : GetNumberSigns", ex.Source, ex.Message, ex.StackTrace), ex);
					if (siavCardManager != null)
					{
						siavCardManager.siavWsCard.Abort();
					}
					if (siavLogin != null)
					{
						siavLogin.Logout();
						siavLogin.siavWsLogin.Close();
					}
				}
				catch (Exception e) { }
			}
			finally
			{
				logger.Debug("Fine elaborazione");
				//siavLogin.Logout();
			}
			return iResult;
		}
		public string GetNumberSignsOnAttachment(string sGuidCard)
		{
			WcfSiavLoginManager siavLogin = null;
			string sResult = "";
			WcfSiavCardManager siavCardManager = null;
			List<string> oAttachments = new List<string>();
			try
			{
				siavLogin = new WcfSiavLoginManager();
				string oUserName = resourceFileManager.getConfigData("UserFlux");
				string oUserPwd = oRijndael.Decrypt(resourceFileManager.getConfigData("PasswordFlux"));

				// Esegue il login
				siavLogin.Login(oUserName, oUserPwd);
				logger.Debug("Sessione login Archiflow: " + siavLogin.oSessionInfo.SessionId);
				siavCardManager = new WcfSiavCardManager(logger);
				siavCardManager.GetCardAttachments2Mod(this.sWorkingFolder, sGuidCard, siavLogin, out oAttachments);
				foreach (string oAttachment in oAttachments)
				{
					if (oAttachment.ToUpper().IndexOf("|" + resourceFileManager.getConfigData("checkAttachSigned")) != -1)
					{

						string sPAthPdf = oAttachment.Substring(0, oAttachment.IndexOf("|"));
						logger.Debug("Scrivo il documento principale: " + sPAthPdf);
						FluxHelper oFluxHelper = new FluxHelper();
						int iResult = 0;
						foreach (var cert in oFluxHelper.EnumPdfSigners(sPAthPdf))
						{
							logger.Debug("Serial Number: " + cert.SerialNumber);
							logger.Debug("Nome: " + cert.SubjectName.Format(true));
							iResult++;
						}
						sResult += sPAthPdf + "|" + iResult + "|";
						logger.Debug("Il risultato attuale è: " + sResult);
					}
				}
			}
			catch (Exception ex)
			{
				try
				{
					logger.Debug(String.Format("{0}>>{1}>>{2}", "ERRORE : GetNumberSignsOnAttachment", ex.Source, ex.Message, ex.StackTrace), ex);
					if (siavCardManager != null)
					{
						siavCardManager.siavWsCard.Abort();
					}
					if (siavLogin != null)
					{
						siavLogin.Logout();
						siavLogin.siavWsLogin.Close();
					}
				}
				catch (Exception e) { }
			}
			finally
			{
				logger.Debug("Fine elaborazione");
				//siavLogin.Logout();
			}
			return sResult;
		}
		public bool SetCardDefaultVisibility(string sQuery)
		{
			string sConnection = "";
			CardManager oCardManager = new CardManager(logger);
			string oUserName = resourceFileManager.getConfigData("UserFlux");
			string oUserPwd = oRijndael.Decrypt(resourceFileManager.getConfigData("PasswordFlux"));
			ConnectionManager connectionManager = new ConnectionManager(logger);

			bool bResult = false;
			try
			{
				QueryDataForReport oQueryDataForReport = new QueryDataForReport();
				sConnection = connectionManager.OpenUserConnect(oUserName, oUserPwd);

				var result = oQueryDataForReport.getDataForReport(null, sQuery);
				if (result != null)
				{
					logger.Debug("Individuati:" + result.Count + " record.");
					if (result.Count > 0)
					{
						List<string> listKeys = new List<string>(result[0].Keys);
						for (int iRow = 0; iRow < result.Count; iRow++)
						{
							for (int iColumn = 0; iColumn < listKeys.Count; iColumn++)
							{
								List<string> listValue = new List<string>(result[iRow].Values);
								logger.Debug("Scheda da elaborare: " + listValue[iColumn].ToString());
								oCardManager.setCardVisibilityDefaultConnLess(sConnection, listValue[iColumn].ToString());
								logger.Debug("Scheda elaborata: " + listValue[iColumn].ToString());
							}
						}
						bResult = true;
					}
					else
					{
						logger.Debug("Non è stato trovato alcun valore");
					}
				}
				connectionManager.CloseConnect();
			}
			catch (Exception ex)
			{
				try
				{
					if (sConnection != "")
						connectionManager.CloseConnect();
				}
				catch (Exception e) { }
			}
			finally
			{
				logger.Debug("Fine elaborazione");
				//siavLogin.Logout();
			}
			return bResult;
		}
		public int ProtocolBillFromPEC(string sGuidCard, out string sOutput)
		{
			sOutput = "";
			WcfSiavLoginManager siavLogin = null;
			int iResult = 0;
			WcfSiavCardManager siavCardManager = null;
			List<string> oAttachments = new List<string>();
			CardBundle oCardBundle = new CardBundle();
			MainDoc oMainDoc = new MainDoc();
			CardBundle oCardBundleAttach = new CardBundle();
			AgrafCard svAgrafCardRG = new AgrafCard();
			svAgrafCardRG.CardContacts = new List<AgrafCardContact>();
			AgrafCard svAgrafCardRF = new AgrafCard();
			svAgrafCardRF.CardContacts = new List<AgrafCardContact>();

			string[] oMetadati = new string[20];
			string versione = string.Empty;

			try
			{
				// Metadato Nota fattura elettronica
				oMetadati[8] = resourceFileManager.getConfigData("NoteFatturaElettronica");
				oMetadati[1] = resourceFileManager.getConfigData("UffDestFatturaElettronica");
				oMetadati[2] = resourceFileManager.getConfigData("MezzoSpedFatturazioneElettronica");

				// Verifica Casella PEC di provenienza sia uguale a quella censita nel file di risorse
				string sEmailBillPEC = resourceFileManager.getConfigData("EmailBillPec");

				siavLogin = new WcfSiavLoginManager();
				string oUserName = resourceFileManager.getConfigData("UserFlux");
				string oUserPwd = oRijndael.Decrypt(resourceFileManager.getConfigData("PasswordFlux"));

				// Esegue il login
				siavLogin.Login(oUserName, oUserPwd);
				logger.Debug("Sessione login Archiflow: " + siavLogin.oSessionInfo.SessionId);
				siavCardManager = new WcfSiavCardManager(logger);
				siavCardManager.GetCard(sGuidCard, siavLogin, out oCardBundle);
				logger.Debug("Scheda trovata: " + oCardBundle.CardId);
				FluxHelper oFluxHelper = new FluxHelper();
				if (oCardBundle != null)
				{
					logger.Debug("Scarico il documento principale della scheda: " + oCardBundle.CardId);
					siavCardManager.GetMainDoc(oCardBundle, out oMainDoc);
					logger.Debug("Maindoc trovato: " + oMainDoc.Filename);
					// Materializza il file sul filesystem
					string sBillFile = this.sWorkingFolder + @"\" + oMainDoc.Filename + "." + oMainDoc.Extension;
					logger.Debug("File materializzato: " + sBillFile);
					oFluxHelper.FileMaterialize(sBillFile, oMainDoc.oByte);
					// Leggo i dati della fattura elettronica
					logger.Debug("Leggo la fattura elettronica");

					//da verificare, leggere anche la versione
					FatturaElettronica.Ordinaria.FatturaOrdinaria fattura = new FatturaElettronica.Ordinaria.FatturaOrdinaria();
					var soXmlReadeSetting = new XmlReaderSettings { IgnoreWhitespace = true };
					var oXmlReader = XmlReader.Create(sBillFile, soXmlReadeSetting);
					fattura.ReadXml(oXmlReader);
					//prendo la versione della fattura elettronica
					versione = FatturaElettronica.Defaults.Versione.Trasmissione.ToString();
					logger.Debug("Leggo i dati del cedente prestatore");
					var oCedentePrestatore = fattura.FatturaElettronicaHeader.CedentePrestatore;
					var personsDump = ObjectDumper.Dump(oCedentePrestatore, DumpStyle.Console);
					string sPiva = oCedentePrestatore.DatiAnagrafici.IdFiscaleIVA.IdCodice;
					string sCodiceFiscale = oCedentePrestatore.DatiAnagrafici.CodiceFiscale;
					string sNumFattura = "";

					logger.Debug("Ho letto la partita iva: " + sPiva);
					logger.Debug("Ho letto il codice fiscale: " + sCodiceFiscale);
					foreach (var doc in fattura.FatturaElettronicaBody)
					{
						var datiDocumento = doc.DatiGenerali.DatiGeneraliDocumento;
						sNumFattura += $"Numero Fattura: {datiDocumento.Numero}" + " " + $"Data: {datiDocumento.Data.ToShortDateString()}" + System.Environment.NewLine;
					}
					sNumFattura = sNumFattura.Substring(0, sNumFattura.Length - 2);
					logger.Debug("Oggetto scheda: " + sNumFattura);

					string[] stringSeparators = new string[] { "\r\n" };
					string[] lines = personsDump.Split(stringSeparators, StringSplitOptions.None);
					string sBillMessage = "Progressivo assoluto scheda documentale: " + oMainDoc.Filename + System.Environment.NewLine;
					string sMessageCheckAgraf = "";
					string sValueBillOwner = sNumFattura + System.Environment.NewLine;
					foreach (string sLine in lines)
					{
						if (sLine.IndexOf(" null") == -1 && (sLine.IndexOf("{") == -1 && sLine.IndexOf("}") == -1))
						{
							sBillMessage += sLine.Trim() + System.Environment.NewLine;//But will print 3 lines in total.
							sValueBillOwner += sLine.Trim() + System.Environment.NewLine;
						}
					}
					logger.Debug("Messaggio fattura: " + sBillMessage);
					logger.Debug("Cerco tra gli allegati interni una PEC contentente una fattura");
					List<Siav.APFlibrary.Model.InternalAttachment> lPathAttachments = new List<Siav.APFlibrary.Model.InternalAttachment>();
					// cerco tra gli allegati circolari se presenti
					siavCardManager.GetCardInternalAttachments(sGuidCard, siavLogin, out lPathAttachments);
					if (lPathAttachments != null)
					{
						foreach (Siav.APFlibrary.Model.InternalAttachment singleInternalAttachment in lPathAttachments)
						{
							logger.Debug("Internal Attachment trovato: " + singleInternalAttachment.Name);
							if (singleInternalAttachment.ArchiveId != "")
							{
								string sNameTypeDocMail = resourceFileManager.getConfigData("MailIn");
								DocumentType oDocumentType = new DocumentType();
								siavCardManager.getTypeDoc(sNameTypeDocMail, siavLogin.oSessionInfo, out oDocumentType);
								if (oDocumentType.DocumentTypeId == oCardBundleAttach.DocumentTypeId)
								{
									logger.Debug("Allegato interno trovato: " + singleInternalAttachment.Name);
									siavCardManager.GetCard(singleInternalAttachment.Name, singleInternalAttachment.ArchiveId, siavLogin, out oCardBundleAttach);
								}
								break;
							}
						}
						// Leggo gli indici della scheda Mail In;
						NameValueCollection MailInIndexes;
						siavCardManager.GetIndexes(oCardBundleAttach, out MailInIndexes);
						logger.Debug("Ricerco la casella email: " + sEmailBillPEC.ToUpper().Trim() + " - " + MailInIndexes[resourceFileManager.getConfigData("MailIn_CasellaPostale")].ToUpper().Trim());

						if (sEmailBillPEC.ToUpper().Trim() == MailInIndexes[resourceFileManager.getConfigData("MailIn_CasellaPostale")].ToUpper().Trim())
						{
							logger.Debug("Indirizzo email individuato");
							WcfSiavAgrafManager wcfSiavAgrafManager = new WcfSiavAgrafManager(logger);

							wcfSiavAgrafManager.LoadRubriche();

							string sTagRG = wcfSiavAgrafManager.listRubriche.Find(x => x.Nome.Contains(resourceFileManager.getConfigData("NomeRubricaCompleta"))).lAgrafTag.Find(x => x.name.Contains(resourceFileManager.getConfigData("NomeTagGenerico"))).id;
							string sTagRF = wcfSiavAgrafManager.listRubriche.Find(x => x.Nome.Contains(resourceFileManager.getConfigData("NomeRubricaFornitori"))).lAgrafTag.Find(x => x.name.Contains(resourceFileManager.getConfigData("NomeTagFornitore"))).id;
							svAgrafCardRG.Tag = Guid.Parse(sTagRG).ToString();
							svAgrafCardRF.Tag = Guid.Parse(sTagRF).ToString();
							// Associo i tag in funzione dei nomi 
							logger.Debug("TAG Rubrica Generica: " + sTagRG + " - " + WcfSiavAgrafManager.OracleToDotNet(sTagRG));
							logger.Debug("TAG Rubrica Fornitori: " + sTagRF + " - " + WcfSiavAgrafManager.OracleToDotNet(sTagRF));

							AgrafCardContact svCardContactRG = new AgrafCardContact();
							List<GenericEntity> anagSubjectRG = new List<GenericEntity>();
							List<GenericEntity> anagSubjectRF = new List<GenericEntity>();
							// Avvio procedura di protocollazione
							// Ricerco soggetto all'interno della rubrica generica e fornitori
							// Ricerco il soggetto nella rubrica generica
							bool bFoundRubricaGenerica = false;

							if (!string.IsNullOrEmpty(sPiva))
							{
								// Ricerco il soggetto nella rubrica completa per partita iva compe tipo soggetto Company
								logger.Debug("Ricerco il soggetto nella rubrica completa per partita iva come Company");
								anagSubjectRG = wcfSiavAgrafManager.GetCompany(sPiva, resourceFileManager.getConfigData("NomeRubricaCompleta"), resourceFileManager.getConfigData("NomeCompanyRubricaSearch"));
								if (anagSubjectRG.Count == 1)
								{
									oMetadati[0] = anagSubjectRG[0].Name;
									logger.Debug("Rubrica generica, partita iva trovata, nome azienda: " + oMetadati[0]);
									bFoundRubricaGenerica = true;
								}
								else if (anagSubjectRG.Count == 0)
								{
									sMessageCheckAgraf += "Soggetto non trovato nella rubrica completa per partita iva: " + sPiva + " come Persona Giuridica" + System.Environment.NewLine;
									logger.Debug("Soggetto non trovato nella rubrica completa per partita iva come Company");
								}
								else if (anagSubjectRG.Count > 1)
								{
									sMessageCheckAgraf += "Sono stati individuati più soggetti nella rubrica completa per partita iva: " + sPiva + "  come Persona Giuridica" + System.Environment.NewLine;
									logger.Debug("Sono stati individuati più soggetti nella rubrica completa per partita iva come Company");
								}
							}
							if (!string.IsNullOrEmpty(sPiva) && bFoundRubricaGenerica == false)
							{
								// Ricerco il soggetto nella rubrica generica per partita iva compe tipo soggetto Person
								logger.Debug("Ricerco il soggetto nella rubrica generica per partita iva come soggetto Person");
								anagSubjectRG = wcfSiavAgrafManager.GetCompany(sPiva, resourceFileManager.getConfigData("NomeRubricaCompleta"), resourceFileManager.getConfigData("NomePersonRubricaSearch"));
								if (anagSubjectRG.Count == 1)
								{
									oMetadati[0] = anagSubjectRG[0].Name + " " + anagSubjectRG[0].Person.LastName;
									logger.Debug("Rubrica generica, codice fiscale trovato, nome azienda: " + oMetadati[0]);
									bFoundRubricaGenerica = true;
								}
								else if (anagSubjectRG.Count == 0)
								{
									sMessageCheckAgraf += "Soggetto non trovato nella rubrica generica per partita iva: " + sPiva + " come Persona Fisica" + System.Environment.NewLine;
									logger.Debug("Soggetto non trovato nella rubrica generica per partita iva come Person");
								}
								else if (anagSubjectRG.Count > 1)
								{
									sMessageCheckAgraf += "Sono stati individuati più soggetti nella rubrica generica per partita iva: " + sPiva + "  come Persona Fisica" + System.Environment.NewLine;
									logger.Debug("Sono stati individuati più soggetti nella rubrica generica per partita iva come Person");
								}
							}
							if (!string.IsNullOrEmpty(sCodiceFiscale) && bFoundRubricaGenerica == false)
							{
								// Ricerco il soggetto nella rubrica completa per codice fiscale compe tipo soggetto Person
								logger.Debug("Ricerco il soggetto nella rubrica completa per codice fiscale come soggetto Person");

								anagSubjectRG = wcfSiavAgrafManager.GetUsersForCas(sCodiceFiscale, resourceFileManager.getConfigData("NomeRubricaCompleta"), resourceFileManager.getConfigData("NomePersonRubricaSearch"));
								if (anagSubjectRG.Count == 1)
								{
									oMetadati[0] = anagSubjectRG[0].Name + " " + anagSubjectRG[0].Person.LastName;
									logger.Debug("Rubrica generica, codice fiscale trovato, nome azienda: " + oMetadati[0]);
									bFoundRubricaGenerica = true;
								}
								else if (anagSubjectRG.Count == 0)
								{
									sMessageCheckAgraf += "Soggetto non trovato nella Rubrica Completa per codice fiscale: " + sCodiceFiscale + " come Persona Fisica" + System.Environment.NewLine;
									logger.Debug("Soggetto non trovato nella rubrica completa per codice fiscale come Person");
								}
								else if (anagSubjectRG.Count > 1)
								{
									sMessageCheckAgraf += "Sono stati individuati più soggetti nella rubrica completa per codice fiscale: " + sCodiceFiscale + " come Persona Fisica" + System.Environment.NewLine;
									logger.Debug("Sono stati individuati più soggetti nella rubrica completa per codice fiscale come Person");
								}
							}
							if (bFoundRubricaGenerica)
							{
								logger.Debug("Soggetto individuato nella rubrica generica");
								// caso corretto -- implementazione algoritmo Associazione con Anagrafica
								svCardContactRG.EntityId = new AgrafCardContactId();
								svCardContactRG.EntityId.ContactId = new AgrafEntityId();
								svCardContactRG.EntityId.ContactId.EntityId = anagSubjectRG[0].EntityId.Id;
								svCardContactRG.EntityId.ContactId.Version = (int)anagSubjectRG[0].EntityId.Version;
								svCardContactRG.EntityId.ContactId.EntityType = anagSubjectRG[0].EntityTypeId;
								svAgrafCardRG.CardContacts.Add(svCardContactRG);
							}
							else
							{
								// specificare soggetto non trovato
								sMessageCheckAgraf += "Ricercando all'interno della rubrica generica, sono stati riscontrati dei problemi in fase di associazione con il soggetto avente codice fiscale: " + sCodiceFiscale + " e partita iva: " + sPiva + System.Environment.NewLine;
							}
							// Ricerco il soggetto nella rubrica fornitori
							bool bFoundRubricaFornitori = false;
							AgrafCardContact svCardContactRF = new AgrafCardContact();
							if (!string.IsNullOrEmpty(sPiva))
							{
								// Ricerco il soggetto nella rubrica fornitori per partita iva compe tipo soggetto Company
								logger.Debug("Ricerco il soggetto nella rubrica fornitori per partita iva come soggetto Company");
								anagSubjectRF = wcfSiavAgrafManager.GetCompany(sPiva, resourceFileManager.getConfigData("NomeRubricaFornitori"), resourceFileManager.getConfigData("NomeCompanyRubricaSearch"));

								if (anagSubjectRF.Count == 1)
								{
									oMetadati[16] = sPiva;
									oMetadati[15] = anagSubjectRF[0].VatID;
									oMetadati[18] = anagSubjectRF[0].GenericEntityExternalId;
									logger.Debug("Rubrica fornitori, partita iva trovata, piva: " + sPiva);
									bFoundRubricaFornitori = true;
								}
								else if (anagSubjectRF.Count == 0)
								{
									sMessageCheckAgraf += "Soggetto non trovato nella rubrica fornitori per partita iva: " + sPiva + " come Persona Giuridica" + System.Environment.NewLine;
									logger.Debug("Soggetto non trovato nella rubrica fornitori per partita iva come Company");
								}
								else if (anagSubjectRF.Count > 1)
								{
									sMessageCheckAgraf += "Sono stati individuati più soggetti nella rubrica fornitori per partita iva: " + sPiva + "  come Persona Giuridica" + System.Environment.NewLine;
									logger.Debug("Sono stati individuati più soggetti nella rubrica fornitori per partita iva come Company");
								}
							}
							if (!string.IsNullOrEmpty(sPiva) && bFoundRubricaFornitori == false)
							{
								// Ricerco il soggetto nella rubrica fornitori per partita iva compe tipo soggetto Company
								logger.Debug("Ricerco il soggetto nella rubrica fornitori per partita iva come soggetto Person");
								anagSubjectRF = wcfSiavAgrafManager.GetCompany(sPiva, resourceFileManager.getConfigData("NomeRubricaFornitori"), resourceFileManager.getConfigData("NomePersonRubricaSearch"));

								if (anagSubjectRF.Count == 1)
								{
									oMetadati[16] = sPiva;
									oMetadati[15] = anagSubjectRF[0].VatID;
									oMetadati[18] = anagSubjectRF[0].GenericEntityExternalId;
									logger.Debug("Rubrica fornitori, partita iva trovata, piva: " + sPiva);
									bFoundRubricaFornitori = true;
								}
								else if (anagSubjectRF.Count == 0)
								{
									sMessageCheckAgraf += "Soggetto non trovato nella rubrica fornitori per partita iva: " + sPiva + " come Persona Fisica" + System.Environment.NewLine;
									logger.Debug("Soggetto non trovato nella rubrica fornitori per partita iva come Person");
								}
								else if (anagSubjectRF.Count > 1)
								{
									sMessageCheckAgraf += "Sono stati individuati più soggetti nella rubrica fornitori per partita iva: " + sPiva + "  come Persona Fisica" + System.Environment.NewLine;
									logger.Debug("Sono stati individuati più soggetti nella rubrica fornitori per partita iva come Person");
								}
							}
							if (!string.IsNullOrEmpty(sCodiceFiscale) && bFoundRubricaFornitori == false)
							{
								// Ricerco il soggetto nella rubrica fornitori per partita iva compe tipo soggetto Person
								logger.Debug("Ricerco il soggetto nella rubrica fornitori per codice fiscale come soggetto Person");
								anagSubjectRF = wcfSiavAgrafManager.GetUsersForCas(sCodiceFiscale, resourceFileManager.getConfigData("NomeRubricaFornitori"), resourceFileManager.getConfigData("NomePersonRubricaSearch"));

								if (anagSubjectRF.Count == 1)
								{
									oMetadati[16] = anagSubjectRF[0].TaxID;
									oMetadati[15] = sCodiceFiscale;
									oMetadati[18] = anagSubjectRF[0].GenericEntityExternalId;
									logger.Debug("Rubrica fornitori, codice fiscale trovato, cf: " + sCodiceFiscale);
									bFoundRubricaFornitori = true;
								}
								else if (anagSubjectRF.Count == 0)
								{
									sMessageCheckAgraf += "Soggetto non trovato nella Rubrica Fornitori per codice fiscale: " + sCodiceFiscale + " come Persona Fisica" + System.Environment.NewLine;
									logger.Debug("Soggetto non trovato nella rubrica fornitori per codice fiscale come Person");
								}
								else if (anagSubjectRF.Count > 1)
								{
									sMessageCheckAgraf += "Sono stati individuati più soggetti nella rubrica fornitori per codice fiscale: " + sCodiceFiscale + " come Persona Fisica" + System.Environment.NewLine;
									logger.Debug("Sono stati individuati più soggetti nella rubrica fornitori per codice fiscale come Person");
								}
							}
							if (bFoundRubricaFornitori)
							{
								logger.Debug("Associazione anagrafica con rubrica fornitori");
								// caso corretto -- implementazione algoritmo Associazione con Anagrafica
								svCardContactRF.EntityId = new AgrafCardContactId();
								svCardContactRF.EntityId.ContactId = new AgrafEntityId();
								svCardContactRF.EntityId.ContactId.EntityId = anagSubjectRF[0].EntityId.Id;
								svCardContactRF.EntityId.ContactId.Version = (int)anagSubjectRF[0].EntityId.Version;
								svCardContactRF.EntityId.ContactId.EntityType = anagSubjectRF[0].EntityTypeId;
								svAgrafCardRF.CardContacts.Add(svCardContactRF);
							}
							else
							{
								// specificare soggetto non trovato
								sMessageCheckAgraf += "Ricercando all'interno della rubrica fornitori, sono stati riscontrati dei problemi in fase di associazione con il soggetto avente codice fiscale: " + sCodiceFiscale + " e partita iva: " + sPiva + System.Environment.NewLine;
							}
							if (bFoundRubricaGenerica == true && bFoundRubricaFornitori == true)
							{
								// Avvia protocollazione
								// Creazione del documento principale
								// Prendo i dati dall'xml, gli applico il foglio di stile generando un HTML
								// Converto tramite Easy Pdf il file HTML in PDF
								string sPathBillHtml = this.sWorkingFolder + @"\Fattura.html";
								FluxHelper fluxHelper = new FluxHelper();
								WsOcf.ServicesClient convertDoc = new WsOcf.ServicesClient();
								WsOcf.MainDocument InputDoc = new WsOcf.MainDocument();
								WsOcf.MainDocument OutputDoc = new WsOcf.MainDocument();

								try
								{
									//da vedere e rendere il foglio di stile modificabile e a seconda della versione (se non c'è la versione uno di default)
									XslTransform myXslTransform = new XslTransform();
									logger.Debug("Path fattura XML: " + sBillFile);
									//logger.Debug("Carico il foglio di stile: " + @"C:\Siav\APFlibrary\App_GlobalResources\FoglioStileFattura.xsl");
									//Da verificare e rendere dinamico a seconda della versione (se non esiste passare un foglio di stile di default)
									
									//string PathFileDiStile= resourceFileManager.getConfigData("PathFileDiStile");
									//if (File.Exists(@PathFileDiStile+ versione+"xsl"))
									//{
									//	myXslTransform.Load(@PathFileDiStile);
									//	logger.Debug(@PathFileDiStile + versione + "xsl");
									//}
									//else
									//{
									//	myXslTransform.Load(@resourceFileManager.getConfigData("PathFileDiStileDefault"));
									//	logger.Debug(@resourceFileManager.getConfigData("PathFileDiStileDefault").ToString());
									//}


									myXslTransform.Load(@resourceFileManager.getConfigData("PathFileDiStileDefault").ToString());
									logger.Debug("Carico il foglio di stile: "+ @resourceFileManager.getConfigData("PathFileDiStileDefault").ToString());
									logger.Debug("Creo il documento principale dalla fattura XML: " + sPathBillHtml);
									myXslTransform.Transform(sBillFile, sPathBillHtml);
									// ho generato il file HTML della fattura
									logger.Debug("Converto la fattura HTML in file PDF");
									InputDoc.Filename = "Fattura.html";
									InputDoc.BinaryContent = Convert.ToBase64String(File.ReadAllBytes(sPathBillHtml));
									convertDoc.base64ToPdfA(out OutputDoc, InputDoc);
									string sPathBillPdf = this.sWorkingFolder + @"\Fattura.pdf";
									fluxHelper.FileMaterialize(sPathBillPdf, System.Convert.FromBase64String(OutputDoc.BinaryContent));
									logger.Debug("File convertito in pdf/A: " + sPathBillPdf);
								}
								catch (Exception ex)
								{
									iResult = 3;
									throw new ArgumentException("Errore durante la creazione del file di fattura elettronica, attenzione usare la massima cautela sul file: " + sPathBillHtml);
								}
								// Valorizzo i metadati della scheda E-Acquisti
								Model.Siav.APFlibrary.Model.FieldsCard oFieldCard = new Model.Siav.APFlibrary.Model.FieldsCard();
								oFieldCard.Oggetto = sNumFattura;

								oFieldCard.MetaDati = new List<string>(oMetadati);
								logger.Debug("Path fattura XML: " + sBillFile);
								logger.Debug("Metadati Scheda: " + this.ToJson(oFieldCard.MetaDati));

								// Carico la visibilità della scheda di fatturazione
								Guid oCardProtocol;
								CardVisibility oCardVisibility = new CardVisibility();
								logger.Debug("Carico la visibilità dalla scheda di fatturazione: " + sGuidCard);

								siavCardManager.GetCardVisibility(sGuidCard, siavLogin, out oCardVisibility);
								logger.Debug("Visibilità Scheda: " + this.ToJson(oCardVisibility));
								Archive oArchive;
								DocumentType oDocumentType;
								siavCardManager.getArchiveDoc(resourceFileManager.getConfigData("IscArcCardInProt").ToString(), siavLogin.oSessionInfo, out oArchive);
								siavCardManager.getTypeDoc(resourceFileManager.getConfigData("ProtocolloFatturaXML").ToString(), siavLogin.oSessionInfo, out oDocumentType);
								logger.Debug("Inserisco la scheda come tipologia E-Acquisti in Archivio protocollo");
								logger.Debug("Anagrafiche:" + ToJson(svAgrafCardRG) + " -> " + ToJson(svAgrafCardRF));
								siavCardManager.Insert(new List<string>(), OutputDoc, siavLogin, oFieldCard, oCardVisibility.Groups, oCardVisibility.Offices, oCardVisibility.Users,
														oDocumentType, oArchive, "", "", svAgrafCardRF, svAgrafCardRG, out oCardProtocol);
								CardManager oCardManager = new CardManager(logger);
								logger.Debug("Applico la visibilità di defaul sulla scheda: " + oCardProtocol.ToString());
								oCardManager.setCardVisibilityDefault(oUserName, oUserPwd, oCardProtocol.ToString());
								// Inserisco la marcatura sul documento prinicpale
								logger.Debug("Applico la segnatura di protocollo: " + oCardProtocol.ToString());
								siavCardManager.siavWsCard.MakePressMark(siavLogin.oSessionInfo.SessionId, oCardProtocol, resourceFileManager.getConfigData("EtichettaElettronica").ToString(), true);
								iResult = 1;
								NameValueCollection oIndexCardProtocol = new NameValueCollection();
								siavCardManager.GetIndexes(oCardProtocol, siavLogin, out oIndexCardProtocol);
								sOutput = "Numero di protocollo: " + oIndexCardProtocol["Numero Protocollo"] + ". Dati fornitore: " + sValueBillOwner;
								bool bAttachIntResult = siavCardManager.SetAttachmentIntenal(oCardBundle.CardId, siavLogin, oCardProtocol, oIndexCardProtocol["Numero Protocollo"], oArchive.ArchiveId.ToString(), "Scheda protocollata");
								logger.Debug("Invio l'email di notifica");
								SendMail sendMail = new SendMail();
								string sReceiverEmail = resourceFileManager.getConfigData("EmailRecProtBillXml");
								string sSenderNameEmail = resourceFileManager.getConfigData("emailNomeMittente");
								string sObject = resourceFileManager.getConfigData("EmailSubProtBillXml"); //"Messaggio automatico - Fattura elettronica protocollata";
								string sbodyMsg = sOutput;
								sendMail.SENDMAIL(sReceiverEmail, sSenderNameEmail, sObject, sbodyMsg, "");
							}
							else
							{
								// Notifica un messaggio per il censimento del soggetto anagrafico via email ed inserendo una annotazione alla scheda
								logger.Debug("Invio l'email di notifica");
								SendMail sendMail = new SendMail();
								string sReceiverEmail = resourceFileManager.getConfigData("EmailRecAnagErrorBillXml");
								string sSenderNameEmail = resourceFileManager.getConfigData("emailNomeMittente");
								string sObject = resourceFileManager.getConfigData("EmailSubAnagErrorBillXml"); //"Problema assocazione soggetto anagrafico con scheda E-Acquisti";
								string sbodyMsg = sNumFattura + System.Environment.NewLine + sMessageCheckAgraf + sBillMessage;
								sendMail.SENDMAIL(sReceiverEmail, sSenderNameEmail, sObject, sbodyMsg, "");
								siavCardManager.InsertNote(oCardBundle, siavLogin, sbodyMsg, "Workflow");
								iResult = 2;
							}
						}
						else
						{
							// Togliere visibilità scheda fattura;
							iResult = 0;
							logger.Debug("Tolgo tutte le visibilità sulla scheda : " + sGuidCard);
							var bRemoveVis = this.RemoveAllVisibility(sGuidCard);
							if (bRemoveVis)
							{
								string[] oMetadatiPred = new string[20];
								for (int i = 0; i < oMetadatiPred.Length; i++)
								{
									oMetadatiPred[i] = null;
								}
								oMetadatiPred[9] = "";
								// Ripristinare indice scheda mail in
								siavCardManager.SetIndexes(oCardBundleAttach, siavLogin, new List<string>(oMetadatiPred));
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				try
				{
					// Notifica un messaggio In caso di errore
					logger.Debug("Invio l'email di notifica caso di errore");
					SendMail sendMail = new SendMail();
					string sReceiverEmail = resourceFileManager.getConfigData("EmailRecErrorGenBillXml");
					string sReceiverBccEmail = resourceFileManager.getConfigData("EmailRecBccErrorGenBillXml");
					string sSenderNameEmail = resourceFileManager.getConfigData("emailNomeMittente");
					string sObject = resourceFileManager.getConfigData("EmailSubErrorGenBillXml"); //"Problema assocazione soggetto anagrafico con scheda E-Acquisti";
					string sbodyMsg = "ERRORE DURANTE IL PROCESSO DI PROTOCOLLAZIONE FATTURA XML" + System.Environment.NewLine + ex.Message + " - " + ex.Source;
					sendMail.SENDMAIL(sReceiverEmail, sSenderNameEmail, sObject, sbodyMsg, sReceiverBccEmail);
					logger.Debug(ex, "ERRORE" + ex.StackTrace + " - " + ex.Source + " - " + ex.Message);
					if (siavCardManager != null)
					{
						siavCardManager.siavWsCard.Abort();
					}
					if (siavLogin != null)
					{
						siavLogin.siavWsLogin.Close();
					}
				}
				catch (Exception e) { }
			}
			finally
			{
				logger.Debug("Fine elaborazione");
				if (siavLogin != null)
				{
					siavLogin.Logout();
				}
			}
			return iResult;
		}

		public bool AddMainDocToCardAttachInt(string sGuidCard)
		{
			WcfSiavLoginManager siavLogin = null;
			bool bResult = false;
			WcfSiavCardManager siavCardManager = null;
			List<string> oAttachments = new List<string>();
			CardBundle oCardBundle = new CardBundle();
			MainDoc oMainDoc = new MainDoc();
			try
			{
				siavLogin = new WcfSiavLoginManager();
				string oUserName = resourceFileManager.getConfigData("UserFlux");
				string oUserPwd = oRijndael.Decrypt(resourceFileManager.getConfigData("PasswordFlux"));

				// Esegue il login
				siavLogin.Login(oUserName, oUserPwd);
				logger.Debug("Sessione login Archiflow: " + siavLogin.oSessionInfo.SessionId);
				siavCardManager = new WcfSiavCardManager(logger);
				siavCardManager.GetCard(sGuidCard, siavLogin, out oCardBundle);
				logger.Debug("Scheda trovata: " + oCardBundle.CardId);
				if (oCardBundle != null)
				{
					siavCardManager.GetMainDoc(oCardBundle, out oMainDoc);
					logger.Debug("Maindoc trovato: " + oMainDoc.Filename);
					List<Siav.APFlibrary.Model.InternalAttachment> lPathAttachments = new List<Siav.APFlibrary.Model.InternalAttachment>();
					// cerco tra gli allegati circolari se presenti
					siavCardManager.GetCardInternalAttachments(sGuidCard, siavLogin, out lPathAttachments);
					if (lPathAttachments != null)
					{
						foreach (Siav.APFlibrary.Model.InternalAttachment singleInternalAttachment in lPathAttachments)
						{
							logger.Debug("Internal Attachment trovato: " + singleInternalAttachment.Name);
							if (singleInternalAttachment.ArchiveId != "")
							{
								CardBundle oCardBundleAttach = new CardBundle();
								siavCardManager.GetCard(singleInternalAttachment.Name, singleInternalAttachment.ArchiveId, siavLogin, out oCardBundleAttach);
								siavCardManager.SetAttachment(oCardBundleAttach, siavLogin, oMainDoc.Filename + '.' + oMainDoc.Extension, Convert.ToBase64String(oMainDoc.oByte), "");
								bResult = true;
								break;
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				try
				{
					logger.Debug(String.Format("{0}>>{1}>>{2}", "ERRORE : AddMainDocToCardAttachInt", ex.Source, ex.Message, ex.StackTrace), ex);
					if (siavCardManager != null)
					{
						siavCardManager.siavWsCard.Abort();
					}
					if (siavLogin != null)
					{
						siavLogin.Logout();
						siavLogin.siavWsLogin.Close();
					}
				}
				catch (Exception e) { }
			}
			finally
			{
				logger.Debug("Fine elaborazione");
				//siavLogin.Logout();
			}
			return bResult;
		}
		public int GetNumberNotes(string sGuidCard)
		{
			WcfSiavLoginManager siavLogin = null;
			WcfSiavCardManager siavCardManager = null;
			Esito oEsito = new Esito();
			int iResult = 0;
			try
			{
				siavLogin = new WcfSiavLoginManager();
				string oUserName = resourceFileManager.getConfigData("UserFlux");
				string oUserPwd = oRijndael.Decrypt(resourceFileManager.getConfigData("PasswordFlux"));

				// Esegue il login
				siavLogin.Login(oUserName, oUserPwd);
				logger.Debug("Sessione login Archiflow: " + siavLogin.oSessionInfo.SessionId);

				siavCardManager = new WcfSiavCardManager(logger);

				oEsito = siavCardManager.getNoteNumber(sGuidCard, siavLogin, out iResult);

				siavLogin.Logout();
				siavLogin.siavWsLogin.Close();

				siavLogin = null;
				siavCardManager.siavWsCard.Close();
				siavCardManager = null;
			}
			catch (Exception ex)
			{
				try
				{
					logger.Error(ex.ToString() + oEsito.Descrizione);
					if (siavCardManager != null)
					{
						siavCardManager.siavWsCard.Abort();
					}
					if (siavLogin != null)
					{
						siavLogin.Logout();
						siavLogin.siavWsLogin.Close();
					}
				}
				catch (Exception e) { }
			}
			finally
			{
				logger.Debug("Fine elaborazione");
				//siavLogin.Logout();
			}
			return iResult;
		}
		public Boolean IsExistMainDoc(string sGuidCard)
		{
			WcfSiavLoginManager siavLogin = null;
			WcfSiavCardManager siavCardManager = null;
			bool bResult = false;
			CardBundle oCardBundle = null;
			try
			{
				siavLogin = new WcfSiavLoginManager();
				string oUserName = resourceFileManager.getConfigData("UserFlux");
				string oUserPwd = oRijndael.Decrypt(resourceFileManager.getConfigData("PasswordFlux"));

				// Esegue il login
				siavLogin.Login(oUserName, oUserPwd);
				logger.Debug("Sessione login Archiflow: " + siavLogin.oSessionInfo.SessionId);

				siavCardManager = new WcfSiavCardManager(logger);

				siavCardManager.GetCard(sGuidCard, siavLogin, out oCardBundle);
				if (oCardBundle != null)
				{
					if (oCardBundle.HasDocument)
					{
						bResult = true;
					}
					else
					{
						bResult = false;
					}
				}
				siavLogin.Logout();
				siavLogin.siavWsLogin.Close();

				siavLogin = null;
				siavCardManager.siavWsCard.Close();
				siavCardManager = null;
			}
			catch (Exception ex)
			{
				try
				{
					logger.Error(ex.ToString());
					if (siavCardManager != null)
					{
						siavCardManager.siavWsCard.Abort();
					}
					if (siavLogin != null)
					{
						siavLogin.Logout();
						siavLogin.siavWsLogin.Close();
					}
				}
				catch (Exception e) { }
			}
			finally
			{
				logger.Debug("Fine elaborazione");
				//siavLogin.Logout();
			}
			return bResult;
		}
		public Boolean CheckCardSigned(string GUIdcard)
		{
			WcfSiavLoginManager siavLogin = null;
			WcfSiavCardManager siavCardManager = null;
			bool bResult = false;
			CardBundle oCardBundle = null;
			try
			{
				siavLogin = new WcfSiavLoginManager();
				string oUserName = resourceFileManager.getConfigData("UserFlux");
				string oUserPwd = oRijndael.Decrypt(resourceFileManager.getConfigData("PasswordFlux"));

				// Esegue il login
				siavLogin.Login(oUserName, oUserPwd);
				logger.Debug("Sessione login Archiflow: " + siavLogin.oSessionInfo.SessionId);

				siavCardManager = new WcfSiavCardManager(logger);

				siavCardManager.GetCard(GUIdcard, siavLogin, out oCardBundle);
				if (oCardBundle.MainDocument.IsSigned == true || oCardBundle.MainDocument.IsSignedPdf == true)
				{
					bResult = true;
				}
				siavLogin.Logout();
				siavLogin.siavWsLogin.Close();

				siavLogin = null;
				siavCardManager.siavWsCard.Close();
				siavCardManager = null;
			}
			catch (Exception ex)
			{
				try
				{
					logger.Error(ex.ToString());
					if (siavCardManager != null)
					{
						siavCardManager.siavWsCard.Abort();
					}
					if (siavLogin != null)
					{
						siavLogin.Logout();
						siavLogin.siavWsLogin.Close();
					}
				}
				catch (Exception e) { }
			}
			finally
			{
				logger.Debug("Fine elaborazione");
				//siavLogin.Logout();
			}
			return bResult;
		}

		public Boolean CheckCardSignedCancellazione(string GUIdcard)
		{
			WcfSiavLoginManager siavLogin = null;
			WcfSiavCardManager siavCardManager = null;
			bool bResult = false;
			CardBundle oCardBundle = null;
			try
			{
				siavLogin = new WcfSiavLoginManager();
				string oUserName = resourceFileManager.getConfigData("UserFlux");
				string oUserPwd = oRijndael.Decrypt(resourceFileManager.getConfigData("PasswordFlux"));

				// Esegue il login
				siavLogin.Login(oUserName, oUserPwd);
				logger.Debug("Sessione login Archiflow: " + siavLogin.oSessionInfo.SessionId);

				siavCardManager = new WcfSiavCardManager(logger);

				siavCardManager.GetCard(GUIdcard, siavLogin, out oCardBundle);
				if (oCardBundle.MainDocument.IsSigned == true || oCardBundle.MainDocument.IsSignedPdf == true)
				{
					bResult = true;
				}
				siavLogin.Logout();
				siavLogin.siavWsLogin.Close();

				siavLogin = null;
				siavCardManager.siavWsCard.Close();
				siavCardManager = null;
			}
			catch (Exception ex)
			{
				try
				{
					logger.Error(ex.ToString());
					if (siavCardManager != null)
					{
						siavCardManager.siavWsCard.Abort();
					}
					if (siavLogin != null)
					{
						siavLogin.Logout();
						siavLogin.siavWsLogin.Close();
					}
				}
				catch (Exception e) { }
			}
			finally
			{
				logger.Debug("Fine elaborazione");
				//siavLogin.Logout();
			}
			return bResult;
		}
		public Boolean SetLinkAnag(string GUIdcard, string CodOCF)
		{
			Boolean bResulAnagProc = false;
			WcfSiavLoginManager siavLogin = null;
			WcfSiavAgrafManager wcfSiavAgrafManager = null;
			WcfSiavCardManager siavCardManager = null;
			CardBundle oCardBundle = null;
			// Flag che certifica l'avvenuta associazione con la rubrica generica
			Boolean bFindRg = false;
			// Flag che certifica l'avvenuta associazione con la rubrica APF
			Boolean bFindApf = false;
			string[] oMetadati = new string[20];

			try
			{
				for (int i = 0; i < oMetadati.Length; i++)
				{
					oMetadati[i] = null;
				}


				siavLogin = new WcfSiavLoginManager();
				wcfSiavAgrafManager = new WcfSiavAgrafManager(logger);
				wcfSiavAgrafManager.LoadRubriche();
				FluxHelper fluxHelper = new FluxHelper();
				string oUserName = resourceFileManager.getConfigData("UserFlux");
				string oUserPwd = oRijndael.Decrypt(resourceFileManager.getConfigData("PasswordFlux"));

				// Esegue il login
				siavLogin.Login(oUserName, oUserPwd);
				logger.Debug("Sessione login Archiflow: " + siavLogin.oSessionInfo.SessionId);













				siavCardManager = new WcfSiavCardManager(logger);
				// Recupera la scheda ove è stato avviato il processo
				siavCardManager.GetCard(GUIdcard, siavLogin, out oCardBundle);

				AgrafCard svAgrafCardRub = new AgrafCard();
				svAgrafCardRub.CardContacts = new List<AgrafCardContact>();
				svAgrafCardRub.Tag = sIdIndexTagRubricaGen;
				AgrafCard svAgrafCardApf = new AgrafCard();
				svAgrafCardApf.CardContacts = new List<AgrafCardContact>();
				svAgrafCardApf.Tag = sIdIndexTagRubricaApf;
				logger.Debug("Dati rubrica caricati");
				string sNote = "";
				// Ricerca all'interno della rubrica generica
				List<GenericEntity> anagUsersRubricaGen = wcfSiavAgrafManager.GetUsers(CodOCF, resourceFileManager.getConfigData("NomeRubricaCompleta"), resourceFileManager.getConfigData("NomePersonRubricaSearch"));
				if (anagUsersRubricaGen.Count > 1)
				{
					// caso errore trovati più utenti con lo stesso codice fiscale
					sNote = "Sono stati trovati n°:" + anagUsersRubricaGen.Count + " record per il codice OCF:" + CodOCF + ", verificare l'anagrafica " + resourceFileManager.getConfigData("NomeRubricaCompleta");
					logger.Debug(sNote);
				}
				else if (anagUsersRubricaGen.Count == 1)
				{
					logger.Debug("Associazione anagrafica con rubrica generica avvenuta con successo");
					bFindRg = true;
					// caso corretto -- implementazione algoritmo Associazione con Anagrafica
					AgrafCardContact svCardContactRg = new AgrafCardContact();
					svCardContactRg.EntityId = new AgrafCardContactId();
					svCardContactRg.EntityId.ContactId = new AgrafEntityId();
					svCardContactRg.EntityId.ContactId.EntityId = anagUsersRubricaGen[0].EntityId.Id;
					svCardContactRg.EntityId.ContactId.Version = (int)anagUsersRubricaGen[0].EntityId.Version;
					svAgrafCardRub.CardContacts.Add(svCardContactRg);
					// Destinatario
					oMetadati[int.Parse(resourceFileManager.getConfigData("IscIndDestinatario"))] = anagUsersRubricaGen[0].Name + " " + anagUsersRubricaGen[0].Person.LastName;
				}
				else
				{
					// caso in cui non è stato trovato l'utente per la scheda
					sNote = "Non è stato trovato alcun record per il codice OCF:" + CodOCF + ", verificare l'anagrafica " + resourceFileManager.getConfigData("NomeRubricaCompleta");
					logger.Debug(sNote);
				}
				// Ricerca all'interno della rubrica Aspiranti e promotori finanziari
				List<GenericEntity> anagUsersAspPF = wcfSiavAgrafManager.GetUsers(CodOCF, resourceFileManager.getConfigData("NomeRubricaAspPf"), resourceFileManager.getConfigData("NomePersonRubricaSearch"));
				if (anagUsersAspPF.Count > 1)
				{
					// caso errore trovati più utenti con lo stesso codice fiscale
					sNote = "Sono stati trovati n°:" + anagUsersAspPF.Count + " record per il codice OCF:" + CodOCF + ", verificare l'anagrafica " + resourceFileManager.getConfigData("NomeRubricaAspPf");
					logger.Debug(sNote);
				}
				else if (anagUsersAspPF.Count == 1)
				{
					bFindApf = true;
					logger.Debug("Associazione anagrafica con rubrica Promotori finanziari avvenuta con successo");

					// caso corretto -- implementazione algoritmo Associazione con Anagrafica
					AgrafCardContact svCardContactApf = new AgrafCardContact();
					svCardContactApf.EntityId = new AgrafCardContactId();
					svCardContactApf.EntityId.ContactId = new AgrafEntityId();
					svCardContactApf.EntityId.ContactId.EntityId = anagUsersAspPF[0].EntityId.Id;
					svCardContactApf.EntityId.ContactId.Version = (int)anagUsersAspPF[0].EntityId.Version;
					svAgrafCardApf.CardContacts.Add(svCardContactApf);
					// codice APF 
					oMetadati[int.Parse(resourceFileManager.getConfigData("IscIndSchedCodApf"))] = anagUsersRubricaGen[0].GenericEntityExternalId;
					// Codice Consob 
					oMetadati[int.Parse(resourceFileManager.getConfigData("IscIndSchedCodConsob"))] = anagUsersRubricaGen[0].GenericEntityExternalId2;
					// Classifica
					oMetadati[int.Parse(resourceFileManager.getConfigData("IscIndSchedClassifica"))] = anagUsersRubricaGen[0].GenericEntityExternalId3;
					// Codice Fiscale
					oMetadati[int.Parse(resourceFileManager.getConfigData("IscIndSchedCodFisc"))] = anagUsersRubricaGen[0].VatID;
				}
				else
				{
					// caso in cui non è stato trovato l'utente per la scheda
					sNote = "Non è stato trovato alcun record per il codice OCF:" + CodOCF + ", verificare l'anagrafica " + resourceFileManager.getConfigData("NomeRubricaAspPf");
					logger.Debug(sNote);
				}
				// Codice OCF
				oMetadati[int.Parse(resourceFileManager.getConfigData("IscIndSchedCodApf"))] = CodOCF;

				// Stato
				oMetadati[int.Parse(resourceFileManager.getConfigData("IscIndSchedStato"))] = "";
				string sNoteData = "";

				if (bFindRg == true && bFindApf == true)
				{
					int iAffected = 0;
					bool bAnagResult = siavCardManager.SetAnag(GUIdcard, siavLogin, svAgrafCardRub, svAgrafCardApf, out iAffected);
					if (bAnagResult)
					{
						// Aggiornamento campi indice anagrafica
						bool bIndexResult = siavCardManager.SetIndexes(oCardBundle, siavLogin, new List<string>(oMetadati));
						if (bIndexResult)
							bResulAnagProc = true;
					}
					else
					{
						throw new ArgumentException("Errore durante l'associazione dell'anagrafica");
					}
				}
				if (bFindRg == false)
				{
					sNoteData = "Il codice OCF: " + CodOCF + " non è stato trovato all'interno della rubrica generica. ";
				}
				if (bFindApf == false)
				{
					sNoteData += "Il codice OCF: " + CodOCF + " non è stato trovato all'interno della rubrica albo promotori e finianziari.";
				}
				siavCardManager.InsertNote(oCardBundle, siavLogin, sNoteData + " Id transazione: " + this.IdSessioneTransazione, resourceFileManager.getConfigData("AutorAnnotation"));
				siavLogin.Logout();
				siavLogin.siavWsLogin.Close();
				siavLogin = null;
				siavCardManager.siavWsCard.Close();
				siavCardManager = null;
				wcfSiavAgrafManager.siavWsAgraf.Close();
				wcfSiavAgrafManager = null;
			}
			catch (Exception ex)
			{
				try
				{
					logger.Error(ex.ToString());
					if (siavCardManager != null)
					{
						siavCardManager.InsertNote(oCardBundle, siavLogin, ex.Message + " Per maggiori informazioni consultare il log per l'id transazione: " + this.IdSessioneTransazione, resourceFileManager.getConfigData("AutorAnnotation"));
						siavCardManager.siavWsCard.Abort();
					}
					if (siavLogin != null)
					{
						siavLogin.Logout();
						siavLogin.siavWsLogin.Close();
					}
					if (wcfSiavAgrafManager != null)
					{
						wcfSiavAgrafManager.siavWsAgraf.Abort();
					}
				}
				catch (Exception e) { }
			}
			finally
			{
				logger.Debug("Fine elaborazione");
				//siavLogin.Logout();
			}
			return bResulAnagProc;
		}

		public String InsertInProtocolArchiveCancellazione(string IdComMaxiva)
		{
			WcfSiavLoginManager siavLogin = null;
			WcfSiavCardManager siavCardManager = null;
			CardBundle oCardBundleIscMassive = null;
			FluxHelper fluxHelper = new FluxHelper();
			int iInsertInProtocol = 0;
			string sResult = "";
			try
			{
				List<Guid> lGUID;
				siavLogin = new WcfSiavLoginManager();
				string oUserName = resourceFileManager.getConfigData("UserFlux");
				string oUserPwd = oRijndael.Decrypt(resourceFileManager.getConfigData("PasswordFlux"));

				// Esegue il login
				siavLogin.Login(oUserName, oUserPwd);
				logger.Debug("Sessione login Archiflow: " + siavLogin.oSessionInfo.SessionId);

				siavCardManager = new WcfSiavCardManager(logger);

				// Sezione per ricavare il cardBundle della scheda massiva generante
				List<Guid> lGUIDIscMassive;
				List<SchedaSearchParameter> lIscMassiveSP = new List<SchedaSearchParameter>();

				List<string> lIscMassiveMet;
				SchedaSearchParameter CancMassiveSP = new SchedaSearchParameter();
				CancMassiveSP.Archivio = resourceFileManager.getConfigData("CancArchiveMassive");
				CancMassiveSP.TipologiaDocumentale = resourceFileManager.getConfigData("CancTypeDocMassive");
				string[] arrIscMassiveMet = new string[20];
				//arrIscMassiveMet[int.Parse("1")] = IdComMaxiva;
				CancMassiveSP.IdRiferimento = IdComMaxiva;
				logger.Debug("Inizio ricerca record");
				logger.Debug("  Cerco per Archivio: " + CancMassiveSP.Archivio);
				logger.Debug("  Cerco per TipologiaDocumentale: " + CancMassiveSP.TipologiaDocumentale);
				logger.Debug("  Cerco per ID comunicazione massiva: " + IdComMaxiva);
				logger.Debug("Fine ricerca record");

				lIscMassiveMet = new List<string>(arrIscMassiveMet);
				CancMassiveSP.MetaDati = lIscMassiveMet;
				lIscMassiveSP.Add(CancMassiveSP);
				siavCardManager.getSearch(lIscMassiveSP, siavLogin, out lGUIDIscMassive);
				foreach (Guid singleIscMassive in lGUIDIscMassive)
				{
					siavCardManager.GetCard(singleIscMassive.ToString(), siavLogin, out oCardBundleIscMassive);
				}
				//
				List<SchedaSearchParameter> lSchedaSearchParameter = new List<SchedaSearchParameter>();

				List<string> lMetadati;
				SchedaSearchParameter schedaSearchParameter = new SchedaSearchParameter();
				schedaSearchParameter.Archivio = resourceFileManager.getConfigData("CancArcCardInError");
				schedaSearchParameter.TipologiaDocumentale = resourceFileManager.getConfigData("CancDocTypeCardInError");
				string[] arr = new string[20];
				arr[int.Parse(resourceFileManager.getConfigData("CancIndexFieldCardInError"))] = IdComMaxiva;
				arr[int.Parse(resourceFileManager.getConfigData("CancIndexStatoCardInError"))] = resourceFileManager.getConfigData("CancValueStatoCardInReadyToProt");

				logger.Debug("Inizio ricerca record");
				logger.Debug("  Cerco per Archivio: " + resourceFileManager.getConfigData("CancArcCardInError"));
				logger.Debug("  Cerco per TipologiaDocumentale: " + resourceFileManager.getConfigData("CancDocTypeCardInError"));
				logger.Debug("  Cerco per ID comunicazione massiva: " + resourceFileManager.getConfigData("CancIndexFieldCardInError"));
				logger.Debug("  Cerco per STATO: " + resourceFileManager.getConfigData("CancValueStatoCardInReadyToProt"));

				logger.Debug("Fine ricerca record");

				lMetadati = new List<string>(arr);
				schedaSearchParameter.MetaDati = lMetadati;
				lSchedaSearchParameter.Add(schedaSearchParameter);
				siavCardManager.getSearch(lSchedaSearchParameter, siavLogin, out lGUID);
				List<int> lOnlyNumericPartGuid = new List<int>();
				foreach (Guid sSingleValue in lGUID)
				{
					lOnlyNumericPartGuid.Add(int.Parse(sSingleValue.ToString().Substring(25)));
				}
				lOnlyNumericPartGuid.Sort();
				short shArchiveToProtId = 0;
				DocumentType oDocType = new DocumentType();
				Archive oArchive = new Archive();
				short shTypeDocToProtId = 0;
				// Estraggo le schede da protocollare dall'ambiente di predisposizione
				siavCardManager.getArchiveDoc(resourceFileManager.getConfigData("CancArcCardInProt"), siavLogin.oSessionInfo, out oArchive);
				siavCardManager.getTypeDoc(resourceFileManager.getConfigData("CancDocTypeCardInProt"), siavLogin.oSessionInfo, out oDocType);
				shArchiveToProtId = oArchive.ArchiveId;
				shTypeDocToProtId = oDocType.DocumentTypeId;
				foreach (int singleGuid in lOnlyNumericPartGuid)
				{
					logger.Debug("Sto elaborando la scheda per eseguire la protocollazione con id: " + singleGuid.ToString());
					List<Guid> lIfExistsGUID;
					List<SchedaSearchParameter> lIfExistsSchedaSearchParameter = new List<SchedaSearchParameter>();
					CardBundle oCardBundleProt;
					siavCardManager.GetCard(singleGuid.ToString(), siavLogin, out oCardBundleProt);
					NameValueCollection CancProtCardIndexes;
					siavCardManager.GetIndexes(oCardBundleProt, out CancProtCardIndexes);

					List<string> lIfExistsMetadati;
					SchedaSearchParameter IfExistsSchedaSearchParameter = new SchedaSearchParameter();
					IfExistsSchedaSearchParameter.Archivio = resourceFileManager.getConfigData("CancArcCardInProt");
					IfExistsSchedaSearchParameter.TipologiaDocumentale = resourceFileManager.getConfigData("CancDocTypeCardInProt");
					string[] IfExistsArr = new string[20];

					IfExistsSchedaSearchParameter.Oggetto = CancProtCardIndexes[resourceFileManager.getConfigData("CancNameFieldObj")].Replace(" - ", " ");
					IfExistsArr[int.Parse(resourceFileManager.getConfigData("CancIndSchedSearchUnique"))] = CancProtCardIndexes[resourceFileManager.getConfigData("CancFieldProt")];

					logger.Debug("Inizio ricerca record");
					logger.Debug("  Cerco per Archivio: " + resourceFileManager.getConfigData("CancArcCardInProt"));
					logger.Debug("  Cerco per TipologiaDocumentale: " + resourceFileManager.getConfigData("CancDocTypeCardInProt"));
					logger.Debug("  Cerco per " + resourceFileManager.getConfigData("CancFieldProt") + ": " + CancProtCardIndexes[resourceFileManager.getConfigData("CancFieldProt")]);
					//logger.Debug("  Cerco per Codice OCF: " + CancProtCardIndexes[resourceFileManager.getConfigData("CancFieldProt")]);
					logger.Debug("  Cerco per Oggetto: " + CancProtCardIndexes[resourceFileManager.getConfigData("CancNameFieldObj")]);
					logger.Debug("Fine ricerca record");

					lIfExistsMetadati = new List<string>(IfExistsArr);
					IfExistsSchedaSearchParameter.MetaDati = lIfExistsMetadati;
					lIfExistsSchedaSearchParameter.Add(IfExistsSchedaSearchParameter);
					// Ricerca se la scheda sia già stata protocollata
					siavCardManager.getSearch(lIfExistsSchedaSearchParameter, siavLogin, out lIfExistsGUID);
					// Inserimento scheda protocollo

					string sCheckConfigurationCU = resourceFileManager.getConfigData("CancOPLA");
					// Il sistema verifica se vi sia un protocollo già presente nel sistema, salta tale controllo se si tratta di elaborare un documento
					// di tipo lettera accompagnatoria

					if (lIfExistsGUID.Count == 0 || fluxHelper.CheckWordInPhrase(IfExistsSchedaSearchParameter.Oggetto, sCheckConfigurationCU) == true)
					{
						Guid oCardProtocol;
						CardBundle oCardBundle = null;
						HashSet<int> fieldDataToErase = new HashSet<int>();
						List<string> lfieldDataToErase = resourceFileManager.getConfigData("CancFieldNoProt").Split(',').ToList();
						foreach (string singleValue in lfieldDataToErase)
						{
							fieldDataToErase.Add(int.Parse(singleValue));
							logger.Debug("Field NOT COPY: " + singleValue);
						}
						siavCardManager.GetCard(singleGuid.ToString(), siavLogin, out oCardBundle);
						siavCardManager.InsertCard(oCardBundle, fieldDataToErase, shArchiveToProtId, shTypeDocToProtId, siavLogin, out oCardProtocol);
						CardManager oCardManager = new CardManager(logger);
						oCardManager.setCardVisibilityDefault(oUserName, oUserPwd, oCardProtocol.ToString());
						CardBundle oCardProt = new CardBundle();
						siavCardManager.GetCard(oCardProtocol.ToString(), siavLogin, out oCardProt);
						List<string> sPathAttachments = new List<string>();
						siavCardManager.GetCardAttachments(sWorkingFolder, oCardBundle.CardId.ToString(), siavLogin, out sPathAttachments);
						if (sPathAttachments != null)
							foreach (string singleAttach in sPathAttachments)
							{
								siavCardManager.SetAttachment(oCardProt, siavLogin, singleAttach, "");
							}
						siavCardManager.siavWsCard.MakePressMark(siavLogin.oSessionInfo.SessionId, oCardProtocol, resourceFileManager.getConfigData("EtichettaElettronica").ToString(), true);
						string[] oMetadatiPred = new string[20];
						for (int i = 0; i < oMetadatiPred.Length; i++)
						{
							oMetadatiPred[i] = null;
						}
						string sProtData = "";
						NameValueCollection IndexesProtCard = null;
						CardBundle oCardBundleProtocol;
						siavCardManager.GetCard(oCardProtocol.ToString(), siavLogin, out oCardBundleProtocol);
						siavCardManager.GetIndexes(oCardBundleProtocol, out IndexesProtCard);
						sProtData = IndexesProtCard[resourceFileManager.getConfigData("CancNameFieldIdProt")] + " - " + IndexesProtCard[resourceFileManager.getConfigData("CancNameFieldDataProt")];
						oMetadatiPred[int.Parse(resourceFileManager.getConfigData("CancIndProtGen"))] = sProtData;
						siavCardManager.SetIndexes(oCardBundle, siavLogin, new List<string>(oMetadatiPred));
						iInsertInProtocol++;
						logger.Debug("Inserita la scheda:" + oCardProtocol.ToString());
						sResult += oCardProtocol.ToString() + "|";
					}
					else if (lIfExistsGUID.Count > 0)
					{
						// Record trovato, notifica l'informazione se il record appartiene ad un invio massivo diverso da quello processato
						bool isCardInvalid = false;// check che verifica se la scheda trovata non sia annullata, perchè in tal caso l'inserimento della scheda DEVE avvenire
						foreach (Guid idGUID in lIfExistsGUID)
						{
							logger.Debug("Processo scheda con GUID:" + idGUID.ToString());

							NameValueCollection IndexesProtCardCheck = null;
							CardBundle oCardBundleCheck = null;
							siavCardManager.GetCard(idGUID.ToString(), siavLogin, out oCardBundleCheck);
							bool checkCardValid;
							siavCardManager.CheckValidCard(oCardBundleCheck, out checkCardValid);
							if (checkCardValid)
							{
								isCardInvalid = true;
								siavCardManager.GetIndexes(oCardBundleCheck, out IndexesProtCardCheck);
								if (IndexesProtCardCheck[resourceFileManager.getConfigData("InsNameFieldComMassiva")] == IdComMaxiva)
								{
									// Non fare nulla
								}
								else
								{
									// Notifica il record individuato
									siavCardManager.InsertNote(oCardBundleIscMassive, siavLogin, "È stata trovata una scheda già protocollata, il suo protocollo assoluto è: " + int.Parse(lIfExistsGUID[0].ToString().Substring(25)) + ". La scheda in predisposizione avente il protocollo assoluto: " + singleGuid + " non è stata protocollata.", resourceFileManager.getConfigData("AutorAnnotation"));
								}
								break;
							}
						}
						if (isCardInvalid == false)
						{
							Guid oCardProtocol;
							CardBundle oCardBundle = null;
							HashSet<int> fieldDataToErase = new HashSet<int>();
							List<string> lfieldDataToErase = resourceFileManager.getConfigData("CancFieldNoProt").Split(',').ToList();
							foreach (string singleValue in lfieldDataToErase)
							{
								fieldDataToErase.Add(int.Parse(singleValue));
							}
							siavCardManager.GetCard(singleGuid.ToString(), siavLogin, out oCardBundle);
							siavCardManager.InsertCard(oCardBundle, fieldDataToErase, shArchiveToProtId, shTypeDocToProtId, siavLogin, out oCardProtocol);
							CardManager oCardManager = new CardManager(logger);
							oCardManager.setCardVisibilityDefault(oUserName, oUserPwd, oCardProtocol.ToString());
							CardBundle oCardProt = new CardBundle();
							siavCardManager.GetCard(oCardProtocol.ToString(), siavLogin, out oCardProt);
							List<string> sPathAttachments = new List<string>();
							siavCardManager.GetCardAttachments(sWorkingFolder, oCardBundle.CardId.ToString(), siavLogin, out sPathAttachments);
							if (sPathAttachments != null)
								foreach (string singleAttach in sPathAttachments)
								{
									siavCardManager.SetAttachment(oCardProt, siavLogin, singleAttach, "");
								}
							siavCardManager.siavWsCard.MakePressMark(siavLogin.oSessionInfo.SessionId, oCardProtocol, resourceFileManager.getConfigData("EtichettaElettronica").ToString(), true);
							string[] oMetadatiPred = new string[20];
							for (int i = 0; i < oMetadatiPred.Length; i++)
							{
								oMetadatiPred[i] = null;
							}
							string sProtData = "";
							NameValueCollection IndexesProtCard = null;
							CardBundle oCardBundleProtocol;
							siavCardManager.GetCard(oCardProtocol.ToString(), siavLogin, out oCardBundleProtocol);
							siavCardManager.GetIndexes(oCardBundleProtocol, out IndexesProtCard);
							sProtData = IndexesProtCard[resourceFileManager.getConfigData("CancNameFieldIdProt")] + " - " + IndexesProtCard[resourceFileManager.getConfigData("CancNameFieldDataProt")];
							oMetadatiPred[int.Parse(resourceFileManager.getConfigData("CancIndProtGen"))] = sProtData;
							siavCardManager.SetIndexes(oCardBundle, siavLogin, new List<string>(oMetadatiPred));
							iInsertInProtocol++;
							logger.Debug("Inserita la scheda:" + oCardProtocol.ToString());
							sResult += oCardProtocol.ToString() + "|";
						}
					}
				}
				// Inserimento note scheda padre massiva
				siavCardManager.InsertNote(oCardBundleIscMassive, siavLogin, "Sono state inserire in Archivio ufficiale numero: " + iInsertInProtocol + " protocolli.", resourceFileManager.getConfigData("AutorAnnotation"));
				// Aggiornamento dello stato scheda padre massiva con il valore "Completato"
				string[] oMetadati = new string[20];
				for (int i = 0; i < oMetadati.Length; i++)
				{
					oMetadati[i] = null;
				}
				oMetadati[int.Parse(resourceFileManager.getConfigData("CancStatoFieldMassive"))] = resourceFileManager.getConfigData("CancStatoCompletedValueMassive");
				siavCardManager.SetIndexes(oCardBundleIscMassive, siavLogin, new List<string>(oMetadati));

				// Verifica inserimento dati confrontando il numero delle schede in predisposizione con quelle protocollate
				lSchedaSearchParameter = new List<SchedaSearchParameter>();

				lMetadati = null;
				schedaSearchParameter = new SchedaSearchParameter();
				schedaSearchParameter.Archivio = resourceFileManager.getConfigData("CancArcCardInError");
				schedaSearchParameter.TipologiaDocumentale = resourceFileManager.getConfigData("CancDocTypeCardInError");
				arr = new string[20];
				arr[int.Parse(resourceFileManager.getConfigData("CancIndexFieldCardInError"))] = IdComMaxiva;

				logger.Debug("Inizio ricerca record");
				logger.Debug("  Cerco per Archivio: " + resourceFileManager.getConfigData("CancArcCardInError"));
				logger.Debug("  Cerco per TipologiaDocumentale: " + resourceFileManager.getConfigData("CancDocTypeCardInError"));
				logger.Debug("  Cerco per ID comunicazione massiva: " + IdComMaxiva);
				logger.Debug("Fine ricerca record");

				lMetadati = new List<string>(arr);
				schedaSearchParameter.MetaDati = lMetadati;
				lSchedaSearchParameter.Add(schedaSearchParameter);
				List<Guid> lGuidPredIsc;
				List<Guid> lGuidProtIsc;
				logger.Debug("lSchedaSearchParameter PRED" + ToJson(lSchedaSearchParameter));
				siavCardManager.getSearch(lSchedaSearchParameter, siavLogin, out lGuidPredIsc);
				logger.Debug("lGuidPredIsc" + ToJson(lGuidPredIsc));
				schedaSearchParameter.Archivio = resourceFileManager.getConfigData("CancArcCardInProt");
				schedaSearchParameter.TipologiaDocumentale = resourceFileManager.getConfigData("CancDocTypeCardInProt");
				logger.Debug("lSchedaSearchParameter PRED" + ToJson(lSchedaSearchParameter));
				siavCardManager.getSearch(lSchedaSearchParameter, siavLogin, out lGuidProtIsc);
				logger.Debug("lGuidProtIsc" + ToJson(lGuidProtIsc));
				if ((lGuidProtIsc != null) && (lGuidProtIsc.Count > 0))
				{
					logger.Debug("Ho trovato Record: " + lGuidProtIsc.Count + " nel protocollo");
				}
				else
					logger.Debug("NON ho trovato Record in lGuidProtIsc nel protocollo");
				if ((lGuidPredIsc != null) && (lGuidPredIsc.Count > 0))
				{
					logger.Debug("Ho trovato Record: " + lGuidPredIsc.Count + " in predisposizione");
				}
				else
					logger.Debug("NON ho trovato Record in lGuidPredIsc in predisposizione");

				if ((lGuidProtIsc != null) && (lGuidProtIsc.Count > 0) && (lGuidPredIsc != null) && (lGuidPredIsc.Count > 0))
				{
					if (lGuidPredIsc.Count == lGuidProtIsc.Count)
					{
						logger.Debug("Creo il report massivo");

						List<int> lOnlyNumericPredPartGuid = new List<int>();
						foreach (Guid sSingleValue in lGuidPredIsc)
						{
							lOnlyNumericPredPartGuid.Add(int.Parse(sSingleValue.ToString().Substring(25)));
						}

						List<string> lDataForReport = new List<string>();
						lOnlyNumericPredPartGuid.Sort();
						foreach (long sSingleValue in lOnlyNumericPredPartGuid)
						{
							NameValueCollection IndexesForReport = null;
							CardBundle oCardBundleForReport;
							string singleRecordReportData = "";
							siavCardManager.GetCard(sSingleValue.ToString(), siavLogin, out oCardBundleForReport);
							siavCardManager.GetIndexes(oCardBundleForReport, out IndexesForReport);
							string sAppo = IndexesForReport[resourceFileManager.getConfigData("CancNamePredFieldIdProt")];
							string numProt = string.Empty;
							string dataProt = string.Empty;
							if (!string.IsNullOrEmpty(sAppo))
							{
								numProt = sAppo.Substring(0, sAppo.IndexOf("-"));
								dataProt = sAppo.Substring(sAppo.IndexOf("-") + 2, sAppo.Length - 2 - sAppo.IndexOf("-"));
							}

							singleRecordReportData = IndexesForReport[resourceFileManager.getConfigData("CancNameFiledAnagXls")] +
													 resourceFileManager.getConfigData("CancNameFieldIdProt") + "|" +
													 numProt + "|" +
													 resourceFileManager.getConfigData("CancNameFieldDataProt") + "|" +
													 dataProt + "|" +
													 resourceFileManager.getConfigData("CancNameFieldEmpty");
							logger.Debug("Record: " + singleRecordReportData);
							lDataForReport.Add(singleRecordReportData);
						}

						// Generazione del report
						string note = resourceFileManager.getConfigData("CancNoteReportMassivo");
						// Inserimento report come allegato scheda massiva

						string pathAttachment = fluxHelper.CreateReportMassive(this.sWorkingFolder, IdComMaxiva, lDataForReport);
						logger.Debug("Path report massivo: " + pathAttachment);

						siavCardManager.SetAttachment(oCardBundleIscMassive, siavLogin, pathAttachment, note);
						//
					}
				}
				siavLogin.Logout();
				siavLogin.siavWsLogin.Close();
				siavLogin = null;
				siavCardManager.siavWsCard.Close();
				siavCardManager = null;
			}
			catch (Exception ex)
			{
				try
				{
					siavCardManager.InsertNote(oCardBundleIscMassive, siavLogin, "Sono stati riscontrati degli errori, prego informare gli amministratori di sistema.", resourceFileManager.getConfigData("AutorAnnotation"));
					logger.Error(ex.ToString());
					if (siavCardManager != null)
					{
						siavCardManager.siavWsCard.Abort();
					}
					if (siavLogin != null)
					{
						siavLogin.Logout();
						siavLogin.siavWsLogin.Close();
					}
					sResult = "";
				}
				catch (Exception e) { }
			}
			finally
			{

				logger.Debug("Fine elaborazione");
				//siavLogin.Logout();
			}
			return sResult;
		}
		public string ToJson(object value)
		{
			var settings = new JsonSerializerSettings
			{
				ReferenceLoopHandling = ReferenceLoopHandling.Ignore
			};

			return JsonConvert.SerializeObject(value, Newtonsoft.Json.Formatting.Indented, settings);
		}
		public String CreateReportMassiveIsc(string IdComMaxiva, string sGuidCard)
		{
			WcfSiavLoginManager siavLogin = null;
			WcfSiavCardManager siavCardManager = null;
			CardBundle oCardBundleIscMassive = null;
			string sResult = "";
			try
			{
				siavLogin = new WcfSiavLoginManager();
				string oUserName = resourceFileManager.getConfigData("UserFlux");
				string oUserPwd = oRijndael.Decrypt(resourceFileManager.getConfigData("PasswordFlux"));
				Guid GUIdcard;
				// Esegue il login
				siavLogin.Login(oUserName, oUserPwd);
				logger.Debug("Sessione login Archiflow: " + siavLogin.oSessionInfo.SessionId);

				siavCardManager = new WcfSiavCardManager(logger);

				DocumentType oDocType = new DocumentType();
				Archive oArchive = new Archive();
				if (sGuidCard.Length > 12)                 // set the guid of the card
					GUIdcard = new Guid("64556990-b196-425c-a0b9-" + int.Parse(sGuidCard.Substring(24, 12)).ToString("000000000000"));
				else
					GUIdcard = new Guid("64556990-b196-425c-a0b9-" + int.Parse(sGuidCard).ToString("000000000000"));
				siavCardManager.GetCard(GUIdcard.ToString(), siavLogin, out oCardBundleIscMassive);
				// Verifica inserimento dati confrontando il numero delle schede in predisposizione con quelle protocollate
				List<SchedaSearchParameter> lSchedaSearchParameter = new List<SchedaSearchParameter>();

				List<string> lMetadati = null;
				SchedaSearchParameter schedaSearchParameter = new SchedaSearchParameter();
				schedaSearchParameter.Archivio = resourceFileManager.getConfigData("IscArcCardInError");
				schedaSearchParameter.TipologiaDocumentale = resourceFileManager.getConfigData("IscDocTypeCardInError");
				string[] arr = new string[20];
				arr[int.Parse(resourceFileManager.getConfigData("IscIndexFieldCardInError"))] = IdComMaxiva;

				logger.Debug("Inizio ricerca record");
				logger.Debug("  Cerco per Archivio: " + resourceFileManager.getConfigData("IscArcCardInError"));
				logger.Debug("  Cerco per TipologiaDocumentale: " + resourceFileManager.getConfigData("IscDocTypeCardInError"));
				logger.Debug("  Cerco per ID comunicazione massiva: " + IdComMaxiva);
				logger.Debug("Fine ricerca record");

				lMetadati = new List<string>(arr);
				schedaSearchParameter.MetaDati = lMetadati;
				lSchedaSearchParameter.Add(schedaSearchParameter);
				List<Guid> lGuidPredIsc;
				siavCardManager.getSearch(lSchedaSearchParameter, siavLogin, out lGuidPredIsc);
				logger.Debug("Ho trovato Record: " + lGuidPredIsc.Count + " in predisposizione");


				logger.Debug("Creo il report massivo");

				List<int> lOnlyNumericPredPartGuid = new List<int>();
				foreach (Guid sSingleValue in lGuidPredIsc)
				{
					lOnlyNumericPredPartGuid.Add(int.Parse(sSingleValue.ToString().Substring(25)));
				}

				List<string> lDataForReport = new List<string>();
				lOnlyNumericPredPartGuid.Sort();
				foreach (long sSingleValue in lOnlyNumericPredPartGuid)
				{
					NameValueCollection IndexesForReport = null;
					CardBundle oCardBundleForReport;
					string singleRecordReportData = "";
					siavCardManager.GetCard(sSingleValue.ToString(), siavLogin, out oCardBundleForReport);
					siavCardManager.GetIndexes(oCardBundleForReport, out IndexesForReport);
					string sAppo = IndexesForReport[resourceFileManager.getConfigData("InsNamePredFieldIdProt")];
					string numProt = string.Empty;
					string dataProt = string.Empty;
					if (!string.IsNullOrEmpty(sAppo))
					{
						numProt = sAppo.Substring(0, sAppo.IndexOf("-"));
						dataProt = sAppo.Substring(sAppo.IndexOf("-") + 2, sAppo.Length - 2 - sAppo.IndexOf("-"));
					}

					singleRecordReportData = IndexesForReport[resourceFileManager.getConfigData("InsNameFiledAnagXls")] +
												resourceFileManager.getConfigData("InsNameFieldIdProt") + "|" +
												numProt + "|" +
												resourceFileManager.getConfigData("InsNameFieldDataProt") + "|" +
												dataProt + "|" +
												resourceFileManager.getConfigData("InsNameFieldEmpty");
					logger.Debug("Record: " + singleRecordReportData);
					lDataForReport.Add(singleRecordReportData);
				}

				// Generazione del report
				FluxHelper fluxHelper = new FluxHelper();
				string note = resourceFileManager.getConfigData("IscNoteReportMassivo");
				// Inserimento report come allegato scheda massiva

				string pathAttachment = fluxHelper.CreateReportMassive(this.sWorkingFolder, IdComMaxiva, lDataForReport);
				logger.Debug("Path report massivo: " + pathAttachment);

				siavCardManager.SetAttachment(oCardBundleIscMassive, siavLogin, pathAttachment, note);

				siavLogin.Logout();
				siavLogin.siavWsLogin.Close();
				siavLogin = null;
				siavCardManager.siavWsCard.Close();
				siavCardManager = null;
			}
			catch (Exception ex)
			{
				try
				{
					logger.Error(ex.ToString());
					if (siavCardManager != null)
					{
						siavCardManager.siavWsCard.Abort();
					}
					if (siavLogin != null)
					{
						siavLogin.Logout();
						siavLogin.siavWsLogin.Close();
					}
				}
				catch (Exception e) { }
			}
			finally
			{

				logger.Debug("Fine elaborazione");
				//siavLogin.Logout();
			}
			return sResult;
		}
		public Boolean CreateCardReport(string sIndex, string sIndexValue)
		{
			string sPathFile = "";
			List<String> sResult = new List<string>();
			bool bResult = false;
			SVAOLLib.Offices oMailOffices = new SVAOLLib.Offices();
			SVAOLLib.Offices oOffices = new SVAOLLib.Offices();
			SVAOLLib.Groups oMailGroups = new SVAOLLib.Groups();
			SVAOLLib.Groups oGroups = new SVAOLLib.Groups();

			SVAOLLib.Users oMailUsers = new SVAOLLib.Users();
			SVAOLLib.Users oUsers = new SVAOLLib.Users();

			SVAOLLib.Card oCard = new SVAOLLib.Card();
			SVAOLLib.Session oSession = new SVAOLLib.Session();
			SVAOLLib.Fields oFields = new SVAOLLib.Fields();
			SVAOLLib.Archive oArchive = new SVAOLLib.Archive();
			SVAOLLib.DocumentType oTypeDocument = new SVAOLLib.DocumentType();
			string sGuidTransaction = Guid.NewGuid().ToString();
			string sConnection = "";
			ConnectionManager connectionManager = new ConnectionManager(logger);
			try
			{

				string sDateFrom = "";
				string sDateTo = "";
				string sReportType = resourceFileManager.getConfigData("ReportPeriod").ToUpper();
				System.Globalization.CultureInfo MyCultureInfo = new System.Globalization.CultureInfo("it-IT");
				Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("it-IT");
				if (sReportType == "C")
				{
					DateTime dtFrom = DateTime.Parse(resourceFileManager.getConfigData("CustomPeriodFrom"), MyCultureInfo);
					DateTime dtTo = DateTime.Parse(resourceFileManager.getConfigData("CustomPeriodTill"), MyCultureInfo);

					sDateFrom = dtFrom.ToString();
					sDateTo = dtTo.ToString();
				}
				else if (sReportType == "M")
				{
					// configurazione ultimo mese
					var today = DateTime.Today;
					var month = new DateTime(today.Year, today.Month, 1);

					sDateFrom = month.AddMonths(-1).ToString();
					sDateTo = month.AddDays(-1).ToString();
				}
				else if (sReportType == "D")
				{
					// configurazione ultimo giorno
					var today = DateTime.Today;
					sDateFrom = today.AddDays(-1).ToString();
					sDateTo = today.AddDays(-1).ToString();
				}
				else if (sReportType == "Y")
				{
					//configurazione ultimo anno
					var today = DateTime.Today;
					var firstDay = new DateTime(today.Year, 1, 1);
					var lastDay = new DateTime(today.Year, 12, 31);
					// configurazione ultimo giorno
					sDateFrom = firstDay.AddYears(-1).ToString();
					sDateTo = lastDay.AddYears(-1).ToString();
				}
				var sNamePageXls = resourceFileManager.getConfigData("NamePageXls");
				logger.Debug("Tipo di ricerca: " + sReportType);
				logger.Debug("A partire da: " + sDateFrom);
				logger.Debug("Fino a: " + sDateTo);
				QueryDataForReport oQueryDataForReport = new QueryDataForReport();
				ExcelManager oExcelManager = new ExcelManager();
				TimeSpan ts = new TimeSpan(23, 59, 59);
				DateTime dtDateFrom = DateTime.Parse(sDateFrom, MyCultureInfo);
				DateTime dtDateTo = DateTime.Parse(sDateTo, MyCultureInfo);
				dtDateTo = dtDateTo.Date + ts;
				logger.Debug("fino a: " + dtDateTo.ToString());

				var parameters = new List<OracleParameter>
				{
					new OracleParameter{ ParameterName = "DATA1", Value = dtDateFrom, OracleDbType = OracleDbType.Date},
					new OracleParameter{ ParameterName = "DATA2", Value = dtDateTo, OracleDbType = OracleDbType.Date},
				};
				var result = oQueryDataForReport.getDataForReport(parameters);
				if (result != null)
				{
					logger.Debug("Individuati:" + result.Count + " record.");
					if (result.Count > 0)
					{
						sNamePageXls = dtDateFrom.ToString().Substring(1, 10) + "-" + dtDateTo.ToString().Substring(1, 10);
						sPathFile = oExcelManager.CreateReportMassive(sWorkingFolder, sNamePageXls, "REPORT_" + sNamePageXls.Replace('/', '.'), result);
						logger.Debug("Report creato nel path:" + sPathFile);
					}
					else
					{
						logger.Debug("Report NON creato perchè non sono stati individuati record da esportare.");
					}
				}


				string oUserName = resourceFileManager.getConfigData("UserFlux");
				string oUserPwd = oRijndael.Decrypt(resourceFileManager.getConfigData("PasswordFlux"));
				sConnection = connectionManager.OpenUserConnect(oUserName, oUserPwd);
				logger.Debug("Apertura connessione: " + sConnection);
				CardManager oCardManager = new CardManager(logger);//sConnection
				oTypeDocument.Id = DocManager.GetIdDocTypeByName(sConnection, resourceFileManager.getConfigData("RepTypeDoc").ToString(), sGuidTransaction);
				logger.Debug("oTypeDocument.Id: " + oTypeDocument.Id);

				oTypeDocument.GUIDconnect = sConnection;
				oTypeDocument.LoadDocumentTypeFromId();
				oArchive.GUIDconnect = sConnection;
				oArchive.Id = DocManager.GetIdArchiveByName(sConnection, resourceFileManager.getConfigData("RepArchive").ToString(), sGuidTransaction);
				logger.Debug("oArchive.Id: " + oArchive.Id);
				oArchive.LoadFromId();
				oSession.GUIDconnect = sConnection;
				oCard.GUIDconnect = sConnection;
				oCard.Archive = oArchive;
				oCard.DocType = oTypeDocument;
				foreach (SVAOLLib.Field oField in oTypeDocument.Fields)
				{
					SVAOLLib.Field oFieldSelected = new SVAOLLib.Field();
					if (oField.Id == SVAOLLib.svIdField.svIfKey11)
					{
						oFieldSelected.Id = oField.Id;
						oFieldSelected.Value = resourceFileManager.getConfigData("RepOfficeName");
						logger.Debug("RepOfficeName: " + oFieldSelected.Value);
						oFields.Add(oFieldSelected);
					}
					else if (oField.Id == SVAOLLib.svIdField.svIfKey12)
					{
						oFieldSelected.Id = oField.Id;
						oFieldSelected.Value = resourceFileManager.getConfigData("RepType");
						logger.Debug("RepType: " + oFieldSelected.Value);
						oFields.Add(oFieldSelected);
					}
					else if (oField.Id == SVAOLLib.svIdField.svIfKey13)
					{
						oFieldSelected.Id = oField.Id;
						oFieldSelected.Value = sIndex;
						logger.Debug("sIndex: " + sIndex);
						oFields.Add(oFieldSelected);
					}
					else if (oField.Id == SVAOLLib.svIdField.svIfKey14)
					{
						oFieldSelected.Id = oField.Id;
						oFieldSelected.Value = sIndexValue;
						logger.Debug("sIndexValue: " + sIndexValue);
						oFields.Add(oFieldSelected);
					}
					else if (oField.Id == SVAOLLib.svIdField.svIfKey15)
					{
					}
					else if (oField.Id == SVAOLLib.svIdField.svIfKey21)
					{
					}
					else if (oField.Id == SVAOLLib.svIdField.svIfKey22)
					{
					}
					else if (oField.Id == SVAOLLib.svIdField.svIfKey23)
					{
					}
					else if (oField.Id == SVAOLLib.svIdField.svIfKey24)
					{

					}
					else if (oField.Id == SVAOLLib.svIdField.svIfKey25)
					{
					}
					else if (oField.Id == SVAOLLib.svIdField.svIfKey31)
					{
					}
					else if (oField.Id == SVAOLLib.svIdField.svIfKey32)
					{
					}
					else if (oField.Id == SVAOLLib.svIdField.svIfKey33)
					{
					}
					else if (oField.Id == SVAOLLib.svIdField.svIfKey34)
					{
					}
					else if (oField.Id == SVAOLLib.svIdField.svIfKey35)
					{
					}
					else if (oField.Id == SVAOLLib.svIdField.svIfKey41)
					{   // ex altro
					}
					else if (oField.Id == SVAOLLib.svIdField.svIfKey42)
					{
					}
					else if (oField.Id == SVAOLLib.svIdField.svIfKey43)
					{
					}
					else if (oField.Id == SVAOLLib.svIdField.svIfKey44)
					{
					}
					else if (oField.Id == SVAOLLib.svIdField.svIfKey45)
					{
					}
					else if (oField.Id == SVAOLLib.svIdField.svIfObj)
					{
					}
					else if (oField.Id == SVAOLLib.svIdField.svIfProtocol)
					{
					}
					else if (oField.Id == SVAOLLib.svIdField.svIfDateDoc)
					{
					}

				}
				oCard.Fields = oFields;
				logger.Debug("CAMPI: " + ToJson(oCard.Fields));
				var SendObjxml = oCard.GetVisibilityAsXML();
				logger.Debug(ToJson(SendObjxml));

				var oVisibility = UtilAction.getEntityVisibilityFromCard(SendObjxml);
				logger.Debug(ToJson(oVisibility));

				oUsers = UtilAction.getUsersFromSharePredefinite(oVisibility);
				logger.Debug(ToJson(oUsers));
				oGroups = UtilAction.getGroupsFromSharePredefinite(oVisibility);
				logger.Debug(ToJson(oGroups));
				oOffices = UtilAction.getOfficesFromSharePredefinite(oVisibility);
				logger.Debug(ToJson(oOffices));

				oMailUsers = UtilAction.getUsersMailFromSharePredefinite(oVisibility);
				oMailGroups = UtilAction.getGroupsMailFromSharePredefinite(oVisibility);
				oMailOffices = UtilAction.getOfficesMailFromSharePredefinite(oVisibility);
				oCard.Offices = oOffices;
				oCard.Users = oUsers;
				oCard.Groups = oGroups;
				// Inserisco la card in Archivio
				oCard.Insert(oMailOffices, oMailGroups, oMailUsers, "", "", 0);
				string sbodyMsg = "In allegato il report giornaliero delle fatture XML.";

				if (sPathFile != string.Empty)
				{
					DocManager.SetMainDocByteArr(sConnection, oCard.GuidCard, File.ReadAllBytes(sPathFile), Path.GetFileName(sPathFile), sGuidTransaction);
				}
				else
					sbodyMsg = "Nessuna fattura riscontrata, il report non è stato generato.";

				//Ottengo il progressivo assouto della scheda appena inserita
				connectionManager.CloseConnect();
				SendMail sendMail = new SendMail();
				string sReceiverEmail = resourceFileManager.getConfigData("EmailRecProtBillXml");
				string sSenderNameEmail = resourceFileManager.getConfigData("emailNomeMittente");
				string sObject = "MESSAGGIO AUTOMATICO - REPORT FATTURE DEL " + sIndexValue;
				List<string> lPath = new List<string>();
				lPath.Add(sPathFile);
				sendMail.SENDMAIL(sReceiverEmail, sSenderNameEmail, sObject, sbodyMsg, "", lPath.ToArray());
			}
			catch (Exception ex)
			{
				try
				{
					SendMail sendMail = new SendMail();
					string sReceiverEmail = resourceFileManager.getConfigData("EmailRecAnagErrorBillXml");
					string sSenderNameEmail = resourceFileManager.getConfigData("emailNomeMittente");
					string sObject = "MESSAGGIO AUTOMATICO - ERRORE GENERAZIONE REPORT FATTURE DEL " + sIndexValue;
					string sbodyMsg = "MESSAGGIO: " + ToJson(ex);
					sendMail.SENDMAIL(sReceiverEmail, sSenderNameEmail, sObject, sbodyMsg, "");
					logger.Error(ToJson(ex));

					if (sConnection != "")
						connectionManager.CloseConnect();
				}
				catch (Exception e) { }
			}
			finally
			{
				logger.Debug("Fine elaborazione");
				//siavLogin.Logout();
			}

			//try
			//{
			//	siavLogin = new WcfSiavLoginManager();
			//	string oUserName = resourceFileManager.getConfigData("UserFlux");
			//	string oUserPwd = oRijndael.Decrypt(resourceFileManager.getConfigData("PasswordFlux"));
			//	// Esegue il login
			//	siavLogin.Login(oUserName, oUserPwd);
			//	logger.Debug("Sessione login Archiflow: " + siavLogin.oSessionInfo.SessionId);

			//	siavCardManager = new WcfSiavCardManager(logger);
			//	Guid outCard = new Guid();
			//	Archive oArchive;
			//	DocumentType oDocumentType;

			//	logger.Debug("Recuper l'oggetto Archivio: " + resourceFileManager.getConfigData("RepArchive").ToString() + " e tipologia documentale" + resourceFileManager.getConfigData("RepTypeDoc").ToString());

			//	Model.Siav.APFlibrary.Model.FieldsCard oFieldCard = new Model.Siav.APFlibrary.Model.FieldsCard();
			//	string[] oMetadati = new string[20];
			//	string sNote = "";
			//	string sMessage = "";
			//	oMetadati[0] = sTypeReport;
			//	oMetadati[1] = sIndex;
			//	oMetadati[2] = sIndexValue;
			//	oFieldCard.MetaDati = new List<string>(oMetadati);
			//	logger.Debug("Avvio il processo di inserimento");
			//	this.SetCardDefaultVisibility
			//	siavCardManager.Insert(sPathAttachmentsCard, sPathMainDoc, siavLogin, oFieldCard, oCardVisibility.Groups, oCardVisibility.Offices, oCardVisibility.Users, oDocumentType, oArchive, sNote, sMessage, null,null, out outCard);
			//	sResult.Add(outCard.ToString());
			//	logger.Debug("Scheda inserita, il suo id: " + outCard.ToString());
			//	siavLogin.Logout();
			//	siavLogin.siavWsLogin.Close();
			//	siavLogin = null;
			//	siavCardManager.siavWsCard.Close();
			//	siavCardManager = null;
			//}
			//catch (Exception ex)
			//{
			//	try
			//	{
			//		logger.Error(ex.ToString());
			//		if (siavCardManager != null)
			//		{
			//			siavCardManager.siavWsCard.Abort();
			//		}
			//		if (siavLogin != null)
			//		{
			//			siavLogin.Logout();
			//			siavLogin.siavWsLogin.Close();
			//		}
			//	}
			//	catch (Exception e) { }
			//}
			//finally
			//{
			//	logger.Debug("Fine elaborazione");
			//}
			return bResult;
		}
		public Boolean CreateReportFromSQL(string sNameSqlResource, string sWhereValue, string sEmailDestinationTo, string sEmailDestinationBcc)
		{
			string sPathFile = "";
			List<String> sResult = new List<string>();
			bool bResult = false;
			string sGuidTransaction = Guid.NewGuid().ToString();
			try
			{
				var arrSQL = sNameSqlResource.Split('§');
				List<string> lPath = new List<string>();
				foreach (string sSingleSQL in arrSQL)
				{
					QueryDataForReport oQueryDataForReport = new QueryDataForReport();
					ExcelManager oExcelManager = new ExcelManager();
					logger.Debug("Query:" + sSingleSQL + " record.");

					var result = oQueryDataForReport.getDataForReport(sSingleSQL, null);
					if (result != null)
					{
						logger.Debug("Individuati:" + result.Count + " record.");
						if (result.Count > 0)
						{
							sPathFile = oExcelManager.CreateReportMassive(sWorkingFolder, sSingleSQL, "REPORT_" + DateTime.Now.AddDays(-1).ToString("dd-MM-yyyy") + "_" + sSingleSQL.Replace('/', '.'), result);
							logger.Debug("Report creato nel path:" + sPathFile);
							lPath.Add(sPathFile);
						}
						else
						{
							logger.Debug("Report NON creato perchè non sono stati individuati record da esportare.");
						}
					}
				}
				SendMail sendMail = new SendMail();
				//string sReceiverEmail = resourceFileManager.getConfigData(sEmailDestination);
				string sSenderNameEmail = resourceFileManager.getConfigData("emailNomeMittente");
				string sObject = "MESSAGGIO AUTOMATICO - REPORT " + sNameSqlResource.Replace("§", " - ");
				sendMail.SENDMAIL(sEmailDestinationTo, sSenderNameEmail, sObject, "In allegato il report in oggetto.", sEmailDestinationBcc, lPath.ToArray());
			}
			catch (Exception ex)
			{
				try
				{
					SendMail sendMail = new SendMail();
					//string sReceiverEmail = resourceFileManager.getConfigData(sEmailDestination);
					string sSenderNameEmail = resourceFileManager.getConfigData("emailNomeMittente");
					string sObject = "MESSAGGIO AUTOMATICO - ERRORE GENERAZIONE REPORT " + sNameSqlResource.Replace("§", " - ");
					string sbodyMsg = "MESSAGGIO: " + ToJson(ex);
					sendMail.SENDMAIL(sEmailDestinationTo, sSenderNameEmail, sObject, sbodyMsg, "");
					logger.Error(ToJson(ex));
				}
				catch (Exception e) { }
			}
			finally
			{
				logger.Debug("Fine elaborazione");
			}
			return bResult;
		}
		public bool PredCreateReport(string sGuidCard)
		{
			CardBundle oCardBundle = null;
			WcfSiavLoginManager siavLogin = null;
			WcfSiavCardManager siavCardManager = null;
			bool bResult = false;
			try
			{
				siavLogin = new WcfSiavLoginManager();
				string oUserName = resourceFileManager.getConfigData("UserFlux");
				string oUserPwd = oRijndael.Decrypt(resourceFileManager.getConfigData("PasswordFlux"));

				// Esegue il login
				siavLogin.Login(oUserName, oUserPwd);
				logger.Debug("Sessione login Archiflow: " + siavLogin.oSessionInfo.SessionId);
				siavCardManager = new WcfSiavCardManager(logger);
				siavCardManager.GetCard(sGuidCard, siavLogin, out oCardBundle);
				logger.Debug("Scheda trovata: " + oCardBundle.CardId);
				if (oCardBundle != null)
				{
					NameValueCollection IndexesForReport = null;
					siavCardManager.GetIndexes(oCardBundle, out IndexesForReport);
					string sKeyReport = IndexesForReport[resourceFileManager.getConfigData("NomeReport")].Replace(" ", "_");
					string sPathXlsx = this.sWorkingFolder; //+ @"\Report_ " + sKeyReport + ".xlsx";
															// Leggo la query
					string sSQL = resourceFileManager.getConfigData("Query" + sKeyReport);
					// Leggo i parametri
					string sSQLParameters = resourceFileManager.getConfigData("Params" + sKeyReport);

					// Creo una Struttura con i Parametri
					List<string> lSqlParametersName = sSQLParameters.Split('|').ToList();

					var attrList = new List<QueryCondition>();
					int iIndex = 1;
					// Carichiamo i parametri che serviranno a produrre le n QUERY 
					foreach (string paramName in lSqlParametersName)
					{
						string sValue = "";
						List<string> lTempValue = new List<string>();
						sValue = IndexesForReport[resourceFileManager.getConfigData("NomeReportValue")];
						if (sValue.IndexOf("|") != -1)
						{
							lTempValue = sValue.Split('|').ToList();
						}
						else
						{
							lTempValue.Add(sValue);
						}
						QueryCondition qc = new QueryCondition();
						qc.Name = paramName;
						qc.PossibleValues = lTempValue;
						attrList.Add(qc);
						Console.WriteLine("Leggo il parametro: " + paramName + " - Valore:" + sValue);
						iIndex++;
					}
					// Genero le "n" query per ogni parametro letto

					var resultAllCases = attrList.Skip(1).Aggregate<QueryCondition, List<Variant>>(
						new List<Variant>(attrList[0].PossibleValues.Select(s => new Variant { AttributeValues = new Dictionary<QueryCondition, string> { { attrList[0], s } } })),
						(acc, atr) =>
						{
							var aggregateResult = new List<Variant>();

							foreach (var createdVariant in acc)
							{
								foreach (var possibleValue in atr.PossibleValues)
								{
									var newVariant = new Variant { AttributeValues = new Dictionary<QueryCondition, string>(createdVariant.AttributeValues) };
									newVariant.AttributeValues[atr] = possibleValue;
									aggregateResult.Add(newVariant);
								}
							}

							return aggregateResult;
						});
					foreach (var singleCasesParameter in resultAllCases)
					{
						string sNameFileXLS = sKeyReport + "_";
						var parameters = new List<OracleParameter>();

						foreach (var singleParameter in singleCasesParameter.AttributeValues)
						{
							logger.Debug("Parametro: " + singleParameter.Key.Name + " - Valore:" + singleParameter.Value);
							OracleParameter temporaryParam = new OracleParameter { ParameterName = singleParameter.Key.Name, Value = singleParameter.Value, OracleDbType = OracleDbType.Varchar2 };
							//if (singleParameter.Value.Length>=5) 
							//	sNameFileXLS += singleParameter.Value.Substring(0, 5).Trim() + "_";
							//else
							sNameFileXLS += singleParameter.Value.Trim() + "_";

							parameters.Add(temporaryParam);
						}
						QueryDataForReport oQueryDataForReport = new QueryDataForReport();
						ExcelManager oExcelManager = new ExcelManager();
						var result = oQueryDataForReport.getDataForReport(parameters, sSQL);
						if (result != null)
						{
							logger.Debug("Individuati:" + result.Count + " record.");
							if (result.Count > 0)
							{
								string sNamePageXls = sKeyReport;
								logger.Debug("Nome Pagina XLS: " + sNamePageXls);
								logger.Debug("Nome File XLS: " + sNameFileXLS.Replace('/', '.'));
								var sPathFile = oExcelManager.CreateReportMassive(@sPathXlsx, sNamePageXls, sNameFileXLS.Replace('/', '.'), result, sKeyReport);
								logger.Debug("Report creato nel path:" + sPathFile, 3);
								siavCardManager.SetMainDoc(oCardBundle, siavLogin, sPathFile);
								bResult = true;
							}
							else
							{
								logger.Debug("Report NON creato perchè non sono stati individuati record da esportare.");
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				try
				{
					logger.Debug(String.Format("{0}>>{1}>>{2}", "ERRORE : PredCreateReport", ex.Source, ex.Message, ex.StackTrace), ex);
					if (siavCardManager != null)
					{
						siavCardManager.siavWsCard.Abort();
					}
					if (siavLogin != null)
					{
						siavLogin.Logout();
						siavLogin.siavWsLogin.Close();
					}
				}
				catch (Exception e) { }
			}
			finally
			{
				logger.Debug("Fine elaborazione");
			}
			return bResult;
		}

		public String CreateReportMassiveIng(string IdComMaxiva, string sGuidCard)
		{
			WcfSiavLoginManager siavLogin = null;
			WcfSiavCardManager siavCardManager = null;
			CardBundle oCardBundleIscMassive = null;
			string sResult = "";
			try
			{
				siavLogin = new WcfSiavLoginManager();
				string oUserName = resourceFileManager.getConfigData("UserFlux");
				string oUserPwd = oRijndael.Decrypt(resourceFileManager.getConfigData("PasswordFlux"));
				Guid GUIdcard;
				// Esegue il login
				siavLogin.Login(oUserName, oUserPwd);
				logger.Debug("Sessione login Archiflow: " + siavLogin.oSessionInfo.SessionId);

				siavCardManager = new WcfSiavCardManager(logger);

				DocumentType oDocType = new DocumentType();
				Archive oArchive = new Archive();
				if (sGuidCard.Length > 12)                 // set the guid of the card
					GUIdcard = new Guid("64556990-b196-425c-a0b9-" + int.Parse(sGuidCard.Substring(24, 12)).ToString("000000000000"));
				else
					GUIdcard = new Guid("64556990-b196-425c-a0b9-" + int.Parse(sGuidCard).ToString("000000000000"));
				siavCardManager.GetCard(GUIdcard.ToString(), siavLogin, out oCardBundleIscMassive);
				// Verifica inserimento dati confrontando il numero delle schede in predisposizione con quelle protocollate
				List<SchedaSearchParameter> lSchedaSearchParameter = new List<SchedaSearchParameter>();

				List<string> lMetadati = null;
				SchedaSearchParameter schedaSearchParameter = new SchedaSearchParameter();
				schedaSearchParameter.Archivio = resourceFileManager.getConfigData("IngArcCardInError");
				schedaSearchParameter.TipologiaDocumentale = resourceFileManager.getConfigData("IngDocTypeCardInError");
				string[] arr = new string[20];
				arr[int.Parse(resourceFileManager.getConfigData("IngIndexFieldCardInError"))] = IdComMaxiva;

				logger.Debug("Inizio ricerca record");
				logger.Debug("  Cerco per Archivio: " + resourceFileManager.getConfigData("IngArcCardInError"));
				logger.Debug("  Cerco per TipologiaDocumentale: " + resourceFileManager.getConfigData("IngDocTypeCardInError"));
				logger.Debug("  Cerco per ID comunicazione massiva: " + IdComMaxiva);
				logger.Debug("Fine ricerca record");

				lMetadati = new List<string>(arr);
				schedaSearchParameter.MetaDati = lMetadati;
				lSchedaSearchParameter.Add(schedaSearchParameter);
				List<Guid> lGuidPredIsc;
				siavCardManager.getSearch(lSchedaSearchParameter, siavLogin, out lGuidPredIsc);
				logger.Debug("Ho trovato Record: " + lGuidPredIsc.Count + " in predisposizione");


				logger.Debug("Creo il report massivo");

				List<int> lOnlyNumericPredPartGuid = new List<int>();
				foreach (Guid sSingleValue in lGuidPredIsc)
				{
					lOnlyNumericPredPartGuid.Add(int.Parse(sSingleValue.ToString().Substring(25)));
				}

				List<string> lDataForReport = new List<string>();
				lOnlyNumericPredPartGuid.Sort();
				foreach (long sSingleValue in lOnlyNumericPredPartGuid)
				{
					NameValueCollection IndexesForReport = null;
					CardBundle oCardBundleForReport;
					string singleRecordReportData = "";
					siavCardManager.GetCard(sSingleValue.ToString(), siavLogin, out oCardBundleForReport);
					siavCardManager.GetIndexes(oCardBundleForReport, out IndexesForReport);
					string sAppo = IndexesForReport[resourceFileManager.getConfigData("InsNamePredFieldIdProt")];
					string numProt = string.Empty;
					string dataProt = string.Empty;
					if (!string.IsNullOrEmpty(sAppo))
					{
						numProt = sAppo.Substring(0, sAppo.IndexOf("-"));
						dataProt = sAppo.Substring(sAppo.IndexOf("-") + 2, sAppo.Length - 2 - sAppo.IndexOf("-"));
					}

					singleRecordReportData = IndexesForReport[resourceFileManager.getConfigData("InsNameFiledAnagXls")] +
												resourceFileManager.getConfigData("InsNameFieldIdProt") + "|" +
												numProt + "|" +
												resourceFileManager.getConfigData("InsNameFieldDataProt") + "|" +
												dataProt + "|" +
												resourceFileManager.getConfigData("InsNameFieldEmpty");
					logger.Debug("Record: " + singleRecordReportData);
					lDataForReport.Add(singleRecordReportData);
				}

				// Generazione del report
				FluxHelper fluxHelper = new FluxHelper();
				string note = resourceFileManager.getConfigData("IscNoteReportMassivo");
				// Inserimento report come allegato scheda massiva

				string pathAttachment = fluxHelper.CreateReportMassive(this.sWorkingFolder, IdComMaxiva, lDataForReport);
				logger.Debug("Path report massivo: " + pathAttachment);

				siavCardManager.SetAttachment(oCardBundleIscMassive, siavLogin, pathAttachment, note);

				siavLogin.Logout();
				siavLogin.siavWsLogin.Close();
				siavLogin = null;
				siavCardManager.siavWsCard.Close();
				siavCardManager = null;
			}
			catch (Exception ex)
			{
				try
				{
					logger.Error(ex.ToString());
					if (siavCardManager != null)
					{
						siavCardManager.siavWsCard.Abort();
					}
					if (siavLogin != null)
					{
						siavLogin.Logout();
						siavLogin.siavWsLogin.Close();
					}
				}
				catch (Exception e) { }
			}
			finally
			{

				logger.Debug("Fine elaborazione");
				//siavLogin.Logout();
			}
			return sResult;
		}
		public String CreateReportMassiveCanc(string IdComMaxiva, string sGuidCard)
		{
			WcfSiavLoginManager siavLogin = null;
			WcfSiavCardManager siavCardManager = null;
			CardBundle oCardBundleIscMassive = null;
			string sResult = "";
			try
			{
				siavLogin = new WcfSiavLoginManager();
				string oUserName = resourceFileManager.getConfigData("UserFlux");
				string oUserPwd = oRijndael.Decrypt(resourceFileManager.getConfigData("PasswordFlux"));
				Guid GUIdcard;
				// Esegue il login
				siavLogin.Login(oUserName, oUserPwd);
				logger.Debug("Sessione login Archiflow: " + siavLogin.oSessionInfo.SessionId);

				siavCardManager = new WcfSiavCardManager(logger);

				DocumentType oDocType = new DocumentType();
				Archive oArchive = new Archive();
				if (sGuidCard.Length > 12)                 // set the guid of the card
					GUIdcard = new Guid("64556990-b196-425c-a0b9-" + int.Parse(sGuidCard.Substring(24, 12)).ToString("000000000000"));
				else
					GUIdcard = new Guid("64556990-b196-425c-a0b9-" + int.Parse(sGuidCard).ToString("000000000000"));
				siavCardManager.GetCard(GUIdcard.ToString(), siavLogin, out oCardBundleIscMassive);
				// Verifica inserimento dati confrontando il numero delle schede in predisposizione con quelle protocollate
				List<SchedaSearchParameter> lSchedaSearchParameter = new List<SchedaSearchParameter>();

				List<string> lMetadati = null;
				SchedaSearchParameter schedaSearchParameter = new SchedaSearchParameter();
				schedaSearchParameter.Archivio = resourceFileManager.getConfigData("CancArcCardInError");
				schedaSearchParameter.TipologiaDocumentale = resourceFileManager.getConfigData("CancDocTypeCardInError");
				string[] arr = new string[20];
				arr[int.Parse(resourceFileManager.getConfigData("CancIndexFieldCardInError"))] = IdComMaxiva;

				logger.Debug("Inizio ricerca record");
				logger.Debug("  Cerco per Archivio: " + resourceFileManager.getConfigData("CancArcCardInError"));
				logger.Debug("  Cerco per TipologiaDocumentale: " + resourceFileManager.getConfigData("CancDocTypeCardInError"));
				logger.Debug("  Cerco per ID comunicazione massiva: " + IdComMaxiva);
				logger.Debug("Fine ricerca record");

				lMetadati = new List<string>(arr);
				schedaSearchParameter.MetaDati = lMetadati;
				lSchedaSearchParameter.Add(schedaSearchParameter);
				List<Guid> lGuidPredIsc;
				siavCardManager.getSearch(lSchedaSearchParameter, siavLogin, out lGuidPredIsc);
				logger.Debug("Ho trovato Record: " + lGuidPredIsc.Count + " in predisposizione");


				logger.Debug("Creo il report massivo");

				List<int> lOnlyNumericPredPartGuid = new List<int>();
				foreach (Guid sSingleValue in lGuidPredIsc)
				{
					lOnlyNumericPredPartGuid.Add(int.Parse(sSingleValue.ToString().Substring(25)));
				}

				List<string> lDataForReport = new List<string>();
				lOnlyNumericPredPartGuid.Sort();
				foreach (long sSingleValue in lOnlyNumericPredPartGuid)
				{
					NameValueCollection IndexesForReport = null;
					CardBundle oCardBundleForReport;
					string singleRecordReportData = "";
					siavCardManager.GetCard(sSingleValue.ToString(), siavLogin, out oCardBundleForReport);
					siavCardManager.GetIndexes(oCardBundleForReport, out IndexesForReport);
					string sAppo = IndexesForReport[resourceFileManager.getConfigData("InsNamePredFieldIdProt")];
					string numProt = string.Empty;
					string dataProt = string.Empty;
					if (!string.IsNullOrEmpty(sAppo))
					{
						numProt = sAppo.Substring(0, sAppo.IndexOf("-"));
						dataProt = sAppo.Substring(sAppo.IndexOf("-") + 2, sAppo.Length - 2 - sAppo.IndexOf("-"));
					}

					singleRecordReportData = IndexesForReport[resourceFileManager.getConfigData("InsNameFiledAnagXls")] +
												resourceFileManager.getConfigData("InsNameFieldIdProt") + "|" +
												numProt + "|" +
												resourceFileManager.getConfigData("InsNameFieldDataProt") + "|" +
												dataProt + "|" +
												resourceFileManager.getConfigData("InsNameFieldEmpty");
					logger.Debug("Record: " + singleRecordReportData);
					lDataForReport.Add(singleRecordReportData);
				}

				// Generazione del report
				FluxHelper fluxHelper = new FluxHelper();
				string note = resourceFileManager.getConfigData("IscNoteReportMassivo");
				// Inserimento report come allegato scheda massiva

				string pathAttachment = fluxHelper.CreateReportMassive(this.sWorkingFolder, IdComMaxiva, lDataForReport);
				logger.Debug("Path report massivo: " + pathAttachment);

				siavCardManager.SetAttachment(oCardBundleIscMassive, siavLogin, pathAttachment, note);

				siavLogin.Logout();
				siavLogin.siavWsLogin.Close();
				siavLogin = null;
				siavCardManager.siavWsCard.Close();
				siavCardManager = null;
			}
			catch (Exception ex)
			{
				try
				{
					logger.Error(ex.ToString());
					if (siavCardManager != null)
					{
						siavCardManager.siavWsCard.Abort();
					}
					if (siavLogin != null)
					{
						siavLogin.Logout();
						siavLogin.siavWsLogin.Close();
					}
				}
				catch (Exception e) { }
			}
			finally
			{

				logger.Debug("Fine elaborazione");
				//siavLogin.Logout();
			}
			return sResult;
		}
		public String CreateReportMassiveProVal(string IdComMaxiva, string sGuidCard)
		{
			WcfSiavLoginManager siavLogin = null;
			WcfSiavCardManager siavCardManager = null;
			CardBundle oCardBundleIscMassive = null;
			string sResult = "";
			try
			{
				siavLogin = new WcfSiavLoginManager();
				string oUserName = resourceFileManager.getConfigData("UserFlux");
				string oUserPwd = oRijndael.Decrypt(resourceFileManager.getConfigData("PasswordFlux"));
				Guid GUIdcard;
				// Esegue il login
				siavLogin.Login(oUserName, oUserPwd);
				logger.Debug("Sessione login Archiflow: " + siavLogin.oSessionInfo.SessionId);

				siavCardManager = new WcfSiavCardManager(logger);

				DocumentType oDocType = new DocumentType();
				Archive oArchive = new Archive();
				if (sGuidCard.Length > 12)                 // set the guid of the card
					GUIdcard = new Guid("64556990-b196-425c-a0b9-" + int.Parse(sGuidCard.Substring(24, 12)).ToString("000000000000"));
				else
					GUIdcard = new Guid("64556990-b196-425c-a0b9-" + int.Parse(sGuidCard).ToString("000000000000"));
				siavCardManager.GetCard(GUIdcard.ToString(), siavLogin, out oCardBundleIscMassive);
				// Verifica inserimento dati confrontando il numero delle schede in predisposizione con quelle protocollate
				List<SchedaSearchParameter> lSchedaSearchParameter = new List<SchedaSearchParameter>();

				List<string> lMetadati = null;
				SchedaSearchParameter schedaSearchParameter = new SchedaSearchParameter();
				schedaSearchParameter.Archivio = resourceFileManager.getConfigData("PvalArcCardInError");
				schedaSearchParameter.TipologiaDocumentale = resourceFileManager.getConfigData("PvalDocTypeCardInError");
				string[] arr = new string[20];
				arr[int.Parse(resourceFileManager.getConfigData("PvalIndexFieldCardInError"))] = IdComMaxiva;

				logger.Debug("Inizio ricerca record");
				logger.Debug("  Cerco per Archivio: " + resourceFileManager.getConfigData("IscArcCardInError"));
				logger.Debug("  Cerco per TipologiaDocumentale: " + resourceFileManager.getConfigData("IscDocTypeCardInError"));
				logger.Debug("  Cerco per ID comunicazione massiva: " + IdComMaxiva);
				logger.Debug("Fine ricerca record");

				lMetadati = new List<string>(arr);
				schedaSearchParameter.MetaDati = lMetadati;
				lSchedaSearchParameter.Add(schedaSearchParameter);
				List<Guid> lGuidPredIsc;
				siavCardManager.getSearch(lSchedaSearchParameter, siavLogin, out lGuidPredIsc);
				logger.Debug("Ho trovato Record: " + lGuidPredIsc.Count + " in predisposizione");


				logger.Debug("Creo il report massivo");

				List<int> lOnlyNumericPredPartGuid = new List<int>();
				foreach (Guid sSingleValue in lGuidPredIsc)
				{
					lOnlyNumericPredPartGuid.Add(int.Parse(sSingleValue.ToString().Substring(25)));
				}

				List<string> lDataForReport = new List<string>();
				lOnlyNumericPredPartGuid.Sort();
				foreach (long sSingleValue in lOnlyNumericPredPartGuid)
				{
					NameValueCollection IndexesForReport = null;
					CardBundle oCardBundleForReport;
					string singleRecordReportData = "";
					siavCardManager.GetCard(sSingleValue.ToString(), siavLogin, out oCardBundleForReport);
					siavCardManager.GetIndexes(oCardBundleForReport, out IndexesForReport);
					string sAppo = IndexesForReport[resourceFileManager.getConfigData("InsNamePredFieldIdProt")];
					string numProt = string.Empty;
					string dataProt = string.Empty;
					if (!string.IsNullOrEmpty(sAppo))
					{
						numProt = sAppo.Substring(0, sAppo.IndexOf("-"));
						dataProt = sAppo.Substring(sAppo.IndexOf("-") + 2, sAppo.Length - 2 - sAppo.IndexOf("-"));
					}

					singleRecordReportData = IndexesForReport[resourceFileManager.getConfigData("InsNameFiledAnagXls")] +
												resourceFileManager.getConfigData("InsNameFieldIdProt") + "|" +
												numProt + "|" +
												resourceFileManager.getConfigData("InsNameFieldDataProt") + "|" +
												dataProt + "|" +
												resourceFileManager.getConfigData("InsNameFieldEmpty");
					logger.Debug("Record: " + singleRecordReportData);
					lDataForReport.Add(singleRecordReportData);
				}

				// Generazione del report
				FluxHelper fluxHelper = new FluxHelper();
				string note = resourceFileManager.getConfigData("IscNoteReportMassivo");
				// Inserimento report come allegato scheda massiva

				string pathAttachment = fluxHelper.CreateReportMassive(this.sWorkingFolder, IdComMaxiva, lDataForReport);
				logger.Debug("Path report massivo: " + pathAttachment);

				siavCardManager.SetAttachment(oCardBundleIscMassive, siavLogin, pathAttachment, note);

				siavLogin.Logout();
				siavLogin.siavWsLogin.Close();
				siavLogin = null;
				siavCardManager.siavWsCard.Close();
				siavCardManager = null;
			}
			catch (Exception ex)
			{
				try
				{
					logger.Error(ex.ToString());
					if (siavCardManager != null)
					{
						siavCardManager.siavWsCard.Abort();
					}
					if (siavLogin != null)
					{
						siavLogin.Logout();
						siavLogin.siavWsLogin.Close();
					}
				}
				catch (Exception e) { }
			}
			finally
			{

				logger.Debug("Fine elaborazione");
				//siavLogin.Logout();
			}
			return sResult;
		}
		public object[] CasellarioCreator(string GUIdcard, out string sOutput)
		{
			List<String> sResult = new List<string>();
			sOutput = "";
			WcfSiavLoginManager siavLogin = null;
			WcfSiavAgrafManager wcfSiavAgrafManager = null;
			WcfSiavCardManager siavCardManager = null;
			// Campo indice protocollo di riferimento
			// Codice OCF
			CardBundle oCardBundle = null;
			string sLog = string.Empty;
			string sLogWarning = string.Empty;
			string sErrorObj = "";
			string sObject = "";
			string sSezTer = string.Empty;
			string sTipoDoc = string.Empty;
			string sIdProtocollo = string.Empty;
			List<long> lPdf = new List<long>();
			string sListaErrati = string.Empty;
			long lCountErrati = 0;
			long lCountNegativi = 0;
			long lCountPositivi = 0;
			string sListaNegativi = string.Empty;
			string sListaPositivi = string.Empty;
			string sLastRecordInserted = string.Empty;
			List<string> lDataForReport = null;
			CardBundle oCardBundleAttach = null;
			string sIdArchiveAttachmentProt = string.Empty;
			string sNumProtAttachmentProt = string.Empty;
			string sIdSchedaProtocollo = string.Empty;
			string sInformationGuide = string.Empty;
			lDataForReport = new List<string>();
			siavLogin = new WcfSiavLoginManager();
			wcfSiavAgrafManager = new WcfSiavAgrafManager(logger);
			FluxHelper fluxHelper = new FluxHelper();

			try
			{
				string oUserName = resourceFileManager.getConfigData("UserFlux");
				string oUserPwd = oRijndael.Decrypt(resourceFileManager.getConfigData("PasswordFlux"));
				NameValueCollection CasMassiveCardIndexes;
				MainDoc oMainDoc;
				List<AdditionalField> CasellarioCardAdditionalFields;
				// Esegue il login
				siavLogin.Login(oUserName, oUserPwd);
				logger.Debug("Sessione login Archiflow: " + siavLogin.oSessionInfo.SessionId);

				siavCardManager = new WcfSiavCardManager(logger);
				// Recupera la scheda ove è stato avviato il processo
				siavCardManager.GetCard(GUIdcard, siavLogin, out oCardBundle);

				// recupero gli indici della scheda massiva
				siavCardManager.GetIndexes(oCardBundle, out CasMassiveCardIndexes);
				sIdProtocollo = CasMassiveCardIndexes[resourceFileManager.getConfigData("CasIdProtocollo")];
				sIdSchedaProtocollo = CasMassiveCardIndexes[resourceFileManager.getConfigData("CasIdSchedaProtocollo")];
				string sNameTipoDocumento = "";
				logger.Debug("Recuperati i dati dalla scheda che ha avviato il processo : " + GUIdcard);
				if (CasMassiveCardIndexes[resourceFileManager.getConfigData("CasTipoDocProt")].ToLower().IndexOf("controlli") > -1)
				{
					sNameTipoDocumento = resourceFileManager.getConfigData("CasTypeDocCheckSubscription");
					sObject = resourceFileManager.getConfigData("CasObjectTypeDocCheckSubscription");
				}
				else if (CasMassiveCardIndexes[resourceFileManager.getConfigData("CasTipoDocProt")].ToLower().IndexOf("iscrizione") > -1)
				{
					sNameTipoDocumento = resourceFileManager.getConfigData("CasTypeDocSubscription");
					sObject = resourceFileManager.getConfigData("CasObjectTypeDocSubscription");
				}
				sTipoDoc = sNameTipoDocumento;
				string sColumnIdSubject = resourceFileManager.getConfigData("IscSearchValueUniqueFromAnag");
				// Recupero i dati della scheda documentale     
				// variabile che conterrà il valore dell'archivio delle singole schede da creare
				Archive oArchiveCas;
				// variabilw che conterrà il valore della tipologia documentale delle singole schede da creare
				DocumentType oDocumentTypeCas;
				siavCardManager.getArchiveDoc(resourceFileManager.getConfigData("CasArchiveSearchUnique").ToString(), siavLogin.oSessionInfo, out oArchiveCas);
				siavCardManager.getTypeDoc(sNameTipoDocumento, siavLogin.oSessionInfo, out oDocumentTypeCas);

				// Recupera il documento principale della scheda di protocollo in entrata massiva
				siavCardManager.GetMainDoc(Guid.Parse(sIdSchedaProtocollo), siavLogin.oSessionInfo, out oMainDoc);
				// Recupero i campi aggiuntivi della scheda di protocollo in entrata massiva
				siavCardManager.GetCardAdditionalFields(sIdSchedaProtocollo, siavLogin, out CasellarioCardAdditionalFields);
				logger.Debug("Recuperati i dati dalla scheda che ha avviato il processo : " + GUIdcard);
				string sPassword = "";
				if (CasellarioCardAdditionalFields != null)
					if (CasellarioCardAdditionalFields.Count > 0)
						sPassword = CasellarioCardAdditionalFields[0].Value;
				// Materializza il file sul filesystem
				string sAnagraphicXlsSource = this.sWorkingFolder + @"\" + oMainDoc.Filename + "_AnagSource." + oMainDoc.Extension;
				logger.Debug("Scrivo il documento principale: " + sAnagraphicXlsSource);
				fluxHelper.FileMaterialize(sAnagraphicXlsSource, oMainDoc.oByte);

				// Estraggo i file dall'archivio compresso.
				ZipManager zip = new ZipManager();
				zip.Decompress(sAnagraphicXlsSource, this.sWorkingFolder + "\\ZIP\\", sPassword);//oRijndael.Decrypt(sPassword));
				foreach (string file in Directory.EnumerateFiles(this.sWorkingFolder + "\\ZIP\\", "*.zip"))
				{
					zip.Decompress(file, this.sWorkingFolder + "\\ZIP\\", sPassword);// oRijndael.Decrypt(sPassword));
				}
				List<PersonaCasellario> negativi = new List<PersonaCasellario>();
				List<PersonaCasellario> positivi = new List<PersonaCasellario>();
				List<PersonaCasellario> errati = new List<PersonaCasellario>();
				List<PersonaDaVerificare> csvPeople = new List<PersonaDaVerificare>();

				//Selezione mittente -> rubrica generica
				AgrafCard svAgrafCardRub = new AgrafCard();
				svAgrafCardRub.CardContacts = new List<AgrafCardContact>();
				svAgrafCardRub.Tag = sIdIndexTagRubricaGen;

				logger.Debug("Dati rubrica caricati");
				List<AgrafCard> agrafCards;
				AgrafCard agrafTribunale;
				siavCardManager.GetAnagrafFromCard(oCardBundle, siavLogin, out agrafCards);
				if (agrafCards.Count > 0)
					agrafTribunale = agrafCards[0];
				else
				{
					sLog = "Non è stato trovato alcun mittente nella scheda massiva.";
					sInformationGuide = sLog;
					throw new ArgumentException(sLog);
				}

				// leggo e ordino i nomi dei file pdf all'interno della cartella
				foreach (string file in Directory.EnumerateFiles(this.sWorkingFolder + "\\ZIP\\", "*.pdf"))
				{
					lPdf.Add(int.Parse(Path.GetFileNameWithoutExtension(file)));
				}
				lPdf.Sort();
				//lPdf.Insert(0, 2);
				foreach (string file in Directory.EnumerateFiles(this.sWorkingFolder + "\\ZIP\\", "*.txt"))
				{
					if (Path.GetFileName(file).ToLower().IndexOf("_err") != -1)
					{
						logger.Debug("Elaboro il file : " + file);
						string text = System.IO.File.ReadAllText(file);
						string[] lines = Regex.Split(text, "\n");// text.Split(new string[] { Environment. }, StringSplitOptions.None);
						lines = lines.Where(x => !string.IsNullOrEmpty(x)).ToArray();
						for (int i = 0; i < lines.ToList().Count; i = i + 6)
						{
							logger.Debug("Elaboro la linea : " + lines[i]);

							PersonaCasellario persona = new PersonaCasellario();
							string parteNome = lines[i].Substring(lines[i].IndexOf("POSIZIONE") + "POSIZIONE".Length + 1,
																lines[i].Length - lines[i].IndexOf("POSIZIONE") - "POSIZIONE".Length - 1);
							logger.Debug("parteNome : " + parteNome);

							var NomeCognome = parteNome.Split('/');
							string Nome = NomeCognome[1].ToUpper();
							string Cognome = NomeCognome[0].ToUpper();
							logger.Debug("Nome : " + Nome);
							logger.Debug("Cognome : " + Cognome);

							string partePosizione = lines[i].Substring(0, lines[i].IndexOf("POSIZIONE") - 1);
							logger.Debug("partePosizione : " + partePosizione);

							persona.Nome = Nome;
							persona.Cognome = Cognome;
							persona.Posizione = int.Parse(partePosizione);
							persona.CittaNascita = lines[i + 1].Replace("NATO A", "").Trim().ToUpper();
							logger.Debug("CittaNascita : " + lines[i + 1].Replace("NATO A", "").Trim());
							string sDateToFormat = lines[i + 2].Replace("IL", "").Trim();
							var dataNascita = sDateToFormat.Split('/');
							persona.DataNascita = string.Format("{0,2:00}", int.Parse(dataNascita[2])) + "/" +
													string.Format("{0,2:00}", int.Parse(dataNascita[1])) + "/" +
													dataNascita[0];
							logger.Debug("persona.DataNascita : " + persona.DataNascita);
							persona.Genere = lines[i + 3].Replace("SESSO", "").Trim().ToUpper();
							logger.Debug("persona.Genere : " + persona.Genere);
							persona.Esito = lines[i + 5].Replace("ESITO", "").Trim();
							logger.Debug("persona.Esito : " + persona.Esito);

							lPdf.Insert(persona.Posizione - 1, 0);
							errati.Add(persona);
						}
					}
				}
				//Lettura dei file di testo per creare la lista Positivi, negativi else errati
				foreach (string file in Directory.EnumerateFiles(this.sWorkingFolder + "\\ZIP\\", "*.txt"))
				{
					if (Path.GetFileName(file).ToLower().IndexOf("_negativi") != -1 ||
						Path.GetFileName(file).ToLower().IndexOf("_positivi") != -1)
					{
						logger.Debug("Elaboro il file : " + file);
						string text = System.IO.File.ReadAllText(file);
						string[] lines = Regex.Split(text, "\n");// text.Split(new string[] { Environment. }, StringSplitOptions.None);
						lines = lines.Where(x => !string.IsNullOrEmpty(x)).ToArray();
						for (int i = 0; i < lines.ToList().Count; i = i + 6)
						{
							logger.Debug("Elaboro la linea : " + lines[i]);

							PersonaCasellario persona = new PersonaCasellario();
							string parteNome = lines[i].Substring(lines[i].IndexOf("POSIZIONE") + "POSIZIONE".Length + 1,
																lines[i].Length - lines[i].IndexOf("POSIZIONE") - "POSIZIONE".Length - 1);
							logger.Debug("parteNome : " + parteNome);

							var NomeCognome = parteNome.Split('/');
							string Nome = NomeCognome[1].ToUpper();
							string Cognome = NomeCognome[0].ToUpper();
							logger.Debug("Nome : " + Nome);
							logger.Debug("Cognome : " + Cognome);

							string partePosizione = lines[i].Substring(0, lines[i].IndexOf("POSIZIONE") - 1);
							logger.Debug("partePosizione : " + partePosizione);

							persona.Nome = Nome;
							persona.Cognome = Cognome;
							persona.Posizione = int.Parse(partePosizione);
							persona.CittaNascita = lines[i + 1].Replace("NATO A", "").Trim().ToUpper();
							logger.Debug("CittaNascita : " + lines[i + 1].Replace("NATO A", "").Trim());
							string sDateToFormat = lines[i + 2].Replace("IL", "").Trim();
							var dataNascita = sDateToFormat.Split('/');
							persona.DataNascita = string.Format("{0,2:00}", int.Parse(dataNascita[2])) + "/" +
													string.Format("{0,2:00}", int.Parse(dataNascita[1])) + "/" +
													dataNascita[0];
							logger.Debug("persona.DataNascita : " + persona.DataNascita);
							persona.Genere = lines[i + 3].Replace("SESSO", "").Trim().ToUpper();
							logger.Debug("persona.Genere : " + persona.Genere);
							persona.Esito = lines[i + 5].Replace("ESITO", "").Trim();
							logger.Debug("persona.Esito : " + persona.Esito);
							logger.Debug("Entro dentro PathCertificato - pdf trovati : " + lPdf.Count);
							persona.PathCertificato = this.sWorkingFolder + "\\ZIP\\" + lPdf[persona.Posizione - 1].ToString() + ".pdf";
							if (Path.GetFileName(file).ToLower().IndexOf("_negativi") != -1)
							{
								logger.Debug("Aggiungo negativo");
								negativi.Add(persona);
							}
							else if (Path.GetFileName(file).ToLower().IndexOf("_positivi") != -1)
							{
								logger.Debug("Aggiungo positivo");
								positivi.Add(persona);
							}
						}
					}
				}
				bool bCsvFound = false;
				string sPathCsv = "";
				// recupero file CSV o da allegato circolare o allegato alla scheda
				List<Siav.APFlibrary.Model.InternalAttachment> lPathAttachments = new List<Siav.APFlibrary.Model.InternalAttachment>();
				// cerco tra gli allegati circolari se presenti
				siavCardManager.GetCardInternalAttachments(GUIdcard, siavLogin, out lPathAttachments);
				if (lPathAttachments != null)
				{
					foreach (Siav.APFlibrary.Model.InternalAttachment singleInternalAttachment in lPathAttachments)
					{
						if (singleInternalAttachment.ArchiveId != "")
						{
							sIdArchiveAttachmentProt = singleInternalAttachment.ArchiveId;
							sNumProtAttachmentProt = singleInternalAttachment.Name;

							siavCardManager.GetCard(singleInternalAttachment.Name, singleInternalAttachment.ArchiveId, siavLogin, out oCardBundleAttach);
							List<string> sPathAttachmentsCard = new List<string>();
							siavCardManager.GetCardAttachments(sWorkingFolder, oCardBundleAttach.CardId.ToString(), siavLogin, out sPathAttachmentsCard);
							if (sPathAttachmentsCard != null)
							{
								foreach (string singlePath in sPathAttachmentsCard)
								{
									if (singlePath.ToLower().IndexOf(".csv") != -1)
									{
										bCsvFound = true;
										sPathCsv = singlePath;
										break;
									}
								}
							}
						}
					}
				}
				// Verifico se l'allegato esterno contiene un file CSV 
				if (bCsvFound == false)
				{
					List<string> sPathAttachmentsCard = new List<string>();
					siavCardManager.GetCardAttachments(sWorkingFolder, oCardBundle.CardId.ToString(), siavLogin, out sPathAttachmentsCard);
					if (sPathAttachmentsCard != null)
					{
						foreach (string singlePath in sPathAttachmentsCard)
						{
							if (singlePath.ToLower().IndexOf(".csv") != -1)
							{
								bCsvFound = true;
								sPathCsv = singlePath;
								break;
							}
						}
					}
				}
				if (bCsvFound == false)
				{
					sLog = "Non è stato trovato alcun file CSV, verificare la corretta istruzione della scheda massiva.";
					sInformationGuide = sLog;
					throw new ArgumentException(sLog);
				}
				if (File.Exists(sPathCsv))
				{
					string line;
					System.IO.StreamReader file =
						new System.IO.StreamReader(sPathCsv);
					while ((line = file.ReadLine()) != null)
					{
						string[] words = line.Split(';');
						if (words[0] != "")
							csvPeople.Add(new PersonaDaVerificare(words[1].Trim().ToUpper(), words[0].Trim().ToUpper(), words[4].Trim(), words[7].Trim().ToUpper(), words[8]));
					}
					file.Close();
				}
				string sEsitoCas = string.Empty;
				// verifica di merito sulle liste Positivi, negativi, errati e utenti su file CSV
				if (positivi.Count + negativi.Count + errati.Count != csvPeople.Count)
				{
					sLogWarning = "Incongruenza tra il totale dei dati sottomessi e il totale delle risposte ricevute.";
					if (positivi.Count + negativi.Count + errati.Count < csvPeople.Count)
					{
						sLogWarning += System.Environment.NewLine + "Gli esiti delle risposte sui soggetti ricevuti hanno un numero inferiore al numero dei soggetti richiesti da verificare. Nel particolare: ";
						var deltaPositivi = csvPeople.Where(b => positivi.Any(a => Regex.Replace(a.Nome, @"[^b-df-hl-np-tv-zB-DF-HL-NP-TV-Z]+", "") == Regex.Replace(b.Nome, @"[^b-df-hl-np-tv-zB-DF-HL-NP-TV-Z]+", "") &&
											   Regex.Replace(a.Cognome, @"[^b-df-hl-np-tv-zB-DF-HL-NP-TV-Z]+", "") == Regex.Replace(b.Cognome, @"[^b-df-hl-np-tv-zB-DF-HL-NP-TV-Z]+", "") &&
											   a.DataNascita == b.DataNascita &&
											   a.Genere == b.Genere));
						var deltaNegativi = csvPeople.Where(b => negativi.Any(a => Regex.Replace(a.Nome, @"[^b-df-hl-np-tv-zB-DF-HL-NP-TV-Z]+", "") == Regex.Replace(b.Nome, @"[^b-df-hl-np-tv-zB-DF-HL-NP-TV-Z]+", "") &&
														Regex.Replace(a.Cognome, @"[^b-df-hl-np-tv-zB-DF-HL-NP-TV-Z]+", "") == Regex.Replace(b.Cognome, @"[^b-df-hl-np-tv-zB-DF-HL-NP-TV-Z]+", "") &&
														a.DataNascita == b.DataNascita &&
														a.Genere == b.Genere));
						var deltaErrati = csvPeople.Where(b => errati.Any(a => Regex.Replace(a.Nome, @"[^b-df-hl-np-tv-zB-DF-HL-NP-TV-Z]+", "") == Regex.Replace(b.Nome, @"[^b-df-hl-np-tv-zB-DF-HL-NP-TV-Z]+", "") &&
														Regex.Replace(a.Cognome, @"[^b-df-hl-np-tv-zB-DF-HL-NP-TV-Z]+", "") == Regex.Replace(b.Cognome, @"[^b-df-hl-np-tv-zB-DF-HL-NP-TV-Z]+", "") &&
														a.DataNascita == b.DataNascita &&
														a.Genere == b.Genere));
						var deltaTotal = csvPeople.Except(deltaPositivi).Except(deltaNegativi).Except(deltaErrati);
						foreach (PersonaDaVerificare personaDaVerificare in deltaTotal)
						{
							sLogWarning += System.Environment.NewLine + "Il soggetto non è stato individuato nella risposta fornita dal casellario: " +
											System.Environment.NewLine + "NOME: " + personaDaVerificare.Nome + " " + personaDaVerificare.Cognome +
											System.Environment.NewLine + "CODICE FISCALE: " + personaDaVerificare.CodiceFiscale +
											System.Environment.NewLine + "DATA DI NASCITA: " + personaDaVerificare.DataNascita +
											System.Environment.NewLine + "GENERE: " + personaDaVerificare.Genere + System.Environment.NewLine;
						}
					}
				}

				wcfSiavAgrafManager.LoadRubriche();

				// associazione lista CERPA con ESITI da archivio compresso
				if (positivi.Count > 0)
				{
					//Regex.Replace("abcdefghilmnopqrstuvzABCDEFGHILMNOPQRSTUVZ!", @"[^b-df-hl-np-tv-zB-DF-HL-NP-TV-Z]+", "")l
					sEsitoCas = "POSITIVO";
					// associazione CSV responso CERPA positivi
					var allPositivi = positivi.Where(b => csvPeople.Any(a => Regex.Replace(a.Nome, @"[^b-df-hl-np-tv-zB-DF-HL-NP-TV-Z]+", "").Trim() == Regex.Replace(b.Nome, @"[^b-df-hl-np-tv-zB-DF-HL-NP-TV-Z]+", "").Trim() &&
											Regex.Replace(a.Cognome, @"[^b-df-hl-np-tv-zB-DF-HL-NP-TV-Z]+", "").Trim() == Regex.Replace(b.Cognome, @"[^b-df-hl-np-tv-zB-DF-HL-NP-TV-Z]+", "").Trim() &&
											a.DataNascita.Trim() == b.DataNascita.Trim() &&
											a.Genere.Trim() == b.Genere.Trim()));
					// processo le schede positive
					if (allPositivi.Count() != positivi.Count)
					{
						var allPositiviNonTrovati = positivi.Where(b => !csvPeople.Any(a => Regex.Replace(a.Nome, @"[^b-df-hl-np-tv-zB-DF-HL-NP-TV-Z]+", "").Trim() == Regex.Replace(b.Nome, @"[^b-df-hl-np-tv-zB-DF-HL-NP-TV-Z]+", "").Trim() &&
											Regex.Replace(a.Cognome, @"[^b-df-hl-np-tv-zB-DF-HL-NP-TV-Z]+", "").Trim() == Regex.Replace(b.Cognome, @"[^b-df-hl-np-tv-zB-DF-HL-NP-TV-Z]+", "").Trim() &&
											a.DataNascita.Trim() == b.DataNascita.Trim() &&
											a.Genere.Trim() == b.Genere.Trim()));
						sLogWarning += " Problema durante l'associazione dei record POSITIVI estratti dal file CSV creato per la richiesta al casellario e i record estratti dal file txt ricevuto dall'esito del controllo, per gli utente/i: ";
						foreach (var positivoNt in allPositiviNonTrovati)
						{
							sLogWarning += positivoNt.Nome + " " + positivoNt.Cognome + " " + positivoNt.DataNascita + "; ";
						}
						sInformationGuide = "Vi sono degli utenti con esito positivo che non sono stati associati." + sLogWarning;
						//throw new ArgumentException(sLog);
					}
					//else { 
					// Inserisco le schede a sistema dei positivi
					logger.Debug("Avvio la creazione delle singole schede documentali Positivi, in tutto: " + positivi.Count);
					foreach (PersonaCasellario personaCasellario in allPositivi)
					{
						List<AgrafCard> agrafCardsNegativi = new List<AgrafCard>();
						string personaCasellarioCf = csvPeople.Where(a => Regex.Replace(a.Nome, @"[^b-df-hl-np-tv-zB-DF-HL-NP-TV-Z]+", "").Trim() == Regex.Replace(personaCasellario.Nome, @"[^b-df-hl-np-tv-zB-DF-HL-NP-TV-Z]+", "").Trim() &&
																	Regex.Replace(a.Cognome, @"[^b-df-hl-np-tv-zB-DF-HL-NP-TV-Z]+", "").Trim() == Regex.Replace(personaCasellario.Cognome, @"[^b-df-hl-np-tv-zB-DF-HL-NP-TV-Z]+", "").Trim() &&
																	a.DataNascita.Trim() == personaCasellario.DataNascita.Trim() &&
																	a.Genere.Trim() == personaCasellario.Genere.Trim())
																	.Select(cf => cf.CodiceFiscale).FirstOrDefault().Trim();

						// Dopo aver ricavato il codice fiscale si eseguirà una ricerca per una verifica di esistenza del record
						// i filtri sono id comunicazione massiva e codice fiscale
						// verifico la presenza del documento all'interno della serie massiva
						// la ricerca viene eseguita tramite codice fiscale del soggetto e Protocollo di Riferimento ovvero l'id della comunicazione massiva
						List<string> lIfExistsMetadati;
						List<SchedaSearchParameter> lIfExistsSchedaSearchParameter = new List<SchedaSearchParameter>();
						List<Guid> lIfExistsGUID;

						SchedaSearchParameter IfExistsSchedaSearchParameter = new SchedaSearchParameter();
						IfExistsSchedaSearchParameter.Archivio = oArchiveCas.ArchiveName;
						IfExistsSchedaSearchParameter.TipologiaDocumentale = oDocumentTypeCas.DocumentTypeName;
						string[] IfExistsArr = new string[20];
						// Campo protocollo di riferimento
						IfExistsArr[IdField.IfKey21.GetHashCode()] = sIdProtocollo;
						// Codice Fiscale
						IfExistsArr[IdField.IfKey31.GetHashCode()] = personaCasellarioCf;

						logger.Debug("Inizio ricerca record");
						logger.Debug("  Cerco per Archivio: " + oArchiveCas.ArchiveName);
						logger.Debug("  Cerco per TipologiaDocumentale: " + oDocumentTypeCas.DocumentTypeName);
						logger.Debug("  Cerco per Codice Fiscale: " + personaCasellarioCf);
						logger.Debug("  Cerco per Id scheda massiva: " + sIdProtocollo);
						logger.Debug("Fine ricerca record");

						lIfExistsMetadati = new List<string>(IfExistsArr);
						IfExistsSchedaSearchParameter.MetaDati = lIfExistsMetadati;
						lIfExistsSchedaSearchParameter.Add(IfExistsSchedaSearchParameter);
						// Ricerca se la scheda sia già stata protocollata
						siavCardManager.getSearch(lIfExistsSchedaSearchParameter, siavLogin, out lIfExistsGUID);
						// Inserimento scheda protocollo
						if (lIfExistsGUID.Count == 0)
						{

							AgrafCardContact svCardContactApf = new AgrafCardContact();
							List<AgrafCard> agrafCardsProt = new List<AgrafCard>(); ;
							agrafCardsNegativi.Add(agrafTribunale);
							//Selezione soggetto -> Aspiranti e consulenti finanziari
							AgrafCard svAgrafCardApf = new AgrafCard();
							svAgrafCardApf.CardContacts = new List<AgrafCardContact>();
							svAgrafCardApf.Tag = sIdIndexTagRubricaApf;
							List<KeyValuePair<int, string>> lfield = new List<KeyValuePair<int, string>>();

							List<GenericEntity> anagUsersAspPF = wcfSiavAgrafManager.GetUsersForCas(personaCasellarioCf, resourceFileManager.getConfigData("NomeRubricaAspPf"), resourceFileManager.getConfigData("NomePersonRubricaSearch"));
							if (anagUsersAspPF.Count > 1)
							{
								// caso errore trovati più utenti con lo stesso codice fiscale
								sLog = "Sono stati trovati n°:" + anagUsersAspPF.Count + " record per il codice fiscale: " + personaCasellarioCf + ", verificare l'anagrafica " + resourceFileManager.getConfigData("NomeRubricaAspPf");
								sInformationGuide = sLog;
								throw new ArgumentException(sLog);
							}
							else if (anagUsersAspPF.Count == 1)
							{
								logger.Debug("Associazione anagrafica con rubrica Promotori finanziari avvenuta con successo");

								// caso corretto -- implementazione algoritmo Associazione con Anagrafica
								svCardContactApf.EntityId = new AgrafCardContactId();
								svCardContactApf.EntityId.ContactId = new AgrafEntityId();
								svCardContactApf.EntityId.ContactId.EntityId = anagUsersAspPF[0].EntityId.Id;
								svCardContactApf.EntityId.ContactId.Version = (int)anagUsersAspPF[0].EntityId.Version;
								// Codice Fiscale
								lfield.Add(new KeyValuePair<int, String>(IdField.IfKey31.GetHashCode(), anagUsersAspPF[0].TaxID));
								// codice OCF 
								lfield.Add(new KeyValuePair<int, String>(IdField.IfKey33.GetHashCode(), anagUsersAspPF[0].GenericEntityExternalId));
								// Codice Consob 
								lfield.Add(new KeyValuePair<int, String>(IdField.IfKey32.GetHashCode(), anagUsersAspPF[0].GenericEntityExternalId2));
								// Classifica
								lfield.Add(new KeyValuePair<int, String>(IdField.IfKey34.GetHashCode(), anagUsersAspPF[0].GenericEntityExternalId3));
								svAgrafCardApf.CardContacts.Add(svCardContactApf);
							}
							else
							{
								// caso in cui non è stato trovato l'utente per la scheda
								sLog = "Non è stato trovato alcun record per il codice fiscale: " + personaCasellarioCf + ", verificare l'anagrafica " + resourceFileManager.getConfigData("NomeRubricaAspPf");
								sInformationGuide = sLog;
								throw new ArgumentException(sLog);
							}
							agrafCardsNegativi.Add(svAgrafCardApf);
							//Selezione mittente -> rubrica generica
							Guid outCard = new Guid();
							lfield.Add(new KeyValuePair<int, String>(IdField.IfKey11.GetHashCode(), CasMassiveCardIndexes[resourceFileManager.getConfigData("CasMittente")]));
							if (CasMassiveCardIndexes[resourceFileManager.getConfigData("CasUfficioIstruttore")].IndexOf("SEZIONE TERRITORIALE 2") != -1)
								sSezTer = resourceFileManager.getConfigData("SezTer2");
							else
								sSezTer = resourceFileManager.getConfigData("SezTer1");
							lfield.Add(new KeyValuePair<int, String>(IdField.IfKey12.GetHashCode(), sSezTer));
							lfield.Add(new KeyValuePair<int, String>(IdField.IfKey13.GetHashCode(), CasMassiveCardIndexes[resourceFileManager.getConfigData("CasMezzoDiTrasmissione")]));
							lfield.Add(new KeyValuePair<int, String>(IdField.IfKey21.GetHashCode(), sIdProtocollo));
							lfield.Add(new KeyValuePair<int, String>(IdField.IfKey22.GetHashCode(), CasMassiveCardIndexes[resourceFileManager.getConfigData("CasFirmatario")]));
							lfield.Add(new KeyValuePair<int, String>(IdField.IfKey25.GetHashCode(), sEsitoCas));
							lfield.Add(new KeyValuePair<int, String>(IdField.IfObj.GetHashCode(), sObject));
							siavCardManager.InsertCardCas(lfield, oArchiveCas.ArchiveId, oDocumentTypeCas, siavLogin, agrafCardsNegativi, personaCasellario.PathCertificato, out outCard);
							CardManager oCardManager = new CardManager(logger);
							oCardManager.setCardVisibilityDefault(oUserName, oUserPwd, outCard.ToString());

							// Inserimento allegato circolare
							string sNote = personaCasellario.Nome + " " + personaCasellario.Cognome + " Esito: POSITIVO";
							NameValueCollection indexCardIntCas;
							siavCardManager.GetIndexes(outCard, siavLogin, out indexCardIntCas);
							sLastRecordInserted = indexCardIntCas[resourceFileManager.getConfigData("CasIdProtocollo")] + " - " + oArchiveCas.ArchiveName;
							bool bResult = siavCardManager.SetAttachmentIntenal(Guid.Parse(sIdSchedaProtocollo), siavLogin, outCard, indexCardIntCas[resourceFileManager.getConfigData("CasIdProtocollo")], oArchiveCas.ArchiveId.ToString(), sNote);

							sResult.Add(outCard.ToString());
						}
						lCountPositivi++;
						sListaPositivi += " Il sig.re/ra " + personaCasellario.Nome + " " + personaCasellario.Cognome + " nato il: " + personaCasellario.DataNascita + " a: " + personaCasellario.CittaNascita +
											" sesso: " + personaCasellario.Genere + System.Environment.NewLine;
						lDataForReport.Add("Sig.re/ra" + "|" + personaCasellario.Nome + " " + personaCasellario.Cognome + "|" +
												"Nato il" + "|" + personaCasellario.DataNascita + "|" +
												"A" + "|" + personaCasellario.CittaNascita + "|" +
												"Genere" + "|" + personaCasellario.Genere + "|" +
												"Esito" + "|POSITIVO");
						//}
					}
				}
				if (negativi.Count > 0)
				{
					sEsitoCas = "NEGATIVO";
					// associazione CSV responso CERPA negativi
					var allNegativi = negativi.Where(b => csvPeople.Any(a => Regex.Replace(a.Nome, @"[^b-df-hl-np-tv-zB-DF-HL-NP-TV-Z]+", "").Trim() == Regex.Replace(b.Nome, @"[^b-df-hl-np-tv-zB-DF-HL-NP-TV-Z]+", "").Trim() &&
																		Regex.Replace(a.Cognome, @"[^b-df-hl-np-tv-zB-DF-HL-NP-TV-Z]+", "").Trim() == Regex.Replace(b.Cognome, @"[^b-df-hl-np-tv-zB-DF-HL-NP-TV-Z]+", "").Trim() &&
																		a.DataNascita.Trim() == b.DataNascita.Trim() &&
																		a.Genere.Trim() == b.Genere.Trim()));
					// processo le schede negative
					if (allNegativi.Count() != negativi.Count)
					{
						var allNegativiNonTrovati = negativi.Where(b => !csvPeople.Any(a => Regex.Replace(a.Nome, @"[^b-df-hl-np-tv-zB-DF-HL-NP-TV-Z]+", "").Trim() == Regex.Replace(b.Nome, @"[^b-df-hl-np-tv-zB-DF-HL-NP-TV-Z]+", "").Trim() &&
																	   Regex.Replace(a.Cognome, @"[^b-df-hl-np-tv-zB-DF-HL-NP-TV-Z]+", "").Trim() == Regex.Replace(b.Cognome, @"[^b-df-hl-np-tv-zB-DF-HL-NP-TV-Z]+", "").Trim() &&
																	   a.DataNascita.Trim() == b.DataNascita.Trim() &&
																	   a.Genere.Trim() == b.Genere.Trim()));
						sLogWarning += " Problema durante l'associazione dei record NEGATIVI estratti dal file CSV creato per la richiesta al casellario e i record estratti dal file txt ricevuto dall'esito del controllo, per gli utente/i: ";
						foreach (var negativoNt in allNegativiNonTrovati)
						{
							sLogWarning += negativoNt.Nome + " " + negativoNt.Cognome + " " + negativoNt.DataNascita + "; ";
						}
						sInformationGuide = "Non sono stati associati degli elementi con esito negativo." + sLogWarning;
						//throw new ArgumentException(sLog);
					}
					//else
					//{
					logger.Debug("Avvio la creazione delle singole schede documentali Negativi, in tutto: " + negativi.Count);
					foreach (PersonaCasellario personaCasellario in allNegativi)
					{
						List<AgrafCard> agrafCardsNegativi = new List<AgrafCard>();

						// verifico la presenza del documento all'interno della serie massiva
						// la ricerca viene eseguita tramite codice fiscale del soggetto e Protocollo di Riferimento ovvero l'id della comunicazione massiva
						//allPositivi[0]
						string personaCasellarioCf = csvPeople.Where(a => Regex.Replace(a.Nome, @"[^b-df-hl-np-tv-zB-DF-HL-NP-TV-Z]+", "").Trim() == Regex.Replace(personaCasellario.Nome, @"[^b-df-hl-np-tv-zB-DF-HL-NP-TV-Z]+", "").Trim() &&
																  Regex.Replace(a.Cognome, @"[^b-df-hl-np-tv-zB-DF-HL-NP-TV-Z]+", "").Trim() == Regex.Replace(personaCasellario.Cognome, @"[^b-df-hl-np-tv-zB-DF-HL-NP-TV-Z]+", "").Trim() &&
																  a.DataNascita.Trim() == personaCasellario.DataNascita.Trim() &&
																  a.Genere.Trim() == personaCasellario.Genere.Trim())
																  .Select(cf => cf.CodiceFiscale).FirstOrDefault().Trim();
						// Dopo aver ricavato il codice fiscale si eseguirà una ricerca per una verifica di esistenza del record
						// i filtri sono id comunicazione massiva e codice fiscale
						// verifico la presenza del documento all'interno della serie massiva
						// la ricerca viene eseguita tramite codice fiscale del soggetto e Protocollo di Riferimento ovvero l'id della comunicazione massiva
						List<string> lIfExistsMetadati;
						List<SchedaSearchParameter> lIfExistsSchedaSearchParameter = new List<SchedaSearchParameter>();
						List<Guid> lIfExistsGUID;

						SchedaSearchParameter IfExistsSchedaSearchParameter = new SchedaSearchParameter();
						IfExistsSchedaSearchParameter.Archivio = oArchiveCas.ArchiveName;
						IfExistsSchedaSearchParameter.TipologiaDocumentale = oDocumentTypeCas.DocumentTypeName;
						string[] IfExistsArr = new string[20];
						// Campo protocollo di riferimento
						IfExistsArr[IdField.IfKey21.GetHashCode()] = sIdProtocollo;
						// Codice Fiscale
						IfExistsArr[IdField.IfKey31.GetHashCode()] = personaCasellarioCf;

						logger.Debug("Inizio ricerca record");
						logger.Debug("  Cerco per Archivio: " + oArchiveCas.ArchiveName);
						logger.Debug("  Cerco per TipologiaDocumentale: " + resourceFileManager.getConfigData("IscDocTypeCardInProt"));
						logger.Debug("  Cerco per Codice Fiscale: " + personaCasellarioCf);
						logger.Debug("  Cerco per Id scheda massiva: " + resourceFileManager.getConfigData("IscObjSearchUnique"));
						logger.Debug("Fine ricerca record");

						lIfExistsMetadati = new List<string>(IfExistsArr);
						IfExistsSchedaSearchParameter.MetaDati = lIfExistsMetadati;
						lIfExistsSchedaSearchParameter.Add(IfExistsSchedaSearchParameter);
						// Ricerca se la scheda sia già stata protocollata
						siavCardManager.getSearch(lIfExistsSchedaSearchParameter, siavLogin, out lIfExistsGUID);
						// Inserimento scheda protocollo
						if (lIfExistsGUID.Count == 0)
						{

							AgrafCardContact svCardContactApf = new AgrafCardContact();
							List<AgrafCard> agrafCardsProt = new List<AgrafCard>();
							agrafCardsNegativi.Add(agrafTribunale);
							//Selezione soggetto -> Aspiranti e consulenti finanziari
							AgrafCard svAgrafCardApf = new AgrafCard();
							svAgrafCardApf.CardContacts = new List<AgrafCardContact>();
							svAgrafCardApf.Tag = sIdIndexTagRubricaApf;
							List<KeyValuePair<int, string>> lfield = new List<KeyValuePair<int, string>>();

							List<GenericEntity> anagUsersAspPF = wcfSiavAgrafManager.GetUsersForCas(personaCasellarioCf, resourceFileManager.getConfigData("NomeRubricaAspPf"), resourceFileManager.getConfigData("NomePersonRubricaSearch"));
							if (anagUsersAspPF.Count > 1)
							{
								// caso errore trovati più utenti con lo stesso codice fiscale
								sLog = "Sono stati trovati n°:" + anagUsersAspPF.Count + " record per il codice fiscale: " + personaCasellarioCf + ", verificare l'anagrafica " + resourceFileManager.getConfigData("NomeRubricaAspPf");
								sInformationGuide = sLog;
								throw new ArgumentException(sLog);
							}
							else if (anagUsersAspPF.Count == 1)
							{
								logger.Debug("Associazione anagrafica con rubrica Promotori finanziari avvenuta con successo");

								// caso corretto -- implementazione algoritmo Associazione con Anagrafica
								svCardContactApf.EntityId = new AgrafCardContactId();
								svCardContactApf.EntityId.ContactId = new AgrafEntityId();
								svCardContactApf.EntityId.ContactId.EntityId = anagUsersAspPF[0].EntityId.Id;
								svCardContactApf.EntityId.ContactId.Version = (int)anagUsersAspPF[0].EntityId.Version;
								// Codice Fiscale
								lfield.Add(new KeyValuePair<int, String>(IdField.IfKey31.GetHashCode(), anagUsersAspPF[0].TaxID));
								// codice OCF 
								lfield.Add(new KeyValuePair<int, String>(IdField.IfKey33.GetHashCode(), anagUsersAspPF[0].GenericEntityExternalId));
								// Codice Consob 
								lfield.Add(new KeyValuePair<int, String>(IdField.IfKey32.GetHashCode(), anagUsersAspPF[0].GenericEntityExternalId2));
								// Classifica
								lfield.Add(new KeyValuePair<int, String>(IdField.IfKey34.GetHashCode(), anagUsersAspPF[0].GenericEntityExternalId3));
								svAgrafCardApf.CardContacts.Add(svCardContactApf);
							}
							else
							{
								// caso errore trovati più utenti con lo stesso codice fiscale
								sLog = "Non è stato trovato alcun record per il codice fiscale: " + personaCasellarioCf + ", verificare l'anagrafica " + resourceFileManager.getConfigData("NomeRubricaAspPf");
								sInformationGuide = sLog;
								throw new ArgumentException(sLog);
							}
							agrafCardsNegativi.Add(svAgrafCardApf);
							//Selezione mittente -> rubrica generica
							Guid outCard = new Guid();

							lfield.Add(new KeyValuePair<int, String>(IdField.IfKey11.GetHashCode(), CasMassiveCardIndexes[resourceFileManager.getConfigData("CasMittente")]));

							if (CasMassiveCardIndexes[resourceFileManager.getConfigData("CasUfficioIstruttore")].ToUpper().IndexOf("MILANO") != -1)
								sSezTer = resourceFileManager.getConfigData("SezTer2");
							else
								sSezTer = resourceFileManager.getConfigData("SezTer1");
							lfield.Add(new KeyValuePair<int, String>(IdField.IfKey12.GetHashCode(), sSezTer));
							lfield.Add(new KeyValuePair<int, String>(IdField.IfKey13.GetHashCode(), CasMassiveCardIndexes[resourceFileManager.getConfigData("CasMezzoDiTrasmissione")]));
							lfield.Add(new KeyValuePair<int, String>(IdField.IfKey21.GetHashCode(), sIdProtocollo));
							lfield.Add(new KeyValuePair<int, String>(IdField.IfKey22.GetHashCode(), CasMassiveCardIndexes[resourceFileManager.getConfigData("CasFirmatario")]));
							lfield.Add(new KeyValuePair<int, String>(IdField.IfKey25.GetHashCode(), sEsitoCas));
							lfield.Add(new KeyValuePair<int, String>(IdField.IfObj.GetHashCode(), sObject));

							siavCardManager.InsertCardCas(lfield, oArchiveCas.ArchiveId, oDocumentTypeCas, siavLogin, agrafCardsNegativi, personaCasellario.PathCertificato, out outCard);
							CardManager oCardManager = new CardManager(logger);
							oCardManager.setCardVisibilityDefault(oUserName, oUserPwd, outCard.ToString());

							// Inserimento allegato circolare
							string sNote = personaCasellario.Nome + " " + personaCasellario.Cognome + " Esito: NEGATIVO";
							NameValueCollection indexCardIntCas;
							siavCardManager.GetIndexes(outCard, siavLogin, out indexCardIntCas);
							sLastRecordInserted = indexCardIntCas[resourceFileManager.getConfigData("CasIdProtocollo")] + " - " + oArchiveCas.ArchiveName;
							bool bResult = siavCardManager.SetAttachmentIntenal(Guid.Parse(sIdSchedaProtocollo), siavLogin, outCard, indexCardIntCas[resourceFileManager.getConfigData("CasIdProtocollo")], oArchiveCas.ArchiveId.ToString(), sNote);
							// Per ogni inserimento genero l'allegato circolare con il documento protocollato.

							sResult.Add(outCard.ToString());
						}
						lCountNegativi++;
						sListaNegativi += " Il sig.re/ra " + personaCasellario.Nome + " " + personaCasellario.Cognome + " nato il: " + personaCasellario.DataNascita + " a: " + personaCasellario.CittaNascita +
											" sesso: " + personaCasellario.Genere + System.Environment.NewLine;
						lDataForReport.Add("Sig.re/ra" + "|" + personaCasellario.Nome + " " + personaCasellario.Cognome + "|" +
							"Nato il" + "|" + personaCasellario.DataNascita + "|" +
							"A" + "|" + personaCasellario.CittaNascita + "|" +
							"Genere" + "|" + personaCasellario.Genere + "|" +
							"Esito" + "|NEGATIVO");
					}
					// processo le schede erronee
					if (errati.Count > 0)
					{
						// Inserisco le schede a sistema dei negativi
						foreach (PersonaCasellario erroneo in errati)
						{
							lCountErrati++;
							sListaErrati += " Il sig.re/ra " + erroneo.Nome + " " + erroneo.Cognome + " nato il: " + erroneo.DataNascita + " a: " + erroneo.CittaNascita +
											" sesso: " + erroneo.Genere + System.Environment.NewLine;
						}
						//}
					}
				}
				siavCardManager.FieldModify(oCardBundle, siavLogin, IdField.IfKey14.GetHashCode(), "COMPLETATO");
				siavCardManager.InsertNote(oCardBundle, siavLogin, "Attività di creazione schede terminata, consultare il report massivo allegato per i dettagli. Sono state inserite n°.: " + sResult.Count + " scheda/e.", resourceFileManager.getConfigData("AutorAnnotation"));
				logger.Debug("Fine elaborazione");
				//}
				//else
				//    throw new ArgumentException("Errore conteggio creazione liste, si è idenitificato: lista negativi: " + negativi.Count + " positivi: " + positivi.Count + " errati: " + errati.Count + " Utenti su CSV: " + csvPeople.Count);
			}
			catch (Exception ex)
			{
				sErrorObj = "ERRORE ";
				// sOutput è formato da IDSessioneTransazione|Descrizione anomalia;
				sOutput = this.IdSessioneTransazione + "|" + ex.Message;
				try
				{
					logger.Error(ex.ToString());
					if (siavCardManager != null)
					{
						siavCardManager.InsertNote(oCardBundle, siavLogin, ex.Message + " Per maggiori informazioni consultare il log per l'id transazione: " + this.IdSessioneTransazione, resourceFileManager.getConfigData("AutorAnnotation"));
						if (!string.IsNullOrEmpty(sLogWarning))
							siavCardManager.InsertNote(oCardBundle, siavLogin, sLogWarning, resourceFileManager.getConfigData("AutorAnnotation"));
						siavCardManager.FieldModify(oCardBundle, siavLogin, IdField.IfKey14.GetHashCode(), "ERRORE");
						siavCardManager.FieldModify(oCardBundle, siavLogin, IdField.IfKey31.GetHashCode(), sInformationGuide);
						siavCardManager.siavWsCard.Abort();
					}
				}
				catch (Exception e) { }
			}
			finally
			{
				// creazione reportistica
				if (!string.IsNullOrEmpty(sLogWarning))
					siavCardManager.InsertNote(oCardBundle, siavLogin, sLogWarning, resourceFileManager.getConfigData("AutorAnnotation"));
				if (lDataForReport.Count > 0)
				{
					string pathAttachment = fluxHelper.CreateReportMassive(this.sWorkingFolder, sIdProtocollo.Replace("/", "_"), lDataForReport);
					logger.Debug("Path report massivo: " + pathAttachment);
					siavCardManager.SetAttachment(oCardBundle, siavLogin, pathAttachment, "Report massivo");
				}
				logger.Debug("Fine elaborazione");
				if (siavLogin != null)
				{
					siavLogin.Logout();
					siavLogin.siavWsLogin.Close();
				}
				if (wcfSiavAgrafManager != null)
				{
					wcfSiavAgrafManager.siavWsAgraf.Close();
					wcfSiavAgrafManager = null;
				}
				siavLogin = null;
				siavCardManager.siavWsCard.Close();
				siavCardManager = null;

				if (resourceFileManager.getConfigData("isTest") != "SI")
				{
					SendMail sendMail = new SendMail();
					string sReceiverEmail = resourceFileManager.getConfigData("emailDestinatario");
					string sSenderNameEmail = resourceFileManager.getConfigData("emailNomeMittente");
					string sObjectEmail = string.Empty;
					if (sErrorObj != "")
						sObjectEmail = sErrorObj + " COMUNICAZIONE MASSIVA CASELLARIO " + sTipoDoc + " " + sSezTer + " - Elaborazione terminata, ultimo record inserito: " + sLastRecordInserted;
					else
						sObjectEmail = "COMUNICAZIONE MASSIVA CASELLARIO " + sTipoDoc + " " + sSezTer + " - Create schede documentali in archivio interno";

					//if (sLog == "") sLog = "Problema con l'esecuzione dell'algoritmo di sistema, prego contattare il supporto tecnico.";
					string sbodyMsg = "Comunicazione Massiva: " + sIdProtocollo + " \r\n "
										+ " Numero elaborati errati: " + " \r\n " + lCountErrati + " \r\n "
										+ " Numero elaborati negativi: " + " \r\n " + lCountNegativi + " \r\n "
										+ " Numero elaborati positivi: " + " \r\n " + lCountPositivi + " \r\n "
										+ sLog + " \r\n " + sLogWarning;
					sendMail.SENDMAIL(sReceiverEmail, sSenderNameEmail, sObjectEmail, sbodyMsg, "");
				}
			}
			return sResult.Select(s => (object)s).ToArray();
		}
		public object[] PredCasellarioCreator(string GUIdcard, out string sOutput)
		{
			string sErrorObj = "";
			List<String> sResult = new List<string>();
			sOutput = "";
			WcfSiavLoginManager siavLogin = null;
			WcfSiavAgrafManager wcfSiavAgrafManager = null;
			WcfSiavCardManager siavCardManager = null;
			// Codice OCF
			string sGuidCardSchedPred = string.Empty;
			string sSezTer = string.Empty;
			string sTipoDoc = string.Empty;
			CardBundle oCardBundle = null;
			string sLog = string.Empty;
			List<string> sPathAttachmentsCard = new List<string>();
			siavLogin = new WcfSiavLoginManager();
			wcfSiavAgrafManager = new WcfSiavAgrafManager(logger);
			FluxHelper fluxHelper = new FluxHelper();
			List<Siav.APFlibrary.Model.InternalAttachment> lPathAttachments = new List<Siav.APFlibrary.Model.InternalAttachment>();
			List<Siav.APFlibrary.Model.InternalAttachment> lPathAttachmentsToAdd = new List<Siav.APFlibrary.Model.InternalAttachment>();

			bool bCsvFound = false;
			bool bAlreadyDone = false;
			bool bNextAttachment = false;
			List<AdditionalField> CasellarioProtCardAdditionalFields = new List<AdditionalField>();
			List<KeyValuePair<int, string>> lfield = new List<KeyValuePair<int, string>>();

			try
			{
				string oUserName = resourceFileManager.getConfigData("UserFlux");
				string oUserPwd = oRijndael.Decrypt(resourceFileManager.getConfigData("PasswordFlux"));
				NameValueCollection CasMassiveCardIndexes;
				MainDoc oMainDoc;
				// Esegue il login
				siavLogin.Login(oUserName, oUserPwd);
				logger.Debug("Sessione login Archiflow: " + siavLogin.oSessionInfo.SessionId);

				siavCardManager = new WcfSiavCardManager(logger);
				// Recupera la scheda ove è stato avviato il processo
				siavCardManager.GetCard(GUIdcard, siavLogin, out oCardBundle);
				// Verifico se vi sia un file CSV allegato alla scheda di protocollazione
				siavCardManager.GetCardAttachments(sWorkingFolder, GUIdcard, siavLogin, out sPathAttachmentsCard);
				if (sPathAttachmentsCard != null)
				{
					foreach (string singlePath in sPathAttachmentsCard)
					{
						if (singlePath.ToLower().IndexOf(".csv") != -1)
						{
							bCsvFound = true;
							break;
						}
					}
				}
				string sArchivioAttach = "";
				string sNotaAttach = "";
				string sIdProtAttach = "";
				// Verifico che non vi sia un allegato circolare su una scheda massiva
				// cerco tra gli allegati circolari se presenti
				siavCardManager.GetCardInternalAttachments(GUIdcard, siavLogin, out lPathAttachments);
				if (lPathAttachments != null)
				{
					foreach (Siav.APFlibrary.Model.InternalAttachment singleInternalAttachment in lPathAttachments)
					{
						if (singleInternalAttachment.ArchiveId != "")
						{
							if (singleInternalAttachment.Note.ToLower() == "scheda massiva")
							{
								bAlreadyDone = true;
								sNotaAttach = singleInternalAttachment.Note;
								sArchivioAttach = singleInternalAttachment.ArchiveId;
								sIdProtAttach = singleInternalAttachment.Name;
								break;
							}
							else if (singleInternalAttachment.Note.ToLower() == "ricezione telematica")
							{

							}
							else
							{
								DocumentType oDocTypeAttachInt = null;
								Card oCardAttachInt = null;
								// Recupera la scheda per individuare a quale tipologia documentale appartiene
								siavCardManager.GetCard(singleInternalAttachment.GuidCard.ToString(), siavLogin, out oCardAttachInt);
								siavCardManager.getTypeDoc(oCardAttachInt.DocumentTypeId, siavLogin.oSessionInfo, out oDocTypeAttachInt);
								logger.Debug("Sto verificando la tipologia " + oDocTypeAttachInt.DocumentTypeName.ToLower() + " " + singleInternalAttachment.Name);
								if (oDocTypeAttachInt.DocumentTypeName.ToLower().IndexOf("u-") != -1)
								{
									if (oDocTypeAttachInt.DocumentTypeName.ToLower().IndexOf("massivi") != -1)
									{
										logger.Debug("Inserisco " + oDocTypeAttachInt.DocumentTypeName.ToLower() + " " + singleInternalAttachment.Name);
										lPathAttachmentsToAdd.Add(singleInternalAttachment);
										bNextAttachment = true;
									}
								}
							}
						}
					}
				}
				// Di norma una scheda protocollata in entrata può contenere:
				//      un allegato interno alla scheda di richiesta in uscita
				//  O
				//      un allegato esterno composto da un file csv ove non vi dovrebbe essere l'allegato interno di richiesta in uscita
				//      inoltre può contenere come allegato circolare una scheda massiva che qualora vi sia, non si permette la continuità di elaborazione
				//      Viene escluso l'allegato circolare con la nota ricezione telematica in quanto trattasi del link alla email PEc ricevuta come risposta
				// ciò fa scaturire 2 scenari
				// se non vi è il file csv e non vi è un allegato circolare che non sia la scheda massiva allora vi è un errore
				// se la scheda è già stata elaborata quindi esiste la scheda massiva
				if (bAlreadyDone == true)
				{
					sResult.Add("-1");
					throw new ArgumentException(resourceFileManager.getConfigData("PredCasDesc_1") + " Ricercare la scheda: " + sIdProtAttach + " con nota: " + sNotaAttach);
				}
				else if (bCsvFound == false && bNextAttachment == false)
				{
					sResult.Add("-2");
					throw new ArgumentException(resourceFileManager.getConfigData("PredCasDesc_2"));
				}
				siavCardManager.GetCardAdditionalFields(GUIdcard, siavLogin, out CasellarioProtCardAdditionalFields);
				logger.Debug("Recuperati i dati dalla scheda che ha avviato il processo : " + GUIdcard);
				string sPassword = "";
				if (CasellarioProtCardAdditionalFields != null)
					if (CasellarioProtCardAdditionalFields.Count > 0)
						sPassword = CasellarioProtCardAdditionalFields[0].Value;
				if (string.IsNullOrEmpty(sPassword))
				{
					sResult.Add("-3");
					throw new ArgumentException(resourceFileManager.getConfigData("PredCasDesc_3"));
				}
				// Recupera il documento principale della scheda di protocollo in entrata massiva
				siavCardManager.GetMainDoc(oCardBundle.CardId, siavLogin.oSessionInfo, out oMainDoc);
				if (oMainDoc.Extension.ToLower().IndexOf("zip") == -1)
				{
					sResult.Add("-4");
					throw new ArgumentException(resourceFileManager.getConfigData("PredCasDesc_4"));
				}
				// Verifico se con la password inserita e con il documento principale della scheda riesco a "spacchettare"
				// l'archivio comprezzo (file zip)
				// Materializza il file sul filesystem
				string sAnagraphicXlsSource = this.sWorkingFolder + @"\" + oMainDoc.Filename + "_AnagSource." + oMainDoc.Extension;
				logger.Debug("Scrivo il documento principale: " + sAnagraphicXlsSource);
				fluxHelper.FileMaterialize(sAnagraphicXlsSource, oMainDoc.oByte);
				try
				{
					// Estraggo i file dall'archivio compresso.
					ZipManager zip = new ZipManager();
					zip.Decompress(sAnagraphicXlsSource, this.sWorkingFolder + "\\ZIP\\", sPassword);//oRijndael.Decrypt(sPassword));
					foreach (string file in Directory.EnumerateFiles(this.sWorkingFolder + "\\ZIP\\", "*.zip"))
					{
						zip.Decompress(file, this.sWorkingFolder + "\\ZIP\\", sPassword);// oRijndael.Decrypt(sPassword));
					}
				}
				catch (Exception ex)
				{
					sResult.Add("-7");
					sLog = resourceFileManager.getConfigData("PredCasDesc_7") + " " + ex.Message;
					throw new ArgumentException(sLog);
				}
				// recupero gli indici della scheda massiva
				siavCardManager.GetIndexes(oCardBundle, out CasMassiveCardIndexes);
				//Selezione mittente -> rubrica generica
				AgrafCard svAgrafCardRub = new AgrafCard();
				svAgrafCardRub.CardContacts = new List<AgrafCardContact>();
				svAgrafCardRub.Tag = sIdIndexTagRubricaGen;

				logger.Debug("Dati rubrica caricati");
				List<AgrafCard> agrafCards;
				AgrafCard agrafTribunale;
				siavCardManager.GetAnagrafFromCard(oCardBundle, siavLogin, out agrafCards);
				if (agrafCards.Count > 0)
					agrafTribunale = agrafCards[0];
				else
				{
					sResult.Add("-5");
					sLog = resourceFileManager.getConfigData("PredCasDesc_5");
					throw new ArgumentException(sLog);
				}
				//Mittente
				lfield.Add(new KeyValuePair<int, String>(IdField.IfKey11.GetHashCode(), CasMassiveCardIndexes[resourceFileManager.getConfigData("PredCasMittente")]));
				// recupero il tipo documento del documento protocollato che ha invocato il flusso
				DocumentType oDocumentType;
				siavCardManager.getTypeDoc(oCardBundle.DocumentTypeId, siavLogin.oSessionInfo, out oDocumentType);
				//Tipo documento protocollo
				sTipoDoc = oDocumentType.DocumentTypeName;
				lfield.Add(new KeyValuePair<int, String>(IdField.IfKey12.GetHashCode(), oDocumentType.DocumentTypeName));
				//Mezzo di trasmissione 
				lfield.Add(new KeyValuePair<int, String>(IdField.IfKey13.GetHashCode(), CasMassiveCardIndexes[resourceFileManager.getConfigData("PredCasMezzoDiTrasmissione")]));
				//firmatario
				lfield.Add(new KeyValuePair<int, String>(IdField.IfKey22.GetHashCode(), CasMassiveCardIndexes[resourceFileManager.getConfigData("PredCasFirmatario")]));
				//note
				lfield.Add(new KeyValuePair<int, String>(IdField.IfKey23.GetHashCode(), CasMassiveCardIndexes[resourceFileManager.getConfigData("PredCasNote")]));
				//id scheda protocollo
				lfield.Add(new KeyValuePair<int, String>(IdField.IfKey24.GetHashCode(), oCardBundle.CardId.ToString()));
				//ufficio istruttore
				sSezTer = CasMassiveCardIndexes[resourceFileManager.getConfigData("PredCasUfficioDest")];
				lfield.Add(new KeyValuePair<int, String>(IdField.IfKey25.GetHashCode(), CasMassiveCardIndexes[resourceFileManager.getConfigData("PredCasUfficioDest")]));
				//oggetto
				lfield.Add(new KeyValuePair<int, String>(IdField.IfObj.GetHashCode(), resourceFileManager.getConfigData("PredCasOggetto")));
				Archive predArchive;
				siavCardManager.getArchiveDoc(resourceFileManager.getConfigData("PredCasArchive"), siavLogin.oSessionInfo, out predArchive);
				DocumentType predDocumentType;
				siavCardManager.getTypeDoc(resourceFileManager.getConfigData("PredCasTypeDocument"), siavLogin.oSessionInfo, out predDocumentType);
				Guid predIdCard;
				siavCardManager.InsertCardCas(lfield, predArchive.ArchiveId, predDocumentType, siavLogin, agrafCards, "", out predIdCard);
				CardManager oCardManager = new CardManager(logger);
				oCardManager.setCardVisibilityDefault(oUserName, oUserPwd, predIdCard.ToString());

				NameValueCollection indexCardPredCas;
				siavCardManager.GetIndexes(predIdCard, siavLogin, out indexCardPredCas);
				siavCardManager.InsertNote(oCardBundle, siavLogin, "Scheda di predisposizione creata correttamente. Generato il protocollo: " + indexCardPredCas[resourceFileManager.getConfigData("CasIdProtocollo")], resourceFileManager.getConfigData("AutorAnnotation"));

				// Inserimento allegato circolare
				bool bResult = siavCardManager.SetAttachmentIntenal(oCardBundle.CardId, siavLogin, predIdCard, indexCardPredCas[resourceFileManager.getConfigData("CasIdProtocollo")], predArchive.ArchiveId.ToString(), "Scheda Massiva");
				foreach (Siav.APFlibrary.Model.InternalAttachment singleInternalAttachment in lPathAttachmentsToAdd)
				{
					bool bResultAttachmentInt = siavCardManager.SetAttachmentIntenal(predIdCard, siavLogin, singleInternalAttachment.GuidCard, singleInternalAttachment.Name, singleInternalAttachment.ArchiveId, singleInternalAttachment.Note);
				}
				sGuidCardSchedPred = predIdCard.ToString();
				sResult.Add(predIdCard.ToString());
			}
			catch (Exception ex)
			{
				sErrorObj = "ERRORE ";

				if (sResult != null)
				{
					if (sResult.Count() > 0)
						sResult.Add("-6");
				}
				// sOutput è formato da IDSessioneTransazione|Descrizione anomalia;
				sOutput = this.IdSessioneTransazione + "|" + ex.Message;
				try
				{
					logger.Error(ex.ToString());
					if (siavCardManager != null)
					{
						siavCardManager.InsertNote(oCardBundle, siavLogin, ex.Message + " Per maggiori informazioni consultare il log per l'id transazione: " + this.IdSessioneTransazione, resourceFileManager.getConfigData("AutorAnnotation"));
						siavCardManager.siavWsCard.Abort();
					}
				}
				catch (Exception e) { }
			}
			finally
			{
				// creazione reportistica
				logger.Debug("Fine elaborazione");
				if (siavLogin != null)
				{
					siavLogin.Logout();
					siavLogin.siavWsLogin.Close();
				}
				if (wcfSiavAgrafManager != null)
				{
					wcfSiavAgrafManager.siavWsAgraf.Close();
					wcfSiavAgrafManager = null;
				}
				siavLogin = null;
				siavCardManager.siavWsCard.Close();
				siavCardManager = null;
				if (resourceFileManager.getConfigData("isTest") != "SI")
				{
					string sObjectEmail = string.Empty;
					string sbodyMsg = string.Empty;
					SendMail sendMail = new SendMail();

					string part = "";
					long aresult = 0;
					int pos = sGuidCardSchedPred.LastIndexOf('-');
					if (pos >= 0)
					{
						part = sGuidCardSchedPred.Substring(pos + 1);
						aresult = long.Parse(part);
					}
					else
					{
						aresult = long.Parse(sGuidCardSchedPred);
					}
					sbodyMsg = "Elaborazione avvenuta con successo, è stata generata la scheda in predisposizione: " + aresult.ToString();
					string sReceiverEmail = resourceFileManager.getConfigData("emailDestinatario");
					string sSenderNameEmail = resourceFileManager.getConfigData("emailNomeMittente");
					if (sErrorObj != "")
					{
						sObjectEmail = sErrorObj + " COMUNICAZIONE MASSIVA CASELLARIO " + sTipoDoc + " " + sSezTer + " - Tentativo di creazione scheda documentale in predisposizione";
						//if (sLog == "") sLog = "Problema con l'esecuzione dell'algoritmo di sistema, prego contattare il supporto tecnico.";
						sbodyMsg = "Errore durante il processo elaborativo sulla scheda: " + GUIdcard + " \r\n " + sLog + " \r\n ";

					}
					else
						sObjectEmail = "COMUNICAZIONE MASSIVA CASELLARIO " + sTipoDoc + " " + sSezTer + " - Creata scheda documentale in predisposizione";

					sendMail.SENDMAIL(sReceiverEmail, sSenderNameEmail, sObjectEmail, sbodyMsg, "");
				}
			}
			return sResult.Select(s => (object)s).ToArray();
		}
	}
}
