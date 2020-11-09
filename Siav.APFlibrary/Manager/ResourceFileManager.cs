using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Resources;
using System.Xml.Linq;

namespace Siav.APFlibrary.Manager
{

    public sealed class ResourceFileManager
    {
        private static volatile ResourceFileManager instance;
        private static object syncRoot = new Object();
        //DA RIMETTERE DOPO I TEST
        private const string filePathName = @"C:\Siav\APFlibrary\App_GlobalResources\ApfResource.resx";
        //private const string filePathName = @"C:\SIAV\TestAPFLibrary_versione_9_7\TestAPFLibrary\TestLibrary\TestLibrary\App_GlobalResources\ApfResource.resx";


        public XDocument _resourceManager;
        private ResourceFileManager() { }

        public void SetResources(string filename = filePathName)
        {
            if (_resourceManager == null)
            {
                _resourceManager = XDocument.Load(filename);
            }
        }
        public string getConfigData(string keyToCheck)
        {
            try{
            XElement result = _resourceManager.Root.Descendants("data")
                                      .Where(k => k.Attribute("name").Value == keyToCheck)
                                      .Select(k => k)
                                      .FirstOrDefault();
            return result.Element("value").FirstNode.ToString();
                }
            catch(Exception ex){
                return "";
            }
        }
        public static ResourceFileManager Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (syncRoot)
                    {
                        if (instance == null) { }
                            instance = new ResourceFileManager();
                    }
                }

                return instance;
            }
        }
    }
       
}
    
/*
    public ResourceFileManager resourceFileManager()
        {
            if (_resourceManager == null)
            {
                System.Reflection.Assembly myAssembly;
                myAssembly = this.GetType().Assembly;
                _resourceManager = new ResourceManager("TestLibrary.App_GlobalResources." + filename, myAssembly);
            }
    */