
using System;
using System.IO;
using System.Collections.Generic;
using SharpDX;

namespace NT
{
    /*
    public class RenderModelComponent : TransfromComponent {
        public RenderModel model {get; protected set;}
        public Material material {get; protected set;}
        public Material[] materials {get; protected set;}
        public Action GetDynamicSurfaces {get; protected set;}
        public bool moveable {get; protected set;}
        public int numJoints {get; protected set;}
        public Matrix[] joints;

        public RenderModelComponent(GameObject inOwner) : base(inOwner) {
            moveable = true;
        } 

        protected void OnUpdateModel() {
            if(registered) {
                gameRenderWorld.UpdateModel(entityHandle);
            }
        }       

        public void SetMoveable(bool v) {
            moveable = v;
            OnUpdateModel();
        }

        public void SetModel(RenderModel inModel) {
            model = inModel;
            if(model != null) {
                numJoints = model.numJoints;
            }
            OnUpdateModel();
        }

        public void SetMaterial(int surfaceIndex, Material material) {
            if(materials != null && surfaceIndex >= 0 && surfaceIndex < materials.Length) {
                materials[surfaceIndex] = material;
            }
            OnUpdateModel();
        }

        public void SetMaterial(Material inMaterial) {
            if(materials != null && materials.Length > 0) {
                materials[0] = inMaterial;
            } else {
                material = inMaterial;
                materials = new Material[1];
                materials[0] = inMaterial;
            }
            OnUpdateModel();
        }

        public void SetMaterials(Material[] inMaterials) {
            materials = inMaterials;
            OnUpdateModel();
        }

        public void SetGetDynamicSurfacesCallback(Action callback) {
            GetDynamicSurfaces = callback;
            OnUpdateModel();
        }

        protected override void OnUpdateTransform() {
            if(registered) {
                gameRenderWorld.UpdateModelTransform(entityHandle);
            }
        } 

        protected override void OnRegister() {
            base.OnRegister();
            entityHandle = gameRenderWorld.AddModel(this);
        }
    }

    public class GameObject {
        public const int MaxComponents = 8;

        public readonly Game gameLocal;
        public readonly int id;
        public RenderWorld gameRenderWorld {get {return gameLocal.renderWorld;}}
        public string name {get; protected set;}
        Component[] ownedComponents;
        int numOwnedComponents;

        public GameObject() {}

        public GameObject(Game myGame, string myName, int myID) : base() {
            name = myName;
            if(string.IsNullOrWhiteSpace(name)) {
                name = this.GetType().ToString() + myID;
            }
            id = myID;
            gameLocal = myGame;
            ownedComponents = new Component[MaxComponents];
        }

        public bool HasComponent(Component component) {
            for(int i = 0; i < numOwnedComponents; i++) {
                if(ownedComponents[i] == component) {
                    return true;
                }
            }
            return false;
        }

        public bool AddOwnedComponent(Component component) {
            if(component.owner != this) {
                return false;
            }
            if(HasComponent(component)) {
                return false;
            }
            if(numOwnedComponents + 1 >= MaxComponents) {
                return false;
            }

            ownedComponents[numOwnedComponents] = component;
            numOwnedComponents++;
            return true;
        }
    }

    public class Game {
        readonly List<GameObject> spawnedGameObjects;  
        public readonly RenderWorld renderWorld;      

        public T SpawnGameObject<T>(string name) where T: GameObject, new() {
            int id = spawnedGameObjects.Count;
            T gameObject = Activator.CreateInstance(typeof(T), this, name, id) as T;
            spawnedGameObjects.Add(gameObject);
            return gameObject;
        }

        public Game() {
            renderWorld = new RenderWorld();
            spawnedGameObjects = new List<GameObject>();
        }
    }
    */
}