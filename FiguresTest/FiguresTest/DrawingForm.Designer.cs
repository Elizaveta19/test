using FiguresTest.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace FiguresTest
{
    partial class DrawingForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }
        
        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new Size(800, 450);
            this.Text = "DrawingForm";

            PictureBox imageControl = new PictureBox();
            imageControl.Width = 800;
            imageControl.Height = 450;

            imageControl.Name = "imageControl";
            imageControl.TabIndex = 5;
            imageControl.TabStop = false;
            imageControl.Paint += new PaintEventHandler(this.imageControl_Paint); 

            Controls.Add(imageControl);
        }
        
        private void imageControl_Paint(object sender, PaintEventArgs e)
        {
            ICollection<Figure> figuresCollection = new List<Figure>()
            {
                new Model.Circle(Color.Black, 2, new Point(150, 150), 50),
                new Model.Rectangle(Color.Red, 3, new Point(50, 50), 50, 60),
                new Model.Square(Color.RoyalBlue, 3, new Point(250, 250), 100),
                new Model.Triangle(Color.Brown, 4, new Point(400, 100), new Point(450, 200), new Point(550, 150))
            };

            foreach (var figure in figuresCollection)
            {
                figure.Draw(e.Graphics);
            }
        }
    }
}

