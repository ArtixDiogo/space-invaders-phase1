using Windows.UI;

namespace SpaceInvaders.Models
{
    /// <summary>
    /// Estrutura para guardar as informações de cada TIPO de inimigo (não um inimigo individual).
    /// Funciona como um "molde" ou "template".
    /// </summary>
    public struct EnemyType
    {
        public string ImageSource { get; set; } // Caminho para a imagem
        public int Points { get; set; }         // Pontos que ele vale
        public string DisplayName { get; set; } // Nome para exibição
    }
}
