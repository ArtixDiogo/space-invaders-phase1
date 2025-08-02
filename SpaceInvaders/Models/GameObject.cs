using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace SpaceInvaders.Models
{
    /// <summary>
    /// Representa a base para qualquer objeto visível no jogo (Jogador, Inimigos).
    /// Controla a posição (X, Y) e a aparência visual.
    /// </summary>
    public abstract class GameObject
    {
        // O elemento da interface (uma Imagem, um Retângulo, etc.)
        public FrameworkElement Visual { get; protected set; }
        
        // Coordenadas X e Y no Canvas
        public double X { get; set; }
        public double Y { get; set; }
        
        // Propriedades para acessar facilmente a largura e altura do visual
        public double Width => Visual.Width;
        public double Height => Visual.Height;

        // Controla se o objeto ainda está ativo no jogo
        public bool IsAlive { get; set; } = true;

        protected GameObject(FrameworkElement visual)
        {
            Visual = visual;
        }

        /// <summary>
        /// Atualiza a posição do elemento visual no Canvas para corresponder às coordenadas X e Y.
        /// Este método deve ser chamado a cada frame do jogo.
        /// </summary>
        public virtual void Update()
        {
            Canvas.SetLeft(Visual, X);
            Canvas.SetTop(Visual, Y);
        }
    }
}
