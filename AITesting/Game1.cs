using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;

using Vector2 = Microsoft.Xna.Framework.Vector2;
using Vector3 = Microsoft.Xna.Framework.Vector3;
using Vector4 = Microsoft.Xna.Framework.Vector4;
namespace AITesting
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        SpriteFont ariel;

        WorldContext worldContext;

        Texture2D entityTexture;
        Texture2D blockTextures;

        Texture2D collisionSprite;
        Texture2D redTexture;

        double timeSpeedupConstant = 3;

        int tickCount = 0;
        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;

            worldContext = new WorldContext();

            this.IsFixedTimeStep = true;
            this.TargetElapsedTime = TimeSpan.FromSeconds(1/180d);
        }


        protected override void Initialize()
        {
            // TODO: Add your initialization logic here

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            entityTexture = new Texture2D(GraphicsDevice, 1, 1);
            entityTexture.SetData<Color>(new Color[] {Color.Black});

            collisionSprite = new Texture2D(GraphicsDevice, 1, 1);
            collisionSprite.SetData<Color>(new Color[] {Color.Green});

            redTexture = new Texture2D(GraphicsDevice, 1, 1);
            redTexture.SetData<Color>(new Color[] { Color.Red });

            blockTextures = Texture2D.FromFile(GraphicsDevice, AppDomain.CurrentDomain.BaseDirectory + "Content\\blockSpriteSheet.png") ;

            ariel = Content.Load<SpriteFont>("Ariel");
            // TODO: use this.Content to load your game content here
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            updateEntity(gameTime);
            updatePhysicsObjects(gameTime);

            base.Update(gameTime);
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
            tickCount++;
            if (tickCount > 3)
            {
                worldContext.controlledEntity.onInput(gameTime);
                tickCount = 0;
            }
            
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            drawBlocks();
            drawEntities();
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
                _spriteBatch.DrawString(ariel, (Math.Truncate(firstEntity.reward * 100) / 100).ToString() + " | " + firstEntity.hitTargetReward, new Vector2(36, 50), Color.Blue);
                _spriteBatch.DrawString(ariel, (Math.Truncate(firstEntity.maxEstimatedReward * 100) / 100).ToString(), new Vector2(36,70), Color.Blue);
                _spriteBatch.DrawString(ariel, firstEntity.actionIndex.ToString(), new Vector2(36, 90), Color.Blue);
                _spriteBatch.DrawString(ariel, firstEntity.samples.Count.ToString(), new Vector2(36, 110), Color.Blue);


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
    }
    public class AiEntity : PhysicsObject {

        public List<Sample> samples = new List<Sample>();
        public double reward;
        public double priorReward;

        public Target t = new Target();
        NeuralNet neuralNet;
        int tickCount = 0;
        double maxUpdateDuration = 0.01;
        double updateDuration;

        double maxEraDuration = 1.5;
        double eraDuration;

        public double greedyEpsilon = 1;
        double greedyEpslionDecay = 0.98;

        public double hitTargetReward = 0;



        public double actionIndex;
        public double maxEstimatedReward;
        public int era;

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
            neuralNet = new NeuralNet(new int[] {8, 15,15, 15, 2});

            collider = new Rectangle(0, 0, (int)(width * worldContext.pixelsPerBlock), (int)(height * worldContext.pixelsPerBlock));

            worldContext.physicsObjects.Add(this);
            eraDuration = maxEraDuration;
        }

        public override void updateLocation(double xChange, double yChange)
        {
            base.updateLocation(xChange, yChange);


            reward = distanceRewardConst / Math.Pow(Math.Pow(t.x - x, 2) + Math.Pow(t.y - y, 2), 0.5) + hitTargetReward;

            if (x >= t.x - 10 && x <= t.x + 10)
            {
                t.randomiseLocation();
                hitTargetReward += 50;
            }

            if (samples.Count > 2)
            {

                samples[samples.Count - 1].rewardChange = reward - priorReward;


            }
        }

        public void onInput(GameTime gameTime) {
            if (eraDuration >= 0)
            {
         

                //My current issue is that the AI is only aware of the immediate difference and cannot see very far into the future. So I do need to re-add all that in
                //Now that I've seen that the model can learn to do something
                //It's predictions got very very far off track near the end intruigingly

                eraDuration -= gameTime.ElapsedGameTime.TotalSeconds;
                //calculate neural net output
                //Then act based on that
                double[,] input = new double[1, 8];
                input[0, 0] = ((x / (worldContext.pixelsPerBlock)) * worldContext.engineController.physicsEngine.blockSizeInMeters);
                input[0, 1] = ((y / (worldContext.pixelsPerBlock)) * worldContext.engineController.physicsEngine.blockSizeInMeters);
                input[0, 2] = velocityX;
                input[0, 3] = velocityY;
                input[0, 4] = accelerationX;
                input[0, 5] = accelerationY;
                input[0, 6] = ((t.x / (worldContext.pixelsPerBlock)) * worldContext.engineController.physicsEngine.blockSizeInMeters);
                input[0, 7] = ((t.y / (worldContext.pixelsPerBlock)) * worldContext.engineController.physicsEngine.blockSizeInMeters); ;
                double[,] neuralNetOutput = neuralNet.calculateNeuralNet(input); //or somesuch

                double maxValue = neuralNetOutput[0,0];
                int maxValueIndex = 0;
                for (int i = 0; i < neuralNetOutput.GetLength(1); i++)
                {
                    if (maxValue < neuralNetOutput[0, i])
                    {
                        maxValue = neuralNetOutput[0, i];
                        maxValueIndex = i;
                    }
                }
                
                
                //Calculate greedy epsilon:
                Random r = new Random();

                if (r.NextDouble() < greedyEpsilon)
                {
                    //Set a random action
                    maxValueIndex = r.Next(0, neuralNetOutput.GetLength(1));
                    maxValue = neuralNetOutput[0, maxValueIndex];
                }

                actionIndex = maxValueIndex;
                maxEstimatedReward = maxValue;

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
                    accelerationY += 3 / gameTime.ElapsedGameTime.TotalSeconds;
                    actionIndex = 2;
                }

                //The reward is equal to 1/ the distance to the target
                reward = distanceRewardConst / Math.Pow(Math.Pow(t.x - x, 2) + Math.Pow(t.y - y, 2), 0.5);

                //Instead, take a sample every 1 second

                if (greedyEpsilon < 0.1) {
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
                        samples[samples.Count - 1].nextState = input;
                    }

                    priorReward = reward;
                    hitTargetReward = 0;
                    if (samples.Count > 1)
                    {
                        neuralNet.updateGradientsFromSample(samples[samples.Count - 2]);
                        neuralNet.learn(1);
                    }

                    samples.Add(currentSample);

                    updateDuration = maxUpdateDuration;
                } else
                {
                    updateDuration -= gameTime.ElapsedGameTime.TotalSeconds;

                }                
            }
            else {
                greedyEpsilon *= greedyEpslionDecay;
                era += 1;
                //What I'll do, is just update but using the methods, ignoring the samples

                //The era ended: Compute all of the gradient changes and such:
                //Ignore the last sample as it has incomplete information
                for (int i = 0; i < samples.Count - 2; i++)
                {
                    //for each sample:
                    //neuralNet.updateGradientsFromSample(samples[i]);
                }
                
                    //neuralNet.learn(samples.Count);
                

                //Reset the agent's location:
                t.x = 300;
                x = 200;
                y = 0;
                velocityX = 0;
                velocityY = 0;
                accelerationY = 0;
                accelerationX = 0;
                
                eraDuration = maxEraDuration;
                samples.Clear();
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

        
        double learnRate = 0.0000001;
        double futureRewardDiscount = 0.5;

        double[] actionCosts = new double[]{0, 0, 0};
        double leakyReluConstant = 0.01;

        double[,] input;
        public NeuralNet(int[] layerNodeNumbers)
        {
            nodeLayers = new NodeLayer[layerNodeNumbers.Length - 1];
            for (int i = 1; i < layerNodeNumbers.Length; i++) {
                nodeLayers[i - 1] = (new NodeLayer(layerNodeNumbers[i], layerNodeNumbers[i-1]));
            }
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
                nodeLayers[0].output = reluActivationFunction(nodeLayers[0].weightedInput);

                //Forward propogate for each layer
                for (int i = 1; i < nodeLayers.Length - 1; i++)
                {
                    //Breaks up the calculation into two arrays to capture the inputs and the activation for easier back propigation
                    nodeLayers[i].weightedInput = calculateLayerWeightedInput(nodeLayers[i - 1].output, nodeLayers[i].weights, nodeLayers[i].biases);
                    nodeLayers[i].output = reluActivationFunction(nodeLayers[i].weightedInput);
                }

                //For the last layer: don't pass it through an activation function: leave it linear:
                nodeLayers[nodeLayers.Length - 1].weightedInput = calculateLayerWeightedInput(nodeLayers[nodeLayers.Length - 2].output, nodeLayers[nodeLayers.Length - 1].weights, nodeLayers[nodeLayers.Length - 1].biases);
                nodeLayers[nodeLayers.Length - 1].output = nodeLayers[nodeLayers.Length - 1].weightedInput;

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
            System.Diagnostics.Debug.WriteLine(error * error);
            return error * error;
        }



        //The output layer bias gradient descent is just the node Values, while the weights are the node values * inputValues
        public void updateGradientsFromSample(Sample s) {
            //calculate the expected output by passing in the sample's next state through the neural net:
            double[,] expectedNeuralNetOutput = calculateNeuralNet(s.nextState);
            //Convert to a 1D array:
            //pass the sample's current position data to flush the inputs/outputs:
            //Compute the optimal reward through the bellman equation:
            calculateNeuralNet(s.state);

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
                s.rewardChange = 0;
            }
            else if (s.rewardChange < 0) {
                s.rewardChange *= 1; //Really punish it
            }
            double optimalReward = (s.rewardChange- actionCosts[s.actionIndex]) + futureRewardDiscount * maxValue;

            //double optimalReward = s.rewardChange;

            //The index is wrong for this :(
            updateOnePath(optimalReward, s.actionIndex);
            /*
            if (s.rewardChange > 0)
            {
                updateOnePath(100, s.actionIndex);
                updateOnePath(0, 1 - s.actionIndex);
            }
            else {//It was a bad decision:
                updateOnePath(0, s.actionIndex);
                updateOnePath(100, 1-s.actionIndex);
            }*/
            
            
            //You don't update all the gradients: only the ones connected the the output that got chosen
        }
        public void updateAllGradients(double[,] expectedNetOutput) {
            NodeLayer outputLayer = nodeLayers[nodeLayers.Length  - 1];
            double[,] nodeValues = outputLayer.calculateNodeValues(expectedNetOutput);
            outputLayer.updateGradientDescent(nodeLayers[nodeLayers.Length - 2].output, nodeValues);
            //An error was occurring because the output layer was linear, but all the previous layers were sigmoid activated, the node values were breaking everything
            nodeValues = outputLayer.reluCalculatedNodeValues(nodeValues);
            for (int i = nodeLayers.Length - 2; i >= 1; i--) {
                nodeValues = nodeLayers[i].calculateHiddenLayerNodeValues(nodeLayers[i + 1], nodeValues);
                nodeLayers[i].updateGradientDescent(nodeLayers[i-1].output, nodeValues);
            }

            //Because the layer doesn't store the inputs, there has to be a final update for the first hidden layer that takes in the net's input
            nodeValues = nodeLayers[0].calculateHiddenLayerNodeValues(nodeLayers[1], nodeValues);
            nodeLayers[0].updateGradientDescent(input, nodeValues);

        }
        public void updateOnePath(double expectedOutput, int outputPathIndex) {

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
            double[,] nodeValues = outputLayer.calculateNodeValues(zeroedExpectedOutput);
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

        public double individualCostDerivative(double actual, double expected)
        {
            return 2 * (actual - expected);
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



        public double[,] calculateNodeValues(double[,] expected)
        {
            double[,] nodeValues = new double[expected.GetLength(0), expected.GetLength(1)];
            for (int x = 0; x < nodeValues.GetLength(0); x++)
            {
                for (int y = 0; y < nodeValues.GetLength(1); y++)
                {
                    nodeValues[x, y] = individualCostDerivative(output[x, y], expected[x, y]);
                }
            }
            //Linearise the outputs by removing the sigmoid function:
            /*
            double[,] sigmoidDerivative = sigmoidActivationDerivative(weightedInput);
            for (int x = 0; x < nodeValues.GetLength(0); x++)
            {
                for (int y = 0; y < nodeValues.GetLength(1); y++)
                {
                    nodeValues[x, y] *= sigmoidDerivative[x, y];
                }
            }*/
            return nodeValues;
        }

        public double[,] sigmoidCalculatedNodeValues(double[,] nodeValues) {
            double[,] sigmoidDerivative = sigmoidActivationDerivative(weightedInput);
            for (int x = 0; x < nodeValues.GetLength(0); x++)
            {
                for (int y = 0; y < nodeValues.GetLength(1); y++)
                {
                    nodeValues[x, y] *= sigmoidDerivative[x, y];
                }
            }
            return nodeValues;
        }

        public double[,] reluCalculatedNodeValues(double[,] nodeValues)
        {
            double[,] reluDerivative = reluActivationDerivative(weightedInput);
            for (int x = 0; x < nodeValues.GetLength(0); x++)
            {
                for (int y = 0; y < nodeValues.GetLength(1); y++)
                {
                    nodeValues[x, y] *= reluDerivative[x, y];
                }
            }
            return nodeValues;
        }

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
            //Last step: Multiply by the derivative of the sigmoid weighted inputs
            double[,] derivativeOfActivationFunction = reluActivationDerivative(weightedInput);
            for (int x = 0; x < output.GetLength(0); x++) {
                for (int y = 0; y < output.GetLength(1); y++) {
                    newNodeValues[x, y] *= derivativeOfActivationFunction[x, y];
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
    }
}
