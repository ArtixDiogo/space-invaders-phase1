

namespace SpaceInvaders.Models
{
    public class Enemy : GameObject
    {
        public int Points { get; }
        public Enemy(Image visual, int points) : base(visual)
        {
            Points = points;
        }
    }
}
