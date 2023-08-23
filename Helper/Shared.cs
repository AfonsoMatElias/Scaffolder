using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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
			var typedKey = Logger.ReadKey().KeyChar;
			Logger.Log(""); // Giving some space

			return typedKey.ToString();
		}

		public static T KeyConverter<T>() where T : Enum
		{
			// Getting the pressed key
			var op = Logger.ReadKey();
			Logger.Log(@"");

			try
			{
				return (T)Enum.Parse(typeof(T), op.KeyChar.ToString());
			}
			catch (Exception ex)
			{
				Logger.Error($"Invalid Option!. Error: {ex.Message} ; {ex.InnerException?.Message ?? ""}");
				Logger.ReadKey();
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

		public static List<string> LoadTemplateLines(string templ, Configuration config, int count)
		{
			List<string> data = new List<string>();
			if (File.Exists(config.Template) && templ == null)
				data = File.ReadAllLines(config.Template).ToList();
			else if (count > 1)
				data = File.ReadAllLines(config.Template).ToList();

			if (data == null && templ != null)
				data = templ.Split("\n").ToList();

			return data;
		}

		public static string LoadTemplate(string templ, Configuration config, int count)
		{
			return string.Join("\n", LoadTemplateLines(templ, config, count));
		}

		public static List<string> GetModelProperties(string filePath)
		{
			return File.ReadAllLines(filePath)
					   .Where(x => x.Trim().StartsWith("public") && x.Trim().Contains("get;"))
					   .ToList();
		}

		public static List<string> GetModelLines(string filePath)
		{
			return File.ReadAllLines(filePath).ToList();
		}

		public static string StringReplacer(string line, string wordToFind, string wordToReplace, int position = 0, bool isContain = false)
		{
			var words = line.Split(" ")
					.Where(x => !string.IsNullOrWhiteSpace(x))
					.ToList().ToList();

			var index = -1;
			if (isContain)
				index = words.FindIndex(w => w.Contains(wordToFind));
			else
				index = words.IndexOf(wordToFind);

			if (words.Count >= index + position)
				words[index + position] = wordToReplace;

			return String.Join(" ", words);
		}

		public static PropStructure BuildProperty(string line)
		{
			// public string Designacao { get; set; }
			// public ICollection<Curso> Cursos { get; set; }

			var parts = line.Trim().Replace(" virtual ", " ").Split(" ").Where(x => !string.IsNullOrWhiteSpace(x)).ToList();

			return new PropStructure
			{
				IsVirtual = line.Contains(" virtual "),
				Modifier = parts.ElementAt(0),
				Type = parts.ElementAt(1),
				ParameterType = new Regex("<(.*?)>").Match(parts.ElementAt(1)).Groups.Values.LastOrDefault()?.Value,
				Name = parts.ElementAt(2),
			};
		}

		public static void Pause()
		{
			Logger.Log("\nPress any key on keyboard to continue...");
			Logger.ReadKey();
		}

		public static string ResolvePath(string path)
		{
			return (OperatingSystem.IsWindows() ? path : ("/" + path));
		}
	}

	public class PropStructure
	{
		public bool IsVirtual { get; set; }
		public string Modifier { get; set; }
		public string Type { get; set; }
		public string ParameterType { get; set; }
		public string Name { get; set; }
	}
}