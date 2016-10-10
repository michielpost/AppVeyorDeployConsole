using AppVeyorDeployConsole.Models;
using AppVeyorDeployConsole.Services;
using ConsoleMenu;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AppVeyorDeployConsole
{
	class Program
	{
		private static AppVeyorService _appVeyorService;
		private static GroupFileService _groupFileService = new GroupFileService();

		static void Main(string[] args)
		{
			MainAsync(args).Wait();
			// or, if you want to avoid exceptions being wrapped into AggregateException:
			//  MainAsync().GetAwaiter().GetResult();

			Console.WriteLine("Press ENTER to exit.");
			Console.ReadLine();
		}


		static Task MainAsync(string[] args)
		{
			string appVeyorKey = "";

			//Get AppVeyor API key from first argument
			if (args.Any())
				appVeyorKey = args.First();

			//Or ask for AppVeyor API key
			if (string.IsNullOrWhiteSpace(appVeyorKey))
			{
				Console.WriteLine("Enter AppVeyor API key:");
				appVeyorKey = Console.ReadLine();

				if (string.IsNullOrEmpty(appVeyorKey))
				{
					Console.WriteLine("No key, exiting");
					return null;
				}
			}

			//Initialize the service
			_appVeyorService = new AppVeyorService(appVeyorKey);

			var choices = new List<Func<Task>>
			{
				CreateEnvironmentGroup,
				NewDeploy
			};
			//Present menu of choices
			var menu = new TypedMenu<Func<Task>>(choices, "Choose a number", x => x.Method.Name);

			var picked = menu.Display();
			picked().Wait();


			return Task.FromResult(string.Empty);
		}

		/// <summary>
		/// Flow to create a new environment group
		/// </summary>
		/// <returns></returns>
		private static async Task CreateEnvironmentGroup()
		{
			Console.WriteLine("Creating new Environment Group");

			//Get GroupName
			Console.WriteLine("Enter new group name:");
			var groupName = Console.ReadLine();

			EnvironmentGroup newGroup = new EnvironmentGroup();
			newGroup.Name = groupName;

			//Get all projects
			var allProject = await _appVeyorService.GetAllAppVeyorProjects();

			Console.WriteLine("Pick a Project:");
			var menu = new TypedMenu<AppVeyorProject>(allProject, "Choose a number", x => x.Name);
			var pickedProject = menu.Display();
			newGroup.Project = pickedProject;

			//Get all environments 
			var allEnv = await _appVeyorService.GetAllAppVeyorEnvironmentsForProject(newGroup.Project.AccountName + "/" + newGroup.Project.Slug);

			AskForEnvironment(newGroup, allEnv);

			//Save group
			//Write JSON to file
			_groupFileService.SaveGroupToFile(newGroup);
			Console.WriteLine("Saved Environment Group {0} with {1} environments", newGroup.Name, newGroup.Environments.Count);

		}

		/// <summary>
		/// Ask for an environment to add to a group
		/// </summary>
		/// <param name="newGroup"></param>
		/// <param name="allEnv"></param>
		private static void AskForEnvironment(EnvironmentGroup newGroup, List<AppVeyorEnvironment> allEnv)
		{
			Console.WriteLine("Pick an Environment:");

			var menu = new TypedMenu<AppVeyorEnvironment>(allEnv.Except(newGroup.Environments).ToList(), "Choose a number", x => x.Name);
			var picked = menu.Display();

			newGroup.Environments.Add(picked);

			Console.WriteLine("Add another environment? (Y/N) (default Y)");
			var answer = Console.ReadLine();

			if (answer.Equals("N", StringComparison.InvariantCultureIgnoreCase))
				return;
			else
				AskForEnvironment(newGroup, allEnv);

		}

		/// <summary>
		/// Flow to trigger a new deploy
		/// </summary>
		/// <returns></returns>
		private static async Task NewDeploy()
		{
			Console.WriteLine("Which Group do you want to deploy to?");

			//Get all groups from disk
			string path = Directory.GetCurrentDirectory();
			var allFiles = Directory.GetFiles(path, "*.group.json");

			if (!allFiles.Any())
			{
				Console.WriteLine("No group.json files found! Please create an environment group first.");
				return;
			}

			List<EnvironmentGroup> groupList = allFiles.Select(_groupFileService.ReadGroupFile).ToList();

			var menu = new TypedMenu<EnvironmentGroup>(groupList, "Choose a number", x => x.Name);
			var picked = menu.Display();

			Console.WriteLine("Deploying");
			Console.WriteLine("---------");
			Console.WriteLine("Environment group: " + picked.Name);
			Console.WriteLine("Project: " + picked.Project.Name);
			Console.WriteLine();
			Console.WriteLine("AppVeyor environments:");
			picked.Environments.ForEach(x => Console.WriteLine("  - " + x.Name));
			Console.WriteLine();
			Console.WriteLine("Last (top 5) deployable builds for branch 'develop'");
			var deployableBuildsResponse = await _appVeyorService.GetDeployableBuilds(picked.Project.AccountName, picked.Project.Slug);

			// filter out:
			// pullRequestId's - when given, then these builds are associated with PR's. Which we do not want
			// branch == develop - we only care about 'develop' branches.
			var developBuilds = deployableBuildsResponse.builds.Where(b => string.IsNullOrEmpty(b.pullRequestId) && b.branch == "develop").ToList();

			int count = 1;
			// show the top 5 builds
			foreach (DeployableBuild deployableBuild in developBuilds)
			{
				Console.WriteLine($"Build version: {deployableBuild.version} - status: {deployableBuild.status} - branch: {deployableBuild.branch}");
				count++;
				if (count > 5) break;
			}

			//Get version to deploy
			string buildVersion = developBuilds.First()?.version;
			Console.WriteLine();
			Console.WriteLine($"Enter build version to deploy (default {buildVersion}):");
			var inputVersion = Console.ReadLine();
			if (!string.IsNullOrEmpty(inputVersion))
			{
				buildVersion = inputVersion;
			}

			Console.WriteLine();
			Console.WriteLine($"You want to deploy {buildVersion}");
			Console.WriteLine("Are you sure? (Y/N) (default N)");
			var answer = Console.ReadLine();

			Console.WriteLine();

			if (answer.Equals("Y", StringComparison.InvariantCultureIgnoreCase))
			{
				//Create new deploys for each environment in this group
				var deploymentIds = await DeployEnvironmentGroup(picked, buildVersion);

				//Monitor deployments
				await WaitForDeployments(deploymentIds);
			}
		}

		private static async Task WaitForDeployments(Dictionary<int, string> deployments)
		{
			List<Task<string>> deploymentTasks = new List<Task<string>>();

			foreach (var deployment in deployments)
			{
				deploymentTasks.Add(WaitForDeployment(deployment.Key, deployment.Value));
			}

			Console.WriteLine();
			Console.WriteLine("Waiting for deployments to finish...");
			Console.WriteLine();

			await Task.WhenAll(deploymentTasks);

			Console.WriteLine();
			Console.WriteLine("All deployments FINISHED.");
		}

		private static async Task<string> WaitForDeployment(int deploymentId, string envName)
		{
			bool finished = false;
			string status = "";

			while (!finished)
			{
				await Task.Delay(TimeSpan.FromSeconds(5));

				DeploymentDetailsResponse details = await _appVeyorService.GetDeploymentDetails(deploymentId);

				if (!string.IsNullOrEmpty(details.Deployment.Finished))
				{
					finished = true;
					status = details.Deployment.Status;
				}
			}

			Console.WriteLine($" - Deploy {envName} finished with status: {status}");

			return status;
		}

		/// <summary>
		/// Trigger deploys for the provided environment group
		/// </summary>
		/// <param name="picked"></param>
		/// <param name="buildVersion"></param>
		/// <returns></returns>
		private static async Task<Dictionary<int, string>> DeployEnvironmentGroup(EnvironmentGroup picked, string buildVersion)
		{
			Dictionary<int, string> deployments = new Dictionary<int, string>();
			Console.WriteLine("-------------------");
			Console.WriteLine("Starting Deployment");
			Console.WriteLine("-------------------");
			foreach (var env in picked.Environments)
			{
				try
				{
					Console.Write($"Starting deploy for {env.Name} build version {buildVersion}...");

					var response = await _appVeyorService.DeployEnvironment(picked.Project, env, buildVersion);

					deployments.Add(response.DeploymentId, env.Name);

					Console.WriteLine("Success!");

				}
				catch
				{
					Console.WriteLine("FAILED!");
				}

			}

			Console.WriteLine();
			Console.WriteLine("Deployments are started.");

			return deployments;

		}

	}
}
