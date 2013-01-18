using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using OSM.API.v6;
using LumenWorks.Framework.IO.Csv;
using System.Diagnostics;

namespace OsmBulkTools
{
    class Program
    {


        static int Main(string[] args)
        {
#if DEBUG
			args = "--csv-to-osm --in=a3000.csv --out=update.xml.osm".Split(' ');
#endif
			int result = -1; 
			
			try
			{
				Arguments arguments = Arguments.Parse(args);
				if (arguments.HasError)
				{
					Trace.WriteLine(arguments.Error);
				}
				else if (arguments.CsvToOsm)
				{
					TagUpdate updater = new TagUpdate(arguments);
					result = updater.Update();
					Trace.WriteLine("Done.");
				}
				else if (arguments.MergeDuplicate)
				{
					MergeDuplicate merge = new MergeDuplicate(arguments);
					merge.Merge();
					Trace.WriteLine("Done.");
				}


				if (arguments.ShowHelp)
				{
					Arguments.PrintUsage();
					result = 1;
				}



				//StreamReader osmReader = new StreamReader(args[0]);
				//osm root = OSM.API.v6.OsmFile.Read(osmReader);

				//Console.WriteLine("Loaded OSM with "+ root.Items.Count ().ToString()+ " items");

				////ExtractMissedNames(root, "missedNames.csv");
				//NamesCSV.BuildFile("highways.osm.xml", "all_names.csv");
			}
			catch (Exception e)
			{
				Trace.WriteLine(e.ToString());
				result = 2;
				Trace.WriteLine("Done with error.");
			}
			return result;
		}

		
	}
}
