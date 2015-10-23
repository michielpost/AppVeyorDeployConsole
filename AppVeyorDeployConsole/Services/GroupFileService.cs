using AppVeyorDeployConsole.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppVeyorDeployConsole.Services
{
	/// <summary>
	/// Responsible for reading and writing (EnvironmentGroup) group.json files
	/// </summary>
	public class GroupFileService
	{
		/// <summary>
		/// Read a group.json file
		/// </summary>
		/// <param name="fileName"></param>
		/// <returns></returns>
		public EnvironmentGroup ReadGroupFile(string fileName)
		{
			using (StreamReader r = new StreamReader(fileName))
			{
				string json = r.ReadToEnd();
				EnvironmentGroup result = JsonConvert.DeserializeObject<EnvironmentGroup>(json);
				return result;
			}
		}

		/// <summary>
		/// Save a group.json file
		/// </summary>
		/// <param name="newGroup"></param>
		public void SaveGroupToFile(EnvironmentGroup newGroup)
		{
			string fileName = string.Format("{0}.group.json", newGroup.Name);

			using (FileStream fs = File.Open(fileName, FileMode.CreateNew))
			using (StreamWriter sw = new StreamWriter(fs))
			using (JsonWriter jw = new JsonTextWriter(sw))
			{
				jw.Formatting = Formatting.Indented;

				JsonSerializer serializer = new JsonSerializer();
				serializer.Serialize(jw, newGroup);
			}


		}
	}
}
