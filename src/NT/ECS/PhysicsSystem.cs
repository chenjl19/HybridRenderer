using System;
using System.Collections.Generic;
//using BulletSharp;

namespace NT
{
    /*
    public abstract class PhysicsContext : IDisposable {
        public DynamicsWorld world {get; protected set;}
        public CollisionConfiguration collisionConfiguration {get; protected set;}
        public Dispatcher dispatcher {get; protected set;}
        public BroadphaseInterface broadphase {get; protected set;}
        public ConstraintSolver solver {get; protected set;}
        public List<CollisionShape> collisionShapes {get; private set;}

        public PhysicsContext() {
            collisionShapes = new List<CollisionShape>();
            Init();
        }

        public virtual void Init() {}

        public void Exit() {
            if(world != null) {
                for(int i = world.NumConstraints - 1; i >= 0; i--) {
                    TypedConstraint constraint = world.GetConstraint(i);
                    world.RemoveConstraint(constraint);
                    constraint.Dispose();
                }
                for(int i = world.NumCollisionObjects - 1; i >= 0; i--) {
                    CollisionObject obj = world.CollisionObjectArray[i];
                    RigidBody body = obj as RigidBody;
                    if(body != null) {
                        body.MotionState.Dispose();
                    }
                    world.RemoveCollisionObject(obj);
                    obj.Dispose();
                }
            }
            foreach(var shape in collisionShapes) {
                shape.Dispose();
            }
            collisionShapes.Clear();

            world.Dispose();
            broadphase.Dispose();
            dispatcher.Dispose();
            collisionConfiguration.Dispose();
        }

        public virtual void Dispose() {
            Exit();
        }

        public virtual int Update(float elapsedTime) {
            return world.StepSimulation(elapsedTime);
        }
    }

    public class Physics : PhysicsContext {
        public Physics() {
            collisionConfiguration = new DefaultCollisionConfiguration();
            dispatcher = new CollisionDispatcher(collisionConfiguration);
            broadphase = new DbvtBroadphase();
            world = new DiscreteDynamicsWorld(dispatcher, broadphase, null, collisionConfiguration);
        }

        public void Update() {
            
        }

        public override void Dispose() {}
    }
    */
}