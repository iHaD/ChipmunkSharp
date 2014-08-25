/* Copyright (c) 2007 Scott Lembcke ported by Jose Medrano (@netonjm)
  
  Permission is hereby granted, free of charge, to any person obtaining a copy
  of this software and associated documentation files (the "Software"), to deal
  in the Software without restriction, including without limitation the rights
  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
  copies of the Software, and to permit persons to whom the Software is
  furnished to do so, subject to the following conditions:
  
  The above copyright notice and this permission notice shall be included in
  all copies or substantial portions of the Software.
  
  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
  SOFTWARE.
 */
using System;
namespace ChipmunkSharp.Constraints
{

    public class cpPivotJoint : cpConstraint
    {

        #region PUBLIC PROPS

        public cpVect anchr1 { get; set; }

        public cpVect anchr2 { get; set; }

        public cpVect r2 { get; set; }

        public cpVect r1 { get; set; }

        public cpVect k1 { get; set; }

        public cpVect k2 { get; set; }

        public cpVect jAcc { get; set; }

        public float jMaxLen { get; set; }

        public cpVect bias { get; set; }

        #endregion

        public cpPivotJoint(cpBody a, cpBody b, cpVect pivot)
            : this(a, b, (a != null ? a.World2Local(pivot) : pivot), (b != null ? b.World2Local(pivot) : pivot))
        {

        }
        public cpPivotJoint(cpBody a, cpBody b, cpVect anchr1, cpVect anchr2)
            : base(a, b)
        {


            // if(typeof(cpVect) ==anchr2   ) {
            if (anchr2 != null)
            {
                var pivot = anchr1;

                anchr1 = (a != null ? a.World2Local(pivot) : pivot);
                anchr2 = (b != null ? b.World2Local(pivot) : pivot);
            }

            this.anchr1 = anchr1;
            this.anchr2 = anchr2;

            this.r1 = this.r2 = cpVect.ZERO;

            this.k1 = cpVect.ZERO;
            this.k2 = cpVect.ZERO;

            this.jAcc = cpVect.ZERO;

            this.jMaxLen = 0.0f;
            this.bias = cpVect.ZERO;
        }

        public override void PreStep(float dt)
        {

            this.r1 = anchr1.Rotate(a.Rotation);// cpvrotate();
            this.r2 = anchr2.Rotate(b.Rotation); // cpvrotate(this.anchr2, b.rot);

            // Calculate mass tensor. Result is stored into this.k1 & this.k2.
            cpEnvironment.k_tensor(a, b, this.r1, this.r2, this.k1, this.k2);

            // compute max impulse
            this.jMaxLen = this.maxForce * dt;

            // calculate bias velocity
            var delta = b.Position.Add(r2).Sub(a.Position.Add(r1));  //cpvsub(cpvadd(b.p, this.r2), cpvadd(a.p, this.r1));
            this.bias = delta.Multiply(-cpEnvironment.bias_coef(errorBias, dt) / dt).Clamp(maxBias);  //cpvclamp(cpvmult(delta, -Util.bias_coef(this.errorBias, dt) / dt), this.maxBias);
        }

        public override void ApplyCachedImpulse(float dt_coef)
        {
            cpEnvironment.apply_impulses(this.a, this.b, this.r1, this.r2, this.jAcc.x * dt_coef, this.jAcc.y * dt_coef);
        }

        public override void ApplyImpulse(float dt)
        {

            // compute relative velocity
            var vr = cpEnvironment.relative_velocity(a, b, r1, r2);

            // compute normal impulse
            var j = cpVect.mult_k(bias.Sub(vr), k1, k2);   // Util.mult_k(cpvsub(this.bias, vr), this.k1, this.k2);
            var jOld = this.jAcc;
            this.jAcc = jAcc.Add(j).Clamp(jMaxLen);  // cpEnvironment.cpvclamp(cpvadd(this.jAcc, j), this.jMaxLen);

            // apply impulse
            cpEnvironment.apply_impulses(a, b, this.r1, this.r2, this.jAcc.x - jOld.x, this.jAcc.y - jOld.y);
        }

        public override float GetImpulse()
        {
            return jAcc.Length;  //cpvlength(this.jAcc);
        }




    }


}