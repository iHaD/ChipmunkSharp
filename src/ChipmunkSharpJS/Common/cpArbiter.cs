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
using System.Linq;
using System;
using System.Collections.Generic;

namespace ChipmunkSharp
{

	public class PointsDistance
	{
		public cpVect pointA, pointB;
		/// Penetration distance of the two shapes. Overlapping means it will be negative.
		/// This value is calculated as cpvdot(cpvsub(point2, point1), normal) and is ignored by cpArbiterSetContactPointSet().
		public float distance;
	}

	public class cpContactPointSet
	{

		public int Count { get { return points.Count; } }

		/// The normal of the collision.
		public cpVect normal;

		public List<PointsDistance> points = new List<PointsDistance>();
	}

	/// @private
	public class cpCollisionHandler
	{

		public static bool AlwaysCollide(cpArbiter arb, cpSpace space, object data) { return true; }
		public static void DoNothing(cpArbiter arb, cpSpace space, object data) { }

		public static cpCollisionHandler cpCollisionHandlerDoNothing
		{
			get
			{

				return new cpCollisionHandler(
				   cp.WILDCARD_COLLISION_TYPE,
	   cp.WILDCARD_COLLISION_TYPE,
	   AlwaysCollide,
	   AlwaysCollide,
	   DoNothing,
	   DoNothing, null
					);


			}
		}


		// Use the wildcard identifier since  the default handler should never match any type pair.
		public static cpCollisionHandler cpCollisionHandlerDefault
		{
			get
			{
				return new cpCollisionHandler(
	   cp.WILDCARD_COLLISION_TYPE,
	   cp.WILDCARD_COLLISION_TYPE,
	   DefaultBegin,
	   DefaultPreSolve,
	   DefaultPostSolve,
	   DefaultSeparate, null
	   );
			}
		}

		public string typeA;
		public string typeB;

		public Func<cpArbiter, cpSpace, object, bool> beginFunc;
		public Func<cpArbiter, cpSpace, object, bool> preSolveFunc;
		public Action<cpArbiter, cpSpace, object> postSolveFunc;
		public Action<cpArbiter, cpSpace, object> separateFunc;

		public object userData;

		public cpCollisionHandler()
		{
			this.typeA = cp.WILDCARD_COLLISION_TYPE;
			this.typeB = cp.WILDCARD_COLLISION_TYPE;

			beginFunc = DefaultBegin;
			preSolveFunc = DefaultPreSolve;
			postSolveFunc = DefaultPostSolve;
			separateFunc = DefaultSeparate;
		}


		public cpCollisionHandler(string a, string b,
			Func<cpArbiter, cpSpace, object, bool> begin,
			Func<cpArbiter, cpSpace, object, bool> preSolve,
			Action<cpArbiter, cpSpace, object> postSolve,
			Action<cpArbiter, cpSpace, object> separate,
			object userData

			)
		{
			this.typeA = a;
			this.typeB = b;

			this.beginFunc = begin;
			this.preSolveFunc = preSolve;
			this.postSolveFunc = postSolve;
			this.separateFunc = separate;
			this.userData = userData;
		}


		public cpCollisionHandler Clone()
		{
			cpCollisionHandler copy = new cpCollisionHandler();
			copy.typeA = typeA;
			copy.typeB = typeB;

			copy.beginFunc = beginFunc;
			copy.preSolveFunc = preSolveFunc;
			copy.postSolveFunc = postSolveFunc;
			copy.separateFunc = separateFunc;

			copy.userData = userData;


			return copy;
		}

		public static bool DefaultBegin(cpArbiter arb, cpSpace space, object o)
		{
			return true;
		}


		public static bool DefaultPreSolve(cpArbiter arb, cpSpace space, object o)
		{
			return true;
		}

		public static void DefaultPostSolve(cpArbiter arb, cpSpace space, object o)
		{
		}

		public static void DefaultSeparate(cpArbiter arb, cpSpace space, object o)
		{
		}


		// Equals function for collisionHandlers.
		public static bool SetEql(cpCollisionHandler check, cpCollisionHandler pair)
		{
			return (
				(check.typeA == pair.typeA && check.typeB == pair.typeB) ||
				(check.typeB == pair.typeA && check.typeA == pair.typeB)
			);
		}



	};

	/// @private
	public enum cpArbiterState
	{
		// Arbiter is active and its the first collision.
		FirstCollision = 1,
		// Arbiter is active and its not the first collision.
		Normal = 2,
		// Collision has been explicitly ignored.
		// Either by returning false from a begin collision handler or calling cpArbiterIgnore().
		Ignore = 3,
		// Collison is no longer active. A space will cache an arbiter for up to cpSpace.collisionPersistence more steps.
		Cached = 4,
		Invalidated = 5
	} ;

	/// A colliding pair of shapes.
	public class cpArbiter
	{
		public string Key { get { return cp.hashPair(a.hashid, b.hashid); } }

		public static int CP_MAX_CONTACTS_PER_ARBITER = 4;

		#region PUBLIC PROPS

		/// Calculated value to use for the elasticity coefficient.
		/// Override in a pre-solve collision handler for custom behavior.
		public float e;
		/// Calculated value to use for the friction coefficient.
		/// Override in a pre-solve collision handler for custom behavior.
		public float u;
		/// Calculated value to use for applying surface velocities.
		/// Override in a pre-solve collision handler for custom behavior.
		public cpVect surface_vr { get; set; }

		/// User definable data pointer.
		/// The value will persist for the pair of shapes until the separate() callback is called.
		/// NOTE: If you need to clean up this pointer, you should implement the separate() callback to do it.
		//public cpDataPointer data;

		public object data;

		#endregion

		#region PRIVATE PROPS

		public cpShape a, b;

		public cpBody body_a, body_b;

		public cpArbiterThread thread_a, thread_b;

		public List<cpContact> contacts;
		cpVect n;

		public cpCollisionHandler handler, handlerA, handlerB;
		public bool swapped;

		public int stamp;
		public cpArbiterState state;

		#endregion

		public int Count { get { return contacts.Count; } }

		/// A colliding pair of shapes.
		public cpArbiter(cpShape a, cpShape b)
		{
			this.handler = null;
			this.swapped = false;

			this.handlerA = null;
			this.handlerB = null;

			/// Calculated value to use for the elasticity coefficient.
			/// Override in a pre-solve collision handler for custom behavior.
			this.e = 0;
			/// Calculated value to use for the friction coefficient.
			/// Override in a pre-solve collision handler for custom behavior.
			this.u = 0;
			/// Calculated value to use for applying surface velocities.
			/// Override in a pre-solve collision handler for custom behavior.
			this.surface_vr = cpVect.Zero;

			this.a = a; this.body_a = a.body;
			this.b = b; this.body_b = b.body;

			this.thread_a = new cpArbiterThread(null, null);
			this.thread_b = new cpArbiterThread(null, null);

			this.contacts = null;

			this.stamp = 0;

			this.state = cpArbiterState.FirstCollision;
		}


		public void Unthread()
		{
			cp.UnthreadHelper(this, this.body_a);
			cp.UnthreadHelper(this, this.body_b);
		}


		/// Returns true if this is the first step a pair of objects started colliding.
		public bool IsFirstContact()
		{
			return state == cpArbiterState.FirstCollision;
		}

		public bool IsRemoval()
		{
			return state == cpArbiterState.Invalidated;
		}

		public int GetCount()
		{
			// Return 0 contacts if we are in a separate callback.
			return ((int)state < (int)cpArbiterState.Cached ? Count : 0);
		}

		/// Get the normal of the @c ith contact point.
		public cpVect GetNormal(int i)
		{
			return cpVect.cpvmult(n, this.swapped ? -1.0f : 1.0f);
		}

		public cpVect GetPointA(int i)
		{
			cp.assertHard(0 <= i && i < GetCount(), "Index error: The specified contact index is invalid for this arbiter");
			return cpVect.cpvadd(this.body_a.p, this.contacts[i].r1);
		}

		public cpVect GetPointB(int i)
		{
			cp.assertHard(0 <= i && i < GetCount(), "Index error: The specified contact index is invalid for this arbiter");
			return cpVect.cpvadd(this.body_b.p, this.contacts[i].r2);
		}

		/// Get the depth of the @c ith contact point.
		public float GetDepth(int i)
		{
			// return this.contacts[i].dist;
			cp.assertHard(0 <= i && i < GetCount(), "Index error: The specified contact index is invalid for this arbiter");

			cpContact con = contacts[i];
			return cpVect.cpvdot(cpVect.cpvadd(cpVect.cpvsub(con.r2, con.r1), cpVect.cpvsub(this.body_b.p, this.body_a.p)), this.n);
		}


		/// Return a contact set from an arbiter.
		public cpContactPointSet GetContactPointSet()
		{
			cpContactPointSet set = new cpContactPointSet();
			var count = GetCount();

			bool swapped = this.swapped;

			set.normal = (swapped ? cpVect.cpvneg(this.n) : this.n);

			PointsDistance tmp;
			for (int i = 0; i < count; i++)
			{
				// Contact points are relative to body CoGs;
				cpVect p1 = cpVect.cpvadd(this.body_a.p, this.contacts[i].r1);
				cpVect p2 = cpVect.cpvadd(this.body_b.p, this.contacts[i].r2);

				tmp = new PointsDistance();
				tmp.pointA = (swapped ? p2 : p1);
				tmp.pointB = (swapped ? p1 : p2);
				tmp.distance = cpVect.cpvdot(cpVect.cpvsub(p2, p1), n);
				set.points.Add(tmp);
			}
			return set;
		}

		public void SetContactPointSet(cpContactPointSet set)
		{
			int count = set.Count;

			cp.assertHard(count == Count, "The number of contact points cannot be changed.");

			this.n = (this.swapped ? cpVect.cpvneg(set.normal) : set.normal);

			for (int i = 0; i < count; i++)
			{
				// Convert back to CoG relative offsets.
				cpVect p1 = set.points[i].pointA;
				cpVect p2 = set.points[i].pointB;

				this.contacts[i].r1 = cpVect.cpvsub(swapped ? p2 : p1, this.body_a.p);
				this.contacts[i].r2 = cpVect.cpvsub(swapped ? p1 : p2, this.body_b.p);
			}

		}

		/// Calculate the total impulse that was applied by this arbiter.
		/// This function should only be called from a post-solve, post-step or cpBodyEachArbiter callback.

		public cpVect TotalImpulse()
		{

			cpVect sum = cpVect.Zero;

			for (int i = 0, count = GetCount(); i < count; i++)
			{
				cpContact con = contacts[i];
				// sum.Add(con.n.Multiply(con.jnAcc));
				sum = sum.Add(con.n.Multiply(con.jnAcc));
			}

			return this.swapped ? sum : sum.Neg();
		}

		/// Calculate the amount of energy lost in a collision including static, but not dynamic friction.
		/// This function should only be called from a post-solve, post-step or cpBodyEachArbiter callback.
		public float TotalKE()
		{

			float eCoef = (1 - this.e) / (1 + this.e);
			float sum = 0.0f;

			//cpContact contacts = contacts;

			for (int i = 0, count = contacts.Count; i < count; i++)
			{

				cpContact con = contacts[i];
				float jnAcc = con.jnAcc;
				float jtAcc = con.jtAcc;

				sum += eCoef * jnAcc * jnAcc / con.nMass + jtAcc * jtAcc / con.tMass;
			}

			return sum;
		}



		/// Causes a collision pair to be ignored as if you returned false from a begin callback.
		/// If called from a pre-step callback, you will still need to return false
		/// if you want it to be ignored in the current step.
		public bool Ignore()
		{
			this.state = cpArbiterState.Ignore;
			return false;
		}

		public float GetRestitution()
		{
			return this.e;
		}

		public void SetRestitution(float restitution)
		{
			this.e = restitution;
		}

		public float GetFriction()
		{
			return this.u;
		}

		public void SetFriction(float friction)
		{
			this.u = friction;
		}


		public cpVect GetSurfaceVelocity()
		{
			return cpVect.cpvmult(this.surface_vr, this.swapped ? -1.0f : 1.0f);
		}


		public void SetSurfaceVelocity(cpVect vr)
		{
			this.surface_vr = cpVect.cpvmult(vr, this.swapped ? -1.0f : 1.0f);
		}

		public object GetUserData()
		{
			return this.data;
		}

		public void SetUserData(object userData)
		{
			this.data = userData;
		}


		/// Return the colliding shapes involved for this arbiter.
		/// The order of their cpSpace.collision_type values will match
		/// the order set when the collision handler was registered.
		public void GetShapes(out cpShape a, out cpShape b)
		{
			if (swapped)
			{
				a = this.b; b = this.a;
			}
			else
				a = this.a; b = this.b;
		}

		public void GetBodies(out cpBody a, out cpBody b)
		{
			cpShape shape_a, shape_b;
			GetShapes(out shape_a, out shape_b);
			a = shape_a.body;
			b = shape_b.body;
		}


		public bool CallWildcardBeginA(cpSpace space)
		{
			cpCollisionHandler handler = this.handlerA;
			return handler.beginFunc(this, space, handler.userData);
		}

		public bool CallWildcardBeginB(cpSpace space)
		{
			cpCollisionHandler handler = this.handlerB;
			this.swapped = !this.swapped;
			bool retval = handler.beginFunc(this, space, handler.userData);
			this.swapped = !this.swapped;
			return retval;
		}

		public bool CallWildcardPreSolveA(cpSpace space)
		{
			cpCollisionHandler handler = this.handlerA;
			return handler.preSolveFunc(this, space, handler.userData);
		}


		public bool CallWildcardPreSolveB(cpSpace space)
		{
			cpCollisionHandler handler = this.handlerB;
			this.swapped = !this.swapped;
			bool retval = handler.preSolveFunc(this, space, handler.userData);
			this.swapped = !this.swapped;
			return retval;
		}

		public void CallWildcardPostSolveA(cpSpace space)
		{
			cpCollisionHandler handler = this.handlerA;
			handler.postSolveFunc(this, space, handler.userData);
		}


		public void CallWildcardPostSolveB(cpSpace space)
		{
			cpCollisionHandler handler = this.handlerB;
			this.swapped = !this.swapped;
			handler.postSolveFunc(this, space, handler.userData);
			this.swapped = !this.swapped;

		}

		public void CallWildcardSeparateA(cpSpace space)
		{
			cpCollisionHandler handler = this.handlerA;
			handler.separateFunc(this, space, handler.userData);
		}


		public void CallWildcardSeparateB(cpSpace space)
		{
			cpCollisionHandler handler = this.handlerB;
			this.swapped = !this.swapped;
			handler.separateFunc(this, space, handler.userData);
			this.swapped = !this.swapped;

		}

		public void Update(cpCollisionInfo info, cpSpace space)
		{

			cpShape a = info.a, b = info.b;

			// For collisions between two similar primitive types, the order could have been swapped since the last frame.
			this.a = a; this.body_a = a.body;
			this.b = b; this.body_b = b.body;

			// Iterate over the possible pairs to look for hash value matches.
			for (int i = 0; i < info.Count; i++)
			{
				cpContact con = info.arr[i];

				// r1 and r2 store absolute offsets at init time.
				// Need to convert them to relative offsets.
				con.r1 = cpVect.cpvsub(con.r1, a.body.p);
				con.r2 = cpVect.cpvsub(con.r2, b.body.p);

				// Cached impulses are not zeroed at init time.
				con.jnAcc = con.jtAcc = 0.0f;

				for (int j = 0; j < this.Count; j++)
				{
					cpContact old = this.contacts[j];

					// This could trigger false positives, but is fairly unlikely nor serious if it does.
					if (con.hash == old.hash)
					{
						// Copy the persistant contact information.
						con.jnAcc = old.jnAcc;
						con.jtAcc = old.jtAcc;
					}
				}
			}
			//TODO: revise
			this.contacts = info.arr.ToList();
			//this.count = info.count;
			this.n = info.n;

			this.e = a.e * b.e;
			this.u = a.u * b.u;

			cpVect surface_vr = cpVect.cpvsub(b.surfaceV, a.surfaceV);
			this.surface_vr = cpVect.cpvsub(surface_vr, cpVect.cpvmult(info.n, cpVect.cpvdot(surface_vr, info.n)));

			string typeA = info.a.type, typeB = info.b.type;
			cpCollisionHandler defaultHandler = space.defaultHandler;
			cpCollisionHandler handler = this.handler = space.lookupHandler(typeA, typeB, defaultHandler);

			// Check if the types match, but don't swap for a default handler which use the wildcard for type A.
			bool swapped = this.swapped = (typeA != handler.typeA && handler.typeA != cp.WILDCARD_COLLISION_TYPE);

			if (handler != defaultHandler || space.usesWildcards)
			{
				// The order of the main handler swaps the wildcard handlers too. Uffda.
				this.handlerA = space.lookupHandler(swapped ? typeB : typeA, cp.WILDCARD_COLLISION_TYPE, cpCollisionHandler.cpCollisionHandlerDoNothing);
				this.handlerB = space.lookupHandler(swapped ? typeA : typeB, cp.WILDCARD_COLLISION_TYPE, cpCollisionHandler.cpCollisionHandlerDoNothing);
			}

			// mark it as new if it's been cached
			if (this.state == cpArbiterState.Cached)
				this.state = cpArbiterState.FirstCollision;

		}

		public void PreStep(float dt, float slop, float bias)
		{
			var a = this.body_a;
			var b = this.body_b;

			cpVect body_delta = cpVect.cpvsub(b.p, a.p);


			for (var i = 0; i < Count; i++)
			{
				var con = this.contacts[i];

				// Calculate the offsets.
				con.r1 = cpVect.cpvsub(con.p, a.GetPosition());
				con.r2 = cpVect.cpvsub(con.p, b.GetPosition());

				// Calculate the mass normal and mass tangent.
				con.nMass = 1 / cp.k_scalar(a, b, con.r1, con.r2, con.n);
				con.tMass = 1 / cp.k_scalar(a, b, con.r1, con.r2, cpVect.cpvperp(con.n));

				// Calculate the target bias velocity.
				con.bias = -bias * Math.Min(0, con.dist + slop) / dt;
				con.jBias = 0;

				// Calculate the target bounce velocity.
				con.bounce = cp.normal_relative_velocity(a, b, con.r1, con.r2, con.n) * this.e;
			}
		}

		public void ThreadForBody(cpBody body, out cpArbiterThread thread)
		{
			//TODO: THIS NEEDS RETURN THE ORIGINAL MEMORY REFERENCE IN ARBITER
			if (this.body_a == body)
				thread = thread_a;
			else
				thread = thread_b;
		}


		[Obsolete("Contact points will transform in cpVect")]
		public void ApplyCachedImpulse(float dt_coef)
		{
			if (this.IsFirstContact()) return;

			var a = this.body_a;
			var b = this.body_b;

			for (var i = 0; i < this.contacts.Count; i++)
			{
				var con = this.contacts[i];
				var nx = con.n.x;
				var ny = con.n.y;
				var jx = nx * con.jnAcc - ny * con.jtAcc;
				var jy = nx * con.jtAcc + ny * con.jnAcc;
				cp.apply_impulses(a, b, con.r1, con.r2, jx * dt_coef, jy * dt_coef);
			}
		}


		[Obsolete("Contact points will transform in cpVect")]
		public void ApplyImpulse(float dt)
		{

			cp.numApplyImpulse++;
			//if (!this.contacts) { throw new Error('contacts is undefined'); }
			var a = this.body_a;
			var b = this.body_b;
			var surface_vr = this.surface_vr;
			var friction = this.u;

			for (var i = 0; i < this.contacts.Count; i++)
			{
				cp.numApplyContact++;

				cpContact con = this.contacts[i];
				var nMass = con.nMass;
				//var n = con.n;
				var r1 = con.r1;
				var r2 = con.r2;

				//var vr = relative_velocity(a, b, r1, r2);
				var vrx = b.v.x - r2.y * b.w - (a.v.x - r1.y * a.w);
				var vry = b.v.y + r2.x * b.w - (a.v.y + r1.x * a.w);

				//var vb1 = vadd(vmult(vperp(r1), a.w_bias), a.v_bias);
				//var vb2 = vadd(vmult(vperp(r2), b.w_bias), b.v_bias);
				//var vbn = vdot(vsub(vb2, vb1), n);

				var vbn = n.x * (b.v_bias.x - r2.y * b.w_bias - a.v_bias.x + r1.y * a.w_bias) +
						n.y * (r2.x * b.w_bias + b.v_bias.y - r1.x * a.w_bias - a.v_bias.y);

				var vrn = cpVect.cpvdot2(vrx, vry, n.x, n.y);
				//var vrt = vdot(vadd(vr, surface_vr), vperp(n));
				var vrt = cpVect.cpvdot2(vrx + surface_vr.x, vry + surface_vr.y, -n.y, n.x);

				var jbn = (con.bias - vbn) * nMass;
				var jbnOld = con.jBias;
				con.jBias = Math.Max(jbnOld + jbn, 0);

				var jn = -(con.bounce + vrn) * nMass;
				var jnOld = con.jnAcc;
				con.jnAcc = Math.Max(jnOld + jn, 0);

				var jtMax = friction * con.jnAcc;
				var jt = -vrt * con.tMass;
				var jtOld = con.jtAcc;
				con.jtAcc = cp.cpclamp(jtOld + jt, -jtMax, jtMax);

				//apply_bias_impulses(a, b, r1, r2, vmult(n, con.jBias - jbnOld));
				var bias_x = n.x * (con.jBias - jbnOld);
				var bias_y = n.y * (con.jBias - jbnOld);
				cp.apply_bias_impulse(a, -bias_x, -bias_y, r1);
				cp.apply_bias_impulse(b, bias_x, bias_y, r2);

				//apply_impulses(a, b, r1, r2, vrotate(n, new Vect(con.jnAcc - jnOld, con.jtAcc - jtOld)));
				var rot_x = con.jnAcc - jnOld;
				var rot_y = con.jtAcc - jtOld;

				// Inlining apply_impulses decreases speed for some reason :/
				cp.apply_impulses(a, b, r1, r2, n.x * rot_x - n.y * rot_y, n.x * rot_y + n.y * rot_x);
			}
		}

		// Equal function for arbiterSet.
		public static bool SetEql(cpShape[] shapes, cpArbiter arb)
		{
			cpShape a = shapes[0];
			cpShape b = shapes[1];
			return ((a == arb.a && b == arb.b) || (b == arb.a && a == arb.b));
		}


		///////////////////////////////////////////////////


		#region OBSOLETE

		/// Calculate the total impulse including the friction that was applied by this arbiter.
		/// This function should only be called from a post-solve, post-step or cpBodyEachArbiter callback.
		[Obsolete("OBSOLETE: JS VERSION")]
		public cpVect TotalImpulseWithFriction()
		{
			var sum = cpVect.Zero;

			for (int i = 0, count = contacts.Count; i < count; i++)
			{
				var con = contacts[i];
				sum = sum.Add(new cpVect(con.jnAcc, con.jtAcc).Rotate(con.n));
			}

			return this.swapped ? sum : sum.Neg();
		}


		/// Get the position of the @c ith contact point.
		[Obsolete("OBSOLETE: JS VERSION")]
		public cpVect GetPoint(int i)
		{
			cp.assertHard(0 <= i && i < contacts.Count, "Index error: The specified contact index is invalid for this arbiter");
			return contacts[i].p;
			// return contacts[i].point;
		}


		[Obsolete("OBSOLETE: JS VERSION")]
		public void CallSeparate(cpSpace space)
		{
			// The handler needs to be looked up again as the handler cached on the arbiter may have been deleted since the last step.
			var handler = space.lookupHandler(this.a.type, this.b.type, space.defaultHandler);
			handler.separateFunc(this, space, null);
		}

		[Obsolete("OBSOLETE: JS VERSION")]
		public cpArbiter Next(cpBody body)
		{
			return (this.body_a == body ? this.thread_a.next : this.thread_b.next);
		}


		[Obsolete("OBSOLETE: JS VERSION")]
		public void Update(List<cpContact> contacts, cpCollisionHandler handler, cpShape a, cpShape b)
		{
			//throw new NotImplementedException();

			if (this.contacts != null)
			{

				// Iterate over the possible pairs to look for hash value matches.
				for (int i = 0; i < this.contacts.Count; i++)
				{
					cpContact old = this.contacts[i];

					for (int j = 0; j < contacts.Count; j++)
					{
						// ContactPoint new_contact = contacts[j];

						// This could trigger false positives, but is fairly unlikely nor serious if it does.
						if (contacts[j].hash == old.hash)
						{
							// Copy the persistant contact information.
							contacts[j].jnAcc = old.jnAcc;
							contacts[j].jtAcc = old.jtAcc;
						}
					}
				}

			}

			this.contacts = contacts;

			this.handler = handler;
			this.swapped = (a.type != handler.typeA);

			this.e = a.e * b.e;
			this.u = a.u * b.u;

			this.surface_vr = cpVect.cpvsub(a.surfaceV, b.surfaceV);

			// For collisions between two similar primitive types, the order could have been swapped.
			this.a = a; this.body_a = a.body;
			this.b = b; this.body_b = b.body;

			// mark it as new if it's been cached
			if (this.state == cpArbiterState.Cached) this.state = cpArbiterState.FirstCollision;

		}

		#endregion


	};

	public struct cpArbiterThread
	{
		public cpArbiter next, prev;

		public cpArbiterThread(cpArbiter next, cpArbiter prev)
		{
			this.next = next;
			this.prev = prev;
		}


	};

}


