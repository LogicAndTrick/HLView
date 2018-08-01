using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using HLView.Formats.Bsp;
using HLView.Graphics;
using HLView.Graphics.Renderables;
using HLView.Visualisers;
using Veldrid;
using Environment = HLView.Formats.Environment.Environment;

namespace HLView
{
    public partial class Viewer : Form
    {
        private string _currentFile;
        private Environment _currentEnvironment;
        private IVisualiser _currentVisualiser;

        private List<IVisualiser> _visualisers;

        public Viewer()
        {
            InitializeComponent();

            _visualisers = GetType().Assembly.GetTypes()
                .Where(x => !x.IsInterface && typeof(IVisualiser).IsAssignableFrom(x))
                .Select(Activator.CreateInstance)
                .OfType<IVisualiser>()
                .ToList();
        }

        private void Viewer_Load(object sender, EventArgs e)
        {
            // 
        }

        private void Viewer_FormClosing(object sender, FormClosingEventArgs e)
        {
            CloseCurrentFile();
        }

        private void OpenMenuItem_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog())
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    OpenFile(ofd.FileName, true);
                }
            }
        }

        private Dictionary<string, TreeNode> _nodes;

        private void RefreshTree()
        {
            FileTree.BeginUpdate();

            FileTree.Nodes.Clear();

            var rootNode = new TreeNode();

            _nodes = new Dictionary<string, TreeNode>(StringComparer.InvariantCultureIgnoreCase);
            _nodes["/"] = rootNode;

            if (_currentEnvironment != null)
            {
                rootNode.Text = _currentEnvironment.Name;
                var dirs = new[] { _currentEnvironment.BaseFolder, _currentEnvironment.ModFolder }
                    .Where(x => !String.IsNullOrWhiteSpace(x) && Directory.Exists(x))
                    .Distinct()
                    .Select(x => new DirectoryInfo(x))
                    .ToList();
                AddTreeNodes(rootNode, dirs);
            }
            else
            {
                rootNode.Text = "No environment";
            }

            FileTree.Nodes.Add(rootNode);

            if (_currentFile != null && _nodes.ContainsKey(_currentFile))
            {
                var currentNode = _nodes[_currentFile];
                while (currentNode != rootNode && currentNode != null)
                {
                    currentNode.Expand();
                    currentNode = currentNode.Parent;
                }

                FileTree.SelectedNode = _nodes[_currentFile];
                FileTree.SelectedNode.EnsureVisible();
            }

            FileTree.EndUpdate();
        }

        private void AddTreeNodes(TreeNode parent, IEnumerable<FileSystemInfo> paths)
        {
            foreach (var p in paths.OrderBy(x => x is DirectoryInfo ? 0 : 1).ThenBy(x => x.Name, StringComparer.InvariantCultureIgnoreCase))
            {
                var node = new TreeNode
                {
                    Text = p.Name,
                    Tag = p.FullName
                };

                _nodes[p.FullName] = node;

                if (p.Exists && p is DirectoryInfo d)
                {
                    AddTreeNodes(node, d.EnumerateFileSystemInfos());
                    if (node.Nodes.Count > 0)
                        parent.Nodes.Add(node);
                }
                else if (p.Exists && p is FileInfo f)
                {
                    if (_visualisers.Any(x => x.Supports(p.FullName)))
                        parent.Nodes.Add(node);
                }
            }
        }

        private void OpenFile(string path, bool switchEnvironment)
        {
            CloseCurrentFile();

            _currentFile = path;

            if (switchEnvironment || _currentEnvironment == null)
            {
                _currentEnvironment = Environment.FromFile(path);
                RefreshTree();
            }

            var vis = _visualisers.FirstOrDefault(x => x.Supports(path));
            if (vis == null) return;

            vis.Container.Dock = DockStyle.Fill;
            Controls.Add(vis.Container);
            vis.Open(_currentEnvironment, path);
            _currentVisualiser = vis;
        }

        private void CloseCurrentFile()
        {
            _currentFile = null;
            if (_currentVisualiser == null) return;

            Controls.Remove(_currentVisualiser.Container);
            _currentVisualiser.Close();
            _currentVisualiser = null;
        }

        private void OpenSelectedNode(object sender, MouseEventArgs e)
        {
            var sn = FileTree.SelectedNode;

            var f = sn?.Tag as string;
            if (f == null || !File.Exists(f)) return;

            OpenFile(f, false);
        }
    }
}
