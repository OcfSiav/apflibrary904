﻿//------------------------------------------------------------------------------
// <auto-generated>
//     Il codice è stato generato da uno strumento.
//     Versione runtime:4.0.30319.42000
//
//     Le modifiche apportate a questo file possono provocare un comportamento non corretto e andranno perse se
//     il codice viene rigenerato.
// </auto-generated>
//------------------------------------------------------------------------------

namespace OCF_Ws.ConversionServices {
    using System.Runtime.Serialization;
    using System;
    
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Runtime.Serialization", "4.0.0.0")]
    [System.Runtime.Serialization.DataContractAttribute(Name="MainDocument", Namespace="http://schemas.datacontract.org/2004/07/ConversionServices.Model")]
    [System.SerializableAttribute()]
    public partial class MainDocument : object, System.Runtime.Serialization.IExtensibleDataObject, System.ComponentModel.INotifyPropertyChanged {
        
        [System.NonSerializedAttribute()]
        private System.Runtime.Serialization.ExtensionDataObject extensionDataField;
        
        private string BinaryContentField;
        
        private string FilenameField;
        
        [global::System.ComponentModel.BrowsableAttribute(false)]
        public System.Runtime.Serialization.ExtensionDataObject ExtensionData {
            get {
                return this.extensionDataField;
            }
            set {
                this.extensionDataField = value;
            }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute(IsRequired=true)]
        public string BinaryContent {
            get {
                return this.BinaryContentField;
            }
            set {
                if ((object.ReferenceEquals(this.BinaryContentField, value) != true)) {
                    this.BinaryContentField = value;
                    this.RaisePropertyChanged("BinaryContent");
                }
            }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute(IsRequired=true)]
        public string Filename {
            get {
                return this.FilenameField;
            }
            set {
                if ((object.ReferenceEquals(this.FilenameField, value) != true)) {
                    this.FilenameField = value;
                    this.RaisePropertyChanged("Filename");
                }
            }
        }
        
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        
        protected void RaisePropertyChanged(string propertyName) {
            System.ComponentModel.PropertyChangedEventHandler propertyChanged = this.PropertyChanged;
            if ((propertyChanged != null)) {
                propertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
            }
        }
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Runtime.Serialization", "4.0.0.0")]
    [System.Runtime.Serialization.DataContractAttribute(Name="Outcome", Namespace="http://schemas.datacontract.org/2004/07/ConversionServices.Model")]
    [System.SerializableAttribute()]
    public partial class Outcome : object, System.Runtime.Serialization.IExtensibleDataObject, System.ComponentModel.INotifyPropertyChanged {
        
        [System.NonSerializedAttribute()]
        private System.Runtime.Serialization.ExtensionDataObject extensionDataField;
        
        private int iCodeField;
        
        private string sDescriptionField;
        
        private string sTransactionIdField;
        
        [global::System.ComponentModel.BrowsableAttribute(false)]
        public System.Runtime.Serialization.ExtensionDataObject ExtensionData {
            get {
                return this.extensionDataField;
            }
            set {
                this.extensionDataField = value;
            }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute(IsRequired=true)]
        public int iCode {
            get {
                return this.iCodeField;
            }
            set {
                if ((this.iCodeField.Equals(value) != true)) {
                    this.iCodeField = value;
                    this.RaisePropertyChanged("iCode");
                }
            }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute(IsRequired=true)]
        public string sDescription {
            get {
                return this.sDescriptionField;
            }
            set {
                if ((object.ReferenceEquals(this.sDescriptionField, value) != true)) {
                    this.sDescriptionField = value;
                    this.RaisePropertyChanged("sDescription");
                }
            }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute(IsRequired=true)]
        public string sTransactionId {
            get {
                return this.sTransactionIdField;
            }
            set {
                if ((object.ReferenceEquals(this.sTransactionIdField, value) != true)) {
                    this.sTransactionIdField = value;
                    this.RaisePropertyChanged("sTransactionId");
                }
            }
        }
        
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        
        protected void RaisePropertyChanged(string propertyName) {
            System.ComponentModel.PropertyChangedEventHandler propertyChanged = this.PropertyChanged;
            if ((propertyChanged != null)) {
                propertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
            }
        }
    }
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    [System.ServiceModel.ServiceContractAttribute(ConfigurationName="ConversionServices.IConversionServices")]
    public interface IConversionServices {
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IConversionServices/base64ToPdfA", ReplyAction="http://tempuri.org/IConversionServices/base64ToPdfAResponse")]
        OCF_Ws.ConversionServices.Outcome base64ToPdfA(out OCF_Ws.ConversionServices.MainDocument mainDocOut, OCF_Ws.ConversionServices.MainDocument mainDoc);
    }
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    public interface IConversionServicesChannel : OCF_Ws.ConversionServices.IConversionServices, System.ServiceModel.IClientChannel {
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    public partial class ConversionServicesClient : System.ServiceModel.ClientBase<OCF_Ws.ConversionServices.IConversionServices>, OCF_Ws.ConversionServices.IConversionServices {
        
        public ConversionServicesClient() {
        }
        
        public ConversionServicesClient(string endpointConfigurationName) : 
                base(endpointConfigurationName) {
        }
        
        public ConversionServicesClient(string endpointConfigurationName, string remoteAddress) : 
                base(endpointConfigurationName, remoteAddress) {
        }
        
        public ConversionServicesClient(string endpointConfigurationName, System.ServiceModel.EndpointAddress remoteAddress) : 
                base(endpointConfigurationName, remoteAddress) {
        }
        
        public ConversionServicesClient(System.ServiceModel.Channels.Binding binding, System.ServiceModel.EndpointAddress remoteAddress) : 
                base(binding, remoteAddress) {
        }
        
        public OCF_Ws.ConversionServices.Outcome base64ToPdfA(out OCF_Ws.ConversionServices.MainDocument mainDocOut, OCF_Ws.ConversionServices.MainDocument mainDoc) {
            return base.Channel.base64ToPdfA(out mainDocOut, mainDoc);
        }
    }
}
