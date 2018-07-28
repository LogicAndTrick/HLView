using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;

namespace HLView.Formats.Bsp
{
    public class BspFile
    {
        public Header Header { get; private set; }
        public string Entities { get; private set; }
        public List<Plane> Planes { get; private set; }
        public List<Texture> Textures { get; private set; }
        public List<Vector3> Vertices { get; private set; }
        public byte[] VisData { get; set; }
        public List<Node> Nodes { get; private set; }
        public List<TextureInfo> TextureInfos { get; private set; }
        public List<Face> Faces { get; private set; }
        public byte[] Lightmap { get; private set; }
        public List<Clipnode> Clipnodes { get; private set; }
        public List<Leaf> Leaves { get; private set; }
        public ushort[] MarkSurfaces { get; private set; }
        public List<Edge> Edges { get; private set; }
        public int[] SurfaceEdges { get; private set; }
        public List<Model> Models { get; private set; }

        public BspFile(Stream stream)
        {
            Planes = new List<Plane>();
            Textures = new List<Texture>();
            Vertices = new List<Vector3>();
            Nodes = new List<Node>();
            TextureInfos = new List<TextureInfo>();
            Faces = new List<Face>();
            Clipnodes = new List<Clipnode>();
            Leaves = new List<Leaf>();
            Edges = new List<Edge>();
            Models = new List<Model>();
            using (var br = new BinaryReader(stream, Encoding.ASCII)) Read(br);
        }

        private void Read(BinaryReader br)
        {
            Header = new Header
            {
                Version = (Version) br.ReadInt32(),
                Lumps = new Lump[Lump.NumLumps]
            };

            if (Header.Version != Version.Goldsource)
            {
                throw new NotSupportedException("Only Goldsource (v30) BSP files are supported.");
            }

            for (var i = 0; i < Header.Lumps.Length; i++)
            {
                Header.Lumps[i] = new Lump
                {
                    Offset = br.ReadInt32(),
                    Length = br.ReadInt32()
                };
            }

            // Entities
            var lump = Header.Lumps[Lump.Entities];
            br.BaseStream.Seek(lump.Offset, SeekOrigin.Begin);
            Entities = Encoding.ASCII.GetString(br.ReadBytes(lump.Length));

            // Planes
            lump = Header.Lumps[Lump.Planes];
            br.BaseStream.Seek(lump.Offset, SeekOrigin.Begin);
            while (br.BaseStream.Position < lump.Offset + lump.Length)
            {
                Planes.Add(new Plane
                {
                    Normal = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle()),
                    Distance = br.ReadSingle(),
                    Type = br.ReadInt32()
                });
            }

            // Textures
            lump = Header.Lumps[Lump.Textures];
            br.BaseStream.Seek(lump.Offset, SeekOrigin.Begin);

            var numTextures = br.ReadUInt32();
            var offsets = new int[numTextures];
            for (var i = 0; i < numTextures; i++) offsets[i] = br.ReadInt32();
            foreach (var offset in offsets)
            {
                br.BaseStream.Seek(lump.Offset + offset, SeekOrigin.Begin);
                var name = br.ReadChars(Texture.NameLength);
                var len = Array.IndexOf(name, '\0');
                var tex = new Texture
                {
                    Name = new string(name, 0, len < 0 ? name.Length : len),
                    Width = br.ReadUInt32(),
                    Height = br.ReadUInt32(),
                    Offsets = new uint[Texture.MipLevels]
                };
                for (var i = 0; i < Texture.MipLevels; i++) tex.Offsets[i] = br.ReadUInt32();
                Textures.Add(tex);
            }

            // Vertices
            lump = Header.Lumps[Lump.Vertices];
            br.BaseStream.Seek(lump.Offset, SeekOrigin.Begin);
            while (br.BaseStream.Position < lump.Offset + lump.Length)
            {
                Vertices.Add(new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle()));
            }

            // Visibility
            lump = Header.Lumps[Lump.Visibility];
            br.BaseStream.Seek(lump.Offset, SeekOrigin.Begin);
            VisData = br.ReadBytes(lump.Length);
            //TODO: decompress vis data

            // Nodes
            lump = Header.Lumps[Lump.Nodes];
            br.BaseStream.Seek(lump.Offset, SeekOrigin.Begin);
            while (br.BaseStream.Position < lump.Offset + lump.Length)
            {
                var node = new Node
                {
                    Plane = br.ReadUInt32(),
                    Children = new short[2],
                    Mins = new short[3],
                    Maxs = new short[3]
                };
                for (var i = 0; i < 2; i++) node.Children[i] = br.ReadInt16();
                for (var i = 0; i < 3; i++) node.Mins[i] = br.ReadInt16();
                for (var i = 0; i < 3; i++) node.Maxs[i] = br.ReadInt16();
                node.FirstFace = br.ReadUInt16();
                node.NumFaces = br.ReadUInt16();
                Nodes.Add(node);
            }

            // Texinfo
            lump = Header.Lumps[Lump.Texinfo];
            br.BaseStream.Seek(lump.Offset, SeekOrigin.Begin);
            while (br.BaseStream.Position < lump.Offset + lump.Length)
            {
                var info = new TextureInfo
                {
                    S = new Vector4(br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle()),
                    T = new Vector4(br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle()),
                    MipTexture = br.ReadInt32(),
                    Flags = br.ReadInt32()
                };
                TextureInfos.Add(info);
            }

            // Faces
            lump = Header.Lumps[Lump.Faces];
            br.BaseStream.Seek(lump.Offset, SeekOrigin.Begin);
            while (br.BaseStream.Position < lump.Offset + lump.Length)
            {
                var face = new Face
                {
                    Plane = br.ReadInt16(),
                    Side = br.ReadInt16(),
                    FirstEdge = br.ReadInt32(),
                    NumEdges = br.ReadInt16(),
                    TextureInfo = br.ReadInt16(),
                    Styles = br.ReadBytes(Face.MaxLightmaps),
                    LightmapOffset = br.ReadInt32()
                };
                Faces.Add(face);
            }

            // Lighting
            lump = Header.Lumps[Lump.Lighting];
            br.BaseStream.Seek(lump.Offset, SeekOrigin.Begin);
            Lightmap = br.ReadBytes(lump.Length);

            // Clipnodes
            lump = Header.Lumps[Lump.Clipnodes];
            br.BaseStream.Seek(lump.Offset, SeekOrigin.Begin);
            while (br.BaseStream.Position < lump.Offset + lump.Length)
            {
                var clip = new Clipnode
                {
                    Plane = br.ReadInt32(),
                    Children = new[] { br.ReadInt16(), br.ReadInt16() }
                };
                Clipnodes.Add(clip);
            }

            // Leaves
            lump = Header.Lumps[Lump.Leaves];
            br.BaseStream.Seek(lump.Offset, SeekOrigin.Begin);
            while (br.BaseStream.Position < lump.Offset + lump.Length)
            {
                var leaf = new Leaf
                {
                    Contents = (Contents) br.ReadInt32(),
                    VisOffset = br.ReadInt32(),
                    Mins = new [] { br.ReadInt16(), br.ReadInt16(), br.ReadInt16() },
                    Maxs = new [] { br.ReadInt16(), br.ReadInt16(), br.ReadInt16() },
                    FirstMarkSurface = br.ReadUInt16(),
                    NumMarkSurfaces = br.ReadUInt16(),
                    AmbientLevels = br.ReadBytes(Leaf.MaxNumAmbientLevels)
                };
                Leaves.Add(leaf);
            }

            // Marksurfaces
            lump = Header.Lumps[Lump.Marksurfaces];
            br.BaseStream.Seek(lump.Offset, SeekOrigin.Begin);
            MarkSurfaces = new ushort[lump.Length / sizeof(ushort)];
            for (var i = 0; i < MarkSurfaces.Length; i++) MarkSurfaces[i] = br.ReadUInt16();

            // Edges
            lump = Header.Lumps[Lump.Edges];
            br.BaseStream.Seek(lump.Offset, SeekOrigin.Begin);
            while (br.BaseStream.Position < lump.Offset + lump.Length)
            {
                var edge = new Edge
                {
                    Start = br.ReadUInt16(),
                    End = br.ReadUInt16()
                };
                Edges.Add(edge);
            }

            // Surfedges
            lump = Header.Lumps[Lump.Surfedges];
            br.BaseStream.Seek(lump.Offset, SeekOrigin.Begin);
            SurfaceEdges = new int[lump.Length / sizeof(int)];
            for (var i = 0; i < SurfaceEdges.Length; i++) SurfaceEdges[i] = br.ReadInt32();

            // Models
            lump = Header.Lumps[Lump.Models];
            br.BaseStream.Seek(lump.Offset, SeekOrigin.Begin);
            while (br.BaseStream.Position < lump.Offset + lump.Length)
            {
                var model = new Model
                {
                    Mins = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle()),
                    Maxs = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle()),
                    Origin = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle()),
                    HeadNodes = new [] { br.ReadInt32(), br.ReadInt32(), br.ReadInt32(), br.ReadInt32() },
                    VisLeaves = br.ReadInt32(),
                    FirstFace = br.ReadInt32(),
                    NumFaces = br.ReadInt32()
                };
                Models.Add(model);
            }
        }
    }
}