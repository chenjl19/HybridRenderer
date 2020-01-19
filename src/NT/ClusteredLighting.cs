using System;
using System.Collections.Generic;
using SharpDX;

namespace NT
{
    public enum FrustumPlane {
        Left,
        Right,
        Top,
        Bottom,
        Forward,
        Back
    }

    public struct ClusterFrustum {
        /*
        public Plane left;
        public Plane right;
        public Plane top;
        public Plane bottom;
        public Plane forward;
        public Plane back;
        */
        public Plane[] planes;
        public BoundingSphere sphere;
        public BoundingBox aabb;
    }

    public class ClusteredLighting {
        bool needUpdateFrustums;
        float minDepth;
        float maxDepth;
        public int numClustersX {get; private set;}
        public int numClustersY {get; private set;}
        public int numClustersZ {get; private set;}
        public ClusterFrustum[] frustums {get; private set;}
        public Vector4 clusteredLightingSize {get; private set;}
        public Vector4 clusteredLightingParms {get; private set;}
        public int numClusters => numClustersX * numClustersY * numClustersZ;
        CameraComponent camera;

        public const int MaxLightsOnScreen = 1024;
        public const int MaxDecalsOnScreen = 1024;
        public const int MaxProbesOnScreen = 256;
        public const int MaxShadowsOnScreen = 128;
        public const int DefaultNumClustersX = 16;
        public const int DefaultNumClustersY = 8;
        public const int DefaultNumClustersZ = 24;
        public const int DefaultNumClusters = DefaultNumClustersX * DefaultNumClustersY * DefaultNumClustersZ;
        public const float DefaultMinDepth = 5f;
        public const float DefaultMaxDepth = 500f;
        public const int MaxItemsPerCluster = 256;

        public ClusteredLighting(CameraComponent cameraComponent) {
            camera = cameraComponent;
            needUpdateFrustums = true;
            SetSplitNum(DefaultNumClustersX, DefaultNumClustersY, DefaultNumClustersZ);
            SetDepths(DefaultMinDepth, DefaultMaxDepth);
        }

        public void SetCamera(CameraComponent cameraComponent) {
            camera = cameraComponent;
            needUpdateFrustums = true;            
        }

        public void SetDepths(float min, float max) {
            minDepth = min;
            maxDepth = max;
            needUpdateFrustums = true;
        }

        public void SetSplitNum(int x, int y, int z) {
            numClustersX = x;
            numClustersY = y;
            numClustersZ = z;
            needUpdateFrustums = true;
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

        public void UpdateClusterFrustums() {
            if(frustums == null) {
                frustums = new ClusterFrustum[numClustersX * numClustersY * numClustersZ];
            }

            Vector2 clusterSize = new Vector2((float)camera.width / (float)numClustersX, (float)camera.height / (float)numClustersY);

            for(int z = 0; z < numClustersZ; z++) {
                float near;
                float far;
                if(z == 0) {
                    near = -camera.nearClipPlane;
                    far = -minDepth;
                }  else {
                    float k = MathF.Pow(maxDepth / minDepth, (float)(z - 1) / (float)(numClustersZ - 1));
                    float k2 = MathF.Pow(maxDepth / minDepth, (float)z / (float)(numClustersZ - 1));
                    near = -minDepth * k;
                    far = -minDepth * k2;
                }

                for (int y = 0; y < numClustersY; y ++) {
                    for (int x = 0; x < numClustersX; x ++) {
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
                        int index = x + y * numClustersX + z * numClustersY * numClustersX;
                        frustums[index].aabb = BoundingBox.FromPoints(corners);
                        frustums[index].sphere = BoundingSphere.FromBox(frustums[index].aabb);
                    }
                }
            }

            //slice = Mathf.Max(0f, MathHelper.Log2(fragViewDepth / StartDepth) / MathHelper.Log2(EndDepth / StartDepth) * (NumClustersZ - 1) + StartClusterZ);
            //slice = Mathf.Min(NumClustersZ - 1, Mathf.Floor(slice));
            clusteredLightingParms = new Vector4(1f / minDepth, 1f / MathHelper.Log2(maxDepth / minDepth) * (numClustersZ - 1), 1f, 0f);
            clusteredLightingSize = new Vector4(numClustersX, numClustersY, numClustersZ, numClusters);
        }

        public enum ClusterDrawType {
            Cluster,
            AABB,
            Sphere
        }

        public void DrawClusterFrustum(int x, int y, int z, Color color) {
            Scene scene = Scene.GlobalScene;

            float zNear = minDepth;
            float zFar = MathF.Min(maxDepth, camera.farClipPlane);
            Vector2 clusterSize = new Vector2(((float)camera.width / (float)numClustersX), ((float)camera.height / (float)numClustersY));
            float minZ = z == 0 ? -camera.nearClipPlane : -zNear * MathF.Pow(zFar / zNear, (float)(z - 1) / (float)(numClustersZ - 1));
            float maxZ = z == 0 ? -zNear : -zNear * MathF.Pow(zFar / zNear, (float)z / (float)(numClustersZ - 1));

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

        public void DrawCluster(int index, Color color, ClusterDrawType drawType = ClusterDrawType.Cluster) {
            Scene scene = Scene.GlobalScene;

            if(drawType == ClusterDrawType.Cluster) {
                int x = index % numClustersX;
                int y = index % (numClustersX * numClustersY) / numClustersX;
                int z = index / (numClustersX * numClustersY);
                DrawClusterFrustum(x, y, z, color);
            } else if(drawType == ClusterDrawType.Sphere) {
                scene.renderDebug.AddSphere(16, frustums[index].sphere.Radius, Vector3.TransformCoordinate(frustums[index].sphere.Center, camera.viewToWorldMatrix), color);
            } else if(drawType == ClusterDrawType.AABB) {
                var corners = frustums[index].aabb.GetCorners();
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
    }
}
