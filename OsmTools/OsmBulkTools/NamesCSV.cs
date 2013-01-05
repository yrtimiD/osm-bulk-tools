using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OSM.API.v6;
using System.IO;
using System.Diagnostics;

namespace OsmBulkTools
{
	public static class NamesCSV
	{
		public static void BuildFile(string inputOsmXmlFile, string outputFileName)
		{
			Osm osm = OsmFile.Read(new StreamReader(inputOsmXmlFile));
			var ways = osm.Ways;
			
			var allNameKeys = ways.SelectMany(w=>w.Tags.Keys).Where(s=>s.StartsWith("name")).Distinct().OrderBy(s=>s).ToArray();
			foreach (var name in allNameKeys)
			{
				Trace.WriteLine(name);
			}

			StreamWriter writer = new StreamWriter(outputFileName);
			StringBuilder header = new StringBuilder();
			header.Append("id,version,");
			header.Append(String.Join(",", allNameKeys));
			writer.WriteLine(header);

			foreach (var w in ways)
			{
				StringBuilder line = new StringBuilder();
				line.AppendFormat("{0},{1},", w.id, w.version);
				bool haveAtLeastOneName = false;
				for (int i = 0; i < allNameKeys.Length; i++)
				{
					if (w.Tags.ContainsKey(allNameKeys[i]))
					{
						line.Append(w.Tags[allNameKeys[i]]);
						haveAtLeastOneName = true;
					}

					line.Append(",");
				}
				if (haveAtLeastOneName)
					writer.WriteLine(line.ToString());
			}

			writer.Flush();
			writer.Close();
		}

		private static void ExtractMissedNames(Osm root, string outputFileName)
		{
			var ways = root.Ways;
			var waysWithMissedNames = ways
				.Where(w =>
					   {
						   var d = w.Tags;
						   return !d.ContainsKey("name") || !d.ContainsKey("name:he") || !d.ContainsKey("name:en");
					   });


			List<String> lines = new List<string>();
			foreach (var w in waysWithMissedNames)
			{
				var tags = w.Tags;

				StringBuilder sb = new StringBuilder();


				sb.Append(w.id);
				sb.Append(",");
				sb.Append(w.version);
				sb.Append(",");


				if (tags.ContainsKey("name"))
					Add(sb, tags["name"]);

				sb.Append(",");

				if (tags.ContainsKey("name:en"))
					Add(sb, tags["name:en"]);

				sb.Append(",");

				if (tags.ContainsKey("name:he"))
					Add(sb, tags["name:he"]);


				lines.Add(sb.ToString());
			}
			lines.Sort();
			lines = lines.Distinct().ToList();
			lines.Insert(0, "\"id\",\"version\",\"name\",\"name:en\",\"name:he\"");
			File.WriteAllLines(outputFileName, lines.ToArray());
		}

		static StringBuilder Add(StringBuilder sb, String str)
		{
			return sb.Append(Safe(str));
		}

		static string Safe(String s)
		{
			return "\"" + s.Replace("\"", "\"\"") + "\"";
		}
	}
}
