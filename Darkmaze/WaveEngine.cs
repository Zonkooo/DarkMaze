using System;
using Microsoft.Xna.Framework;

//from https://www.codeproject.com/Articles/994051/Wave-Simulator-with-Csharp
namespace WaveSimulator
{
    public class WaveEngine
    {
        // "vd" means "vertex data"
        float[] vd; // Height map
        float[] vdv; // Velocity map
        float[] vda; // Acceleration map
        float[] vds; // Sustainability map. Sustainability can be thought as anti-damping.
        bool[] vd_static; // Static particle map. Particles which will act like a obstacle or wall.



        float mass = 0.1f; // Mass of each particle. It is the same for all particles.
        float action_resolution = 20f; // Resolution of movement of particles.
        float sustain = 30f; // Anti-damping. Propagation range increases by increasing this variable. Minimum is 1f.
        float phase1 = 0f; // Current phase value of oscillator1.
        float phase2 = 0f; // Current phase value of oscillator2.

        bool osc1active = false; // Is oscillator1 is turned on?
        bool osc2active = false; // Is oscillator2 is turned on?

        int osc1point = 0; // Location of the oscillator1 in the wave pool. It is an index value.
        int osc2point = 0; // Location of the oscillator2 in the wave pool. It is an index value.

        int size = 200; // Size of the wave pool. It indicates both the width and height since the pool will always be a square.

        readonly Vector3 color1 = Color.White.ToVector3(); // Color of the crest or trough.
        readonly Vector3 color2 = Color.Black.ToVector3(); // Color of the crest or trough. 


        // These variables are used for edge absorbtion. It is used for eliminating reflection from window boundaries.
        int absorb_offset = 10; // Offset from each window boundary where the sustainability starts to decrease.
        float min_sustain = 2f; // The lowest sustainability value. They are located at the boundaries.
        bool edge_absorbtion = true; // If true, the particles near the boundaries will have low sustainability.
        
        [Flags]
        public enum ParticleAttribute
        {
            Height = 1,
            Velocity = 2,
            Acceleration = 4,
            Sustainability = 8,
            Fixity = 16,
            All = 32,
        }


        public float Mass
        {
            get { return mass; }
            set
            {
                if (value > 0f)
                {
                    mass = value;
                }
            }
        }
        
        public float ActionResolution
        {
            get { return action_resolution; }
            set
            {
                if (value >= 1f)
                {
                    action_resolution = value;
                }
            }
        }

        public float Sustainability
        {
            get { return sustain; }
            set
            {
                if (value >= 1f)
                {
                    sustain = value;
                    SetSustain();
                }
            }
        }
        /// <summary>
        /// should be between 0 and 2PI
        /// </summary>
        public float PhaseRate1 { get; set; } = 0.2f;

        /// <summary>
        /// should be between 0 and 2PI
        /// </summary>
        public float PhaseRate2 { get; set; } = 0.2f;

        public int Size
        {
            get { return size; }
            set
            {
                if (size >= 1f)
                {
                    size = value;
                    SetPool();
                }
            }
        }
        public float EdgeSustainability
        {
            get { return min_sustain; }
            set
            {
                if (value >= 1f)
                {
                    min_sustain = value;
                    SetSustain();
                }
            }
        }
        public int AbsorbtionOffset
        {
            get { return absorb_offset; }
            set
            {
                if (value > 0 && value < size / 2)
                {
                    absorb_offset = value;
                    SetSustain();
                }
            }
        }
        public Color ColorStatic { get; set; }

        public bool HighContrast { get; set; } = false;

        public bool EdgeAbsorbtion
        {
            get { return edge_absorbtion; }
            set
            {
                edge_absorbtion = value;
                SetSustain();
            }
        }

        public bool Oscillator1Active
        {
            get { return osc1active; }
            set
            {
                osc1active = value;
                SetSustain();
            }
        }

        public bool Oscillator2Active
        {
            get { return osc2active; }
            set
            {
                osc2active = value;
                SetSustain();
            }
        }
        
        public Point Oscillator1Position
        {
            get { return new Point(osc1point % size, osc1point/size); }
            set
            {
                if (value.X + value.Y * size < size * size)
                {
                    osc1point = value.X + value.Y * size;
                    SetSustain();
                }
            }
        }

        public Point Oscillator2Position
        {
            get { return new Point(osc2point % size, (int)Math.Floor((float)osc2point / (float)size)); }
            set
            {
                if (value.X + value.Y * size < size * size)
                {
                    osc2point = value.X + value.Y * size;
                    SetSustain();
                }
            }
        }

        /// <summary>
        /// Initializes the WaveEngine
        /// </summary>
        /// <param name="control">The control where the engine renders on.</param>
        public WaveEngine(int size)
        {
            this.size = size;
            ColorStatic = new Color(color1 + color2 / 2);
            SetPool();
        }

        public void OneStep(uint[] buffer)
        {
            CalculateForces();
            Generatebitmap(buffer);
        }
        
        /// <summary>
        /// Sets particles' specified attribute(s) to a specified value in a specified rectangular area.
        /// </summary>
        /// <param name="rect">Rectangular area which contains particles.</param>
        /// <param name="value">Value to set the particles to.</param>
        /// <param name="partatt">Attribute(s) that will be set.</param>
        public void SetParticles(Rectangle rect, float value, ParticleAttribute partatt)
        {
            if (rect.X < 0)
                rect.X = 0;

            if (rect.Y < 0)
                rect.Y = 0;

            if (rect.Width + rect.X > size)
                rect.Width -= (rect.X + rect.Width) - size;

            if (rect.Height + rect.Y > size)
                rect.Height -= (rect.Y + rect.Height) - size;

            bool xh = false, xv = false, xa = false, xs = false, xf = false;
            // Let's see which attributes we are gonna deal with.
            if ((ParticleAttribute.All & partatt) == ParticleAttribute.All)
            {
                xh = true; xv = true; xa = true; xs = true; xf = true;
            }
            else
            {
                if ((ParticleAttribute.Height & partatt) == ParticleAttribute.Height)
                    xh = true;
                if ((ParticleAttribute.Velocity & partatt) == ParticleAttribute.Velocity)
                    xv = true;
                if ((ParticleAttribute.Acceleration & partatt) == ParticleAttribute.Acceleration)
                    xa = true;
                if ((ParticleAttribute.Sustainability & partatt) == ParticleAttribute.Sustainability)
                    xs = true;
                if ((ParticleAttribute.Fixity & partatt) == ParticleAttribute.Fixity)
                    xf = true;
            }

            for (int y = rect.Y * size; y < rect.Y * size + rect.Height * size; y += size)
            {
                for (int x = rect.X; x < rect.X + rect.Width; x++)
                {
                    if (xh)
                        vd[x + y] = value;
                    if (xv)
                        vdv[x + y] = value;
                    if (xa)
                        vda[x + y] = value;
                    if (xs)
                        vds[x + y] = value;
                    if (xf)
                        vd_static[x + y] = Convert.ToBoolean(value);
                }
            }
        }

        public void SetWall(int x, int y)
        {
            vd_static[x + y*size] = true;
        }

        public bool IsWall(int x, int y)
        {
            return vd_static[x + y * size];
        }

        /// <summary>
        /// Gives a float array of specified attribute of particles in a specified rectangular area.
        /// </summary>
        /// <param name="rect">Rectangular area which contains particles.</param>
        /// <param name="partatt">Attribute whose array will be given. Only one attribute can be specified and "All" cannot be specified.</param>
        public float[] GetParticles(Rectangle rect, ParticleAttribute partatt)
        {
            float[] result = new float[1];

            bool xh = false, xv = false, xa = false, xs = false, xf = false;

            if ((int)partatt == 1 || (int)partatt == 2 || (int)partatt == 4 || (int)partatt == 8 || (int)partatt == 16)
            {

                if (rect.X < 0)
                    rect.X = 0;

                if (rect.Y < 0)
                    rect.Y = 0;

                if (rect.Width + rect.X > size)
                    rect.Width -= (rect.X + rect.Width) - size;

                if (rect.Height + rect.Y > size)
                    rect.Height -= (rect.Y + rect.Height) - size;

                result = new float[rect.Width * rect.Height];

                if (partatt == ParticleAttribute.Height)
                    xh = true;
                if (partatt == ParticleAttribute.Velocity)
                    xv = true;
                if (partatt == ParticleAttribute.Acceleration)
                    xa = true;
                if (partatt == ParticleAttribute.Sustainability)
                    xs = true;
                if (partatt == ParticleAttribute.Fixity)
                    xf = true;

                int r = 0;
                for (int y = rect.Y * size; y < rect.Y * size + rect.Height * size; y += size)
                {
                    for (int x = rect.X; x < rect.X + rect.Width; x++)
                    {
                        if (xh)
                            result[r] = vd[x + y];
                        if (xv)
                            result[r] = vdv[x + y];
                        if (xa)
                            result[r] = vda[x + y];
                        if (xs)
                            result[r] = vds[x + y];
                        if (xf)
                            result[r] = Convert.ToSingle(vd_static[x + y]);
                        r += 1;
                    }
                }
            }

            return result;
        }

        void CalculateForces()
        {
            float total_height = 0;// This will be used to shift the height center of the whole particle system to the origin.

            // This loop calculates the forces exerted on the particles.
            //Parallel.For(0, vd.Length, new ParallelOptions {MaxDegreeOfParallelism = 4}, index =>
            for (int index = 0; index < vd.Length; index += 1)
            {
                // If this is a static particle, it will not move at all. Continue with the next particle.
                if (vd_static[index])
                {
                    vd[index] = 0;
                    vdv[index] = 0;
                    vda[index] = 0;
                    goto cont;
                }


                if (index == osc1point && osc1active)
                {
                    // This is where the oscillator1 is located. It is currently active.
                    // So this particle only serves as an oscillator for neighbor particles.
                    // It will not be affected by any forces. It will just move up and down.
                    vdv[index] = 0;
                    vda[index] = 0;
                    vd[index] = (float) Math.Sin(phase1);
                    phase1 += PhaseRate1;
                    if (phase1 >= 2f * (float) Math.PI)
                        phase1 -= (float) Math.PI * 2f;

                    goto cont;
                }

                if (index == osc2point && osc2active)
                {
                    vdv[index] = 0;
                    vda[index] = 0;
                    vd[index] = (float) Math.Sin(phase2);
                    phase2 += PhaseRate2;
                    if (phase2 >= 2f * (float) Math.PI)
                        phase2 -= (float) Math.PI * 2f;

                    goto cont;
                }

                // So this particle is neither an oscillator nor static. So let's calculate the force.

                // Reset the acceleration. We do this because acceleration dynamically changes with the force.
                vda[index] = 0;

                // Sum up all the height values so we will find the average height of the system.
                // This doesn't contribute to the force calculation. It is immaterial.
                total_height += vd[index];

                // Now we will find out the average height of the 8 neighbor particles.
                // So we will know where the current particle will be attracted to.

                // "heights" is the sum of all the height values of neighbor particles.
                float heights = 0;
                // "num_of_part" is the number of particles which contributed to the "heights".
                int num_of_part = 0;


                //// UP
                if (!(index >= 0 && index < size)) // Make sure that this point is not on a boundary.
                {
                    if (!vd_static[index - size]) // Make sure that the neighbor particle is not static.
                    {
                        heights += vd[index - size];

                        num_of_part += 1;
                    }
                }


                //// UPPER-RIGHT
                if (!((index + 1) % size == 0 || (index >= 0 && index < size)))
                {
                    if (!vd_static[index - size + 1])
                    {
                        heights += vd[index - size + 1];

                        num_of_part += 1;
                    }
                }

                //// RIGHT
                if (!((index + 1) % size == 0))
                {
                    if (!vd_static[index + 1])
                    {
                        heights += vd[index + 1];

                        num_of_part += 1;
                    }
                }

                //// LOWER-RIGHT
                if (!((index + 1) % size == 0 || (index >= (size * size) - size && index < (size * size))))
                {
                    if (!vd_static[index + size + 1])
                    {
                        heights += vd[index + size + 1];

                        num_of_part += 1;
                    }
                }

                //// DOWN
                if (!(index >= (size * size) - size && index < (size * size)))
                {
                    if (!vd_static[index + size])
                    {
                        heights += vd[index + size];

                        num_of_part += 1;
                    }
                }

                //// LOWER-LEFT
                if (!(index % size == 0 || (index >= (size * size) - size && index < (size * size))))
                {
                    if (!vd_static[index + size - 1])
                    {
                        heights += vd[index + size - 1];

                        num_of_part += 1;
                    }
                }


                //// LEFT
                if (!(index % size == 0))
                {
                    if (!vd_static[index - 1])
                    {
                        heights += vd[index - 1];

                        num_of_part += 1;
                    }
                }


                // UPPER-LEFT

                if (!(index % size == 0 || (index >= 0 && index < size)))
                {
                    if (!vd_static[index - size - 1])
                    {
                        heights += vd[index - size - 1];

                        num_of_part += 1;
                    }
                }


                if (num_of_part != 0)
                {
                    heights /= (float) num_of_part;
                    
                    vda[index] += -(vd[index] - heights) / mass;
                }


                // Damping takes place.
                vda[index] -= vdv[index] / vds[index];

                // Don't let things go beyond their limit.
                // This makes sense. It eliminates a critic uncertainty.
                vda[index] = MathHelper.Clamp(vda[index], -1f, 1f);

                cont:
                ;
            }//);
            // Now we have finished with the force calculation.

            // Origin height is zero. So "shifting" is the distance between the system average height and the origin.
            float shifting = -total_height / (float)vd.Length;



            // We are taking the final steps in this loop
            for (int index = 0; index < vd.Length; index += 1)
            {

                // Acceleration feeds velocity. Don't forget that we took care of the damping before.
                vdv[index] += vda[index];


                // Here is the purpose of "action_resolution":
                // It is used to divide movements.
                // If the particle goes along the road at once, a chaos is most likely unavoidable.
                vd[index] = MathHelper.Clamp(vd[index] + vdv[index] / action_resolution, -1f, 1f);


                // Here is the last step on shifting the whole system to the origin point.
                vd[index] += shifting;


            }

        }


        void Generatebitmap(uint[] rgbdata)
        {
            // It's time for the coloration of the height.
            for (int index = 0; index < vd.Length; index++)
            {
                // Brightness. This value is the 'brightness' of the height.
                // Now we see why "limit" makes sense.
                byte bright = (byte)((vd[index] + 1f) / (2f / 255f));

                if (vd_static[index])
                {
                    rgbdata[index] = GetPackedValue((color1+color2)/2);
                }
                else
                {
                    if (HighContrast)
                    {
                        if (vd[index] > 0)
                        {
                            rgbdata[index] = GetPackedValue(color1);
                        }
                        else
                        {
                            rgbdata[index] = GetPackedValue(color2);
                        }
                    }
                    else
                    {
                        float brightr1 = bright / 255f;
                        float brightr2 = 1f - brightr1;
                        var c = color1 * brightr1 + color2 * brightr2;
                        rgbdata[index] = GetPackedValue(c);
                    }
                }
            }
            
        }

        private static uint GetPackedValue(Vector3 c)
        {
            uint i = 0xFF000000;           //A
            i += (uint) (c.Z * 255) << 16; //B
            i += (uint) (c.Y * 255) << 8;  //G
            i += (uint) (c.X * 255);       //R
            return i;
        }

        /// <summary>
        /// Sets sustainability of each particle.
        /// </summary>
        void SetSustain()
        {
            if (edge_absorbtion)
            {
                // We will fill "vds" array with "sustain" then we will deal with elements near to window boundaries.

                // Since we want the sustainability to decrease towards the edges, "min_sustain" can't be bigger than "sustain".
                if (min_sustain > sustain)
                {
                    min_sustain = 1.0f; // even "sustain" can't be less than 1.0f so this is a reliable value.
                }

                // Sustainability reduction fields should not mix with each other. So the maximum offset is the middle-screen.
                if (absorb_offset >= size / 2)
                {
                    absorb_offset = size / 2 - 1;
                }

                // This value is sustainability decreasion rate per row/column. The decreasion is linear.
                float dec = (sustain - min_sustain) / (float)absorb_offset;
                // This one stores the current sustainability.
                float cur = min_sustain;

                // First, we fill "vds" array with "sustain".
                for (int i = 0; i < vds.Length - 1; i++)
                    vds[i] = sustain;

                // This loop sets up the sustainability values for the top.
                for (int off = 0; off <= absorb_offset; off++)
                {
                    // Process each row/column from the edge to the offset.
                    for (int x = off; x < size - off; x++)
                    {
                        // Process each sustainability element in the current row/column
                        vds[x + off * size] = cur;
                    }
                    cur += dec;
                }

                cur = sustain; // Reset the current sustainability.


                // This loop sets up the sustainability values for the bottom.
                for (int off = 0; off <= absorb_offset; off++)
                {
                    for (int x = absorb_offset - off; x < size - (absorb_offset - off); x++)
                    {
                        vds[x + off * size + size * (size - absorb_offset - 1)] = cur;
                    }
                    cur -= dec;
                }


                cur = sustain;

                // This loop sets up the sustainability values for the left.
                for (int off = 0; off <= absorb_offset; off++)
                {
                    for (int x = absorb_offset - off; x < size - (absorb_offset - off); x++)
                    {
                        vds[x * size + (absorb_offset - off)] = cur;
                    }
                    cur -= dec;
                }

                cur = sustain;

                // This loop sets up the sustainability values for the right.
                for (int off = 0; off <= absorb_offset; off++)
                {
                    for (int x = absorb_offset - off; x < size - (absorb_offset - off); x++)
                    {
                        vds[x * size + off + size - absorb_offset - 1] = cur;
                    }
                    cur -= dec;
                }
            }
            else
            {
                // The only thing to do is to fill "vds" array with "sustain" in this case.
                for (int i = 0; i < vds.Length; i++)
                    vds[i] = sustain;
            }
        }


        /// <summary>
        /// Initializes the wave pool system.
        /// </summary>
        void SetPool()
        {
            vd = new float[size * size];

            vdv = new float[size * size];

            vda = new float[size * size];

            vd_static = new bool[size * size];

            vds = new float[size * size];

            SetSustain();
        }
    }
}
