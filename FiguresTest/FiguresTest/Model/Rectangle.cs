using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FiguresTest.Model
{
    internal class Rectangle : Figure
    {
        /// <summary>
        /// Верхний левый улог прямоугольника, от которого будет рисоваться фигура
        /// </summary>
        private Point _leftTopPoint { get; set; }

        /// <summary>
        /// Ширина
        /// </summary>
        private int _width { get; set; }

        /// <summary>
        /// Высота
        /// </summary>
        private int _height { get; set; }

        public Rectangle(System.Drawing.Color color, int lineWidth, System.Drawing.Point leftTopPoint,  int width, int height) : base(color, lineWidth)
        {
            _leftTopPoint = leftTopPoint;
            _width = width;
            _height = height;
        }

        protected override bool IsValid
        {
            get
            {
                return _width > 0 && _height > 0;
            }
        }

        public override void Draw(Graphics graphics)
        {
            if (IsValid)
            {
                using (Pen pen = new Pen(_color, _lineWidth))
                {
                    graphics.DrawRectangle(pen, _leftTopPoint.X, _leftTopPoint.Y, _width, _height);
                }
            }
        }

        public override double? Area()
        {
            double? result = null;

            if (IsValid)
            {
                result = _width * _height;
            }
            return result;
        }
    }
}
