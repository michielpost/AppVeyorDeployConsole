using System.Collections.Generic;

namespace AppVeyorDeployConsole.Models
{
	public class EnvironmentGroup
	{

		public EnvironmentGroup()
		{
			Environments = new List<AppVeyorEnvironment>();
		}

		public string Name { get; set; }

		public List<AppVeyorEnvironment> Environments { get; set; }

		public AppVeyorProject Project { get; set; }
	}
}
