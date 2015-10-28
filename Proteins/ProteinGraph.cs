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


		float coupleStrength;
		float decoupleStrength;

		public Color HighlightNodesColorPos { get; set; }
		public Color HighlightNodesColorNeg { get; set; }


		List<int> inputs;

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
			inputs = new List<int>();
			coupleStrength = 5.0f;
			decoupleStrength = 0.5f;
		}



		public void AddInput(string Name)
		{
			inputs.Add(GetIdByName(Name));
		}

		public void ClearInputs()
		{
			inputs.Clear();
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

//					Color color = new Color((float)cat1, (float)cat2, (float)cat3);
					Color color = Color.White;
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
				strength = decoupleStrength;
			}
			else if (interType[0] == '-')
			{
				strength = decoupleStrength;
			}
			else if (interType[0] == 'b')
			{
				strength = decoupleStrength;
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
			

			List<int> activateNodes = new List<int>();
			List<int> blockNodes = new List<int>();

			// activate input proteins:
			foreach (int i in inputs)
			{
				GetProtein(i).Activate();
			}



			// coupling / decoupling: -------------------------------------
			foreach (ProteinInteraction e in Edges)
			{
				if (e.Type == "b")
				{
					if (!GetProtein(e.End1).Active && !GetProtein(e.End2).Active)
					{
						if (e.Value > decoupleStrength)
						{
							decouple(GetProtein(e.End1).Name, GetProtein(e.End2).Name, graphSystem);
						}
					}
				}
			}
			// ------------------------------------------------------------





			highlight(graphSystem);
			foreach (ProteinNode prot in Nodes)
			{

				if (prot.Active)
				{
					var outInteractions = GetOutcomingInteractions(prot.Name);
					foreach (var tuple in outInteractions)
					{
						var outInteraction = tuple.Item1;
						ProteinNode nextProt = GetProtein(outInteraction.End2);
						if (outInteraction.Type == "-")
						{
							graphSystem.AddSpark(outInteraction.End1, outInteraction.End2, time, HighlightNodesColorNeg);
							blockNodes.Add(outInteraction.End2);
						}
						else if (outInteraction.Type == "+")
						{
							graphSystem.AddSpark(outInteraction.End1, outInteraction.End2, time, HighlightNodesColorPos);
							activateNodes.Add(outInteraction.End2);
						}
						else if (outInteraction.Type == "b")
						{
							if (outInteraction.Value > decoupleStrength)
							{
								decouple(prot.Name, nextProt.Name, graphSystem);
							}
							else
							{
								couple(prot.Name, nextProt.Name, graphSystem);
							}
						}
					}
				}
			}


			graphSystem.RefreshSparks();
			DeactivateAll();
			UnblockAll();
			foreach (var i in activateNodes)
			{
				GetProtein(i).Activate();
			}
			foreach (var i in blockNodes)
			{
				GetProtein(i).Deactivate();
				GetProtein(i).Blocked = true;
			}
		}



		void highlight(GraphSystem graphSystem)
		{
			List<int> highPlus = new List<int>();
			List<int> highMinus = new List<int>();
			foreach (ProteinNode node in Nodes)
			{
				if (node.Active) highPlus.Add(GetIdByName(node.Name));
				else if (node.Blocked) highMinus.Add(GetIdByName(node.Name));
			}
			graphSystem.HighlightNodes(highPlus, HighlightNodesColorPos);
			graphSystem.HighlightNodes(highMinus, HighlightNodesColorNeg);
		}


		public void DeactivateAll()
		{
			foreach (ProteinNode prot in Nodes)
			{
				prot.Deactivate();
			}
		}


		public void UnblockAll()
		{
			foreach (ProteinNode prot in Nodes)
			{
				prot.Blocked = false;
			}
		}
		
		void couple(string name1, string name2, GraphSystem graphSystem)
		{
			changeEdgeValue(name1, name2, coupleStrength, graphSystem);
		}


		void decouple(string name1, string name2, GraphSystem graphSystem)
		{
			changeEdgeValue(name1, name2, decoupleStrength, graphSystem);
		}


		void changeEdgeValue(string name1, string name2, float value, GraphSystem graphSystem)
		{
			List<Tuple<ProteinInteraction, int>> interactions =	GetInteractions(name1, name2);

			var graph = graphSystem.GetGraph();
			foreach (var inter in interactions)
			{
				int index = inter.Item2;
				var interaction = inter.Item1;
				graph.Edges[index].Value = value;
				this.Edges[index].Value = value;
			}
			graphSystem.UpdateGraph(graph);
		}

		void decoupleEverything(GraphSystem graphSystem)
		{
			var graph = graphSystem.GetGraph();
			foreach (var e in graph.Edges)
			{
				e.Value = decoupleStrength;
			}
			graphSystem.UpdateGraph(graph);
		}
	}
}
