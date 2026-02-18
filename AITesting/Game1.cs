using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading;

using Vector2 = Microsoft.Xna.Framework.Vector2;
using Vector3 = Microsoft.Xna.Framework.Vector3;
using Vector4 = Microsoft.Xna.Framework.Vector4;
namespace AITesting
{

    /* To do:
     * =Loss function was hitting zero, yet it was unable to deal with the targets in any way at all
     * Further: It was saying that the loss function was 0, but there was a distinct 3 value difference
     * 
     * I'm going to "learn" after every gradient descent instead of after cummulating all the values. Hopefully it'll make the changes more accurate
     * Just because you could get stuck over and under estimated in equal portions and it would just say "No changes" and all that
     * 
     * Multiply reward by absolute velocity - prioritise speed
     * pass in a relative location to the target along with the target's location
     * 
     * The target moves to a spot in the opposite direction to the Ai's current direction, in increasing distances
     * 
     * 
     * 
     * New Attempt: Genetic evolution:
     *      Instead of using error functions to back propigate error, just use a large quantity of agents over a set period of time, take the agents that had the best cummulative score at the end of the epoc, slightly modify them
     *      And re-run the testing. It should prevent all of the back propigation errors, and local minima I hope
     * 
     */
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        SpriteFont ariel;
        SpriteFont arielSmall;

        private Matrix worldMatrix, viewMatrix, projectionMatrix;
        private BasicEffect basicEffect;


        WorldContext worldContext;

        Texture2D entityTexture;
        Texture2D blockTextures;

        Texture2D collisionSprite;
        Texture2D redTexture;

        Timer t;
        bool resetList = false;

        public List<Target> path = new List<Target>();

        double timeSpeedupConstant = 9;

        double viewModeCooldown = 0;
        double maxViewModeCooldown = 1;
        bool viewGraph = false;

        int tickCount = 0;
        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;

            worldContext = new WorldContext();

            this.IsFixedTimeStep = true;
            this.TargetElapsedTime = TimeSpan.FromSeconds(1/360d);

            t = new Timer(mouseTimerCallback);
            t.Change(0, 300);
        }

        public void mouseTimerCallback(object timerState) {
            if (Mouse.GetState().LeftButton == ButtonState.Pressed) {
                if (resetList) {
                    path.Clear();
                    resetList = false;
                }
                Target target = new Target();
                target.x = Mouse.GetState().X;
                target.y = Mouse.GetState().Y;
                path.Add(target);
            }
        }

        protected override void Initialize()
        {
            // TODO: Add your initialization logic here

            worldMatrix = Matrix.Identity;
            viewMatrix = Matrix.CreateLookAt(new Vector3(0, 0, 1), Vector3.Zero, Vector3.Up);

            projectionMatrix = Matrix.CreateOrthographicOffCenter(0, 1, 1, 0, 0, 1);

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            entityTexture = new Texture2D(GraphicsDevice, 1, 1);
            entityTexture.SetData<Color>(new Color[] {Color.Black});

            basicEffect = new BasicEffect(_graphics.GraphicsDevice);
            basicEffect.World = worldMatrix;
            basicEffect.View = viewMatrix;
            basicEffect.Projection = projectionMatrix;

            basicEffect.VertexColorEnabled = true;
            basicEffect.LightingEnabled = false;

            collisionSprite = new Texture2D(GraphicsDevice, 1, 1);
            collisionSprite.SetData<Color>(new Color[] {Color.Green});

            redTexture = new Texture2D(GraphicsDevice, 1, 1);
            redTexture.SetData<Color>(new Color[] { Color.Red });

            blockTextures = Texture2D.FromFile(GraphicsDevice, AppDomain.CurrentDomain.BaseDirectory + "Content\\blockSpriteSheet.png") ;

            ariel = Content.Load<SpriteFont>("Ariel");
            arielSmall = Content.Load<SpriteFont>("ArielSmall");
            // TODO: use this.Content to load your game content here
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();
            updatePath();
            updateEntity(gameTime);
            updatePhysicsObjects(gameTime);
            updateViewMode(gameTime);

            base.Update(gameTime);
        }

        public void updatePath() {
            if (Mouse.GetState().LeftButton == ButtonState.Released && path.Count > 0) {
                resetList = true;
            }
        }
        public void updateViewMode(GameTime gameTime) {
            if (viewModeCooldown > 0) {
                viewModeCooldown -= gameTime.ElapsedGameTime.TotalSeconds;
            } else
            {
                if (Keyboard.GetState().IsKeyDown(Keys.M)) {
                    viewGraph = !viewGraph;
                    viewModeCooldown = maxViewModeCooldown;
                }

            }
        }

        public void updatePhysicsObjects(GameTime gameTime)
        {
            EngineController engineController = worldContext.engineController;
            for (int i = 0; i < worldContext.physicsObjects.Count; i++)
            {
                
                //General Physics simulations
                //Order: Acceleration, velocity then location
                if (worldContext.physicsObjects[i].calculatePhysics)
                {
                    worldContext.physicsObjects[i].isOnGround = false;

                    engineController.physicsEngine.addGravity(worldContext.physicsObjects[i]);
                    engineController.physicsEngine.computeAccelerationWithAirResistance(worldContext.physicsObjects[i], gameTime.ElapsedGameTime.TotalSeconds * timeSpeedupConstant);

                    engineController.physicsEngine.detectBlockCollisions(worldContext.physicsObjects[i]);
                    engineController.physicsEngine.computeAccelerationToVelocity(worldContext.physicsObjects[i], gameTime.ElapsedGameTime.TotalSeconds * timeSpeedupConstant);
                    engineController.physicsEngine.applyVelocityToPosition(worldContext.physicsObjects[i], gameTime.ElapsedGameTime.TotalSeconds * timeSpeedupConstant);


                    //Reset acceleration to be calculated next frame
                    worldContext.physicsObjects[i].accelerationX = 0;
                    worldContext.physicsObjects[i].accelerationY = 0;
                }
            }

        }

        public void updateEntity(GameTime gameTime) {
                worldContext.controlledEntity.onInput(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);
            if (viewGraph)
            {
                drawGraph();
            }

            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            if (!viewGraph)
            {
                drawBlocks();
                drawEntities();
            }
            if (viewGraph)
            {
                drawRewards();
            }
            //drawCollisionBox();
            _spriteBatch.End();


            base.Draw(gameTime);
        }
        public void drawBlocks()
        {
            
            for (int x = ((int)-worldContext.screenSpaceOffset.x) / worldContext.pixelsPerBlock - 1; x < ((int)-worldContext.screenSpaceOffset.x + _graphics.PreferredBackBufferWidth) / worldContext.pixelsPerBlock + 1; x++)
            {
                for (int y = ((int)-worldContext.screenSpaceOffset.y) / worldContext.pixelsPerBlock - 1; y < ((int)-worldContext.screenSpaceOffset.y + _graphics.PreferredBackBufferHeight) / worldContext.pixelsPerBlock + 1; y++)
                {
                    if (x >= 0 && y >= 0 && x < worldContext.worldArray.GetLength(0) && y < worldContext.worldArray.GetLength(1))
                    {
                        
                        
                            
                            Color lightLevel = Color.White;
                            
                            _spriteBatch.Draw(blockTextures, new Rectangle(x * worldContext.pixelsPerBlock + worldContext.screenSpaceOffset.x, y * worldContext.pixelsPerBlock + worldContext.screenSpaceOffset.y, (int)worldContext.pixelsPerBlock, (int)worldContext.pixelsPerBlock), worldContext.worldArray[x, y].sourceRectangle, lightLevel);
                        

                    }
                }
            }
        }


        public void drawEntities()
        {
               
            for (int i = 0; i < worldContext.physicsObjects.Count; i++)
            {
                if (worldContext.physicsObjects[i] != null)
                {
                        PhysicsObject entity = worldContext.physicsObjects[i];
                        _spriteBatch.Draw(entityTexture, new Rectangle((int)(entity.x + worldContext.screenSpaceOffset.x), (int)(entity.y + worldContext.screenSpaceOffset.y), (int)(entity.drawWidth * worldContext.pixelsPerBlock), (int)(entity.drawHeight * worldContext.pixelsPerBlock)), Color.White);

                    if (entity is AiEntity a)
                    {
                        _spriteBatch.Draw(collisionSprite, new Rectangle((int)a.t.x, (int)a.t.y, 10, 10), Color.White);
                        
                    }
                }
            }

            //draw the epsilon value:
            if (worldContext.physicsObjects[0] is AiEntity firstEntity) {
                _spriteBatch.DrawString(ariel, (Math.Truncate(firstEntity.greedyEpsilon * 100) / 100).ToString(), new Vector2(36,0), Color.Blue);
                _spriteBatch.DrawString(ariel, (Math.Truncate((firstEntity.reward) * 100) / 100).ToString() + " | " + firstEntity.hitTargetReward, new Vector2(36, 50), Color.Blue);
                _spriteBatch.DrawString(ariel, (Math.Truncate(firstEntity.maxEstimatedReward * 100) / 100).ToString() + " | " + (Math.Sign(firstEntity.maxEstimatedReward) == Math.Sign(firstEntity.reward)), new Vector2(36,70), Color.Blue);
                _spriteBatch.DrawString(ariel, firstEntity.actionIndex.ToString(), new Vector2(36, 90), Color.Blue);
                _spriteBatch.DrawString(ariel, firstEntity.samples.Count.ToString(), new Vector2(36, 110), Color.Blue);

                if (firstEntity.samples.Count > 0)
                {
                    for (int i = 0; i < firstEntity.samples[firstEntity.samples.Count - 1].output.GetLength(1); i++)
                    {
                        _spriteBatch.DrawString(ariel, (Math.Truncate(firstEntity.samples[firstEntity.samples.Count - 1].output[0,i] * 100) / 100).ToString(), new Vector2(100 + 80 * i, 0), Color.Blue);
                    }
                }


                _spriteBatch.DrawString(ariel, firstEntity.era.ToString(), new Vector2(_graphics.PreferredBackBufferWidth - 32, 0), Color.Blue);

            }
        }

        

        public void drawCollisionBox()
        {
            //A version of the collision code. It runs the same basic collision detection system, 
            //but paints a red outline around the blocks that were tested, and colors the blocks that the player is colliding
            //with in green.
            int entityLocationInGridX = (int)Math.Floor(worldContext.physicsObjects[0].x / worldContext.pixelsPerBlock);
            int entityLocationInGridY = (int)Math.Floor(worldContext.physicsObjects[0].y / worldContext.pixelsPerBlock);
            int entityGridWidth = (int)Math.Ceiling((double)worldContext.physicsObjects[0].collider.Width / worldContext.pixelsPerBlock);
            int entityGridHeight = (int)Math.Ceiling((double)worldContext.physicsObjects[0].collider.Height / worldContext.pixelsPerBlock);
            int p = worldContext.pixelsPerBlock;
            for (int x = entityLocationInGridX - 1; x < entityLocationInGridX + entityGridWidth + 1; x++)
            { //A range of x values on either side of the outer bounds of the entity
                for (int y = entityLocationInGridY - 1; y < entityLocationInGridY + entityGridHeight + 1; y++)
                {
                    Rectangle entityCollider = new Rectangle((int)worldContext.physicsObjects[0].x, (int)worldContext.physicsObjects[0].y, worldContext.physicsObjects[0].collider.Width, worldContext.physicsObjects[0].collider.Height);

                    Rectangle blockRect = new Rectangle(x * p, y * p, p, p);
                    if (blockRect.Intersects(entityCollider) && worldContext.worldArray[x, y].ID != 0)
                    {
                        _spriteBatch.Draw(collisionSprite, new Rectangle(x * p + worldContext.screenSpaceOffset.x, y * p + worldContext.screenSpaceOffset.y, p, p), Color.White);
                    }
                    _spriteBatch.Draw(redTexture, new Rectangle(x * p + worldContext.screenSpaceOffset.x, y * p + worldContext.screenSpaceOffset.y, p, 2), Color.White);
                    _spriteBatch.Draw(redTexture, new Rectangle(x * p + worldContext.screenSpaceOffset.x, y * p + worldContext.screenSpaceOffset.y, 2, p), Color.White);
                    _spriteBatch.Draw(redTexture, new Rectangle(x * p + worldContext.screenSpaceOffset.x, (y + 1) * p + worldContext.screenSpaceOffset.y, p, 2), Color.White);
                    _spriteBatch.Draw(redTexture, new Rectangle((x + 1) * p + worldContext.screenSpaceOffset.x, y * p + worldContext.screenSpaceOffset.y, 2, p), Color.White);

                }
            }
        }

        public void drawRewards() {
            if (worldContext.physicsObjects[0] is AiEntity firstEntity)
            {
                
                
                _spriteBatch.DrawString(ariel, (Math.Truncate((firstEntity.lastSampleReward) * 100) / 100).ToString() , new Vector2(_graphics.PreferredBackBufferWidth - 60, 50), Color.Blue);
                _spriteBatch.DrawString(ariel, (Math.Truncate(firstEntity.lastSampleEstimatedReward * 100) / 100).ToString(), new Vector2(_graphics.PreferredBackBufferWidth - 60, 70), Color.Blue);

                List<(int era, double loss, double rewardDifference, double estimatedReward)> cl = firstEntity.neuralNet.critic.criticLossDatapoints;

                if (cl.Count > 0)
                {
                    _spriteBatch.DrawString(ariel, (Math.Truncate(cl[cl.Count - 1].estimatedReward * 100) / 100).ToString(), new Vector2(_graphics.PreferredBackBufferWidth - 60, 90), Color.Blue);
                    int horizontalPixelsPerDatapoint = _graphics.PreferredBackBufferWidth / cl.Count;
                    double verticalMax = (_graphics.PreferredBackBufferHeight - 50) / firstEntity.neuralNet.critic.maxLoss;



                    for (int i = 0; i < cl.Count; i++)
                    {
                        float xLoc = (i * horizontalPixelsPerDatapoint);
                        float yLoc = -12 + _graphics.PreferredBackBufferHeight - (float)(verticalMax * cl[i].loss);

                        if(Mouse.GetState().X > xLoc - horizontalPixelsPerDatapoint/2 && Mouse.GetState().X < xLoc + horizontalPixelsPerDatapoint/2 && Mouse.GetState().Y > yLoc - 30 && Mouse.GetState().Y < yLoc + 30)
                        _spriteBatch.DrawString(arielSmall, (Math.Truncate((cl[i].rewardDifference) * 10) / 10).ToString(), new Vector2(xLoc, yLoc), Color.Blue);
                    }
                }
            }
        }

        public void drawGraph() {
            RasterizerState rasterizerState1 = new RasterizerState();
            rasterizerState1.CullMode = CullMode.None;
            GraphicsDevice.RasterizerState = rasterizerState1;

            drawLoss();


        }
        public void drawLoss() {
            if (worldContext.physicsObjects[0] is AiEntity a) {
                //draw a line between consecutive points:
                

                
                if (a.neuralNet.critic.criticLossDatapoints.Count > 1)
                {

                    List<(int era, double loss, double rewardDifference, double estimatedReward)> cl = a.neuralNet.critic.criticLossDatapoints;
                    VertexPositionColorTexture[] line = new VertexPositionColorTexture[cl.Count];
                    int[] ind = new int[cl.Count];
                    for (int i = 0; i < ind.Length; i++) {
                        ind[i] = i;
                    }

                    int horizontalPixelsPerDatapoint = _graphics.PreferredBackBufferWidth / cl.Count;
                    double verticalMax = (_graphics.PreferredBackBufferHeight - 50) / a.neuralNet.critic.maxLoss;


                    //I should just do x = i * horizontalPixelsPerDatapoint and the y is just _graphics.height + verticalMax * lossValue
                    for (int i = 0; i < cl.Count; i++)
                    {
                        line[i].Position = new Vector3((i * horizontalPixelsPerDatapoint)/(float)_graphics.PreferredBackBufferWidth, 1 - (float)(verticalMax * cl[i].loss)/(float)_graphics.PreferredBackBufferHeight, 0f);
                        line[i].Color = Color.Red;
                        line[i].TextureCoordinate = new Vector2(0f,0f);
                    }
                        foreach (EffectPass pass in basicEffect.CurrentTechnique.Passes)
                        {
                            pass.Apply();

                            GraphicsDevice.DrawUserIndexedPrimitives(
                                PrimitiveType.LineStrip,
                                line,
                                0,
                                line.Length,
                                ind,
                                0,
                                line.Length - 1
                            );
                        }
                    
                    
                }
                
            }
        }
    }


    public class PhysicsEngine
    {
        /*
         * A self contained engine that calculates kinematic physics
         * 
         * 
         * =========================================================
         * Settings file:
         * 
         * - blockSizeInMeters
         * - Gravity
         */


        bool helpDebug = false;
        public double blockSizeInMeters { get; set; } //The pixel size in meters can be found by taking this value and dividing it by pixelsPerBlock
        WorldContext wc;

        int horizontalOverlapMin = 2;
        int verticalOverlapMin = 2;

        double gravity;


        public PhysicsEngine(WorldContext worldContext)
        {
            wc = worldContext;


            //Load txt file and read the values to define important variables
            loadSettings();
        }

        private void loadSettings()
        {
            blockSizeInMeters = 0.6;
            gravity = 25;
        }

        public void computeAccelerationWithAirResistance(PhysicsObject entity, double timeElapsed)
        {
            int directionalityX;
            int directionalityY;
            //If cases to determine the direction of the current velocity. It can be done purely mathematically but it yeilded /0 errors. The directionality is unimportant when velocity = 0
            if (entity.velocityX > 0)
            {
                directionalityX = 1;
            }
            else
            {
                directionalityX = -1;
            }
            if (entity.velocityY > 0)
            {
                directionalityY = 1;
            }
            else
            {
                directionalityY = -1;
            }
            entity.accelerationX += -(directionalityX * (entity.kX * Math.Pow(entity.velocityX, 2)));
            entity.accelerationY += -(directionalityY * (entity.kY * Math.Pow(entity.velocityY, 2)));
        }
        public void computeAccelerationToVelocity(PhysicsObject entity, double timeElapsed)
        {
            entity.velocityX += (entity.accelerationX) * timeElapsed;
            entity.velocityY += (entity.accelerationY) * timeElapsed;




            //Sets the velocity to 0 if it is below a threshold. Reduces excessive sliding and causes the drag function to actually reach a halt
            if ((entity.velocityX > 0 && entity.velocityX < entity.minVelocityX) || (entity.velocityX < 0 && entity.velocityX > -entity.minVelocityX))
            {
                entity.velocityX = 0;
            }
            if ((entity.velocityY > 0 && entity.velocityY < entity.minVelocityY) || (entity.velocityY < 0 && entity.velocityY > -entity.minVelocityY))
            {
                entity.velocityY = 0;
            }

        }

        public void addGravity(PhysicsObject entity)
        {
            entity.accelerationY -= gravity;
        }

        public void applyVelocityToPosition(PhysicsObject entity, double timeElapsed)
        {
            //Adds the velocity * time passed to the x and y variables of the entity. Y is -velocity as the y-axis is flipped from in real life (Up is negative in screen space)
            //Converts the velocity into pixel space. This allows for realistic m/s calculations in the actual physics function and then converted to pixel space for the location

            entity.updateLocation(entity.velocityX * timeElapsed * (wc.pixelsPerBlock / blockSizeInMeters), -entity.velocityY * timeElapsed * (wc.pixelsPerBlock / blockSizeInMeters));
        }


        public void detectBlockCollisions(PhysicsObject entity)
        {
            helpDebug = false;
            //Gets the blocks within a single block radius around the entity. Detects if they are colliding, then if they are, calls another method
            int entityLocationInGridX = (int)Math.Floor(entity.x / wc.pixelsPerBlock);
            int entityLocationInGridY = (int)Math.Floor(entity.y / wc.pixelsPerBlock);
            int entityGridWidth = (int)Math.Ceiling((double)entity.collider.Width / wc.pixelsPerBlock);
            int entityGridHeight = (int)Math.Ceiling((double)entity.collider.Height / wc.pixelsPerBlock);

            Rectangle entityCollider = new Rectangle((int)entity.x, (int)entity.y, entity.collider.Width, entity.collider.Height);
            Block[,] worldArray = wc.worldArray; //A temporary storage of an array to reduce external function calls

            for (int x = entityLocationInGridX - 1; x < entityLocationInGridX + entityGridWidth + 1; x++)
            { //A range of x values on either side of the outer bounds of the entity
                for (int y = entityLocationInGridY - 1; y < entityLocationInGridY + entityGridHeight + 1; y++)
                {
                    if (x >= 0 && y >= 0 && x < worldArray.GetLength(0) && y < worldArray.GetLength(1))
                    {
                        if (worldArray[x, y].ID != 0) //In game implementation, air can either be null or have a special 'colliderless' block type 
                        {

                            Rectangle blockRect = new Rectangle(x * wc.pixelsPerBlock, y * wc.pixelsPerBlock, wc.pixelsPerBlock, wc.pixelsPerBlock);
                            if (blockRect.Intersects(entityCollider))
                            {
                                entity.onBlockCollision(computeCollisionNormal(entityCollider, blockRect), wc, x, y);
                                worldArray[x, y].onCollisionWithPhysicsObject(entity, this, wc);

                            }
                        }
                    }
                }
            }

        }

        public Vector2 computeCollisionNormal(Rectangle entityCollider, Rectangle blockRect)
        {
            (double x, double y) collisionNormal = (0, 0);
            (int x, int y) approximateCollisionDirection = (entityCollider.Center.X - blockRect.Center.X, entityCollider.Center.Y - blockRect.Center.Y);
            
            if (approximateCollisionDirection.x <= 0 && approximateCollisionDirection.y <= 0)
            { //Bottom Right from the player
                int verticalOverlap = entityCollider.Bottom - blockRect.Top;
                int horizontalOverlap = entityCollider.Right - blockRect.Left;
                if (horizontalOverlap < horizontalOverlapMin)
                {
                    horizontalOverlap = 0;
                }
                if (verticalOverlap < verticalOverlapMin)
                {
                    verticalOverlap = 0;
                }
                if (verticalOverlap != 0 || horizontalOverlap != 0)
                {

                    if (verticalOverlap > horizontalOverlap)
                    {
                        
                        return new Vector2(-1, 0);
                    }
                    else
                    {
                        return new Vector2(0, 1);
                    }
                }
            }
            else if (approximateCollisionDirection.x >= 0 && approximateCollisionDirection.y <= 0)
            { //Bottom Left from the player
                int verticalOverlap = entityCollider.Bottom - blockRect.Top;
                int horizontalOverlap = blockRect.Right - entityCollider.Left;
                if (horizontalOverlap < horizontalOverlapMin)
                {
                    horizontalOverlap = 0;
                }
                if (verticalOverlap < verticalOverlapMin)
                {
                    verticalOverlap = 0;
                }
                if (verticalOverlap != 0 || horizontalOverlap != 0)
                {

                    if (verticalOverlap > horizontalOverlap)
                    {
                        return new Vector2(1, 0);
                    }
                    else
                    {
                        return new Vector2(0, 1);
                    }
                }
            }
            else if (approximateCollisionDirection.x <= 0 && approximateCollisionDirection.y >= 0)
            { //Top Right from the player
                int verticalOverlap = blockRect.Bottom - entityCollider.Top;
                int horizontalOverlap = entityCollider.Right - blockRect.Left;
                if (horizontalOverlap < horizontalOverlapMin)
                {
                    horizontalOverlap = 0;
                }
                if (verticalOverlap < verticalOverlapMin)
                {
                    verticalOverlap = 0;
                }
                if (verticalOverlap != 0 || horizontalOverlap != 0)
                {


                    if (verticalOverlap > horizontalOverlap)
                    {
                        return new Vector2(-1, 0);
                    }
                    else
                    {
                        return new Vector2(0, -1);
                    }
                }
            }
            else if (approximateCollisionDirection.x >= 0 && approximateCollisionDirection.y >= 0)
            { //Top Left from the player
                int verticalOverlap = blockRect.Bottom - entityCollider.Top;
                int horizontalOverlap = blockRect.Right - entityCollider.Left;
                if (horizontalOverlap < horizontalOverlapMin)
                {
                    horizontalOverlap = 0;
                }
                if (verticalOverlap < verticalOverlapMin)
                {
                    verticalOverlap = 0;
                }
                if (verticalOverlap != 0 || horizontalOverlap != 0)
                {

                    if (verticalOverlap > horizontalOverlap)
                    {
                        return new Vector2(1, 0);
                    }
                    else
                    {
                        return new Vector2(0, -1);
                    }
                }
            }
            return Vector2.Zero;
        }

    }

    public class PhysicsObject
    {
        public double accelerationX { get; set; }
        public double accelerationY { get; set; }

        public bool calculatePhysics = true;

        public double velocityX { get; set; }
        public double velocityY { get; set; }

        public double x { get; set; }
        public double y { get; set; }

        public double kX { get; set; }
        public double kY { get; set; }

        public double bounceCoefficient { get; set; }

        public double minVelocityX { get; set; }
        public double minVelocityY { get; set; }

        public Rectangle collider { get; set; }

        public double drawWidth { get; set; }
        public double drawHeight { get; set; }
        public double width { get; set; }
        public double height { get; set; }

        public WorldContext worldContext;

        public bool isOnGround { get; set; }

        public PhysicsObject(WorldContext wc)
        {
            accelerationX = 0.0;
            accelerationY = 0.0;
            velocityX = 1.0;
            velocityY = 1.0;
            x = 0.0;
            y = 0.0;
            kX = 0.0;
            kY = 0.0;
            bounceCoefficient = 0.0;
            minVelocityX = 0.5;
            minVelocityY = 0.01;
            isOnGround = false;

            collider = new Rectangle(0, 0, wc.pixelsPerBlock, wc.pixelsPerBlock);

            worldContext = wc;


        }

        public virtual void updateLocation(double xChange, double yChange)
        {
            x += xChange;
            y += yChange;
        }

        public virtual void onBlockCollision(Vector2 collisionNormal, WorldContext worldContext, int blockX, int blockY)
        {

        }

        public void recalculateCollider()
        {
            collider = new Rectangle(0, 0, (int)(width * worldContext.pixelsPerBlock), (int)(height * worldContext.pixelsPerBlock));
        }

        public virtual void hasCollided() { }
    }

    public class Target {
        public double x;
        public double y;

        public double distance = 30;
        public double defaultDistance = 30;
        public double distanceIncrease = 1.5;

        public Target() {
            x = 300;
            y = 200;
        }

        public void randomiseLocation() {
            Random r = new Random();
            x = r.Next(40,500);
            if (r.Next(2) == 1)
            {
                y = 200;
            }
            else {
                y = 350;
            }
            
        }

        public void newLocation(double x, double velocityX) {
            this.x = -Math.Sign(velocityX) * distance + x;

            if(this.x < 40) { this.x = 40; }
            else if( this.x > 500) { this.x = 500; }

            distance *= distanceIncrease;
        }
    }
    public class AiEntity : PhysicsObject {

        public List<Sample> samples = new List<Sample>();
        public double reward;
        public double priorReward;

        public Target t = new Target();
        public NeuralNet neuralNet;
        int tickCount = 0;
        double maxUpdateDuration = 0.01;
        double updateDuration;

        double maxEraDuration = 5;
        double eraDuration;

        public double greedyEpsilon = 1;
        double greedyEpslionDecay = 0.995;

        public double hitTargetReward = 0;

        public Vector2 distanceFromTarget = Vector2.Zero;

        double xWeight = 1;
        double yWeight = 1;

        //An array that contains the reward for moving towards or away from the target for both x and y
        double[] positiveNegativeMovementRewards = new double[] {5, -1, 0, 0};


        public double actionIndex;
        public double maxEstimatedReward;
        public int era = 1;

        public double lastSampleReward;
        public double lastSampleEstimatedReward;

        public double greedyEpsilonDuration;
        public double maxGreedyEpsilonDuration = 0.05;
        public double greedyEpsilonDurationDecay = 0.5;
        public int greedyEpsilonOutput;

        public bool aiControl = true;
        public double maxControlCooldown = 0.1;
        public double controlCooldown = 0;

        public double[,] neuralNetOutput;

        double distanceRewardConst = 3000; //Large because the distance in pixels is typically massive
        public AiEntity(WorldContext worldContext) : base(worldContext) {
            drawHeight = 2;
            drawWidth = 0.9;

            

            accelerationX = 0.0;
            accelerationY = 0.0;
            velocityX = 0;
            velocityY = 0;
            x = 40.0;
            y = 00.0;
            kX = 8;
            kY = 0.01;
            bounceCoefficient = 0.0;
            minVelocityX = 0.5;
            minVelocityY = 0.01;

            width = 0.9;
            height = 2;
            //inputs: x, y, velocityX, velocityY, accelerationX, accelerationY
            //6 inputs, 4 in the first hidden layer, 3 in the output
            int[] neuralNetLayers = new int[] { 11, 15, 15, 2 };
            neuralNet = new NeuralNet(neuralNetLayers, NeuralNetCostFunction.MeanSquaredError, NodeLayerActivationFunction.linear, NodeLayerActivationFunction.relu);
            neuralNet.createOfflineNeuralNet(neuralNetLayers);
            neuralNet.createCriticNeuralNet(new int[] { 11,15, 15, 15, 1});

            collider = new Rectangle(0, 0, (int)(width * worldContext.pixelsPerBlock), (int)(height * worldContext.pixelsPerBlock));

            worldContext.physicsObjects.Add(this);
            eraDuration = maxEraDuration;
        }

        public override void updateLocation(double xChange, double yChange)
        {
            base.updateLocation(xChange, yChange);


            reward = distanceRewardConst / Math.Pow(Math.Pow(t.x - x, 2) * xWeight + Math.Pow(t.y - y, 2) * yWeight, 0.5) + hitTargetReward;

            //If the x difference now is less than it was, the entity is rewarded for moving closer
            //Cummulate all the rewards over the entire duration of the sample, but reduce them according to how long the duration is
            if (aiControl)
            {
                double newReward = 0;
                /*
                if (Math.Abs(t.x - x) <= distanceFromTarget.X)
                {
                    newReward += positiveNegativeMovementRewards[0] * maxUpdateDuration;
                }
                else
                {
                    newReward += positiveNegativeMovementRewards[1] * maxUpdateDuration;
                }
                distanceFromTarget.X = (float)Math.Abs(t.x - x);

                if (Math.Abs(t.y - y) <= distanceFromTarget.Y)
                {
                    newReward += positiveNegativeMovementRewards[2] * maxUpdateDuration;
                }
                else
                {
                    newReward += positiveNegativeMovementRewards[3] * maxUpdateDuration;
                }*/



                //newReward *= Math.Abs(velocityX);

                //reward += newReward;

                distanceFromTarget.Y = (float)Math.Abs(t.y - y);

                if (!isOnGround)
                {
                    //reward -= 0.03;
                }

                if (x >= t.x - 10 && x <= t.x + 10)
                {
                    t.newLocation(x, velocityX);
                    reward += 50;
                }

                if (samples.Count > 2)
                {
                    samples[samples.Count - 1].rewardChange = reward;// - priorReward;
                }
            }
        }

        public void onInput(GameTime gameTime) {
            if (controlCooldown < 0)
            {
                if (Keyboard.GetState().IsKeyDown(Keys.G))
                {
                    aiControl = !aiControl;
                    controlCooldown = maxControlCooldown;
                }
            }
            else {
                controlCooldown -= gameTime.ElapsedGameTime.TotalSeconds;
            }

            if (aiControl)
            {
                if (eraDuration >= 0 && y < 2000)
                {


                    eraDuration -= gameTime.ElapsedGameTime.TotalSeconds;
                    //calculate neural net output
                    //Then act based on that
                    double[,] input = new double[1, 11];
                    input[0, 0] = ((x / (worldContext.pixelsPerBlock)) * worldContext.engineController.physicsEngine.blockSizeInMeters);
                    input[0, 1] = ((y / (worldContext.pixelsPerBlock)) * worldContext.engineController.physicsEngine.blockSizeInMeters);
                    input[0, 2] = velocityX;
                    input[0, 3] = velocityY;
                    input[0, 4] = accelerationX;
                    input[0, 5] = accelerationY;
                    input[0, 6] = ((t.x / (worldContext.pixelsPerBlock)) * worldContext.engineController.physicsEngine.blockSizeInMeters);
                    input[0, 7] = ((t.y / (worldContext.pixelsPerBlock)) * worldContext.engineController.physicsEngine.blockSizeInMeters);
                    input[0, 8] = ((t.x / (worldContext.pixelsPerBlock)) * worldContext.engineController.physicsEngine.blockSizeInMeters) - ((x / (worldContext.pixelsPerBlock)) * worldContext.engineController.physicsEngine.blockSizeInMeters);
                    input[0, 9] = ((t.y / (worldContext.pixelsPerBlock)) * worldContext.engineController.physicsEngine.blockSizeInMeters) - ((y / (worldContext.pixelsPerBlock)) * worldContext.engineController.physicsEngine.blockSizeInMeters);
                    input[0, 10] = Convert.ToInt32(isOnGround);
                    neuralNetOutput = neuralNet.calculateNeuralNet(input);


                    double maxValue = 0;
                    int maxValueIndex = 0;

                    for (int i = 0; i < neuralNetOutput.GetLength(1); i++)
                    {
                        if (maxValue < neuralNetOutput[0, i])
                        {
                            maxValueIndex = i;
                        }
                    }


                    //Calculate greedy epsilon:
                    Random r = new Random();

                    if (r.NextDouble() < greedyEpsilon && greedyEpsilonDuration <= 0)
                    {
                        //Set a random action
                        maxValueIndex = r.Next(0, neuralNetOutput.GetLength(1));
                        greedyEpsilonDuration = maxGreedyEpsilonDuration;
                        greedyEpsilonOutput = maxValueIndex;
                    }

                    if (greedyEpsilonDuration > 0)
                    {
                        greedyEpsilonDuration -= gameTime.ElapsedGameTime.TotalSeconds;
                        maxValueIndex = greedyEpsilonOutput;
                    }

                    maxValue = neuralNetOutput[0, maxValueIndex];

                    actionIndex = maxValueIndex;

                    if ((maxValueIndex == 0 || Keyboard.GetState().IsKeyDown(Keys.D)) && !Keyboard.GetState().IsKeyDown(Keys.A))
                    {
                        //Move right
                        accelerationX += 75;
                        actionIndex = 0;
                    }
                    if ((maxValueIndex == 1 || Keyboard.GetState().IsKeyDown(Keys.A)) && !Keyboard.GetState().IsKeyDown(Keys.D))
                    {
                        //Move left
                        accelerationX -= 75;
                        actionIndex = 1;
                    }
                    if ((maxValueIndex == 2 || Keyboard.GetState().IsKeyDown(Keys.W)) && isOnGround)
                    {
                        accelerationY += 1 / gameTime.ElapsedGameTime.TotalSeconds;
                        actionIndex = 2;
                    }

                    maxEstimatedReward = neuralNet.critic.calculateNeuralNet(input)[0, 0];

                    reward = distanceRewardConst / Math.Pow(Math.Pow(t.x - x, 2) * xWeight + Math.Pow(t.y - y, 2) * yWeight, 0.5) + hitTargetReward;



                    //Instead, take a sample every 1 second

                    if (greedyEpsilon < 0.1)
                    {
                        greedyEpsilon = 0.005;
                    }

                    if (updateDuration <= 0)
                    {

                        //At a certain point: Stop learning
                        //Q-Learning:
                        //The neural net predicts a linear output that corrosponds to the estimated reward from that action: Eg. 40 points for moving left, 20 from moving right
                        //You store a record of samples through out an era. You take the input and the output of the neural net. The reward recieved from that input (eg. difference between t & t + 1)
                        //I'll need to store/update some temporary values I guess.
                        //With each sample, update the gradient descent thing using the current reward as the expected reward.
                        Sample currentSample = new Sample();
                        currentSample.state = input;
                        currentSample.output = neuralNetOutput;
                        currentSample.rewardChange = reward;
                        currentSample.actionIndex = maxValueIndex;

                        //Adjust the previous sample with the current reward difference
                        if (samples.Count > 0)
                        {
                            //samples[samples.Count - 1].rewardChange = reward - samples[samples.Count - 1].rewardChange;
                            if (samples[samples.Count - 1].rewardChange < 0)
                            {
                                //So evidently going left has been found to decrease the reward! SO now what
                            }
                            //The most recent sample in the sample list has the same input as the currentSample. That makes sense
                            samples[samples.Count - 1].nextState = input;
                        }

                        //priorReward = reward;
                        reward = 0;
                        hitTargetReward = 0;


                        samples.Add(currentSample);

                        if (samples.Count > 2)
                        {
                            neuralNet.updateGradientsFromSample(samples[samples.Count - 2], era);
                            neuralNet.learn(1);
                        }

                        updateDuration = maxUpdateDuration;
                    }
                    else
                    {
                        updateDuration -= gameTime.ElapsedGameTime.TotalSeconds;

                    }
                }
                else
                {
                    greedyEpsilon *= greedyEpslionDecay;
                    maxGreedyEpsilonDuration *= greedyEpsilonDurationDecay;
                    era += 1;


                    //What I'll do, is just update but using the methods, ignoring the samples




                    //The era ended: Compute all of the gradient changes and such:
                    //Ignore the last sample as it has incomplete information

                    //After every era, update the offline neural net to equal the current one

                    lastSampleReward = samples[samples.Count - 2].rewardChange;
                    lastSampleEstimatedReward = neuralNet.critic.calculateNeuralNet(samples[samples.Count - 2].state)[0, 0];


                    neuralNet.updateFromSampleList(samples, era);
                    //neuralNet.learn(samples.Count);
                    //neuralNet.critic.learn(samples.Count);
                    neuralNet.updateOfflineNeuralNet();

                    //Reset the agent's location:
                    t.x = 300;
                    t.distance = t.defaultDistance;
                    x = 200;
                    y = 0;
                    velocityX = 0;
                    velocityY = 0;
                    accelerationY = 0;
                    accelerationX = 0;

                    eraDuration = maxEraDuration;
                    samples.Clear();

                    t.randomiseLocation();
                }
            }
            else
            {
                if ((Keyboard.GetState().IsKeyDown(Keys.D)) && !Keyboard.GetState().IsKeyDown(Keys.A))
                {
                    //Move right
                    accelerationX += 75;
                    actionIndex = 0;
                }
                if ((Keyboard.GetState().IsKeyDown(Keys.A)) && !Keyboard.GetState().IsKeyDown(Keys.D))
                {
                    //Move left
                    accelerationX -= 75;
                    actionIndex = 1;
                }
                if ((Keyboard.GetState().IsKeyDown(Keys.W)) && isOnGround)
                {
                    accelerationY += 1 / gameTime.ElapsedGameTime.TotalSeconds;
                    actionIndex = 2;
                }

                double[,] input = new double[1, 11];
                input[0, 0] = ((x / (worldContext.pixelsPerBlock)) * worldContext.engineController.physicsEngine.blockSizeInMeters);
                input[0, 1] = ((y / (worldContext.pixelsPerBlock)) * worldContext.engineController.physicsEngine.blockSizeInMeters);
                input[0, 2] = velocityX;
                input[0, 3] = velocityY;
                input[0, 4] = accelerationX;
                input[0, 5] = accelerationY;
                input[0, 6] = ((t.x / (worldContext.pixelsPerBlock)) * worldContext.engineController.physicsEngine.blockSizeInMeters);
                input[0, 7] = ((t.y / (worldContext.pixelsPerBlock)) * worldContext.engineController.physicsEngine.blockSizeInMeters);
                input[0, 8] = ((t.x / (worldContext.pixelsPerBlock)) * worldContext.engineController.physicsEngine.blockSizeInMeters) - ((x / (worldContext.pixelsPerBlock)) * worldContext.engineController.physicsEngine.blockSizeInMeters);
                input[0, 9] = ((t.y / (worldContext.pixelsPerBlock)) * worldContext.engineController.physicsEngine.blockSizeInMeters) - ((y / (worldContext.pixelsPerBlock)) * worldContext.engineController.physicsEngine.blockSizeInMeters);
                input[0, 10] = Convert.ToInt32(isOnGround);
                maxEstimatedReward = neuralNet.critic.calculateNeuralNet(input)[0,0];
            }
        }

    }
        public class EngineController {
            public PhysicsEngine physicsEngine;
            public EngineController(WorldContext worldContext) {
                physicsEngine = new PhysicsEngine(worldContext);
            }
        }
        public class WorldContext {
            public int pixelsPerBlock = 32;
            public Block[,] worldArray;

            public (int x, int y) screenSpaceOffset = (0,0);
            public List<PhysicsObject> physicsObjects = new List<PhysicsObject>();

            public AiEntity controlledEntity;

            public EngineController engineController;
            public WorldContext() {
                engineController = new EngineController(this);
                controlledEntity = new AiEntity(this);
                worldArray = new Block[30,15];
                for (int x = 0; x < worldArray.GetLength(0); x++) {
                    for (int y = 0; y < worldArray.GetLength(1); y++) {
                        worldArray[x, y] = new Block(new Rectangle(0,0,0,0), 0);
                        worldArray[x, y].setupInitialData(this, null, (x,y));
                    }
                }

                for (int y = 0; y < 15; y++) {
                    worldArray[0, y] = new Block(new Rectangle(0, 0, 32, 32), 1);
                    worldArray[0, y].setupInitialData(this, null, (0, y));
                    worldArray[29, y] = new Block(new Rectangle(0, 0, 32, 32), 1);
                    worldArray[29, y].setupInitialData(this, null, (29, y));
                }

                for (int x = 0; x < 30; x++) {
                    worldArray[x, 10] = new Block(new Rectangle(0,0,32,32), 1);
                    worldArray[x, 10].setupInitialData(this, null, (x,10));

                    if (x > 9 || x < 11) {
                        worldArray[x, 7] = new Block(new Rectangle(0, 0, 32, 32), 1);
                        worldArray[x, 7].setupInitialData(this, null, (x, 7));
                    }
                }

                worldArray[10, 9] = new Block(new Rectangle(0,0,32,32), 1);
                worldArray[10, 9].setupInitialData(this, null, (10,9));



            }
        }
        public class Block
        {
            public Rectangle sourceRectangle;
            public int emmissiveStrength;
            public int ID;
            public List<Vector2> faceVertices;
            public int x { get; set; }
            public int y { get; set; }
            public bool isBlockTransparent = false;
            public (int width, int height) dimensions = (1, 1); //Default to 1 by 1 blocks
            public Vector4 faceDirection;


            public Block(Rectangle textureSourceRectangle, int ID)
            {
                this.sourceRectangle = textureSourceRectangle;
                this.ID = ID;
            }
            public Block(Rectangle textureSourceRectangle, int emmissiveStrength, int ID)
            {
                this.sourceRectangle = textureSourceRectangle;
                this.emmissiveStrength = emmissiveStrength;
                this.ID = ID;
            }
            public Block(int ID)
            {
                this.ID = ID;
            }

            public Block(Block b)
            {
                sourceRectangle = b.sourceRectangle;
                emmissiveStrength = b.emmissiveStrength;
                ID = b.ID;
                dimensions = b.dimensions;
                x = b.x;
                y = b.y;
            }

            public void setLocation((int x, int y) location)
            {
                x = location.x;
                y = location.y;
            }

            //A block specific check if that block can be placed. For example, torches, chests etc.
            public virtual bool canBlockBePlaced(WorldContext worldContext, (int x, int y) location)
            {
                return true;
            }
            public virtual void onBlockPlaced(WorldContext worldContext, (int x, int y) location)
            {
                setLocation(location);
            }
        
            public void blockDestroyed(Dictionary<(int x, int y), Block> exposedBlocks)
            {
                if (exposedBlocks.ContainsKey((x, y))) { exposedBlocks.Remove((x, y)); }
            }

            public virtual void setupInitialData(WorldContext worldContext, int[,] worldArray, (int x, int y) blockLocation)
            {
                x = blockLocation.x;
                y = blockLocation.y;
            }

            public virtual void setupFaceVertices(Vector4 exposedFacesClockwise)
            {
                this.faceDirection = exposedFacesClockwise;
                //2 Vector2s are needed to allow for all 4 directions to be accounted for. However, this isn't the cleanest code and should be later improved
                faceVertices = new List<Vector2>();
                if (exposedFacesClockwise.X == 1)
                {
                    faceVertices.Add(new Vector2(x, y));
                    faceVertices.Add(new Vector2(x + dimensions.width, y));
                }
                if (exposedFacesClockwise.Y == 1)
                {
                    //Check if the vertex already exists from the previous if statement
                    if (!faceVertices.Contains(new Vector2(x + dimensions.width, y)))
                    {

                        faceVertices.Add(new Vector2(x + dimensions.width, y));
                    }


                    faceVertices.Add(new Vector2(x + dimensions.width, y + dimensions.height));
                }
                if (exposedFacesClockwise.Z == 1)
                {
                    if (!faceVertices.Contains(new Vector2(x + dimensions.width, y + dimensions.height)))
                    {
                        faceVertices.Add(new Vector2(x + dimensions.width, y + dimensions.height));
                    }

                    faceVertices.Add(new Vector2(x, y + dimensions.height));
                }
                if (exposedFacesClockwise.W == 1)
                {
                    if (!faceVertices.Contains(new Vector2(x, y + dimensions.height)))
                    {
                        faceVertices.Add(new Vector2(x, y + dimensions.height));
                    }

                    faceVertices.Add(new Vector2(x, y));
                }
            }

            public virtual void onCollisionWithPhysicsObject(PhysicsObject entity, PhysicsEngine physicsEngine, WorldContext wc)
            {
                Rectangle entityCollider = new Rectangle((int)entity.x, (int)entity.y, entity.collider.Width, entity.collider.Height);
                Rectangle blockRect = new Rectangle(x * wc.pixelsPerBlock, y * wc.pixelsPerBlock, wc.pixelsPerBlock, wc.pixelsPerBlock);
                Vector2 collisionNormal = physicsEngine.computeCollisionNormal(entityCollider, blockRect);
                entity.hasCollided();

                //If the signs are unequal on either the velocity or the acceleration then the forces should cancel as the resulting motion would be counteracted by the block
                if (((Math.Sign(collisionNormal.Y) != Math.Sign(entity.velocityY) && entity.velocityY != 0) || (Math.Sign(collisionNormal.Y) != Math.Sign(entity.accelerationY) && entity.accelerationY != 0)) && collisionNormal.Y != 0)
                {
                    entity.velocityY -= (1 + entity.bounceCoefficient) * entity.velocityY;
                    entity.accelerationY -= entity.accelerationY;

                    if (Math.Sign(collisionNormal.Y) > 0)
                    {
                        entity.isOnGround = true;
                    }

                    if (Math.Sign(collisionNormal.Y) > 0)
                    {
                        entity.y = blockRect.Y - entityCollider.Height + 1;
                    }
                    else
                    {
                        entity.y = blockRect.Bottom - 1;
                    }
                }

                if (((Math.Sign(collisionNormal.X) != Math.Sign(entity.velocityX) && entity.velocityX != 0) || (Math.Sign(collisionNormal.X) != Math.Sign(entity.accelerationX) && entity.accelerationX != 0)) && collisionNormal.X != 0)
                {


                    entity.velocityX -= (1 + entity.bounceCoefficient) * entity.velocityX;
                    entity.accelerationX -= entity.accelerationX;

                    if (Math.Sign(collisionNormal.X) > 0)
                    {
                        entity.x = blockRect.Right - 1;
                    }
                    else
                    {
                        entity.x = blockRect.Left - entityCollider.Width + 1;
                    }

                }

            }

            public virtual Block copyBlock()
            {
                return new Block(this);
            }
        }

    public class Sample {
        public double[,] state;
        public double[,] output;
        public double rewardChange;
        public double[,] nextState;
        public int actionIndex;
        public bool wasClipped = false;
    }
    public class NeuralNet {
        //Reward: 1/Distance to target
        //Inputs: Target x, target y
        //          current x, y, velocityX, velocityY, accelerationX, accelerationY
        //Output: left, right or up. 1/0 for each

        //(1/(1 + e^-x)) Sigmoid function

        //Keep things really, really simple: Input -> output
        //Literally just a matrix multiplication. For each node, multiply the previous layer by the weights and then add the bias:

        //foreach(node)
        //{
        //node value = input1 * weight1 + bias1 + input2 * weight2 + bias2 + input3 * weight3 + bias3
        //Can convert all of it to become a matrix:
        //input: [input1, input2, input3] * [weight1, /n weight2, /n weight3] + [bias1, bias2, bias3] then add all the values
        //}

        //Each layer must contain it's weight and bias


        //https://imgs.search.brave.com/4f-cmvCg13xMpg-1b3NM-VAKh-e88uCj88MZ0DLsgFM/rs:fit:860:0:0:0/g:ce/aHR0cHM6Ly90b3dh/cmRzZGF0YXNjaWVu/Y2UuY29tL3dwLWNv/bnRlbnQvdXBsb2Fk/cy8yMDE5LzA2LzFW/eEt0bzhaMzVncVdG/TEZjZjB3UTRnLmpw/ZWc

        //The calculation loop should only be: for each node layer, looping forwards, call calculate layer passing in previous Nodelayer's output, the current weight and bias
        public NodeLayer[] nodeLayers;

        public NeuralNetCostFunction costFunction;
        NodeLayerActivationFunction outputFunction;
        NodeLayerActivationFunction baseLayerFunction;

        public List<(int era, double criticLoss, double rewardDifference, double rewardEstimate)> criticLossDatapoints = new List<(int era, double criticLoss, double rewardDifference, double rewardEstimate)>();
        public double maxLoss;

        //Back propigation:
        //For each node, it's value can be defined as the sigmoid of ( the sum of all the previous layers weights * activation function + a bias)
        //The cost function can just be the mean squared difference
        //For each step/sample, compute what the desired change to the weights is for each node
        //Average that desired change for each weight over some length of time/number of samples
        //Use that as the gradient descent

        //a(L) = sigmoid(z(L))

        //Find the derivative of C in respect to each W
        //Der(z(L)/Respect to W * Derivative of A with respect to Z * derivative of C with respect to A

        //Derivative of C with respect to A: 2 (A - y)
        //Derivatvie of A with respect to Z is just derivative of sigmoid(Z)?
        //Derivative of Z with respect to W is just A(L-1)?

        //The derivative of C in respect to B is just 2(A-y) * derivative of sigmoid(z) because the derivative of the bias is just 1

        //Sum of the Cost for each neuron in the last layer to get the total cost

        //With more than one neuron: Z is just the addition of all the weights * activation function of that neuron

        //The influence on the cost of the activation of a previous neuron is the sum of the influence through all neurons in the current layer. as there's multiple paths through which it influences the cost


        //Finding the influence on the cost of a previous neuron, this can then be passed into that neuron for it to calculate the impact of it's own weights on the cost

        //Derivative of C with respect to A (L-1) = sum of der(z)/der(A(L-1) * der(A)/Der(z) * Der(C)/der(A)



        //So the node values are just the derivative of sigmoid * derivative of cost. Both of which already exist

        //So you average that expression across all training examples to get the change to make


        //With double Q-Learning:
        //You have two separate neural nets: One online and one offline
        //The online neural net is the neural net that makes the decision on what action to take based on the highest Q value
        //Then each step, the output Q value is gradient descented as you do in normal Q learning
        //But the "future" prediction value comes from the offline neural net
        //After each era, the weights and biases of the offline neural net is updated to be the same as the online one
        //This is supposed to stop the inconsistencies of the learning system
        // -> Didn't work


        //PPO
        //Actor - Critic system
        //There are two seperate networks, one (the actor) determines the probability of any discrete action ocurring at that point in time
        //The critc then determines an estimate of the reward of that action
        //If the action was predicted to be good, adjust the probability of that action to be higher based on the gradient (prob(a)currently/prob(a)previously) within a limit
        //Then adjust the critic network to more closely match the actual reward output



        NeuralNet offlineNeuralNet;
        public NeuralNet critic;
        
        double learnRate = 0.000025;
        const double learningDecay = 0.9999;
        const double futureRewardDiscount = 0;

        const double actorEntropyWeight = 0.5;

        double[] actionCosts = new double[]{0, 0, 0, 5};
        double leakyReluConstant = 0.01;

        double[] advantageArray;
        double[] discountedFutureReward;

        const double ppoConstraint = 0.1;
        const double gae = 0.95;

        double[,] input;
        public NeuralNet(int[] layerNodeNumbers, NeuralNetCostFunction costFunction, NodeLayerActivationFunction outputFunction, NodeLayerActivationFunction baseFunction)
        {
            nodeLayers = new NodeLayer[layerNodeNumbers.Length - 1];
            
            for (int i = 1; i < layerNodeNumbers.Length; i++)
            {
                NodeLayer n = (new NodeLayer(layerNodeNumbers[i], layerNodeNumbers[i - 1]));
                n.costFunction = costFunction;
                if (i != layerNodeNumbers.Length - 1)
                {
                    n.activationFunction = baseFunction;
                }
                else {
                    n.activationFunction = outputFunction;
                    n.entropyWeight = actorEntropyWeight;
                }
                    nodeLayers[i - 1] = n;
            }

            this.costFunction = costFunction;
            this.outputFunction = outputFunction;
            this.baseLayerFunction = baseFunction;
        }

        public void createOfflineNeuralNet(int[] layerNodeNumbers) {
            offlineNeuralNet = new NeuralNet(layerNodeNumbers, NeuralNetCostFunction.AdvantageCost, outputFunction, baseLayerFunction);
        }

        public void createCriticNeuralNet(int[] layerNodeNumbers) {
            //The critic only has one output node
            critic = new NeuralNet(layerNodeNumbers, NeuralNetCostFunction.MeanSquaredError, NodeLayerActivationFunction.linear, NodeLayerActivationFunction.relu);
        }
        
        //Refactor everytihing to have the functions in places that make more sense: eg. inside the layers
        public double[,] calculateNeuralNet(double[,] input) 
        {
            this.input = input;
            //Calculate first layer from input:
            bool calculate = true;
            if (input == null) { calculate = false; }
            for(int i = 0; i < nodeLayers.Length; i++)
            {
                if (nodeLayers[i].weights == null) {
                    calculate = false;
                }

            }
            if (calculate)
            {
                nodeLayers[0].weightedInput = calculateLayerWeightedInput(input, nodeLayers[0].weights, nodeLayers[0].biases);

                if (baseLayerFunction == NodeLayerActivationFunction.sigmoid)
                {
                    nodeLayers[0].output = sigmoidActivationFunction(nodeLayers[0].weightedInput);
                }
                else if (baseLayerFunction == NodeLayerActivationFunction.relu)
                {
                    nodeLayers[0].output = reluActivationFunction(nodeLayers[0].weightedInput);
                }
                else if (baseLayerFunction == NodeLayerActivationFunction.quadratic)
                {
                    nodeLayers[0].output = quadraticActivationFunction(nodeLayers[0].weightedInput);
                }
                else if (baseLayerFunction == NodeLayerActivationFunction.softmax)
                {
                    nodeLayers[0].output = logSoftmaxActivationFunction(nodeLayers[0].weightedInput);
                }
                else {
                    nodeLayers[0].output = nodeLayers[0].weightedInput;
                }

                    //Forward propogate for each layer
                    for (int i = 1; i < nodeLayers.Length - 1; i++)
                    {
                        //Breaks up the calculation into two arrays to capture the inputs and the activation for easier back propigation
                        nodeLayers[i].weightedInput = calculateLayerWeightedInput(nodeLayers[i - 1].output, nodeLayers[i].weights, nodeLayers[i].biases);
                        if (baseLayerFunction == NodeLayerActivationFunction.sigmoid)
                        {
                            nodeLayers[i].output = sigmoidActivationFunction(nodeLayers[i].weightedInput);
                        }
                        else if (baseLayerFunction == NodeLayerActivationFunction.relu)
                        {
                            nodeLayers[i].output = reluActivationFunction(nodeLayers[i].weightedInput);
                        }
                        else if (baseLayerFunction == NodeLayerActivationFunction.quadratic)
                        {
                            nodeLayers[i].output = quadraticActivationFunction(nodeLayers[i].weightedInput);
                        }
                        else if (baseLayerFunction == NodeLayerActivationFunction.softmax)
                        {
                            nodeLayers[i].output = logSoftmaxActivationFunction(nodeLayers[i].weightedInput);
                        }
                    else
                        {
                            nodeLayers[i].output = nodeLayers[i].weightedInput;
                        }
                }

                //For the last layer: don't pass it through an activation function: leave it linear:
                nodeLayers[nodeLayers.Length - 1].weightedInput = calculateLayerWeightedInput(nodeLayers[nodeLayers.Length - 2].output, nodeLayers[nodeLayers.Length - 1].weights, nodeLayers[nodeLayers.Length - 1].biases);
                if (outputFunction == NodeLayerActivationFunction.sigmoid)
                {
                    nodeLayers[nodeLayers.Length - 1].output = sigmoidActivationFunction(nodeLayers[nodeLayers.Length - 1].weightedInput);
                }
                else if (outputFunction == NodeLayerActivationFunction.relu)
                {
                    nodeLayers[nodeLayers.Length - 1].output = reluActivationFunction(nodeLayers[nodeLayers.Length - 1].weightedInput);
                }
                else if (outputFunction == NodeLayerActivationFunction.quadratic)
                {
                    nodeLayers[nodeLayers.Length - 1].output = quadraticActivationFunction(nodeLayers[nodeLayers.Length - 1].weightedInput);
                }
                else if (outputFunction == NodeLayerActivationFunction.softmax) {
                    nodeLayers[nodeLayers.Length - 1].output = logSoftmaxActivationFunction(nodeLayers[nodeLayers.Length - 1].weightedInput);
                }
                else
                {
                    nodeLayers[nodeLayers.Length - 1].output = nodeLayers[nodeLayers.Length - 1].weightedInput;
                }
                return nodeLayers[nodeLayers.Length - 1].output;
            }
            else { return null; }
        }

        public double[,] calculateLayerWeightedInput(double[,] input, double[,] nodeLayerWeights, double[,] layerBiases) {
            double[,] weightedInput = multiplyMatrices(nodeLayerWeights,input);
            double[,] weightedInputWithBias = addMatrices(weightedInput, layerBiases);
            //Each row is a singular node

            //Return the value through the sigmoid activation function;
            return weightedInputWithBias;
        }

        public double[,] sigmoidActivationFunction(double[,] matrix) {
            //Setup to allow for 
            for (int x = 0; x < matrix.GetLength(0); x++)
            {
                for (int y = 0; y < matrix.GetLength(1); y++) {
                    matrix[x,y] = (1) / (1 + Math.Pow(Math.E, -matrix[x,y]));
                }
            }
            return matrix;
        }

        public double[,] reluActivationFunction(double[,] matrix) {
            for (int x = 0; x < matrix.GetLength(0); x++)
            {
                for (int y = 0; y < matrix.GetLength(1); y++)
                {
                    if (matrix[x, y] <= 0) {
                        matrix[x, y] *= leakyReluConstant;
                    }
                }
            }
            return matrix;
        }

        public double[,] quadraticActivationFunction(double[,] matrix)
        {
            for (int x = 0; x < matrix.GetLength(0); x++)
            {
                for (int y = 0; y < matrix.GetLength(1); y++)
                {
                    matrix[x, y] *= matrix[x, y];
                }
            }
            return matrix;
        }

        public double[,] logSoftmaxActivationFunction(double[,] matrix) {
            //Find the max value to c-shift:
            
            
            double maxValue = matrix[0,0];
            for (int x = 0; x < matrix.GetLength(0); x++) {
                for (int y = 0; y < matrix.GetLength(1); y++) {
                    if (matrix[x, y] > maxValue) {
                        maxValue = matrix[x, y];
                    }
                }
            }
            double sum = 0.00001;
            for (int x = 0; x < matrix.GetLength(0); x++)
            {
                for (int y = 0; y < matrix.GetLength(1); y++)
                {
                    if (matrix[x, y] - maxValue > -50)
                    {
                        sum += Math.Exp(matrix[x, y] - maxValue);
                    }
                }
            }
            for (int x = 0; x < matrix.GetLength(0); x++)
            {
                for (int y = 0; y < matrix.GetLength(1); y++)
                {
                    if (sum != 0)
                    {
                        if (matrix[x, y] - maxValue < -50) {
                            matrix[x, y] = maxValue - 50;
                        }
                        matrix[x, y] = Math.Exp(matrix[x, y] - maxValue)/sum;

                        if (matrix[x, y] > 1.5) { System.Diagnostics.Debug.WriteLine("Somehow"); }
                    }
                    else {
                        System.Diagnostics.Debug.WriteLine("The sum was zeroed");
                    }
                }
            }

            return matrix;
        }

        public double[,] multiplyMatrices(double[,] input, double[,] weights) {
            if (input.GetLength(0) == weights.GetLength(1))
            {
                //Can multiply:
                double[,] multipliedMatrix = new double[weights.GetLength(0),input.GetLength(1)];
                for (int y = 0; y < input.GetLength(1); y++) {
                    for (int x = 0; x < weights.GetLength(0); x++) {
                        //For each of the output matrice's value:
                        double sumOfValues = 0;
                        for (int loc = 0; loc < input.GetLength(0); loc++) {
                            sumOfValues += input[loc, y] * weights[x, loc];
                        }
                        multipliedMatrix[x, y] = sumOfValues;
                        
                    }
                }
                return multipliedMatrix;

            }
            else {
                return null;
            }
        }

        public double[,] addMatrices(double[,] matrix1, double[,] matrix2) {
            if (matrix1.GetLength(0) == matrix2.GetLength(0) && matrix2.GetLength(1) == matrix2.GetLength(1))
            {
                for (int x = 0; x < matrix1.GetLength(0); x++)
                {
                    for (int y = 0; y < matrix1.GetLength(1); y++)
                    {
                        matrix1[x, y] += matrix2[x, y];
                    }
                }
                return matrix1;
            }
            else {
                return null;
            }
        
        }

        public double[,] calculateCost(double[,] actual, double[,] expected)
        {
            //Calculates the cost of each node and returns it
            if (actual.GetLength(0) == expected.GetLength(0) && actual.GetLength(1) == expected.GetLength(1))
            {
                for (int x = 0; x < actual.GetLength(0); x++) {
                    for (int y = 0; y < actual.GetLength(1); y++) {
                        actual[x,y] = individiualCost(actual[x,y], expected[x,y]);
                    }
                }

                return actual;
            }
            else { System.Diagnostics.Debug.WriteLine("Cost Error"); return null;/*Arbitrary, large value in case it ever occurs, it will be a bad output*/ }
        }

        public double individiualCost(double actual, double expected) {
            double error = actual - expected;
            return error * error;
        }

        public void updateFromSampleList(List<Sample> samples, int era) {
            advantageArray = new double[samples.Count - 1];
            discountedFutureReward = new double[samples.Count - 1];
            int sampleCount = samples.Count;
            critic.criticLossDatapoints.Add((0,0,0,0));

            for (int i = samples.Count - 2; i >= 0; i--)
            {
                calculateAdvantage(samples[i], i, sampleCount);
                
                updateGradientsFromSample(samples[i], i);
                //I realised that the critic never gets a sample update
                critic.discountedFutureReward = discountedFutureReward;
                critic.updateGradientsFromSample(samples[i], i);
                double[,] expectedReward = new double[1, 1];
                expectedReward[0, 0] = samples[i].rewardChange;
                
                //critic.addCriticLossValue(samples[sampleCount - 2].state, expectedReward, era);

                //Learning after every gradient descent instead of after each batch of samples.
                learn(samples.Count);
                critic.learn(samples.Count);
            }
            (int era, double criticLoss, double rewardDifference, double rewardEstimate) currentValue = critic.criticLossDatapoints[era - 2];
            critic.criticLossDatapoints[era - 2] = (currentValue.era, currentValue.criticLoss/(double)samples.Count, currentValue.rewardDifference/(double)samples.Count, currentValue.rewardEstimate/(double)samples.Count);

            if (critic.criticLossDatapoints[era - 2].criticLoss > critic.maxLoss)
            {
                critic.maxLoss = critic.criticLossDatapoints[era - 2].criticLoss;
            }


        }

        public double averageNeuralNet(double[,] values) {
            double average = 0;
            for (int i = 0; i < values.GetLength(1); i++) {
                average += values[0, i];
            }

            if (values.GetLength(1) != 0) {
                average /= values.GetLength(1);
            }

            return average;
        }

        public void addCriticLossValue(double[,] state, double[,] expectedValue, int era) {
            double[,] actualOutput = calculateNeuralNet(state);
            double cost = calculateCost(actualOutput, expectedValue)[0,0];
            
            //Era - 2, as the era value is added onto before being passed into the function
            
            (int era, double criticLoss, double rewardDifference, double rewardEstimate) currentValue = criticLossDatapoints[era - 2];
            criticLossDatapoints[era - 2] = (era, (cost + currentValue.criticLoss), (actualOutput[0,0] - expectedValue[0,0] + currentValue.rewardDifference), (actualOutput[0,0] + currentValue.rewardEstimate));
        }
        public void calculateAdvantage(Sample samples, int i, int sampleCount) {
            
            discountedFutureReward[i] = samples.rewardChange - actionCosts[samples.actionIndex];
            if (i < sampleCount - 2)
            {
                discountedFutureReward[i] += gae * futureRewardDiscount * discountedFutureReward[i + 1];
            }

            //What's the difference between the real discounted future rewards and what the critic predicted? Is it better or worse than expected?
            advantageArray[i] = discountedFutureReward[i] - critic.calculateNeuralNet(samples.state)[0,0];
                
        }
        //The output layer bias gradient descent is just the node Values, while the weights are the node values * inputValues
        public void updateGradientsFromSample(Sample s, int sampleIndex) {
            //calculate the expected output by passing in the sample's next state through the neural net:

            double[,] expectedNeuralNetOutput = null;
            //if (offlineNeuralNet != null) { expectedNeuralNetOutput = offlineNeuralNet.calculateNeuralNet(s.state); }
            
            //Convert to a 1D array:
            //pass the sample's current position data to flush the inputs/outputs:
            //Compute the optimal reward through the bellman equation:

            //Find max and it's index as you predict the future agent to be perfectly optimal
            double maxValue = 0;
            if (expectedNeuralNetOutput != null)
            {
                maxValue = expectedNeuralNetOutput[0, 0];
                for (int i = 0; i < expectedNeuralNetOutput.GetLength(1); i++)
                {

                    if (maxValue < expectedNeuralNetOutput[0, i])
                    {
                        maxValue = expectedNeuralNetOutput[0, i];
                    }
                }
            }
            //If the model is just running into a wall, penalise it
            if (s.rewardChange == 0) {
                s.rewardChange = -1;
            }
            else if (s.rewardChange < 0) {
                s.rewardChange *= 2; //Really punish it
            }
            double optimalReward = s.rewardChange;


            updateOnePath(optimalReward, s.actionIndex, false);
            /*


            //PPO algorithm: r(0) = pi(current)/pi(old)
            //Constrain r to 1 +- e;
            if (costFunction == NeuralNetCostFunction.AdvantageCost)
            {
                
                double rOldValue = s.output[0, s.actionIndex] / expectedNeuralNetOutput[0, s.actionIndex];
                double rValue = rOldValue;

                if (rValue > 1 + ppoConstraint)
                {
                    rValue = 1 + ppoConstraint;
                    s.wasClipped = true;
                }
                else if (rValue < 1 - ppoConstraint)
                {
                    rValue = 1 - ppoConstraint;
                    s.wasClipped = true;
                }

                //If the clipped value is larger (a bigger change) than the old one, unclip it
                if (rValue * advantageArray[sampleIndex] > rOldValue * advantageArray[sampleIndex]) {  rValue = rOldValue; s.wasClipped = false;  }
                calculateNeuralNet(s.state);
                updateOnePath(rValue * advantageArray[sampleIndex], s.actionIndex, s.wasClipped);
                /*
                if (advantageArray != null)
                {
                    if (sampleIndex < advantageArray.Length - 1)
                    {
                        if (advantageArray[sampleIndex] > 0)
                        {
                            updateOnePath(1, s.actionIndex, s.wasClipped);
                        }
                        else
                        {
                            //DO you adjust the output (positive, negative) based on the critics output of the action
                            //Then do this calculation to constrain the values, and update all from there? I'll figure it out later
                            //The loss value is the rValue * reward from the critic. So if you make the system worse, it'll be lower than A

                            updateOnePath(0.1, s.actionIndex, s.wasClipped);
                        }
                    }
                    else
                    {
                        //Just meh the last action
                        updateOnePath(0.5, s.actionIndex, s.wasClipped);
                    }
                }

                
                
            }
            else {
                double[,] tempOutput = calculateNeuralNet(s.state);
                //double[,] expectedOutput = new double[1, 1];
                //expectedOutput[0,0] = discountedFutureReward[sampleIndex];
                //System.Diagnostics.Debug.WriteLine("Expected: " + expectedOutput[0,0]);
                //System.Diagnostics.Debug.WriteLine("Actual: " + tempOutput[0,0]);
                updateGradientsFromSample(expectedOutput, false);
            }
            */
            //You don't update all the gradients: only the ones connected the the output that got chosen
        }
        public void updateOfflineNeuralNet() {
            if (offlineNeuralNet != null) {
                offlineNeuralNet.setNeuralNetWeights(nodeLayers);
            }
        }
        public void setNeuralNetWeights(NodeLayer[] newNodeLayers) {
            for (int i = 0; i < newNodeLayers.Count(); i++) {
                if (i < nodeLayers.Count())
                {
                    nodeLayers[i].setWeight(newNodeLayers[i].weights);
                    nodeLayers[i].setBiases(newNodeLayers[i].biases);
                }
            }
        }
        public void updateAllGradients(double[,] expectedNetOutput, bool wasSampleClipped) {
            NodeLayer outputLayer = nodeLayers[nodeLayers.Length  - 1];
            double[,] nodeValues = outputLayer.calculateNodeValues(expectedNetOutput, wasSampleClipped);
            outputLayer.updateGradientDescent(nodeLayers[nodeLayers.Length - 2].output, nodeValues);
            
                for (int i = nodeLayers.Length - 2; i >= 1; i--)
                {
                    nodeValues = nodeLayers[i].calculateHiddenLayerNodeValues(nodeLayers[i + 1], nodeValues);
                    nodeLayers[i].updateGradientDescent(nodeLayers[i - 1].output, nodeValues);
                }

            //Because the layer doesn't store the inputs, there has to be a final update for the first hidden layer that takes in the net's input
            nodeValues = nodeLayers[0].calculateHiddenLayerNodeValues(nodeLayers[1], nodeValues);
            nodeLayers[0].updateGradientDescent(input, nodeValues);

        }
        public void updateOnePath(double expectedOutput, int outputPathIndex, bool wasSampleClipped) {

            //Mask the output layer's node values for everything except for the action that was taken: By zeroing out all the nodeValues,
            // all further back prop becomes zero
            
            NodeLayer outputLayer = nodeLayers[nodeLayers.Length - 1];
            double[,] zeroedExpectedOutput = new double[1,outputLayer.output.GetLength(1)];
            for (int i = 0; i < zeroedExpectedOutput.GetLength(1); i++) {
                if (i != outputPathIndex)
                {
                    zeroedExpectedOutput[0,i] = 0; //THe output is perfect, so no changes for all the other paths?
                }
                else {
                    zeroedExpectedOutput[0, i] = expectedOutput;
                }
            }
            //Instead, just set all the non-paths to have a perfect output, so it won't change anything?
            double[,] nodeValues = outputLayer.calculateNodeValues(zeroedExpectedOutput, wasSampleClipped);
            
            for (int x = 0; x < nodeValues.GetLength(0); x++) {
                for (int y = 0; y < nodeValues.GetLength(1); y++) {
                    if (y != outputPathIndex) {
                        nodeValues[x, y] = 0;
                    }
                }
            }//*/

            outputLayer.updateGradientDescent(nodeLayers[nodeLayers.Length - 2].output, nodeValues);
            

            for (int i = nodeLayers.Length - 2; i >= 1; i--)
            {
                nodeValues = nodeLayers[i].calculateHiddenLayerNodeValues(nodeLayers[i + 1], nodeValues);
                nodeLayers[i].updateGradientDescent(nodeLayers[i - 1].output, nodeValues);
            }

            //Because the layer doesn't store the inputs, there has to be a final update for the first hidden layer that takes in the net's input
            nodeValues = nodeLayers[0].calculateHiddenLayerNodeValues(nodeLayers[1], nodeValues);
            nodeLayers[0].updateGradientDescent(input, nodeValues);
        }
        public void learn(double elapsedDurationOfArbitrarySize) {
            applyAllGradients(learnRate/elapsedDurationOfArbitrarySize);
            learnRate *= learningDecay;
        }
        public void applyAllGradients(double learningStrength) {
            for (int i = 0; i < nodeLayers.Length; i++) {
                nodeLayers[i].applyGradient(learningStrength);
            }
        }
        //The node output must be without the activation function:
        
    }

    public class NodeLayer {
        public double[,] weights;
        public double[,] biases;
        public double[,] output;

        public double[,] weightedInput;

        public double[,] costWeights;
        public double[,] costBiases;

        public NodeLayerActivationFunction activationFunction;
        public NeuralNetCostFunction costFunction;

        public double entropyWeight; //Recessive for everything except for advantage cost
        public double entropy;
        

        double leakyReluConstant = 0.01;
        public NodeLayer(int nodeCount, int previousLayerNodeCount) {
            //The input is presented as a x,1
            //So the node should be a 1,x

            weights = new double[previousLayerNodeCount, nodeCount];
            costWeights = new double[previousLayerNodeCount, nodeCount];
            biases = new double [1, nodeCount];
            costBiases = new double[1,nodeCount];
            output = new double[1,nodeCount];
            weightedInput = new double[1,nodeCount];

            //Randomise all weights and biases:
            Random r = new Random();
            for (int x = 0; x < weights.GetLength(0); x++)
            {
                for (int y = 0; y < weights.GetLength(1); y++) {
                    weights[x, y] = ((2 *  r.NextDouble()) - 1);
                }
            }

            for (int x = 0; x < biases.GetLength(0); x++) {
                for (int y = 0; y < biases.GetLength(1); y++) {
                    biases[x, y] =  r.NextDouble();
                }
            }
        }

        #region Gradient Descent Calculations
        public void updateGradientDescent(double[,] input, double[,] nodeValues)
        {
            for (int x = 0; x < output.GetLength(0); x++) {
                for (int y = 0; y < output.GetLength(1); y++) {
                    //Foreach of the nodes in the output:
                    //adjust the corrosponding weights by the nodeValue * input
                    for (int inX = 0; inX < input.GetLength(0); inX++) {
                        for (int inY = 0; inY < input.GetLength(1); inY++) {
                            double gradientDescent = input[inX, inY] * nodeValues[x, y];

                            //Adjust the gradient accordingly: Using both Y values because of the orientation of the matrices
                            costWeights[inY, y] += gradientDescent;
                        }
                    }
                    costBiases[x, y] += nodeValues[x, y];
                }
            }
        }

        public double individualCostDerivative(double expected, double actual, bool wasSampleClipped)
        {
            if (costFunction == NeuralNetCostFunction.AdvantageCost)
            {

                //Might have to reverse this value                
                double entropyLoss = actual * (Math.Log(actual) + entropy);


                if (wasSampleClipped)
                {
                    return entropyWeight * entropyLoss;
                }
                else
                {
                    return (expected / actual) + entropyWeight * entropyLoss;
                }
            }
            else {
                //System.Diagnostics.Debug.WriteLine("Expected2: " + expected);
                //System.Diagnostics.Debug.WriteLine("Actual2: " + actual);
                return 2 * (actual - expected);
            }
        }

        public double[,] sigmoidActivationDerivative(double[,] matrix)
        {
            for (int x = 0; x < matrix.GetLength(0); x++)
            {
                for (int y = 0; y < matrix.GetLength(1); y++)
                {
                    double activationFunction = (1) / (1 + Math.Pow(Math.E, -matrix[x, y]));

                    matrix[x, y] = activationFunction * (1 - activationFunction);
                }
            }
            return matrix;
        }

        public double[,] reluActivationDerivative(double[,] matrix) {
            for (int x = 0; x < matrix.GetLength(0); x++)
            {
                for (int y = 0; y < matrix.GetLength(1); y++)
                {
                    if (matrix[x, y] <= 0)
                    {
                        matrix[x, y] = leakyReluConstant;
                    }
                    else {
                        matrix[x, y] = 1;
                    }
                }
            }
            return matrix;
        }

        public double[,] quadraticActivationDerivative(double[,] matrix) {
            for (int x = 0; x < matrix.GetLength(0); x++)
            {
                for (int y = 0; y < matrix.GetLength(1); y++)
                {
                    matrix[x, y] = 2 * matrix[x,y];
                }
            }
            return matrix;
        }

        public double[,] softmaxActivationDerivative(double[,] matrix) {
            //if i = j, return 1 - pi
            //if i != j, return - pi;

            //Because the input is log(pi),
            //We need to do e^log(pi) to just get pi;
            
            for (int x = 0; x < matrix.GetLength(0); x++) {
                for (int y = 0; y < matrix.GetLength(1); y++) {
                    if (x == y)
                    {
                        matrix[x, y] = 1 - matrix[x, y];
                    }
                    else {
                        matrix[x, y] = -matrix[x,y];
                    }
                }
            }
            return matrix;
        }



        public double[,] calculateNodeValues(double[,] expected, bool wasSampleClipped)
        {
            double[,] nodeValues = new double[expected.GetLength(0), expected.GetLength(1)];
            if (costFunction == NeuralNetCostFunction.MeanSquaredError) {
                //System.Diagnostics.Debug.WriteLine(output.GetLength(0) + ", " + output.GetLength(1));
            } else if(costFunction == NeuralNetCostFunction.AdvantageCost) {
                //Compute entropy
                entropy = 0;
                for (int x = 0; x < output.GetLength(0); x++) {
                    for (int y = 0; y < output.GetLength(1); y++) {
                        entropy += output[x, y] * Math.Log(output[x, y]);
                    }
                }
                entropy *= -1;
            }
                for (int x = 0; x < nodeValues.GetLength(0); x++)
                {
                    for (int y = 0; y < nodeValues.GetLength(1); y++)
                    {
                        nodeValues[x, y] = individualCostDerivative(expected[x, y], output[x, y], wasSampleClipped);
                    }
                }

            double[,] activationFunctionArray = new double[0,0];
            if (activationFunction == NodeLayerActivationFunction.sigmoid)
            {
                activationFunctionArray = sigmoidActivationDerivative(weightedInput);
            }
            else if (activationFunction == NodeLayerActivationFunction.relu)
            {
                activationFunctionArray = reluActivationDerivative(weightedInput);
            }
            else if (activationFunction == NodeLayerActivationFunction.quadratic)
            {
                activationFunctionArray = quadraticActivationDerivative(weightedInput);
            }
            else if (activationFunction == NodeLayerActivationFunction.softmax) {
                activationFunctionArray = softmaxActivationDerivative(weightedInput);
            }
            if (activationFunction != NodeLayerActivationFunction.linear && activationFunctionArray.GetLength(1) != 0)
            {
                for (int x = 0; x < nodeValues.GetLength(0); x++)
                {
                    for (int y = 0; y < nodeValues.GetLength(1); y++)
                    {
                        nodeValues[x, y] *= activationFunctionArray[x, y];
                        if (costFunction == NeuralNetCostFunction.MeanSquaredError)
                        {
                            System.Diagnostics.Debug.WriteLine(nodeValues[x, y]);
                        }
                    }
                }
            }
            return nodeValues;
        }

        //Somehow completely ignores the activation function of that layer?? I'll look into this
        public double[,] calculateHiddenLayerNodeValues(NodeLayer oldLayer, double[,] oldNodeValues) {
            double[,] newNodeValues = new double[output.GetLength(0), output.GetLength(1)];

            for (int x = 0; x < newNodeValues.GetLength(0); x++) {
                for (int y = 0; y < newNodeValues.GetLength(1); y++) {
                    double newNodeValue = 0;
                    for (int oldX = 0; oldX < oldNodeValues.GetLength(0); oldX++)
                    {
                        for (int oldY = 0; oldY < oldNodeValues.GetLength(1); oldY++) {
                            double weightedInputDerivative = oldLayer.weights[y,oldY];
                            newNodeValue += weightedInputDerivative * oldNodeValues[oldX, oldY];
                        }
                    }
                    newNodeValues[x, y] = newNodeValue;
                }
            }

            double[,] activationFunctionArray = new double[0, 0];
            if (activationFunction == NodeLayerActivationFunction.sigmoid)
            {
                activationFunctionArray = sigmoidActivationDerivative(weightedInput);
            }
            else if (activationFunction == NodeLayerActivationFunction.relu)
            {
                activationFunctionArray = reluActivationDerivative(weightedInput);
            }
            else if (activationFunction == NodeLayerActivationFunction.quadratic)
            {
                activationFunctionArray = quadraticActivationDerivative(weightedInput);
            }
            else if (activationFunction == NodeLayerActivationFunction.softmax)
            {
                activationFunctionArray = softmaxActivationDerivative(weightedInput);
            }
            if (activationFunction != NodeLayerActivationFunction.linear && activationFunctionArray.GetLength(1) != 0)
            {
                for (int x = 0; x < oldNodeValues.GetLength(0); x++)
                {
                    for (int y = 0; y < oldNodeValues.GetLength(1); y++)
                    {
                        newNodeValues[x, y] *= activationFunctionArray[x, y];
                    }
                }
            }
            return newNodeValues;
        }

        public void applyGradient(double updateStrength) {
            for (int x = 0; x < costWeights.GetLength(0); x++) {
                for (int y = 0; y < costWeights.GetLength(1); y++) {
                    if (costWeights[x, y] * updateStrength > 5) { costWeights[x, y] = 5 / updateStrength; }
                    else if (costWeights[x, y] * updateStrength < -5) { costWeights[x, y] = -5 / updateStrength; }

                    weights[x, y] -= costWeights[x, y] * updateStrength;
                    costWeights[x, y] = 0;
                }
            }

            for (int x = 0; x < costBiases.GetLength(0); x++)
            {
                for (int y = 0; y < costBiases.GetLength(1); y++)
                {
                    
                    if (costBiases[x, y] * updateStrength > 5) { costBiases[x, y] = 5 / updateStrength; }
                    else if (costBiases[x, y] * updateStrength < -5) { costBiases[x, y] = -5 / updateStrength; }
                        biases[x, y] -= costBiases[x, y] * updateStrength;
                    costBiases[x, y] = 0;
                }
            }
        }
        #endregion

        public void setWeight(double[,] weight)
        {
            weights = weight;
        }
        public void setBiases(double[,] bias) {
            biases = bias;
        }
    }

    public enum NodeLayerActivationFunction {
        linear,
        sigmoid,
        relu,
        quadratic,
        softmax
    }
    public enum NeuralNetCostFunction {
        AdvantageCost,
        MeanSquaredError
    }
}
