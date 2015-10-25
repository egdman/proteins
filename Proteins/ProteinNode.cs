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
	public enum SignalType
	{
		None,
		Plus,
		Minus
	}

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


		public string Name
		{
			get
			{
				return Text;
			}
		}


		public SignalType Signal
		{
			get;
			set;
		}


		public ProteinNode(string Text, float size, Color color)
			: base(Text, size, color)
		{
			active = true;
			Signal = SignalType.None;
		}

		public void Activate()
		{
			active = true;
		}

		public void Deactivate()
		{
			active = false;
		}


		public static SignalType FlipSignal(SignalType signal)
		{
			if (signal == SignalType.Plus)
			{
				signal = SignalType.Minus;
			}
			else if (signal == SignalType.Minus)
			{
				signal = SignalType.Plus;
			}
			return signal;
		}
	}
}
