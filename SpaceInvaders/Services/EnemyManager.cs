using Microsoft.UI.Xaml.Media.Imaging;
using SpaceInvaders.Models;

namespace SpaceInvaders.Services
{
    public class EnemyManager
    {
        public List<Enemy> Enemies { get; } = new();
        public bool IsSwarmDestroyed => !Enemies.Any();

        private readonly Canvas _gameCanvas;
        private readonly Dictionary<string, EnemyType> _enemyTypes;
        private readonly SoundManager _soundManager;

        // Variáveis para controle de velocidade
        private double _currentSpeed;
        private double _direction = 1; // Apenas a direção, não mais a velocidade
        private const double InitialSpeed = 1.0;
        private const double SpeedIncrement = 0.05; // Aumento de velocidade por inimigo destruído

        private bool _moveDown = false;
        private int _moveSoundToggle = 0;
        
        private const double Width = 40, Height = 30, HSpacing = 55, VSpacing = 40;

        // Propriedade pública para que a MainPage possa saber a velocidade atual
        public double CurrentSpeed => _currentSpeed;

        public EnemyManager(Canvas gameCanvas, Dictionary<string, EnemyType> enemyTypes, SoundManager soundManager)
        {
            _gameCanvas = gameCanvas;
            _enemyTypes = enemyTypes;
            _soundManager = soundManager;
            _currentSpeed = InitialSpeed;
        }

        public void Update(double screenWidth)
        {
            if (!Enemies.Any()) return;

            if (_moveDown)
            {
                foreach (var enemy in Enemies)
                {
                    enemy.Y += 15;
                }
                _direction *= -1;
                _moveDown = false;
                return;
            }

            bool reachedEdge = false;
            foreach (var enemy in Enemies)
            {
                // O movimento agora é a direção * velocidade atual
                enemy.X += _direction * _currentSpeed;
                if (enemy.X <= 0 || enemy.X >= screenWidth - enemy.Width)
                {
                    reachedEdge = true;
                }
            }
            
            if(reachedEdge)
            {
                _moveDown = true;
            }

            Enemies.ForEach(e => e.Update());

            _soundManager.PlaySound($"invader_move_{_moveSoundToggle + 1}");
            _moveSoundToggle = (_moveSoundToggle + 1) % 4;
        }

        public void SpawnWave()
        {
            ClearSwarm();
            ResetSpeed(); // Reseta a velocidade para a onda nova
            string[] formation = { "alien3", "alien2", "alien2", "alien1", "alien1" };
            for (int row = 0; row < formation.Length; row++)
            {
                string typeKey = formation[row];
                var enemyType = _enemyTypes[typeKey];
                for (int col = 0; col < 11; col++)
                {
                    Image enemyImage = new Image
                    {
                        Width = Width, Height = Height,
                        Source = new BitmapImage(new Uri(enemyType.ImageSource)),
                    };
                    var newEnemy = new Enemy(enemyImage, enemyType.Points)
                    {
                        X = 60 + col * HSpacing,
                        Y = 100 + row * VSpacing
                    };
                    newEnemy.Update();
                    Enemies.Add(newEnemy);
                    _gameCanvas.Children.Add(enemyImage);
                }
            }
        }
        
        public void IncreaseSpeed()
        {
            _currentSpeed += SpeedIncrement;
        }

        public void ResetSpeed()
        {
            _currentSpeed = InitialSpeed;
        }

        public Enemy? GetShooter()
        {
            var potentialShooters = Enemies.Where(e => e.Points == 40).ToList();
            if (!potentialShooters.Any()) return null;

            var random = new Random();
            int index = random.Next(potentialShooters.Count);
            return potentialShooters[index];
        }

        public void RemoveEnemy(Enemy enemy)
        {
            enemy.IsAlive = false;
            IncreaseSpeed(); // Aumenta a velocidade quando um inimigo é removido
            _gameCanvas.Children.Remove(enemy.Visual);
            Enemies.Remove(enemy);
        }

        private void ClearSwarm()
        {
            Enemies.ForEach(e => _gameCanvas.Children.Remove(e.Visual));
            Enemies.Clear();
        }
    }
}
