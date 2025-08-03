using Microsoft.UI;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Shapes;
using System.Text;
using Windows.System;
using SpaceInvaders.Services;
using SpaceInvaders.Models;

namespace SpaceInvaders
{
    public sealed partial class MainPage : Page
    {
        // Gerenciadores
        private readonly SoundManager _soundManager = new();
        private readonly HighScoreManager _highScoreManager = new();
        private EnemyManager? _enemyManager;

        // Estado do Jogo
        private readonly DispatcherTimer _gameTimer = new();
        private Player? _player;
        private readonly List<Rectangle> _playerBullets = new();
        private readonly List<Models.Barrier> _barriers = new(); 

        private int _score;

        private const int WinScore = 500;
        private const double PlayerSpeed = 10;
        private const double PlayerBulletSpeed = -15;
        private const int BarrierHealth = 4;

        public MainPage()
        {
            this.InitializeComponent();
            _gameTimer.Interval = TimeSpan.FromMilliseconds(20);
            _gameTimer.Tick += GameLoop;
            this.KeyDown += OnPageKeyDown;
            this.Loaded += (s, e) => InitializeGame();
        }

        private void InitializeGame()
        {
            var enemyTypes = new Dictionary<string, EnemyType> {
                {"alien1", new EnemyType { ImageSource = "ms-appx:///Assets/Images/alien1.png", Points = 10 }},
                {"alien2", new EnemyType { ImageSource = "ms-appx:///Assets/Images/alien2.png", Points = 20 }},
                {"alien3", new EnemyType { ImageSource = "ms-appx:///Assets/Images/alien3.png", Points = 40 }},
            };
            
            _enemyManager = new EnemyManager(GameCanvas, enemyTypes);
            LoadSounds();
            
            _soundManager.PlayMusic("start_music");
        }

        private void LoadSounds()
        {
            _soundManager.PreloadSound("shoot", @"Sounds\shoot.wav");
            _soundManager.PreloadSound("invader_killed", @"Sounds\invaderkilled.wav");
            _soundManager.PreloadSound("start_music", @"Sounds\spaceinvaders1.wav");
        }

        private void StartGame_Click(object sender, RoutedEventArgs e)
        {
            _soundManager.StopMusic(); 

            StartScreen.Visibility = Visibility.Collapsed;
            InGameUI.Visibility = Visibility.Visible;
            SetupNewGame();
            _gameTimer.Start();
            this.Focus(FocusState.Programmatic);
        }
        
        private async void ShowHighScores_Click(object sender, RoutedEventArgs e)
        {
            var scores = await _highScoreManager.GetScoresAsync();
            var sb = new StringBuilder();
            sb.AppendLine("MELHORES PONTUAÇÕES:\n");

            if (scores.Any())
            {
                int rank = 1;
                foreach (var score in scores)
                {
                    sb.AppendLine($"{rank}. {score} PONTOS");
                    rank++;
                }
            }
            else
            {
                sb.AppendLine("Nenhum placar salvo ainda.");
            }

            var dialog = new ContentDialog
            {
                Title = "Placares",
                Content = sb.ToString(),
                CloseButtonText = "Fechar",
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
        }

        private async void ShowControls_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ContentDialog
            {
                Title = "Controles",
                Content = "Use as SETAS ESQUERDA/DIREITA ou as teclas A/D para mover.\n\nPressione a BARRA DE ESPAÇO para atirar.",
                CloseButtonText = "Entendi",
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
        }

        private void SetupNewGame()
        {
            GameCanvas.Children.Clear();
            _playerBullets.Clear();
            _barriers.Clear();
            
            _player = new Player(PlayerImage);
            _player.X = (InGameUI.Width / 2) - (_player.Width / 2);
            _player.Y = 550;
            GameCanvas.Children.Add(_player.Visual);
            
            _score = 0;
            ScoreText.Text = "0";

            for (int i = 0; i < 4; i++)
            {
                var barrierVisual = new Rectangle { Width = 80, Height = 30, Fill = new SolidColorBrush(Colors.LawnGreen) };
                Canvas.SetLeft(barrierVisual, 80 + i * 180);
                Canvas.SetTop(barrierVisual, 450);
                _barriers.Add(new Models.Barrier(barrierVisual, BarrierHealth));
                GameCanvas.Children.Add(barrierVisual);
            }

            _enemyManager?.SpawnWave();
        }

        private void GameLoop(object? sender, object e)
        {
            if (_player is null || _enemyManager is null) return;
            
            _player.Update();
            MovePlayerBullet();
            CheckCollisions();

            if (_enemyManager.IsSwarmDestroyed && _score < WinScore)
            {
                _enemyManager.SpawnWave();
            }
        }
        
        private void OnPageKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (!_gameTimer.IsEnabled || _player is null) return;
            
            switch (e.Key)
            {
                case VirtualKey.Left or VirtualKey.A: _player.Move(-PlayerSpeed, InGameUI.Width); break;
                case VirtualKey.Right or VirtualKey.D: _player.Move(PlayerSpeed, InGameUI.Width); break;
                case VirtualKey.Space: FirePlayerBullet(); break;
            }
        }

        private void CheckCollisions()
        {
            if (_player is null || _enemyManager is null) return;
            foreach (var bullet in _playerBullets.ToList())
            {
                foreach (var enemy in _enemyManager.Enemies.ToList())
                {
                    if (CheckCollision(bullet, enemy.Visual))
                    {
                        _score += enemy.Points;
                        ScoreText.Text = _score.ToString();
                        _soundManager.PlaySound("invader_killed");
                        _enemyManager.RemoveEnemy(enemy);
                        GameCanvas.Children.Remove(bullet);
                        _playerBullets.Remove(bullet);
                        if (_score >= WinScore) EndGame(true);
                        return;
                    }
                }
                foreach (var barrier in _barriers.ToList())
                {
                    if (CheckCollision(bullet, barrier.Visual))
                    {
                        barrier.TakeDamage();
                        barrier.Visual.Opacity = (double)barrier.Health / barrier.MaxHealth;
                        GameCanvas.Children.Remove(bullet);
                        _playerBullets.Remove(bullet);
                        if (barrier.Health <= 0)
                        {
                            GameCanvas.Children.Remove(barrier.Visual);
                            _barriers.Remove(barrier);
                        }
                        return;
                    }
                }
            }
        }
        
        private void MovePlayerBullet()
        {
            foreach (var bullet in _playerBullets.ToList())
            {
                Canvas.SetTop(bullet, Canvas.GetTop(bullet) + PlayerBulletSpeed);
                if (Canvas.GetTop(bullet) < 0)
                {
                    GameCanvas.Children.Remove(bullet);
                    _playerBullets.Remove(bullet);
                }
            }
        }

        private void FirePlayerBullet()
        {
            if (_player is null || _playerBullets.Any()) return; 
            var bullet = new Rectangle { Width = 5, Height = 15, Fill = new SolidColorBrush(Colors.White) };
            Canvas.SetLeft(bullet, _player.X + _player.Width / 2 - 2.5);
            Canvas.SetTop(bullet, _player.Y - 15);
            _playerBullets.Add(bullet);
            GameCanvas.Children.Add(bullet);
            _soundManager.PlaySound("shoot");
        }
        
        private bool CheckCollision(FrameworkElement a, FrameworkElement b)
        {
            if (a is null || b is null) return false;
            double ax = Canvas.GetLeft(a), ay = Canvas.GetTop(a), bx = Canvas.GetLeft(b), by = Canvas.GetTop(b);
            return ax < bx + b.Width && ax + a.Width > bx && ay < by + b.Height && ay + a.Height > by;
        }
        
        private async void EndGame(bool playerWon)
        {
            _gameTimer.Stop();
            
            await _highScoreManager.AddScoreAsync(_score);

            var dialog = new ContentDialog { Title = playerWon ? "Você Venceu!" : "Fim de Jogo", Content = $"Sua pontuação final foi: {_score}", CloseButtonText = "OK", XamlRoot = this.XamlRoot };
            await dialog.ShowAsync();
            
            InGameUI.Visibility = Visibility.Collapsed;
            StartScreen.Visibility = Visibility.Visible;
            _soundManager.PlayMusic("start_music");
        }
    }
}
