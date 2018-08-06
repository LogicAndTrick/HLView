using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using HLView.Formats.Mdl;
using HLView.Graphics.Primitives;
using Veldrid;
using PixelFormat = System.Drawing.Imaging.PixelFormat;

namespace HLView.Graphics.Renderables
{
    public class MdlRenderable : IRenderable
    {
        public int Order => 2;
        public string Pipeline => "Model";

        private readonly MdlFile _mdl;
        private readonly Vector3 _origin;

        private DeviceBuffer _transformsBuffer;
        private ResourceSet _transformsResourceSet;
        private Matrix4x4[] _transforms;
        private ResourceSet _textureResource;
        private DeviceBuffer _vertexBuffer;
        private DeviceBuffer _indexBuffer;
        private int[] _indicesPerTexture;

        private int _currentSequence;
        private int _currentFrame;
        private long _lastFrameMillis;
        private float _interframePercent;

        public MdlRenderable(MdlFile mdl, Vector3 origin)
        {
            _mdl = mdl;
            _origin = origin;

            _transforms = new Matrix4x4[128];
            for (var i = 0; i < _transforms.Length; i++)
            {
                _transforms[i] = Matrix4x4.Identity;
            }

            _currentSequence = 0;
            _currentFrame = 0;
            _interframePercent = 0;
        }
        
        public void Update(long milliseconds)
        {
            var seq = _mdl.Sequences[_currentSequence];
            var targetFps = 1000 / seq.Framerate;
            var diff = milliseconds - _lastFrameMillis;
            
            _interframePercent += diff / targetFps;
            var skip = (int) _interframePercent;
            _interframePercent -= skip;

            _currentFrame = (_currentFrame + skip) % seq.NumFrames;
            _lastFrameMillis = milliseconds;

            CalculateFrame(_currentFrame, _interframePercent);
        }

        private void CalculateFrame(int currentFrame, float interFramePercent)
        {
            _currentFrame = currentFrame;

            var seq = _mdl.Sequences[_currentSequence];
            var blend = seq.Blends[0];
            var cFrame = blend.Frames[currentFrame % seq.NumFrames];
            var nFrame = blend.Frames[(currentFrame + 1) % seq.NumFrames];

            var indivTransforms = new Matrix4x4[128];
            for (var i = 0; i < _mdl.Bones.Count; i++)
            {
                var bone = _mdl.Bones[i];
                var cPos = bone.Position + cFrame.Positions[i] * bone.PositionScale;
                var nPos = bone.Position + nFrame.Positions[i] * bone.PositionScale;
                var cRot = bone.Rotation + cFrame.Rotations[i] * bone.RotationScale;
                var nRot = bone.Rotation + nFrame.Rotations[i] * bone.RotationScale;

                var cQtn = Quaternion.CreateFromYawPitchRoll(cRot.X, cRot.Y, cRot.Z);
                var nQtn = Quaternion.CreateFromYawPitchRoll(nRot.X, nRot.Y, nRot.Z);

                // MDL angles have Y as the up direction
                cQtn = new Quaternion(cQtn.Y, cQtn.X, cQtn.Z, cQtn.W);
                nQtn = new Quaternion(nQtn.Y, nQtn.X, nQtn.Z, nQtn.W);

                var mat = Matrix4x4.CreateFromQuaternion(Quaternion.Slerp(cQtn, nQtn, interFramePercent));
                mat.Translation = cPos * (1 - interFramePercent) + nPos * interFramePercent;

                indivTransforms[i] = mat;
            }

            for (var i = 0; i < _mdl.Bones.Count; i++)
            {
                var mat = indivTransforms[i];
                var parent = _mdl.Bones[i].Parent;
                while (parent >= 0)
                {
                    var parMat = indivTransforms[parent];
                    mat = mat * parMat;
                    parent = _mdl.Bones[parent].Parent;
                }
                _transforms[i] = mat;
            }
        }

        public float DistanceFrom(Vector3 location)
        {
            return (location - _origin).Length();
        }

        private List<Rectangle> CreateTexuture(SceneContext sc)
        {
            var lmtex = sc.ResourceCache.GetWhiteTexture();
            var lmtv = sc.ResourceCache.GetTextureView(lmtex);

            if (!_mdl.Textures.Any())
            {
                var ptex = sc.ResourceCache.GetPinkTexture();
                var ptv = sc.ResourceCache.GetTextureView(ptex);
                _textureResource = sc.ResourceCache.GetTextureResourceSet(ptv, lmtv);
                return new List<Rectangle>();
            }

            // Combine all the textures into one long texture

            var textures = _mdl.Textures.Select(x => ImageUtilities.CreateBitmap(x.Width, x.Height, x.Data, x.Palette, x.Flags.HasFlag(TextureFlags.Masked))).ToList();

            var width = textures.Max(x => x.Width);
            var height = textures.Sum(x => x.Height);

            var rectangles = new List<Rectangle>();
            
            var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            using (var g = System.Drawing.Graphics.FromImage(bmp))
            {
                var y = 0;
                foreach (var texture in textures)
                {
                    rectangles.Add(new Rectangle(0, y, texture.Width, texture.Height));
                    g.DrawImageUnscaled(texture, 0, y);
                    y += texture.Height;
                }
            }

            textures.ForEach(x => x.Dispose());

            var tex = sc.ResourceCache.GetTexture2D(bmp);
            var tv = sc.ResourceCache.GetTextureView(tex);
            _textureResource = sc.ResourceCache.GetTextureResourceSet(tv, lmtv);

            bmp.Dispose();

            return rectangles;
        }

        public void CreateResources(SceneContext sc)
        {
            // textures = model textures 0 - count
            // vertex buffer = all vertices
            // index buffer = indices grouped by texture, in texture order
            // indices per texture - number of indices per texture, in texture order
            // bone transforms - 128 matrices for the transforms of the current frame
            
            var vertices = new List<ModelVertex>();
            var indices = new Dictionary<short, List<uint>>();
            for (short i = 0; i < _mdl.Textures.Count; i++) indices[i] = new List<uint>();

            var rectangles = CreateTexuture(sc);
            var texHeight = rectangles.Max(x => x.Bottom);
            var texWidth = rectangles.Max(x => x.Right);

            uint vi = 0;
            var skin = _mdl.Skins[0].Textures;
            foreach (var part in _mdl.BodyParts)
            {
                foreach (var model in part.Models)
                {
                    foreach (var mesh in model.Meshes)
                    {
                        var texId = skin[mesh.SkinRef];
                        var rec = rectangles.Count > texId ? rectangles[texId] : Rectangle.Empty;
                        var hoffset = rec.Top / (float) texHeight;
                        var hscale = rec.Height / (float) texHeight;
                        var wscale = rec.Width / (float) texWidth;
                        foreach (var x in mesh.Vertices)
                        {
                            vertices.Add(new ModelVertex
                            {
                                Position = x.Vertex,
                                Normal = x.Normal,
                                Texture = (x.Texture + new Vector2(rec.X, rec.Y)) / new Vector2(texWidth, texHeight),
                                Bone = (uint)x.VertexBone
                            });
                            indices[texId].Add(vi++);
                        }
                    }
                }
            }

            _indicesPerTexture = new int[_mdl.Textures.Count];

            var flatIndices = new uint[vi];
            var currentIndexCount = 0;
            foreach (var kv in indices.OrderBy(x => x.Key))
            {
                var num = kv.Value.Count;
                Array.Copy(kv.Value.ToArray(), 0, flatIndices, currentIndexCount, num);
                currentIndexCount += num;
                _indicesPerTexture[kv.Key] = num;
            }

            var newVerts = vertices.ToArray();

            _vertexBuffer = sc.Device.ResourceFactory.CreateBuffer(new BufferDescription((uint)newVerts.Length * ModelVertex.SizeInBytes, BufferUsage.VertexBuffer));
            _indexBuffer = sc.Device.ResourceFactory.CreateBuffer(new BufferDescription((uint)flatIndices.Length * sizeof(uint), BufferUsage.IndexBuffer));

            sc.Device.UpdateBuffer(_vertexBuffer, 0, newVerts);
            sc.Device.UpdateBuffer(_indexBuffer, 0, flatIndices);

            _transformsBuffer = sc.Device.ResourceFactory.CreateBuffer(
                new BufferDescription((uint)Unsafe.SizeOf<Matrix4x4>() * 128, BufferUsage.UniformBuffer)
            );

            _transformsResourceSet = sc.ResourceCache.GetResourceSet(
                new ResourceSetDescription(sc.ResourceCache.ProjectionLayout, _transformsBuffer)
            );
        }

        public void Render(SceneContext sc, CommandList cl, IRenderContext rc)
        {
            cl.SetGraphicsResourceSet(1, _textureResource);

            sc.Device.UpdateBuffer(_transformsBuffer, 0, _transforms);
            cl.SetGraphicsResourceSet(2, _transformsResourceSet);

            cl.SetVertexBuffer(0, _vertexBuffer);
            cl.SetIndexBuffer(_indexBuffer, IndexFormat.UInt32);

            uint ci = 0;
            for (var i = 0; i < _indicesPerTexture.Length; i++)
            {
                //cl.SetGraphicsResourceSet(1, _textureResources[i]);
                cl.DrawIndexed((uint) _indicesPerTexture[i], 1, ci, 0, 0);
                ci += (uint) _indicesPerTexture[i];
            }
        }

        public void RenderAlpha(SceneContext sc, CommandList cl, IRenderContext rc, Vector3 cameraLocation)
        {
            //
        }

        public void DisposeResources(SceneContext sc)
        {
            _vertexBuffer.Dispose();
            _indexBuffer.Dispose();
        }
    }
}
