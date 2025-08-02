using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Shapes;
using SpaceInvaders.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml;

namespace SpaceInvaders.Services
{
    /// <summary>
    /// Gerencia toda a lógica da horda de inimigos: criação, movimento, velocidade e seleção de atiradores.
    /// </summary>
    public class EnemyManager
    {
        public List<Enemy> Enemies { get; } = new();
        public bool IsSwarmDestroyed => !Enemies.Any();
        
        private double _direction = 1; // 1 para direita, -1 para esquerda
        private int _moveCounter = 0;
        private int _moveInterval = 25; // Controla o ritmo do movimento
        private int _initialEnemyCount;
        private readonly Canvas _gameCanvas;
        private readonly Dictionary<string, EnemyType> _enemyTypes;
        private readonly Random _random = new();

        private const double MoveSpeed = 10, DownStep = 17.5;
        private const int Rows = 5, Columns = 11;
        private const double Width = 60, Height = 45, HSpacing = 50, VSpacing = 40;
        
        public EnemyManager(Canvas gameCanvas, Dictionary<string, EnemyType> enemyTypes)
        {
            _gameCanvas = gameCanvas;
            _enemyTypes = enemyTypes;
        }

        /// <summary>
        /// Cria e posiciona uma nova onda de inimigos na tela.
        /// </summary>
        public void SpawnWave()
        {
            ClearSwarm();
            string[] formation = { "alien3", "alien2", "alien2", "alien1", "alien1" };
            for (int row = 0; row < Rows; row++)
            {
                string typeKey = formation[row];
                var enemyType = _enemyTypes[typeKey];
                for (int col = 0; col < Columns; col++)
                {
                    Image enemyImage = new Image {
                        Width = Width, Height = Height,
                        Source = new BitmapImage(new Uri(enemyType.ImageSource)),
                    };
                    var newEnemy = new Enemy(enemyImage, typeKey, enemyType.Points)
                    {
                        X = 50 + col * HSpacing,
                        Y = 80 + row * VSpacing
                    };
                    Enemies.Add(newEnemy);
                    _gameCanvas.Children.Add(enemyImage);
                }
            }
            _initialEnemyCount = Enemies.Count;
            _moveInterval = 25;
            _direction = 1;
        }

        /// <summary>
        /// Atualiza o estado da horda a cada tick do jogo.
        /// </summary>
        public void Update(double gameWidth, List<Rectangle> barrierBlocks)
        {
            MoveSwarm(gameWidth, barrierBlocks);
        }

        /// <summary>
        /// Controla a lógica de movimento da horda inteira.
        /// </summary>
        private void MoveSwarm(double gameWidth, List<Rectangle> barrierBlocks)
        {
            _moveCounter++;
            // A velocidade aumenta conforme o número de inimigos diminui
            _moveInterval = 2 + (int)(Enemies.Count / (double)_initialEnemyCount * 23);
            if (_moveCounter < _moveInterval) return;
            _moveCounter = 0;

            bool moveDown = false;
            
            // Verifica se a horda atingiu alguma borda lateral
            foreach (var enemy in Enemies)
            {
                if ((_direction > 0 && enemy.X + enemy.Width + MoveSpeed > gameWidth) || (_direction < 0 && enemy.X - MoveSpeed < 0))
                {
                    moveDown = true;
                    break;
                }
            }

            if (moveDown)
            {
                // Se atingiu a borda, inverte a direção e move todos para baixo
                _direction *= -1;
                foreach (var enemy in Enemies)
                {
                    enemy.Y += DownStep;
                    // Verifica se os inimigos estão colidindo com as barreiras ao descer
                    foreach (var block in barrierBlocks.ToList())
                    {
                        if (CheckCollision(enemy.Visual, block))
                        {
                            _gameCanvas.Children.Remove(block);
                            barrierBlocks.Remove(block);
                        }
                    }
                }
            }
            else
            {
                // Se não, continua movendo para o lado
                Enemies.ForEach(e => e.X += MoveSpeed * _direction);
            }

            // Atualiza a posição visual de todos os inimigos
            Enemies.ForEach(e => e.Update());
        }
        
        /// <summary>
        /// Checa a colisão entre dois elementos visuais.
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
        /// Seleciona aleatoriamente um dos inimigos que têm permissão para atirar.
        /// </summary>
        public Enemy? GetRandomShooter()
        {
            var potentialShooters = Enemies.Where(e => e.Type == "alien3").ToList();
            return potentialShooters.Any() ? potentialShooters[_random.Next(potentialShooters.Count)] : null;
        }
        
        /// <summary>
        /// Remove um inimigo da lista e da tela.
        /// </summary>
        public void RemoveEnemy(Enemy enemy)
        {
            enemy.IsAlive = false;
            _gameCanvas.Children.Remove(enemy.Visual);
            Enemies.Remove(enemy);
        }

        /// <summary>
        /// Limpa todos os inimigos da tela e da lista.
        /// </summary>
        private void ClearSwarm()
        {
            Enemies.ForEach(e => _gameCanvas.Children.Remove(e.Visual));
            Enemies.Clear();
        }
    }
}
