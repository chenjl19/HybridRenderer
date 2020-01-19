using System;
using System.IO;
using System.Collections.Generic;
using SharpDX;
using ImGuiNET;

namespace NT
{
    public class TreeNode {
        internal string name;
        internal ImGuiTreeNodeFlags nodeFlags;
    }

    public class TreeNode_GameObject : TreeNode {
        internal delegate void OnInspector(Editor editor, Entity entity);

        internal Entity entity;
        internal OnInspector onInspector;

        public TreeNode_GameObject(Entity _entity) {
            entity = _entity;
            onInspector = null;
            nodeFlags = ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.OpenOnDoubleClick;
        }

        internal void Destroy(Editor editor) {
            if(entity.IsValid()) {
                Scene scene = editor.scene;
                scene.Remove(entity);
                entity = Entity.Invalid();
            }
            onInspector = null;
        }

        internal static void InspectorName(Editor editor, Entity entity) {
            Scene scene = editor.scene;
            NameComponent nameComponent = scene.names.GetComponent(entity);
            ImGui.Text(nameComponent.name);
            ImGui.Spacing();
        }

        internal static void InspectorTransform(Editor editor, Entity entity) {
            Scene scene = editor.scene;
            TransformComponent transform = scene.transforms.GetComponent(entity);
            ImGuiVector v = transform.GetLocalPosition();
            if(ImGui.DragFloat3("Position", ref v.vec3, 0.1f)) {
                transform.SetLocalPosition(v.GetVector3());
            }
            v = transform.localAngles;
            if(ImGui.DragFloat3("Angles", ref v.vec3, 0.1f)) {
                transform.SetLocalRotation(v.GetVector3());
            }
            v = transform.GetLocalScale();
            if(ImGui.DragFloat3("Scale", ref v.vec3, 0.1f)) {
                transform.SetLocalScale(v.GetVector3());
            }
            ImGui.Spacing();
        }

        static bool drawAxis = true;
        static bool drawBounds = true;
        static bool drawDebugMesh = false;
        static bool drawAABB;
        static bool drawBoundingSphere;
        internal static void InspectorMesh(Editor editor, Entity entity) {
            Scene scene = editor.scene;

            MeshComponent mesh = scene.meshes.GetComponent(entity);
            if(ImGui.RadioButton("DrawBounds", drawBounds)) {
                drawBounds ^= true;
            }

            ImGuiVector boundsCenter = mesh.localBoundingBox.Center;
            ImGuiVector boundsExtents = mesh.localBoundingBox.Size * 0.5f;
            ImGui.DragFloat3("Bounds Center", ref boundsCenter.vec3, 0.1f, -99999f, 99999f);
            ImGui.DragFloat3("Bounds Extents", ref boundsExtents.vec3, 0.1f, -99999f, 99999f);
            mesh.localBoundingBox = new BoundingBox(boundsCenter.GetVector3() - boundsExtents.GetVector3(), boundsCenter.GetVector3() + boundsExtents.GetVector3());

            if(drawBounds) {
                TransformComponent transform = null;
                if(mesh.skeletonID.IsValid()) {
                    transform = scene.transforms.GetComponent(mesh.rootJoint);
                } else {
                    transform = scene.transforms.GetComponent(entity);
                }
                BoundingBox worldBounds = MathHelper.TransformBoundingBox(mesh.localBoundingBox, transform.localToWorldMatrix);
                scene.renderDebug.AddBox(worldBounds, Quaternion.Identity, Color.Blue);
            }
            ImGui.Spacing();
        }

        static readonly int[] shadowMapSizes = {
            128, 256, 512, 1024, 2048
        };

        internal static void InspectorLight(Editor editor, Entity entity) {
            Scene scene = editor.scene;
            LightComponent light = scene.lights.GetComponent(entity);
            TransformComponent transform = scene.transforms.GetComponent(entity);
            BoundsComponent bounds = scene.lightBounds.GetComponent(entity);
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
                    if(ImGui.Button("Set Projector") && editor.fileSystemView.selected != null) {
                        var file = editor.fileSystemView.selected as FileNode;
                        if(file != null) {
                            Vector4 scaleBias = Scene.GlobalScene.FindLightProjector(file.name, out string projectorName);
                            light.SetProjector(projectorName, scaleBias);
                        }
                    }
                    light.SetRange(range);
                    light.SetSpotAngle(spotAngle);
                    break;
            }

            //ImGui.PopID();

            if(drawBounds) {
                Vector3 origin = transform.GetPosition();
                Quaternion rotation = transform.GetRotation();
                switch(light.type) {
                    case LightType.Point:
                        if(drawAABB) {
                            scene.renderDebug.AddBox(bounds.boundingBox, Quaternion.Identity, Color.Green);
                        }
                        scene.renderDebug.AddSphere(16, light.range, origin, Color.Green);
                        break;
                    case LightType.Spot:
                        if(drawAABB) {
                            scene.renderDebug.AddBox(bounds.boundingBox, Quaternion.Identity, Color.Green);
                        }
                        if(drawBoundingSphere) {
                            scene.renderDebug.AddSphere(16, bounds.boundingSphere.Radius, bounds.boundingSphere.Center, Color.Green);
                        }
                        scene.renderDebug.AddSpotCone(16, light.range, light.spotAngle, origin, rotation, Color.Green);
                        scene.renderDebug.AddFrustum(0.1f, light.range, MathUtil.DegreesToRadians(light.spotAngle), 1f, origin, transform.GetRight(), transform.GetForward(), transform.GetUp());
                        break;
                }                
            }
        }

        internal static void InspectorReflectionProbe(Editor editor, Entity entity) {
            Scene scene = editor.scene;

            ReflectionProbeComponent probe = scene.reflectionProbes.GetComponent(entity);
            BoundsComponent bounds = scene.reflectionProbeBounds.GetComponent(entity);
            TransformComponent transform = scene.transforms.GetComponent(entity);
            bool enabled = probe.IsEnabled();

            //ImGui.PushID(GetHashCode());
            if(ImGui.RadioButton("Enabled", enabled)) {
                enabled ^= true;
                probe.SetEnabled(enabled);
            }
            if(ImGui.RadioButton("DrawAxis", drawAxis)) {
                drawAxis ^= true;
            }
            if(ImGui.RadioButton("DrawBounds", drawBounds)) {
                drawBounds ^= true;
            }
            if(ImGui.RadioButton("DrawDebugMesh", drawDebugMesh)) {
                drawDebugMesh ^= true;
                MeshComponent mesh = scene.meshes.GetComponent(entity);
                if(mesh != null) {
                    mesh.SetEnabled(drawDebugMesh);
                }
            }
            ImGuiVector boundsCenter = probe.localBounds.Center;
            ImGuiVector boundsExtents = probe.localBounds.Size * 0.5f;
            ImGui.DragFloat3("Bounds Center", ref boundsCenter.vec3, 0.1f, -99999f, 99999f);
            ImGui.DragFloat3("Bounds Extents", ref boundsExtents.vec3, 0.1f, -99999f, 99999f);
            float intensity = probe.specularMultiplier;
            if(ImGui.DragFloat("Intensity", ref intensity, 0.1f, 0f, 1000f)) {
                probe.SetSpecularMultiplier(intensity);
            }
            float innerFalloff = probe.innerFalloff;
            if(ImGui.DragFloat("InnerFalloff", ref innerFalloff, 0.01f, 0f, 1f)) {
                probe.SetInnerFalloff(innerFalloff);
            }
            float fade = probe.distanceFade;
            if(ImGui.DragFloat("Fade In/Out", ref fade, 0.01f)) {
                probe.SetDistanceFade(fade);
            }
            probe.SetBounds(boundsCenter.GetVector3() - boundsExtents.GetVector3(), boundsCenter.GetVector3() + boundsExtents.GetVector3());

            if(ImGui.Button("Bake")) {
                //probe.SetBaked();
                //scene.ClearBakedProbes();
            }

            //ImGui.PopID();
            if(drawAxis) {
                scene.renderDebug.AddAxis(transform.GetLocalPosition(), Quaternion.Identity, 2f);
            }
            if(drawBounds) {
                scene.renderDebug.AddBox(bounds.boundingBox, Quaternion.Identity, Color.Green);
            }                      
        }

        internal static void InspectorScatteringVolume(Editor editor, Entity entity) {
            Scene scene = editor.scene;

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

            //ImGui.PushID(GetHashCode());
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
            //ImGui.PopID();
            if(drawBounds) {
                OrientedBoundingBox obb = new OrientedBoundingBox();
                obb.Extents = scatteringVolume.size * 0.5f;
                obb.Transformation = transform.localToWorldMatrix;
                Scene.GlobalScene.renderDebug.AddBox(obb, Color.Green);
            }            
        } 

        internal static void InspectorDecal(Editor editor, Entity entity) {
            Scene scene = editor.scene;

            DecalComponent decal = scene.decals.GetComponent(entity);
            TransformComponent transform = scene.transforms.GetComponent(entity);
            BoundsComponent bounds = scene.decalBounds.GetComponent(entity);
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

            //ImGui.PushID(GetHashCode());
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
            if(ImGui.Button("Set Albedo") && editor.fileSystemView.selected != null) {
                var file = editor.fileSystemView.selected as FileNode;
                if(file != null) {
                    Vector4 scaleBias = Scene.GlobalScene.FindDecalImage(file.name, DecalImageType.Albedo, out string albedoImageName);
                    decal.SetAlbedoImage(albedoImageName, scaleBias);
                }
            }
            ImGui.Text(decal.normalImageName);
            ImGui.SameLine();
            if(ImGui.Button("Set Normal") && editor.fileSystemView.selected != null) {
                var file = editor.fileSystemView.selected as FileNode;
                if(file != null) {
                    Vector4 scaleBias = Scene.GlobalScene.FindDecalImage(file.name, DecalImageType.Normal, out string normalImageName);
                    decal.SetNormalImage(normalImageName, scaleBias);
                }
            }
            ImGui.Text(decal.specularImageName);
            ImGui.SameLine();
            if(ImGui.Button("Set Specular") && editor.fileSystemView.selected != null) {
                var file = editor.fileSystemView.selected as FileNode;
                if(file != null) {
                    Vector4 scaleBias = Scene.GlobalScene.FindDecalImage(file.name, DecalImageType.Specular, out string specularImageName);
                    decal.SetSpecularImage(specularImageName, scaleBias);
                }
            }            
            //ImGui.PopID();

            if(drawBounds) {
                OrientedBoundingBox obb = new OrientedBoundingBox();
                obb.Extents = decal.size * 0.5f;
                obb.Transformation = transform.localToWorldMatrix;
                Scene.GlobalScene.renderDebug.AddBox(obb, Color.Green);
            }            
        }       

        public static implicit operator Entity(TreeNode_GameObject node) {
            return node.entity;
        }
    }

    public class Level {
        internal readonly Scene scene;
        public string name {get; private set;}
        public Material skyboxMaterial {get; private set;}
        public Entity environmentProbe {get; private set;}
        Image2D[] lightmapDirMaps;
        Image2D[] lightmapColorMaps;
        public List<Hierarchy<TreeNode_GameObject>> actors {get; private set;}

        internal Level(Scene myScene) {
            scene = myScene;
            actors = new List<Hierarchy<TreeNode_GameObject>>();
        }

        struct ParseContext {
            public Lexer src;
            public Token token;
            public Hierarchy<TreeNode_GameObject>[] entities;
        }

        void ParseTransform(int entityIndex, ref ParseContext context) {
            context.src.ExpectTokenString("{");
            TransformComponent transform = scene.transforms.Create(context.entities[entityIndex].owner);
            transform.flags |= TransformComponent.Flags.CacheLocalAngles;
            while(context.src.ReadToken(ref context.token)) {
                if(context.token == "}") {
                    break;
                }
                if(context.token == "localScale") {
                    Vector3 localScale = Vector3.One;
                    context.src.ParseCSharpVector(ref localScale);
                    transform.SetLocalScale(localScale);
                    continue;
                }
                if(context.token == "localRotation") {
                    //Quaternion localRotation = Quaternion.Identity;
                    //context.src.ParseCSharpVector(ref localRotation);
                    //transform.SetLocalRotation(localRotation);
                    Vector3 right = MathHelper.Vec3Right;
                    Vector3 forward = MathHelper.Vec3Forward;
                    Vector3 up = MathHelper.Vec3Up;
                    context.src.ParseCSharpVector(ref right);
                    context.src.ParseCSharpVector(ref forward);
                    context.src.ParseCSharpVector(ref up);
                    transform.SetAxis(right, forward, up);
                    continue;
                }
                if(context.token == "localPosition") {
                    Vector3 localPosition = Vector3.Zero;
                    context.src.ParseCSharpVector(ref localPosition);
                    transform.SetLocalPosition(localPosition);
                    continue;
                }
            }
            context.entities[entityIndex].owner.onInspector += TreeNode_GameObject.InspectorTransform;
        }

        void ParseLight(int entityIndex, ref ParseContext context) {
            context.src.ExpectTokenString("{");

            bool enabled = true;
            bool castShadow = false;
            float intensity = 1f;
            float range = 1f;
            float spotAngle = 30f;
            Color color = Color.White;
            LightType type = LightType.Point;

            while(context.src.ReadToken(ref context.token)) {
                if(context.token == "}") {
                    break;
                }
                if(context.token == "enabled") {
                    enabled = context.src.ParseBool();
                    continue;
                }
                if(context.token == "castShadow") {
                    castShadow = context.src.ParseBool();
                    continue;
                }
                if(context.token == "type") { 
                    context.src.ReadTokenOnLine(ref context.token);
                    if(Enum.TryParse(typeof(LightType), context.token.lexme, true, out var lightType)) {
                        type = (LightType)lightType;
                    }          
                    continue;
                }
                if(context.token == "color") {
                    context.src.ParseCSharpColor(ref color);
                    continue;
                }
                if(context.token == "intensity") {
                    intensity = context.src.ParseFloat();
                    continue;
                }
                if(context.token == "range") {
                    range = context.src.ParseFloat();
                    continue;
                }
                if(context.token == "spotAngle") {
                    spotAngle = context.src.ParseFloat();
                    continue;
                }
            }

            LightComponent lightComponent = type == LightType.Directional ? scene.directionalLights.Create(context.entities[entityIndex].owner) : scene.lights.Create(context.entities[entityIndex].owner);
            lightComponent.SetType(type);
            lightComponent.SetEnabled(enabled);
            lightComponent.SetCastShadow(castShadow);
            lightComponent.SetColor(color.ToVector3(), intensity);
            lightComponent.SetRange(range);
            lightComponent.SetSpotAngle(spotAngle);
            lightComponent.SetSpecularMultiplier(1f);
            if(type != LightType.Directional) {
                scene.lightBounds.Create(context.entities[entityIndex].owner);
            }

            if(lightComponent.type == LightType.Directional) {
                scene.SetSunSource(context.entities[entityIndex].owner);
            }

            TransformComponent transform = scene.transforms.GetComponent(context.entities[entityIndex].owner);
            transform.flags |= TransformComponent.Flags.CacheGlobal;

            context.entities[entityIndex].owner.onInspector += TreeNode_GameObject.InspectorLight;
        }

        void ParseMesh(int entityIndex, ref ParseContext context) {
            context.src.ExpectTokenString("{");
            MeshComponent meshComponent = scene.meshes.Create(context.entities[entityIndex].owner);
            Material[] materials = null;
            Entity[] joints = null;
            string modelAssetName = string.Empty;
            BoundingBox localBounds = MathHelper.NullBoundingBox;
            string lightmapDir = string.Empty;
            string lightmapColor = string.Empty;
            int lightmapIndex = -1;
            Vector4 lightmapScaleOffset = Vector4.Zero;
            bool enabled = true;
            while(context.src.ReadToken(ref context.token)) {
                if(context.token == "}") {
                    break;
                }
                if(context.token == "enabled") {
                    enabled = context.src.ParseBool();
                    continue;
                }
                if(context.token == "asset" && string.IsNullOrEmpty(modelAssetName)) {
                    context.src.ReadTokenOnLine(ref context.token);
                    modelAssetName = $"models/{context.token.lexme}.tmdl";                  
                    continue;
                }
                if(context.token == "joints" && joints == null) {
                    context.src.ExpectTokenString("{");
                    context.src.ExpectTokenString("numJoints");
                    int numJoints = context.src.ParseInteger();
                    joints = new Entity[numJoints];
                    for(int i = 0; i < numJoints; i++) {
                        int jointIndex = context.src.ParseInteger();
                        if(context.entities[jointIndex] == null) {
                            context.entities[jointIndex] = new Hierarchy<TreeNode_GameObject>(new TreeNode_GameObject(Entity.Create()));
                        }
                        joints[i] = context.entities[jointIndex].owner;
                    }
                    context.src.ExpectTokenString("}");
                    continue;
                }
                if(context.token == "materials" && materials == null) {
                    context.src.ExpectTokenString("{");
                    context.src.ExpectTokenString("numMaterials");
                    materials = new Material[context.src.ParseInteger()];
                    int numMateirals = 0;
                    while(context.src.ReadToken(ref context.token)) {
                        if(context.token == "}") {
                            break;
                        }
                        materials[numMateirals] = Common.declManager.FindMaterial(context.token.lexme);
                        numMateirals++;                        
                    }
                    continue;
                }
                if(context.token == "lightmapIndex") {
                    lightmapIndex = context.src.ParseInteger();
                    continue;
                }
                if(context.token == "lightmapScaleOffset") {
                    context.src.ParseCSharpVector(ref lightmapScaleOffset);
                    continue;
                }
                if(context.token == "localBounds") {
                    context.src.ParseCSharpVector(ref localBounds.Minimum);
                    context.src.ParseCSharpVector(ref localBounds.Maximum);
                }
            }

            if(!scene.meshRenderSystem.FindRuntimeMesh(modelAssetName, out var runtimeMesh)) {
                Mesh meshData = AssetLoader.LoadTextModel(Path.Combine(FileSystem.assetBasePath, modelAssetName));
                if(meshData == null) {
                    throw new InvalidDataException("meshData is null.");
                }
                meshComponent.SetRenderData(meshData, materials, meshComponent.IsLightmapStatic() ? typeof(DrawVertexStaticPackedVertexFactory) : typeof(DrawVertexPackedVertexFactory), modelAssetName);
            } else {
                meshComponent.SetRenderData(runtimeMesh, materials);
            }

            if(joints != null) {
                scene.CreateSkeleton(context.entities[entityIndex].owner, joints, meshComponent.runtimeMesh.Item3, out var rootJoint);
                meshComponent.SetSkeleton(context.entities[entityIndex].owner, rootJoint);
                meshComponent.SetLocalBounds(localBounds);
                // DrawVertexPacked
                meshComponent.numPositionStreams = 2;
            } else {
                if(lightmapScaleOffset == Vector4.Zero) {
                    var name = scene.names.GetComponent(context.entities[entityIndex].owner);
                    int fuck = 0;
                }
                if(lightmapIndex >= 0 && lightmapIndex < lightmapDirMaps.Length && lightmapScaleOffset != Vector4.Zero) {
                    meshComponent.UpdateLightmapData(lightmapIndex, lightmapScaleOffset);
                }
                // DrawVertexPacked0
                meshComponent.numPositionStreams = 1;
            }
            meshComponent.SetCastShadow(true);
            meshComponent.SetEnabled(enabled);
            TransformComponent transform = scene.transforms.GetComponent(context.entities[entityIndex].owner);
            transform.flags |= TransformComponent.Flags.CacheInverted;

            context.entities[entityIndex].owner.onInspector += TreeNode_GameObject.InspectorMesh;
        }

        void ParseReflectionProbe(int entityIndex, ref ParseContext context) {
            context.src.ExpectTokenString("{");
            string assetName = string.Empty;
            Vector3 boxMin = Vector3.Zero;
            Vector3 boxMax = Vector3.Zero;
            float intensity = 1f;
            while(context.src.ReadToken(ref context.token)) {
                if(context.token == "}") {
                    break;
                }

                if(context.token == "texture") {
                    context.src.ReadTokenOnLine(ref context.token); 
                    assetName = context.token.lexme;
                    continue;
                }
                if(context.token == "bounds") {
                    context.src.ParseCSharpVector(ref boxMin);
                    context.src.ParseCSharpVector(ref boxMax);
                    continue;
                }   
                if(context.token == "intensity") {
                    intensity = context.src.ParseFloat();
                }          
            }
            if(!string.IsNullOrEmpty(assetName)) {
                Entity entity = context.entities[entityIndex].owner;
                ReflectionProbeComponent reflectionProbeComponent = scene.reflectionProbes.Create(entity);
                reflectionProbeComponent.SetSpecularMultiplier(intensity);
                reflectionProbeComponent.SetBounds(boxMin, boxMax);
                scene.reflectionProbeBounds.Create(entity);
                TransformComponent transform = scene.transforms.GetComponent(context.entities[entityIndex].owner);
                transform.flags |= TransformComponent.Flags.CacheGlobal;
                scene.AddReflectionProbeCubemap(entity, $"textures/gi/{name}/{assetName}.bimg");

                context.entities[entityIndex].owner.onInspector += TreeNode_GameObject.InspectorReflectionProbe;
            }
        }

        void ParseDecal(int entityIndex, ref ParseContext context) {

        }

        void ParseScatteringVolume(int entityIndex, ref ParseContext context) {

        }

        void ParseEntityDefine(int entityIndex, ref ParseContext context) {
            context.src.ExpectTokenString("{");
            if(context.entities[entityIndex] == null) {
                context.entities[entityIndex] = new Hierarchy<TreeNode_GameObject>(new TreeNode_GameObject(Entity.Create()));
            }
            NameComponent nameComponent = scene.names.Create(context.entities[entityIndex].owner);
            while(context.src.ReadToken(ref context.token)) {
                if(context.token == "}") {
                    break;
                }
                if(context.token == "name") {
                    context.src.ReadTokenOnLine(ref context.token);
                    nameComponent.name = context.token.lexme;
                    context.entities[entityIndex].owner.onInspector += TreeNode_GameObject.InspectorName;
                    continue;
                }
                if(context.token == "isStatic") {
                    nameComponent.isStatic = context.src.ParseBool();
                }
                if(context.token == "transform") {
                    ParseTransform(entityIndex, ref context);
                    continue;
                }
                if(context.token == "mesh") {
                    ParseMesh(entityIndex, ref context);
                    continue;
                }
                if(context.token == "light") {
                    ParseLight(entityIndex, ref context);
                    continue;
                }
                if(context.token == "reflectionProbe") {
                    ParseReflectionProbe(entityIndex, ref context);
                    continue;
                }
            }
            context.entities[entityIndex].owner.name = nameComponent.name;

            TransformComponent transform = scene.transforms.GetComponent(context.entities[entityIndex].owner);
            if(nameComponent.isStatic) {
                transform.SetStatic();
            }
        }

        public Entity ParseActor(string assetName) {
            string assetFullpath = Path.Combine(FileSystem.assetBasePath, assetName);
            if(!File.Exists(assetFullpath)) {
                return Entity.Invalid();
            }

            Token token = new Token();
            Lexer lex = new Lexer(File.ReadAllText(assetFullpath), Lexer.Flags.NoStringConcat);

            lex.ExpectTokenString("actor");
            lex.ReadTokenOnLine(ref token);
            lex.ExpectTokenString("{");

            lex.ExpectTokenString("version");
            lex.ExpectTokenString("1");
            lex.ExpectTokenString("numGameObjects");
            int numGameObjects = lex.ParseInteger();
            string actorName = token.lexme;
            ParseContext context = new ParseContext();
            context.src = lex;
            context.token = token;
            context.entities = new Hierarchy<TreeNode_GameObject>[numGameObjects];
            for(int i = 0; i < numGameObjects; i++) {
                ParseEntityDefine(i, ref context);
            }

            lex.ExpectTokenString("hierarchy");
            lex.ExpectTokenString("{");
            int entityIndex = 0;
            while(lex.ReadToken(ref token)) {
                if(token == "}") {
                    break;
                }
                int parentIndex = int.Parse(token.lexme);
                context.entities[entityIndex + 1].ParentTo(context.entities[parentIndex]);
                scene.Attach(context.entities[entityIndex + 1].owner, context.entities[parentIndex].owner);
                entityIndex++;
            }

            lex.ExpectTokenString("}");

            actors.Add(context.entities[0]);

            return context.entities[0].owner;
        }

        void ParseActors(ref Lexer src, ref Token token) {
            src.ExpectTokenString("{");
            while(src.ReadToken(ref token)) {
                if(token == "}") {
                    break;
                }
                ParseActor($"actors/{token.lexme}.tactor");
            }
        }

        void ParseLightmaps(ref Lexer src, ref Token token) {
            src.ExpectTokenString("{");
            int numLightmaps = src.ParseInteger();
            lightmapDirMaps = new Image2D[numLightmaps];
            lightmapColorMaps = new Image2D[numLightmaps];
            int i = 0;
            while(src.ReadToken(ref token)) {
                if(token == "}") {
                    break;
                }
                if(token != "{") {
                    break;
                }

                src.ExpectTokenString("LightmapDir");
                src.ReadTokenOnLine(ref token);
                lightmapDirMaps[i] = ImageManager.Image2DFromFile(Path.Combine(FileSystem.assetBasePath, $"textures/gi/{name}/{token.lexme}.bimg"));
                src.ExpectTokenString("LightmapColor");
                src.ReadTokenOnLine(ref token);
                lightmapColorMaps[i] = ImageManager.Image2DFromFile(Path.Combine(FileSystem.assetBasePath, $"textures/gi/{name}/{token.lexme}.bimg"));
                src.ExpectTokenString("}");
                ++i;
            }
        }

        void ParseAmbientProbe(ref Lexer src, ref Token token) {
            src.ExpectTokenString("{");
            while(src.ReadToken(ref token)) {
                if(token == "}") {
                    break;
                }
            }
        }

        void ParseGI(ref Lexer src, ref Token token) {
            string skybox = string.Empty;
            string environmentProbe = string.Empty;
            Color skyboxTint = Color.White;
            float skyboxExposure = 1f;
            float skyboxRotation = 1f;
            src.ExpectTokenString("{");
            while(src.ReadToken(ref token)) {
                if(token == "}") {
                    break;
                }
                if(token == "skybox") {
                    src.ReadTokenOnLine(ref token);
                    skybox = token.lexme;
                    continue;
                }
                if(token == "skyboxTint") {
                    src.ParseCSharpColor(ref skyboxTint);
                    continue;
                }
                if(token == "skyboxExposure") {
                    skyboxExposure = src.ParseFloat();
                    continue;
                }
                if(token == "skyboxRotation") {
                    skyboxRotation = src.ParseFloat();
                    continue;
                }
                if(token == "environmentProbe") {
                    src.ReadTokenOnLine(ref token);
                    environmentProbe = token.lexme;
                    continue;
                }
                if(token == "ambientProbe") {
                    ParseAmbientProbe(ref src, ref token);
                    continue;
                }
                if(token == "lightmaps") {
                    ParseLightmaps(ref src, ref token);
                    continue;
                }
            } 
            if(!string.IsNullOrEmpty(skybox)) {
                if(skyboxMaterial == null) {
                    skyboxMaterial = new Material(Common.declManager.FindShader("builtin/skybox"));
                }
                skyboxMaterial.SetFloat4("_Tint", skyboxTint.ToVector4());
                skyboxMaterial.SetFloat4("_Parms", new Vector4(skyboxExposure, skyboxRotation, 0f, 0f));
                scene.SetSkybox(skyboxMaterial, $"textures/{skybox}.bimg");
            }   
            if(!string.IsNullOrEmpty(environmentProbe)) {

                //scene.SetEnvironmentProbe($"textures/gi/{name}/{environmentProbe}.bimg");
            }        
        }

        public void ParseLevel(string assetName) {
            string assetFullpath = Path.Combine(FileSystem.assetBasePath, assetName);
            if(!File.Exists(assetFullpath)) {
                return;
            }          

            Token token = new Token();
            Lexer src = new Lexer(File.ReadAllText(assetFullpath), Lexer.Flags.NoStringConcat);

            src.ReadToken(ref token);
            name = token.lexme;
            src.ExpectTokenString("{");
            src.ExpectTokenString("GI");
            ParseGI(ref src, ref token);
            src.ExpectTokenString("actors");
            ParseActors(ref src, ref token);
            src.ExpectTokenString("}");

            scene.SetGIData(lightmapDirMaps, lightmapColorMaps);
        }
    }
}