using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Permissions;
using System.Runtime.InteropServices;
using ComTypes = System.Runtime.InteropServices.ComTypes;
using System.IO;

//[assembly: SecurityPermission(SecurityAction.RequestMinimum, Execution = true)]

namespace OCF_Ws.Manager
{
    /// <summary>
    /// Lingua dell'interfaccia utente
    /// </summary>
    public enum Language
    {
        NonSpecificata = -1,
        Italiano = 0,
        Inglese = 1,
        Tedesco = 2,
        Francese = 3,
        Spagnolo = 4,
        Portoghese = 5,
    };

    public enum SignatureType
    {
        None = -1,
        EnvelopedDataSignature = 0,
        DetachedSignature = 1,
        PDFSignature = 2,
    };

    public enum TimeStampFileFormat
    {
        UndefinedTimeStampFormat = -1,
        TSRFormat = 0,
        MIMFormat = 1,
        M7MFormat = 2,
        TSDFormat = 3,
    };

    /// <summary>
    /// Tipo di verifica da effettuare su di un file firmato o marcato
    /// </summary>
    public enum VerifyDegree
    {
        // Verifica sintattica (rispetta il formato)
        SyntacticVerify = 0x01,

        // Verifica che il content corrisponda alla firma
        IntegrityVerify = 0x02,

        // Verifica che il certificato sia emesso da una CA accreditata
        CertTrustVerify = 0x04,

        // Verifica che il certificato sia in corso di validità
        CertExpirationAndRevokeVerify = 0x08,

        // Verifica le firme a cui la firma è stata apposta
        NestedSignatureVerify = 0x10,

        // Verifica solo l'ultima firma apposta in ordine di tempo
        OnlyLastSignatureVerify = 0x20,

        // Estende NestedSignatureVerify anche al caso di marcature temporali (verifica tutte le buste)
        RecursiveVerify = 0x40 | NestedSignatureVerify,

        // Non verifica i vincoli introdotti con la Deliberazione 45 di Maggio 2010 del CNIPA
        DontCheckDeliberazione45 = 0x8000000,

        // Aggregati
        MinimumVerify = SyntacticVerify | IntegrityVerify,
        StandardVerify = SyntacticVerify | IntegrityVerify | RecursiveVerify,
        FullVerify = StandardVerify | CertTrustVerify | CertExpirationAndRevokeVerify,

    };

    public enum Violation
    {
        // Non rispetta il formato
        Syntactic = 0x01,

        // Il content non corrisponda alla firma/alla marca
        Integrity = 0x02,

        // Il certificato non è emesso da una CA accreditata
        CertTrust = 0x04,

        // Il certificato non è in corso di validità
        CertExpirationAndRevoke = 0x08,

        // Non sono verificati alcuni dei vincoli introdotti con la Deliberazione 45 di Maggio 2010 del CNIPA
        Deliberazione45 = 0x8000000 | Integrity,

    };

    public struct VerifyResult
    {
        /// <summary>
        /// Riporta l'eventuale errore in una delle firme
        /// </summary>
        public Violation? SignatureViolation { get; set; }

        /// <summary>
        /// True se tutte le firme sono valide
        /// </summary>
        public bool? SignaturesValid => (SignatureViolation == null) ? (bool?)null : (SignatureViolation == 0);

        /// <summary>
        /// Riporta l'eventuale errore in una delle marcature temporali
        /// </summary>
        public Violation? TimestampViolation { get; set; }

        /// <summary>
        /// True se tutte le marcature temporali sono valide
        /// </summary>
        public bool? TimestampsValid => (TimestampViolation == null) ? (bool?)null : (TimestampViolation == 0);

        /// <summary>
        /// Il path del file estratto dalla busta di firma/marca (se è stato richesto di estrarlo)
        /// </summary>
        public string ContentFileName { get; set; }

    }

    public enum SaveOption
    {
        DontSave = 0x00000000,
        SaveTimeStampContent = 0x00000001,
        SaveSignContent = 0x00000002,
        SaveTimeStamp = unchecked((int)0x80000000),
    };


    /// <summary>
    /// Eccezione specifica dei modulo di firma e marca SIAV (SvSignEx, SvSignPK, SvSignSC)
    /// </summary>
    [SerializableAttribute]
    public class SvSignException : System.ApplicationException
    {
        public SvSignException() : base() { }
        public SvSignException(string message) : base(message) { }
        public SvSignException(string message, System.Exception innerException) : base(message, innerException) { }

        protected SvSignException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    class SC : IDisposable
    {
        public static string FileIni = "SvSignSc.ini";
        public static Language DefLanguage = Language.Italiano;
        public static SvScRTFlags ModuleUsage = SvScRTFlags.ViewerMode;

        private static bool ModuleInitialized /*= false*/;

        /// <summary>
        /// Parametro ulEnable della InitFunction
        /// </summary>
        public enum SvScRTFlags
        {
            EnableSlotMonitor = 0x01,
            ViewerMode = 0x02,
            DeimpersonateDuringHardwareAccess = 0x04,
        };

        public static bool InitializeModule(bool silent)
        {
            if (ModuleInitialized)
                // Già inizializzata
                return false;

            NativeMethods.SvSetSilentMode(silent);

            CheckScResult(
            NativeMethods.InitFunction(unchecked((IntPtr)0),
                        FileIni, (uint)ModuleUsage,
                        unchecked((UInt16)DefLanguage),
                        NativeMethods.ActivationCode1, NativeMethods.ActivationCode2));

            ModuleInitialized = true;
            return true;
        }

        public SC()
            : this(false)
        {
        }

        public SC(bool silent)
        {
            InitializeModule(true);

            NativeMethods.SvSetSilentMode(silent);

            CheckScResult(
            NativeMethods.SvScOpen(out signSCHandle));

            CurrentSignMode = ClientServerMode.ModeApplet;
        }

        ~SC()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            // free managed resources
            if (disposing)
            {
            }

            // free unmanaged resources
            if (signSCHandle != 0)
            {
                CheckScResult(
                NativeMethods.SvScClose(signSCHandle));

                signSCHandle = 0;
            }
        }

        /// <summary>
        /// Modalità firma "Step"
        /// </summary>
        public enum ClientServerMode
        {
            ModeActiveX = 0,
            ModeApplet = -1,
            ModeStandard = 1,
            ModeHSM = 2,
        };

        ClientServerMode signMode;

        public ClientServerMode CurrentSignMode
        {
            get
            {
                return signMode;
            }

            set
            {
                signMode = value;

                NativeMethods.SvScClientServerMode(signSCHandle, signMode);
            }
        }

        public object GetFileHash(string filePath)
        {
            object hash = new object();
            CheckScResult(NativeMethods.SvScCSSign_Step1(signSCHandle, filePath, out hash));
            return hash;
        }

        public string CreateP7MFile(string originalFilePath, object signedHash)
        {
            CheckScResult(NativeMethods.SvScCSSign_Step3(signSCHandle, originalFilePath, ref signedHash));

            return string.Compare(Path.GetExtension(originalFilePath), ".p7m", true) == 0 ? originalFilePath : originalFilePath + ".p7m";
        }

        public object SignFileHash(object hash)
        {
            object signedHash;
            CheckScResult(NativeMethods.SvScCSSign_Step2(signSCHandle, ref hash, out signedHash, true));
            return signedHash;
        }

        /// <summary>
        /// Ritorna la credenziale di firma (completa di tutte le sue componenti, quindi slot, id smart-card, PIN, ...) 
        /// in forma di nodo Xml.
        /// </summary>
        /// <param name="xmlCredential">In uscita contiene la credenziale; se in ingresso non è null,
        /// viene rimpiazzato, altrimenti viene creato da zero.</param>
        /// <param name="bAskFor">true per richiedere all'utente di impostare la credenziale, 
        /// false per tornare la credenziale corrente</param>
        public void GetCredential(ref System.Xml.XmlElement xmlCredential, bool bAskFor)
        {
            MSXML2.IXMLDOMElement comXmlCredential = null;

            // Da System.Xml.XmlElement a MSXML2.IXMLDOMElement

            if (xmlCredential != null)
            {
                MSXML2.IXMLDOMDocument comXmlDoc = new MSXML2.DOMDocument30();
                comXmlDoc.loadXML(xmlCredential.OuterXml);
                comXmlCredential = comXmlDoc.documentElement;
            }

            // Chiamata effettiva

            CheckScResult(
                  NativeMethods.SvScGetCredential(signSCHandle, ref comXmlCredential, bAskFor, ""));

            // Da MSXML2.IXMLDOMElement a System.Xml.XmlElement

            System.Xml.XmlDocument xmlDoc = new System.Xml.XmlDocument();
            xmlDoc.LoadXml(comXmlCredential.xml);

            if (xmlCredential == null)
                xmlCredential = xmlDoc.DocumentElement;

            else
            {
                xmlCredential.OwnerDocument.InsertAfter(xmlDoc.DocumentElement, xmlCredential);
                xmlCredential.ParentNode.RemoveChild(xmlCredential);
            }
        }

        /// <summary>
        /// Imposta la credenziale di firma a partire da una stringa Xml precedentemente generata dalla GetCredential.
        /// </summary>
        /// <param name="sXmlCredential">Stringa Xml generata dalla GetCredential</param>
        /// <param name="bCompleteCredential">True se la credenziale impostata è completa e 
        /// si può firmare in batch senza l'intervento dell'utente</param>
        public void SetCredential(string sXmlCredential, out bool bCompleteCredential)
        {
            System.Xml.XmlDocument xmlCredential = new System.Xml.XmlDocument();
            xmlCredential.LoadXml(sXmlCredential);

            SetCredential(xmlCredential.DocumentElement, out bCompleteCredential);
        }

        /// <summary>
        /// Imposta la credenziale di firma a partire da un Xml precedentemente generato dalla GetCredential.
        /// </summary>
        /// <param name="xmlCredential">Xml generato dalla GetCredential</param>
        /// <param name="bCompleteCredential">True se la credenziale impostata è completa e 
        /// si può firmare in batch senza l'intervento dell'utente</param>
        public void SetCredential(System.Xml.XmlElement xmlCredential, out bool bCompleteCredential)
        {
            if (xmlCredential == null)
            {
                throw new ArgumentException("Null parameter", "xmlCredential");
            }

            // Da System.Xml.XmlElement a MSXML2.IXMLDOMElement

            MSXML2.IXMLDOMElement comXmlCredential = null;

            MSXML2.IXMLDOMDocument comXmlDoc = new MSXML2.DOMDocument30();
            comXmlDoc.loadXML(xmlCredential.OuterXml);
            comXmlCredential = comXmlDoc.documentElement;
            // Chiamata effettiva
            CheckScResult(
                    NativeMethods.SvScSetCredential(signSCHandle, comXmlCredential, out bCompleteCredential));
        }


        /// <summary>
        /// Verifica un file firmato e/o marcato
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="verifyDegree"></param>
        /// <param name="saveOptions"></param>
        /// <param name="saveTo"></param>
        /// <param name="signFormat"></param>
        /// <param name="timestampFormat"></param>
        /// <returns></returns>
        public VerifyResult Verify(string fileName, VerifyDegree verifyDegree = VerifyDegree.StandardVerify,
                            SaveOption saveOptions = SaveOption.DontSave, string saveTo = null, 
                            SignatureType signFormat = SignatureType.EnvelopedDataSignature,
                            TimeStampFileFormat timestampFormat = TimeStampFileFormat.UndefinedTimeStampFormat)
        {
            StringBuilder workPath = null;

            if (saveOptions != 0 || !string.IsNullOrEmpty(saveTo))
                workPath = new StringBuilder(260);

            if (!string.IsNullOrEmpty(saveTo))
                workPath.Append(saveTo);

            Violation tsViolation, signViolation;

			var success = NativeMethods.SvScVerifyCryptographicEnvelope(signSCHandle,
                                timestampFormat, fileName, workPath,
                                verifyDegree, signFormat, saveOptions,
                                out tsViolation, out signViolation);

            CheckScResult(success || tsViolation != 0 || signViolation != 0);

            return new VerifyResult
            {
                SignatureViolation = (signFormat == SignatureType.None && signViolation == Violation.Syntactic) ? (Violation?)null : signViolation,
                TimestampViolation = (timestampFormat == TimeStampFileFormat.UndefinedTimeStampFormat && tsViolation == Violation.Syntactic) ? (Violation?)null : tsViolation,
                ContentFileName = workPath?.ToString(),
            };
        }

        public VerifyResult VerifyDetached(string envelopeFileName, string contentFileName, 
                            VerifyDegree verifyDegree = VerifyDegree.StandardVerify,
                            SignatureType signFormat = SignatureType.EnvelopedDataSignature,
                            TimeStampFileFormat timestampFormat = TimeStampFileFormat.UndefinedTimeStampFormat)
        {
            var workPath = new StringBuilder(contentFileName);

			Violation tsViolation, signViolation;

			var success = NativeMethods.SvScVerifyCryptographicEnvelope(signSCHandle,
                                timestampFormat, envelopeFileName, workPath,
                                verifyDegree, signFormat, SaveOption.DontSave,
                                out tsViolation, out signViolation);

            CheckScResult(success || tsViolation != 0 || signViolation != 0);

            return new VerifyResult
            {
                SignatureViolation = (signFormat == SignatureType.None && signViolation == Violation.Syntactic) ? (Violation?)null : signViolation,
                TimestampViolation = (timestampFormat == TimeStampFileFormat.UndefinedTimeStampFormat && tsViolation == Violation.Syntactic) ? (Violation?)null : tsViolation,
            };
        }

        #region Core

        // Handle sessione SvSignSC
        private uint signSCHandle;

        /// <summary>
        /// Controlla se l'esito dell'ultima chiamata alla SvSignSC è positivo e, 
        /// se no, solleva un'eccezione.
        /// </summary>
        /// <param name="bRes">Esito dell'ultima chiamata alla SvSignSC</param>
        private static void CheckScResult(bool bRes)
        {
            if (bRes)
                return;

            StringBuilder sBuf = new StringBuilder(); uint uBufSize = 0;
            NativeMethods.SvGetDescriptionOfLastError(sBuf, ref uBufSize);

            sBuf.Length = (int)uBufSize;
            NativeMethods.SvGetDescriptionOfLastError(sBuf, ref uBufSize);

            throw new SvSignException(sBuf.ToString());
        }

        #region Interfaccia SvSignSC

        /// <summary>
        /// Definizione interfaccia SvSignSC.dll - Solo uso interno.
        /// </summary>
        private static class NativeMethods
        {
            // La DLL deve trovarsi nella stessa cartella dell'assembly
            private const string DLL = @"SvSignSc.dll";

            // Codici per l'abilitazione della DLL di firma e marca
            public const Int32 ActivationCode1 = 5041;
            public const Int32 ActivationCode2 = 4791;

            /// <summary>
            /// Inizializzazione uso SvSignSC.dll - Obbligatorio chiamarla prima di ogni altra operazione.
            /// </summary>
            /// <param name="hDlg">Non utilizzato</param>
            /// <param name="sNomeFileIni">File INI dove SvSignSC.dll salva la propria configurazione</param>
            /// <param name="ulEnable">Non utilizzato</param>
            /// <param name="eLanguage">Lingua dell'interfaccia: sono supportate solo le lingue Italiano, Inglese e Tedesco.</param>
            /// <param name="nCod1">Primo codice che abilita l'uso della SvSignSC</param>
            /// <param name="nCod2">Primo codice che abilita l'uso della SvSignSC</param>
            /// <returns>Esito.
            /// In caso di fallimento (esito=False) la SvGetDescriptionOfLastError torna la causa dell'errore.</returns>
            [DllImport(DLL, CallingConvention = CallingConvention.StdCall)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern
            bool InitFunction(
                                                        IntPtr hDlg,
                [MarshalAs(UnmanagedType.LPStr)]        string sNomeFileIni,
                                                        UInt32 ulEnable,
                                                        UInt16 eLanguage,
                                                        Int32 nCod1,
                                                        Int32 nCod2);

            /// <summary>
            /// Imposta la modalità interattiva o "silenziosa" (cioè che non richiede mai l'intervento di un utente)
            /// </summary>
            /// <param name="bSilent">True per la modalità non interattiva. Di default è interattiva.</param>
            /// <returns>Ritorna lo stato precedente.</returns>
            [DllImport(DLL, CallingConvention = CallingConvention.StdCall)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern
            bool SvSetSilentMode(
                [MarshalAs(UnmanagedType.Bool)]         bool bSilent);

            /// <summary>
            /// Ritorna una descrizione dell'ultimo errore verificatosi. 
            /// Da chiamare solo se l'ultima chiamata alla SvSignSC è fallita.
            /// </summary>
            /// <param name="sDescriptionBuffer">Buffer dove tornare la descrizione</param>
            /// <param name="uDescriptionBufferSize">In ingresso la dimensione del buffer, in uscita la lunghezza complessiva della descrizione.</param>
            /// <returns></returns>
            [DllImport(DLL, CallingConvention = CallingConvention.StdCall)]
            public static extern
            uint SvGetDescriptionOfLastError(
                [MarshalAs(UnmanagedType.LPStr)]        StringBuilder sDescriptionBuffer,
                                                        ref uint uDescriptionBufferSize);

            /// <summary>
            /// Apertura di una sessione SvSignSC.
            /// </summary>
            /// <param name="hSignSC">In uscita è l'handle della sessione</param>
            /// <returns>Esito.
            /// In caso di fallimento (esito=False) la SvGetDescriptionOfLastError torna la causa dell'errore.</returns>
            [DllImport(DLL, CallingConvention = CallingConvention.StdCall)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern
            bool SvScOpen(
                [MarshalAs(UnmanagedType.U4)]           out uint hSignSC);

            /// <summary>
            /// Chiusura della sessione SvSignSC.
            /// </summary>
            /// <param name="hSignSC">Handle della sessione da chiudere</param>
            /// <returns>Esito.
            /// In caso di fallimento (esito=False) la SvGetDescriptionOfLastError torna la causa dell'errore.</returns>
            [DllImport(DLL, CallingConvention = CallingConvention.StdCall)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool SvScClose(
                [MarshalAs(UnmanagedType.U4)]           uint hSignSC);

            /// <summary>
            /// Ottiene l'XML della credenziale correntemente impostata nella sessione hSignSC.
            /// </summary>
            /// <param name="hSignSC">Handle della sessione SvSignSC</param>
            /// <param name="xml">In ingresso un elemento xml già formato o null, in uscita l'xml con la credenziale.</param>
            /// <param name="bAskFor">Richiede all'utente di selezionare la credenziale</param>
            /// <param name="sAskOnlyForThisAttribute">Filtra quali componenti della credenziale richiedere</param>
            /// <returns>Esito.
            /// In caso di fallimento (esito=False) la SvGetDescriptionOfLastError torna la causa dell'errore.</returns>
            [DllImport(DLL, CallingConvention = CallingConvention.StdCall)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern
            bool SvScGetCredential(
                [MarshalAs(UnmanagedType.U4)]           uint hSignSC,
                /*[MarshalAs(UnmanagedType.IDispatch)]*/    ref MSXML2.IXMLDOMElement xml,
                [MarshalAs(UnmanagedType.Bool)]         bool bAskFor,
                [MarshalAs(UnmanagedType.LPStr)]        string sAskOnlyForThisAttribute);

            /// <summary>
            /// Imposta la credenziale nella sessione hSignSC
            /// </summary>
            /// <param name="hSignSC">Handle della sessione SvSignSC</param>
            /// <param name="xml">L'elemento xml con la credenziale precedentemnete ottenuta con la SvScGetCredential</param>
            /// <param name="bCredentialComplete">In uscita è True se la credenziale è completa ovvero se da sola basta per effettuare la firma senza richiedere ulteriori parametri all'utente.</param>
            /// <returns>Esito.
            /// In caso di fallimento (esito=False) la SvGetDescriptionOfLastError torna la causa dell'errore.</returns>
            [DllImport(DLL, CallingConvention = CallingConvention.StdCall)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern
            bool SvScSetCredential(
                [MarshalAs(UnmanagedType.U4)]           uint hSignSC,
                /*[MarshalAs(UnmanagedType.IDispatch)]*/    MSXML2.IXMLDOMElement xml,
                [MarshalAs(UnmanagedType.Bool)]         out bool bCredentialComplete);

            /// <summary>
            /// Recupera il certificato corrente in forma di XML
            /// </summary>
            /// <param name="hSignSC">Handle della sessione SvSignSC</param>
            /// <param name="xml">IXMLDOMNode* in cui viene tornato l'XML del certificato</param>
            /// <returns>Esito.
            /// In caso di fallimento (esito=False) la SvGetDescriptionOfLastError torna la causa dell'errore.</returns>
            [DllImport(DLL, CallingConvention = CallingConvention.StdCall)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern
            bool SvScGetCertXml(
                [MarshalAs(UnmanagedType.U4)]           uint hSignSC,
                                                        ref MSXML2.IXMLDOMElement xml);

            [DllImport(DLL, CallingConvention = CallingConvention.StdCall)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern
            bool SvScCSSign_Step1(
                [MarshalAs(UnmanagedType.U4)]           uint hSignSC,
                [MarshalAs(UnmanagedType.LPStr)]        string sFileToSign,
                                                        out object base64FileDigest);

            [DllImport(DLL, CallingConvention = CallingConvention.StdCall)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern
            bool SvScCSSign_Step2(
                [MarshalAs(UnmanagedType.U4)]           uint hSignSC,
                                                        ref object varFileDigestBase64,
                                                        out object varSignedDataBase64,
                [MarshalAs(UnmanagedType.Bool)]         bool bIncludeSignerCertificate);

            [DllImport(DLL, CallingConvention = CallingConvention.StdCall)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern
            bool SvScCSSign_Step3(
                [MarshalAs(UnmanagedType.U4)]           uint hSignSC,
                [MarshalAs(UnmanagedType.LPStr)]        string sOrigFileToSign,
                                                        ref object varSignedDataBase64);

            /// <summary>
            /// setta il SimpleMode, che indica se l'hash firmato arriva "puro" o impacchettato dall'activex
            /// 
            /// </summary>
            /// <param name="hSignSC"></param>
            /// <param name="bSimpleMode"></param>
            /// <returns></returns>
            [DllImport(DLL, CallingConvention = CallingConvention.StdCall)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern
            void SvScClientServerMode(
                [MarshalAs(UnmanagedType.U4)]           uint hSignSC,
                ClientServerMode eClientServerMode);

            [DllImport(DLL, CallingConvention = CallingConvention.StdCall)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern
                bool SvScVerifyCryptographicEnvelope(
                uint hSignSC,
                TimeStampFileFormat eTSFormat,
                string sTSFilePath,
                StringBuilder sFilePath,
                VerifyDegree eVerifyDegree,
                SignatureType eSignedContentFormat,
                SaveOption eSaveOptions,
                out Violation pTSVerifyResult,
                out Violation pContentVerifyResult);

        }

        #endregion

    }
    #endregion

}
