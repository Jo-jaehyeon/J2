using System;
using System.Collections;
using System.Collections.Generic;

namespace J2.Creatures
{
    public class Player : Creature
    {
        public override void Tick(float deltaTime)
        {
            // Move checks the current position against Destination every tick.
            Move(deltaTime);
        }
    }
}
