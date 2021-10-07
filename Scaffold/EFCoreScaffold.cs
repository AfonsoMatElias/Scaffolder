using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Scaffolder.Models;

namespace Scaffolder.Scaffold
{
    public class EFCoreScaffold : GenerationConditions
    {
        public Scaffolders Scaffolders { get; set; }
        public List<Configuration> configs { get; set; }

        private string template = null;
        private string name;

        // Aditionals configuration according to the the type of property
        private Dictionary<string, string[]> typeAditionals = new Dictionary<string, string[]>
        {
            {
                "string", new[] { ".HasMaxLength(200)", }
            },
            {
                "decimal", new[] { ".HasPrecision(18, 2)", }
            }
        };

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

            if (!name.Contains(","))
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
            this.template = Shared.LoadTemplate(this.template, config, this.configs.Count);

            try
            {
                var filePath = Path.Combine(config.Output, "Configurations", $"{config.Header}{name}{config.Trailer}.cs");

                if (!this.FileExistenceHandler(filePath, name, config))
                    return;

                // Getting the model name and path
                var model = this.Scaffolders.Models.FirstOrDefault(x => x.Name == name);
                // Reading the file by lines
                var mTemplate = this.template.Replace("@-Model-@", model.Name);

                var properties = Shared.GetModelProperties(model.Path);

                var identation = "\n            ";
                var propIndetifier = "@-Prop-@";

                void setter(string value)
                    => mTemplate = mTemplate.Replace(propIndetifier, $"{identation}{value}{propIndetifier}");

                string propertyBuilder(string propType, string propName)
                {
                    var ads = new List<string>() { "" };
                    if (typeAditionals.ContainsKey(propType))
                        ads.AddRange(typeAditionals[propType]);

                    return $"builder.Property(e => e.@-Name-@)@-Aditional-@{ identation + "       " }.IsRequired(false);\n"
                        .Replace("@-Name-@", propName)
                        .Replace("@-Aditional-@", string.Join(identation + "       ", ads));
                }

                string virtualPropertyBuilder(string propType, string propName)
                {
                    var ads = new List<string>() { "" };
                    if (typeAditionals.ContainsKey(propType))
                        ads.AddRange(typeAditionals[propType]);

                    return $"builder.HasOne(e => e.{ propName })" +
                         string.Join(identation + "       ", new string[] {
                             $".WithMany(e => e.{ name }s)",
                             $".HasForeignKey(e => e.{ propName }Id)",
                              ".OnDelete(DeleteBehavior.NoAction);"
                         });
                }

                var count = properties.Count();
                for (int i = 1; i <= count; i++)
                {
                    // Getting the line
                    var line = properties.ElementAt(i - 1).Trim();
                    var lineSplited = line.Split(" ");

                    var propName = lineSplited[2];
                    var propType = lineSplited[1];

                    // Skiping the virtual properties
                    if (line.Contains(" virtual "))
                    {
                        propName = lineSplited[2 + 1];
                        propType = lineSplited[1 + 1];

                        if (propType.Contains("ICollection"))
                            continue;

                        setter(virtualPropertyBuilder(propType, propName));
                        continue;
                    }

                    setter(propertyBuilder(propType, propName));
                }

                File.WriteAllText(filePath, mTemplate.Replace(propIndetifier, ""));
                Logger.Done($"file {config.Header}{name}{config.Trailer}.cs created.");
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

            var option = Shared.OptionsSpreader<GenerationOptions>(name =>
            {
                if (name == GenerationOptions.Exit.ToString()) return "";
                return "Generate ";
            });

            switch (option)
            {
                case GenerationOptions.One_By_One:
                    Logger.Log("\nType the Class Name: ");
                    var name = Console.ReadLine();

                    this.Run(name);
                    Shared.Pause();
                    break;

                case GenerationOptions.All_At_Once:
                    this.Scaffolders.Models.ForEach(model => configs.ForEach(cf => this.Generate(model.Name, cf)));

                    Shared.Pause();
                    break;

                case GenerationOptions.Exit:
                    return;
            }

            goto Begin;
        }
    }
}
