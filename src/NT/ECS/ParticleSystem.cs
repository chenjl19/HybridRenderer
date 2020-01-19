using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using SharpDX;

namespace NT
{
    public struct ParticleUpdateInfo {
        public int particleIndex;
        public float frac;
        public idRandom random;
        public float ageInSeconds;
        public idRandom originalRandom;
        public float animationFrameFrac;
    }

    public enum ParticleStageShape {
        Rectangle,
        Cylinder,
        Sphere
    }

    public enum ParticleStageDirection {
        Cone,
        Outward
    }

    public enum ParticleStageOrientation {
        View,
        X,
        Y,
        Z,
        Aimed,
    }

    public enum ParticleStagePath {
        Standard,
        Helix,
        Flies,
        Orbit,
        Drip
    }

    public class ParticleStage {
        public int maxParticles;
        public float cycles;
        public float spawnBunching;
        public float particleLife;
        public float timeOffset;
        public float deadTime;
        public float gravity;
        public bool worldGravity;
        public float fadeInFraction;
        public float fadeOutFraction;
        public float softParticleAlphaScale;
        public float fadeIndexFraction;
        public float initialAngle;
        public bool randomDistribution;
        public int cycleMsec; // particleLife + deadTime
        public Vector3 offset;
        public Vector2 speedRange;
        public Vector2 rotationSpeedRange;
        public Vector4 sizeRange;
        public ParticleStageShape shape;
        public Vector4 shapeParms;
        public ParticleStageOrientation orientation;
        public Vector4 orientationParms;
        public ParticleStageDirection direction;
        public ParticleStagePath path;
        public float[] pathParms;
        public Vector4 directionParms;
        public Color initialColor;
        public Color finalColor;
        public Color fadeColor;
        public string textureAssetName;
        public int numAnimationFrameRows;
        public int numAnimationFrameColumns;
        public int startFrame;
        public float animationRate; // frames per second
        public float boundsExpansion;
        public BoundingBox boundingBox;

        public void ParticleOrigin(ref RenderView renderView, ref ParticleUpdateInfo info, out Vector3 origin) {
            origin = Vector3.Zero;
            
            if(path == ParticleStagePath.Standard) {
                switch(shape) {
                    case ParticleStageShape.Rectangle: {
                        origin.X = (randomDistribution ? info.random.CNextFloat() : 1f) * shapeParms.X;
                        origin.Y = (randomDistribution ? info.random.CNextFloat() : 1f) * shapeParms.Y;
                        origin.Z = (randomDistribution ? info.random.CNextFloat() : 1f) * shapeParms.Z;
                        break;
                    }
                    case ParticleStageShape.Cylinder: {
                        break;
                    }
                    case ParticleStageShape.Sphere: {
                        break;
                    }
                }
                origin += offset;

                Vector3 dir = Vector3.Zero;
                switch(direction) {
                    case ParticleStageDirection.Cone: {
                        float angle1 = MathUtil.DegreesToRadians(info.random.CNextFloat() * directionParms[0]);
                        float angle2 = info.random.CNextFloat() * MathF.PI;
                        MathHelper.SinCos16(angle1, out float s1, out float c1);
                        MathHelper.SinCos16(angle2, out float s2, out float c2);
                        dir.X = s1 * c2;
                        dir.Y = s1 * s2;
                        dir.Z = c1;
                        break;
                    }
                    case ParticleStageDirection.Outward: {
                        dir = Vector3.Normalize(origin);
                        dir.Z += directionParms.X;
                        break;
                    }
                }

                float speed = MathHelper.Integrate(speedRange, info.frac);
                origin += dir * speed * particleLife;
            } else {
                switch(path) {
                    case ParticleStagePath.Drip: {
                        origin.X = 0f;
                        origin.Y = 0f;
                        origin.Z = -(info.ageInSeconds * pathParms[0]);
                        break;
                    }
                }
            }

            if(worldGravity) {

            } else {
                origin.Z -= gravity * info.ageInSeconds * info.ageInSeconds;
            }
        }

        public int ParticleVertices(ref RenderView renderView, ref ParticleUpdateInfo info, Vector3 origin, ref Span<ParticleVertex> vertices, int offset) {
            Vector2 size = Vector2.Lerp(new Vector2(sizeRange.X, sizeRange.Y), new Vector2(sizeRange.Z, sizeRange.W), info.frac);

            Vector3 right = MathHelper.Vec3Right;
            Vector3 up = MathHelper.Vec3Up;

            if(orientation == ParticleStageOrientation.View) {
                right = renderView.right;
                up = renderView.up;
            }

            right *= size.X;
            up *= size.Y;
            vertices[offset + 0].position = origin - right + up;
            vertices[offset + 1].position = origin + right + up;
            vertices[offset + 2].position = origin - right - up;
            vertices[offset + 3].position = origin + right - up;

            return 4;
        }

        Vector2 AnimateTexCoord(Vector2 texCoord, float frame, float numFrames, float numColumns, float numRows, bool useRandomRow, float randomValue) {
            float columnFrame = MathF.Floor(MathUtil.Mod(frame, numColumns));
            float rowFrame = useRandomRow ? MathF.Floor(randomValue * numFrames - 1e-6f) : MathF.Floor(MathUtil.Mod(frame, numFrames) / numColumns);
            return new Vector2((texCoord.X + columnFrame) / numColumns, (texCoord.Y + rowFrame ) / numRows);
        }

        public void ParticleTexcoords(ref ParticleUpdateInfo info, ref Span<ParticleVertex> vertices, int offset) {
            float frame = 0f;
            float frameFrac = 0f;
            int numAnimationFrames = numAnimationFrameRows * numAnimationFrameColumns;
            if(numAnimationFrames > 1) {
                frame += info.frac * numAnimationFrames;
                frameFrac = frame - ((int)frame);
            } else {

            }

            vertices[offset + 0].st0 = AnimateTexCoord(Vector2.Zero, frame, numAnimationFrames, numAnimationFrameColumns, numAnimationFrameRows, false, 0f);
            vertices[offset + 1].st0 = AnimateTexCoord(new Vector2(1f, 0f), frame, numAnimationFrames, numAnimationFrameColumns, numAnimationFrameRows, false, 0f);
            vertices[offset + 2].st0 = AnimateTexCoord(new Vector2(0f, 1f), frame, numAnimationFrames, numAnimationFrameColumns, numAnimationFrameRows, false, 0f);
            vertices[offset + 3].st0 = AnimateTexCoord(new Vector2(1f, 1f), frame, numAnimationFrames, numAnimationFrameColumns, numAnimationFrameRows, false, 0f);
            vertices[offset + 0].st1 = AnimateTexCoord(Vector2.Zero, frame + 1, numAnimationFrames, numAnimationFrameColumns, numAnimationFrameRows, false, 0f);
            vertices[offset + 1].st1 = AnimateTexCoord(new Vector2(1f, 0f), frame + 1, numAnimationFrames, numAnimationFrameColumns, numAnimationFrameRows, false, 0f);
            vertices[offset + 2].st1 = AnimateTexCoord(new Vector2(0f, 1f), frame + 1, numAnimationFrames, numAnimationFrameColumns, numAnimationFrameRows, false, 0f);
            vertices[offset + 3].st1 = AnimateTexCoord(new Vector2(1f, 1f), frame + 1, numAnimationFrames, numAnimationFrameColumns, numAnimationFrameRows, false, 0f);
            
            vertices[offset + 0].color1 = new Color(frameFrac, 0f, 0f, 0f);
            vertices[offset + 1].color1 = new Color(frameFrac, 0f, 0f, 0f);
            vertices[offset + 2].color1 = new Color(frameFrac, 0f, 0f, 0f);
            vertices[offset + 3].color1 = new Color(frameFrac, 0f, 0f, 0f);

        }

        public bool ParticleColors(ref ParticleUpdateInfo info, ref Span<ParticleVertex> vertices, int offset) {
            /*
            float fadeFraction = 1f;
            if(info.frac < fadeInFraction) {
                fadeFraction *= info.frac / fadeInFraction;
            }
            if((1f - info.frac) < fadeOutFraction) {
                fadeFraction *= (1f - info.frac) / fadeOutFraction;
            }
            if(fadeIndexFraction > 0f) {
                float indexFraction = (float)(maxParticles - info.particleIndex) / (float)maxParticles;
                if(indexFraction < fadeIndexFraction) {
                    fadeFraction *= indexFraction / fadeIndexFraction;
                }
            }
            Color color = new Color(finalColor * fadeFraction + fadeColor * (1f - fadeFraction));
            */
            
            float fadeBlend;
            if(info.frac < fadeInFraction) {
                fadeBlend = 1f - info.frac / fadeInFraction;
            } else if(info.frac > (1f - fadeOutFraction)) {
                fadeBlend = (info.frac - (1f - fadeOutFraction)) / fadeOutFraction;
            } else {
                fadeBlend = 0f;
            }
            Color color = Color.Lerp(Color.Lerp(initialColor, finalColor, info.frac), fadeColor, fadeBlend);
            if(color == new Color(0, 0, 0, 0)) {
                return false;
            }
            vertices[offset + 0].color = color;
            vertices[offset + 1].color = color;
            vertices[offset + 2].color = color;
            vertices[offset + 3].color = color;
            return true;
        }

        public int CreateParticle(ref RenderView renderView, ref ParticleUpdateInfo info, ref Span<ParticleVertex> vertices, int offset) {
            if(!ParticleColors(ref info, ref vertices, offset)) {
                return 0;
            }

            ParticleTexcoords(ref info, ref vertices, offset);
            ParticleOrigin(ref renderView, ref info, out Vector3 origin);
            int numVertices = ParticleVertices(ref renderView, ref info, origin, ref vertices, offset);
            return numVertices;
        }

        public ParticleStage() {
            maxParticles = 10;
            spawnBunching = 1f;
            particleLife = 1.5f;
            shape = ParticleStageShape.Rectangle;
            randomDistribution = true;
            path = ParticleStagePath.Standard;
            direction = ParticleStageDirection.Cone;
            directionParms = new Vector4(60f, 0f, 0f, 0f);
            speedRange = new Vector2(1f, 1f);
            gravity = 1f;
            worldGravity = false;
            sizeRange = new Vector4(1f, 1f, 1f, 1f);
            fadeColor = new Color(0, 0, 0, 0);
            finalColor = Color.White;
            fadeInFraction = 0.1f;
            fadeOutFraction = 0.25f;
            numAnimationFrameRows = 1;
            numAnimationFrameColumns = 32;
            animationRate = 0f;

            cycleMsec = (int)((particleLife + deadTime) * 1000f);
        }
    }

    public class ParticleSystem : Decl {
        public ParticleStage[] parsedStages {get; private set;}
        public BoundingBox boundingBox;

        public void UpdateStageBounds(ref ParticleStage stage) {
            stage.boundingBox = MathHelper.NullBoundingBox;

            RenderView renderView = new RenderView();
            renderView.origin = Vector3.Zero;
            renderView.right = MathHelper.Vec3Right;
            renderView.forward = MathHelper.Vec3Forward;
            renderView.up = MathHelper.Vec3Up;

            idRandom steppingRandom = new idRandom();
            ParticleUpdateInfo info = new ParticleUpdateInfo();

            for(int i = 0; i < 1000; i++) {
                info.random = info.originalRandom = steppingRandom;
                for(float inCycleTime = 0; inCycleTime < stage.particleLife; inCycleTime += 0.016f) {
                    if(inCycleTime + 0.016 > stage.particleLife) {
                        inCycleTime = stage.particleLife - 0.001f;
                    }

                    info.frac = inCycleTime / stage.particleLife;
                    info.ageInSeconds = inCycleTime;

                    stage.ParticleOrigin(ref renderView, ref info, out Vector3 origin);
                    MathHelper.BoundingBoxAddPoint(ref stage.boundingBox, origin);
                }
            }

            float maxSize = 0f;
            for(float f = 0f; f <= 1f; f += 1f / 64f) {
                Vector2 size = Vector2.Lerp(new Vector2(stage.sizeRange.X, stage.sizeRange.Y), new Vector2(stage.sizeRange.Z, stage.sizeRange.W), f);
                maxSize = MathF.Max(maxSize, MathF.Max(size.X, size.Y));
            }
            maxSize += 0f;
            MathHelper.BoundingBoxExpand(ref stage.boundingBox, maxSize + stage.boundsExpansion);
        }

        void ParseVector(ref Lexer src, ref Token token, int n, out Vector4 v) {
            v = Vector4.Zero;
            int i = 0;
            while(true) {
                if(!src.ReadTokenOnLine(ref token)) {
                    break;
                }
                if(i == n) {
                    throw new InvalidDataException("ParseParticleStage:too many parms on line.");
                }
                float.TryParse(token.lexme, out var f);
                v[i] = f;
                i++;
            }
        }

        void ParseVector(ref Lexer src, ref Token token, ref float[] parms, int n) {
            int i = 0;
            while(true) {
                if(!src.ReadTokenOnLine(ref token)) {
                    break;
                }
                if(i == n) {
                    throw new InvalidDataException("ParseParticleStage:too many parms on line.");
                }
                float.TryParse(token.lexme, out var f);
                parms[i] = f;
                i++;
            }
        }

        void ParseRangeFloat(ref Lexer src, ref Token token, out Vector2 range) {
            range = Vector2.Zero;
            range.X = src.ParseFloat();
            src.ExpectTokenString("to");
            range.Y = src.ParseFloat();
        }

        void ParseRangeVector2(ref Lexer src, ref Token token, out Vector4 range) {
            float x = src.ParseFloat();
            float y = src.ParseFloat();
            src.ExpectTokenString("to");
            float z = src.ParseFloat();
            float w = src.ParseFloat();
            range = new Vector4(x, y, z, w);
        }       

        void ParseStage(ref Lexer src, out ParticleStage stage) {
            Token token = new Token();
            stage = new ParticleStage();
            while(true) {
                if(!src.ReadToken(ref token)) {
                    break;
                }
                if(token == "}") {
                    break;
                }
                if(token == "count") {
                    stage.maxParticles = Math.Max(0, src.ParseInteger());
                    continue;
                }
                if(token == "life") {
                    stage.particleLife = MathF.Max(0f, src.ParseFloat());
                    continue;
                }
                if(token == "cycles") {
                    stage.cycles = Math.Max(0, src.ParseInteger());
                    continue;
                }
                if(token == "timeOffset") {
                    stage.timeOffset = MathF.Max(0f, src.ParseFloat());
                    continue;
                }
                if(token == "deadTime") {
                    stage.deadTime = MathF.Max(0f, src.ParseFloat());
                    continue;
                }
                if(token == "randomDistribution") {
                    stage.randomDistribution = src.ParseBool();
                    continue;
                }
                if(token == "bunching") {
                    stage.spawnBunching = MathUtil.Clamp(src.ParseFloat(), 0f, 1f);
                    continue;
                }
                if(token == "shape") {
                    src.ReadTokenOnLine(ref token);
                    if(!Enum.TryParse<ParticleStageShape>(token.lexme, true, out stage.shape)) {
                        throw new InvalidDataException($"ParseParticleStage:bad shape '{token.lexme}'");   
                    }
                    ParseVector(ref src, ref token, 4, out stage.shapeParms);
                    continue;
                }
                if(token == "direction") {
                    src.ReadTokenOnLine(ref token);
                    if(!Enum.TryParse<ParticleStageDirection>(token.lexme, true, out stage.direction)) {
                        throw new InvalidDataException($"ParseParticleStage:bad direction '{token.lexme}'");   
                    }
                    ParseVector(ref src, ref token, 4, out stage.directionParms);
                    continue;
                }
                if(token == "orientation") {
                    src.ReadTokenOnLine(ref token);
                    if(!Enum.TryParse<ParticleStageOrientation>(token.lexme, true, out stage.orientation)) {
                        throw new InvalidDataException($"ParseParticleStage:bad orientation '{token.lexme}'");   
                    }
                    ParseVector(ref src, ref token, 4, out stage.orientationParms);
                    continue;
                }
                if(token == "path") {
                    src.ReadTokenOnLine(ref token);
                    if(!Enum.TryParse<ParticleStagePath>(token.lexme, true, out stage.path)) {
                        throw new InvalidDataException($"ParseParticleStage:bad path '{token.lexme}'");
                    }
                    if(stage.pathParms == null) {
                        stage.pathParms = new float[8];
                    }
                    ParseVector(ref src, ref token, ref stage.pathParms, 8);
                    continue;
                }
                if(token == "numFrameRows") {
                    stage.numAnimationFrameRows = Math.Max(0, src.ParseInteger());
                    continue;
                }
                if(token == "numFrameColumns") {
                    stage.numAnimationFrameColumns = Math.Max(0, src.ParseInteger());
                    continue;
                }                
                if(token == "speedRange") {
                    ParseRangeFloat(ref src, ref token, out stage.speedRange);
                    continue;
                }
                if(token == "rotationSpeedRange") {
                    ParseRangeFloat(ref src, ref token, out stage.rotationSpeedRange);
                    continue;
                }
                if(token == "initialAngle") {
                    stage.initialAngle = src.ParseFloat();
                    continue;
                }
                if(token == "sizeRange") {
                    ParseRangeVector2(ref src, ref token, out stage.sizeRange);
                    continue;
                }
                if(token == "fadeIn") {
                    stage.fadeInFraction = MathUtil.Clamp(src.ParseFloat(), 0f, 1f);
                    continue;
                }
                if(token == "fadeOut") {
                    stage.fadeOutFraction = MathUtil.Clamp(src.ParseFloat(), 0f, 1f);
                    continue;
                }
                if(token == "initialColor") {
                    stage.initialColor[0] = MathHelper.Ftob(src.ParseFloat() * 255f);
                    stage.initialColor[1] = MathHelper.Ftob(src.ParseFloat() * 255f);
                    stage.initialColor[2] = MathHelper.Ftob(src.ParseFloat() * 255f);
                    stage.initialColor[3] = MathHelper.Ftob(src.ParseFloat() * 255f);
                    continue;
                }
                if(token == "fadeColor") {
                    stage.fadeColor[0] = MathHelper.Ftob(src.ParseFloat() * 255f);
                    stage.fadeColor[1] = MathHelper.Ftob(src.ParseFloat() * 255f);
                    stage.fadeColor[2] = MathHelper.Ftob(src.ParseFloat() * 255f);
                    stage.fadeColor[3] = MathHelper.Ftob(src.ParseFloat() * 255f);
                    continue;
                }
                if(token == "finalColor") {
                    stage.finalColor[0] = MathHelper.Ftob(src.ParseFloat() * 255f);
                    stage.finalColor[1] = MathHelper.Ftob(src.ParseFloat() * 255f);
                    stage.finalColor[2] = MathHelper.Ftob(src.ParseFloat() * 255f);
                    stage.finalColor[3] = MathHelper.Ftob(src.ParseFloat() * 255f);
                    continue;
                }
                if(token == "offset") {
                    stage.offset[0] = src.ParseFloat();
                    stage.offset[1] = src.ParseFloat();
                    stage.offset[2] = src.ParseFloat();
                    continue;
                }
                if(token == "boundsExpansion") {
                    stage.boundsExpansion = src.ParseFloat();
                    continue;
                }
                if(token == "gravity") {
                    src.ReadTokenOnLine(ref token);
                    if(token == "world") {
                        stage.worldGravity = true;
                    } else {
                        src.UnreadToken(ref token);
                    }
                    stage.gravity = src.ParseFloat();
                    continue;
                }
                throw new InvalidDataException($"ParseParticleStage:unknown token '{token.lexme}'");
            }
            stage.cycleMsec = (int)((stage.particleLife + stage.deadTime) * 1000f);
        }   

        void WriteStage(StringBuilder buffer, int stage) {

        }

        void RebuildTextSource() {
            StringBuilder buffer = new StringBuilder();

            buffer.Append($"particleSystem \"{name}\"");
            buffer.AppendLine(" {");
            for(int i = 0; i < parsedStages.Length; i++) {
                buffer.AppendLine("\t{");
                if(parsedStages[i].offset != Vector3.Zero) {
                    buffer.AppendLine($"\t\toffset {VectorToString(parsedStages[i].offset)}");
                }
                buffer.AppendLine($"\t\tcount {parsedStages[i].maxParticles}");
                buffer.AppendLine($"\t\tlife {parsedStages[i].particleLife}");
                if(parsedStages[i].cycles > 0) {
                    buffer.AppendLine($"\t\tcycles {parsedStages[i].cycles}");
                }
                if(parsedStages[i].deadTime > 0f) {
                    buffer.AppendLine($"\t\tdeadTime {parsedStages[i].deadTime}");
                }
                buffer.AppendLine($"\t\tshape {parsedStages[i].shape.ToString().ToLower()} {VectorToString(parsedStages[i].shapeParms)}");
                buffer.AppendLine($"\t\tdirection {parsedStages[i].direction.ToString().ToLower()} {VectorToString(parsedStages[i].directionParms)}");
                buffer.AppendLine($"\t\torientation {parsedStages[i].orientation.ToString().ToLower()} {VectorToString(parsedStages[i].orientationParms)}");
                //buffer.AppendLine($"\t\tinitialColor {VectorToString(parsedStages[i].initialColor.ToVector4())}");
                if(parsedStages[i].path != ParticleStagePath.Standard && parsedStages[i].pathParms != null) {
                    buffer.Append($"\t\tpath {parsedStages[i].path} ");
                    for(int j = 0; j < parsedStages[i].pathParms.Length; j++) {
                        buffer.Append($"{parsedStages[i].pathParms[j]} ");
                    }
                    buffer.AppendLine();
                }
                buffer.AppendLine($"\t\tfadeColor {VectorToString(parsedStages[i].fadeColor.ToVector4())}");
                buffer.AppendLine($"\t\tfinalColor {VectorToString(parsedStages[i].finalColor.ToVector4())}");
                buffer.AppendLine($"\t\tfadeIn {parsedStages[i].fadeInFraction}");
                buffer.AppendLine($"\t\tfadeOut {parsedStages[i].fadeOutFraction}"); 
                buffer.AppendLine($"\t\tfade {parsedStages[i].fadeIndexFraction}");     
                buffer.AppendLine($"\t\tsizeRange {parsedStages[i].sizeRange.X} {parsedStages[i].sizeRange.Y} to {parsedStages[i].sizeRange.Z} {parsedStages[i].sizeRange.W}");
                buffer.AppendLine($"\t\tspeedRange {parsedStages[i].speedRange.X} to {parsedStages[i].speedRange.Y}");
                if(parsedStages[i].initialAngle != 0f) {
                    buffer.AppendLine($"\t\tinitialAngle {parsedStages[i].initialAngle}");
                }
                buffer.AppendLine($"\t\trotationSpeedRange {parsedStages[i].rotationSpeedRange.X} to {parsedStages[i].rotationSpeedRange.Y}");
                if(parsedStages[i].worldGravity) {
                    buffer.AppendLine($"\t\tgravity world {parsedStages[i].gravity}");
                } else {
                    buffer.AppendLine($"\t\tgravity {parsedStages[i].gravity}");
                }
                if(parsedStages[i].boundsExpansion != 0f) {
                    buffer.AppendLine($"\t\tboundsExpansion {parsedStages[i].boundsExpansion}");
                }
                buffer.AppendLine("\t}");
            }
            buffer.AppendLine("}");

            SetText(buffer.ToString().ToCharArray());
        }

        internal void Save(string filename) {
            RebuildTextSource();
            if(!string.IsNullOrEmpty(filename)) {
                Common.declManager.CreateNewDecal(DeclType.ParticleSystem, name, filename);
            }
            ReplaceSourceFileText();
        }

        internal override void Parse() {
            Token token = new Token();
            Lexer lex = new Lexer(text);

            lex.ExpectTokenString("particleSystem");
            lex.ExpectTokenTypeOnLine(TokenType.String, TokenSubType.None, ref token);
            name = token.lexme;

            List<ParticleStage> stages = new List<ParticleStage>();
            lex.ExpectTokenString("{");
            while(lex.ReadToken(ref token)) {
                if(token == "}") {
                    break;
                }
                if(token == "{") {
                    ParseStage(ref lex, out ParticleStage stage);
                    stages.Add(stage);
                    continue;
                }
            }

            if(stages.Count > 0) {
                parsedStages = stages.ToArray();
                boundingBox = MathHelper.NullBoundingBox;
                for(int i = 0; i < parsedStages.Length; i++) {
                    UpdateStageBounds(ref parsedStages[i]);
                    MathHelper.BoundingBoxAddBounds(ref boundingBox, parsedStages[i].boundingBox);
                }
            }
        }

        public override void Dispose() {

        }
    }
}