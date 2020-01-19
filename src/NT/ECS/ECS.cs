using System;
using System.IO;
using System.Collections.Generic;
using SharpDX;
using SharpDX.Mathematics;
using SharpDX.Direct3D;
using SharpDX.D3DCompiler;
using D3D11 = SharpDX.Direct3D11;
using System.Reflection;

namespace NT
{
    public struct Entity {

        public Guid id {get; private set;}

        public static Entity Create() {
            Entity entity = new Entity();
            entity.id = Guid.NewGuid();
            return entity;
        }

        public static Entity Invalid() {
            Entity entity = new Entity();
            entity.id = Guid.Empty;
            return entity;
        }

        public override bool Equals(object obj)
        {
            //
            // See the full list of guidelines at
            //   http://go.microsoft.com/fwlink/?LinkID=85237
            // and also the guidance for operator== at
            //   http://go.microsoft.com/fwlink/?LinkId=85238
            //
            if (obj == null || GetType() != obj.GetType()) {
                return false;
            }
            
            Entity e = (Entity)obj;
            return e.id == id;
        }
        
        // override object.GetHashCode
        public override int GetHashCode() {
            return base.GetHashCode();
        }

        public static bool operator==(Entity a, Entity b) {
            return a.id == b.id;
        }

        public static bool operator!=(Entity a, Entity b) {
            return a.id != b.id;
        }

        public bool IsValid() {
            return id != Guid.Empty;
        }
    }

    public sealed class ComponentManager<T> where T: new() {
        List<T> components;
        List<Entity> entities;
        Dictionary<Entity, int> lookup;

        public ComponentManager() {
            components = new List<T>();
            entities = new List<Entity>();
            lookup = new Dictionary<Entity, int>();
        }

        public ComponentManager(int num) {
            components = new List<T>(num);
            entities = new List<Entity>(num);
            lookup = new Dictionary<Entity, int>(num);
        }

        public void Clear() {
            components.Clear();
            entities.Clear();
            lookup.Clear();
        }

        public int Num() {
            return components.Count;
        }

        public T this[int index] {
            get {
                return components[index];
            }
        }

        public Entity GetEntity(int index) {
            return entities[index];
        }

        public T Create(Entity entity) {
            if(!entity.IsValid()) {
                throw new InvalidOperationException("Invalid Entity is not allowed!");
            }
            if(lookup.TryGetValue(entity, out _)) {
                throw new InvalidOperationException("Only one of this component type per entity is allowed!");
            }
            if(entities.Count != components.Count || lookup.Count != components.Count) {
                throw new InvalidOperationException("Entity count must always be the same as the number of components!");
            }

            T component = new T();
            lookup.Add(entity, components.Count);
            components.Add(component);
            entities.Add(entity);
            return component;
        }

        public bool Remove(Entity entity) {
            if(lookup.TryGetValue(entity, out int index)) {
                if(index < components.Count - 1) {
                    components[index] = components[components.Count - 1];
                    entities[index] = entities[entities.Count - 1];
                    lookup[entities[index]] = index;
                }
                components.RemoveAt(components.Count - 1);
                entities.RemoveAt(entities.Count - 1);
                lookup.Remove(entity);
                return true;
            }
            return false;
        }

        public void RemoveKeepSorted(Entity entity) {
            if(lookup.TryGetValue(entity, out int index)) {
                entity = entities[index];
                if(index < components.Count - 1) {
                    for(int i = index + 1; i < components.Count; i++) {
                        components[i - 1] = components[i];
                    }
                    for(int i = index + 1; i < entities.Count; i++) {
                        entities[i - 1] = entities[i];
                        lookup[entities[i - 1]] = i - 1;
                    }
                }
                
                components.RemoveAt(components.Count - 1);
                entities.RemoveAt(entities.Count - 1);
                lookup.Remove(entity);
            }
        }

        public bool Contains(Entity entity) {
            return lookup.ContainsKey(entity);
        }

        public void MoveItem(int from, int to) {
            if(from >= Num() || to >= Num() || from == to) {
                return;
            }

            var component = components[from];
            Entity entity = entities[from];

            int direction = from < to ? 1 : -1;
            for(int i = from; i != to; i += direction) {
                int next = i + direction;
                components[i] = components[next];
                entities[i] = entities[next];
                lookup[entities[i]] = i;
            }

            components[to] = component;
            entities[to] = entity;
            lookup[entity] = to;
        }

        public T GetComponent(Entity entity) {
            if(lookup.TryGetValue(entity, out int index)) {
                return components[index];
            }
            return default(T);
        }
    }
}