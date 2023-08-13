using common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace client
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private client _client;

        private Textures _textures;
        private SoundFX _sounds;
        private SpriteFont _font;

        Point fullScreenSize;

        RenderTarget2D tableScreen;
        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            // TODO: Add your initialization logic here

            fullScreenSize = new Point(_graphics.GraphicsDevice.DisplayMode.Width, _graphics.GraphicsDevice.DisplayMode.Height);

            _graphics.PreferredBackBufferHeight = fullScreenSize.Y;
            _graphics.PreferredBackBufferWidth = fullScreenSize.X;
            Window.Position = new Point(
                (GraphicsDevice.DisplayMode.Width - fullScreenSize.X) / 2,
                (GraphicsDevice.DisplayMode.Height - fullScreenSize.Y) / 2
                );
            _graphics.ApplyChanges();

            tableScreen = new RenderTarget2D(
                    GraphicsDevice,
                    320,
                    180,
                    false,
                    SurfaceFormat.Color,
                    DepthFormat.None
                );

            _textures = new Textures();
            _sounds = new SoundFX();
            _font = Content.Load<SpriteFont>("File");

            base.Initialize();

            _client = new client("localhost", 14242, "prez", fullScreenSize, _textures, _sounds, _font);
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            // TODO: use this.Content to load your game content here
            //Texture
            _textures.addTexture("cardmap", Content.Load<Texture2D>("cardsLarge_tilemap_packed"));
            _textures.addTexture("table", Content.Load<Texture2D>("table"));
            _textures.addTexture("pass", Content.Load<Texture2D>("pass"));
            _textures.addTexture("banner", Content.Load<Texture2D>("banner"));
            //Sound

        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            // TODO: Add your update logic here
            _client.tick(true);

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.SetRenderTarget(tableScreen);

            _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise);
            _client.draw(_spriteBatch, GraphicsDevice);
            _spriteBatch.End();

            GraphicsDevice.SetRenderTarget(null);

            _spriteBatch.Begin(SpriteSortMode.Immediate, null, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise);
            _spriteBatch.Draw(
                tableScreen,
                new Rectangle(new Point(0, 0), fullScreenSize),
                Color.White
            );
            _spriteBatch.End();

            // TODO: Add your drawing code here
            base.Draw(gameTime);
        }
    }
}