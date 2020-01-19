using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Reflection;
using SharpDX;
using SharpDX.Mathematics;
using ImGuiNET;

namespace NT
{
    public class UIHierarchy<T> where T: TreeNode {
        public Hierarchy<T> selectedNode {get; private set;}

        public void ClearSelected() {
            if(selectedNode != null) {
                selectedNode.RemoveFromHierarchy();
                selectedNode = null;
            }
        }

        public void DrawNode_r(Hierarchy<T> node) {
            node.owner.nodeFlags |= node.GetChildNode() == null ? ImGuiTreeNodeFlags.Leaf : ImGuiTreeNodeFlags.None;
            if(ImGui.TreeNodeEx(node.owner.name, node.owner.nodeFlags)) {
                if(ImGui.IsItemClicked()) {
                    if(selectedNode != null) {
                        selectedNode.owner.nodeFlags &= ~ImGuiTreeNodeFlags.Selected;
                    }
                    selectedNode = node;
                    selectedNode.owner.nodeFlags |= ImGuiTreeNodeFlags.Selected;
                }
                if(!node.owner.nodeFlags.HasFlag(ImGuiTreeNodeFlags.Leaf)) {
                    node = node.GetChildNode();
                    while(node != null) {
                        DrawNode_r(node);
                        node = node.GetNextSiblingNode();
                    }
                }
                ImGui.TreePop();
            } 
        }
    }

    internal class SceneHierarchyView : HierarchyView {
        List<GameObjectNode> nodes;

        public SceneHierarchyView(Editor editor) : base(editor, "Scene") {
            nodes = new List<GameObjectNode>();
        }

        public void AddEntityNode(Entity entity, GameObjectType type) {
            if(entity.IsValid()) {
                nodes.Add(new GameObjectNode(entity, type, this));
            }
        }

        public void AddEntityNode(GameObjectType type, int sceneID, string name) {
            nodes.Add(new GameObjectNode(type, sceneID, name, this));
        }

        public override void Update() {
            for(int i = 0; i < nodes.Count; i++) {
                nodes[i].OnHierarchyGUI();
            }
        }   
    }

        class ParticleSystemTest {
            TransformComponent transform;

            ParticleSystem particleSystem;

            public ParticleStage stage;
            public ParticleUpdateInfo stageUpdateInfo;

            public ParticleSystemTest() {
                transform = new TransformComponent();
                transform.UpdateTransform();
/*
                stage.maxParticles = 16;
                stage.cycleMsec = 5000;
                stage.particleLife = 1f;
                stage.shape = ParticleStageShape.Rectangle;
                stage.shapeParms = new Vector4(0.1f, 1f, 0.2f, 0f);
                stage.direction = ParticleStageDirection.Cone;
                stage.directionParms = new Vector4(30f, 0f, 0f, 0f);
                stage.speedRange = new Vector2(0.1f, 2f);
                stage.sizeRange = new Vector4(0.01f, 0.01f, 1f, 1f);

                particleSystem = Common.declManager.CreateNewDecal(DeclType.ParticleSystem, "testParticleSystem", Path.Combine(FileSystem.assetBasePath, "decls/particles/test.prt")) as ParticleSystem;
                particleSystem.parsedStages = new ParticleStage[1];
                particleSystem.parsedStages[0] = stage;
                particleSystem.Save(null);
*/
                particleSystem = Common.declManager.FindByType("testParticleSystem", DeclType.ParticleSystem) as ParticleSystem;
                stage = particleSystem.parsedStages[0];
            }

            const int MaxRand = 0x7fff;

            void OnGUI() {
                ImGui.DragInt("MaxParticles", ref stage.maxParticles, 1, 0, 128);

                ImGuiVector v = stage.offset;
                ImGui.DragFloat3("Offset", ref v.vec3, 0.01f);
                stage.offset = v.GetVector3();

                ImGui.DragFloat("ParticleLife", ref stage.particleLife, 0.1f, 0f, 1000f);
                ImGui.DragFloat("SpawnBunching", ref stage.spawnBunching, 0.1f, 0f, 1f);
                if(ImGui.RadioButton("RandomDistribution", stage.randomDistribution)) {
                    stage.randomDistribution ^= true;
                }

                v = stage.shapeParms;
                if(stage.shape == ParticleStageShape.Rectangle) {
                    ImGui.DragFloat4("Rectangle", ref v.vec4, 0.01f);
                }
                stage.shapeParms = v.GetVector4();

                if(stage.direction == ParticleStageDirection.Cone) {
                    ImGui.DragFloat("ConeAngle", ref stage.directionParms.X, 0.01f);
                }

                v = stage.sizeRange;
                ImGui.DragFloat4("SizeRange", ref v.vec4, 0.1f, 0f);
                stage.sizeRange = v.GetVector4();

                v = stage.speedRange;
                ImGui.DragFloat2("SpeedRange", ref v.vec2, 0.1f, 0f);
                stage.speedRange = v.GetVector2();

                v = stage.initialColor;
                ImGui.ColorEdit4("Initial Color", ref v.vec4, ImGuiColorEditFlags.Float | ImGuiColorEditFlags.HDR);
                stage.initialColor = v.GetColor();
                v = stage.finalColor;
                ImGui.ColorEdit4("Final Color", ref v.vec4, ImGuiColorEditFlags.Float | ImGuiColorEditFlags.HDR);
                stage.finalColor = v.GetColor();
                v = stage.fadeColor;
                ImGui.ColorEdit4("Fade Color", ref v.vec4, ImGuiColorEditFlags.Float | ImGuiColorEditFlags.HDR);
                stage.fadeColor = v.GetColor(); 
                ImGui.DragFloat("FadeIn Fraction", ref stage.fadeInFraction, 0.01f, 0f, 1f);     
                ImGui.DragFloat("FadeOut Fraction", ref stage.fadeOutFraction, 0.01f, 0f, 1f);  

                ImGui.DragFloat("Gravity", ref stage.gravity, 0.01f);           

                stage.cycleMsec = (int)((stage.particleLife + stage.deadTime) * 1000f);
            }

            public void Update(
                ref RenderView renderView,
                ref int instanceOffset,
                ref Span<ParticleVertex> vertexBuffer,
                ref int vertexOffset,
                ref Span<UInt16> indexBuffer,
                ref int indexOffset,
                ref Span<ParticleStageDrawSurface> drawSurfaces,
                ref int surfaceOffset)
            {
                int numVertices = 0;
                float diversity = 1f;
                int stageAge = (int)((renderView.time - stage.timeOffset) * 1000f);
                int stageCycleMsec = stageAge / stage.cycleMsec;
                idRandom steppingRandom = new idRandom(((stageCycleMsec << 10) & MaxRand) ^ (int)(diversity * MaxRand));
                idRandom steppingRandom2 = new idRandom((((stageCycleMsec - 1) << 10) & MaxRand) ^ (int)(diversity * MaxRand));

                for(int index = 0; index < stage.maxParticles; index++) {
                    steppingRandom.NextInt();
                    steppingRandom2.NextInt();

                    stageUpdateInfo.particleIndex = index;
                    float bunchOffset = (float)index * (stage.particleLife * stage.spawnBunching * (1f / stage.maxParticles)) * 1000f;
                    int particleAge = stageAge - (int)bunchOffset;
                    int particleCycle = particleAge / stage.cycleMsec;
                    if(particleCycle < 0) {
                        continue;
                    }
                    if(stage.cycles > 0 && particleCycle >= stage.cycles) {
                        continue;
                    }

                    if(particleCycle == stageCycleMsec) {
                        stageUpdateInfo.random = steppingRandom;
                    } else {
                        stageUpdateInfo.random = steppingRandom2;
                    }

                    int inCycleTime = particleAge - particleCycle * stage.cycleMsec;
                    stageUpdateInfo.frac = (float)inCycleTime / (stage.particleLife * 1000f);
                    if(stageUpdateInfo.frac < 0f || stageUpdateInfo.frac > 1f) {
                        continue;
                    }

                    stageUpdateInfo.originalRandom = stageUpdateInfo.random;
                    stageUpdateInfo.ageInSeconds = stageUpdateInfo.frac * stage.particleLife;

                    numVertices += stage.CreateParticle(ref renderView, ref stageUpdateInfo, ref vertexBuffer, vertexOffset + numVertices);
                } 
                
                int numIndices = 0;
                for(int i = 0; i < numVertices; i += 4) {
                    int offset = indexOffset + numIndices;
                    indexBuffer[offset + 0] = (UInt16)(i + 0);
                    indexBuffer[offset + 1] = (UInt16)(i + 1);
                    indexBuffer[offset + 2] = (UInt16)(i + 2);
                    indexBuffer[offset + 3] = (UInt16)(i + 1);
                    indexBuffer[offset + 4] = (UInt16)(i + 3);
                    indexBuffer[offset + 5] = (UInt16)(i + 2);
                    numIndices += 6;
                }  

                drawSurfaces[surfaceOffset].id = instanceOffset++;
                drawSurfaces[surfaceOffset].vertexOffset = vertexOffset;
                drawSurfaces[surfaceOffset].indexOffset = indexOffset;
                drawSurfaces[surfaceOffset].numVertices = numVertices;
                drawSurfaces[surfaceOffset].numIndices = numIndices;
                vertexOffset += numVertices;
                indexOffset += numIndices;
            }

            public void Update(Scene scene) {
                OnGUI();

                RenderView renderView = new RenderView();
                renderView.time = Time.time;

                float diversity = 0f;
                int stageAge = (int)((renderView.time - stage.timeOffset) * 1000f);
                int stageCycleMsec = stageAge / stage.cycleMsec;
                idRandom steppingRandom = new idRandom(((stageCycleMsec << 10) & MaxRand) ^ (int)(diversity * MaxRand));
                idRandom steppingRandom2 = new idRandom((((stageCycleMsec - 1) << 10) & MaxRand) ^ (int)(diversity * MaxRand));

                for(int index = 0; index < stage.maxParticles; index++) {
                    steppingRandom.NextInt();
                    steppingRandom2.NextInt();

                    stageUpdateInfo.particleIndex = index;
                    float bunchOffset = (float)index * (stage.particleLife * stage.spawnBunching * (1f / stage.maxParticles)) * 1000f;
                    int particleAge = stageAge - (int)bunchOffset;
                    int particleCycle = particleAge / stage.cycleMsec;
                    if(particleCycle < 0) {
                        continue;
                    }
                    if(stage.cycles > 0 && particleCycle >= stage.cycles) {
                        continue;
                    }

                    if(particleCycle == stageCycleMsec) {
                        stageUpdateInfo.random = steppingRandom;
                    } else {
                        stageUpdateInfo.random = steppingRandom2;
                    }

                    int inCycleTime = particleAge - particleCycle * stage.cycleMsec;
                    stageUpdateInfo.frac = (float)inCycleTime / (stage.particleLife * 1000f);
                    if(stageUpdateInfo.frac < 0f || stageUpdateInfo.frac > 1f) {
                        continue;
                    }

                    stageUpdateInfo.originalRandom = stageUpdateInfo.random;
                    stageUpdateInfo.ageInSeconds = stageUpdateInfo.frac * stage.particleLife;

                    stage.ParticleOrigin(ref renderView, ref stageUpdateInfo, out Vector3 origin);

                    scene.renderDebug.AddSphere(16, 0.02f, origin, Color.Red);
                }  

                scene.renderDebug.AddBox(particleSystem.boundingBox, Quaternion.Identity, Color.Yellow);              
            }
        }


    internal sealed class Editor {
        CameraComponent camera;
        TransformComponent cameraTransform;
        public readonly Scene scene;
        public readonly SceneHierarchyView sceneView;
        public readonly FileSystemView fileSystemView;

        Level level;

        public Editor() {
            scene = Scene.GlobalScene;
            scene.Init();
            
            level = new Level(scene);
            //scene.InitFromMap(Path.Combine(FileSystem.assetBasePath, "maps/SampleScene.map"));
/*
            Mesh[] levelMeshes = AssetLoader.LoadAssimp(@"G:\src\DeferredTexturing-master\Content\Models\Sponza\Sponza.fbx", out int numMaterials);
            Material[] materials = new Material[numMaterials];
            for(int i = 0; i < materials.Length; i++) {
                materials[i] = Common.declManager.FindMaterial($"SponzaMaterial{i}");
            }
            for(int i = 0; i < levelMeshes.Length; i++) {
                Entity entity = scene.CreateMesh($"Sponza_StatiMesh{i}");
                MeshComponent mesh = scene.meshes.GetComponent(entity);
                mesh.SetRenderData(levelMeshes[i], materials, typeof(DrawVertexPackedVertexFactory), $"Sponza_StatiMesh{i}");
            }
*/
            //scene.CreateActor("actors/ZombieAB.tactor");
            //scene.CreateActor("actors/Mech_Head_02.tactor", new Vector3(0f, 0f, 0f), Quaternion.Identity);
            //scene.CreateMD5Actor("models/md5/revenant/james/revenant.md5mesh", Vector3.Zero, Quaternion.Identity, 0.05f);
            //level.ParseActor("actors/Sponza_New.tactor");
            //level.ParseActor("actors/DamagedHelmet.tactor");
            //level.ParseActor("actors/Cube.tactor");
            level.ParseLevel("levels/Re.level");

            cameraTransform = new TransformComponent();
            camera = new CameraComponent();
            camera.SetAspect(1920, 1080);
            camera.SetFOV(60f);
            camera.SetClippingPlanes(0.1f, 100f);
            camera.SetUpdateFrustumVectors();
            cameraTransform.UpdateTransform();
            camera.TransformCamera(cameraTransform);
            camera.UpdateCamera();

            sceneView = new SceneHierarchyView(this);
            fileSystemView = new FileSystemView(this, FileSystem.assetBasePath);

            for(int i = 0; i < scene.directionalLights.Num(); i++) {
                sceneView.AddEntityNode(scene.directionalLights.GetEntity(i), GameObjectType.Light);
            }
            for(int i = 0; i < scene.lights.Num(); i++) {
                sceneView.AddEntityNode(scene.lights.GetEntity(i), GameObjectType.Light);
            }
            for(int i = 0; i < scene.reflectionProbeBounds.Num(); i++) {
                //sceneView.AddEntityNode(GameObjectType.Probe, i, scene.reflectionProbes[i].name);
            }
        }

        int pointLightID;
        int spotLightID;
        int sphereLightID;
        int decalID;
        int scatteringVolumeID;
        int reflectionProbeID;

        void AddLight(string name, LightType lightType) {
            Entity entity = scene.CreateLight(name, lightType);
            sceneView.AddEntityNode(entity, GameObjectType.Light);
            var node = new Hierarchy<TreeNode_GameObject>(new TreeNode_GameObject(entity));
            node.owner.name = scene.names.GetComponent(entity).name;
            node.owner.onInspector += TreeNode_GameObject.InspectorName;
            node.owner.onInspector += TreeNode_GameObject.InspectorTransform;
            node.owner.onInspector += TreeNode_GameObject.InspectorLight;
            level.actors.Add(node);
        }

        void AddDecal(string name) {
            Entity entity = scene.CreateDecal(name);
            sceneView.AddEntityNode(entity, GameObjectType.Decal);
            var node = new Hierarchy<TreeNode_GameObject>(new TreeNode_GameObject(entity));
            node.owner.name = scene.names.GetComponent(entity).name;
            node.owner.onInspector += TreeNode_GameObject.InspectorName;
            node.owner.onInspector += TreeNode_GameObject.InspectorTransform;
            node.owner.onInspector += TreeNode_GameObject.InspectorDecal;
            level.actors.Add(node);
        }

        void AddScatteringVolume(string name) {
            Entity entity = scene.CreateScatteringVolume(name);
            sceneView.AddEntityNode(entity, GameObjectType.ScatteringVolume);   
            var node = new Hierarchy<TreeNode_GameObject>(new TreeNode_GameObject(entity));
            node.owner.name = scene.names.GetComponent(entity).name;
            node.owner.onInspector += TreeNode_GameObject.InspectorName;
            node.owner.onInspector += TreeNode_GameObject.InspectorTransform;
            node.owner.onInspector += TreeNode_GameObject.InspectorScatteringVolume;
            level.actors.Add(node);       
        }

        void AddReflectionProbe(string name) {
            Entity entity = scene.CreateReflectionProbe(name);
            //sceneView.AddEntityNode(entity, GameObjectType.ScatteringVolume);   
            var node = new Hierarchy<TreeNode_GameObject>(new TreeNode_GameObject(entity));
            node.owner.name = scene.names.GetComponent(entity).name;
            node.owner.onInspector += TreeNode_GameObject.InspectorName;
            node.owner.onInspector += TreeNode_GameObject.InspectorTransform;
            node.owner.onInspector += TreeNode_GameObject.InspectorReflectionProbe;
            level.actors.Add(node);                 
        }

        public void DrawHierarchy() {
            ImGui.Begin("Hierarchy");
            if(ImGui.BeginTabBar("TabBar")) {
                if(ImGui.BeginTabItem("Scene")) {
                    if(ImGui.BeginPopupContextWindow("Add objects")) {
                        if(ImGui.Button("Add PointLight")) {
                            AddLight("PointLight" + pointLightID, LightType.Point);
                            pointLightID++;
                        }
                        if(ImGui.Button("Add SpotLight")) {
                            AddLight("SpotLight" + spotLightID, LightType.Spot);
                            spotLightID++;
                        }
                        if(ImGui.Button("Add SphereLight")) {
                            AddLight("SphereLight" + sphereLightID, LightType.Sphere);
                            sphereLightID++;
                        }
                        if(ImGui.Button("Add Decal")) {
                            AddDecal($"Decal{decalID}");
                            decalID++;
                        }                      
                        if(ImGui.Button("Add Scattering Volume")) {
                            AddScatteringVolume($"ScatteringVolume{scatteringVolumeID}");
                            scatteringVolumeID++;
                        }
                        ImGui.EndPopup();
                    }
                    sceneView.Update();
                    ImGui.EndTabItem();
                }

                if(ImGui.BeginTabItem("FileSystem")) {
                    fileSystemView.Update();
                    ImGui.EndTabItem();
                }

                ImGui.EndTabBar();
            }
            ImGui.End();
        }

        public void DrawInspector() {
            ImGui.Begin("Inspector");
            if(sceneView.selected != null) {
                sceneView.selected.OnInspectorGUI();
                sceneView.selected.OnSceneInspector();
                
            }
            ImGui.End();
        }

        static void DrawCVars(Type type) {
            var fields = type.GetFields(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            foreach(var field in fields) {
                var value = field.GetValue(null);
                if(value is CVarFloat _float) {
                    float v = _float.value;
                    if(ImGui.DragFloat(field.Name, ref v, _float.speed, _float.minValue, _float.maxValue)) {
                        _float.Set(v);
                    }
                } else if(value is CVarInteger _integer) {
                    int v = _integer.value;
                    if(ImGui.DragInt(field.Name, ref v, _integer.speed, _integer.minValue, _integer.maxValue)) {
                        _integer.Set(v);
                    }                    
                } else if(value is CVarVector _vector) {
                    ImGuiVector v = _vector.value;
                    if(_vector.flags.HasFlag(CVar.Flags.Color)) {
                        if(ImGui.ColorEdit4(field.Name, ref v.vec4, ImGuiColorEditFlags.HDR | ImGuiColorEditFlags.Float)) {
                            _vector.Set(v.GetVector4());
                        }
                    } else {
                        if(ImGui.DragFloat4(field.Name, ref v.vec4)) {
                            _vector.Set(v.GetVector4());
                        }                        
                    }
                } else if(value is CVarBool _bool) {
                    if(ImGui.RadioButton(field.Name, _bool.value)) {
                        _bool.Set(_bool.value ^ true);
                    }
                }
            }
        }

        void DrawRenderingSettings() {
            ImGui.Begin("Rendering Settings", ref showRenderingSettings);
            if(ImGui.CollapsingHeader("PostProcessing")) {
                DrawCVars(typeof(PostProcessingCVars));
            }
            if(ImGui.CollapsingHeader("Skylighting")) {
                DrawCVars(typeof(SkylighingCVars));
            }
            ImGui.End();
        }

        void DrawLoadActorWindow() {
            ImGui.Begin("Load Actor", ref showLoadActorWindow);
            ImGui.End();
        }

        UIHierarchy<TreeNode_GameObject> sceneHierarchyView = new UIHierarchy<TreeNode_GameObject>();

        public void DrawLevelEditorWindows() {
            var actors = level.actors;
            ImGui.Begin("Hierarchy Window");
            if(ImGui.BeginTabBar("TabBar")) {
                if(ImGui.BeginTabItem("Scene")) {
                    if(ImGui.BeginPopupContextWindow("Add objects")) {
                        if(ImGui.Button("Delete")) {
                            if(sceneHierarchyView.selectedNode != null) {
                                var node = sceneHierarchyView.selectedNode;
                                var sibling = node.GetNextSiblingNode();
                                while(node != sibling) {
                                    node.owner.Destroy(this);
                                    node = node.GetNextNode();
                                }
                                
                                sceneHierarchyView.ClearSelected();
                            }
                        }
                        if(ImGui.Button("Add PointLight")) {
                            AddLight("PointLight" + pointLightID, LightType.Point);
                            pointLightID++;
                        }
                        if(ImGui.Button("Add SpotLight")) {
                            AddLight("SpotLight" + spotLightID, LightType.Spot);
                            spotLightID++;
                        }
                        if(ImGui.Button("Add SphereLight")) {
                            AddLight("SphereLight" + sphereLightID, LightType.Sphere);
                            sphereLightID++;
                        }
                        if(ImGui.Button("Add Decal")) {
                            AddDecal($"Decal{decalID}");
                            decalID++;
                        }                      
                        if(ImGui.Button("Add Scattering Volume")) {
                            AddScatteringVolume($"ScatteringVolume{scatteringVolumeID}");
                            scatteringVolumeID++;
                        }
                        if(ImGui.Button("Add Reflection Probe")) {
                            AddReflectionProbe($"reflectionProbe{reflectionProbeID}");
                            reflectionProbeID++;
                        }
                        ImGui.EndPopup();
                    }

                    if(ImGui.TreeNodeEx("Scene")) {
                        for(int i = 0; i < actors.Count; i++) {
                            if(!actors[i].owner.entity.IsValid()) {
                                actors.RemoveAt(i);
                                continue;
                            }
                            sceneHierarchyView.DrawNode_r(actors[i]);
                        }
                        ImGui.TreePop();
                    }


                    ImGui.EndTabItem();
                }

                if(ImGui.BeginTabItem("FileSystem")) {
                    fileSystemView.Update();
                    ImGui.EndTabItem();
                }

                ImGui.EndTabBar();
            }

            ImGui.End();

            ImGui.Begin("Inspector Window");
            if(sceneHierarchyView.selectedNode != null) {
                sceneHierarchyView.selectedNode.owner.onInspector(this, sceneHierarchyView.selectedNode.owner);
            }
            ImGui.End();
        }

        bool showRenderingSettings;
        bool showLoadActorWindow;
        public void OnGUI() {
            if(ImGui.BeginMainMenuBar()) {
                if(ImGui.BeginMenu("File")) {
                    if(ImGui.MenuItem("Load Actor")) {
                        showLoadActorWindow = true;
                    }
                    ImGui.EndMenu();
                }
                if(ImGui.BeginMenu("Settings")) {
                    if(ImGui.MenuItem("Rendering")) {
                        showRenderingSettings = true;
                    }
                    ImGui.EndMenu();
                }
                if(showRenderingSettings) {
                    DrawRenderingSettings();
                }
                if(showLoadActorWindow) {
                    DrawLoadActorWindow();
                }
                ImGui.EndMainMenuBar();
            }

            //DrawHierarchy();
            //DrawInspector();
            
            DrawLevelEditorWindows();
        }

        Ray ScreenUVToRay(CameraComponent camera, Vector3 cameraOrigin, Vector2 uv) {
            Vector3 frustumVecX0 = Vector3.Lerp(camera.frustumVectorLT, camera.frustumVectorRT, uv.X); 
            Vector3 frustumVecX1 = Vector3.Lerp(camera.frustumVectorLB, camera.frustumVectorRB, uv.X);
            Vector3 frustumVec = Vector3.Lerp(frustumVecX0, frustumVecX1, uv.Y);
            frustumVec.Normalize();
            return new Ray(cameraOrigin, frustumVec);
        }

        Vector3 ScreenToViewPoint(Vector3 pt) {
            Vector2 uv = new Vector2((pt.X + 0.5f) / camera.width, (pt.Y + 0.5f) / camera.height);
            Vector2 clip = new Vector2(uv.X, 1f - uv.Y) * 2f - 1f;
            float m00 = camera.projectionMatrix[0, 0];
            float m11 = camera.projectionMatrix[1, 1];
            Vector3 view = new Vector3(m00 != 0f ? (clip.X / m00) : 1f, m11 != 0f ? (clip.Y / m11) : 1f, pt.Z);
            view.X *= -pt.Z;
            view.Y *= -pt.Z;
            return view;
        }

        const int NumClustersX = 16;
        const int NumClustersY = 8;
        const int NumClustersZ = 24;
        const float StartDepth = 5f;
        const float EndDepth = 500f;
        ClusterFrustum[] clusterFrustums;
        void UpdateClusterFrustums() {
            if(clusterFrustums == null) {
                clusterFrustums = new ClusterFrustum[NumClustersX * NumClustersY * NumClustersZ];
            }

            float zNear = StartDepth;
            float zFar = MathF.Min(EndDepth, camera.farClipPlane);
            Vector2 clusterSize = new Vector2((float)camera.width / (float)NumClustersX, (float)camera.height / (float)NumClustersY);

            for(int z = 0; z < NumClustersZ; z++) {
                float near;
                float far;
                if(z == 0) {
                    near = -camera.nearClipPlane;
                    far = -zNear;
                }  else {
                    float k = MathF.Pow(zFar / zNear, (float)(z - 1) / (float)(NumClustersZ - 1));
                    float k2 = MathF.Pow(zFar / zNear, (float)z / (float)(NumClustersZ - 1));
                    near = -zNear * k;
                    far = -zNear * k2;
                }

                Plane forward = new Plane(new Vector3(0f, 0f, -1f), near);
                Plane back = new Plane(new Vector3(0f, 0f, 1f), far);
                Plane nearPlane = new Plane(new Vector3(0f, 0f, 1f), near);
                Plane farPlane = new Plane(new Vector3(0f, 0f, 1f), far);

                for (int y = 0; y < NumClustersY; y ++) {
                    Vector3 pt0 = ScreenToViewPoint(new Vector3(0f, MathF.Ceiling(clusterSize.Y * y), -zFar));
                    Vector3 pt1 = ScreenToViewPoint(new Vector3(camera.width, MathF.Ceiling(clusterSize.Y * y), -zFar));
                    Plane top = new Plane(Vector3.Zero, pt0, pt1);
                    
                    pt0 = ScreenToViewPoint(new Vector3(0f, MathF.Ceiling(clusterSize.Y * (y + 1)), -zFar));
                    pt1 = ScreenToViewPoint(new Vector3(camera.width, MathF.Ceiling(clusterSize.Y * (y + 1)), -zFar));
                    Plane bottom = new Plane(Vector3.Zero, pt0, pt1);

                    for (int x = 0; x < NumClustersX; x ++) {
                        int index = x + y * NumClustersX + z * NumClustersY * NumClustersX;
                        {
                            pt0 = ScreenToViewPoint(new Vector3(MathF.Ceiling(clusterSize.X * x), 0, -zFar));
                            pt1 = ScreenToViewPoint(new Vector3(MathF.Ceiling(clusterSize.X * x), camera.height, -zFar));
                            Plane left = new Plane(Vector3.Zero, pt0, pt1);

                            pt0 = ScreenToViewPoint(new Vector3(MathF.Ceiling(clusterSize.X * (x + 1)), 0f, -zFar));
                            pt1 = ScreenToViewPoint(new Vector3(MathF.Ceiling(clusterSize.X * (x + 1)), camera.height, -zFar));
                            Plane right = new Plane(Vector3.Zero, pt0, pt1);

                            if(clusterFrustums[index].planes == null) {
                                clusterFrustums[index].planes = new Plane[6];
                            }
                            clusterFrustums[index].planes[0] = left;
                            clusterFrustums[index].planes[1] = right;
                            clusterFrustums[index].planes[2] = top;
                            clusterFrustums[index].planes[3] = bottom;
                            clusterFrustums[index].planes[4] = forward;
                            clusterFrustums[index].planes[5] = back;
                        }
                        {
                            Vector3[] corners = new Vector3[8];
                            corners[0] = (new Vector3(MathF.Ceiling(clusterSize.X * x), MathF.Ceiling(clusterSize.Y * y), near));
                            corners[1] = (new Vector3(MathF.Ceiling(clusterSize.X * (x + 1)), corners[0].Y, near));
                            corners[2] = (new Vector3(corners[1].X, MathF.Ceiling(clusterSize.Y * (y + 1)), near));
                            corners[3] = (new Vector3(corners[0].X, corners[2].Y, near));
                            corners[4] = (new Vector3(corners[0].X, corners[0].Y, far));
                            corners[5] = (new Vector3(corners[1].X, corners[1].Y, far));
                            corners[6] = (new Vector3(corners[2].X, corners[2].Y, far));
                            corners[7] = (new Vector3(corners[3].X, corners[3].Y, far));
                            for(int i = 0; i < corners.Length; i++) {
                                corners[i] = ScreenToViewPoint(corners[i]);
                            }
/*
                            Vector3 min = ScreenToViewPoint(new Vector3(MathF.Ceiling(clusterSize.X * x), MathF.Ceiling(clusterSize.Y * y), -zFar));
                            Vector3 max = ScreenToViewPoint(new Vector3(MathF.Ceiling(clusterSize.X * (x + 1)), MathF.Ceiling(clusterSize.Y * (y + 1)), -zFar));
                            Ray lt = new Ray(Vector3.Zero, min);
                            Ray rb = new Ray(Vector3.Zero, max);

                            bool b = Collision.RayIntersectsPlane(ref lt, ref nearPlane, out Vector3 nearMin);
                            b = Collision.RayIntersectsPlane(ref rb, ref nearPlane, out Vector3 nearMax);
                            b = Collision.RayIntersectsPlane(ref lt, ref farPlane, out Vector3 farMin);
                            b = Collision.RayIntersectsPlane(ref rb, ref farPlane, out Vector3 farMax);

                            Vector3 aabbMin = Vector3.Min(nearMin, Vector3.Min(nearMax, Vector3.Min(farMin, farMax)));
                            Vector3 aabbMax = Vector3.Max(nearMin, Vector3.Max(nearMax, Vector3.Max(farMin, farMax)));
*/
                            clusterFrustums[index].aabb = BoundingBox.FromPoints(corners);
                            clusterFrustums[index].sphere = BoundingSphere.FromBox(clusterFrustums[index].aabb);
                        }
                    }
                }
            }
        }

        enum ClusterDrawType {
            Cluster,
            AABB,
            Sphere
        }

        void DrawClusterFrustum(int x, int y, int z, Color color) {
            float zNear = StartDepth;
            float zFar = MathF.Min(EndDepth, camera.farClipPlane);
            Vector2 clusterSize = new Vector2(((float)camera.width / (float)NumClustersX), ((float)camera.height / (float)NumClustersY));
            float minZ = z == 0 ? -camera.nearClipPlane : -zNear * MathF.Pow(zFar / zNear, (float)(z - 1) / (float)(NumClustersZ - 1));
            float maxZ = z == 0 ? -zNear : -zNear * MathF.Pow(zFar / zNear, (float)z / (float)(NumClustersZ - 1));

            var pt0 = new Vector3(MathF.Ceiling(clusterSize.X * x), MathF.Ceiling(clusterSize.Y * y), minZ);
            var pt1 = new Vector3(MathF.Ceiling(clusterSize.X * (x + 1)), pt0.Y, minZ);
            var pt2 = new Vector3(pt1.X, MathF.Ceiling(clusterSize.Y * (y + 1)), minZ);
            var pt3 = new Vector3(pt0.X, pt2.Y, minZ);
            var pt00 = new Vector3(pt0.X, pt0.Y, maxZ);
            var pt11 = new Vector3(pt1.X, pt1.Y, maxZ);
            var pt22 = new Vector3(pt2.X, pt2.Y, maxZ);
            var pt33 = new Vector3(pt3.X, pt3.Y, maxZ);

            pt0 = Vector3.TransformCoordinate(ScreenToViewPoint(pt0), camera.viewToWorldMatrix);
            pt1 = Vector3.TransformCoordinate(ScreenToViewPoint(pt1), camera.viewToWorldMatrix);
            pt2 = Vector3.TransformCoordinate(ScreenToViewPoint(pt2), camera.viewToWorldMatrix);
            pt3 = Vector3.TransformCoordinate(ScreenToViewPoint(pt3), camera.viewToWorldMatrix);
            pt00 = Vector3.TransformCoordinate(ScreenToViewPoint(pt00), camera.viewToWorldMatrix);
            pt11 = Vector3.TransformCoordinate(ScreenToViewPoint(pt11), camera.viewToWorldMatrix);
            pt22 = Vector3.TransformCoordinate(ScreenToViewPoint(pt22), camera.viewToWorldMatrix);
            pt33 = Vector3.TransformCoordinate(ScreenToViewPoint(pt33), camera.viewToWorldMatrix);

            scene.renderDebug.AddLine(pt0, pt1, color);
            scene.renderDebug.AddLine(pt1, pt2, color);
            scene.renderDebug.AddLine(pt2, pt3, color);
            scene.renderDebug.AddLine(pt3, pt0, color);
            scene.renderDebug.AddLine(pt00, pt11, color);
            scene.renderDebug.AddLine(pt11, pt22, color);
            scene.renderDebug.AddLine(pt22, pt33, color);
            scene.renderDebug.AddLine(pt33, pt00, color);
            scene.renderDebug.AddLine(pt0, pt00, color);
            scene.renderDebug.AddLine(pt1, pt11, color);
            scene.renderDebug.AddLine(pt2, pt22, color);
            scene.renderDebug.AddLine(pt3, pt33, color);
        }

        void DrawCluster(int index, Color color, ClusterDrawType drawType = ClusterDrawType.Cluster) {
            if(drawType == ClusterDrawType.Cluster) {
                int x = index % NumClustersX;
                int y = index % (NumClustersX * NumClustersY) / NumClustersX;
                int z = index / (NumClustersX * NumClustersY);
                DrawClusterFrustum(x, y, z, color);
            } else if(drawType == ClusterDrawType.Sphere) {
                scene.renderDebug.AddSphere(16, clusterFrustums[index].sphere.Radius, Vector3.TransformCoordinate(clusterFrustums[index].sphere.Center, camera.viewToWorldMatrix), color);
            } else if(drawType == ClusterDrawType.AABB) {
                var corners = clusterFrustums[index].aabb.GetCorners();
                for(int i = 0; i < corners.Length; i++) {
                    corners[i] = Vector3.TransformCoordinate(corners[i], camera.viewToWorldMatrix);
                }

                scene.renderDebug.AddLine(corners[0], corners[1], color);
                scene.renderDebug.AddLine(corners[1], corners[2], color);
                scene.renderDebug.AddLine(corners[2], corners[3], color);
                scene.renderDebug.AddLine(corners[3], corners[0], color);
                scene.renderDebug.AddLine(corners[4], corners[5], color);
                scene.renderDebug.AddLine(corners[5], corners[6], color);
                scene.renderDebug.AddLine(corners[6], corners[7], color);
                scene.renderDebug.AddLine(corners[7], corners[4], color);
                scene.renderDebug.AddLine(corners[4], corners[0], color);
                scene.renderDebug.AddLine(corners[5], corners[1], color);
                scene.renderDebug.AddLine(corners[6], corners[2], color);
                scene.renderDebug.AddLine(corners[7], corners[3], color);
            }
        }

        Ray GetPickRay(UserInput userInput, Veldrid.Sdl2.Sdl2Window mainWindow, Vector3 viewOrigin) {
            Vector2 pt = userInput.mousePosition;
            Vector2 rcpSize = Vector2.One / new Vector2(mainWindow.Width, mainWindow.Height);
            return ScreenUVToRay(scene.mainCamera, viewOrigin, pt * rcpSize);
        }

        Translator translator = new Translator();
        Rotator rotator = new Rotator();
        Scaler scaler = new Scaler();
        float angle;
        Vector2 prevPos;

        public Vector3 UVToSphereNormal(Vector2 tc) {
            float latitude = tc.X * MathF.PI;
            Vector2 sincosLatitude = new Vector2(MathF.Sin(latitude), MathF.Cos(latitude));
            float longitude = tc.Y * MathF.PI * 2f;
            Vector2 sincosLongitude = new Vector2(MathF.Sin(longitude), MathF.Cos( longitude ) );
            return Vector3.Normalize(new Vector3(sincosLatitude.X * sincosLongitude.Y, sincosLatitude.X * sincosLongitude.X, sincosLatitude.Y));
        }

        void DrawSplitFrustum(Vector3[] corners, Color color) {
            scene.renderDebug.AddLine(corners[0], corners[1], color);
            scene.renderDebug.AddLine(corners[1], corners[2], color);
            scene.renderDebug.AddLine(corners[2], corners[3], color);
            scene.renderDebug.AddLine(corners[3], corners[0], color);
            scene.renderDebug.AddLine(corners[4], corners[5], color);
            scene.renderDebug.AddLine(corners[5], corners[6], color);
            scene.renderDebug.AddLine(corners[6], corners[7], color);
            scene.renderDebug.AddLine(corners[7], corners[4], color);
            scene.renderDebug.AddLine(corners[4], corners[0], color);
            scene.renderDebug.AddLine(corners[5], corners[1], color);
            scene.renderDebug.AddLine(corners[6], corners[2], color);
            scene.renderDebug.AddLine(corners[7], corners[3], color);
        }

        public static readonly Vector3 fourCascadesSplit = new Vector3(0.067f, 0.2f, 0.47f);
        static Vector3[] cascadeFrustumCornersVS = new Vector3[8];
        static Vector3[] cascadeFrustumCornersWS = new Vector3[8];
        static Vector3[] cascadeFrustumCornersLS = new Vector3[8];

        void UpdateCascadeFrustumCornersVS(float near, float far, ref Matrix projectionMatrix, ref Vector3[] corners) {
            corners[0] = MathHelper.ClipToViewPoint(-1f, 1f, -near, ref projectionMatrix);
            corners[1] = MathHelper.ClipToViewPoint(1f, 1f, -near, ref projectionMatrix);
            corners[2] = MathHelper.ClipToViewPoint(1f, -1f, -near, ref projectionMatrix);
            corners[3] = MathHelper.ClipToViewPoint(-1f, -1f, -near, ref projectionMatrix);
            corners[4] = MathHelper.ClipToViewPoint(-1f, 1f, -far, ref projectionMatrix);
            corners[5] = MathHelper.ClipToViewPoint(1f, 1f, -far, ref projectionMatrix);
            corners[6] = MathHelper.ClipToViewPoint(1f, -1f, -far, ref projectionMatrix);
            corners[7] = MathHelper.ClipToViewPoint(-1f, -1f, -far, ref projectionMatrix);
        }

        void UpdateCascadeFrustumCornersWS(ref Matrix invViewMatrix, Vector3[] cornersVS, ref Vector3[] cornersWS) {
            for (int i = 0; i < cornersVS.Length; i++) {
                cornersWS[i] = Vector3.TransformCoordinate(cornersVS[i], invViewMatrix);
            }
        }

        void UpdateCascadeFrustumCornersWS(float near, float far, ref Matrix invViewProjectionMatrix, ref Vector3[] cornersWS) {
            cornersWS[0] = MathHelper.ClipToWorldPoint(-1f, 1f, 0, ref invViewProjectionMatrix);
            cornersWS[1] = MathHelper.ClipToWorldPoint(1f, 1f, 0, ref invViewProjectionMatrix);
            cornersWS[2] = MathHelper.ClipToWorldPoint(1f, -1f, 0, ref invViewProjectionMatrix);
            cornersWS[3] = MathHelper.ClipToWorldPoint(-1f, -1f, 0, ref invViewProjectionMatrix);
            cornersWS[4] = MathHelper.ClipToWorldPoint(-1f, 1f, 1, ref invViewProjectionMatrix);
            cornersWS[5] = MathHelper.ClipToWorldPoint(1f, 1f, 1, ref invViewProjectionMatrix);
            cornersWS[6] = MathHelper.ClipToWorldPoint(1f, -1f, 1, ref invViewProjectionMatrix);
            cornersWS[7] = MathHelper.ClipToWorldPoint(-1f, -1f, 1, ref invViewProjectionMatrix);
            for(int i = 0; i < 4; i++) {
                Vector3 cornerRay = cascadeFrustumCornersWS[i + 4] - cascadeFrustumCornersWS[i];
                Vector3 nearCornerRay = cornerRay * near;
                Vector3 farCornerRay = cornerRay * far;
                cascadeFrustumCornersWS[i + 4] = cascadeFrustumCornersWS[i] + farCornerRay;
                cascadeFrustumCornersWS[i] = cascadeFrustumCornersWS[i] + nearCornerRay;
            }
        }

        void UpdateCascadeFrustumCorners(float near, float far, ref Matrix invViewMatrix, ref Matrix projectionMatrix, ref Vector3[] cornersVS, ref Vector3[] cornersWS) {
            UpdateCascadeFrustumCornersVS(near, far, ref projectionMatrix, ref cornersVS);
            UpdateCascadeFrustumCornersWS(ref invViewMatrix, cornersVS, ref cornersWS);
        }


        internal Matrix ComputeDirectionalLightShadowMatrix(
            int cascadeIndex, 
            int numCascades, 
            Vector3 splitRatio, 
            float nearClip, 
            float farClip,
            int shadowMapSize,
            ref Matrix viewMatrix,
            ref Matrix projectionMatrix, 
            out BoundingSphere cullingSphere)
         {
            float halfShadowMapSize = shadowMapSize * 0.5f;
            float range = farClip - nearClip;
            float min = cascadeIndex == 0 ? nearClip : (nearClip + splitRatio[cascadeIndex - 1] * range);
            float max = numCascades == 1 || cascadeIndex == 3 ? farClip : (nearClip + splitRatio[cascadeIndex] * range);
            Matrix invViewMatrix = Matrix.Invert(viewMatrix);
            Matrix invViewProjectionMatrix = Matrix.Invert(viewMatrix * projectionMatrix);
            UpdateCascadeFrustumCorners(min, max, ref invViewMatrix, ref projectionMatrix, ref cascadeFrustumCornersVS, ref cascadeFrustumCornersWS);
            //UpdateCascadeFrustumCornersWS(min, max, ref invViewProjectionMatrix, ref cascadeFrustumCornersWS);
            Vector3 cascadeFrustumCenter = Vector3.Zero;
            for(int i = 0; i < cascadeFrustumCornersWS.Length; i++) {
                cascadeFrustumCenter += cascadeFrustumCornersWS[i];
            }
            cascadeFrustumCenter *= 1f / 8f;

            Vector3 lightUp = MathHelper.Vec3Up;
            float sphereRadius = 0f;
            for(int i = 0; i < 8; i++) {
                float dist = Vector3.Distance(cascadeFrustumCornersWS[i], cascadeFrustumCenter);
                sphereRadius = MathF.Max(sphereRadius, dist);
            }
            sphereRadius = MathF.Ceiling(sphereRadius * 16f) / 16;
            cullingSphere = new BoundingSphere(cascadeFrustumCenter, sphereRadius);
            Vector3 maxExtents = new Vector3(sphereRadius);
            Vector3 minExtents = new Vector3(-sphereRadius);
            Vector3 cascadeExtents = maxExtents - minExtents;
            Vector3 lightViewOrigin = cascadeFrustumCenter + (MathHelper.Vec3Backward * -minExtents.Y);
            Matrix lightViewMatrix = Matrix.LookAtRH(lightViewOrigin, cascadeFrustumCenter, lightUp);
            Matrix lightProjectionMatrix = Matrix.OrthoOffCenterRH(minExtents.X, maxExtents.X, minExtents.Z, maxExtents.Z, -farClip, farClip);

            scene.renderDebug.AddAxis(lightViewOrigin, MathHelper.Vec3Right, MathHelper.Vec3Forward, MathHelper.Vec3Up, 2f);
            //scene.renderDebug.AddSphere(16, cullingSphere.Radius, cullingSphere.Center, Color.Yellow);
            return lightViewMatrix * lightProjectionMatrix;
        }

        public void Update2(UserInput userInput, Veldrid.Sdl2.Sdl2Window mainWindow) {
            OnGUI();

            Vector3 viewOrigin = EditorController.origin;

            if(sceneHierarchyView.selectedNode != null) {
                TransformComponent transform = scene.transforms.GetComponent(sceneHierarchyView.selectedNode.owner);
                transform.localToWorldMatrix.Decompose(out var scale, out var rotation, out var origin);

                translator.origin = origin;
                rotator.rotation = rotation;
                float widgetScale = scene.renderDebug.GetWidgetScale(translator.origin, viewOrigin);
                if(userInput.GetMouseButtonDown(0)) {
                    prevPos = userInput.mousePosition;
                    Ray pickRay = GetPickRay(userInput, mainWindow, viewOrigin);
                    translator.DragBegin(pickRay, rotator.rotation, scene.mainCameraTransform.GetForward(), widgetScale);
                    rotator.DragBegin(pickRay, translator.origin, widgetScale, viewOrigin, scene.mainCameraTransform.GetForward(), scene.mainCamera.viewToWorldMatrix);
                } 
                if((translator.handleType != AxisHandleType.None || rotator.handleType != AxisHandleType.None) && userInput.GetMouseButton(0)) {
                    var dragDir = userInput.mousePosition - prevPos;
                    Ray pickRay = GetPickRay(userInput, mainWindow, viewOrigin);
                    translator.DragUpdate(pickRay, scene.mainCameraTransform.GetForward());
                    rotator.origin = translator.origin;
                    rotator.DragUpdate(pickRay, dragDir);
                    prevPos = userInput.mousePosition;
                    transform.SetLocalPosition(translator.origin);
                    transform.SetLocalRotation(rotator.rotation);
                } else {
                    EditorController.Update(userInput, mainWindow);
                }

                if(userInput.GetMouseButtonUp(0)) {
                    translator.DragEnd();
                    rotator.DragEnd();
                    scaler.DragEnd();
                }   

                Translator.Draw(translator, rotator.rotation, widgetScale);
                Rotator.Draw(rotator, viewOrigin, widgetScale);                
            } else {
                EditorController.Update(userInput, mainWindow);                
            }

            scene.mainCameraTransform.SetLocalPosition(EditorController.origin);
            scene.mainCameraTransform.SetAxis(EditorController.right, EditorController.forward, EditorController.up);
            scene.mainCamera.TransformCamera(scene.mainCameraTransform);
            scene.mainCamera.UpdateCamera();
            scene.Update();     
        }

        public void Update(UserInput userInput, Veldrid.Sdl2.Sdl2Window mainWindow) {
            OnGUI();

            Vector3 viewOrigin = EditorController.origin;

            //UpdateClusterFrustums();
            if(sceneView.selected is GameObjectNode objectNode) {
                Vector3 origin = Vector3.Zero;
                TransformComponent transform = null;
                BoundsComponent bounds = null;
                LightComponent light = null;
                if(objectNode.type == GameObjectType.Light || objectNode.type == GameObjectType.Decal || objectNode.type == GameObjectType.ScatteringVolume) {
                    transform = scene.transforms.GetComponent(objectNode.entity);
                    bounds = scene.lightBounds.GetComponent(objectNode.entity);
                    //light = scene.lights.GetComponent(objectNode.entity);
                    origin = transform.GetLocalPosition();
                    rotator.rotation = transform.GetLocalRotation();
                } else if(objectNode.type == GameObjectType.Decal) {

                } else if(objectNode.type == GameObjectType.Probe) {
                    var probe = objectNode as GameObjectNode;
                    bounds = scene.reflectionProbeBounds[probe.sceneID];
                    //origin = scene.reflectionProbes[probe.sceneID].position;
                }
                translator.origin = origin;
                float widgetScale = scene.renderDebug.GetWidgetScale(translator.origin, viewOrigin);
                if(userInput.GetMouseButtonDown(0)) {
                    prevPos = userInput.mousePosition;
                    Ray pickRay = GetPickRay(userInput, mainWindow, viewOrigin);
                    translator.DragBegin(pickRay, rotator.rotation, scene.mainCameraTransform.GetForward(), widgetScale);
                    rotator.DragBegin(pickRay, translator.origin, widgetScale, viewOrigin, scene.mainCameraTransform.GetForward(), scene.mainCamera.viewToWorldMatrix);
                    //scaler.DragBegin(pickRay, translator.origin, rotator.rotation, scene.mainCameraTransform.GetForward(), scale);
                } 
                if((translator.handleType != AxisHandleType.None || rotator.handleType != AxisHandleType.None) && userInput.GetMouseButton(0)) {
                    var dragDir = userInput.mousePosition - prevPos;
                    Ray pickRay = GetPickRay(userInput, mainWindow, viewOrigin);
                    translator.DragUpdate(pickRay, scene.mainCameraTransform.GetForward());
                    rotator.origin = translator.origin;
                    rotator.DragUpdate(pickRay, dragDir);
                    prevPos = userInput.mousePosition;
                    transform?.SetLocalPosition(translator.origin);
                    transform?.SetLocalRotation(rotator.rotation);
                } else {
                    EditorController.Update(userInput, mainWindow);
                }

                if(userInput.GetMouseButtonUp(0)) {
                    translator.DragEnd();
                    rotator.DragEnd();
                    scaler.DragEnd();
                }   

                //if(light != null) {
                //    bounds.UpdateClusteredLightBounds(camera.worldToViewMatrix, light, transform);
                //} else if(objectNode.type == GameObjectType.Probe) {
                //    bounds.UpdateClusteredProbeBounds(camera.worldToViewMatrix);
                //}  
                //for(int i = 0; i < clusterFrustums.Length; i++) {
                //    if(CollisionEx.Overlap(bounds.planes, bounds.corners, clusterFrustums[i].aabb)) {
                //        DrawCluster(i, Color.Red);
                        //DrawCluster(i, Color.Yellow, ClusterDrawType.AABB);
                //    }
                //} 

                //if(bounds != null) {
                //    scene.renderDebug.AddBox(bounds.boundingBox, Quaternion.Identity, Color.Yellow);
                //}

                Translator.Draw(translator, rotator.rotation, widgetScale);
                Rotator.Draw(rotator, viewOrigin, widgetScale);
            } else {
                EditorController.Update(userInput, mainWindow);
            }

            //Vector3 sunRight = MathHelper.Vec3Right;
            //Vector3 sunForward = MathHelper.Vec3Forward;
            //Vector3 sunUp = MathHelper.Vec3Up;
            //Matrix sunViewMatrix = new Matrix(
            //    sunRight.X, sunForward.X, sunUp.X, 0f,
            //    sunRight.Y, sunForward.Y, sunUp.Y, 0f,
            //    sunRight.Z, sunForward.Z, sunUp.Z, 0f,
            //    0f, 0f, 0f, 1f
            //) * MathHelper.ViewFlipMatrixRH;

            //scene.renderDebug.AddFrustum(camera.nearClipPlane, camera.farClipPlane, camera.fov, camera.aspect, cameraTransform.GetLocalPosition(), cameraTransform.GetRight(), cameraTransform.GetForward(), cameraTransform.GetUp());
            //Matrix cameraViewMatrix = camera.worldToViewMatrix;
            //Matrix cameraProjectionMatrix = camera.projectionMatrix;
            //Matrix sunLocalToWorldMatrix = Matrix.Invert(sunViewMatrix);
            //for(int i = 0; i < 4; i++) {
            //    ComputeDirectionalLightShadowMatrix(i, 4, fourCascadesSplit, camera.nearClipPlane, camera.farClipPlane, 1024, ref cameraViewMatrix, ref cameraProjectionMatrix, out var cullingSphere);
            //    DrawSplitFrustum(cascadeFrustumCornersWS, Color.Red);
            //}

            //particleSystemTest.Update(scene);

            scene.mainCameraTransform.SetLocalPosition(EditorController.origin);
            scene.mainCameraTransform.SetAxis(EditorController.right, EditorController.forward, EditorController.up);
            scene.mainCamera.TransformCamera(scene.mainCameraTransform);
            scene.mainCamera.UpdateCamera();
            scene.Update();     
        }
    }
}