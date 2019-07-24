using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Grouper2.Host.SysVol.Files;
using Grouper2.Utility;

namespace Grouper2.Host.SysVol
{
    public class SysvolMapper
    {
        public static TreeNode<DaclProvider> MapSysvol(string root, bool NoNtfrs, bool ignoreScripts)
        {
            // return vals
            TreeNode<DaclProvider> map = new TreeNode<DaclProvider>(new SysvolRoot(root));

            // Data structure to hold names of subfolders to be
            // examined for files.
            Stack<string> dirs = new Stack<string>();

            if (!Directory.Exists(root)) throw new ArgumentException();

            dirs.Push(root);

            while (dirs.Count > 0)
            {
                string currentDir = dirs.Pop();
                string[] subDirs;
                try
                {
                    subDirs = Directory.GetDirectories(currentDir);
                }
                catch (UnauthorizedAccessException e)
                {
                    Console.Error.WriteLine("I had a problem with " + currentDir +
                                            ". I guess you could try to fix it?");
                    Output.DebugWrite(e.ToString());
                    continue;
                }
                catch (DirectoryNotFoundException e)
                {
                    Console.Error.WriteLine("I had a problem with " + currentDir +
                                            ". I guess you could try to fix it?");
                    Output.DebugWrite(e.ToString());
                    continue;
                }

                string[] files = null;
                try
                {
                    files = Directory.GetFiles(currentDir);
                }

                catch (UnauthorizedAccessException e)
                {
                    Console.Error.WriteLine("I had a problem with " + currentDir +
                                            ". I guess you could try to fix it?");
                    Output.DebugWrite(e.ToString());
                    continue;
                }

                catch (DirectoryNotFoundException e)
                {
                    Console.Error.WriteLine("I had a problem with " + currentDir +
                                            ". I guess you could try to fix it?");
                    Output.DebugWrite(e.ToString());
                    continue;
                }

                // Perform the required action on each file here.
                // Modify this block to perform your required task.
                foreach (string file in files)
                {
                    try
                    {
                        // build the file we want
                        SysvolFile created = FileFactories.Manufacture(file);
                        if (created == null) 
                            continue;
                        // add it to the tree
                        map.FindTreeNode(n => string.Equals(n.Data.Path, currentDir)).AddChild(created);
                    }
                    catch (FileNotFoundException e)
                    {
                        // If file was deleted by a separate application
                        //  or thread since the call to TraverseTree()
                        // then just continue.
                        Console.WriteLine(e.Message);
                    }
                }

                foreach (string str in subDirs)
                {
                    // add children
                    try
                    {
                        // skip if it's not what was asked for explicitly
                        DirectoryInfo di = new DirectoryInfo(str);
                        if (NoNtfrs // we were told not to look in
                            && !string.Equals(di.Name.ToLower(), "policies") // anything that isn't policies
                            && !string.Equals(di.Name.ToLower(), "scripts") // or scripts
                        ) continue;
                        // or if
                        if (ignoreScripts // we were told we don't want to fuck with scripts
                            && string.Equals(di.Name.ToLower(), "scripts") // and this is a script directory
                        ) continue;

                        // first add to the tree after building the right type of folder
                        SysvolDirectory dirObject = FolderFactories.Manufacture(str, root);
                        if (dirObject == null) continue;
                        map.FindTreeNode(n => string.Equals(n.Data.Path, currentDir))
                            .AddChild(dirObject);

                        // add to the search stack
                        dirs.Push(str);
                    }
                    catch (Exception e)
                    {
                        Console.Error.WriteLine("I had a problem with " + currentDir +
                                                ". I guess you could try to fix it?");
                        Output.DebugWrite(e.ToString());
                    }
                }
            }
            return map;
        }


        // blatantly stolen from: https://github.com/gt4dev/yet-another-tree-structure
        public class TreeNode<T> : IEnumerable<TreeNode<T>>
        {
            public TreeNode(T data)
            {
                Data = data;
                Children = new LinkedList<TreeNode<T>>();

                ElementsIndex = new LinkedList<TreeNode<T>>();
                ElementsIndex.Add(this);
            }

            public T Data { get; set; }
            public TreeNode<T> Parent { get; set; }
            public ICollection<TreeNode<T>> Children { get; set; }
            
            public ICollection<TreeNode<T>> Descendants
            {
                get
                {
                    Stack<TreeNode<T>> searches = new Stack<TreeNode<T>>();
                    ICollection<TreeNode<T>> descendants = new List<TreeNode<T>>();

                    searches.Push(this);
                    
                    while (searches.Count > 0)
                    {
                        TreeNode<T> node = searches.Pop();
                        foreach (TreeNode<T> child in node.Children)
                        {
                            descendants.Add(child);
                            if (!child.IsLeaf)
                            {
                                searches.Push(child);
                            }
                        }
                    }

                    return descendants;
                }
            }

            public bool IsRoot => Parent == null;

            public bool IsLeaf => Children.Count == 0;

            public int Level
            {
                get
                {
                    if (IsRoot)
                        return 0;
                    return Parent.Level + 1;
                }
            }

            public TreeNode<T> AddChild(T child)
            {
                TreeNode<T> childNode = new TreeNode<T>(child) {Parent = this};
                Children.Add(childNode);

                RegisterChildForSearch(childNode);

                return childNode;
            }

            public override string ToString()
            {
                return Data != null ? Data.ToString() : "[data null]";
            }


            #region searching

            private ICollection<TreeNode<T>> ElementsIndex { get; }

            private void RegisterChildForSearch(TreeNode<T> node)
            {
                ElementsIndex.Add(node);
                if (Parent != null)
                    Parent.RegisterChildForSearch(node);
            }

            public TreeNode<T> FindTreeNode(Func<TreeNode<T>, bool> predicate)
            {
                return ElementsIndex.FirstOrDefault(predicate);
            }

            #endregion


            #region iterating

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public IEnumerator<TreeNode<T>> GetEnumerator()
            {
                yield return this;
                foreach (TreeNode<T> directChild in Children)
                foreach (TreeNode<T> anyChild in directChild)
                    yield return anyChild;
            }

            #endregion

        }
    }
}