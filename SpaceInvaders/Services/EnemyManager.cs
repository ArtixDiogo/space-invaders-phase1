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

        private const double Width = 40, Height = 30, HSpacing = 80, VSpacing = 40;
        
        public EnemyManager(Canvas gameCanvas, Dictionary<string, EnemyType> enemyTypes)
        {
            _gameCanvas = gameCanvas;
            _enemyTypes = enemyTypes;
        }

        // Os inimigos são estáticos na "primeira fase", então o Update fica vazio.
        public void Update() { }

        public void SpawnWave()
        {
            ClearSwarm();
            string[] formation = { "alien3", "alien2", "alien1" };
            for (int row = 0; row < formation.Length; row++)
            {
                string typeKey = formation[row];
                var enemyType = _enemyTypes[typeKey];
                for (int col = 0; col < 5; col++)
                {
                    Image enemyImage = new Image {
                        Width = Width, Height = Height,
                        Source = new BitmapImage(new Uri(enemyType.ImageSource)),
                    };
                    var newEnemy = new Enemy(enemyImage, enemyType.Points)
                    {
                        X = 100 + col * HSpacing,
                        Y = 100 + row * VSpacing
                    };
                    newEnemy.Update();
                    Enemies.Add(newEnemy);
                    _gameCanvas.Children.Add(enemyImage);
                }
            }
        }
        
        public void RemoveEnemy(Enemy enemy)
        {
            enemy.IsAlive = false;
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
