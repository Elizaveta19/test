using System;
using System.Drawing;

namespace FiguresTest.Model
{
    internal class Triangle : Figure
    {
        private Point _pointA { get; set; }
        private Point _pointB { get; set; }
        private Point _pointC { get; set; }

        private double _sideALength
        {
            get
            {
                return Math.Sqrt(Math.Pow(_pointB.X - _pointC.X, 2) + Math.Pow(_pointB.Y - _pointC.Y, 2));
            }
        }

        private double _sideBLength
        {
            get
            {
                return Math.Sqrt(Math.Pow(_pointA.X - _pointC.X, 2) + Math.Pow(_pointA.Y - _pointC.Y, 2));
            }
        }

        private double _sideCLength
        {
            get
            {
                return Math.Sqrt(Math.Pow(_pointA.X - _pointB.X, 2) + Math.Pow(_pointA.Y - _pointB.Y, 2));
            }
        }

        public Triangle(System.Drawing.Color color, int lineWidth, System.Drawing.Point pointA, System.Drawing.Point pointB, System.Drawing.Point pointC) : base(color, lineWidth)
        {
            _pointA = pointA;
            _pointB = pointB;
            _pointC = pointC;
        }

        /// <summary>
        /// Проверка треугольника на существование
        /// </summary>
        protected override bool IsValid
        {
            get
            {
                if (_sideALength > 0 && _sideBLength > 0 && _sideCLength > 0 
                    && _sideALength + _sideBLength > _sideCLength 
                    && _sideALength + _sideCLength > _sideBLength 
                    && _sideBLength + _sideCLength > _sideALength)
                    return true;
                else
                    return false;
            }
        }

        public override void Draw(Graphics graphics)
        {
            if (IsValid)
            {
                using (Pen pen = new Pen(_color, _lineWidth))
                {
                    graphics.DrawPolygon(pen, new Point[] { _pointA, _pointB, _pointC });
                }
            }
        }

        public override double? Area()
        {
            double? result = null;

            if (IsValid)
            {
                //по формуле Герона
                double halfPerimeter = (_sideALength + _sideBLength + _sideCLength) / 2;
                result = Math.Sqrt(halfPerimeter * (halfPerimeter - _sideALength) * (halfPerimeter - _sideBLength) * (halfPerimeter - _sideCLength));
            }
            return result;
        }
    }
}
