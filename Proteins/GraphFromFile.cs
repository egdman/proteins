using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GraphVis
{
	public abstract class GraphFromFile : Graph
	{

		public abstract void ReadFromFile( string path );

	}
}
