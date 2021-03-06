﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using FimbulwinterClient.Core.Assets.MapInternals;

namespace FimbulwinterClient.Core.Assets
{
    public class Map
    {
        private Ground _ground;
        public Ground Ground
        {
            get { return _ground; }
        }

        private Altitude _altitude;
        public Altitude Altitude
        {
            get { return _altitude; }
        }

        private World _world;
        public World World
        {
            get { return _world; }
        }

        private Texture2D _lightmap;
        public Texture2D Lightmap
        {
            get { return _lightmap; }
        }

        private Effect _effect;
        public Effect Effect
        {
            get { return _effect; }
        }

        private GraphicsDevice _graphicsDevice;
        public GraphicsDevice GraphicsDevice
        {
            get { return _graphicsDevice; }
        }

        public Map(GraphicsDevice gd)
        {
            _graphicsDevice = gd;

            _effect = SharedInformation.ContentManager.Load<Effect>(@"fb\Ragnarok.xnb");
            _effect.CurrentTechnique = _effect.Techniques["MapGround"];
        }

        public bool Load(Stream gat, Stream gnd, Stream rsw)
        {
            Logger.WriteLine("Loading altitude...");
            _altitude = new Altitude();
            if (!_altitude.Load(gat))
                return false;

            Logger.WriteLine("Loading ground...");
            _ground = new Ground(_graphicsDevice);
            if (!_ground.Load(gnd))
                return false;

            Logger.WriteLine("Loading world...");
            _world = new World(_graphicsDevice);
            if (!_world.Load(rsw, this))
                return false;

            Logger.WriteLine("Creating ground vertex buffer...");
            _ground.SetupVertices();

            Logger.WriteLine("Building lightmaps...");
            BuildLightmaps();

            _effect.Parameters["Lightmap"].SetValue(_lightmap);

            _effect.Parameters["AmbientColor"].SetValue(new Vector3(1, 1, 1));
            _effect.Parameters["DiffuseColor"].SetValue(new Vector3(0, 0, 0));

            // FIXME: Where I put the light? O_O
            _effect.Parameters["LightPosition"].SetValue(new Vector3(0, 0, 0));

            return true;
        }

        private void BuildLightmaps()
        {
            // Nothing to be built
            if (_ground.Lightmaps.Length == 0)
                return;

            int w = (int)Math.Floor(Math.Sqrt(_ground.Lightmaps.Length));
            int h = (int)Math.Ceiling((float)_ground.Lightmaps.Length / w);

            Color[] color = new Color[8 * 8 * w * h];

            int x = 0, y = 0;
            for (int i = 0; i < _ground.Lightmaps.Length; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    int offset = y * w * 8 * 8 + j * w * 8 + x * 8;

                    for (int n = 0; n < 8; n++)
                    {
                        color[offset + n] = _ground.Lightmaps[i].Intensity[j * 8 + n];
                        color[offset + n].A = _ground.Lightmaps[i].Brightness[j * 8 + n];
                    }
                }

                y++;
                if (y >= h)
                {
                    y = 0;
                    x++;
                }
            }

            _lightmap = new Texture2D(_graphicsDevice, w * 8, h * 8, false, SurfaceFormat.Color);
            _lightmap.SetData(color);
        }

        public void Update(GameTime gametime)
        {
            _world.UpdateWater(gametime);
            _world.UpdateModels(gametime);
        }

        BasicEffect eff2 = new BasicEffect(SharedInformation.GraphicsDevice);
        public void Draw(GameTime gametime, Matrix view, Matrix projection, Matrix world)
        {
            _effect.Parameters["View"].SetValue(view);
            _effect.Parameters["Projection"].SetValue(projection);
            _effect.Parameters["World"].SetValue(world);

            _effect.CurrentTechnique = _effect.Techniques["MapGround"];
            _ground.Draw(_effect);

            _effect.CurrentTechnique = _effect.Techniques["Water"];
            _world.DrawWater(_effect);

            _effect.CurrentTechnique = _effect.Techniques["Model"];
            _world.DrawModels(_effect);
        }

        public struct VertexPositionColorNormal
        {
            public Vector3 Position;
            public Color Color;
            public Vector3 Normal;

            public readonly static VertexDeclaration VertexDeclaration = new VertexDeclaration
            (
                new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
                new VertexElement(sizeof(float) * 3, VertexElementFormat.Color, VertexElementUsage.Color, 0),
                new VertexElement(sizeof(float) * 3 + 4, VertexElementFormat.Vector3, VertexElementUsage.Normal, 0)
            );
        }

    }
}
