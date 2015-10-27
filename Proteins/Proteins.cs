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


		Color nodeHighlightColorPos;
		Color nodeHighlightColorNeg;

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


			nodeHighlightColorNeg = Color.Red;
			nodeHighlightColorPos = Color.Green;

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
			protGraph.HighlightNodesColorPos = nodeHighlightColorPos;
			protGraph.HighlightNodesColorNeg = nodeHighlightColorNeg;

			var graphSys = GetService<GraphSystem>();

			// add categories of nodes with different localization:
			// category 1 (membrane):
			graphSys.AddCategory(membrane, new Vector3(0, 0, 0), 700);

			// category 2 (cytoplasm):
			graphSys.AddCategory(cytoplasm, new Vector3(0, 0, 0), 300);

			// category 3 (nucleus):
			graphSys.AddCategory(nucleus, new Vector3(0, 0, 0), 100);

			graphSys.AddGraph(protGraph);
			graphSys.Unpause();
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
				
			}
			if (e.Key == Keys.P) // pause/unpause
			{
				var gs = GetService<GraphSystem>();
				if (gs.Paused) gs.Unpause();
				else gs.Pause();
			}

			if (e.Key == Keys.F) // focus on a node
			{
				var cam = GetService<GreatCircleCamera>();
				var pSys = GetService<GraphSystem>();
				if (nodeSelected)
				{
					pSys.Focus(selectedNodeIndex, cam);
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
					var protein		= protGraph.GetProtein(selectedNodeIndex);
					var inEdges		= protGraph.GetIncomingInteractions(protein.Name);
					var outEdges	= protGraph.GetOutcomingInteractions(protein.Name);

					string protName = ((NodeWithText)protGraph.Nodes[selNode]).Text;
					Console.WriteLine("id = " + selNode +
						" name: " + protein.Name + ":  ");
					Console.WriteLine("incoming:");
					foreach (var ie in inEdges)
					{
						Console.WriteLine(ie.Item1.GetInfo());
					}

					Console.WriteLine("outcoming:");
					foreach (var oe in outEdges)
					{
						Console.WriteLine(oe.Item1.GetInfo());
					}
					Console.WriteLine();
				}
			}
			if (e.Key == Keys.R)
			{
				protGraph.ResetSignals();
			}
			if (e.Key == Keys.Q)
			{
				Graph graph = GetService<GraphSystem>().GetGraph();
				graph.WriteToFile("../../../../graph.gr");
				Log.Message("Graph saved to file");
			}
			if (e.Key == Keys.D1)
			{
				protGraph.ResetSignals();
				startPropagate("PKCa", SignalType.Plus, "TCF");
			}
			if (e.Key == Keys.D2)
			{
				protGraph.ResetSignals();
				startPropagate("PKCa", SignalType.Plus, "YAP");
			}

			if (e.Key == Keys.D3)
			{
				protGraph.ResetSignals();
				startPropagate("Ecad", SignalType.Plus, "YAP");
			}
			if (e.Key == Keys.D4)
			{
				protGraph.ResetSignals();
				startPropagate("PKCa", SignalType.Plus, "YAP");
				startPropagate("Ecad", SignalType.Plus, "YAP");
			}
			if (e.Key == Keys.D5)
			{
				protGraph.ResetSignals();
				startPropagate("FZD", SignalType.Plus, "bCAT");
			}
			if (e.Key == Keys.D6)
			{
				var gs = GetService<GraphSystem>();
				gs.AddSpark(
					protGraph.GetIdByName("YAP"),
					protGraph.GetIdByName("TCF"),
					1.0f,
					Color.Red
					);
				gs.RefreshSparks();
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
			var grSys = GetService<GraphSystem>();
			protGraph.Propagate(grSys, delay);
		}

		void startPropagate(string startName, SignalType signal, string endName)
		{
			timer = 0;
			var grSys = GetService<GraphSystem>();
			grSys.DehighlightNodes();
			if (grSys.NodeCount == 0)
			{
				return;
			}

			protGraph.GetProtein(startName).Signal = signal;
			protGraph.GetProtein(endName).Signal = SignalType.End;
			grSys.HighlightNodes(protGraph.GetIdByName(startName), nodeHighlightColorPos);
		}


//		void startPropagate(List<string> startNames, SignalType )

		
	}
}
