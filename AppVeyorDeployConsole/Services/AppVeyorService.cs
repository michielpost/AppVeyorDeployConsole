using AppVeyorDeployConsole.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace AppVeyorDeployConsole.Services
{
	/// <summary>
	/// Responsible for communicating with AppVeyor APIs
	/// </summary>
	public class AppVeyorService
	{
		private HttpClient httpClient = new HttpClient();

		public AppVeyorService(string appVeyorKey)
		{
			httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
			httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", appVeyorKey);
		}

		/// <summary>
		/// Trigger a deploy in AppVeyor
		/// </summary>
		/// <param name="project"></param>
		/// <param name="env"></param>
		/// <param name="buildVersion"></param>
		/// <returns></returns>
		public async Task<StartDeploymentResponse> DeployEnvironment(AppVeyorProject project, AppVeyorEnvironment env, string buildVersion)
		{
			NewDeploy deploy = new Models.NewDeploy()
			{
				AccountName = project.AccountName,
				BuildVersion = buildVersion,
				EnvironmentName = env.Name,
				ProjectSlug = project.Slug
			};

			var jsonSerializerSettings = new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() };

			string envJson = JsonConvert.SerializeObject(deploy, jsonSerializerSettings);

			using (var response = await httpClient.PostAsync("https://ci.appveyor.com/api/deployments", new StringContent(envJson, Encoding.UTF8, "application/json")))
			{
				response.EnsureSuccessStatusCode();

				var resultJson = await response.Content.ReadAsStringAsync();

				var result = JsonConvert.DeserializeObject<StartDeploymentResponse>(resultJson);

				return result;
			}
		}

		/// <summary>
		/// https://ci.appveyor.com/api/environments
		/// </summary>
		public async Task<List<AppVeyorEnvironment>> GetAllAppVeyorEnvironments()
		{
			using (var response = await httpClient.GetAsync("https://ci.appveyor.com/api/environments"))
			{
				response.EnsureSuccessStatusCode();

				var resultJson = await response.Content.ReadAsStringAsync();

				var result = JsonConvert.DeserializeObject<List<AppVeyorEnvironment>>(resultJson);

				return result;
			}

		}

		public async Task<List<AppVeyorEnvironment>> GetAllAppVeyorEnvironmentsForProject(string projectId)
		{
			using (var response = await httpClient.GetAsync($"https://ci.appveyor.com/api/projects/{projectId}/deployable-environments"))
			{
				response.EnsureSuccessStatusCode();

				var resultJson = await response.Content.ReadAsStringAsync();

				var result = JsonConvert.DeserializeObject<List<AppVeyorEnvironment>>(resultJson);

				return result;
			}

		}

		/// <summary>
		/// https://ci.appveyor.com/api/projects
		/// </summary>
		public async Task<List<AppVeyorProject>> GetAllAppVeyorProjects()
		{
			using (var response = await httpClient.GetAsync("https://ci.appveyor.com/api/projects"))
			{
				response.EnsureSuccessStatusCode();

				var resultJson = await response.Content.ReadAsStringAsync();

				var result = JsonConvert.DeserializeObject<List<AppVeyorProject>>(resultJson);

				return result;
			}

		}

		public async Task<DeploymentDetailsResponse> GetDeploymentDetails(int deploymentId)
		{
			using (var response = await httpClient.GetAsync("https://ci.appveyor.com/api/deployments/" + deploymentId))
			{
				response.EnsureSuccessStatusCode();

				var resultJson = await response.Content.ReadAsStringAsync();

				var result = JsonConvert.DeserializeObject<DeploymentDetailsResponse>(resultJson);

				return result;
			}
		}

		/// <summary>
		/// Get deployable builds. This returns a list of builds, with the newest first (and oldest) last.
		/// </summary>
		/// <param name="accountName"></param>
		/// <param name="projectSlug"></param>
		/// <returns></returns>
		public async Task<BuildListResponse> GetDeployableBuilds(string accountName, string projectSlug)
		{
			// https://{{baseUrl}}/api/projects/{{accountName}}/{{projectSlug}}/deployable-builds
			using (var response = await httpClient.GetAsync($"https://ci.appveyor.com/api/projects/{accountName}/{projectSlug}/deployable-builds"))
			{
				response.EnsureSuccessStatusCode();

				var resultJson = await response.Content.ReadAsStringAsync();

				var result = JsonConvert.DeserializeObject<BuildListResponse>(resultJson);

				return result;
			}
		}

		/// <summary>
		/// Get last x builds for project.
		/// </summary>
		/// <param name="accountName"></param>
		/// <param name="projectSlug"></param>
		/// <returns></returns>
		public async Task<BuildListResponse> GetProjectBuilds(string accountName, string projectSlug, int records = 10)
		{
			// https://{{baseUrl}}/api/projects/{{accountName}}/{{projectSlug}}/history?
			using (var response = await httpClient.GetAsync($"https://ci.appveyor.com/api/projects/{accountName}/{projectSlug}/history?recordsNumber={records}"))
			{
				response.EnsureSuccessStatusCode();

				var resultJson = await response.Content.ReadAsStringAsync();

				var result = JsonConvert.DeserializeObject<BuildListResponse>(resultJson);

				return result;
			}
		}
	}
}
