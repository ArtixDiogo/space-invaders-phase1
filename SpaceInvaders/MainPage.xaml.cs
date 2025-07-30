using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.System;
using Windows.UI;

namespace SpaceInvaders
{
    public class Barrier
    {
        public Rectangle? Visual { get; set; }
        public int Health { get; set; }
    }

    public sealed partial class MainPage : Page
    {
        private struct EnemyType
        {
            public string ImageSource { get; set; }
            public int Points { get; set; }
            public string DisplayName { get; set; }
        }

        private readonly Dictionary<string, EnemyType> _enemyTypes = new()
        {
            {"alien1", new EnemyType { ImageSource = "ms-appx:///Assets/Images/alien1.png", Points = 10, DisplayName = "Inimigo Básico" }},
            {"alien2", new EnemyType { ImageSource = "ms-appx:///Assets/Images/alien2.png", Points = 20, DisplayName = "Inimigo Intermediário" }},
            {"alien3", new EnemyType { ImageSource = "ms-appx:///Assets/Images/alien3.png", Points = 40, DisplayName = "Inimigo Avançado" }},
            {"alien4", new EnemyType { ImageSource = "ms-appx:///Assets/Images/alien4.png", Points = 200, DisplayName = "Inimigo Misterioso" }}
        };

        private readonly int[] _specialEnemyScores = { 50, 100, 150, 200, 300 };

        private readonly DispatcherTimer _gameTimer = new();
        private readonly List<Image> _enemies = new();
        private readonly List<Rectangle> _barrierBlocks = new();
        private readonly Random _random = new();
        private readonly List<Rectangle> _enemyBullets = new();
        
        private Rectangle? _playerBullet;
        private Image? _specialEnemy;
        
        private int _playerLives;
        private int _score;
        private int _nextExtraLifeScore;
        
        private double _enemyDirection = 1;
        private double _enemyMoveSpeed = 25;
        private int _enemyMoveCounter = 0;
        private int _enemyMoveInterval = 25;
        private int _initialEnemyCount;
        
        private bool _isPlayerStunned = false;
        private int _stunCounter = 0;
        
        private double _specialEnemySpeed;
        private int _specialEnemySpawnCounter = 0;

        private const int _stunDurationInTicks = 75;
        private const int _specialEnemySpawnInterval = 6000;
        private const double _playerSpeed = 15;
        private const double _playerBulletSpeed = -15;
        private const double _enemyBulletSpeed = 5;
        private const int _initialPlayerLives = 3;
        private const int _maxPlayerLives = 6;
        private const int _winScore = 10000;
        private const int _rowsOfEnemies = 5;
        private const int _enemiesPerRow = 11;
        private const double _enemyWidth = 60;
        private const double _enemyHeight = 45;
        private const double _enemyHorizontalSpacing = 50;
        private const double _enemyVerticalSpacing = 45;
        
        public MainPage()
        {
            this.InitializeComponent();
            _gameTimer.Interval = TimeSpan.FromMilliseconds(20);
            _gameTimer.Tick += GameLoop;
            this.KeyDown += OnPageKeyDown;
            UpdateStartScreenScores();
        }

        private void UpdateStartScreenScores()
        {
            EnemyScores.Text = "PONTUAÇÃO\n" +
                               $"??? PTS - {_enemyTypes["alien4"].DisplayName}\n" +
                               $"{_enemyTypes["alien3"].Points} PTS - {_enemyTypes["alien3"].DisplayName}\n" +
                               $"{_enemyTypes["alien2"].Points} PTS - {_enemyTypes["alien2"].DisplayName}\n" +
                               $"{_enemyTypes["alien1"].Points} PTS - {_enemyTypes["alien1"].DisplayName}";
        }

        private void StartGame_Click(object sender, RoutedEventArgs e)
        {
            StartScreen.Visibility = Visibility.Collapsed;
            InGameUI.Visibility = Visibility.Visible;
            SetupNewGame();
            _gameTimer.Start();
            this.Focus(FocusState.Programmatic);
        }

        private void SetupNewGame()
        {
            _score = 0;
            ScoreText.Text = "0";
            _playerLives = _initialPlayerLives;
            _nextExtraLifeScore = 1000;
            UpdateLivesDisplay();
            
            _isPlayerStunned = false;
            Player.Opacity = 1.0;
            _enemyDirection = 1;
            _enemyMoveCounter = 0;
            
            _enemies.ForEach(enemy => GameCanvas.Children.Remove(enemy));
            _barrierBlocks.ForEach(block => GameCanvas.Children.Remove(block));
            _enemyBullets.ForEach(bullet => GameCanvas.Children.Remove(bullet));
            if (_playerBullet != null) GameCanvas.Children.Remove(_playerBullet);
            if (_specialEnemy != null) GameCanvas.Children.Remove(_specialEnemy);
            
            _enemies.Clear();
            _barrierBlocks.Clear();
            _enemyBullets.Clear();
            _playerBullet = null;
            _specialEnemy = null;
            
            CreatePixelatedBarrier(80, 450);
            CreatePixelatedBarrier(260, 450);
            CreatePixelatedBarrier(440, 450);
            CreatePixelatedBarrier(620, 450);

            SetupNewWave();
        }
        
        private void CreatePixelatedBarrier(double startX, double startY)
        {
            int blockSize = 5;
            int barrierWidthInBlocks = 16;
            int barrierHeightInBlocks = 12;

            for (int row = 0; row < barrierHeightInBlocks; row++)
            {
                for (int col = 0; col < barrierWidthInBlocks; col++)
                {
                    if (row > 6 && col > 3 && col < 12)
                    {
                        if (row > 8 || (col > 5 && col < 10))
                        {
                            continue;
                        }
                    }
                    Rectangle block = new Rectangle {
                        Width = blockSize, Height = blockSize,
                        Fill = new SolidColorBrush(Colors.LawnGreen)
                    };
                    Canvas.SetLeft(block, startX + col * blockSize);
                    Canvas.SetTop(block, startY + row * blockSize);
                    GameCanvas.Children.Add(block);
                    _barrierBlocks.Add(block);
                }
            }
        }

        private void SetupNewWave()
        {
            _enemies.ForEach(enemy => GameCanvas.Children.Remove(enemy));
            _enemies.Clear();
            string[] formation = { "alien3", "alien2", "alien2", "alien1", "alien1" };
            for (int row = 0; row < _rowsOfEnemies; row++)
            {
                string enemyTypeKey = formation[row];
                for (int col = 0; col < _enemiesPerRow; col++)
                {
                    Image enemyImage = new Image {
                        Width = _enemyWidth, Height = _enemyHeight,
                        Source = new BitmapImage(new Uri(_enemyTypes[enemyTypeKey].ImageSource)),
                        Tag = enemyTypeKey
                    };
                    Canvas.SetLeft(enemyImage, 50 + col * _enemyHorizontalSpacing);
                    Canvas.SetTop(enemyImage, 80 + row * _enemyVerticalSpacing);
                    GameCanvas.Children.Add(enemyImage);
                    _enemies.Add(enemyImage);
                }
            }
            _initialEnemyCount = _enemies.Count;
            _enemyMoveInterval = 25;
        }

        private void OnPageKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (!_gameTimer.IsEnabled || _isPlayerStunned) return;
            double playerLeft = Canvas.GetLeft(Player);
            double gameAreaWidth = InGameUI.Width;
            switch (e.Key)
            {
                case VirtualKey.Left or VirtualKey.A:
                    Canvas.SetLeft(Player, Math.Max(0, playerLeft - _playerSpeed));
                    break;
                case VirtualKey.Right or VirtualKey.D:
                    Canvas.SetLeft(Player, Math.Min(gameAreaWidth - Player.Width, playerLeft + _playerSpeed));
                    break;
                case VirtualKey.Space:
                    FireBullet();
                    break;
            }
        }

        private void FireBullet()
        {
            if (_playerBullet != null || _isPlayerStunned) return;
            _playerBullet = new Rectangle { Width = 5, Height = 15, Fill = new SolidColorBrush(Colors.LawnGreen) };
            double playerLeft = Canvas.GetLeft(Player);
            double playerTop = Canvas.GetTop(Player);
            Canvas.SetLeft(_playerBullet, playerLeft + Player.Width / 2 - _playerBullet.Width / 2);
            Canvas.SetTop(_playerBullet, playerTop - _playerBullet.Height);
            GameCanvas.Children.Add(_playerBullet);
        }

        private void EnemyFire(Image enemy)
        {
            Rectangle enemyBullet = new Rectangle { Width = 5, Height = 15, Fill = new SolidColorBrush(Colors.White) };
            double enemyLeft = Canvas.GetLeft(enemy);
            double enemyTop = Canvas.GetTop(enemy);
            Canvas.SetLeft(enemyBullet, enemyLeft + enemy.Width / 2 - enemyBullet.Width / 2);
            Canvas.SetTop(enemyBullet, enemyTop + enemy.Height);
            GameCanvas.Children.Add(enemyBullet);
            _enemyBullets.Add(enemyBullet);
        }

        private void GameLoop(object? sender, object e)
        {
            if (_isPlayerStunned)
            {
                _stunCounter--;
                Player.Opacity = (_stunCounter % 10 < 5) ? 1.0 : 0.2;
                if (_stunCounter <= 0)
                {
                    _isPlayerStunned = false;
                    Player.Opacity = 1.0;
                }
            }
            MovePlayerBullet();
            MoveEnemyBullets();
            MoveEnemySwarm();
            TrySpawnSpecialEnemy();
            MoveSpecialEnemy();
            
            int fireChance = 100 - (_initialEnemyCount - _enemies.Count) / 2;
            if (_random.Next(Math.Max(20, fireChance)) < 2)
            {
                var potentialShooters = _enemies.Where(en => en.Tag?.ToString() == "alien3").ToList();
                if (potentialShooters.Any())
                {
                    var shooter = potentialShooters[_random.Next(potentialShooters.Count)];
                    EnemyFire(shooter);
                }
            }
            
            if (_enemies.Count == 0 && _score < _winScore) SetupNewWave();
        }

        private void TrySpawnSpecialEnemy()
        {
            if (_specialEnemy != null) return;
            _specialEnemySpawnCounter++;
            if (_specialEnemySpawnCounter > _specialEnemySpawnInterval)
            {
                _specialEnemySpawnCounter = 0;
                if (_random.Next(100) < 50)
                {
                    SpawnSpecialEnemy();
                }
            }
        }
        
        private void SpawnSpecialEnemy()
        {
            string specialKey = "alien4";
            _specialEnemy = new Image {
                Width = _enemyWidth + 10, Height = _enemyHeight,
                Source = new BitmapImage(new Uri(_enemyTypes[specialKey].ImageSource)),
                Tag = specialKey
            };
            if (_random.Next(2) == 0)
            {
                _specialEnemySpeed = 3;
                Canvas.SetLeft(_specialEnemy, -_enemyWidth);
            }
            else
            {
                _specialEnemySpeed = -3;
                Canvas.SetLeft(_specialEnemy, InGameUI.Width);
            }
            Canvas.SetTop(_specialEnemy, 60);
            GameCanvas.Children.Add(_specialEnemy);
        }

        private void MoveSpecialEnemy()
        {
            if (_specialEnemy is null) return;
            double xPos = Canvas.GetLeft(_specialEnemy);
            Canvas.SetLeft(_specialEnemy, xPos + _specialEnemySpeed);
            if ((_specialEnemySpeed > 0 && xPos > InGameUI.Width) ||
                (_specialEnemySpeed < 0 && xPos < -_specialEnemy.Width))
            {
                GameCanvas.Children.Remove(_specialEnemy);
                _specialEnemy = null;
            }
        }

        private void MoveEnemySwarm()
        {
            _enemyMoveCounter++;
            _enemyMoveInterval = 2 + (int)(_enemies.Count / (double)_initialEnemyCount * 23);
            if (_enemyMoveCounter < _enemyMoveInterval) return;
            _enemyMoveCounter = 0;

            bool moveDownAndReverse = false;
            double gameAreaWidth = InGameUI.Width;
            foreach (var enemy in _enemies)
            {
                double xPos = Canvas.GetLeft(enemy);
                if ((_enemyDirection > 0 && xPos + _enemyWidth > gameAreaWidth) || (_enemyDirection < 0 && xPos < 0))
                {
                    moveDownAndReverse = true;
                    break;
                }
            }
            if (moveDownAndReverse)
            {
                _enemyDirection *= -1;
                foreach (var enemy in _enemies)
                {
                    Canvas.SetTop(enemy, Canvas.GetTop(enemy) + _enemyVerticalSpacing / 2);
                    if (Canvas.GetTop(enemy) + _enemyHeight >= 450)
                    {
                        EndGame(false);
                        return;
                    }
                    foreach (var block in _barrierBlocks.ToList())
                    {
                        if (CheckCollision(enemy, block))
                        {
                            GameCanvas.Children.Remove(block);
                            _barrierBlocks.Remove(block);
                        }
                    }
                }
            }
            else
            {
                foreach (var enemy in _enemies)
                {
                    Canvas.SetLeft(enemy, Canvas.GetLeft(enemy) + _enemyMoveSpeed * _enemyDirection);
                }
            }
        }

        private void MovePlayerBullet()
        {
            if (_playerBullet is null) return;
            double bulletTop = Canvas.GetTop(_playerBullet);
            Canvas.SetTop(_playerBullet, bulletTop + _playerBulletSpeed);

            if (bulletTop < 0)
            {
                GameCanvas.Children.Remove(_playerBullet);
                _playerBullet = null;
                return;
            }

            if (_specialEnemy != null && CheckCollision(_playerBullet, _specialEnemy))
            {
                int points = _specialEnemyScores[_random.Next(_specialEnemyScores.Length)];
                _score += points;
                ScoreText.Text = _score.ToString();
                CheckForExtraLife();
                GameCanvas.Children.Remove(_specialEnemy);
                _specialEnemy = null;
                GameCanvas.Children.Remove(_playerBullet);
                _playerBullet = null;
                return;
            }

            foreach (var enemy in _enemies.ToList())
            {
                if (CheckCollision(_playerBullet, enemy))
                {
                    string enemyTag = enemy.Tag?.ToString() ?? "alien1";
                    int points = _enemyTypes[enemyTag].Points;
                    GameCanvas.Children.Remove(enemy);
                    _enemies.Remove(enemy);
                    GameCanvas.Children.Remove(_playerBullet);
                    _playerBullet = null;
                    _score += points;
                    ScoreText.Text = _score.ToString();
                    CheckForExtraLife();
                    if (_score >= _winScore) EndGame(true);
                    return;
                }
            }

            foreach (var block in _barrierBlocks.ToList())
            {
                if (CheckCollision(_playerBullet, block))
                {
                    GameCanvas.Children.Remove(block);
                    _barrierBlocks.Remove(block);
                    GameCanvas.Children.Remove(_playerBullet);
                    _playerBullet = null;
                    return;
                }
            }
        }
        
        private void CheckForExtraLife()
        {
            if (_score >= _nextExtraLifeScore)
            {
                if (_playerLives < _maxPlayerLives)
                {
                    _playerLives++;
                    UpdateLivesDisplay();
                }
                _nextExtraLifeScore += 1000;
            }
        }

        private void MoveEnemyBullets()
        {
            foreach (var bullet in _enemyBullets.ToList())
            {
                double bulletTop = Canvas.GetTop(bullet);
                Canvas.SetTop(bullet, bulletTop + _enemyBulletSpeed);

                if (bulletTop > InGameUI.Height)
                {
                    GameCanvas.Children.Remove(bullet);
                    _enemyBullets.Remove(bullet);
                    continue;
                }

                if (CheckCollision(bullet, Player))
                {
                    GameCanvas.Children.Remove(bullet);
                    _enemyBullets.Remove(bullet);
                    PlayerHit();
                    continue;
                }
                
                foreach (var block in _barrierBlocks.ToList())
                {
                    if (CheckCollision(bullet, block))
                    {
                        GameCanvas.Children.Remove(block);
                        _barrierBlocks.Remove(block);
                        GameCanvas.Children.Remove(bullet);
                        _enemyBullets.Remove(bullet);
                        goto nextBullet;
                    }
                }
                nextBullet:;
            }
        }

        private void PlayerHit()
        {
            if (_isPlayerStunned) return;
            _playerLives--;
            UpdateLivesDisplay();
            if (_playerLives <= 0)
            {
                EndGame(false);
            }
            else
            {
                _isPlayerStunned = true;
                _stunCounter = _stunDurationInTicks;
            }
        }
        
        private void UpdateLivesDisplay()
        {
            Life1.Visibility = _playerLives >= 1 ? Visibility.Visible : Visibility.Collapsed;
            Life2.Visibility = _playerLives >= 2 ? Visibility.Visible : Visibility.Collapsed;
            Life3.Visibility = _playerLives >= 3 ? Visibility.Visible : Visibility.Collapsed;
            Life4.Visibility = _playerLives >= 4 ? Visibility.Visible : Visibility.Collapsed;
            Life5.Visibility = _playerLives >= 5 ? Visibility.Visible : Visibility.Collapsed;
            Life6.Visibility = _playerLives >= 6 ? Visibility.Visible : Visibility.Collapsed;
        }

        private bool CheckCollision(FrameworkElement elementA, FrameworkElement elementB)
        {
            if (elementA is null || elementB is null) return false;
            double ax = Canvas.GetLeft(elementA);
            double ay = Canvas.GetTop(elementA);
            double bx = Canvas.GetLeft(elementB);
            double by = Canvas.GetTop(elementB);
            return ax < bx + elementB.Width && ax + elementA.Width > bx && ay < by + elementB.Height && ay + elementA.Height > by;
        }

        private async void EndGame(bool playerWon)
        {
            _gameTimer.Stop();
            var dialog = new ContentDialog
            {
                Title = playerWon ? "Você venceu!" : "Fim de Jogo",
                Content = $"Sua pontuação final foi: {_score}",
                CloseButtonText = "Jogar Novamente",
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
            InGameUI.Visibility = Visibility.Collapsed;
            StartScreen.Visibility = Visibility.Visible;
        }
    }
}
