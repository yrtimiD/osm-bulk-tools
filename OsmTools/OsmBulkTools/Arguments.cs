using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace OsmBulkTools
{
	public class Arguments
	{
		const string PREFIX = "--";
		const string ARG_IN = PREFIX + "in=";
		const string ARG_OUT = PREFIX + "out=";
		const string ARG_CSV_TO_OSM = PREFIX + "csv-to-osm";
		const string ARG_ID = PREFIX + "id=";
		const string ARG_TYPE = PREFIX + "type=";
		const string ARG_UPDATE = PREFIX + "update=";
		const string ARG_MERGE_DUPLICATE = PREFIX + "merge-duplicate";
		static readonly string MISSED_ARG_ERROR = "Must supply {0} parameter" + Environment.NewLine;

		public bool ShowHelp { get; private set; }
		public string Error { get; private set; }
		public bool HasError { get { return String.IsNullOrEmpty(Error) == false; } }
		public bool CsvToOsm { get; private set; }
		public string Input { get; private set; }
		public string Output { get; private set; }
		public string IdField { get; private set; }
		public string TypeField { get; private set; }
		public string UpdateField { get; private set; }
		public bool MergeDuplicate { get; private set; }

		protected Arguments ()	{	}

		public static Arguments Parse(string[] args){
			Arguments result = new Arguments();

			foreach (string arg in args)
			{
				string argName = arg;
				var tokens = arg.Split('=');
				if (tokens.Length == 2)
					argName = tokens[0] + "=";

				switch (argName)
				{
					case ARG_CSV_TO_OSM:
						result.CsvToOsm = true;
						break;
					case ARG_IN:
						result.Input = GetValue(ARG_IN, arg);
						break;
					case ARG_OUT:
						result.Output = GetValue(ARG_OUT, arg);
						break;
					case ARG_ID:
						result.IdField = GetValue(ARG_ID, arg);
						break;
					case ARG_TYPE:
						result.TypeField = GetValue(ARG_TYPE, arg);
						break;
					case ARG_UPDATE:
						result.UpdateField = GetValue(ARG_UPDATE, arg);
						break;
					case ARG_MERGE_DUPLICATE:
						result.MergeDuplicate = true;
						break;
					default:
						result.Error += "Unknown parameter " + arg + Environment.NewLine;
						break;
				}
			}

			result.Validate();
			return result;
		}

		private void Validate()
		{
			if (CsvToOsm)
			{
				if (String.IsNullOrEmpty(Input))
					Error += String.Format(MISSED_ARG_ERROR, ARG_IN);
				if (String.IsNullOrEmpty(Output))
					Error += String.Format(MISSED_ARG_ERROR, ARG_OUT);
				if (String.IsNullOrEmpty(IdField))
					Error += String.Format(MISSED_ARG_ERROR, ARG_ID);
				if (String.IsNullOrEmpty(TypeField))
					Error += String.Format(MISSED_ARG_ERROR, ARG_TYPE);
			}
			else if (MergeDuplicate)
			{
				if (String.IsNullOrEmpty(Input))
					Error += String.Format(MISSED_ARG_ERROR, ARG_IN);
				if (String.IsNullOrEmpty(Output))
					Error += String.Format(MISSED_ARG_ERROR, ARG_OUT);
			}
			else
			{
				ShowHelp = true;
			}
		}

		private static string GetValue(string argumentName, string argumentWithValue)
		{
			return argumentWithValue.Substring(argumentName.Length).Trim();
		}

		public static void PrintUsage()
		{
			Trace.WriteLine(ARG_CSV_TO_OSM + "\tCSV to OSM convert");
			Trace.WriteLine(ARG_MERGE_DUPLICATE + "\tMerge duplicate nodes");
			Trace.WriteLine(ARG_IN + "<csv file>\tinput CSV file name");
			Trace.WriteLine(ARG_OUT + "<osm file>\toutput OSM file name");
			Trace.WriteLine(ARG_ID + "<column name>\tname of the column contains entity ID value");
			Trace.WriteLine(ARG_TYPE + "<column name>\tname of the column contains entity type value (0-node, 1-way, 2-relation)");
			Trace.WriteLine(ARG_UPDATE + "<column name>\tname of the column contains update flag value (optional)");
			Trace.WriteLine(String.Empty);
		}
	}
}
