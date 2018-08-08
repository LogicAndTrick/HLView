using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Windows.Forms;
using HLView.Formats.Bsp;
using HLView.Formats.Mdl;
using HLView.Graphics;
using HLView.Graphics.Renderables;
using Veldrid;
using Environment = HLView.Formats.Environment.Environment;

namespace HLView.Visualisers
{
    public class BspVisualiser : IVisualiser
    {
        private readonly Panel _panel;
        private GraphicsDevice _graphicsDevice;
        private VeldridControl _view;
        private SceneContext _sc;
        private Scene _scene;

        public Control Container => _panel;
        
        public BspVisualiser()
        {
            _panel = new Panel();
        }

        public bool Supports(string path)
        {
            return Path.GetExtension(path) == ".bsp";
        }

        public void Open(Environment environment, string path)
        {
            var options = new GraphicsDeviceOptions()
            {
                HasMainSwapchain = false,
                ResourceBindingModel = ResourceBindingModel.Improved,
                SwapchainDepthFormat = PixelFormat.R32_Float,
            };

            //_graphicsDevice = GraphicsDevice.CreateVulkan(options);
            _graphicsDevice = GraphicsDevice.CreateD3D11(options);

            _view = new VeldridControl(_graphicsDevice, options)
            {
                Dock = DockStyle.Fill
            };
            _panel.Controls.Add(_view);

            _sc = new SceneContext(_graphicsDevice);
            _sc.AddRenderTarget(_view);

            _scene = new Scene();

            BspFile bsp;
            using (var stream = File.OpenRead(path))
            {
                bsp = new BspFile(stream);
            }

            _scene.AddRenderableSource(new BspRenderable(bsp, environment));

            LoadModels(bsp, environment);


            _sc.Scene = _scene;
            _sc.Start();
        }

        private void LoadModels(BspFile bsp, Environment env)
        {
            var loadedModels = new Dictionary<string, MdlFile>(StringComparer.InvariantCultureIgnoreCase);
            foreach (var ent in bsp.Entities.Where(x => EntityModelMap.ContainsKey(x.ClassName)))
            {
                var model = EntityModelMap[ent.ClassName];

                if (model == "")
                {
                    // Use model from the entity
                    if (!ent.KeyValues.ContainsKey("model")) continue;
                    model = ent.KeyValues["model"];
                }

                if (ent.KeyValues.ContainsKey("targetname"))
                {
                    var targetters = bsp.Entities.Where(x => x.KeyValues.ContainsKey("target") && x.KeyValues["target"] == ent.KeyValues["targetname"]).ToList();
                }

                var file = env.GetFile(model);
                if (file != null)
                {
                    file = file.Replace('/', '\\'); // normalise path
                    try
                    {
                        MdlFile mdl;
                        if (loadedModels.ContainsKey(file))
                        {
                            mdl = loadedModels[file];
                        }
                        else
                        {
                            mdl = MdlFile.FromFile(file);
                            loadedModels[file] = mdl;
                        }

                        _scene.AddRenderable(new MdlRenderable(mdl, ent.GetVector3("origin", Vector3.Zero)));
                    }
                    catch
                    {
                        continue;
                    }
                }
            }
        }

        public void Close()
        {
            _sc.RemoveRenderTarget(_view);

            _sc.Stop();
            _sc.Dispose();

            _panel.Controls.Clear();
            _view.Dispose();

            _graphicsDevice.Dispose();

            _scene = null;
            _sc = null;
            _view = null;
            _graphicsDevice = null;
        }

        private static readonly Dictionary<string, string> EntityModelMap = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase)
        {
            {"ammo_9mmclip", "models/w_9mmclip.mdl"},
            {"ammo_9mmAR", "models/w_9mmarclip.mdl"},
            {"ammo_9mmbox", "models/w_chainammo.mdl"},
            {"ammo_ARgrenades", "models/w_argrenade.mdl"},
            {"ammo_buckshot", "models/w_shotshell.mdl"},
            {"ammo_357", "models/w_357ammobox.mdl"},
            {"ammo_rpgclip", "models/w_rpgammo.mdl"},
            {"ammo_gaussclip", "models/w_gaussammo.mdl"},
            {"ammo_crossbow", "models/w_crossbow_clip.mdl"},
            {"info_player_deathmatch", "models/player.mdl"},
            {"info_player_start", "models/player.mdl"},
            {"item_airtank", "models/w_oxygen.mdl"},
            {"item_antidote", "models/w_antidote.mdl"},
            {"item_battery", "models/w_battery.mdl"},
            {"item_healthkit", "models/w_medkit.mdl"},
            {"item_longjump", "models/w_longjump.mdl"},
            {"item_security", "models/w_security.mdl"},
            {"item_suit", "models/w_suit.mdl"},
            {"monster_alien_controller", "models/controller.mdl"},
            {"monster_alien_grunt", "models/agrunt.mdl"},
            {"monster_alien_slave", "models/islave.mdl"},
            {"monster_apache", "models/apache.mdl"},
            {"monster_babycrab", "models/baby_headcrab.mdl"},
            {"monster_barnacle", "models/barnacle.mdl"},
            {"monster_barney", "models/barney.mdl"},
            {"monster_bigmomma", "models/big_mom.mdl"},
            {"monster_bloater", "models/floater.mdl"},
            {"monster_bullchicken", "models/bullsquid.mdl"},
            {"monster_cockroach", "models/roach.mdl"},
            {"monster_flyer_flock", "models/aflock.mdl"},
            {"monster_furniture", ""},
            {"monster_gargantua", "models/garg.mdl"},
            {"monster_generic", ""},
            {"monster_gman", "models/gman.mdl"},
            {"monster_grunt_repel", "models/hgrunt.mdl"},
            {"monster_handgrenade", "models/w_grenade.mdl"},
            {"monster_headcrab", "models/headcrab.mdl"},
            {"monster_hgrunt_dead", "models/hgrunt.mdl"},
            {"monster_houndeye", "models/houndeye.mdl"},
            {"monster_human_assassin", "models/hassassin.mdl"},
            {"monster_human_grunt", "models/hgrunt.mdl"},
            {"monster_ichthyosaur", "models/icky.mdl"},
            {"monster_leech", "models/leech.mdl"},
            {"monster_miniturret", "models/miniturret.mdl"},
            {"monster_nihilanth", "models/nihilanth.mdl"},
            {"monster_osprey", "models/osprey.mdl"},
            {"monster_rat", "models/bigrat.mdl"},
            {"monster_satchelcharge", "models/w_satchel.mdl"},
            {"monster_scientist", "models/scientist.mdl"},
            {"monster_scientist_dead", "models/scientist.mdl"},
            {"monster_sitting_scientist", "models/scientist.mdl"},
            {"monster_sentry", "models/sentry.mdl"},
            {"monster_snark", "models/w_squeak.mdl"},
            {"monster_tentacle", "models/tentacle2.mdl"},
            {"monster_turret", "models/turret.mdl"},
            {"monster_zombie", "models/zombie.mdl"},
            {"weapon_crowbar", "models/w_crowbar.mdl"},
            {"weapon_9mmhandgun", "models/w_9mmhandgun.mdl"},
            {"weapon_357", "models/w_357.mdl"},
            {"weapon_9mmAR", "models/w_9mmar.mdl"},
            {"weapon_shotgun", "models/w_shotgun.mdl"},
            {"weapon_rpg", "models/w_rpg.mdl"},
            {"weapon_gauss", "models/w_gauss.mdl"},
            {"weapon_crossbow", "models/w_crossbow.mdl"},
            {"weapon_egon", "models/w_egon.mdl"},
            {"weapon_satchel", "models/w_satchel.mdl"},
            {"weapon_handgrenade", "models/w_grenade.mdl"},
            {"weapon_snark", "models/w_squeak.mdl"},
            {"weapon_hornetgun", "models/w_hgun.mdl"},
            {"weaponbox", "models/w_chainammo.mdl"},
            {"xen_plantlight", "models/light.mdl"},
            {"xen_hair", "models/hair.mdl"},
            {"xen_tree", "models/tree.mdl"},
            {"xen_spore_small", "models/fungus(small).mdl"},
            {"xen_spore_medium", "models/fungus.mdl"},
            {"xen_spore_large", "models/fungus(large).mdl"},

            {"cycler", ""},
            {"cycler_weapon", ""},
            {"cycler_wreckage", ""},
            {"cycler_sprite", ""},
            {"env_sprite", ""},
            {"env_glow", ""}
        };
    }
}
