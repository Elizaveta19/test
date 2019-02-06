using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FiguresTest.Model
{
    internal class Square : Model.Rectangle
    {
        public int _width { get; set; }

        public Square(System.Drawing.Color color, int lineWidth, System.Drawing.Point leftTopPoint, int width) : base(color, lineWidth, leftTopPoint, width, width) { }

        protected override bool IsValid => base.IsValid;

        public override void Draw(Graphics graphics)
        {
            base.Draw(graphics);
        }

        public override double? Area()
        {
            return base.Area();
        }
    }
}
