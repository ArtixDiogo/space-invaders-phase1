using Microsoft.UI;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
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
        private readonly DispatcherTimer _ufoTimer = new();
        private Player? _player;
        private readonly List<Rectangle> _playerBullets = new();
        private readonly List<Rectangle> _enemyBullets = new();
        private readonly List<Models.Barrier> _barriers = new();
        private Enemy? _ufo;
        private readonly Random _random = new();

        // Novas variáveis para o Stun do Jogador
        private bool _isPlayerStunned = false;
        private readonly DispatcherTimer _playerStunTimer = new();
        private readonly DispatcherTimer _playerBlinkTimer = new();

        private int _score;
        private int _lives;
        private int _lastScoreLifeAward;
        
        private const int InitialLives = 3;
        private const int MaxLives = 6;
        private const double PlayerSpeed = 10;
        private const double PlayerBulletSpeed = -15;
        private const double EnemyBulletSpeed = 5;
        private const int BarrierHealth = 4;

        public MainPage()
        {
            this.InitializeComponent();
            _gameTimer.Interval = TimeSpan.FromMilliseconds(20);
            _gameTimer.Tick += GameLoop;
            _ufoTimer.Tick += SpawnUfo; 
            this.KeyDown += OnPageKeyDown;
            this.Loaded += (s, e) => InitializeGame();

            // Configuração dos timers de Stun
            _playerStunTimer.Interval = TimeSpan.FromSeconds(1.5);
            _playerStunTimer.Tick += PlayerStunEnd;
            _playerBlinkTimer.Interval = TimeSpan.FromMilliseconds(100); // Velocidade do pisca-pisca
            _playerBlinkTimer.Tick += PlayerBlink;
        }

        private void InitializeGame()
        {
            var enemyTypes = new Dictionary<string, EnemyType> {
                {"alien1", new EnemyType { ImageSource = "ms-appx:///Assets/Images/alien1.png", Points = 10 }},
                {"alien2", new EnemyType { ImageSource = "ms-appx:///Assets/Images/alien2.png", Points = 20 }},
                {"alien3", new EnemyType { ImageSource = "ms-appx:///Assets/Images/alien3.png", Points = 40 }},
                {"ufo", new EnemyType { ImageSource = "ms-appx:///Assets/Images/alien4.png", Points = 100 }}
            };

            LoadSounds();
            _enemyManager = new EnemyManager(GameCanvas, enemyTypes, _soundManager);
            _soundManager.PlayMusic("start_music");
        }

        private void LoadSounds()
        {
            _soundManager.PreloadSound("shoot", @"Sounds\shoot.wav");
            _soundManager.PreloadSound("invader_killed", @"Sounds\invaderkilled.wav");
            _soundManager.PreloadSound("player_death", @"Sounds\explosion.wav");
            _soundManager.PreloadSound("start_music", @"Sounds\spaceinvaders1.wav");
            _soundManager.PreloadSound("ufo_sound", @"Sounds\ufo_lowpitch.wav");
            
        }

        private void StartGame_Click(object sender, RoutedEventArgs e)
        {
            _soundManager.StopMusic();

            StartScreen.Visibility = Visibility.Collapsed;
            InGameUI.Visibility = Visibility.Visible;
            SetupNewGame();
            _gameTimer.Start();
            ResetUfoTimer(); 
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
                    sb.AppendLine($"{rank}. {score.Value} PONTOS ({score.Key})");
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
            _enemyBullets.Clear();
            _barriers.Clear();

            _player = new Player(PlayerImage);
            _player.X = (InGameUI.Width / 2) - (_player.Width / 2);
            _player.Y = 550;
            GameCanvas.Children.Add(_player.Visual);

            _score = 0;
            _lastScoreLifeAward = 0;
            ScoreText.Text = "0";
            _lives = InitialLives;
            UpdateLivesDisplay();

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

            // Só atualiza a posição do jogador se ele não estiver atordoado
            if (!_isPlayerStunned)
            {
                _player.Update();
            }

            _enemyManager.Update(InGameUI.Width);
            FireEnemyBullet();
            MovePlayerBullet();
            MoveEnemyBullet();
            MoveUfo();
            CheckCollisions();

            if (_enemyManager.IsSwarmDestroyed)
            {
                _enemyManager.SpawnWave();
            }

            if (_enemyManager.Enemies.Any(enemy => enemy.Y + enemy.Height >= _player.Y))
            {
                EndGame(false);
            }
        }

        private void OnPageKeyDown(object sender, KeyRoutedEventArgs e)
        {
            // Impede o movimento se o jogador estiver atordoado
            if (!_gameTimer.IsEnabled || _player is null || _isPlayerStunned) return;

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
                        HandleScore(enemy.Points);
                        _soundManager.PlaySound("invader_killed");
                        _enemyManager.RemoveEnemy(enemy); 
                        GameCanvas.Children.Remove(bullet);
                        _playerBullets.Remove(bullet);
                        return;
                    }
                }

                if (_ufo != null && CheckCollision(bullet, _ufo.Visual))
                {
                    HandleScore(_ufo.Points);
                    _soundManager.PlaySound("invader_killed");
                    GameCanvas.Children.Remove(_ufo.Visual);
                    _ufo = null;
                    _soundManager.StopMusic();
                    _soundManager.PlayMusic("start_music");
                    GameCanvas.Children.Remove(bullet);
                    _playerBullets.Remove(bullet);
                    ResetUfoTimer();
                    return;
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

            foreach (var bullet in _enemyBullets.ToList())
            {
                // Verifica a colisão com o jogador APENAS se ele não estiver atordoado/invulnerável
                if (!_isPlayerStunned && CheckCollision(bullet, _player.Visual))
                {
                    GameCanvas.Children.Remove(bullet);
                    _enemyBullets.Remove(bullet);
                    PlayerHit();
                    return;
                }

                foreach (var barrier in _barriers.ToList())
                {
                    if (CheckCollision(bullet, barrier.Visual))
                    {
                        barrier.TakeDamage();
                        barrier.Visual.Opacity = (double)barrier.Health / barrier.MaxHealth;
                        GameCanvas.Children.Remove(bullet);
                        _enemyBullets.Remove(bullet);
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

        private void PlayerHit()
        {
            // Se já estiver atordoado, não faz nada
            if (_isPlayerStunned) return; 

            _lives--;
            UpdateLivesDisplay();
            _soundManager.PlaySound("player_death");

            if (_lives <= 0)
            {
                EndGame(false);
            }
            else
            {
                // Inicia o período de Stun e invulnerabilidade
                _isPlayerStunned = true;
                _playerStunTimer.Start();
                _playerBlinkTimer.Start();
            }
        }
        
        // Novo método para o efeito de piscar
        private void PlayerBlink(object? sender, object e)
        {
            if (_player != null)
            {
                _player.Visual.Opacity = _player.Visual.Opacity == 1 ? 0.4 : 1;
            }
        }

        // Novo método para finalizar o Stun
        private void PlayerStunEnd(object? sender, object e)
        {
            _playerStunTimer.Stop();
            _playerBlinkTimer.Stop();
            _isPlayerStunned = false;
            if (_player != null)
            {
                // Garante que o jogador termine o stun totalmente visível
                _player.Visual.Opacity = 1;
            }
        }

        private void HandleScore(int points)
        {
            _score += points;
            ScoreText.Text = _score.ToString();

            if (_score / 1000 > _lastScoreLifeAward)
            {
                _lastScoreLifeAward = _score / 1000;
                if (_lives < MaxLives)
                {
                    _lives++;
                    UpdateLivesDisplay();
                }
            }
        }

        private void UpdateLivesDisplay()
        {
            LivesPanel.Children.Clear();
            for (int i = 0; i < _lives; i++)
            {
                var lifeIcon = new Image
                {
                    Source = new BitmapImage(new Uri("ms-appx:///Assets/Images/lasercannon.png")),
                    Width = 30,
                    Height = 24
                };
                LivesPanel.Children.Add(lifeIcon);
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

        private void MoveEnemyBullet()
        {
            foreach (var bullet in _enemyBullets.ToList())
            {
                Canvas.SetTop(bullet, Canvas.GetTop(bullet) + EnemyBulletSpeed);
                if (Canvas.GetTop(bullet) > InGameUI.Height)
                {
                    GameCanvas.Children.Remove(bullet);
                    _enemyBullets.Remove(bullet);
                }
            }
        }

        private void FirePlayerBullet()
        {
            // Impede o tiro se o jogador estiver atordoado
            if (_player is null || _playerBullets.Any() || _isPlayerStunned) return; 
            var bullet = new Rectangle { Width = 5, Height = 15, Fill = new SolidColorBrush(Colors.White) };
            Canvas.SetLeft(bullet, _player.X + _player.Width / 2 - 2.5);
            Canvas.SetTop(bullet, _player.Y - 15);
            _playerBullets.Add(bullet);
            GameCanvas.Children.Add(bullet);
            _soundManager.PlaySound("shoot");
        }

        private void FireEnemyBullet()
        {
            if (_enemyManager is null) return;
            
            int fireChance = 1 + (int)(_enemyManager.CurrentSpeed * 2);
            if (_random.Next(0, 200) < fireChance) 
            {
                var shooter = _enemyManager.GetShooter();
                if (shooter != null)
                {
                    var bullet = new Rectangle { Width = 4, Height = 10, Fill = new SolidColorBrush(Colors.White) };
                    Canvas.SetLeft(bullet, shooter.X + shooter.Width / 2 - 2);
                    Canvas.SetTop(bullet, shooter.Y + shooter.Height);
                    _enemyBullets.Add(bullet);
                    GameCanvas.Children.Add(bullet);
                }
            }
        }

        private void ResetUfoTimer()
        {
            _ufoTimer.Stop();
            _ufoTimer.Interval = TimeSpan.FromSeconds(_random.Next(15, 46));
            _ufoTimer.Start();
        }

        private void SpawnUfo(object? sender, object e)
        {
            if (_ufo != null) return;

            var ufoImage = new Image
            {
                Width = 40,
                Height = 30,
                Source = new BitmapImage(new Uri("ms-appx:///Assets/Images/alien4.png"))
            };
            _ufo = new Enemy(ufoImage, 100) { X = -50, Y = 50 };
            _ufo.Update();
            GameCanvas.Children.Add(ufoImage);
            _soundManager.PlayMusic("ufo_sound", loop: true);
            
            _ufoTimer.Stop();
        }

        private void MoveUfo()
        {
            if (_ufo == null) return;
            _ufo.X += 5;
            _ufo.Update();
            if (_ufo.X > InGameUI.Width)
            {
                GameCanvas.Children.Remove(_ufo.Visual);
                _ufo = null;
                _soundManager.StopMusic();
                _soundManager.PlayMusic("start_music");
                ResetUfoTimer(); 
            }
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
            _ufoTimer.Stop();

            var nicknameBox = new TextBox { PlaceholderText = "Seu apelido", MaxLength = 10 };
            var panel = new StackPanel { Spacing = 10 };
            panel.Children.Add(new TextBlock { Text = $"Sua pontuação final foi: {_score}" });
            panel.Children.Add(nicknameBox);

            var dialog = new ContentDialog
            {
                Title = "Fim de Jogo",
                Content = panel,
                PrimaryButtonText = "Salvar e Jogar Novamente",
                SecondaryButtonText = "Voltar ao Menu",
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                string nickname = string.IsNullOrWhiteSpace(nicknameBox.Text) ? "JOGADOR" : nicknameBox.Text.ToUpper();
                await _highScoreManager.AddScoreAsync(nickname, _score);
                StartGame_Click(this, new RoutedEventArgs());
            }
            else
            {
                InGameUI.Visibility = Visibility.Collapsed;
                StartScreen.Visibility = Visibility.Visible;
                _soundManager.PlayMusic("start_music");
            }
        }
    }
}
