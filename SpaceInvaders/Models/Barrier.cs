using Microsoft.UI.Xaml.Shapes;

namespace SpaceInvaders.Models
{
    public class Barrier
    {
        public Rectangle Visual { get; set; }
        public int Health { get; set; }
        public int MaxHealth { get; }

        public Barrier(Rectangle visual, int health)
        {
            Visual = visual;
            Health = health;
            MaxHealth = health;
        }

        public void TakeDamage()
        {
            if (Health > 0)
            {
                Health--;
            }
        }
    }
}
