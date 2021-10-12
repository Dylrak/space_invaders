using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace SpaceInvadersProgrammingSpeedrunAnyPercent
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private BitmapImage player_model;
        private Image player;

        private BitmapImage enemy_model;
        private List<Image> enemies = new List<Image>();

        private BitmapImage bullet_model;
        private List<Image> bullets = new List<Image>();

        private BitmapImage enemy_bullet_model;
        private List<Image> enemy_bullets = new List<Image>();

        private DispatcherTimer gameTimer = new DispatcherTimer();

        private readonly int framerate = 60;

        //direction booleans
        private bool go_left = false;
        private bool go_right = false;

        //horizontal player position
        private double player_pos = 350;

        private readonly double player_height = 350;

        private double enemy_direction = 0.01;

        Random rng = new Random();

        public MainWindow()
        {
            InitializeComponent();

            this.Loaded += MainWindow_Loaded;
            this.KeyDown += Window_KeyDown;
            this.KeyUp += Window_KeyUp;

        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {

            //Load bitmaps
            player_model = new BitmapImage(new Uri("pack://application:,,,/Images/player.png"));
            enemy_model = new BitmapImage(new Uri("pack://application:,,,/Images/enemy.png"));
            bullet_model = new BitmapImage(new Uri("pack://application:,,,/Images/bullet.png"));
            enemy_bullet_model = new BitmapImage(new Uri("pack://application:,,,/Images/enemy_bullet.png"));

            //Create player
            player = new Image() { Source = player_model };
            //Add player to canvas
            gameCanvas.Children.Add(player);
            //Set player position
            Canvas.SetTop(player, player_height);
            Canvas.SetLeft(player, player_pos);

            //Create enemies
            for (int y = 50; y < 250; y += 100)
            {
                for (int x = 100; x <= 600; x += 100)
                {
                    Image enemy = new Image() { Source = enemy_model };
                    enemies.Add(enemy);
                    gameCanvas.Children.Add(enemy);

                    Canvas.SetTop(enemy, y);
                    Canvas.SetLeft(enemy, x);
                }
            }

            //Start the game timer:
            gameTimer.Interval = TimeSpan.FromMilliseconds(1.0 / framerate);
            gameTimer.Tick += GameTick;
            gameTimer.Start();
        }

        private void shoot_bullet()
        {
            Image bullet = new Image() { Source = bullet_model };
            Canvas.SetTop(bullet, player_height);
            Canvas.SetLeft(bullet, player_pos);
            bullets.Add(bullet);
            gameCanvas.Children.Add(bullet);
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.A)
            {
                go_left = true;
            }
            if (e.Key == Key.D)
            {
                go_right = true;
            }
            if (e.Key == Key.Space)
            {
                shoot_bullet();
            }
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.A)
            {
                go_left = false;
            }
            if (e.Key == Key.D)
            {
                go_right = false;
            }
        }

        public void GameTick(object sender, EventArgs e)
        {
            #region update_player_pos
            //Only update horizontal player position if either go_left or go_right is true (XOR):
            if (go_left ^ go_right)
            {
                if (go_left && player_pos > 0.1) { player_pos -= 0.1; }
                if (go_right && player_pos < 765) { player_pos += 0.1; }
                Canvas.SetLeft(player, player_pos);
            }
            #endregion

            List<Image> bullets_to_remove = new List<Image>();

            #region update_bullets
            foreach (Image bullet in bullets)
            {
                double bullet_height = Canvas.GetTop(bullet);
                //If the bullet is outside of the screen, remove it later (add to list and remove after foreach so bullets list won't be affected mid-loop)
                if (bullet_height < 0)
                {
                    bullets_to_remove.Add(bullet);
                }
                Canvas.SetTop(bullet, Canvas.GetTop(bullet) - 0.1);
            }
            Rect player_hitbox = get_hitbox(player);
            foreach (Image bullet in enemy_bullets)
            {
                double bullet_height = Canvas.GetTop(bullet);
                //If the bullet is outside of the screen, remove it later (add to list and remove after foreach so bullets list won't be affected mid-loop)
                if (bullet_height > 800)
                {
                    bullets_to_remove.Add(bullet);
                }
                Canvas.SetTop(bullet, Canvas.GetTop(bullet) + 0.1);

                Rect bullet_hitbox = get_hitbox(bullet);
                if (bullet_hitbox.IntersectsWith(player_hitbox)) {
                    end_game(false);
                }
            }
            #endregion

            #region check_enemy_direction
            //If enemies are moving right
            if (enemy_direction > 0)
            {
                Image rightmost_enemy = get_rightmost_enemy();

                //if the rightmost enemy is out of bounds
                if (Canvas.GetLeft(rightmost_enemy) >= 800 - 25)
                {
                    change_enemy_direction();
                }
            } else //If enemies are moving left
            {
                Image leftmost_enemy = get_leftmost_enemy();

                //if the leftmost enemy is out of bounds
                if (Canvas.GetLeft(leftmost_enemy) < 0)
                {
                    change_enemy_direction();
                }
            }
            #endregion
            //Randomly choose to shoot one enemy bullet
            if (rng.Next(69, 1337) == 420)
            {
                shoot_enemy_bullet();
            }

            #region move_enemies
            foreach (Image enemy in enemies)
            {
                //Move enemy to the side
                Canvas.SetLeft(enemy, Canvas.GetLeft(enemy) + enemy_direction);

            }
            #endregion

            List<Image> enemies_to_remove = new List<Image>();

            #region kill_enemies
            foreach (Image enemy in enemies)
            {
                Rect enemy_hitbox = get_hitbox(enemy);
                foreach (Image bullet in bullets)
                {
                    //calculate bullet hitbox
                    Rect bullet_hitbox = get_hitbox(bullet);

                    //If a bullet hits an enemy, remove both the bullet and the enemy
                    if (bullet_hitbox.IntersectsWith(enemy_hitbox)) {
                        enemies_to_remove.Add(enemy);
                        bullets_to_remove.Add(bullet);
                    }

                }
            }
            #endregion

            //Remove bullets
            foreach (Image bullet in bullets_to_remove)
            {
                bullets.Remove(bullet);
                gameCanvas.Children.Remove(bullet);
            }

            //Remove enemies
            foreach (Image enemy in enemies_to_remove)
            {
                enemies.Remove(enemy);
                gameCanvas.Children.Remove(enemy);
            }

            if (enemies.Count == 0)
            {
                end_game(true);
            }
        }

        private Image get_leftmost_enemy()
        {
            Image leftmost_enemy = enemies[0];

            foreach (Image enemy in enemies)
            {
                if (Canvas.GetLeft(enemy) < Canvas.GetLeft(leftmost_enemy))
                {
                    leftmost_enemy = enemy;
                }
            }
            return leftmost_enemy;
        }

        private Image get_rightmost_enemy()
        {
            Image rightmost_enemy = enemies[0];

            foreach (Image enemy in enemies)
            {
                if (Canvas.GetLeft(enemy) > Canvas.GetLeft(rightmost_enemy))
                {
                    rightmost_enemy = enemy;
                }
            }
            return rightmost_enemy;
        }

        private void shoot_enemy_bullet()
        {

            int chosen_enemy = rng.Next(0, enemies.Count);

            Image bullet = new Image() { Source = enemy_bullet_model };
            enemy_bullets.Add(bullet);
            gameCanvas.Children.Add(bullet);
            Canvas.SetTop(bullet, Canvas.GetTop(enemies[chosen_enemy]));
            Canvas.SetLeft(bullet, Canvas.GetLeft(enemies[chosen_enemy]));
        }

        private void change_enemy_direction()
        {
            if (Canvas.GetTop(enemies[0]) + 20 > 250)
            {
                end_game(false);
            }
            //increase enemy speed
            if (enemy_direction > 0)
            {
                enemy_direction += 0.01;
            }
            else
            { 
                enemy_direction -= 0.01;
            }
            //flip the enemy direction 
            enemy_direction = -enemy_direction;
            //move all enemies down one step
            foreach (Image enemy in enemies)
            {
                Canvas.SetTop(enemy, Canvas.GetTop(enemy) + 20);
            }
        }

        private void end_game(bool won)
        {
            endGameMessage.Visibility = Visibility.Visible;
            if (won)
            {
                endGameMessage.Text = "You won!";
            }
            else
            {
                endGameMessage.Text = "You lost ;_;";
            }
            gameTimer.Stop();
        }

        private Rect get_hitbox(Image img)
        {
            Point location = new Point(Canvas.GetLeft(img), Canvas.GetTop(img));
            Size size = new Size(img.ActualWidth, img.ActualHeight);
            return new Rect(location, size);
        }
    }
}
