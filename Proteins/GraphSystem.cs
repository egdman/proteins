using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.IO;

using Fusion;
using Fusion.Graphics;
using Fusion.Mathematics;
using Fusion.Input;

namespace GraphVis {

	[StructLayout(LayoutKind.Explicit, Size=32)]
	public struct Spark
	{
		[FieldOffset(0)]
		int start;

		[FieldOffset(4)]
		int end;

		[FieldOffset(8)]
		float time;

		[FieldOffset(12)]
		float parameter;

		[FieldOffset(16)]
		public Vector4 Color;

		public Spark(Texture2D Texture, int From, int To, float Time, Color color)
		{
			start = From;
			end = To;
			time = Time;
			parameter = 0.0f;
			Color = color.ToVector4();
		}

		public float Parameter
		{
			get
			{
				return parameter;
			}
			set
			{
				if (value > 1.0f) parameter = 1.0f;
				else if (value < 0.0f) parameter = 0.0f;
				else parameter = value;
			}
		}

		public float Time
		{
			get
			{
				return time;
			}
		}

	}


	struct HighlightParams
	{
		public Color color;
		public int number;
	}


	public class ParticleConfig
	{
		[Category("General")]
		public int IterationsPerFrame	{ get; set; }
		
		[Category("General")]
		public float StepSize			{ get; set; }
		

		[Category("Physics")]
		public float RepulsionForce		{ get; set; }
		[Category("Physics")]
		public float SpringTension		{ get; set; }


		[Category("Visuals")]
		public float EdgeOpacity
		{
			get { return linkOpacity; }
			set
			{
				if (value < 0) { linkOpacity = 0; }
				else if (value > 1) { linkOpacity = 1; }
				else { linkOpacity = value; }
			}
		}
		[Category("Visuals")]
		public float NodeScale
		{
			get { return nodeScale; }
			set
			{
				if (value < 0) { nodeScale = 0; }
				else { nodeScale = value; }
			}
		}

		[Category("Advanced")]
		public int SearchIterations { get; set; }
		[Category("Advanced")]
		public int SwitchToManualAfter { get; set; }
		[Category("Advanced")]
		public bool UseGPU { get; set; }
		[Category("Advanced")]
		public LayoutSystem.StepMethod StepMode { get; set; }
		[Category("Advanced")]
		public float C1 { get; set; }
		[Category("Advanced")]
		public float C2 { get; set; }

		float linkOpacity;
		float nodeScale;

		public ParticleConfig()
		{
			// General:
			IterationsPerFrame	= 20;
			StepSize			= 0.02f;
			
			// Visuals:
			EdgeOpacity			= 0.1f;
			nodeScale			= 1.0f;

			// Physics constants:
			RepulsionForce	= 1.0f;
			SpringTension	= 0.1f;

			// Advanced defaults:
			StepMode = LayoutSystem.StepMethod.Fixed;
			SwitchToManualAfter	= 250;
			SearchIterations	= 1;	
			C1 = 0.1f;
			C2 = 0.9f;
			UseGPU		= true;	
			
		}

	}

	public class GraphSystem : GameService {

		[Config]
		public ParticleConfig Config{ get; set; }
//		public const float WorldSize = 50.0f;

		Texture2D		particleTex;
		Texture2D		highlightTex;
		Texture2D		sparkTex;
		TextureAtlas	atlas;

		Ubershader		renderShader;
		Ubershader		computeShader;
		StateFactory	factory;

		float		particleMass;
		float		edgeSize;

		List<Tuple<StructuredBuffer, HighlightParams>> highlightNodesList;
		List<Tuple<StructuredBuffer, HighlightParams>> highlightEdgesList;

		StructuredBuffer	sparkBuffer;

		ConstantBuffer		paramsCB;

		List<List<int> >	edgeIndexLists;
		List<Link>			edgeList;
		List<Particle3d>	nodeList;

		List<Spark>			sparkList;

		Queue<int>			commandQueue;
		Random				rand = new Random();

		LayoutSystem		lay;

		int		referenceNodeIndex;


		public Color BackgroundColor
		{
			get;
			set;
		}


		public Vector4 HighlightNodeColor
		{
			get;
			set;
		}


		public Vector4 HighlightEdgeColor
		{
			get;
			set;
		}

		public BlendState BlendMode
		{
			get;
			set;
		}

		public bool AnchorToNodes
		{
			get;
			set;
		}

		public bool Paused
		{
			get
			{
				return lay.Paused;
			}
		}

		enum RenderFlags {
			DRAW			= 0x1,
			POINT			= 0x1 << 1,
			LINE			= 0x1 << 2,
			SELECTION		= 0x1 << 3,
			HIGH_LINE		= 0x1 << 4,

			ABSOLUTE_POS	= 0x1 << 5,
			RELATIVE_POS	= 0x1 << 6,
			SPARKS			= 0x1 << 7,
		}


		[StructLayout(LayoutKind.Explicit)]
		struct Params {
			[FieldOffset(  0)] public Matrix	View;
			[FieldOffset( 64)] public Matrix	Projection;
			[FieldOffset(128)] public int		MaxParticles;
			[FieldOffset(132)] public int		SelectedNode;
			[FieldOffset(136)] public float		edgeOpacity;
			[FieldOffset(140)] public float		nodeScale;
			[FieldOffset(144)] public Vector4	highNodeColor;
			[FieldOffset(160)] public Vector4	highEdgeColor;
		} 

		/// <summary>
		/// 
		/// </summary>
		/// <param name="game"></param>
		public GraphSystem ( Game game ) : base (game)
		{
			Config = new ParticleConfig();
			HighlightNodeColor	= new Vector4(0, 1, 0, 1);
			HighlightEdgeColor	= new Vector4(0, 1, 0, 1);
			BackgroundColor		= Color.White;
			BlendMode			= BlendState.Additive;
			AnchorToNodes		= false;

			highlightNodesList = new List<Tuple<StructuredBuffer, HighlightParams>>();
			highlightEdgesList = new List<Tuple<StructuredBuffer, HighlightParams>>();
		}

		public int NodeCount { get { return nodeList.Count; } }
		public int EdgeCount { get { return edgeList.Count; } }



		
		/// <summary>
		/// 
		/// </summary>
		public override void Initialize ()
		{
			particleTex		=	Game.Content.Load<Texture2D>("smaller");
			highlightTex	=	Game.Content.Load<Texture2D>("selection");
			sparkTex		=	Game.Content.Load<Texture2D>("spark");
			renderShader	=	Game.Content.Load<Ubershader>("Render");
			computeShader	=	Game.Content.Load<Ubershader>("Compute");

			atlas			=	Game.Content.Load<TextureAtlas>("protein_textures");
			
			// creating the layout system:
			lay = new LayoutSystem(Game, computeShader);
			lay.UseGPU = Config.UseGPU;
			lay.RunPause = LayoutSystem.State.PAUSE;

			factory = new StateFactory( renderShader, typeof(RenderFlags), ( plState, comb ) => 
			{
				plState.RasterizerState	= RasterizerState.CullNone;
				plState.BlendState = BlendMode;
				plState.DepthStencilState = DepthStencilState.Readonly;
				plState.Primitive		= Primitive.PointList;
			} );

			paramsCB			=	new ConstantBuffer( Game.GraphicsDevice, typeof(Params) );
			particleMass		=	1.0f;
			edgeSize			=	1000.0f;

			edgeList			=	new List<Link>();
			nodeList			=	new List<Particle3d>();
			edgeIndexLists		=	new List<List<int>>();
			sparkList			=	new List<Spark>();
			commandQueue		=	new Queue<int>();

			referenceNodeIndex	=	0;

			Game.InputDevice.KeyDown += keyboardHandler;

			base.Initialize();
		}


		public void Pause()
		{
			lay.Pause();
		}

		public void Unpause()
		{
			lay.Unpause();
		}


		void keyboardHandler(object sender, Fusion.Input.InputDevice.KeyEventArgs e)
		{
			if ( e.Key == Keys.OemPlus )
			{
				commandQueue.Enqueue(1);
			}
			if ( e.Key == Keys.OemMinus )
			{
				commandQueue.Enqueue(-1);
			}
		}


		Vector2 PixelsToProj(Point point)
		{
			Vector2 proj = new Vector2(
				(float)point.X / (float)Game.GraphicsDevice.DisplayBounds.Width,
				(float)point.Y / (float)Game.GraphicsDevice.DisplayBounds.Height
			);
			proj.X = proj.X * 2 - 1;
			proj.Y = -proj.Y * 2 + 1;
			return proj;
		}



		public bool ClickNode(Point cursor, StereoEye eye, float threshold, out int nodeIndex )
		{
			nodeIndex = 0;

			var cam = Game.GetService<GreatCircleCamera>();
			var viewMatrix = cam.GetViewMatrix( eye );
			var projMatrix = cam.GetProjectionMatrix( eye );
			Graph graph = this.GetGraph();

			Vector2 cursorProj = PixelsToProj(cursor);
			bool nearestFound = false;
			
			float minZ = 99999;
			int currentIndex = 0;
			foreach (SpatialNode node in graph.Nodes)
			{
				Vector4 posWorld = new Vector4(node.Position, 1.0f);
				if (AnchorToNodes)
				{
					posWorld -= new Vector4(((SpatialNode)graph.Nodes[referenceNodeIndex]).Position, 1.0f);
				}
				Vector4 posView = Vector4.Transform(posWorld, viewMatrix);
				Vector4 posProj = Vector4.Transform(posView, projMatrix);
				posProj /= posProj.W;
				Vector2 diff = new Vector2(posProj.X - cursorProj.X, posProj.Y - cursorProj.Y);
				if (diff.Length() < threshold)
				{
					nearestFound = true;
					if (minZ > posProj.Z)
					{
						minZ = posProj.Z;
						nodeIndex = currentIndex;
					}
				}
				++currentIndex;
			}
			return nearestFound;
		}


		public List<int> DragSelect(Point topLeft, Point bottomRight, StereoEye eye )
		{
			List<int> selectedIndices = new List<int>();
			Vector2 topLeftProj		= PixelsToProj(topLeft);
			Vector2 bottomRightProj	= PixelsToProj(bottomRight);

			var cam = Game.GetService<GreatCircleCamera>();
			var viewMatrix = cam.GetViewMatrix(eye);
			var projMatrix = cam.GetProjectionMatrix(eye);

			Graph graph = this.GetGraph();
			int currentIndex = 0;
			foreach (SpatialNode node in graph.Nodes)
			{
				Vector4 posWorld = new Vector4(node.Position, 1.0f);
				Vector4 posView = Vector4.Transform(posWorld, viewMatrix);
				Vector4 posProj = Vector4.Transform(posView, projMatrix);
				posProj /= posProj.W;
				if
				(	posProj.X >= topLeftProj.X && posProj.X <= bottomRightProj.X &&
					posProj.Y >= bottomRightProj.Y && posProj.Y <= topLeftProj.Y
				)
				{
					selectedIndices.Add(currentIndex);
				}
				++currentIndex;
			}
			return selectedIndices;
		}


		public void AddSpark(int From, int To, float Time, Color color)
		{
			sparkList.Add(new Spark(sparkTex, From, To, Time, color));
		}


		public void RefreshSparks()
		{
			if (sparkBuffer != null)
			{
				sparkBuffer.Dispose();
				sparkBuffer = null;
			}
			if (sparkList.Count > 0)
			{
				sparkBuffer = new StructuredBuffer(
					Game.GraphicsDevice,
					typeof(Spark),
					sparkList.Count,
					StructuredBufferFlags.Counter
					);
				sparkBuffer.SetData(sparkList.ToArray());
			}
		}



		public void RemoveAllSparks()
		{
			sparkList.Clear();
			if (sparkBuffer != null)
			{
				sparkBuffer.Dispose();
				sparkBuffer = null;
			}
		}


		public void PaintEdges(IEnumerable<int> edgeIndices, Color color)
		{
			Link[] links = new Link[lay.LinkCount];
			lay.LinksBuffer.GetData(links);
			foreach (int index in edgeIndices)
			{
				links[index].color = color.ToVector4();
			}
			lay.LinksBuffer.SetData(links);
		}


		public void PaintAllEdges(Color color)
		{
			Link[] links = new Link[lay.LinkCount];
			lay.LinksBuffer.GetData(links);
			for ( int i = 0; i < lay.LinkCount; ++i )
			{
				links[i].color = color.ToVector4();
			}
			lay.LinksBuffer.SetData(links);
		}


		public void HighlightNodes(int nodeIndex, Color color)
		{
			HighlightNodes(new int[1] { nodeIndex }, color);
		}


		public void HighlightNodes(ICollection<int> nodeIndices, Color color)
		{
			if (!(nodeIndices.Count > 0))
			{
				return;
			}
			StructuredBuffer buf = new StructuredBuffer(
				Game.GraphicsDevice,
				typeof(int),
				nodeIndices.Count,
				StructuredBufferFlags.Counter
			);

			HighlightParams hParam = new HighlightParams{ color = color, number = nodeIndices.Count };
			buf.SetData(nodeIndices.ToArray());
			highlightNodesList.Add( new Tuple<StructuredBuffer,HighlightParams>
				(buf, hParam)
			);	
		}


		public void DehighlightNodes()
		{
			for (int i = 0; i < highlightNodesList.Count; ++i)
			{
				var buf =  highlightNodesList[i];
				if (buf.Item1 != null)
				{
					buf.Item1.Dispose();
				}
			}
			highlightNodesList.Clear();
		}


		public void Focus(int nodeIndex, GreatCircleCamera camera)
		{
			referenceNodeIndex = nodeIndex;
			if (!AnchorToNodes)
			{
				var graph = GetGraph();
				camera.CenterOfOrbit = ((SpatialNode)graph.Nodes[nodeIndex]).Position;
			}
		}




		/// <summary>
		/// Returns random radial vector
		/// </summary>
		/// <returns></returns>
		Vector3 RadialRandomVector ()
		{
			Vector3 r;
			do {
				r	=	rand.NextVector3( -Vector3.One, Vector3.One );
			} while ( r.Length() > 1 );
			r.Normalize();
			return r;
		}


		public void AddGraph(Graph graph)
		{
			lay.ResetState();
			nodeList.Clear();
			edgeList.Clear();
			edgeIndexLists.Clear();
			referenceNodeIndex = 0;
			setBuffers( graph );
		}


		public void UpdateGraph(Graph graph)
		{
			nodeList.Clear();
			edgeList.Clear();
			edgeIndexLists.Clear();
			setBuffers(graph);
		}


		void addParticle( Vector3 pos, float size, Vector4 color, float colorBoost = 1 )
		{
			nodeList.Add( new Particle3d {
					Position		=	pos,
					Velocity		=	Vector3.Zero,
					Color			=	color * colorBoost,
					Size			=	size,
					Force			=	Vector3.Zero,
					Mass			=	particleMass,
					Charge			=	Config.RepulsionForce
				}
			);
			edgeIndexLists.Add( new List<int>() );
		}




		void addEdge( int end1, int end2, float length, float strength, Color color )
		{
			int edgeNumber = edgeList.Count;
			edgeList.Add( new Link{
					par1 = (uint)end1,
					par2 = (uint)end2,
					length = length,
					strength = strength,
					color = color.ToVector4(),
				}
			);
			edgeIndexLists[end1].Add(edgeNumber);

			edgeIndexLists[end2].Add(edgeNumber);
		}



		public Graph GetGraph()
		{
			if (lay.CurrentStateBuffer != null)
			{
				Particle3d[] particleArray = new Particle3d[lay.ParticleCount];
				lay.CurrentStateBuffer.GetData(particleArray);
				Graph graph = new Graph();
				foreach (var p in particleArray)
				{
					graph.AddNode(new SpatialNode(p.Position, p.Size, new Color(p.Color)));
				}
				foreach (var l in edgeList)
				{
					graph.AddEdge(new Edge((int)l.par1, (int)l.par2, l.length, l.strength));
				}
				return graph;
			}
			return new Graph();
		}



		void addNode(float size, Color color)
		{
			var zeroV = new Vector3(0, 0, 0);
			addParticle(
					zeroV + RadialRandomVector() * edgeSize,
					size, color.ToVector4(), 1.0f );
		}


		void addNode(float size, Color color, Vector3 position)
		{
			addParticle( position, size, color.ToVector4(), 1.0f );
		}



		void setBuffers(Graph graph)
		{
			foreach (BaseNode n in graph.Nodes)
			{
				if (n is SpatialNode)
				{
					addNode(n.GetSize(), n.GetColor(), ((SpatialNode)n).Position);
				}
				else
				{
					addNode(n.GetSize(), n.GetColor());
				}
			}
			foreach (var e in graph.Edges)
			{
				addEdge(e.End1, e.End2, e.Length, e.Value, new Color(1.0f, 1.0f, 1.0f, 1.0f));
			}
			setBuffers();
		}


		public void AddCategory(ICollection<int> indices, Vector3 Location, float Radius)
		{
			lay.AddCategory(indices, Location, Radius);
		}



		void setBuffers()
		{				
			lay.SetData(nodeList, edgeList, edgeIndexLists);
		}

	
		/// <summary>
		/// 
		/// </summary>
		/// <param name="disposing"></param>
		protected override void Dispose ( bool disposing )
		{
			if (disposing) {
				paramsCB.Dispose();
				disposeOfBuffers();
				if ( factory != null ) {
					factory.Dispose();
				}	
				if ( particleTex != null ) {
					particleTex.Dispose();
				}
				if (renderShader != null)
				{
					renderShader.Dispose();
				}
				if (computeShader != null)
				{
					computeShader.Dispose();
				}
				if (lay != null)
				{
					lay.Dispose();
				}
			}		
			base.Dispose( disposing );
		}

		void disposeOfBuffers()
		{
			DehighlightNodes();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="gameTime"></param>
		public override void Update ( GameTime gameTime )
		{
			base.Update( gameTime );
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="gameTime"></param>
		/// <param name="stereoEye"></param>
		public override void Draw ( GameTime gameTime, Fusion.Graphics.StereoEye stereoEye )
		{
			var device	=	Game.GraphicsDevice;
			var cam = Game.GetService<GreatCircleCamera>();
			
			int lastCommand = 0;
			if ( commandQueue.Count > 0 )
			{
				lastCommand = commandQueue.Dequeue();
			}

			// Calculate positions: ----------------------------------------------------
			lay.UseGPU			= Config.UseGPU;
			lay.SpringTension	= Config.SpringTension;
			lay.StepMode		= Config.StepMode;
			lay.Update(lastCommand);

			// Render: -----------------------------------------------------------------
			Params param = new Params();

			param.View			= cam.GetViewMatrix(stereoEye);
			param.Projection	= cam.GetProjectionMatrix(stereoEye);
			param.SelectedNode	= referenceNodeIndex;

			render( device, lay, param, gameTime );
			
			// Debug output: ------------------------------------------------------------
//			var debStr = Game.GetService<DebugStrings>();
//			debStr.Add( Color.Yellow, "drawing " + nodeList.Count + " points" );
//			debStr.Add( Color.Yellow, "drawing " + edgeList.Count + " lines" );
//			debStr.Add( Color.Black, lay.UseGPU ? "Using GPU" : "Not using GPU" );
			base.Draw( gameTime, stereoEye );
		}


		void render(GraphicsDevice device, LayoutSystem ls, Params parameters, GameTime gameTime)
		{
			parameters.MaxParticles	= lay.ParticleCount;
			parameters.edgeOpacity	= Config.EdgeOpacity;
			parameters.nodeScale	= Config.NodeScale;
			parameters.highNodeColor = HighlightNodeColor;
			parameters.highEdgeColor = HighlightEdgeColor;

			device.ResetStates();
			device.ClearBackbuffer( BackgroundColor );
			device.SetTargets( null, device.BackbufferColor );
			paramsCB.SetData(parameters);

			device.ComputeShaderConstants	[0] = paramsCB;
			device.VertexShaderConstants	[0] = paramsCB;
			device.GeometryShaderConstants	[0] = paramsCB;
			device.PixelShaderConstants		[0] = paramsCB;

			device.PixelShaderSamplers		[0] = SamplerState.LinearWrap;

			int anchorFlag = (AnchorToNodes ? (int)RenderFlags.RELATIVE_POS : (int)RenderFlags.ABSOLUTE_POS);
			
			// draw points: ---------------------------------------------------------------------------
			device.PipelineState = factory[(int)RenderFlags.DRAW|(int)RenderFlags.POINT|anchorFlag];
			device.SetCSRWBuffer( 0, null );
			device.GeometryShaderResources	[2] = ls.CurrentStateBuffer;
			
//			device.PixelShaderResources		[0] = particleTex;
			device.PixelShaderResources		[0] = atlas.Texture;
			device.Draw(nodeList.Count, 0);

								
			// draw lines: ----------------------------------------------------------------------------
			device.PipelineState = factory[(int)RenderFlags.DRAW|(int)RenderFlags.LINE|anchorFlag];
			device.GeometryShaderResources	[2] = ls.CurrentStateBuffer;
			device.GeometryShaderResources	[3] = ls.LinksBuffer;
			device.Draw( edgeList.Count, 0 );



			// draw highlighted points: ---------------------------------------------------------------
			device.PipelineState = factory[(int)RenderFlags.DRAW | (int)RenderFlags.SELECTION|anchorFlag];
			device.PixelShaderResources		[0] = highlightTex;

			foreach (var high in highlightNodesList)
			{
				device.GeometryShaderResources[4] = high.Item1;
				parameters.highNodeColor = high.Item2.color.ToVector4();
				paramsCB.SetData(parameters);
				int num = high.Item2.number;
				device.Draw(num, 0);
			}

			// draw highlighted lines: ----------------------------------------------------------------
	//		device.PipelineState = factory[(int)RenderFlags.DRAW | (int)RenderFlags.HIGH_LINE|anchorFlag];
	//		device.GeometryShaderResources	[5] = highlightedEdgesBuffer;
	//		device.Draw(highlightedEdgesBuffer.GetStructureCount(), 0);


			// draw sparks: ---------------------------------------------------------------------------
			List<Spark> updSparks = new List<Spark>();
			foreach (var sp in sparkList)
			{
				Spark updSp = sp;
				updSp.Parameter = sp.Parameter + gameTime.Elapsed.Milliseconds / sp.Time;
				if (updSp.Parameter < 1.0f)
				{
					updSparks.Add(updSp);
				}
			}
			sparkList = updSparks;

			if (sparkBuffer != null && updSparks.Count > 0)
			{
				sparkBuffer.SetData(updSparks.ToArray());

				device.PipelineState = factory[(int)RenderFlags.DRAW | (int)RenderFlags.SPARKS | anchorFlag];
				device.PixelShaderResources		[0] = sparkTex;
				device.GeometryShaderResources	[2] = ls.CurrentStateBuffer;
				device.GeometryShaderResources	[6] = sparkBuffer;
				device.Draw(sparkList.Count, 0);
			}
		}
	}
}
