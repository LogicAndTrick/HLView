using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;

namespace HLView.Formats.Mdl
{
    public class MdlFile
    {
        public Header Header { get; set; }
        public List<Bone> Bones { get; set; }
        public List<BoneController> BoneControllers { get; set; }
        public List<Hitbox> Hitboxes { get; set; }
        public List<Sequence> Sequences { get; set; }
        public List<SequenceGroup> SequenceGroups { get; set; }
        public List<Texture> Textures { get; set; }
        public List<SkinFamily> Skins { get; set; }
        public List<BodyPart> BodyParts { get; set; }
        public List<Attachment> Attachments { get; set; }

        public MdlFile(IEnumerable<Stream> streams)
        {
            Bones = new List<Bone>();
            BoneControllers = new List<BoneController>();
            Hitboxes = new List<Hitbox>();
            Sequences = new List<Sequence>();
            SequenceGroups = new List<SequenceGroup>();
            Textures = new List<Texture>();
            Skins = new List<SkinFamily>();
            BodyParts = new List<BodyPart>();
            Attachments = new List<Attachment>();

            var readers = streams.Select(x => new BinaryReader(x, Encoding.ASCII)).ToList();
            try
            {
                Read(readers);
            }
            finally
            {
                readers.ForEach(x => x.Dispose());
            }
        }

        public static MdlFile FromFile(string filename)
        {
            var dir = Path.GetDirectoryName(filename);
            var fname = Path.GetFileNameWithoutExtension(filename);

            var streams = new List<Stream>();
            try
            {
                streams.Add(File.OpenRead(filename));
                var tfile = Path.Combine(dir, fname + "t.mdl");
                if (File.Exists(tfile)) streams.Add(File.OpenRead(tfile));
                for (var i = 1; i < 32; i++)
                {
                    var sfile = Path.Combine(dir, fname + i.ToString("00") + ".mdl");
                    if (File.Exists(sfile)) streams.Add(File.OpenRead(sfile));
                    else break;
                }

                return new MdlFile(streams);
            }
            finally
            {
                foreach (var s in streams) s.Dispose();
            }
        }

        private void Read(IEnumerable<BinaryReader> readers)
        {
            var main = new List<BinaryReader>();
            var sequenceGroups = new Dictionary<string, BinaryReader>();

            foreach (var br in readers)
            {
                var id = (ID)br.ReadInt32();
                var version = (Version)br.ReadInt32();

                if (version != Version.Goldsource)
                {
                    throw new NotSupportedException("Only Goldsource (v10) MDL files are supported.");
                }

                if (id != ID.Idsq && id != ID.Idst)
                {
                    throw new NotSupportedException("Only Goldsource (v10) MDL files are supported.");
                }

                if (id == ID.Idst)
                {
                    main.Add(br);
                }
                else
                {
                    var name = br.ReadFixedLengthString(64);
                    sequenceGroups[name] = br;
                }
            }

            foreach (var br in main)
            {
                Read(br, sequenceGroups);
            }
        }

        #region Reading
        
        private void Read(BinaryReader br, Dictionary<string, BinaryReader> sequenceGroups)
        {
            Header = new Header
            {
                ID = ID.Idst,
                Version = Version.Goldsource,
                Name = br.ReadFixedLengthString(64),
                Size = br.ReadInt32(),
                EyePosition = br.ReadVector3(),
                HullMin = br.ReadVector3(),
                HullMax = br.ReadVector3(),
                BoundingBoxMin = br.ReadVector3(),
                BoundingBoxMax = br.ReadVector3(),
                Flags = br.ReadInt32()
            };

            // Read all the nums/offsets from the header
            var sections = new int[(int)Section.NumSections][];
            for (var i = 0; i < (int) Section.NumSections; i++)
            {
                var sec = (Section) i;

                int indexNum;
                if (sec == Section.Texture || sec == Section.Skin) indexNum = 3;
                else indexNum = 2;

                sections[i] = new int[indexNum];
                for (var j = 0; j < indexNum; j++)
                {
                    sections[i][j] = br.ReadInt32();
                }
            }

            // Bones
            var num = SeekToSection(br, Section.Bone, sections);
            var numBones = num;
            for (var i = 0; i < num; i++)
            {
                var bone = new Bone
                {
                    Name = br.ReadFixedLengthString(32),
                    Parent = br.ReadInt32(),
                    Flags = br.ReadInt32(),
                    Controllers = br.ReadArray(6, x => x.ReadInt32()),
                    Position = br.ReadVector3(),
                    Rotation = br.ReadVector3(),
                    PositionScale = br.ReadVector3(),
                    RotationScale = br.ReadVector3()
                };
                Bones.Add(bone);
            }

            // Bone controllers
            num = SeekToSection(br, Section.BoneController, sections);
            for (var i = 0; i < num; i++)
            {
                var boneController = new BoneController
                {
                    Bone = br.ReadInt32(),
                    Type = br.ReadInt32(),
                    Start = br.ReadSingle(),
                    End = br.ReadSingle(),
                    Rest = br.ReadInt32(),
                    Index = br.ReadInt32()
                };
                BoneControllers.Add(boneController);
            }

            // Hitboxes
            num = SeekToSection(br, Section.Hitbox, sections);
            for (var i = 0; i < num; i++)
            {
                var hitbox = new Hitbox
                {
                    Bone = br.ReadInt32(),
                    Group = br.ReadInt32(),
                    Min = br.ReadVector3(),
                    Max = br.ReadVector3()
                };
                Hitboxes.Add(hitbox);
            }

            // Sequence groups
            num = SeekToSection(br, Section.SequenceGroup, sections);
            for (var i = 0; i < num; i++)
            {
                var group = new SequenceGroup
                {
                    Label = br.ReadFixedLengthString(32),
                    Name = br.ReadFixedLengthString(64)
                };
                br.ReadBytes(8); // unused
                SequenceGroups.Add(group);
            }

            // Sequences
            num = SeekToSection(br, Section.Sequence, sections);
            for (var i = 0; i < num; i++)
            {
                var sequence = new Sequence
                {
                    Name = br.ReadFixedLengthString(32),
                    Framerate = br.ReadSingle(),
                    Flags = br.ReadInt32(),
                    Activity = br.ReadInt32(),
                    ActivityWeight = br.ReadInt32(),
                    NumEvents = br.ReadInt32(),
                    EventIndex = br.ReadInt32(),
                    NumFrames = br.ReadInt32(),
                    NumPivots = br.ReadInt32(),
                    PivotIndex = br.ReadInt32(),
                    MotionType = br.ReadInt32(),
                    MotionBone = br.ReadInt32(),
                    LinearMovement = br.ReadVector3(),
                    AutoMovePositionIndex = br.ReadInt32(),
                    AutoMoveAngleIndex = br.ReadInt32(),
                    Min = br.ReadVector3(),
                    Max = br.ReadVector3(),
                    NumBlends = br.ReadInt32(),
                    AnimationIndex = br.ReadInt32(),
                    BlendType = br.ReadArray(2, x => x.ReadInt32()),
                    BlendStart = br.ReadArray(2, x => x.ReadSingle()),
                    BlendEnd = br.ReadArray(2, x => x.ReadSingle()),
                    BlendParent = br.ReadInt32(),
                    SequenceGroup = br.ReadInt32(),
                    EntryNode = br.ReadInt32(),
                    ExitNode = br.ReadInt32(),
                    NodeFlags = br.ReadInt32(),
                    NextSequence = br.ReadInt32()
                };

                var seqGroup = SequenceGroups[sequence.SequenceGroup];

                // Only load seqence group 0 for now (others are in other files)
                if (sequence.SequenceGroup == 0)
                {
                    var pos = br.BaseStream.Position;
                    sequence.Blends = LoadAnimationBlends(br, sequence, numBones);
                    br.BaseStream.Position = pos;
                }
                else if (sequenceGroups.ContainsKey(seqGroup.Name))
                {
                    var reader = sequenceGroups[seqGroup.Name];
                    sequence.Blends = LoadAnimationBlends(reader, sequence, numBones);
                }

                Sequences.Add(sequence);
            }

            // Textures
            num = SeekToSection(br, Section.Texture, sections);
            for (var i = 0; i < num; i++)
            {
                var texture = new Texture
                {
                    Name = br.ReadFixedLengthString(64),
                    Flags = (TextureFlags) br.ReadInt32(),
                    Width = br.ReadInt32(),
                    Height = br.ReadInt32(),
                    Index = br.ReadInt32()
                };
                Textures.Add(texture);
            }

            // Texture data
            for (var i = 0; i < num; i++)
            {
                var t = Textures[i];
                br.BaseStream.Position = t.Index;
                t.Data = br.ReadBytes(t.Width * t.Height);
                t.Palette = br.ReadBytes(256 * 3);
                Textures[i] = t;
            }

            // Skins
            var skinSection = sections[(int)Section.Skin];
            var numSkinRefs = skinSection[0];
            var numSkinFamilies = skinSection[1];
            br.BaseStream.Seek(skinSection[2], SeekOrigin.Begin);
            for (var i = 0; i < numSkinFamilies; i++)
            {
                var skin = new SkinFamily
                {
                    Textures = br.ReadArray(numSkinRefs, x => x.ReadInt16())
                };
                Skins.Add(skin);
            }
            
            // Body parts
            num = SeekToSection(br, Section.BodyPart, sections);
            for (var i = 0; i < num; i++)
            {
                var part = new BodyPart
                {
                    Name = br.ReadFixedLengthString(64),
                    NumModels = br.ReadInt32(),
                    Base = br.ReadInt32(),
                    ModelIndex = br.ReadInt32()
                };
                var pos = br.BaseStream.Position;
                part.Models = LoadModels(br, part);
                br.BaseStream.Position = pos;
                BodyParts.Add(part);
            }

            // Attachments
            num = SeekToSection(br, Section.Attachment, sections);
            for (var i = 0; i < num; i++)
            {
                var attachment = new Attachment
                {
                    Name = br.ReadFixedLengthString(32),
                    Type = br.ReadInt32(),
                    Bone = br.ReadInt32(),
                    Origin = br.ReadVector3(),
                    Vectors = br.ReadArray(3, x => x.ReadVector3())
                };
                Attachments.Add(attachment);
            }

            // Transitions

            // Sounds & Sound groups aren't used
        }

        private static int SeekToSection(BinaryReader br, Section section, int[][] sections)
        {
            var s = sections[(int)section];
            br.BaseStream.Seek(s[1], SeekOrigin.Begin);
            return s[0];
        }

        #endregion

        #region Animations

        private static Blend[] LoadAnimationBlends(BinaryReader br, Sequence sequence, int numBones)
        {
            var blends = new Blend[sequence.NumBlends];
            var blendLength = 6 * numBones;

            br.BaseStream.Seek(sequence.AnimationIndex, SeekOrigin.Begin);

            var animPosition = br.BaseStream.Position;
            var offsets = br.ReadArray(blendLength * sequence.NumBlends, x => x.ReadUInt16());
            for (var i = 0; i < sequence.NumBlends; i++)
            {
                var blendOffsets = new ushort[blendLength];
                Array.Copy(offsets, blendLength * i, blendOffsets, 0, blendLength);

                var startPosition = animPosition + i * blendLength * 2;
                blends[i].Frames = LoadAnimationFrames(br, sequence, numBones, startPosition, blendOffsets);
            }

            return blends;
        }

        private static AnimationFrame[] LoadAnimationFrames(BinaryReader br, Sequence sequence, int numBones, long startPosition, ushort[] boneOffsets)
        {
            var frames = new AnimationFrame[sequence.NumFrames];
            for (var i = 0; i < frames.Length; i++)
            {
                frames[i].Positions = new Vector3[numBones];
                frames[i].Rotations = new Vector3[numBones];
            }
            
            for (var i = 0; i < numBones; i++)
            {
                var boneValues = new short[6][];
                for (var j = 0; j < 6; j++)
                {
                    var offset = boneOffsets[i * 6 + j];
                    if (offset <= 0)
                    {
                        boneValues[j] = new short[sequence.NumFrames];
                        continue;
                    }

                    br.BaseStream.Seek(startPosition + i * 6 * 2 + offset, SeekOrigin.Begin);
                    boneValues[j] = ReadAnimationFrameValues(br, sequence.NumFrames);
                }

                for (var j = 0; j < sequence.NumFrames; j++)
                {
                    frames[j].Positions[i] = new Vector3(boneValues[0][j], boneValues[1][j], boneValues[2][j]);
                    frames[j].Rotations[i] = new Vector3(boneValues[3][j], boneValues[4][j], boneValues[5][j]);
                }
            }

            return frames;
        }
        
        private static short[] ReadAnimationFrameValues(BinaryReader br, int count)
        {
            /*
             * RLE data:
             * byte compressed_length - compressed number of values in the data
             * byte uncompressed_length - uncompressed number of values in run
             * short values[compressed_length] - values in the run, the last value is repeated to reach the uncompressed length
             */
            var values = new short[count];

            for (var i = 0; i < count; /* i = i */)
            {
                var run = br.ReadBytes(2); // read the compressed and uncompressed lengths
                var vals = br.ReadArray(run[0], x => x.ReadInt16()); // read the compressed data
                for (var j = 0; j < run[1] && i < count; i++, j++)
                {
                    var idx = Math.Min(run[0] - 1, j); // value in the data or the last value if we're past the end
                    values[i] = vals[idx];
                }
            }

            return values;
        }

        #endregion

        #region Models

        private static Model[] LoadModels(BinaryReader br, BodyPart part)
        {
            br.BaseStream.Seek(part.ModelIndex, SeekOrigin.Begin);

            var models = new Model[part.NumModels];
            for (var i = 0; i < part.NumModels; i++)
            {
                var model = new Model
                {
                    Name = br.ReadFixedLengthString(64),
                    Type = br.ReadInt32(),
                    Radius = br.ReadSingle(),
                    NumMesh = br.ReadInt32(),
                    MeshIndex = br.ReadInt32(),
                    NumVerts = br.ReadInt32(),
                    VertInfoIndex = br.ReadInt32(),
                    VertIndex = br.ReadInt32(),
                    NumNormals = br.ReadInt32(),
                    NormalInfoIndex = br.ReadInt32(),
                    NormalIndex = br.ReadInt32(),
                    NumGroups = br.ReadInt32(),
                    GroupIndex = br.ReadInt32()
                };

                var pos = br.BaseStream.Position;
                model.Meshes = ReadMeshes(br, model);
                br.BaseStream.Position = pos;

                models[i] = model;
            }

            return models;
        }

        private static Mesh[] ReadMeshes(BinaryReader br, Model model)
        {
            var meshes = new Mesh[model.NumMesh];

            // Read all the vertex data
            br.BaseStream.Position = model.VertInfoIndex;
            var vertexBones = br.ReadBytes(model.NumVerts);

            br.BaseStream.Position = model.NormalInfoIndex;
            var normalBones = br.ReadBytes(model.NumNormals);

            br.BaseStream.Position = model.VertIndex;
            var vertices = br.ReadArray(model.NumVerts, x => x.ReadVector3());

            br.BaseStream.Position = model.NormalIndex;
            var normals = br.ReadArray(model.NumNormals, x => x.ReadVector3());

            // Read the meshes
            br.BaseStream.Position = model.MeshIndex;
            for (var i = 0; i < model.NumMesh; i++)
            {
                var mesh = new Mesh
                {
                    NumTriangles = br.ReadInt32(),
                    TriangleIndex = br.ReadInt32(),
                    SkinRef = br.ReadInt32(),
                    NumNormals = br.ReadInt32(),
                    NormalIndex = br.ReadInt32()
                };
                meshes[i] = mesh;
            }

            // Read the triangle data
            for (var i = 0; i < model.NumMesh; i++)
            {
                meshes[i].Vertices = ReadTriangles(br, meshes[i], vertices, vertexBones, normals, normalBones);
            }

            return meshes;
        }

        private static MeshVertex[] ReadTriangles(BinaryReader br, Mesh mesh, Vector3[] vertices, byte[] vertexBones, Vector3[] normals, byte[] normalBones)
        {
            /*
             * Mesh data
             * short type - abs(type) is the length of the run
             *   - < 0 = triangle fan,
             *   - > 0 = triangle strip
             *   - 0 = end of list
             * short vertex - vertex index
             * short normal - normal index
             * short u, short v - texture coordinates
             */

            var meshVerts = new MeshVertex[mesh.NumTriangles * 3];
            var vi = 0;

            br.BaseStream.Position = mesh.TriangleIndex;

            short type;
            while ((type = br.ReadInt16()) != 0)
            {
                var fan = type < 0;
                var length = Math.Abs(type);
                var pointData = br.ReadArray(4 * length, x => x.ReadInt16());
                for (var i = 0; i < length - 2; i++)
                {
                    //                    | TRIANGLE FAN    |                       | TRIANGLE STRIP (ODD) |         | TRIANGLE STRIP (EVEN) |
                    var add = fan ? new[] { 0, i + 1, i + 2 } : (i % 2 == 1 ? new[] { i + 1, i, i + 2      } : new[] { i, i + 1, i + 2       });
                    foreach (var idx in add)
                    {
                        var vert = pointData[idx * 4 + 0];
                        var norm = pointData[idx * 4 + 1];
                        var s = pointData[idx * 4 + 2];
                        var t = pointData[idx * 4 + 3];
                        
                        meshVerts[vi++] = new MeshVertex
                        {
                            VertexBone = vertexBones[vert],
                            NormalBone = normalBones[norm],
                            Vertex = vertices[vert],
                            Normal = normals[norm],
                            Texture = new Vector2(s, t)
                        };
                    }
                }
            }

            return meshVerts;
        }

        #endregion

        private enum Section : int
        {
            Bone,
            BoneController,
            Hitbox,
            Sequence,
            SequenceGroup,
            Texture,
            Skin,
            BodyPart,
            Attachment,
            Sound,      // Unused
            SoundGroup, // Unused
            Transition,
            NumSections = 11
        }
    }
}
