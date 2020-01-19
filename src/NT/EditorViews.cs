using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using SharpDX;
using SharpDX.Mathematics;
using ImGuiNET;

namespace NT
{
    public struct ImGuiVector {
        public System.Numerics.Vector2 vec2;
        public System.Numerics.Vector3 vec3;
        public System.Numerics.Vector4 vec4;

        public Vector2 GetVector2() {return new Vector2(vec2.X, vec2.Y);}
        public Vector3 GetVector3() {return new Vector3(vec3.X, vec3.Y, vec3.Z);}
        public Vector4 GetVector4() {return new Vector4(vec4.X, vec4.Y, vec4.Z, vec4.W);}
        public Color GetColor() {return new Color(vec4.X, vec4.Y, vec4.Z, vec4.W);}

        public ImGuiVector(Vector2 input) {
            vec2 = new System.Numerics.Vector2(input.X, input.Y);
            vec3 = System.Numerics.Vector3.Zero;
            vec4 = System.Numerics.Vector4.Zero;
        }

        public ImGuiVector(Vector3 input) {
            vec2 = new System.Numerics.Vector2(input.X, input.Y);
            vec3 = new System.Numerics.Vector3(input.X, input.Y, input.Z);
            vec4 = System.Numerics.Vector4.Zero;            
        }

        public ImGuiVector(Vector4 input) {
            vec2 = new System.Numerics.Vector2(input.X, input.Y);
            vec3 = new System.Numerics.Vector3(input.X, input.Y, input.Z);
            vec4 = new System.Numerics.Vector4(input.X, input.Y, input.Z, input.W);
        }

        public ImGuiVector(float x, float y) {
            vec2 = new System.Numerics.Vector2(x, y);
            vec3 = System.Numerics.Vector3.Zero;
            vec4 = System.Numerics.Vector4.Zero;
        }

        public ImGuiVector(float x, float y, float z) {
            vec2 = System.Numerics.Vector2.Zero;
            vec3 = new System.Numerics.Vector3(x, y, z);
            vec4 = System.Numerics.Vector4.Zero;
        }        

        public ImGuiVector(float x, float y, float z, float w) {
            vec2 = System.Numerics.Vector2.Zero;
            vec3 = System.Numerics.Vector3.Zero;
            vec4 = new System.Numerics.Vector4(x, y, z, w);
        }

        public ImGuiVector(Color4 input) {
            vec2 = System.Numerics.Vector2.Zero;
            vec3 = System.Numerics.Vector3.Zero;
            vec4 = new System.Numerics.Vector4(input.Red, input.Green, input.Blue, input.Alpha);
        }

        public static implicit operator ImGuiVector(Vector2 value) {
            return new ImGuiVector(value);
        }

        public static implicit operator ImGuiVector(Vector3 value) {
            return new ImGuiVector(value);
        }

        public static implicit operator ImGuiVector(Vector4 value) {
            return new ImGuiVector(value);
        }

        public static implicit operator ImGuiVector(Color4 value) {
            return new ImGuiVector(value);
        }

        public static implicit operator ImGuiVector(Color value) {
            return new ImGuiVector(value.ToVector4());
        }
    }

    internal abstract class HierarchyNode {
        protected HierarchyView myView;

        public string name;
        public bool state {get; private set;}
        protected HierarchyNode[] children;
        public IntPtr id {get; private set;}
        protected ImGuiTreeNodeFlags flags;

        public HierarchyNode(HierarchyView view) : this("", view) {
        }

        public HierarchyNode(string inName, HierarchyView view) {
            myView = view;
            if(inName != null) {
                name = inName;
            } else {
                name = "";
            }
            id = (IntPtr)this.GetHashCode();
            flags = ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.OpenOnDoubleClick;
        }

        protected virtual void Open() {}
        protected virtual void Close() {}

        public virtual void OnContextMenuGUI() {}
        public virtual void OnInspectorGUI() {}
        public virtual void OnSceneInspector() {}

        public virtual void OnHierarchyGUI() {
            if((flags & ImGuiTreeNodeFlags.Leaf) != 0) {
                ImGui.TreeNodeEx(id, flags, name);
                if(ImGui.IsItemClicked()) {
                    if(myView != null) {
                        if(myView.selected != null) {
                            myView.selected.flags &= ~ImGuiTreeNodeFlags.Selected;
                        }
                        myView.selected = this;
                    }
                    flags |= ImGuiTreeNodeFlags.Selected;
                }
            } else {
                state = ImGui.TreeNodeEx(id, flags, name);
                if(ImGui.IsItemClicked()) {
                    if(myView != null) {
                        if(myView.selected != null) {
                            myView.selected.flags &= ~ImGuiTreeNodeFlags.Selected;
                        }
                        myView.selected = this;
                    }
                    flags |= ImGuiTreeNodeFlags.Selected;
                }
                if(state) {
                    if(children != null) {
                        foreach(var child in children) {
                            child.OnHierarchyGUI();
                        }
                    }
                }
            }

            if(state) {
                ImGui.TreePop();
            }
        }
    }

    public enum GameObjectType {
        Light,
        Decal,
        ScatteringVolume,
        Mesh,
        Probe
    }

    internal class GameObjectNode : HierarchyNode {
        public GameObjectType type {get; private set;}
        public Entity entity {get; private set;}
        public int sceneID {get; private set;}

        public GameObjectNode(GameObjectType inType, int sceneID, string name, HierarchyView sceneView) : base(sceneView) {
            type = inType;
            this.sceneID = sceneID;
            this.name = name;
            flags |= ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.NoTreePushOnOpen;
        }

        public GameObjectNode(Entity inEntity, GameObjectType inType, HierarchyView sceneView) : base(sceneView) {
            if(inEntity.IsValid()) {
                entity = inEntity;
                NameComponent nameComponent = Scene.GlobalScene.names.GetComponent(entity);
                if(nameComponent != null) {
                    name = nameComponent.name;
                }
                type = inType;
                flags |= ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.NoTreePushOnOpen;
            }
        }

        bool drawAxis;
        bool drawBounds = true;
        bool drawAABB = false;
        bool drawBoundingSphere = false;

        readonly int[] shadowMapSizes = {
            128, 256, 512, 1024, 2048
        };

        void InspectorLight() {
            LightComponent light = Scene.GlobalScene.lights.GetComponent(entity);
            TransformComponent transform = Scene.GlobalScene.transforms.GetComponent(entity);
            BoundsComponent bounds = Scene.GlobalScene.lightBounds.GetComponent(entity);
            if(light == null) {
                light = Scene.GlobalScene.directionalLights.GetComponent(entity);
                transform = Scene.GlobalScene.transforms.GetComponent(entity);
            }
            float range = light.range;
            float spotAngle = light.spotAngle;
            bool lightCastShadow = light.IsCastingShadows();
            int lightShadowResolution = 0;
            float scattering = light.lightScattering;
            bool enabled = light.IsEnabled();
            switch(light.shadowResolution) {
                case LightShadowResolution._256:
                    lightShadowResolution = 1;
                    break;
                case LightShadowResolution._512:
                    lightShadowResolution = 2;
                    break;
                case LightShadowResolution._1024:
                    lightShadowResolution = 3;
                    break;
                case LightShadowResolution._2048:
                    lightShadowResolution = 4;
                    break;
            }

            ImGui.PushID(GetHashCode());
            if(ImGui.RadioButton("DrawBounds", drawBounds)) {
                drawBounds ^= true;
            }
            if(ImGui.RadioButton("DrawAABB", drawAABB)) {
                drawAABB ^= true;
            }
            if(ImGui.RadioButton("DrawBoundingSphere", drawBoundingSphere)) {
                drawBoundingSphere ^= true;
            }
            if(ImGui.RadioButton("Enabled", enabled)) {
                enabled ^= true;
            }
            if(ImGui.RadioButton("CastShadow", lightCastShadow)) {
                lightCastShadow ^= true;
            }

            ImGuiVector color = new ImGuiVector(light.color);
            float intensity = light.GetIntensity();
            float specularMultiplier = light.specularMultiplier;
            float shadowBias = light.shadowBias;
            ImGui.ColorEdit3("Color", ref color.vec3, ImGuiColorEditFlags.Float | ImGuiColorEditFlags.HDR);
            ImGui.DragFloat("Intensity", ref intensity, 0.1f, 0f, 99999f);
            ImGui.DragFloat("SpecularMultiplier", ref specularMultiplier, 0.1f, 0f, LightComponent.MaxSpecularMultiplier);
            if(ImGui.DragFloat("Scattering", ref scattering, 0.1f, 0f, 100f)) {
                light.SetScattering(scattering);
            }
            ImGui.DragFloat("ShadowBias", ref shadowBias, 0.0001f, 0.0001f, 1f, shadowBias.ToString("f5"));
            ImGui.Combo("ShadowResolution", ref lightShadowResolution, Enum.GetNames(typeof(LightShadowResolutionMode)), 5);
            light.SetColor(color.GetVector3(), intensity);
            light.SetSpecularMultiplier(specularMultiplier);
            light.SetEnabled(enabled);
            light.SetCastShadow(lightCastShadow);
            light.SetShadowBias(shadowBias);
            light.SetShadowResolution((LightShadowResolution)shadowMapSizes[lightShadowResolution]);
            
            switch(light.type) {
                case LightType.Point:
                    ImGui.DragFloat("Rnage", ref range, 0.1f, LightComponent.MinRange, LightComponent.MaxRange);
                    light.SetRange(range);
                    break;
                case LightType.Spot:
                    ImGui.DragFloat("Rnage", ref range, 0.1f, LightComponent.MinRange, LightComponent.MaxRange);
                    ImGui.DragFloat("SpotAngle", ref spotAngle, 0.1f, LightComponent.MinSpotAngle, LightComponent.MaxSpotAngle);
                    ImGui.Text(light.projectorName);
                    ImGui.SameLine();
                    if(ImGui.Button("Set Projector") && myView.editor.fileSystemView.selected != null) {
                        var file = myView.editor.fileSystemView.selected as FileNode;
                        if(file != null) {
                            Vector4 scaleBias = Scene.GlobalScene.FindLightProjector(file.name, out string projectorName);
                            light.SetProjector(projectorName, scaleBias);
                        }
                    }
                    light.SetRange(range);
                    light.SetSpotAngle(spotAngle);
                    break;
            }

            ImGui.PopID();

            if(drawBounds) {
                Vector3 origin = transform.GetLocalPosition();
                Quaternion rotation = transform.GetLocalRotation();
                switch(light.type) {
                    case LightType.Point:
                        if(drawAABB) {
                            Scene.GlobalScene.renderDebug.AddBox(bounds.boundingBox, Quaternion.Identity, Color.Green);
                        }
                        Scene.GlobalScene.renderDebug.AddSphere(16, light.range, origin, Color.Green);
                        break;
                    case LightType.Spot:
                        if(drawAABB) {
                            Scene.GlobalScene.renderDebug.AddBox(bounds.boundingBox, Quaternion.Identity, Color.Green);
                        }
                        if(drawBoundingSphere) {
                            Scene.GlobalScene.renderDebug.AddSphere(16, bounds.boundingSphere.Radius, bounds.boundingSphere.Center, Color.Green);
                        }
                        Scene.GlobalScene.renderDebug.AddSpotCone(16, light.range, light.spotAngle, origin, rotation, Color.Green);
                        Scene.GlobalScene.renderDebug.AddFrustum(0.1f, light.range, MathUtil.DegreesToRadians(light.spotAngle), 1f, origin, transform.GetRight(), transform.GetForward(), transform.GetUp());
                        break;
                }                
            }
        }

        void InspectorDecal() {
            DecalComponent decal = Scene.GlobalScene.decals.GetComponent(entity);
            TransformComponent transform = Scene.GlobalScene.transforms.GetComponent(entity);
            BoundsComponent bounds = Scene.GlobalScene.decalBounds.GetComponent(entity);
            ImGuiVector size = new ImGuiVector(decal.size);
            ImGuiVector baseColor = new ImGuiVector(decal.baseColor);
            ImGuiVector specularColor = new ImGuiVector(decal.specularColor);
            ImGuiVector emissiveColor = new ImGuiVector(decal.emissiveColor);
            float abledoBlendRatio = decal.albedoBlendRatio;
            float specularBlendRatio = decal.specularBlendRatio;
            float smoothnessBlendRatio = decal.smoothnessBlendRatio;
            float normalBlendRatio = decal.normalBlendRatio;
            int normalBlend = (int)decal.normalBlend;
            bool invertNormal = decal.invertNormal;
            bool enabled = decal.IsEnabled();

            ImGui.PushID(GetHashCode());
            if(ImGui.RadioButton("DrawBounds", drawBounds)) {
                drawBounds ^= true;
            }
            if(ImGui.RadioButton("Enabled", enabled)) {
                enabled ^= true;
            }
            if(ImGui.DragFloat3("Size", ref size.vec3, 0.1f, 0.1f, 1000f)) {
                decal.SetSize(size.GetVector3());
            }
            if(ImGui.ColorEdit4("Base Color", ref baseColor.vec4, ImGuiColorEditFlags.HDR | ImGuiColorEditFlags.Float)) {
                decal.SetBaseColor(baseColor.GetColor());
            }
            if(ImGui.ColorEdit4("Specular Color", ref specularColor.vec4, ImGuiColorEditFlags.HDR | ImGuiColorEditFlags.Float)) {
                decal.SetSpecularColor(specularColor.GetColor(), 1f);
            }
            if(ImGui.ColorEdit4("Emissive Color", ref emissiveColor.vec4, ImGuiColorEditFlags.HDR | ImGuiColorEditFlags.Float)) {
                decal.SetEmissiveColor(emissiveColor.GetColor(), 1f);
            }
            if(ImGui.DragFloat("Albedo BlendRatio", ref abledoBlendRatio, 0.1f, 0f, 1f)) {
                decal.SetAlbedoBlendRatio(abledoBlendRatio);
            }  
            if(ImGui.DragFloat("Specular BlendRatio", ref specularBlendRatio, 0.1f, 0f, 1f)) {
                decal.SetSpecularBlendRatio(specularBlendRatio);
            }  
            if(ImGui.DragFloat("Smoothness BlendRatio", ref smoothnessBlendRatio, 0.1f, 0f, 1f)) {
                decal.SetSmoothnessBlendRatio(smoothnessBlendRatio);
            }  
            if(ImGui.DragFloat("Normal BlendRatio", ref normalBlendRatio, 0.1f, 0f, 1f)) {
                decal.SetNormalBlendRatio(normalBlendRatio);
            }  
            if(ImGui.Combo("Normal Blend", ref normalBlend, Enum.GetNames(typeof(DecalComponent.NormalBlend)), 3)) {
                decal.SetNormalBlendMode((DecalComponent.NormalBlend)normalBlend, invertNormal);
            }
            if(ImGui.RadioButton("Invert Normal", invertNormal)) {
                invertNormal ^= true;
                decal.SetNormalBlendMode((DecalComponent.NormalBlend)normalBlend, invertNormal);
            }
            ImGui.Text(decal.albedoImageName);
            ImGui.SameLine();
            if(ImGui.Button("Set Albedo") && myView.editor.fileSystemView.selected != null) {
                var file = myView.editor.fileSystemView.selected as FileNode;
                if(file != null) {
                    Vector4 scaleBias = Scene.GlobalScene.FindDecalImage(file.name, DecalImageType.Albedo, out string albedoImageName);
                    decal.SetAlbedoImage(albedoImageName, scaleBias);
                }
            }
            ImGui.Text(decal.normalImageName);
            ImGui.SameLine();
            if(ImGui.Button("Set Normal") && myView.editor.fileSystemView.selected != null) {
                var file = myView.editor.fileSystemView.selected as FileNode;
                if(file != null) {
                    Vector4 scaleBias = Scene.GlobalScene.FindDecalImage(file.name, DecalImageType.Normal, out string normalImageName);
                    decal.SetNormalImage(normalImageName, scaleBias);
                }
            }
            ImGui.Text(decal.specularImageName);
            ImGui.SameLine();
            if(ImGui.Button("Set Specular") && myView.editor.fileSystemView.selected != null) {
                var file = myView.editor.fileSystemView.selected as FileNode;
                if(file != null) {
                    Vector4 scaleBias = Scene.GlobalScene.FindDecalImage(file.name, DecalImageType.Specular, out string specularImageName);
                    decal.SetSpecularImage(specularImageName, scaleBias);
                }
            }            
            ImGui.PopID();

            if(drawBounds) {
                OrientedBoundingBox obb = new OrientedBoundingBox();
                obb.Extents = decal.size * 0.5f;
                obb.Transformation = transform.localToWorldMatrix;
                Scene.GlobalScene.renderDebug.AddBox(obb, Color.Green);
            }
        }

        void InspectorScatteringVolume() {
            ScatteringVolumeComponent scatteringVolume = Scene.GlobalScene.scatteringVolumes.GetComponent(entity);
            TransformComponent transform = Scene.GlobalScene.transforms.GetComponent(entity);
            BoundsComponent bounds = Scene.GlobalScene.scatteringVolumeBounds.GetComponent(entity);
            bool enabled = scatteringVolume.IsEnabled();
            bool heightFog = scatteringVolume.IsHeightFog();
            ImGuiVector boundSize = new ImGuiVector(scatteringVolume.size);
            ImGuiVector absorption = new ImGuiVector(scatteringVolume.absorption);
            float absorptionIntensity = scatteringVolume.absorptionIntensity;
            float density = scatteringVolume.density;
            float densityFalloff = scatteringVolume.densityFalloff;
            float densityVariationMin = scatteringVolume.densityVariationMin;
            float densityVariationMax = scatteringVolume.densityVariationMax;

            ImGui.PushID(GetHashCode());
            if(ImGui.RadioButton("Enabled", enabled)) {
                enabled ^= true;
                scatteringVolume.SetEnabled(enabled);
            }
            if(ImGui.DragFloat3("Size", ref boundSize.vec3, 0.1f, 0.1f, 1000f)) {
                scatteringVolume.SetSize(boundSize.GetVector3());
            }
            if(ImGui.RadioButton("HeightFog", heightFog)) {
                heightFog ^= true;
                scatteringVolume.SetHeightFog(heightFog);
            }
            if(ImGui.ColorEdit3("Absorption", ref absorption.vec3, ImGuiColorEditFlags.HDR | ImGuiColorEditFlags.Float)) {
                scatteringVolume.SetAbsorption(absorption.GetVector3(), absorptionIntensity);
            }
            if(ImGui.DragFloat("AbsorptionIntensity", ref absorptionIntensity, 0.1f, 0f, 100f)) {
                scatteringVolume.SetAbsorption(absorption.GetVector3(), absorptionIntensity);
            }
            if(ImGui.DragFloat("Density", ref density, 0.001f, 0.00001f, 1f)) {
                scatteringVolume.SetDensity(density, densityFalloff);
            }
            if(ImGui.DragFloat("DensityFalloff", ref densityFalloff, 0.01f, 0f, 1f)) {
                scatteringVolume.SetDensity(density, densityFalloff);
            }
            if(ImGui.DragFloat("DensityVariationMin", ref densityVariationMin, 0.001f, 0.00001f, 1f)) {
                scatteringVolume.SetDensityVariation(densityVariationMin, densityVariationMax);
            }
            if(ImGui.DragFloat("DensityVariationMax", ref densityVariationMax, 0.001f, 0.00001f, 1f)) {
                scatteringVolume.SetDensityVariation(densityVariationMin, densityVariationMax);
            }

            if(heightFog) {
                float densityHeightK = scatteringVolume.densityHeightK;
                if(ImGui.DragFloat("DensityHeightK", ref densityHeightK, 0.01f, 0f, 1f)) {
                    scatteringVolume.SetDensityHeightK(densityHeightK);
                }
            }
            if(ImGui.RadioButton("DrawBounds", drawBounds)) {
                drawBounds ^= true;
            }
            ImGui.PopID();
            if(drawBounds) {
                OrientedBoundingBox obb = new OrientedBoundingBox();
                obb.Extents = scatteringVolume.size * 0.5f;
                obb.Transformation = transform.localToWorldMatrix;
                Scene.GlobalScene.renderDebug.AddBox(obb, Color.Green);
            }            
        }        

        public override void OnInspectorGUI() {
            switch(type) {
                case GameObjectType.Light:
                    InspectorLight();
                    break;
                case GameObjectType.Decal:
                    InspectorDecal();
                    break;
                case GameObjectType.ScatteringVolume:
                    InspectorScatteringVolume();
                    break;
                case GameObjectType.Probe:
                    break;
            }
        }

        public override void OnSceneInspector() {
            
        }
    }

    internal class FileNode : HierarchyNode {
        public enum ChildrenShowFlags {
            None,
            Directories = 1 << 0,
            Files = 1 << 1,
        }

        protected FileInfo fileInfo;
        protected DirectoryInfo directoryInfo;
        protected ChildrenShowFlags childrenShowFlags;

        public FileInfo GetFileInfo() {return fileInfo;}
        public DirectoryInfo GetDirectoryInfo() {return directoryInfo;}

        public FileNode(HierarchyView view, string path, ChildrenShowFlags showFlags) : base(view) {
            if(File.Exists(path)) {
                fileInfo = new FileInfo(path);
                name = fileInfo.Name;
                flags |= ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.NoTreePushOnOpen;
                childrenShowFlags = ChildrenShowFlags.None;
            } else if(Directory.Exists(path)) {
                directoryInfo = new DirectoryInfo(path);
                name = directoryInfo.Name;
                childrenShowFlags = showFlags;
                Open();
                if(children == null) {
                    flags |= ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.NoTreePushOnOpen;
                }
            }
        }

        public FileNode(HierarchyView view, FileInfo file, ChildrenShowFlags showFlags) : base(view) {
            fileInfo = file;
            name = file.Name;
            if((fileInfo.Attributes & FileAttributes.Directory) == 0) {
                flags |= ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.NoTreePushOnOpen;
                childrenShowFlags = ChildrenShowFlags.None;
                fileInfo = file;
            } else {
                childrenShowFlags = showFlags;
                directoryInfo = new DirectoryInfo(file.FullName);
                Open();
                if(children == null) {
                    flags |= ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.NoTreePushOnOpen;
                }
            }
        }

        public FileNode(HierarchyView view, DirectoryInfo dirInfo, ChildrenShowFlags showFlags) : base(view) {
            name = dirInfo.Name;
            directoryInfo = dirInfo;
            childrenShowFlags = showFlags;
            Open();
            if(children == null) {
                flags |= ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.NoTreePushOnOpen;
            }
        }

        protected override void Open() {
            if(children == null && (flags & ImGuiTreeNodeFlags.Leaf) == 0) {
                if(directoryInfo != null) {
                    if((childrenShowFlags & ChildrenShowFlags.Directories) != 0) {
                        var dirs = directoryInfo.GetDirectories();
                        if(dirs != null && dirs.Length > 0) {
                            children = new FileNode[dirs.Length];
                            for(int i = 0; i < children.Length; i++) {
                                children[i] = new FileNode(myView, dirs[i], childrenShowFlags);
                            }
                        } 
                    } else if((childrenShowFlags & ChildrenShowFlags.Files) != 0) {
                        var files = directoryInfo.GetFiles();
                        if(files.Length > 0) {
                            children = new FileNode[files.Length];
                            for(int i = 0; i < children.Length; i++) {
                                children[i] = new FileNode(myView, files[i], childrenShowFlags);
                            }
                        } 
                    } else {
                        var infos = directoryInfo.GetFileSystemInfos();
                        if(infos.Length > 0) {
                            children = new FileNode[infos.Length];
                            for(int i = 0; i < children.Length; i++) {
                                if(infos[i] is DirectoryInfo) {
                                    children[i] = new FileNode(myView, new DirectoryInfo(infos[i].FullName), childrenShowFlags);
                                } else {
                                    children[i] = new FileNode(myView, new FileInfo(infos[i].FullName), childrenShowFlags);
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    internal class FileSystemView : HierarchyView {
        FileNode root;
        public FileSystemView(Editor editor, string basePath) : base(editor, "FileSystem") {
            root = new FileNode(this, basePath, FileNode.ChildrenShowFlags.None);
        }

        public override void Update() {
            base.Update();
            root.OnHierarchyGUI();
        }
    }

    internal abstract class HierarchyView {
        public HierarchyNode selected;
        public readonly Editor editor;
        public string name {get; private set;}

        public HierarchyView(Editor editor, string inName) {
            this.editor = editor;
            name = inName;
        }

        public virtual void Update() {
            if(selected != null) {
                ImGui.PushID(selected.id);
                if(ImGui.BeginPopupContextWindow()) {
                    selected.OnContextMenuGUI();
                    ImGui.EndPopup();
                }
                ImGui.PopID();
            }            
        }
    }
}