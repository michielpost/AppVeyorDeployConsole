using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppVeyorDeployConsole.Models
{
	public class DeployableBuildsResponse
	{
		public List<DeployableBuild> builds { get; set; }
	}

	public class DeployableBuild
	{
		public string buildNumber { get; set; }

		public string version { get; set; }

		public string status { get; set; }

		public string branch { get; set; }

		public string pullRequestId { get; set; }
	}
}
