using System;
using C3.XNA;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Darkmaze
{
    public class Enemy
    {
        private const float ImpactSpeed = 1f;
        private const int MaxRadius = 30;
        private const float MoveSpeed = 1f;

        public Vector2 Position { get; set; }
        public bool Active { get; set; }
        public Vector2 Target { get; set; }

        private Vector2 _curMove;
        private Vector2 _curDir;
        private float _impactRadius;
        private bool _onTarget;
        private Texture2D _tex;

        public Enemy(Texture2D enemyTex)
        {
            _tex = enemyTex;
        }

        public void Attack(Point target, float precision)
        {
            //TODO : don't end on on walls or outside of the play area
            var perturbation = new Vector2((float) Core.FakeGaussianRandom(8f), (float) Core.FakeGaussianRandom(8f));
            Target = target.ToVector2() + perturbation;
            Active = true;

            _curDir = Target - Position;
            _curDir.Normalize();
            _curDir *= MoveSpeed;
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
                    _impactRadius += ImpactSpeed;
                }
                if (_impactRadius >= MaxRadius)
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
                //sb.DrawLine(Position*2, (Position+_curMove)*2, Color.Red);

                sb.DrawCircle(Target*2, _impactRadius, 16, Color.Black, 2f);
                sb.DrawCircle(Target*2, _impactRadius - 6, 16, Color.Black, 2f);
                sb.DrawCircle(Target*2, _impactRadius - 12, 16, Color.Black, 2f);

                var pos = (Position + _curMove) * 2;
                var angle = MathHelper.Pi - (float)Math.Atan2(_curDir.X, _curDir.Y);
                sb.Draw(_tex, pos, origin: new Vector2(16, 16), rotation:angle);
            }
        }
    }
}
