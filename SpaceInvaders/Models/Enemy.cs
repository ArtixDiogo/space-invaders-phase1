using Microsoft.UI.Xaml.Controls;

namespace SpaceInvaders.Models
{
    /// <summary>
    /// Representa um único inimigo alienígena na tela.
    /// Herda de GameObject, então já tem posição e visual.
    /// </summary>
    public class Enemy : GameObject
    {
        // O tipo do inimigo (ex: "alien1", "alien2") para saber seus pontos e imagem
        public string Type { get; }
        // A pontuação que este inimigo específico concede
        public int Points { get; }

        public Enemy(Image visual, string type, int points) : base(visual)
        {
            Type = type;
            Points = points;
        }
    }
}
