using System;
using System.Collections.Generic;
using System.Text;
using UtilitiesLibrary.Collections;

namespace UtilitiesLibrary.MiscExtensions;



public static class TextExtensions {

	public static string ToWrittenConvention(this int integer) {

		return integer switch {
			< 0 => "negative " + (-integer).ToWrittenConvention(),
			0 => "zero",
			1 => "one",
			2 => "two",
			3 => "three",
			4 => "four",
			5 => "five",
			6 => "six",
			7 => "seven",
			8 => "eight",
			9 => "nine",
			>= 10 => integer.ToString()
		};
	}

	public static string SpaceCamelCaseName(string text) {

		if (string.IsNullOrWhiteSpace(text)) {
			return "";
		}

		StringBuilder spacedText = new();

		spacedText.Append(text[0]);

		for (int i = 1; i < text.Length; i++) {

			if (char.IsUpper(text[i])) {
				spacedText.Append(' ');
			}

			spacedText.Append(text[i]);
		}

		return spacedText.ToString();
	}

	public static string ToCsvFriendly(this string text) {

		// todo: surely I need something with commas here

		text = text.Replace("\"", "\"\"");
		text = "\"" + text + "\"";
		return text;
	}

	public static List<string> SplitTextToCsvColumns(this string text) {

		if (text.IsEmpty()) {
			return [];
		}

		List<string> columns = [];

		bool columnHasQuotes = false;
		bool outsideOfQuotes = true;
		int startOfColumn = 0;

		for (int i = 0; i < text.Length; i++) {

			// start of new column
			if (startOfColumn == i) {
				columnHasQuotes = text[i] is '\"';
				outsideOfQuotes = !columnHasQuotes;
			}

			// end of column
			if (text[i] is ',' && outsideOfQuotes) {

				columns.Add(columnHasQuotes
					? text[(startOfColumn + 1)..(i - 1)].Replace("\"\"", "\"")
					: text[startOfColumn..i]);

				startOfColumn = i + 1;
				continue;
			}

			if (text[i] is '\"' && startOfColumn != i) {

				// invalid quote placement
				if (!columnHasQuotes) {
					throw new ArgumentException("Quote in CSV column that is not enclosed by quotes.", nameof(text));
				}

				// end of the string or the current column
				if (text.Length == i + 1 || text[i + 1] is ',') {
					outsideOfQuotes = true;
					continue;
				}

				// skip over double quotes
				if (text[i + 1] is '\"') {
					i++;
					continue;
				}

				throw new ArgumentException("Single quote not at start or end of column", nameof(text));
			}

		}

		if (columnHasQuotes && !outsideOfQuotes) {
			throw new ArgumentException("Missing closing quote in last column.", nameof(text));
		}

		columns.Add(columnHasQuotes
			? text[(startOfColumn + 1)..^1].Replace("\"\"", "\"")
			: text[startOfColumn..]);

		return columns;
	}

}