using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Scaffolder.Models;
using System.Collections.Generic;
using System;

namespace Scaffolder
{
	public class Scaffolders
	{
		public List<DbModels> Models { get; set; }
		public dynamic Configurations { get; internal set; } = new object();

		public Scaffolders(string content)
		{
			// Building the content
			this.Configurations = JsonConvert.DeserializeObject<dynamic>(content);
			var dbModelsPath = Shared.ResolvePath(this.Get("Models")?.FirstOrDefault().DbModels);

			// Loading the models
			this.Models = Directory.GetFiles(dbModelsPath).Select(filePath =>
			{
				var lineSplitted = File.ReadAllLines(filePath)
					.FirstOrDefault(x => x.Contains("class "))
					.Split(" ")
					.Where(x => !string.IsNullOrWhiteSpace(x))
					.ToList();

				var className = lineSplitted.ElementAt(lineSplitted.IndexOf("class") + 1);

				return new DbModels
				{
					Name = className,
					Path = filePath
				};
			}).ToList();
		}

		public List<Configuration> Get(string key)
		{
			try
			{
				// Creating the object
				return JsonConvert.DeserializeObject<List<Configuration>>(
					// Accessing the data
					this.Configurations[key].ToString()
				);
			}
			catch (System.Exception ex)
			{
				Logger.Error("Error: Configuration not found.");
				Logger.Error($"Error Description: {ex.InnerException?.Message ?? ex.Message}");

				return null;
			}
		}

		public bool ModelExists(string name)
		{
			if (this.Models.Any(m => m.Name == name))
				return true;

			Logger.Error("Model not found.");

			Shared.Pause();
			return false;
		}
	}
}