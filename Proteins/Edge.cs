using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GraphVis
{
	public class Edge
	{
		public int End1;
		public int End2;
		public float Length;
		public float Value;

		public Edge()
		{
			End1 = End2 = 0;
			Length = 1.0f;
			Value = 0;
		}

		public Edge(int end1, int end2, float length, float value)
		{
			End1 = end1;
			End2 = end2;
			Length = length;
			Value = value;
		}

		public virtual string GetInfo()
		{
			return "id1:" + End1 + ",id2:" + End2 + ",length:" + Length + ",value:" + Value;
		}
	}
}
