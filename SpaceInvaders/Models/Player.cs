

namespace SpaceInvaders.Models
{
    public class Player : GameObject
    {
        public Player(Image visual) : base(visual) { }

        public void Move(double dx, double gameWidth)
        {
            double newX = X + dx;
            if (newX >= 0 && newX <= gameWidth - this.Width)
            {
                X = newX;
            }
        }
    }
}
