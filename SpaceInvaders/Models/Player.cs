using Microsoft.UI.Xaml.Controls;

namespace SpaceInvaders.Models
{
    /// <summary>
    /// Representa e controla o estado e as ações do jogador.
    /// </summary>
    public class Player : GameObject
    {
        public int Lives { get; private set; }
        public bool IsStunned => _stunCounter > 0; // Propriedade que retorna true se o jogador estiver paralisado
        
        private int _stunCounter; // Contador para o tempo de paralisia
        private const int StunDurationInTicks = 75; // Duração da paralisia (1.5 segundos)

        public Player(Image visual, int initialLives) : base(visual)
        {
            Lives = initialLives;
        }

        /// <summary>
        /// Move o jogador horizontalmente, respeitando os limites da tela.
        /// </summary>
        /// <param name="dx">A distância a ser movida (positiva para direita, negativa para esquerda).</param>
        /// <param name="gameWidth">A largura da área de jogo.</param>
        public void Move(double dx, double gameWidth)
        {
            if (IsStunned) return; // Não se move se estiver paralisado

            double newX = X + dx;
            // Garante que o jogador não saia da tela
            if (newX >= 0 && newX <= gameWidth - this.Width)
            {
                X = newX;
            }
        }

        /// <summary>
        /// Chamado quando o jogador é atingido por um tiro.
        /// </summary>
        public void TakeHit()
        {
            if (IsStunned) return; // Não pode ser atingido novamente se já estiver paralisado
            
            Lives--;
            if (Lives > 0)
            {
                // Ativa a paralisia
                _stunCounter = StunDurationInTicks;
            }
            else
            {
                // Fim de jogo
                IsAlive = false;
            }
        }

        /// <summary>
        /// Adiciona uma vida, até o limite máximo.
        /// </summary>
        public void AddLife(int maxLives)
        {
            if (Lives < maxLives)
            {
                Lives++;
            }
        }

        /// <summary>
        /// Atualiza o estado do jogador a cada tick (ex: paralisia e efeito de piscar).
        /// </summary>
        public override void Update()
        {
            if (IsStunned)
            {
                _stunCounter--;
                // Efeito visual de piscar, alternando a opacidade
                Visual.Opacity = (_stunCounter % 10 < 5) ? 1.0 : 0.2;
            }
            else
            {
                // Garante que a opacidade volte ao normal
                Visual.Opacity = 1.0;
            }
            // Chama o Update da classe base para atualizar a posição no Canvas
            base.Update();
        }
    }
}
