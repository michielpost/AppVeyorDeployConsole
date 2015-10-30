using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppVeyorDeployConsole.Models
{
	public class DeploymentDetailsResponse
	{
		public Deployment Deployment { get; set; }
	}

	public class Deployment
	{
		public int DeploymentId { get; set; }
		public Environment Environment { get; set; }
		public string Status { get; set; }
		public string Started { get; set; }
		public string Finished { get; set; }
		public string Created { get; set; }
		public string Updated { get; set; }
	}

	public class Environment
	{
		public int DeploymentEnvironmentId { get; set; }
		public string Name { get; set; }
		public string Provider { get; set; }
		public string Created { get; set; }
		public string Updated { get; set; }
	}


}
