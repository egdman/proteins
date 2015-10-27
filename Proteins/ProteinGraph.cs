using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using Fusion;
using Fusion.Graphics;
using Fusion.Mathematics;
using Fusion.Input;

using GraphVis;

namespace Proteins
{
	class ProteinGraph : GraphFromFile
	{

		class Interaction
		{
			public int id1;
			public int id2;
			public string type;
		}
		Dictionary<string, int> idByName;

		public Color HighlightNodesColorPos { get; set; }
		public Color HighlightNodesColorNeg { get; set; }

		public int GetIdByName(string name)
		{
			return idByName[name];
		}
		


		public ProteinGraph() : base()
		{
			idByName = new Dictionary<string, int>();
			HighlightNodesColorPos = 
				HighlightNodesColorNeg = 
				Color.White;
		}

		public override void ReadFromFile(string path)
		{
			var lines = File.ReadAllLines(path);

			Dictionary<int, int> uniqueIds = new Dictionary<int, int>();

			List<Interaction> interactions = new List<Interaction>();
			int numNodes = 0;
			foreach (var line in lines)
			{
				string [] parts = line.Split(new Char[] { '\t', ',' });
				string name = parts[0];
				int	id		= int.Parse(parts[1]);
				int	otherId	= -1;
				if (parts[3].Length > 0)
				{
					otherId = int.Parse(parts[2]);
				}
				string edgeType = parts[3];

				int cat1 = int.Parse(parts[4]);
				int cat2 = int.Parse(parts[5]);
				int cat3 = int.Parse(parts[6]);

				if (!uniqueIds.ContainsKey(id))
				{
					uniqueIds.Add(id, numNodes);
					idByName.Add(name, numNodes);
					++numNodes;

					Color color = new Color((float)cat1, (float)cat2, (float)cat3);
					AddNode(new ProteinNode(name, 1.0f, color));
					 
				}
				if (otherId >= 0)
				{
					interactions.Add(new Interaction{id1 = id, id2 = otherId, type = edgeType});
				}
			}

			foreach (var inter in interactions)
			{
				addInteraction(uniqueIds[inter.id1], uniqueIds[inter.id2], inter.type);
			}
		}


		public List<Tuple<ProteinInteraction, int>> GetInteractions(string name1, string name2)
		{
			int id1 = GetIdByName(name1);
			int id2 = GetIdByName(name2);
			var edgeIndices = GetEdgeIndices(id1, id2);
			List<Tuple<ProteinInteraction, int>> interactions
				= new List<Tuple<ProteinInteraction, int>>();
			foreach ( int ei in edgeIndices )
			{
				interactions.Add(
					new Tuple<ProteinInteraction, int>
						((ProteinInteraction)Edges[ei], ei)
					);
			}
			return interactions;
		}



		public List<Tuple<ProteinInteraction, int>> GetInteractions(string name)
		{
			int id = GetIdByName(name);
			var edgeIndices = GetEdges(id);
			List<Tuple<ProteinInteraction, int>> interactions
				= new List<Tuple<ProteinInteraction, int>>();
			foreach (int ei in edgeIndices)
			{
				interactions.Add(
					new Tuple<ProteinInteraction, int>
						((ProteinInteraction)Edges[ei], ei)
					);
			}
			return interactions;
		}

		public List<Tuple<ProteinInteraction, int>> GetOutcomingInteractions(string name)
		{
			var allInteractions = GetInteractions(name);
			int thisProtId =  GetIdByName(name);
			List<Tuple<ProteinInteraction, int>> outInteractions 
				= new List<Tuple<ProteinInteraction,int>>();
			foreach (var tuple in allInteractions)
			{
				if (tuple.Item1.End1 == thisProtId)
				{
					outInteractions.Add(tuple);
				}
			}
			return outInteractions;
		}


		public List<Tuple<ProteinInteraction, int>> GetIncomingInteractions(string name)
		{
			var allInteractions = GetInteractions(name);
			int thisProtId = GetIdByName(name);
			List<Tuple<ProteinInteraction, int>> inInteractions
				= new List<Tuple<ProteinInteraction, int>>();
			foreach (var tuple in allInteractions)
			{
				if (tuple.Item1.End2 == thisProtId)
				{
					inInteractions.Add(tuple);
				}
			}
			return inInteractions;
		}


		public ProteinNode GetProtein(string name)
		{
			return (ProteinNode)Nodes[idByName[name]];
		}


		public ProteinNode GetProtein(int index)
		{
			return (ProteinNode)Nodes[index];
		}


		public ProteinInteraction GetInteraction(int index)
		{
			return (ProteinInteraction)Edges[index];
		}


		void addInteraction(int id1, int id2, string interType)
		{
			ProteinInteraction edge = new ProteinInteraction();
			edge.End1 = id1;
			edge.End2 = id2;
			edge.Length = 1.0f;
			edge.Type = new string(interType[0], 1);
			float strength = 0;
			if (interType.Length == 0)
			{
				return;
			}
			if (interType[0] == '+')
			{
				strength = 0.5f;
			}
			else if (interType[0] == '-')
			{
				strength = 0.5f;
			}
			else if (interType[0] == 'b')
			{
				strength = 5.0f;
			}
			else
			{
				strength = 0;
			}
			edge.Value = strength;
			AddEdge(edge);
		}


		public void Propagate(GraphSystem graphSystem, float time)
		{
			List<Tuple<int, SignalType>> updatedSignals
				 = new List<Tuple<int,SignalType>>();
			graphSystem.DehighlightNodes();

			foreach (ProteinNode prot in Nodes)
			{
				if (prot.Signal != SignalType.None)
				{
					SignalType signal = prot.Signal;

					var inInteractions = GetIncomingInteractions(prot.Name);
					foreach (var tuple in inInteractions)
					{
						var inInteraction = tuple.Item1;
						ProteinNode prevProt = GetProtein(inInteraction.End1);
						if (inInteraction.Type == "b" &&
							(prevProt.Signal == SignalType.Plus || prevProt.Signal == SignalType.Minus))
						{
							decouple(prot.Name, prevProt.Name, graphSystem);
						}

					}

					// if this is not the end of chain, transmit signal further:
					if (prot.Signal != SignalType.End)
					{
						var outInteractions = GetOutcomingInteractions(prot.Name);
						foreach (var tuple in outInteractions)
						{
							var outInteraction = tuple.Item1;
							ProteinNode nextProt = GetProtein(outInteraction.End2);

							if (outInteraction.Type == "-")
							{
								signal = ProteinNode.FlipSignal(signal);
							}

							if (outInteraction.Type == "b")
							{
								couple(prot.Name, nextProt.Name, graphSystem);
							}

							if (signal == SignalType.Plus)
							{
								graphSystem.AddSpark(outInteraction.End1, outInteraction.End2, time, Color.Green);
							}
							else if (signal == SignalType.Minus)
							{
								graphSystem.AddSpark(outInteraction.End1, outInteraction.End2, time, Color.Red);
							}

							// if next protein is not a destination:
							if (nextProt.Signal != SignalType.End)
							{
								updatedSignals.Add(new Tuple<int, SignalType>(outInteraction.End2, signal));
							}
						}
					}
				}
				if (prot.Signal == SignalType.End)
				{
					graphSystem.HighlightNodes(GetIdByName(prot.Name), Color.White);
				}
				else
				{
					prot.Signal = SignalType.None;
				}
			}
			graphSystem.RefreshSparks();
			List<int> highlightNodesPos = new List<int>();
			List<int> highlightNodesNeg = new List<int>();

			// update signals in nodes:
			foreach (var tuple in updatedSignals)
			{
				GetProtein(tuple.Item1).Signal = tuple.Item2;
				if (tuple.Item2 == SignalType.Plus)
				{
					highlightNodesPos.Add(tuple.Item1);
				}
				else if (tuple.Item2 == SignalType.Minus)
				{
					highlightNodesNeg.Add(tuple.Item1);
				}

				if (tuple.Item2 == SignalType.Plus)
				{
					GetProtein(tuple.Item1).Activate();
				}
				else if (tuple.Item2 == SignalType.Minus)
				{
					GetProtein(tuple.Item1).Deactivate();
				}
			}

			
			graphSystem.HighlightNodes(highlightNodesPos, HighlightNodesColorPos);
			graphSystem.HighlightNodes(highlightNodesNeg, HighlightNodesColorNeg);
		}


		public void ResetSignals()
		{
			foreach (ProteinNode prot in Nodes)
			{
				prot.Signal = SignalType.None;
			}
		}



		void couple(string name1, string name2, GraphSystem graphSystem)
		{
			changeEdgeValue(name1, name2, 5.0f, graphSystem);
		}


		void decouple(string name1, string name2, GraphSystem graphSystem)
		{
			changeEdgeValue(name1, name2, 0.5f, graphSystem);
		}


		void changeEdgeValue(string name1, string name2, float value, GraphSystem graphSystem)
		{

			int id1 = GetIdByName(name1);
			int id2 = GetIdByName(name2);
			List<Tuple<ProteinInteraction, int>> interactions =	GetInteractions(name1, name2);

			var graph = graphSystem.GetGraph();
			foreach (var inter in interactions)
			{
				int index = inter.Item2;
				var interaction = inter.Item1;
				graph.Edges[index].Value = value;
			}
			graphSystem.UpdateGraph(graph);
		}
	}
}
