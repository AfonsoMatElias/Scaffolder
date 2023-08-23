using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace Scaffolder
{
	public static class Logger
	{
		private static string Print(string text, ConsoleColor color)
		{
			// Changing the color to the wanted one
			Console.ForegroundColor = color;

			// Logging the text
			Console.Write(text);

			// Reseting the color
			Console.ForegroundColor = ConsoleColor.White;

			return text;
		}

		/// <summary>
		/// Print colorized red text
		/// </summary>
		/// <param name="text">The text do me logged</param>
		/// <returns>The text logged</returns>
		public static string Done(string text) => Log(text, ConsoleColor.Green);

		/// <summary>
		/// Print colorized red text
		/// </summary>
		/// <param name="text">The text do me logged</param>
		/// <returns>The text logged</returns>
		public static string Error(string text) => Log(text, ConsoleColor.Red);

		/// <summary>
		/// Print colorized red text
		/// </summary>
		/// <param name="text">The text do me logged</param>
		/// <returns>The text logged</returns>
		public static string Warn(string text) => Log(text, ConsoleColor.Yellow);

		/// <summary>
		/// Performs the <see cref="Console.WriteLine"/> method with colorized text if wanted
		/// </summary>
		/// <param name="text">The text do me logged</param>
		/// <returns>The text logged</returns>
		public static string Log(string text, ConsoleColor color = ConsoleColor.White)
		{
			// Running the default log
			Logger.ILog(text, color);
			// Breaking the line as it was assumed Console.WriteLine execution
			Console.Write("\n");
			return text;
		}

		/// <summary>
		/// Executes <see cref="Console.Write"/> with colorized text if needed
		/// </summary>
		/// <param name="text">The text do me logged</param>
		/// <returns>The text logged</returns>
		public static string ILog(string text, ConsoleColor color = ConsoleColor.White)
		{
			if (color != ConsoleColor.White)
				return Print(text, color);

			// Matching the pattern
			var matches = new Regex("\\(([\\S\\s]*?)\\|([\\S\\s]*?)\\)")
				.Matches(text);

			// Storing the original text
			var remainText = text;

			// last index reminder
			var lastIndex = 0;

			// Looping the matches
			foreach (var match in matches)
			{
				// Getting the value 
				var pattern = match.GetType().GetProperty("Value").GetValue(match).ToString();

				// Getting the index of the section
				var index = int.Parse(match.GetType().GetProperty("Index").GetValue(match).ToString());
				// Taking the section that has peace of string that will be colorized
				var section = text.Substring(lastIndex, (index + pattern.Length - lastIndex));

				// Storing the last index
				lastIndex = index + pattern.Length;

				// Getting the remain text
				remainText = remainText.Substring(section.Length);

				// Getting the uncolorized part
				var uncolorizedSection = section.Split(pattern).FirstOrDefault();

				// Printing it
				Console.Write(uncolorizedSection);

				// Getting the content of the pattern
				var patternSplitted = pattern.Substring(1, pattern.Length - 2).Split("|");

				// Getting the value
				var patternValue = patternSplitted.FirstOrDefault();

				// Setting the defatut color
				var patternColor = ConsoleColor.White;

				var patternColorString = patternSplitted.LastOrDefault();

				// Trying to parse the color provided
				Enum.TryParse(typeof(ConsoleColor), patternColorString, out object outPatternColor);

				// Setting the color if it was found
				patternColor = outPatternColor != null ? (ConsoleColor)outPatternColor : patternColor;

				// Printing the colorized text
				Print(patternValue, patternColor);
			}

			// Logging the remain text
			Console.Write(remainText);
			return text;
		}

		/// <summary>
		/// Call Console.ReadKey()
		/// </summary>
		/// <returns></returns>
		public static ConsoleKeyInfo ReadKey()
		{
            return Console.ReadKey();
		}
		
		/// <summary>
		/// Call Console.ReadLine()
		/// </summary>
		/// <returns></returns>
        public static string ReadLine()
		{
            return Console.ReadLine();
		}

		/// <summary>
		/// Performe Console.ReadLine with autocomplete phrases
		/// </summary>
		/// <param name="wordsToAutoComplete">phrases to autocomplete</param>
		/// <returns></returns>
		public static string ReadLine(string[] wordsToAutoComplete)
		{
            var content = Read(wordsToAutoComplete);
            Console.WriteLine();
            return content;
		}

		private static string Read(string[] wordsToAutoComplete)
		{
			// Resolving the words
			var words = (wordsToAutoComplete ?? Array.Empty<string>()).ToList();

			// String builder
			var builder = new Stack<char>();

			/// converts the Stack to a String
			string ToStr() => string.Join("", builder.Reverse());

			// Re-print whole line
			void PrintContentInLine()
			{
				ClearCurrentLine();
				Console.Write(ToStr());
			}

			void ClearCurrentLine()
			{
				var currentLine = Console.CursorTop;
				Console.SetCursorPosition(0, currentLine);
				Console.Write(new string(' ', Console.BufferWidth));
				Console.SetCursorPosition(0, currentLine);
			}

			void Reading()
			{
				var input = Console.ReadKey(intercept: true);

				switch (input.Key)
				{
					case ConsoleKey.Enter:
						Console.Write(input.KeyChar);
						Console.Write(ToStr());
						return;
					case ConsoleKey.Backspace:

						{
							builder.Pop();
							PrintContentInLine();
						}
						break;
					case ConsoleKey.Tab:

						// Example: Afonso, Diambo, Mat
						// Result: Mat
						var lastWord = ToStr().Split(",").SelectMany(x => x.Trim().Split(" ")).LastOrDefault();

						// List: Afonso Diambo Matumona Elias
						var word = words.FirstOrDefault(x => x != lastWord && x.StartsWith(lastWord, true, CultureInfo.InvariantCulture));

						if (word != null)
						{
							// Removing: Mat
							for (int i = 0; i < lastWord.Length; i++)
								builder.Pop();

							// Adding: Matumona
							for (int i = 0; i < word.Length; i++)
								builder.Push(word[i]);
						}

						PrintContentInLine();

						break;

					default:
						builder.Push(input.KeyChar);
						Console.Write(input.KeyChar);
						break;
				}

				Reading();
			}

			Reading();
			return ToStr();
		}
	}
}