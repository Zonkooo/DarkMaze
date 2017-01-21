using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using C3.XNA;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Darkmaze
{
    public class Enemy
    {
        private const int Radius = 80;
        public Vector2 Position { get; set; }
        public bool Active { get; set; }
        public Vector2 Target { get; set; }

        private Vector2 _curMove;
        private Vector2 _curDir;
        private float _impactRadius;
        private Vector2 _center;
        private float _startingAngle;
        private float _endingAngle;
        private bool _onTarget;

        public void Attack(Point target)
        {
            Target = target.ToVector2();
            Active = true;
            _center = GetCenter();
            _startingAngle = GetAngle(Position);
            _endingAngle = GetAngle(Target);

            _curDir = Target - Position;
            _curDir.Normalize();
            _curMove = new Vector2();
            _impactRadius = 0;
            _onTarget = false;
        }
        
        public void Update()
        {
            if (Active)
            {
                if ((Position + _curMove - Target).LengthSquared() > 1f)
                {
                    _curMove += _curDir;
                }
                else
                {
                    if (!_onTarget)
                    {
                        _onTarget = true;
                        Position = Target;
                        _curMove = Vector2.Zero;
                    }
                    _impactRadius++;
                }
                if (_impactRadius >= 20)
                {
                    Active = false;
                }
            }
        }

        public bool KillsOnPosition(Vector2 pos)
        {
            return Active && (pos - Position).Length() < (_impactRadius/2);
        }

        private Vector2 GetCenter()
        {
            var a = Position;
            var b = Target;
            float ab = (a - b).Length();
            float bc = Radius;
            float ac = Radius;

            var costheta = (bc * bc - ac * ac - ab * ab) / (-2 * ac * ab); //((BC) ^ 2 - (AC) ^ 2 - (AB) ^ 2) / (-2(AC)(AB))
            var sintheta = (float)Math.Sqrt((1 - (bc * bc - ac * ac - ab * ab)) / ((-2 * ac * ab) * (-2 * ac * ab))); //sqrt(1-((BC)^2 - (AC)^2 - (AB)^2)/(-2 (AC)(AB))^2)

            var v1 = (costheta * (b.X - a.X) + sintheta * (b.Y - a.Y)) / ab; //(cos(theta)*(x2-x1) + sin(theta)*(y2-y1))/(AB)
            var v2 = (costheta * (b.Y - a.Y) - sintheta * (b.X - a.X)) / ab; //(cos(theta)*(y2-y1) - sin(theta)*(x2-x1))/(AB)

            var c = a + new Vector2(v1, v2);
            return c;
        }

        private float GetAngle(Vector2 p)
        {
            var point = p - _center;
            point.Normalize();
            return MathHelper.TwoPi - (float)Math.Atan2(point.Y, point.X);
        }

        public void Draw(SpriteBatch sb)
        {
            if (Active)
            {
//                sb.DrawArc(_center, Radius, 8, _startingAngle, _endingAngle, Color.Red);
                sb.DrawLine(Position*2, (Position+_curMove)*2, Color.Red);
                if(_impactRadius > 1)
                    sb.DrawCircle(Target*2, _impactRadius, 16, Color.Red);
            }
        }
    }
}
