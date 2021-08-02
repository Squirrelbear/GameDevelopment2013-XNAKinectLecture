using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Microsoft.Kinect;

namespace KinectDemo
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        KinectSensor myKinect;
        Texture2D kinectVideoTexture;
        Texture2D kinectDepthTexture;
        Texture2D lineDot;
        Texture2D face;
        SpriteFont feedback;
        Rectangle videoDisplayRectangle;

        int headX, headY;

        byte[] colorData = null;
        short[] depthData = null;
        Skeleton[] skeletons = null;

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            graphics.IsFullScreen = false;
            graphics.PreferredBackBufferWidth = 640;
            graphics.PreferredBackBufferHeight = 480;
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
            // TODO: Add your initialization logic here
            videoDisplayRectangle = new Rectangle(0, 0, 640,480);//GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);

            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            // TODO: use this.Content to load your game content here
            myKinect = KinectSensor.KinectSensors[0];
            myKinect.ColorStream.Enable();
            myKinect.DepthStream.Enable();
            myKinect.SkeletonStream.Enable();
            myKinect.ColorFrameReady += new EventHandler<ColorImageFrameReadyEventArgs>(myKinect_ColorFrameReady);
            myKinect.DepthFrameReady += new EventHandler<DepthImageFrameReadyEventArgs>(myKinect_DepthFrameReady);
            myKinect.SkeletonFrameReady += new EventHandler<SkeletonFrameReadyEventArgs>(myKinect_SkeletonFrameReady);

            lineDot = new Texture2D(GraphicsDevice, 4, 4);
            lineDot.SetData(new[] { Color.White, Color.White, Color.White, Color.White,
                                    Color.White, Color.White, Color.White, Color.White,
                                    Color.White, Color.White, Color.White, Color.White,
                                    Color.White, Color.White, Color.White, Color.White});
            face = Content.Load<Texture2D>("Images/face");
            feedback = Content.Load<SpriteFont>("Feedback");
            myKinect.Start();
        }
        
        void myKinect_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            using (SkeletonFrame frame = e.OpenSkeletonFrame())
            {
                if (frame != null)
                {
                    skeletons = new Skeleton[frame.SkeletonArrayLength];
                    frame.CopySkeletonDataTo(skeletons);

                    foreach (Skeleton s in skeletons)
                    {
                        if (s.TrackingState == SkeletonTrackingState.Tracked)
                        {
                            Joint Head = s.Joints[JointType.Head];
                            CoordinateMapper coordinateMap = new CoordinateMapper(myKinect);
                            ColorImagePoint hPoint =  coordinateMap.MapSkeletonPointToColorPoint(Head.Position, ColorImageFormat.RgbResolution640x480Fps30);
                            headX = hPoint.X-42;
                            headY = hPoint.Y-80;
                        }
                    }
                }
            }
        }

        void myKinect_DepthFrameReady(object sender, DepthImageFrameReadyEventArgs e)
        {
            using (DepthImageFrame depthFrame = e.OpenDepthImageFrame())
            {
                if (depthFrame == null)
                    return;

                if (depthData == null)
                    depthData = new short[depthFrame.Width * depthFrame.Height];

                depthFrame.CopyPixelDataTo(depthData);

                Color[] bitmap = new Color[depthFrame.Width * depthFrame.Height];

                for (int i = 0; i < bitmap.Length; i++)
                {
                    int depth = depthData[i] >> 3;
                    if (depth == myKinect.DepthStream.UnknownDepth)
                        bitmap[i] = Color.Red;
                    else
                        if (depth == myKinect.DepthStream.TooFarDepth)
                            bitmap[i] = Color.Blue;
                        else
                            if (depth == myKinect.DepthStream.TooNearDepth)
                                bitmap[i] = Color.Green;
                            else
                            {
                                byte depthByte = (byte)(255 - (depth >> 5));
                                bitmap[i] = new Color(depthByte, depthByte, depthByte, 255);
                            }
                }
                kinectDepthTexture = new Texture2D(GraphicsDevice, depthFrame.Width, depthFrame.Height);
                kinectDepthTexture.SetData(bitmap);
            }
        }

        void myKinect_ColorFrameReady(object sender, ColorImageFrameReadyEventArgs e)
        {
            using (ColorImageFrame colorFrame = e.OpenColorImageFrame())
            {
                if (colorFrame == null)
                    return;

                if (colorData == null)
                    colorData = new byte[colorFrame.Width * colorFrame.Height * 4];

                colorFrame.CopyPixelDataTo(colorData);

                kinectVideoTexture = new Texture2D(GraphicsDevice, colorFrame.Width, colorFrame.Height);

                Color[] bitmap = new Color[colorFrame.Width * colorFrame.Height];

                int sourceOffset = 0;

                for (int i = 0; i < bitmap.Length; i++)
                {
                    bitmap[i] = new Color(  colorData[sourceOffset + 2],
                                            colorData[sourceOffset + 1],
                                            colorData[sourceOffset],
                                            255);
                    sourceOffset += 4;
                }
                kinectVideoTexture.SetData(bitmap);
            }
        }

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
            // Allows the game to exit
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
                this.Exit();

            // TODO: Add your update logic here

            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            // TODO: Add your drawing code here
            spriteBatch.Begin();
            if (kinectVideoTexture != null)
                spriteBatch.Draw(kinectVideoTexture, videoDisplayRectangle, Color.White);

            if (kinectDepthTexture != null)
                spriteBatch.Draw(kinectDepthTexture, videoDisplayRectangle, Color.White);

            if (skeletons != null)
            {
                foreach (Skeleton s in skeletons)
                    if (s.TrackingState == SkeletonTrackingState.Tracked)
                    {
                        drawSkeleton(s, Color.White);
                    }

                spriteBatch.Draw(face, new Vector2(headX, headY), Color.White);
            }
   
            spriteBatch.End();

            base.Draw(gameTime);
        }

        private void drawSkeleton(Skeleton s, Color color)
        {
            drawBone(s.Joints[JointType.Head], s.Joints[JointType.ShoulderCenter], color);
            drawBone(s.Joints[JointType.ShoulderCenter], s.Joints[JointType.Spine], color);

            drawBone(s.Joints[JointType.Spine], s.Joints[JointType.HipCenter], color);
            drawBone(s.Joints[JointType.HipCenter], s.Joints[JointType.HipLeft], color);
            drawBone(s.Joints[JointType.HipLeft], s.Joints[JointType.KneeLeft], color);
            drawBone(s.Joints[JointType.KneeLeft], s.Joints[JointType.AnkleLeft], color);
            drawBone(s.Joints[JointType.AnkleLeft], s.Joints[JointType.FootLeft], color);

            drawBone(s.Joints[JointType.HipCenter], s.Joints[JointType.HipRight], color);
            drawBone(s.Joints[JointType.HipRight], s.Joints[JointType.KneeRight], color);
            drawBone(s.Joints[JointType.KneeRight], s.Joints[JointType.AnkleRight], color);
            drawBone(s.Joints[JointType.AnkleRight], s.Joints[JointType.FootRight], color);

            drawBone(s.Joints[JointType.ShoulderCenter], s.Joints[JointType.ShoulderLeft], color);
            drawBone(s.Joints[JointType.ShoulderLeft], s.Joints[JointType.ElbowLeft], color);
            drawBone(s.Joints[JointType.ElbowLeft], s.Joints[JointType.WristLeft], color);
            drawBone(s.Joints[JointType.WristLeft], s.Joints[JointType.HandLeft], color);

            drawBone(s.Joints[JointType.ShoulderCenter], s.Joints[JointType.ShoulderRight], color);
            drawBone(s.Joints[JointType.ShoulderRight], s.Joints[JointType.ElbowRight], color);
            drawBone(s.Joints[JointType.ElbowRight], s.Joints[JointType.WristRight], color);
            drawBone(s.Joints[JointType.WristRight], s.Joints[JointType.HandRight], color);
        }

        private void drawBone(Joint j1, Joint j2, Color color)
        {
            CoordinateMapper coordinateMap = new CoordinateMapper(myKinect);

            ColorImagePoint j1P = coordinateMap.MapSkeletonPointToColorPoint(j1.Position, ColorImageFormat.RgbResolution640x480Fps30);
            Vector2 j1V = new Vector2(j1P.X, j1P.Y);

            ColorImagePoint j2P = coordinateMap.MapSkeletonPointToColorPoint(j2.Position, ColorImageFormat.RgbResolution640x480Fps30);
            Vector2 j2V = new Vector2(j2P.X, j2P.Y);

            drawLine(j1V, j2V, color);
        }
        
        private void drawLine(Vector2 v1, Vector2 v2, Color color)
        {
            Vector2 diff = v2 - v1;
            Vector2 scale = new Vector2(1.0f, diff.Length() / lineDot.Height);
            float angle = (float)(Math.Atan2(diff.Y, diff.X)) - MathHelper.PiOver2;

            Vector2 origin = new Vector2(0.5f, 0.0f);
            spriteBatch.Draw(lineDot, v1, null, color, angle, origin, scale, SpriteEffects.None, 1.0f);
        }
    }
}
