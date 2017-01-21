using System;
using System.Collections.Generic;
using System.Diagnostics;
using MazeGeneration;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using WaveSimulator;

namespace Darkmaze
{
    /// <summary>
    /// This is the main type for your game.
    /// </summary>
    public class Core : Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch _spriteBatch;
        private uint[] _pixels;
        private Texture2D _canvas;
        private WaveEngine _engine;
        private List<Enemy> _enemies = new List<Enemy>();

        private const int Width = 30*10;
        private const int Height = 30*10;

        public Core()
        {
            graphics = new GraphicsDeviceManager(this);
            graphics.PreferredBackBufferWidth = Width*2;
            graphics.PreferredBackBufferHeight = Height*2;
            Content.RootDirectory = "Content";
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            _canvas = new Texture2D(GraphicsDevice, Width, Height, false, SurfaceFormat.Color);
            _pixels = new uint[Width * Height];

            _spriteBatch = new SpriteBatch(GraphicsDevice);

            _engine = new WaveEngine(Height);
            var mfactor = 30;
            var msize = Height / mfactor;
            if(Height%mfactor != 0)
                Debugger.Break();
            var maze = new Maze(msize, msize);
            for (int x = 0; x < msize; x++)
            {
                for (int y = 0; y < msize; y++)
                {
                    if ((maze[x, y] & CellState.Right) != 0)
                    {
                        for (int i = 0; i < mfactor - 1; i++)
                            _engine.SetWall(x * mfactor + mfactor - 1, y * mfactor + i);
                    }
                    if ((maze[x, y] & CellState.Bottom) != 0)
                    {
                        for (int i = 0; i < mfactor - 1; i++)
                            _engine.SetWall(x * mfactor + i, y * mfactor + mfactor - 1);
                    }
                    
                    _engine.SetWall(x * mfactor + mfactor - 1, y * mfactor + mfactor - 1);
                }
            }
            _engine.Oscillator1Position = new Point(Width/2, Height/2);

            var rand = new Random();
            for (int i = 0; i < 10; i++)
            {
                Point position;
                do
                {
                    position = new Point {X = rand.Next(Width), Y = rand.Next(Height)};
                } while (_engine.IsWall(position.X, position.Y));

                _enemies.Add(new Enemy {Position = position.ToVector2()});
            }

            base.Initialize();
        }

        private KeyboardState _prevState = new KeyboardState();
        private int _waveCoolDown;
        private Point _source;
        private bool _dead;

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            var keys = Keyboard.GetState();
            if (!_dead)
            {
                var pos = _engine.Oscillator1Position;
                if (keys.IsKeyDown(Keys.Up) && !_engine.IsWall(pos.X, pos.Y - 1))
                    _engine.Oscillator1Position = new Point(pos.X, pos.Y - 1);
                if (keys.IsKeyDown(Keys.Down) && !_engine.IsWall(pos.X, pos.Y + 1))
                    _engine.Oscillator1Position = new Point(pos.X, pos.Y + 1);
                if (keys.IsKeyDown(Keys.Left) && !_engine.IsWall(pos.X - 1, pos.Y))
                    _engine.Oscillator1Position = new Point(pos.X - 1, pos.Y);
                if (keys.IsKeyDown(Keys.Right) && !_engine.IsWall(pos.X + 1, pos.Y))
                    _engine.Oscillator1Position = new Point(pos.X + 1, pos.Y);

                if (keys.IsKeyDown(Keys.Space) && _prevState.IsKeyDown(Keys.Space) && !_engine.Oscillator1Active)
                {
                    _source = _engine.Oscillator1Position;
                    _engine.Oscillator1Active = true;
                    _engine.phase1 = 0f;
                    _waveCoolDown = (int) (MathHelper.TwoPi / _engine.PhaseRate1); //do one full oscilation
                }
                if (_waveCoolDown-- == 0)
                {
                    _engine.Oscillator1Active = false;
                }

                foreach (var enemy in _enemies)
                {
                    if (enemy.KillsOnPosition(_engine.Oscillator1Position.ToVector2()))
                        _dead = true;
                }
            }

            foreach (var enemy in _enemies)
            {
                //update enemies
                if (!enemy.Active && Math.Abs(_engine.GetAmplitude((int) enemy.Position.X, (int) enemy.Position.Y)) > 0.02f)
                {
                    //jump on player
                    enemy.Attack(_source);
                }
                enemy.Update();
            }

            _engine.OneStep(_pixels);
            _canvas.SetData(_pixels, 0, Width * Height);

            _prevState = keys;
            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            _spriteBatch.Begin();
            
            _spriteBatch.Draw(_canvas, new Rectangle(0, 0, Width*2, Height*2), Color.White);
            
            foreach (var enemy in _enemies)
                enemy.Draw(_spriteBatch);

            _spriteBatch.End();

            if(_dead)
                GraphicsDevice.Clear(Color.Red);

            base.Draw(gameTime);
        }
    }
}
