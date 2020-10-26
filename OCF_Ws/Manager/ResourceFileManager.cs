using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Resources;
using System.Xml.Linq;
using System.Web;

namespace OCF_Ws.Manager
{

	public sealed class ResourceFileManager
	{
		private static volatile ResourceFileManager instance;
		private static object syncRoot = new Object();
		//private const string filePathName = @"G:\SoftwareSiav\AceaRest\AceaResource.resx";
		private const string filePathName = @"\App_GlobalResources\Resource.resx";

		public XDocument _resourceManager;
		private ResourceFileManager() { }

		public void SetResources(string filename = filePathName)
		{
			if (_resourceManager == null)
			{
				_resourceManager = XDocument.Load(System.Web.Hosting.HostingEnvironment.ApplicationPhysicalPath + filename);
			}
		}
		public string getConfigData(string keyToCheck)
		{
			try
			{
				XElement result = _resourceManager.Root.Descendants("data")
										  .Where(k => k.Attribute("name").Value == keyToCheck)
										  .Select(k => k)
										  .FirstOrDefault();
				return HttpUtility.HtmlDecode(result.Element("value").FirstNode.ToString()); 
			}
			catch (Exception ex)
			{
				return "";
				throw new Exception(String.Format("{0} >> {1}: {2}", "ERRORE: getConfigData", ex.Source, ex.Message), ex);
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
