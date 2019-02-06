using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FiguresTest.Model
{
    internal class Circle : Figure
    {
        /// <summary>
        /// Центр окружности
        /// </summary>
        private System.Drawing.Point _centralPoint { get; set; }

        /// <summary>
        /// Радиус
        /// </summary>
        private int _radius { get; set; }
        
        public Circle(System.Drawing.Color color, float lineWidth, System.Drawing.Point leftTopPoint,  int radius) : base(color, lineWidth)
        {
            _centralPoint = new Point(leftTopPoint.X + radius, leftTopPoint.Y + radius);
            _radius = radius;
        }
        
        protected override bool IsValid
        {
            get
            {
                return _radius > 0;
            }
        }

        public override void Draw(Graphics graphics)
        {
            if (IsValid)
            {
                using (Pen pen = new Pen(_color, _lineWidth))
                {
                    // Create rectangle for ellipse.
                    System.Drawing.Rectangle rect = new System.Drawing.Rectangle(
                                                    _centralPoint.X - _radius, 
                                                    _centralPoint.Y - _radius,
                                                    _radius,
                                                    _radius);
                    graphics.DrawEllipse(pen, rect);
                }
            }
        }

        public override double? Area()
        {
            double? result = null;

            if (IsValid)
            {
                result = Math.PI * _radius * _radius;
            }
            return result;
        }
    }
}
