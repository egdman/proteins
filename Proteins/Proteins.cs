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
		int[] cytoplasma	= { 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 };
		int[] nucleus		= { 8, 12, 13, 14, 15, 16, 17, 18, 19 };
		int[] jumpers		= { 8, 12 };
		int[] outerNodes	= { 0, 1, 2 };

		List<int> hotNodes;
		List<int> coldNodes;
		ProteinGraph protGraph;

		Random rnd = new Random();

		int timer;
		int delay;

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
			hotNodes	= new List<int>();
			coldNodes	= new List<int>();
			protGraph	= new ProteinGraph();
			protGraph.ReadFromFile("D:/proteins/signalling_table.csv");

			timer = 0;
			delay = 500;

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
//				graphSys.AddCategory(new List<int> { 0, 1, 2, 20 }, new Vector3(2000, 0, 0), 10);

				// category 2 (cytoplasma):
				graphSys.AddCategory(cytoplasma, new Vector3(0, 0, 0), 300);
//				graphSys.AddCategory(new List<int> { 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 }, new Vector3(0, 0, 0), 10);

				// category 3 (nucleus):
				graphSys.AddCategory(nucleus, new Vector3(0, 0, 0), 100);
//				graphSys.AddCategory(new List<int> { 8, 12, 13, 14, 15, 16, 17, 18, 19 }, new Vector3(1000, 1000, 0), 10);

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
	//				pSys.Select(selectedNodeIndex);
					var adjNodes = protGraph.GetAdjacentNodes(selNode);
					Console.Write("id = " + selNode + ":  ");
					foreach (var an in adjNodes)
					{
						Console.Write(an + ", ");
					}
					Console.WriteLine();
				}


			}
			if (e.Key == Keys.G)
			{
				int startNode = outerNodes[rnd.Next(outerNodes.Length)];

				var grSys = GetService<GraphSystem>();
	//			Graph graph = grSys.GetGraph();
				
				hotNodes.Add(startNode);
				grSys.Select(startNode);
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
//			if (e.Key == Keys.R)
//			{
//				propagate();
//			}


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
			timer += gameTime.Elapsed.Milliseconds;

			if (timer > delay)
			{
				propagate();
				timer = 0;
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
			if (hotNodes.Count > 0)
			{
				List<int> newlyExcitedNodes = new List<int>();
				var grSys = GetService<GraphSystem>();
				foreach (int hn in hotNodes)
				{
					coldNodes.Add(hn);
					var adjNodes = protGraph.GetAdjacentNodes(hn);
					foreach (var an in adjNodes)
					{
						if (!hotNodes.Contains(an) && !coldNodes.Contains(an))
						{
							newlyExcitedNodes.Add(an);
						}
					}
				}
				hotNodes.Clear();
				hotNodes = newlyExcitedNodes;
				if (hotNodes.Count > 0)
				{
					grSys.Select(hotNodes);
				}
				else
				{
					coldNodes.Clear();
				}
			}
		}
	}
}
