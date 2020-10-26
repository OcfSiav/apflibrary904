using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace OCF_Ws.Manager
{
	public class FileManager
	{
		public Boolean FileMaterialize(string path, Byte[] oByte)
		{
			bool bResult = false;
			try
			{
				// Delete the file if it exists.
				if (File.Exists(path))
				{
					File.Delete(path);
				}

				// Create the file.
				using (FileStream fs = File.Create(path))
				{
					// Add some information to the file.
					fs.Write(oByte, 0, oByte.Length);
					bResult = true;
				}
			}
			catch (Exception ex)
			{
				throw new ArgumentException(ex.Message);
			}
			return bResult;
		}
	}
}