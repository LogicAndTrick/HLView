using System.Collections.Generic;
using HLView.Formats.Mdl;

namespace HLView.Graphics.Renderables
{
    public class ModelRenderSettings
    {
        public ModelRenderFlags RenderFlags { get; set; }
        public int Skin { get; set; }
        public int Sequence { get; set; }

        private Dictionary<int, int> _bodyParts;
        private Dictionary<int, float> _controllerValues;

        public ModelRenderSettings(MdlFile mdl)
        {
            RenderFlags = ModelRenderFlags.Model;
            Skin = 0;
            Sequence = 0;

            _bodyParts = new Dictionary<int, int>();
            for (var i = 0; i < mdl.BodyParts.Count; i++) _bodyParts[i] = 0;

            _controllerValues = new Dictionary<int, float>();
            for (var i = 0; i < mdl.BoneControllers.Count; i++) _controllerValues[i] = mdl.BoneControllers[i].Rest;
        }

        public void SetBodyPartModel(int bodyPart, int model)
        {
            _bodyParts[bodyPart] = model;
        }

        public void SetControllerValue(int controller, float value)
        {
            _controllerValues[controller] = value;
        }

        public int GetBodyPartModel(int bodyPart)
        {
            return _bodyParts.ContainsKey(bodyPart) ? _bodyParts[bodyPart] : 0;
        }
    }
}