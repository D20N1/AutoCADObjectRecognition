using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lines
{
    class FoundObjects
    {
        
        public ObjectIdCollection SortedFromLines;
        public ObjectIdCollection SortedFromCircles;
        public FoundObjects()
        {
            SortedFromLines = new ObjectIdCollection();
            SortedFromCircles = new ObjectIdCollection();
        }
        public int GetCount()
        {
            return SortedFromLines.Count + SortedFromCircles.Count;
        }

    }
}
