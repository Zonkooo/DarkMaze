using System;
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
        SpriteBatch spriteBatch;
        private uint[] _pixels;
        private Texture2D _canvas;
        private WaveEngine _engine;
        private const int Width = 200;
        private const int Height = 200;

        public Core()
        {
            graphics = new GraphicsDeviceManager(this);
            graphics.PreferredBackBufferWidth = Width;
            graphics.PreferredBackBufferHeight = Height;
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

            spriteBatch = new SpriteBatch(GraphicsDevice);

            _engine = new WaveEngine(Height);
            var maze = new Maze(100, 100);
            for (int x = 0; x < 100; x++)
            {
                for (int y = 0; y < 100; y++)
                {
                    if ((maze[x, y] & CellState.Right) != 0)
                        _engine.SetWall(x * 2 + 1, y * 2);
                    if ((maze[x, y] & CellState.Bottom) != 0)
                        _engine.SetWall(x * 2, y * 2 + 1);
                    _engine.SetWall(x * 2 + 1, y * 2 + 1);
                }
            }
            _engine.Oscillator1Position = new Point(100, 100);
            _engine.Oscillator1Active = true;


            base.Initialize();
        }
        
        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();
            
            _engine.OneStep(_pixels);
            _canvas.SetData(_pixels, 0, Width * Height);

            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            spriteBatch.Begin();
            spriteBatch.Draw(_canvas, new Rectangle(0, 0, Width, Height), Color.White);
            spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
