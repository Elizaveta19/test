using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FiguresTest.Model
{
    internal abstract class Figure
    {
        /// <summary>
        /// Цвет
        /// </summary>
        protected System.Drawing.Color _color { get; set; }

        /// <summary>
        /// Ширина линии при отрисовки фигуры
        /// </summary>
        protected float _lineWidth { get; set; }

        /// <summary>
        /// Проверка фигуры на существование
        /// </summary>
        /// <returns></returns>
        protected abstract bool IsValid { get; }

        /// <summary>
        /// Вычисление площади фигуры
        /// </summary>
        /// <returns>Площаль фигуры. Null - если фигура с такими параметрами не существует</returns>
        public abstract double? Area();

        public Figure(System.Drawing.Color color, float lineWidth)
        {
            _color = color;
            _lineWidth = lineWidth;
        }

        /// <summary>
        /// Вывод на экран фигуры 
        /// </summary>
        public abstract void Draw(Graphics graphics);
    }
}
