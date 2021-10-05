using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Newtonsoft.Json;

namespace Scaffolder
{
    public class Application
    {
        private string configFilePath;

        public string Name { get; set; }
        public string Version { get; set; }
        public IDictionary<string, dynamic> Applications { get; set; }

        public Project SelectedProject { get; internal set; }
        public static Project GetSelectedProject { get; internal set; }

        public static Application Instance;


        public Application(string configFilePath = "config.json")
        {
            this.configFilePath = configFilePath;
            Instance = this;
        }


        public void Run()
        {
        Init:
            // Spreading the options
            var selectedAppName = this.UserSelectedOption();

            // Loading the project
            this.SelectedProject = this.GetProject(selectedAppName);
            GetSelectedProject = this.SelectedProject;


        Beginning:
            var selectedOption = this.SelectedProject.UserSelectedOption(
                $"Project -> ({ selectedAppName }|Yellow)\n"
            );

            try
            {
                if (selectedOption == "Exit") 
                {
                    Console.Clear();
                    goto Init;
                }

                IEnumerable<Type> types = Assembly.GetExecutingAssembly().ExportedTypes;
                var type = types.FirstOrDefault(x => x.Name == $"{ selectedOption }Scaffold");

                if (type == null) throw new Exception("Not Found");

                // Instantiating the scaffolder class chosed
                var @class = Activator.CreateInstance(type);
                type.GetMethod("Init").Invoke(@class, null);

                Shared.Pause();
            }
            catch
            {
                Logger.Error("Unavailable option, try again!");
            }

            goto Beginning;
        }

        public string UserSelectedOption()
        {
        Beginning:

            var index = 0;
            var keys = this.Applications.Keys;

            Logger.Log("Scaffolder Projects\n");

            // Printing the options
            Logger.Log($"({index++}|Yellow). Create (New|Blue) Config");
            foreach (var key in keys)
                Logger.Log($"({index++}|Yellow). { key }");

            Logger.ILog("Choose an option above: ");

            // Reading the selected option
            var typedKey = Console.ReadKey().KeyChar;
            Logger.Log(""); // Giving some space

            // If invalid
            if (!int.TryParse(typedKey.ToString(), out int selectedOptionIndex) ||
                (keys.Count <= (selectedOptionIndex - 1)))
            {
                goto Beginning;
            }

            if (selectedOptionIndex == 0)
            {
                Logger.ILog($"Type App Name: ");
                var typedLine = Console.ReadLine().Trim();

                if (string.IsNullOrEmpty(typedLine))
                {
                    Logger.Error("Invalid Name Provided!");
                    goto Beginning;
                }

                if (this.Applications.ContainsKey(typedLine))
                {
                    Logger.Error("This Application Scaffolder already exists.");
                    goto Beginning;
                }

                this.Applications.Add(typedLine, new
                {
                    AppPath = ".",
                    Scaffolders = new
                    {
                        Models = new[]{
                                new {
                                    DbModels = $"{ typedLine }/Models"
                                }
                            },

                        Controller = new[]{
                                new {
                                    Tail = "Controller",
                                    Output = $"{ typedLine }/Controllers/Api",
                                    Template = "controller.txt"
                                }
                            },

                        Service = new[]{
                                new {
                                    Tail = "Service",
                                    Output = $"{ typedLine }/Services",
                                    Template = "service.txt"
                                }
                            },

                        ViewModel = new[]{
                                new {
                                    Tail = "Dto",
                                    Output = $"{ typedLine }/Dto",
                                    Template = "viewmodel.txt",
                                    Namespace = $"{ typedLine }",
                                }
                            },

                        EFCore = new[] {
                                new {
                                    Tail = "Config",
                                    Output = $"{ typedLine }/Data",
                                    Template = "efconfig.txt"
                                }
                            }
                    }
                });

                var content = JsonConvert.SerializeObject(this, Formatting.Indented);

                File.WriteAllText(configFilePath, content);
                var dir = Directory.CreateDirectory(Path.Combine("apps", typedLine));

                // Copying the files into the dir
                Directory.EnumerateFiles(".tmp")
                    .ToList()
                    .ForEach(path =>
                    {

                        var fileInfo = new FileInfo(path);
                        var fileContent = File.ReadAllText(path);
                        var fileNewContent = fileContent.Replace("@-Namespace-@", typedLine);
                        File.WriteAllText(Path.Combine(dir.FullName, fileInfo.Name), fileNewContent);
                    });

                Logger.Log("");
                Logger.Log($"({ typedLine }|Blue) Config Added to (config.json|Yellow), please open it, configure it " +
                    "and add all the missing templates according to the new project classes. \nPlease add: ");
                Logger.Warn($"controller.txt, efconfig.txt, viewmodel.txt and service.txt");

                Logger.Log("\nAfter every change type any key to reload the configurations...");
                Console.ReadKey();
                Logger.Log("");

                this.Init(); // Reloading all the configurations
                goto Beginning;
            }

            Console.Clear();
            return keys.ElementAt(selectedOptionIndex - 1);
        }

        public Application Init()
        {
            Logger.Log("Loading the configurations...");
            if (!File.Exists(configFilePath))
            {
                var content = JsonConvert.SerializeObject(new
                {
                    Name = "Scaffolder",
                    Version = "1.1.0",
                    Applications = new { }
                }, Formatting.Indented);

                File.WriteAllText(configFilePath, content);
            }

            var config = JsonConvert.DeserializeObject<Application>(File.ReadAllText(configFilePath));

            var applicationType = typeof(Application);

            // Setting all the values in the main object
            applicationType.GetProperties()
                .ToList()
                .ForEach(item =>
                {
                    // Getting the value from the source
                    var value = applicationType.GetProperty(item.Name).GetValue(config);
                    // Setting it to the destination
                    item.SetValue(this, value);
                });

            

            Logger.Done("Configurations successfuly loaded.");
            Thread.Sleep(500);
            System.Console.Clear();

            return this;
        }

        public Project GetProject(string appKey)
        {
            if (!this.Applications.ContainsKey(appKey))
            {
                Logger.Error("Application Configuration Not Found.");
                return null;
            }

            var project = (Project)JsonConvert.DeserializeObject<Project>(
                this.Applications[appKey].ToString()
            );

            // Setting the name of the selected Application
            project.AppName = appKey;

            return project;
        }
    }
}