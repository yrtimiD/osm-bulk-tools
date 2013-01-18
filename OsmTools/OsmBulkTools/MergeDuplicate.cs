using System;
using System.IO;
using OSM.API.v6;
using System.Collections.Generic;
using System.Diagnostics;

namespace OsmBulkTools
{
	public class MergeDuplicate
	{
		Arguments args;

		public MergeDuplicate (Arguments args)
		{
			this.args = args;
		}

		public void Merge()
		{
			int modify = 0;
			int delete = 0;

			Dictionary<String, List<Node>> nodesByCoord = new Dictionary<string, List<Node>>();
			Osm osm = OsmFile.Read(new StreamReader(args.Input));
			foreach (Node n in osm.Nodes)
			{
				String key = String.Format("{0}_{1}", n.lat, n.lon);
				if (!nodesByCoord.ContainsKey(key))
				{
					nodesByCoord.Add(key, new List<Node>());
				}

				nodesByCoord [key].Add(n);
			}
			foreach (List<Node> nodes in nodesByCoord.Values)
			{
				if (nodes.Count > 1)
				{
					Node n = nodes [0];
					n.action = OSM.API.v6.Action.Modify;
					modify++;
					for (int i=1; i<nodes.Count; i++)
					{
						MergeTags(n, nodes [i]);
						nodes [i].action = OSM.API.v6.Action.Delete;
						delete++;
					}
				}
			}

			Trace.WriteLine("Change summary");
			Trace.Indent();
			Trace.WriteLine(String.Format("Processed nodes: {0}", osm.Nodes.Count));
			Trace.WriteLine(String.Format("Unique coordinates: {0}", nodesByCoord.Count));
			Trace.WriteLine(String.Format("Modified: {0}", modify));
			Trace.WriteLine(String.Format("Deleted: {0}", delete));
			Trace.Unindent();

			OsmFile.Write(osm, new StreamWriter(args.Output));
		}

		public void MergeTags(Node to, Node from)
		{
			foreach (Tag t in from.tags)
			{
				if (to.Tags.ContainsKey(t.k))
				{
					//concatenate value only if value is different
					//FIXME: must compare new value with all previously concatenated. For example: A B A will not skip second A
					if (to.Tags[t.k] != t.v)
						to.Tags[t.k] += ";"+t.v;
				}
				else 
				{
					to.Tags.Add(t.k, t.v);
				}
			}
		}
	}
}

