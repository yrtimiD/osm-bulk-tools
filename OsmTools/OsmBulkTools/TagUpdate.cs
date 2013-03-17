using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;
using LumenWorks.Framework.IO.Csv;
using OSM.API.v6;
using System.Text.RegularExpressions;

namespace OsmBulkTools
{
	public class TagUpdate
	{
		const string ID = "@id";
		const string TYPE = "@type";
		const string UPDATE = "@update";

		Arguments arg;
		public TagUpdate(Arguments arg)
		{
			this.arg = arg;
		}

		public int Update()
		{
			Trace.WriteLine("Current path: " + Environment.CurrentDirectory);
			Trace.WriteLine("Input: " + arg.Input + " Output: " + arg.Output);
			Trace.WriteLine("reading csv data");
			StreamReader csvReader = new StreamReader(arg.Input);
			CsvReader reader = new CsvReader(csvReader, true);
			#region CSV read
			List<Dictionary<string, string>> data = new List<Dictionary<string, string>>();
			string[] headers = reader.GetFieldHeaders();
			bool hasUpdateColumn = !String.IsNullOrEmpty(arg.UpdateField) && headers.Contains(arg.UpdateField);

			if (!ValidateHeaders(headers))
				return 3;

			while (reader.ReadNextRecord())
			{
				Dictionary<string, string> row = new Dictionary<string, string>();

				for (int i = 0; i < reader.FieldCount; i++)
				{
					if (headers [i] != null)//all wrong and ignored columns marked by null in ValidateHeaders(..)
						row.Add(headers [i], reader [i]);
				}

				if (hasUpdateColumn && String.IsNullOrEmpty(row [UPDATE]))
				{
					//skipping current row as it is not marked "for update"
				} else
				{
					data.Add(row);
				}
			}
			reader.Dispose();
			#endregion

			Osm update = new Osm
			{
				version = "0.6",
				upload = true,
				uploadSpecified = true,
				generator = "Israel Street Names Uploader",
				Nodes = new List<Node>(),
				Ways = new List<Way>(),
				Relations = new List<Relation>()
			};

			bool noDuplicateIDsFound = false;

			Trace.WriteLine("Processing nodes");
			Trace.Indent();
			var nodesOnly = data.Where(d => d [TYPE] == "0");
			noDuplicateIDsFound = CheckForDuplicateID(nodesOnly);
			if (noDuplicateIDsFound)
			{
				UpdateElements(update, nodesOnly, ElementType.Node);
			} 
			else
			{
				Trace.WriteLine("Warning: Duplicate node IDs found. Nodes will not be processed.");
			}
			Trace.Unindent();

			Trace.WriteLine("Processing ways");
			Trace.Indent();
			var waysOnly = data.Where(d => d[TYPE] == "1");
			noDuplicateIDsFound = CheckForDuplicateID(waysOnly);
			if (noDuplicateIDsFound)
			{
				UpdateElements(update, waysOnly, ElementType.Way);
			}
			else
			{
				Trace.WriteLine("Warning: Duplicate way IDs found. Ways will not be processed.");
			}
			Trace.Unindent();

			Trace.WriteLine("Processing relations");
			Trace.Indent();
			var relOnly = data.Where(d => d[TYPE] == "2");
			noDuplicateIDsFound = CheckForDuplicateID(relOnly);
			if (noDuplicateIDsFound)
			{
				UpdateElements(update, relOnly, ElementType.Relation);
			}
			else
			{
				Trace.WriteLine("Warning: Duplicate relation IDs found. Relations will not be processed.");
			}
			Trace.Unindent();

			Trace.WriteLine("Saving update file");
			OsmFile.Write(update, new StreamWriter(arg.Output));

			return 0;
		}

		private static Regex tagRegex = new Regex(@"^[a-z0-9_\-:]+$");
		/// <summary>
		/// id, type and update header names replaced with hardcoded known values. all wrong and starting from '@' replaced with <c>null</c>
		/// </summary>
		private bool ValidateHeaders(string[] headers)
		{
			bool allGood = true;
			Trace.WriteLine("Validating headers");
			Trace.Indent();
			for (int i = 0; i < headers.Length; i++)
			{
				if (headers[i] == arg.IdField)
				{
					Trace.WriteLine(String.Format("{0}\tID", headers[i]));
					headers[i] = ID;
					continue;
				}
				else if (headers[i] == arg.TypeField)
				{
					Trace.WriteLine(String.Format("{0}\tType", headers[i]));
					headers[i] = TYPE;
					continue;
				}
				else if (headers[i] == arg.UpdateField)
				{
					Trace.WriteLine(String.Format("{0}\tUpdate flag", headers[i]));
					headers[i] = UPDATE;
					continue;
				}
				else if (headers[i].StartsWith("@"))
				{
					Trace.WriteLine(String.Format("{0}\tSkipping", headers[i]));
					headers[i] = null;
					continue;
				}
				else if (tagRegex.IsMatch(headers[i]) == false)
				{
					Trace.WriteLine(String.Format("{0}\tSuspicious tag name", headers[i]));
					headers[i] = null;
					allGood = false;
					continue;
				}

				Trace.WriteLine(String.Format("{0}\tOK", headers[i]));
			}
			Trace.Unindent();
			return allGood;
		}

		private void UpdateElements(Osm update, IEnumerable<Dictionary<string, string>> data, ElementType type)
		{
			Dictionary<long, Dictionary<string, string>> dataById = data.ToDictionary(d => long.Parse(d[ID]), d => d);

			Proxy p = new Proxy();
			int page = 0;
			int pageSize = 100;
			int count = data.Count();
			while (page * pageSize < count)
			{
				//Trace.WriteLine("Page " + page.ToString() + " size " + pageSize.ToString());
				var currentPage = dataById.Keys.Skip(page * pageSize).Take(pageSize);

				Trace.WriteLine(String.Format("Getting next {0} {1}s from the API", pageSize, type));
				IEnumerable<Element> elements = null;
				#region get elements by type
				try
				{
					switch (type)
					{
						case ElementType.Node:
							elements = p.GetNodes(currentPage).Cast<Element>();
							break;
						case ElementType.Way:
							elements = p.GetWays(currentPage).Cast<Element>();
							break;
						case ElementType.Relation:
							elements = p.GetRelations(currentPage).Cast<Element>();
							break;
					}
				}
				catch (Exception ex)
				{
					Trace.WriteLine(ex.ToString());
					continue;
				}
				#endregion

				Update(elements, dataById, type);

				switch (type)
				{
					case ElementType.Node:
						var changedNodes = elements.Where(e => e.actionSpecified).Cast<Node>();
						update.Nodes.AddRange(changedNodes);
						break;
					case ElementType.Way:
						var changedWays = elements.Where(e => e.actionSpecified).Cast<Way>();
						update.Ways.AddRange(changedWays);
						break;
					case ElementType.Relation:
						var changedRelations = elements.Where(e => e.actionSpecified).Cast<Relation>();
						update.Relations.AddRange(changedRelations);
						break;
					default:
						throw new ArgumentException("Unsupported elements type: " + type.ToString());
				}

				page++;
				int done = ((page * pageSize)<count)?(page * pageSize):count;
				Trace.WriteLine(String.Format("{0} out of {1} {2}s have been processed", done, count, type));
			}
		}

		/// <summary>
		/// Returns true if all ID's are distinct
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		private static bool CheckForDuplicateID(IEnumerable<Dictionary<string, string>> data)
		{
			bool noDuplicateFound = true;
			Trace.WriteLine("Checking for duplicate IDs");
			Trace.Indent();
			HashSet<long> ids = new HashSet<long>();
			foreach (var row in data)
			{
				if (ids.Add(long.Parse(row[ID])) == false)
				{
					Trace.WriteLine("Duplicate ID: " + row[ID]);
					noDuplicateFound = false;
				}
			}
			Trace.Unindent();
			return noDuplicateFound;
		}

		/// <summary>
		/// Actually updates tag of elements by data in dataById
		/// </summary>
		/// <param name="elements"></param>
		/// <param name="dataById"></param>
		/// <param name="type"></param>
		private static void Update(IEnumerable<Element> elements, Dictionary<long, Dictionary<string, string>> dataById, ElementType type)
		{
			Trace.WriteLine("Updating " + type.ToString() + "s");
			Trace.Indent();
			foreach (var e in elements)
			{
				if (e.visible == false)
				{
					Trace.WriteLine("Skipping deleted " + type + ": " + e.id.ToString());
					continue;
				}
				Trace.WriteLine("ID:" + e.id);
				bool changed = UpdateTags(e.Tags, dataById[e.id]);
				if (changed)
				{
					e.action = OSM.API.v6.Action.Modify;
				}
			}
			Trace.Unindent();
		}

		private static bool UpdateTags(Dictionary<string, string> tags, Dictionary<string, string> data)
		{
			bool changed = false;

			Trace.Indent();
			Trace.WriteLine("Updating tags");
			foreach (var item in data)
			{
				if (item.Key.StartsWith("@") || item.Key.Length == 0) //skip system keys
				{
					continue;
				}
				else if (String.IsNullOrEmpty(item.Value)) //empty value - delete tag
				{
					if (tags.ContainsKey(item.Key))
					{
						Trace.WriteLine(String.Format("- {0}={1}",item.Key, tags[item.Key]));
						tags.Remove(item.Key);
						changed = true;
					}
				}
				else //add or replace new value
				{
					if (tags.ContainsKey(item.Key))
					{
						if (tags[item.Key] != item.Value)
						{
							Trace.WriteLine(String.Format("- {0}={1}", item.Key, tags[item.Key]));
							Trace.WriteLine(String.Format("+ {0}={1}", item.Key, item.Value));
							changed = true;
						}
					}
					else
					{
						Trace.WriteLine(String.Format("+ {0}={1}", item.Key, item.Value));
						changed = true;
					}
					tags[item.Key] = item.Value;
				}
			}
			Trace.Unindent();
			return changed;
		}


		//static readonly String[] nameTags = new[] { "name", "name:he", "name:he1", "name:he2", "name:he3", "name:he4", "name:ar", "name:ru", "name:en", "name:en1", "name:en2", "name:en3", "name:en4", "name:en5", "name:en6" };
		//private static void RemoveAllNameTags(Dictionary<String, String> tags)
		//{
		//    foreach (var n in nameTags)
		//    {
		//        tags.Remove(n);
		//    }
		//}
	}
}
