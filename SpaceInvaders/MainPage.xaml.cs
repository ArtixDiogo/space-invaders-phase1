using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.System;
using Windows.UI;
using SpaceInvaders.Services;
using SpaceInvaders.Models;

namespace SpaceInvaders
{
    /// <summary>
    /// Classe principal da página do jogo. Atua como o "Maestro",
    /// coordenando as classes de lógica (Managers) e o estado do jogo.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        // Gerenciadores de Lógica
        private EnemyManager? _enemyManager;

        // Estado do Jogo
        private readonly DispatcherTimer _gameTimer = new();
        private readonly Random _random = new();
        private Player? _player;
        private readonly List<Rectangle> _playerBullets = new();
        private readonly List<Rectangle> _enemyBullets = new();
        private readonly List<Rectangle> _barrierBlocks = new();

        private int _score;
        private int _nextExtraLifeScore;

        // Constantes
        private const int WinScore = 2000;
        private const int InitialPlayerLives = 3;
        private const int MaxPlayerLives = 6;
        private const double PlayerBulletSpeed = -15;
        private const double EnemyBulletSpeed = 5;
        
        public MainPage()
        {
            this.InitializeComponent();
            _gameTimer.Interval = TimeSpan.FromMilliseconds(20);
            _gameTimer.Tick += GameLoop;
            this.KeyDown += OnPageKeyDown;
            // Adia a inicialização para o evento 'Loaded' para garantir que a UI esteja pronta
            this.Loaded += (s, e) => InitializeGame();
        }

        /// <summary>
        /// Prepara os gerenciadores de lógica do jogo.
        /// </summary>
        private void InitializeGame()
        {
            var enemyTypes = new Dictionary<string, EnemyType> {
                {"alien1", new EnemyType { ImageSource = "ms-appx:///Assets/Images/alien1.png", Points = 10, DisplayName = "Inimigo Básico" }},
                {"alien2", new EnemyType { ImageSource = "ms-appx:///Assets/Images/alien2.png", Points = 20, DisplayName = "Inimigo Intermediário" }},
                {"alien3", new EnemyType { ImageSource = "ms-appx:///Assets/Images/alien3.png", Points = 40, DisplayName = "Inimigo Avançado" }},
                {"alien4", new EnemyType { ImageSource = "ms-appx:///Assets/Images/alien4.png", Points = 200, DisplayName = "Inimigo Misterioso" }}
            };
            
            _enemyManager = new EnemyManager(GameCanvas, enemyTypes);
        }
        
        /// <summary>
        /// Chamado quando o botão "Iniciar Jogo" é clicado.
        /// </summary>
        private void StartGame_Click(object sender, RoutedEventArgs e)
        {
            StartScreen.Visibility = Visibility.Collapsed;
            InGameUI.Visibility = Visibility.Visible;
            SetupNewGame();
            _gameTimer.Start();
            this.Focus(FocusState.Programmatic);
        }
        
        /// <summary>
        /// Configura ou reseta o estado do jogo para um novo início.
        /// </summary>
        private void SetupNewGame()
        {
            // Limpa todos os elementos visuais do jogo anterior
            GameCanvas.Children.Clear();
            _playerBullets.Clear();
            _enemyBullets.Clear();
            _barrierBlocks.Clear();
            
            // Cria um novo objeto jogador
            _player = new Player(PlayerImage, InitialPlayerLives);
            _player.X = (InGameUI.Width / 2) - (_player.Width / 2);
            _player.Y = 550;
            GameCanvas.Children.Add(_player.Visual);
            
            // Reseta o placar e as vidas
            _score = 0;
            ScoreText.Text = "0";
            _nextExtraLifeScore = 1000;
            UpdateLivesDisplay();

            // Cria os escudos
            CreatePixelatedBarrier(80, 450);
            CreatePixelatedBarrier(260, 450);
            CreatePixelatedBarrier(440, 450);
            CreatePixelatedBarrier(620, 450);

            // Cria a primeira onda de inimigos
            _enemyManager?.SpawnWave();
        }

        /// <summary>
        /// O loop principal do jogo, executado a cada tick do timer.
        /// </summary>
        private void GameLoop(object? sender, object e)
        {
            if (_player is null || _enemyManager is null) return;

            // Atualiza o estado de todos os objetos principais
            _player.Update();
            _enemyManager.Update(InGameUI.Width, _barrierBlocks);
            
            MoveBullets();
            HandleEnemyShooting();
            CheckCollisions();

            // Se todos os inimigos foram destruídos, cria uma nova onda
            if (_enemyManager.IsSwarmDestroyed && _score < WinScore)
            {
                _enemyManager.SpawnWave();
            }
        }
        
        /// <summary>
        /// Processa a entrada do teclado do jogador.
        /// </summary>
        private void OnPageKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (!_gameTimer.IsEnabled || _player is null) return;
            
            const double playerSpeed = 15;
            switch (e.Key)
            {
                case VirtualKey.Left or VirtualKey.A:
                    _player.Move(-playerSpeed, InGameUI.Width);
                    break;
                case VirtualKey.Right or VirtualKey.D:
                    _player.Move(playerSpeed, InGameUI.Width);
                    break;
                case VirtualKey.Space:
                    FirePlayerBullet();
                    break;
            }
        }
        
        /// <summary>
        /// Controla a chance de um inimigo atirar.
        /// </summary>
        private void HandleEnemyShooting()
        {
            if (_random.Next(100) < 2)
            {
                var shooter = _enemyManager?.GetRandomShooter();
                if (shooter != null)
                {
                    FireEnemyBullet(shooter);
                }
            }
        }

        /// <summary>
        /// Verifica todas as possíveis colisões no jogo.
        /// </summary>
        private void CheckCollisions()
        {
            if (_player is null || _enemyManager is null) return;

            // 1. Tiros do jogador vs Inimigos
            foreach (var bullet in _playerBullets.ToList())
            {
                foreach (var enemy in _enemyManager.Enemies.ToList())
                {
                    if (CheckCollision(bullet, enemy.Visual))
                    {
                        _score += enemy.Points;
                        ScoreText.Text = _score.ToString();
                        CheckForExtraLife();
                        
                        _enemyManager.RemoveEnemy(enemy);
                        
                        GameCanvas.Children.Remove(bullet);
                        _playerBullets.Remove(bullet);
                        goto nextPlayerBullet; // Pula para o próximo tiro do jogador
                    }
                }
                nextPlayerBullet:;
            }

            // 2. Tiros inimigos vs Jogador
            foreach (var bullet in _enemyBullets.ToList())
            {
                if (CheckCollision(bullet, _player.Visual))
                {
                    _player.TakeHit();
                    UpdateLivesDisplay();

                    GameCanvas.Children.Remove(bullet);
                    _enemyBullets.Remove(bullet);

                    if (!_player.IsAlive)
                    {
                        EndGame(false);
                        return;
                    }
                }
            }

            // 3. Todos os tiros vs Barreiras
            var allBullets = _playerBullets.Concat(_enemyBullets).ToList();
            foreach (var bullet in allBullets)
            {
                foreach (var block in _barrierBlocks.ToList())
                {
                    if (CheckCollision(bullet, block))
                    {
                        GameCanvas.Children.Remove(block);
                        _barrierBlocks.Remove(block);
                        GameCanvas.Children.Remove(bullet);
                        if (_playerBullets.Contains(bullet)) _playerBullets.Remove(bullet);
                        if (_enemyBullets.Contains(bullet)) _enemyBullets.Remove(bullet);
                        goto nextCombinedBullet;
                    }
                }
                nextCombinedBullet:;
            }
        }
        
        /// <summary>
        /// Move todos os tiros (do jogador e inimigos) na tela.
        /// </summary>
        private void MoveBullets()
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

        /// <summary>
        /// Cria um tiro para o jogador.
        /// </summary>
        private void FirePlayerBullet()
        {
            // CORREÇÃO: A condição agora é >= 1, permitindo apenas um tiro.
            if (_player is null || _player.IsStunned || _playerBullets.Count >= 1) return;
            
            var bullet = new Rectangle { Width = 5, Height = 15, Fill = new SolidColorBrush(Colors.LawnGreen) };
            Canvas.SetLeft(bullet, _player.X + _player.Width / 2 - 2.5);
            Canvas.SetTop(bullet, _player.Y - 15);
            _playerBullets.Add(bullet);
            GameCanvas.Children.Add(bullet);
        }
        
        /// <summary>
        /// Cria um tiro para um inimigo.
        /// </summary>
        private void FireEnemyBullet(Enemy enemy)
        {
            var bullet = new Rectangle { Width = 5, Height = 15, Fill = new SolidColorBrush(Colors.White) };
            Canvas.SetLeft(bullet, enemy.X + enemy.Width / 2 - 2.5);
            Canvas.SetTop(bullet, enemy.Y + enemy.Height);
            _enemyBullets.Add(bullet);
            GameCanvas.Children.Add(bullet);
        }
        
        /// <summary>
        /// Verifica se a pontuação atingiu o limiar para uma vida extra.
        /// </summary>
        private void CheckForExtraLife()
        {
            if (_player is null) return;
            if (_score >= _nextExtraLifeScore)
            {
                _player.AddLife(MaxPlayerLives);
                UpdateLivesDisplay();
                _nextExtraLifeScore += 1000;
            }
        }

        /// <summary>
        /// Atualiza a exibição visual das vidas do jogador.
        /// </summary>
        private void UpdateLivesDisplay()
        {
            if (_player is null) return;
            Life1.Visibility = _player.Lives >= 1 ? Visibility.Visible : Visibility.Collapsed;
            Life2.Visibility = _player.Lives >= 2 ? Visibility.Visible : Visibility.Collapsed;
            Life3.Visibility = _player.Lives >= 3 ? Visibility.Visible : Visibility.Collapsed;
            Life4.Visibility = _player.Lives >= 4 ? Visibility.Visible : Visibility.Collapsed;
            Life5.Visibility = _player.Lives >= 5 ? Visibility.Visible : Visibility.Collapsed;
            Life6.Visibility = _player.Lives >= 6 ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        /// Algoritmo genérico de detecção de colisão entre dois elementos.
        /// </summary>
        private bool CheckCollision(FrameworkElement elementA, FrameworkElement elementB)
        {
            if (elementA is null || elementB is null) return false;
            double ax = Canvas.GetLeft(elementA);
            double ay = Canvas.GetTop(elementA);
            double bx = Canvas.GetLeft(elementB);
            double by = Canvas.GetTop(elementB);
            return ax < bx + elementB.Width && ax + elementA.Width > bx && ay < by + elementB.Height && ay + elementA.Height > by;
        }
        
        /// <summary>
        /// Cria uma barreira de proteção a partir de pequenos blocos.
        /// </summary>
        private void CreatePixelatedBarrier(double startX, double startY)
        {
            int blockSize = 5;
            int barrierWidthInBlocks = 16;
            int barrierHeightInBlocks = 12;
            for (int row = 0; row < barrierHeightInBlocks; row++)
            {
                for (int col = 0; col < barrierWidthInBlocks; col++)
                {
                    if (row > 6 && col > 3 && col < 12) { if (row > 8 || (col > 5 && col < 10)) continue; }
                    Rectangle block = new Rectangle { Width = blockSize, Height = blockSize, Fill = new SolidColorBrush(Colors.LawnGreen) };
                    Canvas.SetLeft(block, startX + col * blockSize);
                    Canvas.SetTop(block, startY + row * blockSize);
                    GameCanvas.Children.Add(block);
                    _barrierBlocks.Add(block);
                }
            }
        }
        
        /// <summary>
        /// Encerra o jogo e exibe uma caixa de diálogo.
        /// </summary>
        private async void EndGame(bool playerWon)
        {
            _gameTimer.Stop();
            var dialog = new ContentDialog {
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
