
namespace SpaceInvaders.Models
{
    public abstract class GameObject
    {
        public FrameworkElement Visual { get; protected set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Width => Visual.Width;
        public double Height => Visual.Height;
        public bool IsAlive { get; set; } = true;

        protected GameObject(FrameworkElement visual)
        {
            Visual = visual;
        }

        public virtual void Update()
        {
            Canvas.SetLeft(Visual, X);
            Canvas.SetTop(Visual, Y);
        }
    }
}
