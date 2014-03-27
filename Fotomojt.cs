using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Color = Microsoft.Xna.Framework.Color;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace Fotomojt
{
    public class Fotomojt : Game
    {
        readonly GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        private readonly string dir = @"C:\Users\samil_000\Pictures\";
        private readonly Random random = new Random();
        private readonly TimeSpan timeBetweenImages = new TimeSpan(0, 0, 2);

        private TimeSpan nextImageChange = new TimeSpan(0, 0, 0);
        private string currentFile;

        public Fotomojt(string dir, TimeSpan timeBetweenImages, int port)
        {
            this.dir = dir;
            this.timeBetweenImages = timeBetweenImages;

            graphics = new GraphicsDeviceManager(this);

            httpServer = new HttpListener();
            httpServer.Prefixes.Add(string.Format("http://*:{0}/", port));
            httpServer.Start();
            httpServer.BeginGetContext(OnHttpRequest, httpServer);
        }

        private void OnHttpRequest(IAsyncResult ar)
        {
            var context = httpServer.EndGetContext(ar);

            context.Response.StatusCode = 200;
            context.Response.StatusDescription = "OK";
            context.Response.Headers[HttpResponseHeader.ContentType] = "text/html";
            context.Response.Headers[HttpResponseHeader.ContentEncoding] = "utf-8";

            using (var w = new StreamWriter(context.Response.OutputStream, Encoding.UTF8))
            {
                w.Write(@"<form method=""POST""><input type=""submit"" value=""Nästa bild"" /></form>");   
            }
            context.Response.Close();

            if (context.Request.HttpMethod == "POST")
            {
                nextImageChange = new TimeSpan(0, 0, 0);
            }

            httpServer.BeginGetContext(OnHttpRequest, httpServer);
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            Window.AllowUserResizing = true;
            /*Window.ClientSizeChanged += (sender, args) =>
            {
                var field = typeof(OpenTKGameWindow)
                    .GetField("updateClientBounds", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null)
                    field.SetValue(Window, false);
            };*/

            Console.WriteLine(GraphicsDevice.DisplayMode.Width);
            Console.WriteLine(GraphicsDevice.DisplayMode.Height);
            /*
            graphics.IsFullScreen = true;
            graphics.PreferredBackBufferWidth = GraphicsDevice.DisplayMode.Width;
            graphics.PreferredBackBufferHeight = GraphicsDevice.DisplayMode.Height;
            graphics.ApplyChanges();
            */
            base.Initialize();
        }

        private async Task<Texture2D> LoadNextImage()
        {
            Texture2D img = null;
            while (img == null)
            {
                var files = Directory.EnumerateFiles(dir).ToArray();
                int index;
                try
                {
                    index = (Array.FindIndex(files, s => s == currentFile) + 1) % files.Length;
                }
                catch (ArgumentOutOfRangeException)
                {
                    index = random.Next(files.Length);
                }
                currentFile = files[index];

                Console.WriteLine("Loading: " + currentFile);

                img = await Task.Run(() =>
                {
                    try
                    {
                        Stream imageStream = new FileStream(currentFile, FileMode.Open, FileAccess.Read,
                            FileShare.Read);
                        var i = Image.FromStream(imageStream);

                        var ratio = Math.Min(
                            (float) Window.ClientBounds.Width/i.Width,
                            (float) Window.ClientBounds.Height/i.Height);

                        if (ratio < 1 || (!Equals(i.RawFormat, ImageFormat.Jpeg) && !Equals(i.RawFormat, ImageFormat.Png)))
                        {
                            ratio = Math.Min(ratio, 1f);
                            var b = new Bitmap((int) (i.Width*ratio), (int) (i.Height*ratio));
                            using (var graphicsHandle = Graphics.FromImage(b))
                            {
                                graphicsHandle.InterpolationMode = InterpolationMode.HighQualityBicubic;
                                graphicsHandle.DrawImage(i, 0, 0, b.Width, b.Height);
                            }
                            imageStream = new MemoryStream();
                            b.Save(imageStream, ImageFormat.Png);
                            imageStream.Seek(0, SeekOrigin.Begin);
                        }

                        return Texture2D.FromStream(GraphicsDevice, imageStream);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        return null;
                    }
                });

                if (img == null)
                {
                    await Task.Delay(100);
                }
            }

            Console.WriteLine("Loaded: " + currentFile);

            return img;
        }

        private void ShowNextImage(GameTime gameTime)
        {
            if (nextImage.Status == TaskStatus.RanToCompletion)
            {
                try
                {
                    image = nextImage.Result;
                    nextImageChange = gameTime.TotalGameTime + timeBetweenImages;
                    Console.WriteLine("Displaying: " + currentFile);
                    nextImage = LoadNextImage();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            nextImage = LoadNextImage();
        }

        public Texture2D image { get; set; }
        public Task<Texture2D> nextImage;
        private HttpListener httpServer;

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            var keyboard = Keyboard.GetState(PlayerIndex.One);

            if (keyboard.IsKeyDown(Keys.Escape))
            {
                Exit();
            }

            if (gameTime.TotalGameTime > nextImageChange || keyboard.IsKeyDown(Keys.Space))
            {
                ShowNextImage(gameTime);
            }

            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            if (image != null)
            {
                var ratio = Math.Min((float)Window.ClientBounds.Width / image.Width,
                    (float)Window.ClientBounds.Height / image.Height);

                var rect = new Rectangle(
                    (int)((Window.ClientBounds.Width - image.Width * ratio) / 2.0f),
                    (int)((Window.ClientBounds.Height - image.Height * ratio) / 2.0f),
                    (int)(image.Width * ratio),
                    (int)(image.Height * ratio));

                spriteBatch.Begin();
                spriteBatch.Draw(image, rect, Color.White);
                spriteBatch.End();
            }

            base.Draw(gameTime);
        }
    }
}
