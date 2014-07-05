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
namespace ChipmunkSharp
{

    public class cpBB
    {

        public int numBB = 0;
        public float l, b, r, t;

        public cpBB(float l1, float b1, float r1, float t1)
        {
            l = l1;
            b = b1;
            r = r1;
            t = t1;
            numBB++;
        }

        public cpBB(cpVect p, float r)
            : this(p.x - r, p.y - r, p.x + r, p.y + r)
        {

        }

        #region Static Methods

        #endregion



        /// ructs a cpBB for a circle with the given position and radius.
        public static cpBB NewForCircle(cpVect p, float r)
        {
            return new cpBB(p.x - r, p.y - r, p.x + r, p.y + r);
        }

        /// Returns true if @c a and @c b intersect.
        public bool Intersects(cpBB a)
        {
            return Intersects(this, a);
        }
        public static bool Intersects(cpBB a, cpBB b)
        {
            return (a.l <= b.r && b.l <= a.r && a.b <= b.t && b.b <= a.t);
        }

        /// Returns true if @c other lies completely within @c bb.
        public bool ContainsBB(cpBB other)
        {
            return ContainsBB(this, other);
        }
        public static bool ContainsBB(cpBB bb, cpBB other)
        {
            return (bb.l <= other.l && bb.r >= other.r && bb.b <= other.b && bb.t >= other.t);
        }

        /// Returns true if @c bb contains @c v.
        public bool ContainsVect(cpVect v)
        {
            return ContainsVect(this, v);
        }
        public static bool ContainsVect(cpBB bb, cpVect v)
        {
            return (bb.l <= v.x && bb.r >= v.x && bb.b <= v.y && bb.t >= v.y);
        }

        /// Returns a bounding box that holds both bounding boxes.
        public cpBB Merge(cpBB a)
        {
            return Merge(this, a);
        }
        public static cpBB Merge(cpBB a, cpBB b)
        {
            return new cpBB(
                cpEnvironment.cpfmin(a.l, b.l),
                cpEnvironment.cpfmin(a.b, b.b),
                cpEnvironment.cpfmax(a.r, b.r),
                cpEnvironment.cpfmax(a.t, b.t)
            );
        }

        /// Returns a bounding box that holds both @c bb and @c v.
        public cpBB Expand(cpVect v)
        {
            return Expand(this, v);
        }
        public static cpBB Expand(cpBB bb, cpVect v)
        {
            return new cpBB(
                cpEnvironment.cpfmin(bb.l, v.x),
                cpEnvironment.cpfmin(bb.b, v.y),
                cpEnvironment.cpfmax(bb.r, v.x),
                cpEnvironment.cpfmax(bb.t, v.y)
            );
        }

        /// Returns the center of a bounding box.
        public static cpVect Center(cpBB bb)
        {
            return cpVect.cpvlerp(cpVect.cpv(bb.l, bb.b), cpVect.cpv(bb.r, bb.t), 0.5f);
        }
        public cpVect Center()
        {
            return Center(this);
        }


        /// Returns the area of the bounding box.
        public static float Area(cpBB bb)
        {
            return (bb.r - bb.l) * (bb.t - bb.b);
        }
        public float Area()
        {
            return Area(this);
        }


        /// Merges @c a and @c b and returns the area of the merged bounding box.
        public static float MergedArea(cpBB a, cpBB b)
        {
            return (cpEnvironment.cpfmax(a.r, b.r) - cpEnvironment.cpfmin(a.l, b.l)) * (cpEnvironment.cpfmax(a.t, b.t) - cpEnvironment.cpfmin(a.b, b.b));
        }

        public float MergedArea(cpBB a)
        {
            return MergedArea(this, a);
        }


        /// Returns the fraction along the segment query the cpBB is hit. Returns INFINITY if it doesn't hit.
        public static float SegmentQuery(cpBB bb, cpVect a, cpVect b)
        {
            float idx = 1.0f / (b.x - a.x);
            float tx1 = (bb.l == a.x ? -cpEnvironment.Infinity : (bb.l - a.x) * idx);
            float tx2 = (bb.r == a.x ? cpEnvironment.Infinity : (bb.r - a.x) * idx);
            float txmin = cpEnvironment.cpfmin(tx1, tx2);
            float txmax = cpEnvironment.cpfmax(tx1, tx2);

            float idy = 1.0f / (b.y - a.y);
            float ty1 = (bb.b == a.y ? -cpEnvironment.Infinity : (bb.b - a.y) * idy);
            float ty2 = (bb.t == a.y ? cpEnvironment.Infinity : (bb.t - a.y) * idy);
            float tymin = cpEnvironment.cpfmin(ty1, ty2);
            float tymax = cpEnvironment.cpfmax(ty1, ty2);

            if (tymin <= txmax && txmin <= tymax)
            {
                float min = cpEnvironment.cpfmax(txmin, tymin);
                float max = cpEnvironment.cpfmin(txmax, tymax);

                if (0.0 <= max && min <= 1.0) return cpEnvironment.cpfmax(min, 0.0f);
            }

            return cpEnvironment.Infinity;
        }

        public float SegmentQuery(cpVect a, cpVect b)
        {
            return SegmentQuery(this, a, b);
        }


        /// Return true if the bounding box intersects the line segment with ends @c a and @c b.
        public static bool IntersectsSegment(cpBB bb, cpVect a, cpVect b)
        {
            return (SegmentQuery(bb, a, b) != cpEnvironment.Infinity);
        }
        public bool IntersectsSegment(cpVect a, cpVect b)
        {
            return IntersectsSegment(this, a, b);
        }


        /// Clamp a vector to a bounding box.
        public static cpVect ClampVect(cpBB bb, cpVect v)
        {
            return cpVect.cpv(cpEnvironment.cpfclamp(v.x, bb.l, bb.r), cpEnvironment.cpfclamp(v.y, bb.b, bb.t));
        }
        public cpVect ClampVect(cpVect v)
        {
            return ClampVect(this, v);
        }


        /// Wrap a vector to a bounding box.
        public static cpVect WrapVect(cpBB bb, cpVect v)
        {
            // wrap a vector to a bbox
            float ix = Math.Abs(bb.r - bb.l);
            float modx = (v.x - bb.l) % ix;
            float x = (modx > 0) ? modx : modx + ix;

            float iy = Math.Abs(bb.t - bb.b);
            float mody = (v.y - bb.b) % iy;
            float y = (mody > 0) ? mody : mody + iy;

            return new cpVect(x + bb.l, y + bb.b);
        }

        /// Constructs a cpBB for a circle with the given position and radius.
        public static cpBB cpBBNewForCircle(cpVect p, float r)
        {
            return cpBBNew(p.x - r, p.y - r, p.x + r, p.y + r);
        }

        /// Convenience constructor for cpBB structs.
        public static cpBB cpBBNew(float l, float b, float r, float t)
        {
            cpBB bb = new cpBB(l, b, r, t);
            return bb;
        }

    } ;

}

