using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using Scaffolder.Models;

namespace Scaffolder.Scaffold
{
	public class EFCoreScaffold : GenerationConditions
	{
		public Scaffolders Scaffolders { get; set; }
		public List<Configuration> configs { get; set; }

		private string template = null;
		private string name;

		public EFCoreScaffold()
		{
			this.Scaffolders = Application.Instance.SelectedProject.GetScaffolders();
			this.name = this.GetType().Name.Replace("Scaffold", "");
			this.configs = this.Scaffolders.Get(this.name);
		}

		public void Run(string name)
		{
			void exec(string m)
			{
				if (!this.Scaffolders.ModelExists(m)) return;
				configs.ForEach(cf => this.Generate(m, cf));
			}

			if (!name.Contains(','))
				exec(name);
			else
				name.Split(",").Select(s => s.Trim()).ToList().ForEach(m =>
				{
					configs.ForEach(cf =>
					{
						this.Generate(m, cf);
					});
				});
		}

		public void Generate(string name, Configuration config)
		{
			// this.template = Shared.LoadTemplate(this.template, config, this.configs.Count);
			var templateLines = Shared.LoadTemplateLines(this.template, config, this.configs.Count);
			var mappedModels = Scaffolders.Models.ToDictionary(x => x.Name, x => x);

			try
			{
				var filePath = Path.Combine(config.Output, "Configurations", $"{config.Header}{name}{config.Trailer}.{config.Extension ?? "cs"}");

				if (!this.FileExistenceHandler(filePath, name, config))
					return;

				// Getting the model name and path
				var model = mappedModels.Get(name);

				var properties = Shared.GetModelProperties(model.Path);
				var lastLine = properties.LastOrDefault();

				templateLines = templateLines.Select(templateLine =>
				{
					// Applying already the replacers
					config.Replacers.ForEach(x => templateLine = templateLine.Replace(x.CurrentValue, x.NewValue));

					if (!templateLine.Contains("@-Prop-@"))
						return templateLine;

					var configPropLines = new List<string>();

					void addExtraLine(string propLine)
					{
						if (lastLine != propLine)
							configPropLines.Add(templateLine.Replace("@-Prop-@", ""));
					}

					properties.ForEach(propLine =>
					{
						var builtProperty = Shared.BuildProperty(propLine);
						if (builtProperty.IsVirtual && (mappedModels.ContainsKey(builtProperty.Type) || mappedModels.ContainsKey(builtProperty.ParameterType)))
						{
							// Relations
							var isCollection = propLine.Contains("ICollection") || propLine.Contains("IEnumerable") || propLine.Contains("IList") || propLine.Contains("List");

							if (!isCollection)
							{ // One To Many
								var defaultRelationOne = "builder.HasOne(e => e.@-Relation-@).WithMany(e => e.@-Model-@s).HasForeignKey(e => e.@-Relation-@Id).OnDelete(DeleteBehavior.NoAction);";
								var relationalPropBuilder = config.Builders.Get("Relation:One") ?? defaultRelationOne;
								var configuredRelationLine = relationalPropBuilder.Replace("@-Name-@", builtProperty.Name)
																				.Replace("@-Model-@", model.Name)
																				.Replace("@-Relation-@", builtProperty.Type);
								if (!string.IsNullOrEmpty(configuredRelationLine))
								{
									configPropLines.Add(templateLine.Replace("@-Prop-@", configuredRelationLine));
									addExtraLine(propLine);
									return;
								}
							}
							else
							{ // Many to One
								var relationalPropBuilder = config.Builders.Get("Relation:Many") ?? "";
								var configuredRelationLine = relationalPropBuilder.Replace("@-Name-@", builtProperty.Name)
																				.Replace("@-Model-@", model.Name)
																				.Replace("@-Relation-@", builtProperty.Name);
								
								if (!string.IsNullOrEmpty(configuredRelationLine))
								{
									configPropLines.Add(templateLine.Replace("@-Prop-@", configuredRelationLine));
									addExtraLine(propLine);
									return;
								}
							}

							return;
						}

						// Getting the config if exists
						var propBuilder = config.Builders.Get("Property:" + builtProperty.Type) ??
																config.Builders.Get("Property") ??
																"builder.Property(e => e.@-Name-@).IsRequired(false);";

						// Replacing the Keys
						var configuredLine = propBuilder.Replace("@-Name-@", builtProperty.Name)
								   .Replace("@-Model-@", model.Name);

						// Adding the configured line
						configPropLines.Add(templateLine.Replace("@-Prop-@", configuredLine));

						addExtraLine(propLine);
					});

					return string.Join(Environment.NewLine, configPropLines);
				}).ToList();

				File.WriteAllText(filePath, string.Join(Environment.NewLine, templateLines)
					.Replace("@-Model-@", model.Name));
				Logger.Done($"file {config.Header}{name}{config.Trailer}.{config.Extension ?? "cs"} created.");
			}
			catch (Exception ex)
			{
				Logger.Error($"Error: {ex.Message} ; {ex.InnerException?.Message ?? ""}");
			}
		}

		public void Init()
		{
		Begin:
			Console.Clear();

			Logger.Log(
				string.Join(" -> ", new[] {
					$"Project",
					$"({ Application.Instance.SelectedProject.AppName }|Yellow)",
					$"({ this.name }|Yellow)"
				})
			);
			Logger.Log("");

			var option = Shared.OptionsSpreader<EFCoreScaffoldGenerationOptions>(name =>
			{
				if (name == EFCoreScaffoldGenerationOptions.Exit.ToString()) return "";
				return "Generate ";
			});

			switch (option)
			{
				case EFCoreScaffoldGenerationOptions.One_By_One:
					Logger.Log("\nType the Class Name: ");
					var name = Logger.ReadLine(this.Scaffolders.Models.Select(x => x.Name).ToArray());

					this.Run(name);
					Shared.Pause();
					break;

				case EFCoreScaffoldGenerationOptions.All_At_Once:
					this.Scaffolders.Models.ForEach(model => configs.ForEach(cf => this.Generate(model.Name, cf)));

					Shared.Pause();
					break;

				case EFCoreScaffoldGenerationOptions.DbSets:
					var dbContext = "public class DbContext {\n" +
					"        " + String.Join("\n        ", this.Scaffolders.Models.Select(x =>
					{
						return $"public DbSet<{x.Name}> {x.Name} {{ get; set; }}";
					})) +
					"\n}";

					Console.WriteLine(dbContext);

					Shared.Pause();
					break;

				case EFCoreScaffoldGenerationOptions.Exit:
					return;
			}

			goto Begin;
		}
	}

	enum EFCoreScaffoldGenerationOptions
	{
		One_By_One = 1,
		All_At_Once,
		DbSets,
		Exit
	}
}
