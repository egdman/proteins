using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GraphVis;

namespace Proteins
{
	public class ProteinInteraction : Edge
	{
		public string Type {get; set;}
		public ProteinInteraction()
			: base()
		{ }

		public ProteinInteraction(int end1, int end2, float length, float value, string type)
			: base(end1, end2, length, value)
		{
			Type = type;
		}
		public override string GetInfo()
		{
			return base.GetInfo() + ",type:" + Type;
		}
	}
}
