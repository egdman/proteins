using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Fusion;
using Fusion.Graphics;
using Fusion.Mathematics;
using Fusion.Input;

using GraphVis;

namespace Proteins
{
	class ProteinNode : NodeWithText
	{
		bool active;

		public bool Active
		{
			get
			{
				return active;
			}
		}


		public ProteinNode(string Text, float size, Color color)
			: base(Text, size, color)
		{
			active = true;
		}

		public void Activate()
		{
			active = true;
		}

		public void Deactivate()
		{
			active = false;
		}
	}
}
