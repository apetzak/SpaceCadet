using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Media;

namespace SpaceCadet
{
    public partial class MainWindow : Window
    {
        public static Game game = new Game();

        public MainWindow()
        {
            InitializeComponent();
            game.Canvas = canvas;
            game.Grid = grid;
            game.AddLabel(new Label(), "0", 1, 90);          
        }

        ///////////////////////////////////////////////////////////////////////////////////////////
        // Controls

        public void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Left || e.Key == Key.A)
            {
                game.LeftIsPressed = true;
                game.Ship.MovingDirection = 4;
            }              
            if (e.Key == Key.Right || e.Key == Key.D)
            {
                game.RightIsPressed = true;
                game.Ship.MovingDirection = 2;
            } 
            if (e.Key == Key.Space)
            {
                if (!game.IsActive)
                    game.Restart();
                else
                    game.SpaceIsPressed = true;
            }
            if (e.Key == Key.S && game.IsActive)
            {
                if (game.SoundEnabled)
                {
                    game.SoundEnabled = false;
                    //game.AddLabel(new Label(), "Mute", 3, 110);
                }
                else
                {
                    game.SoundEnabled = true;
                    //game.Grid.Children.RemoveAt(2);
                }

            }
            if (e.Key == Key.W && game.IsActive == true)
            {
                if (!game.IsPaused)
                {
                    game.IsPaused = true;
                    game.Timer.Stop();
                    game.AddLabel(new Label(), "Paused", 0, 110);
                }    
                else
                {
                    game.Grid.Children.RemoveAt(2);
                    game.IsPaused = false;
                    game.Timer.Start();
                }         
            }                
        }

        public void MainWindow_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Left || e.Key == Key.A)
            {
                game.Ship.MovingDirection = 0;
                if (game.RightIsPressed == true)
                    game.Ship.MovingDirection = 2;             
                game.LeftIsPressed = false;
            }
            else if (e.Key == Key.Right || e.Key == Key.D)
            {
                game.Ship.MovingDirection = 0;
                if (game.LeftIsPressed == true)
                    game.Ship.MovingDirection = 4;       
                game.RightIsPressed = false;
            }
            if (e.Key == Key.Space)
                game.SpaceIsPressed = false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        public class Game
        {
            public bool IsPaused = false;
            public bool IsActive = false;
            public bool BossMode = false;
            public bool SoundEnabled = true;    
            public int ReloadTimer = 0;
            public int EnemyReloadTimer = 0;
            public int Score = 0;
            public bool SIsPressed = false;
            public bool SpaceIsPressed = false;
            public bool LeftIsPressed = false;
            public bool RightIsPressed = false;
            public DispatcherTimer Timer = new DispatcherTimer();
            public Grid Grid = new Grid();
            public Canvas Canvas = new Canvas();
            public Label lblScore = new Label();
            public List<GameObject> PlayerObjects = new List<GameObject>();
            public List<Enemy> Enemies = new List<Enemy>();
            public List<GameObject> EnemyBullets = new List<GameObject>();
            public List<Explosion> Explosions = new List<Explosion>();
            public List<Squadron> Squadrons = new List<Squadron>();
            public Ship Ship = new Ship(Colors.Silver);

            public Game()
            {
                Timer.Interval = TimeSpan.FromMilliseconds(.5);
                Timer.Tick += Timer_Tick;
                Timer.Start();
                PlayerObjects.Add(Ship);
                Squadrons.Add(new Squadron());
                foreach (Enemy e in Squadrons[0].Enemies)
                    Enemies.Add(e);
                IsActive = true;
            }

            public void Timer_Tick(object source, EventArgs e)
            {
                SpawnEnemies();
                CheckForPlayerHit();
                CheckForEnemyHit();
                Canvas.Children.Clear();
                MoveSquadron();
                DrawObjects();
                PlayerShoot();
                EnemyShoot();
                RemoveBullets();
                RemoveExplosions();
                Transform();                               
            }

            public void SpawnEnemies()
            {
                if (Enemies.Count < 4)
                {
                    Squadron s = new Squadron();
                    Squadrons.Add(s);
                    foreach (Enemy e in s.Enemies)
                        Enemies.Add(e);
                }
            }

            public void CheckForEnemyHit()
            {
                if (!BossMode)
                {
                    for (int i = Enemies.Count - 1; i >= 0; i--)
                        foreach (Pixel p in Enemies[i].Pixels)
                            for (int i2 = Ship.Bullets.Count - 1; i2 >= 0; i2--)
                                if (Ship.Bullets[i2].Pixels[0].X == p.X && Ship.Bullets[i2].Pixels[0].Y == p.Y)
                                {
                                    PlayerObjects.Remove(Ship.Bullets[i2]);
                                    Ship.Bullets.RemoveAt(i2);
                                    Explosions.Add(new Explosion(Enemies[i].X, Enemies[i].Y));

                                    foreach (Squadron s in Squadrons)
                                        for (int _i = s.Enemies.Count - 1; _i >= 0; _i--)
                                            if (s.Enemies[_i] == Enemies[i])
                                                s.Enemies.RemoveAt(_i);

                                    Enemies.RemoveAt(i);
                                    if (game.IsActive)
                                    {
                                        PlaySound("explosion");
                                        Score++;
                                        Grid.Children.RemoveAt(1);
                                        AddLabel(new Label(), Score.ToString(), 1, 90);
                                    }
                                }

                    for (int i = Squadrons.Count - 1; i >= 0; i--)
                        if (Squadrons[i].Enemies.Count == 0)
                            Squadrons.RemoveAt(i);
                }
                else
                {

                }
            }

            public void CheckForPlayerHit()
            {
                foreach (Pixel P in Ship.Pixels)
                    foreach (GameObject b in EnemyBullets)
                        if (P.X == b.Pixels[0].X && P.Y == b.Pixels[0].Y)
                        {
                            PlaySound("_explosion");
                            EnemyBullets.Remove(b);
                            GameOver();
                            break;
                        }                        
            }

            public void MoveSquadron()
            {
                foreach (Squadron s in Squadrons)
                    foreach (Enemy e in s.Enemies)
                        if (s.MovingDirection != e.MovingDirection)
                        {
                            foreach (Enemy en in s.Enemies)
                                en.MovingDirection = e.MovingDirection;
                            s.MovingDirection = e.MovingDirection;
                        }
            }

            public void DrawObjects()
            {
                foreach (GameObject o in PlayerObjects)
                    o.Move(Canvas);
                foreach (Enemy e in Enemies)
                    e.Move(Canvas);
                foreach (GameObject b in EnemyBullets)
                    b.Move(Canvas);
                foreach (Explosion e in Explosions)
                    e.Move(Canvas);
            }

            public void PlayerShoot()
            {
                ReloadTimer++;
                if (SpaceIsPressed && ReloadTimer > 12 && IsActive)
                {
                    PlaySound("laser");
                    Ship.Shoot(game);
                    ReloadTimer = 0;
                }
            }

            public void EnemyShoot()
            {
                EnemyReloadTimer++;
                foreach (Enemy enemy in Enemies)
                    if (enemy.Fire == true && EnemyReloadTimer > 12)
                    {
                        enemy.Shoot(game);
                        enemy.Fire = false;
                        EnemyReloadTimer = 0;
                    }
            }

            public void RemoveBullets()
            {
                for (int i = Ship.Bullets.Count - 1; i >= 0; i--)
                    if (Ship.Bullets[i].Pixels[1].Y > 500)
                        Ship.Bullets.RemoveAt(i);

                for (int i = PlayerObjects.Count - 1; i >= 0; i--)
                    if (PlayerObjects[i].Pixels[0].Y < 0 || PlayerObjects[i].Pixels[0].Y > 550)
                        PlayerObjects.RemoveAt(i);

                for (int i = EnemyBullets.Count - 1; i >= 0; i--)
                    if (EnemyBullets[i].Pixels[0].Y < 0)
                        EnemyBullets.RemoveAt(i);
            }

            public void RemoveExplosions()
            {
                for (int i = Explosions.Count - 1; i >= 0; i--)             
                    if (Explosions[i].Counter > 25)                
                        Explosions.RemoveAt(i);
            }

            public void Transform()
            {
                if (Squadrons.Count == 4 && Enemies.Count == 4)
                    if ((Squadrons[0].Color != Squadrons[1].Color) && (Squadrons[0].Color != Squadrons[2].Color) && (Squadrons[0].Color != Squadrons[3].Color)
                     && (Squadrons[1].Color != Squadrons[2].Color) && (Squadrons[1].Color != Squadrons[3].Color) && (Squadrons[2].Color != Squadrons[3].Color))
                    {
                        BossMode = true;
                        foreach (Enemy e in Enemies)
                            e.Transform = true;
                    }
            }

            /////////////////////////////////////

            public void PlaySound(string file)
            {
                if (SoundEnabled)
                {
                    string path = @"C:\Users\Alec\Documents\Visual Studio 2015\Projects\Games\SpaceCadet\SpaceCadet\Sounds\" + file + ".wav";
                    SoundPlayer player = new SoundPlayer(path);
                    player.Load();
                    player.Play();
                }
            }

            public void GameOver()
            {           
                Explosions.Add(new Explosion(Ship.Pixels[6].X, Ship.Pixels[6].Y));
                IsActive = false;
                Ship.Pixels = new List<Pixel>();
                Ship.Pixels.Add(new Pixel(-50, 0));
                PlayerObjects.Remove(Ship);
                AddLabel(new Label(), "Game Over", 0, 160);
            }

            public void Restart()
            {
                Score = 0;
                PlayerObjects = new List<GameObject>();
                Enemies = new List<Enemy>();
                EnemyBullets = new List<GameObject>();
                Explosions = new List<Explosion>();
                Ship = new Ship(Colors.Silver);
                Grid.Children.RemoveAt(1);
                Grid.Children.RemoveAt(1);
                PlayerObjects.Add(Ship);
                IsActive = true;
                AddLabel(new Label(), "0", 1, 90);
            }

            public void AddLabel(Label lbl, string content, int ha, int width)
            {
                SolidColorBrush white = new SolidColorBrush(Color.FromRgb(180, 180, 180));
                lbl.Content = content;
                lbl.Foreground = white;
                lbl.FontSize = 30;
                lbl.Width = width;
                if (ha == 1)
                    lbl.HorizontalAlignment = HorizontalAlignment.Right;
                else if (ha == 2)
                    lbl.HorizontalAlignment = HorizontalAlignment.Center;
                else if (ha == 3)
                    lbl.HorizontalAlignment = HorizontalAlignment.Left;
                Canvas.SetZIndex(lbl, 4);
                Grid.Children.Add(lbl);
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        public class Pixel
        {
            public int X { get; set; }
            public int Y { get; set; }

            public Pixel(int x, int y)
            {
                X = x;
                Y = y;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        public class GameObject
        {
            public Color Color { get; set; }
            public List<Pixel> Pixels = new List<Pixel>();
            public int MovingDirection;
            public int ZIndex = 1;

            public GameObject() { }
            public GameObject(Color color)
            {
                Color = color;
                Pixels = pixels();
            }

            public virtual void Draw(Canvas canvas)
            {
                foreach (Pixel p in this.Pixels)
                {
                    Rectangle r = new Rectangle();
                    r.Fill = new SolidColorBrush(Color);
                    r.Width = 6;
                    r.Height = 6;
                    Canvas.SetLeft(r, p.X);
                    Canvas.SetBottom(r, p.Y);
                    Canvas.SetZIndex(r, ZIndex);
                    canvas.Children.Add(r);
                }
            }

            public virtual void Move(Canvas canvas)
            {
                if (MovingDirection == 1)
                    foreach (Pixel p in Pixels)
                        p.Y = p.Y + 10;                 
                if (MovingDirection == 2)        
                    foreach (Pixel p in Pixels)
                        p.X = p.X + 5;                
                if (MovingDirection == 3)
                    foreach (Pixel p in Pixels)
                        p.Y = p.Y - 5;                 
                if (MovingDirection == 4)
                    foreach (Pixel p in Pixels)
                        p.X = p.X - 5;

                Draw(canvas);
            }

            public virtual List<Pixel> pixels()
            {
                return new List<Pixel>();
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        public class Ship : GameObject
        {
            public List<GameObject> Bullets = new List<GameObject>();
            public int BulletDirection;
            public int BulletStartPosition;
            public Color BulletColor = Colors.Red;

            public Ship() { }
            public Ship(Color color)
            {
                Color = color;
                Pixels = pixels();
                ZIndex = 3;
                BulletDirection = 1;
                BulletStartPosition = 5;
            }

            public override void Move(Canvas canvas)
            {
                if ((Pixels[16].X > 0 && MovingDirection == 4) || (Pixels[24].X < 495 && MovingDirection == 2))
                {
                    if (MovingDirection == 2)
                        foreach (Pixel p in Pixels)
                            p.X = p.X + 5;
                    else if (MovingDirection == 4)
                        foreach (Pixel p in Pixels)
                            p.X = p.X - 5;
                    Draw(canvas);
                }
                else
                {
                    MovingDirection = 0;
                    Draw(canvas);
                }
            }

            public void Shoot(Game game)
            {
                Pixel p1 = new Pixel(Pixels[0].X, Pixels[0].Y);
                Pixel p2 = new Pixel(Pixels[0].X, Pixels[0].Y + BulletStartPosition);
                GameObject bullet = new GameObject();
                bullet.MovingDirection = BulletDirection;
                bullet.Pixels.Add(p1);
                bullet.Pixels.Add(p2);
                bullet.Color = BulletColor;
                Bullets.Add(bullet);
                if (bullet.Color == Colors.Red)
                    game.PlayerObjects.Add(bullet);
                if (bullet.Color == Colors.Lime)
                    game.EnemyBullets.Add(bullet);              
            }

            public override List<Pixel> pixels()
            {
                List<Pixel> p = new List<Pixel>();
                BuildRow(p, 250, 35, 1);
                BuildRow(p, 250, 30, 3);
                BuildRow(p, 250, 25, 5);
                BuildRow(p, 250, 20, 7);
                BuildRow(p, 250, 15, 9);
                BuildRow(p, 250, 10, 3);
                return p;
            }

            public void BuildRow(List<Pixel> p, int x, int y, int length)
            {
                int _x = x - ((length - 1) / 2) * 5;
                for (int i = 0; i < length; i++)
                {
                    p.Add(new Pixel(_x, y));
                    _x = _x + 5;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        public class Enemy : Ship
        {
            public double MoveTimer = 0;
            public static Random rand = new Random();
            public int DelayCounter = 0;
            public int DelayEnd = rand.Next(0, 100);
            public bool Fire = false;
            public int X;
            public int Y;
            public bool Transform = false;

            public Enemy() { }
            public Enemy(Color color, int x, int y, int moveDirection)
            {
                X = x;
                Y = y;
                Color = color;
                Pixels = pixels();
                ZIndex = 2;
                BulletDirection = 3;
                BulletColor = Colors.Lime;
                MovingDirection = moveDirection;
                BulletStartPosition = -5;
            }

            public override void Move(Canvas canvas)
            {
                DelayCounter++;
                if (DelayCounter == DelayEnd)
                {
                    Fire = true;
                    DelayEnd = rand.Next(50, 100);
                    DelayCounter = 0;
                }

                if (!Transform)
                {
                    if (MovingDirection == 2 && X > 450)
                        MovingDirection = 4;
                    else if (MovingDirection == 3 && Y < 200)
                        MovingDirection = 1;
                    else if (MovingDirection == 4 && X < 50)
                        MovingDirection = 2;
                    else if (MovingDirection == 1 && Y > 450)
                        MovingDirection = rand.Next(2, 3);
                }
                else
                {
                    if (X > 250)
                        MovingDirection = 4;
                    else if (X < 250)
                        MovingDirection = 2;
                    else if (Y > 300)
                        MovingDirection = 3;
                    else if (Y < 300)
                        MovingDirection = 1;
                    else if (X == 250 && Y == 300)
                        MovingDirection = 0;
                }

                MoveTimer = MoveTimer + .8;
                if (MoveTimer > 1)
                {
                    if (MovingDirection == 1)
                        foreach (Pixel p in Pixels)
                            p.Y = p.Y + 5;
                    if (MovingDirection == 2)
                        foreach (Pixel p in Pixels)
                            p.X = p.X + 5;
                    if (MovingDirection == 3)
                        foreach (Pixel p in Pixels)
                            p.Y = p.Y - 5;
                    if (MovingDirection == 4)
                        foreach (Pixel p in Pixels)
                            p.X = p.X - 5;

                    MoveTimer = 0;
                    X = Pixels[6].X;
                    Y = Pixels[6].Y;
                }
                Draw(canvas);
            }

            public override List<Pixel> pixels()
            {
                List<Pixel> p = new List<Pixel>();
                BuildRow(p, X, Y - 10, 1);
                BuildRow(p, X, Y - 5, 3);               
                BuildRow(p, X, Y + 0 , 5);
                BuildRow(p, X, Y + 5, 7);
                p.Add(new Pixel(X + 15, Y + 10));
                p.Add(new Pixel(X - 15, Y + 10));
                return p;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        public class Squadron
        {
            public List<Enemy> Enemies = new List<Enemy>();
            static Random rand = new Random();
            int FormationWidth = rand.Next(2, 5);
            int FormationHeight = rand.Next(1, 2);
            public int MovingDirection = 2;
            public Pixel SpawnLocation = new Pixel(0, 0);
            public Color Color;
            
            public Squadron()
            { 
                if (FormationWidth >= 4)
                    FormationHeight = 1;
                SetSpawnLocation();
                SetColor();
                BuildFormation();
            }

            public void SetSpawnLocation()
            {
                Random r = new Random();
                int location = r.Next(0, 8);
                SetLocation(location, 0, -50, 200, 2);
                SetLocation(location, 1, -50, 300, 2);
                SetLocation(location, 2, -50, 350, 2);
                SetLocation(location, 3, 250, 550, 3);
                SetLocation(location, 4, 325, 550, 3);
                SetLocation(location, 5, 400, 550, 3);
                SetLocation(location, 6, 850, 200, 4);
                SetLocation(location, 7, 850, 300, 4);
                SetLocation(location, 8, 850, 350, 4);
            }

            public void SetLocation(int location, int temp, int x, int y, int moveDirection)
            {
                if (location == temp)
                {
                    SpawnLocation.X = x;
                    SpawnLocation.Y = y;
                    MovingDirection = moveDirection;
                }
            }

            public void SetColor()
            {
                int color = rand.Next(0, 6);
                if (color == 0)
                    Color = Colors.Crimson;
                else if (color == 1)
                    Color = Colors.OrangeRed;
                else if (color == 2)
                    Color = Colors.Yellow;
                else if (color == 3)
                    Color = Colors.Green;
                else if (color == 4)
                    Color = Colors.Blue;
                else if (color == 5)
                    Color = Colors.Indigo;
                else if (color == 6)
                    Color = Colors.Violet;
            }

            public void BuildFormation()
            {
                for (int i = 0; i < FormationWidth; i++)
                {
                    Enemies.Add(new Enemy(Color, SpawnLocation.X - (55 * i), SpawnLocation.Y, MovingDirection));
                    for (int i2 = 1; i2 < FormationHeight + 1; i2++)                   
                        Enemies.Add(new Enemy(Color, SpawnLocation.X - (55 * i), SpawnLocation.Y + (55 * i2), MovingDirection));                  
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        public class Boss : Enemy
        {
            public Boss() { }
            public Boss(Color color, int x, int y, int moveDirection)
            {
                X = x;
                Y = y;
                Color = color;
                Pixels = pixels();
                ZIndex = 2;
                BulletDirection = 3;
                BulletColor = Colors.Lime;
                MovingDirection = moveDirection;
                DelayCounter = 0;
                DelayEnd = rand.Next(0, 100);
                BulletStartPosition = -5;               
            }

            public override void Move(Canvas canvas)
            {
                DelayCounter++;
                if (DelayCounter == DelayEnd)
                {
                    Fire = true;
                    DelayEnd = rand.Next(50, 100);
                    DelayCounter = 0;
                }
                Random r = new Random();

                if (MovingDirection == 2 && Pixels[6].X > 450)
                    MovingDirection = 4;
                else if (MovingDirection == 3 && Pixels[6].Y < 200)
                    MovingDirection = 1;
                else if (MovingDirection == 4 && Pixels[6].X < 50)
                    MovingDirection = 2;
                else if (MovingDirection == 1 && Pixels[6].Y > 450)
                    MovingDirection = r.Next(2, 3);

                MoveTimer = MoveTimer + .8;
                if (MoveTimer > 1)
                {
                    if (MovingDirection == 1)
                        foreach (Pixel p in Pixels)
                            p.Y = p.Y + 5;
                    if (MovingDirection == 2)
                        foreach (Pixel p in Pixels)
                            p.X = p.X + 5;
                    if (MovingDirection == 3)
                        foreach (Pixel p in Pixels)
                            p.Y = p.Y - 5;
                    if (MovingDirection == 4)
                        foreach (Pixel p in Pixels)
                            p.X = p.X - 5;

                    MoveTimer = 0;
                    X = Pixels[6].X;
                    Y = Pixels[6].Y;
                }
                Draw(canvas);
            }

            public override List<Pixel> pixels()
            {
                List<Pixel> p = new List<Pixel>();
                BuildRow(p, X, Y - 10, 1);
                BuildRow(p, X, Y + 5, 7);
                BuildRow(p, X, Y + 0, 5);
                BuildRow(p, X, Y - 5, 3);
                return p;
            }
        }


        ///////////////////////////////////////////////////////////////////////////////////////////

        public class Explosion : GameObject
        {
            public int Counter = 0;
            int X;
            int Y;

            public Explosion(int x, int y)
            {
                Color = Colors.Orange;
                Pixels = pixels();
                ZIndex = 4;
                X = x;
                Y = y;
            }

            public override void Move(Canvas canvas)
            {
                Counter++;
                Pixels = pixels();
                Draw(canvas);
            }

            public override List<Pixel> pixels()
            {
                List<Pixel> p = new List<Pixel>();
                if (Counter < 5 || Counter >= 20)
                {
                    FirstLayer(p);
                }                                            
                else if ((Counter >= 5 && Counter < 10) || (Counter >= 15 && Counter < 20))
                {
                    FirstLayer(p);
                    SecondLayer(p);
                }
                else if (Counter >= 10 && Counter < 15)
                {
                    FirstLayer(p);
                    SecondLayer(p);
                    ThirdLayer(p);
                }
                return p;
            }

            public void FirstLayer(List<Pixel> p)
            {
                p.Add(new Pixel(X - 5, Y));
                p.Add(new Pixel(X + 5, Y));
                p.Add(new Pixel(X, Y));
                p.Add(new Pixel(X, Y - 5));
                p.Add(new Pixel(X, Y + 5));
            }

            public void SecondLayer(List<Pixel> p)
            {
                p.Add(new Pixel(X - 10, Y));
                p.Add(new Pixel(X + 10, Y));
                p.Add(new Pixel(X, Y - 10));
                p.Add(new Pixel(X, Y + 10));
                p.Add(new Pixel(X + 5, Y + 5));
                p.Add(new Pixel(X + 5, Y - 5));
                p.Add(new Pixel(X - 5, Y - 5));
                p.Add(new Pixel(X - 5, Y + 5));
            }

            public void ThirdLayer(List<Pixel> p)
            {
                p.Add(new Pixel(X - 15, Y));
                p.Add(new Pixel(X + 15, Y));
                p.Add(new Pixel(X, Y - 15));
                p.Add(new Pixel(X, Y + 15));
                p.Add(new Pixel(X + 10, Y + 5));
                p.Add(new Pixel(X + 10, Y - 5));
                p.Add(new Pixel(X - 10, Y - 5));
                p.Add(new Pixel(X - 10, Y + 5));
                p.Add(new Pixel(X + 5, Y + 10));
                p.Add(new Pixel(X + 5, Y - 10));
                p.Add(new Pixel(X - 5, Y - 10));
                p.Add(new Pixel(X - 5, Y + 10));
            }
        }
    }
}