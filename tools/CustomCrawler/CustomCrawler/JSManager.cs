/***

   Copyright (C) 2020. rollrat. All Rights Reserved.
   
   Author: Custom Crawler Developer

***/

using Esprima;
using Esprima.Ast;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomCrawler
{
    public class JsManager : ILazy<JsManager>
    {
        Dictionary<string, Script> contents = new Dictionary<string, Script>();

        public void Register(string url, string js)
        {
            var parser = new JavaScriptParser(js, new ParserOptions { Loc = true });

            try
            {
                var script = parser.ParseScript(true);
                contents.Add(url, script);
            }
            catch { }
        }

        public bool Contains(string url) => contents.ContainsKey(url);

        public List<INode> FindByLocation(string url, int line, int column)
        {
            var result = new List<INode>();

            if (contents.ContainsKey(url))
            {
                var script = contents[url];
                find_internal(ref result, script.ChildNodes, line, column);
            }

            return result;
        }

        class bb : INode
        {
            public bb(int l, int c)
            {
                Location = new Location(new Position(l, c), new Position(l, c));
            }

            public Nodes Type => Nodes.ArrayExpression;

            public Range Range { get; set; }
            public Location Location { get; set; }

            public IEnumerable<INode> ChildNodes { get; set; }
        }

        void find_internal(ref List<INode> result, INode node, int line, int column)
        {
            if (node.Location.Start.Line > line || node.Location.End.Line < line)
                return;

            if (node.Location.Start.Line == node.Location.End.Line)
            {
                if (node.Location.Start.Column > column || node.Location.End.Column < column)
                    return;
            }

            result.Add(node);

            if (node.ChildNodes == null || node.ChildNodes.Count() == 0)
                return;

            var ii = node.ChildNodes.ToList().BinarySearch(new bb(line, column), Comparer<INode>.Create((x, y) =>
            {
                if (x.Location.Start.Line != y.Location.Start.Line)
                    return x.Location.Start.Line.CompareTo(y.Location.Start.Line);
                if (x.Location.End.Line != y.Location.End.Line)
                    return x.Location.End.Line.CompareTo(y.Location.End.Line);
                if (x.Location.Start.Column != y.Location.Start.Column)
                    return x.Location.Start.Column.CompareTo(y.Location.Start.Line);
                if (x.Location.End.Column != y.Location.End.Column)
                    return x.Location.End.Column.CompareTo(y.Location.End.Column);
                return 0;
            }));

            find_internal(ref result, node.ChildNodes.ElementAt(ii), line, column);
        }

        void find_internal(ref List<INode> result, IEnumerable<INode> node, int line, int column)
        {
            if (node == null || node.Count() == 0)
                return;

            var nrr = node.ToList();
            var ii = nrr.BinarySearch(new bb(line, column), Comparer<INode>.Create((x, y) =>
            {
                if (x.Location.Start.Line != y.Location.Start.Line)
                    return x.Location.Start.Line.CompareTo(y.Location.Start.Line);
                //if (x.Location.End.Line != y.Location.End.Line)
                //    return x.Location.End.Line.CompareTo(y.Location.End.Line);
                if (x.Location.Start.Column != y.Location.Start.Column)
                    return x.Location.Start.Column.CompareTo(y.Location.Start.Column);
                //if (x.Location.End.Column != y.Location.End.Column)
                //    return x.Location.End.Column.CompareTo(y.Location.End.Column);
                return 0;
            }));

            if (node.Count() == 1)
                ii = 0;

            if (ii < 0)
                ii = ~ii - 1;

            if (ii < 0 || ii >= node.Count())
                return;
            
            var z = node.ElementAt(ii);

            if (z.Location.Start.Line > line || z.Location.End.Line < line)
                return;

            if (z.Location.Start.Line == z.Location.End.Line)
            {
                if (z.Location.Start.Column > column || z.Location.End.Column < column)
                    return;
            }

            result.Add(z);

            //find_internal(ref result, z.ChildNodes.ElementAt(ii), line, column);
            find_internal(ref result, z.ChildNodes, line, column);
        }
    }
}
