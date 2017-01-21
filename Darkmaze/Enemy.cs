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
        private bool _onTarget;

        public void Attack(Point target)
        {
            Target = target.ToVector2();
            Active = true;

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
