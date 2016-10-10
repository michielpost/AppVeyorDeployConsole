using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppVeyorDeployConsole.Models
{
	public class BuildListResponse
	{
		public List<Build> Builds { get; set; }
	}

	public class Build
	{
		public string BuildNumber { get; set; }

		public string Version { get; set; }

		public string Status { get; set; }

		public string Branch { get; set; }

		public string Message { get; set; }

		public string PullRequestId { get; set; }
	}
}
