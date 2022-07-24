using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Scaffolder.Models;

namespace Scaffolder
{
	public class Shared
	{
		public static string PathNormalizer(params string[] value)
		{
			if (value.Length == 0)
				return "";

			// Adding the root path to the collection
			var path = new System.Collections.Generic.List<string>
			{
				Application.Instance.SelectedProject.AppPath
			};

			// Adding the remain path to the list
			path.AddRange(value);

			// Building the full path
			return Shared.ResolvePath(Path.Combine(path.ToArray()));
		}

		static string BackFolder(string path, int numberOfJumps = 1)
		{
			for (int i = 0; i < numberOfJumps; i++) path = Directory.GetParent(path).FullName;
			return path;
		}

		string DictionaryPrinter(ICollection<string> keys)
		{
			var index = 1;
			// Printing the options
			foreach (var key in keys)
				Logger.Log($"({index++}|Yellow). {key}");

			Logger.ILog("Choose an option above: ");

			// Reading the selected option
			var typedKey = Console.ReadKey().KeyChar;
			Logger.Log(""); // Giving some space

			return typedKey.ToString();
		}

		public static T KeyConverter<T>() where T : Enum
		{
			// Getting the pressed key
			var op = Console.ReadKey();
			Logger.Log(@"");

			try
			{
				return (T)Enum.Parse(typeof(T), op.KeyChar.ToString());
			}
			catch (Exception ex)
			{
				Logger.Error($"Invalid Option!. Error: {ex.Message} ; {ex.InnerException?.Message ?? ""}");
				Console.ReadKey();
				return default(T);
			}
		}

		public static T OptionsSpreader<T>(Func<string, string> beforePrint = null) where T : Enum
		{
			var optionsFields = typeof(T).GetFields();

			for (int i = 0; i < optionsFields.Count(); i++)
			{
				if (i == 0) continue;

				var name = optionsFields[i].Name;
				var prefix = beforePrint?.Invoke(name);

				Logger.Log($"({i}|Yellow). {prefix}{name.Replace("_", " ")}");
			}

			Logger.ILog(@"Choose an option above: ");

			return Shared.KeyConverter<T>();
		}

		public static string LoadTemplate(string templ, Configuration config, int count)
		{
			if (File.Exists(config.Template) && templ == null)
				templ = File.ReadAllText(config.Template);
			else if (count > 1)
				templ = File.ReadAllText(config.Template);

			return templ;
		}

		public static List<string> GetModelProperties(string filePath)
		{
			return File.ReadAllLines(filePath)
					   .Where(x => x.Trim().StartsWith("public") && x.Trim().Contains("get;"))
					   .ToList();
		}

		public static void Pause()
		{
			Logger.Log("\nPress any key on keyboard to continue...");
			Console.ReadKey();
		}

        public static string ResolvePath(string path) 
        {
            return (OperatingSystem.IsWindows() ? path : ("/" + path));
        }
	}
}