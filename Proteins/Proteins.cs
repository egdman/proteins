using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion;
using Fusion.Mathematics;
using Fusion.Graphics;
using Fusion.Audio;
using Fusion.Input;
using Fusion.Content;
using Fusion.Development;
using GraphVis;

namespace Proteins
{
	public class Proteins : Game
	{

		int selectedNodeIndex;
		bool nodeSelected;
		int[] membrane		= { 0, 1, 2, 20 };
		int[] cytoplasm		= { 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 };
		int[] nucleus		= { 8, 12, 13, 14, 15, 16, 17, 18, 19 };
		int[] jumpers		= { 8, 12 };
		int[] outerNodes	= { 0, 1, 2 };

//		List<int> hotNodes;
		List<int> coldNodes;
		ProteinGraph protGraph;

		Random rnd = new Random();

		int timer;
		int timer2;

		int delay;
		int delay2;

		bool YAPNucleus;
		bool bCATNucleus;
		/// <summary>
		/// Proteins constructor
		/// </summary>
		public Proteins()
			: base()
		{
			//	enable object tracking :
			Parameters.TrackObjects = true;

			//	uncomment to enable debug graphics device:
			//	(MS Platform SDK must be installed)
			//	Parameters.UseDebugDevice	=	true;

			//	add services :
			AddService(new SpriteBatch(this), false, false, 0, 0);
			AddService(new DebugStrings(this), true, true, 9999, 9999);
			AddService(new DebugRender(this), true, true, 9998, 9998);
			AddService(new GraphSystem(this), true, true, 9997, 9997);
			AddService(new GreatCircleCamera(this), true, true, 9996, 9996);

			//	add here additional services :

			//	load configuration for each service :
			LoadConfiguration();

			//	make configuration saved on exit :
			Exiting += Game_Exiting;


			nodeSelected = false;
			selectedNodeIndex = 0;
//			hotNodes	= new List<int>();
			coldNodes	= new List<int>();
			protGraph	= new ProteinGraph();

			timer = 0;
			timer2 = 0;

			delay = 500;
			delay2 = 5000;

			YAPNucleus = true;
			bCATNucleus = true;

		}


		/// <summary>
		/// Initializes game :
		/// </summary>
		protected override void Initialize()
		{
			//	initialize services :
			base.Initialize();

			//	add keyboard handler :
			InputDevice.KeyDown += InputDevice_KeyDown;

			//	load content & create graphics and audio resources here:

			var gs = GetService<GraphSystem>();
			gs.BackgroundColor = Color.Black;
			gs.BlendMode = BlendState.Additive;

			protGraph.ReadFromFile("../../../../signalling_table.csv");
			protGraph.GetProtein("bCAT").Deactivate();
		}



		/// <summary>
		/// Disposes game
		/// </summary>
		/// <param name="disposing"></param>
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				//	dispose disposable stuff here
				//	Do NOT dispose objects loaded using ContentManager.
			}
			base.Dispose(disposing);
		}



		/// <summary>
		/// Handle keys
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void InputDevice_KeyDown(object sender, Fusion.Input.InputDevice.KeyEventArgs e)
		{
			if (e.Key == Keys.F1)
			{
				DevCon.Show(this);
			}

			if (e.Key == Keys.F5)
			{
				Reload();
			}

			if (e.Key == Keys.F12)
			{
				GraphicsDevice.Screenshot();
			}

			if (e.Key == Keys.Escape)
			{
				Exit();
			}
			if (e.Key == Keys.X)
			{
				var graphSys = GetService<GraphSystem>();	

				// add categories of nodes with different localization:
				// category 1 (membrane):
				graphSys.AddCategory(membrane, new Vector3(0, 0, 0), 700);

				// category 2 (cytoplasm):
				graphSys.AddCategory(cytoplasm, new Vector3(0, 0, 0), 300);

				// category 3 (nucleus):
				graphSys.AddCategory(nucleus, new Vector3(0, 0, 0), 100);

	//			protGraph.Nodes[
				graphSys.AddGraph(protGraph);
			}
			if (e.Key == Keys.P)
			{
				GetService<GraphSystem>().Pause();
			}

			if (e.Key == Keys.F) // focus on a node
			{
				var cam = GetService<GreatCircleCamera>();
				var pSys = GetService<GraphSystem>();
				if (nodeSelected)
				{
					pSys.Focus(selectedNodeIndex);
				}
			}

			if (e.Key == Keys.LeftButton)
			{
				var pSys = GetService<GraphSystem>();
				Point cursor = InputDevice.MousePosition;
				int selNode = 0;
				if (nodeSelected = pSys.ClickNode(cursor, StereoEye.Mono, 0.025f, out selNode))
				{
					selectedNodeIndex = selNode;
					var adjEdges = protGraph.GetEdges(selNode);
					string protName = ((NodeWithText)protGraph.Nodes[selNode]).Text;
					Console.WriteLine("id = " + selNode +
						" name: " + protName + ":  ");
					foreach (var ae in adjEdges)
					{
						var interaction = protGraph.Edges[ae];
						if (interaction.End1 == selNode)
						{
							Console.WriteLine(protGraph.Edges[ae].GetInfo());
						}
					}
					Console.WriteLine();
				}


			}
			if (e.Key == Keys.H)
			{
				if (bCATNucleus)
				{
					decouple("bCAT", "TCF");
				}
				else
				{
					couple("bCAT", "TCF");
				}
				bCATNucleus = !bCATNucleus;
			}
			if (e.Key == Keys.G)
			{
				if (YAPNucleus)
				{
					decouple("YAP", "TCF");
				}
				else
				{
					couple("YAP", "TCF");
				}
				YAPNucleus = !YAPNucleus;
		
				
	//			Graph graph = grSys.GetGraph();
				

//				pSys.Select(startNode);
//				int edgeIndex = graph.GetEdgeIndex(8, 13);
				

	//			Graph.Edge edge1 = graph.Edges[13];
	//			Graph.Edge edge2 = graph.Edges[8];
	//			float tmp = edge1.Value;
	//			edge1.Value = edge2.Value;
	//			edge2.Value = tmp;

	//			graph.Edges[13] = edge1;
	//			graph.Edges[8] = edge2;
	//			pSys.UpdateGraph(graph);
			}
			if (e.Key == Keys.D2)
			{
				startPropagate("PKCa", SignalType.Plus, "YAP");
			}

			if (e.Key == Keys.D3)
			{
				startPropagate("Ecad", SignalType.Plus, "TCF");
			}
			if (e.Key == Keys.D4)
			{
			}


		}



		/// <summary>
		/// Saves configuration on exit.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void Game_Exiting(object sender, EventArgs e)
		{
			SaveConfiguration();
		}



		/// <summary>
		/// Updates game
		/// </summary>
		/// <param name="gameTime"></param>
		protected override void Update(GameTime gameTime)
		{
			var ds = GetService<DebugStrings>();
			timer	+= gameTime.Elapsed.Milliseconds;
			timer2	+= gameTime.Elapsed.Milliseconds;
			if (timer > delay)
			{
				propagate();
				timer = 0;
			}

			if (timer2 > delay2)
			{
	//			startPropagate("YAP", SignalType.Plus);
				timer2 = 0;
			}

			ds.Add(Color.Orange, "FPS {0}", gameTime.Fps);
			ds.Add("F1   - show developer console");
			ds.Add("F5   - build content and reload textures");
			ds.Add("F12  - make screenshot");
			ds.Add("ESC  - exit");

			base.Update(gameTime);

			//	Update stuff here :
		}



		/// <summary>
		/// Draws game
		/// </summary>
		/// <param name="gameTime"></param>
		/// <param name="stereoEye"></param>
		protected override void Draw(GameTime gameTime, StereoEye stereoEye)
		{
			base.Draw(gameTime, stereoEye);

			//	Draw stuff here :
		}


		void propagate()
		{	
			List<int> selected = new List<int>();
			protGraph.Propagate();
			foreach (ProteinNode prot in protGraph.Nodes)
			{
				if (prot.Signal == SignalType.Plus || prot.Signal == SignalType.Minus)
				{
					selected.Add(protGraph.GetIdByName(prot.Name));
				}
			}
			var grSys = GetService<GraphSystem>();
			if (selected.Count > 0)
			{
				grSys.Select(selected);
			}
		}

		void startPropagate(string startName, SignalType signal, string endName)
		{
			timer = 0;
			var grSys = GetService<GraphSystem>();
			protGraph.ResetSignals();
			grSys.Deselect();
			if (grSys.NodeCount == 0)
			{
				return;
			}

			protGraph.GetProtein(startName).Signal = signal;
			protGraph.GetProtein(endName).Signal = SignalType.End;
			grSys.Select(protGraph.GetIdByName(startName));
		}


		void couple(string name1, string name2)
		{
			changeEdgeValue(name1, name2, 5.0f);
		}


		void decouple(string name1, string name2)
		{
			changeEdgeValue(name1, name2, 0.5f);
		}


		void changeEdgeValue(string name1, string name2, float value)
		{
			var grSys = GetService<GraphSystem>();
			int id1 = protGraph.GetIdByName(name1);
			int id2 = protGraph.GetIdByName(name2);
			List<Tuple<ProteinInteraction, int>> interactions = 
				protGraph.GetInteractions(name1, name2);

			var graph = grSys.GetGraph();
			foreach (var inter in interactions)
			{
				int index = inter.Item2;
				var interaction = inter.Item1;
				if (interaction.Type == "b")
				{
					graph.Edges[index].Value = value;
					
				}
			}
			grSys.UpdateGraph(graph);
		}
	}
}
