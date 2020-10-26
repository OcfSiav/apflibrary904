using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Resources;

namespace TestLibrary
{

    public sealed class ResourceFileManager
    {
        private static volatile ResourceFileManager instance;
        private static object syncRoot = new Object();
        private const string ResxFileName = "ApfResource";
        public ResourceManager _resourceManager;
        private ResourceFileManager() { }

        public void SetResources(string filename = ResxFileName)
        {
            if (_resourceManager == null)
            {
                System.Reflection.Assembly myAssembly;
                myAssembly = this.GetType().Assembly;
                _resourceManager = new ResourceManager("TestLibrary.App_GlobalResources." + filename, myAssembly);
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